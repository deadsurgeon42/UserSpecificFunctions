using System;
using System.Collections.Generic;
using System.Linq;
using TShockAPI;
using TShockAPI.Hooks;

namespace UserSpecificFunctions.Extensions
{
	/// <summary>
	/// Provides extension methods for the <see cref="TSPlayer"/> class.
	/// </summary>
	public static class TSPlayerExtensions
	{
		/// <summary>
		/// Pulls the player's USF information from the database.
		/// </summary>
		/// <param name="tsPlayer">The <see cref="TSPlayer"/> object.</param>
		/// <returns>A <see cref="PlayerInfo"/> object.</returns>
		public static PlayerInfo GetPlayerInfo(this TSPlayer tsPlayer)
		{
			if (!tsPlayer.IsLoggedIn || tsPlayer.User == null)
				return null;

			return UserSpecificFunctions.Instance.USFDatabase.GetPlayer(tsPlayer.User.ID);
		}

		/// <summary>
		/// Invokes a command delegate.
		/// </summary>
		/// <param name="command">The command.</param>
		/// <param name="msg">The command text.</param>
		/// <param name="tsPlayer">The command issuer.</param>
		/// <param name="args">The parsed text.</param>
		/// <returns>True or false.</returns>
		private static bool RunCommand(Command command, string msg, TSPlayer tsPlayer, List<string> args)
		{
			try
			{
				CommandDelegate commandD = command.CommandDelegate;
				commandD(new CommandArgs(msg, tsPlayer, args));
			}
			catch (Exception ex)
			{
				tsPlayer.SendErrorMessage("Command failed, check logs for more details.");
				TShock.Log.Error(ex.ToString());
			}

			return true;
		}

		/// <summary>
		/// Executes a command. Checks for specific permissions.
		/// </summary>
		/// <param name="player">The command issuer.</param>
		/// <param name="text">The command text.</param>
		/// <returns>True or false.</returns>
		public static bool ExecuteCommand(this TSPlayer player, string text)
		{
			string cmdText = text.Remove(0, 1);
			string cmdPrefix = text[0].ToString();
			bool silent = false;

			if (cmdPrefix == TShock.Config.CommandSilentSpecifier)
				silent = true;

			var args = typeof(Commands).CallMethod<List<string>>("ParseParameters", cmdText);
			if (args.Count < 1)
				return false;

			string cmdName = args[0].ToLower();
			args.RemoveAt(0);

			IEnumerable<Command> cmds = Commands.ChatCommands.FindAll(c => c.HasAlias(cmdName));

			if (PlayerHooks.OnPlayerCommand(player, cmdName, cmdText, args, ref cmds, cmdPrefix))
				return true;

			if (cmds.Count() == 0)
			{
				if (player.AwaitingResponse.ContainsKey(cmdName))
				{
					Action<CommandArgs> call = player.AwaitingResponse[cmdName];
					player.AwaitingResponse.Remove(cmdName);
					call(new CommandArgs(cmdText, player, args));
					return true;
				}
				player.SendErrorMessage("Invalid command entered. Type {0}help for a list of valid commands.", TShock.Config.CommandSpecifier);
				return true;
			}
			foreach (Command command in cmds)
			{
				if (!command.AllowServer && !player.RealPlayer)
				{
					player.SendErrorMessage("You must use this command in-game.");
				}
				else if ((!command.CanRun(player) && !player.GetPlayerInfo().HasPermission(command.Permissions.Any() ? command.Permissions[0] : null)) || (command.CanRun(player) && player.GetPlayerInfo().Permissions.Negated(command.Permissions.Any() ? command.Permissions[0] : null)))
				{
					TShock.Utils.SendLogs(string.Format("{0} tried to execute {1}{2}.", player.Name, TShock.Config.CommandSpecifier, cmdText), Color.PaleVioletRed, player);
					player.SendErrorMessage("You do not have access to this command.");
				}
				else
				{
					if (command.DoLog)
						TShock.Utils.SendLogs(string.Format("{0} executed: {1}{2}.", player.Name, silent ? TShock.Config.CommandSilentSpecifier : TShock.Config.CommandSpecifier, cmdText), Color.PaleVioletRed, player);
					RunCommand(command, cmdText, player, args);
				}
			}

			return true;
		}
	}
}
