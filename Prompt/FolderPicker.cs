using System;
using System.Windows.Forms;

namespace Apex.Prompt
{
    public partial class FolderPicker : Form
    {
        public string ChosenFolder;

        public FolderPicker(string Title, string Description, string DefaultFolder)
        {
            InitializeComponent();
            this.Text = Title;
            lblDescription.Text = Description;
            txtFolder.Text = DefaultFolder;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            ChosenFolder = txtFolder.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            folderDialogue.SelectedPath = txtFolder.Text;
            if (folderDialogue.ShowDialog() == DialogResult.OK)
            {
                txtFolder.Text = folderDialogue.SelectedPath;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
