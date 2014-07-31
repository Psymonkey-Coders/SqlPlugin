/*
 * Created by SharpDevelop.
 * User: Vl4dim1r
 * Date: 7/30/2014
 * Time: 2:48 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace SqlPlugin
{
	/// <summary>
	/// Description of SQlPlayers.
	/// </summary>
	public class SQLPlayers
	{
		public int SteamID;
		public int EntID;
		public string SteamName;
		public int Health;
		public int Energy;
		public int Credits;
		public int Kills;
		public int Deaths;
		public string OwnedItems;
		public int FirstJoin;
		public int LastJoin;
		public int PlayTime;
		public int Edited;
		#region "methods"
		public SQLPlayers(int steamid, int entid, string steamName, int health, int energy, int credits, int kills, int deaths, string owneditems, int firstJoin, int lastJoin, int playTime, int edited)
		{
			this.SteamID = steamid;
			this.EntID = entid;
			this.SteamName = steamName;
			this.Health = health;
			this.Energy = energy;
			this.Credits = credits;
			this.Kills = kills;
			this.Deaths = deaths;
			this.OwnedItems = owneditems;
			this.FirstJoin = firstJoin;
			this.LastJoin = lastJoin;
			this.PlayTime = playTime;
		}
		#endregion
	}
}
