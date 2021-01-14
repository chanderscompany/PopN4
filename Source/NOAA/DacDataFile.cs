using System;
using System.IO;
using System.Net;
using System.Collections;
using DACarter.Utilities;

namespace DACarter.NOAA {

	public interface IDataFile {
		//bool ReadRecord();
		void Write();
		void Move(int nrec);
		void Move(TimeSpan span);
		void SetFileName(string fn);
	}

	public abstract class DataFileBase {
		protected enum OpenedFileType {
			NotOpened,
			OpenedTextReader,
			OpenedTextWriter,
			OpenedBinaryReader,
			OpenedBinaryWriter
		} 

		protected bool _fileIsOpen;
		protected string _fileName;
		protected BinaryReader _bReader;
		protected OpenedFileType _openedAsType;
		protected ArrayList _recordList;
		protected int _recordNumber;
		protected bool _haveReadEntireFile;
		protected bool _atEOF, _atBOF;

		public abstract int RecordNumber {get;}
		public abstract bool AtEOF {get;}
		public abstract bool AtBOF {get;}

		public DataFileBase() {	
			_haveReadEntireFile = false;
			_atBOF = true;
			_atEOF = false;
			_bReader = null;
			_fileName = "";
			_openedAsType = OpenedFileType.NotOpened;
			_fileIsOpen = false;
			_recordNumber = 0;		// number (starting with 1) of record that we are at the end of
			_recordList = new ArrayList(1024);  // position of beginning of each record
												// so, after reading 1 record, _recordNumber==1,
												// and we are at position _recordList[1].
		}

		protected virtual void openBinaryFileReader(string fileName) {
			if (_fileIsOpen && (_openedAsType!=OpenedFileType.OpenedBinaryReader)) {
				throw new IOException("DataFile: This object previously opened a file in a different mode");
			}
			if (_fileIsOpen && (_fileName != fileName)) {
				// trying to open a different file - OK
				// but must close old file first.
				_bReader.Close();
				_fileIsOpen = false;
				_openedAsType = OpenedFileType.NotOpened;
			}
			if (!_fileIsOpen) {
				_bReader = null;
				// these constructors throw exceptions if errors
				FileStream fs = new FileStream(fileName,FileMode.Open,FileAccess.Read);
				_bReader = new BinaryReader(fs);
				_fileIsOpen = true;
				_openedAsType = OpenedFileType.OpenedBinaryReader;
				_fileName = fileName;
				_recordList.Clear();
				_recordNumber = 0;
			}
			if (_bReader == null) {
				throw new IOException("DataFile: BinaryReader was not created");
			}
		}
	}

	/// <summary>
	/// This class provides functionality for reading and writing data for a COM file.
	/// </summary>
	public class ComFile :  DataFileBase, IDataFile {

		// fields
		//
		private Int16 npar, hdrNhts, hdrNrx;

		// methods
		//

		public virtual bool SkipRecord() {
			long beginPosition;	// to store position of beginning of record
			int nWords;

			if (!_fileIsOpen) {
				throw new InvalidOperationException("ComFile: File not opened before Read()");
			}

			if (_recordNumber > _recordList.Count) {
				// this record position should have been in list
				throw new InvalidOperationException("ComFile.ReadRecord: record number mismatch");
			}

			beginPosition = _bReader.BaseStream.Position;
			// check first byte of next word to see if it is 2- or 4-bytes
			int ch = _bReader.PeekChar();
			if (ch == -1) {
				// end of file
				checkBofEof();
				return false;
			}
			if (ch == 0xFF) {
				nWords = IPAddress.NetworkToHostOrder(_bReader.ReadInt32());
				nWords = nWords & 0x00ffffff;
			}
			else {
				nWords = IPAddress.NetworkToHostOrder(_bReader.ReadInt16());
			}

			if (nWords == 0) {
				checkBofEof();
				return false;
			}

			_bReader.BaseStream.Seek((long)(2*nWords),SeekOrigin.Current);
			
			// if we got this far, might be a good record. Add to list:
			_recordNumber++;
			if (_recordNumber == _recordList.Count+1) {
				// reading next new record, add position to list
				_recordList.Add(beginPosition);
			}
			else {
				// sanity check
				//long currentPosition = _bReader.BaseStream.Position;
				if ((long)_recordList[_recordNumber-1] != beginPosition) {
					throw new InvalidOperationException("ComFile.ReadRecord: record number mismatch on skip");
				}
			}

			checkBofEof();
			return true;
		}

		/// <summary>
		/// Reads next record of COM file.
		/// </summary>
		/// <returns></returns>
		public virtual bool ReadRecord(ComData data) {
			long beginPosition;	// to store position of beginning of record
			int nWords;

			if (!_fileIsOpen) {
				throw new InvalidOperationException("ComFile: File not opened before Read()");
			}

			if (_recordNumber > _recordList.Count) {
				// this record position should have been in list
				throw new InvalidOperationException("ComFile.ReadRecord: record number mismatch");
			}

			beginPosition = _bReader.BaseStream.Position;

			if (data == null) {
				throw new ArgumentNullException("ReadRecord argument is null");
			}
			lock(data) {
				// check first byte of next word to see if it is 2- or 4-bytes
				int ch = _bReader.PeekChar();
				if (ch == -1) {
					// end of file
					checkBofEof();
					return false;
				}
				if (ch == 0xFF) {
					nWords = IPAddress.NetworkToHostOrder(_bReader.ReadInt32());
					nWords = nWords & 0x00ffffff;
				}
				else {
					nWords = IPAddress.NetworkToHostOrder(_bReader.ReadInt16());
				}
				//Console.WriteLine("nWords = {0}",_nWords);

				if (nWords == 0) {
					checkBofEof();
					return false;
				}

				// npar
				npar = IPAddress.NetworkToHostOrder(_bReader.ReadInt16());
				//Console.WriteLine("npar = {0}",npar);
				data.SetHeaderSize(npar);
			
				// read and fill header array
				data.Hdr[0] = npar;
				for (int i = 1; i<npar; i++) {
					data.Hdr[i] = IPAddress.NetworkToHostOrder(_bReader.ReadInt16());
				}
				hdrNhts = data.Hdr[(int)ComData.HdrId.Nhts];
				hdrNrx = data.Hdr[(int)ComData.HdrId.Nrx];
				if (nWords != (npar + hdrNhts*(1+5*hdrNrx)) ) {
					throw new ApplicationException("COM file nWord count does not match with parameters");
				}

				data.SetDataSize(hdrNhts, hdrNrx);

				DateTime dt = DACarter.Utilities.DacDateTime.FromDayOfYear(
					data.Hdr[(int)ComData.HdrId.Year],
					data.Hdr[(int)ComData.HdrId.Doy],
					data.Hdr[(int)ComData.HdrId.Hour],
					data.Hdr[(int)ComData.HdrId.Minute],
					data.Hdr[(int)ComData.HdrId.Second]);

				// read data:
				//_bReader.ReadBytes(2*(_nWords-npar));
				for (int iht=0; iht<hdrNhts; iht++) {
					data.Ht[iht] = IPAddress.NetworkToHostOrder(_bReader.ReadInt16());
					int irx;
					for (irx=0; irx<hdrNrx; irx++) {
						data.Vel[irx,iht] = IPAddress.NetworkToHostOrder(_bReader.ReadInt16());
					}
					for (irx=0; irx<hdrNrx; irx++) {
						data.Snr[irx,iht] = IPAddress.NetworkToHostOrder(_bReader.ReadInt16());
					}
					for (irx=0; irx<hdrNrx; irx++) {
						data.Noise[irx,iht] = IPAddress.NetworkToHostOrder(_bReader.ReadInt32());
					}
					for (irx=0; irx<hdrNrx; irx++) {
						data.Width[irx,iht] = IPAddress.NetworkToHostOrder(_bReader.ReadInt16());
					}
				}

				// if we got this far, must be a good record. Add to list:
				_recordNumber++;
				if (_recordNumber == _recordList.Count+1) {
					// reading next new record, add position to list
					_recordList.Add(beginPosition);
				}
				else {
					// sanity check
					//long currentPosition = _bReader.BaseStream.Position;
					if ((long)_recordList[_recordNumber-1] != beginPosition) {
						throw new InvalidOperationException("ComFile.ReadRecord: record number mismatch on reread");
					}
				}

				checkBofEof();
			}

			return true;
		}

		protected void checkBofEof() {
			_atBOF = false;
			_atEOF = false;
			if (_bReader.BaseStream.Length == _bReader.BaseStream.Position) {
				_haveReadEntireFile = true;
				_atEOF = true;
			}
			else if (_recordNumber == 1) {
				_atBOF = true;
			}
			else if (_haveReadEntireFile) {
				if (_recordNumber == _recordList.Count) {
					_atEOF = true;
				}
			}
		}
/*
		public ComData GetDataCopy() {
			ComData data = new ComData();
			data.SetSize(npar,hdrNhts,hdrNrx);
			data.Hdr.CopyTo(data.Hdr,0);
			data.Ht.CopyTo(data.Ht,0);

			for (int iht=0; iht<hdrNhts; iht++) {
				int irx;
				for (irx=0; irx<hdrNrx; irx++) {
					data.Vel[irx,iht] = data.Vel[irx,iht];
				}
				for (irx=0; irx<hdrNrx; irx++) {
					data.Snr[irx,iht] = data.Snr[irx,iht];
				}
				for (irx=0; irx<hdrNrx; irx++) {
					data.Noise[irx,iht] = data.Noise[irx,iht];
				}
				for (irx=0; irx<hdrNrx; irx++) {
					data.Width[irx,iht] = data.Width[irx,iht];
				}
			}
			return data;
		}
*/
		/// <summary>
		/// Writes a record to the COM file.
		/// </summary>
		public virtual void Write() {}

		/// <summary>
		/// Move to the beginning of record number recNum.
		/// </summary>
		/// <param name="recNum">Record number (first record = 1)</param>
		public void MoveToRecord(int recNum) {
			if ((recNum < 1) || (recNum > _recordList.Count)) {
				throw new ArgumentException("ComFile: Invalid record number in MoveToRecord()");
			}
			_bReader.BaseStream.Seek((long)_recordList[recNum-1],SeekOrigin.Begin);
			_recordNumber = recNum-1;
			// next record read will be _recordNumber == recNum
		}

		/// <summary>
		/// Overloaded function to move to a new record in the COM file
		///   without reading that record.
		/// </summary>
		/// <param name="nrec">Number of records to move.</param>
		/// <remarks>
		/// nrec is number of records to move from current file position.  A value of 0
		///   will not move the file position and the next read command will read
		///   the next record.
		/// </remarks>
		public void Move(int nrec) {
			if (!_fileIsOpen) {
				throw new InvalidOperationException("ComFile: File not opened before Move()");
			}
			if (nrec < 0) {
				MoveBackRecord(-nrec);
			}
			else if (nrec > 0) {
				MoveForwardRecord(nrec);
			}
		}

		/// <summary>
		/// Overloaded function to move to a new record in the COM file.
		/// </summary>
		/// <param name="span">TimeSpan to move relative to current position.</param>
		public void Move(TimeSpan span) {
			if (!_fileIsOpen) {
				throw new InvalidOperationException("ComFile: File not opened before Move()");
			}
		}

		/// <summary>
		/// Opens a COM file.
		/// </summary>
		/// <param name="fn">Name of the COM file.</param>
		public virtual void SetFileName(string fn) {
			openBinaryFileReader(fn);
			// read header at beginning of file
			if (_bReader.BaseStream.Position == 0) {
				short[] header = new Int16[4];
				for (int i = 0; i<4; i++) {
					header[i] = IPAddress.NetworkToHostOrder(_bReader.ReadInt16());
					Console.WriteLine("header = {0}",header[i]);
				}
				if ( (header[0] != (short)3) || (header[1] != (short)32001) ) {
					throw new ApplicationException("Improper COM header");
				}
			}
		}

		private void MoveBackRecord(int nrec) {
			// move back over nrec records
			// We are at end of  record # _recordNumber
			// We want to move to beginning of record (nrec-1) before this,
			//   which is the end of the record (nrec) before this.
			// Set _recordNumber to record# we will be at the end of;
			// Seek to the beginning of (_recordNumber+1) whose index is (_recordNumber)
			_recordNumber -= nrec;
			if (_recordNumber < 0)
				_recordNumber = 0;
			_bReader.BaseStream.Seek((long)_recordList[_recordNumber],SeekOrigin.Begin);
		}

		private void MoveForwardRecord(int nrec) {
			bool readOK;
			for (int i=0; i<(nrec); i++) {
				//ComData data = new ComData();
				//readOK = ReadRecord(data);
				readOK = SkipRecord();
				if (!readOK) {
					break;
				}
			}
		}

		public void GoToBOF() {
			_recordNumber = 0;
			_bReader.BaseStream.Seek((long)_recordList[0],SeekOrigin.Begin);
		}

		public void GoToEOF() {
			MoveForwardRecord(int.MaxValue);
		}

		public override int RecordNumber {
			get {
				return _recordNumber;
			}
		}

		public override bool AtBOF {
			get {
				return _atBOF;
			}
		}

		public override bool AtEOF {
			get {
				return _atEOF;
			}
		}

		//private Int16[] _irec = new Int16[64];
		//public ComData _data = new ComData(); 
	}


	public class PopFile : IDataFile {
		public bool ReadRecord(ComData data) {
			throw new InvalidOperationException("PopFile.ReadRecord: Not Implemented");
			//return false;
		}
		public void Write() {}
		public void Move(int nrec) {}
		public void Move(TimeSpan span) {}
		public void SetFileName(string fn) {}
	}

	public class TogaFile : IDataFile {
		public bool ReadRecord() {return false;}
		public void Write() {}
		public void Move(int nrec) {}
		public void Move(TimeSpan span) {}
		public void SetFileName(string fn) {}
	}

	public class DomsatFile : IDataFile {
		public bool ReadRecord() {return false;}
		public void Write() {}
		public void Move(int nrec) {}
		public void Move(TimeSpan span) {}
		public void SetFileName(string fn) {}
	}

	public class PdmFile : IDataFile {
		public bool ReadRecord() {return false;}
		public void Write() {}
		public void Move(int nrec) {}
		public void Move(TimeSpan span) {}
		public void SetFileName(string fn) {}
	}
}

