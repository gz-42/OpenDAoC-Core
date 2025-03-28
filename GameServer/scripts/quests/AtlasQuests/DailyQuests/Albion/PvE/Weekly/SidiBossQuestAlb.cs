using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.API;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerTitles;
using DOL.GS.Quests;

namespace DOL.GS.WeeklyQuest.Albion
{
    public class SidiBossQuestAlb : Quests.WeeklyQuest
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private const string questTitle = "[Weekly] Harder Adversaries";
        private const int minimumLevel = 45;
        private const int maximumLevel = 50;

        // Kill Goal
        private int _deadSidiBossMob = 0;
        private const int MAX_KILLGOAL = 3;

        private static GameNPC James = null; // Start NPC


        // Constructors
        public SidiBossQuestAlb() : base()
        {
        }

        public SidiBossQuestAlb(GamePlayer questingPlayer) : base(questingPlayer, 1)
        {
        }

        public SidiBossQuestAlb(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
        {
        }

        public SidiBossQuestAlb(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
        {
        }

        public override int Level
        {
            get
            {
                // Quest Level
                return minimumLevel;
            }
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            if (!ServerProperties.Properties.LOAD_QUESTS)
                return;


            #region defineNPCs

            GameNPC[] npcs = WorldMgr.GetNPCsByName("James", eRealm.Albion);

            if (npcs.Length > 0)
                foreach (GameNPC npc in npcs)
                    if (npc.CurrentRegionID == 51 && npc.X == 534044 && npc.Y == 549664)
                    {
                        James = npc;
                        break;
                    }

            if (James == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Could not find James , creating it ...");
                James = new GameNPC();
                James.Model = 254;
                James.Name = "James";
                James.GuildName = "Advisor To The King";
                James.Realm = eRealm.Albion;
                James.CurrentRegionID = 51;
                James.Size = 50;
                James.Level = 59;
                //Caer Gothwaite Location
                James.X = 534044;
                James.Y = 549664;
                James.Z = 4940;
                James.Heading = 3143;
                GameNpcInventoryTemplate templateAlb = new GameNpcInventoryTemplate();
                templateAlb.AddNPCEquipment(eInventorySlot.TorsoArmor, 1005);
                templateAlb.AddNPCEquipment(eInventorySlot.HandsArmor, 142);
                templateAlb.AddNPCEquipment(eInventorySlot.FeetArmor, 143);
                James.Inventory = templateAlb.CloseTemplate();
                James.AddToWorld();
                if (SAVE_INTO_DATABASE)
                {
                    James.SaveIntoDatabase();
                }
            }

            #endregion

            #region defineItems

            #endregion

            #region defineObject

            #endregion

            GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
            GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

            GameEventMgr.AddHandler(James, GameObjectEvent.Interact, new DOLEventHandler(TalkToJames));
            GameEventMgr.AddHandler(James, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToJames));

            James.AddQuestToGive(typeof(SidiBossQuestAlb));

            if (log.IsInfoEnabled)
                log.Info("Quest \"" + questTitle + "\" Alb initialized");
        }

        [ScriptUnloadedEvent]
        public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
            //if not loaded, don't worry
            if (James == null)
                return;
            // remove handlers
            GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
            GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

            GameEventMgr.RemoveHandler(James, GameObjectEvent.Interact, new DOLEventHandler(TalkToJames));
            GameEventMgr.RemoveHandler(James, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToJames));

            James.RemoveQuestToGive(typeof(SidiBossQuestAlb));
        }

        protected static void TalkToJames(DOLEvent e, object sender, EventArgs args)
        {
            //We get the player from the event arguments and check if he qualifies		
            GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
            if (player == null)
                return;

            if (James.CanGiveQuest(typeof(SidiBossQuestAlb), player) <= 0)
                return;

            //We also check if the player is already doing the quest
            SidiBossQuestAlb quest = player.IsDoingQuest(typeof(SidiBossQuestAlb)) as SidiBossQuestAlb;

            if (e == GameObjectEvent.Interact)
            {
                if (quest != null)
                {
                    switch (quest.Step)
                    {
                        case 1:
                            James.SayTo(player,
                                "Please, enter Caer Sidi and slay strong opponents. If you succeed come back for your reward.");
                            break;
                        case 2:
                            James.SayTo(player, "Hello " + player.Name + ", did you [succeed]?");
                            break;
                    }
                }
                else
                {
                    James.SayTo(player, "Hello " + player.Name + ", I am James. " +
                                         "An infiltrator has reported the forces in Caer Sidi are planning an attack. \n" +
                                         "We want to pre-empt them and [end their plotting] before they have the chance. Care to help?");
                }
            }
            // The player whispered to the NPC
            else if (e == GameLivingEvent.WhisperReceive)
            {
                WhisperReceiveEventArgs wArgs = (WhisperReceiveEventArgs) args;
                if (quest == null)
                {
                    switch (wArgs.Text)
                    {
                        case "end their plotting":
                            player.Out.SendQuestSubscribeCommand(James,
                                QuestMgr.GetIDForQuestType(typeof(SidiBossQuestAlb)),
                                "Will you help James " + questTitle + "");
                            break;
                    }
                }
                else
                {
                    switch (wArgs.Text)
                    {
                        case "succeed":
                            if (quest.Step == 2)
                            {
                                player.Out.SendMessage("Thank you for your contribution!", eChatType.CT_Chat,
                                    eChatLoc.CL_PopupWindow);
                                quest.FinishQuest();
                            }

                            break;
                        case "abort":
                            player.Out.SendCustomDialog(
                                "Do you really want to abort this quest, \nall items gained during quest will be lost?",
                                new CustomDialogResponse(CheckPlayerAbortQuest));
                            break;
                    }
                }
            }
        }

        public override bool CheckQuestQualification(GamePlayer player)
        {
            // if the player is already doing the quest his level is no longer of relevance
            if (player.IsDoingQuest(typeof(SidiBossQuestAlb)) != null)
                return true;

            // This checks below are only performed is player isn't doing quest already

            //if (player.HasFinishedQuest(typeof(Academy_47)) == 0) return false;

            //if (!CheckPartAccessible(player,typeof(CityOfCamelot)))
            //	return false;

            if (player.Level < minimumLevel || player.Level > maximumLevel)
                return false;

            return true;
        }

        private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
        {
            SidiBossQuestAlb quest = player.IsDoingQuest(typeof(SidiBossQuestAlb)) as SidiBossQuestAlb;

            if (quest == null)
                return;

            if (response == 0x00)
            {
                SendSystemMessage(player, "Good, now go out there and finish your work!");
            }
            else
            {
                SendSystemMessage(player, "Aborting Quest " + questTitle + ". You can start over again if you want.");
                quest.AbortQuest();
            }
        }

        protected static void SubscribeQuest(DOLEvent e, object sender, EventArgs args)
        {
            QuestEventArgs qargs = args as QuestEventArgs;
            if (qargs == null)
                return;

            if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(SidiBossQuestAlb)))
                return;

            if (e == GamePlayerEvent.AcceptQuest)
                CheckPlayerAcceptQuest(qargs.Player, 0x01);
            else if (e == GamePlayerEvent.DeclineQuest)
                CheckPlayerAcceptQuest(qargs.Player, 0x00);
        }

        private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
        {
            if (James.CanGiveQuest(typeof(SidiBossQuestAlb), player) <= 0)
                return;

            if (player.IsDoingQuest(typeof(SidiBossQuestAlb)) != null)
                return;

            if (response == 0x00)
            {
                player.Out.SendMessage("Thank you for helping Albion.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
            }
            else
            {
                //Check if we can add the quest!
                if (!James.GiveQuest(typeof(SidiBossQuestAlb), player, 1))
                    return;

                James.SayTo(player, "Thank you " + player.Name + ", be an enrichment for our realm!");
            }
        }

        //Set quest name
        public override string Name
        {
            get { return questTitle; }
        }

        // Define Steps
        public override string Description
        {
            get
            {
                switch (Step)
                {
                    case 1:
                        return "Find a way to Caer Sidi and kill strong opponents. \nKilled: Bosses in Caer Sidi (" +
                               _deadSidiBossMob + " | "+ MAX_KILLGOAL +")";
                    case 2:
                        return "Return to James in Caer Gothwaite for your Reward.";
                }

                return base.Description;
            }
        }

        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = sender as GamePlayer;

            if (player == null || player.IsDoingQuest(typeof(SidiBossQuestAlb)) == null)
                return;

            if (sender != m_questPlayer)
                return;

            if (Step != 1 || e != GameLivingEvent.EnemyKilled) return;
            EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
                
            // check if a GameEpicBoss died + if its in Caer Sidi
            if (gArgs.Target.Realm != 0 || gArgs.Target is not GameEpicBoss ||
                gArgs.Target.CurrentRegionID != 60) return;
            _deadSidiBossMob++;
            player.Out.SendMessage(
                "[Weekly] Bosses killed in Caer Sidi: (" + _deadSidiBossMob + " | " + MAX_KILLGOAL + ")",
                eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
            player.Out.SendQuestUpdate(this);

            if (_deadSidiBossMob >= MAX_KILLGOAL)
            {
                Step = 2;
            }
        }

        public override string QuestPropertyKey
        {
            get => "SidiBossQuestAlb";
            set { ; }
        }

        public override void LoadQuestParameters()
        {
            _deadSidiBossMob = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
        }

        public override void SaveQuestParameters()
        {
            SetCustomProperty(QuestPropertyKey, _deadSidiBossMob.ToString());
        }

        public override void FinishQuest()
        {
            m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel));
            m_questPlayer.AddMoney(Money.GetMoney(0, 0, m_questPlayer.Level * 5, 0, Util.Random(50)), "You receive {0} as a reward.");
            AtlasROGManager.GenerateReward(m_questPlayer, 1500);
            _deadSidiBossMob = 0;
            base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
        }
    }
}