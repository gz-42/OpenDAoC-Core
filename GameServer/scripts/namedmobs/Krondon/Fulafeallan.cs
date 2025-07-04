﻿using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class Fulafeallan : GameEpicBoss
	{
		public Fulafeallan() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Fulafeallan Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			if (FulafeallanAdd.PartsCount2 > 0)
			{
				switch (damageType)
				{
					case eDamageType.Slash: return 95;// dmg reduction for melee dmg
					case eDamageType.Crush: return 95;// dmg reduction for melee dmg
					case eDamageType.Thrust: return 95;// dmg reduction for melee dmg
					default: return 95;// dmg reduction for rest resists
				}
			}
			else
			{
				switch (damageType)
				{
					case eDamageType.Slash: return 20;// dmg reduction for melee dmg
					case eDamageType.Crush: return 20;// dmg reduction for melee dmg
					case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
					default: return 30;// dmg reduction for rest resists
				}
			}
		}

		public override int MeleeAttackRange => 350;
		public override bool HasAbility(string keyName)
		{
			if (IsAlive && keyName == GS.Abilities.CCImmunity)
				return true;

			return base.HasAbility(keyName);
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.20;
		}
		public override int MaxHealth
		{
			get { return 30000; }
		}
		public override bool AddToWorld()
		{
			Model = 933;
			Level = 77;
			Name = "Fulafeallan";
			Size = 150;

			Strength = 280;
			Dexterity = 150;
			Constitution = 100;
			Quickness = 80;
			Piety = 200;
			Intelligence = 200;
			Charisma = 200;
			Empathy = 400;

			MaxSpeedBase = 250;
			TetherRange = 3800;
			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(8);

			FulafeallanBrain sbrain = new FulafeallanBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
	}
}

namespace DOL.AI.Brain
{
	public class FulafeallanBrain : StandardMobBrain
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public FulafeallanBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		private bool spawnadds = false;
		private bool RemoveAdds = false;
		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				spawnadds = false;
				if (!RemoveAdds)
				{
					foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
					{
						if (npc != null)
						{
							if (npc.IsAlive && npc.Brain is FulafeallanAddBrain)
							{
								npc.RemoveFromWorld();
								FulafeallanAdd.PartsCount2 = 0;
							}
						}
					}
					RemoveAdds = true;
				}
			}
			if (HasAggro && Body.TargetObject != null)
			{
				RemoveAdds = false;
				if (spawnadds == false)
				{
					SpawnAdds();
					spawnadds = true;
				}
			}
			base.Think();
		}
		public void SpawnAdds()
		{
			for (int i = 0; i < Util.Random(4, 5); i++)
			{
				FulafeallanAdd npc = new FulafeallanAdd();
				npc.X = Body.X + Util.Random(-100, 100);
				npc.Y = Body.Y + Util.Random(-100, 100);
				npc.Z = Body.Z;
				npc.Heading = Body.Heading;
				npc.CurrentRegion = Body.CurrentRegion;
				npc.RespawnInterval = -1;
				npc.AddToWorld();
			}
		}
	}
}
////////////////////////////////////////////////////////////Fuladl adds////////////////////////////////////////////////
namespace DOL.GS
{
	public class FulafeallanAdd : GameNPC
	{
		public FulafeallanAdd() : base() { }

		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 30;// dmg reduction for melee dmg
				case eDamageType.Crush: return 30;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 30;// dmg reduction for melee dmg
				default: return 30;// dmg reduction for rest resists
			}
		}

		public override int MeleeAttackRange => 350;
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 300;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.15;
		}
		public override int MaxHealth
		{
			get { return 3000; }
		}
		public static int PartsCount2 = 0;
		public override void Die(GameObject killer)
		{
			--PartsCount2;
			base.Die(killer);
		}
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 150; }
		public override bool AddToWorld()
		{
			Model = 933;
			Level = (byte)(Util.Random(65, 68));
			Name = "Part of Fulafeallan";
			Size = (byte)(Util.Random(50, 70));
			++PartsCount2;
			MaxSpeedBase = 250;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(8);

			FulafeallanAddBrain sbrain = new FulafeallanAddBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			base.AddToWorld();
			return true;
		}
	}
}

namespace DOL.AI.Brain
{
	public class FulafeallanAddBrain : StandardMobBrain
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public FulafeallanAddBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 1000;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
