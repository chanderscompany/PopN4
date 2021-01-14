
namespace DACarter.Utilities {

	public enum DestinationType {
		Archive,
		Overflow
	}

	public enum ArchiveIntervalType {
		Daily,
		Hourly,
		All
	}

	public enum SourceType {
		ArchiveSource,
		Overflow
	}

	public enum FileNameMatchType {
		All,
		UseWriteTimes,
		//Ask,
		None
	}

	public struct ArchiverOptions {
		public bool Debug;
		public bool AutoStart;
		public bool TestRun;
		public bool Copy;
		public bool Verify;
		public bool IncludeSubdirectories;
		public bool NoDelete;
		public bool IgnoreArchiveBit;
		public bool CloseWhenFinished;
		//public ArchiveIntervalType Interval;
		public FileNameMatchType MatchOption;
		public bool NewConfig;

		public ArchiverOptions(bool archiverDefault) {
			// constructor to set archiver default options
			if (archiverDefault == true) {
				AutoStart = true;
				TestRun = false;
				Copy = true;
				Verify = true;
				NoDelete = false;
				IncludeSubdirectories = true;
				//Interval = ArchiveIntervalType.Daily;
				MatchOption = FileNameMatchType.UseWriteTimes;
				IgnoreArchiveBit = false;
				CloseWhenFinished = false;
				Debug = false;
				NewConfig = false;
			}
			else {
				AutoStart = false;
				TestRun = true;
				Copy = true;
				Verify = false;
				NoDelete = true;
				IncludeSubdirectories = false;
				//Interval = ArchiveIntervalType.All;
				MatchOption = FileNameMatchType.All;
				IgnoreArchiveBit = true;
				CloseWhenFinished = true;
				Debug = false;
				NewConfig = false;
			}
		}
/*
		public ArchiverOptions(ArchiverOptions opt) {
			// copy constructor
			TestRun = opt.TestRun;
			Copy = opt.Copy;
			Verify = opt.Verify;
			IncludeSubdirectories = opt.IncludeSubdirectories;
			NoDelete = opt.NoDelete;
			Interval = opt.Interval;
			MatchOption = opt.MatchOption;
			IgnoreArchiveBit = opt.IgnoreArchiveBit;
		}
*/
	}


}
