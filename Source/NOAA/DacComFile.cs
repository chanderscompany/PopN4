using System;
using System.Net;

using DACarter.Utilities;

namespace DACarter.NOAA
{
	/// <summary>
	/// Summary description for DacComFile.
	/// </summary>
	public class DacComFile : DacDataFileBase
	{
		public DacComFile()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		protected override long CustomReadFileHeader() {
			short[] header = new Int16[4];
			for (int i = 0; i<4; i++) {
				header[i] = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());
				Console.WriteLine("header = {0}",header[i]);
			}
			if ( (header[0] != (short)3) || (header[1] != (short)32001) ) {
				throw new ApplicationException("Improper COM header");
			}
			return 8;
		}

		protected override bool CustomReadRecord(DacData data, out DateTime timeStamp, out long recordLength, out long filePositionChange) {
			int nWords;
			int ch = 0;
			int clockUnits;

			timeStamp = DateTime.MinValue;
			PopData popData = data as PopData;

			try {
				// check first byte of next word to see if it is 2- or 4-bytes
				ch = _BReader.PeekChar();
				if (ch == -1) {
					// end of file
					checkBofEof();
					timeStamp = DateTime.MinValue;
					recordLength = filePositionChange = 0;
					return false;
				}
				if (ch == 0xFF) {
					nWords = IPAddress.NetworkToHostOrder(_BReader.ReadInt32());
					nWords = nWords & 0x00ffffff;
				}
				else {
					nWords = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());
				}

				if (nWords == 0) {
					checkBofEof();
					timeStamp = DateTime.MinValue;
					recordLength = filePositionChange = 0;
					return false;
				}

				// read irec[0]
				int npar = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());
				if (npar < 64) {
					// no versions of COM file header should have less than 64 items
					throw new Exception("COM file header has less than 64 items.");
				}

				int pwClocks = 0;			// irec[9]
				int delayClocks = 0;		// irec[10]
				int spacingClocks = 0;		// irec[11]

				// read next 15 header items
				if (popData == null) {
					// skip next 15
					for (int i = 0; i < 15; i++) {
						_BReader.ReadInt16();
					}
				}
				else {
					_BReader.ReadInt16();	// irec[1] = "original size"
					popData.Hdr.NHts = (int)IPAddress.NetworkToHostOrder(_BReader.ReadInt16());  // irec[2]
					popData.Hdr.NSets = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());		// irec[3]
					popData.Hdr.NRx = 1;
					popData.Hdr.NPts = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());		// irec[4]
					popData.Hdr.NSpec = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());		// irec[5]
					_BReader.ReadInt16();														// irec[6]		
					popData.Hdr.NCI = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());		// irec[7]
					popData.Hdr.IPP = 1000 * IPAddress.NetworkToHostOrder(_BReader.ReadInt16());	// irec[8]
					pwClocks = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());			// irec[9]
					delayClocks = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());		// irec[10]
					spacingClocks = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());		// irec[11]
					_BReader.ReadInt16();														// irec[12]
					_BReader.ReadInt16();														// irec[13]
					_BReader.ReadInt16();														// irec[14]
					_BReader.ReadInt16();														// irec[15]
				}

				// read time stamp
				int year = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());			// irec[16]
				int day = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());
				int hour = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());
				int minute = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());
				int second = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());		// irec[20]

				timeStamp = DacDateTime.FromDayOfYear(year, day, hour, minute, second);
				if (data != null) {
					// save base class members
					data.Notes = npar.ToString();
					data.TimeStamp = timeStamp;
				}

				// OK, so far we have read 21 items of header.
				// There is a total of npar header items.

				if (popData == null) {
					// not saving any more of this record, so skip to end
					for (int i = 0; i < nWords - 21; i++) {
						_BReader.ReadInt16();
					}
				}
				else {
					// read rest of header
					_BReader.ReadInt16();														// irec[21]
					_BReader.ReadInt16();														// irec[22]
					_BReader.ReadInt16();														// irec[23]
					int momentFlag = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());		// irec[24] 2==yes
					_BReader.ReadInt16();														// irec[25]
					_BReader.ReadInt16();														// irec[26]
					_BReader.ReadInt16();														// irec[27]
					_BReader.ReadInt16();														// irec[28]
					popData.Hdr.Azimuth = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());	// irec[29]
					double freq = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());			// irec[30] units?
					popData.Hdr.TxFreq = freq * 1.0e5;
					_BReader.ReadInt16();														// irec[31]
					popData.Hdr.Altitude = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());	// irec[32] units?
					popData.Hdr.NCode = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());		// irec[33] units?
					popData.Hdr.Elevation = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());	// irec[34] 
					if (popData.Hdr.Elevation > 90.0) {
						popData.Hdr.Elevation /= 100.0;
					}
					_BReader.ReadInt16();														// irec[35]
					int ICRA = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());				// irec[36] 
					popData.Hdr.SysDelay = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());	// irec[37] units?
					popData.Hdr.MinutesToUT = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());	// irec[38]
					int dop0 = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());														// irec[39] dop0
					int dop1 = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());														// irec[40]
					int dop2 = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());														// irec[41]
					int dop3 = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());														// irec[42] dop3
																								//
					for (int i = 0; i < 15; i++) {
						_BReader.ReadInt16();
					}
					clockUnits = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());			// irec[58] nsec
					_BReader.ReadInt16();														// irec[59]
					_BReader.ReadInt16();														// irec[60]
					_BReader.ReadInt16();														// irec[61]
					_BReader.ReadInt16();														// irec[62]
					popData.Hdr.RadarID = IPAddress.NetworkToHostOrder(_BReader.ReadInt16());	// irec[63]

					// read any header fields beyond the first 64
					// They may include:
					//	MetFlag at irec[64],	// if met data follow, this is 16 bit flag to indicate which instruments present
					//	ShipLat at irec[83]
					//  ShipLong,
					//	ShipSpeed,
					//	ShipHeading,
					//	ShipCompass
					//
					for (int i = 0; i < npar - 64; i++) {
						_BReader.ReadInt16();
					}

					popData.Hdr.PW = pwClocks * clockUnits;			
					popData.Hdr.Delay = delayClocks * clockUnits;		
					popData.Hdr.Spacing = spacingClocks * clockUnits;
		
					// set PopData header items that do not exist in COM header
					popData.Hdr.DirName = "Az = " + popData.Hdr.Azimuth.ToString("f0") + " El = " + popData.Hdr.Elevation.ToString("f2");

					// Done with Header
					// Set data dimensions 
					// and read data

					popData.Hdr.HasWindMoments = true;
					popData.Hdr.HasRassMoments = false;
					popData.SetSize(1, popData.Hdr.NRx, popData.Hdr.NHts, 0);

					for (int iht = 0; iht < popData.Hdr.NHts; iht++) {
						popData.Hts[iht] = IPAddress.NetworkToHostOrder(_BReader.ReadInt16()) / 100.0f;		// read decameters
						for (int irx = 0; irx < popData.Hdr.NRx; irx++) {
							popData.Vel[0, irx, iht] = IPAddress.NetworkToHostOrder(_BReader.ReadInt16()) / 100.0f;	// read cm/sec
							popData.Snr[0, irx, iht] = IPAddress.NetworkToHostOrder(_BReader.ReadInt16()) / 100.0f;	// read milliBells
							popData.Noise[0, irx, iht] = IPAddress.NetworkToHostOrder(_BReader.ReadInt32());		//
							popData.Width[0, irx, iht] = IPAddress.NetworkToHostOrder(_BReader.ReadInt16()) / 100.0f;	// read cm/sec
						}
					}
				}  // end of else popData!=null

				recordLength = 2 * (nWords + 1);
				filePositionChange = recordLength;


			}
			catch (Exception) {
				// corrupted record
				throw;
			} 
			
			return true;
		}

		/*
		// ReadNextRecord from previous version of ComFile class
	 
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


		*/



		protected override bool CustomSkipRecord(out long currentPosition) {
			throw new NotImplementedException();
		}

		protected override bool CustomReadRecordTime(out DateTime timeStamp, out long recSize, out long filePositionChange) {
			PopData data = null;
			return CustomReadRecord(data, out timeStamp, out recSize, out filePositionChange);
		}

		protected override bool CustomOpenInit() {
			return true;
		}

		protected override OpenFileType GetOpenFileType() {
			return OpenFileType.BinaryReader;
		}

	}
}
