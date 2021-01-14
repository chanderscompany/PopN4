using System;
using System.Collections.Generic;
using System.ComponentModel;
//using System.Data;
using System.Drawing;
//using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

using DACarter.Utilities;

namespace POPN4 {
    public partial class SequenceForm : Form {

        private string _folder;
        private string _seqFileFullPath;
        private string _seqFileName;

        private SequenceForm() {
            InitializeComponent();
        }

        public SequenceForm(string seqFileFullPath) {

            _seqFileFullPath = seqFileFullPath;
            _folder = Path.GetDirectoryName(_seqFileFullPath);
            _seqFileName = Path.GetFileName(_seqFileFullPath);

            InitializeComponent();

            this.Text = _seqFileName;

            FillParxFileList();
            InitSeqList();
        }

        public void FillParxFileList() {

            listBoxAllParxFiles.Items.Clear();
            string[] parxFiles = Directory.GetFiles(_folder, "*.parx");
            foreach (string fileName in parxFiles) {
                string newFileName = Path.GetFileName(fileName);
                listBoxAllParxFiles.Items.Add(newFileName);
            }
        }

        public void InitSeqList() {
            //using  {};
            TextFile seqFile = new TextFile(_seqFileFullPath, openForWriting: false);
            string fileName;
            do {
                fileName = seqFile.ReadLine();
                if (!string.IsNullOrWhiteSpace(fileName)) {
                    listBoxFilesInSeq.Items.Add(fileName);
                }
            } while (fileName != null);
            seqFile.Close();
        }

        private void buttonAdd_Click(object sender, EventArgs e) {
            int parIndex = listBoxAllParxFiles.SelectedIndex;
            int seqIndex = listBoxFilesInSeq.SelectedIndex;
            string fileName;
            if (parIndex >= 0) {
                fileName = (string)listBoxAllParxFiles.Items[parIndex];
                if (seqIndex >= 0) {
                    listBoxFilesInSeq.Items.Insert(seqIndex + 1, fileName);
                    listBoxFilesInSeq.SelectedIndex = seqIndex + 1;
                }
                else {
                    listBoxFilesInSeq.Items.Add(fileName);
                    listBoxFilesInSeq.SelectedIndex = listBoxFilesInSeq.Items.Count - 1;
                }
            }
        }

        private void buttonRemove_Click(object sender, EventArgs e) {
            int seqIndex = listBoxFilesInSeq.SelectedIndex;
            string fileName;
            if (seqIndex >= 0) {
                listBoxFilesInSeq.Items.RemoveAt(seqIndex);
            }
        }

        private void buttonClear_Click(object sender, EventArgs e) {
            listBoxFilesInSeq.Items.Clear();
        }

        private void buttonMoveUp_Click(object sender, EventArgs e) {
            int seqIndex = listBoxFilesInSeq.SelectedIndex;
            string fileName;
            if (seqIndex > 0) {
                fileName = (string)listBoxFilesInSeq.Items[seqIndex];
                listBoxFilesInSeq.Items.RemoveAt(seqIndex);
                listBoxFilesInSeq.Items.Insert(seqIndex - 1, fileName);
                listBoxFilesInSeq.SelectedIndex = seqIndex - 1;
            }
        }

        private void buttonMoveDown_Click(object sender, EventArgs e) {
            int seqIndex = listBoxFilesInSeq.SelectedIndex;
            string fileName;
            int lastIndex = listBoxFilesInSeq.Items.Count - 1;
            if (seqIndex >= 0 && seqIndex < lastIndex) {
                fileName = (string)listBoxFilesInSeq.Items[seqIndex];
                listBoxFilesInSeq.Items.RemoveAt(seqIndex);
                listBoxFilesInSeq.Items.Insert(seqIndex + 1, fileName);
                listBoxFilesInSeq.SelectedIndex = seqIndex + 1;
            }
        }

        private void buttonRepeat_Click(object sender, EventArgs e) {
            int seqIndex = listBoxFilesInSeq.SelectedIndex;
            string fileName;
            if (seqIndex >= 0) {
                fileName = (string)listBoxFilesInSeq.Items[seqIndex];
                listBoxFilesInSeq.Items.Insert(seqIndex + 1, fileName);
                listBoxFilesInSeq.SelectedIndex = seqIndex + 1;
            }
        }

        private void buttonSave_Click(object sender, EventArgs e) {
            SaveSeqFile(_seqFileFullPath);
        }

        private void buttonSaveAs_Click(object sender, EventArgs e) {

            saveFileDialog1.InitialDirectory = _folder;
            saveFileDialog1.FileName = _seqFileName;
            saveFileDialog1.DefaultExt = ".seq";
            saveFileDialog1.Filter = "POPN Sequence Files (*.seq) | *.seq";
            saveFileDialog1.CheckPathExists = true;
            DialogResult rr = saveFileDialog1.ShowDialog();
            if (rr == DialogResult.OK) {
                string ext = Path.GetExtension(saveFileDialog1.FileName);
                if (ext.ToLower() == ".seq") {
                    string fullFileName = saveFileDialog1.FileName;
                    string folder = Path.GetDirectoryName(fullFileName);
                    string fileName = Path.GetFileName(fullFileName);
                    bool isOK = SaveSeqFile(fullFileName);
                    if (isOK) {
                        _folder = folder;
                        _seqFileFullPath = fullFileName;
                        _seqFileName = fileName;
                        this.Text = fileName;
                    }
                }
                else {
                    MessageBox.Show("Don't know how to save " + ext + " files yet.", "File NOT saved");
                }
            }
        }

        private bool SaveSeqFile(string fullFileName) {
            try {
                TextFile seqFile = new TextFile(fullFileName, openForWriting: true, append: false);
                foreach (string fileName in listBoxFilesInSeq.Items) {
                    seqFile.WriteLine(fileName);
                }
                seqFile.Close();
                MessageBoxEx.Show("File Saved OK", 800);
            }
            catch (Exception e) {
                MessageBox.Show(e.Message, "Error Saving seq File");
                return false;
            }
            return true;
        }

        private void listBoxAllParxFiles_DoubleClick(object sender, EventArgs e) {
            buttonAdd_Click(null, null);
        }

    }   // end class

}   // end namespace
