namespace POPN4 {
    partial class PowerMeterDisplay {
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
            this.labelPower = new System.Windows.Forms.Label();
            this.labelTemp = new System.Windows.Forms.Label();
            this.labelOffset = new System.Windows.Forms.Label();
            this.labelFreq = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // labelPower
            // 
            this.labelPower.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.labelPower.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelPower.Location = new System.Drawing.Point(24, 9);
            this.labelPower.Name = "labelPower";
            this.labelPower.Size = new System.Drawing.Size(170, 42);
            this.labelPower.TabIndex = 0;
            this.labelPower.Text = "-888.8 dBm";
            this.labelPower.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelTemp
            // 
            this.labelTemp.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.labelTemp.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelTemp.Location = new System.Drawing.Point(24, 58);
            this.labelTemp.Name = "labelTemp";
            this.labelTemp.Size = new System.Drawing.Size(170, 36);
            this.labelTemp.TabIndex = 1;
            this.labelTemp.Text = "-88.8 F";
            this.labelTemp.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelOffset
            // 
            this.labelOffset.Location = new System.Drawing.Point(58, 97);
            this.labelOffset.Name = "labelOffset";
            this.labelOffset.Size = new System.Drawing.Size(110, 23);
            this.labelOffset.TabIndex = 2;
            this.labelOffset.Text = "Offset = 999 dB";
            this.labelOffset.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelFreq
            // 
            this.labelFreq.Location = new System.Drawing.Point(58, 117);
            this.labelFreq.Name = "labelFreq";
            this.labelFreq.Size = new System.Drawing.Size(110, 23);
            this.labelFreq.TabIndex = 3;
            this.labelFreq.Text = "Freq = 99999 MHz";
            this.labelFreq.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // PowerMeterDisplay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.SeaShell;
            this.ClientSize = new System.Drawing.Size(219, 143);
            this.Controls.Add(this.labelFreq);
            this.Controls.Add(this.labelOffset);
            this.Controls.Add(this.labelTemp);
            this.Controls.Add(this.labelPower);
            this.MaximizeBox = false;
            this.Name = "PowerMeterDisplay";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "PowerMeter";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label labelPower;
        private System.Windows.Forms.Label labelTemp;
        private System.Windows.Forms.Label labelOffset;
        private System.Windows.Forms.Label labelFreq;
    }
}