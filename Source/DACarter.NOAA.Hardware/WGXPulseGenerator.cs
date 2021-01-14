using System;
using DACarter.PopUtilities;
using DACarter.Utilities;
using System.Windows.Forms;
using System.Runtime.InteropServices;


namespace DACarter.NOAA.Hardware {

	public class WGXPulseGenerator : PulseGeneratorDevice {

        /*

        [DllImport("adwgc.dll", EntryPoint = "adwg_ADWGC")]
        private static extern int adwg_ADWGC();

        [DllImport("adwgc.dll", EntryPoint = "adwg_Terminate")]
        private static extern void adwg_Terminate();

        [DllImport("adwgc.dll", EntryPoint = "adwg_Abort")]
        private static extern void adwg_Abort();

        [DllImport("adwgc.dll", EntryPoint = "adwg_UseIntClock")]
        private static extern void adwg_UseIntClock();

        [DllImport("adwgc.dll", EntryPoint = "adwg_UseExtClock")]
        private static extern void adwg_UseExtClock();

        [DllImport("adwgc.dll", EntryPoint = "adwg_SetReqClock",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern void adwg_SetReqClock(Int32 freqHz);

        [DllImport("adwgc.dll", EntryPoint = "adwg_GetReqClock")]
        private static extern Int32 adwg_GetReqClock();

        [DllImport("adwgc.dll", EntryPoint = "adwg_GetNrSamples")]
        private static extern Int32 adwg_GetNrSamples();

        [DllImport("adwgc.dll", EntryPoint = "adwg_SetInternalTrigger",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern void adwg_SetInternalTrigger([MarshalAs(UnmanagedType.I1)] bool useInternal);

        [DllImport("adwgc.dll", EntryPoint = "adwg_GetInternalTrigger")]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool adwg_GetInternalTrigger();

        [DllImport("adwgc.dll", EntryPoint = "adwg_IsDeviceReady")]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool adwg_IsDeviceReady();

        [DllImport("adwgc.dll", EntryPoint = "adwg_IsRunning")]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool adwg_IsRunning();

        [DllImport("adwgc.dll", EntryPoint = "adwg_SetInfiniteLoop",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern void adwg_SetInfiniteLoop([MarshalAs(UnmanagedType.I1)] bool useInternal);

        [DllImport("adwgc.dll", EntryPoint = "adwg_GetInfiniteLoop")]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool adwg_GetInfiniteLoop();

        [DllImport("adwgc.dll", EntryPoint = "adwg_AdwgRunLoop",
           CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 adwg_AdwgRunLoop(int nSamples,
                            [MarshalAs(UnmanagedType.I1)] bool alwaysFalse,
                            [MarshalAs(UnmanagedType.I1)] bool msg);

        [DllImport("adwgc.dll", EntryPoint = "adwg_DisCtrlSeq")]
        private static extern void adwg_DisCtrlSeq();

        [DllImport("adwgc.dll", EntryPoint = "adwg_DisDefaultCtrl")]
        private static extern void adwg_DisDefaultCtrl();

        [DllImport("adwgc.dll", EntryPoint = "adwg_SetDataMaskOut",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern void adwg_SetDataMaskOut(ref UInt16 mask);

        [DllImport("adwgc.dll", EntryPoint = "adwg_GetDataMaskOut",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern void adwg_GetDataMaskOut(ref UInt16 mask);

        [DllImport("adwgc.dll", EntryPoint = "adwg_ClearBuffer")]
        private static extern void adwg_ClearBuffer();

        [DllImport("adwgc.dll", EntryPoint = "adwg_WriteMultiSample", CallingConvention = CallingConvention.Cdecl)]
        private static extern int adwg_WriteMultiSample(short[] ary, int size);

        [DllImport("adwgc.dll", EntryPoint = "adwg_WriteSingleSample", CallingConvention = CallingConvention.Cdecl)]
        private static extern int adwg_WriteSingleSample(short data);
         * 
         * */

        #region Protected base class members
		//
		//protected PopParameters _parameters;
		//protected int _parSetIndex;
		//
		#endregion

		#region Properties
		//
        public int FifoSize {
            get { return _fifoSize; }
            set { _fifoSize = value; }
        }

        public int ClockPerNs {
            get { return CLKPER; }
            set { CLKPER = value; }
        }

        public bool UseInternalClock {
            get { return _useInternalClock; }
            set { _useInternalClock = value; }
        }

        public UInt16[] FifoArray {
			get { return (UInt16[])_fifoArray.Clone(); }
			//set { _fifoArray = value; }
		}
		//

		#endregion Properties

		#region Local Variables
		//
		private PopParameters.TypeOfRadar _radarType;
		private PopParameters.BeamParameters _bmParameters;
		private PopParameters.FmCwParameters _fmcwParameters;
		private PopParameters _activeParameters;		// PopParameters currently actively running on PBX
		private int _activeParSetIndex;					// ParSetIndex currently actively running on PBX
		private int _fifoSize;
		private UInt16[] _fifoArray;
        private bool _useInternalClock;


		private int _index;				// index of current word in FIFO array
		private int _currTime;			// nsec length of current pbx array
		private int _currClock;
		private int _currIpp;			// the current IPP (1 or 2) being worked on
		private int _nIpp;				// the total number of IPP's per sync pulse

		private int _nSamples;			// number of sample pulses per IPP
		private int _spacing;			// nsec spacing of samples
		private int _delay;				// nsec delay from end of TX to first sample
		private int _txLength;			// nsec length of TX pulse
		private int _ipp;				// nsec length of IPP
		private int _nCode;				// number of pulse code bits; 0 = no pulse coding;
		private int _preTR, _postTR, _preBlank, _postBlank;
		private int _nRX;				// number of receivers;
		private int _attenGates;		// number of attenuated gates
		private int _sync;				// nsec length of synch pulse
		private bool _txIsOn;

		private int[] _pulseStarts;			// gives start time, stop time, and pulse configuration for all TR signals (TRSIGS)
		private int[] _pulseStops;
		private UInt16[] _pulseBits;

        //private bool _hardwareExists;

		//private IOPortBase PortIO;

		//
		#endregion Local Variables

		#region PBX Constant Definitions
		//
		public struct PbxPulseBits {
			//public const int RETRANS = 0x8000;   // retransmit output bit 
            public const int SAMPLE = 0x80;
            public const int TX = 0x40;
			public const int TR = 0x20;
			public const int SYNCH = 0x01;  // tr synch bit
            public const int BLANK = 0x02;  // blanker bit
            public const int PHASE = 0x04;  // tx phase bit
			public const int ATTEN = 0x08;  // rx attenuator bit
		}


		private int CLKPER;             // pulsebox clock period in nanosec 
		private int MINSAMPTIME;        // min length (nsec) for sample words 
		private int MAX_NCODE;			// max code bit length 
		private int PC_NUM_CODES;		// number of possible pulse codes 
		private int TRSIGS;				// number of pulse signals during TR 
		//
		//
		#endregion Constants

		#region Constructors
		//
		/// <summary>
		/// Constructors
		/// </summary>
		public WGXPulseGenerator()
			: base() {
			ConstructPbx(false);
		}

		public WGXPulseGenerator(PopParameters parameters, int parSetIndex)
			: base(parameters, parSetIndex) {
			ConstructPbx(false);
		}

		private void ConstructPbx(bool PbxAlreadyExists) {
			// if called with PbxAlreadyExists = true,
			//	then do not do anything that might disrupt pulse outputs
			//	Only use this instance to control ports
			//	such as Direction and BW
			_activeParameters = null;
			_activeParSetIndex = -1;
            _useInternalClock = true;
            InitPbxConstants();

            /*
            adwg_ADWGC();                   // Open ADWGC library
            adwg_SetReqClock(1000000);      // Set clock freq. to 1MHz
            int clk = adwg_GetReqClock();

            adwg_SetInternalTrigger(true);     // External trigger is not used, pattern generation starts immediately
            bool tr = adwg_GetInternalTrigger();

            adwg_DisCtrlSeq();              // Disable control sequence
            adwg_DisDefaultCtrl();          // Disable default values on control lines

            UInt16 DataEnable = (UInt16)0xFFFF;
            adwg_SetDataMaskOut(ref DataEnable);  // Enable all data lines
            UInt16 mask = 0;
            adwg_GetDataMaskOut(ref mask);

            adwg_ClearBuffer();
            */

			// create whatever PortIO method we are using:
		}

		private void InitPbxConstants() {

			MAX_NCODE = 32;
			PC_NUM_CODES = 7;
			TRSIGS = (6 + MAX_NCODE);

			_pulseBits = new UInt16[TRSIGS];
			_pulseStarts = new int[TRSIGS];
			_pulseStops = new int[TRSIGS];

			return;
		}
		//
		#endregion Constructors


		#region Overridden Base Class Methods
		//
		///////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Checks for existance of working Pulse Controller Card
		/// by writing and reading back words to/from the FIFO.
		/// </summary>
		/// <remarks>
		/// This operation destroys contents of FIFO.
		/// </remarks>
		/// <returns></returns>
		public override bool Exists() {
            throw new NotImplementedException();
            return true;
		}


        public override void PbxReset() {
           throw new NotImplementedException();
        }


		///////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override void Setup(PopParameters parameters, int parSetIndex) {

			bool success = false;

			_parameters = parameters;
			_parSetIndex = parSetIndex;
			_radarType = _parameters.SystemPar.RadarPar.RadarType;
			_bmParameters = _parameters.GetBeamParameters(parSetIndex);
			if (_radarType == PopParameters.TypeOfRadar.FmCw) {
				_fmcwParameters = _parameters.GetFmCwParameters(parSetIndex);
			}

            /*
            if (_useInternalClock) {
                adwg_UseIntClock();             // Select the internal clock source
            }
            else {
                adwg_UseExtClock();
            }
            */
            
            CLKPER = _parameters.SystemPar.RadarPar.PBConstants.PBClock;
            int freq = (int)(1.0e9/CLKPER);
            /*
            adwg_SetReqClock(freq);      // Set clock freq
            */
            MINSAMPTIME = 300;
            
            _attenGates = _bmParameters.AttenuatedGates;
			if (_attenGates < 1) {
				_attenGates = 0;
			}
			_sync = _parameters.SystemPar.RadarPar.PBConstants.PBSynch;
			if ((_sync < 100) || (_sync > 1000)) {
				_sync = 500;
			}
			_txIsOn = _parameters.SystemPar.RadarPar.TxIsOn;

			if (_radarType == PopParameters.TypeOfRadar.FmCw) {
				//int ptsPerFFT = _fmcwParameters.TxSweepSampleNPts;
				//bool overlap = _fmcwParameters.TxSweepSampleOverlap;
				//int nFFT = _fmcwParameters.TxSweepSampleNSpec;
                /*
                if (overlap) {
                    _nSamples = ptsPerFFT + (nFFT - 1) * ptsPerFFT / 2;
                }
                else {
                    _nSamples = nFFT * ptsPerFFT;
                }
                */
                _nSamples = _parameters.GetSamplesPerIPP(parSetIndex);
				_ipp = (Int32)(_fmcwParameters.IppMicroSec * 1000);
				_spacing = _fmcwParameters.TxSweepSampleSpacingNs;
				_delay = _fmcwParameters.TxSweepSampleDelayNs;
				_txLength = 1000;
				_preTR = _parameters.SystemPar.RadarPar.PBConstants.PBPreTR;
				_postTR = _parameters.SystemPar.RadarPar.PBConstants.PBPostTR;
				_preBlank = _parameters.SystemPar.RadarPar.PBConstants.PBPreBlank;
				_postBlank = _parameters.SystemPar.RadarPar.PBConstants.PBPostBlank;
				_nCode = 0;
				_nRX = 1;

            }
			else {
				throw new ApplicationException("Pulsed Doppler for PBX card not implemented yet.");
			}

			//
			// start creating pulse sequence
			//
			bool testingPulses = false;
            _fifoArray = null;
			for (int ipar = 0; ipar < 1; ipar++) {
				// expand limit of this loop if testing all parameter sets

				if (_nCode <= 0) {
					_nIpp = _nRX;
				}
				else {
					throw new ApplicationException("Pulse coding not implemented yet.");
				}
                if ((_ipp % CLKPER) != 0) {
                    throw new ApplicationException("Makepulse ipp not multiple of clock period");
                }

                _fifoSize = (_nIpp * _ipp) / CLKPER;
                _fifoArray = null;
                _fifoArray = new ushort[_fifoSize];

				_index = 0;
				_currClock = 0;
				_currTime = 0;

				for (int i = 0; i < _nIpp; i++) {
					_currIpp = i + 1;
					MakeTrAndSamples();
					FinishIPP();
				}

				CheckLast();

				int time = _currClock * CLKPER;
				if (time != _nIpp*_ipp) {
					throw new ApplicationException("PBX End Time not Correct");
				}

				if (!testingPulses) {
					LoadPbx();
				}
				
			}

			// if we got this far, we did good:
			success = true;
			if (success) {
				_activeParSetIndex = _parSetIndex;
				_activeParameters = _parameters;
			}

		}  // end method Setup()


		///////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override void StartPulses() {
            //PortIO.WritePort8(PbxPorts.REGISTER, PbxCommands.START);
		}

		///////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override void StopPulses() {
            //PortIO.WritePort8(PbxPorts.REGISTER, PbxCommands.STOP);
        }
		//
		#endregion Override Members

		
		#region Public Methods
		//




		#endregion Public Methods

		#region Private Methods
		/// <summary>
		/// Creates sequence of pulses
		/// </summary>
		private void MakeTrAndSamples() {

			int i, nc, isam;
			int tm, newtm, txCode;
			UInt16 pulse, newpulse;

			if (_nCode > 0)
				nc = _nCode;
			else
				nc = 1;

			// set codes, start, stop times 
			// for all pulses 
			// index 0 = TR
			// index 1 = Sync
			// index 2 = Blanker
			// index 3 = TX
			// index 4 = ATTEN
			// index 5 = Samples
			
			// Set the pulse bit configurations
			_pulseBits[0] = PbxPulseBits.TR;
			if (_currIpp == 1)
				_pulseBits[1] = PbxPulseBits.SYNCH;
			else
				_pulseBits[1] = 0;
			_pulseBits[2] = PbxPulseBits.BLANK;
			if (_txIsOn)
				_pulseBits[3] = PbxPulseBits.TX;
			else
				_pulseBits[3] = 0;
			if (_attenGates > 0)
				_pulseBits[4] = PbxPulseBits.ATTEN;
			else
				_pulseBits[4] = 0;
			_pulseBits[5] = PbxPulseBits.SAMPLE;

			// TR times
			_pulseStarts[0] = 0;
			_pulseStops[0] = _preTR + (nc * _txLength) + _postTR;
			// Sync times
			_pulseStarts[1] = 0; 
			_pulseStops[1] = _sync;
			// Blanker times
			_pulseStarts[2] = _preTR - _preBlank;                 
			if (_pulseStarts[2] < 0)
				_pulseStarts[2] = 0;
			_pulseStops[2] = _preTR + nc * _txLength + _postBlank;
			// TX times
			_pulseStarts[3] = _preTR;                           
			_pulseStops[3] = _preTR + nc * _txLength;
			// Attenuator times
			if (_attenGates > 0) {                            
				_pulseStarts[4] = 0;
				if (_attenGates > _nSamples)
					_attenGates = _nSamples;
				_pulseStops[4] = _preTR + (nc * _txLength) + _delay + (_attenGates - 1) * _spacing;

			}
			else {
				_pulseStarts[4] = 0;
				_pulseStops[4] = 0;
			}
			// Sample pulse times
			_pulseStarts[5] = _preTR + nc * _txLength + _delay;
			_pulseStops[5] = _pulseStarts[5] + MINSAMPTIME;

			// clear Tx pulse coding
			for (i = 6; i < TRSIGS; i++) {
				_pulseBits[i] = 0;
				_pulseStarts[i] = -1;
				_pulseStops[i] = -1;
			}

			// set Tx pulse code phase bits
			if (_nCode > 0) {
				throw new ApplicationException("Pulse coding not implemented yet");
				/*
				// get code sequence 
				if (_currIpp <= _nIpp / 2)                            
					txCode = PC_CODE_BITS[PC_INDEX * 2];
				else
					txCode = PC_CODE_BITS[PC_INDEX * 2 + 1];
				for (i = 0; i < _nCode; i++) {
					_pulseBits[6 + i] = PHASE_BIT;
					if (txCode & 0x01) {                        // turn on proper bit 
						_pulseStarts[6 + i] = _pulseStarts[3] + i * _txLength;
						_pulseStops[6 + i] = _pulseStarts[3] + (i + 1) * _txLength;
						// extend last phase bit out to end of blanker
						// add dac 950817 because was flipping before end of TX
						// mod: dac 030115 make last phase go until end of TR
						// mod: dac 030115 make first phase start at begin of TR
						if (i == _nCode - 1)
							_pulseStops[6 + i] = _pulseStops[0];

						if (i == 0)
							_pulseStarts[6] = _pulseStarts[0];


					}
					code >>= 1;
				}
				*/
			}

			isam = 0;      // isam is number of sample gates done
			tm = -1;
			pulse = 0;
			tm = nextm(tm, ref pulse);        /* get first pulse config */
			while ((pulse != 0) || (isam < _nSamples)) {

				newpulse = pulse;                     /* next pulse before changes */
				newtm = nextm(tm, ref newpulse);   /* find time of next change */
				/* check if we are creating a sample now */
				if ((pulse & PbxPulseBits.SAMPLE) != 0) {
					isam++;
					if (isam == _nSamples) {
						/* if last one, don't do anymore */
						_pulseStarts[5] = 0;
						_pulseStops[5] = 0;
						newpulse = (UInt16)(newpulse & ~((UInt16)PbxPulseBits.SAMPLE));
					}
				}
				makepulse(pulse, (newtm - tm));

				tm = newtm;
				pulse = newpulse;
			}
			_currTime += tm;         /* time span of current pbary words */

			return;
		}

		private void makepulse(UInt16 pulse, int nsTime) {

			int clk; 
			int minclk,xcnt,prevcnt;
			UInt16 prevpls;

			if (nsTime == 0)
				return;
			if (nsTime < 0) {
				throw new ApplicationException("Makepulse time < 0");
			}
			if ((nsTime % CLKPER) != 0) {
				throw new ApplicationException("Makepulse time not multiple of clock period");
			}
			clk = nsTime / CLKPER;               // convert nsec to clock periods */

			storepulse(pulse, (int)(clk));

			return ;
		}


		private void storepulse(UInt16 pulse, int count) {
			if ((_index + 1) > _fifoSize) {
				throw new ApplicationException("PBX ARRAY OVERFLOW  ");
			}
            for (int i = 0; i < count; i++) {
                _fifoArray[_index] = (UInt16)(pulse);
                _index++;
            }
			_currClock = _currClock + count;
			return;
		}

		private int nextm(int nowTime, ref UInt16 pulse) {
			// returns next time at which there is a change 
			// in any of the TRSIGS pulses 
			// also sends back next pulse configuration in pulse 

			int newTime;
			int i;

			newTime = 0x7fffffff;
			for (i = 0; i < TRSIGS; i++) {
				if ((_pulseStarts[i] > nowTime) && (_pulseStarts[i] < newTime)) {
					newTime = _pulseStarts[i];
				}
				if (i != 5) {   // don't look for end of sample 
					if ((_pulseStops[i] > nowTime) && (_pulseStops[i] < newTime)) {
						newTime = _pulseStops[i];
					}
				}
			}
			for (i = 0; i < TRSIGS; i++) {
				if (_pulseStarts[i] == newTime) {
					pulse = (UInt16)(pulse | _pulseBits[i]);
				}
				if (_pulseStops[i] == newTime) {
					pulse = (UInt16)(pulse & ~(_pulseBits[i]));
				}
			}

			// turn off sample bit if not starting 
			if (_pulseStarts[5] != newTime)
				pulse = (UInt16)(pulse & ~(PbxPulseBits.SAMPLE));
			// if are starting sample, setup next sample 
			// (should never access stop time) 
			if ((pulse & PbxPulseBits.SAMPLE) != 0) {
				_pulseStarts[5] += _spacing;
				_pulseStops[5] = _pulseStarts[5] + MINSAMPTIME;
			}

			return (newTime);
		}

		private void FinishIPP() {

			if (_currTime > _nIpp*_ipp) {
				throw new ApplicationException("A PBX PULSE GOES BEYOND IPP  ");
			}
			makepulse(0,((_nIpp*_ipp)-_currTime));
			_currTime = _currIpp*_ipp;
			return;
		}

		private void CheckLast() {

			return;
		}

		private void LoadPbx() {

            for (int i = 0; i < _index; i++) {
				//PbxWriteFifo(_fifoArray[i]);
			}
			//
			return;
		}

		//
		#endregion Private Methods
	}
}
