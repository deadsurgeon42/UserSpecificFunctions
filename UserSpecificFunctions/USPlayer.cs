using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserSpecificFunctions
{
    public class USPlayer
    {
        public int UserID { get; set; }
        public string Prefix { get; set; }
        public string Suffix { get; set; }
        public byte R { get; private set; }
        public byte G { get; private set; }
        public byte B { get; private set; }
        public List<string> Permissions { get; set; }

        public string Color
        {
            get { return string.Format("{0},{1},{2}", R.ToString("D3"), G.ToString("D3"), B.ToString("D3")); }
            set
            {
                if (value != null)
                {
                    string[] color = value.Split(',');
                    if (color.Length == 3)
                    {
                        byte r, g, b;
                        if (byte.TryParse(color[0], out r) && byte.TryParse(color[1], out g) && byte.TryParse(color[2], out b))
                        {
                            R = r;
                            G = g;
                            B = b;
                            return;
                        }
                    }
                }
            }
        }

        public USPlayer()
        {
            UserID = 0;
            Prefix = null;
            Suffix = null;
            R = 0;
            G = 0;
            B = 0;
            Permissions = new List<string>();
        }

        public USPlayer(int UserID, string Prefix, string Suffix, string Color, List<string> Permissions)
        {
            this.UserID = UserID;
            this.Prefix = Prefix;
            this.Suffix = Suffix;
            this.Color = Color;
            this.Permissions = Permissions;
        }

        public void AddPermission(string permission)
        {
            this.Permissions.Add(permission);
        }

        public void RemovePermission(string permission)
        {
            this.Permissions.Remove(permission);
        }

        public bool HasPermission(string permission)
        {
            return this.Permissions.Contains(permission);
        }
    }
}
