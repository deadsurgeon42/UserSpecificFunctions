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
		/// <summary>
		/// The player's ID.
		/// </summary>
		public int UserID { get; set; }

		/// <summary>
		/// The player's prefix.
		/// </summary>
		public string Prefix { get; set; }

		/// <summary>
		/// The player's suffix.
		/// </summary>
		public string Suffix { get; set; }

		/// <summary>
		/// The player's chat color.
		/// </summary>
		public string ChatColor { get; set; }

		/// <summary>
		/// The player's permissions.
		/// </summary>
		public List<string> Permissions { get; set; }

		/// <summary>
		/// Checks if the player has a specific permission.
		/// </summary>
		/// <param name="permission">The permission to check for.</param>
		/// <returns>True or false.</returns>
		public bool HasPermission(string permission)
		{
			// Ensure the permission is not negated.
			if (Permissions.Negated(permission))
				return false;

			// Check if the permission is null or whitespace.
			if (string.IsNullOrWhiteSpace(permission))
				return true;

			// Check whether the player's permission list contains the specified permission.
			if (Permissions.Contains(permission))
				return true;

			// Check for any permission.* nodes.
			string[] nodes = permission.Split('.');
			for (int i = nodes.Length - 1; i >= 0; i--)
			{
				nodes[i] = "*";
				if (Permissions.Contains(string.Join(".", nodes, 0, i + 1)))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Populates the <see cref="PlayerInfo"/> object based on the search query.
		/// </summary>
		/// <param name="reader">The <see cref="QueryResult"/> object.</param>
		public void Load(QueryResult reader)
		{
			UserID = reader.Get<int>("UserID");
			Prefix = reader.Get<string>("Prefix");
			Suffix = reader.Get<string>("Suffix");
			ChatColor = reader.Get<string>("Color");
			Permissions = reader.Get<string>("Permissions")?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PlayerInfo"/> class.
		/// </summary>
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
