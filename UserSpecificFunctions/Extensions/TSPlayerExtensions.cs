using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace UserSpecificFunctions.Extensions
{
	/// <summary>
	/// Provides extension methods for the <see cref="TSPlayer"/> class.
	/// </summary>
	public static class TSPlayerExtensions
	{
		/// <summary>
		/// Pulls the player's USF information from the database.
		/// </summary>
		/// <param name="tsPlayer">The <see cref="TSPlayer"/> object.</param>
		/// <returns>A <see cref="PlayerInfo"/> object.</returns>
		public static PlayerInfo GetPlayerInfo(this TSPlayer tsPlayer)
		{
			if (!tsPlayer.IsLoggedIn || tsPlayer.User == null)
				return null;

			return UserSpecificFunctions.Instance.USFDatabase.GetPlayer(tsPlayer.User.ID);
		}
	}
}
