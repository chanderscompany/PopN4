using System;
using System.IO;
using System.Windows.Forms;

namespace DACarter.Utilities
{
	/// <summary>
	/// Helpful methods to read/write text from/to a file.
	///		Static write methods:
	///			// easy, for short writes
	///			WriteLineToFile(string fileName, string text, bool append)
	///		Instance write methods:
	///			// faster for large number of writes
	///			TextFile writer = new TextFile();
	///			writer.OpenForWriting(string fileName, bool append);
	///			writer.WriteLine(string line);
	///			writer.Close();
	///		Instance read methods:
	///			TextFile reader = new TextFile();
	///			reader.OpenForReading(string fileName);
	///			string line = reader.ReadLine();
	///			reader.Close();
	/// </summary>
	public class TextFile
	{

		private StreamReader _reader;
		private StreamWriter _writer;

		/// <summary>
		/// Static methods are simple ways to write to a text file.
		/// Writes one string at a time, opening and closing file for each line written.
		/// Appends string to existing file or creates a new file.
		/// No CR-LF added at end of text string.
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="text"></param>
		/// <returns></returns>
		public static bool WriteTextToFile(string fileName, string text, bool append, bool addCrToLF) {
			try {
				string path = Path.GetDirectoryName(fileName);
                if (path != String.Empty) {
                    if (!Directory.Exists(path)) {
                        Directory.CreateDirectory(path);
                    }
                }
                else {
                    path = Application.StartupPath;
                    fileName = Path.Combine(path, fileName);
                }
				using (StreamWriter writer = new StreamWriter(fileName, append) ) {
					if (addCrToLF) {
						string text2 = text.Replace("\n", "\r\n");
						writer.Write(text2);
					}
					else {
						writer.Write(text);
					}
				}
			}
			catch (Exception e) {
				return false;
			}
			return true;
		}

		/// <summary>
		/// Static methods are simple ways to write to a text file.
		/// Writes one string at a time, opening and closing file for each line written.
		/// Appends string to existing file or creates a new file.
		/// Appends a carriage-return / line-feed to text.
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="text"></param>
		/// <param name="append"></param>
		/// <returns></returns>
		public static bool WriteLineToFile(string fileName, string text, bool append) {
			try {
				string path = Path.GetDirectoryName(fileName);
				if (path != String.Empty) {
					if (!Directory.Exists(path)) {
						Directory.CreateDirectory(path);
					}
				}
                else {
                    path = Application.StartupPath;
                    fileName = Path.Combine(path, fileName);
                }
                using (StreamWriter writer = new StreamWriter(fileName, append)) {
					string line = text.Replace("\n", "\r\n");
					writer.WriteLine(line);
				}
			}
			catch (Exception e) {
				return false;
			}
			return true;
		}


		public static bool WriteLineToFile(string fileName, string text) {
			return WriteLineToFile(fileName, text, true);
		}

		public TextFile() {
			_reader = null;
		}

		public TextFile(string fileName, bool openForWriting, bool append) {
			bool openOK = true;
            string path = Path.GetDirectoryName(fileName);
            if (String.IsNullOrEmpty(path)) {
                path = Application.StartupPath;
                fileName = Path.Combine(path, fileName);
            }
            if (!openForWriting) {
				if (!File.Exists(fileName)) {
					throw new ApplicationException("TextFile file does not exist for reading");
				}
				openOK = OpenForReading(fileName);
				//_reader = File.OpenText(fileName);
			}
			else {
				openOK = OpenForWriting(fileName, append);
			}
			if (!openOK) {
				throw new ApplicationException("Error opening TextFile file.");
			}
		}

		public TextFile(string fileName, bool openForWriting)
			: this(fileName, openForWriting, true) {}

		/// <summary>
		/// Easy way to open file for reading without using TextFile non-default constructors
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public bool OpenForReading(string fileName) {
			if (_reader != null) {
				return false;
			}
            string path = Path.GetDirectoryName(fileName);
            if (String.IsNullOrEmpty(path)) {
                path = Application.StartupPath;
                fileName = Path.Combine(path, fileName);
            }
            if (!File.Exists(fileName)) {
				return false;
			}
			try {
				_reader = File.OpenText(fileName);
				return true;
			}
			catch (Exception) {
				return false;
			}
		}

		public string ReadLine() {
			if (_reader != null) {
				try {
					if (!_reader.EndOfStream) {
						return _reader.ReadLine();
					}
					else {
						return null;
					}
				}
				catch (Exception) {
					return null;
				}
			}
			else {
				return null;
			}
		}

		public bool OpenForWriting(string fileName, bool append) {
			if (_writer != null) {
				return false;
			}
			try {
				string path = Path.GetDirectoryName(fileName);
				if (path != String.Empty) {
					if (!Directory.Exists(path)) {
						Directory.CreateDirectory(path);
					}
				}
				_writer = new StreamWriter(fileName, append);
				
			}
			catch (Exception) {
				return false;
			}
			return true;
		}

		public bool WriteLine(string line) {
			try {
				line = line.Replace("\n", "\r\n");
				_writer.WriteLine(line);
			}
			catch (Exception) {
				return false;
			}
			return true;
		}

		public void Close() {
			if (_reader != null) {
				_reader.Close();
				_reader = null;
			}
			if (_writer != null) {
				_writer.Close();
				_writer = null;
			}
		}

		~TextFile() {
			Close();
		}


	}   // end of class TextFile

}   // end of namespace
