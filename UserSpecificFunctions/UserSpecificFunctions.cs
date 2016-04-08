using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using UserSpecificFunctions.Extensions;

namespace UserSpecificFunctions
{
    [ApiVersion(1, 22)]
    public class UserSpecificFunctions : TerrariaPlugin
    {
        public override string Name { get { return "User Specific Functions"; } }
        public override string Author { get { return "Professor X"; } }
        public override string Description { get { return ""; } }
        public override Version Version { get { return new Version(1, 0, 0, 0); } }

        internal static Config config = new Config();
        internal static Dictionary<int, USFPlayer> players = new Dictionary<int, USFPlayer>();

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
            LoadConfig();
            Database.DBConnect();
            Database.LoadDatabase();

            Commands.ChatCommands.RemoveAll(c => c.HasAlias("help"));

            Commands.ChatCommands.Add(new Command(USFCommands.Help, "help") { HelpText = "Lists commands or gives help on them." });
            Commands.ChatCommands.Add(new Command(USFCommands.USFMain, "us"));
            Commands.ChatCommands.Add(new Command(Permissions.setPermissions, USFCommands.USFPermission, "permission"));
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
                Color color = players[tsplr.User.ID].ChatColor == "000,000,000" ? new Color(tsplr.Group.R, tsplr.Group.G, tsplr.Group.B) : players[tsplr.User.ID].ChatColor.ToColor();

                if (!TShock.Config.EnableChatAboveHeads)
                {
                    string msg = string.Format(TShock.Config.ChatFormat, tsplr.Group.Name, prefix, tsplr.Name, suffix, args.Text);
                    TSPlayer.All.SendMessage(msg, color);
                    TSPlayer.Server.SendMessage(msg, color);
                    TShock.Log.Info("Broadcast: {0}", msg);

                    args.Handled = true;
                }
                else
                {
                    Player ply = Main.player[args.Who];
                    string name = ply.name;
                    ply.name = string.Format(TShock.Config.ChatAboveHeadsFormat, tsplr.Group.Name, prefix, tsplr.Name, suffix);
                    NetMessage.SendData((int)PacketTypes.PlayerInfo, -1, -1, ply.name, args.Who, 0, 0, 0, 0);
                    ply.name = name;
                    var text = args.Text;
                    NetMessage.SendData((int)PacketTypes.ChatText, -1, args.Who, text, args.Who, color.R, color.G, color.B);
                    NetMessage.SendData((int)PacketTypes.PlayerInfo, -1, -1, name, args.Who, 0, 0, 0, 0);

                    string msg = String.Format("<{0}> {1}", String.Format(TShock.Config.ChatAboveHeadsFormat, tsplr.Group.Name, prefix, tsplr.Name, suffix), text);
                    tsplr.SendMessage(msg, color);
                    TSPlayer.Server.SendMessage(msg, color);
                    TShock.Log.Info("Broadcast: {0}", msg);

                    args.Handled = true;
                }
            }

            else if (args.Text.StartsWith(TShock.Config.CommandSpecifier) || args.Text.StartsWith(TShock.Config.CommandSilentSpecifier)
                && !string.IsNullOrWhiteSpace(args.Text.Substring(1)))
            {
                try
                {
                    if (tsplr.User != null && players.ContainsKey(tsplr.User.ID))
                    {
                        args.Handled = Utils.ExecuteComamnd(tsplr, args.Text);
                    }
                    else
                    {
                        args.Handled = Commands.HandleCommand(tsplr, args.Text);
                    }
                }
                catch (Exception ex)
                {
                    TShock.Log.ConsoleError("An exception occured executing a command.");
                    TShock.Log.Error(ex.ToString());
                }
            }
        }

        private void OnReload(ReloadEventArgs args)
        {
            LoadConfig();
            Database.LoadDatabase();
        }
        #endregion

        #region LoadConfig
        internal static void LoadConfig()
        {
            string configPath = Path.Combine(TShock.SavePath, "UserSpecificFunctions.json");
            (config = config.Read(configPath)).Write(configPath);
        }
        #endregion
    }
}
