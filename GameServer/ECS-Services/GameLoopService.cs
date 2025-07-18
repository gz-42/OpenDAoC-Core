﻿using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using ECS.Debug;

namespace DOL.GS
{
    public static class GameLoopService
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = nameof(GameLoopService);
        private const string SERVICE_NAME_BEGIN = $"{SERVICE_NAME}_Begin";
        private const string SERVICE_NAME_END = $"{SERVICE_NAME}_End";

        private static int _preTickActionCount;
        private static int _postTickActionCount;
        private static ConcurrentQueue<IPostedAction> _preTickActions = new();
        private static ConcurrentQueue<IPostedAction> _postTickActions = new();

        public static void BeginTick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME_BEGIN;
            Diagnostics.StartPerfCounter(SERVICE_NAME_BEGIN);

            if (Volatile.Read(ref _preTickActionCount) > 0)
            {
                GameLoop.ExecuteWork(Interlocked.Exchange(ref _preTickActionCount, 0), static _ =>
                {
                    if (_preTickActions.TryDequeue(out IPostedAction result))
                    {
                        try
                        {
                            result.Invoke();
                        }
                        catch (Exception e)
                        {
                            if (log.IsErrorEnabled)
                                log.Error($"Critical error encountered in {SERVICE_NAME_BEGIN}: {e}");
                        }
                    }
                });
            }

            GameLoop.PrepareForNextTick();
            Diagnostics.StopPerfCounter(SERVICE_NAME_BEGIN);
        }

        public static void EndTick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME_END;
            Diagnostics.StartPerfCounter(SERVICE_NAME_END);

            if (Volatile.Read(ref _postTickActionCount) > 0)
            {
                GameLoop.ExecuteWork(Interlocked.Exchange(ref _postTickActionCount, 0), static _ =>
                {
                    if (_postTickActions.TryDequeue(out IPostedAction result))
                    {
                        try
                        {
                            result.Invoke();
                        }
                        catch (Exception e)
                        {
                            if (log.IsErrorEnabled)
                                log.Error($"Critical error encountered in {SERVICE_NAME_END}: {e}");
                        }
                    }
                });
            }

            Diagnostics.StopPerfCounter(SERVICE_NAME_END);
        }

        public static void PostBeforeTick<TState>(Action<TState> action, TState state)
        {
            _preTickActions.Enqueue(new PostedAction<TState>(action, state));
            Interlocked.Increment(ref _preTickActionCount);
        }

        public static void PostAfterTick<TState>(Action<TState> action, TState state)
        {
            _postTickActions.Enqueue(new PostedAction<TState>(action, state));
            Interlocked.Increment(ref _postTickActionCount);
        }

        private readonly struct PostedAction<T> : IPostedAction
        {
            public readonly Action<T> Action;
            public readonly T State;

            public PostedAction(Action<T> action, T state)
            {
                Action = action;
                State = state;
            }

            public void Invoke()
            {
                Action(State);
            }
        }

        private interface IPostedAction
        {
            void Invoke();
        }
    }
}
