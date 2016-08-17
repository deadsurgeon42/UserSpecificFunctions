using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using TShockAPI;
using TShockAPI.DB;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using UserSpecificFunctions.Extensions;

namespace UserSpecificFunctions
{
	public class Database
	{
		private static IDbConnection db;
		public List<PlayerInfo> PlayerData { get; private set; }

		public void DBConnect()
		{
			switch (TShock.Config.StorageType.ToLower())
			{
				case "mysql":
					string[] dbHost = TShock.Config.MySqlHost.Split(':');
					db = new MySqlConnection()
					{
						ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
							dbHost[0],
							dbHost.Length == 1 ? "3306" : dbHost[1],
							TShock.Config.MySqlDbName,
							TShock.Config.MySqlUsername,
							TShock.Config.MySqlPassword)

					};
					break;

				case "sqlite":
					string sql = Path.Combine(TShock.SavePath, "tshock.sqlite");
					db = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
					break;
			}

			SqlTableCreator sqlcreator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

			sqlcreator.EnsureTableStructure(new SqlTable("UserSpecificFunctions",
				new SqlColumn("UserID", MySqlDbType.Int32),
				new SqlColumn("Prefix", MySqlDbType.Text),
				new SqlColumn("Suffix", MySqlDbType.Text),
				new SqlColumn("Color", MySqlDbType.Text),
				new SqlColumn("Permissions", MySqlDbType.Text)));

			Task.Run(() => LoadPlayerData());
		}

		/// <summary>
		/// Reloads player data into memory.
		/// </summary>
		/// <returns></returns>
		public async Task LoadPlayerData()
		{
			List<PlayerInfo> playerData = await GetPlayersAsync();
			PlayerData = playerData;
		}

		/// <summary>
		/// Gets the player matching the specified ID.
		/// </summary>
		/// <param name="playerID">The player's ID.</param>
		/// <returns>A <see cref="PlayerInfo"/> object.</returns>
		public PlayerInfo GetPlayer(int playerID)
		{
			return PlayerData.Find(p => p.UserID == playerID);
		}

		/// <summary>
		/// Gets the player matching the specified ID as an asynchronous operation.
		/// </summary>
		/// <param name="playerID"></param>
		/// <returns>A task with the return type of list of <see cref="PlayerInfo"/> objects.</returns>
		public Task<PlayerInfo> GetPlayerAsync(int playerID)
		{
			return Task.Run(() =>
			{
				using (QueryResult reader = db.QueryReader("SELECT * FROM UserSpecificFunctions WHERE UserID=@0;", playerID.ToString()))
				{
					if (reader.Read())
					{
						PlayerInfo playerInfo = new PlayerInfo();
						playerInfo.Load(reader);
						return playerInfo;
					}
				}

				return null;
			});
		}

		/// <summary>
		/// Pulls players from the database as an asynchronous operation.
		/// </summary>
		/// <returns>A task with the return type of list of <see cref="PlayerInfo"/> objects.</returns>
		public Task<List<PlayerInfo>> GetPlayersAsync()
		{
			List<PlayerInfo> players = new List<PlayerInfo>();
			return Task.Run(async () =>
			{
				using (QueryResult reader = db.QueryReader("SELECT * FROM UserSpecificFunctions"))
				{
					while (reader.Read())
					{
						players.Add(await GetPlayerAsync(reader.Get<int>("UserID")));
					}
				}

				return players;
			});
		}

		/// <summary>
		/// Asynchronously inserts a player into the database.
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public Task<bool> AddPlayerAsync(PlayerInfo player)
		{
			return Task.Run(() => 
			{
				try
				{
					lock (PlayerData)
					{
						PlayerData.Add(player);
						string query = "INSERT INTO UserSpecificFunctions (UserID, Prefix, Suffix, Color, Permissions) VALUES (@0, @1, @2, @3, @4);";
						return db.Query(query, player.UserID, player.Prefix, player.Suffix, player.ChatColor, player.Permissions.Any() ? player.Permissions.Separate(",") : null) == 1;
					}
				}
				catch(Exception ex)
				{
					TShock.Log.Error(ex.ToString());
					return false;
				}
			});
		}

		/// <summary>
		/// Asynchronously sets the player's prefix.
		/// </summary>
		/// <param name="playerID">The player's ID.</param>
		/// <param name="prefix">The player's prefix.</param>
		/// <returns>A task with the return type of true or false.</returns>
		public Task<bool> SetPrefixAsync(int playerID, string prefix)
		{
			return Task.Run(async () =>
			{
				try
				{
					PlayerInfo player = await GetPlayerAsync(playerID);
					if (player == null)
					{
						await AddPlayerAsync(new PlayerInfo() { UserID = playerID, Prefix = prefix });
					}
					else
					{
						player.Prefix = prefix;
						db.Query("UPDATE UserSpecificFunctions SET Prefix=@0 WHERE UserID=@1;", player.Prefix, player.UserID.ToString());
						lock (PlayerData)
						{
							PlayerData.RemoveAll(p => p.UserID == player.UserID);
							PlayerData.Add(player);
						}
					}

					return true;
				}
				catch (Exception ex)
				{
					TShock.Log.Error(ex.ToString());
					return false;
				}
			});
		}

		/// <summary>
		/// Asynchronously sets the player's prefix.
		/// </summary>
		/// <param name="playerID">The player's ID.</param>
		/// <param name="suffix">The player's suffix.</param>
		/// <returns>A task with the return type of true or false.</returns>
		public Task<bool> SetSuffixAsync(int playerID, string suffix)
		{
			return Task.Run(async () =>
			{
				try
				{
					PlayerInfo player = await GetPlayerAsync(playerID);
					if (player == null)
					{
						await AddPlayerAsync(new PlayerInfo() { UserID = playerID, Suffix = suffix });
					}
					else
					{
						player.Suffix = suffix;
						db.Query("UPDATE UserSpecificFunctions SET Suffix=@0 WHERE UserID=@1;", player.Suffix, player.UserID.ToString());
						lock (PlayerData)
						{
							PlayerData.RemoveAll(p => p.UserID == player.UserID);
							PlayerData.Add(player);
						}
					}

					return true;
				}
				catch (Exception ex)
				{
					TShock.Log.Error(ex.ToString());
					return false;
				}
			});
		}

		/// <summary>
		/// Asynchronously sets the player's color.
		/// </summary>
		/// <param name="playerID">The player's ID.</param>
		/// <param name="color">The player's color.</param>
		/// <returns>A task with the return type of true or false.</returns>
		public Task<bool> SetColorAsync(int playerID, string chatColor)
		{
			return Task.Run(async () =>
			{
				try
				{
					PlayerInfo player = await GetPlayerAsync(playerID);
					if (player == null)
					{
						await AddPlayerAsync(new PlayerInfo() { UserID = playerID, ChatColor = chatColor });
					}
					else
					{
						player.ChatColor = chatColor;
						db.Query("UPDATE UserSpecificFunctions SET Color=@0 WHERE UserID=@1;", player.ChatColor, player.UserID.ToString());
						lock (PlayerData)
						{
							PlayerData.RemoveAll(p => p.UserID == player.UserID);
							PlayerData.Add(player);
						}
					}

					return true;
				}
				catch (Exception ex)
				{
					TShock.Log.Error(ex.ToString());
					return false;
				}
			});
		}

		/// <summary>
		/// Asynchronously adds permissions to the player.
		/// </summary>
		/// <param name="playerID">The player's ID.</param>
		/// <param name="permissions">The list of permissions.</param>
		/// <returns>A task with the return type of true or false.</returns>
		public Task<bool> AddPlayerPermissionsAsync(int playerID, List<string> permissions)
		{
			return Task.Run(async () =>
			{
				try
				{
					PlayerInfo player = await GetPlayerAsync(playerID);
					if (player == null)
					{
						await AddPlayerAsync(new PlayerInfo() { UserID = playerID, Permissions = permissions });
					}
					else
					{
						permissions.Where(p => !player.Permissions.Contains(p)).ForEach(p => player.Permissions.Add(p));
						db.Query("UPDATE UserSpecificFunctions SET Permissions=@0 WHERE UserID=@1;", player.Permissions.Separate(","), player.UserID.ToString());
						lock (PlayerData)
						{
							PlayerData.RemoveAll(p => p.UserID == player.UserID);
							PlayerData.Add(player);
						}
					}

					return true;
				}
				catch (Exception ex)
				{
					TShock.Log.Error(ex.ToString());
					return false;
				}
			});
		}

		/// <summary>
		/// Asynchronously removes permissions from the player.
		/// </summary>
		/// <param name="playerID">The player's ID.</param>
		/// <param name="permissions">The list of permissions.</param>
		/// <returns>A task with the return type of true or false.</returns>
		public Task<bool> RemovePlayerPermissionsAsync(int playerID, List<string> permissions)
		{
			return Task.Run(async () =>
			{
				try
				{
					PlayerInfo player = await GetPlayerAsync(playerID);
					if (player == null)
					{
						return false;
					}
					else
					{
						permissions.ForEach(p => player.Permissions.Remove(p));
						db.Query("UPDATE UserSpecificFunctions SET Permissions=@0 WHERE UserID=@1;", player.Permissions.Count > 0 ? player.Permissions.Separate(",") : null, player.UserID.ToString());
						lock (PlayerData)
						{
							PlayerData.RemoveAll(p => p.UserID == player.UserID);
							PlayerData.Add(player);
						}
					}

					return true;
				}
				catch (Exception ex)
				{
					TShock.Log.Error(ex.ToString());
					return false;
				}
			});
		}

		/// <summary>
		/// Asynchronously removes the player from the database.
		/// </summary>
		/// <param name="playerID">The player's ID.</param>
		/// <returns>A task with the return type of true or false.</returns>
		public Task<bool> RemoveUserAsync(int playerID)
		{
			return Task.Run(() =>
			{
				return db.Query("DELETE FROM UserSpecificFunctions WHERE UserID=@0;", playerID.ToString()) == 1;
			});
		}

		/// <summary>
		/// Asynchronously resets the player's information.
		/// </summary>
		/// <param name="playerID"></param>
		/// <returns></returns>
		public Task<bool> ResetDataAsync(int playerID)
		{
			return Task.Run(async () =>
			{
				try
				{
					await Task.WhenAll(SetPrefixAsync(playerID, null), SetSuffixAsync(playerID, null), SetColorAsync(playerID, null));
					return true;
				}
				catch (Exception ex)
				{
					TShock.Log.Error(ex.ToString());
					return false;
				}
			});
		}

		/// <summary>
		/// Asynchronously removes empty entries from the database.
		/// </summary>
		/// <returns></returns>
		public Task<bool> PurgeEntriesAsync()
		{
			return Task.Run(async () =>
			{
				try
				{
					List<PlayerInfo> pendingRemoval = new List<PlayerInfo>();
					foreach (PlayerInfo player in PlayerData)
					{
						if (player.Prefix == null && player.Suffix == null && player.ChatColor == null && !player.Permissions.Any())
						{
							pendingRemoval.Add(player);
							await RemoveUserAsync(player.UserID);
						}
					}

					lock (PlayerData)
					{
						PlayerData.RemoveAll(p => pendingRemoval.Contains(p));
					}

					return true;
				}
				catch (Exception ex)
				{
					TShock.Log.Error(ex.ToString());
					return false;
				}
			});
		}
	}
}
