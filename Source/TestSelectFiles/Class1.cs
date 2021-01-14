using System;
using System.IO;
using System.Text.RegularExpressions;

namespace TestSelectFiles
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class Class1
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			//
			// TODO: Add code to start application here
			//

			string folder = @"X:\DOMSAT\DCPMsgs\Piura\";
			//string fileNamePattern = @"[0-9a-f]{8}\.\d{6}\.20\d\d";
			string fileNamePattern = @"[a-zA-Z0-9.]*$";

			string fullNamePattern = @"[a-zA-Z]:[\\/][\\/\w]*" + fileNamePattern;

			DirectoryInfo DirInfo = new DirectoryInfo(folder);
			FileInfo[] fileInfos = DirInfo.GetFiles();
			string fileList = "";
			foreach (FileInfo file in fileInfos) {
				//Console.WriteLine(file.Name);
				fileList = fileList + file.Name + "\n";
			}
			Console.WriteLine(fileList);

			Console.WriteLine("\nMatches:");
			Regex re = new Regex(fileNamePattern,RegexOptions.IgnoreCase |
												RegexOptions.Multiline);
			MatchCollection matches = re.Matches(fileList);
			foreach (Match match in matches) {
				if ((match.Success) && (match.Length >= 0)) {
					Console.WriteLine(match.Value + " " + match.Index);
				}
			}
			Console.WriteLine("{0} matches found.",matches.Count);

		}
	}
}
