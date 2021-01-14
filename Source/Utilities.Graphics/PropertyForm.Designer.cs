namespace DACarter.Utilities.Graphics {
	partial class PropertyForm {
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
			this.PlotPropertyGrid = new System.Windows.Forms.PropertyGrid();
			this.SuspendLayout();
			// 
			// PlotPropertyGrid
			// 
			this.PlotPropertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.PlotPropertyGrid.Location = new System.Drawing.Point(-1, 1);
			this.PlotPropertyGrid.Name = "PlotPropertyGrid";
			this.PlotPropertyGrid.Size = new System.Drawing.Size(328, 406);
			this.PlotPropertyGrid.TabIndex = 0;
			// 
			// PropertyForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(325, 407);
			this.Controls.Add(this.PlotPropertyGrid);
			this.Name = "PropertyForm";
			this.Text = "QuickPlot Properties";
			this.ResumeLayout(false);

		}

		#endregion

		public System.Windows.Forms.PropertyGrid PlotPropertyGrid;

	}
}
