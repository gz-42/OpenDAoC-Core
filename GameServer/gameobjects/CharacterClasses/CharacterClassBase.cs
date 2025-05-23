using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.Realm;
using DOL.Language;

namespace DOL.GS
{
	/// <summary>
	/// The Base class for all Character Classes in DOL
	/// </summary>
	public abstract class CharacterClassBase : ICharacterClass
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// id of class in Client
		/// </summary>
		protected int m_id;

		/// <summary>
		/// Name of class
		/// </summary>
		protected string m_name;

		/// <summary>
		/// Female name of class
		/// </summary>
		protected string m_femaleName;

		/// <summary>
		/// Base of this class
		/// </summary>
		protected string m_basename;

		/// <summary>
		/// Profession of character, e.g. Defenders of Albion
		/// </summary>
		protected string m_profession;

		/// <summary>
		/// multiplier for specialization points per level in 10th
		/// </summary>
		protected int m_specializationMultiplier = 10;

		/// <summary>
		/// BaseHP for hp calculation
		/// </summary>
		protected int m_baseHP = 700;

		/// <summary>
		/// Stat gained every level.
		///	see eStat consts
		/// </summary>
		protected eStat m_primaryStat = eStat.UNDEFINED;

		/// <summary>
		/// Stat gained every second level.
		/// see eStat consts
		/// </summary>
		protected eStat m_secondaryStat = eStat.UNDEFINED;

		/// <summary>
		/// Stat gained every third level.
		/// see eStat consts
		/// </summary>
		protected eStat m_tertiaryStat = eStat.UNDEFINED;

		/// <summary>
		/// Stat that affects the power/mana pool.
		/// Do not set if they do not have a power pool/spells
		/// </summary>
		protected eStat m_manaStat = eStat.UNDEFINED;

		/// <summary>
		/// Weapon Skill Base value to influence weapon skill calc
		/// </summary>
		protected int m_wsbase = 400;

		/// <summary>
		/// Weapon Skill Base value to influence ranged weapon skill calc
		/// </summary>
		protected int m_wsbaseRanged = 440;

		/// <summary>
		/// The GamePlayer for this character
		/// </summary>
		public GamePlayer Player { get; private set; }

		private static readonly string[] AutotrainableSkills = new string[0];

		public CharacterClassBase()
		{
			m_id = 0;
			m_name = "Unknown Class";
			m_basename = "Unknown Base Class";
			m_profession = string.Empty;

			// initialize members from attributes
			Attribute[] attrs = Attribute.GetCustomAttributes(this.GetType(), typeof(CharacterClassAttribute));
			foreach (Attribute attr in attrs)
			{
				if (attr is CharacterClassAttribute)
				{
					m_id = ((CharacterClassAttribute)attr).ID;
					m_name = ((CharacterClassAttribute)attr).Name;
					m_basename = ((CharacterClassAttribute)attr).BaseName;
					if (!string.IsNullOrEmpty(((CharacterClassAttribute)attr).FemaleName))
						m_femaleName = ((CharacterClassAttribute)attr).FemaleName;
					break;
				}
			}
		}

		public virtual void Init(GamePlayer player)
		{
			// TODO : Should Throw Exception Here.
			if (Player != null && log.IsWarnEnabled)
				log.WarnFormat("Character Class initializing Player when it was already initialized ! Old Player : {0} New Player : {1}", Player, player);
			
			Player = player;
		}

		public abstract List<PlayerRace> EligibleRaces { get; }

		public string FemaleName
		{
			get { return m_femaleName; }
		}

		public int BaseHP
		{
			get { return m_baseHP; }
		}

		public int ID
		{
			get { return m_id; }
		}

		public string Name
		{
			get { return (Player != null && Player.Gender == eGender.Female && !string.IsNullOrEmpty(m_femaleName)) ? m_femaleName : m_name; }
		}

		public string BaseName
		{
			get { return m_basename; }
		}

		/// <summary>
		/// Return Translated Profession
		/// </summary>
		public string Profession
		{
			get
			{
				return LanguageMgr.TryTranslateOrDefault(Player, m_profession, m_profession);
			}
		}

		public int SpecPointsMultiplier
		{
			get { return m_specializationMultiplier; }
		}

		/// <summary>
		/// This is specifically used for adjusting spec points as needed for new training window
		/// For standard DOL classes this will simply return the standard spec multiplier
		/// </summary>
		public int AdjustedSpecPointsMultiplier
		{
			get { return m_specializationMultiplier; }
		}

		public eStat PrimaryStat
		{
			get { return m_primaryStat; }
		}

		public eStat SecondaryStat
		{
			get { return m_secondaryStat; }
		}

		public eStat TertiaryStat
		{
			get { return m_tertiaryStat; }
		}

		public eStat ManaStat
		{
			get { return m_manaStat; }
		}

		public int WeaponSkillBase
		{
			get { return m_wsbase; }
		}

		public int WeaponSkillRangedBase
		{
			get { return m_wsbaseRanged; }
		}

		/// <summary>
		/// Maximum number of pulsing spells that can be active simultaneously
		/// </summary>
		public virtual ushort MaxPulsingSpells
		{
			get { return 1; }
		}

		public virtual string GetTitle(GamePlayer player, int level)
		{
			
			// Clamp level in 5 by 5 steps - 50 is the max available translation for now
			int clamplevel = Math.Min(50, (level / 5) * 5);
			
			string none = LanguageMgr.TryTranslateOrDefault(player, "!None!", "PlayerClass.GetTitle.none");
			
			if (clamplevel > 0)
				return LanguageMgr.TryTranslateOrDefault(player, string.Format("!{0}!", m_name), string.Format("PlayerClass.{0}.GetTitle.{1}", m_name, clamplevel));

			return none;
		}

		public virtual eClassType ClassType
		{
			get { return eClassType.ListCaster; }
		}

		public virtual bool IsFocusCaster => false;
		public virtual bool IsAssassin => false;

		/// <summary>
		/// Return the base list of Realm abilities that the class
		/// can train in.  Added by Echostorm for RAs
		/// </summary>
		/// <returns></returns>
		public virtual IList<string> GetAutotrainableSkills()
		{
			return AutotrainableSkills;
		}

		/// <summary>
		/// What Champion trainer does this class use?
		/// </summary>
		/// <returns></returns>
		public virtual GameTrainer.eChampionTrainerType ChampionTrainerType()
		{
			return GameTrainer.eChampionTrainerType.None;
		}

		/// <summary>
		/// Add things that are required for current level
		/// Skills and other things are handled through player specs... (on Refresh Specs)
		/// </summary>
		/// <param name="player">player to modify</param>
		/// <param name="previousLevel">the previous level of the player</param>
		public virtual void OnLevelUp(GamePlayer player, int previousLevel)
		{
		}

		/// <summary>
		/// Add various skills as the player levels his realm rank up
		/// </summary>
		/// <param name="player">player to modify</param>
		public virtual void OnRealmLevelUp(GamePlayer player)
		{
			//we dont want to add things when players arent using their advanced class
			if (player.CharacterClass.BaseName == player.CharacterClass.Name)
				return;
		}

		/// <summary>
		/// Add all spell-lines and other things that are new when this skill is trained
		/// </summary>
		/// <param name="player">player to modify</param>
		/// <param name="skill">The skill that is trained</param>
		public virtual void OnSkillTrained(GamePlayer player, Specialization skill)
		{
		}

		/// <summary>
		/// Checks whether player has ability to use lefthanded weapons
		/// </summary>
		public virtual bool CanUseLefthandedWeapon
		{
			get { return false; }
		}

		public virtual bool HasAdvancedFromBaseClass()
		{
			return true;
		}

		public virtual void SetControlledBrain(IControlledBrain controlledBrain)
		{
			if (controlledBrain == Player.ControlledBrain) return;
			if (controlledBrain == null)
			{
				Player.Out.SendPetWindow(null, ePetWindowAction.Close, 0, 0);
				// Message: You lose control of {0}!
				Player.Out.SendMessage(LanguageMgr.GetTranslation(Player.Client.Account.Language, "GamePlayer.GamePet.SpellEnd.YouLoseControl", Player.ControlledBrain.Body.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				// Message: You release control of your controlled target.
				Player.Out.SendMessage(LanguageMgr.GetTranslation(Player.Client.Account.Language, "GamePlayer.GamePet.SpellEnd.YouReleaseControl"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}
			else
			{
				if (controlledBrain.Owner != Player)
					throw new ArgumentException("ControlledNpc with wrong owner is set (player=" + Player.Name + ", owner=" + controlledBrain.Owner.Name + ")", "controlledNpc");
				if (Player.ControlledBrain == null)
					Player.InitControlledBrainArray(1);
				Player.Out.SendPetWindow(controlledBrain.Body, ePetWindowAction.Open, controlledBrain.AggressionState, controlledBrain.WalkState);
				if (controlledBrain.Body != null)
				{
					Player.Out.SendNPCCreate(controlledBrain.Body); // after open pet window again send creation NPC packet
					if (controlledBrain.Body.Inventory != null)
						Player.Out.SendLivingEquipmentUpdate(controlledBrain.Body);
				}
			}

			Player.ControlledBrain = controlledBrain;
		}

		/// <summary>
		/// Releases controlled object
		/// </summary>
		public virtual void CommandNpcRelease()
		{
			ControlledMobBrain controlledBrain;

			if (Player.TargetObject is not GameNPC targetNpc || !Player.IsControlledNPC(targetNpc))
				controlledBrain = Player.ControlledBrain as ControlledMobBrain;
			else
				controlledBrain = targetNpc.Brain as ControlledMobBrain;

			controlledBrain?.OnRelease();
			return;
		}

		/// <summary>
		/// Invoked when pet is released.
		/// </summary>
		public virtual void OnPetReleased() { }

		/// <summary>
		/// Can this character start an attack?
		/// </summary>
		/// <param name="attackTarget"></param>
		/// <returns></returns>
		public virtual bool StartAttack(GameObject attackTarget)
		{
			return true;
		}

		/// <summary>
		/// Return the health percent of this character
		/// </summary>
		public virtual byte HealthPercentGroupWindow
		{
			get
			{
				return Player.HealthPercent;
			}
		}

		public virtual bool CreateShadeEffect(out ECSGameAbilityEffect effect)
		{
			effect = EffectListService.GetAbilityEffectOnTarget(Player, eEffect.Shade);

			if (effect != null)
				return false;

			effect = new ShadeECSGameEffect(new ECSGameEffectInitParams(Player, 0, 1));
			return effect.IsActive;
		}

		public virtual bool CancelShadeEffect(out ECSGameAbilityEffect effect)
		{
			effect = EffectListService.GetAbilityEffectOnTarget(Player, eEffect.Shade);
			return effect != null && effect.Stop();
		}

		public virtual bool Shade(bool makeShade, out ECSGameAbilityEffect effect)
		{
			if (Player.HasShadeModel == makeShade)
			{
				if (makeShade && (Player.ObjectState == GameObject.eObjectState.Active))
					Player.Out.SendMessage(LanguageMgr.GetTranslation(Player.Client.Account.Language, "GamePlayer.Shade.AlreadyShade"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

				effect = null;
				return false;
			}

			return makeShade ? CreateShadeEffect(out effect) : CancelShadeEffect(out effect);
		}

		/// <summary>
		/// Called when player is removed from world.
		/// </summary>
		/// <returns></returns>
		public virtual bool RemoveFromWorld()
		{
			return true;
		}

		/// <summary>
		/// What to do when this character dies
		/// </summary>
		/// <param name="killer"></param>
		public virtual void Die(GameObject killer)
		{
		}

		public virtual void Notify(DOLEvent e, object sender, EventArgs args)
		{
		}

		public virtual bool CanChangeCastingSpeed(SpellLine line, Spell spell)
		{
			return true;
		}
	}

	/// <summary>
	/// Usable default Character Class, if not other can be found or used
	/// just for getting things valid in problematic situations
	/// </summary>
	public class DefaultCharacterClass : CharacterClassBase
	{
		public DefaultCharacterClass()
			: base()
		{
			m_id = 0;
			m_name = "Unknown";
			m_basename = "Unknown Class";
			m_profession = "None";
		}

        public override List<PlayerRace> EligibleRaces => PlayerRace.AllRaces;
    }
}
