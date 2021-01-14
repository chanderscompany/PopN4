using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
//using DACarter.PopUtilities;


namespace DACarter.PopUtilities {

	public class DacLogger {

        /// <summary>
        /// Writes at time stamped entry into log file in application folder.
        /// </summary>
        /// <param name="message"></param>
        public static void WriteEntry(string message) {

            string fullPath = Application.ExecutablePath;
            //string fileName = Path.GetFileNameWithoutExtension(fullPath);
            string folder = Path.GetDirectoryName(fullPath);
            WriteEntryToFolder(message, folder);

            return;
        }


        /// <summary>
        /// Writes at time stamped entry into log file in specified folder.
        /// </summary>
        /// <param name="message"></param>
        public static void WriteEntry(string message, string folder) {

            string fullPath = Application.ExecutablePath;
            //string fileName = Path.GetFileNameWithoutExtension(fullPath);
            //string folder = Path.GetDirectoryName(fullPath);
            WriteEntryToFolder(message, folder);

            return;
        }


        /// <summary>
        /// Like WriteEntry(), but write to folder specified in POPN state file.
        /// </summary>
        /// <param name="message"></param>
        public static void WriteEntryEx(string message) {
            string logFileFolder = PopStateFile.GetLogFolder();
            if (logFileFolder.Trim() != String.Empty) {
                WriteEntryToFolder(message, logFileFolder);
            }
            else {
                WriteEntry(message);
            }
        }

		/// <summary>
		/// WriteEntryToFolder
		/// </summary>
		/// <param name="message"></param>
		/// <param name="folder"></param>
		private static void WriteEntryToFolder(string message, string folder) {
			string appFullPath = Application.ExecutablePath;
			string fileName = Path.GetFileNameWithoutExtension(appFullPath);
			//string folder = Path.GetDirectoryName(fullPath);

			if (String.IsNullOrWhiteSpace(folder)) {
				folder = Path.GetDirectoryName(appFullPath);
			}
			DateTime dt = DateTime.Now;
			fileName = fileName + "_" + dt.Year + "_" + dt.DayOfYear.ToString("d3") + ".Log";
			string logFile = Path.Combine(folder, fileName);

			try {
				if (!Directory.Exists(folder)) {
					Directory.CreateDirectory(folder);
				}

				using (StreamWriter sw = new StreamWriter(logFile, true)) {
					sw.WriteLine(dt.ToShortDateString() + "  " + dt.ToString("HH:mm:ss") + " -- " + message);
				}
			}
			catch (Exception e) {
				Console.Beep(220,500);
				return;
			}

			return;
		}
	}

}
