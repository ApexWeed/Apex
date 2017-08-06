using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Apex.Extensions
{
    public static class ControlExtensions
    {
        public static void SplitWidth(this Control control)
        {
            if (control.Controls.Count == 0)
                return;

            var width = control.Width / control.Controls.Count;
            for (var i = 0; i < control.Controls.Count; i++)
            {
                var child = control.Controls[i];
                child.Width = width;
                child.Left = width * i;

                if (i == control.Controls.Count - 1)
                    child.Width += control.Width % width;
            }
        }
        public static void SplitHeight(this Control control)
        {
            if (control.Controls.Count == 0)
                return;

            var height = control.Height / control.Controls.Count;
            for (var i = 0; i < control.Controls.Count; i++)
            {
                var child = control.Controls[i];
                child.Height = height;
                child.Top = height * i;

                if (i == control.Controls.Count - 1)
                    child.Height += control.Height % height;
            }
        }
    }
}
