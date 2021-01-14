using System;

namespace DACarter.NOAA
{
	public enum FileType {
		None,
		DOMSAT,
		POPMom,
		POPSpc,
		COM,
		TOGA,
		PDMI,
		PDMO
	}

	public enum ProfilerMode {
		None,
		High,
		Low
	}

	public enum ProfilerFrequency {
		None,
		VHF,
		UHF,
		SBand
	}

	public enum FileTimeSpan {
		None,
		Hour,
		Day,
		Days,
		Year
	}

	public enum ProfilerName {
		None,			// Line 0 
		Biak50,			// Line 1
		Biak915,		// Line 2
		Christmas50,	// Line 3
		Christmas915,	// Line 4
		Darwin50,		// Line 5
		Darwin920,		// Line 6
		Galapagos915,	// Line 7
		Kapinga915,		// Line 8
		Kavieng915,		// Line 9
		Nauru915,		// Line 10
		Manus915,		// Line 11
		Piura50,		// Line 12
		Tarawa915,		// Line 13
		RVKexue1,		// Line 14
		RVShiyan3,		// Line 15
		RVKaimi,		// Line 16
		RVRHBrown,		// Line 17
		RVMoana,		// Line 18
		TRMM,			// Line 19
		New				// Line 20
	}

	interface IDacFileName
	{
		// properties
		string Folder {get; set;}
		DateTime BeginDateTime {get; set;}
		DateTime EndDateTime {get; set;}
		FileType DataFileType {get;}
		ProfilerMode DataMode {get; set;}
		ProfilerFrequency Frequency {get; set;}
		FileTimeSpan DataTimeSpan {get; set;}
		ProfilerName StationName {get; set;}

		string TogaStationName {get; set;}
		string PdmStationName {get; set;}
		string DCPID {get; set;}

		// methods
		string[] GetFullPathName();
		string[] GetFileName();
		void Next();
		void UndoNext();
	}

	/// <summary>
	/// Summary description for DacFileName.
	/// </summary>
	public class DacFileName : IDacFileName
	{
		// private default constructor
		private DacFileName() {}

		// public constructor requires FileType
		public DacFileName(FileType type){
			_type = type;
			// initialize strings in _current struct
			// (other members are auto initialized OK)
			_current.Folder = "";
			_current.FullPath = "";
			_current.FileName = "";

			// set _previous 
			_previous =  _current;

			int year = _current.BeginTime.Year;
			_current.BeginTime = new DateTime(1,1,22);
		}

		//
		// properties
		//
		public FileType DataFileType {
			get{return _type;}
		}

		public string Folder {
			get {return _current.Folder;}
			set {_current.Folder = value;}
		}

		public DateTime BeginDateTime {
			get {return _current.BeginTime;}
			set {_current.BeginTime = value;}
		}

		public DateTime EndDateTime {
			get {return _current.EndTime;}
			set {_current.EndTime = value;}
		}

		public ProfilerMode DataMode {
			get {return _current.Mode;}
			set {_current.Mode = value;}
		}

		public ProfilerFrequency Frequency {
			get {return _current.Freq;}
			set {
				if (_stationName == ProfilerName.New) {
					_current.Freq = value;
				} else {
					throw new InvalidOperationException("DacFileName Error: Cannot set Freq independently of station name.");
				}
			}
		}

		public FileTimeSpan DataTimeSpan {
			get {return _current.Span;}
			set {_current.Span = value;}
		}

		public ProfilerName StationName {
			get {return _stationName;}
			set {
				_stationName = value;
				// choosing station name also specifies frequency,
				// and toga and pdm station prefixes
				_current.Freq = _stationFrequencies[(int)_stationName];
				_togaStationName = _togaStationNames[(int)_stationName];
				_pdmStationName = _pdmStationNames[(int)_stationName];
				_dcpId = _dcpIds[(int)_stationName];
			}
		}

		public string TogaStationName {
			get {return _togaStationName;}
			set {
				if (_stationName != ProfilerName.New) {
					throw new InvalidOperationException("DacFileName Error: Cannot set Toga name independently of station name.");
				}
				else if (value.Length != 2) {
					throw new InvalidOperationException("DacFileName Error: TOGA station name must be 2 characters.");
				}
				else {
					_togaStationName = value;
				}
			}
		}

		public string DCPID {
			get {return _dcpId;}
			set {
				if (_stationName != ProfilerName.New) {
					throw new InvalidOperationException("DacFileName Error: Cannot set DCP ID independently of station name.");
				}
				else if (value.Length != 8) {
					throw new InvalidOperationException("DacFileName Error: DCP ID must be 8 characters.");
				}
				else {
					_dcpId = value;
				}
			}
		}

		public string PdmStationName {
			get {return _pdmStationName;}
			set {
				if (_stationName != ProfilerName.New) {
					throw new InvalidOperationException("DacFileName Error: Cannot set Pdm name independently of station name.");
				}
				else if (value.Length != 3) {
					throw new InvalidOperationException("DacFileName Error: pdm station name must be 3 characters.");
				}
				else {
					_pdmStationName = value;
				}
			}
		}

		//
		// methods
		//

		
		/// <summary>
		/// Computes full path name of file based on settings
		/// of properties of this class.
		/// </summary>
		/// <returns>Path names in an array of strings</returns>
		/// <remarks>
		/// Throws InvalidOperationException if properties not set properly.
		/// </remarks>
		public string[] GetFullPathName() {
			string[] fileNames;
			fileNames = GetFileName();
			string[] path = new string[fileNames.Length];
			string folder = _current.Folder;
			// If folder not specified, explicitly set to current directory.
			// Make sure folder ends in backslash.
			if (folder.Length == 0) {
				folder = @".\";
			}
			else if ( !folder.EndsWith("\\") && !folder.EndsWith("/") ) {
				folder += @"\";
			}
			int i = 0;
			foreach (string fileName in fileNames) {
				path[i++] = folder + fileName;
			}
			return path;
		}

		/// <summary>
		/// Computes file name based on settings of properties of this class.
		/// </summary>
		/// <returns>File names in an array of strings</returns>
		/// <remarks>
		/// Throws InvalidOperationException if properties not set properly.
		/// </remarks>
		public string[] GetFileName() {
			// 
			// File Type not set
			//
			if (_type == FileType.None) {
				throw new InvalidOperationException("DacFileName Error: Cannot GetFileName: FileType is 'None'.");
			}
			// 
			// COM files
			//
			// prerequisite properties:
			//	BeginDateTime
			//	EndDateTime
			//
			else if (_type == FileType.COM) {
				if (_current.BeginTime.Year != _current.EndTime.Year) {
					throw new InvalidOperationException("DacFileName Error: Must begin and end in same year");
				}
				string[] fileNames = new String[1];
				fileNames[0] = string.Format("c{0:000}.{1:000}",
						_current.BeginTime.DayOfYear,
						_current.EndTime.DayOfYear);	
				return fileNames;	
			}
			// 
			// DOMSAT files
			//  iiiiiiii.mmddyy.hhmm	// for individual message files
			//  iiiiiiii.yyyy_mm.txt	// for combined monthly files (also *.zip)
			//
			// prerequisite properties:
			//	StationName (or DcpId for new stations)
			//	BeginDateTime
			//
			else if (_type == FileType.DOMSAT) {
				// throw new InvalidOperationException("DacFileName Error: FileType is 'DOMSAT'.");
				if ((_stationName == ProfilerName.None)) {
					throw new InvalidOperationException("DacFileName Error: Profiler Name not specified");
				}
				else if ((_dcpId == "XXXXXXXX")) {
					throw new InvalidOperationException("DacFileName Error: Need to specify DCP ID");
				}
				else {
					string[] fileNames = new string[3];
					fileNames[0] = string.Format("{0}.{1:00}{2:00}{3:00}.{4:00}{5:00}",
						_dcpId,
						_current.BeginTime.Month,
						_current.BeginTime.Day,
						_current.BeginTime.Year%100,
						_current.BeginTime.Hour,
						_current.BeginTime.Minute );
					string combined = string.Format("{0}.{1:0000}_{2:00}",
						_dcpId,
						_current.BeginTime.Year,
						_current.BeginTime.Month);
					fileNames[1] = combined + ".txt";
					fileNames[2] = combined + ".zip";
					return fileNames;
				}
			}
			// 
			// PDM files
			//	sss_tm_yyyy.txt			// for year files of type t and mode m at station sss
			//  sss_tm_yyyy_ddd.txt		// for day files
			//  sss_tm_yyyy_ddd_ddd.txt	// for a range of days
			//
			// prerequisite properties:
			//	StationName (or PdmStationName for new stations)
			//	BeginDateTime
			//	DataMode (for 915 profilers)
			//  EndDateTime (for DataTimeSpan==Days)
			//  ProfilerFrequency (for new stations only)
			//
			else if ( (_type == FileType.PDMI) || (_type == FileType.PDMO)) {
				if ((_stationName == ProfilerName.None)) {
					throw new InvalidOperationException("DacFileName Error: Profiler Name not specified");
				}
				else if ((_pdmStationName == "XXX")) {
					throw new InvalidOperationException("DacFileName Error: Need to specify PdmStationName");
				}
				else {
					string[] fileNames = new String[1];
					string modeChar;
					string typeChar;

					if ( _type == FileType.PDMI) {
						// interpolated (gridded) files
						typeChar = "i";
					}
					else if (_type == FileType.PDMO) {
						// original (TOGA-like) data files
						typeChar = "o";
					}
					else {
						throw new InvalidOperationException("DacFileName Error: Invalid PDM type");
					}

					if ((_current.Freq == ProfilerFrequency.UHF) && (_current.Mode == ProfilerMode.High)) {
						modeChar = "a";
					}
					else if ((_current.Freq == ProfilerFrequency.UHF) && (_current.Mode == ProfilerMode.Low)) {
						modeChar = "b";
					}
					else if (_current.Freq == ProfilerFrequency.VHF) {
						modeChar = "e";
					}
					else {
						throw new InvalidOperationException("DacFileName Error: Invalid mode for this frequency");
					}

					// create sss_tm_yyy part of name
					fileNames[0] = string.Format("{0}_{1}{2}_{3:0000}",
						_pdmStationName,
						typeChar,
						modeChar,
						_current.BeginTime.Year);
				
					// check the time span of the data
					if (_current.Span == FileTimeSpan.Year) {
						// do nothing; we are done
					}
					else if (_current.Span == FileTimeSpan.Day) {
						string suffix = string.Format("_{0:000}",_current.BeginTime.DayOfYear);
						fileNames[0] += suffix;
					}					
					else if (_current.Span == FileTimeSpan.Days) {
						if (_current.BeginTime.Year != _current.EndTime.Year) {
							throw new InvalidOperationException("DacFileName Error: Must begin and end in same year");
						}
						string suffix = string.Format("_{0:000}_{1:000}",
											_current.BeginTime.DayOfYear,
											_current.EndTime.DayOfYear);						
						fileNames[0] += suffix;
					}					
					else {
						throw new InvalidOperationException("DacFileName Error: Invalid TimeSpan for pdm file");
					}
					
					fileNames[0] += ".txt";

					return fileNames;
				}
			}
			// 
			// POP moment files
			//  returns D file and H file name.
			//
			// prerequisite properties:
			//  DataTimeSpan
			//  BeginDateTime
			//
			else if (_type == FileType.POPMom) {
				// Make 2 files Dyyddda.mom and Hyyddda.mom
				// Replace 'a' with c-z for hourly files
				if ((_current.Span != FileTimeSpan.Day) && (_current.Span != FileTimeSpan.Hour)) {
					throw new InvalidOperationException("DacFileName Error: Wrong FileTimeSpan.");
				}
				else {
					string[] fileNames = new string[2];
					string root = GetPopRoot();
					fileNames[0] = "D" + root + ".mom";
					fileNames[1] = "H" + root + ".mom";
					return fileNames;
				}
			}
			// 
			// POP spectral files
			//  returns D file and H file name.
			//
			// prerequisite properties:
			//  DataTimeSpan
			//  BeginDateTime
			//
			else if (_type == FileType.POPSpc) {
				// Make 2 files Dyyddda.spc and Hyyddda.spc
				// Replace 'a' with c-z for hourly files
				if ((_current.Span != FileTimeSpan.Day) && (_current.Span != FileTimeSpan.Hour)) {
					throw new InvalidOperationException("DacFileName Error: Wrong FileTimeSpan.");
				}
				else {
					string[] fileNames = new string[2];
					string root = GetPopRoot();
					fileNames[0] = "D" + root + ".spc";
					fileNames[1] = "H" + root + ".spc";
					return fileNames;
				}
			}
			// 
			// TOGA files
			//	returns TOGA file name and tar.Z file name
			//
			// prerequisite properties:
			//  StationName (or TogaStationName for new stations)
			//  DataTimeSpan
			//  BeginDateTime
			//  DataMode
			//
			else if (_type == FileType.TOGA) {
				//throw new InvalidOperationException("DacFileName Error: FileType is 'TOGA'.");
				if (_stationName == ProfilerName.None) {
					throw new InvalidOperationException("DacFileName Error: Profiler Name not specified");
				}
				else if ((_togaStationName == "XX")) {
					throw new InvalidOperationException("DacFileName Error: Need to specify TogaStationName");
				}
				else {
					string[] fileNames = new String[2];
					//int index = (int)_stationName;
					string modeChar;
					if (_current.Mode == ProfilerMode.High) {
						modeChar = "h";
					}
					else if (_current.Mode == ProfilerMode.Low) {
						modeChar = "l";
					}
					else {
						throw new InvalidOperationException("DacFileName Error: Profiler Data Mode not specified");
					}

					// TOGA file name
					fileNames[0] = string.Format("{0}{1}{2:00}{3:000}.{4:00}a",
						_togaStationName,
						modeChar,
						_current.BeginTime.Year%100,
						_current.BeginTime.DayOfYear,
						_current.BeginTime.Hour*2 + _current.BeginTime.Minute/30);

					// TOGA tar file name
					fileNames[1] = string.Format("{0}{1}{2:00}{3:00}x.tar.Z",
						_togaStationName,
						modeChar,
						_current.BeginTime.Year%100,
						_current.BeginTime.DayOfYear/10);


					return fileNames;
				}
			}
			// 
			// unknown files
			//
			else {
				// must have added a new FileType
				throw new InvalidOperationException("DacFileName Error: Unsupported FileType");
			}
		}

		/// <summary>
		/// Computes best guess for next file name in sequence.
		/// </summary>
		/// <remarks>
		/// After calling Next()
		/// call GetFileName() or GetFullPathName() to view new file name.
		/// Call UndoNext() to return to previous file name.
		/// </remarks>
		public void Next() {
			_previous = _current;
			// compute next filename
			// _current = ....
			return;
		}

		public void UndoNext() {
			// undo next filename
			_current = _previous;
			return;
		}

		//
		// Private fields and methods
		//

		private readonly FileType _type;
		private ProfilerName _stationName;
		private string _togaStationName;
		private string _pdmStationName;
		private string _dcpId;

		struct LocalParams {
			public string Folder;
			public DateTime BeginTime;
			public DateTime EndTime;
			public ProfilerMode Mode;
			public ProfilerFrequency Freq;
			public FileTimeSpan Span;
			public string FullPath;
			public string FileName;
		};

		private LocalParams _current;
		private LocalParams _previous; 

		private string[] _togaStationNames = new string[] {
			"XX",	// Line 0
			"bi",	// Line 1
			"bb",	// Line 2
			"ch",	// Line 3
			"cx",	// Line 4
			"ds",	// Line 5
			"db",	// Line 6
			"ga",	// Line 7
			"kp",	// Line 8
			"kv",	// Line 9
			"na",	// Line 10
			"ma",	// Line 11
			"pi",	// Line 12
			"ta",	// Line 13
			"s1",	// Line 14
			"e3",	// Line 15
			"km",	// Line 16
			"rb",	// Line 17
			"mw",	// Line 18
			"XX",	// Line 19
			"XX"	// Line 20
		};

		private string[] _pdmStationNames = new string[] {
			"XXX",	// Line 0
			"bia",	// Line 1
			"bia",	// Line 2
			"chr",	// Line 3
			"chr",	// Line 4
			"dar",	// Line 5
			"dar",	// Line 6
			"gal",	// Line 7
			"kap",	// Line 8
			"kav",	// Line 9
			"nau",	// Line 10
			"man",	// Line 11
			"piu",	// Line 12
			"tar",	// Line 13
			"kx1",	// Line 14
			"sh3",	// Line 15
			"kai",	// Line 16
			"rhb",	// Line 17
			"mwv",	// Line 18
			"XXX",	// Line 19
			"XXX"	// Line 20
		};

		private string[] _dcpIds = new string[] {
			"XXXXXXXX",	// Line 0
			"XXXXXXXX",	// Line 1
			"XXXXXXXX",	// Line 2
			"7540011C",	// Line 3
			"7540011C",	// Line 4
			"XXXXXXXX",	// Line 5
			"XXXXXXXX",	// Line 6
			"75404216",	// Line 7
			"XXXXXXXX",	// Line 8
			"XXXXXXXX",	// Line 9
			"3642F53E",	// Line 10
			"3642E648",	// Line 11
			"754027F0",	// Line 12
			"75403486",	// Line 13
			"XXXXXXXX",	// Line 14
			"XXXXXXXX",	// Line 15
			"XXXXXXXX",	// Line 16
			"XXXXXXXX",	// Line 17
			"XXXXXXXX",	// Line 18
			"75405160",	// Line 19
			"XXXXXXXX"	// Line 20
		};

		private ProfilerFrequency[] _stationFrequencies = new ProfilerFrequency[] {
			ProfilerFrequency.None,	// Line 0
			ProfilerFrequency.VHF,	// Line 1
			ProfilerFrequency.UHF,	// Line 2
			ProfilerFrequency.VHF,	// Line 3
			ProfilerFrequency.UHF,	// Line 4
			ProfilerFrequency.VHF,	// Line 5
			ProfilerFrequency.UHF,	// Line 6
			ProfilerFrequency.UHF,	// Line 7
			ProfilerFrequency.UHF,	// Line 8
			ProfilerFrequency.UHF,	// Line 9
			ProfilerFrequency.UHF,	// Line 10
			ProfilerFrequency.UHF,	// Line 11
			ProfilerFrequency.VHF,	// Line 12
			ProfilerFrequency.UHF,	// Line 13
			ProfilerFrequency.UHF,	// Line 14
			ProfilerFrequency.UHF,	// Line 15
			ProfilerFrequency.UHF,	// Line 16
			ProfilerFrequency.UHF,	// Line 17
			ProfilerFrequency.UHF,	// Line 18
			ProfilerFrequency.None,	// Line 19
			ProfilerFrequency.None	// Line 20
		};

		private string GetPopRoot() {
			char suffix;
			if (_current.Span == FileTimeSpan.Day) {
				suffix = 'a';
			}
			if (_current.Span == FileTimeSpan.Hour) {
				suffix = 'c';
				suffix += (char)(_current.BeginTime.Hour);
			}
			else {
				throw(new InvalidOperationException("DacFileName Error: Improper FileTimeSpan"));
			}
			string root = string.Format("{0:00}{1:000}"+suffix,_current.BeginTime.Year%100,_current.BeginTime.DayOfYear);
			return root;
		}

		
	}
}
