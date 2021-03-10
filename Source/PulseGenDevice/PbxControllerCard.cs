using System;

using DACarter.PopUtilities;

namespace DACarter.NOAA.Hardware {

	public class PbxControllerCard : PulseGeneratorDevice {

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
		private int _maxFifoSize = 65536;			// max size for iterating thru FIFO; used in local routines
		private int _fifoSize;
		private UInt16[] _fifoArray;


		private int _index;				// index of current word in FIFO array
		private int _currTime;			// nsec length of current pbx array
		private int _currClock;
		private int _currIpp;			// the current IPP (1 or 2) being worked on
		private int _nIpp;				// the total number of IPP's per sync pulse
		//private UInt16 _lastPbxWord = 0;

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

        private bool _hardwareExists;

		private IOPortBase PortIO;

		//
		#endregion Local Variables

		#region PBX Constant Definitions
		//
		public struct PbxPulseBits {
			public const int RETRANS = 0x8000;   // retransmit output bit 
			public const int TX = 0x4000;
			public const int TR = 0x2000;
			public const int SAMPLE = 0x1000;
			public const int SYNCH = 0x0800;  // tr synch bit
			public const int PHASE = 0x0400;  // tx phase bit
			public const int ATTEN = 0x0400;  // rx attenuator bit
			public const int BLANK = 0x0200;  // blanker bit
		}

		public struct PbxCommands {
			public const int RESET = 0x01;
			public const int START = 0x02;
			public const int STOP = 0x04;
			public const int RETRANS = 0x08;
		}

		public struct PbxStatusBits {
			public const int NotBUSY = 0x01;
			public const int FifoNotEMPTY = 0x02;
			public const int FifoNotFULL = 0x04;
			public const int NotParityERROR = 0x08;
		}

		public struct PbxPorts {
			public const int FIFO = 0x330;
			public const int REGISTER = 0x331;  // command (out) and status (in) register
			public const int DIRECTION = 0x332;
			public const int BW = 0x333;
			public const int VERSION = 0x334;
		}

		private int CLKPER;             // pulsebox clock period in nanosec 
		private int PBXCNT;             // extra clock count to add to pbx count 
		//    to get actual duration 
		private int PBMINCNT;           // min count loaded in fifo word 
		private int PBMAXCNT;           // max count loaded into fifo word 
		private int PBMINCLK;           // min clocks per fifo word 
		private int PBMAXCLK;           // max clocks per fifo word 
		private int MINLASTCLK;         // min clocks for fifo word before retrans) 
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
		public PbxControllerCard()
			: base() {
			ConstructPbx(false);
		}

		public PbxControllerCard(PopParameters parameters, int parSetIndex)
			: base(parameters, parSetIndex) {
			ConstructPbx(false);
		}

		public PbxControllerCard(bool PbxAlreadyExists)
			: base() {
			ConstructPbx(PbxAlreadyExists);
		}

		private void ConstructPbx(bool PbxAlreadyExists) {
			// if called with PbxAlreadyExists = true,
			//	then do not do anything that might disrupt pulse outputs
			//	Only use this instance to control ports
			//	such as Direction and BW
			PortIO = new PortIO_TVicPort();
			if (PbxAlreadyExists) {
				_hardwareExists = PortIO.HardwareExists = true;
			}
			else {
				_hardwareExists = PortIO.HardwareExists = this.Exists();
				_activeParameters = null;
				_activeParSetIndex = -1;
				_fifoSize = GetFifoSize();
				InitPbxConstants();
				_fifoArray = new UInt16[_fifoSize];
			}
			// create whatever PortIO method we are using:
		}

		private void InitPbxConstants() {

			int vers;

			// read board num
			vers = 0x0003 & PbxReadVersion();
			if ((vers) == 1)           // #1 = 20 MHz 
				CLKPER = 50;
			else if ((vers) == 2)      // #2 = 4 MHz 
				CLKPER = 250;
			else                       // #0 = 10 MHz 
				CLKPER = 100;
			if (CLKPER > 110) {        // check this OK for 4 MHz 
				PBXCNT = 1;
				PBMINCNT = 1;
			}
			else {
				PBXCNT = 2;
				PBMINCNT = 0;
			}
			PBMAXCNT = 255;
			PBMINCLK = ((PBMINCNT) + (PBXCNT));
			PBMAXCLK = ((PBMAXCNT) + (PBXCNT));
			MINLASTCLK = 4;
			// sample words minimum length (nsec) 
			MINSAMPTIME = 150 + CLKPER;
			if ((MINSAMPTIME % CLKPER) != 0)
				MINSAMPTIME = CLKPER * (MINSAMPTIME / CLKPER + 1);

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
			//ConstructPbx();
			UInt16 word1 = 0x44;
			UInt16 word2 = 0x33;
			PbxReset();
			PbxWriteFifo(word1);
			PbxWriteFifo(word2);
			PbxRetransmit();
			UInt16 read1 = PbxReadFifo();
			UInt16 read2 = PbxReadFifo();
			if ((read1 == word1) && (read2 == word2)) {
				return true;
			}
			else {
				return false;
			}
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
            if ((_radarType == PopParameters.TypeOfRadar.FmCwDop) ||
                (_radarType == PopParameters.TypeOfRadar.FmCwSA)) {
                _fmcwParameters = _parameters.GetFmCwParameters(parSetIndex);
			}

			_attenGates = _bmParameters.AttenuatedGates;
			if (_attenGates < 1) {
				_attenGates = 0;
			}
			_sync = _parameters.SystemPar.RadarPar.PBConstants.PBSynch;
			if ((_sync < 100) || (_sync > 1000)) {
				_sync = 500;
			}
			_txIsOn = _parameters.SystemPar.RadarPar.TxIsOn;

			if ((_radarType == PopParameters.TypeOfRadar.FmCwDop) ||
                (_radarType == PopParameters.TypeOfRadar.FmCwSA)) {
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
				throw new ApplicationException("Hey! Pulsed Doppler for PBX card not implemented yet.");
			}

			//
			// start creating pulse sequence
			//
			bool testingPulses = false;
			for (int ipar = 0; ipar < 1; ipar++) {
				// expand limit of this loop if testing all parameter sets

				if (_nCode <= 0) {
					_nIpp = _nRX;
				}
				else {
					throw new ApplicationException("Pulse coding not implemented yet.");
				}

				_index = 0;
				_currClock = 0;
				_currTime = 0;

				if (!testingPulses) {
					PbxReset();
				}

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
            PortIO.WritePort8(PbxPorts.REGISTER, PbxCommands.START);
		}

		///////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override void StopPulses() {
            PortIO.WritePort8(PbxPorts.REGISTER, PbxCommands.STOP);
        }
		//

        /// <summary>
        /// Reset call for backward compatibility
        /// Future: use override Reset()
        /// </summary>
        public void PbxReset() {
            PortIO.WritePort8(PbxPorts.REGISTER, PbxCommands.RESET);
        }

        public override void Reset() {
            PortIO.WritePort8(PbxPorts.REGISTER, PbxCommands.RESET);
        }

        #endregion Override Members

		
		#region Public Methods
		//

		public void PbxRetransmit() {
			PortIO.WritePort8(PbxPorts.REGISTER, PbxCommands.RETRANS);
		}

		public void PbxWriteFifo(UInt16 word1) {
			PortIO.WritePort16(PbxPorts.FIFO, word1);
		}

		public UInt16 PbxReadFifo() {
			return PortIO.ReadPort16(PbxPorts.FIFO);
		}

		public void PbxWriteBW(int BWcode) {
			// Bandwidth port is 2 bits wide
			PortIO.WritePort8(PbxPorts.BW, (byte)BWcode);
		}

		public UInt16 PbxReadVersion() {
			return PortIO.ReadPort8(PbxPorts.VERSION);
		}

        public UInt16 PbxReadStatus() {
            return PortIO.ReadPort8(PbxPorts.REGISTER);
        }

        public override int ReadStatus() {
            return (int)PortIO.ReadPort8(PbxPorts.REGISTER);
        }

        public override bool IsBusy() {
            // overrid base class method
            return PbxIsBusy();
        }

        public override void Close() {
            return;
        }

		public bool PbxIsBusy() {
            // specific to inverted status bits of this device
			if ((PbxReadStatus() & PbxStatusBits.NotBUSY) == 0) {
				return true;
			}
			else {
				return false;
			}
		}

		public bool PbxIsEmpty() {
			if ((PbxReadStatus() & PbxStatusBits.FifoNotEMPTY) == 0) {
				return true;
			}
			else {
				return false;
			}
		}

		public bool PbxIsFull() {
			if ((PbxReadStatus() & PbxStatusBits.FifoNotFULL) == 0) {
				return true;
			}
			else {
				return false;
			}
		}

		public bool PbxHasError() {
			if ((PbxReadStatus() & PbxStatusBits.NotParityERROR) == 0) {
				return true;
			}
			else {
				return false;
			}
		}

        public int GetFifoItemCount() {
            int count = 0;
            PbxRetransmit();
            for (int i = 0; i < _fifoSize; i++) {
                if (!PbxIsEmpty()) {
                    PbxReadFifo();
                    count++;
                }
                else {
                    break;
                }
            }
            return count;
        }

		#endregion Public Methods

		#region Private Methods
		//
		/// <summary>
		/// Destructively returns the size of the FIFO.
		/// </summary>
		/// <returns></returns>
		private int GetFifoSize() {
			int size = -1;
			int block = _maxFifoSize;
			PbxReset();
			for (int i = 0; i < block; i++) {
				PbxWriteFifo(0x55);
				if (PbxIsFull()) {
					size = i + 1;
					return size;
				}
			}
			return block;	// if got here, at least this big
		}

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

			while (clk > PBMAXCLK) {			 // if too long for one pbx word */
				clk = clk - PBMAXCLK;            // break into multiple words */
				storepulse(pulse, PBMAXCNT);
				pulse = (UInt16)(pulse & ~(PbxPulseBits.SAMPLE));  // turn off sample bit for remainder */
			}
			if ((pulse & PbxPulseBits.SAMPLE) != 0)
				minclk = MINSAMPTIME / CLKPER;
			else
				minclk = PBMINCLK;
			if (clk < minclk) {						// if remaining clock less than min */
				xcnt = (int)(minclk - clk);			// try to steal from previous word */
				prevpls = (UInt16)(_fifoArray[_index - 1] & 0xff00);
				prevcnt = (_fifoArray[_index - 1] & 0x00ff);
				if (((pulse & PbxPulseBits.SAMPLE) == 0) &&
					((prevpls & ~(PbxPulseBits.SAMPLE)) == (pulse & ~(PbxPulseBits.RETRANS))) &&
					(prevcnt >= (xcnt + PBMINCNT))) {
					erasepulse();
					storepulse(prevpls, (prevcnt - xcnt));
					storepulse(pulse, (minclk - PBXCNT));
				}
				else {
					throw new ApplicationException("\n*** Pulse duration less than minimum ***");
				}
			}
			else
				storepulse(pulse, (int)(clk - PBXCNT));

			/* final check on pulse duration  added DAC 950215 */
			/* current pulse */
			if (_index > 0) {
				if ((_fifoArray[_index - 1] & PbxPulseBits.SAMPLE) != 0)
					minclk = MINSAMPTIME / CLKPER;
				else
					minclk = PBMINCLK;
				if (((_fifoArray[_index - 1] & 0x00ff) + PBXCNT) < minclk) {
					throw new ApplicationException("\n*** Pulse duration less than minimum ***");
				}
			}
			/* previous pulse */
			if (_index > 1) {
				if ((_fifoArray[_index - 2] & PbxPulseBits.SAMPLE) != 0)
					minclk = MINSAMPTIME / CLKPER;
				else
					minclk = PBMINCLK;
				if (((_fifoArray[_index - 2] & 0x00ff) + PBXCNT) < minclk) {
					throw new ApplicationException("\n*** Pulse duration less than minimum ***");
				}
			}
			return ;
		}

		private void erasepulse() {
			int clk;

			_index--;
			clk = (_fifoArray[_index] & 0x00ff) + PBXCNT;
			_currClock = _currClock - clk;
			return;
		}

		private void storepulse(UInt16 pulse, int count) {
			if ((_index + 1) > _fifoSize) {
				throw new ApplicationException("PBX ARRAY OVERFLOW  ");
			}
			_fifoArray[_index] = (UInt16)(pulse | (UInt16)count);
			_index++;
			_currClock = _currClock + count + PBXCNT;
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

			int lasttim;
			UInt16 lastpls;

			if (_currTime > _nIpp*_ipp) {
				throw new ApplicationException("A PBX PULSE GOES BEYOND IPP  ");
			}
			makepulse(0,((_nIpp*_ipp)-_currTime));
			lastpls =   (UInt16)(_fifoArray[_index-1] & 0xff00);
			lasttim = ((_fifoArray[_index - 1] & 0x00ff) + PBXCNT) * CLKPER;
			if (_currIpp == _nIpp)
				// set retrans in last word
				lastpls = (UInt16)(lastpls | PbxPulseBits.RETRANS); 
			erasepulse();
			makepulse(lastpls,lasttim);
			_currTime = _currIpp*_ipp;
			return;
		}

		private void CheckLast() {

			int id;
			UInt16 prevpls,prevcnt,lastpls,lastcnt,xcnt;
			int totper;

			id = _index - 1;
			prevcnt = (UInt16)(_fifoArray[id - 1] & 0x00ff);
			if ((int)prevcnt < (MINLASTCLK-PBXCNT)) {
				// try to steal from last word:
				xcnt = (UInt16)(MINLASTCLK - PBXCNT - prevcnt);   
				prevpls = (UInt16)(_fifoArray[id - 1] & 0xff00);
				lastpls = (UInt16)(_fifoArray[id] & 0xff00);
				lastcnt = (UInt16)(_fifoArray[id] & 0x00ff);
				if (((lastpls & PbxPulseBits.SAMPLE) == 0) &&
					((prevpls & ~(PbxPulseBits.SAMPLE)) == (lastpls & ~(PbxPulseBits.RETRANS))) &&
					( lastcnt >= (xcnt+PBMINCNT)) ) {
					 // steal counts:  
					erasepulse();
					erasepulse();
					storepulse(prevpls,(prevcnt+xcnt));
					storepulse(lastpls, (lastcnt - xcnt));
				 }
				 else if (((lastpls & PbxPulseBits.SAMPLE) == 0) && 
						(lastcnt >= PBMINCNT+(MINLASTCLK) ) ) {
					/* split last pulse: */       
					erasepulse();
					storepulse((UInt16)(lastpls & ~PbxPulseBits.RETRANS), (MINLASTCLK - PBXCNT));
					storepulse(lastpls, (lastcnt - MINLASTCLK));
				 }
				else {
					throw new ApplicationException("Cannot make last pulse");
				}
			}
			totper = _currClock*CLKPER;    /* check that ipp is correct */
			if (totper != _nIpp*_ipp) {
			  throw new ApplicationException("PBX array count wrong");
			}
			return;
		}

		private void LoadPbx() {
			//int i,j;
			UInt16 rwrd;
			UInt16 status;

            if (!_hardwareExists) {
                return;
            }

            PbxReset();
			PbxReset();
			for (int i = 0; i < _index; i++) {
				PbxWriteFifo(_fifoArray[i]);
			}
			//
			PbxRetransmit();
            if (_hardwareExists) {
				// if there is a PBX card, read back a few words and check
                status = PbxReadStatus();
				for (int i = 0; i < _index; i++) {
					if (_fifoArray[i] != (rwrd = PbxReadFifo())) {
						string err = string.Format("FIFO read error word #{0}: {1:x} {2:x}", i + 1, _fifoArray[i], rwrd);
						throw new ApplicationException(err);
					}
				}
			}

			/*
			// debugging:
			string msg = "";
			for (int i = 0; i < 10; i++) {
				msg += " 0x" + _fifoArray[i].ToString("x");
			}
			MessageBox.Show(msg);
			*/
			
			PbxRetransmit();
			//  

			for (int j = 0; j < 10; j++) {
				StartPulses();

				// outp(PB_REG_PORT,PB_START);
				status = PbxReadStatus();
				//if ((status & 0x09) != 0x08) {   /* if parity error or not busy */
                if (PbxHasError() || !PbxIsBusy()) {
					if (j == 9) {
						throw new ApplicationException("PBX Start Error, status = 0x" + status.ToString("x"));
					}
					PbxReset();
					for (int i = 0; i < _index; i++) {
						PbxWriteFifo(_fifoArray[i]);
					}
				}
				else
					break;
			}
			return;
		}

		//
		#endregion Private Methods
	}
}
