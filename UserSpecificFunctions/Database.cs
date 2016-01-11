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
using static UserSpecificFunctions.UserSpecificFunctions;

namespace UserSpecificFunctions
{
    public static class Database
    {
        public static IDbConnection db;

        public static void InitDB()
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

        internal static void LoadDatabase()
        {
            players.Clear();

            using (QueryResult reader = db.QueryReader("SELECT * FROM UserSpecificFunctions"))
            {
                while (reader.Read())
                {
                    int userID = reader.Get<int>("UserID");
                    string prefix = reader.Get<string>("Prefix");
                    string suffix = reader.Get<string>("Suffix");
                    string color = reader.Get<string>("Color");
                    List<string> playerPermissions = new List<string>();

                    string[] permissions = reader.Get<string>("Permissions").Split(',');

                    foreach (string permission in permissions)
                    {
                        if (permission != null)
                            playerPermissions.Add(permission);
                    }

                    players.Add(userID, new USFPlayer(userID, prefix, suffix, color, playerPermissions));
                }
            }
        }

        internal static void SetPrefix(int userid, string prefix = null)
        {
            if (!players.ContainsKey(userid))
            {
                players.Add(userid, new USFPlayer(userid, prefix, null, "000,000,000", new List<string> { "" }));
                db.Query("INSERT INTO UserSpecificFunctions (UserID, Prefix, Suffix, Color, Permissions) VALUES (@0, @1, @2, @3, @4);", userid.ToString(), prefix, null, "000,000,000", "");
            }
            else
            {
                players[userid].Prefix = prefix;
                db.Query("UPDATE UserSpecificFunctions SET Prefix=@0 WHERE UserID=@1;", prefix, userid.ToString());
            }
        }

        internal static void SetSuffix(int userid, string suffix = null)
        {
            if (!players.ContainsKey(userid))
            {
                players.Add(userid, new USFPlayer(userid, null, suffix, "000,000,000", new List<string> { "" }));
                db.Query("INSERT INTO UserSpecificFunctions (UserID, Prefix, Suffix, Color, Permissions) VALUES (@0, @1, @2, @3, @4);", userid.ToString(), null, suffix, "000,000,000", "");
            }
            else
            {
                players[userid].Suffix = suffix;
                db.Query("UPDATE UserSpecificFunctions SET Suffix=@0 WHERE UserID=@1;", suffix, userid.ToString());
            }
        }

        internal static void SetColor(int userid, string color = "000,000,000")
        {
            if (!players.ContainsKey(userid))
            {
                players.Add(userid, new USFPlayer(userid, null, null, color, new List<string> { "" }));
                db.Query("INSERT INTO UserSpecificFunctions (UserID, Prefix, Suffix, Color, Permissions) VALUES (@0, @1, @2, @3, @4);", userid.ToString(), null, null, color, "");
            }
            else
            {
                players[userid].ChatColor = color;
                db.Query("UPDATE UserSpecificFunctions SET Color=@0 WHERE UserID=@1;", color, userid.ToString());
            }
        }

        //internal static void ModifyPermissions(int userid, string permission, bool remove = false)
        //{
        //    if (!remove)
        //    {
        //        if (!players.ContainsKey(userid))
        //        {
        //            players.Add(userid, new USFPlayer(userid, null, null, "000,000,000", new List<string> { permission }));
        //            db.Query("INSERT INTO UserSpecificFunctions (UserID, Prefix, Suffix, Color, Permissions) VALUES (@0, @1, @2, @3, @4);", userid.ToString(), null, null, "000,000,000", permission);
        //        }
        //        else
        //        {
        //            players[userid].AddPermission(permission);
        //            string perms = string.Join(",", players[userid].Permissions.ToArray());
        //            db.Query("UPDATE UserSpecificFunctions SET Permissions=@0 WHERE UserID=@1;", perms, userid.ToString());
        //        }
        //    }
        //    else
        //    {
        //        players[userid].RemovePermission(permission);
        //        string perms = string.Join(",", players[userid].Permissions.ToArray());
        //        db.Query("UPDATE UserSpecificFunctions SET Permissions=@0 WHERE UserID=@1;", perms, userid.ToString());
        //    }
        //}

        internal static void ResetPlayerData(int userid)
        {
            SetPrefix(userid);
            SetSuffix(userid);
            SetColor(userid);
        }

        internal static void DeleteUser(int userid)
        {
            players.Remove(userid);
            db.Query("DELETE FROM UserSpecificFunctions WHERE UserID=@0;", userid.ToString());
        }

        internal static void PurgeInvalid()
        {
            foreach (int key in players.Where(x => TShock.Users.GetUserByID(x.Key) == null || players[x.Key].Prefix == null
                && players[x.Key].Suffix == null && players[x.Key].ChatColor == "000,000,000").Select(x => x.Key))
            {
                db.Query("DELETE FROM UserSpecificFunctions WHERE UserID=@0;", key);
            }

            LoadDatabase();
        }
    }
}
