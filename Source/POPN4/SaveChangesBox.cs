using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace POPN {
	public partial class SaveChangesBox : Form {

		[DllImport("user32")]
		public static extern int MessageBeep(int wType);

		public SaveChangesBox() {
			InitializeComponent();
		}

		private void buttonExit_Click(object sender, EventArgs e) {
			// DialogResult property of button is returned by ShowDialog method
		}

		private void buttonCancel_Click(object sender, EventArgs e) {
			// DialogResult property of button is returned by ShowDialog method
		}

		private void SaveChangesBox_Load(object sender, EventArgs e) {
			MessageBeep((int)MessageBoxIcon.Exclamation);
		}
	}
}
