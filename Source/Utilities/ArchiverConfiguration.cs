using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace DACarter.Utilities {

	//////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// ArchiverConfiguration
	/// The class that holds all the information contained in the archiver.cfg file.
	/// It contains 3 public properties:
	///		string PathID
	///		DestinationDriveCollection DestDrives
	///		ArrayList SourceList (an array of SourceInfo objects)
	/// </summary>
	/// 
	[Serializable]
	public class ArchiverConfiguration {

		private string _pathID;
		private DestinationDriveCollection _destDrives;
		private ArrayList _sourceList;
		private ArrayList _regexList;
		private ArchiveIntervalType _archiveInterval;
		private string _logFilePath;

		private string _commentString;

		public ArchiverConfiguration() {
			_sourceList = new ArrayList();
			_destDrives = new DestinationDriveCollection();
			_regexList = new ArrayList();
			_archiveInterval = ArchiveIntervalType.Daily;
			_pathID = "";
			_logFilePath = "";
		}

		public void Clear() {
			_sourceList.Clear();
			_destDrives.Clear();
			_regexList.Clear();
			_archiveInterval = ArchiveIntervalType.Daily;
			_pathID = "";
			_logFilePath = "";
		}

		public string PathID {
			get { return _pathID; }
			set { _pathID = value; }
		}

		public string LogFilePath {
			get { return _logFilePath; }
			set { _logFilePath = value; }
		}

		public DestinationDriveCollection DestDrives {
			get { return _destDrives; }
			set { _destDrives = value; }
		}

		public ArrayList SourceList {
			get { return _sourceList; }
			set { _sourceList = value; }
		}

		public ArrayList RegexList {
			get { return _regexList;}
			set { _regexList = value;}
		}

		public ArchiveIntervalType ArchiveInterval {
			get {return _archiveInterval;}
			set { _archiveInterval = value;}
		}

		public string commentString {
			get { return _commentString; }
			set { _commentString = value; }
		}

	}

	//////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// SourceInfo
	/// The class that contains the information from a single <source> section
	///		of the Archiver.cfg file.
	/// </summary>
	/// 
	[Serializable]
	public class SourceInfo {
		private string _sourcePath;
		private string _destination;

		public SourceInfo(string src, string dest) {
			_sourcePath = src;
			_destination = dest;
		}

		public string SourcePath {
			get { return _sourcePath; }
		}

		public string Destination {
			get { return _destination; }
		}

	} ;

	//////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// DestinationDriveCollection
	/// The class that contains all the information from the
	///		<DestinationDrives> section of the Archiver.cfg file.
	///	This class behaves as a public collection of MirrorSet objects
	///		which can be accessed via an enumerator (foreach loop)
	///			or an index (e.g. destDriveCollection[i])
	///	Its public properties are
	///		int Count (a count of the number of MirrorSets)
	///		double ReservedSpace (bytes)
	///	Its public methods are
	///		StartNewMirrorSet() (create a new MirrorSet; next drive added to this set)
	///		AddDrive() (add a drive to the current MirrorSet)
	///		Clear()	(remove all MirrorSets from the collection)
	///		
	/// </summary>
	/// 
	[Serializable]
	public class DestinationDriveCollection : IEnumerable {

		// regular expression to capture size and units of reserved space
		//	size can be with or without decimal:  32, 1.2, 3., .2 
		//	units can be b,kb,mb,gb,kib,mib,gib case insensitive.
		private static Regex regex = new Regex(@"
				(?<size>(-?\d+\.\d*)|(-?\d*\.\d+)|(-?\d+))\s*(?<units>(([kmg]+[i]*)?[b])|[b]?)",
			RegexOptions.IgnoreCase |
			RegexOptions.Multiline |
			RegexOptions.ExplicitCapture |
			RegexOptions.Compiled |
				RegexOptions.IgnorePatternWhitespace);

		//private int _iSet;
		private MirrorSet _mirrors;
		private string _reservedSpace;
		private int _nCopiesRequired;

		private ArrayList _destinationDrives;

		public DestinationDriveCollection() {
			Clear();
		}

		public void UpdateMirrorNames() {
			int index = 0;
			foreach (MirrorSet mirrorSet in _destinationDrives) {
				index++;
				if (mirrorSet.Type == DestinationType.Overflow) {
					mirrorSet.Name = index + " (Overflow)";
				}
				else {
					mirrorSet.Name = index + " (Archive)";
				}
			}
		}

		public void StartNewMirrorSet(string type, double reservedSpace) {
			//_mirrors = new StringCollection();
			DestinationType driveType;
			string name;
			if (type.ToLower().Trim() == "overflow") {
				driveType = DestinationType.Overflow;
				name = (_destinationDrives.Count+1).ToString() + " (Overflow)";
			}
			else {
				driveType = DestinationType.Archive;
				name = (_destinationDrives.Count+1).ToString() + " (Archive)";
			}
			_mirrors = new MirrorSet(driveType, name);
			_mirrors.ReservedSpace = reservedSpace;
			_destinationDrives.Add(_mirrors);
		}

		public void InsertMirrorSet(int index, string type, double reservedSpace) {
			DestinationType driveType;
			string name;
			if (type.ToLower().Trim() == "overflow") {
				driveType = DestinationType.Overflow;
				name = (_destinationDrives.Count+1).ToString() + " (Overflow)";
			}
			else {
				driveType = DestinationType.Archive;
				name = (_destinationDrives.Count+1).ToString() + " (Archive)";
			}
			_mirrors = new MirrorSet(driveType, name);
			_mirrors.ReservedSpace = reservedSpace;
			_destinationDrives.Insert(index, _mirrors);
		}

		public void AddDrive(string drive) {
			MirrorSet currentMirrorSet = (MirrorSet) _destinationDrives[_destinationDrives.Count-1];
			currentMirrorSet.Add(drive);
		}

		public int Count {
			get { return _destinationDrives.Count; }
		}

		public MirrorSet this[int index] {
			get { return (MirrorSet) _destinationDrives[index]; }
		}

		public void Clear() {
			_destinationDrives = new ArrayList();
			_reservedSpace = "1";
			_nCopiesRequired = -1;
		}

		public void RemoveAt(int index) {
			if (index < _destinationDrives.Count) {
				_destinationDrives.RemoveAt(index);
			}
		}

		public int CopiesRequired {
			get { return _nCopiesRequired; }
			set { _nCopiesRequired = value; }
		}

		public string ReservedSpaceString {
			get { return _reservedSpace; }
			set { _reservedSpace = value; }
		}

		public static double ParseBytes(string label) {
			
			double size = 0.0;
			Match match = regex.Match(label);
			if (match.Success) {
				Group sizeGroup = match.Groups["size"];
				size = Double.Parse(sizeGroup.Value);
				Group unitsGroup = match.Groups["units"];
				string units = unitsGroup.Value;
				double factor = 1000;
				if (units.Length >= 3) {
					if (units.ToLower()[1] == 'i') {
						factor = 1024.0;
					}
					else {
						// ERROR must have an 'i' here
						factor = 1000;
					}
				}
				else if (units.Length == 2) {
					factor = 1000.0;
				}
				if (units.Length >= 2) {
					char prefix = units.ToLower()[0];
					if (prefix == 'k') {
						size = size*factor;
					}
					else if (prefix == 'm') {
						size = size*factor*factor;
					}
					else if (prefix == 'g') {
						size = size*factor*factor*factor;
					}
				}
				else if (units.Length == 1) {
					if (units.ToLower()[0] != 'b') {
						//ERROR: only single char unit is 'b'
					}							
				}
				else {
					// no units; assume bytes
				}
					
			}
			return size;
		}

		public double ReservedSpace {
			get {
				return ParseBytes(_reservedSpace);
			}
		}

		#region IEnumerable Members

		public IEnumerator GetEnumerator() {
			return _destinationDrives.GetEnumerator();
		}

		#endregion

	}  // end of class DestinationDriveCollection

	//////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// 
	/// </summary>
	/// 
	[Serializable]
	public class MirrorSet {
		private StringCollection _drives;
		private DestinationType _type;
		private double _reservedSpace;
		private string _name;

		public MirrorSet(DestinationType type) {
			_drives = new StringCollection();
			_type = type;
			_name = "NoName";
		}

		public MirrorSet(DestinationType type, string name) {
			_drives = new StringCollection();
			_type = type;
			_name = name;
		}

		public void Add(string drive) {
			_drives.Add(drive);
		}

		public int Count {
			get { return _drives.Count; }
		}

		public String this[int index] {
			get { return _drives[index]; }
		}

		public void Clear() {
			_drives.Clear();
			_type = DestinationType.Archive;
		}

		public void RemoveAt(int index) {
			_drives.RemoveAt(index);
		}

		public void Insert(int index, string drive) {
			_drives.Insert(index, drive);
		}

		public DestinationType Type {
			get { return _type; }
			set {_type = value;}
		}

		public double ReservedSpace {
			get { return _reservedSpace; }
			set { _reservedSpace = value; }
		}

		public string[] GetCopyOfDrivesArray() {
			// gets copy of string array contain drives list
			// Modifying this copy does not alter the drives in the MirrorSet object
			//	and changing the MirrorSet will not affect the copy.
			string[] array = new string[_drives.Count] ;
			_drives.CopyTo(array, 0);
			return array;
		}

		public string Name {
			get { return _name; }
			set { _name = value; }
		}

		#region IEnumerable Members

		public StringEnumerator GetEnumerator() {
			return _drives.GetEnumerator();
		}

		#endregion

	}  // end of class MirrorSet

}  // end of namespace
