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
using System.IO;
using System.Reflection;

using DOL.Database;
using DOL.Database.Connection;

using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace DOL.Tests.Integration.Database.MySQL
{
	[SetUpFixture]
	public class MySQLDBSetUp
	{
		public MySQLDBSetUp()
		{
		}

		public static SqlObjectDatabase Database { get; set; }
		public static string ConnectionString { get; set; }

		[OneTimeSetUp]
		public void SetUp()
		{
			var CodeBase = new FileInfo(new Uri(Assembly.GetExecutingAssembly().Location).LocalPath).Directory;
			ConnectionString = "Server=localhost;Port=3306;Database=test_dol_database;User ID=root;Password=;Treat Tiny As Boolean=False";

			Database = (SqlObjectDatabase)ObjectDatabase.GetObjectDatabase(EConnectionType.DATABASE_MYSQL, ConnectionString);

			Console.WriteLine("DB Configured : {0}, {1}", Database.ConnectionType, ConnectionString);
		}
	}
}
