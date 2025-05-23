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

using DOL.Database;

namespace DOL.GS.DatabaseUpdate
{
    /// <summary>
    /// Checks and updates the ServerProperty table.
    /// </summary>
    [DatabaseUpdate]
    public class ServerPropertiesUpdate : IDatabaseUpdater
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void Update()
        {
            RemoveACLKUNLS();
        }

        #region RemoveACLKUNLS
        /// <summary>
        /// Removes the no longer used 'allowed_custom_language_keys' and 'use_new_language_system' entries.
        /// </summary>
        private void RemoveACLKUNLS()
        {
            log.Info("Updating the ServerProperty table...");

            var properties = GameServer.Database.SelectAllObjects<DbServerProperty>();

            bool aclkFound = false;
            bool unlsFound = false;
            foreach (DbServerProperty property in properties)
            {
                if (property.Key != "allowed_custom_language_keys" && property.Key != "use_new_language_system")
                    continue;

                if (property.Key == "allowed_custom_language_keys")
                    aclkFound = true;

                if (property.Key == "use_new_language_system")
                    unlsFound = true;

                GameServer.Database.DeleteObject(property);

                if (aclkFound && unlsFound)
                    break;
            }

            log.Info("ServerProperty table update complete!");
        }
        #endregion RemoveACLKUNLS
    }
}