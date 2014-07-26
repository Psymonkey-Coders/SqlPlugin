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

		private void CreatePlayersTable()
		{
			string query = "CREATE TABLE SEDS.PLAYERS (SteamID INT(20), EntID INT(20), SteamName VARCHAR(20), Health INT(10), Energy INT(10), Credits INT(10), Kills INT(10), Deaths INT(10), OwnedItems VARCHAR(255), FirstJoin VARCHAR(20), LastJoin VARCHAR(20), PlayTime VARCHAR(20), Edited INT(1))";
			if (this.ConnectToDatabase() == true)
			{
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.ExecuteNonQuery();
				this.DisconnectFromDatabase();

			}
		}
		private void CreateCubeGridsTable()
		{
			string query = "CREATE TABLE SEDS.CUBEGRIDS (ID INT(20), Beacon VARCHAR(20), Owner INT(20), Size INT(20), FuelTime INT(20), Power INT(1), LocX INT(10), LocY INT(10), LocZ Int(10), Edited INT(1))";
			if (this.ConnectToDatabase() == true)
			{
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.ExecuteNonQuery();
				this.DisconnectFromDatabase();

			}
		}
		private void CreateInstancesTable()
		{
			string query = "CREATE TABLE SEDS.INSTANCES (ID INT(20), ServiceName VARCHAR(20), WorldPath VARCHAR(255), ExecPath VARCHAR(255), Status VARCHAR(20), Version VARCHAR(10), Mods VARCHAR(10), LastSave VARCHAR(20), AssyMult INT(5), RefMult INT(5), RefEff INT(5), WeldSpeed INT(5), GrindSpeed INT(5), InvMult INT(10), Port INT(5), MaxPlayers INT(3), CurrentPlayers INT(3))";
			if (this.ConnectToDatabase() == true)
			{
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.ExecuteNonQuery();
				this.DisconnectFromDatabase();

			}
		}
		private void CreatePluginsTable()
		{
			string query = "CREATE TABLE SEDS.PLUGINS (ID INT(50), Name VARCHAR(20), Motd VARCHAR(255), Advert VARCHAR(255), GCEnabled INT(1), GCDelay INT(20), WatchdogEnabled INT(1), DBReporting INT(1), Edited INT(1))";
			if (this.ConnectToDatabase() == true)
			{
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.ExecuteNonQuery();
				this.DisconnectFromDatabase();

			}
		}

		private void InsertPlayerInfo()
		{
			string query = "INSERT INTO PLAYERS (STEAMID, EntID, SteamName, Health, Energy, Credits, Kills, Deaths, OwnedItems, FirstJoin, LastJoin, PlayTime, Edited) VALUES(STEAMID, EntID, SteamName, Health, Energy, Credits, Kills, Deaths, OwnedItems, FirstJoin, LastJoin, PlayTime)";

				//open connection
				 if (this.ConnectToDatabase() == true)
				 {
				//create command and assign the query and connection from the constructor
				 MySqlCommand cmd = new MySqlCommand(query, connection);
        
				 //Execute command
				 cmd.ExecuteNonQuery();

				 //close connection
				 this.DisconnectFromDatabase();
				 }

		}
		private void InsertCubeGrid()
		{
			string query = "INSERT INTO CUBEGRIDS (ID, EntID, BeaconName, Owner, Size, FuelTime, Power, LocX, LocY, Locz, Edited) VALUES(ID, EntID, BeaconName, Owner, Size, FuelTime, Power, LocX, LocY, Locz)";

			//open connection
			if (this.ConnectToDatabase() == true)
			{
				//create command and assign the query and connection from the constructor
				MySqlCommand cmd = new MySqlCommand(query, connection);

				//Execute command
				cmd.ExecuteNonQuery();

				//close connection
				this.DisconnectFromDatabase();
			}
		} 
		private void InsertInstance()
		{
			string query = "INSERT INTO INSTANCES (ID, ServiceName, WorldPath, ExecPath, Status, Version, Mods, LastSave, AssyMult, RefMult, RefEff, WeldSpeed, GrindSpeed, InvMult, Port, MaxPlayers, CurrentPlayers, Edited) VALUES(ID, ServiceName, WorldPath, ExecPath, Status, Version, Mods, LastSave, AssyMult, RefMult, RefEff, WeldSpeed, GrindSpeed, InvMult, Port, MaxPlayers, CurrentPlayers, Edited)";

			//open connection
			if (this.ConnectToDatabase() == true)
			{
				//create command and assign the query and connection from the constructor
				MySqlCommand cmd = new MySqlCommand(query, connection);

				//Execute command
				cmd.ExecuteNonQuery();

				//close connection
				this.DisconnectFromDatabase();
			}
		}
		private void InsertPluginConfig()
		{
			string query = "INSERT INTO PLUGINS (ID, Name, Motd, Advert, GCEnabled, GCDelay, WatchdogEnabled, DBReporting, Edited) VALUES(ID, Name, Motd, Advert, GCEnabled, GCDelay, WatchdogEnabled, DBReporting, Edited)";

			//open connection
			if (this.ConnectToDatabase() == true)
			{
				//create command and assign the query and connection from the constructor
				MySqlCommand cmd = new MySqlCommand(query, connection);

				//Execute command
				cmd.ExecuteNonQuery();

				//close connection
				this.DisconnectFromDatabase();
			}
		}

		private void UpdatePlayerInfo()
		{
			string query = "UPDATE PLAYERS SET EntID=ENTID, SteamName=STEAMNEAME, Health=HEALTH, Energy=ENERGY, Credits=CREDITS, Kills=KILLS, Deaths=DEATHS, OwnedItems=OWNEDITEMS, FirstJoin=FIRSTJOIN, LastJoin=LASTJOIN, PlayTime=PLAYTIME, Edited=EDITED WHERE SteamID = STEAMID";

			//Open connection
			if (this.ConnectToDatabase() == true)
			{
				//create mysql command
				MySqlCommand cmd = new MySqlCommand();
				//Assign the query using CommandText
				cmd.CommandText = query;
				//Assign the connection using Connection
				cmd.Connection = connection;

				//Execute query
				cmd.ExecuteNonQuery();

				//close connection
				this.DisconnectFromDatabase();
			}
		}
		private void UpdateCubeGrid()
		{
			string query = "UPDATE CUBEGRIDS SET ID=ID, BeaconName=BEACONNAME, Owner=OWNER, Size=SIZE, FuelTime=FUELTIME, Power=POWER, LocX=LOCX, LocY=LOCY, LocZ=LOCZ, Edited=EDITED WHERE ID = ID";

			//Open connection
			if (this.ConnectToDatabase() == true)
			{
				//create mysql command
				MySqlCommand cmd = new MySqlCommand();
				//Assign the query using CommandText
				cmd.CommandText = query;
				//Assign the connection using Connection
				cmd.Connection = connection;

				//Execute query
				cmd.ExecuteNonQuery();

				//close connection
				this.DisconnectFromDatabase();
			}
		}
		private void UpdateInstance()
		{
			string query = "UPDATE INSTANCES SET ID=ID, ServiceName=SERVICENAME, WorldPath=WORLDPATH, ExecPath=EXECPATH, Status=STATUS, Version=VERSION, Mods=MODS, LastSave=LASTSAVE, AssyMult=ASSYMULT, RefMult=REFMULT, RefEff=REFEFF, WeldSpeed=WELDSPEED, GrindSpeed=GRINDSPEED, InvMult=INVMULT, Port=PORT, MaxPlayers=MAXPLAYERS, CurrentPlayers=CURRENTPLAYERS, Edited=EDITED WHERE ID = ID";

			//Open connection
			if (this.ConnectToDatabase() == true)
			{
				//create mysql command
				MySqlCommand cmd = new MySqlCommand();
				//Assign the query using CommandText
				cmd.CommandText = query;
				//Assign the connection using Connection
				cmd.Connection = connection;

				//Execute query
				cmd.ExecuteNonQuery();

				//close connection
				this.DisconnectFromDatabase();
			}
		}
		private void UpdatePluginConfig()
		{
			string query = "UPDATE PLUGINS SET ID=ID, Name=NAME, Motd=MOTD, Advert=ADVERT, GCEnabled=GCENABLED, WatchdogEnabled=WATCHDOG, DBReporting=DBREPORT, Edited=EDITED WHERE SteamID = STEAMID";

			//Open connection
			if (this.ConnectToDatabase() == true)
			{
				//create mysql command
				MySqlCommand cmd = new MySqlCommand();
				//Assign the query using CommandText
				cmd.CommandText = query;
				//Assign the connection using Connection
				cmd.Connection = connection;

				//Execute query
				cmd.ExecuteNonQuery();

				//close connection
				this.DisconnectFromDatabase();
			}
		}

		private void DeletePlayerInfo()
		{
			string query = "DELETE FROM PLAYERS WHERE SteamID=STEAMID";

			if (this.ConnectToDatabase() == true)
			{
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.ExecuteNonQuery();
				this.DisconnectFromDatabase();
			}
		}
		private void DeleteCubeGrid()
		{
			string query = "DELETE FROM CUBEGRIDS WHERE ID=ID";

			if (this.ConnectToDatabase() == true)
			{
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.ExecuteNonQuery();
				this.DisconnectFromDatabase();
			}
		}
		private void DeleteInstance()
		{
			string query = "DELETE FROM INSTANCES WHERE ID=ID";

			if (this.ConnectToDatabase() == true)
			{
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.ExecuteNonQuery();
				this.DisconnectFromDatabase();
			}
		}
		private void DeletePluginConfig()
		{
			string query = "DELETE FROM PLUGINS WHERE ID=ID";

			if (this.ConnectToDatabase() == true)
			{
				MySqlCommand cmd = new MySqlCommand(query, connection);
				cmd.ExecuteNonQuery();
				this.DisconnectFromDatabase();
			}
		}

		private List <string> [] SelectPlayerInfo()
		{
			    string query = "SELECT * FROM PLAYERINFO";

				//Create a list to store the result
				 List< string >[] list = new List< string >[13];
				for (int x = 0; x < 13; x++)
				{
				 list[x] = new List< string >();
				}

				 //Open connection
				 if (this.ConnectToDatabase() == true)
				 {
					    //Create Command
					   MySqlCommand cmd = new MySqlCommand(query, connection);
					  //Create a data reader and Execute the command
					  MySqlDataReader dataReader = cmd.ExecuteReader();
					  
					  //Read the data and store them in the list
					  while (dataReader.Read())
					 {
						  list[0].Add(dataReader["SteamID"] + "");
						  list[1].Add(dataReader["EntID"] + "");
						  list[2].Add(dataReader["SteamName"] + "");
						  list[3].Add(dataReader["Health"] + "");
						  list[4].Add(dataReader["Energy"] + "");
						  list[5].Add(dataReader["Credits"] + "");
						  list[6].Add(dataReader["Kills"] + "");
						  list[7].Add(dataReader["Deaths"] + "");
						  list[8].Add(dataReader["OwnedItems"] + "");
						  list[9].Add(dataReader["FirstJoin"] + "");
						  list[10].Add(dataReader["Lastjoin"] + "");
						  list[11].Add(dataReader["PlayTime"] + "");
						  list[12].Add(dataReader["Edited"] + "");
					 }

					 //close Data Reader
					 dataReader.Close();

					//close Connection
					this.DisconnectFromDatabase();

					 //return list to be displayed
					 return list;
				 }
				 else
				  {
				     return list;
				  }
		}
		private List <string> [] SelectCubeGrid()
		{
			string query = "SELECT * FROM CUBEGRIDS";

			//Create a list to store the result
			List<string>[] list = new List<string>[13];
			for (int x = 0; x < 13; x++)
			{
				list[x] = new List<string>();
			}

			//Open connection
			if (this.ConnectToDatabase() == true)
			{
				//Create Command
				MySqlCommand cmd = new MySqlCommand(query, connection);
				//Create a data reader and Execute the command
				MySqlDataReader dataReader = cmd.ExecuteReader();

				//Read the data and store them in the list
				while (dataReader.Read())
				{
					list[0].Add(dataReader["EntID"] + "");
					list[1].Add(dataReader["BeaconName"] + "");
					list[2].Add(dataReader["Owner"] + "");
					list[3].Add(dataReader["Size"] + "");
					list[4].Add(dataReader["FuelTime"] + "");
					list[5].Add(dataReader["Power"] + "");
					list[6].Add(dataReader["LocX"] + "");
					list[7].Add(dataReader["LocY"] + "");
					list[8].Add(dataReader["LocZ"] + "");
				}

				//close Data Reader
				dataReader.Close();

				//close Connection
				this.DisconnectFromDatabase();

				//return list to be displayed
				return list;
			}
			else
			{
				return list;
			}
		}
		private List <string> [] SelectInstance()
		{
			string query = "SELECT * FROM INSTANCES";

			//Create a list to store the result
			List<string>[] list = new List<string>[13];
			for (int x = 0; x < 13; x++)
			{
				list[x] = new List<string>();
			}

			//Open connection
			if (this.ConnectToDatabase() == true)
			{
				//Create Command
				MySqlCommand cmd = new MySqlCommand(query, connection);
				//Create a data reader and Execute the command
				MySqlDataReader dataReader = cmd.ExecuteReader();

				//Read the data and store them in the list
				while (dataReader.Read())
				{
					list[0].Add(dataReader["ID"] + "");
					list[1].Add(dataReader["ServiceName"] + "");
					list[2].Add(dataReader["WorldPath"] + "");
					list[3].Add(dataReader["ExecPath"] + "");
					list[4].Add(dataReader["Status"] + "");
					list[5].Add(dataReader["Version"] + "");
					list[6].Add(dataReader["Mods"] + "");
					list[7].Add(dataReader["LastSave"] + "");
					list[8].Add(dataReader["AssyMult"] + "");
					list[9].Add(dataReader["RefMult"] + "");
					list[10].Add(dataReader["RefEff"] + "");
					list[11].Add(dataReader["WeldSpeed"] + "");
					list[12].Add(dataReader["GrindSpeed"] + "");
					list[13].Add(dataReader["InvMult"] + "");
					list[14].Add(dataReader["Port"] + "");
					list[15].Add(dataReader["MaxPlayers"] + "");
					list[16].Add(dataReader["CurrentPlayers"] + "");
				}

				//close Data Reader
				dataReader.Close();

				//close Connection
				this.DisconnectFromDatabase();

				//return list to be displayed
				return list;
			}
			else
			{
				return list;
			}
		}
		private List <string> [] SelectPluginConfig()
		{
			string query = "SELECT * FROM PLUGINS";

			//Create a list to store the result
			List<string>[] list = new List<string>[13];
			for (int x = 0; x < 13; x++)
			{
				list[x] = new List<string>();
			}

			//Open connection
			if (this.ConnectToDatabase() == true)
			{
				//Create Command
				MySqlCommand cmd = new MySqlCommand(query, connection);
				//Create a data reader and Execute the command
				MySqlDataReader dataReader = cmd.ExecuteReader();

				//Read the data and store them in the list
				while (dataReader.Read())
				{
					list[0].Add(dataReader["ID"] + "");
					list[1].Add(dataReader["Name"] + "");
					list[2].Add(dataReader["Motd"] + "");
					list[3].Add(dataReader["Advert"] + "");
					list[4].Add(dataReader["GCEnabled"] + "");
					list[5].Add(dataReader["GCDelay"] + "");
					list[6].Add(dataReader["WatchdogEnabled"] + "");
					list[7].Add(dataReader["DBReporting"] + "");
				}

				//close Data Reader
				dataReader.Close();

				//close Connection
				this.DisconnectFromDatabase();

				//return list to be displayed
				return list;
			}
			else
			{
				return list;
			}
		}
		/*private int Count()
		{

		}
		8?



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
