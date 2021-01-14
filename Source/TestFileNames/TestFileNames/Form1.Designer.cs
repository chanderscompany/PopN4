namespace TestFileNames {
	partial class Form1 {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.textBoxFilePath = new System.Windows.Forms.TextBox();
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.buttonBrowse = new System.Windows.Forms.Button();
			this.textBoxFileName = new System.Windows.Forms.TextBox();
			this.textBoxFileType = new System.Windows.Forms.TextBox();
			this.textBoxTimeStamp1 = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.textBoxTimeStamp2 = new System.Windows.Forms.TextBox();
			this.label5 = new System.Windows.Forms.Label();
			this.textBoxPopPrefix = new System.Windows.Forms.TextBox();
			this.buttonParse = new System.Windows.Forms.Button();
			this.label6 = new System.Windows.Forms.Label();
			this.textBoxSite = new System.Windows.Forms.TextBox();
			this.buttonRevParse = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// textBoxFilePath
			// 
			this.textBoxFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxFilePath.Location = new System.Drawing.Point(13, 30);
			this.textBoxFilePath.Name = "textBoxFilePath";
			this.textBoxFilePath.Size = new System.Drawing.Size(329, 20);
			this.textBoxFilePath.TabIndex = 0;
			this.textBoxFilePath.Text = "  Choose a file...";
			// 
			// openFileDialog1
			// 
			this.openFileDialog1.FileName = "openFileDialog1";
			// 
			// buttonBrowse
			// 
			this.buttonBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonBrowse.BackColor = System.Drawing.Color.Honeydew;
			this.buttonBrowse.Cursor = System.Windows.Forms.Cursors.Hand;
			this.buttonBrowse.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.buttonBrowse.Location = new System.Drawing.Point(374, 29);
			this.buttonBrowse.Name = "buttonBrowse";
			this.buttonBrowse.Size = new System.Drawing.Size(75, 23);
			this.buttonBrowse.TabIndex = 1;
			this.buttonBrowse.Text = "Browse";
			this.buttonBrowse.UseVisualStyleBackColor = false;
			this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
			// 
			// textBoxFileName
			// 
			this.textBoxFileName.Location = new System.Drawing.Point(85, 101);
			this.textBoxFileName.Name = "textBoxFileName";
			this.textBoxFileName.Size = new System.Drawing.Size(257, 20);
			this.textBoxFileName.TabIndex = 2;
			// 
			// textBoxFileType
			// 
			this.textBoxFileType.Location = new System.Drawing.Point(85, 129);
			this.textBoxFileType.Name = "textBoxFileType";
			this.textBoxFileType.Size = new System.Drawing.Size(257, 20);
			this.textBoxFileType.TabIndex = 3;
			// 
			// textBoxTimeStamp1
			// 
			this.textBoxTimeStamp1.Location = new System.Drawing.Point(85, 157);
			this.textBoxTimeStamp1.Name = "textBoxTimeStamp1";
			this.textBoxTimeStamp1.Size = new System.Drawing.Size(257, 20);
			this.textBoxTimeStamp1.TabIndex = 4;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(13, 105);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(54, 13);
			this.label1.TabIndex = 5;
			this.label1.Text = "FileName:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(13, 133);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(50, 13);
			this.label2.TabIndex = 6;
			this.label2.Text = "FileType:";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(13, 161);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(69, 13);
			this.label3.TabIndex = 7;
			this.label3.Text = "TimeStamp1:";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(13, 189);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(69, 13);
			this.label4.TabIndex = 8;
			this.label4.Text = "TimeStamp2:";
			// 
			// textBoxTimeStamp2
			// 
			this.textBoxTimeStamp2.Location = new System.Drawing.Point(86, 185);
			this.textBoxTimeStamp2.Name = "textBoxTimeStamp2";
			this.textBoxTimeStamp2.Size = new System.Drawing.Size(257, 20);
			this.textBoxTimeStamp2.TabIndex = 9;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(13, 217);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(55, 13);
			this.label5.TabIndex = 10;
			this.label5.Text = "PopPrefix:";
			// 
			// textBoxPopPrefix
			// 
			this.textBoxPopPrefix.Location = new System.Drawing.Point(86, 213);
			this.textBoxPopPrefix.Name = "textBoxPopPrefix";
			this.textBoxPopPrefix.Size = new System.Drawing.Size(257, 20);
			this.textBoxPopPrefix.TabIndex = 11;
			// 
			// buttonParse
			// 
			this.buttonParse.BackColor = System.Drawing.Color.Aquamarine;
			this.buttonParse.Cursor = System.Windows.Forms.Cursors.Hand;
			this.buttonParse.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.buttonParse.Location = new System.Drawing.Point(125, 63);
			this.buttonParse.Name = "buttonParse";
			this.buttonParse.Size = new System.Drawing.Size(75, 23);
			this.buttonParse.TabIndex = 12;
			this.buttonParse.Text = "Parse";
			this.buttonParse.UseVisualStyleBackColor = false;
			this.buttonParse.Click += new System.EventHandler(this.buttonParse_Click);
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(16, 245);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(28, 13);
			this.label6.TabIndex = 13;
			this.label6.Text = "Site:";
			// 
			// textBoxSite
			// 
			this.textBoxSite.Location = new System.Drawing.Point(86, 241);
			this.textBoxSite.Name = "textBoxSite";
			this.textBoxSite.Size = new System.Drawing.Size(257, 20);
			this.textBoxSite.TabIndex = 14;
			// 
			// buttonRevParse
			// 
			this.buttonRevParse.BackColor = System.Drawing.Color.PowderBlue;
			this.buttonRevParse.Cursor = System.Windows.Forms.Cursors.Hand;
			this.buttonRevParse.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.buttonRevParse.Location = new System.Drawing.Point(233, 62);
			this.buttonRevParse.Name = "buttonRevParse";
			this.buttonRevParse.Size = new System.Drawing.Size(75, 23);
			this.buttonRevParse.TabIndex = 15;
			this.buttonRevParse.Text = "Rev Parse";
			this.buttonRevParse.UseVisualStyleBackColor = false;
			this.buttonRevParse.Click += new System.EventHandler(this.buttonRevParse_Click);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.AntiqueWhite;
			this.ClientSize = new System.Drawing.Size(476, 299);
			this.Controls.Add(this.buttonRevParse);
			this.Controls.Add(this.textBoxSite);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.buttonParse);
			this.Controls.Add(this.textBoxPopPrefix);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.textBoxTimeStamp2);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.textBoxTimeStamp1);
			this.Controls.Add(this.textBoxFileType);
			this.Controls.Add(this.textBoxFileName);
			this.Controls.Add(this.buttonBrowse);
			this.Controls.Add(this.textBoxFilePath);
			this.Name = "Form1";
			this.Text = "Test FileNameParser";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox textBoxFilePath;
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private System.Windows.Forms.Button buttonBrowse;
		private System.Windows.Forms.TextBox textBoxFileName;
		private System.Windows.Forms.TextBox textBoxFileType;
		private System.Windows.Forms.TextBox textBoxTimeStamp1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox textBoxTimeStamp2;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox textBoxPopPrefix;
		private System.Windows.Forms.Button buttonParse;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.TextBox textBoxSite;
		private System.Windows.Forms.Button buttonRevParse;
	}
}

