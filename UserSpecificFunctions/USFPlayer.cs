using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserSpecificFunctions.Extensions;

namespace UserSpecificFunctions
{
    public class USFPlayer
    {
        public int UserID { get; set; }
        public string Prefix { get; set; }
        public string Suffix { get; set; }
        public string ChatColor { get; set; }
        public List<string> Permissions { get; set; }

        public USFPlayer()
        {
            UserID = 0;
            Prefix = null;
            Suffix = null;
            ChatColor = null;
            Permissions = new List<string>();
        }

        public USFPlayer(int userid, string prefix, string suffix, string chatColor, List<string> permissions)
        {
            UserID = userid;
            Prefix = prefix;
            Suffix = suffix;
            ChatColor = chatColor;
            Permissions = permissions;
        }

        public void AddPermission(string permission) => this.Permissions.Add(permission);
        public void RemovePermission(string permission) => this.Permissions.Remove(permission);
        public bool HasPermission(string permission) => this.Permissions.Contains(permission);
    }
}
