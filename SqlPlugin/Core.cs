using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

using SEModAPIExtensions.API; 
using SEModAPIExtensions.API.Plugin; 
using SEModAPIExtensions.API.Plugin.Events; 

using SEModAPIInternal.Support;
using SEModAPIInternal.API.Common;

using MySql.Data.MySqlClient; 

namespace SqlPlugin
{
	public class Core : PluginBase
	{
		#region "Attributes"

		//private static string m_firstRun;

		private static string m_databaseName;
		private static string m_databaseHost;
		private static string m_databasePort;
		private static string m_databaseUser;
		private static string m_databasePass;

		private static bool m_connectOnStartup;
		private static bool m_connectedToDatabase;
		private static bool m_reconnectOnBroken;
		private static bool m_databaseEnabled;
		private static bool m_databaseLocked;
		private static bool m_isDebugging;
		private static bool m_databaseSettingsChanged;

		private static int m_reconnectAttemptLimit;
		private static int m_reconnectAttemptCount;
		private static int m_sqltickRate;
		public static int m_databaseTickRate;

		private MySqlConnection connection = new MySqlConnection();
		
		#endregion

		#region "Constructors and Initializers

		// Called when the server first launches
		public Core()
		{
			Console.WriteLine("SQL Plugin '" + Id.ToString() + "' constructed!");

			// default values
			m_databaseName = "";
			m_databaseHost = "";
			m_databasePort = "";
			m_databaseUser = "";
			m_databasePass = "";
			m_sqltickRate = 100;
			m_databaseTickRate = 100;

			m_databaseEnabled = true;
			m_databaseLocked = false;
			m_connectedToDatabase = false;

			m_reconnectAttemptCount = 0;
			m_reconnectAttemptLimit = 5;

			//set this to true for testing if you wish!
			m_connectOnStartup = false;
		}

		// Called when the server finishes loading
		public override void Init()
		{
			connection.ConnectionString = String.Format("SERVER={0};DATABASE={1};PORT={2};UID={3};PASSWORD={4};",
				m_databaseHost, m_databaseName, m_databasePort, m_databaseUser, m_databasePass);

			connection.StateChange += connection_StateChange;

			Console.WriteLine("SQL Plugin '" + Id.ToString() + "' initialized!");

			Console.WriteLine("SQL Plugin - Ready to connect to database");

			if(m_connectOnStartup)
				this.ConnectToDatabase();	
		}

		#endregion

		#region "Properties"

		// get set variables, options on the properties panel for plugin
		[Category("Connection Setup")]
		[Description("The IP/Domain used to connect to SQL host ")]
		[Browsable(true)]
		[ReadOnly(false)]
		[DisplayName("Database Host")]
		public string DatabaseHost
		{
			get { return m_databaseHost; }
			set { m_databaseHost = value;}
		}

		[Category("Connection Setup")]
		[Description("The Port used to connect to SQL host ")]
		[Browsable(true)]
		[ReadOnly(false)]
		[DisplayName("Database Port")]
		public string DatabasePort
		{
			get { return m_databasePort; }
			set { m_databasePort = value; }
		}

		[Category("Connection Setup")]
		[Description("The user used to connect to SQL host ")]
		[Browsable(true)]
		[ReadOnly(false)]
		[DisplayName("Database User")]
		public string DatabaseUser
		{
			get { return m_databaseUser; }
			set { m_databaseUser = value; }
		}

		[Category("Connection Setup")]
		[Description("The password used to connect to SQL host")]
		[Browsable(true)]
		[ReadOnly(false)]
		[DisplayName("Database Password")]
		public string DatabasePass
		{
			get { return m_databasePass; }	
			set { m_databasePass = value; }		
		}

		[Category("Connection Setup")]
		[Description("The name of the SQL database you want to connect to")]
		[Browsable(true)]
		[ReadOnly(false)]
		[DisplayName("Database Name")]
		public string DatabaseName
		{
			get { return m_databaseName; }
			set { m_databaseName = value; }	
		}

		[Category("Connection Setup")]
		[Description("True/False Set to true to attempt to connect")]
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
			get { return m_connectOnStartup; }
			set { m_connectOnStartup = value; }
		}

		[Category("Database Options")]
		[Description("Reconnect to the database if the connection is broken or not connected.")]
		[Browsable(true)]
		[ReadOnly(false)]
		[DisplayName("Reconnect on failure")]
		public bool DatabaseReconnect
		{
			get { return m_reconnectOnBroken; }
			set { m_reconnectOnBroken = value; }
		}

		[Category("Database Options")]
		[Description("How many times can the connection attempt to reconnect if its broken or not connected.")]
		[Browsable(true)]
		[ReadOnly(false)]
		[DisplayName("Connection Attempt Limit")]
		public int DatabaseReconnectLimit
		{
			get { return m_reconnectAttemptLimit; }
			set { m_reconnectAttemptLimit = value; }
		}

		[Category("Database Options")]
		[Description("Settings Locked? Checking the connection works is recommended before setting this to true.")]
		[Browsable(true)]
		[ReadOnly(false)]
		[DisplayName("Lock Database")]
		public bool DatabaseLocked
		{
			get { return m_databaseLocked; }
			set { m_databaseLocked = value; }
		}

		[Category("Database Options")]
		[Description("Query tickrate? Server Extender is 20 tick / s, this must be in multiples of 20.")]
		[Browsable(true)]
		[ReadOnly(false)]
		public int DatabaseTickrate
		{
			get { return m_databaseTickRate; }
			set { m_databaseTickRate = value; }
		}

		[Category("Global Options")]
		[Description("Debug Enabled?")]
		[Browsable(true)]
		[ReadOnly(false)]
		[DisplayName("Enable Debugging")]
		public bool DebugEnabled
		{
			get { return m_isDebugging; }
			set { m_isDebugging = value; }
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
					m_connectedToDatabase = false;
					this.DisconnectFromDatabase();
					

					if (m_reconnectOnBroken && m_reconnectAttemptLimit != m_reconnectAttemptCount)
					{
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

				connection.Open();
				return m_connectedToDatabase;
			}
			catch (MySqlException ex)
			{	
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
				return m_connectedToDatabase;
			}
			catch (MySqlException ex)
			{
				LogManager.GameLog.WriteLineAndConsole(ex.ToString());
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
