using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Language;

namespace DOL.GS.Housing
{
	public class HouseMgr
	{
		public static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		private static ECSGameTimer CheckRentTimer = null;
		private static Dictionary<ushort, Dictionary<int, House>> _houseList;
		private static Dictionary<ushort, int> _idList;
		private static int TimerInterval = Properties.RENT_CHECK_INTERVAL * 60 * 1000;

		protected enum eLotSpawnType
		{
			Marker,
			House
		}

		public static bool Start(GameClient client = null)
		{
			// load hookpoint offsets
			House.LoadHookpointOffsets();

			// initialize the house template manager
			HouseTemplateMgr.Initialize();

			_houseList = new Dictionary<ushort, Dictionary<int, House>>();
			_idList = new Dictionary<ushort, int>();

			int regions = 0;
			foreach (RegionEntry entry in WorldMgr.GetRegionList())
			{
				Region reg = WorldMgr.GetRegion(entry.id);
				if (reg != null && reg.UseHousingManager)
				{
					if (!_houseList.ContainsKey(reg.ID))
						_houseList.Add(reg.ID, new Dictionary<int, House>());

					if (!_idList.ContainsKey(reg.ID))
						_idList.Add(reg.ID, 0);

					regions++;
				}
			}

			int houses = 0;
			int lotmarkers = 0;

			foreach (DbHouse house in GameServer.Database.SelectAllObjects<DbHouse>())
			{
				Dictionary<int, House> housesForRegion;

				_houseList.TryGetValue(house.RegionID, out housesForRegion);

				// if we don't have the given region loaded as a housing zone, skip this house
				if (housesForRegion == null)
					continue;

				// if we already loaded this house, that's no bueno, but just skip
				if (housesForRegion.ContainsKey(house.HouseNumber))
					continue;

				if (SpawnLot(house, housesForRegion) == eLotSpawnType.House)
				{
					houses++;
				}
				else
				{
					lotmarkers++;
				}
			}

			if (log.IsInfoEnabled)
				log.Info("[Housing] Loaded " + houses + " houses and " + lotmarkers + " lotmarkers in " + regions + " regions!");

			if (client != null)
				client.Out.SendMessage("Loaded " + houses + " houses and " + lotmarkers + " lotmarkers in " + regions + " regions!", eChatType.CT_System, eChatLoc.CL_SystemWindow);

			if (CheckRentTimer == null)
			{
                CheckRentTimer =
					new ECSGameTimer(null, (ECSGameTimer.ECSTimerCallback) CheckRents, TimerInterval);
			}

			return true;
		}

		/// <summary>
		/// Force loading of any defined housing and house markers for a specific region
		/// </summary>
		/// <param name="regionID"></param>
		/// <returns></returns>
		public static string LoadHousingForRegion(ushort regionID)
		{
			string result = string.Empty;
			var regionHousing = DOLDB<DbHouse>.SelectObjects(DB.Column("RegionID").IsEqualTo(regionID));

			if (regionHousing == null || regionHousing.Count == 0)
				return "No housing found for region.";

			int houses = 0;
			int lotmarkers = 0;

			// re-initialize lists for this region

			if (!_houseList.ContainsKey(regionID))
			{
				_houseList.Add(regionID, new Dictionary<int, House>());
			}
			else
			{
				_houseList[regionID] = new Dictionary<int, House>();
			}

			if (!_idList.ContainsKey(regionID))
			{
				_idList.Add(regionID, 0);
			}
			else
			{
				_idList[regionID] = 0;
			}

			Dictionary<int, House> housesForRegion;
			_houseList.TryGetValue(regionID, out housesForRegion);

			if (housesForRegion == null)
			{
				result = "LoadHousingForRegion: No dictionary defined for region ID " + regionID;
				log.WarnFormat(result);
				return result;
			}

			foreach (DbHouse house in regionHousing)
			{
				// if we already loaded this house, that's no bueno, but just skip
				if (housesForRegion.ContainsKey(house.HouseNumber))
					continue;

				if (SpawnLot(house, housesForRegion) == eLotSpawnType.House)
				{
					houses++;
				}
				else
				{
					lotmarkers++;
				}
			}

			string results = "[Housing] Loaded " + houses + " houses and " + lotmarkers + " lotmarkers for region " + regionID + "!";

			if (log.IsInfoEnabled)
				log.Info(results);

			return results;
		}

		/// <summary>
		/// Spawn house or lotmarker on this lot
		/// </summary>
		/// <param name="house"></param>
		private static eLotSpawnType SpawnLot(DbHouse house, Dictionary<int, House> housesForRegion)
		{
			eLotSpawnType spawnType = eLotSpawnType.Marker;

			if (string.IsNullOrEmpty(house.OwnerID) == false)
			{
				var newHouse = new House(house) { UniqueID = house.HouseNumber };

				newHouse.LoadFromDatabase();

				// store the house
				housesForRegion.Add(newHouse.HouseNumber, newHouse);

				if (newHouse.Model > 0)
				{
					spawnType = eLotSpawnType.House;
				}
			}

			if (spawnType == eLotSpawnType.Marker)
			{
				// this is either an available lot or a purchased lot without a house
				GameLotMarker.SpawnLotMarker(house);
			}

			return spawnType;
		}

		/// <summary>
		/// Remove all housing for this regionID
		/// </summary>
		/// <param name="regionID"></param>
		public static void RemoveHousingForRegion(ushort regionID)
		{
			_houseList.Remove(regionID);
			_idList.Remove(regionID);
		}

		public static void Stop()
		{
		}

		public static IDictionary<int, House> GetHouses(ushort regionID)
		{
			// try and get the houses for the given region
			Dictionary<int, House> housesByRegion;
			_houseList.TryGetValue(regionID, out housesByRegion);

			return housesByRegion;
		}

		public static House GetHouse(ushort regionID, int houseNumber)
		{
			// try and get the houses for the given region
			_houseList.TryGetValue(regionID, out Dictionary<int, House> housesByRegion);

			// if we couldn't find houses for the region, return null
			if (housesByRegion == null)
				return null;

			// if the house number exists, return the house
			if (housesByRegion.TryGetValue(houseNumber, out House house))
				return house;

			// couldn't find the house, return null
			return null;
		}

		public static House GetHouse(int houseNumber)
		{
			// search thru each housing region, and if a house is found with
			// the given house number, return it
			foreach (var housingRegion in _houseList.Values)
			{
				if (housingRegion.TryGetValue(houseNumber, out House house))
					return house;
			}

			// couldn't find the house, return null
			return null;
		}

		public static GameConsignmentMerchant GetConsignmentByHouseNumber(int houseNumber)
		{
			// search thru each housing region, and if a house is found with
			// the given house number, return the consignment merchant
			foreach (var housingRegion in _houseList.Values)
			{
				if (housingRegion.TryGetValue(houseNumber, out House house))
					return house.ConsignmentMerchant;
			}

			// couldn't find the house, return null
			return null;
		}

		public static void AddHouse(House house)
		{
			// try and get the houses for the given region
			Dictionary<int, House> housesByRegion;
			_houseList.TryGetValue(house.RegionID, out housesByRegion);

			if (housesByRegion == null)
				return;

			// if the house doesn't exist yet, add it
			if (!housesByRegion.ContainsKey(house.HouseNumber))
			{
				housesByRegion.Add(house.HouseNumber, house);
			}
			else
			{
				// replace the existing lot with our new house
				housesByRegion[house.HouseNumber] = house;
			}

			if (house.Model == 0)
			{
				// if this is a lot marker purchase then reset all permissions and customization
				RemoveHouseItems(house);
				RemoveHousePermissions(house);
				ResetHouseData(house);
			}
			else
			{
				// create a new set of permissions
				for (int i = HousingConstants.MinPermissionLevel; i < HousingConstants.MaxPermissionLevel + 1; i++)
				{
					if (house.PermissionLevels.TryGetValue(i, out DbHousePermissions housePermissions))
					{
						if (housePermissions != null)
							GameServer.Database.DeleteObject(housePermissions);
					}

					// create a new, blank permission
					var permission = new DbHousePermissions(house.HouseNumber, i);
					house.PermissionLevels.Add(i, permission);

					// add the permission to the database
					GameServer.Database.AddObject(permission);
				}
			}

			// save the house, broadcast an update
			house.SaveIntoDatabase();
			house.SendUpdate();
		}

		public static bool UpgradeHouse(House house, DbInventoryItem deed)
		{
			int newModel = 0;
			switch (deed.Id_nb)
			{
				case "housing_alb_cottage_deed":
					newModel = 1;
					break;
				case "housing_alb_house_deed":
					newModel = 2;
					break;
				case "housing_alb_villa_deed":
					newModel = 3;
					break;
				case "housing_alb_mansion_deed":
					newModel = 4;
					break;
				case "housing_mid_cottage_deed":
					newModel = 5;
					break;
				case "housing_mid_house_deed":
					newModel = 6;
					break;
				case "housing_mid_villa_deed":
					newModel = 7;
					break;
				case "housing_mid_mansion_deed":
					newModel = 8;
					break;
				case "housing_hib_cottage_deed":
					newModel = 9;
					break;
				case "housing_hib_house_deed":
					newModel = 10;
					break;
				case "housing_hib_villa_deed":
					newModel = 11;
					break;
				case "housing_hib_mansion_deed":
					newModel = 12;
					break;
			}

			if (newModel == 0)
				return false;

			// remove all players from the home before we upgrade it
			foreach (GamePlayer player in house.GetAllPlayersInHouse())
			{
				player.LeaveHouse();
			}

			// if there is a consignment merchant, we have to re-initialize since we changed the house
			var merchant = DOLDB<DbHouseConsignmentMerchant>.SelectObject(DB.Column("HouseNumber").IsEqualTo(house.HouseNumber));
			long oldMerchantMoney = 0;
			if (merchant != null)
			{
				oldMerchantMoney = merchant.Money;
			}

			RemoveHouseItems(house);

			// change the model of the house
			house.Model = newModel;

			// re-add the merchant if there was one
			if (merchant != null)
			{
				house.AddConsignment(oldMerchantMoney);
			}

			// save the house, and broadcast an update
			house.SaveIntoDatabase();
			house.SendUpdate();

			return true;
		}

		public static void RemoveHouse(House house)
		{
			// try and get the houses for the given region
			Dictionary<int, House> housesByRegion;
			_houseList.TryGetValue(house.RegionID, out housesByRegion);

			if (housesByRegion == null)
				return;

			// remove all players from the house
			foreach (GamePlayer player in house.GetAllPlayersInHouse())
			{
				player.LeaveHouse();
			}

			// remove the house for all nearby players
			foreach (GamePlayer player in WorldMgr.GetPlayersCloseToSpot(house, WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendRemoveHouse(house);
				player.Out.SendGarden(house);
			}

			if (house.Model > 0)
				log.WarnFormat("Removing house #{0} owned by {1}!", house.HouseNumber, house.DatabaseItem.Name);
			else
				log.WarnFormat("Lotmarker #{0} owned by {1} had rent due, lot now for sale!", house.HouseNumber, house.DatabaseItem.Name);

			RemoveHouseItems(house);
			RemoveHousePermissions(house);
			ResetHouseData(house);

			house.OwnerID = string.Empty;
			house.KeptMoney = 0;
			house.Name = string.Empty; // not null !
			house.DatabaseItem.CreationTime = DateTime.Now;
			house.DatabaseItem.LastPaid = DateTime.MinValue;

			// saved the cleared house in the database
			house.SaveIntoDatabase();

			// remove the house from the list of houses in the region
			housesByRegion.Remove(house.HouseNumber);

			// spawn a lot marker for the now-empty lot
			GameLotMarker.SpawnLotMarker(house.DatabaseItem);
		}

		public static void RemoveHouseItems(House house)
		{
			house.RemoveConsignmentMerchant();

			IList<DbHouseIndoorItem> iobjs = DOLDB<DbHouseIndoorItem>.SelectObjects(DB.Column("HouseNumber").IsEqualTo(house.HouseNumber));
			GameServer.Database.DeleteObject(iobjs);
			house.IndoorItems.Clear();

			IList<DbHouseOutdoorItem> oobjs = DOLDB<DbHouseOutdoorItem>.SelectObjects(DB.Column("HouseNumber").IsEqualTo(house.HouseNumber));
			GameServer.Database.DeleteObject(oobjs);
			house.OutdoorItems.Clear();

			IList<DbHouseHookPointItem> hpobjs = DOLDB<DbHouseHookPointItem>.SelectObjects(DB.Column("HouseNumber").IsEqualTo(house.HouseNumber));
			GameServer.Database.DeleteObject(hpobjs);

			foreach (DbHouseHookPointItem item in house.HousepointItems.Values)
			{
				if (item.GameObject is GameObject)
				{
					(item.GameObject as GameObject).Delete();
				}
			}
			house.HousepointItems.Clear();
		}

		public static void RemoveHousePermissions(House house)
		{
			// clear the house number for the guild if this is a guild house
			if (house.DatabaseItem.GuildHouse)
			{
				Guild guild = GuildMgr.GetGuildByName(house.DatabaseItem.GuildName);
				if (guild != null)
				{
					guild.GuildHouseNumber = 0;
				}
			}

			house.DatabaseItem.GuildHouse = false;
			house.DatabaseItem.GuildName = null;

			IList<DbHousePermissions> pobjs = DOLDB<DbHousePermissions>.SelectObjects(DB.Column("HouseNumber").IsEqualTo(house.HouseNumber));
			GameServer.Database.DeleteObject(pobjs);
			house.PermissionLevels.Clear();

			IList<DbHouseCharsXPerms> cpobjs = DOLDB<DbHouseCharsXPerms>.SelectObjects(DB.Column("HouseNumber").IsEqualTo(house.HouseNumber));
			GameServer.Database.DeleteObject(cpobjs);
			house.CharXPermissions.Clear();
		}

		public static void ResetHouseData(House house)
		{
			house.Model = 0;
			house.Emblem = 0;
			house.IndoorGuildBanner = false;
			house.IndoorGuildShield = false;
			house.DoorMaterial = 0;
			house.OutdoorGuildBanner = false;
			house.OutdoorGuildShield = false;
			house.Porch = false;
			house.PorchMaterial = 0;
			house.PorchRoofColor = 0;
			house.RoofMaterial = 0;
			house.Rug1Color = 0;
			house.Rug2Color = 0;
			house.Rug3Color = 0;
			house.Rug4Color = 0;
			house.TrussMaterial = 0;
			house.WallMaterial = 0;
			house.WindowMaterial = 0;
		}

		/// <summary>
		/// Checks if a player is the owner of the house, this checks all characters on the account
		/// </summary>
		/// <param name="house">The house object</param>
		/// <param name="player">The player to check</param>
		/// <returns>True if the player is the owner</returns>
		public static bool IsOwner(DbHouse house, GamePlayer player)
		{
			// house and player can't be null
			if (house == null || player == null)
				return false;

			// if owner id isn't set, there is no owner
			if (string.IsNullOrEmpty(house.OwnerID))
				return false;

			// check if this a guild house, and if the player
			// 1) belongs to the guild and is 2) a GM in the guild
			if (player.Guild != null && house.GuildHouse)
			{
				if (player.Guild.Name == house.GuildName && player.Guild.HasRank(player, Guild.eRank.Leader))
					return true;
			}
			else
			{
				foreach (DbCoreCharacter c in player.Client.Account.Characters)
				{
					if (house.OwnerID == c.ObjectId)
						return true;
				}
			}

			return false;
		}

		public static int GetHouseNumberByPlayer(GamePlayer p)
		{
			House house = GetHouseByPlayer(p);

			return house != null ? house.HouseNumber : 0;
		}

		/// <summary>
		/// Get the house object from the owner player
		/// </summary>
		public static House GetHouseByPlayer(GamePlayer player)
		{
			HashSet<string> characterIds = new();

			foreach (DbCoreCharacter character in player.Client.Account.Characters)
			{
				if ((eRealm) character.Realm == player.Realm)
					characterIds.Add(character.ObjectId);
			}

			return GetHouseByCharacterIds(characterIds);
		}

		public static House GetHouseByCharacterIds(HashSet<string> characterIds)
		{
			foreach (var housesInWorld in _houseList.ToList())
			{
				foreach (var housesInRegion in housesInWorld.Value.ToList())
				{
					House house = housesInRegion.Value;

					if (characterIds.Contains(house.OwnerID))
						return house;
				}
			}

			return null;
		}

		/// <summary>
		/// Gets the guild house object by real owner
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public static House GetGuildHouseByPlayer(GamePlayer p)
		{
			// make sure player is in a guild
			if (p.Guild == null || p.Guild.GuildOwnsHouse == false)
				return null;

			var house = GetHouse(p.Guild.GuildHouseNumber);

			if (p.Realm != house?.Realm)
				return null;

			if (house != null)
				return house;

			// check every house in every region until we find
			// a house that belongs to the same guild as the player
			foreach (var regs in _houseList)
			{
				foreach (var entry in regs.Value)
				{
					house = entry.Value;

					if (house.DatabaseItem.GuildName == p.Guild.Name)
						return house;
				}
			}

			// didn't find a house that belonged to the player's guild,
			// or they aren't in a guild, so return null
			return null;
		}

		/// <summary>
		/// Transfer a house to a guild house
		/// </summary>
		/// <param name="player"></param>
		/// <param name="house"></param>
		/// <returns></returns>
		public static bool HouseTransferToGuild(GamePlayer player, House house)
		{
			// player must be in a guild
			if (player.Guild == null)
				return false;

			// player's guild can't already have a guild house
			if (player.Guild.GuildOwnsHouse)
				return false;

			// player needs to own the house to be able to xfer it
			if (house.HasOwnerPermissions(player) == false)
			{
				ChatUtil.SendSystemMessage(player, "You do not own this house!");
				return false;
			}

			// player needs to be a GM in the guild to xfer his personal house to the guild
			if (player.Guild.HasRank(player, Guild.eRank.Leader) == false)
			{
				ChatUtil.SendSystemMessage(player, "You are not the leader of a guild!");
				return false;
			}

			// Demand any consignment merchant inventory is removed before allowing a transfer
			var consignmentMerchant = house.ConsignmentMerchant;
			if (consignmentMerchant != null && (consignmentMerchant.GetDbItems(player).Count > 0 || consignmentMerchant.TotalMoney > 0))
			{
				ChatUtil.SendSystemMessage(player, "All items and money must be removed from your consignment merchant in order to transfer this house!");
				return false;
			}

			// send house xfer prompt to player
			player.Out.SendCustomDialog(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Player.Housing.TransferToGuild", player.Guild.Name), MakeGuildLot);

			return true;
		}

		private static void MakeGuildLot(GamePlayer player, byte response)
		{
			// user responded no/decline
			if (response != 0x01)
				return;

			var playerHouse = GetHouse(GetHouseNumberByPlayer(player));
			var playerGuild = player.Guild;

			// double check and make sure this guild isn't null
			if (playerGuild == null)
				return;

			// adjust the house to be under guild control
			playerHouse.DatabaseItem.OwnerID = playerGuild.GuildID;
			playerHouse.DatabaseItem.Name = playerGuild.Name;
			playerHouse.DatabaseItem.GuildHouse = true;
			playerHouse.DatabaseItem.GuildName = playerGuild.Name;

			// adjust guild to reflect their new guild house
			player.Guild.GuildHouseNumber = playerHouse.HouseNumber;

			// notify guild members of the guild house acquisition
			player.Guild.SendMessageToGuildMembers(
				LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Player.Housing.GuildNowOwns", player.Guild.Name, player.Name),
				eChatType.CT_Guild, eChatLoc.CL_SystemWindow);

			// save the guild and broadcast updates
			player.Guild.SaveIntoDatabase();

			// save the house and broadcast updates
			playerHouse.SaveIntoDatabase();
			playerHouse.SendUpdate();
		}

		public static long GetRentByModel(int model)
		{
			if (model > 0)
			{
				switch (model % 4)
				{
					case 0:
						return Properties.HOUSING_RENT_MANSION;
					case 1:
						return Properties.HOUSING_RENT_COTTAGE;
					case 2:
						return Properties.HOUSING_RENT_HOUSE;
					case 3:
						return Properties.HOUSING_RENT_VILLA;
				}
			}

			return Properties.HOUSING_RENT_COTTAGE;
		}

		public static int CheckRents(ECSGameTimer timer)
		{
			if (Properties.RENT_DUE_DAYS == 0)
				return 0;

			TimeSpan diff;
			var houseRemovalList = new List<House>();

			foreach (var regs in _houseList)
			{
				foreach (var entry in regs.Value)
				{
					var house = entry.Value;

					// if the house has no owner or is set to not be purged, 
					// we just skip over it
					if (string.IsNullOrEmpty(house.OwnerID) || house.NoPurge)
						continue;

					// get the time that rent was last paid for the house
					diff = DateTime.Now - house.LastPaid;

					// get the amount of rent for the given house
					long rent = GetRentByModel(house.Model);

					// Does this house need to pay rent?
					if (rent > 0L && diff.Days >= Properties.RENT_DUE_DAYS)
					{					
						long lockboxAmount = house.KeptMoney;
						long consignmentAmount = 0;

						var consignmentMerchant = house.ConsignmentMerchant;
						if (consignmentMerchant != null)
						{
							consignmentAmount = consignmentMerchant.TotalMoney;
						}

						// try to pull from the lockbox first
						if (lockboxAmount >= rent)
						{
							house.KeptMoney -= rent;
							house.LastPaid = DateTime.Now;
							house.SaveIntoDatabase();
						}
						else
						{
							long remainingDifference = (rent - lockboxAmount);

							// not enough was in the lockbox.  see if we have the difference
							// on the consignment merchant
							if (remainingDifference <= consignmentAmount)
							{
								// we have the difference, phew!
								house.KeptMoney = 0;
								consignmentMerchant.TotalMoney -= remainingDifference;
								house.LastPaid = DateTime.Now;
								house.SaveIntoDatabase();
							}
							else
							{
								// house can't afford rent, so we schedule house to be repossessed.
								log.Warn($"[HOUSING] House {house.HouseNumber} owned by {house.Name} can't afford rent and is being repossesed! rentamount: {rent} lockboxAmount: {lockboxAmount} consignmentAmount: {consignmentAmount}");
								houseRemovalList.Add(house);
							}
						}
					}
				}
			}

			foreach (House h in houseRemovalList)
			{
				RemoveHouse(h);
			}

			return TimerInterval;
		}


		public static void SendHousingMerchantWindow(GamePlayer player, eMerchantWindowType merchantType)
		{
			GameServer.ServerRules.SendHousingMerchantWindow(player, merchantType);
		}

		public static void BuyHousingItem(GamePlayer player, ushort slot, byte count, eMerchantWindowType merchantType)
		{
			GameServer.ServerRules.BuyHousingItem(player, slot, count, merchantType);
		}


		/// <summary>
		/// This function gets the house close to spot
		/// </summary>
		/// <returns>array of house</returns>
		public static IEnumerable GetHousesCloseToSpot(ushort regionid, int x, int y, int radius)
		{
			var myhouses = new ArrayList();
			int radiussqrt = radius * radius;
			foreach (House house in GetHouses(regionid).Values)
			{
				int xdiff = house.X - x;
				int ydiff = house.Y - y;
				int range = xdiff * xdiff + ydiff * ydiff;
				if (range < 0)
					range *= -1;
				if (range > radiussqrt)
					continue;
				myhouses.Add(house);
			}
			return myhouses;
		}
	}
}
