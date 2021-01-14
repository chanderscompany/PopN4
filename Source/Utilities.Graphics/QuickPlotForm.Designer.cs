namespace DACarter.Utilities.Graphics {
	partial class QuickPlotForm {
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
			this.SuspendLayout();
			// 
			// QuickPlotForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(477, 340);
			this.Name = "QuickPlotForm";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.Text = "QuickPlot";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.ResizeBegin += new System.EventHandler(this.QuickPlotForm_ResizeBegin);
			this.ClientSizeChanged += new System.EventHandler(this.Form1_ClientSizeChanged);
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.QuickPlotForm_Paint);
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.QuickPlotForm_FormClosed);
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
			this.ResizeEnd += new System.EventHandler(this.QuickPlotForm_ResizeEnd);
			this.ResumeLayout(false);

		}

		#endregion
	}
}
