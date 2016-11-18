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

        public static string GetResourceString(string Key)
        {
            var methodInfo = typeof(Environment).GetMethod(nameof(GetResourceString), flags, null, new Type[] { typeof(string) }, null);

            return (string)methodInfo.Invoke(null, new object[] { Key });
        }
    }
}
