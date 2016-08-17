using System.Collections.Generic;
using TShockAPI.DB;

namespace UserSpecificFunctions.Extensions
{
	/// <summary>
	/// Provides extension methods for the <see cref="UserManager"/> class.
	/// </summary>
	public static class UserManagerExtensions
	{
		/// <summary>
		/// Returns a list of <see cref="User"/> objects matching the username. 
		/// </summary>
		/// <param name="userManager">The <see cref="UserManager"/> instance.</param>
		/// <param name="userName">The username.</param>
		/// <returns>A list of <see cref="User"/>s.</returns>
		public static List<User> GetUsersByNameEx(this UserManager userManager, string userName)
		{
			var users = new List<User>();

			foreach (User user in userManager.GetUsersByName(userName))
			{
				if (user != null)
				{
					if (user.Name == userName)
					{
						return new List<User> { user };
					}
					else if (user.Name.ToLower().StartsWith(userName.ToLower()))
					{
						users.Add(user);
					}
				}
			}

			return users;
		}
	}
}
