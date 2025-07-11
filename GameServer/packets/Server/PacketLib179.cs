using System.Reflection;
using DOL.GS.Housing;
using DOL.GS.PlayerTitles;

namespace DOL.GS.PacketHandler
{
	[PacketLib(179, GameClient.eClientVersion.Version179)]
	public class PacketLib179 : PacketLib178
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs a new PacketLib for Version 1.79 clients
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib179(GameClient client):base(client)
		{
		}

		public override void SendUpdatePlayer()
		{
			GamePlayer player = m_gameClient.Player;
			if (player == null)
				return;

			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.VariousUpdate))))
			{
				pak.WriteByte(0x03); //subcode
				pak.WriteByte(0x0f); //number of entry
				pak.WriteByte(0x00); //subtype
				pak.WriteByte(0x00); //unk
									 //entry :

				pak.WriteByte(player.GetDisplayLevel(m_gameClient.Player)); //level
				pak.WritePascalString(player.Name); // player name
				pak.WriteByte((byte)(player.MaxHealth >> 8)); // maxhealth high byte ?
				pak.WritePascalString(player.CharacterClass.Name); // class name
				pak.WriteByte((byte)(player.MaxHealth & 0xFF)); // maxhealth low byte ?
				pak.WritePascalString( /*"The "+*/player.CharacterClass.Profession); // Profession
				pak.WriteByte(0x00); //unk
				pak.WritePascalString(player.CharacterClass.GetTitle(player, player.Level)); // player level
																							 //todo make function to calcule realm rank
																							 //client.Player.RealmPoints
																							 //todo i think it s realmpoint percent not realrank
				pak.WriteByte((byte)player.RealmLevel); //urealm rank
				pak.WritePascalString(player.RealmRankTitle(player.Client.Account.Language)); // Realm title
				pak.WriteByte((byte)player.RealmSpecialtyPoints); // realm skill points
				pak.WritePascalString(player.CharacterClass.BaseName); // base class
				pak.WriteByte((byte)(HouseMgr.GetHouseNumberByPlayer(player) >> 8)); // personal house high byte
				pak.WritePascalString(player.GuildName); // Guild name
				pak.WriteByte((byte)(HouseMgr.GetHouseNumberByPlayer(player) & 0xFF)); // personal house low byte
				pak.WritePascalString(player.LastName); // Last name
				pak.WriteByte((byte)(player.MLLevel + 1)); // ML Level (+1)
				pak.WritePascalString(player.RaceName); // Race name
				pak.WriteByte(0x0);

				if (player.GuildRank != null)
					pak.WritePascalString(player.GuildRank.Title); // Guild title
				else
					pak.WritePascalString("");
				pak.WriteByte(0x0);

				AbstractCraftingSkill skill = CraftingMgr.getSkillbyEnum(player.CraftingPrimarySkill);
				if (skill != null)
					pak.WritePascalString(skill.Name); //crafter guilde: alchemist
				else
					pak.WritePascalString("None"); //no craft skill at start

				pak.WriteByte(0x0);
				pak.WritePascalString(player.CraftTitle.GetValue(player, player)); //crafter title: legendary alchemist
				pak.WriteByte(0x0);
				pak.WritePascalString(player.MLTitle.GetValue(player, player)); //ML title

				// new in 1.75
				pak.WriteByte(0x0);
				if (player.CurrentTitle != PlayerTitleMgr.ClearTitle)
					pak.WritePascalString(player.CurrentTitle.GetValue(player, player)); // new in 1.74 - Custom title
				else
					pak.WritePascalString("None");

				// new in 1.79
				if (player.Champion)
					pak.WriteByte((byte)(player.ChampionLevel + 1)); // Champion Level (+1)
				else
					pak.WriteByte(0x0);
				pak.WritePascalString(player.CLTitle.GetValue(player, player)); // Champion Title
				SendTCP(pak);
			}
		}

		public override void SendUpdatePoints()
		{
			if (m_gameClient.Player == null)
				return;
			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.CharacterPointsUpdate))))
			{
				pak.WriteInt((uint)m_gameClient.Player.RealmPoints);
				pak.WriteShort(m_gameClient.Player.LevelPermill);
				pak.WriteShort((ushort) m_gameClient.Player.SkillSpecialtyPoints);
				pak.WriteInt((uint)m_gameClient.Player.BountyPoints);
				pak.WriteShort((ushort) m_gameClient.Player.RealmSpecialtyPoints);
				pak.WriteShort(m_gameClient.Player.ChampionLevelPermill);
				SendTCP(pak);
			}
		}
	}
}
