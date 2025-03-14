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
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Linq;

using DOL.Database;
using DOL.GS.DatabaseUpdate;
using DOL.GS.ServerProperties;


namespace DOL.GS.DatabaseUpdate
{
	/// <summary>
	/// Automated Database Updater target at XML Package File
	/// Will read for folder "insert" or "replace" inside Game Server Scripts to track File Package
	/// Will try to load accordingly at Server Startup (insert as insert ignore, replace to override data)
	/// </summary>
    [DatabaseUpdate]
	public class AutoXMLDatabaseUpdate : IDatabaseUpdater
	{
		#region ServerProperties
		/// <summary>
		/// Enable or Disable the Auto XML Database Update Script
		/// </summary>
		[ServerProperty("xmlautoload", "xml_autoload_update_enable", "Enable or disable Auto XML Dataase Update Packages (Should be enabled for first run...)", true)]
		public static bool XML_AUTOLOAD_UPDATE_ENABLE;

		/// <summary>
		/// Set Default Path for Loading "Insert" XML Package Directory
		/// </summary>
		[ServerProperty("xmlautoload", "xml_load_insert_directory", "Enforce directory path where the XML Insert Packages are Loaded To Database (Relative to Scripts or Absolute...)", "dbupdater/insert")]
		public static string XML_LOAD_INSERT_DIRECTORY;

		/// <summary>
		/// Set Default Path for Loading "Replace" XML Package Directory
		/// </summary>
		[ServerProperty("xmlautoload", "xml_load_replace_directory", "Enforce directory path to where the XML Replace Packages are Loaded To Database (Relative to Scripts or Absolute...)", "dbupdater/replace")]
		public static string XML_LOAD_REPLACE_DIRECTORY;
		#endregion
		
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
		
		/// <summary>
		/// Entry point for Database Update Request on Server Start
		/// </summary>
		public void Update()
		{
			if (!XML_AUTOLOAD_UPDATE_ENABLE)
			{
				if (log.IsInfoEnabled)
					log.Info("Auto XML Database Update Disabled! Will not Load any XML Packages...");
				return;
			}
			
			// List All available Files
			if (log.IsInfoEnabled)
				log.Info("Loading Auto XML Database Updates... (this can take a few minutes)");
			try
			{
				var insertdir = Path.IsPathRooted(XML_LOAD_INSERT_DIRECTORY) ? XML_LOAD_INSERT_DIRECTORY : string.Format("{0}{1}scripts{1}{2}", GameServer.Instance.Configuration.RootDirectory, Path.DirectorySeparatorChar, XML_LOAD_INSERT_DIRECTORY);
					
				if (!Directory.Exists(insertdir))
					Directory.CreateDirectory(insertdir);
				
				var replacedir = Path.IsPathRooted(XML_LOAD_REPLACE_DIRECTORY) ? XML_LOAD_REPLACE_DIRECTORY : string.Format("{0}{1}scripts{1}{2}", GameServer.Instance.Configuration.RootDirectory, Path.DirectorySeparatorChar, XML_LOAD_REPLACE_DIRECTORY);
					
				if (!Directory.Exists(replacedir))
					Directory.CreateDirectory(replacedir);

				// Records of Played Script
				var records = GameServer.Database.SelectAllObjects<AutoXMLUpdateRecord>();
				
				// INSERTS
				ApplyXMLChangeForDirectory(records, insertdir, false);
				// REPLACES
				ApplyXMLChangeForDirectory(records, replacedir, true);
				
			}
			catch (Exception e)
			{
				if (log.IsErrorEnabled)
					log.ErrorFormat("Error While loading Auto XML Update Packages: {0}", e);
			}
		}
		
		/// <summary>
		/// Apply All Change For Directory According to Database existing update records
		/// </summary>
		/// <param name="records">All Database Object for XML Updates</param>
		/// <param name="directory">Directory to read from</param>
		/// <param name="replace">Replace mode if True</param>
		private void ApplyXMLChangeForDirectory(IEnumerable<AutoXMLUpdateRecord> records, string directory, bool replace)
		{
			var inserts = GetXMLPackagesForUpdate(directory);
			
			// List all file, 
			foreach (var entry in inserts)
			{
				var relativeID =  GetRelativePath(entry.Key.FullName, new DirectoryInfo(directory).Parent.FullName);
				
				var rec = GetAutoXMLUpdateRecordFromCollection(records, relativeID, entry.Value);
				var wasNull = rec.FileHash == null;
				
				// New or Outdated File - need to be applied !				
				if (wasNull || !rec.FileHash.Equals(entry.Value))
				{
					if (CheckXMLPackageAndApply(entry.Key, replace))
					{
						rec.LoadResult = "SUCCESS";
					}
					else
					{
						rec.LoadResult = "FAILURE";
					}
					
					rec.FileHash = entry.Value;
					if (wasNull)
					{
						GameServer.Database.AddObject(rec);
					}
					else
					{
						GameServer.Database.SaveObject(rec);
					}
				}
			}
		}
		
		/// <summary>
		/// Retrieve Files in XML repository with according hashfile
		/// </summary>
		/// <param name="rootPath"></param>
		/// <returns></returns>
		private IDictionary<FileInfo, string> GetXMLPackagesForUpdate(string rootPath)
		{
			var result = new Dictionary<FileInfo, string>();
			try
			{
				var pathInsert = new DirectoryInfo(rootPath);
				
				foreach(var fi in pathInsert.GetFiles("*.xml", SearchOption.AllDirectories))
				{
					string hashStr = null;
					using (var stream = fi.OpenRead())
				    {
				        var sha = new SHA256Managed();
				        var hash = sha.ComputeHash(stream);
				        hashStr = BitConverter.ToString(hash).Replace("-", String.Empty);
				    }
					result.Add(fi, hashStr);
				}
			}
			catch (Exception e)
			{
				if (log.IsErrorEnabled)
					log.ErrorFormat("Error While retrieving Auto XML Package in path {1} - {0}", e, rootPath);
			}
			
			return result;
		}
		
		/// <summary>
		/// Check the XML Package Given for Replace or Insert Apply
		/// </summary>
		/// <param name="xml">FileInfo for XML Package</param>
		/// <param name="replace">Enforce Replace Mode</param>
		/// <returns>True if success, False if any errors</returns>
		private bool CheckXMLPackageAndApply(FileInfo xml, bool replace)
		{
			
			var packageName = string.Format("{0}{1}{2}", xml.Directory.Name, Path.DirectorySeparatorChar, xml.Name);
			
			if (log.IsInfoEnabled)
				log.InfoFormat("Auto Loading XML File {0} into Database (Mode:{1})", packageName, replace ? "Replace" : "Insert");
			
			var result = true;
			
			try
			{
				//Load the XML File
				var xmlTable = LoaderUnloaderXML.LoadXMLTableFromFile(xml);
				if (xmlTable.Length > 0)
				{
					// Guess Object Type
					var xmlType = xmlTable.First().GetType();
					var tableHandler = new DataTableHandler(xmlType);
					
					// Find unique Fields
					var uniqueMember = DatabaseUtil.GetUniqueMembers(xmlType);
					
					// Get all object "Method" Through Reflection
					var classMethod = GameServer.Database.GetType().GetMethod("SelectAllObjects", Type.EmptyTypes);
					var genericMethod = classMethod.MakeGenericMethod(xmlType);
					var existingObjects = ((IEnumerable)genericMethod.Invoke(GameServer.Database, new object[]{})).Cast<DataObject>().ToArray();
					
					// Store Object to Alter
					var toDelete = new ConcurrentBag<DataObject>();
					var toAdd = new ConcurrentBag<DataObject>();
					
					// Check if an Object already exists
					xmlTable.AsParallel().ForAll(obj => {
					                             	// Check if Exists Compare Unique and Non-Generated Primary Keys
					                             	var exists = existingObjects
					                             		.FirstOrDefault(entry => uniqueMember
					                             		                .Any(constraint => constraint
					                             		                     .All(bind => bind.ValueType == typeof(string)
					                             		                          ? bind.GetValue(entry).ToString().Equals(bind.GetValue(obj).ToString(), StringComparison.OrdinalIgnoreCase)
					                             		                          : bind.GetValue(entry) == bind.GetValue(obj)))
					                             		               );
					                             	
					                             	if (exists != null)
					                             	{
					                             		if (replace)
					                             		{
					                             			// Delete First
					                             			toDelete.Add(exists);
					                             			toAdd.Add(obj);
					                             		}
					                             		// Silently ignore duplicate inserts only...
					                             	}
					                             	else
					                             	{
					                             		toAdd.Add(obj);
					                             	}
					                             });
					// Delete First
					foreach (var obj in toDelete)
						obj.AllowDelete = true;
					
					GameServer.Database.DeleteObject(toDelete);
					
					// Then Insert
					var previousAllowAdd = toAdd.Select(obj => obj.AllowAdd).ToArray();
					foreach (var obj in toAdd)
						obj.AllowAdd = true;
					
					GameServer.Database.AddObject(toAdd);
					
					// Reset Allow Add Flag
					var current = 0;
					foreach (var obj in toAdd)
					{
						obj.AllowAdd = previousAllowAdd[current];
						current++;
					}
				}
				else
				{
					if (log.IsWarnEnabled)
						log.WarnFormat("XML Package {0} Found Empty, may be a parsing Error...", packageName);
					result = false;
				}
			}
			catch (Exception e)
			{
				if (log.IsErrorEnabled)
					log.ErrorFormat("Error While Loading XML Package {0} into Database (Mode:{1}) - {2}", packageName, replace ? "Replace" : "Insert", e);
				result = false;
			}
			
			return result;
		}
		
		/// <summary>
		/// Retrieve a relative Path from a given file and Directory
		/// </summary>
		/// <param name="filespec"></param>
		/// <param name="folder"></param>
		/// <returns></returns>
		private string GetRelativePath(string filespec, string folder)
		{
			var pathUri = new Uri(filespec);
			
			// Folders must end in a slash
			if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.CurrentCulture))
			{
				folder += Path.DirectorySeparatorChar;
			}
			
			var folderUri = new Uri(folder);
			return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString());
		}
		
		/// <summary>
		/// Return an existing Record for XML Package or a New one which can be added to db.
		/// </summary>
		/// <param name="records">Database Collection of Objects</param>
		/// <param name="relativeID">Relative Path Used as File Name ID</param>
		/// <param name="hash">SHA256 Hash to check for updates</param>
		/// <returns></returns>
		private AutoXMLUpdateRecord GetAutoXMLUpdateRecordFromCollection(IEnumerable<AutoXMLUpdateRecord> records, string relativeID, string hash)
		{
			var previous = records.FirstOrDefault(r => r.FilePackage.Equals(relativeID));
			
			if (previous == null)
			{
				previous = new AutoXMLUpdateRecord();
				previous.FilePackage = relativeID;
				previous.AllowAdd = true;
			}
			
			return previous;
		}
	}
}
