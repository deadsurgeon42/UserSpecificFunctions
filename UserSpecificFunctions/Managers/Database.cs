using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using TShockAPI;
using TShockAPI.DB;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;

namespace UserSpecificFunctions
{
    public class Database
    {
        private static IDbConnection db;

        internal static void DBConnect()
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
        }

		public PlayerInfo GetPlayer(int playerID)
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
		}

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

		public List<PlayerInfo> GetPlayers()
		{
			List<PlayerInfo> players = new List<PlayerInfo>();
			using (QueryResult reader = db.QueryReader("SELECT * FROM UserSpecificFunctions"))
			{
				while (reader.Read())
				{
					players.Add(GetPlayer(reader.Get<int>("UserID")));
				}
			}

			return players;
		}

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

		public Task<bool> SetPrefixAsync(int playerID, string prefix)
		{
			return Task.Run(async () =>
			{
				try
				{
					if (await GetPlayerAsync(playerID) == null)
					{
						db.Query("INSERT INTO UserSpecificFunctions (UserID, Prefix, Suffix, Color, Permissions) VALUES (@0, @1, @2, @3, @4);", playerID.ToString(), prefix, null, null, null);
					}
					else
					{
						db.Query("UPDATE UserSpecificFunctions SET Prefix=@0 WHERE UserID=@1;", prefix, playerID.ToString());
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

		public Task<bool> SetSuffixAsync(int playerID, string suffix)
		{
			return Task.Run(async () =>
			{
				try
				{
					if (await GetPlayerAsync(playerID) == null)
					{
						db.Query("INSERT INTO UserSpecificFunctions (UserID, Prefix, Suffix, Color, Permissions) VALUES (@0, @1, @2, @3, @4);", playerID.ToString(), null, suffix, null, null);
					}
					else
					{
						db.Query("UPDATE UserSpecificFunctions SET Suffix=@0 WHERE UserID=@1;", suffix, playerID.ToString());
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

		public Task<bool> SetColorAsync(int playerID, string chatColor)
		{
			return Task.Run(async () =>
			{
				try
				{
					if (await GetPlayerAsync(playerID) == null)
					{
						db.Query("INSERT INTO UserSpecificFunctions (UserID, Prefix, Suffix, Color, Permissions) VALUES (@0, @1, @2, @3, @4);", playerID.ToString(), null, null, chatColor, null);
					}
					else
					{
						db.Query("UPDATE UserSpecificFunctions SET Color=@0 WHERE UserID=@1;", chatColor, playerID.ToString());
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

		public Task<bool> AddPlayerPermissionsAsync(int playerID, List<string> permissions)
		{
			return Task.Run(async () =>
			{
				try
				{
					PlayerInfo player = await GetPlayerAsync(playerID);
					if (player == null)
					{
						db.Query("INSERT INTO UserSpecificFunctions (UserID, Prefix, Suffix, Color, Permissions) VALUES (@0, @1, @2, @3, @4);", playerID.ToString(), null, null, null, string.Join(",", permissions.ToArray()));
					}
					else
					{
						permissions.ForEach(p => player.Permissions.Add(p));
						db.Query("UPDATE UserSpecificFunctions SET Permissions=@0 WHERE UserID=@1;", string.Join(",", player.Permissions.ToArray()), player.UserID.ToString());
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
						db.Query("UPDATE UserSpecificFunctions SET Permissions=@0 WHERE UserID=@1;", player.Permissions.Count > 0 ? string.Join(",", player.Permissions.ToArray()) : null, player.UserID.ToString());
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

		public Task<bool> RemoveUserAsync(int playerID)
		{
			return Task.Run(() => 
			{
				if (db.Query("DELETE FROM UserSpecificFunctions WHERE UserID=@0;", playerID.ToString()) == 1)
					return true;
				else
					return false;
			});
		}

		public Task<bool> ResetDataAsync(int playerID)
		{
			return Task.Run(() => 
			{
				try
				{
					SetPrefixAsync(playerID, null);
					SetSuffixAsync(playerID, null);
					SetColorAsync(playerID, null);
					return true;
				}
				catch (Exception ex)
				{
					TShock.Log.Error(ex.ToString());
					return false;
				}
			});
		}

		public Task<bool> PurgeEntriesAsync()
		{
			return Task.Run(() => 
			{
				try
				{
					foreach (PlayerInfo player in GetPlayers())
					{
						if (player.Prefix == null && player.Suffix == null && player.ChatColor == null && player.Permissions.Count == 0)
						{
							RemoveUserAsync(player.UserID);
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
    }
}
