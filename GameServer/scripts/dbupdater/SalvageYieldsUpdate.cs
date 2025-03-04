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
using System;
using System.Collections;
using DOL.Database;

namespace DOL.GS.DatabaseUpdate
{
	/// <summary>
	/// Checks and updates the new SalvageYields table
	/// </summary>
	[DatabaseUpdate]
	public class SalvageYieldsUpdate : IDatabaseUpdater
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// If new SalvageYield table is empty then copy values from old Salvage table
		/// </summary>
		public void Update()
		{
			int count = 0;
			var newSalvage = GameServer.Database.SelectAllObjects<DbSalvageYield>();

			if (newSalvage == null || newSalvage.Count == 0)
			{
				log.InfoFormat("Updating the SalvageYield table...", count);

				var oldSalvage = GameServer.Database.SelectAllObjects<DbSalvage>();

				foreach (DbSalvage salvage in oldSalvage)
				{
					DbSalvageYield salvageYield = new DbSalvageYield();
					salvageYield.ID = ++count; // start at 1
					salvageYield.ObjectType = salvage.ObjectType;
					salvageYield.SalvageLevel = salvage.SalvageLevel;
					salvageYield.MaterialId_nb = salvage.Id_nb;
					salvageYield.Count = 0;
					salvageYield.Realm = salvage.Realm;
					salvageYield.PackageID = DbSalvageYield.LEGACY_SALVAGE_ID;
					GameServer.Database.AddObject(salvageYield);
				}
			}

			if (count > 0)
			{
				log.InfoFormat("Copied {0} entries from Salvage to SalvageYield.", count);
			}
		}
	}
}
