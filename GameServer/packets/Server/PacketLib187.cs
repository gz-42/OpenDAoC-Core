using System;
using System.Reflection;
using DOL.Database;
using DOL.GS.Quests;

namespace DOL.GS.PacketHandler
{
	[PacketLib(187, GameClient.eClientVersion.Version187)]
	public class PacketLib187 : PacketLib186
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs a new PacketLib for Version 1.87 clients
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib187(GameClient client)
			: base(client)
		{
		}

		public override void SendQuestOfferWindow(GameNPC questNPC, GamePlayer player, RewardQuest quest)
		{
			SendQuestWindow(questNPC, player, quest, true);
		}

		public override void SendQuestRewardWindow(GameNPC questNPC, GamePlayer player, RewardQuest quest)
		{
			SendQuestWindow(questNPC, player, quest, false);
		}

		protected override void SendQuestWindow(GameNPC questNPC, GamePlayer player, RewardQuest quest,	bool offer)
		{
			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.Dialog))))
			{
				ushort QuestID = QuestMgr.GetIDForQuestType(quest.GetType());
				pak.WriteShort((offer) ? (byte)0x22 : (byte)0x21); // Dialog
				pak.WriteShort(QuestID);
				pak.WriteShort((ushort)questNPC.ObjectID);
				pak.WriteByte(0x00); // unknown
				pak.WriteByte(0x00); // unknown
				pak.WriteByte(0x00); // unknown
				pak.WriteByte(0x00); // unknown
				pak.WriteByte((offer) ? (byte)0x02 : (byte)0x01); // Accept/Decline or Finish/Not Yet
				pak.WriteByte(0x01); // Wrap
				pak.WritePascalString(quest.Name);

				if (quest.Summary.Length > 255)
				{
					pak.WritePascalString(quest.Summary.Substring(0, 255));
				}
				else
				{
					pak.WritePascalString(quest.Summary);
				}

				if (offer)
				{
					if (quest.Story.Length > (ushort)ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH)
					{
						pak.WriteShort((ushort)ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH);
						pak.WriteStringBytes(quest.Story.Substring(0, (ushort)ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH));
					}
					else
					{
						pak.WriteShort((ushort)quest.Story.Length);
						pak.WriteStringBytes(quest.Story);
					}
				}
				else
				{
					if (quest.Conclusion.Length > (ushort)ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH)
					{
						pak.WriteShort((ushort)ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH);
						pak.WriteStringBytes(quest.Conclusion.Substring(0, (ushort)ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH));
					}
					else
					{
						pak.WriteShort((ushort)quest.Conclusion.Length);
						pak.WriteStringBytes(quest.Conclusion);
					}
				}

				pak.WriteShort(QuestID);
				pak.WriteByte((byte)quest.Goals.Count); // #goals count
				foreach (RewardQuest.QuestGoal goal in quest.Goals)
				{
					pak.WritePascalString(String.Format("{0}\r", goal.Description));
				}
				pak.WriteByte((byte)quest.Level);
				pak.WriteByte((byte)quest.Rewards.MoneyPercent);
				pak.WriteByte((byte)quest.Rewards.ExperiencePercent(player));
				pak.WriteByte((byte)quest.Rewards.BasicItems.Count);
				foreach (DbItemTemplate reward in quest.Rewards.BasicItems)
					WriteTemplateData(pak, reward, 1);
				pak.WriteByte((byte)quest.Rewards.ChoiceOf);
				pak.WriteByte((byte)quest.Rewards.OptionalItems.Count);
				foreach (DbItemTemplate reward in quest.Rewards.OptionalItems)
					WriteTemplateData(pak, reward, 1);
				SendTCP(pak);
			}
		}

		protected virtual void WriteTemplateData(GSTCPPacketOut pak, DbItemTemplate template, int count)
		{
			if (template == null)
			{
				pak.Fill(0x00, 19);
				return;
			}

			pak.WriteByte((byte)template.Level);

			int value1;
			int value2;

			switch (template.Object_Type)
			{
				case (int)eObjectType.Arrow:
				case (int)eObjectType.Bolt:
				case (int)eObjectType.Poison:
				case (int)eObjectType.GenericItem:
					value1 = count; // Count
					value2 = template.SPD_ABS;
					break;
				case (int)eObjectType.Thrown:
					value1 = template.DPS_AF;
					value2 = count; // Count
					break;
				case (int)eObjectType.Instrument:
					value1 = (template.DPS_AF == 2 ? 0 : template.DPS_AF);
					value2 = 0;
					break;
				case (int)eObjectType.Shield:
					value1 = template.Type_Damage;
					value2 = template.DPS_AF;
					break;
				case (int)eObjectType.AlchemyTincture:
				case (int)eObjectType.SpellcraftGem:
					value1 = 0;
					value2 = 0;
					/*
					must contain the quality of gem for spell craft and think same for tincture
					*/
					break;
				case (int)eObjectType.GardenObject:
					value1 = 0;
					value2 = template.SPD_ABS;
					/*
					Value2 byte sets the width, only lower 4 bits 'seem' to be used (so 1-15 only)

					The byte used for "Hand" (IE: Mini-delve showing a weapon as Left-Hand
					usabe/TwoHanded), the lower 4 bits store the height (1-15 only)
					*/
					break;

				default:
					value1 = template.DPS_AF;
					value2 = template.SPD_ABS;
					break;
			}
			pak.WriteByte((byte)value1);
			pak.WriteByte((byte)value2);

			if (template.Object_Type == (int)eObjectType.GardenObject)
				pak.WriteByte((byte)(template.DPS_AF));
			else
				pak.WriteByte((byte)(template.Hand << 6));
			pak.WriteByte((byte)((template.Type_Damage > 3
				? 0
				: template.Type_Damage << 6) | template.Object_Type));
			pak.WriteShort((ushort)template.Weight);
			pak.WriteByte(template.BaseConditionPercent);
			pak.WriteByte(template.BaseDurabilityPercent);
			pak.WriteByte((byte)template.Quality);
			pak.WriteByte((byte)template.Bonus);
			pak.WriteShort((ushort)template.Model);
			pak.WriteByte((byte)template.Extension);
			if (template.Emblem != 0)
				pak.WriteShort((ushort)template.Emblem);
			else
				pak.WriteShort((ushort)template.Color);
			pak.WriteByte((byte)0); // Flag
			pak.WriteByte((byte)template.Effect);
			if (count > 1)
				pak.WritePascalString(String.Format("{0} {1}", count, template.Name));
			else
				pak.WritePascalString(template.Name);
		}

		protected override void SendQuestPacket(AbstractQuest quest, byte index)
		{
			if (quest == null || quest is not RewardQuest)
			{
				base.SendQuestPacket(quest, index);
				return;
			}

			RewardQuest rewardQuest = quest as RewardQuest;
			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.QuestEntry))))
			{
				pak.WriteByte(index);
				pak.WriteByte((byte)rewardQuest.Name.Length);
				pak.WriteShort(0x00); // unknown
				pak.WriteByte((byte)rewardQuest.Goals.Count);
				pak.WriteByte((byte)rewardQuest.Level);
				pak.WriteStringBytes(rewardQuest.Name);
				pak.WritePascalString(rewardQuest.Description);
				int goalindex = 0;
				foreach (RewardQuest.QuestGoal goal in rewardQuest.Goals)
				{
					goalindex++;
					String goalDesc = String.Format("{0}\r", goal.Description);
					pak.WriteShortLowEndian((ushort)goalDesc.Length);
					pak.WriteStringBytes(goalDesc);
					pak.WriteShortLowEndian((ushort)goal.ZoneID2);
					pak.WriteShortLowEndian((ushort)goal.XOffset2);
					pak.WriteShortLowEndian((ushort)goal.YOffset2);
					pak.WriteShortLowEndian(0x00);	// unknown
					pak.WriteShortLowEndian((ushort)goal.Type);
					pak.WriteShortLowEndian(0x00);	// unknown
					pak.WriteShortLowEndian((ushort)goal.ZoneID1);
					pak.WriteShortLowEndian((ushort)goal.XOffset1);
					pak.WriteShortLowEndian((ushort)goal.YOffset1);
					pak.WriteByte((byte)((goal.IsAchieved) ? 0x01 : 0x00));
					if (goal.QuestItem == null)
						pak.WriteByte(0x00);
					else
					{
						pak.WriteByte((byte)goalindex);
						WriteTemplateData(pak, goal.QuestItem, 1);
					}
				}
				SendTCP(pak);
			}
		}
	}
}
