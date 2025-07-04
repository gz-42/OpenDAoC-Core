using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DOL.Database;
using DOL.Events;
using DOL.GS.Keeps;
using DOL.GS.ServerProperties;

namespace DOL.GS
{
    /// <summary>
    /// This class represents a region in DAOC. A region is everything where you
    /// need a loadingscreen to go there. Eg. whole Albion is one Region, Midgard and
    /// Hibernia are just one region too. Darkness Falls is a region. Each dungeon, city
    /// is a region ... you get the clue. Each Region can hold an arbitary number of
    /// Zones! Camelot Hills is one Zone, Tir na Nog is one Zone (and one Region)...
    /// </summary>
    public class Region
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        #region Region Variables

        /// <summary>
        /// This is the minimumsize for object array that is allocated when
        /// the first object is added to the region must be dividable by 32 (optimization)
        /// </summary>
        public static readonly int MINIMUMSIZE = 256;


        /// <summary>
        /// This holds all objects inside this region. Their index = their id!
        /// </summary>
        protected GameObject[] m_objects;


        /// <summary>
        /// Object to lock when changing objects in the array
        /// </summary>
        public readonly Lock ObjectsSyncLock = new();

        /// <summary>
        /// This holds a counter with the absolute count of all objects that are actually in this region
        /// </summary>
        protected int m_objectsInRegion;

        /// <summary>
        /// Total number of objects in this region
        /// </summary>
        public int TotalNumberOfObjects
        {
            get { return m_objectsInRegion; }
        }

        /// <summary>
        /// This array holds a bitarray
        /// Its used to know which slots in region object array are free and what allocated
        /// This is used to accelerate inserts a lot
        /// </summary>
        protected uint[] m_objectsAllocatedSlots;

        /// <summary>
        /// This holds the index of a possible next object slot
        /// but needs further checks (basically its lastaddedobjectIndex+1)
        /// </summary>
        protected int m_nextObjectSlot;

        /// <summary>
        /// This holds the gravestones in this region for fast access
        /// Player unique id(string) -> GameGraveStone
        /// </summary>
        protected readonly Hashtable m_graveStones;
        private readonly Lock _graveStonesLock = new();

        /// <summary>
        /// Holds all the Zones inside this Region
        /// </summary>
        protected readonly List<Zone> m_zones;

        protected readonly Lock _lockAreas = new();

        /// <summary>
        /// Holds all the Areas inside this Region
        /// 
        /// ZoneID, AreaID, Area
        ///
        /// Areas can be registed to a reagion via AddArea
        /// and events will be thrown if players/npcs/objects enter leave area
        /// </summary>
        private Dictionary<ushort, IArea> m_Areas;

        protected Dictionary<ushort, IArea> Areas
        {
            get { return m_Areas; }
        }

        /// <summary>
        /// Cache for zone area mapping to quickly access all areas within a certain zone
        /// </summary>
        protected ushort[][] m_ZoneAreas;

        /// <summary>
        /// /// Cache for number of items in m_ZoneAreas array.
        /// </summary>
        protected ushort[] m_ZoneAreasCount;

        /// <summary>
        /// How often shall we remove unused objects
        /// </summary>
        protected static readonly int CLEANUPTIMER = 60000;

        /// <summary>
        /// Contains the # of players in the region
        /// </summary>
        protected int m_numPlayer = 0;

        #endregion

        #region Constructor

        private RegionData m_regionData;
        public RegionData RegionData
        {
            get { return m_regionData; }
            protected set { m_regionData = value; }
        }

        /// <summary>
        /// Factory method to create regions.  Will create a region of data.ClassType, or default to Region if 
        /// an error occurs or ClassType is not specified
        /// </summary>
        /// <param name="time"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Region Create(RegionData data)
        {
            try
            {
                Type t = typeof(Region);

                if (string.IsNullOrEmpty(data.ClassType) == false)
                {
                    t = Type.GetType(data.ClassType);

                    if (t == null)
                    {
                        t = ScriptMgr.GetType(data.ClassType);
                    }

                    if (t != null)
                    {
                        ConstructorInfo info = t.GetConstructor(new Type[] { typeof(RegionData) });

                        Region r = (Region)info.Invoke(new object[] { data });

                        if (r != null)
                        {
                            // Success with requested classtype
                            if (log.IsInfoEnabled)
                                log.InfoFormat("Created Region {0} using ClassType '{1}'", r.ID, data.ClassType);

                            return r;
                        }

                        if (log.IsErrorEnabled)
                            log.ErrorFormat("Failed to Invoke Region {0} using ClassType '{1}'", r.ID, data.ClassType);
                    }
                    else if (log.IsErrorEnabled)
                        log.ErrorFormat("Failed to find ClassType '{0}' for region {1}!", data.ClassType, data.Id);
                }
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled)
                    log.ErrorFormat("Failed to start region {0} with requested classtype: {1}.  Exception: {2}!", data.Id, data.ClassType, ex.Message);
            }

            // Create region using default type
            return new Region(data);
        }

        /// <summary>
        /// Constructs a new empty Region
        /// </summary>
        /// <param name="time">The time manager for this region</param>
        /// <param name="data">The region data</param>
        public Region(RegionData data)
        {
            m_regionData = data;
            m_objects = new GameObject[0];
            m_objectsInRegion = 0;
            m_nextObjectSlot = 0;
            m_objectsAllocatedSlots = new uint[0];

            m_graveStones = new Hashtable();

            m_zones = new List<Zone>();
            m_ZoneAreas = new ushort[64][];
            m_ZoneAreasCount = new ushort[64];
            for (int i = 0; i < 64; i++)
            {
                m_ZoneAreas[i] = new ushort[AbstractArea.MAX_AREAS_PER_ZONE];
            }

            m_Areas = new Dictionary<ushort, IArea>();
            List<string> list = null;

            if (ServerProperties.Properties.DEBUG_LOAD_REGIONS != string.Empty)
                list = Util.SplitCSV(ServerProperties.Properties.DEBUG_LOAD_REGIONS, true);

            if (list != null && list.Count > 0)
            {
                m_loadObjects = false;

                foreach (string region in list)
                {
                    if (region.ToString() == ID.ToString())
                    {
                        m_loadObjects = true;
                        break;
                    }
                }
            }

            list = Util.SplitCSV(ServerProperties.Properties.DISABLED_REGIONS, true);
            foreach (string region in list)
            {
                if (region.ToString() == ID.ToString())
                {
                    m_isDisabled = true;
                    break;
                }
            }

            list = Util.SplitCSV(ServerProperties.Properties.DISABLED_EXPANSIONS, true);
            foreach (string expansion in list)
            {
                if (expansion.ToString() == m_regionData.Expansion.ToString())
                {
                    m_isDisabled = true;
                    break;
                }
            }
        }



        /// <summary>
        /// What to do when the region collapses.
        /// This is called when instanced regions need to be closed
        /// </summary>
        public virtual void OnCollapse()
        {
            //Delete objects
            foreach (GameObject obj in m_objects)
            {
                if (obj != null)
                {
                    obj.Delete();
                    RemoveObject(obj);
                    obj.CurrentRegion = null;
                }
            }

            m_objects = null;

            foreach (Zone z in m_zones)
            {
                z.Delete();
            }

            m_zones.Clear();

            m_graveStones.Clear();

            DOL.Events.GameEventMgr.RemoveAllHandlersForObject(this);
        }


        #endregion

        /// <summary>
        /// Handles players leaving this region via a zonepoint
        /// </summary>
        /// <param name="player"></param>
        /// <param name="zonePoint"></param>
        /// <returns>false to halt processing of this request</returns>
        public virtual bool OnZonePoint(GamePlayer player, DbZonePoint zonePoint)
        {
            return true;
        }

        #region Properties

        public virtual bool IsRvR
        {
            get
            {
                switch (m_regionData.Id)
                {
                    case 163://new frontiers
                    case 165: //cathal valley
                    case 233://Sumoner hall
                    case 234://1to4BG
                    case 235://5to9BG
                    case 236://10to14BG
                    case 237://15to19BG
                    case 238://20to24BG
                    case 239://25to29BG
                    case 240://30to34BG
                    case 241://35to39BG
                    case 242://40to44BG and Test BG
                    case 244://Frontiers RvR dungeon
                    case 249://Darkness Falls - RvR dungeon
                    case 489://lvl5-9 Demons breach
                        return true;
                    default:
                        return false;
                }
            }
        }

        public virtual bool IsFrontier
        {
            get { return m_regionData.IsFrontier; }
            set { m_regionData.IsFrontier = value; }
        }

        /// <summary>
        /// Is the Region a temporary instance
        /// </summary>
        public virtual bool IsInstance
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Is this region a standard DAoC region or a custom server region
        /// </summary>
        public virtual bool IsCustom
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets whether this region is a dungeon or not
        /// </summary>
        public virtual bool IsDungeon
        {
            get
            {
                const int dungeonOffset = 8192;
                const int zoneCount = 1;

                if (Zones.Count != zoneCount)
                    return false; //Dungeons only have 1 zone!

                var zone = Zones[0];

                if (zone.XOffset == dungeonOffset && zone.YOffset == dungeonOffset)
                    return true; //Only dungeons got this offset

                return false;
            }
        }

        /// <summary>
        /// Gets the # of players in the region
        /// </summary>
        public virtual int NumPlayers
        {
            get { return m_numPlayer; }
        }

        /// <summary>
        /// The Region Name eg. Region000
        /// </summary>
        public virtual string Name
        {
            get { return m_regionData.Name; }
        }
        //Dinberg: Changed this to virtual, so that Instances can take a unique Name, for things like quest instances.

        /// <summary>
        /// The Regi on Description eg. Cursed Forest
        /// </summary>
        public virtual string Description
        {
            get { return m_regionData.Description; }
        }
        //Dinberg: Virtual, so that we can change this if need be, for quests eg 'Hermit Dinbargs Cave'
        //or for the hell of it, eg Jordheim (Instance).

        /// <summary>
        /// The ID of the Region eg. 21
        /// </summary>
        public virtual ushort ID
        {
            get { return m_regionData.Id; }
        }
        //Dinberg: Changed this to virtual, so that Instances can take a unique ID.

        /// <summary>
        /// The Region Server IP ... for future use
        /// </summary>
        public string ServerIP
        {
            get { return m_regionData.Ip; }
        }

        /// <summary>
        /// The Region Server Port ... for future use
        /// </summary>
        public ushort ServerPort
        {
            get { return m_regionData.Port; }
        }

        /// <summary>
        /// An ArrayList of all Zones within this Region
        /// </summary>
        public List<Zone> Zones
        {
            get { return m_zones; }
        }

        /// <summary>
        /// Returns the object array of this region
        /// </summary>
        public GameObject[] Objects
        {
            get { return m_objects; }
        }

        /// <summary>
        /// Gets or Sets the region expansion (we use client expansion + 1)
        /// </summary>
        public virtual int Expansion
        {
            get { return m_regionData.Expansion + 1; }
        }

        /// <summary>
        /// Gets or Sets the water level in this region
        /// </summary>
        public virtual int WaterLevel
        {
            get { return m_regionData.WaterLevel; }
        }

        /// <summary>
        /// Gets or Sets diving flag for region
        /// Note: This flag should normally be checked at the zone level
        /// </summary>
        public virtual bool IsRegionDivingEnabled
        {
            get { return m_regionData.DivingEnabled; }
        }

        /// <summary>
        /// Does this region contain housing?
        /// </summary>
        public virtual bool HousingEnabled
        {
            get { return m_regionData.HousingEnabled; }
        }

        /// <summary>
        /// Should this region use the housing manager?
        /// Standard regions always use the housing manager if housing is enabled, custom regions might not.
        /// </summary>
        public virtual bool UseHousingManager
        {
            get { return HousingEnabled; }
        }

        /// <summary>
        /// Gets the current region time in milliseconds
        /// </summary>
        public virtual long Time
        {
            get { return GameLoop.GameLoopTime; }
        }

        protected bool m_isDisabled = false;
        /// <summary>
        /// Is this region disabled
        /// </summary>
        public virtual bool IsDisabled
        {
            get { return m_isDisabled; }
        }

        protected bool m_loadObjects = true;
        /// <summary>
        /// Will this region load objects
        /// </summary>
        public virtual bool LoadObjects
        {
            get { return m_loadObjects; }
        }

        //Dinberg: Added this for instances.
        /// <summary>
        /// Added to allow instances; the 'appearance' of the region, the map the GameClient uses.
        /// </summary>
        public virtual ushort Skin
        {
            get { return ID; }
        }

        /// <summary>
        /// Should this region respond to time manager send requests
        /// Normally yes, might be disabled for some instances.
        /// </summary>
        public virtual bool UseTimeManager
        {
            get { return true; }
            set { }
        }


        /// <summary>
        /// Each region can return it's own game time
        /// By default let WorldMgr handle it
        /// </summary>
        public virtual uint GameTime
        {
            get { return WorldMgr.GetCurrentGameTime(); }
            set { }
        }


        /// <summary>
        /// Get the day increment for this region.
        /// By default let WorldMgr handle it
        /// </summary>
        public virtual uint DayIncrement
        {
            get { return WorldMgr.GetDayIncrement(); }
            set { }
        }

        /// <summary>
        /// Create a keep for this region
        /// </summary>
        /// <returns></returns>
        public virtual AbstractGameKeep CreateGameKeep()
        {
            return new GameKeep();
        }
        
        /// <summary>
        /// Create a new Relic keep for this region
        /// </summary>
        /// <returns></returns>
        public virtual AbstractGameKeep CreateRelicGameKeep()
        {
            return new RelicGameKeep();
        }

        /// <summary>
        /// Create the appropriate GameKeepTower for this region
        /// </summary>
        /// <returns></returns>
        public virtual AbstractGameKeep CreateGameKeepTower()
        {
            return new GameKeepTower();
        }

        /// <summary>
        /// Create the appropriate GameKeepComponent for this region
        /// </summary>
        /// <returns></returns>
        public virtual GameKeepComponent CreateGameKeepComponent()
        {
            return new GameKeepComponent();
        }

        /// <summary>
        /// Determine if the current time is AM.
        /// </summary>
        public virtual bool IsAM
        {
            get
            {
                if (m_isPM)
                    return false;
                return true;
            }
        }

        private bool m_isPM;
        /// <summary>
        /// Determine if the current time is PM.
        /// </summary>
        public virtual bool IsPM
        {
            get
            {
                uint cTime = GameTime;

                uint hour = cTime / 1000 / 60 / 60;
                bool pm = hour >= 12 && hour <= 23;

                m_isPM = pm;

                return m_isPM;
            }
            set { m_isPM = value; }
        }

        private bool m_isNightTime;
        /// <summary>
        /// Determine if current time is between 6PM and 6AM, can be used for conditional spells.
        /// </summary>
        public virtual bool IsNightTime
        {
            get
            {
                uint cTime = GameTime;

                uint hour = cTime / 1000 / 60 / 60;
                bool night = hour is >= 18 or < 6;

                m_isNightTime = night;
                    
                return m_isNightTime;
            }
            set { m_isNightTime = value; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts the RegionMgr
        /// </summary>
        public void StartRegionMgr()
        {
            Notify(RegionEvent.RegionStart, this);
        }

        /// <summary>
        /// Stops the RegionMgr
        /// </summary>
        public void StopRegionMgr()
        {
            Notify(RegionEvent.RegionStop, this);
        }

        /// <summary>
        /// Reallocates objects array with given size
        /// </summary>
        /// <param name="count">The size of new objects array, limited by MAXOBJECTS</param>
        public virtual void PreAllocateRegionSpace(int count)
        {
            if (count > Properties.REGION_MAX_OBJECTS)
                count = Properties.REGION_MAX_OBJECTS;
            lock (ObjectsSyncLock)
            {
                if (m_objects.Length > count) return;
                GameObject[] newObj = new GameObject[count];
                Array.Copy(m_objects, newObj, m_objects.Length);
                if (count / 32 + 1 > m_objectsAllocatedSlots.Length)
                {
                    uint[] slotarray = new uint[count / 32 + 1];
                    Array.Copy(m_objectsAllocatedSlots, slotarray, m_objectsAllocatedSlots.Length);
                    m_objectsAllocatedSlots = slotarray;
                }
                m_objects = newObj;
            }
        }

        /// <summary>
        /// Loads the region from database
        /// </summary>
        /// <param name="mobObjs"></param>
        /// <param name="mobCount"></param>
        /// <param name="merchantCount"></param>
        /// <param name="itemCount"></param>
        /// <param name="bindCount"></param>
        public virtual void LoadFromDatabase(DbMob[] mobObjs, ref long mobCount, ref long merchantCount, ref long itemCount, ref long bindCount)
        {
            if (!LoadObjects)
                return;

            Assembly gasm = Assembly.GetAssembly(typeof(GameServer));
            var staticObjs = DOLDB<DbWorldObject>.SelectObjects(DB.Column("Region").IsEqualTo(ID));
            var bindPoints = DOLDB<DbBindPoint>.SelectObjects(DB.Column("Region").IsEqualTo(ID));
            int count = mobObjs.Length + staticObjs.Count;
            if (count > 0) PreAllocateRegionSpace(count + 100);
            int myItemCount = staticObjs.Count;
            int myMobCount = 0;
            int myMerchantCount = 0;
            int myBindCount = bindPoints.Count;
            string allErrors = string.Empty;

            if (mobObjs.Length > 0)
            {
                Parallel.ForEach(mobObjs, (mob) =>
                {
                    GameNPC myMob = null;
                    string error = string.Empty;
  
                    // Default Classtype
                    string classtype = ServerProperties.Properties.GAMENPC_DEFAULT_CLASSTYPE;
                    
                    // load template if any
                    INpcTemplate template = null;
                    if(mob.NPCTemplateID != -1)
                    {
                    	template = NpcTemplateMgr.GetTemplate(mob.NPCTemplateID);
                    }
                    

                    if (Properties.USE_NPCGUILDSCRIPTS && mob.Guild.Length > 0 && mob.Realm >= 0 && mob.Realm <= (int)eRealm._Last)
                    {
                        Type type = ScriptMgr.FindNPCGuildScriptClass(mob.Guild, (eRealm)mob.Realm);
                        if (type != null)
                        {
                            try
                            {
                                
                                myMob = (GameNPC)type.Assembly.CreateInstance(type.FullName);
                               	
                            }
                            catch (Exception e)
                            {
                                if (log.IsErrorEnabled)
                                    log.Error("LoadFromDatabase", e);
                            }
                        }
                    }

                    if (myMob == null)
                    {
                    	if(template != null && template.ClassType != null && template.ClassType.Length > 0 && template.ClassType != DbMob.DEFAULT_NPC_CLASSTYPE && template.ReplaceMobValues)
                    	{
                			classtype = template.ClassType;
                    	}
                        else if (mob.ClassType != null && mob.ClassType.Length > 0 && mob.ClassType != DbMob.DEFAULT_NPC_CLASSTYPE)
                        {
                            classtype = mob.ClassType;
                        }

                        try
                        {
                            myMob = (GameNPC)gasm.CreateInstance(classtype, false);
                        }
                        catch
                        {
                            error = classtype;
                        }

                        if (myMob == null)
                        {
                            foreach (Assembly asm in ScriptMgr.Scripts)
                            {
                                try
                                {
                                    myMob = (GameNPC)asm.CreateInstance(classtype, false);
                                    error = string.Empty;
                                }
                                catch
                                {
                                    error = classtype;
                                }

                                if (myMob != null)
                                    break;
                            }

                            if (myMob == null)
                            {
                                myMob = new GameNPC();
                                error = classtype;
                            }
                        }
                    }

                    if (!allErrors.Contains(error))
                        allErrors += " " + error + ",";

                    if (myMob != null)
                    {
                        try
                        {
                            myMob.LoadFromDatabase(mob);

                            if (myMob is GameMerchant)
                            {
                                Interlocked.Increment(ref myMerchantCount);
                            }
                            else
                            {
                                Interlocked.Increment(ref myMobCount);
                            }
                        }
                        catch (Exception e)
                        {
                            if (log.IsErrorEnabled)
                                log.Error("Failed: " + myMob.GetType().FullName + ":LoadFromDatabase(" + mob.GetType().FullName + ");", e);
                            throw;
                        }

                        myMob.AddToWorld();
                    }
                });
            }

            if (staticObjs.Count > 0)
            {
                Parallel.ForEach(staticObjs, (item) =>
                {
                    GameStaticItem myItem;
                    if (!string.IsNullOrEmpty(item.ClassType))
                    {
                        myItem = gasm.CreateInstance(item.ClassType, false) as GameStaticItem;
                        if (myItem == null)
                        {
                            foreach (Assembly asm in ScriptMgr.Scripts)
                            {
                                try
                                {
                                    myItem = (GameStaticItem)asm.CreateInstance(item.ClassType, false);
                                }
                                catch { }
                                if (myItem != null)
                                    break;
                            }
                            if (myItem == null)
                                myItem = new GameStaticItem();
                        }
                    }
                    else
                        myItem = new GameStaticItem();

                    myItem.LoadFromDatabase(item);
                    myItem.AddToWorld();
                });
            }

            foreach (DbBindPoint bindPoint in bindPoints)
                AddArea(new Area.BindArea("bind point", bindPoint));

            if (myMobCount + myItemCount + myMerchantCount + myBindCount > 0)
            {
                if (log.IsInfoEnabled)
                    log.Info(string.Format("Region: {0} ({1}) loaded {2} mobs, {3} merchants, {4} items {5} bindpoints", Description, ID, myMobCount, myMerchantCount, myItemCount, myBindCount));

                if (log.IsDebugEnabled)
                    log.Debug("Used Memory: " + GC.GetTotalMemory(false) / 1024 / 1024 + "MB");

                if (allErrors != string.Empty && log.IsErrorEnabled)
                    log.Error("Error loading the following NPC ClassType(s), GameNPC used instead:" + allErrors.TrimEnd(','));
            }

            Interlocked.Add(ref mobCount, myMobCount);
            Interlocked.Add(ref merchantCount, myMerchantCount);
            Interlocked.Add(ref itemCount, myItemCount);
            Interlocked.Add(ref bindCount, myBindCount);
        }

        /// <summary>
        /// Adds an object to the region and assigns the object an id
        /// </summary>
        /// <param name="obj">A GameObject to be added to the region</param>
        /// <returns>success</returns>
        internal bool AddObject(GameObject obj)
        {
            //Assign a new id
            lock (ObjectsSyncLock)
            {
                if (obj.ObjectID != -1)
                {
                    if (obj.ObjectID < m_objects.Length && obj == m_objects[obj.ObjectID - 1])
                    {
                        if (log.IsWarnEnabled)
                            log.Warn($"Object is already in \"{Description}\". ({obj})");

                        return false;
                    }

                    if (log.IsWarnEnabled)
                        log.Warn($"Object already has an OID. ({obj})");

                    return false;
                }

                GameObject[] objectsRef = m_objects;

                //*** optimized object management for memory saving primary but keeping it very fast - Blue ***

                // find first free slot for the object
                int objID = m_nextObjectSlot;
                if (objID >= m_objects.Length || m_objects[objID] != null)
                {

                    // we are at array end, are there any holes left?
                    if (m_objects.Length > m_objectsInRegion)
                    {
                        // yes there are some places left in current object array, try to find them
                        // by using the bit array (can check 32 slots at once!)

                        int i = m_objects.Length / 32;
                        // INVARIANT: i * 32 is always lower or equal to m_objects.Length (integer division property)
                        if (i * 32 == m_objects.Length)
                        {
                            i -= 1;
                        }

                        bool found = false;
                        objID = -1;

                        while (!found && (i >= 0))
                        {
                            if (m_objectsAllocatedSlots[i] != 0xffffffff)
                            {
                                // we found a free slot
                                // => search for exact place

                                int currentIndex = i * 32;
                                int upperBound = (i + 1) * 32;
                                while (!found && (currentIndex < m_objects.Length) && (currentIndex < upperBound))
                                {
                                    if (m_objects[currentIndex] == null)
                                    {
                                        found = true;
                                        objID = currentIndex;
                                    }

                                    currentIndex++;
                                }

                                // INVARIANT: at this point, found must be true (otherwise the underlying data structure is corrupt)
                            }

                            i--;
                        }
                    }
                    else
                    { // our array is full, we must resize now to fit new objects

                        if (objectsRef.Length == 0)
                        {

                            // there is no array yet, so set it to a minimum at least
                            objectsRef = new GameObject[MINIMUMSIZE];
                            Array.Copy(m_objects, objectsRef, m_objects.Length);
                            objID = 0;

                        }
                        else if (objectsRef.Length >= Properties.REGION_MAX_OBJECTS)
                        {

                            // no available slot
                            if (log.IsErrorEnabled)
                                log.Error($"Can't add new object to \"{Description}\" because it is full. ({obj})");

                            return false;
                        }
                        else
                        {

                            // we need to add a certain amount to grow
                            int size = (int)(m_objects.Length * 1.20);
                            if (size < m_objects.Length + 256)
                                size = m_objects.Length + 256;
                            if (size > Properties.REGION_MAX_OBJECTS)
                                size = Properties.REGION_MAX_OBJECTS;
                            objectsRef = new GameObject[size]; // grow the array by 20%, at least 256
                            Array.Copy(m_objects, objectsRef, m_objects.Length);
                            objID = m_objects.Length; // new object adds right behind the last object in old array

                        }
                        // resize the bitarray as well
                        int diff = objectsRef.Length / 32 - m_objectsAllocatedSlots.Length;
                        if (diff >= 0)
                        {
                            uint[] newBitArray = new uint[Math.Max(m_objectsAllocatedSlots.Length + diff + 50, 100)];	// add at least 100 integers, makes it resize less often, serves 3200 new objects, only 400 bytes
                            Array.Copy(m_objectsAllocatedSlots, newBitArray, m_objectsAllocatedSlots.Length);
                            m_objectsAllocatedSlots = newBitArray;
                        }
                    }
                }

                if (objID < 0)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"There was an unexpected problem while adding new object to \"{Description}\". ({obj})");

                    return false;
                }

                // if we found a slot add the object
                GameObject oidObj = objectsRef[objID];
                if (oidObj == null)
                {
                    objectsRef[objID] = obj;
                    m_nextObjectSlot = objID + 1;
                    m_objectsInRegion++;
                    obj.ObjectID = objID + 1;
                    m_objectsAllocatedSlots[objID / 32] |= (uint)1 << (objID % 32);
                    Thread.MemoryBarrier();
                    m_objects = objectsRef;

                    if (obj is GamePlayer)
                    {
                        ++m_numPlayer;
                    }
                    else
                    {
                        if (obj is GameGravestone)
                        {
                            lock (_graveStonesLock)
                            {
                                m_graveStones[obj.InternalID] = obj;
                            }
                        }
                    }
                }
                else
                {
                    // no available slot
                    if (log.IsErrorEnabled)
                        log.Error($"Can't add new object to \"{Description}\" because  OID is already used by {oidObj}. ({obj})");

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Removes the object with the specified ID from the region
        /// </summary>
        /// <param name="obj">A GameObject to be removed from the region</param>
        internal void RemoveObject(GameObject obj)
        {
            lock (ObjectsSyncLock)
            {
                int index = obj.ObjectID - 1;
                if (index < 0)
                {
                    return;
                }

                if (obj is GamePlayer)
                {
                    --m_numPlayer;
                }
                else
                {
                    if (obj is GameGravestone)
                    {
                        lock (_graveStonesLock)
                        {
                            m_graveStones.Remove(obj.InternalID);
                        }
                    }
                }

                GameObject inPlace = m_objects[obj.ObjectID - 1];
                if (inPlace == null)
                {
                    if (log.IsErrorEnabled)
                    {
                        log.Error("RemoveObject conflict! OID" + obj.ObjectID + " " + obj.Name + "(" + obj.CurrentRegionID + ") but there was no object at that slot");
                        log.Error(new StackTrace().ToString());
                    }

                    return;
                }
                if (obj != inPlace)
                {
                    if (log.IsErrorEnabled)
                    {
                        log.Error("RemoveObject conflict! OID" + obj.ObjectID + " " + obj.Name + "(" + obj.CurrentRegionID + ") but there was another object already " + inPlace.Name + " region:" + inPlace.CurrentRegionID + " state:" + inPlace.ObjectState);
                        log.Error(new StackTrace().ToString());
                    }

                    return;
                }

                if (m_objects[index] != obj)
                {
                    if (log.IsErrorEnabled)
                        log.Error("Object OID is already used by another object! (used by:" + m_objects[index].ToString() + ")");
                }
                else
                {
                    m_objects[index] = null;
                    m_nextObjectSlot = index;
                    m_objectsAllocatedSlots[index / 32] &= ~(uint)(1 << (index % 32));
                }
                obj.ObjectID = -1; // invalidate object id
                m_objectsInRegion--;
            }
        }

        /// <summary>
        /// Searches for players gravestone in this region
        /// </summary>
        /// <param name="player"></param>
        /// <returns>the found gravestone or null</returns>
        public GameGravestone FindGraveStone(GamePlayer player)
        {
            lock (_graveStonesLock)
            {
                return (GameGravestone)m_graveStones[player.InternalID];
            }
        }

        /// <summary>
        /// Gets the object with the specified ID
        /// </summary>
        /// <param name="id">The ID of the object to get</param>
        /// <returns>The object with the specified ID, null if it didn't exist</returns>
        public GameObject GetObject(ushort id)
        {
            if (m_objects == null || id <= 0 || id > m_objects.Length)
                return null;
            return m_objects[id - 1];
        }

        /// <summary>
        /// Returns the zone that contains the specified x and y values
        /// </summary>
        /// <param name="x">X value for the zone you're retrieving</param>
        /// <param name="y">Y value for the zone you're retrieving</param>
        /// <returns>The zone you're retrieving or null if it couldn't be found</returns>
        public Zone GetZone(int x, int y)
        {
            int varX = x;
            int varY = y;
            foreach (Zone zone in m_zones)
            {
                if (zone.XOffset <= varX && zone.YOffset <= varY && (zone.XOffset + zone.Width) > varX && (zone.YOffset + zone.Height) > varY)
                    return zone;
            }
            return null;
        }

        /// <summary>
        /// Gets the X offset for the specified zone
        /// </summary>
        /// <param name="x">X value for the zone's offset you're retrieving</param>
        /// <param name="y">Y value for the zone's offset you're retrieving</param>
        /// <returns>The X offset of the zone you specified or 0 if it couldn't be found</returns>
        public int GetXOffInZone(int x, int y)
        {
            Zone z = GetZone(x, y);
            if (z == null)
                return 0;
            return x - z.XOffset;
        }

        /// <summary>
        /// Gets the Y offset for the specified zone
        /// </summary>
        /// <param name="x">X value for the zone's offset you're retrieving</param>
        /// <param name="y">Y value for the zone's offset you're retrieving</param>
        /// <returns>The Y offset of the zone you specified or 0 if it couldn't be found</returns>
        public int GetYOffInZone(int x, int y)
        {
            Zone z = GetZone(x, y);
            if (z == null)
                return 0;
            return y - z.YOffset;
        }

        /// <summary>
        /// Check if this region is a capital city
        /// </summary>
        /// <returns>True, if region is a capital city, else false</returns>
        public virtual bool IsCapitalCity
        {
            get
            {
                switch (this.Skin)
                {
                    case 10: return true; // Camelot City
                    case 101: return true; // Jordheim
                    case 201: return true; // Tir na Nog
                    default: return false;
                }
            }
        }

        /// <summary>
        /// Check if this region is a housing zone
        /// </summary>
        /// <returns>True, if region is a housing zone, else false</returns>
        public virtual bool IsHousing
        {
            get
            {
                switch (this.Skin) // use the skin of the region
                {
                    case 2: return true; 	// Housing alb
                    case 102: return true; 	// Housing mid
                    case 202: return true; 	// Housing hib
                    default: return false;
                }
            }
        }

        /// <summary>
        /// Check if the given region is Atlantis.
        /// </summary>
        /// <param name="regionId"></param>
        /// <returns></returns>
        public static bool IsAtlantis(int regionId)
        {
            return (regionId == 30 || regionId == 73 || regionId == 130);
        }

        #endregion

        #region Area

        /// <summary>
        /// Adds an area to the region and updates area-zone cache
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        public virtual IArea AddArea(IArea area)
        {
            lock (_lockAreas)
            {
                ushort nextAreaID = 0;

                foreach (ushort areaID in m_Areas.Keys)
                {
                    if (areaID >= nextAreaID)
                    {
                        nextAreaID = (ushort)(areaID + 1);
                    }
                }

                area.ID = nextAreaID;
                m_Areas.Add(area.ID, area);

                int zonePos = 0;
                foreach (Zone zone in Zones)
                {
                    if (area.IsIntersectingZone(zone))
                    	m_ZoneAreas[zonePos][m_ZoneAreasCount[zonePos]++] = area.ID;
                    
                    zonePos++;
                }
                return area;
            }
        }

        /// <summary>
        /// Removes an area from the list of areas and updates area-zone cache
        /// </summary>
        /// <param name="area"></param>
        public virtual void RemoveArea(IArea area)
        {
            lock (_lockAreas)
            {
                if (m_Areas.ContainsKey(area.ID) == false)
                {
                    return;
                }

                m_Areas.Remove(area.ID);
                int ZoneCount = Zones.Count;

                for (int zonePos = 0; zonePos < ZoneCount; zonePos++)
                {
                    for (int areaPos = 0; areaPos < m_ZoneAreasCount[zonePos]; areaPos++)
                    {
                        if (m_ZoneAreas[zonePos][areaPos] == area.ID)
                        {
                            // move the remaining m_ZoneAreas array one to the left

                            for (int i = areaPos; i < m_ZoneAreasCount[zonePos] - 1; i++)
                            {
                                m_ZoneAreas[zonePos][i] = m_ZoneAreas[zonePos][i + 1];
                            }

                            m_ZoneAreasCount[zonePos]--;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the areas for given location,
        /// less performant than getAreasOfZone so use other on if possible
        /// </summary>
        public virtual List<IArea> GetAreasOfSpot(IPoint3D point)
        {
            Zone zone = GetZone(point.X, point.Y);
            return GetAreasOfZone(zone, point);
        }

        /// <summary>
        /// Gets the areas for a certain spot,
        /// less performant than getAreasOfZone so use other on if possible
        /// </summary>
        public virtual List<IArea> GetAreasOfSpot(int x, int y, int z)
        {
            Zone zone = GetZone(x, y);
            Point3D p = new Point3D(x, y, z);
            return GetAreasOfZone(zone, p);
        }

        public virtual List<IArea> GetAreasOfZone(Zone zone, IPoint3D p)
        {
            return GetAreasOfZone(zone, p, true);
        }

        /// <summary>
        /// Gets the areas for a certain spot
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="p"></param>
        /// <param name="checkZ"></param>
        /// <returns></returns>
        public virtual List<IArea> GetAreasOfZone(Zone zone, IPoint3D p, bool checkZ)
        {
            lock (_lockAreas)
            {
                int zoneIndex = Zones.IndexOf(zone);
                var areas = new List<IArea>();

                if (zoneIndex >= 0)
                {
                    try
                    {
                        for (int i = 0; i < m_ZoneAreasCount[zoneIndex]; i++)
                        {
                            IArea area = m_Areas[m_ZoneAreas[zoneIndex][i]];
                            if (area.IsContaining(p, checkZ))
                            {
                                areas.Add(area);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (log.IsErrorEnabled)
                            log.Error("GetArea exception.Area count " + m_ZoneAreasCount[zoneIndex], e);
                    }
                }

                return areas;
            }
        }

        public virtual List<IArea> GetAreasOfZone(Zone zone, int x, int y, int z)
        {
            lock (_lockAreas)
            {
                int zoneIndex = Zones.IndexOf(zone);
                var areas = new List<IArea>();

                if (zoneIndex >= 0)
                {
                    try
                    {
                        for (int i = 0; i < m_ZoneAreasCount[zoneIndex]; i++)
                        {
                            IArea area = m_Areas[m_ZoneAreas[zoneIndex][i]];
                            if (area.IsContaining(x, y, z))
                                areas.Add(area);
                        }
                    }
                    catch (Exception e)
                    {
                        if (log.IsErrorEnabled)
                            log.Error("GetArea exception.Area count " + m_ZoneAreasCount[zoneIndex], e);
                    }
                }
                return areas;
            }
        }

        #endregion

        #region Notify

        public virtual void Notify(DOLEvent e, object sender, EventArgs args)
        {
            GameEventMgr.Notify(e, sender, args);
        }

        public virtual void Notify(DOLEvent e, object sender)
        {
            Notify(e, sender, null);
        }

        public virtual void Notify(DOLEvent e)
        {
            Notify(e, null, null);
        }

        public virtual void Notify(DOLEvent e, EventArgs args)
        {
            Notify(e, null, args);
        }

        #endregion

        #region Get in radius

        public void GetInRadius<T>(Point3D point, eGameObjectType objectType, ushort radius, List<T> list) where T : GameObject
        {
            if (list.Count > 0)
            {
                if (log.IsErrorEnabled)
                    log.Error($"GetInRadius: list is not empty, clearing it.{Environment.NewLine}{Environment.StackTrace}");

                list.Clear();
            }

            Zone startingZone = GetZone(point.X, point.Y);

            if (startingZone == null)
                return;

            startingZone.GetObjectsInRadius(point, objectType, radius, list);
            uint sqRadius = (uint) radius * radius;

            foreach (Zone currentZone in m_zones)
            {
                if (currentZone != startingZone && currentZone.ObjectCount > 0 && CheckShortestDistance(currentZone, point.X, point.Y, sqRadius))
                    currentZone.GetObjectsInRadius(point, objectType, radius, list);
            }
        }

        /// <summary>
        /// get the shortest distance from a point to a zone
        /// </summary>
        /// <param name="zone">The zone to check</param>
        /// <param name="x">X value of the point</param>
        /// <param name="y">Y value of the point</param>
        /// <param name="squareRadius">The square radius to compare the distance with</param>
        /// <returns>True if the distance is shorter false either</returns>
        private static bool CheckShortestDistance(Zone zone, int x, int y, uint squareRadius)
        {
            //  coordinates of zone borders
            int xLeft = zone.XOffset;
            int xRight = zone.XOffset + zone.Width;
            int yTop = zone.YOffset;
            int yBottom = zone.YOffset + zone.Height;
            long distance = 0;

            if ((y >= yTop) && (y <= yBottom))
            {
                int xdiff = Math.Min(Math.Abs(x - xLeft), Math.Abs(x - xRight));
                distance = (long)xdiff * xdiff;
            }
            else
            {
                if ((x >= xLeft) && (x <= xRight))
                {
                    int ydiff = Math.Min(Math.Abs(y - yTop), Math.Abs(y - yBottom));
                    distance = (long)ydiff * ydiff;
                }
                else
                {
                    int xdiff = Math.Min(Math.Abs(x - xLeft), Math.Abs(x - xRight));
                    int ydiff = Math.Min(Math.Abs(y - yTop), Math.Abs(y - yBottom));
                    distance = (long)xdiff * xdiff + (long)ydiff * ydiff;
                }
            }

            return (distance <= squareRadius);
        }

        [Obsolete("Deprecated. Use GetInRadius<T>(Point3D point, eGameObjectType objectType, ushort radius, List<T> list) instead.")]
        public List<GameStaticItem> GetItemsInRadius(Point3D point, ushort radius)
        {
            List<GameStaticItem> result = new();
            GetInRadius<GameStaticItem>(point, eGameObjectType.ITEM, radius, result);
            return result;
        }

        [Obsolete("Deprecated. Use GetInRadius<T>(Point3D point, eGameObjectType objectType, ushort radius, List<T> list) instead.")]
        public List<GameNPC> GetNPCsInRadius(Point3D point, ushort radius)
        {
            List<GameNPC> result = new();
            GetInRadius<GameNPC>(point, eGameObjectType.NPC, radius, result);
            return result;
        }

        [Obsolete("Deprecated. Use GetInRadius<T>(Point3D point, eGameObjectType objectType, ushort radius, List<T> list) instead.")]
        public List<GamePlayer> GetPlayersInRadius(Point3D point, ushort radius)
        {
            List<GamePlayer> result = new();
            GetInRadius<GamePlayer>(point, eGameObjectType.PLAYER, radius, result);
            return result;
        }

        [Obsolete("Deprecated. Use GetInRadius<T>(Point3D point, eGameObjectType objectType, ushort radius, List<T> list) instead.")]
        public List<GameDoorBase> GetDoorsInRadius(Point3D point, ushort radius)
        {
            List<GameDoorBase> result = new();
            GetInRadius<GameDoorBase>(point, eGameObjectType.DOOR, radius, result);
            return result;
        }

        #endregion
    }

	#region Helpers classes

	/// <summary>
	/// Holds a Object and it's distance towards the center
	/// </summary>
	public class PlayerDistEntry
	{
		public PlayerDistEntry(GamePlayer o, int distance)
		{
			Player = o;
			Distance = distance;
		}

		public GamePlayer Player;
		public int Distance;
	}

	/// <summary>
	/// Holds a Object and it's distance towards the center
	/// </summary>
	public class NPCDistEntry
	{
		public NPCDistEntry(GameNPC o, int distance)
		{
			NPC = o;
			Distance = distance;
		}

		public GameNPC NPC;
		public int Distance;
	}

	/// <summary>
	/// Holds a Object and it's distance towards the center
	/// </summary>
	public class ItemDistEntry
	{
		public ItemDistEntry(GameStaticItem o, int distance)
		{
			Item = o;
			Distance = distance;
		}

		public GameStaticItem Item;
		public int Distance;
	}

	/// <summary>
	/// Holds a Object and it's distance towards the center
	/// </summary>
	public class DoorDistEntry
	{
		public DoorDistEntry(GameDoorBase d, int distance)
		{
			Door = d;
			Distance = distance;
		}

		public GameDoorBase Door;
		public int Distance;
	}

	#endregion
}
