﻿using System;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;

namespace DOL.GS
{
    public class HighLordSaeor : GameEpicBoss
    {
        private static new readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameEventMgr.AddHandler(GameLivingEvent.Dying, new DOLEventHandler(PlayerKilledBySaeor));

            if (log.IsInfoEnabled)
                log.Info("High Lord Saeor initialized..");
        }
        [ScriptUnloadedEvent]
        public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
            GameEventMgr.RemoveHandler(GameLivingEvent.Dying, new DOLEventHandler(PlayerKilledBySaeor));
        }
        public HighLordSaeor()
            : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 40; // dmg reduction for melee dmg
                case eDamageType.Crush: return 40; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 40; // dmg reduction for melee dmg
                default: return 70; // dmg reduction for rest resists
            }
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
            get { return 100000; }
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162133);
            LoadTemplate(npcTemplate);

            // demon
            BodyType = 2;
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
            Faction = FactionMgr.GetFactionByID(191);

            SaeorBrain sBrain = new SaeorBrain();
            SetOwnBrain(sBrain);
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }

        public override int MeleeAttackRange => 450;
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        private static void PlayerKilledBySaeor(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = sender as GamePlayer;

            if (player == null)
                return;

            DyingEventArgs eArgs = args as DyingEventArgs;

            if (eArgs?.Killer?.Name != "High Lord Saeor")
                return;

            foreach (GameNPC mob in player.GetNPCsInRadius(1000))
            {
                if (mob is not HighLordSaeor) continue;
                mob.Health += player.MaxHealth;
                mob.UpdateHealthManaEndu();
            }
        }
    }
}

namespace DOL.AI.Brain
{
    public class SaeorBrain : StandardMobBrain
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public SaeorBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 850;
        }
        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
            }
            base.Think();
        }
    }
}