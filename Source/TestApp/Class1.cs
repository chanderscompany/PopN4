using System;
using DACarter.Utilities;
using DACarter.NOAA;

namespace TestApp
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class Class1 {
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args) {

			try {
				/*
				DacFileName fn = new DacFileName(FileType.TOGA);
				fn.StationName = ProfilerName.New;
				fn.TogaStationName = "qw";
				//fn.BeginDateTime = DacDateTime.FromDayOfYear(2003,64);
				fn.BeginDateTime = new DateTime(2003,3,5,23,0,0);
				fn.EndDateTime = DateTime.Now;
				fn.DataMode = ProfilerMode.High;
				fn.DataTimeSpan = FileTimeSpan.Days;
				fn.Folder = @"c:\junk";
				fn.Frequency = ProfilerFrequency.VHF;
				*/

				DacFileName fn = new DacFileName(FileType.DOMSAT);
				fn.StationName = ProfilerName.New;
				fn.DCPID = "12345678";
				//fn.StationName = ProfilerName.Biak50;
				//fn.BeginDateTime = DacDateTime.FromDayOfYear(2003,106);
				fn.BeginDateTime = new DateTime(2003,4,16,23,0,0);
				fn.Folder = @"c:\junk";

				// experiment with enums
				Console.WriteLine(ComData.HdrId.Alt + " has value {0:d}",ComData.HdrId.Alt);
				int id = 83;
				Console.WriteLine(" Index " + id + " is " + Enum.GetName(typeof(ComData.HdrId),id));
				Console.WriteLine(FileType.POPSpc + " has value {0:d}",FileType.POPSpc);
				Array values = Enum.GetValues(typeof(ComData.HdrId));
				Array names = Enum.GetNames(typeof(ComData.HdrId));
				/*
				foreach (int value in values) {
					// this fails if values are not consecutive:
					Console.WriteLine(value + " - " + names.GetValue(value));
				}
				*/
				for (int ii=0; ii<values.Length; ii++) {
					Console.WriteLine((int)values.GetValue(ii) + " - " + names.GetValue(ii));
				}
				Console.WriteLine("fn is " + fn.ToString());

				string[] fileNames;
				string[] pathNames;
				fileNames = fn.GetFileName();
				pathNames = fn.GetFullPathName();
				foreach (string filename in fileNames) {
					Console.WriteLine(filename);
				}
				foreach (string pathname in pathNames) {
					Console.WriteLine(pathname);
				}
			}
			catch (InvalidOperationException ex) {
				Console.WriteLine(ex.Message);
			}

			ComFile comFile = new ComFile();
			ComData data = new ComData();
			DateTime dt1,dt2;
			dt1 = DateTime.Now;
			try {
				comFile.SetFileName(@"X:\Com\ComData\cxi01\c001.002");
				while (comFile.ReadRecord(data)) {
					//ComData cd = data.Copy();
					Console.WriteLine("  " + data.Ht[0]);
				}
				
				Console.WriteLine("End of File");
			}
			catch (Exception ex) {
				Console.WriteLine(comFile.ToString() + ": " + ex.Message);
			}
			dt2 = DateTime.Now;
			TimeSpan sp = dt2-dt1;
			Console.WriteLine("Total time = " + sp.TotalMilliseconds);

			string Header = @"#Header lines:      43
#Most heights:      23 
#Data column:       1, station (_), a3, xxx
#Data column:       2, mode (_), a1, x
#Data column:       3, N latitude (deg), f6.2, 99.00
#Data column:       4, E longitude (deg), f7.2, 9999.00
#Data column:       5, year (UT), i4, 9999
#Data column:       6, day of year (UT), i3, 999
#Data column:       7, hour (UT), i2, 99
#Data column:       8, min (UT), i2, 99
#Data column:       9, total sec (since Jan. 1, 1970 UT), i10, 9999999999
#Data column:       10, ht (mASL), i7, 9999999
#Data column:       11, u (m/s), f7.2, 9999.00
#Data column:       12, v (m/s), f7.2, 9999.00
#Data column:       13, wid1 (m/s), f7.2, 9999.00
#Data column:       14, wid2 (m/s), f7.2, 9999.00
#Data column:       15, snr1 (dB), f6.1, 9999.0
#Data column:       16, snr2 (dB), f6.1, 9999.0
#Data column:       17, n12 (_), f6.1, 9999.0
#Data column:       18, sumwt12 (_), f7.4, 99.0000
#Data column:       19, wht (mASL), i7, 9999999
#Data column:       20, w (m/s), f7.2, 9999.00
#Data column:       21, wid3 (m/s), f7.2, 9999.00
#Data column:       22, snr3 (dB), f6.1, 9999.0
#Data column:       23, n3 (_), f6.1, 9999.0
#Data column:       24, sumwt3 (_), f7.4, 99.0000";

			PdmData pdmData = new PdmData();
			try {
				pdmData.InitFromHeader(Header);
				pdmData.AddNextLine(@"gal b  -0.90  -89.61 2001 070 00 00  984268800     100 9999.00 9999.00 9999.00 9999.00 9999.0 9999.0 9999.0 99.0000     100 9999.00 9999.00 9999.0 9999.0 99.0000
");
				Console.WriteLine("There are {0} data columns",pdmData.DataLabelList.Count);
				int[] ary = new Int32[5];
				if (ary.GetType().GetElementType() == typeof(System.Int32)) {
					Console.WriteLine("This is an array");
				}
				else {
					Console.WriteLine("Not");
				}
			}
			catch (Exception ex) {
				Console.WriteLine(pdmData.ToString() + ": " + ex.Message);
			}

		}
	}
}
