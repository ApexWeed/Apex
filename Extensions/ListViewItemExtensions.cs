using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Apex.Extensions
{
    public static class ListViewItemExtensions
    {
        /// <summary>
        /// Finds an item in a listview from its tag.
        /// </summary>
        /// <param name="List">The list to search.</param>
        /// <param name="Tag">The tag to check for.</param>
        /// <returns></returns>
        public static ListViewItem FindByTag(this ListView.ListViewItemCollection List, string Tag)
        {
            try
            {
                foreach (ListViewItem item in List)
                {
                    if (item.Tag as string == Tag)
                        return item;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            throw new KeyNotFoundException($"The tag \"{Tag as string}\" was not found in the ListViewItemCollection");
        }
    }
}
