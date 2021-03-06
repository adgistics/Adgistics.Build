// <auto-generated />
namespace NArrange.Gui.Configuration
{
    /// <summary>
    /// Partial class.
    /// </summary>
    partial class ConfigurationPicker
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._buttonEdit = new System.Windows.Forms.Button();
            this._buttonBrowse = new System.Windows.Forms.Button();
            this._textBoxFile = new System.Windows.Forms.TextBox();
            this._openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this._buttonCreate = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _buttonEdit
            // 
            this._buttonEdit.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this._buttonEdit.Enabled = false;
            this._buttonEdit.Location = new System.Drawing.Point(339, -1);
            this._buttonEdit.Name = "_buttonEdit";
            this._buttonEdit.Size = new System.Drawing.Size(75, 23);
            this._buttonEdit.TabIndex = 7;
            this._buttonEdit.Text = "&Edit";
            this._buttonEdit.UseVisualStyleBackColor = true;
            this._buttonEdit.Visible = false;
            this._buttonEdit.Click += new System.EventHandler(this.HandleButtonEditClick);
            // 
            // _buttonBrowse
            // 
            this._buttonBrowse.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this._buttonBrowse.Location = new System.Drawing.Point(258, -1);
            this._buttonBrowse.Name = "_buttonBrowse";
            this._buttonBrowse.Size = new System.Drawing.Size(75, 23);
            this._buttonBrowse.TabIndex = 1;
            this._buttonBrowse.Text = "&Browse...";
            this._buttonBrowse.UseVisualStyleBackColor = true;
            this._buttonBrowse.Click += new System.EventHandler(this.HandleButtonBrowseClick);
            // 
            // _textBoxFile
            // 
            this._textBoxFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this._textBoxFile.Location = new System.Drawing.Point(1, 1);
            this._textBoxFile.Name = "_textBoxFile";
            this._textBoxFile.Size = new System.Drawing.Size(251, 20);
            this._textBoxFile.TabIndex = 0;
            this._textBoxFile.TextChanged += new System.EventHandler(this.HandleTextBoxFileTextChanged);
            // 
            // _openFileDialog
            // 
            this._openFileDialog.FileName = "openFileDialog1";
            // 
            // _buttonCreate
            // 
            this._buttonCreate.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this._buttonCreate.Enabled = false;
            this._buttonCreate.Location = new System.Drawing.Point(339, -1);
            this._buttonCreate.Name = "_buttonCreate";
            this._buttonCreate.Size = new System.Drawing.Size(75, 23);
            this._buttonCreate.TabIndex = 2;
            this._buttonCreate.Text = "C&reate";
            this._buttonCreate.UseVisualStyleBackColor = true;
            this._buttonCreate.Click += new System.EventHandler(this.HandleButtonCreateClick);
            // 
            // ConfigurationPicker
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this._buttonCreate);
            this.Controls.Add(this._buttonEdit);
            this.Controls.Add(this._buttonBrowse);
            this.Controls.Add(this._textBoxFile);
            this.Name = "ConfigurationPicker";
            this.Size = new System.Drawing.Size(417, 23);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button _buttonEdit;
        private System.Windows.Forms.Button _buttonBrowse;
        private System.Windows.Forms.TextBox _textBoxFile;
        private System.Windows.Forms.OpenFileDialog _openFileDialog;
        private System.Windows.Forms.Button _buttonCreate;
    }
}
