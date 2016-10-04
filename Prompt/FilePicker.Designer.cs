namespace Apex.Prompt
{
    partial class FilePicker
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.txtFile = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.lblDescription = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.fileDialogue = new System.Windows.Forms.OpenFileDialog();
            this.SuspendLayout();
            // 
            // txtFile
            // 
            this.txtFile.Location = new System.Drawing.Point(12, 63);
            this.txtFile.Name = "txtFile";
            this.txtFile.Size = new System.Drawing.Size(467, 19);
            this.txtFile.TabIndex = 0;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(485, 61);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.TabIndex = 1;
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(404, 90);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 2;
            this.btnOk.Text = "Ok";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // lblDescription
            // 
            this.lblDescription.Location = new System.Drawing.Point(12, 9);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(548, 49);
            this.lblDescription.TabIndex = 3;
            this.lblDescription.Text = "Description";
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(485, 90);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // fileDialogue
            // 
            this.fileDialogue.FileName = "openFileDialog1";
            // 
            // FilePicker
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(572, 125);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtFile);
            this.Name = "FilePicker";
            this.Text = "FolderPicker";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtFile;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.OpenFileDialog fileDialogue;
    }
}