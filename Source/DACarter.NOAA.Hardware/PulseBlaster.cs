using System;
using DACarter.PopUtilities;
using DACarter.Utilities;
using System.Windows.Forms;
using System.Threading;

using SpinCore.SpinAPI;

namespace DACarter.NOAA.Hardware {

	public class PulseBlaster : PulseGeneratorDevice {

		#region Protected base class members
		//
		//protected PopParameters _parameters;
		//protected int _parSetIndex;
		//
		#endregion

		#region Properties
		//
		//

		#endregion Properties

		#region Local Variables
		//
		private PopParameters.TypeOfRadar _radarType;
		private PopParameters.BeamParameters _bmParameters;
		private PopParameters.FmCwParameters _fmcwParameters;
		private PopParameters _activeParameters;		// PopParameters currently actively running on PBX
		private int _activeParSetIndex;					// ParSetIndex currently actively running on PBX

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

        private bool _hardwareExists;

        private int[] _pulseStarts;			// gives start time, stop time, and pulse configuration for all TR signals (TRSIGS)
        private int[] _pulseStops;
        private UInt16[] _pulseBits;

        private struct instructionType {
            public int pulses;
            public int type;
            public int data;
            public int duration;
        };
        private int _fifoSize;
        private instructionType[] _instructions;

        private SpinAPI _spinAPI;

        //
		#endregion Local Variables

		#region PBX Constant Definitions
		//
        public struct PbxPulseBits
        {
            public const int TR =       0x0001;
            public const int BLANK =    0x0002;  // blanker bit
            public const int SAMPLE =   0x0004;
            public const int TX =       0x0008;
            public const int SYNCH =    0x0010;  // tr synch bit
            public const int PHASE =    0x0020;  // tx phase bit
            public const int ATTEN =    0x0040;  // rx attenuator bit
            public const int RETRANS =  0x0080;  // retransmit output bit 
        }

        public struct PbxStatusBits {
            //public const int NotBUSY = 0x01;
            public const int STOPPED = 0x01;
            public const int RESET =   0x02;
            public const int RUNNING = 0x04;
            public const int WAITING = 0x08;  // 
            //public const int FifoNotEMPTY = 0x02;
            //public const int FifoNotFULL = 0x04;
            //public const int NotParityERROR = 0x08;
        }


        private int CLKPER;             // pulsebox clock period in nanosec 
        private int MAX_NCODE;			// max code bit length 
        private int PC_NUM_CODES;		// number of possible pulse codes 
        private int TRSIGS;				// number of pulse signals during TR 
        //
        private int PBXCNT;             // extra clock count to add to pbx count to get actual duration 
        private int PBMINCNT;           // min count loaded in fifo word 
        private int PBMAXCNT;           // max count loaded into fifo word 
        private int PBMINCLK;           // min clocks per fifo word 
        private int PBMAXCLK;           // max clocks per fifo word 
        private int MINLASTCLK;         // min clocks for fifo word before retrans
        private int MINSAMPTIME;        // min length (nsec) for sample words 
        #endregion Constants

		#region Constructors
		//
		/// <summary>
		/// Constructors
		/// </summary>
		public PulseBlaster()
			: base() {
			ConstructPbx(false);
		}

		public PulseBlaster(PopParameters parameters, int parSetIndex)
			: base(parameters, parSetIndex) {
			ConstructPbx(false);
		}

        public PulseBlaster(bool PbxAlreadyExists)
			: base() {
			ConstructPbx(PbxAlreadyExists);
		}

		private void ConstructPbx(bool PbxAlreadyExists) {
			// if called with PbxAlreadyExists = true,
			//	then do not do anything that might disrupt pulse outputs
			//	Only use this instance to control other ports
            //  This option was for original pulse box card;
            //  For PulseBlaster do not use:
            PbxAlreadyExists = false;

            _spinAPI = new SpinAPI();
            _spinAPI.Init();
            Thread.Sleep(1000);
            bool exists = this.Exists();
            if (!this.Exists()) {
                throw new ApplicationException("PulseBlaster card not found");
            }
            int stat = ReadStatus();
            if (stat < 0) {
                // weird status; board may already be running
                // Apparently need some pause in here somewhere
                //  to make this reset work.
                //  TODO: find out why pause needed with spinAPI calls
                Thread.Sleep(500);
                _spinAPI.Reset();
                Thread.Sleep(500);
                _spinAPI = null;
                _spinAPI = new SpinAPI();
                _spinAPI.Init();
                Thread.Sleep(500);
            }
            int stat2 = ReadStatus();
            if (stat2 < 0) {
                string msg = "\n Board may be attached to another process.";
                throw new ApplicationException("PulseBlaster status = " + stat2.ToString() + msg);
            }
            else if (PbxIsBusy()) {
                StopPulses();
            }
            int stat3 = ReadStatus();
            // set an initial clock freq.
            // In future may be selectable in set-up
            // and call setclock again in LoadProgram
            //_spinAPI.ClockFrequency = 100.0;
            _spinAPI.SetClock(100.0);
            Thread.Sleep(100);
            if (PbxAlreadyExists) {
				_hardwareExists = true;
			}
			else {
				_hardwareExists = this.Exists();
				_activeParameters = null;
				_activeParSetIndex = -1;
                //_spinAPI.Stop();
                //StopPulses();
                InitPbxConstants();
            }
            //_fifoSize = 4096;
            //_fifoSize = 8*1024;  // max number of pulse patterns; not size of board memory (since we load loops)
            //_instructions = new instructionType[_fifoSize];

        }

		private void InitPbxConstants() {

            CLKPER = 10;      // 100 MHz clock
			MAX_NCODE = 32;
			PC_NUM_CODES = 7;
			TRSIGS = (6 + MAX_NCODE);
            MINSAMPTIME = 200;

            PBXCNT = 0;    // extra added to count to get actual clock cycles
            PBMINCNT = 6;
            PBMAXCNT = Int32.MaxValue / 2;
            PBMINCLK = ((PBMINCNT) + (PBXCNT));
            PBMAXCLK = ((PBMAXCNT) + (PBXCNT));
            MINLASTCLK = 6;
            MINSAMPTIME = 2*PBMINCNT*CLKPER;
            
            // index 0 = TR
            // index 1 = Sync
            // index 2 = Blanker
            // index 3 = TX
            // index 4 = ATTEN
            // index 5 = Samples
            //
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
		/// 
		/// </summary>
		/// <remarks>
		/// 
		/// </remarks>
		/// <returns></returns>
		public override bool Exists() {
			//ConstructPbx();
            if (_spinAPI.BoardCount > 0) {
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
			if ((_radarType == PopParameters.TypeOfRadar.FmCwSA) ||
                (_radarType == PopParameters.TypeOfRadar.FmCwDop)) {
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
            _sync = 700;
			_txIsOn = _parameters.SystemPar.RadarPar.TxIsOn;

			if ((_radarType == PopParameters.TypeOfRadar.FmCwSA) ||
                (_radarType == PopParameters.TypeOfRadar.FmCwDop)) {
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
				throw new ApplicationException("Hey: Pulsed Doppler for PBX card not implemented yet.");
			}

            _instructions = null;
            _fifoSize = _nSamples + 100;
            _instructions = new instructionType[_fifoSize];

			//
			// start creating pulse sequence
			//

            for (int i = 0; i < _fifoSize; i++) {
                _instructions[i].duration = 0;
                _instructions[i].pulses = 0;
            }
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
					Reset();
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

                int stat = ReadStatus();
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
            _spinAPI.Start();
            int status = _spinAPI.Status;
        }

		///////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override void StopPulses() {
            Thread.Sleep(10);
            _spinAPI.Stop();
            Thread.Sleep(100);
            int statA = ReadStatus();
            Clear();
            Thread.Sleep(1000);
            int statB = ReadStatus();
        }
		//

        /// <summary>
        /// This method override is required, but PulseBlaster reset
        ///     does nothing that we are really interested in
        ///     (it stops program and moves instruction pointer to beginning)
        /// </summary>
        public override void Reset() {
            //_spinAPI.Reset();
            //Thread.Sleep(100);
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Close() {
            //_spinAPI.Close();
        }

        /// <summary>
        /// Clear the digital output channels 
        /// </summary>
        private void Clear() {
            //return;
            int statA = ReadStatus();
            //_spinAPI.Stop();
            //int statB = ReadStatus();
            //_spinAPI.Reset();
            //int statC = ReadStatus();
            _spinAPI.Init();
            _spinAPI.SetClock(100.0);
            _spinAPI.StartProgramming(ProgramTarget.PULSE_PROGRAM);
            _spinAPI.PBInst(0x00, OpCode.CONTINUE, 0, 1, TimeUnit.ms);
            _spinAPI.PBInst(0x00, OpCode.STOP, 0, 1, TimeUnit.ms);
            _spinAPI.StopProgramming();
            _spinAPI.Reset();
            _spinAPI.Start();
            Thread.Sleep(100);
            int stat1 = ReadStatus();
            _spinAPI.Stop();
            int stat2 = ReadStatus();
            //_spinAPI.Reset();
            //int stat3 = ReadStatus();
        }



        /// <summary>
        /// Base class test for busy
        /// </summary>
        /// <returns></returns>
        public override bool IsBusy() {
            return PbxIsBusy();
        }

        #endregion Override Members

		
		#region Public Methods
		//


		public override int ReadStatus() {
            // bit 0 hi (1) = stopped
            // bit 1 hi (2) = reset
            // bit 2 hi (4) = running
            return _spinAPI.Status;
        }


		public bool PbxIsBusy() {
            if ((ReadStatus() & PbxStatusBits.RUNNING) != 0) {
				return true;
			}
			else {
				return false;
			}
		}


		public bool PbxHasError() {
            return false;
		}


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

		private void makepulse(int pulse, int nsTime) {

			int clk; 
			int minclk,xcnt,prevcnt;
			int prevpls;

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
				prevpls = _instructions[_index - 1].pulses;
                prevcnt = _instructions[_index - 1].duration/CLKPER;
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
				if ((_instructions[_index - 1].pulses & PbxPulseBits.SAMPLE) != 0)
					minclk = MINSAMPTIME / CLKPER;
				else
					minclk = PBMINCLK;
				if (((_instructions[_index - 1].duration)/CLKPER + PBXCNT) < minclk) {
					throw new ApplicationException("\n*** Pulse duration less than minimum ***");
				}
			}
			/* previous pulse */
            if (_index > 1) {
                if ((_instructions[_index - 2].pulses & PbxPulseBits.SAMPLE) != 0)
					minclk = MINSAMPTIME / CLKPER;
				else
					minclk = PBMINCLK;
				if (((_instructions[_index - 2].duration)/CLKPER + PBXCNT) < minclk) {
					throw new ApplicationException("\n*** Pulse duration less than minimum ***");
				}
			}
			return ;
		}

		private void erasepulse() {
			int clk;

			_index--;
			clk = (_instructions[_index].duration)/CLKPER + PBXCNT;
			_currClock = _currClock - clk;
			return;
		}

		private void storepulse(int pulse, int count) {
			if ((_index + 1) > _fifoSize) {
				throw new ApplicationException("PBX ARRAY OVERFLOW  ");
			}
			//_fifoArray[_index] = (pulse | count);
            _instructions[_index].pulses = pulse;
            _instructions[_index].duration = (count + PBXCNT) * CLKPER;
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
			int lastpls;

			if (_currTime > _nIpp*_ipp) {
				throw new ApplicationException("A PBX PULSE GOES BEYOND IPP  ");
			}
			makepulse(0,((_nIpp*_ipp)-_currTime));
			lastpls =  _instructions[_index-1].pulses;
			lasttim = ((_instructions[_index - 1].duration)/CLKPER + PBXCNT) * CLKPER;
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
			int prevpls,prevcnt,lastpls,lastcnt,xcnt;
			int totper;

			id = _index - 1;
			prevcnt = (_instructions[id - 1].duration)/CLKPER;
			if (prevcnt < (MINLASTCLK-PBXCNT)) {
				// try to steal from last word:
				xcnt = (UInt16)(MINLASTCLK - PBXCNT - prevcnt);   
				prevpls = (UInt16)(_instructions[id - 1].pulses);
				lastpls = (UInt16)(_instructions[id].pulses);
                lastcnt = (UInt16)(_instructions[id].duration)/CLKPER;
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
			int status;

            if (!_hardwareExists) {
                return;
            }

            status = ReadStatus();
            if (status < 0) {
                string msg = "PulseBlaster Status < 0\n Another process may be attached to card";
            }
            LoadProgram();
            /*
			for (int i = 0; i < _index; i++) {
				PbxWriteFifo(_fifoArray[i]);
			}
            */
			//
			//StopPulses();
            if (_hardwareExists) {
				// if there is a PBX card, read back a few words and check
                status = ReadStatus();
			}

			/*
			// debugging:
			string msg = "";
			for (int i = 0; i < 10; i++) {
				msg += " 0x" + _fifoArray[i].ToString("x");
			}
			MessageBox.Show(msg);
			*/
			
			//StopPulses();
			//  
            int stat2 = ReadStatus();
            StartPulses();
            int stat3 = ReadStatus();
            Thread.Sleep(10);
            if (!PbxIsBusy()) {
                throw new ApplicationException("PulseBlaster cannot start pulses.");
            }

            /*
			for (int j = 0; j < 10; j++) {
				StartPulses();

				// outp(PB_REG_PORT,PB_START);
                status = ReadStatus();
                if (PbxHasError() || !PbxIsBusy()) {
					if (j == 9) {
						throw new ApplicationException("PBX Start Error, status = 0x" + status.ToString("x"));
					}
					Reset();
                    LoadProgram();
				}
				else
					break;
			}
            */
			return;
		}

        /// <summary>
        /// Load instructions onto PulseBlaster board
        /// </summary>
        private void LoadProgram()
        {
            //_spinAPI.Close();
            _spinAPI.Init();
            //StopPulses();
            //_spinAPI.ClockFrequency = 100.0;
            _spinAPI.SetClock(100.0);
            int stat = ReadStatus();
            _spinAPI.StartProgramming(ProgramTarget.PULSE_PROGRAM);
            /*
            if (!_spinAPI.StartProgramming(ProgramTarget.PULSE_PROGRAM))
            {
                throw new ApplicationException("PulseBlaster Failed to start programming.");
            }
             * */

            int loopPattern = -1;
            int loopInstDuration = 0;
            int loopStartInst = -1;
            int loopCount = 0;
            int instDuration;
            int instPattern;
            bool isLastInstruction = false;

            foreach (instructionType inst in _instructions) {

                // NOTE: doesn't work if sample is last pattern of ipp

                instDuration = inst.duration;
                instPattern = inst.pulses;
                
                if (instDuration == 0) {
                    break;
                }


                OpCode opCode;
                int data;
                if ((instPattern & PbxPulseBits.RETRANS) != 0) {
                    // set opCode for last instruction:
                    opCode = OpCode.BRANCH;
                    data = 0;               // jump to instruction 0
                    isLastInstruction = true;
                    //_spinAPI.PBInst(instPattern, opCode, data, instDuration, TimeUnit.ns);
                }
                else {
                    // set opCode for normal instruction:
                    opCode = OpCode.CONTINUE;
                    data = 0;
                }

                if ((instPattern & PbxPulseBits.SAMPLE) != 0) {
                    // sample pulse instruction
                    // Are we working on a loop instruction?
                    if (loopCount > 0) {
                        if ((instPattern == loopPattern) && (loopInstDuration == instDuration)) {
                            loopCount++;
                        }
                        else {
                            // changing pattern and duration in the middle of the samples
                            // Ignore for now; continue with original pattern.
                            // Should only get here if blank ends during samples.
                            // Should load current loop and start another.
                            loopCount++;
                        }
                    }
                    else {
                        // start a samples loop
                        loopInstDuration = instDuration;
                        loopPattern = instPattern;
                        loopCount = 1;

                    }
                }
                else {
                    // normal instruction
                    if (loopCount > 0) {
                        // we have a loop instruction to finish first
                        int pattern1 = loopPattern;
                        int pattern2 = loopPattern & ~PbxPulseBits.SAMPLE;
                        int duration1 = loopInstDuration >= 400 ? 200 : PBMINCLK * CLKPER; // pulse on time
                        int duration2 = loopInstDuration - duration1;                      // pulse off time
                        loopStartInst = _spinAPI.PBInst(pattern1, OpCode.LOOP, loopCount, duration1, TimeUnit.ns);
                        _spinAPI.PBInst(pattern2, OpCode.END_LOOP, loopStartInst, duration2, TimeUnit.ns);
                    }
                    loopCount = 0;
                    // now load the current instruction:
                    _spinAPI.PBInst(inst.pulses, opCode, data, inst.duration, TimeUnit.ns);
                }

                if (isLastInstruction) {
                    break;
                }

            }

            _spinAPI.StopProgramming();
            /*
            if (!_spinAPI.StopProgramming()) {
                throw new ApplicationException("PulseBlaster Failed to stop programming.");
            }
             * */
        }

		//
		#endregion Private Methods
	}
}
