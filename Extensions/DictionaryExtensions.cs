using System.Collections.Generic;

namespace Apex.Extensions
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Adds an entry to a dictionary if it does not already exist.
        /// </summary>
        /// <param name="Dict">The dictionary to add the item to.</param>
        /// <param name="Key">The key of the item to add.</param>
        /// <param name="Value">The value of the item to add.</param>
        public static void AddDistinct<TKey, TValue>(this Dictionary<TKey, TValue> Dict, TKey Key, TValue Value)
        {
            if (!Dict.ContainsKey(Key))
                Dict.Add(Key, Value);
        }
    }
}
