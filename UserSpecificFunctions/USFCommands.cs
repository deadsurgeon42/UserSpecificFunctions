using System.Collections.Generic;
using System.Linq;
using TShockAPI;
using TShockAPI.DB;
using UserSpecificFunctions.Extensions;
using static UserSpecificFunctions.UserSpecificFunctions;

namespace UserSpecificFunctions
{
	public static class USFCommands
	{
		public static void Help(CommandArgs args)
		{
			if (args.Parameters.Count > 1)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}help <command/page>", TShock.Config.CommandSpecifier);
				return;
			}

			int pageNumber;
			if (args.Parameters.Count == 0 || int.TryParse(args.Parameters[0], out pageNumber))
			{
				if (!PaginationTools.TryParsePageNumber(args.Parameters, 0, args.Player, out pageNumber))
				{
					return;
				}

				IEnumerable<string> cmdNames = from cmd in Commands.ChatCommands
											   where cmd.CanRun(args.Player) && (!args.Player.GetPlayerInfo()?.Permissions.Negated(cmd.Permissions.Any() ? cmd.Permissions[0] : null) ?? true)
											   || (args.Player.GetPlayerInfo() != null && args.Player.GetPlayerInfo().HasPermission(cmd.Permissions.Any() ? cmd.Permissions[0] : null) && (cmd.Name != "auth" || TShock.AuthToken != 0))
											   orderby cmd.Name
											   select TShock.Config.CommandSpecifier + cmd.Name;

				PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(cmdNames),
					new PaginationTools.Settings
					{
						HeaderFormat = "Commands ({0}/{1}):",
						FooterFormat = "Type {0}help {{0}} for more.".SFormat(TShock.Config.CommandSpecifier)
					});
			}
			else
			{
				string commandName = args.Parameters[0].ToLower();
				if (commandName.StartsWith(TShock.Config.CommandSpecifier))
				{
					commandName = commandName.Substring(1);
				}

				Command command = Commands.ChatCommands.Find(c => c.Names.Contains(commandName));
				if (command == null)
				{
					args.Player.SendErrorMessage("Invalid command.");
					return;
				}
				if (!command.CanRun(args.Player) && !args.Player.HasPermission(command.Permissions[0]))
				{
					args.Player.SendErrorMessage("You do not have access to this command.");
					return;
				}

				args.Player.SendSuccessMessage("{0}{1} help: ", TShock.Config.CommandSpecifier, command.Name);
				if (command.HelpDesc == null)
				{
					args.Player.SendInfoMessage(command.HelpText);
					return;
				}
				foreach (string line in command.HelpDesc)
				{
					args.Player.SendInfoMessage(line);
				}
			}
		}

		public static void USFMain(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage("Invalid syntax:");
				args.Player.SendErrorMessage("{0}us prefix <player name> <prefix>", TShock.Config.CommandSpecifier);
				args.Player.SendErrorMessage("{0}us suffix <player name> <suffix>", TShock.Config.CommandSpecifier);
				args.Player.SendErrorMessage("{0}us color <player name> <r,g,b>", TShock.Config.CommandSpecifier);
				args.Player.SendErrorMessage("{0}us remove <player name>", TShock.Config.CommandSpecifier);
				args.Player.SendErrorMessage("{0}us reset <player name>", TShock.Config.CommandSpecifier);
				args.Player.SendErrorMessage("{0}us purge", TShock.Config.CommandSpecifier);
				args.Player.SendErrorMessage("{0}us read <player name>", TShock.Config.CommandSpecifier);
				return;
			}

			switch (args.Parameters[0].ToLower())
			{
				case "prefix":
					{
						SetPlayerPrefix(args);
					}
					break;
				case "suffix":
					{
						SetPlayerSuffix(args);
					}
					break;
				case "color":
					{
						SetPlayerColor(args);
					}
					break;
				case "remove":
					{
						RemoveOption(args);
					}
					break;
				case "reset":
					{
						ResetPlayerData(args);
					}
					break;
				case "purge":
					{
						PurgeDatabase(args);
					}
					break;
				case "read":
					{
						ReadPlayerData(args);
					}
					break;
			}
		}

		public static void USFPermission(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage("Invalid syntax:");
				args.Player.SendErrorMessage("{0}permission add <player name> <permission>", TShock.Config.CommandSpecifier);
				args.Player.SendErrorMessage("{0}permission delete <player name> <permission>", TShock.Config.CommandSpecifier);
				args.Player.SendErrorMessage("{0}permission list <player name> [page]", TShock.Config.CommandSpecifier);
				return;
			}

			switch (args.Parameters[0].ToLower())
			{
				case "add":
					{
						AddPlayerPermissions(args);
					}
					return;
				case "del":
				case "rem":
				case "delete":
				case "remove":
					{
						RemovePlayerPermissions(args);
					}
					return;
				case "list":
					{
						ListPlayerPermissions(args);
					}
					return;
			}
		}

		#region SetPrefix
		private static async void SetPlayerPrefix(CommandArgs args)
		{
			if (!args.Player.IsLoggedIn && args.Player.RealPlayer)
			{
				args.Player.SendErrorMessage("You must be logged in to do that.");
				return;
			}

			if (!args.Player.HasPermission(Permissions.setPrefix))
			{
				args.Player.SendErrorMessage("You don't have access to this command.");
				return;
			}

			if (args.Parameters.Count < 3)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}us prefix <player name> <prefix>", TShock.Config.CommandSpecifier);
				return;
			}

			List<User> users = TShock.Users.GetUsersByNameEx(args.Parameters[1]);
			if (users.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid player!");
				return;
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(args.Player, users.Select(p => p.Name));
				return;
			}
			else if (users[0].Name != args.Player.User.Name && !args.Player.HasPermission(Permissions.setOther))
			{
				args.Player.SendErrorMessage("You cannot modify this player's prefix!");
				return;
			}
			else
			{
				args.Parameters.RemoveRange(0, 2);
				string prefix = string.Join(" ", args.Parameters.Select(p => p));

				foreach (string word in Instance.USFConfig.UnAllowedWords)
				{
					if (prefix.ToLower().Contains(word.ToLower()))
					{
						args.Player.SendErrorMessage("Your prefix cannot contain the word: '{0}'", word);
						return;
					}
				}

				if (prefix.Length > Instance.USFConfig.PrefixLength)
				{
					args.Player.SendErrorMessage("Your prefix cannot be longer than {0} characters.", Instance.USFConfig.PrefixLength);
					return;
				}
				else
				{
					if (await Instance.USFDatabase.SetPrefixAsync(users[0].ID, prefix))
					{
						args.Player.SendSuccessMessage("Set {0} prefix to: '{1}'", users[0].Name.SuffixPossesion(), prefix);
					}
					else
					{
						args.Player.SendErrorMessage("Something went wrong while trying to change the player's prefix. Please contact an administrator.");
					}
				}
			}
		}
		#endregion

		#region SetSuffix
		private static async void SetPlayerSuffix(CommandArgs args)
		{
			if (!args.Player.IsLoggedIn && args.Player.RealPlayer)
			{
				args.Player.SendErrorMessage("You must be logged in to do that.");
				return;
			}

			if (!args.Player.HasPermission(Permissions.setSuffix))
			{
				args.Player.SendErrorMessage("You don't have access to this command.");
				return;
			}

			if (args.Parameters.Count < 3)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}us suffix <player name> <suffix>", TShock.Config.CommandSpecifier);
				return;
			}

			List<User> users = TShock.Users.GetUsersByNameEx(args.Parameters[1]);
			if (users.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid player!");
				return;
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(args.Player, users.Select(p => p.Name));
				return;
			}
			else if (users[0].Name != args.Player.User.Name && !args.Player.HasPermission(Permissions.setOther))
			{
				args.Player.SendErrorMessage("You cannot modify this player's suffix!");
				return;
			}
			else
			{
				args.Parameters.RemoveRange(0, 2);
				string suffix = string.Join(" ", args.Parameters.Select(p => p));

				foreach (string word in Instance.USFConfig.UnAllowedWords)
				{
					if (suffix.ToLower().Contains(word.ToLower()))
					{
						args.Player.SendErrorMessage("Your suffix cannot contain the word: '{0}'", word);
						return;
					}
				}

				if (suffix.Length > Instance.USFConfig.PrefixLength)
				{
					args.Player.SendErrorMessage("Your prefix cannot be longer than {0} characters.", Instance.USFConfig.SuffixLength);
					return;
				}
				else
				{
					if (await Instance.USFDatabase.SetSuffixAsync(users[0].ID, suffix))
					{
						args.Player.SendSuccessMessage("Set {0} suffix to: '{1}'", users[0].Name.SuffixPossesion(), suffix);
					}
					else
					{
						args.Player.SendErrorMessage("Something went wrong while trying to change the player's suffix. Please contact an administrator.");
					}
				}
			}
		}
		#endregion

		#region SetColor
		private static async void SetPlayerColor(CommandArgs args)
		{
			if (!args.Player.IsLoggedIn && args.Player.RealPlayer)
			{
				args.Player.SendErrorMessage("You must be logged in to do that.");
				return;
			}

			if (!args.Player.HasPermission(Permissions.setColor))
			{
				args.Player.SendErrorMessage("You don't have access to this command.");
				return;
			}

			if (args.Parameters.Count != 3)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}us color <player name> <rrr,ggg,bbb>", TShock.Config.CommandSpecifier);
				return;
			}

			List<User> users = TShock.Users.GetUsersByNameEx(args.Parameters[1]);
			if (users.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid player!");
				return;
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(args.Player, users.Select(p => p.Name));
				return;
			}
			else if (users[0].Name != args.Player.User.Name && !args.Player.HasPermission(Permissions.setOther))
			{
				args.Player.SendErrorMessage("You cannot modify this player's chat color!");
				return;
			}
			else
			{
				byte r, g, b;
				string[] color = args.Parameters[2].Split(',');
				if (color.Length == 3 && byte.TryParse(color[0], out r) && byte.TryParse(color[1], out g) && byte.TryParse(color[2], out b))
				{
					if (await Instance.USFDatabase.SetColorAsync(users[0].ID, args.Parameters[2]))
					{
						args.Player.SendSuccessMessage("Set {0} chat color to: '{1}'", users[0].Name.SuffixPossesion(), args.Parameters[2]);
					}
					else
					{
						args.Player.SendErrorMessage("Something went wrong while trying to change the player's chat color. Please contact an administrator.");
					}
				}
				else
					args.Player.SendErrorMessage("Invalid color format!");
			}
		}
		#endregion

		#region RemoveOption
		private static async void RemoveOption(CommandArgs args)
		{
			if (!args.Player.IsLoggedIn && args.Player.RealPlayer)
			{
				args.Player.SendErrorMessage("You must be logged in to do that.");
				return;
			}

			if (args.Parameters.Count != 3)
			{
				args.Player.SendErrorMessage("Invalid syntax: {0}us remove <player name> <prefix/suffix/color>", TShock.Config.CommandSpecifier);
				return;
			}

			List<User> users = TShock.Users.GetUsersByNameEx(args.Parameters[1]);
			if (users.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid player!");
				return;
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(args.Player, users.Select(p => p.Name));
				return;
			}
			else if (users[0].Name != args.Player.User.Name && !args.Player.Group.HasPermission(Permissions.setOther))
			{
				args.Player.SendErrorMessage("You can't modify this player's data.");
				return;
			}
			else
			{
				PlayerInfo player = Instance.USFDatabase.GetPlayer(users[0].ID);
				switch (args.Parameters[2].ToLower())
				{
					case "prefix":
						{
							if (!args.Player.HasPermission(Permissions.removePrefix))
							{
								args.Player.SendErrorMessage("You don't have access to this command.");
								return;
							}
							else if (player == null || player.Prefix == null)
							{
								args.Player.SendErrorMessage("This user does not have a prefix to remove.");
								return;
							}
							else
							{
								if (await Instance.USFDatabase.SetPrefixAsync(users[0].ID, null))
								{
									args.Player.SendSuccessMessage("Removed {0} prefix.", users[0].Name.SuffixPossesion());
								}
								else
								{
									args.Player.SendErrorMessage("Something went wrong while trying to remove the player's prefix. Please contact an administrator.");
								}
							}
						}
						break;
					case "suffix":
						{
							if (!args.Player.HasPermission(Permissions.removeSuffix))
							{
								args.Player.SendErrorMessage("You don't have access to this command.");
								return;
							}
							else if (player == null || player.Suffix == null)
							{
								args.Player.SendErrorMessage("This user does not have a suffix to remove.");
								return;
							}
							else
							{
								if (await Instance.USFDatabase.SetSuffixAsync(users[0].ID, null))
								{
									args.Player.SendSuccessMessage("Removed {0} suffix.", users[0].Name.SuffixPossesion());
								}
								else
								{
									args.Player.SendErrorMessage("Something went wrong while trying to remove the player's suffix. Please contact an administrator.");
								}
							}
						}
						break;
					case "color":
						{
							if (!args.Player.HasPermission(Permissions.removeColor))
							{
								args.Player.SendErrorMessage("You don't have access to this command.");
								return;
							}
							else if (player == null || player.ChatColor == null)
							{
								args.Player.SendErrorMessage("This user does not have a chat color to remove.");
								return;
							}
							else
							{
								if (await Instance.USFDatabase.SetColorAsync(users[0].ID, null))
								{
									args.Player.SendSuccessMessage("Removed {0} color.", users[0].Name.SuffixPossesion());
								}
								else
								{
									args.Player.SendErrorMessage("Something went wrong while trying to remove the player's chat color. Please contact an administrator.");
								}
							}
						}
						break;
				}
			}
		}
		#endregion

		#region ResetData
		private static async void ResetPlayerData(CommandArgs args)
		{
			if (!args.Player.IsLoggedIn && args.Player.RealPlayer)
			{
				args.Player.SendErrorMessage("You must be logged in to do that.");
				return;
			}

			if (!args.Player.HasPermission(Permissions.resetStats))
			{
				args.Player.SendErrorMessage("You don't have acces to this command.");
				return;
			}

			if (args.Parameters.Count != 2)
			{
				args.Player.SendErrorMessage("{0}us reset <player name>", TShock.Config.CommandSpecifier);
				return;
			}

			List<User> users = TShock.Users.GetUsersByNameEx(args.Parameters[1]);
			if (users.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid player!");
				return;
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(args.Player, users.Select(p => p.Name));
				return;
			}
			else if (Instance.USFDatabase.GetPlayer(users[0].ID) == null)
			{
				args.Player.SendErrorMessage("This player has no custom data to rest.");
				return;
			}
			else if (users[0].Name != args.Player.User.Name && !args.Player.HasPermission(Permissions.setOther))
			{
				args.Player.SendErrorMessage("You don't have access to this command.");
				return;
			}
			else
			{
				if (await Instance.USFDatabase.ResetDataAsync(users[0].ID))
				{
					args.Player.SendSuccessMessage("Reset {0} data successfully.", users[0].Name.SuffixPossesion());
				}
				else
				{
					args.Player.SendErrorMessage("Something went wrong while resesting the player's data. Please contact an administrator.");
				}
			}
		}
		#endregion

		#region PurgeDatabase
		private static async void PurgeDatabase(CommandArgs args)
		{
			if (!args.Player.HasPermission(Permissions.purgeStats))
			{
				args.Player.SendErrorMessage("You don't have access to this command.");
				return;
			}
			else
			{
				if (await Instance.USFDatabase.PurgeEntriesAsync())
				{
					args.Player.SendSuccessMessage("Invalid entries purged.");
				}
				else
				{
					args.Player.SendErrorMessage("Something went wrong while purging the database. Please contact an administrator.");
				}
			}
		}
		#endregion

		#region ReadData
		private static async void ReadPlayerData(CommandArgs args)
		{
			if (args.Parameters.Count != 2)
			{
				args.Player.SendErrorMessage("Invalid syntax: {0}us read <player name>", TShock.Config.CommandSpecifier);
				return;
			}

			List<User> users = TShock.Users.GetUsersByNameEx(args.Parameters[1]);
			if (users.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid player!");
				return;
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(args.Player, users.Select(p => p.Name));
				return;
			}
			else if (Instance.USFDatabase.GetPlayer(users[0].ID) == null)
			{
				args.Player.SendErrorMessage("This user has no custom data to read.");
				return;
			}
			else if (users[0].Name != args.Player.User.Name && !args.Player.Group.HasPermission(Permissions.readOther))
			{
				args.Player.SendErrorMessage("You can't read this player's data.");
				return;
			}
			else
			{
				PlayerInfo player = await Instance.USFDatabase.GetPlayerAsync(users[0].ID);
				args.Player.SendMessage($"Username: {users[0].Name}", Color.LawnGreen);
				args.Player.SendMessage($"Prefix: {player.Prefix?.ToString() ?? "None"}", Color.LawnGreen);
				args.Player.SendMessage($"Suffix: {player.Suffix?.ToString() ?? "None"}", Color.LawnGreen);
				args.Player.SendMessage($"Color: {player.ChatColor?.ToString() ?? "None"}", Color.LawnGreen);
			}
		}
		#endregion

		#region AddPermissions
		private static async void AddPlayerPermissions(CommandArgs args)
		{
			if (args.Parameters.Count < 3)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}permission add <player name> <permissions>", TShock.Config.CommandSpecifier);
				return;
			}

			List<User> users = TShock.Users.GetUsersByNameEx(args.Parameters[1]);
			if (users.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid player!");
				return;
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(args.Player, users.Select(p => p.Name));
				return;
			}
			else
			{
				args.Parameters.RemoveRange(0, 2);
				if (await Instance.USFDatabase.AddPlayerPermissionsAsync(users[0].ID, args.Parameters))
				{
					args.Player.SendSuccessMessage("Modified {0} permissions successfully.", users[0].Name.SuffixPossesion());
				}
				else
				{
					args.Player.SendErrorMessage("Something went wrong while modifying permissions. Please contact an administrator.");
				}
			}
		}
		#endregion

		#region RemovePermissions
		private static async void RemovePlayerPermissions(CommandArgs args)
		{
			if (args.Parameters.Count < 3)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}permission remove <player name> <permissions>", TShock.Config.CommandSpecifier);
				return;
			}

			List<User> users = TShock.Users.GetUsersByNameEx(args.Parameters[1]);
			if (users.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid player!");
				return;
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(args.Player, users.Select(p => p.Name));
				return;
			}
			else if (Instance.USFDatabase.GetPlayer(users[0].ID) == null)
			{
				args.Player.SendErrorMessage("This user has no permissions to remove.");
				return;
			}
			else
			{
				args.Parameters.RemoveRange(0, 2);
				if (await Instance.USFDatabase.RemovePlayerPermissionsAsync(users[0].ID, args.Parameters))
				{
					args.Player.SendSuccessMessage("Modified {0} permissions successfully.", users[0].Name.SuffixPossesion());
				}
				else
				{
					args.Player.SendErrorMessage("Something went wrong while modifying permissions. Please contact an administrator.");
				}
			}
		}
		#endregion

		#region ListPermissions
		private static void ListPlayerPermissions(CommandArgs args)
		{
			if (args.Parameters.Count < 2 || args.Parameters.Count > 3)
			{
				args.Player.SendErrorMessage("Invalid syntax: {0}permission list <player name> [page]", TShock.Config.CommandSpecifier);
				return;
			}

			List<User> users = TShock.Users.GetUsersByNameEx(args.Parameters[1]);
			if (users.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid player!");
				return;
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(args.Player, users.Select(p => p.Name));
				return;
			}
			else if (Instance.USFDatabase.GetPlayer(users[0].ID) == null || !Instance.USFDatabase.GetPlayer(users[0].ID).Permissions.Any())
			{
				args.Player.SendErrorMessage("This player has no permissions to list.");
				return;
			}
			else
			{
				int pageNum;
				if (!PaginationTools.TryParsePageNumber(args.Parameters, 2, args.Player, out pageNum))
					return;

				List<string> permissionList = Instance.USFDatabase.GetPlayer(users[0].ID).Permissions;
				PaginationTools.SendPage(args.Player, pageNum, PaginationTools.BuildLinesFromTerms(permissionList), new PaginationTools.Settings
				{
					HeaderFormat = "{0} permissions:".SFormat(users[0].Name.SuffixPossesion()),
					FooterFormat = "Type {0}permission list {1} {{0}} for more.".SFormat(TShock.Config.CommandSilentSpecifier, users[0].Name),
					NothingToDisplayString = "This player has no permissions to list."
				});
			}
		}
		#endregion
	}
}
