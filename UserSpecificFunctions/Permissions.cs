using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserSpecificFunctions
{
	public class Permissions
	{
		/// <summary>
		/// Allows players. to set their prefix.
		/// </summary>
		public static readonly string setPrefix = "us.prefix";

		/// <summary>
		/// Allows players to set their suffix.
		/// </summary>
		public static readonly string setSuffix = "us.suffix";

		/// <summary>
		/// Allows players to set their color.
		/// </summary>
		public static readonly string setColor = "us.color";

		/// <summary>
		/// Allows players to modify permissions.
		/// </summary>
		public static readonly string setPermissions = "us.permission";

		/// <summary>
		/// Allows players to change other players' information.
		/// </summary>
		public static readonly string setOther = "us.setother";

		/// <summary>
		/// Allows players to read other players' information.
		/// </summary>
		public static readonly string readOther = "us.readother";

		/// <summary>
		/// Allows players to remove their prefix.
		/// </summary>
		public static readonly string removePrefix = "us.remove.prefix";

		/// <summary>
		/// Allows players to remove their suffix.
		/// </summary>
		public static readonly string removeSuffix = "us.remove.suffix";

		/// <summary>
		/// Allows players to remove their color.
		/// </summary>
		public static readonly string removeColor = "us.remove.color";

		/// <summary>
		/// Allows players to reset their information.
		/// </summary>
		public static readonly string resetStats = "us.reset";

		/// <summary>
		/// Allows players to purge the database.
		/// </summary>
		public static readonly string purgeStats = "us.purge";
	}
}
