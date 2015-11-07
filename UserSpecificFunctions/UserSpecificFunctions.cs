using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Hooks;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using UserSpecificFunctions.Extensions;

namespace UserSpecificFunctions
{
    [ApiVersion(1, 22)]
    public class UserSpecificFunctions : TerrariaPlugin
    {
        public override string Name { get { return "UserSpecificFunctions"; } }
        public override string Author { get { return "Professor X"; } }
        public override string Description { get { return ""; } }
        public override Version Version { get { return new Version(1, 0, 0, 0); } }

        private static string Specifier;

        private IDbConnection db;

        private Dictionary<int, USPlayer> players = new Dictionary<int, USPlayer>();

        private Config config = new Config();
        public static string configPath = Path.Combine(TShock.SavePath, "UserSpecificFunctions.json");

        public UserSpecificFunctions(Main game) : base(game)
        {

        }

        #region Initialize/Dispose
        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.ServerChat.Register(this, OnChat);

            GeneralHooks.ReloadEvent += OnReload;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.ServerChat.Deregister(this, OnChat);

                GeneralHooks.ReloadEvent -= OnReload;
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Hooks
        private void OnInitialize(EventArgs args)
        {
            Specifier = TShock.Config.CommandSpecifier;
            InitDB();
            LoadConfig();
            LoadDatabase();
            Commands.ChatCommands.Add(new Command(UserCommand, "us"));
        }

        private void OnChat(ServerChatEventArgs args)
        {
            if (args.Handled)
                return;

            TSPlayer tsplr = TShock.Players[args.Who];

            if (!args.Text.StartsWith(TShock.Config.CommandSpecifier) && !args.Text.StartsWith(TShock.Config.CommandSilentSpecifier)
                && !tsplr.mute && tsplr.IsLoggedIn && players.ContainsKey(tsplr.User.ID))
            {
                string prefix = players[tsplr.User.ID].Prefix == null ? tsplr.Group.Prefix : players[tsplr.User.ID].Prefix;
                string suffix = players[tsplr.User.ID].Suffix == null ? tsplr.Group.Suffix : players[tsplr.User.ID].Suffix;
                Color color = players[tsplr.User.ID].Color == "000,000,000" ? new Color(tsplr.Group.R, tsplr.Group.G, tsplr.Group.B) : new Color(players[tsplr.User.ID].R, players[tsplr.User.ID].G, players[tsplr.User.ID].B);

                TSPlayer.All.SendMessage(string.Format(TShock.Config.ChatFormat, tsplr.Group.Name, prefix, tsplr.Name, suffix, args.Text), color);
                TSPlayer.Server.SendMessage(string.Format(TShock.Config.ChatFormat, tsplr.Group.Name, prefix, tsplr.Name, suffix, args.Text), color);

                args.Handled = true;
            }
        }

        private void OnReload(ReloadEventArgs args)
        {
            LoadConfig();
            LoadDatabase();
        }
        #endregion

        #region Commands
        private void UserCommand(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("Invalid syntax:");
                args.Player.SendErrorMessage("{0}us prefix <player name> <prefix>", Specifier);
                args.Player.SendErrorMessage("{0}us suffix <player name> <suffix>", Specifier);
                args.Player.SendErrorMessage("{0}us color <player name> <r,g,b>", Specifier);
                args.Player.SendErrorMessage("{0}us remove <player name>", Specifier);
                args.Player.SendErrorMessage("{0}us reset <player name>", Specifier);
                args.Player.SendErrorMessage("{0}us purge", Specifier);
                args.Player.SendErrorMessage("{0}us read <player name>", Specifier);
                return;
            }

            switch (args.Parameters[0].ToLower())
            {
                case "prefix":
                    {
                        if (!args.Player.IsLoggedIn && args.Player.RealPlayer)
                        {
                            args.Player.SendErrorMessage("You must be logged in to do that.");
                            return;
                        }
                        else if (!args.Player.Group.HasPermission("us.prefix"))
                        {
                            args.Player.SendErrorMessage("You don't have access to this command.");
                            return;
                        }
                        else if (args.Parameters.Count < 3)
                        {
                            args.Player.SendErrorMessage("Invalid syntax: {0}us prefix <player name> <prefix>", Specifier);
                            return;
                        }
                        else
                        {
                            List<User> users = TShock.Users.GetUsersByName(args.Parameters[1]);
                            if (users.Count == 0)
                            {
                                args.Player.SendErrorMessage("Invalid user.");
                                return;
                            }
                            else if (users.Count > 1)
                            {
                                TShock.Utils.SendMultipleMatchError(args.Player, users.Select(u => u.Name));
                                return;
                            }
                            else if (users[0].Name != args.Player.User.Name && !args.Player.Group.HasPermission("us.setother"))
                            {
                                args.Player.SendErrorMessage("You don't have permission to modify this player's data.");
                                return;
                            }
                            else
                            {
                                args.Parameters.RemoveAt(0);
                                args.Parameters.RemoveAt(0);
                                string prefix = string.Join(" ", args.Parameters.Select(x => x));

                                foreach (string word in config.UnAllowedWords)
                                {
                                    if (prefix.Contains(word))
                                    {
                                        args.Player.SendErrorMessage("You cannot use '{0}' in your prefix.", word);
                                        return;
                                    }
                                }

                                if (prefix.Length > config.PrefixLength)
                                {
                                    args.Player.SendErrorMessage("Your prefix cannot be longer than {0} chars.", config.PrefixLength);
                                    return;
                                }
                                else
                                {
                                    SetPrefix(users[0].ID, prefix);
                                    args.Player.SendSuccessMessage("Set {0} prefix to '{1}'.", users[0].Name.Suffix(), prefix);
                                }
                            }
                        }
                    }
                    return;
                case "suffix":
                    {
                        if (!args.Player.IsLoggedIn && args.Player.RealPlayer)
                        {
                            args.Player.SendErrorMessage("You must be logged in to do that.");
                            return;
                        }
                        else if (!args.Player.Group.HasPermission("us.suffix"))
                        {
                            args.Player.SendErrorMessage("You don't have access to this command.");
                            return;
                        }
                        else if (args.Parameters.Count < 3)
                        {
                            args.Player.SendErrorMessage("Invalid syntax: {0}us suffix <player name> <suffix>", Specifier);
                            return;
                        }
                        else
                        {
                            List<User> users = TShock.Users.GetUsersByName(args.Parameters[1]);
                            if (users.Count == 0)
                            {
                                args.Player.SendErrorMessage("Invalid user.");
                                return;
                            }
                            else if (users.Count > 1)
                            {
                                TShock.Utils.SendMultipleMatchError(args.Player, users.Select(u => u.Name));
                                return;
                            }
                            else if (users[0].Name != args.Player.User.Name && !args.Player.Group.HasPermission("us.setother"))
                            {
                                args.Player.SendErrorMessage("You don't have permission to modify this player's data.");
                                return;
                            }
                            else
                            {
                                args.Parameters.RemoveAt(0);
                                args.Parameters.RemoveAt(0);
                                string suffix = string.Join(" ", args.Parameters.Select(x => x));

                                foreach (string word in config.UnAllowedWords)
                                {
                                    if (suffix.Contains(word))
                                    {
                                        args.Player.SendErrorMessage("You cannot use '{0}' in your suffix.", word);
                                        return;
                                    }
                                }

                                if (suffix.Length > config.SuffixLength)
                                {
                                    args.Player.SendErrorMessage("Your suffix cannot be longer than {0} chars.", config.SuffixLength);
                                    return;
                                }
                                else
                                {
                                    SetSuffix(users[0].ID, suffix);
                                    args.Player.SendSuccessMessage("Set {0} suffix to '{1}'.", users[0].Name.Suffix(), suffix);
                                }
                            }
                        }
                    }
                    return;
                case "color":
                    {
                        if (!args.Player.IsLoggedIn && args.Player.RealPlayer)
                        {
                            args.Player.SendErrorMessage("You must be logged in to do that.");
                            return;
                        }
                        else if (!args.Player.Group.HasPermission("us.color"))
                        {
                            args.Player.SendErrorMessage("You don't have access to this command.");
                            return;
                        }
                        else if (args.Parameters.Count < 3)
                        {
                            args.Player.SendErrorMessage("Invalid syntax: {0}us color <player name> <r,g,b> (values cannot be greater than 255)", Specifier);
                            return;
                        }
                        else
                        {
                            List<User> users = TShock.Users.GetUsersByName(args.Parameters[1]);
                            if (users.Count == 0)
                            {
                                args.Player.SendErrorMessage("Invalid user.");
                                return;
                            }
                            else if (users.Count > 1)
                            {
                                TShock.Utils.SendMultipleMatchError(args.Player, users.Select(u => u.Name));
                                return;
                            }
                            else if (users[0].Name != args.Player.User.Name && !args.Player.Group.HasPermission("us.setother"))
                            {
                                args.Player.SendErrorMessage("You don't have permission to modify this player's data.");
                                return;
                            }
                            else
                            {
                                args.Parameters.RemoveAt(0);
                                args.Parameters.RemoveAt(0);
                                string color = string.Join(" ", args.Parameters.Select(x => x));
                                string[] Color = color.Split(',');

                                byte r, g, b;
                                if (Color.Length == 3 && byte.TryParse(Color[0], out r) && byte.TryParse(Color[1], out g) && byte.TryParse(Color[2], out b))
                                {
                                    SetColor(users[0].ID, color);
                                    args.Player.SendSuccessMessage("Set {0} color to '{1}'.", users[0].Name.Suffix(), color);
                                }
                                else
                                    args.Player.SendErrorMessage("Invalid syntax: {0}us color <player name> <r,g,b> (values cannot be greater than 255)", Specifier);
                            }
                        }
                    }
                    return;
                case "remove":
                    {
                        if (!args.Player.IsLoggedIn && args.Player.RealPlayer)
                        {
                            args.Player.SendErrorMessage("You must be logged in to do that.");
                            return;
                        }
                        else if (args.Parameters.Count != 3)
                        {
                            args.Player.SendErrorMessage("Invalid syntax: {0}us remove <player name> <prefix/suffix/color>", Specifier);
                            return;
                        }
                        else
                        {
                            List<User> users = TShock.Users.GetUsersByName(args.Parameters[1]);
                            if (users.Count == 0)
                            {
                                args.Player.SendErrorMessage("Invalid user.");
                                return;
                            }
                            else if (users.Count > 1)
                            {
                                TShock.Utils.SendMultipleMatchError(args.Player, users.Select(u => u.Name));
                                return;
                            }
                            else if (users[0].Name != args.Player.User.Name && !args.Player.Group.HasPermission("us.setother"))
                            {
                                args.Player.SendErrorMessage("You don't have permission to modify this player's data.");
                                return;
                            }
                            else
                            {
                                switch (args.Parameters[2].ToLower())
                                {
                                    case "prefix":
                                        {
                                            if (!args.Player.Group.HasPermission("us.remove.prefix"))
                                            {
                                                args.Player.SendErrorMessage("You don't have access to this command.");
                                                return;
                                            }
                                            else if (!players.ContainsKey(users[0].ID) || players[users[0].ID].Prefix == null)
                                            {
                                                args.Player.SendErrorMessage("This user doesn't have a prefix to remove.");
                                                return;
                                            }
                                            else
                                            {
                                                SetPrefix(users[0].ID);
                                                args.Player.SendSuccessMessage("Removed {0} prefix.", users[0].Name.Suffix());
                                            }
                                        }
                                        return;
                                    case "suffix":
                                        {
                                            if (!args.Player.Group.HasPermission("us.remove.suffix"))
                                            {
                                                args.Player.SendErrorMessage("You don't have access to this command.");
                                                return;
                                            }
                                            else if (!players.ContainsKey(users[0].ID) || players[users[0].ID].Suffix == null)
                                            {
                                                args.Player.SendErrorMessage("This user doesn't have a suffix to remove.");
                                                return;
                                            }
                                            else
                                            {
                                                SetSuffix(users[0].ID);
                                                args.Player.SendSuccessMessage("Removed {0} suffix.", users[0].Name.Suffix());
                                            }
                                        }
                                        return;
                                    case "color":
                                        {
                                            if (!args.Player.Group.HasPermission("us.remove.color"))
                                            {
                                                args.Player.SendErrorMessage("You don't have access to this command.");
                                                return;
                                            }
                                            else if (!players.ContainsKey(users[0].ID) || players[users[0].ID].Color == "000,000,000")
                                            {
                                                args.Player.SendErrorMessage("This user doesn't have a color to remove.");
                                                return;
                                            }
                                            else
                                            {
                                                SetColor(users[0].ID);
                                                args.Player.SendSuccessMessage("Removed {0} color.", users[0].Name.Suffix());
                                            }
                                        }
                                        return;
                                }
                            }
                        }
                    }
                    return;
                case "reset":
                    {
                        if (!args.Player.Group.HasPermission("us.reset"))
                        {
                            args.Player.SendErrorMessage("You don't have access to this command.");
                            return;
                        }
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage("Invalid syntax: {0}us reset <player name>", Specifier);
                            return;
                        }
                        else
                        {
                            List<User> users = TShock.Users.GetUsersByName(args.Parameters[1]);
                            if (users.Count == 0)
                            {
                                args.Player.SendErrorMessage("Invalid user.");
                                return;
                            }
                            else if (users.Count > 1)
                            {
                                TShock.Utils.SendMultipleMatchError(args.Player, users.Select(u => u.Name));
                                return;
                            }
                            else if (users[0].Name != args.Player.User.Name && !args.Player.Group.HasPermission("us.setother"))
                            {
                                args.Player.SendErrorMessage("You don't have permission to modify this player's data.");
                                return;
                            }
                            else
                            {
                                ResetPlayerData(users[0].ID);
                                args.Player.SendSuccessMessage("{0} data has been reset.", users[0].Name.Suffix());
                            }
                        }
                    }
                    return;
                case "purge":
                    {
                        if (!args.Player.Group.HasPermission("us.purge"))
                        {
                            args.Player.SendErrorMessage("You don't have access to this command.");
                            return;
                        }
                        else
                        {
                            PurgeInvalid();
                            args.Player.SendSuccessMessage("Players without any custom data have been purged.");
                        }
                    }
                    return;
                case "read":
                    {
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage("Invalid syntax: {0}us read <player name>", Specifier);
                            return;
                        }
                        else
                        {
                            List<User> users = TShock.Users.GetUsersByName(args.Parameters[1]);
                            if (users.Count == 0)
                            {
                                args.Player.SendErrorMessage("Invalid user.");
                                return;
                            }
                            else if (users.Count > 1)
                            {
                                TShock.Utils.SendMultipleMatchError(args.Player, users.Select(u => u.Name));
                                return;
                            }
                            else if (users[0].Name != args.Player.User.Name && !args.Player.Group.HasPermission("us.readother"))
                            {
                                args.Player.SendErrorMessage("You don't have permission to read this player's data.");
                                return;
                            }
                            else
                            {
                                args.Player.SendMessage("User: {0}".SFormat(TShock.Users.GetUserByID(users[0].ID).Name), Color.DodgerBlue);
                                args.Player.SendMessage("Prefix: {0}".SFormat(string.IsNullOrWhiteSpace(players[users[0].ID].Prefix) ? "None" : players[users[0].ID].Prefix), Color.DodgerBlue);
                                args.Player.SendMessage("Suffix: {0}".SFormat(string.IsNullOrWhiteSpace(players[users[0].ID].Suffix) ? "None" : players[users[0].ID].Suffix), Color.DodgerBlue);
                                args.Player.SendMessage("Color: {0}".SFormat(players[users[0].ID].Color == "000,000,000" ? "None" : players[users[0].ID].Color), Color.DodgerBlue);
                            }
                        }
                    }
                    return;
                default:
                    {
                        args.Player.SendErrorMessage("Invalid subcommand.");
                    }
                    return;
            }
        }
        #endregion

        #region Database Methods
        private void InitDB()
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
                new SqlColumn("Color", MySqlDbType.Text)));
        }

        private void LoadDatabase()
        {
            players.Clear();

            using (QueryResult reader = db.QueryReader("SELECT * FROM UserSpecificFunctions"))
            {
                while (reader.Read())
                {
                    int UserID = reader.Get<int>("UserID");
                    string Prefix = reader.Get<string>("Prefix");
                    string Suffix = reader.Get<string>("Suffix");
                    string Color = reader.Get<string>("Color");

                    players.Add(UserID, new USPlayer(UserID, Prefix, Suffix, Color));
                }
            }
        }

        private void SetPrefix(int userid, string prefix = null)
        {
            if (!players.ContainsKey(userid))
            {
                players.Add(userid, new USPlayer(userid, prefix, null, "000,000,000"));
                db.Query("INSERT INTO UserSpecificFunctions (UserID, Prefix, Suffix, Color) VALUES (@0, @1, @2, @3);", userid.ToString(), prefix, null, "000,000,000");
            }
            else
            {
                players[userid].Prefix = prefix;
                db.Query("UPDATE UserSpecificFunctions SET Prefix=@0 WHERE UserID=@1;", prefix, userid.ToString());
            }
        }

        private void SetSuffix(int userid, string suffix = null)
        {
            if (!players.ContainsKey(userid))
            {
                players.Add(userid, new USPlayer(userid, null, suffix, "000,000,000"));
                db.Query("INSERT INTO UserSpecificFunctions (UserID, Prefix, Suffix, Color) VALUES (@0, @1, @2, @3);", userid.ToString(), null, suffix, "000,000,000");
            }
            else
            {
                players[userid].Suffix = suffix;
                db.Query("UPDATE UserSpecificFunctions SET Suffix=@0 WHERE UserID=@1;", suffix, userid.ToString());
            }
        }

        private void SetColor(int userid, string color = "000,000,000")
        {
            if (!players.ContainsKey(userid))
            {
                players.Add(userid, new USPlayer(userid, null, null, color));
                db.Query("INSERT INTO UserSpecificFunctions (UserID, Prefix, Suffix, Color) VALUES (@0, @1, @2, @3);", userid.ToString(), null, null, color);
            }
            else
            {
                players[userid].Color = color;
                db.Query("UPDATE UserSpecificFunctions SET Color=@0 WHERE UserID=@1;", color, userid.ToString());
            }
        }

        private void ResetPlayerData(int userid)
        {
            players[userid].Prefix = null;
            players[userid].Suffix = null;
            players[userid].Color = "000,000,000";
            db.Query("UPDATE UserSpecificFunctions SET Prefix=null, Suffix=null, Color=@0 WHERE UserID=@1;", "000,000,000", userid.ToString());
        }

        private void DeleteUser(int userid)
        {
            players.Remove(userid);
            db.Query("DELETE FROM UserSpecificFunctions WHERE UserID=@0;", userid.ToString());
        }

        private void PurgeInvalid()
        {
            foreach (int key in players.Where(x => TShock.Users.GetUserByID(x.Key) == null || players[x.Key].Prefix == null
                && players[x.Key].Suffix == null && players[x.Key].Color == "000,000,000").Select(x => x.Key))
            {
                db.Query("DELETE FROM UserSpecificFunctions WHERE UserID=@0;", key);
            }

            LoadDatabase();
        }
        #endregion

        #region LoadConfig
        private void LoadConfig()
        {
            (config = config.Read(configPath)).Write(configPath);
        }
        #endregion
    }
}
