using System;
using System.Collections.Generic;
using System.Threading;
using System.ComponentModel;
using System.Reflection;

using DAQCOMLib;
using DACarter.Utilities;
using DACarter.PopUtilities;

namespace DACarter.NOAA.Hardware {
    /// <summary>
    /// Class to operate DAQ 3000USB series boards from IOTech.
    /// Runs only on 32-bit systems.
    /// </summary>
    public class DAQBoardIOTech : DAQDevice {

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
        private DAQCOMLib.IDigitalIO[] _DIO;
        private DAQCOMLib.IPort[] _DIOPortA, _DIOPortB, _DIOPortC;
        private _IAcqEvents_CompletionStatusChangedEventHandler _CompletionStatusChanged;

        private int _totalDataSamples;  // total samples for all devices
        private TimerCallback _timerDelegate;
        private bool _aborted;
        private int _maxDataAtOnce;
        private int _maxScansAtOnce;
        private int _maxNSpecAtOnce;
        private Exception _acqException;

        //private int _nScansAtOnce, _iScansAcquired;
        //private int _nDataAtOnce;
        private bool _scanIsCompleted;

        private System.Threading.Timer _progressTimer;
        private int TimerInterval = 250;
        private int TimerDelay = 250;

        private AcqCompletionStatus CompletionStatus;


        // background worker threads
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

        /// <summary>
        /// Constructor
        /// </summary>

        public DAQBoardIOTech() {
            CreateDevices();
        }

        public DAQBoardIOTech(PopParameters parameters) {

            CreateDevices();

            if ((parameters != null) && (_numDevices > 0)) {

                // create sampling order for each receiver, based on DAQ serial number order listed in par file
                _parameters = parameters;

                RxID = new PopUtilities.PopParameters.RxIDParameters[_numDevices];

                int nrx = _parameters.SystemPar.RadarPar.ProcPar.NumberOfRx;
                int nrxDim = _parameters.SystemPar.RadarPar.ProcPar.RxID.Length;
                if (nrxDim < nrx) {
                    throw new ApplicationException("NRxDim less than NRx in Parameters ProcPar.");
                }
                if (_numDevices > nrxDim) {
                    throw new ApplicationException("ProcPar.RxID.Length less than number of attached DAQ.");
                }

                if (_numDevices == 1) {
                    // if only one RX, do not require that its serial number be specified in setup system page
                    RxID[0].RxIDName = _serialNumbers[0].ToString();
                    RxID[0].iSampleOrder = 0;
                }
                else {
                    // otherwise get associated SN for each RX from setup system page
                    for (int i = 0; i < _numDevices; i++) {
                        int rxNumber = int.Parse(_parameters.SystemPar.RadarPar.ProcPar.RxID[i].RxIDName);
                        RxID[i].RxIDName = _parameters.SystemPar.RadarPar.ProcPar.RxID[i].RxIDName;
                        RxID[i].iSampleOrder = _serialNumbers.IndexOf(rxNumber);
                    }
                }
            }



        }

        private void CreateDevices() {
            CompletionStatus = AcqCompletionStatus.acsPending;

            _externalMatrix = null;

            //MessageBoxEx.Show("In CreateDevices ", 5000);
            CreateComObjects();
            //MessageBoxEx.Show("After CreatCOMObjects ", 5000);
            InitDevice();
            //MessageBoxEx.Show("After InitDevice ", 5000);

            _needsSetup = true;

            // initialize default property values
            _totalScans = 0;
            _totalDataSamples = 0;
            _maxDataAtOnce = 5242880 * 2;
            //_maxDataAtOnce = 5242880 * 3;
            _maxScansAtOnce = 0;
            _maxAnalogInput = VoltageRange.Volts10;
            _analogInputUnits = MeasurementUnits.Raw;
            _allDataCompleted = false;
            _progress = 0;
            _aborted = false;
            _maxNSpecAtOnce = -1;

            // create timer with infinite due time and infinite period
            _timerDelegate = new TimerCallback(TimerTick_GetProgress);
            // must call _progressTimer.Dispose() when finished:
            _progressTimer = new Timer(_timerDelegate);

            // Create background thread object for Fetching data from DAQ;
            FetchDataThread = new SimpleWorkerThread();
            FetchDataThread.SetWorkerMethod(FetchData);
        }

        /// <summary>
        /// Initialize DAQ device on construction
        /// </summary>
        protected void InitDevice() {

            Assembly daclib = Assembly.GetAssembly(typeof(DAQCOMLib.IDevice));
            string daqlibFile = daclib.ToString();
            _DAQLibrary = daqlibFile;
            //MessageBoxEx.Show("daclibFile = " + daqlibFile, 5000);

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

            //MessageBoxEx.Show("SysDevices count = " + _SysDevices.Count.ToString(), 5000);
            if (_SysDevices.Count < 1) {
                throw new ApplicationException("No valid DAQ device attached");
            }
            // take first device from list
            bool foundDevice = true;
            _deviceNames = new List<string>();
            _deviceTypes = new List<string>();
            _deviceSerialNumberLabels = new List<string>();
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

                    // device name from library combines type and serial number
                    string name = _SysDevices[i].Name;
                    int i1 = name.IndexOf('{');
                    int i2 = name.IndexOf('}');
                    string serial;
                    try {
                        serial = name.Substring(i1 + 1, i2 - i1 - 1);
                    }
                    catch {
                        // probably not a real DAQ device.
                        // Display full name:
                        serial = name;
                    }
                    _deviceSerialNumberLabels.Add(serial);
                }
            }

            // put serial numbers int base class int List
            int numDev = _deviceSerialNumberLabels.Count;
            _serialNumbers.Clear();
            foreach (string sn in _deviceSerialNumberLabels) {
                int snum;
                bool ok = int.TryParse(sn, out snum);
                if (ok) {
                    _serialNumbers.Add(snum);
                }
                else {
                    break;
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

            _numDevices = _DaqBoards.Count;

            // setup digital IO access for 1st daqBoard
            // SupportedDigitalIOs dios = _Dev.SupportedDigitalIOs;  // for debug info


            _DIO = new IDigitalIO[_numDevices];
            _DIOPortA = new IPort[_numDevices];
            for (int i = 0; i < _numDevices; i++) {
                _DIO[i] = _DaqBoards[i].DigitalIOs.Add(
                            DigitalIOType.diotDirectP2,
                            DeviceBaseAddress.dbaP2Address0,
                            DeviceModulePosition.dmpPosition0);
                _DIOPortA[i] = _DIO[i].Ports[1];  // yes, the 3 ports are at index 1-3
                //_DIOPortB[i] = _DIO[i].Ports[2];
                //_DIOPortC[i] = _DIO[i].Ports[3];
            }
            //_DIOPortA.Write(0xff);


            return;

        }  //end InitDevice()


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

        }

        /// <summary>
        /// Setup DAQ device based on user supplied parameters
        /// </summary>
        public override void Setup() {

            _maxScansAtOnce = _maxDataAtOnce / NumDevices;

            if (_totalScans < 1) {
                throw new ApplicationException("DaqBoard3000USB: Number of data samples has not been specified.");
            }

            //_nScansAtOnce = _totalScans;

            DAQCOMLib.DeviceBaseChannel devBaseChannel = DAQCOMLib.DeviceBaseChannel.dbcDaqChannel0;
            DAQCOMLib.DeviceModulePosition devModPos = DAQCOMLib.DeviceModulePosition.dmpPosition0;
            DAQCOMLib.AnalogInputType aiType = DAQCOMLib.AnalogInputType.aitDAQBRD3kUSBInputs;
            DAQCOMLib.Range range;
            DAQCOMLib.DaqBoard3kUSBChannels channels;

            _Config.ScanList.RemoveAll();

            //Set the number of scans to collect.
            _Acq.DataStore.BufferSizeInScans = _totalScans;
            _Config.ScanCount = _totalScans;

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
            double nScans = _Acq.AcquiredScans;
            _progress = nScans / (double)_totalScans;
            // notify others about progress update
            if (ProgressNotifier != null) {
                Delegate[] methods = ProgressNotifier.GetInvocationList();
                //_prevProgress = _progress;
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

        /// <summary>
        /// 
        /// </summary>
        public override void Start() {
            _progress = 0;
            _aborted = false;

            if (_needsSetup) {
                Setup();
            }

            // Arm and start if not manual start:
            _allDataCompleted = false;

            // Before arming all boards, clear enable lines that gate trigger on;
            for (int iBoard = 0; iBoard < _numDevices; iBoard++) {
                _DIOPortA[iBoard].Write(0x00);
            }

            _Acq.Arm();
            if (_testWithoutPbx) {
                _Acq.Start();
            }
           
            for (int iBoard = 0; iBoard < _numDevices; iBoard++) {
                // after arming all boards, set enable lines to gate trigger on;
                // Only one of these is used as the hardwired enable line
                _DIOPortA[iBoard].Write(0x01);
            }
            // start timer to tick every 250 ms
            _progressTimer.Change(TimerDelay, TimerInterval);
        }

        private object FetchData(object arg, out bool cancelled, BackgroundWorker bw) {

            //double memUsedMB31 = GC.GetTotalMemory(false) / 1000000.0;

            cancelled = false;
            //float a1 = (float)_dataSubArray.GetValue(_dataSubArray.Length - 1);
            // NOTE: this call seems to allocate additional memory:
            int ReturnedScans = _Acq.DataStore.FetchData(ref _dataSubArray, _Config.ScanCount);
            //float a2 = (float)_dataSubArray.GetValue(_dataSubArray.Length - 1);

            //double memUsedMB4 = GC.GetTotalMemory(false) / 1000000.0;

            try {
                if (_externalMatrix == null) {
                    _dataArray = (float[])_dataSubArray;
                    
                    int count = _Config.ScanCount;
                    for (int i = 0; i < count; i++) {
                        _dataArray[i] = (float)((ushort)_dataArray[i] - (ushort)32768);
                    }
                    
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

                _scanIsCompleted = true;

            }
            catch (Exception e) {
                //MessageBoxEx.Show(e.Message, "Error in FetchData()", 4000);
                throw e;
            }
            return ReturnedScans;
        }

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


        /// <summary>
        /// Abort the data acquisition process.
        /// Only needs to be called if you want to interupt the acquisition before it's done.
        /// </summary>
        public override void Stop() {
            // turn off timer
            _progressTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _Acq.Abort();
        }

        /// <summary>
        /// Make Close() equivalent to calling Dispose (need call only one of them)
        /// </summary>
        public override void Close() {
            Dispose(true);
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

            if (_disposed) {
                return;
            }

            if (disposing) {
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
                    _SysDevices = null;

                    // This is also required:
                    _progressTimer.Dispose();
                    _progressTimer = null;
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

    }  // end DAQBoard IOTech class
}
