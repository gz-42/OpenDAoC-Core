﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    // A very specialized thread pool, meant to be used by the game loop and its dedicated thread exclusively.
    public sealed class GameLoopThreadPoolMultiThreaded : GameLoopThreadPool
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        // Bias factor that shapes an inverse power-law curve used to scale chunk sizes.
        // Higher values favor smaller, safer chunks for better load balancing;
        // lower values produce larger chunks for faster throughput when work is even.
        // 2.5 appears to be a good default for real-world skew.
        private const double WORK_SPLIT_BIAS_FACTOR = 2.5;
        private const int MAX_DEGREE_OF_PARALLELISM = 128;
        private const int WORKER_TIMEOUT_MS = 7500;     // Timeout for worker threads to finish their work before being interrupted and restarted.
        private const long IDLE_CYCLE = -1;             // Represents an idle worker thread cycle, used to detect stuck threads.

        // Thread pool configuration and state.
        private bool _running;
        private int _degreeOfParallelism;               // Total threads, including caller.
        private int _workerCount;                       // Number of dedicated worker threads (= _degreeOfParallelism - 1).
        private double[] _workSplitBiasTable;           // Lookup table for chunk size biasing.

        // Thread management.
        private Thread[] _workers;                      // Worker threads (excludes caller).
        private long[] _workerCycle;                    // Worker cycle phase, used to detect stuck threads.
        private Thread _watchdog;                       // Monitors worker health; restarts if needed.
        private CancellationTokenSource _shutdownToken;

        // Work coordination.
        private CountdownEvent _workerStartLatch;       // Signals when all workers are initialized.
        private ManualResetEventSlim[] _workReady;      // Per-worker event to trigger work.

        // Work dispatch.
        private Action<int> _workAction;                // Per-item work action.
        private Action _workerRoutine;                  // Worker thread routine.
        private readonly WorkState _workState = new();

        [StructLayout(LayoutKind.Explicit)]
        private class WorkState
        {
            [FieldOffset(0)]
            public int RemainingWork;                   // Total items left to process.
            [FieldOffset(128)]
            public int CompletedWorkerCount;            // Count of workers finished for current iteration.
        }

        public GameLoopThreadPoolMultiThreaded(int degreeOfParallelism)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(degreeOfParallelism, MAX_DEGREE_OF_PARALLELISM);
            _degreeOfParallelism = degreeOfParallelism;
        }

        public override void Init()
        {
            if (Interlocked.CompareExchange(ref _running, true, false))
                return;

            Configure();
            BuildChunkDivisorTable();
            StartWorkers();
            StartWatchdog();

            void Configure()
            {
                _workerCount = _degreeOfParallelism - 1;
                _workers = new Thread[_workerCount];
                _workerCycle = new long[_workerCount];
                _workerStartLatch = new(_workerCount);
                _workReady = new ManualResetEventSlim[_workerCount];
                _shutdownToken = new();
                _workerRoutine = ProcessWorkActions;
                base.Init();
            }

            void BuildChunkDivisorTable()
            {
                _workSplitBiasTable = new double[_degreeOfParallelism + 1];

                for (int i = 1; i <= _degreeOfParallelism; i++)
                    _workSplitBiasTable[i] = Math.Pow(i, WORK_SPLIT_BIAS_FACTOR);

                _workSplitBiasTable[0] = 1; // Prevent division by zero, fallback.
            }

            void StartWorkers()
            {
                for (int i = 0; i < _workerCount; i++)
                {
                    Thread worker = new(new ParameterizedThreadStart(InitWorker))
                    {
                        Name = $"{GameLoop.THREAD_NAME}_Worker_{i}",
                        IsBackground = true
                    };
                    worker.Start((i, false));
                }

                _workerStartLatch.Wait(); // If for some reason a thread fails to start, we'll be waiting here forever.
            }

            void StartWatchdog()
            {
                _watchdog = new(WatchdogLoop)
                {
                    Name = $"{GameLoop.THREAD_NAME}_Watchdog",
                    Priority = ThreadPriority.Lowest,
                    IsBackground = true
                };
                _watchdog.Start();
            }
        }

        public override void ExecuteWork(int count, Action<int> workAction)
        {
            try
            {
                if (count <= 0)
                    return;

                _workAction = workAction;
                _workState.RemainingWork = count;
                _workState.CompletedWorkerCount = 0;

                // If the count is less than the degree of parallelism, only signal the required number of workers.
                // The caller thread will also be used, so in this case we need to subtract one from the amount of workers to start.
                int workersToStart = count < _degreeOfParallelism ? count - 1 : _workerCount;

                for (int i = 0; i < workersToStart; i++)
                    _workReady[i].Set();

                _workerRoutine();
                Interlocked.Increment(ref _workState.CompletedWorkerCount);

                // Spin very tightly until all the workers have completed their work.
                // We could adjust the spin wait time if we get here early, but this is hard to predict.
                // However we really don't want to yield the CPU here, as this could delay the return by a lot.
                while (Volatile.Read(ref _workState.CompletedWorkerCount) < workersToStart + 1)
                    Thread.SpinWait(1);
            }
            catch (Exception e)
            {
                if (log.IsFatalEnabled)
                    log.Fatal($"Critical error encountered in \"{nameof(GameLoopThreadPoolMultiThreaded)}\"", e);

                GameServer.Instance.Stop();
            }
        }

        public override void PrepareForNextTick()
        {
            _workerRoutine = static () => _tickLocalPools.Reset();
            ExecuteWork(_degreeOfParallelism, null);
            _workerRoutine = ProcessWorkActions;
        }

        public override void Dispose()
        {
            if (!Interlocked.CompareExchange(ref _running, false, true))
                return;

            if (_watchdog != null && _watchdog != Thread.CurrentThread && _watchdog.IsAlive)
                _watchdog.Join();

            _workerStartLatch.Wait(); // Make sure any worker being (re)started has finished.
            _shutdownToken.Cancel();

            for (int i = 0; i < _workers.Length; i++)
            {
                Thread worker = _workers[i];

                if (worker != null && Thread.CurrentThread != worker && worker.IsAlive)
                    worker.Join();
            }
        }

        protected override void InitWorker(object obj)
        {
            (int Id, bool Restart) = ((int, bool)) obj;
            _workers[Id] = Thread.CurrentThread;
            _workerCycle[Id] = IDLE_CYCLE;
            _workReady[Id]?.Dispose();
            _workReady[Id] = new ManualResetEventSlim();
            base.InitWorker(obj);
            _workerStartLatch.Signal();

            // If this is a restart, we need to free the caller thread.
            if (Restart)
                Interlocked.Increment(ref _workState.CompletedWorkerCount);

            RunWorkerLoop(Id, _shutdownToken.Token);
        }

        private void RunWorkerLoop(int id, CancellationToken cancellationToken)
        {
            ManualResetEventSlim workReady = _workReady[id];
            ref long workerCycle = ref _workerCycle[id];
            long cycle = IDLE_CYCLE;

            while (Volatile.Read(ref _running))
            {
                try
                {
                    workReady.Wait(cancellationToken);
                    workerCycle = ++cycle;
                    workReady.Reset();
                    _workerRoutine();
                    Interlocked.Increment(ref _workState.CompletedWorkerCount); // Not in the finally block on purpose.
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ThreadInterruptedException)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"Thread \"{Thread.CurrentThread.Name}\" was interrupted");

                    break;
                }
                catch (Exception e)
                {
                    if (log.IsFatalEnabled)
                        log.Fatal($"Critical error encountered in \"{nameof(GameLoopThreadPoolMultiThreaded)}\"", e);

                    GameServer.Instance.Stop();
                    break;
                }
                finally
                {
                    workerCycle = IDLE_CYCLE;
                }
            }

            if (log.IsInfoEnabled)
                log.Info($"Thread \"{Thread.CurrentThread.Name}\" is stopping");
        }

        private void ProcessWorkActions()
        {
            int remainingWork = Volatile.Read(ref _workState.RemainingWork);

            while (remainingWork > 0)
            {
                int workersRemaining = _degreeOfParallelism - Volatile.Read(ref _workState.CompletedWorkerCount);
                int chunkSize = (int) (remainingWork / _workSplitBiasTable[workersRemaining]);

                // Prevent infinite loops.
                if (chunkSize < 1)
                    chunkSize = 1;

                int start = Interlocked.Add(ref _workState.RemainingWork, -chunkSize);
                int end = start + chunkSize;

                if (end < 1)
                    break;

                if (start < 0)
                    start = 0;

                for (int i = start; i < end; i++)
                    _workAction(i);

                remainingWork = start - 1;
            }
        }

        private void WatchdogLoop()
        {
            List<int> _workersToRestart = new();

            while (Volatile.Read(ref _running))
            {
                try
                {
                    // Remove the dead threads from the dictionary, then replace them.
                    for (int i = 0; i < _workers.Length; i++)
                    {
                        Thread worker = _workers[i];

                        // Thread is null if it was removed by the watchdog. At this point it's already being replaced, hopefully.
                        if (worker == null)
                            continue;

                        // Make sure the thread is still alive.
                        if (worker.Join(100))
                        {
                            if (log.IsWarnEnabled)
                                log.Warn($"Watchdog: Thread \"{worker.Name}\" has exited unexpectedly. Restarting...");

                            _workersToRestart.Add(i);
                        }
                        else
                        {
                            long cycle = Volatile.Read(ref _workerCycle[i]);

                            if (cycle > IDLE_CYCLE)
                            {
                                // If the thread takes more than a couple of seconds to finish its work, interrupt and restart it.
                                if (worker.Join(WORKER_TIMEOUT_MS))
                                {
                                    if (log.IsWarnEnabled)
                                        log.Warn($"Watchdog: Thread \"{worker.Name}\" has exited unexpectedly. Restarting...");

                                    _workersToRestart.Add(i);
                                }
                                else if (Volatile.Read(ref _workerCycle[i]) == cycle)
                                {
                                    if (log.IsWarnEnabled)
                                        log.Warn($"Watchdog: Thread \"{worker.Name}\" is taking too long. Attempting to restart it...");

                                    worker.Interrupt();
                                    worker.Join(); // Will never return if the thread is stuck in an infinite loop.
                                    _workersToRestart.Add(i);
                                }
                            }
                        }
                    }

                    if (_workersToRestart.Count == 0)
                        continue;

                    // Initialize the countdown event before starting any thread.
                    _workerStartLatch = new(_workersToRestart.Count);

                    foreach (int id in _workersToRestart)
                    {
                        if (log.IsWarnEnabled)
                            log.Warn($"Watchdog: Restarting thread \"{_workers[id].Name}\"");

                        _workers[id] = null;
                        Thread newThread = new(new ParameterizedThreadStart(InitWorker))
                        {
                            Name = $"{GameLoop.THREAD_NAME}_Worker_{id}",
                            IsBackground = true,
                        };
                        newThread.Start((id, true));
                    }

                    _workersToRestart.Clear();
                }
                catch (ThreadInterruptedException)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"Thread \"{Thread.CurrentThread.Name}\" was interrupted");
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Thread \"{Thread.CurrentThread.Name}\"", e);
                }
            }

            if (log.IsInfoEnabled)
                log.Info($"Thread \"{Thread.CurrentThread.Name}\" is stopping");
        }
    }

    public sealed class GameLoopThreadPoolSingleThreaded : GameLoopThreadPool
    {
        public GameLoopThreadPoolSingleThreaded() { }

        public override void ExecuteWork(int count, Action<int> workAction)
        {
            for (int i = 0; i < count; i++)
                workAction(i);
        }

        public override void PrepareForNextTick()
        {
            _tickLocalPools.Reset();
        }

        public override void Dispose() { }
    }

    public abstract class GameLoopThreadPool : IDisposable
    {
        [ThreadStatic]
        protected static TickLocalPools _tickLocalPools;

        public virtual void Init()
        {
            _tickLocalPools = new();
        }

        public abstract void ExecuteWork(int count, Action<int> workAction);

        public abstract void PrepareForNextTick();

        public abstract void Dispose();

        public T GetForTick<T>(PooledObjectKey key, Action<T> initializer) where T : IPooledObject<T>, new()
        {
            T result = _tickLocalPools != null ? _tickLocalPools.GetForTick<T>(key) : new();
            initializer?.Invoke(result);
            return result;
        }

        protected virtual void InitWorker(object obj)
        {
            _tickLocalPools = new();
        }

        protected sealed class TickLocalPools
        {
            private Dictionary<PooledObjectKey, ITickObjectPool> _localPools = new()
            {
                { PooledObjectKey.InPacket, new TickObjectPool<GSPacketIn>() },
                { PooledObjectKey.TcpOutPacket, new TickObjectPool<GSTCPPacketOut>() },
                { PooledObjectKey.UdpOutPacket, new TickObjectPool<GSUDPPacketOut>() }
            };

            public T GetForTick<T>(PooledObjectKey key) where T : IPooledObject<T>, new()
            {
                return (_localPools[key] as TickObjectPool<T>).GetForTick();
            }

            public void Reset()
            {
                foreach (var pair in _localPools)
                    pair.Value.Reset();
            }

            private sealed class TickObjectPool<T> : ITickObjectPool where T : IPooledObject<T>, new()
            {
                private const int INITIAL_CAPACITY = 64;       // Initial capacity of the pool.
                private const double TRIM_SAFETY_FACTOR = 2.5; // Trimming allowed when size > smoothed usage * this factor.
                private const int HALF_LIFE = 300_000;         // Half-life (ms) for EMA decay.
                private static double DECAY_FACTOR;            // EMA decay factor based on HALF_LIFE and tick rate.

                private T[] _items = new T[INITIAL_CAPACITY];  // Backing pool array.
                private int _used;                             // Objects rented this tick.
                private double _smoothedUsage;                 // Smoothed recent peak usage.
                private int _logicalSize;                      // Highest non-null index in use.

                static TickObjectPool()
                {
                    DECAY_FACTOR = Math.Exp(-Math.Log(2) / (GameLoop.TickDuration * HALF_LIFE / 1000.0));
                }

                public T GetForTick()
                {
                    T item;

                    if (_used < _logicalSize)
                    {
                        item = _items[_used];
                        _used++;
                    }
                    else
                    {
                        item = new();

                        if (_used >= _items.Length)
                            Array.Resize(ref _items, _items.Length * 2);

                        _items[_used++] = item;
                        _logicalSize = Math.Max(_logicalSize, _used);
                    }

                    item.IssuedTimestamp = GameLoop.GameLoopTime;
                    return item;
                }

                public void Reset()
                {
                    _smoothedUsage = Math.Max(_used, _smoothedUsage * DECAY_FACTOR + _used * (1 - DECAY_FACTOR));
                    int newLogicalSize = (int) (_smoothedUsage * TRIM_SAFETY_FACTOR);

                    if (_logicalSize > newLogicalSize)
                    {
                        for (int i = newLogicalSize; i < _logicalSize; i++)
                            _items[i] = default;

                        _logicalSize = newLogicalSize;
                    }

                    _used = 0;
                }
            }

            private interface ITickObjectPool
            {
                void Reset();
            }
        }
    }

    public enum PooledObjectKey
    {
        InPacket,
        TcpOutPacket,
        UdpOutPacket
    }

    public interface IPooledObject<T>
    {
        static abstract PooledObjectKey PooledObjectKey { get; }
        static abstract T GetForTick(Action<T> initializer);

        // The game loop tick timestamp when this object was issued.
        // Will be 0 if created outside the game loop (e.g., by a .NET worker thread without local object pools).
        long IssuedTimestamp { get; set; }
    }

    public static class PooledObjectExtensions
    {
        public static bool IsValidForTick<T>(this IPooledObject<T> obj)
        {
            return obj.IssuedTimestamp == GameLoop.GameLoopTime;
        }
    }
}
