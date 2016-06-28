using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserSpecificFunctions
{
	public class Permissions
	{
		public static readonly string setPrefix = "us.prefix";
		public static readonly string setSuffix = "us.suffix";
		public static readonly string setColor = "us.color";
		public static readonly string setPermissions = "us.permission";
		public static readonly string setOther = "us.setother";
		public static readonly string readOther = "us.readother";

		public static readonly string removePrefix = "us.remove.prefix";
		public static readonly string removeSuffix = "us.remove.suffix";
		public static readonly string removeColor = "us.remove.color";

		public static readonly string resetStats = "us.reset";
		public static readonly string purgeStats = "us.purge";
	}
}
