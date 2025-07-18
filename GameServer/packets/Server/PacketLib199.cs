/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;

namespace DOL.GS.PacketHandler
{
	[PacketLib(199, GameClient.eClientVersion.Version199)]
	public class PacketLib199 : PacketLib198
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs a new PacketLib for Version 1.99 clients
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib199(GameClient client)
			: base(client)
		{
			//Dunnerholl 2009-09-10: some new packets found that have still to be included
		}

		// Dunnerholl 2009-09-10 i do not like the way this is done
		public override void SendCharacterOverview(eRealm realm)
		{
			int firstAccountSlot;
			switch (realm)
			{
				case eRealm.Albion:
					firstAccountSlot = 100;
					break;
				case eRealm.Midgard:
					firstAccountSlot = 200;
					break;
				case eRealm.Hibernia:
					firstAccountSlot = 300;
					break;
				default:
					throw new Exception("CharacterOverview requested for unknown realm " + realm);
			}

			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.CharacterOverview))))
			{
				pak.FillString(m_gameClient.Account.Name, 24);
				IList<DbInventoryItem> items;
				DbCoreCharacter[] characters = m_gameClient.Account.Characters;
				if (characters == null)
				{
					pak.Fill(0x0, 1880);
				}
				else
				{
					for (int i = firstAccountSlot; i < firstAccountSlot + 10; i++)
					{
						bool written = false;
						for (int j = 0; j < characters.Length && !written; j++)
						{
							if (characters[j].AccountSlot == i)
							{
								pak.FillString(characters[j].Name, 24);
								items = DOLDB<DbInventoryItem>.SelectObjects(DB.Column("OwnerID").IsEqualTo(characters[j].ObjectId).And(DB.Column("SlotPosition").IsGreaterOrEqualTo(10)).And(DB.Column("SlotPosition").IsLessOrEqualTo(37)));
								byte ExtensionTorso = 0;
								byte ExtensionGloves = 0;
								byte ExtensionBoots = 0;
								foreach (DbInventoryItem item in items)
								{
									switch (item.SlotPosition)
									{
										case 22:
											ExtensionGloves = item.Extension;
											break;
										case 23:
											ExtensionBoots = item.Extension;
											break;
										case 25:
											ExtensionTorso = item.Extension;
											break;
										default:
											break;
									}
								}
								pak.WriteByte(0x01);
								pak.WriteByte((byte)characters[j].EyeSize);
								pak.WriteByte((byte)characters[j].LipSize);
								pak.WriteByte((byte)characters[j].EyeColor);
								pak.WriteByte((byte)characters[j].HairColor);
								pak.WriteByte((byte)characters[j].FaceType);
								pak.WriteByte((byte)characters[j].HairStyle);
								pak.WriteByte((byte)((ExtensionBoots << 4) | ExtensionGloves));
								pak.WriteByte((byte)((ExtensionTorso << 4) | (characters[j].IsCloakHoodUp ? 0x1 : 0x0)));
								pak.WriteByte((byte)characters[j].CustomisationStep); //1 = auto generate config, 2= config ended by player, 3= enable config to player
								pak.WriteByte((byte)characters[j].MoodType);
								pak.Fill(0x0, 13); //0 String
								Region reg = WorldMgr.GetRegion((ushort)characters[j].Region);
								if (reg != null)
								{
									var description = m_gameClient.GetTranslatedSpotDescription(reg, characters[j].Xpos, characters[j].Ypos, characters[j].Zpos);
									pak.FillString(description, 24);
								}
								else
								{
									pak.Fill(0x0, 24); //No known location
								}
								if (characters[j].Class == 0)
								{
									pak.FillString("", 24); //Class name
								}
								else
								{
									pak.FillString(((eCharacterClass)characters[j].Class).ToString(), 24); //Class name
								}
								//pak.FillString(GamePlayer.RACENAMES[characters[j].Race], 24);
	                            pak.FillString(m_gameClient.RaceToTranslatedName(characters[j].Race, characters[j].Gender), 24);
								pak.WriteByte((byte)characters[j].Level);
								pak.WriteByte((byte)characters[j].Class);
								pak.WriteByte((byte)characters[j].Realm);
								pak.WriteByte((byte)((((characters[j].Race & 0x10) << 2) + (characters[j].Race & 0x0F)) | (characters[j].Gender << 4))); // race max value can be 0x1F
								pak.WriteShortLowEndian((ushort)characters[j].CurrentModel);
								pak.WriteByte((byte)characters[j].Region);
								if (reg == null || (int)m_gameClient.ClientType > reg.Expansion)
									pak.WriteByte(0x00);
								else
									pak.WriteByte((byte)(reg.Expansion + 1)); //0x04-Cata zone, 0x05 - DR zone
								pak.WriteInt(0x0); // Internal database ID
								pak.WriteByte((byte)characters[j].Strength);
								pak.WriteByte((byte)characters[j].Dexterity);
								pak.WriteByte((byte)characters[j].Constitution);
								pak.WriteByte((byte)characters[j].Quickness);
								pak.WriteByte((byte)characters[j].Intelligence);
								pak.WriteByte((byte)characters[j].Piety);
								pak.WriteByte((byte)characters[j].Empathy);
								pak.WriteByte((byte)characters[j].Charisma);

								int found = 0;
								//16 bytes: armor model
								for (int k = 0x15; k < 0x1D; k++)
								{
									found = 0;
									foreach (DbInventoryItem item in items)
									{
										if (item.SlotPosition == k && found == 0)
										{
											pak.WriteShortLowEndian((ushort)item.Model);
											found = 1;
										}
									}
									if (found == 0)
										pak.WriteShort(0x00);
								}
								//16 bytes: armor color
								for (int k = 0x15; k < 0x1D; k++)
								{
									int l;
									if (k == 0x15 + 3)
										//shield emblem
										l = (int)eInventorySlot.LeftHandWeapon;
									else
										l = k;

									found = 0;
									foreach (DbInventoryItem item in items)
									{
										if (item.SlotPosition == l && found == 0)
										{
											if (item.Emblem != 0)
												pak.WriteShortLowEndian((ushort)item.Emblem);
											else
												pak.WriteShortLowEndian((ushort)item.Color);
											found = 1;
										}
									}
									if (found == 0)
										pak.WriteShort(0x00);
								}
								//8 bytes: weapon model
								for (int k = 0x0A; k < 0x0E; k++)
								{
									found = 0;
									foreach (DbInventoryItem item in items)
									{
										if (item.SlotPosition == k && found == 0)
										{
											pak.WriteShortLowEndian((ushort)item.Model);
											found = 1;
										}
									}
									if (found == 0)
										pak.WriteShort(0x00);
								}
								if (characters[j].ActiveWeaponSlot == (byte)eActiveWeaponSlot.TwoHanded)
								{
									pak.WriteByte(0x02);
									pak.WriteByte(0x02);
								}
								else if (characters[j].ActiveWeaponSlot == (byte)eActiveWeaponSlot.Distance)
								{
									pak.WriteByte(0x03);
									pak.WriteByte(0x03);
								}
								else
								{
									byte righthand = 0xFF;
									byte lefthand = 0xFF;
									foreach (DbInventoryItem item in items)
									{
										if (item.SlotPosition == (int)eInventorySlot.RightHandWeapon)
											righthand = 0x00;
										if (item.SlotPosition == (int)eInventorySlot.LeftHandWeapon)
											lefthand = 0x01;
									}
									if (righthand == lefthand)
									{
										if (characters[j].ActiveWeaponSlot == (byte)eActiveWeaponSlot.TwoHanded)
											righthand = lefthand = 0x02;
										else if (characters[j].ActiveWeaponSlot == (byte)eActiveWeaponSlot.Distance)
											righthand = lefthand = 0x03;
									}
									pak.WriteByte(righthand);
									pak.WriteByte(lefthand);
								}
								if (reg == null || reg.Expansion != 1)
								{
									pak.WriteByte(0x00);
								}
								else
								{
									pak.WriteByte(0x01); //0x01=char in ShroudedIsles zone, classic client can't "play"
								}
								pak.WriteByte((byte)characters[j].Constitution);
								pak.Fill(0x0, 4);//new trailing bytes in 1.99
								written = true;
							}
						}
						if (!written)
						{
							pak.Fill(0x0, 188);
						}
					}
				}
				pak.Fill(0x0, 90);

				SendTCP(pak);
			}
		}
	}
}
