using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserSpecificFunctions.Extensions
{
	/// <summary>
	/// Provides extension methods for <see cref="List{T}"/>
	/// </summary>
	public static class ListExtensions
	{
		/// <summary>
		/// Checks to see whether a permission is negated or not.
		/// </summary>
		/// <param name="permissions">The list to operate on.</param>
		/// <param name="permission">The permission to check for.</param>
		/// <returns>True or false.</returns>
		public static bool Negated(this List<string> permissions, string permission)
		{
			return permissions.Any(p => p != null && (p.StartsWith("!") && p.Substring(1) == permission));
		}

		/// <summary>
		/// Separates the list using the separator and returns it as a new string.
		/// </summary>
		/// <typeparam name="T">The type of list to separate.</typeparam>
		/// <param name="list">The list to operate on.</param>
		/// <param name="separator">The separator char.</param>
		/// <returns>A separated string.</returns>
		public static string Separate<T>(this List<T> list, string separator)
		{
			return string.Join(separator, list.ToArray());
		}
	}
}
