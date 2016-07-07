using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI.DB;
using UserSpecificFunctions.Extensions;

namespace UserSpecificFunctions
{
	public class PlayerInfo
	{
		public int UserID { get; set; }
		public string Prefix { get; set; }
		public string Suffix { get; set; }
		public string ChatColor { get; set; }
		public List<string> Permissions { get; set; }

		public bool HasPermission(string permission)
		{
			if (Permissions.Negated(permission))
				return false;

			if (string.IsNullOrWhiteSpace(permission))
				return true;

			if (Permissions.Contains(permission))
				return true;

			return false;
		}

		public void Load(QueryResult reader)
		{
			UserID = reader.Get<int>("UserID");
			Prefix = reader.Get<string>("Prefix");
			Suffix = reader.Get<string>("Suffix");
			ChatColor = reader.Get<string>("Color");
			Permissions = reader.Get<string>("Permissions")?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
		}

		public PlayerInfo()
		{
			UserID = 0;
			Prefix = null;
			Suffix = null;
			ChatColor = null;
			Permissions = new List<string>();
		}
	}
}
