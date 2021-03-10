using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;

using DACarter.Utilities;

namespace DACarter.NOAA {

	public class FileNameParser {

		#region DataFileType Enumeration
		/*
		// NOTE: This enum is now member of DACarter.NOAA namespace and
		//	can be found in DacDataFileBase.cs
		public enum DataFileType {
			PopDayFile,				// Dyydddx.SPC
			PopHourFile,			// Dyydddx.SPC (x=c...z)
			LapxmPopDayFile,		// Dsssyydddx.SPC
			LapxmPopHourFile,		// Dsssyydddhhx.SPC
			CnsDayFile,				// Wyyddd.CNS (also Tyy...)
			EtlCnsHourFile,			// sssyyddd.hhW (also ...hhT)
			ComLoopFile,			// LOOPddd or LOOPddd.yy
			ComCFile,				// Cddd.ddd
			PdtFile,				// sss_IA_yyyy.txt and ...
			TogaTarFile,			// ssHyyddX.TAR.Z
			TogaZipFile,			// 
			TogaFile,				// ssHyyddd.jja (jj = half hour index)
			DisdrometerFile,		// DDyyyy_mmdd_hh.TXT
			CeilometerFile,			// Dyyddd.CT
			DigitalThermometerFile,	// DTyydddx.TXT
			CampbellCr10File,		// CRyydddx.txt
			DomsatSingleMsgFile,	// XXXXXXXX.mmddyy.hhmm
			DomsatMultipleMsgFile,	// XXXXXXXX.yyyy_mm.TXT
			AsciiMomentsFile,		// ydddhhmm.mod and *.moh
			CrossbowDMU,			// yydddhh.DAT (or DMUyydddhh.txt)
			SmoGps,					// GPS1.OUT	(Scripps Marine Observatory GPS file)
			Radiosonde1,			// ssshdd.TXT or ssshhdd.TXT
			DarwinMsgArchive,		// Ayyddd_freq.dat
			UnknownFile,			// used for unrecognized file
			Undetermined,			// used when type not checked yet
			Other					// A known type with unspecified name
		}
		*/
		#endregion

		/*
		public struct ParsedFileNameStruct {
			public string Site;
			public DateTime TimeStamp;
			public DateTime EndTimeStamp;
			public string PopPrefix;
			public DataFileType FileType;
		}
		*/

		#region List of all Regex expressions for matching file names
		// POP day file; file = DacPopFile; data = PopData
        private static Regex regexPopDay = new Regex(@"^(?<popprefix>[DH])(?<year>\d\d)(?<day>\d\d\d)[A-B].*\.((SPC)|(MOM))$",
            RegexOptions.IgnoreCase |
            RegexOptions.ExplicitCapture |
            RegexOptions.Compiled);
        // POP hour file; file = DacPopFile; data = PopData
        // matches POP file names with hour given by letter (assumes suffx c-z is always an hour letter)
        private static Regex regexPopHour = new Regex(@"^(?<popprefix>[DH])(?<year>\d\d)(?<day>\d\d\d)(?<hourletter>[C-Z]).*\.((SPC)|(MOM))$",
            RegexOptions.IgnoreCase |
            RegexOptions.ExplicitCapture |
            RegexOptions.Compiled);
        // POP raw timeseries day file; file = DacPopFile; data = PopData
        private static Regex regexPopDayRawTS = new Regex(@"^(?<popprefix>[DH])(?<year>\d\d)(?<day>\d\d\d)[A-B].*\.(RAW.TS)$",
            RegexOptions.IgnoreCase |
            RegexOptions.ExplicitCapture |
            RegexOptions.Compiled);
        // POP raw time series hour file; file = DacPopFile; data = PopData
        // matches POP file names with hour given by letter (assumes suffx c-z is always an hour letter)
        private static Regex regexPopHourRawTS = new Regex(@"^(?<popprefix>[DH])(?<year>\d\d)(?<day>\d\d\d)(?<hourletter>[C-Z]).*\.(RAW.TS)$",
            RegexOptions.IgnoreCase |
            RegexOptions.ExplicitCapture |
            RegexOptions.Compiled);
        // POP timeseries day file; file = DacPopFile; data = PopData
        private static Regex regexPopDayTS = new Regex(@"^(?<popprefix>[DH])(?<year>\d\d)(?<day>\d\d\d)[A-B].*\.(TS)$",
            RegexOptions.IgnoreCase |
            RegexOptions.ExplicitCapture |
            RegexOptions.Compiled);
        // POP time series hour file; file = DacPopFile; data = PopData
        // matches POP file names with hour given by letter (assumes suffx c-z is always an hour letter)
        // Now obsolete and conflicts with next POP hour time series file
        /*
        private static Regex regexPopHourTS = new Regex(@"^(?<popprefix>[DH])(?<year>\d\d)(?<day>\d\d\d)(?<hourletter>[C-Z]).*\.(TS)$",
            RegexOptions.IgnoreCase |
            RegexOptions.ExplicitCapture |
            RegexOptions.Compiled);
        */
        // POP timeseries hour file; file = DacPopFile; data = PopData
        private static Regex regexPopHourTS = new Regex(@"^(?<popprefix>[DH])(?<year>\d\d)(?<day>\d\d\d)(?<hour>\d\d)[A-Z].*\.(TS)$",
            RegexOptions.IgnoreCase |
            RegexOptions.ExplicitCapture |
            RegexOptions.Compiled);
        // LapXM POP day file; file = DacPopFile; data = PopData
        private static Regex regexLapxmPopDay = new Regex(@"^(?<popprefix>[DH])(?<site>\w\w\w)(?<year>\d\d)(?<day>\d\d\d)[A-Z].*\.((SPC)|(MOM))$",
            RegexOptions.IgnoreCase |
            RegexOptions.ExplicitCapture |
            RegexOptions.Compiled);
        // LapXM POP hour file; file = DacPopFile; data = PopData
        private static Regex regexLapxmPopHour = new Regex(@"^(?<popprefix>[DH])(?<site>\w\w\w)(?<year>\d\d)(?<day>\d\d\d)(?<hour>\d\d)[A-Z].*\.((SPC)|(MOM))$",
            RegexOptions.IgnoreCase |
            RegexOptions.ExplicitCapture |
            RegexOptions.Compiled);
        // LapXM POP raw ts day file; file = DacPopFile; data = PopData
        private static Regex regexLapxmPopDayRawTS = new Regex(@"^(?<popprefix>[DH])(?<site>\w\w\w)(?<year>\d\d)(?<day>\d\d\d)[A-Z].*\.(RAW.TS)$",
            RegexOptions.IgnoreCase |
            RegexOptions.ExplicitCapture |
            RegexOptions.Compiled);
        // LapXM POP raw ts hour file; file = DacPopFile; data = PopData
        private static Regex regexLapxmPopHourRawTS = new Regex(@"^(?<popprefix>[DH])(?<site>\w\w\w)(?<year>\d\d)(?<day>\d\d\d)(?<hour>\d\d)[A-Z].*\.(RAW.TS)$",
            RegexOptions.IgnoreCase |
            RegexOptions.ExplicitCapture |
            RegexOptions.Compiled);
        // LapXM POP timeseries day file; file = DacPopFile; data = PopData
        private static Regex regexLapxmPopDayTS = new Regex(@"^(?<popprefix>[DH])(?<site>\w\w\w)(?<year>\d\d)(?<day>\d\d\d)[A-Z].*\.(TS)$",
            RegexOptions.IgnoreCase |
            RegexOptions.ExplicitCapture |
            RegexOptions.Compiled);
        // LapXM POP timeseries hour file; file = DacPopFile; data = PopData
        private static Regex regexLapxmPopHourTS = new Regex(@"^(?<popprefix>[DH])(?<site>\w\w\w)(?<year>\d\d)(?<day>\d\d\d)(?<hour>\d\d)[A-Z].*\.(TS)$",
            RegexOptions.IgnoreCase |
            RegexOptions.ExplicitCapture |
            RegexOptions.Compiled);
        // Consensus day file; file = DacCnsFile; data = WindsData
		private static Regex regexCnsDay = new Regex(@"^[WT](?<year>\d\d)(?<day>\d\d\d).*\.CNS$",
			RegexOptions.IgnoreCase |
			RegexOptions.ExplicitCapture |
			RegexOptions.Compiled);
		// Consensus day file; file = EtlCnsFile; data = WindsData
		private static Regex regexEtlCnsHour = new Regex(@"^(?<site>\w\w\w)(?<year>\d\d)(?<day>\d\d\d)\.(?<hour>\d\d)(W|T)$",
			RegexOptions.IgnoreCase |
			RegexOptions.ExplicitCapture |
			RegexOptions.Compiled);
		// COM loop file (single day); file = DacComFile; data = PopData
		private static Regex regexComLoop = new Regex(@"^LOOP(?<day>\d\d\d)\.?(?<year>(\d\d)?)$",
			RegexOptions.IgnoreCase |
			RegexOptions.ExplicitCapture |
			RegexOptions.Compiled);
		// COM C file (multiple days); file = DacComFile; data = PopData
		private static Regex regexComC = new Regex(@"^C(?<day>\d\d\d)\.(?<dayend>\d\d\d)$",
			RegexOptions.IgnoreCase |
			RegexOptions.ExplicitCapture |
			RegexOptions.Compiled);
		// PDT (gridded) files; file = DacPdtFile; data = PdtData:WindsData
		// Following covers all pdt types (with or without day range):
		private static Regex regexPdt0 = new Regex(@"^(?<site>\w\w\w)_(I|O)(A|B|E)_(?<year>\d\d\d\d)(_(?<day>\d\d\d))?(_(?<dayend>\d\d\d))?.*\.TXT$",
			RegexOptions.IgnoreCase |
			RegexOptions.ExplicitCapture |
			RegexOptions.Compiled);
		// Toga half-hour file; file = DacTogaFile; data = WindsData
		private static Regex regexToga = new Regex(@"^(?<site>\w\w)(H|L)(?<year>\d\d)(?<day>\d\d\d)\.(?<halfhour>\d\d)(A|M|N)$",
			RegexOptions.IgnoreCase |
			RegexOptions.ExplicitCapture |
			RegexOptions.Compiled);
		// Toga 10-day tar file; file = DacTogaFile; data = WindsData
		private static Regex regexTogaTar = new Regex(@"^(?<site>\w\w)(H|L)(?<year>\d\d)(?<daydecade>\d\d)x\.tar(.Z)?$",
			RegexOptions.IgnoreCase |
			RegexOptions.ExplicitCapture |
			RegexOptions.Compiled);
		private static Regex regexDisdrom = new Regex(@"^DD(?<year>\d\d\d\d)_(?<month>\d\d)(?<dayofmonth>\d\d)_(?<hour>\d\d)\.TXT$",
			RegexOptions.IgnoreCase |
			RegexOptions.ExplicitCapture |
			RegexOptions.Compiled);
		private static Regex regexCeilom = new Regex(@"^D(?<year>\d\d)(?<day>\d\d\d)\.CT$",
			RegexOptions.IgnoreCase |
			RegexOptions.ExplicitCapture |
			RegexOptions.Compiled);
		private static Regex regexThermom = new Regex(@"^DT(?<year>\d\d)(?<day>\d\d\d)[A-Z]\.TXT$",
			RegexOptions.IgnoreCase |
			RegexOptions.ExplicitCapture |
			RegexOptions.Compiled);
		private static Regex regexCampbell = new Regex(@"^CR(?<year>\d\d)(?<day>\d\d\d)[A-Z]\.TXT$",
			RegexOptions.IgnoreCase |
			RegexOptions.ExplicitCapture |
			RegexOptions.Compiled);
		private static Regex regexAsciiMom = new Regex(@"^(?<year1digit>\d)(?<day>\d\d\d)(?<hour>\d\d)(?<minute>\d\d)\.MO(?<popprefix>[DH])$",
			RegexOptions.IgnoreCase |
			RegexOptions.ExplicitCapture |
			RegexOptions.Compiled);
		// DOMSAT single-message file; file = DacDomsatFile; data = WindsData
		private static Regex regexDomsatSingle = new Regex(@"^(?<site>\w{8})\.(?<month>\d\d)(?<dayofmonth>\d\d)(?<year>\d\d)\.(?<hour>\d\d)(?<minute>\d\d)$",
			RegexOptions.IgnoreCase |
			RegexOptions.ExplicitCapture |
			RegexOptions.Compiled);
		// DOMSAT multi-message (month?) file; file = DacDomsatFile; data = WindsData
		private static Regex regexDomsatMultiple = new Regex(@"^(?<site>\w{8})\.(?<year>\d\d\d\d)(_(?<month>\d\d))?.*\.TXT$",
			RegexOptions.IgnoreCase |
			RegexOptions.ExplicitCapture |
			RegexOptions.Compiled);
		// Crossbow DMU files; file = DacDmuFile; data = SurfaceData; contain buoy orientation and motion data
		//	original files names:
		private static Regex regexDMU = new Regex(@"^(?<year>\d\d)(?<day>\d\d\d)(?<hour>\d\d)\.DAT",
			RegexOptions.IgnoreCase |
			RegexOptions.ExplicitCapture |
			RegexOptions.Compiled);
		// Crossbow DMU, proposed rename of file:
		private static Regex regexDMU2 = new Regex(@"^DMU(?<year>\d\d)(?<day>\d\d\d)(?<hour>\d\d)\.TXT",
			RegexOptions.IgnoreCase |
			RegexOptions.ExplicitCapture |
			RegexOptions.Compiled);
		//  Buoy GPS data (Scripps Marine Observatory GPS file) data = SurfaceData
		private static Regex regexSmoGps = new Regex(@"GPS1.OUT",
			RegexOptions.IgnoreCase |
			RegexOptions.ExplicitCapture |
			RegexOptions.Compiled);
		// NWS radiosonde file (as from Miramar, San Diego) file = DacRadiosonde1File; data = WindsData
		private static Regex regexSonde1 = new Regex(@"^(?<site>\w{3})(?<hour>\d{1,2})(?<dayofmonth>\d\d)\.TXT",
			RegexOptions.IgnoreCase |
			RegexOptions.ExplicitCapture |
			RegexOptions.Compiled);
		// Darwin message archive file, contains consensus wind data
		private static Regex regexDarwinMsgArchive = new Regex(@"^.(?<year>\d\d)(?<day>\d\d\d)_.*\.dat",
			RegexOptions.IgnoreCase |
			RegexOptions.ExplicitCapture |
			RegexOptions.Compiled);
		#endregion

		//public static SortedList<Regex, DataFileType> RegexKeyList;

		private List<Regex> RegexList;
		private List<DataFileType> FileTypeList;

		public FileNameParser() {

			//RegexKeyList = new SortedList<Regex, DataFileType>(50);
			RegexList = new List<Regex>(50);
			FileTypeList = new List<DataFileType>(50);

			//RegexKeyList.Add(regexPopDay, DataFileType.PopDayFile);
			RegexList.Add(regexPopDay);
			FileTypeList.Add(DataFileType.PopDayFile);
			//RegexKeyList.Add(regexPopHour, DataFileType.PopHourFile);
			RegexList.Add(regexPopHour);
			FileTypeList.Add(DataFileType.PopHourFile);
            //
            RegexList.Add(regexPopDayRawTS);
            FileTypeList.Add(DataFileType.PopRawTSDayFile);
            RegexList.Add(regexPopHourRawTS);
            FileTypeList.Add(DataFileType.PopRawTSHourFile);
            RegexList.Add(regexPopDayTS);
            FileTypeList.Add(DataFileType.PopTSDayFile);
            RegexList.Add(regexPopHourTS);
            FileTypeList.Add(DataFileType.PopTSHourFile);
            //
			//RegexKeyList.Add(regexLapxmPopDay, DataFileType.LapxmPopDayFile );
			RegexList.Add(regexLapxmPopDay);
			FileTypeList.Add(DataFileType.LapxmPopDayFile);
			//RegexKeyList.Add(regexLapxmPopHour, DataFileType.LapxmPopHourFile );
			RegexList.Add(regexLapxmPopHour);
			FileTypeList.Add(DataFileType.LapxmPopHourFile);
            //
            RegexList.Add(regexLapxmPopDayRawTS);
            FileTypeList.Add(DataFileType.LapxmPopRawTSDayFile);
            RegexList.Add(regexLapxmPopHourRawTS);
            FileTypeList.Add(DataFileType.LapxmPopRawTSHourFile);
            RegexList.Add(regexLapxmPopDayTS);
            FileTypeList.Add(DataFileType.LapxmPopTSDayFile);
            RegexList.Add(regexLapxmPopHourTS);
            FileTypeList.Add(DataFileType.LapxmPopTSHourFile);
            //
			//RegexKeyList.Add(regexCnsDay,		DataFileType.CnsDayFile );
			RegexList.Add(regexCnsDay);
			FileTypeList.Add(DataFileType.CnsDayFile);
			//RegexKeyList.Add(regexEtlCnsHour,	DataFileType.EtlCnsHourFile );
			RegexList.Add(regexEtlCnsHour);
			FileTypeList.Add(DataFileType.EtlCnsHourFile);
			//RegexKeyList.Add(regexComLoop,		DataFileType.ComLoopFile );
			RegexList.Add(regexComLoop);
			FileTypeList.Add(DataFileType.ComLoopFile);
			//RegexKeyList.Add(regexComC,		DataFileType.ComCFile );
			RegexList.Add(regexComC);
			FileTypeList.Add(DataFileType.ComCFile);
			//RegexKeyList.Add(regexPdt0,		DataFileType.PdtFile );
			RegexList.Add(regexPdt0);
			FileTypeList.Add(DataFileType.PdtFile);
			//RegexKeyList.Add(regexTogaTar,		DataFileType.TogaTarFile );
			RegexList.Add(regexTogaTar);
			FileTypeList.Add(DataFileType.TogaTarFile);
			//RegexKeyList.Add(regexToga,		DataFileType.TogaFile );
			RegexList.Add(regexToga);
			FileTypeList.Add(DataFileType.TogaFile);
			//RegexKeyList.Add(regexDisdrom,		DataFileType.DisdrometerFile );
			RegexList.Add(regexDisdrom);
			FileTypeList.Add(DataFileType.DisdrometerFile);
			//RegexKeyList.Add(regexCeilom,		DataFileType.CeilometerFile );
			RegexList.Add(regexCeilom);
			FileTypeList.Add(DataFileType.CeilometerFile);
			//RegexKeyList.Add(regexThermom,		DataFileType.DigitalThermometerFile );
			RegexList.Add(regexThermom);
			FileTypeList.Add(DataFileType.DigitalThermometerFile);
			//RegexKeyList.Add(regexCampbell,	DataFileType.CampbellCr10File );
			RegexList.Add(regexCampbell);
			FileTypeList.Add(DataFileType.CampbellCr10File);
			//RegexKeyList.Add(regexAsciiMom,	DataFileType.AsciiMomentsFile );
			RegexList.Add(regexAsciiMom);
			FileTypeList.Add(DataFileType.AsciiMomentsFile);
			//RegexKeyList.Add(regexDomsatSingle, DataFileType.DomsatSingleMsgFile );
			RegexList.Add(regexDomsatSingle);
			FileTypeList.Add(DataFileType.DomsatSingleMsgFile);
			//RegexKeyList.Add(regexDomsatMultiple, DataFileType.DomsatMultipleMsgFile );
			RegexList.Add(regexDomsatMultiple);
			FileTypeList.Add(DataFileType.DomsatMultipleMsgFile);
			//RegexKeyList.Add(regexDMU,			DataFileType.CrossbowDMU );
			RegexList.Add(regexDMU);
			FileTypeList.Add(DataFileType.CrossbowDMU);
			//RegexKeyList.Add(regexDMU2,		DataFileType.CrossbowDMU );
			RegexList.Add(regexDMU2);
			FileTypeList.Add(DataFileType.CrossbowDMU);
			//RegexKeyList.Add(regexSmoGps,		DataFileType.SmoGps );
			RegexList.Add(regexSmoGps);
			FileTypeList.Add(DataFileType.SmoGps);
			//RegexKeyList.Add(regexSonde1,		DataFileType.Radiosonde1 );
			RegexList.Add(regexSonde1);
			FileTypeList.Add(DataFileType.Radiosonde1);
			//RegexKeyList.Add(regexDarwinMsgArchive, DataFileType.DarwinMsgArchive );
			RegexList.Add(regexDarwinMsgArchive);
			FileTypeList.Add(DataFileType.DarwinMsgArchive);
		}

		/// <summary>
		/// Method for public to add another Regular Expression to end of list.
		/// The associated DataFileType will be "Other".
		/// </summary>
		/// <param name="regExp"></param>
		public  void AddRegex(string regExp) {
			Regex newRegEx = new Regex(regExp, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled);
			//RegexKeyList.Add(newRegEx, DataFileType.Other);
			RegexList.Add(newRegEx);
			FileTypeList.Add(DataFileType.Other);
		}

		public  void AddGenericYydddRegex() {
			Regex newRegEx = new Regex(@".*(?<year>\d\d)(?<day>\d\d\d).*",
										RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled);
			//RegexKeyList.Add(newRegEx, DataFileType.Other);
			RegexList.Add(newRegEx);
			FileTypeList.Add(DataFileType.Other);
		}

		public  bool Parse(string fileName, out ParsedFileNameStruct info) {

			string justFileName = (Path.GetFileName(fileName)).ToLower();
			
			int numTypes = RegexList.Count;
			for (int i = 0; i < numTypes; i++) {
				Regex regex = RegexList[i];
				Match match = regex.Match(justFileName);
				if (match.Success) {
					info = ParseFileName(match);
					if (info.FileType != DataFileType.UnknownFile) {
						info.FileType = FileTypeList[i];
						return true;
					}
				}
			}
			// if match no known types:
			info.Site = "???";
			info.TimeStamp = DateTime.MinValue;
			info.EndTimeStamp = DateTime.MaxValue;
			info.PopPrefix = "";
			info.FileType = DataFileType.UnknownFile;
			return false;
		}

		public  bool ParseReverse(string fileName, out ParsedFileNameStruct info) {

			string justFileName = Path.GetFileName(fileName);

			int numTypes = RegexList.Count;
			for (int i = numTypes-1; i >= 0; i--) {
				Regex regex = RegexList[i];
				Match match = regex.Match(justFileName);
				if (match.Success) {
					info = ParseFileName(match);
					info.FileType = FileTypeList[i];
					return true;
				}
			}
			// if match no known types:
			info.Site = "???";
			info.TimeStamp = DateTime.MinValue;
			info.EndTimeStamp = DateTime.MaxValue;
			info.PopPrefix = "";
			info.FileType = DataFileType.UnknownFile;
			return false;
		}

		/// <summary>
		/// Takes the successful results of a regular expression match
		///		and strips out the found elements in the file name.
		/// Returns a ParsedFileNameStruct with all members filled in
		///		except for the FileType.
		/// </summary>
		/// <param name="match"></param>
		/// <returns></returns>
		private  ParsedFileNameStruct ParseFileName(Match match) {

			string yearMatch;
			string year1DigitMatch;
			string dayYearMatch;
			string dayYearEndMatch;
			string dayMonthMatch;
			string monthMatch;
			string hourMatch;
			string minuteMatch;
			string halfHourMatch;
			string hourLetterMatch;
			string popPrefix;
			string dayDecadeMatch;

			ParsedFileNameStruct _parsedFileName;

			int year, dayOfYear, dayOfYearEnd, dayOfMonth, month, hour, minute;

			// find values of all possible regex groups
			popPrefix = match.Groups["popprefix"].Value;
			year1DigitMatch = match.Groups["year1digit"].Value;
			yearMatch = match.Groups["year"].Value;
			dayYearMatch = match.Groups["day"].Value;
			dayYearEndMatch = match.Groups["dayend"].Value;
			dayMonthMatch = match.Groups["dayofmonth"].Value;
			monthMatch = match.Groups["month"].Value;
			hourMatch = match.Groups["hour"].Value;
			halfHourMatch = match.Groups["halfhour"].Value;
			hourLetterMatch = match.Groups["hourletter"].Value;
			minuteMatch = match.Groups["minute"].Value;
			dayDecadeMatch = match.Groups["daydecade"].Value;

			// compute end day (may be overridden later)
			//	(may someday want to make end time based on file type)
			if (dayYearEndMatch != String.Empty) {
				dayOfYearEnd = Int32.Parse(dayYearEndMatch);
			}
			else {
				dayOfYearEnd = -1;
			}

			// compute year
			if (year1DigitMatch != String.Empty) {
				int year1 = Int32.Parse(year1DigitMatch);
				int now1 = DateTime.Now.Year % 10;		// this year's single digit year
				int decade = DateTime.Now.Year - now1;
				if (year1 <= now1) {
					year = year1 + decade;
				}
				else {
					year = year1 + decade - 10;
				}
			}
			else if (yearMatch != String.Empty) {
				int year2 = Int32.Parse(yearMatch);
				int now2 = DateTime.Now.Year % 100;		// this year's double digit year
				if (year2 <= now2) {
					year = year2 + 2000;
				}
				else if (year2 < 200) {
					year = year2 + 1900;
				}
				else {
					year = year2;
				}
			}
			else {
				year = 1;
			}

			// compute day of year
			if (monthMatch != String.Empty) {
				month = Int32.Parse(monthMatch);
				if (dayMonthMatch != String.Empty) {
					dayOfMonth = Int32.Parse(dayMonthMatch);
				}
				else {
					dayOfMonth = 1;
				}
				DateTime dt = new DateTime(year, month, dayOfMonth);
				dayOfYear = dt.DayOfYear;
			}
			else if (dayYearMatch != String.Empty) {
				dayOfYear = Int32.Parse(dayYearMatch);
			}
			else if (dayMonthMatch != String.Empty) {
				// have day of month but not month: use January
				dayOfYear = Int32.Parse(dayMonthMatch);
			}
			else if (dayDecadeMatch != String.Empty) {
				// have day decade 
				dayOfYear = 10 * Int32.Parse(dayDecadeMatch);
			}
			else {
				// no day specified, must be year file
				dayOfYear = 1;
				if (dayOfYearEnd < 0) {
					DateTime dataYear = new DateTime(year, 1, 1);
					DateTime nextYear = new DateTime(year + 1, 1, 1);
					TimeSpan daysPerYear = nextYear - dataYear;
					dayOfYearEnd = daysPerYear.Days;
				}
			}

			// compute minute
			if (minuteMatch != String.Empty) {
				minute = Int32.Parse(minuteMatch);
			}
			else {
				minute = 0;
			}

			// compute hour (and possibly minute)
			if (hourLetterMatch != String.Empty) {
				char h = hourLetterMatch.ToLower()[0];
				double dh = (int)h - (int)'c';
				hour = (int)dh;
			}
			else if (halfHourMatch != String.Empty) {
				int halfHour = Int32.Parse(halfHourMatch);
				hour = (int)(halfHour / 2.0);
				minute = 30 * (halfHour - hour * 2);
			}
			else if (hourMatch != String.Empty) {
				hour = Int32.Parse(hourMatch);
			}
			else {
				hour = 0;
			}

			// pass site name and timestamp
			try {
				_parsedFileName.PopPrefix = popPrefix;
				_parsedFileName.Site = match.Groups["site"].Value;
				_parsedFileName.TimeStamp = DacDateTime.FromDayOfYear(year, dayOfYear, hour, minute, 0);
				_parsedFileName.FileType = DataFileType.Undetermined;
				if (dayOfYearEnd > 0) {
					// for now only compute EndTimeStamp if end day specified in file name
					_parsedFileName.EndTimeStamp = DacDateTime.FromDayOfYear(year, dayOfYearEnd, 0, 0, 0);
				}
				else {
					_parsedFileName.EndTimeStamp = DateTime.MaxValue;
				}
			}
			catch (Exception e) {
				_parsedFileName.PopPrefix = "";
				_parsedFileName.Site = "";
				_parsedFileName.FileType = DataFileType.UnknownFile;
				_parsedFileName.TimeStamp = DateTime.MinValue;
				_parsedFileName.EndTimeStamp = DateTime.MaxValue;
			}
			return _parsedFileName;
		}

	}
}
