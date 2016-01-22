using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Hooks;
using UserSpecificFunctions.Extensions;
using static UserSpecificFunctions.UserSpecificFunctions;

namespace UserSpecificFunctions
{
    public static class Utils
    {
        public static bool CheckPerm(int userid, string permission)
        {
            if (TShock.Users.GetUserByID(userid) != null && players.ContainsKey(userid))
            {
                return players[userid].HasPermission(permission);
            }

            return false;
        }

        public static List<string> ListCommands(int userid)
        {
            List<string> permissions = new List<string>();
            foreach (string permission in players[userid].Permissions)
            {
                Command command = Commands.ChatCommands.Find(c => c.Permissions.Contains(permission));
                permissions.Add(permission + " ({0}{1})".SFormat(command == null ? "" : TShock.Config.CommandSpecifier, command == null ? "Not a command." : command.Name));
            }

            return permissions;
        }

        public static bool RunCommand(Command command, string msg, TSPlayer tsPlayer, List<string> args)
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

        public static bool ExecuteComamnd(TSPlayer player, string text)
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
                else if (!command.CanRun(player) && !CheckPerm(player.User.ID, command.Permissions[0]))
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
