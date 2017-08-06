using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Apex
{
    public static class OpenEnvironment
    {
        private static BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;
        private static MethodInfo GetResourceStringMethod;

        public static string GetResourceString(string Key)
        {
            if (GetResourceStringMethod == null)
            {
                GetResourceStringMethod = typeof(Environment).GetMethod(nameof(GetResourceString), flags, null, new Type[] { typeof(string) }, null);
            }

            return (string)GetResourceStringMethod.Invoke(null, new object[] { Key });
        }

        public static string GetResourceString(string Key, params object[] Values)
        {
            return string.Format(GetResourceString(Key), Values);
        }
    }
}
