using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace UserSpecificFunctions.Extensions
{
	public static class TSPlayerExtensions
	{
		public static PlayerInfo GetPlayerInfo(this TSPlayer tsPlayer)
		{
			if (!tsPlayer.IsLoggedIn || tsPlayer.User == null)
				return null;

			return UserSpecificFunctions.USFDatabase.GetPlayer(tsPlayer.User.ID);
		}
	}
}
