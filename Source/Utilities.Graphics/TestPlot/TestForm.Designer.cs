namespace TestPlot {
	partial class TestForm {
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
			this.buttonPlot = new System.Windows.Forms.Button();
			this.panel1 = new System.Windows.Forms.Panel();
			this.colorDialog1 = new System.Windows.Forms.ColorDialog();
			this.button1 = new System.Windows.Forms.Button();
			this.panel2 = new System.Windows.Forms.Panel();
			this.panel3 = new System.Windows.Forms.Panel();
			this.button2 = new System.Windows.Forms.Button();
			this.button3 = new System.Windows.Forms.Button();
			this.buttonBack = new System.Windows.Forms.Button();
			this.comboBoxColor = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.comboBoxPlotType = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.checkBoxBlend = new System.Windows.Forms.CheckBox();
			this.checkBoxExtend = new System.Windows.Forms.CheckBox();
			this.checkBoxRepeat = new System.Windows.Forms.CheckBox();
			this.buttonBackground = new System.Windows.Forms.Button();
			this.labelBackground = new System.Windows.Forms.Label();
			this.buttonRGB = new System.Windows.Forms.Button();
			this.buttonTest = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// buttonPlot
			// 
			this.buttonPlot.Location = new System.Drawing.Point(12, 12);
			this.buttonPlot.Name = "buttonPlot";
			this.buttonPlot.Size = new System.Drawing.Size(75, 23);
			this.buttonPlot.TabIndex = 0;
			this.buttonPlot.Text = "Plot";
			this.buttonPlot.UseVisualStyleBackColor = true;
			this.buttonPlot.Click += new System.EventHandler(this.buttonPlot_Click);
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.Color.OrangeRed;
			this.panel1.Location = new System.Drawing.Point(260, 20);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(118, 23);
			this.panel1.TabIndex = 1;
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(179, 17);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 2;
			this.button1.Text = "Pick Color 1";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// panel2
			// 
			this.panel2.BackColor = System.Drawing.Color.Coral;
			this.panel2.Location = new System.Drawing.Point(260, 41);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(118, 22);
			this.panel2.TabIndex = 3;
			// 
			// panel3
			// 
			this.panel3.BackColor = System.Drawing.Color.Red;
			this.panel3.Location = new System.Drawing.Point(260, 60);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(118, 22);
			this.panel3.TabIndex = 3;
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(179, 39);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(75, 23);
			this.button2.TabIndex = 4;
			this.button2.Text = "Pick Color 2";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.button2_Click);
			// 
			// button3
			// 
			this.button3.Location = new System.Drawing.Point(179, 61);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(75, 23);
			this.button3.TabIndex = 5;
			this.button3.Text = "Pick Color 3";
			this.button3.UseVisualStyleBackColor = true;
			this.button3.Click += new System.EventHandler(this.button3_Click);
			// 
			// buttonBack
			// 
			this.buttonBack.Location = new System.Drawing.Point(12, 41);
			this.buttonBack.Name = "buttonBack";
			this.buttonBack.Size = new System.Drawing.Size(75, 23);
			this.buttonBack.TabIndex = 6;
			this.buttonBack.Text = "Back";
			this.buttonBack.UseVisualStyleBackColor = true;
			this.buttonBack.Click += new System.EventHandler(this.buttonBack_Click);
			// 
			// comboBoxColor
			// 
			this.comboBoxColor.DropDownHeight = 200;
			this.comboBoxColor.FormattingEnabled = true;
			this.comboBoxColor.IntegralHeight = false;
			this.comboBoxColor.Location = new System.Drawing.Point(12, 119);
			this.comboBoxColor.Name = "comboBoxColor";
			this.comboBoxColor.Size = new System.Drawing.Size(141, 21);
			this.comboBoxColor.TabIndex = 7;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(13, 103);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(74, 13);
			this.label1.TabIndex = 8;
			this.label1.Text = "Color scheme:";
			// 
			// comboBoxPlotType
			// 
			this.comboBoxPlotType.FormattingEnabled = true;
			this.comboBoxPlotType.Location = new System.Drawing.Point(179, 118);
			this.comboBoxPlotType.Name = "comboBoxPlotType";
			this.comboBoxPlotType.Size = new System.Drawing.Size(96, 21);
			this.comboBoxPlotType.TabIndex = 9;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(179, 103);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(51, 13);
			this.label2.TabIndex = 10;
			this.label2.Text = "Plot type:";
			// 
			// checkBoxBlend
			// 
			this.checkBoxBlend.AutoSize = true;
			this.checkBoxBlend.Location = new System.Drawing.Point(293, 122);
			this.checkBoxBlend.Name = "checkBoxBlend";
			this.checkBoxBlend.Size = new System.Drawing.Size(53, 17);
			this.checkBoxBlend.TabIndex = 11;
			this.checkBoxBlend.Text = "Blend";
			this.checkBoxBlend.UseVisualStyleBackColor = true;
			// 
			// checkBoxExtend
			// 
			this.checkBoxExtend.AutoSize = true;
			this.checkBoxExtend.Location = new System.Drawing.Point(16, 147);
			this.checkBoxExtend.Name = "checkBoxExtend";
			this.checkBoxExtend.Size = new System.Drawing.Size(96, 17);
			this.checkBoxExtend.TabIndex = 12;
			this.checkBoxExtend.Text = "Extend Lowest";
			this.checkBoxExtend.UseVisualStyleBackColor = true;
			// 
			// checkBoxRepeat
			// 
			this.checkBoxRepeat.AutoSize = true;
			this.checkBoxRepeat.Location = new System.Drawing.Point(16, 171);
			this.checkBoxRepeat.Name = "checkBoxRepeat";
			this.checkBoxRepeat.Size = new System.Drawing.Size(93, 17);
			this.checkBoxRepeat.TabIndex = 13;
			this.checkBoxRepeat.Text = "Repeat Upper";
			this.checkBoxRepeat.UseVisualStyleBackColor = true;
			// 
			// buttonBackground
			// 
			this.buttonBackground.Location = new System.Drawing.Point(90, 193);
			this.buttonBackground.Name = "buttonBackground";
			this.buttonBackground.Size = new System.Drawing.Size(75, 23);
			this.buttonBackground.TabIndex = 15;
			this.buttonBackground.Text = "Pick Bkgd";
			this.buttonBackground.UseVisualStyleBackColor = true;
			this.buttonBackground.Click += new System.EventHandler(this.buttonBackground_Click);
			// 
			// labelBackground
			// 
			this.labelBackground.BackColor = System.Drawing.SystemColors.Window;
			this.labelBackground.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.labelBackground.Location = new System.Drawing.Point(12, 195);
			this.labelBackground.Name = "labelBackground";
			this.labelBackground.Size = new System.Drawing.Size(75, 20);
			this.labelBackground.TabIndex = 16;
			this.labelBackground.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// buttonRGB
			// 
			this.buttonRGB.Location = new System.Drawing.Point(185, 158);
			this.buttonRGB.Name = "buttonRGB";
			this.buttonRGB.Size = new System.Drawing.Size(75, 23);
			this.buttonRGB.TabIndex = 17;
			this.buttonRGB.Text = "R-G-B";
			this.buttonRGB.UseVisualStyleBackColor = true;
			this.buttonRGB.Click += new System.EventHandler(this.buttonRGB_Click);
			// 
			// buttonTest
			// 
			this.buttonTest.Location = new System.Drawing.Point(13, 71);
			this.buttonTest.Name = "buttonTest";
			this.buttonTest.Size = new System.Drawing.Size(75, 23);
			this.buttonTest.TabIndex = 18;
			this.buttonTest.Text = "Test";
			this.buttonTest.UseVisualStyleBackColor = true;
			this.buttonTest.Click += new System.EventHandler(this.buttonTest_Click);
			// 
			// TestForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(377, 253);
			this.Controls.Add(this.buttonTest);
			this.Controls.Add(this.buttonRGB);
			this.Controls.Add(this.labelBackground);
			this.Controls.Add(this.buttonBackground);
			this.Controls.Add(this.checkBoxRepeat);
			this.Controls.Add(this.checkBoxExtend);
			this.Controls.Add(this.checkBoxBlend);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.comboBoxPlotType);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.comboBoxColor);
			this.Controls.Add(this.buttonBack);
			this.Controls.Add(this.button3);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.panel3);
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.buttonPlot);
			this.Name = "TestForm";
			this.Text = "TestForm";
			this.Load += new System.EventHandler(this.TestForm_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonPlot;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.ColorDialog colorDialog1;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Button button3;
		private System.Windows.Forms.Button buttonBack;
		private System.Windows.Forms.ComboBox comboBoxColor;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox comboBoxPlotType;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.CheckBox checkBoxBlend;
		private System.Windows.Forms.CheckBox checkBoxExtend;
		private System.Windows.Forms.CheckBox checkBoxRepeat;
		private System.Windows.Forms.Button buttonBackground;
		private System.Windows.Forms.Label labelBackground;
		private System.Windows.Forms.Button buttonRGB;
		private System.Windows.Forms.Button buttonTest;
	}
}
