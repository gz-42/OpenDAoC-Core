using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.GS.Commands;

namespace DOL.GS.PacketHandler.Client.v168
{
    /// <summary>
    /// Handles spell cast requests from client
    /// </summary>
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.UseSpell, "Handles Player Use Spell Request.", eClientStatus.PlayerInGame)]
    public class UseSpellHandler : AbstractCommandHandler, IPacketHandler
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly Logging.Logger Log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            if (client.Player.ObjectState is not GameObject.eObjectState.Active || client.ClientState is not GameClient.eClientState.Playing)
                return;

            int flagSpeedData;
            int spellLevel;
            int spellLineIndex;

            if (client.Version >= GameClient.eClientVersion.Version1124)
            {
                if (client.Player.IsPositionUpdateFromPacketAllowed())
                {
                    client.Player.X = (int) packet.ReadFloatLowEndian();
                    client.Player.Y = (int) packet.ReadFloatLowEndian();
                    client.Player.Z = (int) packet.ReadFloatLowEndian();
                    client.Player.CurrentSpeed = (short) packet.ReadFloatLowEndian();
                    client.Player.Heading = packet.ReadShort();
                    client.Player.OnPositionUpdateFromPacket();
                }

                flagSpeedData = packet.ReadShort();
                spellLevel = packet.ReadByte();
                spellLineIndex = packet.ReadByte();
                // Two bytes at end, not sure what for.
            }
            else
            {
                flagSpeedData = packet.ReadShort();
                ushort heading = packet.ReadShort();

                if (client.Version > GameClient.eClientVersion.Version171)
                {
                    if (client.Player.IsPositionUpdateFromPacketAllowed())
                    {
                        int xOffsetInZone = packet.ReadShort();
                        int yOffsetInZone = packet.ReadShort();
                        int currentZoneID = packet.ReadShort();
                        int realZ = packet.ReadShort();

                        Zone newZone = WorldMgr.GetZone((ushort) currentZoneID);

                        if (newZone == null)
                            Log.Warn($"Unknown zone in UseSpellHandler: {currentZoneID} player: {client.Player.Name}");
                        else
                        {
                            client.Player.X = newZone.XOffset + xOffsetInZone;
                            client.Player.Y = newZone.YOffset + yOffsetInZone;
                            client.Player.Z = realZ;
                        }

                        client.Player.OnPositionUpdateFromPacket();
                    }
                }

                spellLevel = packet.ReadByte();
                spellLineIndex = packet.ReadByte();
                client.Player.Heading = heading;
            }

            GamePlayer player = client.Player;

            // Commenting out. 'flagSpeedData' doesn't vary with movement speed, and this stops the player for a fraction of a second.
            //if ((flagSpeedData & 0x200) != 0)
            //	player.CurrentSpeed = (short)(-(flagSpeedData & 0x1ff)); // backward movement
            //else
            //	player.CurrentSpeed = (short)(flagSpeedData & 0x1ff); // forward movement

            player.IsStrafing = (flagSpeedData & 0x4000) != 0;
            player.TargetInView = (flagSpeedData & 0xa000) != 0; // why 2 bits? that has to be figured out
            player.GroundTargetInView = (flagSpeedData & 0x1000) != 0;

            GetSkill(player, spellLineIndex, spellLevel, out Skill sk, out SpellLine sl);

            if (sk is Spell spell && sl != null)
                player.CastSpell(spell, sl);
            else if (sk is Styles.Style style)
                player.styleComponent.ExecuteWeaponStyle(style);
            else if (sk is Ability ability)
                player.castingComponent.RequestUseAbility(ability);
            else
            {
                if (Log.IsWarnEnabled)
                    Log.Warn($"Client <{player.Client.Account.Name}> requested incorrect spell at level {spellLevel} in spell-line {(sl == null || sl.Name == null ? "unknown" : sl.Name)}");

                player.Out.SendMessage($"Error : Spell (Line {spellLineIndex}, Level {spellLevel}) can't be resolved...", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
            }
        }

        private static void GetSkill(GamePlayer player, int spellLineIndex, int spellLevel, out Skill sk, out SpellLine sl)
        {
            sk = null;
            sl = null;

            List<Tuple<SpellLine, List<Skill>>> snap = player.GetAllUsableListSpells();

            if (spellLineIndex >= snap.Count)
                return;

            int index = -1;
            List<Skill> skills = snap[spellLineIndex].Item2;

            for (int i = 0; i < skills.Count; i++)
            {
                Skill skill = skills[i];

                if (skill is Spell spell && spell.Level == spellLevel)
                {
                    index = i;
                    break;
                }

                if (skill is Styles.Style style && style.SpecLevelRequirement == spellLevel)
                {
                    index = i;
                    break;
                }

                if (skill is Ability ability && ability.SpecLevelRequirement == spellLevel)
                {
                    index = i;
                    break;
                }
            }

            if (index > -1)
                sk = snap[spellLineIndex].Item2[index];

            sl = snap[spellLineIndex].Item1;
        }
    }
}
