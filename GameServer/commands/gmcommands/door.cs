using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.PacketHandler.Client.v168;

namespace DOL.GS.Commands
{
	[Cmd(
		"&door",
		ePrivLevel.GM,
		"GMCommands.door.Description",
		"'/door show' toggle enable/disable add dialog when targeting doors",
		"GMCommands.door.Add",
		"GMCommands.door.Update",
		"GMCommands.door.Delete",
		"GMCommands.door.Name",
		"GMCommands.door.Level",
		"GMCommands.door.Realm",
		"GMCommands.door.Guild",
		"'/door sound <soundid>'",
		"GMCommands.door.Info",
		"GMCommands.door.Heal",
		"GMCommands.door.Locked",
		"GMCommands.door.Unlocked")]
	public class NewDoorCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		private int DoorID;
		private int doorType;
		private string Realmname;
		private string statut;

		#region ICommandHandler Members

		public void OnCommand(GameClient client, string[] args)
		{
			GameDoor targetDoor = null;

			if (args.Length > 1 && args[1] == "show" && client.Player != null)
			{
				if (client.Player.TempProperties.GetProperty<bool>(DoorMgr.WANT_TO_ADD_DOORS))
				{
					client.Player.TempProperties.RemoveProperty(DoorMgr.WANT_TO_ADD_DOORS);
					client.Out.SendMessage("You will no longer be shown the add door dialog.", eChatType.CT_System,
					                       eChatLoc.CL_SystemWindow);
				}
				else
				{
					client.Player.TempProperties.SetProperty(DoorMgr.WANT_TO_ADD_DOORS, true);
					client.Out.SendMessage("You will now be shown the add door dialog if door is not found in the DB.",
					                       eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}

				return;
			}

			if (client.Player.CurrentRegion.IsInstance)
			{
				client.Out.SendMessage("You can't add doors inside an instance.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			if (client.Player.TargetObject == null)
			{
				client.Out.SendMessage("You must target a door", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			if (client.Player.TargetObject != null &&
			    (client.Player.TargetObject is GameNPC || client.Player.TargetObject is GamePlayer))
			{
				client.Out.SendMessage("You must target a door", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			if (client.Player.TargetObject != null && client.Player.TargetObject is GameDoor)
			{
				targetDoor = (GameDoor) client.Player.TargetObject;
				DoorID = targetDoor.DoorId;
				doorType = targetDoor.DoorId/100000000;
			}

			if (args.Length < 2)
			{
				DisplaySyntax(client);
				return;
			}

			switch (args[1])
			{
				case "name":
					name(client, targetDoor, args);
					break;
				case "guild":
					guild(client, targetDoor, args);
					break;
				case "level":
					level(client, targetDoor, args);
					break;
				case "realm":
					realm(client, targetDoor, args);
					break;
				case "info":
					info(client, targetDoor);
					break;
				case "heal":
					heal(client, targetDoor);
					break;
				case "locked":
					locked(client, targetDoor);
					break;
				case "unlocked":
					unlocked(client, targetDoor);
					break;
				case "kill":
					kill(client, targetDoor, args);
					break;
				case "delete":
					delete(client, targetDoor);
					break;
				case "add":
					add(client, targetDoor);
					break;
				case "update":
					update(client, targetDoor);
					break;
				case "sound":
					sound(client, targetDoor, args);
					break;

				default:
					DisplaySyntax(client);
					return;
			}
		}

		#endregion

		private void add(GameClient client, GameDoor targetDoor)
		{
			var DOOR = DOLDB<DbDoor>.SelectObject(DB.Column("InternalID").IsEqualTo(DoorID));

			if (DOOR != null)
			{
				client.Out.SendMessage("The door is already in the database", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}
			if (DOOR == null)
			{
				if (doorType != 7 && doorType != 9)
				{
					var door = new DbDoor();
					door.ObjectId = null;
					door.InternalID = DoorID;
					door.Name = "door";
					door.Type = DoorID/100000000;
					door.Level = 20;
					door.Realm = 6;
					door.X = targetDoor.X;
					door.Y = targetDoor.Y;
					door.Z = targetDoor.Z;
					door.Heading = targetDoor.Heading;
					door.Health = 2545;
					GameServer.Database.AddObject(door);
					(targetDoor).AddToWorld();
					client.Player.Out.SendMessage("Added door ID:" + DoorID + "to the database", eChatType.CT_Important,
					                              eChatLoc.CL_SystemWindow);
					//DoorMgr.Init( );
					return;
				}
			}
		}

		private void update(GameClient client, GameDoor targetDoor)
		{
			delete(client, targetDoor);

			if (targetDoor != null)
			{
				if (doorType != 7 && doorType != 9)
				{
					var door = new DbDoor();
					door.ObjectId = null;
					door.InternalID = DoorID;
					door.Name = "door";
					door.Type = DoorID/100000000;
					door.Level = targetDoor.Level;
					door.Realm = (byte) targetDoor.Realm;
					door.Health = targetDoor.Health;
					door.Locked = Convert.ToInt32(targetDoor.Locked);
					door.X = client.Player.X;
					door.Y = client.Player.Y;
					door.Z = client.Player.Z;
					door.Heading = client.Player.Heading;
					GameServer.Database.AddObject(door);
					(targetDoor).AddToWorld();
					client.Player.Out.SendMessage("Added door " + DoorID + " to the database", eChatType.CT_Important,
					                              eChatLoc.CL_SystemWindow);
					return;
				}
			}
		}

		private void delete(GameClient client, GameDoor targetDoor)
		{
			var DOOR = DOLDB<DbDoor>.SelectObject(DB.Column("InternalID").IsEqualTo(DoorID));

			if (DOOR != null)
			{
				GameServer.Database.DeleteObject(DOOR);
				client.Out.SendMessage("Door removed", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}
			if (DOOR == null)
			{
				client.Out.SendMessage("This door doesn't exist in the database", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}
		}


		private void name(GameClient client, GameDoor targetDoor, string[] args)
		{
			string doorName = string.Empty;

			if (args.Length > 2)
				doorName = String.Join(" ", args, 2, args.Length - 2);

			if (doorName != string.Empty)
			{
				targetDoor.Name = CheckName(doorName, client);
				targetDoor.SaveIntoDatabase();
				client.Out.SendMessage("You changed the door name to " + targetDoor.Name, eChatType.CT_System,
				                       eChatLoc.CL_SystemWindow);
			}
			else
			{
				DisplaySyntax(client, args[1]);
			}
		}

		private void sound(GameClient client, GameDoor targetDoor, string[] args)
		{
			uint doorSound;

			try
			{
				if (args.Length > 2)
				{
					doorSound = Convert.ToUInt16(args[2]);
					targetDoor.Flag = doorSound;
					targetDoor.SaveIntoDatabase();
					client.Out.SendMessage("You set the door sound to " + doorSound, eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
				else
				{
					DisplaySyntax(client, args[1]);
				}
			}
			catch
			{
				DisplaySyntax(client, args[1]);
			}
		}

		private void guild(GameClient client, GameDoor targetDoor, string[] args)
		{
			string guildName = string.Empty;

			if (args.Length > 2)
				guildName = String.Join(" ", args, 2, args.Length - 2);

			if (guildName != string.Empty)
			{
				targetDoor.GuildName = CheckGuildName(guildName, client);
				targetDoor.SaveIntoDatabase();
				client.Out.SendMessage("You changed the door guild to " + targetDoor.GuildName, eChatType.CT_System,
				                       eChatLoc.CL_SystemWindow);
			}
			else
			{
				if (targetDoor.GuildName != string.Empty)
				{
					targetDoor.GuildName = string.Empty;
					targetDoor.SaveIntoDatabase();
					client.Out.SendMessage("Door guild removed", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
				else
					DisplaySyntax(client, args[1]);
			}
		}

		private void level(GameClient client, GameDoor targetDoor, string[] args)
		{
			byte level;

			try
			{
				level = Convert.ToByte(args[2]);
				targetDoor.Level = level;
				targetDoor.Health = targetDoor.MaxHealth;
				targetDoor.SaveIntoDatabase();
				client.Out.SendMessage("You changed the door level to " + targetDoor.Level, eChatType.CT_System,
				                       eChatLoc.CL_SystemWindow);
			}
			catch (Exception)
			{
				DisplaySyntax(client, args[1]);
			}
		}

		private void realm(GameClient client, GameDoor targetDoor, string[] args)
		{
			byte realm;

			try
			{
				realm = Convert.ToByte(args[2]);
				targetDoor.Realm = (eRealm) realm;
				targetDoor.SaveIntoDatabase();
				client.Out.SendMessage("You changed the door realm to " + targetDoor.Realm, eChatType.CT_System,
				                       eChatLoc.CL_SystemWindow);
			}
			catch (Exception)
			{
				DisplaySyntax(client, args[1]);
			}
		}

		private void info(GameClient client, GameDoor targetDoor)
		{
			if (targetDoor.Realm == eRealm.None)
				Realmname = "None";
			else if (targetDoor.Realm == eRealm.Albion)
				Realmname = "Albion";
			else if (targetDoor.Realm == eRealm.Midgard)
				Realmname = "Midgard";
			else if (targetDoor.Realm == eRealm.Hibernia)
				Realmname = "Hibernia";
			else if (targetDoor.Realm == eRealm.Door)
				Realmname = "All";

			if (targetDoor.Locked)
				statut = " Locked";
			else
				statut = " Unlocked";

			int doorType = DoorRequestHandler.HandlerDoorId / 100000000;

			var info = new List<string>();

			info.Add(" + Door Info :  " + targetDoor.Name);
			info.Add("  ");
			info.Add(" + Name : " + targetDoor.Name);
			info.Add(" + ID : " + DoorID);
			info.Add(" + Realm : " + (int) targetDoor.Realm + " : " + Realmname);
			info.Add(" + Level : " + targetDoor.Level);
			info.Add(" + Guild : " + targetDoor.GuildName);
			info.Add(" + Health : " + targetDoor.Health + " / " + targetDoor.MaxHealth);
			info.Add(" + Statut : " + statut);
			info.Add(" + Type : " + doorType);
			info.Add(" + X : " + targetDoor.X);
			info.Add(" + Y : " + targetDoor.Y);
			info.Add(" + Z : " + targetDoor.Z);
			info.Add(" + Heading : " + targetDoor.Heading);

			client.Out.SendCustomTextWindow("Door Information", info);
		}

		private void heal(GameClient client, GameDoor targetDoor)
		{
			targetDoor.Health = targetDoor.MaxHealth;
			targetDoor.SaveIntoDatabase();
			client.Out.SendMessage("You change the door health to " + targetDoor.Health, eChatType.CT_System,
			                       eChatLoc.CL_SystemWindow);
		}

		private void locked(GameClient client, GameDoor targetDoor)
		{
			targetDoor.Locked = true;
			targetDoor.SaveIntoDatabase();
			client.Out.SendMessage("Door " + targetDoor.Name + " is locked", eChatType.CT_System, eChatLoc.CL_SystemWindow);
		}

		private void unlocked(GameClient client, GameDoor targetDoor)
		{
			targetDoor.Locked = false;
			targetDoor.SaveIntoDatabase();
			client.Out.SendMessage("Door " + targetDoor.Name + " is unlocked", eChatType.CT_System, eChatLoc.CL_SystemWindow);
		}

		private void kill(GameClient client, GameDoor targetDoor, string[] args)
		{
			try
			{
				lock (targetDoor.XpGainersLock)
				{
					targetDoor.AddXPGainer(client.Player, targetDoor.Health);
					targetDoor.Die(client.Player);
					client.Out.SendMessage("Door " + targetDoor.Name + " health reaches 0", eChatType.CT_System,
										   eChatLoc.CL_SystemWindow);
				}
			}
			catch (Exception e)
			{
				client.Out.SendMessage(e.ToString(), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}
		}

		private string CheckName(string name, GameClient client)
		{
			if (name.Length > 47)
				client.Out.SendMessage("The door name must not be longer than 47 bytes", eChatType.CT_System,
				                       eChatLoc.CL_SystemWindow);
			return name;
		}

		private string CheckGuildName(string name, GameClient client)
		{
			if (name.Length > 47)
				client.Out.SendMessage("The guild name is " + name.Length + ", but only 47 bytes 'll be displayed",
				                       eChatType.CT_System, eChatLoc.CL_SystemWindow);
			return name;
		}
	}
}
