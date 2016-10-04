using System.Windows.Forms;

namespace Apex
{
    public static class InputFiltering
    {
        public static void FilterNumeric(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != '-')
            {
                e.Handled = true;
            }

            // Only allow one decimal point
            if (e.KeyChar == '.' && (sender as TextBox).Text.IndexOf('.') > -1)
            {
                e.Handled = true;
            }

            // Allow negative numbers.
            if (e.KeyChar == '-' && ((sender as TextBox).Text.IndexOf('-') > -1 || (sender as TextBox).SelectionStart > 0))
            {
                e.Handled = true;
            }
        }

        public static void FilterPositiveNumeric(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            {
                e.Handled = true;
            }

            // Only allow one decimal point
            if (e.KeyChar == '.' && (sender as TextBox).Text.IndexOf('.') > -1)
            {
                e.Handled = true;
            }
        }

        public static void FilterInteger(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '-')
            {
                e.Handled = true;
            }

            // Allow negative numbers.
            if (e.KeyChar == '-' && ((sender as TextBox).Text.IndexOf('-') > -1 || (sender as TextBox).SelectionStart > 0))
            {
                e.Handled = true;
            }
        }

        public static void FilterPositiveInteger(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }
    }
}
