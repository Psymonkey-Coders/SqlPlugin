using System;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

using MySql.Data.MySqlClient;

using SEModAPIExtensions.API.Plugin;
using SEModAPIInternal.Support;

namespace SqlPlugin
{
	public class Core : PluginBase
	{
		#region "Config File Classes"

		[Serializable()]
		public class DatabaseSetup
		{
			public string DatabaseName { get; set; }
			public string DatabaseHost { get; set; }
			public string DatabasePort { get; set; }
			public string DatabaseUser { get; set; }
			public string DatabasePass { get; set; }
		}

		[Serializable()]
		public class SQLPluginConfig
		{

			private DatabaseSetup databaseSetup = new DatabaseSetup();
			public DatabaseSetup DatabaseSetup { get { return databaseSetup; } set { databaseSetup = value; } }

			public bool ConnectOnStartup { get; set; }
			public bool ReconnectOnBroken { get; set; }
			public bool DatabaseEnabled { get; set; }
			public bool DatabaseLocked { get; set; }
			public bool IsDebugging { get; set; }

			public int ReconnectAttemptLimit { get; set; }
			public int SqlTickRate { get; set; }
			public int DatabaseTickRate { get; set; }

			public SQLPluginConfig() { }
		}

		#endregion

		#region "Attributes"

		//private static string m_firstRun;

		private static byte[] entropy = Encoding.Unicode.GetBytes("SQL Is The Way To Go Bro");

		private static bool m_databaseSettingsChanged;
		private static bool m_connectedToDatabase;

		private static int m_reconnectAttemptCount;

		private static string m_dataFile;

		private MySqlConnection connection;

		private SQLPluginConfig Config;

		private XmlSerializer Serializer;

		#endregion

		#region "Constructors and Initializers

		// Called when the server first launches
		public Core()
		{
			Console.WriteLine("SQL Plugin '" + Id.ToString() + "' constructed!");

			// Get the current path of the DLL.
			string m_assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			m_dataFile = Path.Combine(m_assemblyFolder, "SQLPlugin_Settings.xml");

			Config = new SQLPluginConfig();
			Serializer = new XmlSerializer(typeof(SQLPluginConfig));
			connection = new MySqlConnection(); 
			try
			{
				if (!File.Exists(m_dataFile))
				{
					// default values
					Config.DatabaseSetup.DatabaseName = "";
					Config.DatabaseSetup.DatabaseHost = "localhost";
					Config.DatabaseSetup.DatabasePort = "1337";
					Config.DatabaseSetup.DatabaseUser = "";
					Config.DatabaseSetup.DatabasePass = "";
					Config.SqlTickRate = 100;
					Config.DatabaseTickRate = 100;

					Config.DatabaseEnabled = true;
					Config.DatabaseLocked = false;

					Config.ReconnectAttemptLimit = 5;

					Config.ConnectOnStartup = false;

					this.SaveXMLConfig();
				}
				else
				{
					FileStream readFileStream = new FileStream(m_dataFile, FileMode.Open, FileAccess.Read, FileShare.Read);
					Config = (SQLPluginConfig)Serializer.Deserialize(readFileStream);
					readFileStream.Close();
					Console.WriteLine("SQL Plugin - Loaded Config");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("SQL Plugin - Exception Message (check log for details): " + ex.Message);
				LogManager.GameLog.WriteLine("SQL Plugin - Exception: " + ex.ToString() + "\n<<<<<END SQL EXCEPTION>>>>>\n\n");
			}

			m_connectedToDatabase = false;
			m_reconnectAttemptCount = 0;
		}

		// Called when the server finishes loading
		public override void Init()
		{
			connection.ConnectionString = String.Format("SERVER={0};DATABASE={1};PORT={2};UID={3};PASSWORD={4};",
				this.DatabaseHost, this.DatabaseName, this.DatabasePort, this.DatabaseUser, this.DatabasePass);

			connection.StateChange += connection_StateChange;

			Console.WriteLine("SQL Plugin '" + Id.ToString() + "' initialized!");

			Console.WriteLine("SQL Plugin - Ready to connect to database");

			if(ConnectOnStartup)
				this.ConnectToDatabase();	
		}

		#endregion

		#region "Properties"

		[Browsable(false)]
		public bool Changed
		{
			get;
			private set;
		}

		[Category("Connection Setup")]
		[Description("The IP/Domain used to connect to SQL host ")]
		[Browsable(true)]
		[ReadOnly(false)]
		[DisplayName("Database Host")]
		public string DatabaseHost
		{
			get { return Config.DatabaseSetup.DatabaseHost; }
			set { Config.DatabaseSetup.DatabaseHost = value; Changed = true; }
		}

		[Category("Connection Setup")]
		[Description("The Port used to connect to SQL host ")]
		[Browsable(true)]
		[ReadOnly(false)]
		[DisplayName("Database Port")]
		public string DatabasePort
		{
			get { return Config.DatabaseSetup.DatabasePort; }
			set { Config.DatabaseSetup.DatabasePort = value; Changed = true; }
		}

		[Category("Connection Setup")]
		[Description("The user used to connect to SQL host ")]
		[Browsable(true)]
		[ReadOnly(false)]
		[DisplayName("Database User")]
		public string DatabaseUser
		{
			get { return Config.DatabaseSetup.DatabaseUser; }
			set { Config.DatabaseSetup.DatabaseUser = value; Changed = true; }
		}

		[Category("Connection Setup")]
		[Description("The password used to connect to SQL host")]
		[Browsable(true)]
		[ReadOnly(false)]
		[DisplayName("Database Password")]
		public string DatabasePass
		{
			get { return ToInsecureString(DecryptString(Config.DatabaseSetup.DatabasePass)); }
			set { Config.DatabaseSetup.DatabasePass = EncryptString(ToSecureString(value)); Changed = true; }		
		}

		[Category("Connection Setup")]
		[Description("The name of the SQL database you want to connect to")]
		[Browsable(true)]
		[ReadOnly(false)]
		[DisplayName("Database Name")]
		public string DatabaseName
		{
			get { return Config.DatabaseSetup.DatabaseName; }
			set { Config.DatabaseSetup.DatabaseName = value; Changed = true; }	
		}

		[Category("Connection Setup")]
		[Description("Set to true to attempt a connect. \n Will change to false if the connection failed/closed")]
		[Browsable(true)]
		[ReadOnly(false)]
		[DisplayName("Connected To DB")]
		public bool ConnectedDatabase
		{
			get { return m_connectedToDatabase; }
			set { this.ConnectToDatabase(); }
		}

		[Category("Database Options")]
		[Description("Reconnect to the database if the connection is broken or not connected.\n Useless until saving is implemented")]
		[Browsable(true)]
		[ReadOnly(false)]
		[DisplayName("Connect on startup")]
		public bool ConnectOnStartup
		{
			get { return Config.ConnectOnStartup; }
			set { Config.ConnectOnStartup = value; Changed = true; }
		}

		[Category("Database Options")]
		[Description("Reconnect to the database if the connection is broken.")]
		[Browsable(true)]
		[ReadOnly(false)]
		[DisplayName("Reconnect On Broken")]
		public bool ReconnectOnBroken
		{
			get { return Config.ReconnectOnBroken; }
			set { Config.ReconnectOnBroken = value; Changed = true; }
		}

		[Category("Database Options")]
		[Description("How many times can the connection attempt to reconnect if its broken or not connected.")]
		[Browsable(true)]
		[ReadOnly(false)]
		[DisplayName("Connection Attempt Limit")]
		public int ReconnectAttemptLimit
		{
			get { return Config.ReconnectAttemptLimit; }
			set { Config.ReconnectAttemptLimit = value; Changed = true; }
		}

		[Category("Database Options")]
		[Description("Settings Locked? Checking the connection works is recommended before setting this to true.")]
		[Browsable(true)]
		[ReadOnly(false)]
		[DisplayName("Lock Database")]
		public bool DatabaseLocked
		{
			get { return Config.DatabaseLocked; }
			set { Config.DatabaseLocked = value; Changed = true; }
		}

		[Category("Database Options")]
		[Description("Query tickrate? Server Extender is 20 tick / s, this must be in multiples of 20.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public int DatabaseTickRate
		{
			get { return Config.DatabaseTickRate; }
			set { Config.DatabaseTickRate = value; Changed = true; }
		}

		[Category("Global Options")]
		[Description("Debug Enabled?")]
		[Browsable(true)]
		[ReadOnly(false)]
		[DisplayName("Enable Debugging")]
		public bool IsDebugging
		{
			get { return Config.IsDebugging; }
			set { Config.IsDebugging = value; Changed = true; }
		}

		#endregion

		#region "Event Handlers"

		// Fires off if the event changes
		void connection_StateChange(object sender, StateChangeEventArgs e)
		{
			switch (e.CurrentState)
			{
				// if the connection breaks, make sure its closed, reconnect if the user wants
				// limited by the attempt amount
				case ConnectionState.Broken:

					LogManager.GameLog.WriteLineAndConsole("SQL Plugin - Connection to database failed!");
					this.DisconnectFromDatabase();
					m_connectedToDatabase = false;

					if (ReconnectOnBroken && ReconnectAttemptLimit <= m_reconnectAttemptCount)
					{
						LogManager.GameLog.WriteLineAndConsole("SQL Plugin - Attempting to reconnect to database..");
						this.ConnectToDatabase();
						m_reconnectAttemptCount += 1;
					}
					break;
				
				case ConnectionState.Connecting:
					LogManager.GameLog.WriteLineAndConsole("SQL Plugin - Attempting to connect to database..");
					break;

				case ConnectionState.Open:
					LogManager.GameLog.WriteLineAndConsole("SQL Plugin - Connected to database!");
					m_connectedToDatabase = true;
					break;

				case ConnectionState.Closed:
					LogManager.GameLog.WriteLineAndConsole("SQL Plugin - Database Connection Closed!");
					m_connectedToDatabase = false;
					break;
			}
		}

		// Runs 10 times a second when server is running
		//This cannot run 10 times a second, more like once every 10 seconds. I guess I could count 100 ticksbetween each query ~Vl4dim1r
		public override void Update()
		{
			// Idk what to put here for an example, look at my MOTDPlugin

			//after 10 seconds (100 ticks)
			//query the database for Cube Grids
			//query the database for Cube Grid Edited Flag
				//if database has been edited then apply database changes to server
				//else run Cube grid update

			//query the database for Player Info Edited Flag
				//if database has been edited then apply database changes to server
				//else run player info update

			if (Changed)
			{
				this.SaveXMLConfig();
				Changed = false;
			}
				

		}
		public override void Shutdown()
		{
            this.DisconnectFromDatabase();
		}

		#endregion

		#region "Methods"

		private bool ConnectToDatabase()
		{
			try
			{
				if (m_connectedToDatabase)
					return true;

				System.Threading.Tasks.Task.Factory.StartNew(() => connection.Open());
				return true;
			}
			catch (MySqlException ex)
			{
				LogManager.GameLog.WriteLine("SQL Plugin - Exception: " + ex.ToString() + "\n<<<<<END SQL EXCEPTION>>>>>\n\n");
				switch (ex.Number)
				 {
					 case 0:
						 LogManager.GameLog.WriteLineAndConsole("SQL Plugin - Cannot connect to server.  Contact administrator");
						 break;

					 case 1045:
						LogManager.GameLog.WriteLineAndConsole("SQL Plugin - Invalid username/password, please try again");
						break;
				  }
			 return false;
			}
		}

		private bool DisconnectFromDatabase()
		{
			try
			{
				if (!m_connectedToDatabase)
					return true;

				connection.Close();
				return true;
			}
			catch (MySqlException ex)
			{
				Console.WriteLine("SQL Plugin - Exception - Message (check log for details): " + ex.Message);
				LogManager.GameLog.WriteLine("SQL Plugin - Exception: " + ex.ToString() + "\n<<<<<END SQL EXCEPTION>>>>>\n\n");
				return false;
			}
		}
		public bool NonQuery(string table, string queryType, string fields, string values, string sqlparams) // NonQueries only for Insert, Update, and delete queries
		{
			MySqlCommand cmd = new MySqlCommand();
			cmd.Connection = connection;

			if (queryType == "INSERT")
			{
				cmd.CommandText = "INSERT INTO @table VALUES ";
			}
			else
			{
				if(queryType == "DELETE")
				{
					cmd.CommandText = "DELETE FROM @table WHERE ";
				}
				else
				{
					if(queryType == "UPDATE")
					{
						cmd.CommandText = "UPDATE @table SET ";
					}
					else
					{
						LogManager.GameLog.WriteLineAndConsole("SQL Plugin - Invalid Query Type");
					}
				}
			}		
			cmd.Prepare();
			cmd.Parameters.AddWithValue("@table",table);
			cmd.ExecuteNonQuery();
			return true;
		}

		public void SelectQuery()
		{
			string query = "SELECT * FROM CubeGrids, Players, Instances, Plugins";
			MySqlCommand cmd = new MySqlCommand(query, connection);
			//Create a data reader and Execute the command
			MySqlDataReader reader = cmd.ExecuteReader();
			while (reader.Read())
			{
			  SQLCubeGrids f = new SQLCubeGrids((int)reader[0], (string)reader[1], (int)reader[2], (int)reader[3], (int)reader[4], (int)reader[5], (int)reader[6], (int)reader[7], (int)reader[8], (int)reader[9]);
			  SQLPlayers m = new SQLPlayers((int)reader[10], (int)reader[11], (string)reader[12], (int)reader[13], (int)reader[14], (int)reader[15], (int)reader[16], (int)reader[17], (string)reader[18], (int)reader[19], (int)reader[20], (int)reader[21], (int)reader[22]); 
			}
			//close Data Reader
			reader.Close();
			//return list to be displayed
		}

		public int CountColumns(string table)		
		{
			string sqltable = table;
			string query = String.Format("SELECT Count(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE table_schema ='{0}'AND table_name = '{1}'", sqltable, DatabaseName);
    		int Count = -1;
        	MySqlCommand cmd = new MySqlCommand(query, connection);
       		Count = int.Parse(cmd.ExecuteScalar() + "");
        	return Count;
		}

		public void SaveXMLConfig()
		{
			try
			{
				TextWriter textWriter = new StreamWriter(m_dataFile);
				Serializer.Serialize(textWriter, Config);
				textWriter.Close();

				Console.WriteLine("SQL Plugin - Saved Data");
			}
			catch (Exception ex)
			{
				Console.WriteLine("SQL Plugin - Exception - Message (check log for details): " + ex.Message);
				LogManager.GameLog.WriteLine("SQL Plugin - Exception: " + ex.ToString() + "\n<<<<<END SQL EXCEPTION>>>>>\n\n");
			}
		}

		#region "Security"

		public static string EncryptString(SecureString input)
		{
			byte[] encryptedData = ProtectedData.Protect(
				Encoding.Unicode.GetBytes(ToInsecureString(input)),entropy,
				DataProtectionScope.CurrentUser);
			return Convert.ToBase64String(encryptedData);
		}

		public static SecureString DecryptString(string encryptedData)
		{
			try
			{
				byte[] decryptedData = ProtectedData.Unprotect(
					Convert.FromBase64String(encryptedData),
					entropy,
					DataProtectionScope.CurrentUser);
				return ToSecureString(Encoding.Unicode.GetString(decryptedData));
			}
			catch
			{
				return new SecureString();
			}
		}

		public static SecureString ToSecureString(string input)
		{
			SecureString secure = new SecureString();
			foreach (char c in input)
			{
				secure.AppendChar(c);
			}
			secure.MakeReadOnly();
			return secure;
		}

		public static string ToInsecureString(SecureString input)
		{
			string returnValue = string.Empty;
			IntPtr ptr = Marshal.SecureStringToBSTR(input);
			try
			{
				returnValue = Marshal.PtrToStringBSTR(ptr);
			}
			finally
			{
				Marshal.ZeroFreeBSTR(ptr);
			}
			return returnValue;
		}

		#endregion

		#endregion

		#region "Table Structure Info"
		/*
		 * SQL Table Structure as read in the Google Doc: https://docs.google.com/document/d/1x9lsvGikb6qZsHcUSyK_QqHV3NdQCmPupVqC0Fove1A/edit?usp=sharing 
		 * Tables
			Row Columns
		Cube Grids
			ID
			Beacon Name
			Owner 
			Size
			Power status 
			Basic Control (On/Off Reactors)
			Location
			Edited 


		Service Configuration
		 *	Service ID
			Service Name
			Location of World
			Location of Executable
			Status (online/Offline)
			Version
			Mods
			Timestamp of last save 
			Assembly rate
			Assembly Efficiency
			Gamemode
			Refinery rate
			refinery Efficiency
			Welder Speed
			Grinder Speed
			Inventory Multiplier
			Port
			MaxPlayers
			CurrentPlayers
			Edited

		Player Info
			SteamID
			CharacterID
			Steam Name
			Current Health
			Current Energy
			Credits
			Kills
			Deaths
			ID’s of Ships Owned
			FirstJoin 
			LastJoin
			PlayTime
			Edited

		Plugin configurations
			Motd
			Adverts
		garbage Collect On/Off
		Watchdog On/off
		Database Reporting On/Off
		Edited
		 */
		#endregion

	}
}
