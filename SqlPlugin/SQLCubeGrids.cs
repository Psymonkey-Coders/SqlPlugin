/*
 * Created by SharpDevelop.
 * User: Vl4dim1r
 * Date: 7/29/2014
 * Time: 2:21 PM
 * 
 */
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
	/// <summary>
	/// Provides methods for handling SQL - Server data sync
	/// </summary>
	public class SQLCubeGrids
	{
		public int EntID;
		public string Beacon;
		public int Owner;
		public int Size;
		public int FuelTime;
		public int Power;
		public int LocX;
		public int LocY;
		public int LocZ;
		public int Edited;

		
	#region "Methods"
			public SQLCubeGrids(int ID, string beacon, int owner, int size, int fuelTime, int power, int locX, int locY, int locZ, int edited)
		{
			this.EntID = ID;
			this.Beacon = beacon;
			this.Owner = owner;
			this.Size = size;
			this.FuelTime = fuelTime;
			this.Power = power;
			this.LocX = locX;
			this.LocY = locY;
			this.LocZ = locZ;
			this.Edited = edited;
		}
	#endregion
	}
}
