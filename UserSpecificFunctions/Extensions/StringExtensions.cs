using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace UserSpecificFunctions.Extensions
{
	/// <summary>
	/// Provides extension methods for the <see cref="string"/> class.
	/// </summary>
    public static class StringExtensions
    {
        public static string SuffixPossesion(this string source)
        {
            return source + (source.EndsWith("s") ? "'" : "'s");
        }

		/// <summary>
		/// Attempts to convert a string to a Color.
		/// </summary>
		/// <param name="source">The string to convert.</param>
		/// <returns>A <see cref="Color"/> object.</returns>
        public static Color ToColor(this string source)
        {
            string[] color = source.Split(',');
            byte r, g, b;

            if (color.Length == 3 && byte.TryParse(color[0], out r) && byte.TryParse(color[1], out g) && byte.TryParse(color[2], out b))
            {
                return new Color(r, g, b);
            }
            else
                throw new Exception("[USF] An error occured at 'ToColor'");
        }
    }
}
