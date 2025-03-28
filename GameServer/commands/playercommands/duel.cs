using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		 "&duel",
		 ePrivLevel.Player,
		 "Duel another player",
		 "/duel")]

/*
[11:44:14] You're not currently considering a duel.
[11:44:21] Aediorfha challenges you to a duel!   /duel accept to begin, /duel decline to decline.
[11:44:23] You accept your duel!  Begin fighting!

[16:28:26] You challenge Spacy to a duel!
[16:28:47] Spacy accepts the duel.  Begin fighting!

[16:23:54] Spacy shoots you with his axe!
[16:23:54] You block Spacy's attack!
[16:23:56] Your duel ends!
[16:23:56] The greater criosphinx hits your torso for 848 (-768) damage!
[16:23:57] You prepare to sprint!
[16:23:59] @@You say, "LOLL"

[11:44:41] Your duel ends!
[11:44:41] Hyri was just defeated in a duel by Aediorfha!
[11:44:41] Your health returns to normal.
[11:44:41] Your Constitution has decreased.
[11:44:41] Your hits have decreased.
[11:44:41] You have died.  Type /release to return to your last bind point.
[11:44:41] Aediorfha wins the duel!
[11:44:41] You died fighting for your realm and lose no experience!
[11:44:50] You release your corpse unto death.
[11:44:50] Your surroundings suddenly change!

[20:14:39] You critical hit for an additional 1 damage!
[20:14:39] Your duel ends!
[20:14:39] You just killed Feraloth!
[20:14:39] Feraloth just died.  His corpse lies on the ground.
[20:14:39] Adoacred wins the duel!
[20:14:43] You sit down.  Type '/stand' or move to stand up.

[20:16:12] @@[Chat] Feraloth: "duel only possible at 100% hp"

[20:17:26] Your duel ends!
[20:17:26] Adoacred was just defeated in a duel by Feraloth!
[20:17:26] Your group invitation is cancelled.
[20:17:26] You have died.  Type /release to return to your last bind point.
[20:17:26] Feraloth wins the duel!

[20:20:56] You can't alter your release on a duel death!



[02:10:04] /duel options are challenge/decline/cancel/surrender/accept
[02:10:13] You need to target someone to duel!
[02:10:19] You need to target someone to duel!
[02:10:28] You're not currently considering a duel.
[02:10:30] You're not currently considering a duel.
[02:10:35] You haven't challenged anyone to a duel.
[02:10:36] You haven't challenged anyone to a duel.
[02:10:41] You aren't in a duel!
[02:10:42] You aren't in a duel!
[02:10:45] You're not currently considering a duel.
[02:10:46] You're not currently considering a duel.
[02:10:48] You target [Gerissa]
[02:10:48] You examine Gerissa.  She is a member of the Fighter class in your realm.
[02:10:53] You challenge Gerissa to a duel!
[02:10:56] You're not currently considering a duel.
[02:11:02] You cancel your duel invitation.
[02:11:06] You challenge Gerissa to a duel!
[02:11:24] You cancel your duel invitation.
[02:11:26] You challenge Gerissa to a duel!
[02:11:41] You cancel your duel invitation.
[02:11:42] You challenge Gerissa to a duel!
[02:11:56] Gerissa accepts the duel.  Begin fighting!

[02:11:43] Gwoawin cancels the duel.

[02:17:34] You will quit in 5 seconds.
[02:17:34] Your duel ends!
[02:17:34] Gwoawin has left the group.
[02:17:34] You leave your group.
[02:17:34] Gwoawin has left the chat group.
[02:17:34] You leave your chat group.
*** Chat Log Closed: Wed Mar 31 02:17:53 2004

[04:20:12] You are already in a duel.  /duel surrender to end it

 */
	public class DuelCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		private const string DUEL_STARTER_WEAK = "DuelStarter";
		private const string CHALLENGE_TARGET_WEAK = "DuelTarget";

		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "duel"))
				return;

			switch (client.Player.CurrentRegionID)
			{
				case 10:
				case 101:
				case 201:
					{
						DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Duel.SafeZone"), new object[] { });
						return;
					}
			}

			WeakRef weak = null;
			GamePlayer duelStarter = null;
			GamePlayer duelTarget = null;

			if (args.Length > 1)
			{
				switch (args[1].ToLower())
				{
					case "challenge":
					{
						GamePlayer target = client.Player.TargetObject as GamePlayer;

						if (target == null || target == client.Player)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Duel.NeedTarget"), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
							return;
						}

						if (!CheckDuelStart(client.Player, target))
							return;

						weak = client.Player.TempProperties.GetProperty<WeakRef>(CHALLENGE_TARGET_WEAK);
						if (weak != null && (duelTarget = weak.Target as GamePlayer) != null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Duel.YouAlreadyChallenging", duelTarget.Name), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
							return;
						}
						weak = client.Player.TempProperties.GetProperty<WeakRef>(DUEL_STARTER_WEAK);
						if (weak != null && (duelStarter = weak.Target as GamePlayer) != null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Duel.YouAlreadyConsidering", duelStarter.Name), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
							return;
						}

						if (target.TempProperties.GetProperty<WeakRef>(DUEL_STARTER_WEAK) != null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Duel.TargetAlreadyConsidering", target.Name), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
							return;
						}
						if (target.TempProperties.GetProperty<WeakRef>(CHALLENGE_TARGET_WEAK) != null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Duel.TargetAlreadyChallenging", target.Name), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
							return;
						}

						target.TempProperties.SetProperty(DUEL_STARTER_WEAK, new WeakRef(client.Player));
						client.Player.TempProperties.SetProperty(CHALLENGE_TARGET_WEAK, new WeakRef(target));
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Duel.YouChallenge", target.Name), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
						target.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Duel.ChallengesYou", client.Player.Name), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
						return;
					}

					case "accept":
					{
						weak = client.Player.TempProperties.GetProperty<WeakRef>(DUEL_STARTER_WEAK);

						if (weak == null || (duelStarter = weak.Target as GamePlayer) == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Duel.ConsideringDuel"), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
							return;
						}

						if (!CheckDuelStart(client.Player, duelStarter))
							return;

						GameDuel duel = new(duelStarter, client.Player);
						duel.Start();
						duelStarter.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Duel.TargetAccept", client.Player.Name), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
						client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Duel.YouAccept"), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
						client.Player.TempProperties.RemoveProperty(DUEL_STARTER_WEAK);
						duelStarter.TempProperties.RemoveProperty(CHALLENGE_TARGET_WEAK);
						return;
					}

					case "decline":
					{
						weak = client.Player.TempProperties.GetProperty<WeakRef>(DUEL_STARTER_WEAK);
						client.Player.TempProperties.RemoveProperty(DUEL_STARTER_WEAK);

						if (weak == null || (duelStarter = weak.Target as GamePlayer) == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Duel.NotInDuel"), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
							return;
						}


						duelStarter.TempProperties.RemoveProperty(CHALLENGE_TARGET_WEAK);
						duelStarter.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Duel.TargetDeclines", client.Player.Name), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Duel.YouDecline", duelStarter.Name), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
						return;
					}

					case "cancel":
					{
						weak = client.Player.TempProperties.GetProperty<WeakRef>(CHALLENGE_TARGET_WEAK);
						client.Player.TempProperties.RemoveProperty(CHALLENGE_TARGET_WEAK);

						if (weak == null || (duelTarget = weak.Target as GamePlayer) == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Duel.YouHaventChallenged"), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
							return;
						}

						duelTarget.TempProperties.RemoveProperty(DUEL_STARTER_WEAK);
						duelTarget.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Duel.TargetCancel", client.Player.Name), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Duel.YouCancel"), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
						return;
					}

					case "surrender":
					{
						GamePlayer target = client.Player.DuelPartner;

						if (target == null)
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Duel.NotInDuel"), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
							return;
						}

						client.Player.Duel.Stop();
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Duel.YouSurrender", target.Name), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
						target.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Duel.TargetSurrender", client.Player.Name), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
						Message.SystemToArea(client.Player, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Duel.PlayerVsPlayer", client.Player.Name, target.Name), eChatType.CT_Emote, client.Player, target);
						return;
					}
				}
			}

			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Duel.DuelOptions"), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
		}

		/// <summary>
		/// Checks if a duel can be started between 2 players at this moment
		/// </summary>
		/// <param name="actionSource">The duel starter</param>
		/// <param name="actionTarget">The duel target</param>
		/// <returns>true if players can start a duel</returns>
		private static bool CheckDuelStart(GamePlayer actionSource, GamePlayer actionTarget)
		{
			if (!GameServer.ServerRules.IsSameRealm(actionSource, actionTarget, true))
			{
				actionSource.Out.SendMessage(LanguageMgr.GetTranslation(actionSource.Client, "Scripts.Players.Duel.EnemyRealm"), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
				return false;
			}
			if (actionSource.DuelPartner != null)
			{
				actionSource.Out.SendMessage(LanguageMgr.GetTranslation(actionSource.Client, "Scripts.Players.Duel.YouInDuel"), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
				return false;
			}
			if (actionTarget.DuelPartner != null)
			{
				actionSource.Out.SendMessage(LanguageMgr.GetTranslation(actionSource.Client, "Scripts.Players.Duel.TargetInDuel", actionTarget.Name), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
				return false;
			}
			if (actionTarget.InCombat)
			{
				actionSource.Out.SendMessage(LanguageMgr.GetTranslation(actionSource.Client, "Scripts.Players.Duel.TargetInCombat", actionTarget.Name), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
				return false;
			}
			if (actionSource.InCombat)
			{
				actionSource.Out.SendMessage(LanguageMgr.GetTranslation(actionSource.Client, "Scripts.Players.Duel.YouInCombat"), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
				return false;
			}
			if (actionTarget.Group != null)
			{
				actionSource.Out.SendMessage(LanguageMgr.GetTranslation(actionSource.Client, "Scripts.Players.Duel.TargetInGroup", actionTarget.Name), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
				return false;
			}
			if (actionSource.Group != null)
			{
				actionSource.Out.SendMessage(LanguageMgr.GetTranslation(actionSource.Client, "Scripts.Players.Duel.YouInGroup"), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
				return false;
			}
			if (actionSource.Health < actionSource.MaxHealth)
			{
				actionSource.Out.SendMessage(LanguageMgr.GetTranslation(actionSource.Client, "Scripts.Players.Duel.YouHealth"), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
				return false;
			}
			if (actionTarget.Health < actionTarget.MaxHealth)
			{
				actionSource.Out.SendMessage(LanguageMgr.GetTranslation(actionSource.Client, "Scripts.Players.Duel.TargetHealth"), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
				return false;
			}

			return true;
		}
	}
}
