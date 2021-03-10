using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace DACarter.NOAA
{
	// moved from FileNameParser:
	public enum DataFileType {
		PopDayFile,				// Dyydddx.SPC
		PopHourFile,			// Dyydddx.SPC (x=c...z)
        PopRawTSDayFile,        // *.raw.ts
        PopRawTSHourFile,       // *.raw.ts
        PopTSDayFile,           // *.ts
        PopTSHourFile,          // *.ts
        LapxmPopDayFile,		// Dsssyydddx.SPC
        LapxmPopHourFile,		// Dsssyydddhhx.SPC
        LapxmPopRawTSDayFile,		// Dsssyydddx.SPC
        LapxmPopRawTSHourFile,		// Dsssyydddhhx.SPC
        LapxmPopTSDayFile,		// Dsssyydddx.SPC
        LapxmPopTSHourFile,		// Dsssyydddhhx.SPC
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
	public struct ParsedFileNameStruct {
		public string Site;
		public DateTime TimeStamp;
		public DateTime EndTimeStamp;
		public string PopPrefix;
		public DataFileType FileType;
	}


	/// <summary>
	/// Summary description for DacDataFileBase.
	/// </summary>
	public abstract class DacDataFileBase
	{
		#region protected and private fields
		private string _fileName;
		protected BinaryReader _BReader;
		protected BinaryWriter _BWriter;
		protected DacStreamReader _TReader;
		protected TextWriter _TWriter;
		//protected OpenFileType _typeToOpen;
		private OpenFileType _openedAsType;
		private DataFileType _dataFileType;
		//private Type _fileClassType;
		private ArrayList _recordList;	// List of file position of beginning of each record.
											// so, after reading 1 record, _recordNumber==1,
											// and we are at position _recordList[1].
		protected int _recordNumber;		// record number that we are positioned before (0=BOF)
											// first record is record number 0
		private ArrayList _timeList;		// List of timeStamps for every record
		private DateTime _timeStamp;
		protected long _CurrentPosition;	// actual file position of reader; updated by subclasses
		private long _fileLength;
		private bool _haveReadEntireFile;
		private bool _atEOF, _atBOF;
		private bool _cancelRequest;

		// these fields used by textReaders only
		protected string _lineBuffer;	// contains the last read line being saved for the next record
		protected long _bufferSize;		// the number of bytes the line buffer line occupied in the file, not _lineBuffer.Length
		protected long _bufferPosition;	// the byte position of the file pointer at the END of the line that is in the line buffer

		#endregion

		#region public constructor
		public DacDataFileBase() {
			_recordList = new ArrayList(1500); 
			_timeList = new ArrayList(1500);

			Initialize();
		}

		private void Initialize() {
			_timeList.Clear();
			_recordList.Clear();
			_haveReadEntireFile = false;
			_atBOF = true;
			_atEOF = false;
			if (_BReader != null) {
				_BReader.Close();
			}
			if (_BWriter != null) {
				_BWriter.Close();
			}
			if (_TReader != null) {
				_TReader.Close();
			}
			if (_TWriter != null) {
				_TWriter.Close();
			}
			_BReader = null;
			_BWriter = null;
			_TReader = null;
			_TWriter = null;
			_fileName = "";
			_openedAsType = OpenFileType.NotOpened;
			_CurrentPosition = 0;
			_fileLength = 0;
			_recordNumber = 0;					
			_timeStamp = DateTime.MinValue;
			_cancelRequest = false;
			_dataFileType = DataFileType.UnknownFile;
		}

		#endregion


		#region public and protected abstract methods
		//
		// Note to inheritors:
		// Avoid calling peekChar -- really slows down reading
		// OK to call stream.Length and stream.Position after every record
		//
		protected abstract long CustomReadFileHeader();	// read file header after opening; return file position
		protected abstract bool CustomReadRecord(DacData data, out DateTime timeStamp, out long recordLength, out long filePositionChange);
		protected abstract bool CustomSkipRecord(out long currentPosition);		// not really needed
		protected abstract bool CustomReadRecordTime(out DateTime timeStamp, out long recordLength, out long filePositionChange);
		//protected abstract bool CustomOpenInit();  // this method is now virtual; only override if needed
		protected abstract OpenFileType GetOpenFileType();	// returns proper OpenFileType
		#endregion

		#region public implemented methods

		// //////////////////////////////////////////////////////////////////
		//
		// These public methods are fully implemented in this base class
		//
		// //////////////////////////////////////////////////////////////////

		public enum OpenFileType {
			NotOpened,
			TextReader,
			TextWriter,
			BinaryReader,
			BinaryWriter
		}

		public enum WriteFileMode {
			Append,
			Overwrite
		}


		/// <summary>
		/// Opens fileName for reading.
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns>
		/// true if file was successfully opened.
		/// false if this file reader object is wrong type for the requested fileName.
		/// throws exception if open attempt failed.
		/// </returns>
		public bool OpenFileReader(string fileName) {
			// calls OpenFileReaderType with proper type
			return OpenFileReaderType(fileName, GetOpenFileType());
		}

        // POPREV: added CloseFileReader 2012/05/24
        protected void CloseFileReader() {
            CloseFileReader(GetOpenFileType());
        }

		public bool ReadNextRecord(DacData data) {

			long recordLength;
			DateTime timeStamp;
			long filePositionChange;
			long beginPosition = preRead();

			// call read method in derived class
			data.Clear();
			bool readOK = CustomReadRecord(data, out timeStamp, out recordLength, out filePositionChange);

			postRead(readOK, beginPosition, recordLength, timeStamp, filePositionChange);

			return readOK;

		}

		public bool ReadNextRecordTime(out DateTime timeStamp) {

			long recordLength;
			long filePositionChange;
			long beginPosition = preRead();

			// call read method in derived class
			bool readOK = CustomReadRecordTime(out timeStamp, out recordLength, out filePositionChange);

			postRead(readOK, beginPosition, recordLength, timeStamp, filePositionChange);

			return readOK;

		}

		public bool MoveToRead(int recordNumber, DacData data) {
			//  TODO: handle recordNumber > numRec
			MoveTo(recordNumber);
			return ReadNextRecord(data);
		}

		public bool MoveToRead(DateTime time, DacData data) {
			MoveTo(time);
			return ReadNextRecord(data);
		}

		/// <summary>
		/// Same function as a Move() followed by a ReadNextRecord()
		/// except that the Move is relative to the beginning of the
		/// last record read rather than the current file position
		/// (which is at the end of the last record read).
		/// </summary>
		/// <param name="recordsToSkip"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public bool MoveRead(int recordsToSkip, DacData data) {
			Move(recordsToSkip-1);
			return ReadNextRecord(data);
		}

		public bool MoveRead(TimeSpan timeInterval, DacData data) {
			Move(timeInterval);
			return ReadNextRecord(data);
		}

		public void ReadBOF(DacData data) {
			//_comFile.SetFileName(DataPackage.InputFile);
			GoToBOF();
			ReadNextRecord(data);
		}

		public bool GoToBOF() {
			try {
				_recordNumber = 0;
				if (_recordList.Count > 0) {
					_CurrentPosition = (long)_recordList[0];
				}
				else {
					_CurrentPosition = 0;
				}
				Seek(_CurrentPosition, SeekOrigin.Begin);
				checkBofEof();
			}
			catch (Exception) {
				return false;
			}
			return true;
		}

		public bool GoToEOF() {
			if (_haveReadEntireFile) {
				Seek(0, SeekOrigin.End);
				_CurrentPosition = _fileLength-1;
				_recordNumber = _recordList.Count-1;
			}
			else {
				MoveForwardRecord(int.MaxValue);
			}
			return true;
		}

		public void ReadEOF(DacData data) {
			//SetFileName(DataPackage.InputFile);
			GoToEOF();
			// after reaching EOF, back over last record and re-read it
			Move(-1);
			ReadNextRecord(data);
		}

		public bool Move(int nrec) {
			if (nrec == 0) {
				return true;
			}
			else if (nrec > 0) {
				return MoveForwardRecord(nrec);
			}
			else {
				return MoveBackwardRecord(Math.Abs(nrec));
			}
		}

		public bool MoveTo(int recordNumber) {
			// moves to just before record #recordNumber (first record is 0)
			int offset = recordNumber - _recordNumber;
			return Move(offset);
			
			/*
			if (recordNumber > _recordList.Count) {
				GoToEOF();
			}
			long position = (long)_recordList[recordNumber-1];
			if (_openedAsType == OpenFileType.OpenBinaryReader) {
				_bReader.BaseStream.Seek(position, SeekOrigin.Begin);
				_position = position;
				_recordNumber = recordNumber-1;
				return true;
			}
			else if (_openedAsType == OpenFileType.OpenTextReader) {
				_tReader.BaseStream.Seek(position, SeekOrigin.Begin);
				_position = position;
				_recordNumber = recordNumber-1;
				return true;
			}
			else {
				throw new ArgumentException("DataFileBase.CloseFileReader: Improper file type.");
			}
			*/
		}

		public bool Move(TimeSpan span) {
			DateTime targetTime;
			DateTime timeStamp;
			/*
			if (AtBOF) {
				ReadNextRecordTime(out timeStamp);
				Move(-1);
			}
			*/
			if (_timeStamp == DateTime.MinValue) {
				// at BOF before reading any records
				ReadNextRecordTime(out timeStamp);
				Move(-1);
			}
			else {
				timeStamp = _timeStamp;
			}
			targetTime = GetTimeIntervalBoundary(timeStamp, span);
			int prevRec = _recordNumber;
			MoveTo(targetTime);
			if (span < TimeSpan.Zero) {
				// special case if we are backing up.
				// make sure we actually moved.
				int currRec = _recordNumber;
				if ((currRec == prevRec) && (currRec != 0)) {
					// didn't move and we are not at BOF
					// so move back to interval before this one.
					DateTime newTarget = GetTimeIntervalBoundary(targetTime, span);
					MoveTo(newTarget);
				}
			}
			return true;
		}

		public bool MoveTo(DateTime date) {
			bool readMore = false;
			if (_timeList.Count > 0) {
				// we have already read some records
				// Go to beginning or end of these records
				//	if date is outside their timestamp range
				DateTime firstTime = (DateTime)_timeList[0];
				if (date <= firstTime) {
					return GoToBOF();
				}
				DateTime lastTime = (DateTime)_timeList[_timeList.Count-1];
				if (date >= lastTime) {
					// move to last read record
					MoveTo(_timeList.Count-1);
					// is there more to read?
					if (!_haveReadEntireFile) {
						readMore = true;
					}
				}
				else {
					// date is within range we have already read
					//	so look up time in list.
					DateTime timeStamp;
					int recordNumber=0;
					for (int irec = 1; irec < _timeList.Count; irec++) {
						timeStamp = (DateTime) _timeList[irec];
						if (timeStamp >= date) {
							recordNumber = irec;	
							break;
						}
					}
					MoveTo(recordNumber);
				}
			}
			else {
				readMore = true;
			}

			if (readMore) {
				bool readOK = false;
				DateTime timeStamp;
				do {
					readOK = ReadNextRecordTime(out timeStamp);
				} while (readOK && (timeStamp < date) );
				Move(-1);
			}

			return true;
		}

		#endregion

		#region public properties

		public bool FileIsOpen {
			get {
				if (_openedAsType == OpenFileType.NotOpened) {
					return false;
				}
				else {
					return true;
				}
			}
		}

		public int RecordNumber {
			get {return _recordNumber;}
		}

		public long Position {
			//get {return _CurrentPosition;}
			get {
				if (_recordNumber < _recordList.Count) {
					return (long)_recordList[_recordNumber];
				}
				else {
					return -99;
				}
			}
		}

		public long BeginRecordPosition {
			//get {return _CurrentPosition;}
			get {
				if ((_recordNumber < _recordList.Count) && (_recordNumber > 0)) {
					return (long)_recordList[_recordNumber-1];
				}
				else {
					return -99;
				}
			}
		}

		public DateTime TimeStamp {
			get {return _timeStamp;}
		}

		public bool AtBOF {
			get {return _atBOF;}
		}

		public bool AtEOF {
			get {return _atEOF;}
		}

		public string FileName {
			get {return _fileName;}
		}

		public bool CancelRequest {
			get {return _cancelRequest;}
			set {_cancelRequest = value;}
		}

		#endregion


		#region protected implemented methods

		// //////////////////////////////////////////////////////////////////
		//
		// These protected methods are fully implemented in this base class
		//
		// //////////////////////////////////////////////////////////////////

		
		protected bool BaseOpenInit() {
			// these are only used in TextReader file types
			_lineBuffer = String.Empty;
			_bufferSize = 0;
			_bufferPosition = -1;
			return true;
		}

		// this can be overridden by derived class if it needs to initialize on Open() --
		//	-- but remember to call BaseOpenInit
		protected virtual bool CustomOpenInit() {
			return BaseOpenInit();
		}
		
		/// <summary>
		/// Returns first line of a record for a TextReader file.
		/// The line is obtained either from the buffer, left over from a previous ReadRecord,
		///		or directly from the text file;
		///	This helper method NEEDS to be called ONLY if need buffer to read ahead into next record,
		///		but should work for any TextReader file derived from this base class.
		/// </summary>
		/// <example>
		/// Sample usage in CustomReadRecord method of derived class:
		/// <code>
		///		base.GetFirstLineOfRecord(out line, out recordSize, out filePositionChange);
		///		if (line == null) 
		///			return;  //EOF
		///		do {
		///			line = _TReader.ReadLine();
		///			if (line == null)
		///				break;  //EOF
		///			else
		///				if (LineBelongsInCurrentRecord)
		///					recordSize += _TReader.LineLength;
		///		} while LineBelongsInCurrentRecord;
		///		base.UpdateLineBuffer(line, filePositionChange);
		/// </code>
		/// </example>
		/// <param name="line"></param>
		/// <param name="lineLength">The proper line length to compute the record size.</param>
		/// <param name="filePositionChange">The number of bytes that the file pointer moved</param>		
		protected void GetFirstLineOfRecord(out string line, out long lineLength, out long filePositionChange) {
			lineLength = 0;
			filePositionChange = 0;
			//long currentPosition = _CurrentPosition;	// from base class, file position before read
	
			if ((_lineBuffer == string.Empty) || (_CurrentPosition != _bufferPosition)) {
				// There is no previously read line in line buffer or we have moved to a different record,
				// so read a new line.
				_lineBuffer = string.Empty;
				_bufferSize = 0;
				line = _TReader.ReadLine();
				if (line != null) {
					lineLength = _TReader.LineLength;
					filePositionChange = _TReader.LineLength;
				}
				else {
					lineLength = 0;
				}
			}
			else {
				// read line in buffer
				line = _lineBuffer;
				lineLength = _bufferSize;
			}
		}

		/// <summary>
		/// Takes last line read and puts it in line buffer.
		///	Assumes that this line belongs to the next record, which
		///		will be read later.
		///	If this method is called when NOT buffering data, line must be null.
		/// </summary>
		/// <param name="line"></param>
		/// <param name="filePositionChange">Number of bytes file pointer has moved during reading of 
		///		last record, including this buffer line.
		///	</param>
		protected void UpdateLineBuffer(string line, long filePositionChange) {
			if (line != null) {
				_lineBuffer = line;
				_bufferSize = _TReader.LineLength;
			}
			else {
				_lineBuffer = string.Empty;
			}
			_bufferPosition = _CurrentPosition + filePositionChange;
		}


		/// <summary>
		/// Opens file for reading;
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="openFileType"></param>
		/// <returns>
		/// true if file was successfully opened or is already open.
		/// false if this file object is wrong type to read the file fileName.
		/// throws exception if open attempt failed.
		/// </returns>
		protected bool OpenFileReaderType(string fileName, OpenFileType openFileType) {
			ParsedFileNameStruct info;
			DataFileType newDataFileType = GetFileTypeFromName(fileName, out info);
			if (_dataFileType != DataFileType.UnknownFile) {
				if (newDataFileType != _dataFileType) {
					return false;
				}
			}
			bool openOK = true;
			/*
			if (FileIsOpen && (_openedAsType!= fileType)) {
				throw new IOException("DataFile: This object previously opened a file in a different mode");
			}
			*/
			if (FileIsOpen && (_fileName != fileName)) {
				// trying to open a different file - OK
				// but must close old file first.
				CloseFileReader(openFileType);
			}
			if (!FileIsOpen) {
				openOK = OpenThisFileForReading(fileName, openFileType);
				if (openOK) {
					return true;
				}
				else {
					throw new IOException("DacDataFileBase.OpenFileReader: FileReader was not created\nCheck file name. File may not exist");
				}
			}
			else {
				return true;
			}
		}

		protected bool OpenThisFileForReading(string fileName, OpenFileType fileType) {

			Initialize();	// clear everything because we are starting over with new file
			_fileName = fileName;

			bool openedOK = false;
			if (File.Exists(fileName)) {
				if (fileType == OpenFileType.TextReader) {
					// open text file
					try {
						int bufferSize = 128;
						_TReader = new DacStreamReader(fileName, Encoding.UTF8, true, bufferSize);
						_openedAsType = OpenFileType.TextReader;
						_fileLength = _TReader.BaseStream.Length;
						openedOK = true;
					}
					catch (Exception) {
						//Console.Write(e);
						openedOK = false;
					}
				}
				else if (fileType == OpenFileType.BinaryReader) {
					// open binary file
					FileStream fs = null;
					try {
						fs = new FileStream(fileName,FileMode.Open,FileAccess.Read);
						_BReader = new BinaryReader(fs);
						_openedAsType = OpenFileType.BinaryReader;
						_fileLength = _BReader.BaseStream.Length;
						openedOK = true;
					}
					catch (Exception) {
						//Console.Write(e);
						openedOK = false;
						if (fs != null) {
							fs.Close();
						}
						if (_BReader != null) {
							_BReader.Close();
						}
					}
				}
				else {
					throw new ArgumentException("DataFileBase.OpenThisFileForReading: Improper file type.");
				}
			}

			if (openedOK) {
				ParsedFileNameStruct info;
				CustomOpenInit();							// do custom init for subclass
				_dataFileType = GetFileTypeFromName(fileName, out info);
				_CurrentPosition = CustomReadFileHeader();	// read file header if any
				_recordList.Add(_CurrentPosition);			// set first entry in record list
				return true;
			}
			else {
				_openedAsType = OpenFileType.NotOpened;
				_recordList.Clear();
				_fileLength = 0;
				return false;
			}

			/*
			if (fileType == OpenFileType.BinaryReader) {
				_BReader = null;
				FileStream fs = null;
				try {
					fs = new FileStream(fileName,FileMode.Open,FileAccess.Read);
					_BReader = new BinaryReader(fs);
					_openedAsType = OpenFileType.BinaryReader;
					_fileLength = _BReader.BaseStream.Length;
					_CurrentPosition = CustomReadFileHeader();
					_recordList.Add(_CurrentPosition);		// set first entry in record list
					CustomOpenInit();						// do custom init for subclass
					return true;
				}
				catch (Exception e) {
					if (fs != null) {
						fs.Close();
					}
					if (_BReader != null) {
						_BReader.Close();
					}
					_openedAsType = OpenFileType.NotOpened;
					_recordList.Clear();
					_fileLength = 0;
					_fileName = "";
					return false;
				}
			}
			else if (fileType == OpenFileType.TextReader) {
				_TReader = null;
				if (File.Exists(fileName)) {
					//_tReader = File.OpenText(fileName);
					// _tReader = new DacStreamReader(fileName);
					int bufferSize = 128;
					_TReader = new DacStreamReader(fileName, Encoding.UTF8, true, bufferSize);
				}
				if (_TReader != null) {
					_openedAsType = OpenFileType.TextReader;
					_fileLength = _TReader.BaseStream.Length;
					_CurrentPosition = CustomReadFileHeader();
					_recordList.Add(_CurrentPosition);		// set first entry in record list
					CustomOpenInit();						// do custom init for subclass
					return true;
				}
				else {
					_openedAsType = OpenFileType.NotOpened;
					_recordList.Clear();
					_fileLength = 0;
					_fileName = "";
					return false;
				}
			}
			else {
				throw new ArgumentException("DataFileBase.CloseFileReader: Improper file type.");
			}
			*/
		}

		protected void CloseFileReader(OpenFileType fileType) {
			_openedAsType = OpenFileType.NotOpened;
			_fileLength = 0;
			_recordNumber = 0;
			_CurrentPosition = 0;
			if (fileType == OpenFileType.BinaryReader) {
                if (_BReader != null) {
                    _BReader.Close();
                    _BReader = null;
                }
			}
			else if (fileType == OpenFileType.TextReader) {
                if (_TReader != null) {
                    _TReader.Close();
                    _TReader = null;
                }
			}
			else {
				throw new ArgumentException("DataFileBase.CloseFileReader: Improper file type.");
			}
		}

		protected bool OpenFileWriterType(string fileName, OpenFileType openFileType, WriteFileMode writeMode) {
			ParsedFileNameStruct info;
			DataFileType newDataFileType = GetFileTypeFromName(fileName, out info);
			if (_dataFileType != DataFileType.UnknownFile) {
				if (newDataFileType != _dataFileType) {
					return false;
				}
			}
			bool openOK = true;
			/*
			if (FileIsOpen && (_openedAsType!= fileType)) {
				throw new IOException("DataFile: This object previously opened a file in a different mode");
			}
			*/
			if (FileIsOpen && (_fileName != fileName)) {
				// trying to open a different file - OK
				// but must close old file first.
				CloseFileWriter(openFileType);
			}
			if (!FileIsOpen) {
				openOK = OpenThisFileForWriting(fileName, openFileType, writeMode);
				if (openOK)	{
					return true;
				}
				else {
					throw new IOException("DacDataFileBase.OpenFileReader: FileWriter was not created.");
				}
			}
			else {
				return true;
			}
		}

		private bool OpenThisFileForWriting(string fileName, OpenFileType fileType, WriteFileMode mode)	{
			Initialize();	// clear everything because we are starting over with new file
			_fileName = fileName;

			bool openedOK = false;
			if (File.Exists(fileName)) {
				if (fileType == OpenFileType.TextWriter) {
					throw new ArgumentException("DataFileBase.OpenThisFileForWriting: Text write not supported.");
					// open text file
					/*
					try
					{
						int bufferSize = 128;
						_TReader = new DacStreamReader(fileName, Encoding.UTF8, true, bufferSize);
						_openedAsType = OpenFileType.TextReader;
						_fileLength = _TReader.BaseStream.Length;
						openedOK = true;
					}
					catch (Exception)
					{
						//Console.Write(e);
						openedOK = false;
					}
					*/
				}
				else if (fileType == OpenFileType.BinaryWriter)	{
					// open binary file
					FileStream fs = null;
					try
					{
						if (mode == WriteFileMode.Append) {
							fs = new FileStream(fileName, FileMode.Append, FileAccess.Write);
						}
						else {
							fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
						}
						_BWriter = new BinaryWriter(fs);
						_openedAsType = OpenFileType.BinaryWriter;
						_fileLength = _BWriter.BaseStream.Length;
						openedOK = true;
					}
					catch (Exception)
					{
						//Console.Write(e);
						openedOK = false;
						if (fs != null)
						{
							fs.Close();
						}
						if (_BWriter != null)
						{
							_BWriter.Close();
						}
					}
				}
				else {
					throw new ArgumentException("DataFileBase.CloseFileWriter: Improper file type.");
				}
			}

			if (openedOK) {
				ParsedFileNameStruct info;
				CustomOpenInit();							// do custom init for subclass
				_dataFileType = GetFileTypeFromName(fileName, out info);
				//_CurrentPosition = CustomReadFileHeader();	// read file header if any
				//_recordList.Add(_CurrentPosition);			// set first entry in record list
				return true;
			}
			else {
				_openedAsType = OpenFileType.NotOpened;
				_recordList.Clear();
				_fileLength = 0;
				return false;
			}

		}

		private void CloseFileWriter(OpenFileType fileType) {
			_openedAsType = OpenFileType.NotOpened;
			_fileLength = 0;
			_recordNumber = 0;
			_CurrentPosition = 0;
			if (fileType == OpenFileType.BinaryWriter) {
                if (_BWriter != null) {
                    _BWriter.Close();
                    _BWriter = null;
                }
			}
			else if (fileType == OpenFileType.TextWriter) {
				throw new ArgumentException("DataFileBase.CloseFileWriter: Improper file type.");
				//_TReader.Close();
			}
			else {
				throw new ArgumentException("DataFileBase.CloseFileReader: Improper file type.");
			}
		}

		protected void checkBofEof() {
			_atBOF = false;
			_atEOF = false;
			if (_CurrentPosition  == _fileLength) {
				_haveReadEntireFile = true;
				_atEOF = true;
			}
			else if (_recordNumber <= 1) {
				_atBOF = true;
			}
			else if (_haveReadEntireFile) {
				if (_recordNumber == _recordList.Count-1) {
					_atEOF = true;
				}
			}
		}  // end of checkBofEof()


		#endregion

		#region private implemented methods

		// //////////////////////////////////////////////////////////////////
		//
		// These private methods are fully implemented in this base class
		//
		// //////////////////////////////////////////////////////////////////

		/// <summary>
		/// Called after each call to CustomReadRecord...() methods.
		/// This method is the only method that modifies _CurrentPosition after a read record.
		/// </summary>
		/// <param name="readOK"></param>
		/// <param name="beginRecordPosition">The file pointer position at the beginning of the record that we just read.</param>
		/// <param name="recordLength">Number of bytes in the record we just read.</param>
		/// <param name="timeStamp"></param>
		/// <param name="filePositionChange">The number of bytes that the file pointer moved during that read.</param>
		private void postRead(bool readOK, long beginRecordPosition, long recordLength, DateTime timeStamp, long filePositionChange) {

			_CurrentPosition += filePositionChange;

			if (readOK) {
				_timeStamp = timeStamp;
				// if we got this far, must be a good record. Add to list:
				_recordNumber++;
				if (_recordNumber == _recordList.Count) {
					// reading next new record, add current file position to list
					_recordList.Add(beginRecordPosition + recordLength);
					_timeList.Add(timeStamp);
				}
				else {
					// sanity check
					//long currentPosition = _bReader.BaseStream.Position;
					if ((long)_recordList[_recordNumber-1] != beginRecordPosition) {
						throw new InvalidOperationException("DataFileBase.ReadRecord: record number mismatch on reread");
					}
				}
			}
			else {
				
			}
			checkBofEof();
		}

		private long preRead() {
			if (!FileIsOpen) {
				throw new InvalidOperationException("DataFileBase.ReadRecord: File not opened before Read()");
			}
	
			if (_recordNumber >= _recordList.Count) {
				// this record position should have been in list
				throw new InvalidOperationException("DataFileBase.ReadRecord: record number mismatch");
			}
	
			long beginRecordPosition;
			//beginFilePosition = _CurrentPosition;
			beginRecordPosition = (long)_recordList[_recordNumber];
			//long recPosition = (long)_recordList[_recordNumber];
			return beginRecordPosition;
		}

		private bool MoveForwardRecord(int nrec) {
			// TODO improve this
			// 1) do simple read recs if nrec is small
			// 2) don't seek if already at end of list
			//
			// returns false if stops at EOF? Or not?
			bool readOK;
			if (_haveReadEntireFile) {
				if ((nrec + _recordNumber) >= _recordList.Count ) {		
					// jump directly to EOF
					Seek(0, SeekOrigin.End);
					_CurrentPosition = _fileLength;
					_recordNumber = _recordList.Count-1;
				}
				else {
					long position = (long)_recordList[nrec + _recordNumber];
					Seek(position, SeekOrigin.Begin);
					_CurrentPosition = position;
					_recordNumber += nrec;
				}
				checkBofEof();
			}
			else {
				// skip ahead as far as we can from list
				//int skipTo=0;
				int skipTo = ((long)_recordNumber + (long)nrec) >= _recordList.Count-1 ? (_recordList.Count-1) : (_recordNumber + nrec);
				int skipped = skipTo - _recordNumber;
				//long position;
				if (skipped > 0) {
					long position = (long)_recordList[skipTo];
					Seek(position, SeekOrigin.Begin);
					_CurrentPosition = position;
				}
				_recordNumber = skipTo;
				//_CurrentPosition = position;
				if (skipTo > 0) {
					_timeStamp = (DateTime)_timeList[skipTo-1];
				}

				// then read the rest of the records
				if (skipped < nrec) {
					for (int i=0; i<(nrec-skipped); i++) {
						DateTime timeStamp;
						readOK = ReadNextRecordTime(out timeStamp);
						//_position = currentPos;
						checkBofEof();
						if (!readOK) {
							//return false;
							break;
						}
					}
				}
			
				checkBofEof();
			}
			return true;
		}

		private bool MoveBackwardRecord(int nrec) {
			if (nrec >= _recordNumber) {
				return GoToBOF();
			}
			else {
				int newRecordNumber = _recordNumber - nrec;
				long position = (long)_recordList[newRecordNumber];
				Seek(position, SeekOrigin.Begin);
				_recordNumber = newRecordNumber;
				_CurrentPosition = position;
				return true;
			}
		}

		private long Seek(long offset, SeekOrigin origin) {
			if (_openedAsType == OpenFileType.BinaryReader) {
				return _BReader.BaseStream.Seek(offset, origin);
			}
			else if (_openedAsType == OpenFileType.TextReader) {
				_TReader.DiscardBufferedData();
				return _TReader.BaseStream.Seek(offset, origin);
			}
			else {
				throw new ArgumentException("DataFileBase.Seek: Improper file type.");
			}
		}

		private DateTime GetTimeIntervalBoundary(DateTime currentTime, TimeSpan timeInterval) {

			TimeSpan span1Hr = new TimeSpan(0,1,0,0);
			TimeSpan span1Day = new TimeSpan(1,0,0,0);
			TimeSpan span1Min = new TimeSpan(0,0,1,0);

			DateTime searchTime;
			TimeSpan roundedTimeInterval;
			bool searchBackwards = false;

			if (timeInterval < TimeSpan.Zero) {
				timeInterval = timeInterval.Duration();
				searchBackwards = true;
			}

			if (timeInterval < span1Min) {
				int boundary = timeInterval.Seconds;
				roundedTimeInterval = new TimeSpan(0,0,0,boundary);
				// truncate current time to previous timeInterval second boundary
				int prevSecond = (currentTime.Second/boundary)*boundary;
				searchTime = new DateTime(currentTime.Year,
											currentTime.Month,
											currentTime.Day,
											currentTime.Hour,
											currentTime.Minute,
											prevSecond);
			}
			else if (timeInterval < span1Hr) {
				int boundary = timeInterval.Minutes;
				roundedTimeInterval = new TimeSpan(0,0,boundary,0);
				// truncate current time to previous timeInterval minute boundary
				int prevMinute = (currentTime.Minute/boundary)*boundary;
				searchTime = new DateTime(currentTime.Year,
					                        currentTime.Month,
					                        currentTime.Day,
					                        currentTime.Hour,
					                        prevMinute,
					                        0);
			}
			else if (timeInterval < span1Day) {
				int boundary = timeInterval.Hours;
				roundedTimeInterval = new TimeSpan(0,boundary,0,0);
				// truncate current time to previous timeInterval hour boundary
				int prevHour = (currentTime.Hour/boundary)*boundary;
				searchTime = new DateTime(currentTime.Year,
					                        currentTime.Month,
					                        currentTime.Day,
					                        prevHour,
					                        0,
					                        0);
			}
			else {
				// truncate current time to previous day boundary
				roundedTimeInterval = new TimeSpan(timeInterval.Days,0,0,0);
				searchTime = new DateTime(currentTime.Year,
					                        currentTime.Month,
					                        currentTime.Day,
					                        0, 0, 0);
			}

			if (searchBackwards) {
				// if moving backwards, use this searchTime as is
				//	unless it is the same as current time,
				//	then go back another interval.
				if (searchTime == currentTime) {
					searchTime -= roundedTimeInterval;
				}
			}
			else {
				// if moving forward, 
				// move timeInterval forward
				searchTime += roundedTimeInterval;
			}
			return searchTime;

		}

		#endregion

		#region private static fields
/*		
		// Moved to FileNameParser class:
		//
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
		// PDT files; file = DacPdtFile; data = PdtData:WindsData
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
		private static Regex regexAsciiMom = new Regex(@"^(?<year1digit>\d)(?<day>\d\d\d)(?<hour>\d\d)(?<minute>\d\d)\.MO(D|H)$",
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
		private static Regex regexDMU =  new Regex(@"^(?<year>\d\d)(?<day>\d\d\d)(?<hour>\d\d)\.DAT",
			RegexOptions.IgnoreCase |
			RegexOptions.ExplicitCapture |
			RegexOptions.Compiled);
		// Crossbow DMU, proposed rename of file:
		private static Regex regexDMU2 = new Regex(@"^DMU(?<year>\d\d)(?<day>\d\d\d)(?<hour>\d\d)\.TXT",
			RegexOptions.IgnoreCase |
			RegexOptions.ExplicitCapture |
			RegexOptions.Compiled);
		// NWS radiosonde file (as from Miramar, San Diego) file = DacRadiosonde1File; data = WIndsData
		private static Regex regexSonde1 = new Regex(@"^(?<site>\w{3})(?<hour>\d{1,2})(?<dayofmonth>\d\d)\.TXT",
			RegexOptions.IgnoreCase |
			RegexOptions.ExplicitCapture |
			RegexOptions.Compiled);
		private static Regex regexDarwinMsgArchive = new Regex(@"^.(?<year>\d\d)(?<day>\d\d\d)_.*\.dat",
			RegexOptions.IgnoreCase |
			RegexOptions.ExplicitCapture |
			RegexOptions.Compiled);
*/		
		#endregion


		#region public static methods

		/*
		public struct ParsedFileNameStruct {
			public string Site;
			public DateTime TimeStamp;
			public DateTime EndTimeStamp;
			public string PopPrefix;
		} 
		*/

		// this static structure contains info about the filename
		//	that was passed to the GetFileTypeFromName static method.
		private static ParsedFileNameStruct _parsedFileName;

		/// <summary>
		/// Returns a new data object of the proper type
		///		derived from DacData type.
		/// </summary>
		/// <param name="fileType"></param>
		/// <returns></returns>
		public static DacData GetDataObjectForFileType(DataFileType fileType) {
			if ((fileType == DataFileType.PopDayFile) ||
				(fileType == DataFileType.PopHourFile) ||
				(fileType == DataFileType.LapxmPopDayFile) ||
				(fileType == DataFileType.LapxmPopHourFile) ||
				(fileType == DataFileType.ComCFile) || 
				(fileType == DataFileType.ComLoopFile)) {
					return new PopData();
			}
			else if ((fileType == DataFileType.CnsDayFile) || 
				(fileType == DataFileType.DomsatMultipleMsgFile) ||
				(fileType == DataFileType.DomsatSingleMsgFile) ||
				(fileType == DataFileType.CnsDayFile) ||
				(fileType == DataFileType.EtlCnsHourFile) || 
				(fileType == DataFileType.TogaFile) ||
				(fileType == DataFileType.Radiosonde1)) {
					return new WindsData();
			}
			else if (fileType == DataFileType.PdtFile) {
				return new PdtData();	// note: is derived from WindsData
			}
			else if ((fileType == DataFileType.CrossbowDMU) ||
				(fileType == DataFileType.SmoGps)) {
				return new SurfaceData();
			}
			else {
				throw new ArgumentException("Unknown DataFileType in GetDataObjectForFileType() arg! Check file name.");
			}
		}

		public static DacDataFileBase GetFileObjectForFileType(DataFileType fileType) {
			if ((fileType == DataFileType.PopDayFile) ||
				(fileType == DataFileType.PopHourFile) ||
				(fileType == DataFileType.LapxmPopDayFile) ||
				(fileType == DataFileType.LapxmPopHourFile)) {
				return new DacPopFile();
			}
			else if ((fileType == DataFileType.ComCFile) || 
				(fileType == DataFileType.ComLoopFile)) {
				return new DacComFile();
			}
			else if (fileType == DataFileType.PdtFile) {
				return new DacPdtFile();
			}
			else if ((fileType == DataFileType.CnsDayFile) ||
				(fileType == DataFileType.EtlCnsHourFile)) {
				return new DacCnsFile();
			}
			else if ((fileType == DataFileType.DomsatSingleMsgFile) ||
				(fileType == DataFileType.DomsatMultipleMsgFile)) {
				return new DacDomsatFile();
			}
			else if (fileType == DataFileType.CrossbowDMU) {
				return new DacDmuFile();
			}
			else if (fileType == DataFileType.SmoGps) {
				return new DacSmoGpsFile();
			}
			else if (fileType == DataFileType.Radiosonde1) {
				return new DacRadiosonde1File();
			}
			else {
				throw new ArgumentException("Unknown DataFileType in GetFileObjectForFileType() arg.");
			}
		}

		/*
		public static Type GetFileObjectTypeForFileType(DataFileType fileType) {
			if ((fileType == DataFileType.PopDayFile) ||
				(fileType == DataFileType.PopHourFile) ||
				(fileType == DataFileType.LapxmPopDayFile) ||
				(fileType == DataFileType.LapxmPopHourFile)) {
				return typeof(DacPopFile);
			}
			else if ((fileType == DataFileType.ComCFile) || 
				(fileType == DataFileType.ComLoopFile)) {
				return typeof(DacComFile);
			}
			else if (fileType == DataFileType.PdtFile) {
				return typeof(DacPdtFile);
			}
			else {
				throw new ArgumentException("Unknown DataFileType in GetFileObjectForFileType() arg.");
			}
		}
		*/

		/// <summary>
		/// Converts a POP header file name to the matching data file name.
		/// </summary>
		/// <param name="hFile">string containing header file name</param>
		/// <returns>string containing data file name</returns>
		public static string GetDataFileFromHeaderFile(string hFile) {
			string fName = Path.GetFileName(hFile);
			if ((fName.ToLower()).StartsWith("h")) {
				if (char.IsLower(fName, 0)) {
					fName = fName.Remove(0, 1);
					fName = fName.Insert(0, "d");
				}
				else {
					fName = fName.Remove(0, 1);
					fName = fName.Insert(0, "D");
				}
				string folder = Path.GetDirectoryName(hFile);
				string dFile = Path.Combine(folder, fName);
				return dFile;
			}
			else {
				// failed to convert, return quietly
				return hFile;
			}
		}


		/// <summary>
		/// Returns the DataFileType of the fileName argument
		///		and fills the ParsedFileName structure
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public static DataFileType GetFileTypeFromName(string fileName, out ParsedFileNameStruct info) {

			FileNameParser parser = new FileNameParser();

			parser.Parse(fileName, out info);

			return info.FileType;

/*
			_parsedFileName.Site = "??";
			_parsedFileName.TimeStamp = DateTime.MinValue;
			_parsedFileName.EndTimeStamp = DateTime.MaxValue;


			fileName = fileName.Trim(' ');
			fileName = Path.GetFileName(fileName);

			Match match;
			//string yearMatch, monthMatch, dayMonthMatch, dayYearMatch, hourMatch, minuteMatch;

			if (fileName.Trim().ToUpper() == "GPS1.OUT") {
				_parsedFileName.EndTimeStamp = DateTime.MinValue;
				_parsedFileName.Site = "Buoy";
				_parsedFileName.TimeStamp = DateTime.MinValue;
				info = _parsedFileName;
				return DataFileType.SmoGps;
			}

			// POP day file: DyydddX.SPC
			match = regexPopDay.Match(fileName);
			if (match.Success) {
				ParseFileName(match);
				info = _parsedFileName;
				return DataFileType.PopDayFile;
			}

			// original POP hour file: Dyydddx.SPC
			match = regexPopHour.Match(fileName);
			if (match.Success) {
				ParseFileName(match);
				info = _parsedFileName;
				return DataFileType.PopHourFile;
			}

			// Lapxm day file: DsssyydddX.spc
			match = regexLapxmPopDay.Match(fileName);
			if (match.Success) {
				ParseFileName(match);
				info = _parsedFileName;
				return DataFileType.LapxmPopDayFile;
			}

			// Lapxm hour file: DsssyydddhhX.spc
			match = regexLapxmPopHour.Match(fileName);
			if (match.Success) {
				ParseFileName(match);
				info = _parsedFileName;
				return DataFileType.LapxmPopHourFile;
			}

			// PDT file: SSS_ia_yyy_ddd_ddd.TXT
			match = regexPdt0.Match(fileName);
			if (match.Success) {
				ParseFileName(match);
				info = _parsedFileName;
				return DataFileType.PdtFile;
			}

			// COM "C" file: Cddd.ddd
			match = regexComC.Match(fileName);
			if (match.Success) {
				ParseFileName(match);
				info = _parsedFileName;
				return DataFileType.ComCFile;
			}

			// COM "loop" file: LOOPddd.yy
			match = regexComLoop.Match(fileName);
			if (match.Success) {
				ParseFileName(match);
				info = _parsedFileName;
				return DataFileType.ComLoopFile;
			}

			// Consensus day files: Wyyddd.CNS
			match = regexCnsDay.Match(fileName);
			if (match.Success) {
				ParseFileName(match);
				info = _parsedFileName;
				return DataFileType.CnsDayFile;
			}

			// ETL hourly consensus files: sssyyddd.hhW
			match = regexEtlCnsHour.Match(fileName);
			if (match.Success) {
				ParseFileName(match);
				info = _parsedFileName;
				return DataFileType.EtlCnsHourFile;
			}

			// TOGA COARE uncompressed files: ssHyyddd.hhA
			match = regexToga.Match(fileName);
			if (match.Success) {
				ParseFileName(match);
				info = _parsedFileName;
				return DataFileType.TogaFile;
			}

			// raw DOMSAT msg file: xxxxxxxx.mmddyy.hhmm
			match = regexDomsatSingle.Match(fileName);
			if (match.Success) {
				ParseFileName(match);
				info = _parsedFileName;
				return DataFileType.DomsatSingleMsgFile;
			}

			// DOMSAT combined msg file: xxxxxxxx.yyyy_mm.TXT
			match = regexDomsatMultiple.Match(fileName);
			if (match.Success) {
				ParseFileName(match);
				info = _parsedFileName;
				return DataFileType.DomsatMultipleMsgFile;
			}

			match = regexDisdrom.Match(fileName);
			if (match.Success) {
				ParseFileName(match);
				info = _parsedFileName;
				return DataFileType.DisdrometerFile;
			}

			match = regexCeilom.Match(fileName);
			if (match.Success) {
				ParseFileName(match);
				info = _parsedFileName;
				return DataFileType.CeilometerFile;
			}

			match = regexThermom.Match(fileName);
			if (match.Success) {
				ParseFileName(match);
				info = _parsedFileName;
				return DataFileType.DigitalThermometerFile;
			}

			match = regexCampbell.Match(fileName);
			if (match.Success) {
				ParseFileName(match);
				info = _parsedFileName;
				return DataFileType.CampbellCr10File;
			}

			match = regexAsciiMom.Match(fileName);
			if (match.Success) {
				ParseFileName(match);
				info = _parsedFileName;
				return DataFileType.AsciiMomentsFile;
			}

			// Crossbow DMU file
			match = regexDMU.Match(fileName);
			if (match.Success) {
				ParseFileName(match);
				info = _parsedFileName;
				return DataFileType.CrossbowDMU;
			}

			// Crossbow DMU file
			match = regexDMU2.Match(fileName);
			if (match.Success) {
				ParseFileName(match);
				info = _parsedFileName;
				return DataFileType.CrossbowDMU;
			}

			// Radiosonde1 file
			match = regexSonde1.Match(fileName);
			if (match.Success) {
				ParseFileName(match);
				info = _parsedFileName;
				return DataFileType.Radiosonde1;
			}

			// Darwin Message Archive File
			match = regexDarwinMsgArchive.Match(fileName);
			if (match.Success) {
				ParseFileName(match);
				info = _parsedFileName;
				return DataFileType.DarwinMsgArchive;
			}

			info = _parsedFileName;
			return DataFileType.UnknownFile;
*/
		}

		/*
		private static void ParseFileName(Match match) {
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
			if (monthMatch != String.Empty ) {
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
				hour = (int)(halfHour/2.0);
				minute = 30 * (halfHour - hour*2);
			}
			else if (hourMatch != String.Empty) {
				hour = Int32.Parse(hourMatch);
			}
			else {
				hour = 0;
			}

			// pass site name and timestamp
			_parsedFileName.PopPrefix = popPrefix;
			_parsedFileName.Site = match.Groups["site"].Value;
			_parsedFileName.TimeStamp = DacDateTime.FromDayOfYear(year, dayOfYear, hour, minute, 0);
			if (dayOfYearEnd > 0) {
				// for now only compute EndTimeStamp if end day specified in file name
				_parsedFileName.EndTimeStamp = DacDateTime.FromDayOfYear(year, dayOfYearEnd, 0, 0, 0);
			}
		}
		*/

		/// <summary>
		/// Returns a chronologically sorted array of the full path names of
		/// all the files of a specific DataFileType in a specific folder.
		/// </summary>
        /// <param name="fileType"></param>
        /// <param name="site">The site name given in the file name</param>
        /// <param name="path">The folder to look in.  The name of a file in that folder is also accepted.</param>
		/// <returns></returns>
        /// <remarks>Revised 2013Dec10 (rev > 3.28) to only include files from one site</remarks>
		public static string[] GetAllFilesOfType(DataFileType fileType, string site, string path) {
			StringCollection fileCollection = new StringCollection();
			string folder = Path.GetDirectoryName(path);
			DirectoryInfo di = new DirectoryInfo(folder);
			FileInfo[] files = di.GetFiles();
			ParsedFileNameStruct info;
			foreach (FileInfo file in files) {
				DataFileType type = GetFileTypeFromName(file.Name, out info);
				if ((type == fileType) && (info.Site == site) && (info.PopPrefix.ToLower() != "h")) {
					// note: POP H files are not counted here $RevDate:1/14/2010 11:15:54 AM$
					fileCollection.Add(file.FullName);
				}
			}
			string[] filesOfType = new string[fileCollection.Count];
			fileCollection.CopyTo(filesOfType, 0);
			Array.Sort(filesOfType, new CompareByDate());
			return filesOfType;
		}

		public static string GetNextFileOfType(string currentFile, bool recycle) {
			string nextFile = currentFile;
			ParsedFileNameStruct info;
			DataFileType currentType = DacDataFileBase.GetFileTypeFromName(currentFile, out info);
            string currentSite = info.Site;
			string[] filesOfType = DacDataFileBase.GetAllFilesOfType(currentType, currentSite, currentFile);
            Array.Sort(filesOfType);
			int index = -1;
			for (int i = 0; i < filesOfType.Length; i++) {
				if (currentFile == filesOfType[i]) {
					index = i+1;
					if (index == filesOfType.Length) {
						// recycle back to zero at end of list
						if (recycle) {
							index = 0;
						}
						else {
							index = -1;
						}
					}
					break;
				}
			}
            if ((index >= 0) && (index < filesOfType.Length)) {
                nextFile = filesOfType[index];
            }
            else {
                nextFile = "";
            }
			return nextFile;
		}

        /// <summary>
        /// Returns file that represents next day of same file type
        /// </summary>
        /// <param name="currentFile"></param>
        /// <returns></returns>
        public static string GetNextDayFileOfType(string currentFile) {

            DateTime nextFileDate, currentFileDate;
            DateTime nextFileDay, currentFileDay;
            DateTime nextFileHour, currentFileHour;
            ParsedFileNameStruct info;
            GetFileTypeFromName(currentFile, out info);
            currentFileDate = info.TimeStamp;
            currentFileDay = new DateTime(currentFileDate.Year, currentFileDate.Month, currentFileDate.Day);
            currentFileHour = new DateTime(currentFileDate.Year, currentFileDate.Month, currentFileDate.Day, currentFileDate.Hour, 0, 0);

            string nextFile = "";
            do {
                nextFile = GetNextFileOfType(currentFile, recycle: false);
                if (!string.IsNullOrWhiteSpace(nextFile)) {
                    GetFileTypeFromName(nextFile, out info);
                    nextFileDate = info.TimeStamp;
                    if (info.FileType == DataFileType.LapxmPopHourFile ||
                        info.FileType == DataFileType.LapxmPopRawTSHourFile ||
                        info.FileType == DataFileType.LapxmPopTSHourFile ||
                        info.FileType == DataFileType.PopTSHourFile ||
                        info.FileType == DataFileType.PopRawTSHourFile ||
                        info.FileType == DataFileType.PopHourFile) {
                        // hourly files
                        nextFileHour = new DateTime(nextFileDate.Year, nextFileDate.Month, nextFileDate.Day, nextFileDate.Hour, 0, 0);
                        if (nextFileHour > currentFileHour) {
                            break;
                        }
                    }
                    else {
                        // day files
                        nextFileDay = new DateTime(nextFileDate.Year, nextFileDate.Month, nextFileDate.Day);
                        if (nextFileDay > currentFileDay) {
                            break;
                        }
                    }
                    currentFile = nextFile;
                }
            } while (!string.IsNullOrWhiteSpace(nextFile));

            return nextFile;

        }

		private class CompareByDate : IComparer {
			int IComparer.Compare(object x, object y) {
				string filex = x as string;
				string filey = y as string;
				if ((filex != null) && (filey != null)) {
					ParsedFileNameStruct infox, infoy;
					GetFileTypeFromName(filex, out infox);
					GetFileTypeFromName(filey, out infoy);
					return DateTime.Compare(infox.TimeStamp, infoy.TimeStamp);
				}
				else {
					throw new ArgumentException();
				}
			}
		}

		#endregion




	}  // end of class DacDataFileBase

}
