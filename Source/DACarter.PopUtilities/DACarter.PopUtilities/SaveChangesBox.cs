using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace DACarter.PopUtilities {
	public partial class SaveChangesBox : Form {

		[DllImport("user32")]
		public static extern int MessageBeep(int wType);

		public SaveChangesBox() {
			InitializeComponent();
		}

		private void buttonExit_Click(object sender, EventArgs e) {

		}

		private void buttonCancel_Click(object sender, EventArgs e) {

		}

		private void SaveChangesBox_Load(object sender, EventArgs e) {
			MessageBeep((int)MessageBoxIcon.Exclamation);
		}
	}
}
