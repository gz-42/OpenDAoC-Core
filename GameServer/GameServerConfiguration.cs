using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using DOL.Config;
using DOL.Database.Connection;

namespace DOL.GS
{
	/// <summary>
	/// This is the game server configuration
	/// </summary>
	public class GameServerConfiguration : BaseServerConfig
	{
		#region Server

		/// <summary>
		/// holds the server root directory
		/// </summary>
		protected string m_rootDirectory;

		/// <summary>
		/// Holds the log configuration file path
		/// </summary>
		protected string m_logConfigFile;

		/// <summary>
		/// Name of the scripts compilation target
		/// </summary>
		protected string m_scriptCompilationTarget;

		/// <summary>
		/// The assemblies to include when compiling the scripts
		/// </summary>
		protected string m_scriptAssemblies;

		/// <summary>
		/// Enable/Disable Startup Script Compilation
		/// </summary>
		protected bool m_enableCompilation;

		/// <summary>
		/// True if the server shall automatically create accounts
		/// </summary>
		protected bool m_autoAccountCreation;

		/// <summary>
		/// The game server type
		/// </summary>
		protected EGameServerType m_serverType;

		/// <summary>
		/// The game server name
		/// </summary>
		protected string m_ServerName;

		/// <summary>
		/// The short server name, shown in /loc command
		/// </summary>
		protected string m_ServerNameShort;

		/// <summary>
		/// The max client count.
		/// </summary>
		protected int m_maxClientCount;

		/// <summary>
		/// The endpoint to send UDP packets from.
		/// </summary>
		protected IPEndPoint m_udpOutEndpoint;

        /// <summary>
        /// Whether collecting of Metrics is enbaled or not
        /// </summary>
        public bool MetricsEnabled { get; protected set; } = false;

        /// <summary>
        /// The interval in which Metrics are calculted/exported
        /// </summary>
        public int MetricsExportInterval { get; protected set; } = 0;

        /// <summary>
        /// The OpenTelemetry Protocol Endpoint to push the metrics to
        /// </summary>
        public Uri OtlpEndpoint { get; protected set; }

		#endregion
		#region Logging
		/// <summary>
		/// The logger name where to log the gm+ commandos
		/// </summary>
		protected string m_gmActionsLoggerName;

		/// <summary>
		/// The logger name where to log cheat attempts
		/// </summary>
		protected string m_cheatLoggerName;

		/// <summary>
		/// The logger name where to log duplicate IP connections
		/// </summary>
		protected string m_dualIPLoggerName;

		/// <summary>
		/// The file name of the invalid names file
		/// </summary>
		protected string m_invalidNamesFile = string.Empty;

		#endregion
		#region Database

		/// <summary>
		/// The path to the XML database folder
		/// </summary>
		protected string m_dbConnectionString;

		/// <summary>
		/// Type database type
		/// </summary>
		protected EConnectionType m_dbType;

		/// <summary>
		/// True if the server shall autosave the db
		/// </summary>
		protected bool m_autoSave;

		/// <summary>
		/// The auto save interval in minutes
		/// </summary>
		protected int m_saveInterval;

		#endregion
		#region Load/Save

        /// <summary>
        /// Loads the config values from a specific config element
        /// </summary>
        /// <param name="root">the root config element</param>
        protected override void LoadFromConfig(ConfigElement root)
        {
            base.LoadFromConfig(root);

            // Removed to not confuse users
//			m_rootDirectory = root["Server"]["RootDirectory"].GetString(m_rootDirectory);

            m_logConfigFile = root["Server"]["LogConfigFile"].GetString(m_logConfigFile);

            m_scriptCompilationTarget = root["Server"]["ScriptCompilationTarget"].GetString(m_scriptCompilationTarget);
            m_scriptAssemblies = root["Server"]["ScriptAssemblies"].GetString(m_scriptAssemblies);
            m_enableCompilation = root["Server"]["EnableCompilation"].GetBoolean(true);
            m_autoAccountCreation = root["Server"]["AutoAccountCreation"].GetBoolean(m_autoAccountCreation);

            string serverType = root["Server"]["GameType"].GetString("Normal");
            switch (serverType.ToLower())
            {
                case "normal":
                    m_serverType = EGameServerType.GST_Normal;
                    break;
                case "casual":
                    m_serverType = EGameServerType.GST_Casual;
                    break;
                case "roleplay":
                    m_serverType = EGameServerType.GST_Roleplay;
                    break;
                case "pve":
                    m_serverType = EGameServerType.GST_PvE;
                    break;
                case "pvp":
                    m_serverType = EGameServerType.GST_PvP;
                    break;
                case "test":
                    m_serverType = EGameServerType.GST_Test;
                    break;
                default:
                    m_serverType = EGameServerType.GST_Normal;
                    break;
            }

            m_ServerName = root["Server"]["ServerName"].GetString(m_ServerName);
            m_ServerNameShort = root["Server"]["ServerNameShort"].GetString(m_ServerNameShort);
            m_dualIPLoggerName = root["Server"]["DualIPLoggerName"].GetString(m_dualIPLoggerName);
            m_cheatLoggerName = root["Server"]["CheatLoggerName"].GetString(m_cheatLoggerName);
            m_gmActionsLoggerName = root["Server"]["GMActionLoggerName"].GetString(m_gmActionsLoggerName);
            m_invalidNamesFile = root["Server"]["InvalidNamesFile"].GetString(m_invalidNamesFile);

            string db = root["Server"]["DBType"].GetString("XML");
            switch (db.ToLower())
            {
                case "xml":
                    m_dbType = EConnectionType.DATABASE_XML;
                    break;
                case "mysql":
                    m_dbType = EConnectionType.DATABASE_MYSQL;
                    break;
                case "sqlite":
                    m_dbType = EConnectionType.DATABASE_SQLITE;
                    break;
                case "mssql":
                    m_dbType = EConnectionType.DATABASE_MSSQL;
                    break;
                case "odbc":
                    m_dbType = EConnectionType.DATABASE_ODBC;
                    break;
                case "oledb":
                    m_dbType = EConnectionType.DATABASE_OLEDB;
                    break;
                default:
                    m_dbType = EConnectionType.DATABASE_XML;
                    break;
            }

            m_dbConnectionString = root["Server"]["DBConnectionString"].GetString(m_dbConnectionString);
            m_autoSave = root["Server"]["DBAutosave"].GetBoolean(m_autoSave);
            m_saveInterval = root["Server"]["DBAutosaveInterval"].GetInt(m_saveInterval);

            // Parse UDP out endpoint
            IPAddress address = null;
            int port = -1;
            string addressStr = root["Server"]["UDPOutIP"].GetString(string.Empty);
            string portStr = root["Server"]["UDPOutPort"].GetString(string.Empty);
            if (IPAddress.TryParse(addressStr, out address)
                && int.TryParse(portStr, out port)
                && IPEndPoint.MaxPort >= port
                && IPEndPoint.MinPort <= port)
            {
                m_udpOutEndpoint = new IPEndPoint(address, port);
            }

            this.MetricsEnabled = root["Server"]["MetricsEnabled"].GetBoolean(false);
            this.OtlpEndpoint = new Uri(root["Server"]["OtlpEndpoint"].GetString("http://localhost:4317/"));
            string durationFromConfig = root["Server"]["MetricsExportInterval"].GetString("60s");
            this.MetricsExportInterval = ParseDurationToMilliseconds(durationFromConfig);
        }

        /// <summary>
        /// Parse a string like 10s, 2m or 4h to
        /// </summary>
        /// <param name="durationString">The string containing the value</param>
        /// <returns>The duartion in milliseconds</returns>
        /// <exception cref="FormatException"></exception>
        private static int ParseDurationToMilliseconds(string durationString)
        {
            string pattern = @"^(\d+)([smh])$"; // Matches digits followed by s, m, or h
            Match match = Regex.Match(durationString, pattern, RegexOptions.IgnoreCase);

            if (!match.Success) throw new FormatException("Invalid duration format.");

            if (!int.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out int value)) // Parse with InvariantCulture
                throw new FormatException("Invalid numeric value.");

            char unit = char.ToLower(match.Groups[2].Value[0]);

            TimeSpan timeSpan = unit switch
            {
                's' => TimeSpan.FromSeconds(value),
                'm' => TimeSpan.FromMinutes(value),
                'h' => TimeSpan.FromHours(value),
                _ => TimeSpan.FromSeconds(value)
            };

            return (int)timeSpan.TotalMilliseconds;
        }

        /// <summary>
		/// Saves the values into a specific config element
		/// </summary>
		/// <param name="root">the root config element</param>
		protected override void SaveToConfig(ConfigElement root)
		{
			base.SaveToConfig(root);
			root["Server"]["ServerName"].Set(m_ServerName);
			root["Server"]["ServerNameShort"].Set(m_ServerNameShort);
			// Removed to not confuse users
//			root["Server"]["RootDirectory"].Set(m_rootDirectory);
			root["Server"]["LogConfigFile"].Set(m_logConfigFile);

			root["Server"]["ScriptCompilationTarget"].Set(m_scriptCompilationTarget);
			root["Server"]["ScriptAssemblies"].Set(m_scriptAssemblies);
			root["Server"]["EnableCompilation"].Set(m_enableCompilation);
			root["Server"]["AutoAccountCreation"].Set(m_autoAccountCreation);

			string serverType = "Normal";

			switch (m_serverType)
			{
				case EGameServerType.GST_Normal:
					serverType = "Normal";
					break;
				case EGameServerType.GST_Casual:
					serverType = "Casual";
					break;
				case EGameServerType.GST_Roleplay:
					serverType = "Roleplay";
					break;
				case EGameServerType.GST_PvE:
					serverType = "PvE";
					break;
				case EGameServerType.GST_PvP:
					serverType = "PvP";
					break;
				case EGameServerType.GST_Test:
					serverType = "Test";
					break;
				default:
					serverType = "Normal";
					break;
			}
			root["Server"]["GameType"].Set(serverType);

			root["Server"]["CheatLoggerName"].Set(m_cheatLoggerName);
			root["Server"]["DualIPLoggerName"].Set(m_dualIPLoggerName);
			root["Server"]["GMActionLoggerName"].Set(m_gmActionsLoggerName);
			root["Server"]["InvalidNamesFile"].Set(m_invalidNamesFile);

			string db = "XML";

			switch (m_dbType)
			{
			case EConnectionType.DATABASE_XML:
				db = "XML";
					break;
			case EConnectionType.DATABASE_MYSQL:
				db = "MYSQL";
					break;
			case EConnectionType.DATABASE_SQLITE:
				db = "SQLITE";
					break;
			case EConnectionType.DATABASE_MSSQL:
				db = "MSSQL";
					break;
			case EConnectionType.DATABASE_ODBC:
				db = "ODBC";
					break;
			case EConnectionType.DATABASE_OLEDB:
				db = "OLEDB";
					break;
				default:
					m_dbType = EConnectionType.DATABASE_XML;
					break;
			}
			root["Server"]["DBType"].Set(db);
			root["Server"]["DBConnectionString"].Set(m_dbConnectionString);
			root["Server"]["DBAutosave"].Set(m_autoSave);
			root["Server"]["DBAutosaveInterval"].Set(m_saveInterval);

			// Store UDP out endpoint
			if (m_udpOutEndpoint != null)
			{
				root["Server"]["UDPOutIP"].Set(m_udpOutEndpoint.Address.ToString());
				root["Server"]["UDPOutPort"].Set(m_udpOutEndpoint.Port.ToString());
			}
		}
		#endregion
		#region Constructors
		/// <summary>
		/// Constructs a server configuration with default values
		/// </summary>
		public GameServerConfiguration() : base()
		{
			m_ServerName = "Dawn Of Light";
			m_ServerNameShort = "DOLSERVER";

			if (Assembly.GetEntryAssembly() != null)
				m_rootDirectory = new FileInfo(Assembly.GetEntryAssembly().Location).DirectoryName;
			else
				m_rootDirectory = new FileInfo(Assembly.GetAssembly(typeof(GameServer)).Location).DirectoryName;

			m_logConfigFile = Path.Combine(Path.Combine(".", "config"), "logconfig.xml");

			m_scriptCompilationTarget = Path.Combine(Path.Combine(".", "lib"), "GameServerScripts.dll");
			m_scriptAssemblies = " ";
			m_enableCompilation = true;
			m_autoAccountCreation = true;
			m_serverType = EGameServerType.GST_Normal;

			m_cheatLoggerName = "cheats";
			m_dualIPLoggerName = "dualip";
			m_gmActionsLoggerName = "gmactions";
		    InventoryLoggerName = "inventories";
		    m_invalidNamesFile = Path.Combine(Path.Combine(".", "config"), "invalidnames.txt");

			m_dbType = EConnectionType.DATABASE_SQLITE;
			m_dbConnectionString = string.Format("Data Source={0};Version=3;Pooling=False;Cache Size=1073741824;Journal Mode=Off;Synchronous=Off;Foreign Keys=True;Default Timeout=60",
			                                     Path.Combine(m_rootDirectory, "dol.sqlite3.db"));
			m_autoSave = true;
			m_saveInterval = 10;
        }

		#endregion

		/// <summary>
		/// Gets or sets the root directory of the server
		/// </summary>
		public string RootDirectory
		{
			get { return m_rootDirectory; }
			set { m_rootDirectory = value; }
		}

		/// <summary>
		/// Gets or sets the log configuration file of this server
		/// </summary>
		public string LogConfigFile
		{
			get
			{
				if(Path.IsPathRooted(m_logConfigFile))
					return m_logConfigFile;
				else
					return Path.Combine(m_rootDirectory, m_logConfigFile);
			}
			set { m_logConfigFile = value; }
		}

		/// <summary>
		/// Gets or sets the script compilation target
		/// </summary>
		public string ScriptCompilationTarget
		{
			get { return m_scriptCompilationTarget; }
			set { m_scriptCompilationTarget = value; }
		}

		/// <summary>
		/// Gets or sets the script assemblies to be included in the script compilation
		/// </summary>
		[Obsolete("ScriptAssemblies is going to be removed.")]
		public string[] ScriptAssemblies
		{
			get
			{
				return new string[] { "System.dll", "System.Xml.dll" }
					.Union(new DirectoryInfo(Path.Combine(RootDirectory, "lib"))
					.EnumerateFiles("*.dll", SearchOption.TopDirectoryOnly)
					.Select(f => f.Name).Where(f => !f.Equals(new FileInfo(ScriptCompilationTarget).Name, StringComparison.OrdinalIgnoreCase)))
					.Union(AdditionalScriptAssemblies)
					.ToArray();
			}
		}

		public string[] AdditionalScriptAssemblies => string.IsNullOrEmpty(m_scriptAssemblies.Trim()) ? new string[] { } : m_scriptAssemblies.Split(',');

		/// <summary>
		/// Get or Set the Compilation Flag
		/// </summary>
		public bool EnableCompilation
		{
			get { return m_enableCompilation; }
			set { m_enableCompilation = value; }
		}

		/// <summary>
		/// Gets or sets the auto account creation flag
		/// </summary>
		public bool AutoAccountCreation
		{
			get { return m_autoAccountCreation; }
			set { m_autoAccountCreation = value; }
		}

		/// <summary>
		/// Gets or sets the server type
		/// </summary>
		public EGameServerType ServerType
		{
			get { return m_serverType; }
			set { m_serverType = value; }
		}

		/// <summary>
		/// Gets or sets the server name
		/// </summary>
		public string ServerName
		{
			get { return m_ServerName; }
			set { m_ServerName = value; }
		}

		/// <summary>
		/// Gets or sets the short server name
		/// </summary>
		public string ServerNameShort
		{
			get { return m_ServerNameShort; }
			set { m_ServerNameShort = value; }
		}

		/// <summary>
		/// Gets or sets the GM action logger name
		/// </summary>
		public string GMActionsLoggerName
		{
			get { return m_gmActionsLoggerName; }
			set { m_gmActionsLoggerName = value; }
		}

		/// <summary>
		/// Gets or sets the cheat logger name
		/// </summary>
		public string CheatLoggerName
		{
			get { return m_cheatLoggerName; }
			set { m_cheatLoggerName = value; }
		}

		/// <summary>
		/// Gets or sets the cheat logger name
		/// </summary>
		public string DualIPLoggerName
		{
			get { return m_dualIPLoggerName; }
			set { m_dualIPLoggerName = value; }
		}

		/// <summary>
		/// Gets or sets the trade logger name
		/// </summary>
		public string InventoryLoggerName { get; set; }

		/// <summary>
		/// Gets or sets the invalid name filename
		/// </summary>
		public string InvalidNamesFile
		{
			get
			{
				if(Path.IsPathRooted(m_invalidNamesFile))
					return m_invalidNamesFile;
				else
					return Path.Combine(m_rootDirectory, m_invalidNamesFile);
			}
			set { m_invalidNamesFile = value; }
		}

		/// <summary>
		/// Gets or sets the xml database path
		/// </summary>
		public string DBConnectionString
		{
			get { return m_dbConnectionString; }
			set { m_dbConnectionString = value; }
		}

		/// <summary>
		/// Gets or sets the DB type
		/// </summary>
		public EConnectionType DBType
		{
			get { return m_dbType; }
			set { m_dbType = value; }
		}

		/// <summary>
		/// Gets or sets the autosave flag
		/// </summary>
		public bool AutoSave
		{
			get { return m_autoSave; }
			set { m_autoSave = value; }
		}

		/// <summary>
		/// Gets or sets the autosave interval
		/// </summary>
		public int SaveInterval
		{
			get { return m_saveInterval; }
			set { m_saveInterval = value; }
		}
	}
}
