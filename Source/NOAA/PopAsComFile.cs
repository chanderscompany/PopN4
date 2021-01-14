using System;
using System.IO;
using System.Net;
using System.Collections;
using System.Runtime.InteropServices;
using DACarter.Utilities;

namespace DACarter.NOAA { 
	/// <summary>
	/// Summary description for PopAsComFile.
	/// </summary>
	public class PopAsComFile : ComFile {

		//private int _numBytesPreamble, _numBytesMoments, _numBytesInstruments;
		//private int _numBytesSpectra, _numBytesTimeSeries;
		protected DataPreambleStructure _dataPreamble;
		/*protected*/
		

		private HeaderStruct _headerStruct;
		protected DateTime _recordTime;
		protected long _lastGoodHdr;
		protected long _lastDataPos;
		protected long _lastHdrPos;
		protected bool _HeaderFileIsOpen;
		protected string _headerFileName;

		protected BinaryReader _bHdrReader;

		// **************************************
		#region POP file data structures

		//
		// we have to jump through some hoops here
		//	to tell .NET what the formats are of the structures
		//	that are coming in from the POP file
		//	by using [StructLayout] and [MarshalAs] attributes.
		//
		// Also have not figured out how to get .NET
		//	to accept arrays of structs inside a struct.
		//	So had to itemize elements of struct arrays.
		//

		const int MAXRAD = 1;        // max number of radars 
		const int MAXBMPAR = 4;      // max number of beam parameter sets 
		const int MAXBM = 10;        // max number of beams 
		const int MAXDIR = 9;        // number of possible beam directions 
		const int MAXBW = 4;         // max number of bandwidths on Rx
		const int NMISC = 22;       // number of shorts in misc[] array 


		[ StructLayout( LayoutKind.Sequential, CharSet=CharSet.Ansi, Pack=2 )]
		protected struct DataPreambleStructure {
			public Int16 datatype;
			public UInt32 wnbytes;
			public Int16 spct;
			public UInt32 hdrloc;
			public UInt32 DataTime;
			public Int16 C_rad;
			public Int16 C_bm;
			public Int16 NCI;
			public Int16 NSPEC;
		}

		[ StructLayout( LayoutKind.Sequential, CharSet=CharSet.Ansi, Pack=2 )]
		public struct BeamParStruct {
			public Int32	ipp;		
			public Int32	pw;		
			public Int32	delay;
			public Int32	space;
			public Int16	nhts;
			public Int16	nci;
			public Int16	nspec;
			public Int16	npts;
			public Int16	sysdelay;  // delay thru rx in nanosec 
			public Int16	bwcode;    // rx bandwidth switch code 
			public Int16	atten;     // # range gates to attenuate 
			public Int16	ncode;     // ncode = # bits in pulse code 
		};

		[ StructLayout( LayoutKind.Sequential, CharSet=CharSet.Ansi, Pack=2 )]
		public struct BeamStruct {
			public Int16 idir;      // direction index, dir_[idir], 0 to (NUMDIR-1)
			public Int16 ipar;      // parameter set index, 0 to (NUMPAR-1) /
			public Int16 nrep;      // number of repetitions (records) at this position 
		};

		[ StructLayout( LayoutKind.Sequential, CharSet=CharSet.Ansi, Pack=2 )]
		public struct PbConstStruct {
			public Int32 PBPRETR;     // all times in nanosec 
			public Int32 PBPOSTTR;
			public Int32 PBSYNCH;
			public Int32 PBPREBLNK;
			public Int32 PBPOSTBLNK;
		};

		[ StructLayout( LayoutKind.Sequential, CharSet=CharSet.Ansi, Pack=2 )]
		public struct DirStruct {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=11)]
			public char[] label;		// 10-character direction label name 
			public Int16 az;				// beam azimuth
			public Int16 elev;			//elevation in degrees 
			public Int16 dircode;			// beam direction code for beam steering 
		};

		[ StructLayout( LayoutKind.Sequential, CharSet=CharSet.Ansi, Pack=2 )]
		public struct ProcParStruct {           
			public Int16 DCfil;
			public Int16 Window;
			public Int16 DComit;					// # pts omitted around dc (obsolete) 
			public Int16 Omithts;					// # hts to apply dcomit 
			public Int16 Dop0;
			public Int16 Dop1;					// start and interval pt # for moments (1-NPTS) 
			public Int16 Dop2;
			public Int16 Dop3;					// start and interval pt # for second moments 
			public Int16 lRassOn;					// parameters for rass acoustic source (see below)  
			public Int16 lRassLowFrequencyHz;		// parameters for rass acoustic source (see below) 
			public Int16 lRassHighFrequencyHz;	// parameters for rass acoustic source (see below)   
			public Int16 lRassStepHz;				// parameters for rass acoustic source (see below)   
			public Int16 lRassDwellMs;			// parameters for rass acoustic source (see below) 
			public Int16 lRassSweep;				// parameters for rass acoustic source (see below)  
			public Int16 Cltrht;					// max ht for clutter removal (km*10);
			public Int16 Specavg;					// <1 for MEAN, ==1 for H&S spectral averaging 
			public Int16 Nrx;						// # multiplexed interferometer receivers 
			public Int16 sp_nmet;					// # met instruments -- 941004 DAC
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=NMISC)]
			public Int16[] misc;				// space saver for future use, keep total = 80b 
		};

		[ StructLayout( LayoutKind.Sequential, CharSet=CharSet.Ansi, Pack=2 )]
		public struct Junk {
			RadarIDStruct bm1;
			RadarIDStruct bm2;
		}


		//BeamParStruct[] par = new BeamParStruct[4];

		[ StructLayout( LayoutKind.Sequential, CharSet=CharSet.Ansi, Pack=2 )]
		public struct RadarIDStruct {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=31)]
			public char[]	name;			// name of radar 
			public Int16	code;			// code number for radar 
			public Int32	freq;				// tx freq in Mhz*100 
			public float	maxduty;			// max duty cycle 
			public Int16	maxtx;				// max Tx pulse length (usec) 
			public Int16	txon;				// tx pulse on (1) or off (0) 
			public Int16	numdir;				// number of allowable directions 
			public Int16	numbm;				// number of beam positions chosen 
			public Int16	numpar;				// number of beam parameter sets chosen 
			//[MarshalAs(UnmanagedType.ByValArray, SizeConst=MAXBMPAR)]
			//public BeamParStruct[] par;	// array of beam postion parameter sets 
			public BeamParStruct par1;	
			public BeamParStruct par2;	
			public BeamParStruct par3;	
			public BeamParStruct par4;	
			//[MarshalAs(UnmanagedType.ByValArray, SizeConst=MAXBM)]
			//public BeamStruct[] bm;			// array of chosen beam positions 
			public BeamStruct bm1;
			public BeamStruct bm2;
			public BeamStruct bm3;
			public BeamStruct bm4;
			public BeamStruct bm5;
			public BeamStruct bm6;
			public BeamStruct bm7;
			public BeamStruct bm8;
			public BeamStruct bm9;
			public BeamStruct bm10;
			public PbConstStruct	pb;									// pulse box constants for this radar 
			//[MarshalAs(UnmanagedType.ByValArray, SizeConst=MAXDIR)]
			//public DirStruct[]	dir;			// array of allowable directions 
			public DirStruct dir1; 
			public DirStruct dir2; 
			public DirStruct dir3; 
			public DirStruct dir4; 
			public DirStruct dir5; 
			public DirStruct dir6; 
			public DirStruct dir7; 
			public DirStruct dir8; 
			public DirStruct dir9; 
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=(2*MAXBW))]
			public short[] rxbw;				// matched pulsewidths (nsec) for rx bandwidth 
			public ProcParStruct proc;
      
		};


		[ StructLayout( LayoutKind.Sequential, CharSet=CharSet.Ansi, Pack=2 )]
		public struct PopParStruct {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=31)]
			public char[] name;					// station name 
			public Int16 lat;						// N latitude,	
			public Int16 lng;						// E longitude, deg*100 
			public Int16 mintoUT;					// # minutes add to sys time to get UT
			public Int16 alt;						// altitude above sea level, meters 
			public Int16 numrad;					// number of radars at this station 
			public RadarIDStruct radar;
		};

			
		[ StructLayout( LayoutKind.Sequential, CharSet=CharSet.Ansi, Pack=2 )]
		public struct HeaderStruct {
			public UInt16 hdrtype;
			public UInt16 hdrsize;
			public UInt16 nmet;
			public Int16 MaxNumRadars;			// max number of radars
			public Int16 MaxNumBeamsParams;	// max number of beam parameter sets
			public Int16 MaxNumBeams;			// max number of beams
			public Int16 MaxNumDirections;		// number of possible beam directions
			public Int16 MaxNumBandwidths;		// max number of bandwidths on Rx
			public PopParStruct poppar;
			public UInt32 dataloc;
		};

		#endregion
		// **************************************


		/// <summary>
		/// default constructor
		/// </summary>
		public PopAsComFile() {
			_bHdrReader = null;
			_lastGoodHdr = -1;
			_lastDataPos = 0;
			_lastHdrPos = 0;
			_HeaderFileIsOpen = false;
			_headerFileName = "NoName";
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public override bool ReadRecord(ComData data) {
			//base.ReadRecord(data);

			long beginPosition;	// to store position of beginning of record
			//int numEndBytes;
			//int bytesRead;

			if (data == null) {
				throw new ArgumentNullException("PopAsComFile: ReadRecord argument is null");
			}

			if (!_fileIsOpen) {
				throw new InvalidOperationException("PopAsComFile: File not opened before Read()");
			}

			if (_recordNumber > _recordList.Count) {
				// this record position should have been in list
				throw new InvalidOperationException("PopAsComFile.ReadRecord: record number mismatch");
			}

			//_numBytesPreamble = _numBytesMoments = _numBytesInstruments = 0;
			//_numBytesSpectra = _numBytesTimeSeries = 0;

			_lastDataPos = _bReader.BaseStream.Position;
			beginPosition = _bReader.BaseStream.Position;

			lock(data) {

				bool dataIsThere = ReadPreamble();
				if (!dataIsThere) {
					return false;
				}

				if ( !(IsValidDataType(_dataPreamble.datatype) ) ) {
					throw new ArgumentException("PopAsComFile.ReadRecord: Invalid Data Type");
				}

				// decode the time
				//   Pop file time is number of seconds since Jan. 1, 1970
				DateTime epoch0 = new DateTime(1970,1,1);
				TimeSpan time_t = new TimeSpan(_dataPreamble.DataTime * 10000000L); // 100 nsec ticks
				_recordTime = epoch0 + time_t;
				//string sTime = recTime.ToString();

				// do we need to read a header record?
				if (_dataPreamble.hdrloc != _lastGoodHdr) {
					// yes

					if (_dataPreamble.hdrloc != _lastHdrPos) {
						// but we are not at the right location for this header.
						// Suspect that header pointer in data record is bad
						// but let's read header ID anyway
					}

					
					_bHdrReader.BaseStream.Seek(_dataPreamble.hdrloc, System.IO.SeekOrigin.Begin);
					_lastHdrPos = _bHdrReader.BaseStream.Position;

					// now see if we are at correct position and still need to read a header
					if ((_dataPreamble.hdrloc == _lastHdrPos) && (_dataPreamble.hdrloc != _lastGoodHdr)) {
						// OK, read a header record
						ReadAndValidateHdrStruct();	
						_lastGoodHdr = _lastHdrPos;
					}

				}  // end of need to read new header

				// fill COM header data
				Int16 clock = 100;

				int numMomentSets;
				if (_headerStruct.poppar.radar.proc.Dop3 > 0) {
					numMomentSets = 2;
				}
				else {
					numMomentSets = 1;
				}
				int nrx = _headerStruct.poppar.radar.proc.Nrx;
				int dop0 = _headerStruct.poppar.radar.proc.Dop0;
				int dop1 = _headerStruct.poppar.radar.proc.Dop1;
				int dop2 = _headerStruct.poppar.radar.proc.Dop2;
				int dop3 = _headerStruct.poppar.radar.proc.Dop3;

				int cbm = _dataPreamble.C_bm;
				int cpar = 0;
				int cdir = 0;
				switch(cbm) {
					case 0:
						cpar = _headerStruct.poppar.radar.bm1.ipar;
						cdir = _headerStruct.poppar.radar.bm1.idir;
						break;
					case 1:
						cpar = _headerStruct.poppar.radar.bm2.ipar;
						cdir = _headerStruct.poppar.radar.bm2.idir;
						break;
					case 2:
						cpar = _headerStruct.poppar.radar.bm3.ipar;
						cdir = _headerStruct.poppar.radar.bm3.idir;
						break;
					case 3:
						cpar = _headerStruct.poppar.radar.bm4.ipar;
						cdir = _headerStruct.poppar.radar.bm4.idir;
						break;
					case 4:
						cpar = _headerStruct.poppar.radar.bm5.ipar;
						cdir = _headerStruct.poppar.radar.bm5.idir;
						break;
					case 5:
						cpar = _headerStruct.poppar.radar.bm6.ipar;
						cdir = _headerStruct.poppar.radar.bm6.idir;
						break;
					case 6:
						cpar = _headerStruct.poppar.radar.bm7.ipar;
						cdir = _headerStruct.poppar.radar.bm7.idir;
						break;
					case 7:
						cpar = _headerStruct.poppar.radar.bm8.ipar;
						cdir = _headerStruct.poppar.radar.bm8.idir;
						break;
					case 8:
						cpar = _headerStruct.poppar.radar.bm9.ipar;
						cdir = _headerStruct.poppar.radar.bm9.idir;
						break;
					case 9:
						cpar = _headerStruct.poppar.radar.bm10.ipar;
						cdir = _headerStruct.poppar.radar.bm10.idir;
						break;
					default:
						cpar = 0;
						cdir = 0;
						break;
				}

				int nhts=0, npts=0, nspec=0, nci=0;
				int ipp=0, pw=0, delay=0, spacing=0, sysdelay=0;
				int ncode=0;
				switch(cpar) {
					case 0:
						nhts = _headerStruct.poppar.radar.par1.nhts;
						npts = _headerStruct.poppar.radar.par1.npts;
						nspec = _headerStruct.poppar.radar.par1.nspec;
						nci = _headerStruct.poppar.radar.par1.nci;
						ipp = _headerStruct.poppar.radar.par1.ipp;
						pw = _headerStruct.poppar.radar.par1.pw;
						delay = _headerStruct.poppar.radar.par1.delay;
						spacing = _headerStruct.poppar.radar.par1.space;
						sysdelay = _headerStruct.poppar.radar.par1.sysdelay;
						ncode = _headerStruct.poppar.radar.par1.ncode;
						break;
					case 1:
						nhts = _headerStruct.poppar.radar.par2.nhts;
						npts = _headerStruct.poppar.radar.par2.npts;
						nspec = _headerStruct.poppar.radar.par2.nspec;
						nci = _headerStruct.poppar.radar.par2.nci;
						ipp = _headerStruct.poppar.radar.par2.ipp;
						pw = _headerStruct.poppar.radar.par2.pw;
						delay = _headerStruct.poppar.radar.par2.delay;
						spacing = _headerStruct.poppar.radar.par2.space;
						sysdelay = _headerStruct.poppar.radar.par2.sysdelay;
						ncode = _headerStruct.poppar.radar.par2.ncode;
						break;
					case 2:
						nhts = _headerStruct.poppar.radar.par3.nhts;
						npts = _headerStruct.poppar.radar.par3.npts;
						nspec = _headerStruct.poppar.radar.par3.nspec;
						nci = _headerStruct.poppar.radar.par3.nci;
						ipp = _headerStruct.poppar.radar.par3.ipp;
						pw = _headerStruct.poppar.radar.par3.pw;
						delay = _headerStruct.poppar.radar.par3.delay;
						spacing = _headerStruct.poppar.radar.par3.space;
						sysdelay = _headerStruct.poppar.radar.par3.sysdelay;
						ncode = _headerStruct.poppar.radar.par3.ncode;
						break;
					case 3:
						nhts = _headerStruct.poppar.radar.par4.nhts;
						npts = _headerStruct.poppar.radar.par4.npts;
						nspec = _headerStruct.poppar.radar.par4.nspec;
						nci = _headerStruct.poppar.radar.par4.nci;
						ipp = _headerStruct.poppar.radar.par4.ipp;
						pw = _headerStruct.poppar.radar.par4.pw;
						delay = _headerStruct.poppar.radar.par4.delay;
						spacing = _headerStruct.poppar.radar.par4.space;
						sysdelay = _headerStruct.poppar.radar.par4.sysdelay;
						ncode = _headerStruct.poppar.radar.par4.ncode;
						break;
					default:
						break;
				}

				int az, elev;
				switch(cdir) {
					case 0: 
						az = _headerStruct.poppar.radar.dir1.az;
						elev = _headerStruct.poppar.radar.dir1.elev;
						break;
					case 1: 
						az = _headerStruct.poppar.radar.dir2.az;
						elev = _headerStruct.poppar.radar.dir2.elev;
						break;
					case 2: 
						az = _headerStruct.poppar.radar.dir3.az;
						elev = _headerStruct.poppar.radar.dir3.elev;
						break;
					case 3: 
						az = _headerStruct.poppar.radar.dir4.az;
						elev = _headerStruct.poppar.radar.dir4.elev;
						break;
					case 4: 
						az = _headerStruct.poppar.radar.dir5.az;
						elev = _headerStruct.poppar.radar.dir5.elev;
						break;
					case 5: 
						az = _headerStruct.poppar.radar.dir6.az;
						elev = _headerStruct.poppar.radar.dir6.elev;
						break;
					case 6: 
						az = _headerStruct.poppar.radar.dir7.az;
						elev = _headerStruct.poppar.radar.dir7.elev;
						break;
					case 7: 
						az = _headerStruct.poppar.radar.dir8.az;
						elev = _headerStruct.poppar.radar.dir8.elev;
						break;
					case 8: 
						az = _headerStruct.poppar.radar.dir9.az;
						elev = _headerStruct.poppar.radar.dir9.elev;
						break;
					default:
						az=-1;
						elev=-1;
						break;
				}

				int icra = _headerStruct.poppar.radar.proc.Specavg;
				if (icra < 0) {
					icra = 0;
				}

				
				/*
				int dop0, dop1, dop2, dop3;
				dop0 = _headerStruct.poppar.radar.proc.Dop0;
				dop1 = _headerStruct.poppar.radar.proc.Dop1;
				dop2 = _headerStruct.poppar.radar.proc.Dop2;
				dop3 = _headerStruct.poppar.radar.proc.Dop3;
				*/

				int npar = 64;	// for non-met data?
				data.SetHeaderSize(npar);
				data.SetDataSize(nhts, numMomentSets);
				
				data.Hdr[(int)ComData.HdrId.Npar] = (short)npar;	
				data.Hdr[(int)ComData.HdrId.OrigSize]= 0 ;
				data.Hdr[(int)ComData.HdrId.Nhts] = (short)nhts;
				data.Hdr[(int)ComData.HdrId.Nrx] = (short)numMomentSets;	// NOT REALLY NRX - is 2 for RASS
				data.Hdr[(int)ComData.HdrId.Npts] = (short)npts;
				//data.Hdr[(int)ComData.HdrId.Nspec] = (short)nspec;
				data.Hdr[(int)ComData.HdrId.Nspec] = (short)_dataPreamble.NSPEC;
				//data.Hdr[(int)ComData.HdrId.Nci] = (short)nci;
				data.Hdr[(int)ComData.HdrId.Nci] = (short)_dataPreamble.NCI;
				data.Hdr[(int)ComData.HdrId.Ippus] = (short)(ipp/1000);
				data.Hdr[(int)ComData.HdrId.Pwclk] = (short)(pw/clock);
				data.Hdr[(int)ComData.HdrId.Delayclk] = (short)(delay/clock);
				data.Hdr[(int)ComData.HdrId.Spacingclk] = (short)(spacing/clock);
				data.Hdr[(int)ComData.HdrId.Nsam] = (short)nhts;
				data.Hdr[(int)ComData.HdrId.Year] = (short)_recordTime.Year;
				data.Hdr[(int)ComData.HdrId.Doy] = (short)_recordTime.DayOfYear;
				data.Hdr[(int)ComData.HdrId.Hour] = (short)_recordTime.Hour;
				data.Hdr[(int)ComData.HdrId.Minute] = (short)_recordTime.Minute;
				data.Hdr[(int)ComData.HdrId.Second] = (short)_recordTime.Second;
				data.Hdr[(int)ComData.HdrId.Az] = (short)az;
				data.Hdr[(int)ComData.HdrId.Freq] = (short)(_headerStruct.poppar.radar.freq/10.0);
				data.Hdr[(int)ComData.HdrId.Alt] = (short)_headerStruct.poppar.alt;
				data.Hdr[(int)ComData.HdrId.PCode] = (short)ncode;
				data.Hdr[(int)ComData.HdrId.Elev] = (short)elev;
				data.Hdr[(int)ComData.HdrId.Icra] = (short)icra;
				data.Hdr[(int)ComData.HdrId.SysDly] = (short)sysdelay;
				data.Hdr[(int)ComData.HdrId.UTmin] = (short)_headerStruct.poppar.mintoUT;
				data.Hdr[(int)ComData.HdrId.Dop0] = (short)dop0;
				data.Hdr[(int)ComData.HdrId.Dop1] = (short)dop1;
				data.Hdr[(int)ComData.HdrId.Dop2] = (short)dop2;
				data.Hdr[(int)ComData.HdrId.Dop3] = (short)dop3;
				data.Hdr[(int)ComData.HdrId.Clockns] = clock;
				data.Hdr[(int)ComData.HdrId.Lat] = (short)_headerStruct.poppar.lat;
				data.Hdr[(int)ComData.HdrId.Lng] = (short)_headerStruct.poppar.lng;
				data.Hdr[(int)ComData.HdrId.Rev] = 0;
				data.Hdr[(int)ComData.HdrId.SiteId] = _headerStruct.poppar.radar.code;
					
				//
				// read data
				//

				int dataType = _dataPreamble.datatype;
				int spct = _dataPreamble.spct;
				uint wnbytes = _dataPreamble.wnbytes;

				int nDataBytes = 0;	
				int DataType3 = (dataType % 1000);	// last 3 digits of data type
				int DataType2 = (dataType % 100);	// last 2 digits of data type

				// Are there moments?
				int nBytesMoments=0;
				if ( (spct & 0x2) == 0 ) {
					// yes
					if (DataType3 == 15) {
						nBytesMoments = 5*nhts*nrx;
						throw new Exception("POP data file type x015 not currently supported");
					}
					else if (DataType3 == 115) {
						nBytesMoments = 8*nhts*nrx;
						double vscale = data.Nyquist/10000.0;
						for (int irx=0; irx<nrx; irx++) {
							for (int iht=0; iht<nhts; iht++) {
								short vel = _bReader.ReadInt16();
								short wid = _bReader.ReadInt16();
								short snr = _bReader.ReadInt16();
								short noise = _bReader.ReadInt16();
								data.Ht[iht] = (Int16)(data.GetKm(iht,true)*100.0);
								data.Vel[0,iht] = (Int16)(vel*vscale*100);
								data.Width[0,iht] = (Int16)(wid*vscale*100);
								data.Snr[0,iht] = snr;
								data.Noise[0,iht] = (Int32)(Math.Pow(10.0,noise/1000.0)+0.5);
							}
						}
					}
					else if (DataType3 == 117) {
						// has RASS too
						if (numMomentSets != 2) {
							throw new ApplicationException("For RASS, COM NRX should be 2.");
						}
						data.Hdr[(int)ComData.HdrId.Nrx] = 2;
						nBytesMoments = 8*nhts;		// wind moments
						nBytesMoments += 8*nhts;	// rass moments
						double vscale = data.Nyquist/10000.0;
						for (int irx=0; irx<2; irx++) {
							for (int iht=0; iht<nhts; iht++) {
								short vel = _bReader.ReadInt16();
								short wid = _bReader.ReadInt16();
								short snr = _bReader.ReadInt16();
								short noise = _bReader.ReadInt16();
								data.Ht[iht] = (Int16)(data.GetKm(iht,true)*100.0);
								data.Vel[irx,iht] = (Int16)(vel*vscale*100);
								data.Width[irx,iht] = (Int16)(wid*vscale*100);
								data.Snr[irx,iht] = (Int16)snr;
								data.Noise[irx,iht] = (Int32)(Math.Pow(10.0,noise/1000.0)+0.5);
							}
						}
					}
					else if (DataType3 == 215) {
						nBytesMoments = 10*nhts*nrx;
						throw new Exception("POP data file type x215 not currently supported");
					}
					else if (DataType3 == 16) {
						nBytesMoments = 11*nhts*nrx;
						throw new Exception("POP data file type x016 not currently supported");
					}
					/*
					else if ( (DataType3 == 116) || (DataType3 == 117) ) {
						nBytesMoments = 16*nhts*nrx;
					}
					*/
					else if ( (DataType3 == 216) || (DataType3 == 217) ) {
						nBytesMoments = 18*nhts*nrx;
						throw new Exception("POP data file type x217 not currently supported");
					}
					else {
						throw new Exception(" Moments data type error ") ;
					}

					nDataBytes += nBytesMoments;
				}

				// Are there met instruments?
				int nBytesInstruments=0;
				if (_headerStruct.nmet != 0) {
					// yes
					nBytesInstruments = 4*_headerStruct.nmet;
					nDataBytes += nBytesInstruments;
				}

				// Are there spectra?
				int nBytesSpectra=0;
				if ((spct & 0x1) != 0) {
					//yes
					if (DataType2 == 17) {
						nBytesSpectra = 4*nhts*nrx*(dop1+dop3);
					}
					else {
						nBytesSpectra = 4*nhts*nrx*npts;
					}
					nDataBytes += nBytesSpectra;
				}

				// Are there time series data?
				int nBytesTimeSeries=0;
				if ((spct & 0x4) != 0) {
					nBytesTimeSeries = 8*nhts*nrx*npts;
					nDataBytes += nBytesTimeSeries;
				}

				// Does data size agree with NBytes in header?

				if (wnbytes != 24 + nDataBytes + 4) {
					throw new Exception(" Data size differs from Hdr NBytes ") ;
				}

				/////////
				// read remainder of data record
				/////////
				
				//byte[] dataBytes = _bReader.ReadBytes(nDataBytes+4);

				
				byte[] metBytes;
				if (nBytesInstruments != 0) {
					metBytes = _bReader.ReadBytes(nBytesInstruments);
				}

				byte[] spectraBytes;
				if (nBytesSpectra != 0) {
					spectraBytes = _bReader.ReadBytes(nBytesSpectra);
				}

				byte[] timeSeriesBytes;
				if (nBytesTimeSeries != 0) {
					timeSeriesBytes = _bReader.ReadBytes(nBytesTimeSeries);
				}

				// skip nBytes at end of record
				int endBytes = _bReader.ReadInt32();
				if (endBytes != wnbytes) {
					//throw new Exception(" NBytes at beginning and end of record do not agree. ") ;
				}


			}  // end of lock(data)
			
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

			return true;

		}	// end of ReadRecord()

		/// <summary>
		/// 
		/// </summary>
		protected bool ReadPreamble() {

			// read in first 24 bytes of data record
			byte[] paramBytes;
			paramBytes = _bReader.ReadBytes(24);
			if (paramBytes.Length < 24) {
				// probably EOF
				return false;
			}

			// convert bytes to _dataPreamble
			Object obj = DacSerializer.RawDeserialize(paramBytes, typeof(DataPreambleStructure));
			if (obj is DataPreambleStructure) {
				_dataPreamble = (DataPreambleStructure)obj;
			}
			else {
				throw new ArgumentException("PopAsComFile: Cannot marshal dataPreamble");
			}

			return true;

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dtype"></param>
		/// <returns></returns>
		protected bool IsValidDataType(int dtype) {

			// is data type a valid type?
			int upper = dtype/100;
			if ((upper < 30) || (upper > 32)) {
				return false;
			}

			int lower = dtype % 100;
			if ((lower < 15) || (lower > 21)) {
				return false;
			}

			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		void ReadAndValidateHdrStruct() {

			//int xsize = Marshal.SizeOf(typeof(HeaderStruct) );

			if (_HeaderFileIsOpen) {
				// get size of Header structure
				_lastHdrPos = _bHdrReader.BaseStream.Position;
				int rawsize = Marshal.SizeOf( typeof(HeaderStruct) );
				byte[] headerBytes = _bHdrReader.ReadBytes(580);
				// convert bytes to _headerStruct
				Object obj = DacSerializer.RawDeserialize(headerBytes, typeof(HeaderStruct));
				if (obj is HeaderStruct) {
					_headerStruct = (HeaderStruct)obj;
				}
				else {
					throw new ArgumentException("PopAsComFile: Cannot marshal headerStruct");
				}

			}
			else {
				throw new Exception("PopAsComFile: Header file is not open");
			}

		}

		/// <summary>
		/// 
		/// </summary>
		public override void Write() {}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fn"></param>
		public override void SetFileName(string fn) {

			// open data file (in base class)
			openBinaryFileReader(fn);

			// open header file
			string headerFileName;
			string headerFullPathName = "";
			string dataFileName = Path.GetFileName(fn);
			string dirName = Path.GetDirectoryName(fn);
			if ((dataFileName.ToUpper())[0] == 'D') {
				headerFileName = dataFileName.Remove(0,1);
				headerFileName = headerFileName.Insert(0,"H");
				headerFullPathName = Path.Combine(dirName, headerFileName);
			}
			//throw new Exception("header is "+headerFullPathName);

			if (!_HeaderFileIsOpen) {
				_bHdrReader = null;
				// these constructors throw exceptions if errors
				FileStream fs = new FileStream(headerFullPathName,FileMode.Open,FileAccess.Read);
				_bHdrReader = new BinaryReader(fs);
				_HeaderFileIsOpen = true;
				_headerFileName = headerFullPathName;
			}

		}

		public override bool SkipRecord() {
			long beginPosition;	// to store position of beginning of record
			Int32 nBytes, nEndBytes;

			if (!_fileIsOpen) {
				throw new InvalidOperationException("ComFile: File not opened before Read()");
			}

			if (_recordNumber > _recordList.Count) {
				// this record position should have been in list
				throw new InvalidOperationException("ComFile.ReadRecord: record number mismatch");
			}

			beginPosition = _bReader.BaseStream.Position;

			try {
				Int16 dtype = _bReader.ReadInt16();
				nBytes = _bReader.ReadInt32();
			}
			catch (EndOfStreamException eos) {
				return false;
			}

			if (nBytes == 0) {
				checkBofEof();
				return false;
			}

			_bReader.BaseStream.Seek((long)(nBytes-10),SeekOrigin.Current);
			// read nbytes written at end of record
			nEndBytes = _bReader.ReadInt32();
			if (nBytes != nEndBytes) {
				throw new Exception("NBytes mismatch at beginning and end of record");
			}
			
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

		public void DoNothing() {
			return;
		}

		public HeaderStruct Header {
			get { lock(this){return _headerStruct;} }
		}


	}	// end of class PopAsComFile

}	// end of namespace
