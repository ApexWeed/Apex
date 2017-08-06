using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Apex.Extensions
{
    public static class ListBoxExtensions
    {
        public enum MoveDirection
        {
            Up,
            Down
        }

        /// <summary>
        /// Sets the selection in a listbox.
        /// </summary>
        /// <param name="ListBoxControl">The listbox to update.</param>
        /// <param name="Selection">The indexes to select.</param>
        public static void SetSelection(this ListBox listBoxControl, int[] selection)
        {
            foreach (int Index in selection)
            {
                listBoxControl.SetSelected(Index, true);
            }
        }

        /// <summary>
        /// Moves the current selection up or down.
        /// </summary>
        /// <param name="direction">Direction to move the selection.</param>
        public static void MoveSelection(this ListBox listBox, MoveDirection direction)
        {
            var itemArray = new object[listBox.Items.Count];
            listBox.Items.CopyTo(itemArray, 0);
            var indexArray = new int[listBox.SelectedIndices.Count];
            listBox.SelectedIndices.CopyTo(indexArray, 0);
            var newSelection = new List<int>();
            if (direction == MoveDirection.Up)
            {
                Array.Sort(indexArray, (a, b) => a.CompareTo(b));
                for (int i = 0; i < indexArray.Length; i++)
                {
                    // Can't move above first index.
                    if (indexArray[i] == 0)
                    {
                        newSelection.Add(indexArray[i]);
                        indexArray[i] = -1;
                        continue;
                    }

                    // Last index was unable to move. Either first index or group is already at the top.
                    if (i > 0 && indexArray[i - 1] == -1)
                    {
                        newSelection.Add(indexArray[i]);
                        indexArray[i] = -1;
                        continue;
                    }

                    var value = itemArray[indexArray[i] - 1];
                    itemArray[indexArray[i] - 1] = itemArray[indexArray[i]];
                    itemArray[indexArray[i]] = value;
                    newSelection.Add(indexArray[i] - 1);
                }
            }
            else if (direction == MoveDirection.Down)
            {
                Array.Sort(indexArray, (a, b) => b.CompareTo(a));
                for (int i = 0; i < indexArray.Length; i++)
                {
                    // Can't move below last index.
                    if (indexArray[i] == itemArray.Length - 1)
                    {
                        newSelection.Add(indexArray[i]);
                        indexArray[i] = -1;
                        continue;
                    }

                    // Last index was unable to move. Either first index or group is already at the top.
                    if (i > 0 && indexArray[i - 1] == -1)
                    {
                        newSelection.Add(indexArray[i]);
                        indexArray[i] = -1;
                        continue;
                    }

                    var value = itemArray[indexArray[i] + 1];
                    itemArray[indexArray[i] + 1] = itemArray[indexArray[i]];
                    itemArray[indexArray[i]] = value;
                    newSelection.Add(indexArray[i] + 1);
                }
            }

            listBox.Items.Clear();
            listBox.Items.AddRange(itemArray);
            listBox.SetSelection(newSelection.ToArray());
        }

        public static void RemoveSelection(this ListBox listBox)
        {
            var itemArray = new object[listBox.Items.Count];
            listBox.Items.CopyTo(itemArray, 0);
            var itemList = itemArray.ToList();
            var indexArray = new int[listBox.SelectedIndices.Count];
            listBox.SelectedIndices.CopyTo(indexArray, 0);
            Array.Sort(indexArray, (a, b) => b.CompareTo(a));
            foreach (int index in indexArray)
            {
                itemList.RemoveAt(index);
            }

            listBox.Items.Clear();
            listBox.Items.AddRange(itemList.ToArray());
        }
    }
}
