using System.Reflection;

namespace DOL.GS.PacketHandler
{
	[PacketLib(186, GameClient.eClientVersion.Version186)]
	public class PacketLib186 : PacketLib185
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs a new PacketLib for Version 1.86 clients
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib186(GameClient client)
			: base(client)
		{
		}

		/// <summary>
		/// The bow prepare animation
		/// </summary>
		public override int BowPrepare
		{
			get { return 0x3E80; }
		}

		/// <summary>
		/// one dual weapon hit animation
		/// </summary>
		public override int OneDualWeaponHit
		{
			get { return 0x3E81; }
		}

		/// <summary>
		/// both dual weapons hit animation
		/// </summary>
		public override int BothDualWeaponHit
		{
			get { return 0x3E82; }
		}

		/// <summary>
		/// The bow shoot animation
		/// </summary>
		public override int BowShoot
		{
			get { return 0x3E83; }
		}

		public override void SendCombatAnimation(GameObject attacker, GameObject defender, ushort weaponID, ushort shieldID, int style, byte stance, byte result, byte targetHealthPercent)
		{
			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.CombatAnimation))))
			{
				if (attacker != null)
					pak.WriteShort((ushort)attacker.ObjectID);
				else
					pak.WriteShort(0x00);

				if (defender != null)
					pak.WriteShort((ushort)defender.ObjectID);
				else
					pak.WriteShort(0x00);

				pak.WriteShort(weaponID);
				pak.WriteShort(shieldID);
				pak.WriteShortLowEndian((ushort)style);
				pak.WriteByte(stance);
				pak.WriteByte(result);

				// If Health Percent is invalid get the living Health.
				if (defender is GameLiving && targetHealthPercent > 100)
				{
					targetHealthPercent = (defender as GameLiving).HealthPercent;
				}

				pak.WriteByte(targetHealthPercent);
				pak.WriteByte(0);//unk
				SendTCP(pak);
			}
		}

		public override void SendMinotaurRelicMapRemove(byte id)
		{
			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.MinotaurRelicMapRemove))))
			{
				pak.WriteIntLowEndian((uint)id);
				SendTCP(pak);
			}
		}

		public override void SendMinotaurRelicMapUpdate(byte id, ushort region, int x, int y, int z)
		{
			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.MinotaurRelicMapUpdate))))
			{
				pak.WriteIntLowEndian((uint)id);
				pak.WriteIntLowEndian((uint)region);
				pak.WriteIntLowEndian((uint)x);
				pak.WriteIntLowEndian((uint)y);
				pak.WriteIntLowEndian((uint)z);

				SendTCP(pak);
			}
		}

		public override void SendMinotaurRelicWindow(GamePlayer player, int effect, bool flag)
		{
			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.VisualEffect))))
			{
				pak.WriteShort((ushort)player.ObjectID);
				pak.WriteByte((byte)13);

				if (flag)
				{
					pak.WriteByte(0);
					pak.WriteInt((uint)effect);
				}
				else
				{
					pak.WriteByte(1);
					pak.WriteInt((uint)effect);
				}

				SendTCP(pak);
			}
		}

		public override void SendMinotaurRelicBarUpdate(GamePlayer player, int xp)
		{
			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.VisualEffect))))
			{
				pak.WriteShort((ushort)player.ObjectID);
				pak.WriteByte((byte)14);
				pak.WriteByte(0);
				//4k maximum
				if (xp > 4000) xp = 4000;
				if (xp < 0) xp = 0;

				pak.WriteInt((uint)xp);

				SendTCP(pak);
			}
		}
	}
}
