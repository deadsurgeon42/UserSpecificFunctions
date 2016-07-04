using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserSpecificFunctions.Extensions
{
	public static class ListExtensions
	{
		public static bool Negated(this List<string> permissions, string permission)
		{
			if (permission.StartsWith("!") && permissions.Contains(permission))
				return true;

			return false;
		}
	}
}
