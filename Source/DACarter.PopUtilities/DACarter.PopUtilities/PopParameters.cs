using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using DACarter.Utilities;

namespace DACarter.PopUtilities {

	public class PopParameters {

        //
        // 
        #region Static Methods
        #endregion Static Methods
        //
        //


        //
		//
		#region Public Fields
		//
		// Change this number everytime a change
		// is made to the definition of this class
		public double PopParametersVersion = 20151208;
		public string Source;		// the file or program that created this parameter set

		public ArrayDimensions ArrayDim;
		public SystemParameters SystemPar;
		public MeltingLayerParameters MeltingLayerPar;
		public ReplayMode ReplayPar;
		public ModeExcludeIntervalsStruct ExcludeMomentIntervals;
        public SignalPeakSearchRangeStruct SignalPeakSearchRange;
        public DebugOptions Debug;

		public static readonly double MperNsVac = 0.149896229;  // meters per nsec delay (vaccuum)
		public static readonly double MperNsAir = 0.149852322;  // meters per nsec delay (air at STP)
		public static readonly double MperNs = MperNsVac;		// for backward consistancy, use vaccuum value for now

		//
		#endregion Public Fields
		//
		//


		//
		#region Structure Definitions

		public enum TypeOfRadar {
            Unknown = 0,
			PulsedTx = 1,
			FmCwDop = 2,
            FmCwSA = 4,
            FmCw = 6       // can be used to check for bits 2,4; otherwise do not use
		}

		public enum WindowType {
			Riesz,
			Hanning,
			Hamming,
			Blackman,
			Rectangular
		}

        public struct DebugOptions {
            public bool NoHardware;
            public bool NoPbx;
            public bool DebugToFile;
            public bool UseAllocator;
            public bool SaveFilteredTS;
            public bool DoParallelTasks;
        }

		public struct ReplayMode {
			public bool Enabled;
			public string InputFile;
			public bool ProcessTimeSeries;		// compute new spectra from (Doppler) timeseries data
			public bool ProcessSpectra;			// compute new moments from spectral data
            // additional options POPN3
            public bool ProcessRawSamples;
            public bool ProcessMoments;
            public bool ProcessXCorr;           // recompute velocities from recorded XCorrelations
            public bool UseFMCWNSpecOnReplay;  // for replay with timeseries processing, instead of recorded NSpec
            public int NumberRecordsAtOnce;    // number of records to read consecutively for one replay "dwell"
            // ////////////////////////
			public int TimeDelayMs;
            public int StartDay;
            public int EndDay;
			[XmlIgnore]public TimeSpan StartTime;
			[XmlIgnore]public TimeSpan EndTime;
			// the following nonsense is required
			//   because .NET cannot serialize TimeSpan
			//   so we convert it to a string
			[XmlElement("StartTime", DataType = "duration")]
			public string XmlStartTime {
				get {
					return StartTime.ToString();
					//return StartTime.Hours.ToString("00")+":"+StartTime.Minutes.ToString("00");
				}
				set { StartTime = TimeSpan.Parse(value); }
			}
			[XmlElement("EndTime", DataType = "duration")]
			public string XmlEndTime {
				get {
					return ((int)(EndTime.TotalHours)).ToString("00") + ":" + EndTime.Minutes.ToString("00");
				}
				set {
					try {
						EndTime = TimeSpan.Parse(value);
					}
					catch (Exception e) {
						EndTime = new TimeSpan(24, 0, 0);
					}

				}
			}
		}


		public struct ArrayDimensions {
			public int MAXBEAMS;			// default for backwards compatibility with POP4 = 10
			public int MAXBEAMPAR;			// default = 4
			public int MAXDIRECTIONS;		// default = 9
			public int MAXBW;				// default = 4
			public int MAXCNSMODES;			// default = 2
			public int MAXOUTPUTFILES;
            public int MAXRXID;
		}

		public struct SystemParameters 	{
			public string StationName;			// station name 
			public double Latitude, Longitude;	// N latitude, E longitude 
			public int MinutesToUT;				// # minutes add to system time to get UT 
			public int Altitude;				// altitude above sea level, meters 
			public int NumberOfRadars;			// number of radars at this station 
			public RadarParameters RadarPar;	// radar id structure for each radar 
		}

		public struct RadarParameters	{
            private int _numOtherInstruments;
            public string RadarName;			// name of radar 
			public int RadarID;					// ID code number for radar 
			public TypeOfRadar RadarType;		// Pulsed, FMCW, etc.
			public double TxFreqMHz;			// tx freq in Mhz 
            public double AntSpacingM;          // spacing between centers of spaced antennas (m)
            public double ASubH;
			public double MaxTxDutyCycle;		// max duty cycle 
			public int MaxTxLengthUsec;			// max Tx pulse length (microsec) 
			public double MinIppUsec;			// minimum IPP in usec
			public bool TxIsOn;					// tx pulse on or off  
			//public int NumberOfDirections;		// number of allowable directions 
			//public int NumberOfBeams;			// number of beam positions chosen 
			//public int NumberOfParameterSets;	// number of beam parameter sets chosen 
			public BeamPosition[] BeamSequence;	// array of chosen beam positions [MAXBM]
			public BeamParameters[] BeamParSet;	// array of beam position parameter sets [MAXBMPAR]
			public Direction[] BeamDirections;	// array of allowable directions [MAXDIR]
			public PbxConstants PBConstants;	// pulse box constants for this radar 
			public RxBwParameters[] RxBw;		// matched pulsewidths (nsec) for rx bandwidth [MAXBW]
												//		plus total extra delay for each rx bw 
			public ProcessingParameters ProcPar;
			public FmCwParameters[] FmCwParSet;
            // POPREV added powermeter 20130502
            public PowerMeterParameters PowMeterPar;

            // POPREV 4.15 Other instrument readings added 20160126
            public int NumOtherInstruments; /* {
                
                get { return _numOtherInstruments; }
                set {
                    _numOtherInstruments = value;
                    // reallocate code array
                    OtherInstrumentCodes = null;
                    if (_numOtherInstruments > 0) {
                        OtherInstrumentCodes = new int[_numOtherInstruments];
                    }
                }
            }
                                             * */
            public int[] OtherInstrumentCodes;
		}

        public struct PowerMeterParameters {
            public bool Enabled;
            public double OffsetDB;
            public int WriteIntervalSec;
        }

		public struct BeamPosition {
			[XmlAttributeAttribute]
			public int i;
			public int ParameterIndex;			// parameter set index, 0 to (NUMPAR-1) 
			public int DirectionIndex;			// direction index, dir_[idir], 0 to (NUMDIR-1) 
			public int NumberOfReps;			// number of repetitions (records) at this position 
		}

		public struct BeamParameters { 
			[XmlAttributeAttribute]
			public int i;
            // dac with rev 2.12 Ipp changed from int to double
            public double IppMicroSec;
            public int PulseWidthNs;		
			public int SampleDelayNs, SpacingNs;	// all values in nanosec 
			public int NHts, NCI, NSpec, NPts;
			public int SystemDelayNs;			// delay thru rx in nanosec 
			public int BwCode;					// rx bandwidth switch code 0-3 
			public int AttenuatedGates;			// # range gates to attenuate 
			public int NCode;					// ncode = # bits in pulse code 
			public bool TxPhaseFlipIsOn;
		}

		public struct FmCwParameters {
			[XmlAttributeAttribute]
			public int i;
			public double IppMicroSec;
			//public double TxSweepFreqIntervalHz;
			//public double TxSweepTimeIntervalUSec;
			public double TxSweepCenterFreqMHz;
			public double TxSweepRateHzUSec;
			public int TxSweepTimeStepClocks;
            public bool IsRadioButtonFreqOffset;
            public double TxSweepOffsetHz;
            public double GateOffset;
            public int TxSweepSampleNPts;
			public int TxSweepSampleNSpec;
			public int TxSweepSampleSpacingNs;
			public int TxSweepSampleDelayNs;
			public bool TxSweepSampleOverlap;
			public WindowType TxSweepSampleWindow;
            public bool TxSweepSampleDcFilter;
            public bool TxSweepSampleDcFilter2;
            public double TxSweepBeyondSamplesUs;
            public bool PostBlankIsAuto;
            public int DopplerNPts;
			public int DopplerNSpec;
			public bool DopplerOverlap;
			public WindowType DopplerWindow;
			public bool DopplerDcFilter;
			public int DopplerNCI;
            public bool SelectGatesToKeep;
			public int DopplerKeepGateFirst, DopplerKeepGateLast;
			public int RangeOffsetM;
			public bool InputSampleUnitsIsRaw;
			public double InputSampleVoltMax;
			public bool ApplyFilterCorrection;
            public bool UseFilterCoeffs;
            public bool UseFreqResp;
			public string FilterFile;
			public bool AD9959Enabled;
			public double DDSRefClockMHz;
			public int DDSMultiplier;
            public double DDS3FreqStartHz, DDS3FreqEndHz;
            public int DDS3PhaseDeg;
            public double DDS4FreqStartHz, DDS4FreqEndHz;
            public int DDS4PhaseDeg;
            public int XCorrNptsMultiplier;       // POPREV 4.13 multiplier for total npts used in xcorr time series
            public int XCorrMaxLag;     // range of lags (+/-) in XCorr vector
            public int XCorrLineFitPts; // total pts at center of XCorr used for line fit
            public double XCorrFilterFraction;  // fraction of spectral pts allowed thru lowpass filter
            public bool XCorrUseFFT;    // use FFT method to compute xcorr
            public int XCorrPolyFitOrder;
            public bool XCorrAdjustBase;        // remove baseline from correlations before wind analysis?
            public int XCorrLagsToInterpolate;   // must be zero or odd number
            public int XCorrLagsToCurveFit;      // total number of lags to fit curve to (must be odd)
        }

		public struct Direction {
			[XmlAttributeAttribute]
			public int i;
			public string Label;				// 10-character direction label name 
			public double Azimuth, Elevation;   // beam azimuth and elevation in degrees 
			public int SwitchCode;				// beam direction switch code for beam steering 
		};

		public struct PbxConstants {
			public int PBPreTR;					// all times in nanosec 
			public int PBPostTR;
			public int PBSynch;
			public int PBPreBlank;
			public int PBPostBlank;
			public int PBClock;
		}

		public struct ProcessingParameters {						// structure for proccessing parameters 
			public bool IsDcFiltering, IsWindowing;
			//public int DComit;				// # pts omitted around dc (obsolete) 
			//public int Omithts;				// # hts to apply dcomit 
			// NOTE: when NPTS was allowed to vary with par # 
			// Dop0...Dop2 should have been made an array
			public int Dop0, Dop1;				// start and interval pt # for moments (1-NPTS) 
			public int Dop2, Dop3;				// start and interval pt # for second moments 
			public int[] RassSourceParams;		// parameters for rass acoustic source[6]   
			public bool RemoveClutter;
			public double MaxClutterHtKm;		// max ht for clutter removal; 
            public bool KeepOriginalSpectra;    // do not store spectra with ground clutter zeroed out
            public double GCTimesBigger;        // GC algorithm parameter
            public double GCPercentLess;
            public bool GCRestrictExtent;       // restrict GC extent to prev (above) signal boundary
            public bool GCRestrictIfDcInPrev;   // don't search go GC if prev (above) signal covers DC
            public double GCMinSigThldDB;       // min signal at DC required to turn off GC below (dB above ACR thld)
            public int GCMethod;                // 0 = set GC to noise; 1 = interpolate; 
            public bool GCDebugLog;
			public bool IsIcraAvg;				// was Specavg: <1 for MEAN, ==1 for H&S spectral averaging  
			public int NumberOfRx;				// # multiplexed interferometer receivers 
            public RxIDParameters[] RxID;
			//public int NumberOfMetInst;			// # met instruments -- 941004 DAC 
			public bool IsWritingPopFile;
			public string PopFilePathName;
			public ConsensusParameters[] CnsPar;// Consensus averaging parameters for vert and oblique
			//public int Misc;					// space saver for future use[NMISC], keep total = 80b 
			public PopFileParameters[] PopFiles;	// Info for output data files
            public int NSpecAtATime;            // number of spectra for which to accquire raw data samples at one time before processing
            public bool AllocTSOnly;            // if not saving processed products to data file, only do time series
            public bool DoClutterWavelet;
            public bool DoDespikeWavelet;
            public bool DoHarmonicWavelet;
            public double WaveletClutterThldMed;    // clipping threshold relative to median value of wavelet segment
            public double WaveletDespikeThldMed;    // clipping threshold relative to median value of wavelet segment
            public double WaveletClutterCutoffMps;  // upper limit of clutter filter Doppler region
            public double WaveletClutterMaxHt;      // upper limit in height to apply this filter to
            public bool DoAutoCorr1Rx;              // compute autocorr of time series even for single rx systems
        }

		public struct RxBwParameters {
			[XmlAttributeAttribute]
			public int i;
			//public int BwSwitchCode;	// switch code is the index 0-3
			public int BwPwNs;		// Matching PW for this BW
			public int BwDelayNs;	// delay thru rx in nanosec
		}

		public struct ConsensusParameters {
			[XmlAttributeAttribute]
			public int i;
			public bool CnsEnable;
			public bool CnsIsVertical;			// These parameters apply to vertical or oblique beams?
			public bool CnsIsRass;				// These parameters apply to RASS modes?
			public int CnsAvgTimeMin;			// consensus averaging time in minutes
			public int CnsThldPercent;			// percent of records required to consense
			public double CnsWindow;			// width of window in m/s
			public bool CnsIsVertCorrection;	// adjust computed horizontal winds by vertical component?
			public bool CnsWriteToFile;
			public string CnsFilePath;
			public bool CnsUseTriads;
			public int CnsTriadBeams;
            public int ReplayBeamMode;      // typically 3 or 5, to specify actual beam sequence when replaying
		}

        public struct RxIDParameters {
            [XmlAttributeAttribute]
            public int iRx;           // Rx index, i.e. i=0 is Rx1; also order in RxID array
            public string RxIDName;
            public int iSampleOrder;    // position of this receiver in sampling sequence, i=0 is first sampled
        }

		public struct PopFileParameters {
			[XmlAttributeAttribute]
			public int i;
			public bool FileWriteEnabled;
            public bool IncludeSpectra;
            public bool IncludeXCorr;
            public bool IncludeACorr;
            public bool IncludeMoments;
			public bool IncludeSingleTS;
			public bool IncludeFullTS;
            public bool WriteSingleTSTextFile;
            public bool WriteFullTSTextFile;
            public bool WriteRawTSTextFile;
			public bool WriteRawTSFile;
			public bool WriteHourlyFiles;		// if false then day files
			public bool WriteModeOverwrite;		// if false then append data to file
			public bool UseLapxmFileName;		// if false then use POP name convention
			public string FileNameSuffix;		// single letter following date in name
			public string FileNameSite;			// 3-letter site in LapXM name
			public string FileFolder;
            public string LogFileFolder;
		}

		public struct MeltingLayerParameters {
			public bool Enable;
			public int CalculateEveryMinute;
			public int MinHeightM;
			public int MaxHeightM;
			public int MinSnrDvvPairs;		// min instances where both SNR>= MinSnrRain and DVV>=2.5
			public int MinSnrRain;			// min range corrected SNR value used in MinSnrDvPairs
			public double DeltaSnrBb;		// SNR diff value used in vertical bright band evaluation
			public double DeltaDvvBb;
			public double MinSnrBb;
			public int DvvBbOnlyMaxHeightM;	// max altitude below which to use the DvvBbOnly test
			public double DvvBbOnlyMinSnr;
			public int GateSpacingResolution;	// num gates to include in the bright band search
			public int BrightBandPercent;		// min % total BB regions/total number of verticals
			public int AcceptHeightRangeM;		// range around median altitude in which BB must exist to be included
			public int AcceptPercent;			// min % accepted BB regions/total BB regions
			public int QcMaxRainAboveBb;		// max number of gates with rain that can be above melting layer altitude
			public string OutputFileFolder;
			public bool WriteHourlyFiles;
			public string LogFileFolder;
			public bool UseRangeCorrectedMinSnr;
			public double RangeCorrectedMinSnrOffset;	// snr threshold = (offset) + 20 log10(r)
			public bool UseDataRegionOnly;		// data outside this region is removed:
			public int MinDataHtM;				// SNR and DVV data below this is removed.
			public int MaxDataHtM;				// SNR and SVV data above this is removed.
			public bool IncludeHeader;			// write header in log file?
			public bool WriteLogFile;
			public bool SkipNarrowWidths;
			public double NarrowWidthMS;
            public bool ModifyNoiseLevels;
            //public int NumUpperHtsForNoise;     // number of upper hts to use as noise level for all hts
            public int NoiseGateLoIndex, NoiseGateHiIndex;   // range of gates to use for noise level for all hts

		}

		public struct ModeParameters {
			public double Azimuth;
			public double Elevation;
			public int PulseWidthNs;
			public int NCode;
		}

		public struct MomentExcludeInterval {
			[XmlAttributeAttribute]
			public int i;
			public int HtLowM;
			public int HtHighM;
			public double VelLowMS;
			public double VelHighMS;
		}

		public struct ModeExcludeIntervals {
			[XmlAttributeAttribute]
			public int i;
			public string Label;
			public ModeParameters Mode;
			public MomentExcludeInterval[] MomentExcludeIntervals;
		}

		public struct ModeExcludeIntervalsStruct {
			public bool Enabled;
			public ModeExcludeIntervals[] AllModesExcludeIntervals;
		}

        public struct SignalPeakSearchRangeStruct {
            public bool Enabled;
            public double VelLowMS;
            public double VelHighMS;
        }

        public static int PopFileDim {
            get { return _popFileDim; }
        }


		#endregion Structure Definitions
		//

        //private int _numOtherInstruments;

        // default parameters (Max sizes that user can select)
        private static int _beamParDim = 4;		// default for backwards compatibility with POP4 = 4
        private static int  _beamDim = 10;		// default = 10
	    private static int _dirDim = 9;			// default = 9
		private static int _bwDim = 4;			// default = 4
		private static int _cnsDim = 2;
		private static int _popFileDim = 6;     // first 2 are specified on setup screen; other 4 are for cross- and auto-correlations in place of spectra
        private static int _rxIDDim = 3;
        
        //
		#region Constructors
		//
		/// <summary>
		/// Public constructor for PopParameters
		/// </summary>
		/// 

		public PopParameters() {
			Init(_beamDim, _beamParDim, _dirDim, _bwDim, _cnsDim, _popFileDim, _rxIDDim);
		}

		public PopParameters(int BeamDim, int BeamParDim, int DirDim, int BwDim, int CnsDim, int PFileDim, int RxIDDim) {
			Init(BeamDim, BeamParDim, DirDim, BwDim, CnsDim, PFileDim, RxIDDim);
		}

		private void Init(int BeamDim, int BeamParDim, int DirDim, int BwDim, int CnsDim, int FileDim, int RxIDDim) {

			// initialize dimensions
			ArrayDim.MAXBEAMPAR = BeamParDim;
			ArrayDim.MAXBEAMS = BeamDim;			
			ArrayDim.MAXDIRECTIONS = DirDim;		
			ArrayDim.MAXBW = BwDim;			
			ArrayDim.MAXCNSMODES = CnsDim;
			ArrayDim.MAXOUTPUTFILES = FileDim;
            ArrayDim.MAXRXID = RxIDDim;
            //SystemPar.RadarPar.OtherInstruments.NumOtherInstruments = 0;   // this one can be set to any value after constructions
            SystemPar.RadarPar.NumOtherInstruments = 0;   // this one can be set to any value after constructions
            //
			// allocate arrays
			SystemPar.RadarPar.BeamParSet = new BeamParameters[BeamParDim];
			SystemPar.RadarPar.FmCwParSet = new FmCwParameters[BeamParDim];
			SystemPar.RadarPar.BeamSequence = new BeamPosition[BeamDim];
			SystemPar.RadarPar.BeamDirections = new Direction[DirDim];
			SystemPar.RadarPar.RxBw = new RxBwParameters[BwDim];
			SystemPar.RadarPar.ProcPar.RassSourceParams = new int[6];
			SystemPar.RadarPar.ProcPar.CnsPar = new ConsensusParameters[CnsDim];
			SystemPar.RadarPar.ProcPar.PopFiles = new PopFileParameters[FileDim];
            SystemPar.RadarPar.ProcPar.RxID = new RxIDParameters[RxIDDim];

			SystemPar.RadarPar.ProcPar.IsWritingPopFile = false;
			SystemPar.NumberOfRadars = 1;

			// Fill strings to make sure there is an entry
			//	in the XML file, for manual editing.
			SystemPar.RadarPar.ProcPar.PopFilePathName = "_";
			SystemPar.StationName = "_";
			SystemPar.RadarPar.RadarName = " ";
			for (int i = 0; i < DirDim; i++) {
				SystemPar.RadarPar.BeamDirections[i].Label = "_";
				SystemPar.RadarPar.BeamDirections[i].i = i;
			}
            for (int i = 0; i < CnsDim; i++) {
                SystemPar.RadarPar.ProcPar.CnsPar[i].CnsFilePath = "_";
                SystemPar.RadarPar.ProcPar.CnsPar[i].i = i;
            }
            for (int i = 0; i < RxIDDim; i++) {
                SystemPar.RadarPar.ProcPar.RxID[i].RxIDName = "_";
                SystemPar.RadarPar.ProcPar.RxID[i].iRx = i;
            }
            for (int i = 0; i < FileDim; i++) {
                SystemPar.RadarPar.ProcPar.PopFiles[i].FileFolder = "_";
                SystemPar.RadarPar.ProcPar.PopFiles[i].LogFileFolder = "_";
                SystemPar.RadarPar.ProcPar.PopFiles[i].FileNameSite = "_";
				SystemPar.RadarPar.ProcPar.PopFiles[i].FileNameSuffix = "_";
				SystemPar.RadarPar.ProcPar.PopFiles[i].i = i;
			}

			// attribute i in some arrays is used only as a 
			//	visual help for viewing XML file visually
			for (int i = 0; i < BeamParDim; i++) {
				SystemPar.RadarPar.BeamParSet[i].i = i;
				SystemPar.RadarPar.FmCwParSet[i].i = i;
			}
			for (int i = 0; i < BeamDim; i++) {
				SystemPar.RadarPar.BeamSequence[i].i = i;
			}
			for (int i = 0; i < BwDim; i++) {
				SystemPar.RadarPar.RxBw[i].i = i;
			}

			SystemPar.RadarPar.FmCwParSet[0].FilterFile = "_";

			MeltingLayerPar.OutputFileFolder = "_";
		}

		#endregion Constructors
		//

		//
		#region Public Methods
		//
		public void WriteToFile(string filePath) {
			StreamWriter ParFileWriter;
			try {
				ParFileWriter = new StreamWriter(filePath);
				XmlSerializer ParFileSerializer = new XmlSerializer(typeof(PopParameters));
				ParFileSerializer.Serialize(ParFileWriter, this);
				ParFileWriter.Close();
			}
			catch (Exception e) {
                try {
                    // TODO change messagebox to status error message
                    MessageBoxEx.Show(e.Message, "Error writing parx file", 8000);
                }
                catch {
                }
			}
		}

		public static PopParameters ReadFromFile(string filePath) {
			PopParameters newPar;
			StreamReader reader = new StreamReader(filePath);
			XmlSerializer ParFileSerializer = new XmlSerializer(typeof(PopParameters));
			newPar = (PopParameters)ParFileSerializer.Deserialize(reader);
			reader.Close();

            // POPREV 3.22 added for increase in size of popfiles array
            if (newPar.SystemPar.RadarPar.ProcPar.PopFiles.Length < PopParameters.PopFileDim) {
                newPar.ArrayDim.MAXOUTPUTFILES = PopParameters.PopFileDim;
                newPar = newPar.DeepCopy();
            }

			// make sure parameter set from file is compatible with current version:
			PopParameters updatedPar = newPar.DeepCopy();
			//updatedPar.Source = "Copy of " + filePath;

			// testing
			//newPar.SystemPar.RadarPar.BeamDirections[0].Label = "Direction1";
			//newPar.SystemPar.RadarPar.BeamSequence[0].NumberOfReps = 99;

			return updatedPar;
		}

		/// <summary>
		/// Make a deep copy of this PopParameters object.
		/// Deep copy means that all elements including arrays
		/// are distinct, independent objects in the original and the copy.
		/// NOTE: While making this copy, the new object is
		/// updated so that the array sizes match the values 
		/// in the ArrayDim structure.  Thus parameter files can be 
		/// redimensioned by changing ArrayDim values and reading
		/// in or copying the file.
		/// </summary>
		/// <param name="old"></param>
		/// <returns></returns>
		public PopParameters DeepCopy() {

			PopParameters copyPar = new PopParameters();

			// Check that original array had arrays properly defined;
			if (this.SystemPar.RadarPar.BeamSequence == null) {
				throw new ApplicationException("BeamSequence Array not defined in parameter file.");
			}
			if (this.SystemPar.RadarPar.BeamParSet == null) {
				throw new ApplicationException("BeamParSet Array not defined in parameter file.");
			}
			if (this.SystemPar.RadarPar.FmCwParSet == null) {
				this.SystemPar.RadarPar.FmCwParSet = new FmCwParameters[this.ArrayDim.MAXBEAMPAR];
				//throw new ApplicationException("FmCwParSet Array not defined in parameter file.");
			}
			if (this.SystemPar.RadarPar.BeamDirections == null) {
				throw new ApplicationException("BeamDirections Array not defined in parameter file.");
			}
			if (this.SystemPar.RadarPar.RxBw == null) {
				throw new ApplicationException("RxBw Array not defined in parameter file.");
			}
            if (this.SystemPar.RadarPar.ProcPar.CnsPar == null) {
                throw new ApplicationException("ProcPar.CnsPar Array not defined in parameter file.");
            }
            if ((this.SystemPar.RadarPar.ProcPar.RxID == null) || (this.SystemPar.RadarPar.ProcPar.RxID.Length == 0)) {
                //throw new ApplicationException("ProcPar.RxID Array not defined in parameter file.");
                this.SystemPar.RadarPar.ProcPar.RxID = new RxIDParameters[1];
                this.ArrayDim.MAXRXID = 1;
            }
            if (this.ExcludeMomentIntervals.AllModesExcludeIntervals == null) {
				//throw new ApplicationException("MomentExcludeIntervals Array not defined in parameter file.");
			}

			if (this.SystemPar.RadarPar.ProcPar.PopFiles == null) {
				this.ArrayDim.MAXOUTPUTFILES = _popFileDim;
				this.SystemPar.RadarPar.ProcPar.PopFiles = new PopFileParameters[this.ArrayDim.MAXOUTPUTFILES];
				//throw new ApplicationException("ProcPar.PopFiles Array not defined in parameter file.");
			}

			// save original array sizes
			// Remember, when we are done here, ArrayDim values must match actual array sizes,
			//		even if the input array was manually edited differently.
			int oldMAXBEAMS = this.SystemPar.RadarPar.BeamSequence.Length;
			int oldMAXBEAMPAR = this.SystemPar.RadarPar.BeamParSet.Length;
			int oldMAXDIRECTIONS = this.SystemPar.RadarPar.BeamDirections.Length;
			int oldMAXBW = this.SystemPar.RadarPar.RxBw.Length;
			int oldMAXCNSMODES = this.SystemPar.RadarPar.ProcPar.CnsPar.Length;
			int oldMAXOUTPUTFILES = this.SystemPar.RadarPar.ProcPar.PopFiles.Length;
            int oldMAXRXID = this.SystemPar.RadarPar.ProcPar.RxID.Length;

			// transfer everything from old to new parameter set (arrays are shared, original size)
			copyPar = this.ShallowCopy();

			// create a dummy parameter set with the arrays properly dimensioned and initialized
			PopParameters dummyPar = new PopParameters(copyPar.ArrayDim.MAXBEAMS,
														copyPar.ArrayDim.MAXBEAMPAR,
														copyPar.ArrayDim.MAXDIRECTIONS,
														copyPar.ArrayDim.MAXBW,
														copyPar.ArrayDim.MAXCNSMODES,
														copyPar.ArrayDim.MAXOUTPUTFILES,
                                                        copyPar.ArrayDim.MAXRXID);

			// create new arrays based on array sizes given in ArrayDim
			copyPar.PopParametersVersion = dummyPar.PopParametersVersion;
			//copyPar.SystemPar.RadarPar.Beams = new BeamPosition[copyPar.ArrayDim.MAXBEAMS];
			copyPar.SystemPar.RadarPar.BeamSequence = dummyPar.SystemPar.RadarPar.BeamSequence;
			//copyPar.SystemPar.RadarPar.BeamParams = new BeamParameters[copyPar.ArrayDim.MAXBEAMPAR];
			copyPar.SystemPar.RadarPar.BeamParSet = dummyPar.SystemPar.RadarPar.BeamParSet;
			copyPar.SystemPar.RadarPar.FmCwParSet = dummyPar.SystemPar.RadarPar.FmCwParSet;
			//copyPar.SystemPar.RadarPar.BeamDirections = new Direction[copyPar.ArrayDim.MAXDIRECTIONS];
			copyPar.SystemPar.RadarPar.BeamDirections = dummyPar.SystemPar.RadarPar.BeamDirections;
			//copyPar.SystemPar.RadarPar.RxBw = new RxBwParameters[copyPar.ArrayDim.MAXBW];
			copyPar.SystemPar.RadarPar.RxBw = dummyPar.SystemPar.RadarPar.RxBw;
			//copyPar.SystemPar.RadarPar.ProcPar.CnsPar = new ConsensusParameters[copyPar.ArrayDim.MAXCNSMODES];
            copyPar.SystemPar.RadarPar.ProcPar.CnsPar = dummyPar.SystemPar.RadarPar.ProcPar.CnsPar;
            copyPar.SystemPar.RadarPar.ProcPar.RxID = dummyPar.SystemPar.RadarPar.ProcPar.RxID;
            copyPar.SystemPar.RadarPar.ProcPar.PopFiles = dummyPar.SystemPar.RadarPar.ProcPar.PopFiles;
/**/
			// clone MomentExcludeIntervals
			if ((this.ExcludeMomentIntervals.AllModesExcludeIntervals != null) &&
							(this.ExcludeMomentIntervals.AllModesExcludeIntervals.Length != 0)) {
				int oldModeExcludeIntervals = this.ExcludeMomentIntervals.AllModesExcludeIntervals.Length;  // doesn't have size in ArrayDim
				copyPar.ExcludeMomentIntervals.AllModesExcludeIntervals = new ModeExcludeIntervals[oldModeExcludeIntervals];
				for (int i = 0; i < oldModeExcludeIntervals; i++) {
					copyPar.ExcludeMomentIntervals.AllModesExcludeIntervals[i] = this.ExcludeMomentIntervals.AllModesExcludeIntervals[i];
					copyPar.ExcludeMomentIntervals.AllModesExcludeIntervals[i].i = i;
					if (this.ExcludeMomentIntervals.AllModesExcludeIntervals[i].MomentExcludeIntervals != null) {
						int intervals = this.ExcludeMomentIntervals.AllModesExcludeIntervals[i].MomentExcludeIntervals.Length;
						copyPar.ExcludeMomentIntervals.AllModesExcludeIntervals[i].MomentExcludeIntervals =
														new MomentExcludeInterval[intervals];
						for (int j = 0; j < intervals; j++) {
							copyPar.ExcludeMomentIntervals.AllModesExcludeIntervals[i].MomentExcludeIntervals[j] = 
														this.ExcludeMomentIntervals.AllModesExcludeIntervals[i].MomentExcludeIntervals[j];
							copyPar.ExcludeMomentIntervals.AllModesExcludeIntervals[i].MomentExcludeIntervals[j].i = j;
						}
					}
				}
			}
			else {
				// create empty MomentExcludeIntervals
				copyPar.ExcludeMomentIntervals.AllModesExcludeIntervals = new ModeExcludeIntervals[1];
				copyPar.ExcludeMomentIntervals.AllModesExcludeIntervals[0].i = 0;
				copyPar.ExcludeMomentIntervals.AllModesExcludeIntervals[0].MomentExcludeIntervals = new MomentExcludeInterval[1];
				copyPar.ExcludeMomentIntervals.AllModesExcludeIntervals[0].MomentExcludeIntervals[0].i = 0;
			}
/**/	
            // clone OtherInstruments array
            int numInst = this.SystemPar.RadarPar.NumOtherInstruments;
            copyPar.SystemPar.RadarPar.NumOtherInstruments = numInst;
            if (numInst > 0) {
                copyPar.SystemPar.RadarPar.OtherInstrumentCodes = new int[numInst];
                for (int i = 0; i < numInst; i++) {
                    copyPar.SystemPar.RadarPar.OtherInstrumentCodes[i] =
                            this.SystemPar.RadarPar.OtherInstrumentCodes[i];
                }
            }
            else {
                copyPar.SystemPar.RadarPar.OtherInstrumentCodes = null;
            }
            
            /*
            numInst = this.SystemPar.RadarPar.OtherInstruments.NumOtherInstruments;
            copyPar.SystemPar.RadarPar.OtherInstruments.NumOtherInstruments = numInst;
            if (numInst > 0) {
                copyPar.SystemPar.RadarPar.OtherInstruments.OtherInstrumentCodes = new int[numInst];
                for (int i = 0; i < numInst; i++) {
                    copyPar.SystemPar.RadarPar.OtherInstruments.OtherInstrumentCodes[i] =
                            this.SystemPar.RadarPar.OtherInstruments.OtherInstrumentCodes[i];
                }
            }
            else {
                copyPar.SystemPar.RadarPar.OtherInstruments.OtherInstrumentCodes = null;
            }
            */

			//resize and clone Beams array
			if (oldMAXBEAMS < copyPar.ArrayDim.MAXBEAMS) {
				for (int i = 0; i < oldMAXBEAMS; i++) {
					copyPar.SystemPar.RadarPar.BeamSequence[i] = this.SystemPar.RadarPar.BeamSequence[i];
				}
			}
			else {
				for (int i = 0; i < copyPar.ArrayDim.MAXBEAMS; i++) {
					copyPar.SystemPar.RadarPar.BeamSequence[i] = this.SystemPar.RadarPar.BeamSequence[i];
				}
			}
			// resize and clone BeamParams array
			if (oldMAXBEAMPAR < copyPar.ArrayDim.MAXBEAMPAR) {
				for (int i = 0; i < oldMAXBEAMPAR; i++) {
					copyPar.SystemPar.RadarPar.BeamParSet[i] = this.SystemPar.RadarPar.BeamParSet[i];
					copyPar.SystemPar.RadarPar.FmCwParSet[i] = this.SystemPar.RadarPar.FmCwParSet[i];
				}
			}
			else {
				for (int i = 0; i < copyPar.ArrayDim.MAXBEAMPAR; i++) {
					copyPar.SystemPar.RadarPar.BeamParSet[i] = this.SystemPar.RadarPar.BeamParSet[i];
					copyPar.SystemPar.RadarPar.FmCwParSet[i] = this.SystemPar.RadarPar.FmCwParSet[i];
				}
			}
			// resize and clone BeamDirections array
			if (oldMAXDIRECTIONS < copyPar.ArrayDim.MAXDIRECTIONS) {
				for (int i = 0; i < oldMAXDIRECTIONS; i++) {
					copyPar.SystemPar.RadarPar.BeamDirections[i] = this.SystemPar.RadarPar.BeamDirections[i];
				}
			}
			else {
				for (int i = 0; i < copyPar.ArrayDim.MAXDIRECTIONS; i++) {
					copyPar.SystemPar.RadarPar.BeamDirections[i] = this.SystemPar.RadarPar.BeamDirections[i];
				}
			}
			// resize and clone RxBw arrays
			if (oldMAXBW < copyPar.ArrayDim.MAXBW) {
				for (int i = 0; i < oldMAXBW; i++) {
					copyPar.SystemPar.RadarPar.RxBw[i] = this.SystemPar.RadarPar.RxBw[i];
				}
			}
			else {
				for (int i = 0; i < copyPar.ArrayDim.MAXBW; i++) {
					copyPar.SystemPar.RadarPar.RxBw[i] = this.SystemPar.RadarPar.RxBw[i];
				}
			}
            // resize and clone CnsPar arrays
            if (oldMAXCNSMODES < copyPar.ArrayDim.MAXCNSMODES) {
                for (int i = 0; i < oldMAXCNSMODES; i++) {
                    copyPar.SystemPar.RadarPar.ProcPar.CnsPar[i] = this.SystemPar.RadarPar.ProcPar.CnsPar[i];
                }
            }
            else {
                for (int i = 0; i < copyPar.ArrayDim.MAXCNSMODES; i++) {
                    copyPar.SystemPar.RadarPar.ProcPar.CnsPar[i] = this.SystemPar.RadarPar.ProcPar.CnsPar[i];
                }
            }
            // resize and clone RxID arrays
            if (oldMAXRXID < copyPar.ArrayDim.MAXRXID) {
                for (int i = 0; i < oldMAXRXID; i++) {
                    copyPar.SystemPar.RadarPar.ProcPar.RxID[i] = this.SystemPar.RadarPar.ProcPar.RxID[i];
                }
            }
            else {
                for (int i = 0; i < copyPar.ArrayDim.MAXRXID; i++) {
                    copyPar.SystemPar.RadarPar.ProcPar.RxID[i] = this.SystemPar.RadarPar.ProcPar.RxID[i];
                }
            }
            // resize and clone PopFile arrays
			if (oldMAXOUTPUTFILES < copyPar.ArrayDim.MAXOUTPUTFILES) {
				for (int i = 0; i < oldMAXOUTPUTFILES; i++) {
					copyPar.SystemPar.RadarPar.ProcPar.PopFiles[i] = this.SystemPar.RadarPar.ProcPar.PopFiles[i];
				}
			}
			else {
				for (int i = 0; i < copyPar.ArrayDim.MAXOUTPUTFILES; i++) {
					copyPar.SystemPar.RadarPar.ProcPar.PopFiles[i] = this.SystemPar.RadarPar.ProcPar.PopFiles[i];
				}
			}

			/*
			// define actual number of beams, sets, and directions that are used.
			int numBeams = 0;
			for (int i = 0; i < copyPar.ArrayDim.MAXBEAMS; i++) {
				if (copyPar.SystemPar.RadarPar.BeamSequence[i].NumberOfReps != 0) {
					numBeams++;
				}
			}
			copyPar.SystemPar.RadarPar.NumberOfBeams = numBeams;
			int numParSets = 0;
			for (int i = 0; i < copyPar.ArrayDim.MAXBEAMPAR; i++) {
				if (copyPar.SystemPar.RadarPar.BeamParSet[i].NHts != 0) {
					numParSets++;
				}
			}
			int numDirections = 0;
			for (int i = 0; i < copyPar.ArrayDim.MAXDIRECTIONS; i++) {
				if (copyPar.SystemPar.RadarPar.BeamDirections[i].Label != "") {
					numDirections++;
				}
			}
			*/

			//copyPar.Source = "Deep Copy";

			return copyPar;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		private PopParameters ShallowCopy() {

			PopParameters newPar = new PopParameters();
			newPar.SystemPar = this.SystemPar;
			newPar.ArrayDim = this.ArrayDim;
			newPar.PopParametersVersion = this.PopParametersVersion;
			newPar.Source = this.Source;
			newPar.MeltingLayerPar = this.MeltingLayerPar;
			newPar.ReplayPar = this.ReplayPar;
            newPar.Debug = this.Debug;
            newPar.SignalPeakSearchRange = this.SignalPeakSearchRange;
			newPar.ExcludeMomentIntervals = this.ExcludeMomentIntervals;
			//newPar.ModeExcludeIntervals.Enabled = this.ModeExcludeIntervals.Enabled;
			//newPar.ModeExcludeIntervals.MomentExcludeIntervalsMode = this.ModeExcludeIntervals.MomentExcludeIntervalsMode;

			return newPar;
		}

        
        public static bool operator ==(PopParameters left, PopParameters right) {
            if ((object)left == null && (object)right == null) {
                return true;
            }
            if ((object)left == null || (object)right == null) {
                return false;
            }

            return left.Equals(right);
        }

        public static bool operator !=(PopParameters left, PopParameters right) {
            return !(left == right);
        }
         


		/// <summary>
		/// Returns true only if the 2 parameter objects
		///		contain the same parameters
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		/// <remarks>
		/// When overriding Equals() then we should also
		///		override GetHashCode() --
		///		otherwise using these objects in a
		///		Hashtable will not work correctly.
		/// We have not done this yet.
		/// NOTE: We can equate structures directly unless
		///		they contain array members. Then we must
		///		equate the elements of the arrays individually.
        ///	NOTE NOTE: defining Equals() does not override == operatior,
        ///	    need to override == and != above
		/// </remarks>
        
		public override bool Equals(object obj) {

			PopParameters rhs = obj as PopParameters;
			if (rhs == null) {
				return false;
			}

			try {
				//
				if ((this.Source != rhs.Source) ||
					(this.PopParametersVersion != rhs.PopParametersVersion)) {
					return false;
				}
				// check array sizes and stored array dims
				int thisBSLen = this.SystemPar.RadarPar.BeamSequence.Length;
				int thisBPSLen = this.SystemPar.RadarPar.BeamParSet.Length;
				int thisFMPSLen = this.SystemPar.RadarPar.FmCwParSet.Length;
				int thisBDLen = this.SystemPar.RadarPar.BeamDirections.Length;
				int thisRBLen = this.SystemPar.RadarPar.RxBw.Length;
				int thisCPLen = this.SystemPar.RadarPar.ProcPar.CnsPar.Length;
                int thisRXLen = this.SystemPar.RadarPar.ProcPar.RxID.Length;
				int thisPFLen = this.SystemPar.RadarPar.ProcPar.PopFiles.Length;
				int thatBSLen = rhs.SystemPar.RadarPar.BeamSequence.Length;
				int thatBPSLen = rhs.SystemPar.RadarPar.BeamParSet.Length;
				int thatFMPSLen = rhs.SystemPar.RadarPar.FmCwParSet.Length;
				int thatBDLen = rhs.SystemPar.RadarPar.BeamDirections.Length;
				int thatRBLen = rhs.SystemPar.RadarPar.RxBw.Length;
				int thatCPLen = rhs.SystemPar.RadarPar.ProcPar.CnsPar.Length;
                int thatRXLen = rhs.SystemPar.RadarPar.ProcPar.RxID.Length;
				int thatPFLen = rhs.SystemPar.RadarPar.ProcPar.PopFiles.Length;
				if ((thisBSLen != thatBSLen) ||
					(thisBPSLen != thatBPSLen) ||
					(thisFMPSLen != thatFMPSLen) ||
					(thisBDLen != thatBDLen) ||
					(thisRBLen != thatRBLen) ||
                    (thisPFLen != thatPFLen) ||
                    (thisRXLen != thatRXLen) ||
                    (thisCPLen != thatCPLen)) {
					return false;
				}
				if ((this.ArrayDim.MAXBEAMPAR != rhs.ArrayDim.MAXBEAMPAR) ||
					(this.ArrayDim.MAXBEAMS != rhs.ArrayDim.MAXBEAMS) ||
					(this.ArrayDim.MAXBW != rhs.ArrayDim.MAXBW) ||
                    (this.ArrayDim.MAXCNSMODES != rhs.ArrayDim.MAXCNSMODES) ||
                    (this.ArrayDim.MAXRXID != rhs.ArrayDim.MAXRXID) ||
                    (this.ArrayDim.MAXOUTPUTFILES != rhs.ArrayDim.MAXOUTPUTFILES) ||
					(this.ArrayDim.MAXDIRECTIONS != rhs.ArrayDim.MAXDIRECTIONS)) {
					return false;
				}

				// check SystemPar
				if ((this.SystemPar.Altitude != rhs.SystemPar.Altitude) ||
					(this.SystemPar.Latitude != rhs.SystemPar.Latitude) ||
					(this.SystemPar.Longitude != rhs.SystemPar.Longitude) ||
					(this.SystemPar.MinutesToUT != rhs.SystemPar.MinutesToUT) ||
					(this.SystemPar.NumberOfRadars != rhs.SystemPar.NumberOfRadars) ||
					(this.SystemPar.StationName != rhs.SystemPar.StationName) ) {
					return false;
				}

				// check RadarPar
				if ((this.SystemPar.RadarPar.RadarID != rhs.SystemPar.RadarPar.RadarID) ||
					(this.SystemPar.RadarPar.RadarName != rhs.SystemPar.RadarPar.RadarName) ||
					(this.SystemPar.RadarPar.RadarType != rhs.SystemPar.RadarPar.RadarType) ||
                    (this.SystemPar.RadarPar.TxFreqMHz != rhs.SystemPar.RadarPar.TxFreqMHz) ||
                    (this.SystemPar.RadarPar.AntSpacingM != rhs.SystemPar.RadarPar.AntSpacingM) ||
                    (this.SystemPar.RadarPar.ASubH != rhs.SystemPar.RadarPar.ASubH) ||
                    (this.SystemPar.RadarPar.TxIsOn != rhs.SystemPar.RadarPar.TxIsOn) ||
					(this.SystemPar.RadarPar.MaxTxDutyCycle != rhs.SystemPar.RadarPar.MaxTxDutyCycle) ||
					(this.SystemPar.RadarPar.MaxTxLengthUsec != rhs.SystemPar.RadarPar.MaxTxLengthUsec) ||
					(this.SystemPar.RadarPar.MinIppUsec != rhs.SystemPar.RadarPar.MinIppUsec) ||
					//(this.SystemPar.RadarPar.NumberOfBeams != rhs.SystemPar.RadarPar.NumberOfBeams) ||
					//(this.SystemPar.RadarPar.NumberOfDirections != rhs.SystemPar.RadarPar.NumberOfDirections) ||
					//(this.SystemPar.RadarPar.NumberOfParameterSets != rhs.SystemPar.RadarPar.NumberOfParameterSets) ||
					(!this.SystemPar.RadarPar.PBConstants.Equals(rhs.SystemPar.RadarPar.PBConstants)) ||
                    (!this.SystemPar.RadarPar.PowMeterPar.Equals(rhs.SystemPar.RadarPar.PowMeterPar)) )  {
					return false; 
				}

				// check BeamSequence
				for (int i = 0; i < this.ArrayDim.MAXBEAMS; i++) {
					if (!this.SystemPar.RadarPar.BeamSequence[i].Equals(rhs.SystemPar.RadarPar.BeamSequence[i])) {
						return false;
					}
				}

				// check BeamParSet
				for (int i = 0; i < this.ArrayDim.MAXBEAMPAR; i++) {
					if (!this.SystemPar.RadarPar.BeamParSet[i].Equals(rhs.SystemPar.RadarPar.BeamParSet[i])) {
						return false;
					}
				}

				// check FmCwParSet
				for (int i = 0; i < this.ArrayDim.MAXBEAMPAR; i++) {
					if (!this.SystemPar.RadarPar.FmCwParSet[i].Equals(rhs.SystemPar.RadarPar.FmCwParSet[i])) {
						return false;
					}
				}

				// check BeamDirections
				for (int i = 0; i < this.ArrayDim.MAXDIRECTIONS; i++) {
					if (!this.SystemPar.RadarPar.BeamDirections[i].Equals(rhs.SystemPar.RadarPar.BeamDirections[i])) {
						return false;
					}
				}

				// check RxBw
				for (int i = 0; i < this.ArrayDim.MAXBW; i++) {
					if (!this.SystemPar.RadarPar.RxBw[i].Equals(rhs.SystemPar.RadarPar.RxBw[i])) {
						return false;
					}
				}

                // check OtherInstrumentCodes
                int rhsNumber = rhs.SystemPar.RadarPar.NumOtherInstruments;
                if (this.SystemPar.RadarPar.NumOtherInstruments != rhsNumber) {
                    return false;
                }
                if (rhsNumber > 0) {
                    if (this.SystemPar.RadarPar.OtherInstrumentCodes.Length < rhsNumber) {
                        return false;
                    }
                    if (rhs.SystemPar.RadarPar.OtherInstrumentCodes.Length < rhsNumber) {
                        return false;
                    }
                    for (int i = 0; i < rhsNumber; i++) {
                        if (this.SystemPar.RadarPar.OtherInstrumentCodes[i] !=
                            rhs.SystemPar.RadarPar.OtherInstrumentCodes[i]) {
                            return false;
                        }
                    }
                }

                /*
                int rhsNumber2 = rhs.SystemPar.RadarPar.OtherInstruments.NumOtherInstruments;
                if (this.SystemPar.RadarPar.OtherInstruments.NumOtherInstruments != rhsNumber2) {
                    return false;
                }
                if (rhsNumber2 > 0) {
                    for (int i = 0; i < rhsNumber2; i++) {
                        if (this.SystemPar.RadarPar.OtherInstruments.OtherInstrumentCodes[i] !=
                            rhs.SystemPar.RadarPar.OtherInstruments.OtherInstrumentCodes[i]) {
                                return false;
                        }
                    }
                }
                */

				// check ProcPar
				if ((this.SystemPar.RadarPar.ProcPar.Dop0 != rhs.SystemPar.RadarPar.ProcPar.Dop0) ||
					(this.SystemPar.RadarPar.ProcPar.Dop1 != rhs.SystemPar.RadarPar.ProcPar.Dop1) ||
					(this.SystemPar.RadarPar.ProcPar.Dop2 != rhs.SystemPar.RadarPar.ProcPar.Dop2) ||
					(this.SystemPar.RadarPar.ProcPar.Dop3 != rhs.SystemPar.RadarPar.ProcPar.Dop3) ||
                    (this.SystemPar.RadarPar.ProcPar.IsDcFiltering != rhs.SystemPar.RadarPar.ProcPar.IsDcFiltering) ||
                    (this.SystemPar.RadarPar.ProcPar.IsIcraAvg != rhs.SystemPar.RadarPar.ProcPar.IsIcraAvg) ||
					(this.SystemPar.RadarPar.ProcPar.IsWindowing != rhs.SystemPar.RadarPar.ProcPar.IsWindowing) ||
					(this.SystemPar.RadarPar.ProcPar.IsWritingPopFile != rhs.SystemPar.RadarPar.ProcPar.IsWritingPopFile) ||
					(this.SystemPar.RadarPar.ProcPar.MaxClutterHtKm != rhs.SystemPar.RadarPar.ProcPar.MaxClutterHtKm) ||
					(this.SystemPar.RadarPar.ProcPar.RemoveClutter != rhs.SystemPar.RadarPar.ProcPar.RemoveClutter) ||
                    (this.SystemPar.RadarPar.ProcPar.KeepOriginalSpectra != rhs.SystemPar.RadarPar.ProcPar.KeepOriginalSpectra) ||
                    (this.SystemPar.RadarPar.ProcPar.GCPercentLess != rhs.SystemPar.RadarPar.ProcPar.GCPercentLess) ||
                    (this.SystemPar.RadarPar.ProcPar.GCRestrictExtent != rhs.SystemPar.RadarPar.ProcPar.GCRestrictExtent) ||
                    (this.SystemPar.RadarPar.ProcPar.GCRestrictIfDcInPrev != rhs.SystemPar.RadarPar.ProcPar.GCRestrictIfDcInPrev) ||
                    (this.SystemPar.RadarPar.ProcPar.GCMinSigThldDB != rhs.SystemPar.RadarPar.ProcPar.GCMinSigThldDB) ||
                    (this.SystemPar.RadarPar.ProcPar.GCTimesBigger != rhs.SystemPar.RadarPar.ProcPar.GCTimesBigger) ||
                    (this.SystemPar.RadarPar.ProcPar.GCMethod != rhs.SystemPar.RadarPar.ProcPar.GCMethod) ||
                    //(this.SystemPar.RadarPar.ProcPar.NumberOfMetInst != rhs.SystemPar.RadarPar.ProcPar.NumberOfMetInst) ||
					(this.SystemPar.RadarPar.ProcPar.NumberOfRx != rhs.SystemPar.RadarPar.ProcPar.NumberOfRx) ||
                    (this.SystemPar.RadarPar.ProcPar.NSpecAtATime != rhs.SystemPar.RadarPar.ProcPar.NSpecAtATime) ||
                    (this.SystemPar.RadarPar.ProcPar.AllocTSOnly != rhs.SystemPar.RadarPar.ProcPar.AllocTSOnly) ||
                    (this.SystemPar.RadarPar.ProcPar.DoClutterWavelet != rhs.SystemPar.RadarPar.ProcPar.DoClutterWavelet) ||
                    (this.SystemPar.RadarPar.ProcPar.DoDespikeWavelet != rhs.SystemPar.RadarPar.ProcPar.DoDespikeWavelet) ||
                    (this.SystemPar.RadarPar.ProcPar.DoHarmonicWavelet != rhs.SystemPar.RadarPar.ProcPar.DoHarmonicWavelet) ||
                    (this.SystemPar.RadarPar.ProcPar.WaveletClutterThldMed != rhs.SystemPar.RadarPar.ProcPar.WaveletClutterThldMed) ||
                    (this.SystemPar.RadarPar.ProcPar.WaveletDespikeThldMed != rhs.SystemPar.RadarPar.ProcPar.WaveletDespikeThldMed) ||
                    (this.SystemPar.RadarPar.ProcPar.WaveletClutterCutoffMps != rhs.SystemPar.RadarPar.ProcPar.WaveletClutterCutoffMps) ||
                    (this.SystemPar.RadarPar.ProcPar.WaveletClutterMaxHt != rhs.SystemPar.RadarPar.ProcPar.WaveletClutterMaxHt) ||
                    (this.SystemPar.RadarPar.ProcPar.DoAutoCorr1Rx != rhs.SystemPar.RadarPar.ProcPar.DoAutoCorr1Rx) ||
                    (this.SystemPar.RadarPar.ProcPar.PopFilePathName != rhs.SystemPar.RadarPar.ProcPar.PopFilePathName))
                {
					return false;
				}
                for (int i = 0; i < this.ArrayDim.MAXCNSMODES; i++) {
                    if (!this.SystemPar.RadarPar.ProcPar.CnsPar[i].Equals(rhs.SystemPar.RadarPar.ProcPar.CnsPar[i])) {
                        return false;
                    }
                }
                for (int i = 0; i < this.ArrayDim.MAXRXID; i++) {
                    if (!this.SystemPar.RadarPar.ProcPar.RxID[i].Equals(rhs.SystemPar.RadarPar.ProcPar.RxID[i])) {
                        return false;
                    }
                }
                for (int i = 0; i < this.ArrayDim.MAXOUTPUTFILES; i++) {
					if (!this.SystemPar.RadarPar.ProcPar.PopFiles[i].Equals(rhs.SystemPar.RadarPar.ProcPar.PopFiles[i])) {
						return false;
					}
				}
				for (int i = 0; i < 6; i++) {
					if (!this.SystemPar.RadarPar.ProcPar.RassSourceParams[i].Equals(rhs.SystemPar.RadarPar.ProcPar.RassSourceParams[i])) {
						return false;
					}
				}
				if (!this.MeltingLayerPar.Equals(rhs.MeltingLayerPar)) {
					return false;
				}

				// check Replay Par
				if (!this.ReplayPar.Equals(rhs.ReplayPar)) {
					return false;
				}

                // check Debug Options
                if (!this.Debug.Equals(rhs.Debug)) {
                    return false;
                }

                if (!this.SignalPeakSearchRange.Equals(rhs.SignalPeakSearchRange)) {
                    return false;
                }

				return true;

			}
			catch (Exception) {
				return false;
			}
		}
        

        /*
        public static bool operator==(PopParameters lhs, PopParameters rhs)  {

            //PopParameters rhs = obj as PopParameters;
            if (rhs == null && lhs == null) {
                return true;
            }

            if (rhs == null || lhs == null) {
                return false;
            }

            try {
                //
                if ((lhs.Source != rhs.Source) ||
                    (lhs.PopParametersVersion != rhs.PopParametersVersion)) {
                    return false;
                }
                // check array sizes and stored array dims
                int thisBSLen = lhs.SystemPar.RadarPar.BeamSequence.Length;
                int thisBPSLen = lhs.SystemPar.RadarPar.BeamParSet.Length;
                int thisFMPSLen = lhs.SystemPar.RadarPar.FmCwParSet.Length;
                int thisBDLen = lhs.SystemPar.RadarPar.BeamDirections.Length;
                int thisRBLen = lhs.SystemPar.RadarPar.RxBw.Length;
                int thisCPLen = lhs.SystemPar.RadarPar.ProcPar.CnsPar.Length;
                int thisRXLen = lhs.SystemPar.RadarPar.ProcPar.RxID.Length;
                int thisPFLen = lhs.SystemPar.RadarPar.ProcPar.PopFiles.Length;
                int thatBSLen = rhs.SystemPar.RadarPar.BeamSequence.Length;
                int thatBPSLen = rhs.SystemPar.RadarPar.BeamParSet.Length;
                int thatFMPSLen = rhs.SystemPar.RadarPar.FmCwParSet.Length;
                int thatBDLen = rhs.SystemPar.RadarPar.BeamDirections.Length;
                int thatRBLen = rhs.SystemPar.RadarPar.RxBw.Length;
                int thatCPLen = rhs.SystemPar.RadarPar.ProcPar.CnsPar.Length;
                int thatRXLen = rhs.SystemPar.RadarPar.ProcPar.RxID.Length;
                int thatPFLen = rhs.SystemPar.RadarPar.ProcPar.PopFiles.Length;
                if ((thisBSLen != thatBSLen) ||
                    (thisBPSLen != thatBPSLen) ||
                    (thisFMPSLen != thatFMPSLen) ||
                    (thisBDLen != thatBDLen) ||
                    (thisRBLen != thatRBLen) ||
                    (thisPFLen != thatPFLen) ||
                    (thisRXLen != thatRXLen) ||
                    (thisCPLen != thatCPLen)) {
                    return false;
                }
                if ((lhs.ArrayDim.MAXBEAMPAR != rhs.ArrayDim.MAXBEAMPAR) ||
                    (lhs.ArrayDim.MAXBEAMS != rhs.ArrayDim.MAXBEAMS) ||
                    (lhs.ArrayDim.MAXBW != rhs.ArrayDim.MAXBW) ||
                    (lhs.ArrayDim.MAXCNSMODES != rhs.ArrayDim.MAXCNSMODES) ||
                    (lhs.ArrayDim.MAXRXID != rhs.ArrayDim.MAXRXID) ||
                    (lhs.ArrayDim.MAXOUTPUTFILES != rhs.ArrayDim.MAXOUTPUTFILES) ||
                    (lhs.ArrayDim.MAXDIRECTIONS != rhs.ArrayDim.MAXDIRECTIONS)) {
                    return false;
                }

                // check SystemPar
                if ((lhs.SystemPar.Altitude != rhs.SystemPar.Altitude) ||
                    (lhs.SystemPar.Latitude != rhs.SystemPar.Latitude) ||
                    (lhs.SystemPar.Longitude != rhs.SystemPar.Longitude) ||
                    (lhs.SystemPar.MinutesToUT != rhs.SystemPar.MinutesToUT) ||
                    (lhs.SystemPar.NumberOfRadars != rhs.SystemPar.NumberOfRadars) ||
                    (lhs.SystemPar.StationName != rhs.SystemPar.StationName)) {
                    return false;
                }

                // check RadarPar
                if ((lhs.SystemPar.RadarPar.RadarID != rhs.SystemPar.RadarPar.RadarID) ||
                    (lhs.SystemPar.RadarPar.RadarName != rhs.SystemPar.RadarPar.RadarName) ||
                    (lhs.SystemPar.RadarPar.RadarType != rhs.SystemPar.RadarPar.RadarType) ||
                    (lhs.SystemPar.RadarPar.TxFreqMHz != rhs.SystemPar.RadarPar.TxFreqMHz) ||
                    (lhs.SystemPar.RadarPar.TxIsOn != rhs.SystemPar.RadarPar.TxIsOn) ||
                    (lhs.SystemPar.RadarPar.MaxTxDutyCycle != rhs.SystemPar.RadarPar.MaxTxDutyCycle) ||
                    (lhs.SystemPar.RadarPar.MaxTxLengthUsec != rhs.SystemPar.RadarPar.MaxTxLengthUsec) ||
                    (lhs.SystemPar.RadarPar.MinIppUsec != rhs.SystemPar.RadarPar.MinIppUsec) ||
                    //(lhs.SystemPar.RadarPar.NumberOfBeams != rhs.SystemPar.RadarPar.NumberOfBeams) ||
                    //(lhs.SystemPar.RadarPar.NumberOfDirections != rhs.SystemPar.RadarPar.NumberOfDirections) ||
                    //(lhs.SystemPar.RadarPar.NumberOfParameterSets != rhs.SystemPar.RadarPar.NumberOfParameterSets) ||
                    (!lhs.SystemPar.RadarPar.PBConstants.Equals(rhs.SystemPar.RadarPar.PBConstants))) {
                    return false;
                }

                // check BeamSequence
                for (int i = 0; i < lhs.ArrayDim.MAXBEAMS; i++) {
                    if (!lhs.SystemPar.RadarPar.BeamSequence[i].Equals(rhs.SystemPar.RadarPar.BeamSequence[i])) {
                        return false;
                    }
                }

                // check BeamParSet
                for (int i = 0; i < lhs.ArrayDim.MAXBEAMPAR; i++) {
                    if (!lhs.SystemPar.RadarPar.BeamParSet[i].Equals(rhs.SystemPar.RadarPar.BeamParSet[i])) {
                        return false;
                    }
                }

                // check FmCwParSet
                for (int i = 0; i < lhs.ArrayDim.MAXBEAMPAR; i++) {
                    if (!lhs.SystemPar.RadarPar.FmCwParSet[i].Equals(rhs.SystemPar.RadarPar.FmCwParSet[i])) {
                        return false;
                    }
                }

                // check BeamDirections
                for (int i = 0; i < lhs.ArrayDim.MAXDIRECTIONS; i++) {
                    if (!lhs.SystemPar.RadarPar.BeamDirections[i].Equals(rhs.SystemPar.RadarPar.BeamDirections[i])) {
                        return false;
                    }
                }

                // check RxBw
                for (int i = 0; i < lhs.ArrayDim.MAXBW; i++) {
                    if (!lhs.SystemPar.RadarPar.RxBw[i].Equals(rhs.SystemPar.RadarPar.RxBw[i])) {
                        return false;
                    }
                }

                // check ProcPar
                if ((lhs.SystemPar.RadarPar.ProcPar.Dop0 != rhs.SystemPar.RadarPar.ProcPar.Dop0) ||
                    (lhs.SystemPar.RadarPar.ProcPar.Dop1 != rhs.SystemPar.RadarPar.ProcPar.Dop1) ||
                    (lhs.SystemPar.RadarPar.ProcPar.Dop2 != rhs.SystemPar.RadarPar.ProcPar.Dop2) ||
                    (lhs.SystemPar.RadarPar.ProcPar.Dop3 != rhs.SystemPar.RadarPar.ProcPar.Dop3) ||
                    (lhs.SystemPar.RadarPar.ProcPar.IsDcFiltering != rhs.SystemPar.RadarPar.ProcPar.IsDcFiltering) ||
                    (lhs.SystemPar.RadarPar.ProcPar.IsIcraAvg != rhs.SystemPar.RadarPar.ProcPar.IsIcraAvg) ||
                    (lhs.SystemPar.RadarPar.ProcPar.IsWindowing != rhs.SystemPar.RadarPar.ProcPar.IsWindowing) ||
                    (lhs.SystemPar.RadarPar.ProcPar.IsWritingPopFile != rhs.SystemPar.RadarPar.ProcPar.IsWritingPopFile) ||
                    (lhs.SystemPar.RadarPar.ProcPar.MaxClutterHtKm != rhs.SystemPar.RadarPar.ProcPar.MaxClutterHtKm) ||
                    (lhs.SystemPar.RadarPar.ProcPar.RemoveClutter != rhs.SystemPar.RadarPar.ProcPar.RemoveClutter) ||
                    (lhs.SystemPar.RadarPar.ProcPar.KeepOriginalSpectra != rhs.SystemPar.RadarPar.ProcPar.KeepOriginalSpectra) ||
                    (lhs.SystemPar.RadarPar.ProcPar.GCPercentLess != rhs.SystemPar.RadarPar.ProcPar.GCPercentLess) ||
                    (lhs.SystemPar.RadarPar.ProcPar.GCRestrictExtent != rhs.SystemPar.RadarPar.ProcPar.GCRestrictExtent) ||
                    (lhs.SystemPar.RadarPar.ProcPar.GCRestrictIfDcInPrev != rhs.SystemPar.RadarPar.ProcPar.GCRestrictIfDcInPrev) ||
                    (lhs.SystemPar.RadarPar.ProcPar.GCMinSigThldDB != rhs.SystemPar.RadarPar.ProcPar.GCMinSigThldDB) ||
                    (lhs.SystemPar.RadarPar.ProcPar.GCTimesBigger != rhs.SystemPar.RadarPar.ProcPar.GCTimesBigger) ||
                    (lhs.SystemPar.RadarPar.ProcPar.NumberOfMetInst != rhs.SystemPar.RadarPar.ProcPar.NumberOfMetInst) ||
                    (lhs.SystemPar.RadarPar.ProcPar.NumberOfRx != rhs.SystemPar.RadarPar.ProcPar.NumberOfRx) ||
                    (lhs.SystemPar.RadarPar.ProcPar.PopFilePathName != rhs.SystemPar.RadarPar.ProcPar.PopFilePathName)) {
                    return false;
                }
                for (int i = 0; i < lhs.ArrayDim.MAXCNSMODES; i++) {
                    if (!lhs.SystemPar.RadarPar.ProcPar.CnsPar[i].Equals(rhs.SystemPar.RadarPar.ProcPar.CnsPar[i])) {
                        return false;
                    }
                }
                for (int i = 0; i < lhs.ArrayDim.MAXRXID; i++) {
                    if (!lhs.SystemPar.RadarPar.ProcPar.RxID[i].Equals(rhs.SystemPar.RadarPar.ProcPar.RxID[i])) {
                        return false;
                    }
                }
                for (int i = 0; i < lhs.ArrayDim.MAXOUTPUTFILES; i++) {
                    if (!lhs.SystemPar.RadarPar.ProcPar.PopFiles[i].Equals(rhs.SystemPar.RadarPar.ProcPar.PopFiles[i])) {
                        return false;
                    }
                }
                for (int i = 0; i < 6; i++) {
                    if (!lhs.SystemPar.RadarPar.ProcPar.RassSourceParams[i].Equals(rhs.SystemPar.RadarPar.ProcPar.RassSourceParams[i])) {
                        return false;
                    }
                }

                // check Melting Layer Par
                if (!lhs.MeltingLayerPar.Equals(rhs.MeltingLayerPar)) {
                    return false;
                }

                // check Replay Par
                if (!lhs.ReplayPar.Equals(rhs.ReplayPar)) {
                    return false;
                }

                // check Debug Options
                if (!lhs.Debug.Equals(rhs.Debug)) {
                    return false;
                }

                if (!lhs.SignalPeakSearchRange.Equals(rhs.SignalPeakSearchRange)) {
                    return false;
                }

                return true;

            }
            catch (Exception) {
                return false;
            }
        }  // end operator==

        public static bool operator !=(PopParameters left, PopParameters right) {
            return !(left == right);
        }

        */


		public int GetBeamsInSequence() {
			int nBeams = 0;
			for (int i = 0; i < this.ArrayDim.MAXBEAMS; i++) {
				if (this.SystemPar.RadarPar.BeamSequence[i].NumberOfReps != 0) {
					nBeams++;
				}
				else {
					break;
				}
			}
			return nBeams;
		}

		/// <summary>
		/// Returns the BeamParameters structure at index bmParIndex
		///		in the BeamParSet array.
		/// </summary>
		/// <param name="bmParIndex"></param>
		/// <returns></returns>
		public PopParameters.BeamParameters GetBeamParameters(int bmParIndex) {
			return this.SystemPar.RadarPar.BeamParSet[bmParIndex];
		}

		public double GetBeamParNyquist(int bmParIndex) {
			double hzms = (149.896 / this.SystemPar.RadarPar.TxFreqMHz);
			double ipp = this.SystemPar.RadarPar.BeamParSet[bmParIndex].IppMicroSec;
			double nci = this.SystemPar.RadarPar.BeamParSet[bmParIndex].NCI;
			if (nci == 0) {
				nci = 1;
			}
			double nyq = 0.0;
			if (ipp != 0.0) {
				nyq = (float)(0.5e6 * hzms / ipp / nci);    // nyquist freq in m/s 
			}
			return nyq;
		}

		public double[] GetBeamParHeightsM(int bmParIndex, double elevationAngle) {
			BeamParameters[] beamPar = this.SystemPar.RadarPar.BeamParSet;
			int nhts = this.SystemPar.RadarPar.BeamParSet[bmParIndex].NHts;
			double[] heights = new double[nhts];
			double sysDelay = this.SystemPar.RadarPar.RxBw[0].BwDelayNs;
			double sampleDelay = beamPar[bmParIndex].SampleDelayNs;
			double spacing = beamPar[bmParIndex].SpacingNs;
			for (int iGate = 0; iGate < nhts; iGate++) {
				heights[iGate] = ((sampleDelay-sysDelay) + (iGate * spacing)) * MperNs;
				heights[iGate] *= Math.Sin(elevationAngle * Math.PI / 180.0);
			}
			return heights;
		}

		/// <summary>
		/// Returns the FmCwParameters structure at index bmParIndex
		///		in the FmCwParSet array.
		/// </summary>
		/// <param name="bmParIndex"></param>
		/// <returns></returns>
		public PopParameters.FmCwParameters GetFmCwParameters(int bmParIndex) {
			return this.SystemPar.RadarPar.FmCwParSet[bmParIndex];
		}

		/// <summary>
		/// Returns the BeamPosition structure at index bmSeqIndex
		///		in the BeamSequence array.
		/// </summary>
		/// <param name="bmSeqIndex"></param>
		/// <returns></returns>
		public PopParameters.BeamPosition GetBeamPosition(int bmSeqIndex) {
			return this.SystemPar.RadarPar.BeamSequence[bmSeqIndex];
		}

		/// <summary>
		/// Returns the Direction structure at index bmDirIndex
		///		in the BeamDirections array.
		/// </summary>
		/// <param name="bmDirIndex"></param>
		/// <returns></returns>
		public PopParameters.Direction GetBeamDirection(int bmDirIndex) {
			return this.SystemPar.RadarPar.BeamDirections[bmDirIndex];
		}

        public int GetSamplesPerIPP(int parSetIndex) {

            int nSamples;
            TypeOfRadar radarType = SystemPar.RadarPar.RadarType;

            if ((radarType == PopParameters.TypeOfRadar.FmCwDop) || 
                (radarType == PopParameters.TypeOfRadar.FmCwSA) ||
                (radarType == PopParameters.TypeOfRadar.FmCw)) {
                FmCwParameters fmcwParameters = GetFmCwParameters(parSetIndex);
                int ptsPerFFT = fmcwParameters.TxSweepSampleNPts;
                bool overlap = fmcwParameters.TxSweepSampleOverlap;
                int nFFT = fmcwParameters.TxSweepSampleNSpec;
                if (overlap) {
                    nSamples = ptsPerFFT + (nFFT - 1) * ptsPerFFT / 2;
                }
                else {
                    nSamples = nFFT * ptsPerFFT;
                }
                return nSamples;

            }
            else {
                throw new ApplicationException("PopParameters.GetSamplesPerIPP does not support Pulsed Doppler");
            }
        }

        public double GetGateOffset() {
            // calculates gate offset from saved parameters
            double igate;

            double offset = this.SystemPar.RadarPar.FmCwParSet[0].TxSweepOffsetHz;
            int npts = this.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts;
            int spacing = this.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleSpacingNs;
            double sweepRate = this.SystemPar.RadarPar.FmCwParSet[0].TxSweepRateHzUSec;
            int sysDelayNs = this.SystemPar.RadarPar.RxBw[0].BwDelayNs;

            try {
                double sampleSpacingHz = 1.0e9 / (npts * spacing);
                double rangeSpacingNs = 1.0e9 * sampleSpacingHz / (sweepRate);
                double rangeResM = PopParameters.MperNs * rangeSpacingNs;
                double sysDelayCorrectionM = sysDelayNs * PopParameters.MperNs;
                igate = offset / sampleSpacingHz + sysDelayCorrectionM / rangeResM;
            }
            catch (Exception e) {
                igate = Double.NaN;
            }

            return igate;
        }


		#endregion Public Methods
	}

	public struct PopParCurrentIndices {
		public int ParI;
		public int BmSeqI;
		public int DirI;
		public int CnsParI;
		public int FmCwParI;
	}


}
