using System;
using System.Threading.Tasks;
using System.IO;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using UserSpecificFunctions.Extensions;
using DiscordBridge.Chat;

namespace UserSpecificFunctions
{
	[ApiVersion(2, 0)]
	public class UserSpecificFunctions : TerrariaPlugin
	{
		public override string Name { get { return "User Specific Functions"; } }
		public override string Author { get { return "Professor X"; } }
		public override string Description { get { return ""; } }
		public override Version Version { get { return new Version(1, 4, 7, 0); } }

		public Config USFConfig = new Config();
		public Database USFDatabase = new Database();

		public static UserSpecificFunctions Instance;
		public UserSpecificFunctions(Main game) : base(game)
		{
			Instance = this;
		}

		#region Initialize/Dispose
		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);

			ChatHandler.PlayerChatting += OnChat;
			PlayerHooks.PlayerPermission += OnPlayerPermission;
			GeneralHooks.ReloadEvent += OnReload;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);

				ChatHandler.PlayerChatting -= OnChat;
				PlayerHooks.PlayerPermission -= OnPlayerPermission;
				GeneralHooks.ReloadEvent -= OnReload;
			}
			base.Dispose(disposing);
		}
		#endregion

		#region Hooks
		/// <summary>
		/// Internal hook, fired when the server is set up.
		/// </summary>
		/// <param name="args">The <see cref="EventArgs"/> object.</param>
		private void OnInitialize(EventArgs args)
		{
			LoadConfig();
			USFDatabase.DBConnect();

			Commands.ChatCommands.RemoveAll(c => c.HasAlias("help"));

			Commands.ChatCommands.Add(new Command(USFCommands.Help, "help") { HelpText = "Lists commands or gives help on them." });
			Commands.ChatCommands.Add(new Command(USFCommands.USFMain, "us"));
			Commands.ChatCommands.Add(new Command(Permissions.setPermissions, USFCommands.USFPermission, "permission"));
		}

		/// <summary>
		/// Internal hook, fired when a chat message is sent.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="args">The event args.</param>
		private void OnChat(object sender, PlayerChattingEventArgs args)
		{
			// Ensure the player has modified data.
			if (args.Player.GetPlayerInfo() == null)
			{
				return;
			}

			// Change prefix if the player has a custom prefix
			if (!String.IsNullOrWhiteSpace(args.Player.GetPlayerInfo().Prefix))
			{
				args.Message.Prefixes.RemoveAll(s => s.Text == args.Player.Group.Prefix);
				args.Message.Prefix(args.Player.GetPlayerInfo().Prefix);
			}

			// Change suffix if the player has a custom suffix
			if (!String.IsNullOrWhiteSpace(args.Player.GetPlayerInfo().Suffix))
			{
				args.Message.Suffixes.RemoveAll(s => s.Text == args.Player.Group.Suffix);
				args.Message.Suffix(args.Player.GetPlayerInfo().Suffix);
			}

			args.ColorFormatters.Add("USF",String.IsNullOrWhiteSpace(args.Player.GetPlayerInfo().ChatColor)
				? null : args.Player.GetPlayerInfo().ChatColor?.ToColor());

			// Colorize the message if the player has a custom color
			if (args.ColorFormatters["USF"].HasValue)
				args.Message.Colorize(args.ColorFormatters["USF"]);
		}

		/// <summary>
		/// Internal hook, fired whenever <see cref="TSPlayer.HasPermission(string)"/> is invoked.
		/// </summary>
		/// <param name="args">The <see cref="PlayerPermissionEventArgs"/> object.</param>
		private void OnPlayerPermission(PlayerPermissionEventArgs args)
		{
			// Return if the event was already handled by another plugin.
			if (args.Handled)
			{
				return;
			}

			// Ensure the player is not null and has special permissions.
			if (args.Player == null || args.Player.GetPlayerInfo() == null)
			{
				return;
			}

			// Handle the event.
			args.Handled = args.Player.GetPlayerInfo().HasPermission(args.Permission);
		}

		/// <summary>
		/// Internal hook, fired whenever a player executes /reload.
		/// </summary>
		/// <param name="args">The <see cref="ReloadEventArgs"/> object.</param>
		private void OnReload(ReloadEventArgs args)
		{
			LoadConfig();
			Task.Run(() => USFDatabase.LoadPlayerData());
		}
		#endregion

		#region LoadConfig
		/// <summary>
		/// Internal method, reloads the configuration file.
		/// </summary>
		internal void LoadConfig()
		{
			string configPath = Path.Combine(TShock.SavePath, "UserSpecificFunctions.json");
			USFConfig = Config.TryRead(configPath);
		}
		#endregion
	}
}
