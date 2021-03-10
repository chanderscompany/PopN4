using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

using MccDaq;

namespace DACarter.NOAA.Hardware {

    public class DAQBoardMCC : DAQDevice {

        private MccDaq.MccBoard[] _DaqBoard;

        private IntPtr[] _memHandle;		        //  handle for memory allocated by Windows via MccDaq.MccService.WinBufAlloc()
        private short[] _chanArray;                 // list of channels to acquire
        private MccDaq.ChannelType[] _chanTypeArray;
        private MccDaq.Range[] _gainArray;          //  array to hold gain queue information
        private MccDaq.ScanOptions _options;
        private System.Threading.Timer _tmrCheckStatus;
        private TimerCallback _timerDelegate;
        private int _timerInterval;
        private int _winBufferSize;

        static private List<int> _initialSerialNumbers = new List<int>();


        // override (hide) base property to update progress value when read
        override public double Progress {
            get {
                short status;
                int curCount, curIndex;
                _progress = 0.0;
                int count = _totalScans;
                if ((_DaqBoard != null) && (_DaqBoard.Length > 0)) {
                    _DaqBoard[0].GetStatus(out status, out curCount, out curIndex, FunctionType.DaqiFunction);
                    _progress = (double)curIndex / count;
                }
                return _progress; 
            }
        }

        #region DLLImports
        /*
        InfoType = 2  // BOARDINFO
        BoardNum = Val(TextBox1.Text) 'you'll want to change this to the board number of your card.
        DevNum = 0  // for counter or DIO device on board
        ConfigItem = 224 // BIMFGSERIALNUM
        r = cbGetConfig(InfoType, BoardNum, DevNum, ConfigItem, ConfigVal)
        Label2.Text = ConfigVal.ToString


        <DllImport("cbw32.dll")> _
        Public Shared Function cbGetConfig(ByVal InfoType As Integer, ByVal BoardNum As Integer, _
         ByVal DevNum As Integer, ByVal ConfigItem As Integer, ByRef ConfigVal As Long) _
            As Long
        End Function

        <DllImport("cbw32.dll")> _
        Public Shared Function cbSetConfig(ByVal InfoType As Integer, ByVal BoardNum As Integer, _
         ByVal DevNum As Integer, ByVal ConfigItem As Integer, ByVal ConfigVal As Long) _
            As Long
        End Function
          
         * */

        [DllImport("cbw64.dll")]
        static extern long cbGetConfig(int infoType, int boardNum, int devNum, int configItem, ref long configVal);

        [DllImport("cbw32.dll", EntryPoint = "cbGetConfig")]
        static extern long cbGetConfig32(int infoType, int boardNum, int devNum, int configItem, ref long configVal);

        //[DllImport("cbw64.dll")]
        //static extern long cbSetConfig(int infoType, int boardNum, int devNum, int configItem, long configVal);

        #endregion DLLImports

        public DAQBoardMCC() {
            CreateDevices();
        }

        public DAQBoardMCC(PopUtilities.PopParameters parameters) {

            Assembly daclib = Assembly.GetAssembly(typeof(MccBoard));
            string daclibFile = daclib.ToString();
            //MessageBoxEx.Show("daclibFileMCC = " + daclibFile, 5000);
            _DAQLibrary = daclibFile;
            
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
            // before accessing MCC library,
            //  create cb.cfg file with info about attached devices
            DaqMccConfigFile cbFile = new DaqMccConfigFile();
            // MCC older versions looked here for cb.cfg:
            cbFile.Write(@"C:\ProgramData\Measurement Computing\DAQ\CB.CFG");
            // MCC versions 2.6 and above look here:
            if (Directory.Exists(@"C:\Program Files (x86)")) {
                cbFile.Write(@"C:\Program Files (x86)\Measurement Computing\DAQ\CB.CFG");
            }
            else {
                cbFile.Write(@"C:\Program Files\Measurement Computing\DAQ\CB.CFG");
            }
            _serialNumbers = cbFile.SerialNumbers;
            _numDevices = _serialNumbers.Count;
            if (_numDevices <= 0) {
                //return; 
            }

            //////
            bool needsRestart = false;
            if (_initialSerialNumbers.Count != 0) {
                if (_initialSerialNumbers.Count != _serialNumbers.Count) {
                    needsRestart = true;
                }
                for (int i = 0; i < _serialNumbers.Count; i++) {
                    if (_initialSerialNumbers[i] != _serialNumbers[i]) {
                        needsRestart = true;
                        break;
                    }
                }
            }
            if (needsRestart) {
                throw new ApplicationException("MCC DAQ devices have changed! MUST restart POPN application.");
            }
            else {
                _initialSerialNumbers.Clear();
                foreach (int sn in _serialNumbers) {
                    _initialSerialNumbers.Add(sn);
                }
            }
            //////

            _DaqBoard = new MccBoard[_numDevices];
            _memHandle = new IntPtr[_numDevices];
            _gainArray = new MccDaq.Range[1];
            _chanTypeArray = new MccDaq.ChannelType[1];
            _chanArray = new short[1];
            _timerDelegate = new TimerCallback(tmrCheckStatus_Tick);
            _tmrCheckStatus = new System.Threading.Timer(_timerDelegate);
            _timerInterval = 200;
            _winBufferSize = 0;

            bool boardFound = false;

            for (int i = 0; i < _numDevices; i++) {
                try {
                    _DaqBoard[i] = new MccDaq.MccBoard(i);
                    boardFound = GetBoardFullName(i);

                    // Surprisingly, DaqBoard can be created, 
                    //  with the correct serial number of a previous board,
                    //  even if USB is not currently connected. So we need to test the board itself:
                    //  (NOTE: this test should no longer be needed, now that we are creating our own 
                    //      correct and current cb.cfg file.)
                    if (boardFound) {
                        _DaqBoard[i].DConfigPort(DigitalPortType.FirstPortA, DigitalPortDirection.DigitalOut);
                        _DaqBoard[i].DConfigBit(DigitalPortType.FirstPortA, 0, DigitalPortDirection.DigitalOut);
                        ErrorInfo err = _DaqBoard[i].DBitOut(DigitalPortType.FirstPortA, 0, DigitalLogicState.Low);
                        if (err.Value != ErrorInfo.ErrorCode.NoErrors) {
                            boardFound = false;
                        }
                    }
                    if (!boardFound) {
                        throw new ApplicationException("Cannot talk to MCC DAQ device " + i.ToString() + ": " + _serialNumbers[i].ToString());
                    }
                }
                catch {
                    _DaqBoard[i] = null;
                    //throw new ApplicationException("Cannot create MCC DAQ device " + i.ToString() + ": " + _serialNumbers[i].ToString());
                }
            }

        }

        /////////////////////////////////////////////
        /// <summary>
        /// Puts MCC device type and serial number
        ///     in  format of IOTech sysDevices[i].Name
        ///     e.g. "deviceType{123456}"
        /// </summary>
        /// <param name="boardNumber"></param>
        /// <returns></returns>
        private bool GetBoardFullName(int boardNumber) {
            bool boardFound;
            string boardName = string.Empty;
            string boardModel = string.Empty;

            //MessageBox.Show("Inside GetBoardFullName");

            float revNum, vxdRevNum;
            ErrorInfo e2 = MccService.GetRevision(out revNum, out vxdRevNum);
            //ErrorInfo e1 = MccService.DeclareRevision(ref revNum);
            ErrorInfo err = MccService.GetBoardName(boardNumber, ref boardModel);
            if (err.Value != ErrorInfo.ErrorCode.NoErrors) {
                string msg1 = "MCC error code " + err.Value.ToString() + "  " + boardModel + " Bd# " + boardNumber.ToString();
                string msg2 = e2.Value.ToString() + " rev # " + revNum.ToString();
                //MessageBoxEx.Show(msg1,3000);
                //MessageBoxEx.Show(msg2, 3000);
                boardFound = false;
                throw new ApplicationException(msg1 + " \n " + msg2);
            }
            else {
                long serNum = BoardSerialNum(boardNumber);
                string serNumString = serNum.ToString();
                boardName = boardModel + "{" + serNumString + "}";
                //MessageBoxEx.Show("BoardFullName " + boardNumber.ToString() + "  " + boardName, 3000);
                _deviceNames.Add(boardName);
                _deviceSerialNumberLabels.Add(serNumString);
                _deviceTypes.Add(boardModel);
                boardFound = true;
            }
            return boardFound;
        }

        long BoardSerialNum(int boardNumber) {
            long serNum = 0;
            int bits = IntPtr.Size * 8;
            if (bits == 64) {
                cbGetConfig(2, boardNumber, 0, 224, ref serNum);
            }
            else {
                cbGetConfig32(2, boardNumber, 0, 224, ref serNum);
            }
            return serNum;
        }

        /////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        public override void Setup() {

            _allDataCompleted = false;

            //double memUsedMB00 = GC.GetTotalMemory(false) / 1000000.0;
            
            // take data from analog channel 0, +/-10v
            _chanArray[0] = 0;
            _chanTypeArray[0] = MccDaq.ChannelType.Analog;
            _gainArray[0] = MccDaq.Range.Bip10Volts;

            if (_dataArray != null) {
                // MCC device does not use _dataArray
                _dataArray = null;
            }
            if (_dataSubArray != null) {
                _dataSubArray = null;
            }

            if ((_intDataArray == null) || (_intDataArray.Length != _numDevices)) {
                _intDataArray = null;
                _intDataArray = new short[_numDevices][];
            }
            for (int iBoard = 0; iBoard < _numDevices; iBoard++) {
                if ((_intDataArray[iBoard] == null) || (_intDataArray[iBoard].Length != _numDevices)) {
                    _intDataArray[iBoard] = null;
                    _intDataArray[iBoard] = new short[_totalScans];
                }
            }

            //double memUsedMB01 = GC.GetTotalMemory(false) / 1000000.0;


            // Run acquisition in the background
            _options = ScanOptions.Background;

            if (_testWithoutPbx) {

            }
            else {
                // These options specify using TTLTRG as trigger and XAPCR as pacer clock (samples)
                _options = _options | MccDaq.ScanOptions.ExtTrigger | ScanOptions.ExtClock;

                // This sets trigger to rising edge of TTLTRG
                for (int iBoard = 0; iBoard < _numDevices; iBoard++) {
                    _DaqBoard[iBoard].SetTrigger(TriggerType.TrigPosEdge, 0, 0);
                    // configure bit0 digital I/O line as output
                    _DaqBoard[iBoard].DConfigPort(DigitalPortType.FirstPortA, DigitalPortDirection.DigitalOut);
                    _DaqBoard[iBoard].DConfigBit(DigitalPortType.FirstPortA, 0, DigitalPortDirection.DigitalOut);
                }
            }

            //double memUsedMB10 = GC.GetTotalMemory(false) / 1000000.0;
            
            // create Windows memory buffer for each board
            for (int iBoard = 0; iBoard < _numDevices; iBoard++) {
                if ((_memHandle[iBoard] == IntPtr.Zero) || (_winBufferSize < _totalScans)) {
                    if (_memHandle[iBoard] != IntPtr.Zero) {
                        // existing buffer not big enough
                        MccDaq.MccService.WinBufFreeEx(_memHandle[iBoard]);
                    }
                    _memHandle[iBoard] = MccDaq.MccService.WinBufAllocEx(_totalScans); // set aside memory to hold data
                    if (_memHandle[iBoard] == IntPtr.Zero) {
                        throw new OutOfMemoryException("");
                    }
                }
            }
            _winBufferSize = _totalScans;  // size of current Windows buffers

            //ouble memUsedMB11 = GC.GetTotalMemory(false) / 1000000.0;
            
            // setup is complete;
            _needsSetup = false;
            _allDataCompleted = false;

        }

        /// <summary>
        /// 
        /// </summary>
        public override void Start() {
            _allDataCompleted = false;
            if (_needsSetup) {
                Setup();
            }

            if (_totalScans < 1) {
                throw new InvalidOperationException("Invalid number of DAQ scans specified.");
            }

            int ChanCount = 1;
            int PretrigCount = 0;
            int Count = _totalScans;
            MccDaq.ErrorInfo err;

            // Reading at least one sample before starting 
            //  seems to eliminate the voltage drift during first IPP
            ushort dummy;
            for (int iBoard = 0; iBoard < _numDevices; iBoard++) {
                try {
                    _DaqBoard[iBoard].AIn(iBoard, Range.Bip10Volts, out dummy);
                }
                catch (Exception ee) {
                    string msg = "";
                    msg = " device #" + (iBoard + 1).ToString();
                    throw new ApplicationException("Cannot set DAQ board " + msg);
                }
            }


            for (int iBoard = 0; iBoard < _numDevices; iBoard++) {
                // Before arming all boards, clear enable lines that gate trigger on;
                err = _DaqBoard[iBoard].DBitOut(DigitalPortType.FirstPortA, 0, DigitalLogicState.Low);
            }
            for (int iBoard = 0; iBoard < _numDevices; iBoard++) {
                // arm each board for acquisition scan
                ScanOptions Options0 = new ScanOptions();
                int Count0 = 1;
                err = _DaqBoard[iBoard].DaqInScan(_chanArray,
                                                _chanTypeArray,
                                                _gainArray,
                                                ChanCount,
                                                ref _sampleRate,
                                                ref PretrigCount,
                                                ref Count,
                                                _memHandle[iBoard],
                                                _options);
                if (err.Value != ErrorInfo.ErrorCode.NoErrors) {
                    throw new ApplicationException("DAQ Error: " + err.Value.ToString());
                }
            }
            for (int iBoard = 0; iBoard < _numDevices; iBoard++) {
                // after arming all boards, set enable lines to gate trigger on;
                // Only one of these is used as the hardwired enable line
                _DaqBoard[iBoard].DBitOut(DigitalPortType.FirstPortA, 0, DigitalLogicState.High);
            }

            StartTimer();

        }

        private void StopTimer() {
            _tmrCheckStatus.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void StartTimer() {
            _tmrCheckStatus.Change(_timerInterval, _timerInterval);
        }

        ////////////////////////////////////////////////////////////////////////
        private void tmrCheckStatus_Tick(object stateInfo) {

            StopTimer();

            short status = MccBoard.Running;
            int curCount = 0, curIndex = 0;
            int count = _totalScans;
            ErrorInfo err;

            bool allFinished = true;
            for (int iBoard = 0; iBoard < _numDevices; iBoard++) {
                // if ANY board is running, allFinished is false
                err = _DaqBoard[iBoard].GetStatus(out status, out curCount, out curIndex, FunctionType.DaqiFunction);
                if (iBoard == 0) {
                    // POPREV: now update progress directly when property read 4.0.0
                    //_progress = (double)curIndex / count;
                }
                if (status == MccBoard.Running) {
                    allFinished = false;
                    break;
                }

            }

            if (allFinished) {

                // Transfer the data from the memory buffer set up by Windows to an array
                int FirstPoint = 0;
                for (int iBoard = 0; iBoard < _numDevices; iBoard++) {
                    err = MccDaq.MccService.WinBufToArray(_memHandle[iBoard], _intDataArray[iBoard], FirstPoint, count);
                    // Rescale data to 0 at 0 volts.
                    // Original data in int array (when viewed as unsigned) go 
                    //  from 0 at -maxVolts to 65536 at +maxVolts.
                    for (int i = 0; i < count; i++) {
                        _intDataArray[iBoard][i] = (short)((ushort)_intDataArray[iBoard][i] - (ushort)32768);
                    }
                }

                _allDataCompleted = true;

            }
            else {
                StartTimer();
            }


        }


        /// <summary>
        /// 
        /// </summary>
        public override void Stop() {
            StopTimer();
            for (int iBoard = 0; iBoard < _numDevices; iBoard++) {
                _DaqBoard[iBoard].StopBackground(MccDaq.FunctionType.DaqiFunction);
            }
        }

        // Make Close() the equivalent of Dispose()
        // Need to call only one of the two.
        public override void Close() {
            Dispose(true);
        }

        private void Cleanup() {
            Stop();
            // reset  some parameters so we get a definite error if we try to use them
            _nrx = _nspec = _npts = _ngates = -1;
            _totalScans = -1;
            _needsSetup = true;
            _tmrCheckStatus.Dispose();
            _tmrCheckStatus = null;
        }

        protected override void Dispose(bool disposing) {
            //Console.WriteLine("In Derived Dispose(" + disposing.ToString() + ")");
            if (_disposed) {
                return;
            }
            try {
                if (disposing) {
                    // clean up managed resources...
                    Cleanup();
                }
                // clean up native resources
                for (int iBoard = 0; iBoard < _numDevices; iBoard++) {
                    if (_memHandle != null && _memHandle.Length > iBoard) {
                        if (_memHandle[iBoard] != IntPtr.Zero) {
                            MccDaq.MccService.WinBufFreeEx(_memHandle[iBoard]);
                        }
                    }
                }
                //Console.WriteLine("  Disposing of resources in derived class.");
            }
            finally {
                base.Dispose(disposing);
            }
        }

    }  // end class DAQBoardMCC
}
