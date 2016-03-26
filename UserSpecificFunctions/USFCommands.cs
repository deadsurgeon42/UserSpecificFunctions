using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                                               where cmd.CanRun(args.Player)
                                               || (Utils.CheckPerm(args.Player.User.ID, cmd.Permissions[0])) && (cmd.Name != "auth" || TShock.AuthToken != 0)
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
                if (!command.CanRun(args.Player) && !Utils.CheckPerm(args.Player.User.ID, command.Permissions[0]))
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
                        if (!args.Player.IsLoggedIn && args.Player.RealPlayer)
                        {
                            args.Player.SendErrorMessage("You must be logged in to do that.");
                            return;
                        }
                        else if (!args.Player.Group.HasPermission(Permissions.setPrefix))
                        {
                            args.Player.SendErrorMessage("You don't have access to this command.");
                            return;
                        }
                        else if (args.Parameters.Count < 3)
                        {
                            args.Player.SendErrorMessage("Invalid syntax: {0}us prefix <player name> <prefix>", TShock.Config.CommandSpecifier);
                            return;
                        }
                        else
                        {
                            User user = TShock.Users.GetUserByName(args.Parameters[1]);
                            if (user == null)
                            {
                                args.Player.SendErrorMessage("Invalid user.");
                                return;
                            }
                            else if (user.Name != args.Player.User.Name && !args.Player.Group.HasPermission(Permissions.setOther))
                            {
                                args.Player.SendErrorMessage("You cannot modify this player's data.");
                                return;
                            }
                            else
                            {
                                args.Parameters.RemoveRange(0, 2);
                                string prefix = string.Join(" ", args.Parameters.Select(x => x));

                                foreach (string word in config.UnAllowedWords)
                                {
                                    if (prefix.Contains(word))
                                    {
                                        args.Player.SendErrorMessage($"You can't use '{word}' in your prefix.");
                                        return;
                                    }
                                }

                                if (prefix.Length > config.PrefixLength)
                                {
                                    args.Player.SendErrorMessage($"Your prefix cannot be longer than {config.PrefixLength} chars.");
                                    return;
                                }
                                else
                                {
                                    Database.SetPrefix(user.ID, prefix);
                                    args.Player.SendSuccessMessage($"Set {user.Name.Suffix()} prefix to '{prefix}'.");
                                }
                            }
                        }
                    }
                    break;
                case "suffix":
                    {
                        if (!args.Player.IsLoggedIn && args.Player.RealPlayer)
                        {
                            args.Player.SendErrorMessage("You must be logged in to do that.");
                            return;
                        }
                        else if (!args.Player.Group.HasPermission(Permissions.setSuffix))
                        {
                            args.Player.SendErrorMessage("You don't have access to this command.");
                            return;
                        }
                        else if (args.Parameters.Count < 3)
                        {
                            args.Player.SendErrorMessage("Invalid syntax: {0}us suffix <player name> <suffix>", TShock.Config.CommandSpecifier);
                            return;
                        }
                        else
                        {
                            User user = TShock.Users.GetUserByName(args.Parameters[1]);
                            if (user == null)
                            {
                                args.Player.SendErrorMessage("Invalid user.");
                                return;
                            }
                            else if (user.Name != args.Player.User.Name && !args.Player.Group.HasPermission(Permissions.setOther))
                            {
                                args.Player.SendErrorMessage("You cannot modify this player's data.");
                                return;
                            }
                            else
                            {
                                args.Parameters.RemoveRange(0, 2);
                                string suffix = string.Join(" ", args.Parameters.Select(x => x));

                                foreach (string word in config.UnAllowedWords)
                                {
                                    if (suffix.Contains(word))
                                    {
                                        args.Player.SendErrorMessage($"You can't use '{word}' in your suffix.");
                                        return;
                                    }
                                }

                                if (suffix.Length > config.SuffixLength)
                                {
                                    args.Player.SendErrorMessage($"Your suffix cannot be longer than {config.SuffixLength} chars.");
                                    return;
                                }
                                else
                                {
                                    Database.SetSuffix(user.ID, suffix);
                                    args.Player.SendSuccessMessage($"Set {user.Name.Suffix()} suffix to '{suffix}'.");
                                }
                            }
                        }
                    }
                    break;
                case "color":
                    {
                        if (!args.Player.IsLoggedIn && args.Player.RealPlayer)
                        {
                            args.Player.SendErrorMessage("You must be logged in to do that.");
                            return;
                        }
                        else if (!args.Player.Group.HasPermission(Permissions.setColor))
                        {
                            args.Player.SendErrorMessage("You don't have access to this command.");
                            return;
                        }
                        else if (args.Parameters.Count < 3)
                        {
                            args.Player.SendErrorMessage("Invalid syntax: {0}us color <player name> <rrr,ggg,bbb>", TShock.Config.CommandSpecifier);
                            return;
                        }
                        else
                        {
                            User user = TShock.Users.GetUserByName(args.Parameters[1]);
                            if (user == null)
                            {
                                args.Player.SendErrorMessage("Invalid user.");
                                return;
                            }
                            else if (user.Name != args.Player.User.Name && !args.Player.Group.HasPermission(Permissions.setOther))
                            {
                                args.Player.SendErrorMessage("You cannot modify this player's data.");
                                return;
                            }
                            else
                            {
                                args.Parameters.RemoveRange(0, 2);
                                string color = string.Join(" ", args.Parameters.Select(x => x));
                                string[] Color = color.Split(',');

                                byte r, g, b;
                                if (Color.Length == 3 && byte.TryParse(Color[0], out r) && byte.TryParse(Color[1], out g) && byte.TryParse(Color[2], out b))
                                {
                                    Database.SetColor(user.ID, color);
                                    args.Player.SendSuccessMessage($"Set {user.Name.Suffix()} color to '{color}'.");
                                }
                                else
                                    args.Player.SendErrorMessage("Invalid syntax: {0}us color <player name> <rrr,ggg,bbb> (values cannot be greater than 255)", TShock.Config.CommandSpecifier);
                            }
                        }
                    }
                    break;
                case "remove":
                    {
                        if (!args.Player.IsLoggedIn && args.Player.RealPlayer)
                        {
                            args.Player.SendErrorMessage("You must be logged in to do that.");
                            return;
                        }
                        else if (args.Parameters.Count != 3)
                        {
                            args.Player.SendErrorMessage("Invalid syntax: {0}us remove <player name> <prefix/suffix/color>", TShock.Config.CommandSpecifier);
                            return;
                        }
                        else
                        {
                            User user = TShock.Users.GetUserByName(args.Parameters[1]);
                            if (user == null)
                            {
                                args.Player.SendErrorMessage("Invalid user.");
                                return;
                            }
                            else if (user.Name != args.Player.User.Name && !args.Player.Group.HasPermission(Permissions.setOther))
                            {
                                args.Player.SendErrorMessage("You can't modify this player's data.");
                                return;
                            }
                            else
                            {
                                switch (args.Parameters[2].ToLower())
                                {
                                    case "prefix":
                                        {
                                            if (!args.Player.Group.HasPermission(Permissions.removePrefix))
                                            {
                                                args.Player.SendErrorMessage("You don't have access to this command.");
                                                return;
                                            }
                                            else if (!players.ContainsKey(user.ID) || players[user.ID].Prefix == null)
                                            {
                                                args.Player.SendErrorMessage("This user doesn't have a prefix to remove.");
                                                return;
                                            }
                                            else
                                            {
                                                Database.SetPrefix(user.ID);
                                                args.Player.SendSuccessMessage($"Removed {user.Name.Suffix()} prefix.");
                                            }
                                        }
                                        break;
                                    case "suffix":
                                        {
                                            if (!args.Player.Group.HasPermission(Permissions.removeSuffix))
                                            {
                                                args.Player.SendErrorMessage("You don't have access to this command.");
                                                return;
                                            }
                                            else if (!players.ContainsKey(user.ID) || players[user.ID].Suffix == null)
                                            {
                                                args.Player.SendErrorMessage("This user doesn't have a suffix to remove.");
                                                return;
                                            }
                                            else
                                            {
                                                Database.SetSuffix(user.ID);
                                                args.Player.SendSuccessMessage($"Removed {user.Name.Suffix()} suffix.");
                                            }
                                        }
                                        break;
                                    case "color":
                                        {
                                            if (!args.Player.Group.HasPermission(Permissions.removeColor))
                                            {
                                                args.Player.SendErrorMessage("You don't have access to this command.");
                                                return;
                                            }
                                            else if (!players.ContainsKey(user.ID) || players[user.ID].ChatColor == "000,000,000")
                                            {
                                                args.Player.SendErrorMessage("This user doesn't have a color to remove.");
                                                return;
                                            }
                                            else
                                            {
                                                Database.SetColor(user.ID);
                                                args.Player.SendSuccessMessage($"Removed {user.Name.Suffix()} color.");
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                    }
                    break;
                case "reset":
                    {
                        if (!args.Player.IsLoggedIn && args.Player.RealPlayer)
                        {
                            args.Player.SendErrorMessage("You must be logged in to do that.");
                            return;
                        }
                        else if (!args.Player.Group.HasPermission(Permissions.resetStats))
                        {
                            args.Player.SendErrorMessage("You don't have acces to this command.");
                            return;
                        }
                        else if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage("{0}us reset <player name>", TShock.Config.CommandSpecifier);
                            return;
                        }
                        else
                        {
                            User user = TShock.Users.GetUserByName(args.Parameters[1]);
                            if (user == null)
                            {
                                args.Player.SendErrorMessage("Invalid user.");
                                return;
                            }
                            else if (user.Name != args.Player.Name && !args.Player.Group.HasPermission(Permissions.setOther))
                            {
                                args.Player.SendErrorMessage("You can't modify this player's data.");
                                return;
                            }
                            else
                            {
                                Database.ResetPlayerData(user.ID);
                                args.Player.SendErrorMessage($"Reset {user.Name.Suffix()} data.");
                            }
                        }
                    }
                    break;
                case "purge":
                    {
                        if (!args.Player.Group.HasPermission(Permissions.purgeStats))
                        {
                            args.Player.SendErrorMessage("You don't have access to this command.");
                            return;
                        }
                        else
                        {
                            Database.PurgeInvalid();
                            args.Player.SendSuccessMessage("Players without any custom data have been purged from the database.");
                        }
                    }
                    break;
                case "read":
                    {
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage("Invalid syntax: {0}us read <player name>", TShock.Config.CommandSpecifier);
                            return;
                        }

                        User user = TShock.Users.GetUserByName(args.Parameters[1]);
                        if (user == null)
                        {
                            args.Player.SendErrorMessage("Invalid user.");
                            return;
                        }
                        else if (user.Name != args.Player.User.Name && !args.Player.Group.HasPermission(Permissions.readOther))
                        {
                            args.Player.SendErrorMessage("You can't read this player's data.");
                            return;
                        }
                        else if (!players.ContainsKey(user.ID))
                        {
                            args.Player.SendErrorMessage("This user doesn't have any data to read.");
                            return;
                        }
                        else
                        {
                            args.Player.SendMessage($"Username: {user.Name}", Color.LawnGreen);
                            args.Player.SendMessage($"Prefix: {players[user.ID].Prefix?.ToString() ?? "None"}", Color.LawnGreen);
                            args.Player.SendMessage($"Suffix: {players[user.ID].Suffix?.ToString() ?? "None"}", Color.LawnGreen);
                            args.Player.SendMessage($"Color: {(players[user.ID].ChatColor == "000,000,000" ? "None" : players[user.ID].ChatColor)}", Color.LawnGreen);
                        }
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
                return;
            }

            switch (args.Parameters[0].ToLower())
            {
                case "add":
                    {
                        if (args.Parameters.Count != 3)
                        {
                            args.Player.SendErrorMessage("Invalid syntax: {0}permission add <player name> <permission>", TShock.Config.CommandSpecifier);
                            return;
                        }

                        string permission = string.Join(" ", args.Parameters[2]);

                        User user = TShock.Users.GetUserByName(args.Parameters[1]);
                        if (user == null)
                        {
                            args.Player.SendErrorMessage("Invalid user.");
                            return;
                        }
                        else if (Utils.CheckPerm(user.ID, permission))
                        {
                            args.Player.SendInfoMessage("This user already has this permission.");
                            return;
                        }
                        else
                        {
                            Database.ModifyPermissions(user.ID, permission);
                            args.Player.SendSuccessMessage($"Modified {user.Name.Suffix()} permissions successfully.");
                        }
                    }
                    return;
                case "del":
                case "rem":
                case "delete":
                case "remove":
                    {
                        if (args.Parameters.Count != 3)
                        {
                            args.Player.SendErrorMessage("Invalid syntax: {0}permission delete <player name> <permission>", TShock.Config.CommandSpecifier);
                            return;
                        }

                        string permission = string.Join(" ", args.Parameters[2]);

                        User user = TShock.Users.GetUserByName(args.Parameters[1]);
                        if (user == null)
                        {
                            args.Player.SendErrorMessage("Invalid user.");
                            return;
                        }
                        else if (!Utils.CheckPerm(user.ID, permission))
                        {
                            args.Player.SendInfoMessage("This user does not have this permission.");
                            return;
                        }
                        else
                        {
                            Database.ModifyPermissions(user.ID, permission, true);
                            args.Player.SendSuccessMessage($"Modified {user.Name.Suffix()} permissions successfully.");
                        }
                    }
                    return;
                case "list":
                    {
                        if (args.Parameters.Count < 2 || args.Parameters.Count > 3)
                        {
                            args.Player.SendErrorMessage("Invalid syntax: {0}permission list <player name> [page]", TShock.Config.CommandSpecifier);
                            return;
                        }

                        User user = TShock.Users.GetUserByName(args.Parameters[1]);
                        if (user == null)
                        {
                            args.Player.SendErrorMessage("Invalid user.");
                            return;
                        }
                        else if (!players.ContainsKey(user.ID))
                        {
                            args.Player.SendErrorMessage("This user doesn't have any permissions to list.");
                            return;
                        }
                        else
                        {
                            int pageNum;
                            if (!PaginationTools.TryParsePageNumber(args.Parameters, 2, args.Player, out pageNum))
                                return;

                            PaginationTools.SendPage(args.Player, pageNum, PaginationTools.BuildLinesFromTerms(Utils.ListCommands(user.ID)),
                                new PaginationTools.Settings
                                {
                                    HeaderFormat = $"{user.Name.Suffix()} permissions:",
                                    FooterFormat = "Type {0}permission list {1} {{0}} for more.".SFormat(TShock.Config.CommandSpecifier, user.Name),
                                    NothingToDisplayString = "This user has no specific permissions to display."
                                });
                        }
                    }
                    return;
            }
        }
    }
}
