using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Apex.Extensions
{
    public static class TreeNodeCollectionExtensions
    {
        public static TreeNode FindByTag(this TreeNodeCollection Nodes, object Tag)
        {
            var searchNodes = new Queue<TreeNode>();
            foreach (TreeNode node in Nodes)
            {
                searchNodes.Enqueue(node);
            }

            while (searchNodes.Count > 0)
            {
                var node = searchNodes.Dequeue();
                if (node.Tag == Tag)
                {
                    return node;
                }

                foreach (TreeNode subNode in node.Nodes)
                {
                    searchNodes.Enqueue(subNode);
                }
            }

            return null;
        }
    }
}
