﻿/*
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

namespace DOL.GS.PacketHandler
{
    [PacketLib(1107, GameClient.eClientVersion.Version1107)]
    public class PacketLib1107 : PacketLib1106
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Constructs a new PacketLib for Client Version 1.107
        /// Untested stub to allow connects
        /// </summary>
        /// <param name="client">the gameclient this lib is associated with</param>
        public PacketLib1107(GameClient client)
            : base(client)
        {

        }
    }
}
