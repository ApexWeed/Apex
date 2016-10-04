using System;
using System.Collections.Generic;
using System.Linq;

namespace Apex
{
    public static class EnumUtil
    {
        public static IEnumerable<T> GetValues<T>()
        {
            return (T[])Enum.GetValues(typeof(T));
        }

        public static object[] GetValuesCombo<T>()
        {
            return GetValues<T>().Cast<object>().ToArray();
        }
    }
}
