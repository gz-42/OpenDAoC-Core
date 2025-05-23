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

using System.Reflection;
using DOL.GS.ServerProperties;
using DOL.Network;

namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.ClientCrash, "Handles client crash packets", eClientStatus.None)]
    public class ClientCrashPacketHandler : IPacketHandler
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            string dllName = packet.ReadString(16);
            packet.Position = 0x50;
            uint upTime = packet.ReadInt();
            string text = $"Client crash ({client}) dll:{dllName} clientUptime:{upTime}sec";

            if (log.IsInfoEnabled)
                log.Info(text);

            if (log.IsDebugEnabled)
            {
                if (Properties.SAVE_PACKETS)
                {
                    log.Debug("Last client sent/received packets (from older to newer):");

                    foreach (IPacket prevPak in client.PacketProcessor.GetLastPackets())
                        log.Debug(prevPak.ToHumanReadable());
                }
                else
                    log.Debug($"Enable the server property {nameof(Properties.SAVE_PACKETS)} to see the last few sent/received packets.");
            }

            client.Out.SendPlayerQuit(true);

            if (client.Player != null)
            {
                client.Player.SaveIntoDatabase();
                client.Player.Quit(true);
            }

            client.Disconnect();
        }
    }
}
