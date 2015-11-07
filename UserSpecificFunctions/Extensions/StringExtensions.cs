using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserSpecificFunctions.Extensions
{
    public static class StringExtensions
    {
        public static string Suffix(this string _string)
        {
            return _string + (_string.EndsWith("S") || _string.EndsWith("s") ? "'" : "'s");
        }
    }
}
