using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

using SEModAPIExtensions.API; //required for plugins
using SEModAPIExtensions.API.Plugin; //required for plugins
using SEModAPIExtensions.API.Plugin.Events; // plugin events

using SEModAPIInternal.Support;
using SEModAPIInternal.API.Common;

using MySql.Data.MySqlClient; //Add MySql Library

namespace SqlPlugin
{
	public class Core : PluginBase
	{
		#region "Attributes"

		// variable definitions
		// access static (string,bool, ect ect) m_variableName;

		private static string m_firstRun;

		private static string m_databaseName;
		private static string m_databaseHost;
		private static string m_DatabasePort;
		private static string m_databaseUser;
		private static string m_databasePass;
		private static int m_sqltickRate;
		public static int m_databaseTickRate;

		private static bool m_databaseEnabled;
		private static bool m_databaseLocked;
		private static bool m_isDebugging; //SandboxGameAssemblyWrapper.IsDebugging

		private static bool m_databaseSettingsChanged;
		
		private MySqlConnection connection;
		
		#endregion

		#region "Constructors and Initializers

		// Called when the server first launches
		public Core()
		{
			Console.WriteLine("SQL Plugin '" + Id.ToString() + "' constructed!");
		}

		// Called when the server finishes loading
		public override void Init()
		{

			string connectionString;
			connectionString = "SERVER=" + m_databaseHost + ";" + "DATABASE=" +
			m_databaseName + ";" + "PORT=" + m_DatabasePort + ";" + "UID=" + m_databaseUser + ";" + "PASSWORD=" + m_databasePass + ";";

			connection = new MySqlConnection(connectionString);
			Console.WriteLine("SQL Plugin '" + Id.ToString() + "' initialized!");
			this.ConnectToDatabase();
            Console.WriteLine("SQL Plugin: Connected to database");
		}

		#endregion

		#region "Properties"

		// get set variables, options on the properties panel for plugin
		[Category("SQL Database")]
		[Description("The IP/Domain used to connect to SQL host ")]
		[Browsable(true)]
		[ReadOnly(false)]
		public string DatabaseHost
		{
			get
			{
				return m_databaseHost; // Send m_databaseNames value to the plugins properties panel
			}
			set
			{
				m_databaseHost = value; // if the value changes in the code, it updates the plugins properties panel
			}
		}

		[Category("SQL Database")]
		[Description("The user used to connect to SQL host ")]
		[Browsable(true)]
		[ReadOnly(false)]
		public string DatabaseUser
		{
			get
			{
				return m_databaseUser; // Send m_databaseNames value to the plugins properties panel
			}
			set
			{
				m_databaseUser = value; // if the value changes in the code, it updates the plugins properties panel
			}
		}

		[Category("SQL Database")]
		[Description("The password used to connect to SQl host")]
		[Browsable(true)]
		[ReadOnly(false)]
		public string DatabasePass
		{
			get
			{
				return m_databasePass; // Send m_databaseNames value to the plugins properties panel
			}
			set
			{
				m_databasePass = value; // if the value changes in the code, it updates the plugins properties panel
			}
		}

		[Category("SQL Database")]
		[Description("The name of the SQL database you want to connect to")]
		[Browsable(true)]
		[ReadOnly(false)]
		public string DatabaseName
		{
			get
			{
				return m_databaseName; // Send m_databaseNames value to the plugins properties panel
			}
			set
			{
				m_databaseName = value; // if the value changes in the code, it updates the plugins properties panel
			}
		}

		[Category("SQL Database")]
		[Description("Debug Enabled?")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool DatabaseEnabled
		{
			get
			{
				return m_isDebugging; // Send m_databaseNames value to the plugins properties panel
			}
			set
			{
				m_isDebugging = value; // if the value changes in the code, it updates the plugins properties panel
			}
		}
		[Category("SQL Database")]
		[Description("Settings Locked? Checking the connection works is recommended before setting this to true.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool DatabaseLocked
		{
			get
			{
				return m_databaseLocked; // Send m_databaseNames value to the plugins properties panel
			}
			set
			{
				m_databaseLocked = value; // if the value changes in the code, it updates the plugins properties panel
			}
		}
		[Category("SQL Database")]
		[Description("Query tickrate? Server Extender is 20 tick / s, this must be in multiples of 20.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public int DatabaseTickrate
		{
			get
			{
				return m_databaseTickRate; // Send m_databaseNames value to the plugins properties panel
			}
			set
			{
				m_databaseTickRate = value; // if the value changes in the code, it updates the plugins properties panel
			}
		}
		#endregion

		#region "Event Handlers"

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

		}
		public override void Shutdown()
		{
            DisconnectFromDatabase();
            Console.WriteLine("SQL Plugin: Disconnected from database");
		}

		#endregion

		#region "Methods"


		private bool ConnectToDatabase()
		{
			// try to use try/catch blocks in something that could cause an exception
			try
			{
				// I'm sure failing to connect to a database could cause an exception!
				connection.Open();
					return true;
			}
			catch (MySqlException ex)
			{
				
				switch (ex.Number)
				 {
					 case 0:
					  LogManager.APILog.WriteLineAndConsole("Cannot connect to server.  Contact administrator");
					  break;

					 case 1045:
					  LogManager.APILog.WriteLineAndConsole("Invalid username/password, please try again");
					 break;
				  }
			 return false;

			}
		}
		private bool DisconnectFromDatabase()
		{
			try
			{
				connection.Close();
				return true;
			}
			catch (MySqlException ex)
			{
				LogManager.APILog.WriteLineAndConsole(ex.Message);
				return false;
			}
		}

		



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
