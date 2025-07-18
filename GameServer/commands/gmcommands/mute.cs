using System;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[Cmd(
		"&mute",
		ePrivLevel.GM,
		"Command to mute annoying players.  Player mutes are temporary, allchars are set on an account and must be removed.",
		"/mute <playername or #ClientID> - example /mute #24  to mute player on client id 24",
		"/mute <playername or #ClientID> allchars - this applies an account mute to this player",
		"/mute <playername or #ClientID> remove - remove all mutes from this players account")]
	public class MuteCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 2)
			{
				DisplaySyntax(client);
				return;
			}

			GameClient playerClient = null;

			if (args[1].StartsWith("#"))
			{
				try
				{
					int sessionID = Convert.ToInt32(args[1][1..]);
					playerClient = ClientService.GetClientBySessionId(sessionID);
				}
				catch
				{
					DisplayMessage(client, "Invalid client ID");
				}
			}
			else
			{
				playerClient = ClientService.GetPlayerByExactName(args[1])?.Client;
			}

			if (playerClient == null)
			{
				DisplayMessage(client, "No player found for '" + args[1] + "'");
				return;
			}

			if (client.Account.PrivLevel < playerClient.Account.PrivLevel)
			{
				DisplayMessage(client, "Your privlevel is not high enough to mute this player.");
				return;
			}

			bool mutedAccount = false;

			if (args.Length > 2 && (args[2].ToLower() == "account" || args[2].ToLower() == "allchars"))
			{
				if (playerClient != null)
				{
					playerClient.Account.IsMuted = true;
					playerClient.Player.IsMuted = true;
					GameServer.Database.SaveObject(playerClient.Account);
					mutedAccount = true;
				}
			}
			else if (args.Length > 2 && args[2].ToLower() == "remove")
			{
				if (playerClient != null)
				{
					playerClient.Account.IsMuted = false;
					playerClient.Player.IsMuted = false;
					GameServer.Database.SaveObject(playerClient.Account);
					mutedAccount = true;
				}
			}
			else
			{
				if (playerClient != null)
				{
					if (playerClient.Account.IsMuted)
					{
						DisplayMessage(client, "This player has an allchars mute which must be removed first.");
						return;
					}

					playerClient.Player.IsMuted = !playerClient.Player.IsMuted;
				}
			}

			if (playerClient.Player.IsMuted)
			{
				playerClient.Player.Out.SendMessage("You have been muted from public channels by staff member " + client.Player.Name + "!", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
				client.Player.Out.SendMessage("You have muted player " + playerClient.Player.Name + " from public channels!", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
				if (mutedAccount)
				{
					playerClient.Player.Out.SendMessage("This mute has been placed on all characters for this account.", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
					client.Player.Out.SendMessage("This action was done to the players account.", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
				}

				log.Warn(client.Player.Name + " muted " + playerClient.Player.Name);
			}
			else
			{
				playerClient.Player.Out.SendMessage("You have been unmuted from public channels by staff member " + client.Player.Name + "!", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
				client.Player.Out.SendMessage("You have unmuted player " + playerClient.Player.Name + " from public channels!", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
				if (mutedAccount)
				{
					client.Player.Out.SendMessage("This action was done to the players account.", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
				}

				log.Warn(client.Player.Name + " un-muted " + playerClient.Player.Name);
			}
			return;
		}
	}
}
