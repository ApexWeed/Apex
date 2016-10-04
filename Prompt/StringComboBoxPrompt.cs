using System;
using System.Windows.Forms;

namespace Apex.Prompt
{
    public partial class StringComboBoxPrompt : Form
    {
        public string ChosenString;

        /// <summary>
        /// Initialises a new combobox prompt.
        /// </summary>
        /// <param name="Prompt">The text for the window.</param>
        /// <param name="Title">The text in the title bar.</param>
        /// <param name="ComboBoxValues">The values for the combo box.</param>
        /// <param name="AllowNew">Whether to allow the user to enter a new value.</param>
        public StringComboBoxPrompt(string Prompt, string Title, string[] ComboBoxValues, bool AllowNew = true)
        {
            InitializeComponent();

            lblPrompt.Text = Prompt;
            this.Text = Title;
            cmbComboBox.DataSource = ComboBoxValues;
            cmbComboBox.DropDownStyle = AllowNew ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList;
            btnOK.Text = "Ok";
            btnCancel.Text = "Cancel";
        }

        /// <summary>
        /// Cancel the form.
        /// </summary>
        /// <param name="sender">Sender that fired the event.</param>
        /// <param name="e">Event args associated with this event.</param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        /// <summary>
        /// Submit form values.
        /// </summary>
        /// <param name="sender">Sender that fired the event.</param>
        /// <param name="e">Event args associated with this event.</param>
        private void btnOK_Click(object sender, EventArgs e)
        {
            ChosenString = cmbComboBox.Text;

            if (string.IsNullOrWhiteSpace(ChosenString))
            {
                MessageBox.Show("Input cannot be blank");
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
