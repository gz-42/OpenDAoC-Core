using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DOL.Database;
using DOL.Events;
using DOL.GS.Housing;
using DOL.GS.PacketHandler;
using DOL.GS.Quests;
using DOL.Language;

namespace DOL.GS
{
	/// <summary>
	/// This class holds all information that
	/// EVERY object in the game world needs!
	/// </summary>
	public abstract class GameObject : Point3D
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		#region State/Random/Type

		/// <summary>
		/// Holds the current state of the object
		/// </summary>
		public enum eObjectState : byte
		{
			/// <summary>
			/// Active, visibly in world
			/// </summary>
			Active,
			/// <summary>
			/// Inactive, currently being moved or stuff
			/// </summary>
			Inactive,
			/// <summary>
			/// Deleted, waiting to be cleaned up
			/// </summary>
			Deleted
		}

		/// <summary>
		/// The Object's state! This is needed because
		/// when we remove an object it isn't instantly
		/// deleted but the state is merely set to "Deleted"
		/// This prevents the object from vanishing when
		/// there still might be enumerations running over it.
		/// A timer will collect the deleted objects and free
		/// them at certain intervals.
		/// </summary>
		protected volatile eObjectState m_ObjectState;

		/// <summary>
		/// Returns the current state of the object.
		/// Object's with state "Deleted" should not be used!
		/// </summary>
		public virtual eObjectState ObjectState
		{
			get { return m_ObjectState; }
			set { m_ObjectState = value; }
		}

		public abstract eGameObjectType GameObjectType { get; }

		#endregion

		#region Position

		private ushort _rawHeading;

		public virtual string OwnerID { get; set; }
		public virtual eRealm Realm { get; set; }
		public virtual Region CurrentRegion { get; set; }
		public virtual ushort CurrentRegionID
		{
			get => CurrentRegion == null ? (ushort) 0 : CurrentRegion.ID;
			set => CurrentRegion = WorldMgr.GetRegion(value);
		}
		public Zone CurrentZone => CurrentRegion?.GetZone(X, Y);
		public SubZoneObject SubZoneObject { get; set; } // Used for subzone management.
		public virtual ushort Heading
		{
			get => (ushort) (_rawHeading & 0xFFF);
			set => _rawHeading = value;
		}
		public ushort RawHeading => _rawHeading; // Includes extra bits that clients send.

		/// <summary>
		/// Returns the angle towards a target spot in degrees, clockwise
		/// </summary>
		/// <param name="tx">target x</param>
		/// <param name="ty">target y</param>
		/// <returns>the angle towards the spot</returns>
		public float GetAngle( IPoint2D point )
		{
			float headingDifference = GetHeading(point) - Heading;

			if (headingDifference < 0)
				headingDifference += 4096.0f;

			return headingDifference * 360.0f / 4096.0f;
		}

        /// <summary>
        /// Get distance to a point
        /// </summary>
        /// <param name="point">Target point</param>
        /// <returns>Distance or int.MaxValue if distance cannot be calculated</returns>
        public override int GetDistanceTo( IPoint3D point )
        {
			GameObject obj = point as GameObject;

			if ( obj == null || this.CurrentRegionID == obj.CurrentRegionID )
			{
				return base.GetDistanceTo( point );
			}
			else
			{
				return int.MaxValue;
			}
        }

        /// <summary>
        /// Get distance to a point (with z-axis adjustment)
        /// </summary>
        /// <param name="point">Target point</param>
        /// <param name="zfactor">Z-axis factor - use values between 0 and 1 to decrease the influence of Z-axis</param>
        /// <returns>Adjusted distance or int.MaxValue if distance cannot be calculated</returns>
        public override int GetDistanceTo( IPoint3D point, double zfactor )
        {
			GameObject obj = point as GameObject;

			if ( obj == null || this.CurrentRegionID == obj.CurrentRegionID )
			{
				return base.GetDistanceTo( point, zfactor );
			}
			else
			{
				return int.MaxValue;
			}
        }

		/// <summary>
		/// Checks if an object is within a given radius, optionally ignoring z values
		/// </summary>
		/// <param name="obj">Target object</param>
		/// <param name="radius">Radius</param>
		/// <param name="ignoreZ">Ignore Z values</param>
		/// <returns>False if the object is null, in a different region, or outside the radius; otherwise true</returns>
		public bool IsWithinRadius(GameObject obj, int radius)
		{
			if (obj == null)
				return false;

			if (this.CurrentRegionID != obj.CurrentRegionID)
				return false;

			return base.IsWithinRadius(obj, radius);
		}

		public virtual bool IsObjectInFront(GameObject target, double heading, int alwaysTrueRange = 32)
		{
			if (target == null)
				return false;

			float angle = GetAngle(target);

			if (angle >= 360 - heading / 2 || angle < heading / 2)
				return true;

			// If the target is closer than 32 units, it is considered always in view.
			// Tested and works this way for normal evade, parry, block (in 1.69).
			return IsWithinRadius(target, alwaysTrueRange);
		}

		/// <summary>
		/// Checks if object is underwater
		/// </summary>
		public virtual bool IsUnderwater
		{
			get
			{
				if (CurrentRegion == null || CurrentZone == null)
					return false;
				// Special land areas below the waterlevel in NF
				if (CurrentRegion.ID == 163)
				{
					// Mount Collory
					if ((Y > 664000) && (Y < 670000) && (X > 479000) && (X < 488000)) return false;
					if ((Y > 656000) && (Y < 664000) && (X > 472000) && (X < 488000)) return false;
					if ((Y > 624000) && (Y < 654000) && (X > 468500) && (X < 488000)) return false;
					if ((Y > 659000) && (Y < 683000) && (X > 431000) && (X < 466000)) return false;
					if ((Y > 646000) && (Y < 659001) && (X > 431000) && (X < 460000)) return false;
					if ((Y > 624000) && (Y < 646001) && (X > 431000) && (X < 455000)) return false;
					if ((Y > 671000) && (Y < 683000) && (X > 431000) && (X < 471000)) return false;
					// Breifine
					if ((Y > 558000) && (Y < 618000) && (X > 456000) && (X < 479000)) return false;
					// Cruachan Gorge
					if ((Y > 586000) && (Y < 618000) && (X > 360000) && (X < 424000)) return false;
					if ((Y > 563000) && (Y < 578000) && (X > 360000) && (X < 424000)) return false;
					// Emain Macha
					if ((Y > 505000) && (Y < 555000) && (X > 428000) && (X < 444000)) return false;
					// Hadrian's Wall
					if ((Y > 500000) && (Y < 553000) && (X > 603000) && (X < 620000)) return false;
					// Snowdonia
					if ((Y > 633000) && (Y < 678000) && (X > 592000) && (X < 617000)) return false;
					if ((Y > 662000) && (Y < 678000) && (X > 581000) && (X < 617000)) return false;
					// Sauvage Forrest
					if ((Y > 584000) && (Y < 615000) && (X > 626000) && (X < 681000)) return false;
					// Uppland
					if ((Y > 297000) && (Y < 353000) && (X > 610000) && (X < 652000)) return false;
					// Yggdra
					if ((Y > 408000) && (Y < 421000) && (X > 671000) && (X < 693000)) return false;
					if ((Y > 364000) && (Y < 394000) && (X > 674000) && (X < 716000)) return false;
				}

				return Z < CurrentZone.Waterlevel;
			}
		}

		/// <summary>
		/// Holds all areas this object is currently within
		/// </summary>
		public virtual IList<IArea> CurrentAreas
		{
			get => CurrentZone.GetAreasOfSpot(this);
			set { }
		}

		protected House m_currentHouse;
		/// <summary>
		/// Either the house an object is in or working on (player editing a house)
		/// </summary>
		public virtual House CurrentHouse
		{
			get { return m_currentHouse; }
			set { m_currentHouse = value; }
		}

		/// <summary>
		/// Is this object in a house
		/// </summary>
		protected bool m_inHouse;
		public virtual bool InHouse
		{
			get { return m_inHouse; }
			set { m_inHouse = value; }
		}

		/// <summary>
		/// Is this object visible to another?
		/// This does not check for stealth.
		/// </summary>
		/// <param name="checkObject"></param>
		/// <returns></returns>
		public virtual bool IsVisibleTo(GameObject checkObject)
		{
			if (checkObject == null ||
				CurrentRegion != checkObject.CurrentRegion ||
				InHouse != checkObject.InHouse ||
				(InHouse && checkObject.InHouse && CurrentHouse != checkObject.CurrentHouse))
			{
				return false;
			}

			return true;
		}

		#endregion

		#region Level/Name/Model/GetName/GetPronoun/GetExamineMessage

		/// <summary>
		/// The level of the Object
		/// </summary>
		protected byte m_level = 0; // Default to 0 to force AutoSetStats() to be called when level set

		/// <summary>
		/// The name of the Object
		/// </summary>
		protected string m_name;
		
		/// <summary>
		/// The guild name of the Object
		/// </summary>
		protected string m_guildName;

		/// <summary>
		/// The model of the Object
		/// </summary>
		protected ushort m_model;
		
		
		/// <summary>
		/// Gets or Sets the current level of the Object
		/// </summary>
		public virtual byte Level
		{
			get { return m_level; }
			set { m_level = value; }
		}

		/// <summary>
		/// Gets or Sets the effective level of the Object
		/// </summary>
		public virtual int EffectiveLevel
		{
			get { return Level; }
			set { }
		}

		/// <summary>
		/// What level is displayed to another player
		/// </summary>
		public virtual byte GetDisplayLevel(GamePlayer player)
		{
			return Level;
		}

		/// <summary>
		/// Gets or Sets the current Name of the Object
		/// </summary>
		public virtual string Name
		{
			get { return m_name; }
			set { m_name = value; }
		}

		public virtual string GuildName
		{
			get { return m_guildName; }
			set { m_guildName = value; }
		}

		/// <summary>
		/// Gets or Sets the current Model of the Object
		/// </summary>
		public virtual ushort Model
		{
			get { return m_model; }
			set { m_model = value; }
		}

        /// <summary>
        /// Whether or not the object can be attacked.
        /// </summary>
        public virtual bool IsAttackable
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsAttackableDoor
        {
            get
            {
                if (this.Realm == eRealm.None)
                    return true;

                return false;
            }
        }

        protected int m_health;
        public virtual int Health
        {
            get => m_health;
            set
            {
                int maxHealth = MaxHealth;

                if (value >= maxHealth)
                    m_health = maxHealth;
                else
                    m_health = value > 0 ? value : 0;
            }
        }

        protected int m_maxHealth;
        public virtual int MaxHealth
        {
            get => m_maxHealth;
            set => m_maxHealth = value;
        }

        public virtual byte HealthPercent => (byte) (MaxHealth <= 0 ? 0 : Math.Clamp(Health * 100 / MaxHealth, 0, 100));
        public virtual byte HealthPercentGroupWindow => HealthPercent;

        public virtual string GetName(int article, bool firstLetterUppercase, string lang, ITranslatableObject obj)
        {
            switch (lang)
            {
                case "EN":
                    {
                        return GetName(article, firstLetterUppercase);
                    }
                default:
                    {
                        if (obj is GameNPC)
                        {
                            var translation = (DbLanguageGameNpc)LanguageMgr.GetTranslation(lang, obj);
                            if (translation != null) return translation.Name;
                        }

						return GetName(article, firstLetterUppercase);;
                    }
            }
        }

		private const string m_vowels = "aeuio";

		/// <summary>
		/// Returns name with article for nouns
		/// </summary>
		/// <param name="article">0=definite, 1=indefinite</param>
		/// <param name="firstLetterUppercase">Forces the first letter of the returned string to be upper case</param>
		/// <returns>name of this object (includes article if needed)</returns>
		public virtual string GetName(int article, bool firstLetterUppercase)
		{
			/*
			 * http://www.camelotherald.com/more/888.shtml
			 * - All monsters names whose names begin with a vowel should now use the article 'an' instead of 'a'.
			 * 
			 * http://www.camelotherald.com/more/865.shtml
			 * - Instances where objects that began with a vowel but were prefixed by the article "a" (a orb of animation) have been corrected.
			 */

			if (Name.Length < 1)
				return string.Empty;

			// actually this should be only for Named mobs (like dragon, legion) but there is no way to find that out
			if (char.IsUpper(Name[0]) && this is GameLiving) // proper noun
			{
				return Name;
			}

			if (article == 0)
			{
				if (firstLetterUppercase)
					return LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "GameObject.GetName.Article1", Name);
				else
					return LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "GameObject.GetName.Article2", Name);
			}
			else
			{
				// if first letter is a vowel
				if (m_vowels.IndexOf(Name[0]) != -1)
				{
					if (firstLetterUppercase)
						return LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "GameObject.GetName.Article3", Name);
					else
						return LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "GameObject.GetName.Article4", Name);
				}
				else
				{
					if (firstLetterUppercase)
						return LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "GameObject.GetName.Article5", Name);
					else
						return LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "GameObject.GetName.Article6", Name);
				}
			}
		}

        public String Capitalize(bool capitalize, String text)
        {
            if (!capitalize) return text;

            string result = string.Empty;
            if (text == null || text.Length <= 0) return result;
            result = text[0].ToString().ToUpper();
            if (text.Length > 1) result += text.Substring(1, text.Length - 1);
            return result;
        }

		/// <summary>
		/// Pronoun of this object in case you need to refer it in 3rd person
		/// http://webster.commnet.edu/grammar/cases.htm
		/// </summary>
		/// <param name="firstLetterUppercase"></param>
		/// <param name="form">0=Subjective, 1=Possessive, 2=Objective</param>
		/// <returns>pronoun of this object</returns>
		public virtual string GetPronoun(int form, bool firstLetterUppercase)
		{
			switch (form)
			{
				default: // Subjective
					if (firstLetterUppercase)
						return LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "GameObject.GetPronoun.Pronoun1");
					else
						return LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "GameObject.GetPronoun.Pronoun2");
				case 1: // Possessive
					if (firstLetterUppercase)
						return LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "GameObject.GetPronoun.Pronoun3");
					else
						return LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "GameObject.GetPronoun.Pronoun4");
				case 2: // Objective
					if (firstLetterUppercase)
						return LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "GameObject.GetPronoun.Pronoun5");
					else
						return LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "GameObject.GetPronoun.Pronoun6");
			}
		}

		/// <summary>
		/// Creates an array list of examine messages to return to the player upon targeting the object
		/// </summary>
		/// <param name="player">The GamePlayer examining/targeting this object</param>
		/// <returns>Multiple translated string messages</returns>
		public virtual IList GetExamineMessages(GamePlayer player)
		{
			IList list = new ArrayList(4);
			// Message: You target [{0}].
			list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObject.GetExamineMessages.YouTarget", GetName(0, false)));
			return list;
		}

		public virtual bool IsStealthed => false;

		#endregion

		#region IDs/Database

		/// <summary>
		/// True if this object is saved in the DB
		/// </summary>
		protected bool m_saveInDB;

		/// <summary>
		/// The objectID. This is -1 as long as the object is not added to a region!
		/// </summary>
		protected int m_ObjectID = -1;

		/// <summary>
		/// The internalID. This is the unique ID of the object in the DB!
		/// </summary>
		protected string m_InternalID;

		/// <summary>
		/// Gets or Sets the current ObjectID of the Object
		/// This is done automatically by the Region and should
		/// not be done manually!!!
		/// </summary>
		public int ObjectID
		{
			get { return m_ObjectID; }
			set { m_ObjectID = value; }
		}

		/// <summary>
		/// Gets or Sets the internal ID (DB ID) of the Object
		/// </summary>
		public virtual string InternalID
		{
			get { return m_InternalID; }
			set { m_InternalID = value; }
		}

		/// <summary>
		/// Sets the state for this object on whether or not it is saved in the database
		/// </summary>
		public bool SaveInDB
		{
			get { return m_saveInDB; }
			set { m_saveInDB = value; }
		}

		/// <summary>
		/// Saves an object into the database
		/// </summary>
		public virtual void SaveIntoDatabase()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		public virtual void LoadFromDatabase(DataObject obj)
		{
			InternalID = obj.ObjectId;
		}

		/// <summary>
		/// Deletes a character from the DB
		/// </summary>
		public virtual void DeleteFromDatabase()
		{
			GameBoat boat = BoatMgr.GetBoatByOwner(InternalID);
			if (boat != null)
				boat.DeleteFromDatabase();
		}

		#endregion

		#region Add-/Remove-/Create-/Move-

		/// <summary>
		/// Creates this object in the gameworld
		/// </summary>
		/// <param name="regionID">region target</param>
		/// <param name="x">x target</param>
		/// <param name="y">y target</param>
		/// <param name="z">z target</param>
		/// <param name="heading">heading</param>
		/// <returns>true if created successfully</returns>
		public virtual bool Create(ushort regionID, int x, int y, int z, ushort heading)
		{
			if (m_ObjectState == eObjectState.Active)
				return false;
			CurrentRegionID = regionID;
			m_x = x;
			m_y = y;
			m_z = z;
			Heading = heading;
			return AddToWorld();
		}

		/// <summary>
		/// Creates the item in the world
		/// </summary>
		/// <returns>true if object was created</returns>
		public virtual bool AddToWorld()
		{
			if (m_ObjectState == eObjectState.Active)
				return false;

			if (CurrentRegion == null)
			{
				if (log.IsWarnEnabled)
					log.Warn($"Invalid region for ({this})");

				return false;
			}

			Zone zone = CurrentRegion.GetZone(X, Y);

			if (zone == null)
			{
				if (log.IsWarnEnabled)
					log.Warn($"Couldn't find a zone for ({this})");

				return false;
			}

			if (!CurrentRegion.AddObject(this) || !zone.AddObject(this))
				return false;

			Notify(GameObjectEvent.AddToWorld, this);
			ObjectState = eObjectState.Active;
			m_spawnTick = GameLoop.GameLoopTime;

			if (!_isDataQuestsLoaded)
				LoadDataQuests();

			return true;
		}

		/// <summary>
		/// Removes the item from the world
		/// </summary>
		public virtual bool RemoveFromWorld()
		{
			if (CurrentRegion == null || ObjectState != eObjectState.Active)
				return false;

			Notify(GameObjectEvent.RemoveFromWorld, this);
			ObjectState = eObjectState.Inactive;

			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				player.Out.SendObjectRemove(this);

			CurrentRegion.RemoveObject(this);
			ClearObjectsInRadiusCache();
			return true;
		}

		/// <summary>
		/// Move this object to a GameLocation
		/// </summary>
		/// <param name="loc"></param>
		/// <returns></returns>
		public virtual bool MoveTo(GameLocation loc)
		{
			return MoveTo(loc.RegionID, loc.X, loc.Y, loc.Z, loc.Heading);
		}

		/// <summary>
		/// Moves the item from one spot to another spot, possible even
		/// over region boundaries
		/// </summary>
		/// <param name="regionID">new regionid</param>
		/// <param name="x">new x</param>
		/// <param name="y">new y</param>
		/// <param name="z">new z</param>
		/// <param name="heading">new heading</param>
		/// <returns>true if moved</returns>
		public virtual bool MoveTo(ushort regionID, int x, int y, int z, ushort heading)
		{
			if (m_ObjectState != eObjectState.Active)
				return false;

			Region rgn = WorldMgr.GetRegion(regionID);

			if (rgn == null)
				return false;

			Zone newZone = rgn.GetZone(x, y);
			if (newZone == null)
				return false;

			Notify(GameObjectEvent.MoveTo, this, new MoveToEventArgs(regionID, x, y, z, heading));

			if (!RemoveFromWorld())
				return false;
			m_x = x;
			m_y = y;
			m_z = z;
			Heading = heading;
			CurrentRegionID = regionID;
			return AddToWorld();
		}

		/// <summary>
		/// Marks this object as deleted!
		/// </summary>
		public virtual void Delete()
		{
			Notify(GameObjectEvent.Delete, this);
			RemoveFromWorld();
			ObjectState = eObjectState.Deleted;
			ClearObjectsInRadiusCache();
			GameEventMgr.RemoveAllHandlersForObject(this);
		}

		/// <summary>
		/// Holds the GameTick of when this object was added to the world
		/// </summary>
		protected long m_spawnTick = 0;

		/// <summary>
		/// Gets the GameTick of when this object was added to the world
		/// </summary>
		public long SpawnTick
		{
			get { return m_spawnTick; }
			set { m_spawnTick = value; }
		}

		#endregion

		#region Quests

		/// <summary>
		/// A cache of every DBDataQuest object
		/// </summary>
		protected static Dictionary<ushort, List<DbDataQuest>> _dataQuestCache = null;

		/// <summary>
		/// List of DataQuests available for this object
		/// </summary>
		protected List<DataQuest> _dataQuests = new();
		protected readonly Lock _dataQuestsLock = new();
		private bool _isDataQuestsLoaded;

		/// <summary>
		/// Fill the data quest cache with all DBDataQuest objects
		/// </summary>
		public static void FillDataQuestCache()
		{
			Dictionary<ushort, List<DbDataQuest>> newCache = new();
			int count = 0;

			foreach (DbDataQuest quest in GameServer.Database.SelectAllObjects<DbDataQuest>())
			{
				if (!newCache.TryGetValue(quest.StartRegionID, out List<DbDataQuest> list))
				{
					list = new();
					newCache[quest.StartRegionID] = list;
				}

				list.Add(quest);
				count++;
			}

			_dataQuestCache = newCache;

			if (log.IsInfoEnabled)
				log.Info($"Data quest cache initialized with {count} quests for {newCache.Count} regions");
		}

		/// <summary>
		/// Get a preloaded list of all data quests
		/// </summary>
		public static List<DbDataQuest> DataQuestCache => _dataQuestCache.SelectMany(k => k.Value).ToList();

		/// <summary>
		/// Load any data driven quests for this object
		/// </summary>
		public void LoadDataQuests(GamePlayer loader = null)
		{
			_dataQuests.Clear();
			Dictionary<ushort, List<DbDataQuest>> cacheSnapshot = _dataQuestCache; // Thread-safe read.

			if (cacheSnapshot.TryGetValue(CurrentRegionID, out var regionQuests))
			{
				foreach (DbDataQuest quest in regionQuests)
					LoadQuest(this, quest, loader);
			}

			if (cacheSnapshot.TryGetValue(0, out var globalQuests))
			{
				foreach (DbDataQuest quest in globalQuests)
					LoadQuest(this, quest, loader);
			}

			_isDataQuestsLoaded = true;

			static void LoadQuest(GameObject obj, DbDataQuest quest, GamePlayer loader)
			{
				if (quest.StartName != obj.Name)
					return;

				DataQuest dq = new(quest, obj);
				obj.AddDataQuest(dq);

				if (loader != null && !string.IsNullOrEmpty(dq.LastErrorText))
					ChatUtil.SendErrorMessage(loader, dq.LastErrorText);
			}
		}

		public void AddDataQuest(DataQuest quest)
		{
			if (_dataQuests.Contains(quest) == false)
				_dataQuests.Add(quest);
		}

		public void RemoveDataQuest(DataQuest quest)
		{
			if (_dataQuests.Contains(quest))
				_dataQuests.Remove(quest);
		}

		/// <summary>
		/// All the data driven quests for this object
		/// </summary>
		public List<DataQuest> DataQuestList
		{
			get { return _dataQuests; }
		}

		#endregion Quests

		#region Interact

        /// <summary>
        /// The distance this object can be interacted with
        /// </summary>
        public virtual int InteractDistance
        {
            get { return WorldMgr.INTERACT_DISTANCE; }
        }

		/// <summary>
		/// This function is called from the ObjectInteractRequestHandler
		/// </summary>
		/// <param name="player">GamePlayer that interacts with this object</param>
		/// <returns>false if interaction is prevented</returns>
		public virtual bool Interact(GamePlayer player)
		{
			if (player.Client.Account.PrivLevel == 1 && !this.IsWithinRadius(player, InteractDistance))
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObject.Interact.TooFarAway", GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				Notify(GameObjectEvent.InteractFailed, this, new InteractEventArgs(player));
				return false;
			}

			Notify(GameObjectEvent.Interact, this, new InteractEventArgs(player));
			player.Notify(GameObjectEvent.InteractWith, player, new InteractWithEventArgs(this));

			foreach (DataQuest q in DataQuestList)
			{
				// Notify all our potential quests of the interaction so we can check for quest offers
				q.Notify(GameObjectEvent.Interact, this, new InteractEventArgs(player));
			}

			return true;
		}

		#endregion

		#region Combat

		/// <summary>
		/// This living takes damage
		/// </summary>
		/// <param name="ad">AttackData containing damage details</param>
		public virtual void TakeDamage(AttackData ad)
		{
			TakeDamage(ad.Attacker, ad.DamageType, ad.Damage, ad.CriticalDamage);
		}

		/// <summary>
		/// This method is called whenever this living 
		/// should take damage from some source
		/// </summary>
		/// <param name="source">the damage source</param>
		/// <param name="damageType">the damage type</param>
		/// <param name="damageAmount">the amount of damage</param>
		/// <param name="criticalAmount">the amount of critical damage</param>
		public virtual void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			Notify(GameObjectEvent.TakeDamage, this, new TakeDamageEventArgs(source, damageType, damageAmount, criticalAmount));
		}

		#endregion

		#region ConLevel/DurLevel

		public int GetConLevel(GameObject compare)
		{
			return ConLevels.GetConLevel(EffectiveLevel, compare.EffectiveLevel);
		}

		public static int GetConLevel(int level, int compareLevel)
		{
			return ConLevels.GetConLevel(level, compareLevel);
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

		#region ObjectsInRadius

		private const int MAX_POOL_SIZE_PER_TYPE = 10;
		private readonly Lock _objectInRadiusCachesLock = new();
		private readonly Dictionary<eGameObjectType, ObjectsInRadiusCache> _objectsInRadiusCaches = new();
		private readonly Dictionary<eGameObjectType, List<IList>> _listPools = new();
		private readonly Dictionary<eGameObjectType, int> _poolIndexes = new();
		private long _lastPoolCleanupTime = 0;

		public void ClearObjectsInRadiusCache()
		{
			lock (_objectInRadiusCachesLock)
			{
				_objectsInRadiusCaches.Clear();
				_listPools.Clear();
				_poolIndexes.Clear();
			}
		}

		public List<T> GetObjectsInRadius<T>(eGameObjectType objectType, ushort radiusToCheck) where T : GameObject
		{
			if (CurrentRegion == null)
				return new(); // Should never happen.

			lock (_objectInRadiusCachesLock)
			{
				if (!_objectsInRadiusCaches.TryGetValue(objectType, out ObjectsInRadiusCache cache))
				{
					cache = new ObjectsInRadiusCache(new List<T>(), 0, 0);
					_objectsInRadiusCaches[objectType] = cache;
				}

				if (cache.ExpireTime >= GameLoop.GameLoopTime)
				{
					// If the radius being checked is smaller than the cached radius, build a filtered list.
					if (cache.Radius > radiusToCheck)
					{
						List<T> filtered = RentPooledList<T>(objectType);

						// While this saves a call to `CurrentRegion.GetInRadius<T>`, it could still be a bit slow.
						// The alternative would be to sort the cached list by distance and use binary search to find the first object within the radius.
						// But whether that would be faster or not is debatable, and would depend on how often the cache is hit with a smaller radius.
						foreach (T obj in cache.List)
						{
							if (IsWithinRadius(obj, radiusToCheck))
								filtered.Add(obj);
						}

						return filtered;
					}
					else if (cache.Radius == radiusToCheck)
					{
						List<T> copy = RentPooledList<T>(objectType);
						copy.AddRange((List<T>) cache.List);
						return copy;
					}
				}

				// If the cache is no longer valid or if the radius being checked is bigger than the cached radius, refresh the cache.
				List<T> cachedList = (List<T>) cache.List;
				cachedList.Clear();
				CurrentRegion.GetInRadius(this, objectType, radiusToCheck, cachedList);
				cache.Set(cachedList, radiusToCheck, GameLoop.GameLoopTime + 500);
				List<T> result = RentPooledList<T>(objectType);
				result.AddRange(cachedList);
				return result;
			}
		}

		public List<GamePlayer> GetPlayersInRadius(ushort radiusToCheck)
		{
			return GetObjectsInRadius<GamePlayer>(eGameObjectType.PLAYER, radiusToCheck);
		}

		public List<GameNPC> GetNPCsInRadius(ushort radiusToCheck)
		{
			return GetObjectsInRadius<GameNPC>(eGameObjectType.NPC, radiusToCheck);
		}

		public List<GameStaticItem> GetItemsInRadius(ushort radiusToCheck)
		{
			return GetObjectsInRadius<GameStaticItem>(eGameObjectType.ITEM, radiusToCheck);
		}

		public List<GameDoorBase> GetDoorsInRadius(ushort radiusToCheck)
		{
			return GetObjectsInRadius<GameDoorBase>(eGameObjectType.DOOR, radiusToCheck);
		}

		private void CleanupPoolsIfNeeded()
		{
			long currentTime = GameLoop.GameLoopTime;

			if (currentTime <= _lastPoolCleanupTime)
				return;

			_lastPoolCleanupTime = currentTime;

			foreach (var pair in _poolIndexes)
			{
				if (pair.Value <= 0)
					continue;

				List<IList> pool = _listPools[pair.Key];

				for (int i = 0; i < pool.Count; i++)
					pool[i].Clear();

				_poolIndexes[pair.Key] = 0;
			}
		}

		private List<T> RentPooledList<T>(eGameObjectType objectType)
		{
			CleanupPoolsIfNeeded();

			if (!_listPools.TryGetValue(objectType, out var pool))
			{
				pool = new();
				_listPools[objectType] = pool;
				_poolIndexes[objectType] = 0;
			}

			int currentIndex = _poolIndexes[objectType]++;

			if (currentIndex < pool.Count)
				return (List<T>) pool[currentIndex];
			else if (pool.Count < MAX_POOL_SIZE_PER_TYPE)
			{
				List<T> newList = new();
				pool.Add(newList);
				return newList;
			}
			else
				return new(); // Pool is at max size, fallback to untracked allocation.
		}

		private class ObjectsInRadiusCache
		{
			public IList List { get; set; }
			public ushort Radius { get; set; }
			public long ExpireTime { get; set; }

			public ObjectsInRadiusCache(IList list, ushort radius, long expireTime)
			{
				Set(list, radius, expireTime);
			}

			public void Set(IList list, ushort radius, long expireTime)
			{
				List = list;
				Radius = radius;
				ExpireTime = expireTime;
			}
		}

		#endregion

		#region Item/Money

		/// <summary>
		/// Called when the object is about to get an item from someone
		/// </summary>
		/// <param name="source">Source from where to get the item</param>
		/// <param name="item">Item to get</param>
		/// <returns>true if the item was successfully received</returns>
		public virtual bool ReceiveItem(GameLiving source, DbInventoryItem item)
		{
			foreach (DataQuest quest in DataQuestList)
			{
				quest.Notify(GameLivingEvent.ReceiveItem, this, new ReceiveItemEventArgs(source, this, item));
			}

			if (item == null || item.OwnerID == null)
			{
				// item was taken
				return true;
			}

			return false;
		}

		/// <summary>
		/// Called when the object is about to get an item from someone
		/// </summary>
		/// <param name="source">Source from where to get the item</param>
		/// <param name="templateID">templateID for item to add</param>
		/// <returns>true if the item was successfully received</returns>
		public virtual bool ReceiveItem(GameLiving source, string templateID)
		{
			DbItemTemplate template = GameServer.Database.FindObjectByKey<DbItemTemplate>(templateID);
			if (template == null)
			{
				if (log.IsErrorEnabled)
					log.Error("Item Creation: ItemTemplate not found ID=" + templateID);
				return false;
			}

			return ReceiveItem(source, GameInventoryItem.Create(template));
		}

		/// <summary>
		/// Receive an item from a living
		/// </summary>
		/// <param name="source"></param>
		/// <param name="item"></param>
		/// <returns>true if player took the item</returns>
		public virtual bool ReceiveItem(GameLiving source, WorldInventoryItem item)
		{
			return ReceiveItem(source, item.Item);
		}

		/// <summary>
		/// Called when the object is about to get money from someone
		/// </summary>
		/// <param name="source">Source from where to get the money</param>
		/// <param name="money">array of money to get</param>
		/// <returns>true if the money was successfully received</returns>
		public virtual bool ReceiveMoney(GameLiving source, long money)
		{
			return false;
		}

		#endregion

        #region Spell Cast

        /// <summary>
        /// Returns true if the object has the spell effect,
        /// else false.
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public virtual bool HasEffect(Spell spell)
        {
            return false;
        }

        /// <summary>
        /// Returns true if the object has a spell effect of a
        /// given type, else false.
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public virtual bool HasEffect(Type effectType)
        {
            return false;
        }

        #endregion

		/// <summary>
		/// Returns the string representation of the GameObject
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			Region reg = CurrentRegion;
			return new StringBuilder(128)
				.Append(GetType().FullName)
				.Append(" name=").Append(Name)
				.Append(" DB_ID=").Append(InternalID)
				.Append(" oid=").Append(ObjectID.ToString())
				.Append(" state=").Append(ObjectState.ToString())
				.Append(" reg=").Append(reg == null ? "null" : reg.ID.ToString())
				.Append(" loc=").Append(X.ToString()).Append(',').Append(Y.ToString()).Append(',').Append(Z.ToString())
				.ToString();
		}

        /// <summary>
        /// All objects are neutral.
        /// </summary>
        public virtual eGender Gender
        {
            get { return eGender.Neutral; }
            set { }
        }

		static GameObject()
		{
			FillDataQuestCache();
		}

		/// <summary>
		/// Constructs a new empty GameObject
		/// </summary>
		public GameObject()
		{
			m_saveInDB = false;
			m_name = string.Empty;
			m_ObjectState = eObjectState.Inactive;
			m_boat_ownerid = string.Empty;
			SubZoneObject = new(this);
		}

		public static bool PlayerHasItem(GamePlayer player, string str)
		{
			DbInventoryItem item = player.Inventory.GetFirstItemByID(str, eInventorySlot.Min_Inv, eInventorySlot.Max_Inv);
			if (item != null)
				return true;
			return false;
		}
		private static string m_boat_ownerid;
		public static string ObjectHasOwner()
		{
			if (m_boat_ownerid == string.Empty)
				return string.Empty;
			else
				return m_boat_ownerid;
		}

		public virtual void OnUpdateOrCreateForPlayer() { }
	}
}
