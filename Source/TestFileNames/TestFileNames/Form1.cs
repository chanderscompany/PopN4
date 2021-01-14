using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

using DACarter.NOAA;
using DACarter.Utilities;


namespace TestFileNames {
	public partial class Form1 : Form {

		private string _filePath = "";
		private string _fileName = "";
		private FileNameParser.ParsedFileNameStruct _info;

		public Form1() {
			InitializeComponent();
		}

		private void buttonBrowse_Click(object sender, EventArgs e) {
			DialogResult rr = openFileDialog1.ShowDialog();
			if (rr == DialogResult.OK) {
				_filePath = openFileDialog1.FileName;
				textBoxFilePath.Text = _filePath;
			}
		}

		private void buttonParse_Click(object sender, EventArgs e) {
			FileNameParser parser = new FileNameParser();
			//parser.AddGenericYydddRegex();

			_fileName = Path.GetFileName(_filePath);
			parser.Parse(_fileName, out _info);

			textBoxFileName.Text = _fileName;
			textBoxFileType.Text = _info.FileType.ToString();
			textBoxTimeStamp1.Text = _info.TimeStamp.ToString("HH:mm:ss   MMM dd, yyyy") + "  Day " + _info.TimeStamp.DayOfYear ;
			textBoxTimeStamp2.Text = _info.EndTimeStamp.ToString("HH:mm:ss   MMM dd, yyyy") + "  Day " + _info.EndTimeStamp.DayOfYear;
			textBoxPopPrefix.Text = _info.PopPrefix;
			textBoxSite.Text = _info.Site;
		}

		private void buttonRevParse_Click(object sender, EventArgs e) {

			FileNameParser parser = new FileNameParser();
			parser.AddGenericYydddRegex();

			_fileName = Path.GetFileName(_filePath);
			parser.ParseReverse(_fileName, out _info);

			textBoxFileName.Text = _fileName;
			textBoxFileType.Text = _info.FileType.ToString();
			textBoxTimeStamp1.Text = _info.TimeStamp.ToString("HH:mm:ss   MMM dd, yyyy") + "  Day " + _info.TimeStamp.DayOfYear;
			textBoxTimeStamp2.Text = _info.EndTimeStamp.ToString("HH:mm:ss   MMM dd, yyyy") + "  Day " + _info.EndTimeStamp.DayOfYear;
			textBoxPopPrefix.Text = _info.PopPrefix;
			textBoxSite.Text = _info.Site;
		}
	}
}
