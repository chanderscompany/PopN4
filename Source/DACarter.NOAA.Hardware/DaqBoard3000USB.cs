using System;
using System.Runtime;
using System.Threading;
using System.Collections.Generic;
using DAQCOMLib;
using DACarter.Utilities;
using System.ComponentModel;

namespace DACarter.NOAA.Hardware {

    public class DaqBoard3000USB : DataAcquisitionDevice {

        //
        // NOTE: In DAQ-speak a "scan" is a set of channels and devices that are sampled synchronously.
        //  So variables here with "scan" in the name are equivalent to number of samples from 1 device.
        //  I.e. one scan simultaneously acquires one sample from each device.
        //  Data points acquired = scans * nDevices
        //
        // There is only 1 channel
        // 
        // NOTE, Very Important:
        //  The Dispose() method must be called when finished with this object, else memory it uses will
        //      never be recovered.  Finalize() [in base class] does not get called automatically, 
        //      due to event handler:
        //          _CompletionStatusChanged = new _IAcqEvents_CompletionStatusChangedEventHandler(_Acq_CompletionStatusChanged);
        //          _Acq.CompletionStatusChanged += _CompletionStatusChanged;
        //

        #region Public Enums
        //
        public enum VoltageRange {
            Volts5,
            Volts10
        }

        public enum MeasurementUnits {
            Volts,
            Raw
        }

		public enum DIOPorts {
			NullPort,
			PortA,
			PortB,
			PortC
		}
        //
	    #endregion        
        
        #region Private Fields
        //
        private DAQCOMLib.DaqSystem _Sys;
        private DAQCOMLib.Acq _Acq;

        private List<DAQCOMLib.IDevice> _DaqDevices;
        private List<DAQCOMLib.IDaq3xxx> _DaqBoards;

        private DAQCOMLib.AvailableDevices _SysDevices;
        //private DAQCOMLib.IDevice _Dev;
        //private DAQCOMLib.IDaq3xxx daqBoard;  // IDaq3xxx is derived from IDevice
        private DAQCOMLib.Config _Config;
        private DAQCOMLib.ScanList _ScanList;
        private DAQCOMLib.IAnalogInput _AInput;
        private DAQCOMLib.IAnalogInputs _AInputs;
		private DAQCOMLib.IDigitalIO _DIO;
		private DAQCOMLib.IPort _DIOPortA, _DIOPortB, _DIOPortC;
        private _IAcqEvents_CompletionStatusChangedEventHandler _CompletionStatusChanged;

        private bool _needsSetup;

        // fields encapsulated as properties
        private int _totalScans;   // number of samples taken on each daq device
        private int _totalDataSamples;  // total samples for all devices
        private VoltageRange _maxAnalogInput;
        private MeasurementUnits _analogInputUnits;
        private System.Array _dataSubArray;
        private float[] _dataArray;
        private float[] _dummyArray;
        private bool _allDataCompleted;
        private int _progressNotifyInterval;  // percentage interval to be notified about progress changes
        private double _progress;			 // fractional progress completed (0.0 - 1.0)
        private double _prevProgress;
        private TimerCallback _timerDelegate;
        private bool _aborted;
		private int _maxDataAtOnce;
        private int _maxScansAtOnce;
		private int _maxNSpecAtOnce;
		private int _nSpecTotal;
		private bool _takeDataAllAtOnce;
        private bool _testWithoutPbx = false;
        private double[][][][] _externalMatrix;
        private List<string> _deviceNames;
        private List<string> _deviceTypes;
        private Exception _acqException;

        private int _nrx, _nspec, _npts, _ngates;
        private int _numDevices;

		private int _nScansAtOnce, _iScansAcquired;
		//private int _nDataAtOnce;
		private bool _scanIsCompleted;

        private System.Threading.Timer _progressTimer;
		private int TimerInterval = 250;
		private int TimerDelay = 250;

		// background worker threads
		SimpleWorkerThread Start2Thread;
		SimpleWorkerThread FetchDataThread;
        //
        #endregion private fields

        #region Public EventHandler Delegates
        //
        // Users of this class add their EventHandler to the delegate
        //  in order to be notified when sampling is done.
        //  signature: EventHandler(Object sender, EventArgs e);
        public event EventHandler AcquisitionCompleteEvent;
        // subscribe to this event if you want notification of progress changes
        //  at every ProgressNotifyInterval
        public event EventHandler ProgressNotifier;
        //
        #endregion Public EventHandler Delegates

        #region Properties
        //

        public bool TakeAllDataAtOnce {
            get { return _takeDataAllAtOnce; }
            set { _takeDataAllAtOnce = value; }
        }

        public Array BufferForSamples {
            get { return _dataSubArray; }
            set { _dataSubArray = value; }
        }

        public AcqCompletionStatus CompletionStatus;

		public DAQCOMLib.Acq Acq {
			get { return _Acq; }
		}

        public Exception AcqException {
            get { return _acqException; }
            //set { _acqException = value; }
        }

        /// <summary>
        /// if this array is provided by caller,
        /// then do not create internal DataArray
        /// </summary>
        public double[][][][] OutputMultiDimArray {
            get { return _externalMatrix; }
            set {
                throw new ApplicationException("DAQBoard external OutputArray not supported as of rev 3.17.");
                _externalMatrix = value;
                _nrx = _externalMatrix.GetLength(0);
                _nspec = _externalMatrix.GetLength(1);
                _npts = _externalMatrix.GetLength(2);
                _ngates = _externalMatrix.GetLength(3);
            }
        }

        public List<string> DeviceNames {
            get { return _deviceNames; }
            set { _deviceNames = value; }
        }

        public List<string> DeviceTypes {
            get { return _deviceTypes; }
            set { _deviceTypes = value; }
        }

        public int NDataSamplesPerDevice {
            // number of samples taken on each daq device
            get { return _totalScans; }
            set {
                int oldValue = _totalScans;
                // need to call Setup() if property changed
                if (oldValue != value) {
                    _totalScans = value;
                    _needsSetup = true;
                }
                _totalDataSamples = _totalScans * NumDevices;
            }
        }
		public int MaxNSpecAtOnce {
			get { return _maxNSpecAtOnce; }
		}
		public int MaxDataAtOnce {
			get { return _maxDataAtOnce; }
			set {
				int oldValue = _maxDataAtOnce;
				if (oldValue != value) {
					_maxDataAtOnce = value;
					_needsSetup = true;
				}
			}
		}
		public int NSpec {
			get { return _nSpecTotal; }
			set {
				int oldValue = _nSpecTotal;
				if (oldValue != value) {
					_nSpecTotal = value;
					_needsSetup = true;
				}
			}
		}
		public double AcquiredSamples {
			get {
				if (_Acq != null) {
					return _Acq.AcquiredScans;
				}
				else {
					return 0.0;
				}
			}
		}
        public VoltageRange MaxAnalogInput {
            get { return _maxAnalogInput; }
            set {
                VoltageRange oldValue = _maxAnalogInput;
                // need to call Setup() if property changed
                if (oldValue != value) {
                    _maxAnalogInput = value;
                    _needsSetup = true;
                }
            }
        }
        public MeasurementUnits AnalogInputUnits {
            get { return _analogInputUnits; }
            set {
                MeasurementUnits oldValue = _analogInputUnits;
                // need to call Setup() if property changed
                if (oldValue != value) {
                    _analogInputUnits = value;
                    _needsSetup = true;
                }
            }
        }
        public float[] DataArray {
            get { return _dataArray; }
            //set { _dataSysArray = value; }
        }
        public bool DataIsAvailable {
            get { return _allDataCompleted; }
        }
        public bool Aborted {
            get { return _aborted; }
        }
        public double Progress {
            get { return _progress; }
        }
        public int ProgressNotifyInterval {
            get { return _progressNotifyInterval; }
            set { _progressNotifyInterval = value; }
        }
        public bool TestWithoutPbx {
            get { return _testWithoutPbx; }
            set { _testWithoutPbx = value; }
        }
        public int NumDevices {
            get {
                // set in constructor's InitDevice()
                return _numDevices;
                /*
                if (_DaqBoards != null) {
                    return _numDevices;
                }
                else {
                    return 0;
                }
                */
            }
        }

        //
        #endregion Properties

        #region Constructor
        //
        public DaqBoard3000USB() {

            Array aaa;
            aaa = new float[100];

            // TODO: debug:
            //Console.Beep(440, 500);
            //Console.Beep(660, 500);
            _dummyArray = null;
            //_dummyArray = new float[30000000];

            CompletionStatus = AcqCompletionStatus.acsPending;

            _externalMatrix = null;

			_takeDataAllAtOnce = false;

            CreateComObjects();
            InitDevice();

            _needsSetup = true;

            // initialize default property values
            _totalScans = 0;
            _totalDataSamples = 0;
            _maxDataAtOnce = 5242880 * 2;
            //_maxDataAtOnce = 5242880 * 3;
            _maxScansAtOnce = 0;
            _maxAnalogInput = VoltageRange.Volts10;
            _analogInputUnits = MeasurementUnits.Volts;
            _allDataCompleted = false;
            _progress = 0;
            _prevProgress = -2.0;
            _aborted = false;
			_maxNSpecAtOnce = -1;
			_nSpecTotal = -1;

			_nScansAtOnce = 0;
			_iScansAcquired = 0;

            // TODO: debug
            
            // create time with infinite due time and infinite period
            _timerDelegate = new TimerCallback(TimerTick_GetProgress);
            // must call _progressTimer.Dispose() when finished:
            _progressTimer = new Timer(_timerDelegate);
            
			// Create background thread object for Fetching data from DAQ;
			FetchDataThread = new SimpleWorkerThread();
			FetchDataThread.SetWorkerMethod(FetchData);

			// Create background thread object for acquiring data in chunks
			Start2Thread = new SimpleWorkerThread();
			Start2Thread.SetWorkerMethod(Start2);
            Start2Thread.SetCompletedMethod((a,b,c,d) => Start2Completed(a, b, c, d));
            
        }

        public void Close() {
            _Sys.Close();
        }
        //
        #endregion Constructor

        #region Private Methods

        ///////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        private void CreateComObjects() {
            //Create DaqCOM objects

            // dac TODO: sometimes hangs up here:
            _Sys = null;
            _Sys = new DaqSystem();
            _Acq = _Sys.Add();
            _Config = _Acq.Config;
            _ScanList = _Config.ScanList;
            _SysDevices = _Acq.AvailableDevices;
            // This event handler must be removed before DaqBoard3000USB object can be GC'ed
            _CompletionStatusChanged = new _IAcqEvents_CompletionStatusChangedEventHandler(_Acq_CompletionStatusChanged);
            _Acq.CompletionStatusChanged += _CompletionStatusChanged;

			//_Acq.AcqErrorEvent += new _IAcqEvents_AcqErrorEventEventHandler(OnAcquisitionError);
			//_Acq.AcqStateChanged += new _IAcqEvents_AcqStateChangedEventHandler(OnAcqStateChanged);
			//_Acq.ActiveChanged += new _IAcqEvents_ActiveChangedEventHandler(OnAcqActiveChanged);

		}  // end CreateComObjects

        ///////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        private void InitDevice() {

            if (_DaqBoards == null) {
                _DaqBoards = new List<IDaq3xxx>();
            }
            else {
                _DaqBoards.Clear();
            }
            if (_DaqDevices == null) {
                _DaqDevices = new List<IDevice>();
            }
            else {
                _DaqDevices.Clear();
            }

            DAQCOMLib.IDevice daqDev;
            DAQCOMLib.IDaq3xxx daqBoardFound;

            // setting buffer size meaningful only in BufferMode == dbmDataStore
            _Acq.DataStore.AutoSizeBuffers = false;
            //_Acq.DataStore.BufferSizeInScans = 1024;
            _Acq.DataStore.IgnoreDataStoreOverruns = false;
            _Acq.DataStore.IgnoreDriverOverruns = false;
            _Acq.DataStore.IgnoreDeviceOverruns = false;
            ///////
            DataBufferMode bufferMode = _Acq.DataStore.BufferMode; // default is dbmDataStore
            //_Acq.DataStore.BufferMode = DataBufferMode.dbmDriver;
            _Acq.DataStore.BufferMode = DataBufferMode.dbmDataStore;

            if (_SysDevices.Count < 1) {
                throw new ApplicationException("No valid DAQ device attached");
            }
            // take first device from list
			bool foundDevice = true;
            _deviceNames = new List<string>();
            _deviceTypes = new List<string>();
            /*
            for (int i = 1; i <= _SysDevices.Count; i++) {
                _deviceNames.Add(_SysDevices[i].Name);
                _deviceTypes.Add(_SysDevices[i].DeviceType.ToString());
            }
            */
            for (int i = 1; i <= _SysDevices.Count; i++) {
                foundDevice = true;
                daqDev = _SysDevices.CreateFromIndex(i);
                switch (daqDev.DeviceType) {
					case DAQCOMLib.DeviceType.dtDaqBoard3001USB: break;
					case DAQCOMLib.DeviceType.dtDaqBoard3005USB: break;
					case DAQCOMLib.DeviceType.dtDaqBoard3031USB: break;
					case DAQCOMLib.DeviceType.dtDaqBoard3035USB: break;
					default:
						foundDevice = false;
						break;
						//throw new ApplicationException("This software works with the DaqBoard3000 USB series only!");
				}
				if (foundDevice) {
                    daqBoardFound = (DAQCOMLib.IDaq3xxx)daqDev;   //this is the hardware specific reference,
                                                                //  which has additional properties specific to the Daq3000
                    _DaqBoards.Add(daqBoardFound);

                    _deviceNames.Add(_SysDevices[i].Name);
                    _deviceTypes.Add(_SysDevices[i].DeviceType.ToString());
                    //daqBoard = daqBoardFound;
					//break;
				}
			}
			if (!foundDevice) {
				throw new ApplicationException("This software works with the DaqBoard3000 USB series only!");
			}

            foreach (IDaq3xxx daqBoard in _DaqBoards) {
                daqBoard.Open();
                bool isOpen = daqBoard.IsOpen;
                if (!isOpen) {
                    throw new ApplicationException("Cannot Open daqBoard");
                }
                _AInputs = daqBoard.AnalogInputs;
            }

			// setup digital IO access for 1st daqBoard
			// SupportedDigitalIOs dios = _Dev.SupportedDigitalIOs;  // for debug info
            _DIO = _DaqBoards[0].DigitalIOs.Add(
						DigitalIOType.diotDirectP2,
						DeviceBaseAddress.dbaP2Address0,
						DeviceModulePosition.dmpPosition0);
			_DIOPortA = _DIO.Ports[1];  // yes, the 3 ports are at index 1-3
			_DIOPortB = _DIO.Ports[2];
			_DIOPortC = _DIO.Ports[3];
			//_DIOPortA.Write(0xff);

            _numDevices = _DaqBoards.Count;

			return;
        }  //end InitDevice()

		/// <summary>
		/// This method is called every time the timer ticks.
		/// It reads the board to determine progress (fraction of scans completed).
		/// And passes progress to anyone who subscribes to ProgressNotifier event.
		/// When acquiring all data at once time, that event is the only thing we
		/// can use to determine if scan has been aborted, so we must always fire event,
		/// even if board has stopped running (e.g. because of samples being turned off).
		/// </summary>
		/// <param name="stateInfo"></param>
        private void TimerTick_GetProgress(object stateInfo) {
			// TODO: dac: note display problem: iDataAcquired updates before AcquiredScans is reset to 0
            double nScans = _Acq.AcquiredScans + _iScansAcquired;
            _progress = nScans / (double)_totalScans;
            // notify others about progress update
            if (ProgressNotifier != null) {
				Delegate[] methods = ProgressNotifier.GetInvocationList();
				_prevProgress = _progress;
				EventArgs args = new EventArgs();
				// dac: can cause exception thrown from ReportProgress
				//	if worker has already completed at this point
				//	happens mostly with ABORT button during dwell
				try {
					ProgressNotifier(this, new EventArgs());
				}
				catch (Exception e) {
						string message = e.Message;
				}
            }
        }
        
        #endregion Private Methods

        #region Public Methods
        //


		public void NotifyProgress() {
			ProgressNotifier(this, new EventArgs());
		}


        ///////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        public void SetUp() {

            _maxScansAtOnce = _maxDataAtOnce / NumDevices;

			if (_totalScans < 1) {
				throw new ApplicationException("DaqBoard3000USB: Number of data samples has not been specified.");
			}
			if (_nSpecTotal < 1) {
				throw new ApplicationException("DaqBoard3000USB: NSpec has not been specified.");
			}

			// compute how many spectra we can take during first acquire:
			if (_takeDataAllAtOnce) {
				_nScansAtOnce =  _totalScans;
			}
			else {
                int pointsPerSpec = _totalScans / _nSpecTotal;
				int nSpecAtOnce;
                if (_totalScans <= _maxScansAtOnce) {
                    _nScansAtOnce = _totalScans;
				}
				else {
                    // TODO: dac: handle fnspec < 1
                    double ff = (double)_maxScansAtOnce / (double)_totalScans;
					double fnSpec = ff * _nSpecTotal;
					nSpecAtOnce = (int)Math.Floor(fnSpec);
					_nScansAtOnce = pointsPerSpec * nSpecAtOnce;
				}
			}

            DAQCOMLib.DeviceBaseChannel devBaseChannel = DAQCOMLib.DeviceBaseChannel.dbcDaqChannel0;
            DAQCOMLib.DeviceModulePosition devModPos = DAQCOMLib.DeviceModulePosition.dmpPosition0;
            DAQCOMLib.AnalogInputType aiType = DAQCOMLib.AnalogInputType.aitDAQBRD3kUSBInputs;
            DAQCOMLib.Range range;
            DAQCOMLib.DaqBoard3kUSBChannels channels;

            _Config.ScanList.RemoveAll();

            //Set the number of scans to collect.
            //_Config.ScanCount = _nDataSamples;
            //_Acq.DataStore.BufferSizeInScans = _nDataSamples;
            _Acq.DataStore.BufferSizeInScans = _nScansAtOnce;
            _Config.ScanCount = _nScansAtOnce;

            //
            // now do some things for each daqBoard attached
            //
            foreach (IDaq3xxx daqBoard in _DaqBoards) {

                daqBoard.AnalogInputs.RemoveAll();
                daqBoard.DigitalIOs.RemoveAll();
                daqBoard.SetPoints.RemoveAll();

                //all channels are found inside this analog input
                _AInput = daqBoard.AnalogInputs.Add(aiType, devBaseChannel, devModPos);

                channels = (DAQCOMLib.DaqBoard3kUSBChannels)daqBoard.AnalogInputs[1].Channels;
                channels[1].Name = "Ch 00";
                channels[1].DifferentialMode = false;

                SupportedUnits supportedUnits = channels[1].SupportedUnits;
                SupportedUnit unit0 = supportedUnits[7];

                // set input range
                if (_maxAnalogInput == VoltageRange.Volts10) {
                    range = channels[1].Ranges.get_ItemByType(RangeType.rtBipolar10);
                }
                else if (_maxAnalogInput == VoltageRange.Volts5) {
                    range = channels[1].Ranges.get_ItemByType(RangeType.rtBipolar5);
                }
                else {
                    throw new ApplicationException("Unsupported Input Voltage Range");
                }
                range.UseAsSelectedRange();

                if (_analogInputUnits == MeasurementUnits.Volts) {
                    channels[1].EngrUnits = UnitType.utVolts;
                }
                else if (_analogInputUnits == MeasurementUnits.Raw) {
                    // raw units range from +0 at MinVoltageRange to 32767 at -0v
                    //          then -32768 at +0v to -1 at MaxVoltageRange
                    //          i.e. they are unsigned 16-bit ints converted to
                    //          signed 16-bit ints then converted to floats.
                    channels[1].EngrUnits = UnitType.utRawData;
                }
                else {
                    throw new ApplicationException("Unsupported Input Voltage Units");
                }
                channels[1].AddToScanList();

                if (_testWithoutPbx) {
                    daqBoard.ClockSource = DeviceClockSource.dcsInternal;
                }
                else {
                    //set sample clock source
                    // using external analog pacer clock XAPCR (on TB10)
                    daqBoard.ClockSource = DeviceClockSource.dcsExternal;
                }
            }

////
            float mn = _Config.MinScanRate;
            float mx = _Config.MaxScanRate;

            if (_testWithoutPbx) {
                _Acq.Config.ScanRate = 500000.0F;
                _Acq.Starts.get_ItemByType(DAQCOMLib.StartType.sttManual).UseAsAcqStart();
                _Acq.Stops.get_ItemByType(DAQCOMLib.StopType.sptScanCount).UseAsAcqStop();
                //MessageBoxEx.Show("Running DAQ without PBX", 1000);
            }
            else {

                //Specify the start/stop conditions.
                // using XTTLTRIG input to start (TTLTRIG on TB13)
                _Acq.Starts.get_ItemByType(DAQCOMLib.StartType.sttTTLRising).UseAsAcqStart();
                // stopping when all samples taken
                _Acq.Stops.get_ItemByType(DAQCOMLib.StopType.sptScanCount).UseAsAcqStop();
            }


            // setup is complete;
            _needsSetup = false;
            _allDataCompleted = false;

        }  // end Setup()

        ///////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Start the data acquisition process
        /// </summary>
		/// 
        public void Start() {
			if (_takeDataAllAtOnce) {
				Start0();			// execute Start0 in this thread
			}
			else {
				//bool cancelled;
				//Start2(null, out cancelled, null);		// execute Start2 in this thread
				_aborted = false;
				_iScansAcquired = 0;
				_progress = 0.0;
				_allDataCompleted = false;
				Start1();			// execute Start2 in another thread
			}

        }

		/// <summary>
		/// This Start() method acquires only the number of samples requested -
		///		in one scan - and then returns.
		///	The caller must determine how many times to call this and where to
		///		put the data.
		/// </summary>
		private void Start0() {
			_iScansAcquired = 0;
			_progress = 0;
			_prevProgress = -2.0;
			_aborted = false;
			if (_needsSetup) {
				SetUp();
			}
			// Arm and start if not manual start:
			_allDataCompleted = false;
            //Console.Beep(440, 100);
            _Acq.Arm();
            if (_testWithoutPbx) {
                _Acq.Start();
            }
            // start timer to tick every 250 ms
			_progressTimer.Change(TimerDelay, TimerInterval);
		}


		private void Start1() {
			if (Start2Thread.IsBusy) {
				Start2Thread.Cancel();
				for (int i = 0; i < 50; i++) {
					if (!Start2Thread.IsBusy) {
						break;
					}
					Thread.Sleep(20);
				}
			}
			// start Start2 method asynchronously and immediately return
            // _acqException is set if start2thread throws
            _acqException = null;
			Start2Thread.Go();
			// make sure dataIsAvailable flag is off when we return
			//_dataIsAvailable = false;
			return;
		}

		/// <summary>
		/// This Start() method stays here and waits for acquisition to finish
		///		and then starts daq again until all points are acquired.
		/// </summary>
		private object Start2(object arg, out bool cancelled, BackgroundWorker bw) {
			cancelled = false;
			_progress = 0;
			_prevProgress = -2.0;
			_aborted = false;
			if (_needsSetup) {
				SetUp();
			}

			// allocate array if needed
			//_dataArray = null;
            if ((_externalMatrix == null) && (!_takeDataAllAtOnce)) {
                // if filling internal data array
                // AND not taking all data at once
                if (_dataArray == null) {
                    int usedMemoryMB;
                    int requestMemMB = (int)((_totalDataSamples*4)>>20);
                    bool memOK = DacMemory.EnoughMemoryIsAvailable(requestMemMB, out usedMemoryMB);
                    _dataArray = new float[_totalDataSamples];
                }
                else if (_dataArray.Length < _totalDataSamples) {
                    int usedMemoryMB;
                    int requestMemMB = (int)((_totalDataSamples * 4) >> 20);
                    bool memOK = DacMemory.EnoughMemoryIsAvailable(requestMemMB, out usedMemoryMB);
                    _dataArray = null;
                    _dataArray = new float[_totalDataSamples];
                }
            }
            else {
                // if using external multidimensional array 
                // OR taking all data at once.
                // In latter case we use _dataSubArray
                _dataArray = null;
            }

			// start timer to tick every 250 ms
			_progressTimer.Change(TimerDelay, TimerInterval);
			_allDataCompleted = false;

			// init count of samples taken
			_iScansAcquired = 0;
			// number of samples for first scan
			_Config.ScanCount = _nScansAtOnce;

			// start sampling process
			_scanIsCompleted = false;
			_Acq.Arm();
            //throw new ApplicationException("Dummy EXCeption in Start2() DaqBoard3000USB");
            if (_testWithoutPbx) {
                _Acq.Start();
            }
			//Console.Beep(440, 100);

			while (!_allDataCompleted) {
				while (!_scanIsCompleted) {
					Thread.Sleep(100);
					if (bw != null) {
						if (bw.CancellationPending) {
							cancelled = true;
							break;
						}
					}
				}
                if (cancelled) {
					_progressTimer.Change(Timeout.Infinite, Timeout.Infinite);
					_Acq.Abort();
					AcqCompletionStatus acs = _Acq.CompletionStatus;
					int cnt = _Acq.DataStore.AvailableScans;
					if (cnt != 0) {
						_Acq.DataStore.FlushData(cnt);
					}
					/*
					_Acq.Disarm();
					AcqCompletionStatus acs = _Acq.CompletionStatus;
					int cnt = _Acq.DataStore.AvailableScans;
					if (cnt != 0) {
						_Acq.DataStore.FlushData(cnt);
					}
					*/
					break;
				}

				// garbage collect memory -
				//	helps reduce memory footprint of arm-fetch cycle
				GC.Collect();

				if (!_allDataCompleted) {
					Thread.Sleep(100);
					_scanIsCompleted = false;
					AcqCompletionStatus acs = _Acq.CompletionStatus;
                    // dac TODO: Arm() can throw an exception (E_FAIL returned by COM component)
                    //  Exceptions are caught in Start2Completed()
					_Acq.Arm();
                    Console.Beep(440, 100);
                    if (_testWithoutPbx) {
                        _Acq.Start();
                    }
                    //Console.Beep(440, 50);
				}

			}

			return null;
		}

        private void Start2Completed(object sender, object e, bool wasCanceled, Exception except) {
            if (except != null) {
                Console.Beep(880, 1000);
                _acqException = new ApplicationException(except.Message + " ** In DAQ Start2 ** ");
            }
            else {
                //Console.Beep(640, 100);
                _acqException = null;
            }
            //_acqException = except;
            int x = 0;
        }

        bool WeHaveEnoughMemory(int reqMemInMB, out int totalMemInMB) {

            long memBefore = GC.GetTotalMemory(false);
            totalMemInMB = (int)(memBefore >> 20);

            if (reqMemInMB > 0) {
                
                MemoryFailPoint mfp = null;
                try {
                    mfp = new MemoryFailPoint(reqMemInMB);
                }
                catch (InsufficientMemoryException e) {
                    return false;
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Abort the data acquisition process.
        /// Only needs to be called if you want to interupt the acquisition before it's done.
        /// </summary>
        public void Stop() {

            // TODO: what do we want here?
			//Dispose(true);
			/*
            //_Acq.Disarm();
            _Acq.Abort();
			//
			if (FetchDataThread.IsBusy) {
				FetchDataThread.Cancel();
			}
			*/
            // turn off timer
            _progressTimer.Change(Timeout.Infinite, Timeout.Infinite);
            // POPREV: added back for 3.17.1:
            _Acq.Abort();
        }

		/// <summary>
		/// Routines to write bytes or bits to DIO ports
		/// </summary>
		public void ClearPorts() {
			_DIOPortA.Write(0x0);
			_DIOPortB.Write(0x0);
			_DIOPortC.Write(0x0);
		}

		// clears all 8 bits of a Digital IO port
		public void ClearPort(DIOPorts port) {
			if (port == DIOPorts.PortA) {
				_DIOPortA.Write(0x0);
			}
			else if (port == DIOPorts.PortB) {
				_DIOPortB.Write(0x0);
			}
			else if (port == DIOPorts.PortC) {
				_DIOPortC.Write(0x0);
			}
		}

		// writes lower 8 bits of value to Digital IO port, 0=LSB
		public void WritePort(DIOPorts port, int value) {
			if (port == DIOPorts.PortA) {
				_DIOPortA.Write(value);
			}
			else if (port == DIOPorts.PortB) {
				_DIOPortB.Write(value);
			}
			else if (port == DIOPorts.PortC) {
				_DIOPortC.Write(value);
			}
			else {
				throw new ApplicationException("Invalid port in DAQ WritePort().");
			}
		}

		public int ReadPort(DIOPorts port) {
			int value = 0;
			if (port == DIOPorts.PortA) {
				_DIOPortA.Read(ref value);
			}
			else if (port == DIOPorts.PortB) {
				_DIOPortB.Read(ref value);
			}
			else if (port == DIOPorts.PortC) {
				_DIOPortC.Read(ref value);
			}
			else {
				throw new ApplicationException("Invalid port in DAQ ReadPort().");
			}
			return value;
		}

		// read single bit from port, 0 = LSB
		public int ReadPortBit(DIOPorts port, int bit) {
			if ((bit < 0) || (bit > 7)) {
				throw new ApplicationException(" DAQ DIOPort bit must be 0-7");
			}
			int byteValue = ReadPort(port);
			int mask = 0x01;
			mask = mask << bit;
			if ((byteValue & mask) == mask) {
				return 1;
			}
			else {
				return 0;
			}
		}


		// sets a single bit of DIO port without affecting other bits
		public void SetPortBit(DIOPorts port, int bit) {
			int newByte = 0;
			int pattern = 0;
			int oldByte = 0;
			if ((bit < 0) || (bit > 7)) {
				throw new ApplicationException("DAQ DIOPort bit must be 0-7");
			}
			pattern = 1 << bit;
			oldByte = ReadPort(port);
			newByte = oldByte | pattern;
			WritePort(port, newByte);
		}

		// clears single bit of DIO port without affecting other bits
		public void ClearPortBit(DIOPorts port, int bit) {
			int newByte = 0;
			int pattern = 0;
			int oldByte = 0;
			if ((bit < 0) || (bit > 7)) {
				throw new ApplicationException("DAQ DIOPort bit must be 0-7");
			}
			pattern = 1 << bit;
			pattern = ~pattern;
			oldByte = ReadPort(port);
			newByte = oldByte & pattern;
			WritePort(port, newByte);
		}

        //
        #endregion Public Methods

        #region Acquisition Complete Event Handler
		
        ///////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Completion Status Changed Event Handler
        /// </summary>
        private void _Acq_CompletionStatusChanged(int AcqIndex, AcqCompletionStatus newCompletionStatus) {

            CompletionStatus = newCompletionStatus;

            if (newCompletionStatus == AcqCompletionStatus.acsComplete) {

                // turn off timer
                _progressTimer.Change(Timeout.Infinite, Timeout.Infinite);

                //double memUsedMB0 = GC.GetTotalMemory(false) / 1000000.0;

                //Console.Beep(880, 100);
                //System.Media.SystemSounds.Beep.Play();
                AcqCompletionStatus status = _Acq.CompletionStatus;
                double nScans = _Acq.AcquiredScans;

                int availScans = _Acq.DataStore.AvailableScans;
                bool isActive = _Acq.Active;
                AcqState state = _Acq.AcqState;

                int bufsize = _Acq.DataStore.BufferSizeInScans;

				if (nScans > _Config.ScanCount) {
                    // this should not happen, but was problem with old driver versions
                    throw new ApplicationException("Number of scans overran scancount.");
                }

                if (availScans != _Config.ScanCount) {
                    throw new ApplicationException("availScans != _Config.ScanCount");
                }

                // allocate array if needed
				
                if (_dataSubArray == null) {
                    _dataSubArray = new float[_Config.ScanCount * _numDevices];
                }
                // must test for not equal (rather than <) because of Array.CopyTo
                // POPREV 3.17 CopyTo changed to Array.copy, so can use <
                else if (_dataSubArray.Length < _Config.ScanCount * _numDevices) {
                    _dataSubArray = null;
                    _dataSubArray = new float[_Config.ScanCount * _numDevices];
                }


                //double memUsedMB1 = GC.GetTotalMemory(false) / 1000000.0;

                //Console.Beep(1200, 500);
                // get data from Daq board
				try {
					bool cancelled;
					FetchData(null, out cancelled, null);
					//FetchDataThread.Go();  // execute FetchData() in different thread
				}
				catch (Exception e) {
					//MessageBoxEx.Show(e.Message, "Error in FetchDataThread", 4000);
					throw e;
				}
                //Console.Beep(2000, 500);

                //double memUsedMB2 = GC.GetTotalMemory(false) / 1000000.0;
					

            }
            else if ((newCompletionStatus == AcqCompletionStatus.acsAborted) ||
                (newCompletionStatus == AcqCompletionStatus.acsUserAborted)) {
                _aborted = true;
				if (FetchDataThread.IsBusy) {
					FetchDataThread.Cancel();
				}
            }

			//int exit = 1;
        }

    	#endregion Event Handlers 

		private object FetchData(object arg, out bool cancelled, BackgroundWorker bw) {

            //double memUsedMB31 = GC.GetTotalMemory(false) / 1000000.0;

            cancelled = false;
            //float a1 = (float)_dataSubArray.GetValue(_dataSubArray.Length - 1);
            // NOTE: this call seems to allocate additional memory:
            int ReturnedScans = _Acq.DataStore.FetchData(ref _dataSubArray, _Config.ScanCount);
            //float a2 = (float)_dataSubArray.GetValue(_dataSubArray.Length - 1);

            //double memUsedMB4 = GC.GetTotalMemory(false) / 1000000.0;
            
            try {
                if (_takeDataAllAtOnce) {
                    if (_externalMatrix == null) {
                        _dataArray = (float[])_dataSubArray;
                    }
                    else {
                        int i = 0;
                        for (int ispec = 0; ispec < _nspec; ispec++) {
                            for (int ipt = 0; ipt < _npts; ipt++) {
                                for (int igate = 0; igate < _ngates; igate++) {
                                    for (int irx = 0; irx < _nrx; irx++) {
                                        _externalMatrix[irx][ispec][ipt][igate] = (double)((float)_dataSubArray.GetValue(i++));
                                    }
                                }
                            }
                        }
                    }

					_allDataCompleted = true;
					if (AcquisitionCompleteEvent != null) {
						AcquisitionCompleteEvent(this, new EventArgs());
					}
				}
				else {
					// acquiring data in segments...
					// copy acquired data into total data array,
					//	beginning at index _iDataAcquired (#pts sampled before this)
					AcqCompletionStatus acs = _Acq.CompletionStatus;
					//Thread.Sleep(1000);
					//AcqCompletionStatus acs2 = _Acq.CompletionStatus;
                    if (_externalMatrix == null) {
                        //_dataSubArray.CopyTo(_dataArray, _iScansAcquired * _numDevices);
                        Array.Copy(_dataSubArray, 0, _dataArray, _iScansAcquired * _numDevices, _Config.ScanCount * _numDevices);
                        string name0 = _deviceNames[0];
                        string type0 = _deviceTypes[0];
                    }
                    else {
                        // TODO dac: get partial data to external array
                        int oneSpec = _npts * _ngates;  // scans req for one spec avg.
                        int totalScans = _nspec * oneSpec;
                        int subSetRemainder = _Config.ScanCount % oneSpec;
                        if (subSetRemainder != 0) {
                            throw new ApplicationException("NOT acquiring an integral number of spectral averages.");
                        }
                        int beginSpec = _iScansAcquired / oneSpec;
                        int specToGet = _Config.ScanCount / oneSpec;
                        if ((beginSpec + specToGet) > _nspec) {
                            throw new ApplicationException("Trying to acquire too many spectra.");
                        }
                        
                        int isrc = 0;
                        for (int ispec = beginSpec; ispec < specToGet; ispec++) {
                            for (int ipt = 0; ipt < _npts; ipt++) {
                                for (int igate = 0; igate < _ngates; igate++) {
                                    for (int irx = 0; irx < _nrx; irx++) {
                                            _externalMatrix[irx][ispec][ipt][igate] = (double)_dataSubArray.GetValue(isrc++);
                                    }
                                }
                            }
                        }
                        if (isrc != _Config.ScanCount * _nrx) {
                            throw new ApplicationException("Acquired/ScanCount mismatch.");
                        }
                    }


					_iScansAcquired += _Config.ScanCount;

                    if (_iScansAcquired >= _totalScans) {
						// we are done
						_progress = 1.0;
						_allDataCompleted = true;
						if (AcquisitionCompleteEvent != null) {
							AcquisitionCompleteEvent(this, new EventArgs());
						}
					}
					else {
						// have more data to get
                        int scansToDo = _totalScans - _iScansAcquired;
						if (scansToDo >= _nScansAtOnce) {
                            _Config.ScanCount = _nScansAtOnce;
						}
						else {
							_Config.ScanCount = scansToDo;
						}
						// turn timer back on
						_progressTimer.Change(TimerDelay, TimerInterval);
						// start another scan
						// but signal to start it in original thread
						//	with _scanIsCompleted
						//_Acq.Arm();
					}
				}
				_scanIsCompleted = true;

			}
			catch (Exception e) {
				//MessageBoxEx.Show(e.Message, "Error in FetchData()", 4000);
				throw e;
			}
			return ReturnedScans;
		}

        /// <summary>
        /// Dispose() method must be called when finished using a
        ///     DaqBoard3000USB object in order not to leak memory.
        /// Most of this code is probably not necessary, but the
        ///     following is absolutely required in order for the 
        ///     object to be released and memory garbage collected:
        ///         _Acq.CompletionStatusChanged -= _CompletionStatusChanged;
        ///         _progressTimer.Dispose();
        ///         (and maybe others)
        /// </summary>
        /// <param name="disposing"></param>
		protected override void Dispose(bool disposing) {
			if (disposing) {
				if (Start2Thread != null) {
					int cnt1 = _Acq.DataStore.AvailableScans;
					if (Start2Thread.IsBusy) {
						Start2Thread.Cancel();
						for (int i = 0; i < 50; i++) {
							// let thread stop
							if (!Start2Thread.IsBusy) {
								break;
							}
							Thread.Sleep(100);
						}
					}
					int cnt2 = _Acq.DataStore.AvailableScans;
					Start2Thread.Dispose();
					int cnt3 = _Acq.DataStore.AvailableScans;
				}
				if (_Acq != null) {
					int cnt1 = _Acq.DataStore.AvailableScans;
					_Acq.Abort();
					int cnt2 = _Acq.DataStore.AvailableScans;
					/**/
					if (_Sys != null) {
						_Sys.RemoveAll();
						_Sys.Close();
						_Sys = null;
					}
                    if (_Acq != null) {
                        _Config.ScanList.RemoveAll();
                        _Config = null;
                        _ScanList = null;
                        _SysDevices = null;
                        //_Acq.CompletionStatusChanged = null;


                        //TODO: This is the most important part of Dispose;
                        //  The event handler must be removed for GC to occur.
                        _Acq.CompletionStatusChanged -= _CompletionStatusChanged;
                        _Acq = null;
                    }
                    //_DaqBoards.Clear();
                    //_DaqDevices.Clear();
                    _SysDevices = null;

                    // This is also required:
                    _progressTimer.Dispose();
                    _progressTimer = null;
                    //_timerDelegate = null;
                    /**/
				}
				if (FetchDataThread != null) {
					if (FetchDataThread.IsBusy) {
						FetchDataThread.Cancel();
					}
					FetchDataThread.Dispose();
				}
			}
			base.Dispose(disposing);
		}



    }  // end class DaqBoard3000USB

}  // end namespace
