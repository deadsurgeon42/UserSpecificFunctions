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
	[ApiVersion(1, 23)]
	public class UserSpecificFunctions : TerrariaPlugin
	{
		public override string Name { get { return "User Specific Functions"; } }
		public override string Author { get { return "Professor X"; } }
		public override string Description { get { return ""; } }
		public override Version Version { get { return new Version(1, 3, 2, 0); } }

		public static Config USFConfig = new Config();
		public static Database USFDatabase = new Database();

		public UserSpecificFunctions(Main game) : base(game)
		{

		}

		#region Initialize/Dispose
		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
			ServerApi.Hooks.ServerChat.Register(this, OnChat);

			PlayerHooks.PlayerPermission += OnPlayerPermission;
			GeneralHooks.ReloadEvent += OnReload;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
				ServerApi.Hooks.ServerChat.Deregister(this, OnChat);

				PlayerHooks.PlayerPermission -= OnPlayerPermission;
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
			if (tsplr == null || tsplr.GetPlayerInfo() == null)
				return;

			if (!tsplr.HasPermission(TShockAPI.Permissions.canchat) || tsplr.mute)
				return;

			if (!args.Text.StartsWith(TShock.Config.CommandSpecifier) && !args.Text.StartsWith(TShock.Config.CommandSilentSpecifier))
			{
				string prefix = tsplr.GetPlayerInfo().Prefix?.ToString() ?? tsplr.Group.Prefix;
				string suffix = tsplr.GetPlayerInfo().Suffix?.ToString() ?? tsplr.Group.Suffix;
				Color chatColor = tsplr.GetPlayerInfo().ChatColor?.ToColor() ?? tsplr.Group.ChatColor.ToColor();

				if (!TShock.Config.EnableChatAboveHeads)
				{
					string message = string.Format(TShock.Config.ChatFormat, tsplr.Group.Name, prefix, tsplr.Name, suffix, args.Text);
					TSPlayer.All.SendMessage(message, chatColor);
					TSPlayer.Server.SendMessage(message, chatColor);
					TShock.Log.Info("Broadcast: {0}", message);

					args.Handled = true;
				}
				else
				{
					Player player = Main.player[args.Who];
					string name = player.name;
					player.name = string.Format(TShock.Config.ChatAboveHeadsFormat, tsplr.Group.Name, prefix, tsplr.Name, suffix);
					NetMessage.SendData((int)PacketTypes.PlayerInfo, -1, -1, player.name, args.Who, 0, 0, 0, 0);
					player.name = name;
					var text = args.Text;
					NetMessage.SendData((int)PacketTypes.ChatText, -1, args.Who, text, args.Who, chatColor.R, chatColor.G, chatColor.B);
					NetMessage.SendData((int)PacketTypes.PlayerInfo, -1, -1, name, args.Who, 0, 0, 0, 0);

					string message = string.Format("<{0}> {1}", string.Format(TShock.Config.ChatAboveHeadsFormat, tsplr.Group.Name, prefix, tsplr.Name, suffix), text);
					tsplr.SendMessage(message, chatColor);
					TSPlayer.Server.SendMessage(message, chatColor);
					TShock.Log.Info("Broadcast: {0}", message);

					args.Handled = true;
				}
			}
			else
			{
				if (!string.IsNullOrWhiteSpace(args.Text.Substring(1)))
				{
					try
					{
						args.Handled = Utils.ExecuteComamnd(tsplr, args.Text);
					}
					catch (Exception ex)
					{
						TShock.Log.ConsoleError("An exception occured executing a command.");
						TShock.Log.Error(ex.ToString());
					}
				}
			}
		}

		private void OnPlayerPermission(PlayerPermissionEventArgs args)
		{
			if (args.Handled)
				return;

			if (args.Player == null || args.Player.GetPlayerInfo() == null)
				return;

			args.Handled = args.Player.GetPlayerInfo().HasPermission(args.Permission);
		}

		private void OnReload(ReloadEventArgs args)
		{
			LoadConfig();
		}
		#endregion

		#region LoadConfig
		internal static void LoadConfig()
		{
			string configPath = Path.Combine(TShock.SavePath, "UserSpecificFunctions.json");
			USFConfig = Config.Read(configPath);
		}
		#endregion
	}
}
