using System;
using System.Collections;

namespace DACarter.NOAA {

	// PopHeader structure is used in PopData class
	public struct PopHeader {
		//public DateTime TimeStamp;
		public int TimeConvention;		// timestamp at begin, middle, end = 0,1,2
		public int MinutesToUT;
		public TimeSpan AveragingTime;
		public string StationName;
		public string RadarName;
		public int RadarID;
		public double LatitudeN;		// degrees
		public double LongitudeE;
		public int Altitude;		// meters
		public double TxFreq;		// MHz
		public bool TxIsOn;
		public int NCI;
		public int NSpec;
		public int NPts;
		public int DcFilter;
		public int Window;
		public int WindsSpectrumBeginIndex;
		public int WindsSpectrumNumPoints;				// start and interval pt # for moments (1-NPTS) 
		public int RassSpectrumBeginIndex;
		public int RassSpectrumNumPoints;				// start and interval pt # for second moments 
		public bool RassIsOn;			// parameters for rass acoustic source (see below)  
		public int RassLowFrequencyHz;	// parameters for rass acoustic source (see below) 
		public int RassHighFrequencyHz;	// parameters for rass acoustic source (see below)   
		public int RassStepHz;			// parameters for rass acoustic source (see below)   
		public int RassDwellMs;			// parameters for rass acoustic source (see below) 
		public int RassSweep;			// parameters for rass acoustic source (see below)  
		public int CltrHt;				// max ht for clutter removal (km*10);
		public int SpecAvg;				// <1 for MEAN, ==1 for H&S spectral averaging (ICRA)
		public int NRx;					// # multiplexed interferometer receivers 
		public int NMet;				// # met instruments
        public int[] MetCodes;
        public int RxMode;              // if < 1, multiplexed sampling; if flag 4 set, parallel, else multiplexed;
		public string DirName;			// label given to beam direction
		public double Azimuth;			// beam azimuth angle
		public double Elevation;			// beam elevation angle
		public int AntSwitchCode;	// antenna position switch code // added dac 2009Feb25
		public int IPP;					// nsec		
		public int PW;		
		public int Delay;			
		public int Spacing;
		public int NHts;
		public int NSets;		// number of sets of moments (=2 if RassIsOn, else =1)
		public int SysDelay;	// delay thru rx in nanosec 
		public int BwCode;		// rx bandwidth switch code 
		public int Atten;		// # range gates to attenuate 
		public int NCode;		// ncode = # bits in pulse code 
		public int Flip;
		public int NRxMode;		// is it in data file?, ==1 for dual polarization (NRx>1); otherwise 0
		public bool HasWindMoments;
		public bool HasRassMoments;
		public bool HasFullSpectra;
		public bool HasRassSpectra;
		public bool HasShortTimeSeries;
		public bool HasFullTimeSeries;
        public bool HasFMRawTimeSeries;  // HasFullTimeSeries==true and data is from *.raw.ts file
		// probably not needed:
		public int PBPreTR;     // all times in nanosec 
		public int PBPostTR;
		public int PBSynch;
		public int PBPreBlank;
		public int PBPostBlank;	
			
	}  // end of PopHeader struct

	//
	// The following structs can be used to mimic the original POP Header file format.
	//
	public struct PopHeaderFileStruct {
		public int RevLevel;
		public int HdrBytes;
		public int NumInstruments;
		public int MaxRadars, MaxBeamParams, MaxBeams, MaxDirections, MaxBandwidths;
		public HdrSystemParameters SysPar;
		public int DataFilePosition;
        public int ReceiverMode;        // < 1 is multiplexed; flag 4 set is parallel (interlaced) sampling
		public int[] HdrInstrumentCodes;
	}

	public struct HdrSystemParameters {
		public string StationName;				// station name 
		public double Latitude, Longitude;		// N latitude, E longitude, (original int is deg*100 )
		public int MinutesToUT;					// # minutes add to sys time to get UT 
		public int Altitude;					// altitude above sea level, meters 
		//public int NumRadars;					// number of radars at this station  = 1
		public string RadarName;				// name of radar 
		public int RadarID;						// code number for radar 
		public double Frequency;					// tx freq (original int in Mhz*100)
		//public double MaxDuty;					// max duty cycle 
		//public int MaxTx;						// max Tx pulse length (usec) 
		public bool TxIsOn;						// tx pulse on 
		public int NumDirections;				// number of allowable directions 
		public int NumBeams;					// number of beam positions chosen 
		public int NumParamSets;				// number of beam parameter sets chosen 
		public HdrBeamParameters[] BeamParams;	// array of beam postion parameter sets (dimension = 4)
		public HdrBeamControl[] Beams;			// array of chosen beam positions (dimension = 10) 
		public HdrPbxConstants PbxConstants;    // pulse box constants for this radar
		public HdrDirections[] Directions;		// array of allowable directions (dimension = 9) 
		public HdrRxBandwidth[] RxBw;			// matched pulsewidths (nsec) for rx bandwidth (dimension = 4)
												// plus total extra delay for each rx bw 
		public HdrProcessingParameters Processing;
	}

	public struct HdrBeamControl {
		public int DirIndex;
		public int ParameterIndex;
		public int Repetitions;
	}

	public struct HdrBeamParameters {
		public int IPP;					// nsec		
		public int PW;
		public int Delay;
		public int Spacing;
		public int NHts;
		public int NCI;
		public int NSpec;
		public int NPts;
		public int SysDelay;	// delay thru rx in nanosec 
		public int BWIndex;
		public int NAtten;
		public int NCode;
        public int Flip;  // added 20120801; set if tx phase flip
	}

	public struct HdrDirections {
		public string DirectionLabel;
		public double Azimuth;
		public double Elevation;
		public int SwitchCode;
	}

	public struct HdrRxBandwidth {
		public int PulseWidth;		// nsec
		public int RxDelay;			// nsec
	}

	public struct HdrPbxConstants {
		public int PBPreTR;			// all times in nanosec 
		public int PBPostTR;
		public int PBSynch;
		public int PBPreBlank;
		public int PBPostBlank;
	};

	public struct HdrProcessingParameters {

		public int DcFilter, Window;
		public int DcOmit;						// # pts omitted around dc (obsolete) 
		public int OmitHts;						// # hts to apply dcomit 

		public int Dop0, Dop1;					// start and interval pt # for moments (1-NPTS) 
		public int Dop2, Dop3;					// start and interval pt # for second moments 
	    public HdrRassParameters RassParams;    // parameters for rass acoustic source    
		public int CltrHt;						// max ht for clutter removal (km*10); 
		public int SpecAvg;						// <1 for MEAN, ==1 for H&S spectral averaging  
		public int NRx;							// # multiplexed interferometer receivers  
		public int NMet;						// # met instruments -- 941004 DAC 
		//int misc[22];							// space saver for future use, keep total = 80b 
	};

	public struct HdrRassParameters {
		public bool RassIsOn;	
		public int RassBeginFreq;		// Hz
		public int RassEndFreq;
		public int RassStep;
		public int RassDwell;			// usec
		public int RassSweep;
	}


	//
	// End of Pop Header File Structure definitions
	//

	public class PopData : DacData {
									// NSets is number of sets of moments (=2 for RASS, else =1)
		//public bool FillTimeSeriesArrays;
		public PopHeader Hdr;
		public int CurrentBeamIndex;
		public float[] Hts;			// dimension is [NHts], units are km
		public float[,,] Vel;		// dimensions are [NRx, NSets, NHTs] (m/sec)
		public float[,,] Snr;		// ""; (dB)
		public float[,,] Noise;		// ""; linear noise level
		public float[,,] Width;		// ""; (m/sec)
		public float[,,] Spectra;		// dimensions are [NRx, NHts, NPts]
		public float[,,,] TimeSeries;	// dimensions are [NRx, NHts, NPts*NSpec, 2]
		public enum PopDataType {
			Velocity,
			SNR,
			Width,
			NoiseLevel
		}

		// These 4 values are set only in the 'Set..Size' methods
		// and represent the current array sizes, not the parameter values:
		private int _nsets, _nhts, _nrx, _nptsSpec, _nptsTS, _nspec, _numTS;

		
		public int NHts {
			get {
				/*lock(this)*/ {
					return Hdr.NHts;
				}
			}
		}
		public int NSets {
			get {
				/*lock(this)*/ {
					return Hdr.NSets;
				}
			}
		}
		public int NRx {
			get {
				/*lock(this)*/ {
					return Hdr.NRx;
				}
			}
		}
		public int NPts {
			get {
				/*lock(this)*/ {
					return Hdr.NPts;
				}
			}
		}
		

		// dac 7Sep2005 TimeStamp is now in base DacData
		/*
		public DateTime TimeStamp {
			get {
				lock (this) { 
					return Hdr.TimeStamp;
				}
			}
		}
		*/

		public double Nyquist {
			get {
				/*lock(this)*/ {
					double nyq = (2.997925e8/Hdr.TxFreq)/(4.0*Hdr.NCI*Hdr.IPP*1.0e-9);
					return nyq;
				}
			}
		}

		public double DefaultSNRThreshold {
			// Tony's formula for SNR threshold
			get {
				/*lock(this)*/ {
					int nspec = Hdr.NSpec;
					int npts = Hdr.NPts;
					double qq = nspec - 2.3125 + 170.0/npts;
					if (qq < 0.2) {
						qq = 0.2;
					}
					qq = 25.0 * Math.Sqrt(qq)/(nspec*npts);
					return (10.0 * Math.Log10(qq));
				}
			}
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public PopData(){
			//FillTimeSeriesArrays = false;  // by default do not read time series data
			// force arrays to be created:
			_nsets = -1;
			_nhts = -1;
			_nrx = -1;
            _nptsTS = -1;
            _nptsSpec = -1;
            _numTS = -1;
			// create zero-length arrays:
			SetSize(0, 0, 0, 0);
		}

		/// <summary>
		/// Constructor with array dimensions as arguments
		/// </summary>
		/// <param name="nsets">Number of sets of moments (i.e. RASS and Winds)</param>
		/// <param name="nhts">Number of heights</param>
		/// <param name="nrx">Number of multiplexed receiver sets</param>
		public PopData(int nrx, int nsets, int nhts, int npts) {
			//FillTimeSeriesArrays = false;  // by default do not read time series data
			// force arrays to be created:
			_nsets = -1;
			_nhts = -1;
			_nrx = -1;
            _nptsTS = -1;
            _nptsSpec = -1;
            _numTS = -1;
			SetSize(nrx, nsets, nhts, npts);
		}

        public PopData(int nrx, int nsets, int nhts, int nptsSpec, int nptsTS, int numTS) {
            _nsets = -1;
            _nhts = -1;
            _nrx = -1;
            _nptsTS = -1;
            _nptsSpec = -1;
            _numTS = -1;
            SetSize(nrx, nsets, nhts, nptsSpec, nptsTS, numTS);
        }

		public PopData(int nrx, int nsets, int nhts, int npts, int numTS) {
			//FillTimeSeriesArrays = false;  // by default do not read time series data
			// force arrays to be created:
			_nsets = -1;
			_nhts = -1;
			_nrx = -1;
            _nptsTS = -1;
            _nptsSpec = -1;
            _numTS = -1;
			SetSize(nrx, nsets, nhts, npts, numTS);
		}

		public override void Clear() {
			Notes = "";
			TimeStamp = DateTime.MinValue;
		}

		/// <summary>
		/// Redimension data arrays if necessary
		/// </summary>
		/// <param name="nrx"></param>
		/// <param name="nsets"></param>
		/// <param name="nhts"></param>
		/// <param name="npts"></param>
		//public void SetSize(int nrx, int nsets, int nhts, int npts) {
			//SetDataSize(nrx, nsets, nhts, npts);
		//}

		/// <summary>
		/// Sets up internal array sizes based on values in a supplied
		/// header array.  Copies the first npar=hdr[0] elements
		/// of the parameter header array into the internal header.
		/// </summary>
		/// <remarks>
		/// This method is equivalent to calling SetSize() and then
		/// assigning values to the Hdr[] array, but this method will
		/// guarrantee that the internal array dimensions agree with
		/// the header parameters.
		/// </remarks>
		/// <param name="hdr"></param>
		public void InitFromHeader(PopHeader hdr) {
			// set header and data array sizes
			if (hdr.HasFullTimeSeries || hdr.HasShortTimeSeries) {
				SetSize(hdr.NRx, hdr.NSets, hdr.NHts, hdr.WindsSpectrumNumPoints + hdr.RassSpectrumNumPoints, hdr.NSpec);
			}
			else {
				SetSize(hdr.NRx, hdr.NSets, hdr.NHts, hdr.WindsSpectrumNumPoints + hdr.RassSpectrumNumPoints);
			}
            if (hdr.HasFullTimeSeries || hdr.HasShortTimeSeries) {
                //FillTimeSeriesArrays = true;
            }
            // This is same as a deep copy since hdr struct contains only value types
            Hdr = hdr;
		}

		// this version assumes the internal header has already been set
		public void InitFromHeader() {
			// set header and data array sizes
            // POPREV modified in 4.8 for different spectral and time series npts
            SetSize(Hdr.NRx, Hdr.NSets, Hdr.NHts, Hdr.WindsSpectrumNumPoints + Hdr.RassSpectrumNumPoints, Hdr.NPts, Hdr.NSpec);
        }

		// Sets the size of the data arrays.
		//
		public void SetSize(int nrx,int nsets,int nhts,int npts) {
			SetSize(nrx, nsets, nhts, npts, 0);
		}

        public void SetSize(int nrx, int nsets, int nhts, int npts, int nts) {
            // assumes # spectral pts is same as # time series pts
            SetSize(nrx, nsets, nhts, npts, npts, nts);
        }

        public void SetSize(int nrx, int nsets, int nhts, int nptsSpec, int nptsTS, int nts) {
			// nts specifies the max number of timeseries sets to read
            if ((nhts < 0) || (nrx < 0) || (nsets < 0) || (nptsSpec < 0) || (nptsTS < 0) || (nts < 0)) {
				throw new ArgumentException("SetSize size is less than 0");
			}
			if (_nhts != nhts) {
				Hts = new float[nhts];
			}
			if (Hdr.HasWindMoments) {
				if ((_nhts != nhts) || (_nrx != nrx) || (_nsets != nsets)) {
					Vel = new float[nrx, nsets, nhts];
					Snr = new float[nrx, nsets, nhts];
					Noise = new float[nrx, nsets, nhts];
					Width = new float[nrx, nsets, nhts];
				}
				
			} 
			if (Hdr.HasFullSpectra || Hdr.HasRassSpectra) {
                if ((_nrx != nrx) || (_nhts != nhts) || (_nptsSpec != nptsSpec)) {
                    Spectra = new float[nrx, nhts, nptsSpec];	
				}
				// TODO: what if HasSpectra changes from false to true in same file? 
			}

			int numTS = 0; 
			if (Hdr.HasFullTimeSeries) {
				numTS = Math.Min(nts, Hdr.NSpec);
			}
            else if (Hdr.HasShortTimeSeries) {
                numTS = Math.Min(nts, 1);
            }
            else {
                numTS = 0;
            }
            if (numTS > 0) {
                if ((_nrx != nrx) || (_nhts != nhts) || (_nptsTS != nptsTS) || (_numTS != numTS)) {
                    TimeSeries = new float[nrx, nhts, nptsTS * numTS, 2];
                }
            }

			_nhts = nhts;
			_nrx = nrx;
            _nptsTS = nptsTS;
            _nptsSpec = nptsSpec;
            _nsets = nsets;
			_numTS = numTS;
		}

		/// <summary>
		/// Returns a new PopData object whose contents are identical to the original.
		/// </summary>
		/// <returns>The new PopData object.</returns>
		public PopData Copy() {

			PopData data = new PopData(_nrx, _nsets,_nhts, _nptsSpec, _nptsTS, _numTS);
			CopyTo(data);
			return data;
			
		}	//end of PopData.Copy()

		/// <summary>
		/// Copies all the elements of this object to another existing object.
		/// </summary>
		/// <param name="dest">The destination PopData object.</param>
		public void CopyTo(PopData dest) {
			if (dest == null) {
				throw new ArgumentNullException("PopData CopyTo() argument is null");
			}
			dest.SetSize(_nrx, _nsets, _nhts, _nptsSpec, _nptsTS, _numTS);
			// TODO - this needs deep copy if Hdr contains any reference types except strings - dac
			dest.Hdr = this.Hdr;

			for (int iht=0; iht<_nhts; iht++) {
				dest.Hts[iht] = this.Hts[iht];
			}
			for (int irx=0; irx<_nrx; irx++) {
				for (int iset=0; iset<_nsets; iset++) {
					for (int iht=0; iht<_nhts; iht++)  {
						dest.Vel[irx,iset,iht] = this.Vel[irx,iset,iht];
						dest.Snr[irx,iset,iht] = this.Snr[irx,iset,iht];
						dest.Noise[irx,iset,iht] = this.Noise[irx,iset,iht];
						dest.Width[irx,iset,iht] = this.Width[irx,iset,iht];
					}
				}
			}

			if (_nptsSpec > 0) {
				for (int irx=0; irx<_nrx; irx++) {
					for (int iht=0; iht<_nhts; iht++)  {
						for (int ipt=0; ipt<_nptsSpec; ipt++) {
							dest.Spectra[irx, iht, ipt] = this.Spectra[irx, iht, ipt];
						}
					}
				}
			}

			if (_numTS > 0) {
				for (int irx = 0; irx < _nrx; irx++) {
					//for (int its = 0; its < _numTS; its++) {
						for (int iht = 0; iht < _nhts; iht++) {
							for (int ipt = 0; ipt < _nptsTS * _numTS; ipt++) {
								for (int iri = 0; iri < 2; iri++) {
									dest.TimeSeries[irx, iht, ipt, iri] = this.TimeSeries[irx, iht, ipt, iri];
								}
							}
						}
					//}
				}
			}

		}  // end of PopData.CopyTo()


		/// <summary>
		/// Computes height in km for a given range gate.
		/// </summary>
		/// <param name="iht">Height index.</param>
		/// <param name="isASL">true for height above sea level, false for above ground.</param>
		/// <returns>Height in km.</returns>
		public double GetHtKm(int iht, bool isASL) {

			double km;
			double fctr;
			double hdrElev;

			km = GetRangeKm(iht);

			hdrElev = Hdr.Elevation;
			fctr = (float)Math.Sin(Math.PI * hdrElev / 180.0);

			km *= fctr;

			if (isASL) {
				km += (double)Hdr.Altitude/1000.0;
			}

			return(km);
		}

		public double GetRangeKm(int iht) {

			double km;
			int sdelay, pw;
			int delay, spacing;

			sdelay = Hdr.SysDelay;
			pw = Hdr.PW;
			delay = Hdr.Delay;
			spacing = Hdr.Spacing;

			if (sdelay == -1) {
				// old CXI50 ID=42 <1991Day10
				// not sure what sysdelay should be
				sdelay = pw / 2;
			}
			// if SYSDELAY is negative, units are microsec:
			if (sdelay < 0) {
				sdelay = -sdelay * 1000;
			}

			//km = ((delay + iht * spacing) - sdelay + (nc - 1) * pw) * .149896 / 1000.0 * fctr;
			km = ((delay + iht * spacing) - sdelay) * .149896 / 1000.0;
			return km;
		}

		public string StationLabel {
			get {
				lock(this) {
					if ((Hdr.StationName != null) && (Hdr.StationName != "")) {
						return Hdr.StationName;
					}
					else {
						// no Station name in data (e.g. from COM file)
						int site = Hdr.RadarID;
						string label;
						switch (site) {
							case 100:
								label = "Boulder Test";
								break;
							case 103:
								label = "Mt. Isa";
								break;
							case 104:
								label = "Darwin 50";
								break;
							case 105:
								label = "Christmas 915";
								break;
							case 106:
								label = "Pohnpei 50";
								break;
							case 107:
								label = "Saipan 50";
								break;
							case 109:
								label = "Harp 915";
								break;
							case 110:
								label = "Saipan 915";
								break;
							case 112:
								label = "Christmas 50";
								break;
							case 113:
								label = "Piura 50";
								break;
							case 114:
								label = "Biak 50";
								break;
							case 118:
								if (TimeStamp.Year < 2001) {
									label = "Flatland 915";
								}
								else {
									label = "Pease 915";
								}
								break;
							case 119:
								label = "Manus 915";
								break;
							case 121:
								label = "Darwin 920";
								break;
							case 123:
								label = "Kapinga 915";
								break;
							case 124:
								label = "Kavieng 915";
								break;
							case 126:
								label = "Ship Sci1";
								break;
							case 127:
								label = "Ship Exp3";
								break;
							case 128:
								label = "Nauru 915";
								break;
							case 130:
								label = "Flatland 50";
								break;
							case 131:
								label = "Moana Wave 915";
								break;
							case 133:
								label = "Tarawa 915";
								break;
							case 135:
								label = "Galapagos 915";
								break;
							case 136:
								label = "Kapinga 915";
								break;
							case 139:
								label = "PACS RHB";
								break;
							case 140:
								label = "Biak 915";
								break;
							case 141:
								label = "MCTEX SBand";
								break;
							case 142:
								label = "Manus SBand";
								break;
							case 143:
								label = "Garden Pt";
								break;
							case 144:
								label = "Ships";
								break;
							case 146:
								label = "TRMM SBand";
								break;
							case 147:
								label = "RHB Ship SBand";
								break;
							default:
								label = "Unknown";
								break;
						}
						return label;
					}  // end of else
				}  // end of lock
			}  // end of get
		}  // end of StationLabel property

		/// Names for the header parameters
		/// that can be used in place of array indices.
		/// in old COM Int16 header array
		/// 
		/*
		public enum HdrId {
			Npar,
			OrigSize,
			Nhts,		// same as Nsam
			Nrx,
			Npts,
			Nspec,
			Nrej,		// not really used
			Nci,
			Ippus,
			Pwclk,
			Delayclk,
			Spacingclk,
			Nsam,
			Delay2,		// same as DelayClk
			Spacing2,	// same as Spacingclk
			Nsam2,		// same as Nsam
			Year,
			Doy,
			Hour,
			Minute,
			Second,
			Nmin,		// not used
			Nthld,		// not used
			Spec,		// ==1 if spectra in file
			Mom,
			X25,
			X26,
			X27,
			X28,
			Az,
			Freq,
			X31,
			Alt,
			PCode,
			Elev,
			X35,
			Icra,
			SysDly,		// nsec, unless <0 then usec
			UTmin,
			Dop0,
			Dop1,
			Dop2,
			Dop3,
			X43,X44,X45,X46,X47,X48,X49,X50,
			X51,X52,X53,X54,X55,X56,X57,
			Clockns,
			X59,
			Lat,
			Lng,
			Rev,
			SiteId,		// index 63
			MetFlag,	// if met data follow, this is 16 bit flags to indicate which instruments present
			ShipLat = 83,
			ShipLong,
			ShipSpeed,
			ShipHeading,
			ShipCompass

		}
		*/
	}

	/*
	// ***********************************************************************************************************

	/// <summary>
	/// A general class to collect PopDataBlock objects of any beam or mode.
	/// This class contains a list of PopDataBlockMode objects, each of which 
	/// contains PopDataBlocks corresponding to one particular mode.
	/// The PopDataBlockMode objects can be retrieved
	/// through an indexer on a PopDataBlockModeSet object.
	/// A PopDataBlockMode object contains a list of PopDataBlocks that
	/// correspond to all the beams in that mode.
	/// PopDataBlock objects can be retrieved through an indexer on the
	/// PopDataBlockMode object.
	/// Data from PopData items can be written to PopDataBlocks collected
	/// in this PopDataBlockModeSet object via the Add method.
	/// 
	/// PopDataBlockModeSet (ht/time arrays of all modes)
	///      contains a list of
	/// PopDataBlockMode objects (each is ht/time data from all beams of one mode)
	///      each of which contains a list of 
	/// PopDataBlock objects (each is ht/time data from one mode/beam)
	///      each of which contains lists of vectors of data from
	/// PopData objects (each is ht profile of data from one time (one dwell))
	/// 
	/// </summary>
	public class PopDataBlockModeSet : IEnumerable {

		private ArrayList _dataBlockModeList;  // contains PopDataBlockSeries items
		public PopModeAccessor Mode;
		private int _stationID;
		private string _stationLabel;

		public PopDataBlockModeSet() {
			_dataBlockModeList = new ArrayList();
			Mode = new PopModeAccessor(this);
			_stationID = -1;
			_stationLabel = "??";
		}

		public bool DataIsMatch(PopData data) {
			if (_dataBlockModeList.Count == 0) {
				return true;
			}
			else if (data.Hdr.RadarID != _stationID) {
				return false;
			}
			else {
				return true;
			}
		}


		public void Add(PopData data) {
			
			if (!DataIsMatch(data)) {
				throw new ArgumentException("PopDataBlockModeSet: Add data type does not match station in series");
			}
			if (_dataBlockModeList.Count == 0) {
				// no mode blocks in list, use this data to set station
				//_dataBlockModeList.Add(new PopDataBlockMode());
				_stationID = data.Hdr.RadarID;
				_stationLabel = data.StationLabel;
			}
			
			// find a data block mode (a PopDataBlockMode item)
			// in list that matches this data
			// and add data to that mode
			bool matchingModeFound = false;
			//PopDataBlockMode mode;
			foreach (PopDataBlockMode mode in _dataBlockModeList) {
				//mode = obj as PopDataBlockMode;
				if (mode.DataIsMatch(data)) {
					mode.Add(data);
					matchingModeFound = true;
					break;
				}
			}
			// if no matching block found, start a new one
			if (!matchingModeFound) {
				PopDataBlockMode mode = new PopDataBlockMode();
				mode.Add(data);
				_dataBlockModeList.Add(mode);

			}
		}


		/// <summary>
		/// Extracts x,y,z arrays (time, height, data value)
		/// for a particular data type 
		/// and a specific mode and beam.
		/// </summary>
		/// <param name="tData"></param>
		/// <param name="hData"></param>
		/// <param name="zData"></param>
		/// <param name="mode"></param>
		/// <param name="beam"></param>
		/// <param name="type"></param>
		/// <param name="setIndex"></param>
		public void GetTHZArray(out DateTime[] tData, out double[] hData, out double[,] zData,
								int mode, int beam, PopData.PopDataType type, int snrThreshold, int setIndex) {

			int ptCount = this.Mode[mode].Beam[beam].Ntimes;
			int htCount = this.Mode[mode].Beam[beam].Nhts;

			hData = new double[htCount];
			tData = new DateTime[ptCount];
			zData = new double[ptCount, htCount];

			this.Mode[mode].Beam[beam].GetTHZArray(out tData, out hData, out zData, type, snrThreshold, setIndex);

		}	// end of GetTHZArrays()


		// define indexer so we can access blockModes in the list
		// through an index on a PopDataBlockModeSet object.
		// e.g. PopDataBlockModeSet modeSet;
		// e.g. PopDataBlockMode mode = modeSet[i];
		public PopDataBlockMode this[int i] {
			get {
				return (PopDataBlockMode)_dataBlockModeList[i];
			}
		}

		// An alternative way to access blocks in the list
		// e.g. PopDataBlockModeSet modeSet;
		// e.g. PopDataBlockMode mode = modeSet.Mode[i];
		public class PopModeAccessor {
			PopDataBlockModeSet _set;
			public PopModeAccessor(PopDataBlockModeSet set) {
				_set = set;
			}
			public PopDataBlockMode this[int i] {
				get {
					return (PopDataBlockMode)_set[i];
				}
			}
		}


		public int Count {
			get {
				return (_dataBlockModeList.Count);
			}
		}

		public int StationID {
			get {
				return _stationID;
			}
		}

		public string StationLabel {
			get {
				return _stationLabel;
			}
		}

		// implement IEnumerable so we can use foreach on PopDataBlockModeSet.
		// New implementation for .NET 2.0
		public IEnumerator GetEnumerator() {
			for (int i = 0; i < this.Count; i++) {
				yield return this[i];
			}
		}

		// implement IEnumerable so we can use foreach on PopDataBlockModeSet.
		
		#region IEnumerable Members
		
		public IEnumerator GetEnumerator() {
			return new MyCollectionEnumerator(this);
		}
		private class MyCollectionEnumerator : IEnumerator {

			private int _lastCount;
			private PopDataBlockModeSet _col;
			int _index;

			public MyCollectionEnumerator(PopDataBlockModeSet col) {
				_col = col;
				_lastCount = col.Count;
				_index = -1;
			}
			#region IEnumerator Members

			public void Reset() {
				if (_lastCount != _col.Count) {
					throw new InvalidOperationException();
				}
				_index = -1;
			}

			public object Current {
				get {
					return _col[_index];
				}
			}

			public bool MoveNext() {
				if (_lastCount != _col.Count) {
					throw new InvalidOperationException();
				}
				if (++_index >= _col.Count) {
					return false;
				}
				return true;
			}

			#endregion
		}
		
		#endregion
		

	}
	
	// ***********************************************************************************************************


	/////////////////////////////////////////////////////////////////////

	/// <summary>
	/// Contains a list of PopDataBlock items all representing the same mode.
	/// A mode is defined as all having the same
	/// pw, pcode, nrx, delay, and spacing.
	/// This version - nhts can vary.
	/// </summary>
	public class PopDataBlockMode : IEnumerable {
		#region private class members
		private int _pw;
		private int _ncode;
		private int _nsets;
		private int _delay;
		private int _spacing;
		//private Int16 _clock;
		private int _stationID;
		private string _stationLabel;
		private ArrayList _dataBlockList;  // contains PopDataBlock items
		#endregion 


		public BeamAccessor Beam;

		public PopDataBlockMode() {
			_dataBlockList = new ArrayList();
			Beam = new BeamAccessor(this);
			//_initialize(Int16.MaxValue,Int16.MaxValue,Int16.MaxValue,Int16.MaxValue,Int16.MaxValue,Int16.MaxValue);
			initialize(null);
		}

		private void initialize(PopData data) {
			if (data == null) {
				_pw = int.MaxValue;
				_ncode = int.MaxValue;
				_nsets = int.MaxValue;
				_delay = int.MaxValue;
				_spacing = int.MaxValue;
				_stationID = -1;
				_stationLabel = "??";
			}
			else {
				_pw = data.Hdr.PW;
				_ncode = data.Hdr.NCode;
				_nsets = data.Hdr.NSets;
				_delay = data.Hdr.Delay;
				_spacing = data.Hdr.Spacing;
				_stationID = data.Hdr.RadarID;
				_stationLabel = data.StationLabel;
			}
		}

		
		private void _initialize(Int16 pw, Int16 pcode,
			Int16 nrx, Int16 delay, Int16 spacing, Int16 clock) {
			_pw = pw;
			_pcode = pcode;
			_nrx = nrx;
			_delay = delay;
			_spacing = spacing;
			_clock = clock;
		}
		
		
		public bool DataIsMatch(PopData data) {
			if (_dataBlockList.Count == 0) {
				return true;
			}
			if ((data.Hdr.Delay != _delay) ||
				(data.Hdr.Spacing != _spacing) ||
				(data.Hdr.PW != _pw) ||
				(data.Hdr.NCode != _ncode) ||
				(data.Hdr.NSets != _nsets) )  {

				return false;
			}
			else {
				return true;
			}
		}

		public void Add(PopData data) {
			if (!DataIsMatch(data)) {
				throw new ArgumentException("PopDataBlockMode: Add data type does not match data in series");
			}
			if (_dataBlockList.Count == 0) {
				// no blocks in list, create one to match this data
				_dataBlockList.Add(new PopDataBlock());
				initialize(data);
			}
			// find a data block in list that matches this data
			// and add data to that block
			bool matchingBlockFound = false;
			foreach (PopDataBlock blk in _dataBlockList) {
				if (blk.DataIsMatch(data)) {
					blk.Add(data);
					matchingBlockFound = true;
					break;
				}
			}
			// if no matching block found, start a new one
			if (!matchingBlockFound) {
				PopDataBlock blk = new PopDataBlock();
				blk.Add(data);
				_dataBlockList.Add(blk);

			}
		}

		// define indexer so we can access blocks in the list
		// through an index on a PopDataBlockMode object.
		// e.g. PopDataBlockMode mode;
		// e.g. PopDataBlock blk = mode[i];
		public PopDataBlock this[int i] {
			get {
				return (PopDataBlock)_dataBlockList[i];
			}
		}

		// An alternative way to access blocks in the list
		// e.g. PopDataBlockMode mode;
		// e.g. PopDataBlock blk = mode.Beam[i];
		public class BeamAccessor {
			PopDataBlockMode _mode;
			public BeamAccessor(PopDataBlockMode mode) {
				_mode = mode;
			}
			public PopDataBlock this[int i] {
				get {
					return (PopDataBlock)_mode[i];
				}
			}
		}

		//
		// properties
		//
		public int Count {
			get {
				return (_dataBlockList.Count);
			}
		}
		public int Pw {
			get {
				if (_dataBlockList.Count == 0) {
					throw new InvalidOperationException("PopDataBlockSeries: PW not yet defined");
				}
				else {
					return _pw;
				}
			}
		}
		public int Delay {
			get {
				if (_dataBlockList.Count == 0) {
					throw new InvalidOperationException("PopDataBlockSeries: Delay not yet defined");
				}
				else {
					return _delay;
				}
			}
		}
		public int Spacing {
			get {
				if (_dataBlockList.Count == 0) {
					throw new InvalidOperationException("PopDataBlockSeries: Spacing not yet defined");
				}
				else {
					return _spacing;
				}
			}
		}
		public int NCode {
			get {
				if (_dataBlockList.Count == 0) {
					throw new InvalidOperationException("PopDataBlockSeries: Pulse Code not yet defined");
				}
				else {
					return _ncode;
				}
			}
		}
		public int NSets {
			get {
				if (_dataBlockList.Count == 0) {
					throw new InvalidOperationException("PopDataBlockSeries: NRx not yet defined");
				}
				else {
					return _nsets;
				}
			}
		}


		public int StationID {
			get {
				return _stationID;
			}
		}

		public string StationLabel {
			get {
				return _stationLabel;
			}
		}


		// implement IEnumerable so we can use foreach on PopDataBlockMode.
		// New implementation for .NET 2.0
		public IEnumerator GetEnumerator() {
			for (int i = 0; i < this.Count; i++) {
				yield return this[i];
			}
		}

		// implement IEnumerable so we can use foreach on PopDataBlockMode.
		// Old implementation for .NET 1.1
		
		#region IEnumerable Members
		public IEnumerator GetEnumerator() {
			return new MyCollectionEnumerator(this);
		}
		private class MyCollectionEnumerator : IEnumerator {

			private int _lastCount;
			private PopDataBlockMode _col;
			int _index;

			public MyCollectionEnumerator(PopDataBlockMode col) {
				_col = col;
				_lastCount = col.Count;
				_index = -1;
			}
			#region IEnumerator Members

			public void Reset() {
				if (_lastCount != _col.Count) {
					throw new InvalidOperationException();
				}
				_index = -1;
			}

			public object Current {
				get {
					return _col[_index];
				}
			}

			public bool MoveNext() {
				if (_lastCount != _col.Count) {
					throw new InvalidOperationException();
				}
				if (++_index >= _col.Count) {
					return false;
				}
				return true;
			}

			#endregion
		}
		#endregion
		

	}


	// ***********************************************************************************************************

	public class PopDataBlock {
		
		// PopData public arrays (for reference):
		public float[] Hts;			// dimension is [NHts], units are km
		public float[,,] Vel;		// dimensions are [NRx, NSets, NHTs]
		public float[,,] Snr;
		public float[,,] Noise;
		public float[,,] Width;
		public float[,,] Spectra;		// dimensions are [NRx, NHts, NPts]
		
		// PopDataBlock public arrays:
		public float[] Hts;
		public ArrayList Times;
		public ArrayList VelBlock;
		public ArrayList SnrBlock;
		public ArrayList NoiseBlock;
		public ArrayList WidthBlock;

		private float _azimuth;
		private float _elevation;
		private int _pw;
		private int _ncode;
		private int _nrx;
		private int _nsets;
		private int _npts;
		private double _defaultThreshold;
		private int _stationID;
		private string _stationLabel;

		public PopDataBlock() {
			_createArrays();
			initialize(null);
			//_initialize(null,Int16.MaxValue,Int16.MaxValue,
			//	Int16.MaxValue,Int16.MaxValue,Int16.MaxValue);
		}

		private void _createArrays() {
			int initialCapacity = 250;
			Times = new ArrayList(initialCapacity);
			VelBlock = new ArrayList(initialCapacity);
			SnrBlock = new ArrayList(initialCapacity);
			NoiseBlock = new ArrayList(initialCapacity);
			WidthBlock = new ArrayList(initialCapacity);
		}

		private void initialize(PopData data) {
			if (data == null) {
				Hts = null;
				_pw = Int16.MaxValue;
				_ncode = Int16.MaxValue;
				_nrx = 0;
				_nsets = 0;
				_npts = 0;
				_azimuth = Int16.MaxValue;
				_elevation = Int16.MaxValue;
				_defaultThreshold = -99;
				_stationID = -1;
				_stationLabel = "??";
				//_clock = Int16.MaxValue;
			}
			else {
				if (Hts != null) {
					throw new InvalidOperationException("PopDataBlock already initialized");
				}
				if (data.Hts != null) {
					data.Hts.CopyTo(Hts, 0);
				}
				else {
					Hts = null;
				}
				_pw = data.Hdr.PW;
				_ncode = data.Hdr.NCode;
				_nrx = data.NRx;
				_nsets = data.NSets;
				_npts = data.NPts;
				_azimuth = data.Hdr.Azimuth;
				_elevation = data.Hdr.Elevation;
				_defaultThreshold = data.DefaultSNRThreshold;
				_stationID = data.Hdr.RadarID;
				_stationLabel = data.StationLabel;
				//_clock = data.Hdr[(int)PopData.HdrId.Clockns];
			}
		}

		
		private void _initialize(Int16[] hts, Int16 az, Int16 elev,
								Int16 pw, Int16 pcode, Int16 nrx) {
			if (Hts != null) {
				throw new InvalidOperationException("PopDataBlock already initialized");
			}
			if (hts != null) {
				Hts = (Int16[])(hts.Clone());
			}
			else {
				Hts = null;
			}
			_azimuth = az;
			_elevation = elev;
			_pw = pw;
			_pcode = pcode;
			_nrx = nrx;
		}
		

		public bool DataIsMatch(PopData data) {
			if (Hts == null) {
				return true;
			}
			int nhts = Hts.Length;
			if (data.NHts != nhts) {
				return false;
			}
			if ((data.Hts[0] != Hts[0]) ||
				(data.Hts[nhts-1] != Hts[nhts-1]) ||
				(data.Hdr.Azimuth != _azimuth) ||
				(data.Hdr.Elevation != _elevation) ||
				(data.Hdr.PW != _pw) ||
				(data.Hdr.NCode != _ncode) ||
				(data.Hdr.NRx != _nrx) ||
				(data.Hdr.NSets != _nsets) ||
				(data.Hdr.NPts != _npts) ||
				(data.Hdr.RadarID != _stationID) ) {
				return false;
			}
			else {
				return true;
			}

		}

		/// <summary>
		/// Add data from one PopData object to the end of the block
		///   of data within this PopDataBlock object.
		/// Height array of PopData object must match the height array
		///   of this PopDataBlock object.
		/// </summary>
		/// <param name="data">Source PopData object</param>
		public void Add(PopData data) {
			if (!DataIsMatch(data)) {
				throw new ArgumentException("PopDataBlock: Add data type does not match data in block");
			}

			if (Hts == null) {
				initialize(data);
				
				_initialize(data.Ht,
							data.Hdr[(int)PopData.HdrId.Az],
							data.Hdr[(int)PopData.HdrId.Elev],
							data.Hdr[(int)PopData.HdrId.Pwclk],
							data.Hdr[(int)PopData.HdrId.PCode],
							data.Hdr[(int)PopData.HdrId.Nrx]);
				_defaultThreshold = data.DefaultSNRThreshold;
				
			}

			float[,,] vel = new float[data.NRx, data.NSets, data.NHts];
			Array.Copy(data.Vel,vel,vel.Length);
			VelBlock.Add(vel);

			float[,,] snr = new float[data.NRx, data.NSets, data.NHts];
			Array.Copy(data.Snr,snr,snr.Length);
			SnrBlock.Add(snr);

			float[,,] wid = new float[data.NRx, data.NSets, data.NHts];
			Array.Copy(data.Width,wid,wid.Length);
			WidthBlock.Add(wid);

			float[,,] noise = new float[data.NRx, data.NSets, data.NHts];
			Array.Copy(data.Noise,noise,noise.Length);
			NoiseBlock.Add(noise);

			Times.Add(data.TimeStamp);
		}

		/// <summary>
		/// Extracts x,y,z arrays (time, height, data value)
		/// for a particular data type 
		/// </summary>
		/// <param name="tData"></param>
		/// <param name="hData"></param>
		/// <param name="zData"></param>
		/// <param name="type"></param>
		/// <param name="snrThreshold"></param>
		/// <param name="setIndex"></param>
		public void GetTHZArray(out DateTime[] tData, out double[] hData, out double[,] zData,
		                        PopData.PopDataType type, int snrThreshold, int setIndex) {

			if ((setIndex < 0) || (setIndex > this.NSets-1)) {
				throw new ArgumentException("PopDataBlock.GetTHZArray: Set number does not exist in data");
			}

			int ptCount = this.Ntimes;
			int htCount = this.Nhts;
			hData = new double[htCount];
			tData = new DateTime[ptCount];
			zData = new double[ptCount, htCount];

			// default index ranges
			int firstHtIndex = 0;
			int lastHtIndex = htCount-1;
			int firstTimeIndex = 0;
			int lastTimeIndex = ptCount-1;

			bool filterOn = false;
			float filterValue = float.MinValue;

			if (snrThreshold > -100.0) {
				filterOn = true;
				filterValue = (float)snrThreshold;
			}

			if (type == PopData.PopDataType.SNR) {
				if ((filterOn) && (filterValue < -59.0)) {
					// contour plot blows up if "bad" values of -60 dB are plotted
					filterValue = -59.0f;
				}
			}

			// set up height and time arrays
			for (int iht = firstHtIndex; iht <= lastHtIndex; iht++) {
				hData[iht] = this.Hts[iht];
			}

			for (int ipt = firstTimeIndex; ipt <= lastTimeIndex; ipt++) {
				tData[ipt] = (DateTime)(this.Times[ipt]);
			}

			// calculate z array
			if (type == PopData.PopDataType.NoiseLevel) {
				// get noise level without filtering
				for (int i=0; i<ptCount; i++) {				
					float[,] noiseArray;
					noiseArray = (float[,])NoiseBlock[i];
					for (int iht = firstHtIndex; iht <= lastHtIndex; iht++) {
						zData[i,iht] = noiseArray[setIndex,iht];
					}
				}
			}
			else {
				// get other data types with SNR filtering
				for (int i=0; i<ptCount; i++) {				
					float[,] snrArray;
					float[,] dataArray;
					snrArray = (float[,])SnrBlock[i];

					if (type == PopData.PopDataType.Velocity) {
						dataArray = (float[,])VelBlock[i];
					}
					else if (type == PopData.PopDataType.SNR) {
						dataArray = (float[,])SnrBlock[i];
					}
					else if (type == PopData.PopDataType.Width) {
						dataArray = (float[,])WidthBlock[i];
					}
					else {
						throw new ArgumentException("StackedPlot: unexpected plot data type.");
					}

					for (int iht = firstHtIndex; iht <= lastHtIndex; iht++) {
						if (filterOn && (snrArray[0,iht] < filterValue)) {
							zData[i,iht] = double.MaxValue;
						}
						else {
							zData[i,iht] = (dataArray[setIndex,iht]);
						}
					}
				}  // end for i
			}  // end else
		}  // end PopDataBlock.GetTHZArray()


		// ///////////////////////////
		// Public properties

		public int Nhts {
			get {
				if (Hts != null) {
					return Hts.Length;
				}
				else {
					return 0;
				}
			} 
		}

		public int Ntimes {
			get {
				if (Times != null) {
					return Times.Count;
				}
				else {
					return 0;
				}
			}
		}
		public float NRx {
			get {
				return _nrx;
			}
		}
		public float NPts {
			get {
				return _npts;
			}
		}
		public float NSets {
			get {
				return _nsets;
			}
		}
		public float Azimuth {
			get {
				return _azimuth;
			}
		}
		public float Elevation {
			get {
				return _elevation;
			}
		}

		public double DefaultSNRThreshold {
			get {
				return _defaultThreshold;
			}
		}

		public int StationID {
			get {
				return _stationID;
			}
		}

		public string StationLabel {
			get {
				return _stationLabel;
			}
		}
	}
	*/

}


