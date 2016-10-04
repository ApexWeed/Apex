using System.Collections.Generic;

namespace Apex.Extensions
{
    public static class ListExtensions
    {
        /// <summary>
        /// Adds a string to the list if it doesn't exist already.
        /// </summary>
        /// <param name="List">The list to add to.</param>
        /// <param name="Item">The item to add to the list.</param>
        public static void AddDistinct(this List<string> List, string Item)
        {
            foreach (string ListItem in List)
            {
                if (ListItem == Item)
                    return;
            }

            List.Add(Item);
        }

        /// <summary>
        /// Adds a list of strings to a list if they don't exist already.
        /// </summary>
        /// <param name="List">The list to add items to.</param>
        /// <param name="Items">The items to add.</param>
        public static void AddDistinct(this List<string> List, List<string> Items)
        {
            foreach (string Item in Items)
            {
                List.AddDistinct(Item);
            }
        }

        /// <summary>
        /// Adds an item to the list if it doesn't exist already.
        /// </summary>
        /// <typeparam name="T">The type of item to add.</typeparam>
        /// <param name="List">The list to add the item to.</param>
        /// <param name="Item">The item to add.</param>
        public static void AddDistinct<T>(this List<T> List, T Item)
        {
            foreach (T listItem in List)
            {
                if (listItem.Equals(Item))
                {
                    return;
                }
            }

            List.Add(Item);
        }

        /// <summary>
        /// Adds a list of items to a list if they don't already exist.
        /// </summary>
        /// <typeparam name="T">The type of the items to add.</typeparam>
        /// <param name="List">The list to add the items to.</param>
        /// <param name="Items">The items to add to the list.</param>
        public static void AddDistinct<T>(this List<T> List, IEnumerable<T> Items)
        {
            foreach (var item in Items)
            {
                List.AddDistinct(item);
            }
        }
    }
}
