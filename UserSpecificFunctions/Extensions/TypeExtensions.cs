using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace UserSpecificFunctions.Extensions
{
    public static class TypeExtensions
    {
        public static T CallMethod<T>(this Type type, string name, params object[] args)
        {
            MethodInfo methodInfo = type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
            return (T)methodInfo.Invoke(null, args);
        }
    }
}
