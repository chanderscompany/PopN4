using System;
using System.Runtime;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
//using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
//using System.Drawing;
using MathNet.Numerics;

using DAQCOMLib;
using ipp;

using POPCommunication;
using DACarter.PopUtilities;
using DACarter.NOAA.Hardware;
using DACarter.Utilities;
using DACarter.Utilities.Maths;


using POPN;
using System.IO.MemoryMappedFiles;


namespace POPN4Service {

	partial class PopNDwellWorker {

        public POPNEventLogWriter EventLogWriter;
        public bool RunningAsService;

        private bool _autoStart;
        private string _autoStartParFile;
		private int _progress = 0;
		private PopStatus _status;
		private bool _pauseChecked;
		private CommandQueue _commandQueue;
		//private int _commandCount = 0;
		private bool _cancel;
        private string _parFileName;
        private PopParameters _parameters;
        private bool _noHardware;
        private bool _noPbx;
        private bool _useAlloc;  // use new memory allocator module
        private int _nSamples, _nPts, _nXCPtMult, _nSpec, _nHts, _maxLag;

        private DAQDevice _daqBoard;
        private PulseGeneratorDevice _pbx = null;
        //private WGXPulseGenerator _wgx = null;
        private AD9959EvalBd _DDS = null;

        //private DateTime _startTime, _nowTime;

        private bool _usingSeqFile;

        private PopNConsensus _consensus;
        private DwellData _cnsDwell;

        private MCPowerMeter _powerMeter = null;

        int _counter;

        private int _nRx;
        private BackgroundWorker _controlWorker;
        private double _progressFraction;
        private double[] _filterFactors;
        private string _appDirectory;

        private PopNAllocator _memoryAllocator;
        private PopFileWriter3 _fileWriter;
        private PopDwellSequencer _dwellSequencer;
        private PopParFileSequencer _parxSequencer;
        private PopDataPackage3 _dataPackage;
        private MeltingLayerCalculator3 _meltingLayer;
        private PopNReplay _replay;

        private bool _endOfData;
        private bool _noRetryOnError;  // is set on errors we don't want to try restarting again

        private string _logFolder;
        private bool _firstThread;  //   true after worker thread first started, before first dwell
        private bool _beforeFirstDwell;

        private List<string> _parxSeqList;  // keeps list of parx files as we go through *.seq list

        public BackgroundWorker WorkerThread {
            get; 
            set;
        }

		public POPCommunicator Communicator {
			get;
			set;
		}

        //private MemoryMappedFile _mmf88;

		public PopNDwellWorker() {

            try {
                //_mmf88 = MemoryMappedFile.CreateNew("Global\\Test123", 1000, MemoryMappedFileAccess.ReadWrite);
            }
            catch {
                // catch error here:
                int x = 0;
            }

			_commandQueue = new CommandQueue();
			_cancel = false;
            _appDirectory = Application.StartupPath;
            //ComputedMoments = new MomentArrays();
            // do this to catch any attempt to allocate arrays without initializing these values:
            _nRx = _nPts = _nHts = _nSamples = _nSpec = _nXCPtMult = -1;
            _autoStart = false;
            _autoStartParFile = "";
            //isWriting = false;
            _noRetryOnError = false;
            _beforeFirstDwell = true;
            _memoryAllocator = null;
            _parxSeqList = new List<string>();
        }

		public void CommandReceived(object sender, POPCommunicator.PopCommandArgs arg) {
			PopCommands command = arg.Command;
			//Console.WriteLine("POPTest Service has received command: " + command);
			_commandQueue.Enqueue(command);
		}

 
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// called on exit from DoWork method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		public void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            TextFile.WriteLineToFile("DebugStatus2.txt", "Worker thread completed " + DateTime.Now.ToString(), true);
            EventLogWriter.WriteEntry("Exiting worker thread.", 900);
            SendStatusString("In workerCompleted");
            if (e.Error != null) {
                SendStatusString("In workerCompleted 2015; e.Error = " + e.Error.ToString() + " e.cancelled = " + e.Cancelled.ToString());
            }
            if (_pbx != null) {
                //SendStatusString("checking PBX:");
                _pbx.StopPulses();
                _pbx.Reset();
                _pbx.Close();
                //SendStatusString("done PBX:");
            }
            //SendStatusString("checking DDS:");
            if (_DDS != null) {
                //SendStatusString("checking DDS2:");
                //SendStatusString("skipping checking DDS2:");
                try {
                    _DDS = new AD9959EvalBd(100.0, 5, 1000, true);
                }
                catch {
                }
                _DDS = null;
                SendStatusString("done DDS:");
            }
            //SendStatusString("checking error:");
            if (e.Error != null) {
                // exception was thrown in DoWork method
                string message = "@@@--Dwell Worker thread threw exception: " + e.Error.Message;
                SendStatusException(message);
                EventLogWriter.WriteEntry(message, 901);
                if ((e.Error.InnerException != null) && !string.IsNullOrWhiteSpace(e.Error.InnerException.Message)) {
                    EventLogWriter.WriteEntry("InnerException:  " + e.Error.InnerException.Message, 901);
                    SendStatusString("InnerException:  " + e.Error.InnerException.Message);
                }
                if (_logFolder == null) {
                    SendStatusString("_logFolder is null");
                }
                else if (string.IsNullOrWhiteSpace(_logFolder)) {
                    SendStatusString("_logFolder is empty");
                }
                else {
                    SendStatusString("Log Folder = " + _logFolder);
                }
                message = "  -- !! StackTrace: " + e.Error.StackTrace;
                SendStatusString(message);
                //MessageBox.Show(e.Error.StackTrace);
                
                EventLogWriter.WriteEntry(message, 902);

                // wait some time for user to intervene with Abort command,
                //  which will turn off autostart
                bool autoStart0 = PopNStateFile.GetAutoStart();
                PopStatus runState = PopNStateFile.GetCurrentStatus();
                if (autoStart0 || runState == PopStatus.Running) {
                    SendStatusString("  Click ABORT to avoid AutoRun on Restart.");
                    Thread.Sleep(3000);
                    PopCommands command = CheckCommand(PopCommands.Stop);
                    if ((command == PopCommands.None) || (command == PopCommands.Ping)){
                        Thread.Sleep(5000);
                        command = CheckCommand(PopCommands.Stop);
                        if ((command == PopCommands.None) || (command == PopCommands.Ping)) {
                            Thread.Sleep(5000);
                            command = CheckCommand(PopCommands.Stop);
                        }
                        else {
                            //SendStatusString("  Command2 = " + command.ToString());
                        }
                    }
                    else {
                        //SendStatusString("  Command1 = " + command.ToString());
                    }
                }

                //Communicator.UpdateStatus(new PopStatusMessage("Dwell Worker thread threw exception: " + e.Error.Message));
                //
                // test here:
                // see what happens to recovery settings:
                /*
                using (Process proc = new Process()) {

                    proc.StartInfo.FileName = "taskkill";
                    proc.StartInfo.Arguments = "/IM popn4Service.exe /F";
                    proc.StartInfo.WorkingDirectory = "";
                    proc.StartInfo.UseShellExecute = true;
                    proc.Start();

                    proc.WaitForExit();
                }
                */

                runState = PopNStateFile.GetCurrentStatus();
                SendStatusString("  Status = " + runState.ToString());

                if (RunningAsService) {

                    message = "  +++ Service is self-destructing; Should do Recovery/Restart.";
                    SendStatusString(message);
                    //EventLogPOPN4Service.WriteEntry(message, System.Diagnostics.EventLogEntryType.Information, 909);
                    if (!_noRetryOnError) {
                        // except on errors where we don't want retry,
                        //  exit with error flag to cause service to die with restart.
                        // If not running as service, this will kill Control Panel.
                        Environment.Exit(-1);
                    }
                }

                _noRetryOnError = false;

                //Application.Exit();
                //throw new ApplicationException("Throwing from WorkerCompleted.");
                //Application.Restart();

			}
            else if (e.Cancelled) {
                string message = "@@@Dwell Worker thread was cancelled:  " + (string)e.Result;
                SendStatusString(message);
                EventLogWriter.WriteEntry(message, 912);
                //Communicator.UpdateStatus(new PopStatusMessage("Dwell Worker thread was cancelled:  " + (string)e.Result));

            }
            else {
                string message = "@@@Dwell Worker thread terminated:  " + (string)e.Result;
                SendStatusString(message);
                EventLogWriter.WriteEntry(message, 913);
                //Communicator.UpdateStatus(new PopStatusMessage("Dwell Worker thread terminated:  " + (string)e.Result));
            }
            // always restart to keep dwell worker thread running
            WorkerThread.RunWorkerAsync();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		public void ProgressChanged(object sender, ProgressChangedEventArgs e) {
			int threadID = Thread.CurrentThread.ManagedThreadId;
			Communicator.UpdateStatus(new PopStatusMessage((PopStatus)e.UserState, e.ProgressPercentage));
		}

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
		/// DoWork method called at service start-up
        /// It runs continuously in the background,
        /// waiting for commands, collecting data, and processing data.
		/// 
		/// DoWork()
		/// Executes the code that runs in the background worker thread.
		/// This event handler is triggered by a call to
		///		RunWorkerAsync(argument) method of BackgroundWorker object.
		/// NOTE: If RunWorkerAsync is called while _DoWork is running,
		///		an exception is thrown.  Check worker.IsBusy first.
		/// </summary>
		/// <param name="sender">
		/// The BackgroundWorker object.
		/// </param>
		/// <param name="e">
		/// The DoWorkEventArgs object.
		/// e.Argument is the argument passed to RunWorkerAsync()
		/// e.Cancel is set here if worker.CancellationPending is true;
		/// e.Result can be set here and is available to RunWorkCompleted in its event args.
		/// </param>
		/// <remarks>
		/// Things to do in this method:
		/// > Get the BackgroundWorker that raised this event (sender).
		/// > Get argument passed to this thread (e.Argument)
		/// > If worker.CancellationPending is true then set
		///		e.Cancel true and exit.
		///		Note: CancellationPending is set when the main thread calls 
		///			worker.CancelAsync()
		/// > Call worker.ReportProgress() if desired.
		/// > Set e.Result on exit if desired.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void DoWork(object sender, DoWorkEventArgs e) {

            SynchronizationContext context = SynchronizationContext.Current;
            int threadID = Thread.CurrentThread.ManagedThreadId;

            EventLogWriter.WriteEntry("Starting worker thread.", 100);
            //SendStatusString("Hi-Ho Hi-Ho Starting worker thread " + threadID.ToString());
            bool debug = PopNStateFile.GetDebug();
            // now getting debug info from par file:
            //bool debug = _parameters.Debug.DebugToFile;
            if (debug) {
                BreadCrumbs.Enabled = true;
            }
            else {
                BreadCrumbs.Enabled = false;
            }

            _logFolder = PopNStateFile.GetLogFolder();
            PopNLogger.LogFolder = _logFolder;

            object arg = e.Argument;
            _controlWorker = sender as BackgroundWorker;

            bool isFirstDwell;  // first dwell after restart; used in memory useage debugging

            InitThread();

            bool firstTime = true;

            //MessageBox.Show("Message from worker.");
            do {
                // arrive here when first run or after an abort/unload

                CheckDDSDeviceDriver();

                isFirstDwell = true;

                PopCommands command = WaitToStart();

                if (command.Includes(PopCommands.Stop)) {
                    // POPREV 4.2.2 added so that we can recheck DDS before start.
                    continue;
                }

                if (command.Includes(PopCommands.Kill)) {
                    break;
                }

                if (_parameters != null) {
                    if (_parameters.Debug.DebugToFile) {
                        DisplayMemUse("Before InitStart:");
                    }
                }

                InitStart(); 

                _counter = 0;
                DateTime recTime;

                do {
                    // arrive here at beginning of each dwell

                    //Console.Beep(370, 400);  //+++++++++++++++++++++++

                    _endOfData = false;

                    BreadCrumbs.Drop(0);  // clears debug file
                    BreadCrumbs.Drop(10);
                    if (context != null) {
                        BreadCrumbs.Drop(12, "DwellWorker context = " + context.ToString());
                    }
                    else {
                        BreadCrumbs.Drop(12, "DwellWorker context = null");
                    }

                    Communicator.UpdateStatus(new PopStatusMessage(cmdParFile: _parFileName));

                    // TODO: we don't need all of this if in replay mode:

                    if (isFirstDwell) {
                        if (_parameters != null) {
                            if (_parameters.Debug.DebugToFile) {
                                DisplayMemUse("Begin dwell:");
                            }
                        }
                        //isFirstDwell = false;
                    }

                    // initialize dwell parameters;
                    //  read replay data record in here:
                    InitDwell();

                    if (!_endOfData) {
                        // TODO: this has to come after replay readRecord
                        recTime = _dataPackage.RecordTimeStamp;
                    }
                    else {
                        // POPREV added 4.8
                        // EOF on replay condition
                        // TODO: This doesn't allow for any processing of averages, etc.
                        //break;
                    }


                    if (_useAlloc) {

                        bool writePartialRawFile = false;
                        PopParameters.PopFileParameters[] popFiles = _parameters.SystemPar.RadarPar.ProcPar.PopFiles;
                        int nw = popFiles.Length;
                        if (_parameters.SystemPar.RadarPar.ProcPar.AllocTSOnly) {
                            for (int i = 0; i < nw; i++) {
                                if (popFiles[0].FileWriteEnabled) {
                                    if (popFiles[0].WriteRawTSFile) {
                                        if (!popFiles[0].WriteSingleTSTextFile &&
                                            !popFiles[0].WriteFullTSTextFile &&
                                            !popFiles[0].IncludeSingleTS &&
                                            !popFiles[0].IncludeFullTS &&
                                            !popFiles[0].IncludeSpectra &&
                                            !popFiles[0].IncludeMoments &&
                                            !popFiles[0].IncludeACorr &&
                                            !popFiles[0].IncludeXCorr) {

                                            // option to add multiple dwells to one long 
                                            //  raw time series record
                                            writePartialRawFile = true;
                                        }
                                    }
                                }
                            }
                        }

                        int nSpecAtATime;

                        if (_parameters.ReplayPar.Enabled) {
                            // in replay mode, must take all spectra at once
                            nSpecAtATime = 1000000;
                        }
                        else {
                            nSpecAtATime = _memoryAllocator.SampledTSSpecDim;
                        }

                        bool breakFromAcqLoop = false;

                        //>>>>>>>>> begin loop here for partial data acquisition
                        for (int iSpec = 0; iSpec < _nSpec; iSpec+=nSpecAtATime) {

                            if (!_parameters.ReplayPar.Enabled) {
                                Console.Beep(440, 200);  //+++++++++++++++++++++++
                            }

                            int nSpecToDo = nSpecAtATime;
                            int nSpecLeft = _nSpec - iSpec;
                            if (nSpecLeft < nSpecAtATime) {
                                nSpecToDo = nSpecLeft;
                            }

                            // acquire and process nSpecToDo groups of samples at a time:

                            _status = _status.Remove(PopStatus.Computing);

                            if (isFirstDwell && iSpec==0) {
                                if (_parameters.Debug.DebugToFile) {
                                    //DisplayMemUse("  Before Acquire:  ");
                                }
                                //isFirstDwell = false;
                            }

                            command = AcquireSamples(iSpec, nSpecToDo);

                            if (!_parameters.ReplayPar.Enabled) {
                                Console.Beep(1000, 200);  //+++++++++++++++++++++++
                            }

                            _status = _status.Add(PopStatus.Computing);

                            if (AbortCycle(e, command, 100)) {
                                breakFromAcqLoop = true;
                                break;
                            }


                            BreadCrumbs.Drop(105, "< ProcessSamples");

                            command = ProcessRawSamples(iSpec, nSpecToDo);

                            command = ProcessTimeSeries(iSpec, nSpecToDo);

                            if (AbortCycle(e, command, 106)) {
                                breakFromAcqLoop = true;
                                break;
                            }

                        }
                        //>>>>>>>>>>>> end loop here for partial data acquisition

                        if (breakFromAcqLoop) {
                            break;
                        }

                        _status = _status.Add(PopStatus.Computing);

                        Communicator.UpdateStatus(new PopStatusMessage(_status, 100));

                    }

                    _status = _status.Add(PopStatus.Computing);

                    if (_powerMeter != null) {
                        MCPowerMeter.PMStatus status = _powerMeter.Status;
                        _powerMeter.ReadMeter();
                        _powerMeter.WriteToFile();
                    }

                    BreadCrumbs.Drop(119, "< ProcessSpectra");
                    command = ProcessSpectra();
                    if (AbortCycle(e, command, 120)) {
                        break;
                    }

                    command = ProcessMoments();
                    if (AbortCycle(e, command, 130)) {
                        break;
                    }

                    //
                    // signal that data is ready 
                    //
                    PopStatus prevStatus = _status;
                    _status = prevStatus | PopStatus.DataReady;
                    if (_dataPackage != null) {
                        Communicator.UpdateStatus(new PopStatusMessage(_status, timeStamp: _dataPackage.RecordTimeStamp));
                    }
                    // clear DataReady status after sending it once
                    _status = _status.Remove(PopStatus.DataReady);
                    Communicator.UpdateStatus(new PopStatusMessage(_status));

                    WriteDataFile();
                    BreadCrumbs.Drop(140, "> WriteDataFile");

                    if (WorkerThread.CancellationPending) {
                        e.Cancel = true;
                        break;
                    }

                    if (_pauseChecked) {
                        _status = PopStatus.Paused;
                        PopNStateFile.SetCurrentStatus(_status);
                        Communicator.UpdateStatus(new PopStatusMessage(_status));
                        if (!_endOfData) {
                            Communicator.UpdateStatus(new PopStatusMessage(timeStamp: _dataPackage.RecordTimeStamp));
                        }
                        Communicator.UpdateStatus(new PopStatusMessage(_status, _progress));

                        isFirstDwell = true;  // show mem useage after pause

                        command = WaitForGo();
                        if (command.Includes(PopCommands.Kill) || command.Includes(PopCommands.Stop)) {
                            _status = PopStatus.Stopped;
                            break;
                            //Cancel = true;
                        }
                    }
                    
                    if (isFirstDwell) {
                        if (_parameters.Debug.DebugToFile) {
                            DisplayMemUse("  End dwell:  ");
                        }
                        isFirstDwell = true;
                    }
                    
                    BreadCrumbs.Drop(168, "> Near end of dwell");
                    if (WorkerThread.CancellationPending) {
                        e.Cancel = true;
                        break;
                    }

                    if (_endOfData) {
                        break;
                    }

                    BreadCrumbs.Drop(170, "> At end of dwell");

                    Communicator.UpdateStatus(new PopStatusMessage(_status, 0));

                } while (!_cancel);  // end of dwell

                // STOP or ABORT reaches here

                if (_replay != null) {
                    _replay.Close();
                }

                if (command.Includes(PopCommands.Kill)) {
                    // using Kill command to simulate an exception inside dwell worker thread
                    e.Result = "Kill Command";
                    throw new ApplicationException("Kill command throws an Exception.");
                }

                if (WorkerThread.CancellationPending) {
                    e.Cancel = true;
                    SendStatusString("Worker thread cancelled at end of main loop.");
                    break;
                }

                GC.Collect();
                Thread.Sleep(1500);

                DisplayMemUse("  On abort:  ");
                
                // do not set CurrentStatus to Stopped before exception thrown
                //  i.e. make sure exceptions leave status Running for restarts.
                _status = PopStatus.Stopped;
                PopNStateFile.SetCurrentStatus(_status);

            } while (true);

            if (_powerMeter != null) {
                _powerMeter.Close();
            }

            ShutDown();

            if (WorkerThread.CancellationPending) {
                e.Cancel = true;
                SendStatusString("##Worker thread cancelled");
                return;
            }

            _status = PopStatus.Stopped;
            PopNStateFile.SetCurrentStatus(_status);

			return;

		}

        /// <summary>
        /// Check to see if attached AD9959 needs firmware downloaded.
        /// </summary>
        private void CheckDDSDeviceDriver() {

            //SendStatusString("Checking DDS device driver.");
            Microsoft.Win32.RegistryKey HKLM, LibUsbK;
            try {
                HKLM = Microsoft.Win32.Registry.LocalMachine;
                LibUsbK = HKLM.OpenSubKey(@"System\CurrentControlSet\Services\libusbk\Enum");
            }
            catch (Exception ee) {
                LibUsbK = null;
            }
            if (LibUsbK != null) {
                int count;
                try {
                    count = (int)LibUsbK.GetValue("Count");
                }
                catch (Exception ex) {
                    count = 0;
                }
                if (count > 0) {
                    string dev = (string)LibUsbK.GetValue("0");
                    if (dev.IndexOf("PID_EE06", StringComparison.OrdinalIgnoreCase) >= 0) {
                        SendStatusString("--Loading AD9959 firmware.");
                        LoadDDSFirmware.Run();
                        SendStatusString(LoadDDSFirmware.GetResults());
                    }
                    else if (dev.IndexOf("PID_EE07", StringComparison.OrdinalIgnoreCase) >= 0) {
                        SendStatusString("--AD9959 already correctly renumerated.");
                    }
                    else {
                        SendStatusString("--No DDS device found.");
                    }
                }
                else {
                    SendStatusString("--Zero LibUsbK devices.");
                }
            }
            else {
                SendStatusString(" --No LibUsbK driver devices found.");
            }
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="command"></param>
        /// <param name="crumbFlag"></param>
        /// <returns></returns>
        private bool AbortCycle(DoWorkEventArgs e, PopCommands command, int crumbFlag) {
            bool abort = false;
            BreadCrumbs.Drop(crumbFlag);
            if (command.Includes(PopCommands.Kill) || command.Includes(PopCommands.Stop)) {
                BreadCrumbs.Drop(crumbFlag + 1, "  in abort break");
                abort = true;
            }
            if (WorkerThread.CancellationPending) {
                BreadCrumbs.Drop(crumbFlag+2, "  in cancel pending");
                e.Cancel = true;
                abort = true;
            }
            return abort;
        }

        //*********************************************************************************

        /// <summary>
        /// 
        /// </summary>
        /// <param name="label"></param>
        private void DisplayMemUse(string label) { 
            int usedMemMb, largestAvailMb;
            DACarter.Utilities.DacMemory.EnoughMemoryIsAvailable(1, out usedMemMb, out largestAvailMb);
            SendStatusString(label + " Used Mem = " + usedMemMb.ToString() + " MB; Largest avail = " + largestAvailMb.ToString() + " MB");
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        private void WriteDataFile() {
            if (_endOfData || _dataPackage == null) {
                return;
            }
            if (_dataPackage.RassIsOn) {
                // POPREV as of 4.6, RASS is allowed
                //return;
            }
            bool isWriting = false;
            PopParameters.PopFileParameters[] filePar = _dataPackage.Parameters.SystemPar.RadarPar.ProcPar.PopFiles;
            if (filePar.Length > 0) {
                if (filePar[0].FileWriteEnabled) {
                    isWriting = true;
                }
            }
            if (filePar.Length > 1) {
                if (filePar[1].FileWriteEnabled) {
                    isWriting = true;
                }
            }
            if (isWriting) {
                _status = _status.ReplaceWith(PopStatus.Computing, PopStatus.Writing);
                Communicator.UpdateStatus(new PopStatusMessage(_status));
                try {
                    if (_fileWriter == null) {
                        _fileWriter = new PopFileWriter3(_dataPackage);
                    }
                    else {
                        _fileWriter.DataPackage = _dataPackage;
                    }
                    _fileWriter.CurrentParIndices = _dwellSequencer.CurrentIndices;
                    bool isOK = _fileWriter.WritePopRecord();
                    if (!isOK) {
                        SendStatusException("WritePopRecord ERROR: " + _fileWriter.ErrorMessage);
                        //Communicator.UpdateStatus(new PopStatusMessage("WritePopRecord ERROR: " + _fileWriter.ErrorMessage));
                    }
                    else {
                        // dac debug
                        if ( !String.IsNullOrWhiteSpace(_fileWriter.StatusMessage)) {
                            SendStatusString(_fileWriter.StatusMessage);                           
                        }
                    }
                }
                catch (Exception ex) {
                    //MessageBoxEx.Show("Unhandled error writing POP data file\n" + ex.Message, 5000);
                    // POPREV 3.19.3 added following status instead of throwing exception:
                    SendStatusException("WritePopRecord ERROR: " + ex.Message);
                    //throw;
                }
            }
            else {
                // TODO: dac: POPN2: this delay is needed when not writing files in order
                //	to update the plots in the main thread --
                //	Figure out why!
                //Thread.Sleep(30);
            }

            //command = ProcessSpectra();
            _status = _status.Remove(PopStatus.Writing);
            _status = _status.Remove(PopStatus.Computing);
            Communicator.UpdateStatus(new PopStatusMessage(_status));

        }

        //////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Things we do when starting the worker thread.
        /// Called from DoWork method.
        /// </summary>
        private void InitThread() {

            _firstThread = true;

            //_commandQueue.Clear();

            int threadID = Thread.CurrentThread.ManagedThreadId;

            //string fullPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            //string folder = Path.GetDirectoryName(fullPath);
            //string fileName = Path.Combine(folder, "POPNLog.txt");

            int maxWaitSeconds = 10;
            int sleepMs = 200;
            int count = 0;
            while (Communicator.SubscriberCount == 0) {
                // wait a bit in case a UI app is starting up
                Thread.Sleep(sleepMs);
                count++;
                if (count * sleepMs > maxWaitSeconds * 1000) {
                    break;
                }
            }
            if (Communicator.SubscriberCount != 0) {
                Thread.Sleep(1000);
                SendStatusString("  Comm Server connected to " + Communicator.SubscriberCount.ToString() + " client");
                //SendStatusString("Hello, Dwell worker thread has been started: ID " + threadID.ToString());
                //Communicator.UpdateStatus(new PopStatusMessage("Comm Server connected to " + Communicator.SubscriberCount.ToString() + " client"));
                //Communicator.UpdateStatus(new PopStatusMessage("Dwell worker thread has been started."));
                Thread.Sleep(1000);
            }
            else {
                SendStatusString("  Comm Server has NO clients.");
                //Communicator.UpdateStatus(new PopStatusMessage("Comm Server has NO clients."));
            }

            //MeltingLayerCalculator3.PreviousBrightBandHeightM = -1;

            // Read previous status to decide if to start automatically or continue
            PopStatus prevStatus = PopNStateFile.GetCurrentStatus();
            PopCommands prevCommand = PopNStateFile.GetLastCommand();
            bool autoStart = PopNStateFile.GetAutoStart();
            string prevParFile = PopNStateFile.GetLastParFile();
            if (prevStatus.Includes(PopStatus.Running) || autoStart) {
                _autoStart = true;
                _autoStartParFile = prevParFile;
            }

            // starting from scratch
            _status = PopStatus.Stopped;
            //PopNStateFile.SetCurrentStatus(_status);
            
            return;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Things we do when starting from a Stopped state.
        /// </summary>
        /// <returns></returns>
        private void InitStart() {

            if (_firstThread) {
                _firstThread = false;
                string exe = Path.GetFileName(Application.ExecutablePath);
                string version = Application.ProductVersion;
                SendStatusString("  Executing from file " + exe);
                SendStatusString("  Service Version: " + version);
                int bitness = IntPtr.Size * 8;
                SendStatusString("  Running as " + bitness.ToString() + "-bit application.");
            }
            
            // POPREV: added rev 3.17.3
            if (_memoryAllocator != null) {
                _memoryAllocator = null;
            }

            _meltingLayer = null;

            // POPREV: added ver 3.13 to make sure dds is off before we start
            try {
                _DDS = new AD9959EvalBd(100.0, 4, 5000.0, true);
                //_DDS.ResetDDS();
                _DDS = null;
            }
            catch (Exception ee) {
                //SendStatusString("DDS ctor error: " + ee.Message);
                // no DDS -- OK
            }

            // POPREV: force hardware to reset on restart after ABORT or STOP; rev 4.0.2
            if (_daqBoard != null) {
                _daqBoard.Close();
                _daqBoard = null;
            }
            _pbx = null;

            // get the parameter file name that the worker thread was commanded to use,
            //  either by user interface or by autostart POPNState file
            _parFileName = PopNStateFile.GetParFileCommand();

            string ext = Path.GetExtension(_parFileName);
            if (ext.ToLower().Contains("seq")) {
                // sequence file was specified: read parx file for each new dwell
                _usingSeqFile = true;
                _parxSequencer = null;
                _parxSequencer = new PopParFileSequencer(_parFileName);
                _parxSeqList.Clear();
            }
            else {
                // parx file was specified; initialize once now
                _usingSeqFile = false;
                InitParxFile(_parFileName);
                // Note debug modes
                if (_noHardware || _noPbx || _useAlloc) {

                    SendStatusString("--------------");
                    if (_noHardware) {
                        SendStatusString("!!Running in NOHARDWARE mode.");
                    }
                    if (_noPbx) {
                        SendStatusString("!!Running in NOPBX mode.");
                    }
                    if (_useAlloc) {
                        //SendStatusString("!!Using Memory Allocator Module");
                    }
                    SendStatusString("--------------");
                }
            }

            string message = "==Starting--Using par file: " + Path.GetFileName(_parFileName);
            SendStatusString(message);
            EventLogWriter.WriteEntry(message, 120);

            PopNStateFile.SetCurrentParFile(_parFileName);


            _beforeFirstDwell = true;

        }

        /// <summary>
        /// Read the setup parameters from the parameter file
        /// </summary>
        /// <param name="parFileName"></param>
        private void InitParxFile(string parFileName) {

            if (string.IsNullOrWhiteSpace(parFileName)) {
                throw new ApplicationException(">> Parfile is empty in InitParxFile");
            }
            SendStatusString("  parFile = " + parFileName);
            try {
                _parameters = PopParameters.ReadFromFile(parFileName);
            }
            catch (Exception ee) {
                throw new ApplicationException(">> Parfile is empty in InitParxFile: " + parFileName);
            }
            _parameters.Source = parFileName;
            _logFolder = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].LogFileFolder;
            if (string.IsNullOrWhiteSpace(_logFolder)) {
                throw new ApplicationException(">> _logFolder is empty in InitParxFile");
            }
            PopNLogger.LogFolder = _logFolder;
            if (_usingSeqFile) {
                // archive the parx file only the first time it is used
                if (!_parxSeqList.Contains(parFileName)) {
                    _parxSeqList.Add(parFileName);
                    ParxFileArchiver.Archive(parFileName);
                }
            }
            else {
                ParxFileArchiver.Archive(parFileName);
            }
            PopNStateFile.SetLogFolder(_logFolder);
            _noHardware = _parameters.Debug.NoHardware;
            _noPbx = _parameters.Debug.NoPbx;

            // check number of other instruments given in par file
            //  standard procedure after rev 4.15 is to use one instrument for timestamp fractional seconds
            // If this was not set in parameter file, do it here.
            // Obviously, if we have additional instrument readings to record, changes must be made here.
            // Also can override this to go back to original with numOtherInstruments = 0
            int num = _parameters.SystemPar.RadarPar.NumOtherInstruments;
            if (num == 0) {
                _parameters.SystemPar.RadarPar.NumOtherInstruments = 1;
                _parameters.SystemPar.RadarPar.OtherInstrumentCodes = null;
                _parameters.SystemPar.RadarPar.OtherInstrumentCodes = new int[1];
                _parameters.SystemPar.RadarPar.OtherInstrumentCodes[0] = 0x5453;
            }
            // To override and return to previous version, uncomment the following line:
            //_parameters.SystemPar.RadarPar.NumOtherInstruments = 0;


            if (_parameters.ReplayPar.Enabled) {
                _useAlloc = false;
            }
            else {
                _useAlloc = _parameters.Debug.UseAllocator;
            }
            // POPREV 3.21 _useAlloc = true all the time
            _useAlloc = true;
            if (_parameters.Debug.UseAllocator == false) {
                // if allocator turned off in parx file, make block large
                
            }


            _dwellSequencer = null;
            _dwellSequencer = new PopDwellSequencer(_parameters);

            // reread debug now that we have actual par file
            bool debug = _parameters.Debug.DebugToFile;
            if (debug) {
                BreadCrumbs.Enabled = true;
            }
            else {
                BreadCrumbs.Enabled = false;
            }

            // set up hardware
            // acquisition hardware
            // NOTE: will throw exception if board not available:
            InitHardware();

            if (_parameters.ReplayPar.Enabled) {
                /*
                _replay = new PopNReplay(_parameters.ReplayPar.InputFile,
                                            _parameters.ReplayPar.StartTime,
                                            _parameters.ReplayPar.StartDay,
                                            _parameters.ReplayPar.EndTime,
                                            _parameters.ReplayPar.EndDay);
                 * */
                _replay = new PopNReplay(_parameters.ReplayPar);
                _replay.ProcessingPar = _parameters;

                // in replay mode, do not try to restart on errors:
                _noRetryOnError = true;

                if (_parameters.SystemPar.RadarPar.ProcPar.CnsPar[0].CnsEnable) {
                    _consensus = new PopNConsensus();
                    _consensus.useTriads = true;
                    _consensus.UseVerticalCorrection = _parameters.SystemPar.RadarPar.ProcPar.CnsPar[0].CnsIsVertCorrection;
                    // the following only read seq in replay par file, not from replay data:
                    //_consensus.TriadBeams = _parameters.GetBeamsInSequence();  // but use this for real-time
                    // for replay mode, user specifies mode:
                    _consensus.TriadBeams = _parameters.SystemPar.RadarPar.ProcPar.CnsPar[0].ReplayBeamMode;
                    _cnsDwell = new DwellData();
                }

                // do this in InitDwell (actually in GetReplayData) for replay:
                //InitDataPackage();
            }
            else {
                if (_replay != null) {
                    _replay.Close();
                    _replay = null;
                }
                if (_parameters.SystemPar.RadarPar.ProcPar.CnsPar[0].CnsEnable) {
                    _consensus = new PopNConsensus();
                    _consensus.useTriads = true;
                    _consensus.TriadBeams = _parameters.GetBeamsInSequence(); 
                    _cnsDwell = new DwellData();
                }
                // in real-time mode, can do this at start:
                InitDataPackage();
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private PopCommands WaitToStart() {

            _status = PopStatus.Stopped;
            _progress = 0;
            Communicator.UpdateStatus(new PopStatusMessage(_status, _progress));

            PopCommands command = PopCommands.None;

            // wait for GO or autostart
            SendStatusString("Waiting for GO");
            //Communicator.UpdateStatus(new PopStatusMessage("Waiting for GO"));
            CheckPreviousState();

            // POPREV 4.2.2 removed loop while stop command
            //do {
                command = WaitForGo();
            //} while (command.Includes(PopCommands.Stop));

            if (command.Includes(PopCommands.Stop)) {
                // POPREV 4.2.2 now exit on stop/abort button
                return command;
            }

            if (command.Includes(PopCommands.Kill)) {
                return command;
            }

            if (!command.Includes(PopCommands.Go)) {
                throw new ApplicationException("Expecting GO command out of WaitToStart");
            }

            return command;
        }

        ///////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Send a string message to Log file and to Communciator UpdateStatus
        /// </summary>
        /// <param name="message"></param>
        private void SendStatusException(string message) {
            //string time = DateTime.Now.DayOfYear.ToString("000 ") + DateTime.Now.ToString("HH:mm:ss");
            Communicator.UpdateStatus(new PopStatusMessage(excMsg: message)); // + "  (" + time + ")"));
            //if (!String.IsNullOrWhiteSpace(_logFolder)) {
                DacLogger.WriteEntry(message, _logFolder);
            //}
        }

        private void SendStatusString(string message) {
            //string time = DateTime.Now.DayOfYear.ToString("000 ") + DateTime.Now.ToString("HH:mm:ss");
            Communicator.UpdateStatus(new PopStatusMessage(message)); // + "  (" + time + ")"));
            //if (!String.IsNullOrWhiteSpace(_logFolder)) {
                DacLogger.WriteEntry(message, _logFolder);
            //}
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initialize the hardware
        /// </summary>
        private void InitHardware() {

            // for debug only:
            //_pbx = PulseGeneratorFactory.GetNewPulseGenDevice();

            if (!_parameters.ReplayPar.Enabled) {
                if (_parameters.SystemPar.RadarPar.PowMeterPar.Enabled) {
                    // if parfile calls for power meter and don't have one, create one
                    if (_powerMeter == null) {
                        try {
                            _powerMeter = new MCPowerMeter();
                        }
                        catch (OutOfMemoryException) {
                            _powerMeter = null;
                        }
                    }
                    if (_powerMeter != null) {
                        _powerMeter.FreqMHz = _parameters.SystemPar.RadarPar.TxFreqMHz;
                        _powerMeter.OffsetDB = _parameters.SystemPar.RadarPar.PowMeterPar.OffsetDB;
                        _powerMeter.WriteIntervalSec = _parameters.SystemPar.RadarPar.PowMeterPar.WriteIntervalSec;
                        _powerMeter.FileNamePrefix = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].FileNameSite;
                        _powerMeter.OutputPath = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].FileFolder;
                        _powerMeter.MakeHourFiles = false;
                        MCPowerMeter.PMStatus status = _powerMeter.Status;
                        if (status == MCPowerMeter.PMStatus.Error) {
                            //_powerMeter = null;
                        }
                    }
                }
                else {
                    _powerMeter = null;
                }
            }
            else {
                _powerMeter = null;
            }

            BreadCrumbs.Drop(330, "InitHardware");
            //_daqBoard = null;
            BreadCrumbs.Drop(332, "InitHardware");

            _nRx = _parameters.SystemPar.RadarPar.ProcPar.NumberOfRx;
            if ((_nRx < 1) || (_nRx > 3)) {
                throw new ApplicationException("Invalid NRX");
            }

            // get either PulseBlaster or PbxControllerCard device
            // POPREV if test added dac 20130204

            // POPREV added in 3.23.1 20130604 (to initialize PulseBlaster)
            // Seems to be needed when running DAQ with PulseBlaster without DDS (clock chip inserted)
            //  in order for PulseGeneratorFactory to return PulseBlaster instead of PbxControllerCard object
            //  when both cards present.  (I know, it doesn't make any sense.)
            PulseGeneratorDevice _blaster = null;
            if (!_noHardware) {
                try {
                    _blaster = new PulseBlaster();
                    SendStatusString("$$$ _blaster is null = " + (_blaster == null).ToString());
                    if (_blaster != null) {
                        SendStatusString("$$$ _blaster exists = " + _blaster.Exists().ToString());
                        int status = _blaster.ReadStatus();
                        SendStatusString("$$$ _blaster status = " + status.ToString());
                        _blaster.Close();
                        _blaster = null;
                    }
                }
                catch (DllNotFoundException ee) {
                    SendStatusString("$$$ DLL exception = " + ee.Message);
                    // for some reason, even when caught here, this exception will be thrown again to top of program
                }
                catch (Exception e) {
                    SendStatusString("$$$ new PulseBlaster exception = " + e.Message);
                }
                
            }
            /////////////////////////////////////////

            if (_noHardware) {
                _pbx = null;
            }
            else {
                bool getPBXonly = false;
                // 
                if (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCwSA) {
                    //getPBXonly = true;
                }
                _pbx = PulseGeneratorDevice.GetNewPulseGenDevice(getPBXonly);
            }

            // for debug info only:
            //SendStatusString("$$$ _noHardware = " + _noHardware.ToString());
            if (!_noHardware) {
                SendStatusString("$$$ _pbx is null = " + (_pbx == null).ToString());
                if (_pbx != null) {
                    SendStatusString("$$$ _pbx is PulseBlaster = " + (_pbx is PulseBlaster).ToString());
                    SendStatusString("$$$ _pbx is PulseBoxCard = " + (_pbx is PbxControllerCard).ToString());
                    int status = _pbx.ReadStatus();
                    SendStatusString("$$$ _pbx status = " + status.ToString());
                }
                
            }

            //_wgx = new WGXPulseGenerator();

            _DDS = null;
            if (!_parameters.ReplayPar.Enabled && !_noHardware) {
                if (_parameters.SystemPar.RadarPar.FmCwParSet[0].AD9959Enabled) {
                    double refClock = _parameters.SystemPar.RadarPar.FmCwParSet[0].DDSRefClockMHz;
                    int clockMultiplier = _parameters.SystemPar.RadarPar.FmCwParSet[0].DDSMultiplier;

                    // use this to check DDS setup without DDS hardware attached:
                    //MessageBoxEx.Show("faking DDS", 1000);
                    //_DDS = new AD9959EvalBd(_parameters, false);

                    _DDS = new AD9959EvalBd(_parameters, true);

                    if (_DDS == null) {
                        SendStatusString("DDS ctor returns null.");

                    }

                }
            }

            BreadCrumbs.Drop(333, "InitHardware");
            bool repeat = false;
            do {
                repeat = false;
                if (false /*replayMode*/) {
                    // _replay = new PopNReplay(_parameters.ReplayPar.InputFile);
                }
                else if (!_noHardware) {
                //else if (true) {
                    // debug:
                    //Console.Beep(440, 500);
                    //Console.Beep(660, 500);
                    try {
                        BreadCrumbs.Drop(334, "InitHardware");
                        InitDaq();
                        BreadCrumbs.Drop(336, "InitHardware");
                    }
                    catch (Exception ex) {
                        if (_daqBoard != null) {
                            _daqBoard.Close();
                            _daqBoard = null;
                        }
                        int showTime = 12;
                        DialogResult rr = DialogResult.Abort;
                        try {
                            rr = DialogResult.Abort;
                            rr = MessageBoxEx.Show("DaqBoard NOT Found!\n\n" +
                                                "Use -NoHardware command line option or IGNORE to run in Test Mode\n" +
                                                "or Press RETRY when board is connected\n\n" +
                                                "This message disappears in " + showTime.ToString() +
                                                " seconds and program restarts.\n",
                                                "POPN Error",
                                                MessageBoxButtons.AbortRetryIgnore,
                                                MessageBoxIcon.Stop,
                                                MessageBoxDefaultButton.Button1,
                                //MessageBoxOptions.ServiceNotification,
                                                (uint)(showTime * 1000));
                        }
                        catch  {
                            rr = DialogResult.Abort;
                        }

                        BreadCrumbs.Drop(337, "InitHardware");
                        if (rr == DialogResult.Ignore) {
                            _noHardware = true;
                            _parameters.Debug.NoHardware = true;
                            //PopNStateFile.SetNoHardware(_noHardware);
                        }
                        else if (rr == DialogResult.Retry) {
                            repeat = true;
                        }
                        else {
                            SendStatusString("%%% DAQ Board NOT Found. %%%");
                            SendStatusString(ex.Message);
                            throw new ApplicationException("DAQ Board NOT Found.");
                        }
                    }
                }
            } while (repeat);

            BreadCrumbs.Drop(339, "InitHardware");

        }

        private void InitDaq() {

            if (_daqBoard != null) {
                // if DAQ object already exists,
                //   stop it, close, and dispose properly
                //   so that the object and all its memory
                //   are released.
                _daqBoard.Stop();
                _daqBoard.Close();
                //_daqBoard.ProgressNotifier -= UpdateFmCwProgress;
                _daqBoard = null;
            }

            GC.Collect();
            Thread.Sleep(1500);

            if (_daqBoard == null) {
                // dac TODO: what happens when does not return from ctor?
                //Thread.Sleep(500);
                _daqBoard = DAQDevice.GetAttachedDAQ(_parameters);
            }
            BreadCrumbs.Drop(335, "InitHardware");
            _daqBoard.TestWithoutPbx = _noPbx;
            _daqBoard.AnalogInputUnits = DAQDevice.MeasurementUnits.Raw;
            _daqBoard.MaxAnalogInput = DAQDevice.VoltageRange.Volts10;
            _daqBoard.NDataSamplesPerDevice = _nSamples * _nPts * _nSpec;

            //_daqBoard.ProgressNotifyInterval = 1;
            //_daqBoard.ProgressNotifier += UpdateFmCwProgress;

            if (_daqBoard.NumDevices != _parameters.SystemPar.RadarPar.ProcPar.NumberOfRx) {
                throw new ApplicationException("Number of DAQ NOT EQUAL to NRX");
            }

            _nRx = _daqBoard.NumDevices;
            _parameters.SystemPar.RadarPar.ProcPar.NumberOfRx = _nRx;
        }

        private void InitDataPackage() {

            if (_dataPackage == null) {
                _dataPackage = new PopDataPackage3();
            }
            _dataPackage.Parameters = _parameters;
            _dataPackage.NoHardware = _noHardware;

            int usedMemMb, largestAvailMb;
            DACarter.Utilities.DacMemory.EnoughMemoryIsAvailable(1, out usedMemMb, out largestAvailMb);
            //SendStatusString(  " Before alloc: Used Mem = " + usedMemMb.ToString() + " MB; Largest avail = " + largestAvailMb.ToString() + " MB");


            _nRx = _parameters.SystemPar.RadarPar.ProcPar.NumberOfRx;
            _nSamples = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts;
            //_nPts = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNPts;
            _nPts = _parameters.SystemPar.RadarPar.BeamParSet[0].NPts;
            _nSpec = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNSpec;
            _nXCPtMult = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrNptsMultiplier;
            if (_nXCPtMult > _nSpec) {
                _nXCPtMult = _nSpec;
            }
            _nHts = _nSamples / 2 + 1;
            _maxLag = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrMaxLag;
            _polyFitOrder = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrPolyFitOrder;
            _xcorrAdjustBase = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrAdjustBase;
            _xcorrLagsToFit = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrLagsToCurveFit;
            _xcorrLagsToInterp = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrLagsToInterpolate;


            if (_useAlloc) {
                if (_memoryAllocator == null) {
                    _memoryAllocator = new PopNAllocator();
                    _memoryAllocator.SendMessageAction += SendStatusString;
                }
                _memoryAllocator.Parameters = _parameters;
                _memoryAllocator.AllocateDataArrays(_dataPackage);
                CreateFilterFactors();
            }
            else {
                throw new ApplicationException("Always using memory allocator now.");
            }

            //DACarter.Utilities.DacMemory.EnoughMemoryIsAvailable(1, out usedMemMb, out largestAvailMb);
            //SendStatusString(" After alloc: Used Mem = " + usedMemMb.ToString() + " MB; Largest avail = " + largestAvailMb.ToString() + " MB");
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
		/// Idles until GO command received
		/// -- also aborts if STOP or KILL command received
		/// </summary>
		/// <returns></returns>
		private PopCommands WaitForGo() {

			PopCommands command = PopCommands.None;
            int waitCount = 0;
            do {

                command = PopCommands.None;

                if (WorkerThread.CancellationPending) {
                    //e.Cancel = true;
                    //break;
                    command = PopCommands.Stop;
                    //SendStatusString("Worker thread has been cancelled while in pause.");
                }
                else {
                    command = CheckCommand(breakOnCommand: PopCommands.Go);
                }

				//while (_commandQueue.Count > 0) {
                /*
				bool isGo = true;
				bool isPing = command.Includes(PopCommands.Ping);
				isGo = command.Includes(PopCommands.Go);
				//if (command != PopCommands.Ping) {
				if (!command.Includes(PopCommands.Ping)) {
					//_commandCount++;
					int threadID = Thread.CurrentThread.ManagedThreadId;
					Communicator.UpdateStatus(new PopStatusMessage("Received command: " + command.ToString() + " " + _commandCount.ToString()));
					if ((command.Includes(PopCommands.Kill)) || (command.Includes(PopCommands.Go)) || command.Includes(PopCommands.Stop)) {
						break;
					}
				}
                */
				//}

				if ((command.Includes(PopCommands.Kill)) || (command.Includes(PopCommands.Go)) || command.Includes(PopCommands.Stop)) {
					break;
				}
                Thread.Sleep(100);
                if (waitCount % 10 == 0) {
                    Communicator.UpdateStatus(new PopStatusMessage(_status));
                    if (_status.Includes(PopStatus.Paused)) {
                        // update parfile being used, in case listener just connected
                        Communicator.UpdateStatus(new PopStatusMessage(cmdParFile: _parFileName));
                    }
                    waitCount = 0;
                }
                waitCount++;
			} while (true);

            if (command.Includes(PopCommands.PauseChecked)) {
                _pauseChecked = true;
            }
            else if (command.Includes(PopCommands.PauseUnchecked)) {
                _pauseChecked = false;
            }

            if (command.Includes(PopCommands.Go)) {
			    if (_pauseChecked) {
                    _status = PopStatus.RunningPausePending;
				    Communicator.UpdateStatus(new PopStatusMessage(_status, progress: 0));
			    }
			    else if (!_pauseChecked) {
                    _status = PopStatus.Running;
                    Communicator.UpdateStatus(new PopStatusMessage(_status, progress: 0));
			    }
            }

            PopNStateFile.SetCurrentStatus(_status);

			return command;
		}

        ///////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// CheckPreviousState()
        /// Called when waiting to start the dwell cycle from the beginning 
        /// (not including continuing from pause).
        /// Checks to see if the last recorded state was "running".
        /// If so, automatically put a "Go" command in the command queue.
        /// </summary>
        private void CheckPreviousState() {
            // if was previously in running state, restart with these commands:
            //PopStatus lastStatus = PopNStateFile.GetCurrentStatus();
            if (_autoStart) {
                SendStatusString("AutoStart");
                //PopNLogger.WriteEntry("AutoStarting");
                EventLogWriter.WriteEntry("AutoStarting", 125);
                //Communicator.UpdateStatus(new PopStatusMessage("AutoStart"));
                // get the last parfile that was being used
                _parFileName = PopNStateFile.GetLastParFile();
                // replace par file name set by user interface with autostart parfile:
                PopNStateFile.SetParFileCommand(_parFileName);
                PopCommands fakeCommand = PopCommands.Go | PopCommands.PauseUnchecked;
                _commandQueue.Enqueue(fakeCommand);
                // get debug options from previous state file
                // POPREV: _noHardware is set from parFile (3.15):
                //_noHardware = PopNStateFile.GetNoHardware();
                //_noPbx = PopNStateFile.GetNoPbx();
                _autoStart = false;
            }
            /*
            else if (lastStatus == PopStatus.Paused) {
                _pauseChecked = true;
            }
            */

            //_noHardware = PopNStateFile.GetNoHardware();
            //_noPbx = PopNStateFile.GetNoPbx();
            // now debug info is in parameter file:
            //_noHardware = _parameters.Debug.NoHardware;
            //_noPbx = _parameters.Debug.NoPbx;
                
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
		private void ShutDown() {
            SendStatusString("##POPN4Service worker thread is shutting down.");
			//Communicator.UpdateStatus(new PopStatusMessage("POPN4Service worker thread is shutting down."));
            BreadCrumbs.Drop(431, "Shutdown");
            // turn off hardware before leaving
            if (_pbx != null) {
                _pbx.StopPulses();
            }
            BreadCrumbs.Drop(433);
            if (_DDS != null) {
                _DDS.ResetDDS();
            }
            BreadCrumbs.Drop(435);
            // DAC: removed following because DAQ calls sometimes hang
            //_FmCw.Stop();
            BreadCrumbs.Drop(436);
            PopNLogger.WriteEntry("End of worker thread.");
            BreadCrumbs.Drop(340, "End Worker thread.");
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
		/// Checks the command queue and returns the last command read from the queue
		/// -- Stops reading the queue when a STOP or KILL command is received
        /// --  also stops when breakOnCommand command is received.
		/// -- Sets _pausedChecked field whenever a command indicates its value
		/// -- Returns PopCommands.None if queue is empty.
		/// </summary>
		/// <returns></returns>
		private PopCommands CheckCommand(PopCommands breakOnCommand = PopCommands.Kill ) {
			PopCommands command = PopCommands.None;
			while (_commandQueue.Count > 0) {
				command = _commandQueue.Dequeue();
				if (!command.Includes(PopCommands.Ping)) {
					//_commandCount++;
					int threadID = Thread.CurrentThread.ManagedThreadId;
					//Communicator.UpdateStatus(new PopStatusMessage("Received command: " + command.ToString()));
					//communicator.UpdateStatus(new PopStatusMessage());
					if (command.Includes(PopCommands.PauseChecked)) {
						_pauseChecked = true;
                        if (_status.Includes(PopStatus.Running)) {
                            _status = _status.Remove(PopStatus.Running);
                            _status = _status.Add(PopStatus.RunningPausePending);
                        }
						Communicator.UpdateStatus(new PopStatusMessage(_status));
					}
					else if (command.Includes(PopCommands.PauseUnchecked)) {
						_pauseChecked = false;
                        if (_status.Includes(PopStatus.RunningPausePending)) {
                            _status = _status.Remove(PopStatus.RunningPausePending);
                            _status = _status.Add(PopStatus.Running);                           
                        }
						Communicator.UpdateStatus(new PopStatusMessage(_status));
					}
                    // turn off pulses on ABORT
                    if (command.Includes(PopCommands.Stop)) {
                        if (_pbx != null) {
                            _pbx.StopPulses();
                        }
                        if (_DDS != null) {
                            _DDS = new AD9959EvalBd(100.0, 5, 1000, true);
                            _DDS = null;
                        }
                    }

                    // set the desired state for restart after crash
                    if (command.Includes(PopCommands.Stop)) {
                        PopNStateFile.SetCurrentStatus(PopStatus.Stopped);
                        PopNStateFile.SetAutoStart(false);
                        SendStatusString("  Received Stop Command");
                        //Communicator.UpdateStatus(new PopStatusMessage("Received Stop Command"));
                    }
                    else {
                        PopNStateFile.SetCurrentStatus(_status);
                    }

                    // remember last command for restart after crash
                    if (!command.Includes(PopCommands.Kill)) {
                        PopNStateFile.SetLastCommand(command);
                    }

                    if ((command.Includes(PopCommands.Stop)) || (command.Includes(PopCommands.Kill))) {
						break;
					}
                    if (command.Includes(breakOnCommand)) {
                        break;
                    }
				}
			}
			return command;
		}

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Does initialization based on parameters for this dwell.
        /// Since when in replay mode, parameters can change with each record, data record is read here.
        /// </summary>
        private void InitDwell() {

            //throw new ApplicationException("TODO: Handle this exception thrown from InitDwell");

            if (_usingSeqFile) {
                // we are changing parx file at beginning of each dwell,
                //  so read and initialize it here.

                
                string parxFullFileName = _parxSequencer.NextParFile();
                InitParxFile(parxFullFileName);
                string parxFileName = Path.GetFileName(parxFullFileName);
                Communicator.UpdateStatus(new PopStatusMessage(curParFile: parxFileName));
                
            }

            if (!_parameters.ReplayPar.Enabled) {

                // this is now done in InitDataPackage:
                /*
                _nRx = _parameters.SystemPar.RadarPar.ProcPar.NumberOfRx;
                _nSamples = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts;
                _nPts = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNPts;
                _nSpec = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNSpec;
                _nHts = _nSamples / 2 + 1;
                */
                
                _dataPackage.RecordTimeStamp = DateTime.Now;
                _dataPackage.RassIsOn = false;
                Communicator.UpdateStatus(new PopStatusMessage(timeStamp: _dataPackage.RecordTimeStamp));

                int ibm = _dwellSequencer.Next();
                _dataPackage.CurrentParIndices.BmSeqI = _dwellSequencer.CurrentBeamSeqIndex;
                _dataPackage.CurrentParIndices.DirI = _dwellSequencer.CurrentDirectionIndex;
                _dataPackage.CurrentParIndices.ParI = _dwellSequencer.CurrentBeamParIndex;
                //int curParI = sequencer.CurrentBeamParIndex;

                // TODO: only need this to repeat if parameters have changed or pbx stopped:
                BreadCrumbs.Drop(100);
                // turn pulses off when talking to DDS - prob not necessary
                if (_pbx != null) {
                    if (_pbx.IsBusy()) {
                        _pbx.StopPulses();                        
                    }
                }
                BreadCrumbs.Drop(110);
                if (_DDS != null) {
                    //MessageBox.Show("Calling RunFmCwFreqSweeps, pbx = null is" + (pbx==null).ToString());
                    //_DDS.RunFmCwFreqSweeps(_parameters, _pbx);
                    _DDS.StartAllFrequencies();

                    if (_beforeFirstDwell) {
                        
                        int[,] registers = _DDS.GetRegisterValues();
                        if (registers != null) {
                            string ss;
                            int nChnls = registers.GetLength(0);
                            int nRegs = registers.GetLength(1);
                            SendStatusString("..DDS registers: (freq0, freq1, fdelta, tdelta)");
                            ss = string.Format("....{0} channels and {1} registers:", nChnls, nRegs);
                            //SendStatusString(ss);
                            for (int i = 0; i < nChnls; i++) {
                                int freq0 = registers[i, 1];
                                int freq1 = registers[i, 7];
                                int freqDelta = registers[i, 5];
                                int timeDelta = registers[i, 4];
                                ss = string.Format("....{0}, {1}, {2}, {3}", freq0, freq1, freqDelta, timeDelta);
                                SendStatusString(ss);
                            }
                        }
                        _beforeFirstDwell = false;
                    }

                }
                BreadCrumbs.Drop(115);

                // Start the pulse generator
                //
                /*
                 * // for debugging:
                 * //
                bool isPB = _pbx is PulseBlaster;
                bool isPBX = _pbx is PbxControllerCard;
                if (isPBX) {
                    int whoa = 0;
                    throw new ApplicationException("PulseBlaster board not detected. Plase try again.");
                }
                bool exists = _pbx.Exists();
                int status1 = _pbx.ReadStatus();
                _pbx.Setup(_parameters, _dataPackage.CurrentParIndices.ParI);
                bool isBusy = _pbx.IsBusy();
                int status2 = _pbx.ReadStatus();
                Thread.Sleep(2000);
                _pbx.StopPulses();
                int status3 = _pbx.ReadStatus();
                Thread.Sleep(500);
                _pbx.Reset();
                 * */

                if (!_noHardware) {
                    if (_pbx != null) {
                        _pbx.Reset();
                        _pbx.Setup(_parameters, _dataPackage.CurrentParIndices.ParI);
                    }
                }
                else if (!_parameters.Debug.NoPbx) {
                    // test mode for pulse sequence without actual pbx
                    //_pbx = new PulseBlaster(_parameters, 0);
                }

                _progress = 0;
                Communicator.UpdateStatus(new PopStatusMessage(_status, _progress));
            }
            else {
                GetReplayDwell();
            }  // end if replay mode

        }

        /// <summary>
        /// Gets data from a replay file record and
        ///     prepares for dwell processing
        /// </summary>
        private void  GetReplayDwell() {
            // replay mode
            // read record

            PopDataPackage3 sampledDataPackage;
            ReplayStatus replayStatus;
            DateTime recordTimeStamp;

            do {
                replayStatus = GetReplayData(out sampledDataPackage, out _progress, out recordTimeStamp);

                // call this to set _sequencer properties to proper values
                int ibm = _dwellSequencer.Next();

                if (replayStatus.Includes(ReplayStatus.AfterEndTime) ||
                    replayStatus.Includes(ReplayStatus.EOF)) {
                    _endOfData = true;
                }
                else {
                    _endOfData = false;
                }
                _dataPackage = sampledDataPackage;

                // can use this test to slow down status updates;
                // To update every nth record, set m = n-1
                int m = 0;
                if (_counter++ == m) {
                    _counter = 0;
                    if (!_endOfData) {
                        Communicator.UpdateStatus(new PopStatusMessage(timeStamp: recordTimeStamp));
                    }
                    Communicator.UpdateStatus(new PopStatusMessage(_status, _progress));
                    /*
                    if (!_endOfData) {
                        Communicator.UpdateStatus(new PopStatusMessage(timeStamp: _dataPackage.RecordTimeStamp));
                    }
                    Communicator.UpdateStatus(new PopStatusMessage(_status, _progress));
                    */
                }

                if (_endOfData) {
                    break;
                }

            } while (replayStatus.Includes(ReplayStatus.BeforeStartTime));

            if (_endOfData) {

                SendStatusString(" **** EOF on read. *****");
                SendStatusString(" **** EOF on read. *****");
                return;
            }
            else {

                _parameters = sampledDataPackage.Parameters;

                _nSamples = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts;
                _nHts = _parameters.SystemPar.RadarPar.BeamParSet[0].NHts;
                if (_nSamples == 0) {
                    // in replay _nSamples is > 0 only for raw.ts files,
                    //  so the following corrects nSamples for other files
                    _nSamples = _nHts;
                }
                //_nPts = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNPts;
                _nPts = _parameters.SystemPar.RadarPar.BeamParSet[0].NPts;
                //_nSpec = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNSpec;
                _nSpec = _parameters.SystemPar.RadarPar.BeamParSet[0].NSpec;
                _nXCPtMult = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrNptsMultiplier;
                _nRx = _parameters.SystemPar.RadarPar.ProcPar.NumberOfRx;
                _maxLag = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrMaxLag;
                _polyFitOrder = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrPolyFitOrder;
                _xcorrAdjustBase = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrAdjustBase;
                _xcorrLagsToFit = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrLagsToCurveFit;
                _xcorrLagsToInterp = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrLagsToInterpolate;      
                
                CreateFilterFactors();
            }

        }

        ////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// GetReplayData
        /// </summary>
        /// <param name="sampledDataPackage"></param>
        /// <returns>bool abort</returns>
        private ReplayStatus GetReplayData(out PopDataPackage3 sampledDataPackage, out int progress, out DateTime timeStamp) {

            //bool abort1 = false;
            sampledDataPackage = null;
            progress = 0;
            timeStamp = DateTime.MinValue;

            bool readOK = false;
            // send replay processing info to _replay module
            // _replay.DataPackage.Parameters.ReplayPar = _parameters.ReplayPar;
            try {
                readOK = _replay.ReadRecord();
            }
            catch (Exception e) {
                int x = 0;
                _status = PopStatus.Stopped;
                PopNStateFile.SetCurrentStatus(_status);
                Communicator.UpdateStatus(new PopStatusMessage(PopStatus.Stopped));
                Communicator.UpdateStatus(new PopStatusMessage("** Replay ReadRecord ERROR **"));
                _noRetryOnError = true;
                throw new ApplicationException("Replay.ReadRecord Error: " + e.Message);
            }
            _progressFraction = _replay.ProgressFraction;
            progress = (int)(100 * _progressFraction);
            //Communicator.UpdateStatus(new PopStatusMessage(_status, (int)(100 * _progressFraction)));

            Thread.Sleep(10);

            PopCommands cmd = CheckCommand();
            if (cmd.Includes(PopCommands.Kill) || cmd.Includes(PopCommands.Stop)) {
                _status = PopStatus.Stopped;
                _replay.Status = ReplayStatus.EOF;
                return _replay.Status;
            }
            /*
            if (_status.Includes(PopStatus.RunningPausePending) && _replay.Status == ReplayStatus.BeforeStartTime) {
                _status = PopStatus.Paused;
                PopNStateFile.SetCurrentStatus(_status);
                Communicator.UpdateStatus(new PopStatusMessage(_status));
                cmd = WaitForGo();
                if (cmd.Includes(PopCommands.Kill) || cmd.Includes(PopCommands.Stop)) {
                    _status = PopStatus.Stopped;
                    //break;
                    //Cancel = true;
                    _replay.Status = ReplayStatus.EOF;
                    return _replay.Status;
                }
                //return _replay.Status;
            }
         
            if (cmd.Includes(PopCommands.Kill) || cmd.Includes(PopCommands.Stop)) {
                _status = PopStatus.Stopped;
                //break;
                //Cancel = true;
                _replay.Status = ReplayStatus.EOF;
                return _replay.Status;
            }
             * */

            if (_replay.DataPackage != null) {
                timeStamp = _replay.DataPackage.RecordTimeStamp;
            }
                
            if (readOK) {
                if (_replay.IsRassRecord) {
                    // POPREV as of 4.6 RASS is allowed
                    _replay.DataPackage.RassIsOn = true;
                    //sampledDataPackage = _replay.DataPackage;
                    // TODO: skipping rass
                    //return true;
                }
                else {
                    _replay.DataPackage.RassIsOn = false;
                }
            }

            if (_replay.HasStatus(ReplayStatus.AfterEndTime) ||
                _replay.HasStatus(ReplayStatus.EOF)) {
                //abort1 = true;
                return _replay.Status;
            }

            //Thread.Sleep(500);

            // get parameters from recorded data
            //	except for processing parameters from this run of POPN
            // TODO: NOTE to fix: some processing params are set in POPNReplay.POP5ToPopN()
            //      and some are set here.

            sampledDataPackage = _replay.DataPackage;

            sampledDataPackage.Parameters.Source = _parameters.Source;
            sampledDataPackage.Parameters.ReplayPar.Enabled = true;
            bool processTS = sampledDataPackage.Parameters.ReplayPar.ProcessTimeSeries = _parameters.ReplayPar.ProcessTimeSeries;
            bool processSpec = sampledDataPackage.Parameters.ReplayPar.ProcessSpectra = _parameters.ReplayPar.ProcessSpectra;
            bool processSamples = sampledDataPackage.Parameters.ReplayPar.ProcessRawSamples = _parameters.ReplayPar.ProcessRawSamples;
            bool processMom = sampledDataPackage.Parameters.ReplayPar.ProcessMoments = _parameters.ReplayPar.ProcessMoments;
            sampledDataPackage.Parameters.ReplayPar.EndDay = _parameters.ReplayPar.EndDay;
            sampledDataPackage.Parameters.ReplayPar.EndTime = _parameters.ReplayPar.EndTime;
            sampledDataPackage.Parameters.ReplayPar.StartDay = _parameters.ReplayPar.StartDay;
            sampledDataPackage.Parameters.ReplayPar.StartTime = _parameters.ReplayPar.StartTime;

            sampledDataPackage.Parameters.Debug.DebugToFile = _parameters.Debug.DebugToFile;

            // TODO add conditional on level on replay reprocessing and availability of data type (spec, mom, TS)

            if (processSamples || processTS || processSpec) {
                // spectral reprocessing

                sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.RemoveClutter = _parameters.SystemPar.RadarPar.ProcPar.RemoveClutter;
                sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.KeepOriginalSpectra = _parameters.SystemPar.RadarPar.ProcPar.KeepOriginalSpectra;
                sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.GCPercentLess = _parameters.SystemPar.RadarPar.ProcPar.GCPercentLess;
                sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.GCRestrictExtent = _parameters.SystemPar.RadarPar.ProcPar.GCRestrictExtent;
                sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.GCRestrictIfDcInPrev = _parameters.SystemPar.RadarPar.ProcPar.GCRestrictIfDcInPrev;
                sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.GCMinSigThldDB = _parameters.SystemPar.RadarPar.ProcPar.GCMinSigThldDB;
                sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.GCTimesBigger = _parameters.SystemPar.RadarPar.ProcPar.GCTimesBigger;
                sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.MaxClutterHtKm = _parameters.SystemPar.RadarPar.ProcPar.MaxClutterHtKm;
                sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.GCMethod = _parameters.SystemPar.RadarPar.ProcPar.GCMethod;

                sampledDataPackage.Parameters.ExcludeMomentIntervals.Enabled = _parameters.ExcludeMomentIntervals.Enabled;
                sampledDataPackage.Parameters.ExcludeMomentIntervals.AllModesExcludeIntervals = _parameters.ExcludeMomentIntervals.AllModesExcludeIntervals;
                
                sampledDataPackage.Parameters.SignalPeakSearchRange.Enabled = _parameters.SignalPeakSearchRange.Enabled;
                sampledDataPackage.Parameters.SignalPeakSearchRange.VelLowMS = _parameters.SignalPeakSearchRange.VelLowMS;
                sampledDataPackage.Parameters.SignalPeakSearchRange.VelHighMS = _parameters.SignalPeakSearchRange.VelHighMS;
            }

            if (processSamples || processTS || processSpec || processMom) {
                // moment processing
                sampledDataPackage.Parameters.MeltingLayerPar = _parameters.MeltingLayerPar;
            }

            if (processSamples || processTS) {

                sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.IsDcFiltering = _parameters.SystemPar.RadarPar.ProcPar.IsDcFiltering;      //
                sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.IsIcraAvg = _parameters.SystemPar.RadarPar.ProcPar.IsIcraAvg;              //
                sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.IsWindowing = _parameters.SystemPar.RadarPar.ProcPar.IsWindowing;          //
                sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.DoAutoCorr1Rx = _parameters.SystemPar.RadarPar.ProcPar.DoAutoCorr1Rx;
                sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.DoClutterWavelet = _parameters.SystemPar.RadarPar.ProcPar.DoClutterWavelet;
                sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.DoDespikeWavelet = _parameters.SystemPar.RadarPar.ProcPar.DoDespikeWavelet;
                sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.DoHarmonicWavelet = _parameters.SystemPar.RadarPar.ProcPar.DoHarmonicWavelet;
                sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.WaveletClutterThldMed = _parameters.SystemPar.RadarPar.ProcPar.WaveletClutterThldMed;
                sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.WaveletClutterCutoffMps = _parameters.SystemPar.RadarPar.ProcPar.WaveletClutterCutoffMps;
                sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.WaveletClutterMaxHt = _parameters.SystemPar.RadarPar.ProcPar.WaveletClutterMaxHt;
                sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.WaveletDespikeThldMed = _parameters.SystemPar.RadarPar.ProcPar.WaveletDespikeThldMed;
                sampledDataPackage.Parameters.Debug.SaveFilteredTS = _parameters.Debug.SaveFilteredTS;
                sampledDataPackage.Parameters.Debug.DoParallelTasks = _parameters.Debug.DoParallelTasks;
            }

            //sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.CnsPar = _parameters.SystemPar.RadarPar.ProcPar.CnsPar;
            sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.IsWritingPopFile = _parameters.SystemPar.RadarPar.ProcPar.IsWritingPopFile;
            sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.PopFilePathName = _parameters.SystemPar.RadarPar.ProcPar.PopFilePathName;
            sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.PopFiles = _parameters.SystemPar.RadarPar.ProcPar.PopFiles;
            //sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.IsWindowing = _parameters.SystemPar.RadarPar.ProcPar.IsWindowing;

            // set FMCW parameters based on replay parameter file
            int nSamples = sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts;
            sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0] = _parameters.SystemPar.RadarPar.FmCwParSet[0];
            // this parameter may be set in PopNReplay from raw.ts files
            sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts = nSamples;
            sampledDataPackage.Parameters.SystemPar.RadarPar.RadarType = _parameters.SystemPar.RadarPar.RadarType;

            //except for some items read from data file header:
            sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].SelectGatesToKeep = _parameters.SystemPar.RadarPar.FmCwParSet[0].SelectGatesToKeep;

            if (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCwSA) {
                sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrMaxLag = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrMaxLag;
                sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrFilterFraction = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrFilterFraction;
                sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrLineFitPts = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrLineFitPts;
                sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrUseFFT = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrUseFFT;
                sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrPolyFitOrder = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrPolyFitOrder;
                sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrLagsToCurveFit = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrLagsToCurveFit;
                sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrLagsToInterpolate = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrLagsToInterpolate;
                sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrAdjustBase = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrAdjustBase;
            }

            if (_parameters.SystemPar.RadarPar.FmCwParSet[0].SelectGatesToKeep) {
                sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateFirst = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateFirst;
                sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateLast = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateLast;
            }
            else {
                sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateFirst = 0;
                sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateLast = sampledDataPackage.Parameters.SystemPar.RadarPar.BeamParSet[0].NHts - 1;
            }

            if (!_parameters.ReplayPar.ProcessTimeSeries && !_parameters.ReplayPar.ProcessRawSamples) {
                // not processing Doppler time series, so use recorded parameters that we have
                sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerDcFilter = sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.IsDcFiltering;
                if (!sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.IsWindowing) {
                    sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerWindow = PopParameters.WindowType.Rectangular;
                }
                else {
                    // POPREV else added 3.19.4
                    sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerWindow = PopParameters.WindowType.Hanning;
                }
            }
            if (_parameters.ReplayPar.Enabled) {
                if (_parameters.ReplayPar.ProcessRawSamples) {
                    sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleWindow = 
                                        _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleWindow;
                    sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleDcFilter =
                                        _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleDcFilter;
                    sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleDcFilter2 =
                                        _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleDcFilter2;
                }
                if ((_parameters.ReplayPar.ProcessTimeSeries)
                    || (_parameters.ReplayPar.ProcessRawSamples)) {
                    PopParameters.WindowType window = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerWindow;
                    sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerWindow = window;
                    if (window == PopParameters.WindowType.Rectangular) {
                        sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.IsWindowing = false;
                    }
                    else {
                        sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.IsWindowing = true;
                    }
                    bool dcfil = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerDcFilter;
                    sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerDcFilter = dcfil;
                    if (dcfil) {
                        sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.IsDcFiltering = true;
                    }
                    else {
                        sampledDataPackage.Parameters.SystemPar.RadarPar.ProcPar.IsDcFiltering = false;
                    }
                                      
                }
                if ((_parameters.ReplayPar.ProcessSpectra) ||
                    (_parameters.ReplayPar.ProcessTimeSeries)
                    || (_parameters.ReplayPar.ProcessRawSamples)) {
                    // options to set if reprocessing spectra
                    
                }
                if ((_parameters.ReplayPar.ProcessMoments) ||
                    (_parameters.ReplayPar.ProcessSpectra) ||
                    (_parameters.ReplayPar.ProcessTimeSeries)
                    || (_parameters.ReplayPar.ProcessRawSamples)) {
                    // options to set if reprocessing moments
                }
                
            }
            sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNCI = sampledDataPackage.Parameters.SystemPar.RadarPar.BeamParSet[0].NCI;
            sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNPts = sampledDataPackage.Parameters.SystemPar.RadarPar.BeamParSet[0].NPts;
            sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNSpec = sampledDataPackage.Parameters.SystemPar.RadarPar.BeamParSet[0].NSpec;
            sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].IppMicroSec = sampledDataPackage.Parameters.SystemPar.RadarPar.BeamParSet[0].IppMicroSec;
            // we will not use "KeepGates" in replay mode:
            if (sampledDataPackage.Parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCwDop) {
                //sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateFirst = 0;
                //sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateLast = sampledDataPackage.Parameters.SystemPar.RadarPar.BeamParSet[0].NHts - 1;
            }

            sampledDataPackage.CurrentParIndices.BmSeqI = 0;
            sampledDataPackage.CurrentParIndices.DirI = 0;
            sampledDataPackage.CurrentParIndices.ParI = 0;

            /*
            sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerOverlap = ;
            sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].InputSampleUnitsIsRaw = ;
            sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].RangeOffsetM = ;
            sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleDcFilter = ;
            sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleDcFilter2 = ;
            sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepCenterFreqMHz = ;
            sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepOffsetHz = ;
            sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepRateHzUSec = ;
            sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleDelayNs = ;
            sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts = ;
            sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNSpec = ;
            sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleOverlap = ;
            sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleSpacingNs = ;
            sampledDataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleWindow = ;
             * */


            //sampledDataPackage.Parameters.ModeExcludeIntervals = _parameters.ModeExcludeIntervals;


            return _replay.Status;
        }



        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private PopCommands AcquireSamples(int firstSpec, int nSpecToDo) {
            PopCommands command = PopCommands.None;
            bool abort = false;

            if (_parameters.ReplayPar.Enabled) {
                // replay module has already acquired any time series data
                command = CheckCommand();
                return command;
            }
            else if ((_daqBoard != null) && !_noHardware) {
                // getting data from DAQ Board
                //double memUsedMB0 = GC.GetTotalMemory(false) / 1000000.0;
                //SendStatusString("Before AcquireDaqData: " + memUsedMB0.ToString("F0") + " MB used");
                command = AcquireDaqData(firstSpec, nSpecToDo, out abort);
                //memUsedMB0 = GC.GetTotalMemory(true) / 1000000.0;
                //SendStatusString("Before DownloadData: " + memUsedMB0.ToString("F0") + " MB used");
                if (!abort) {
                    DownloadData(firstSpec, nSpecToDo);
                }
            }
            else {
                command = CreateTestTimeSeries(firstSpec, nSpecToDo);
                /*
                do {
                    command = CheckCommand();
                    if ((command.Includes(PopCommands.Stop)) || (command.Includes(PopCommands.Kill))) {
                        _status = PopStatus.Stopped;
                        Communicator.UpdateStatus(new PopStatusMessage(_status));
                        break;
                    }
                    Thread.Sleep(1000);
                    _progress += 10;
                    Communicator.UpdateStatus(new PopStatusMessage(_status, _progress));
                    //_worker.ReportProgress(_progress, "");
                } while (_progress < 100);
                */
            }

            // rx sampling should be in the same order as the devices in name list
            double sample0 = _dataPackage.SampledTimeSeries[0][0][0][0];
            double sample1;
            if (_nRx > 1) {
                sample1 = _dataPackage.SampledTimeSeries[1][0][0][0];
            }
            string devName0, devName1, devName2;
            if (_daqBoard != null) {
                // for debugging
                if (_daqBoard.DeviceNames.Count >= 1) {
                    devName0 = _daqBoard.DeviceNames[0];
                }
                if (_daqBoard.DeviceNames.Count >= 2) {
                    devName1 = _daqBoard.DeviceNames[1];
                }
                if (_daqBoard.DeviceNames.Count >= 3) {
                    devName2 = _daqBoard.DeviceNames[2];
                }
            }

            if ((command.Includes(PopCommands.Stop)) || (command.Includes(PopCommands.Kill))) {
                return command;
            }
            else {
                command = CheckCommand();
                return command;
            }
        }

        ///////////////////////////////////////////////////////////////////////
        /// <summary>
        /// AcquireDaqData
        /// </summary>
        /// <param name="abort"></param>
        private PopCommands AcquireDaqData(int firstSpec, int nSpecToDo, out bool abort) {

            abort = false;
            DateTime currTime;
            TimeSpan timeInterval;
            PopCommands command;

            command = PopCommands.None;

            // timeOut value = expected sample time, doubled, plus 10 sec
            double timeOutTime = 1.5 * _parameters.SystemPar.RadarPar.FmCwParSet[0].IppMicroSec *
                            _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNPts *
                            _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNSpec / 1.0e6 +
                            10;

            DateTime startTime = DateTime.Now;
            _daqBoard.NDataSamplesPerDevice = _nSamples * _nPts * nSpecToDo;
            //_daqBoard.NSpec = nSpecToDo;
            if (true) {
                // _daqBoard.NDataSamples = 1000;
            }

            //SendStatusString("Size of DaqBuffer is " + (sizeof(float) * _memoryAllocator.DaqBuffer.Length).ToString() + " bytes.");
            _daqBoard.BufferForSamples = _memoryAllocator.DaqBuffer;
            //double memUsedMB1 = GC.GetTotalMemory(false) / 1000000.0;
            //Thread.Sleep(100);
            //SendStatusString("Before _daqBoard.Start(): " + memUsedMB1.ToString("F0") + " MB used");
            _daqBoard.Start();      // 30 msec
            //Thread.Sleep(100);
            //double memUsedMB2 = GC.GetTotalMemory(false) / 1000000.0;
            //SendStatusString("After _daqBoard.Start(): " + memUsedMB2.ToString("F0") + " MB used");

            BreadCrumbs.Drop(30, "--FmCwAcq");
            int statusUpdateCount = 5;
            int statusUpdateCounter = 0;
            while (!_daqBoard.DataIsAvailable) {  // 1.04*sampleTime + 200 msec

                command = CheckCommand();
                if ((command.Includes(PopCommands.Stop)) /* || (command.Includes(PopCommands.Kill)) */ ) {
                    _status = PopStatus.Stopped;
                    Communicator.UpdateStatus(new PopStatusMessage(_status));
                    _daqBoard.Stop();
                    abort = true;
                    break;
                }

                if (_daqBoard.AcqException != null) {
                    string msg = _daqBoard.AcqException.Message;
                    if (_daqBoard.AcqException.InnerException != null) {
                        msg = " Inner Exception: " + _daqBoard.AcqException.InnerException.Message;
                    }
                    throw new ApplicationException(msg);
                }

                //BreadCrumbs.Drop(40, "--FmCwAcq");
                if (_daqBoard.Aborted) {
                    BreadCrumbs.Drop(41, "--FmCwAcq");
                    abort = true;
                    //break;
                }

                BreadCrumbs.Drop(50, "--FmCwAcq");
                currTime = DateTime.Now;
                timeInterval = currTime - startTime;

                ////////////////////////////////////////////////////////
                //if (timeInterval.TotalSeconds > 15) {
                //    Thread.Sleep(38000);
                //    currTime = DateTime.Now;
                //    timeInterval = currTime - startTime;
                //}
                ////////////////////////////////////////////////////////

                if (abort || (timeInterval.TotalSeconds > timeOutTime) || (command.Includes(PopCommands.Kill))) {
                    BreadCrumbs.Drop(51, "--FmCwAcq, abort = " + abort.ToString());
                    //if (_daqBoard.Acq.CompletionStatus == AcqCompletionStatus.acsComplete) {
                    //	int x = 0;
                    //}
                    //BreadCrumbs.Drop(52, "--FmCwAcq");
                    //double acqData = _daqBoard.AcquiredSamples;
                    //BreadCrumbs.Drop(53, "--FmCwAcq");
                    //_daqBoard.Stop();

                    //AcqCompletionStatus completion = _daqBoard.CompletionStatus;

                    BreadCrumbs.Drop(54, "--FmCwAcq");
                    if (abort) {
                        BreadCrumbs.Drop(55, "--FmCwAcq");
                        SendStatusString("DAQ User Aborted.");
                        /*
                        if (_daqBoard.Acq.CompletionStatus == AcqCompletionStatus.acsUserAborted) {
                            //PopNLogger.WriteEntry("DAQ user Aborted.");
                            SendStatusString("DAQ User Aborted.");
                            break;
                        }
                        else {
                            PopNLogger.WriteEntry("DAQ non-user Aborted.");
                            //EventLogWriter.WriteEntry("DAQ non-user Aborted..", 880);
                            SendStatusString("DAQ non-user Abort: " + _daqBoard.Acq.CompletionStatus.ToString());
                            throw new ApplicationException("DAQ Abort Status.");
                        }
                         * */
                    }
                    else {
                        BreadCrumbs.Drop(58, "--FmCwAcq");
                        string time = timeOutTime.ToString();
                        PopNLogger.WriteEntry("Timeout in AcquireDaqData()");
                        try {
                            MessageBoxEx.ShowAsync("Timeout in AcquireDaqData();  ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning, 4000);
                        }
                        catch {
                        }
                        SendStatusString("%%% DAQ TimeOut. %%%");
                        throw new ApplicationException("FmCwAcquireAndProcess.Go() timeout " + time + " sec.");
                    }
                }
                BreadCrumbs.Drop(60, "--FmCwAcq");
                //_progressFraction = _daqBoard.Progress;
                if (statusUpdateCounter == 0) {
                    // update progress status
                    _progressFraction = (firstSpec + nSpecToDo * _daqBoard.Progress) / _nSpec;
                    Communicator.UpdateStatus(new PopStatusMessage(_status, (int)(100 * _progressFraction)));
                }
                statusUpdateCounter++;
                if (statusUpdateCounter == statusUpdateCount) {
                    statusUpdateCounter = 0;
                }
                Thread.Sleep(100);
                BreadCrumbs.Drop(70, "--FmCwAcq");

                //double memUsedMB20 = GC.GetTotalMemory(false) / 1000000.0;
                //SendStatusString("While waiting: " + memUsedMB20.ToString("F0") + " MB used");
            }

            //double memUsedMB22 = GC.GetTotalMemory(false) / 1000000.0;
            //SendStatusString("After DataAvailable: " + memUsedMB22.ToString("F0") + " MB used");
            
            if (_daqBoard.DataIsAvailable) {
                //_progressFraction = 1.0;
                _progressFraction = (double)(firstSpec + nSpecToDo) / _nSpec;
                Communicator.UpdateStatus(new PopStatusMessage(_status, (int)(100 * _progressFraction)));
            }
            else {
                //_progressFraction = _daqBoard.Progress;
                _progressFraction = (firstSpec + nSpecToDo * _daqBoard.Progress) / _nSpec;
                Communicator.UpdateStatus(new PopStatusMessage(_status, (int)(100 * _progressFraction)));
            }
            return command;
        }


        ///////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        private void DownloadData(int firstSpec, int nSpecToDo) {

            // debug flag
            int flag = 0;
            int ipt=0, index=0;

            try {

                // factor to convert voltage samples to raw 16-bit samples
                // for +/- 10 volt range
                double factor = GetTSeriesScaleFactor();
                flag = 1;

                //SendStatusString("DownloadData: nrx = " + _nRx.ToString());
                //SendStatusString("DownloadData: #SN = " + _daqBoard.SerialNumbers.Count.ToString());
                //SendStatusString("DownloadData: #TSrx = " + _dataPackage.SampledTimeSeries.Length.ToString());

                int[] rxOrder = new int[_nRx];
                // rxOrder[i] is the time order in which Rx#(i+1) was sampled
                for (int i = 0; i < _nRx; i++) {
                    int iOrder;// = _parameters.SystemPar.RadarPar.ProcPar.RxID[i].iSampleOrder;
                    if (_daqBoard.RxID != null && _daqBoard.RxID.Length == _nRx) {
                        // this order determined by current DAQ device
                        iOrder = _daqBoard.RxID[i].iSampleOrder;
                    }
                    else {
                        // this order determined last time setup screen run
                        iOrder = _parameters.SystemPar.RadarPar.ProcPar.RxID[i].iSampleOrder;
                    }
                    flag = 2;
                    if ((iOrder < _nRx) && (iOrder >= 0)) {
                        rxOrder[iOrder] = i;
                    }
                    flag = 3;
                }
                if (_nRx == 2) {
                    SendStatusString("DAQ dev: " + _daqBoard.SerialNumbers[0].ToString() + " " + _daqBoard.SerialNumbers[1].ToString());
                    SendStatusString("   rxOrder: " + rxOrder[0].ToString() + " " + rxOrder[1].ToString());
                }
                else if (_nRx == 3) {
                    SendStatusString("DAQ dev: " + _daqBoard.SerialNumbers[0].ToString() + " " + 
                                                    _daqBoard.SerialNumbers[1].ToString() + " " +
                                                    _daqBoard.SerialNumbers[2].ToString());
                    SendStatusString("   rxOrder: " + rxOrder[0].ToString() + " " + rxOrder[1].ToString() + " " + rxOrder[2].ToString());
                }

                bool mcc = true;
                if (_daqBoard.IntDataArray == null) {
                    mcc = false;
                }
                
                //SendStatusString("^^^ mcc = " + mcc.ToString());
                //SendStatusString("^^^ factor = " + factor.ToString());

                flag = 4;
                ipt = -1;  // counts number of samples per device
                index = 0;  // counts total samples on all devices
                int irx;
                for (int k = 0; k < nSpecToDo; k++) {
                    for (int j = 0; j < _nPts; j++) {
                        for (int i = 0; i < _nSamples; i++) {
                            ipt++;
                            for (int m = 0; m < _nRx; m++) {
                                flag = 5;
                                irx = rxOrder[m];
                                flag = 6;
                                if (mcc) {
                                    //_dataPackage.SampledTimeSeries[irx][k][j][i] = factor * _daqBoard.IntDataArray[irx][ipt];
                                    // POPREV fixed rxorder in DownloadData for MCC DAQ rev 4.0.2  20120218
                                    _dataPackage.SampledTimeSeries[irx][k][j][i] = factor * _daqBoard.IntDataArray[m][ipt];
                                }
                                else {
                                    _dataPackage.SampledTimeSeries[irx][k][j][i] = factor * _daqBoard.DataArray[index];
                                }
                                flag = 7;
                                index++;
                            }
                        }
                    }

                }
                //SendStatusString("^^^ data = " + _dataPackage.SampledTimeSeries[0][0][0][0].ToString());

            }
            catch (Exception e) {

                string msg = "Error in DownloadData()\n " + "flag = " + flag.ToString() + "ipt = " + ipt.ToString();
                SendStatusString(msg);
                SendStatusException(msg);
                throw new ApplicationException(msg + " " + e.Message); ;
            }
        }

        /// <summary>
        /// Convert voltage units to 16-bit raw
        /// </summary>
        /// <returns></returns>
        private double GetTSeriesScaleFactor() {
            double factor;
            if (!_parameters.SystemPar.RadarPar.FmCwParSet[0].InputSampleUnitsIsRaw) {
                factor = Math.Pow(2.0, 15.0) / 10.0;
            }
            else {
                factor = 1.0;
            }
            return factor;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handles events thrown by DAQ board progress
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void UpdateFmCwProgress(object sender, EventArgs args) {

            //Console.Beep(1000, 100);
            // this handles events thrown by DAQ board progress
            int id = Thread.CurrentThread.ManagedThreadId;
            double progress = _progressFraction;
            int progressPercent = (int)(progress * 100.0 + 0.5);
            //_dataPackage.Progress = progress;
            //CheckPauseStatus();
            // dac added rev 2.0
            //_dataPackage.Message = _modeString;
            //_controlWorker.ReportProgress(progressPercent, _dataPackage);
            ////////////////
            // dac: UpdatePopStatus call can cause exception thrown from ReportProgress
            //	if worker has already completed at this point;
            //	happens mostly with ABORT button during dwell
            bool busy;
            if (_controlWorker.IsBusy && !_controlWorker.CancellationPending) {
                try {
                    busy = _controlWorker.IsBusy;
                    Communicator.UpdateStatus(new PopStatusMessage(_status, progressPercent));
                }
                catch (Exception exx) {
                    string message = exx.Message;
                    throw;
                }
            }
            // we will never intentionally cancel the dwell worker thread
            if (_controlWorker.CancellationPending) {
                _daqBoard.Stop();
            }
            ////
        }

        private PopCommands ProcessTimeSeries(int firstSpec, int nSpecToDo) {

            PopCommands command;

            if (_endOfData) {
                command = CheckCommand();
                return command;
            }

            if ((!_parameters.ReplayPar.Enabled ||
                _parameters.ReplayPar.ProcessRawSamples ||
                _parameters.ReplayPar.ProcessTimeSeries) &&
                _dataPackage.TransformedTimeSeries != null) {

                //if (_dataPackage.Spectra != null &&
                //    _dataPackage.XCorrMag != null) {
                        
                ProcessDopplerTimeSeries(_dataPackage.TransformedTimeSeries,
                                        _dataPackage.Spectra, 
                                        _dataPackage.XCorrMag, 
                                        _maxLag, 
                                        firstSpec, 
                                        nSpecToDo);

                //}
            }

            Communicator.UpdateStatus(new PopStatusMessage(_status));
            command = CheckCommand();
            return command;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeSeries"></param>
        /// <param name="XCorr"></param>
        /// <param name="firstSpec"></param>
        /// <param name="nSpecToDo"></param>
        private IntelMath XCorrelations = null;

        /// <summary>
        /// Do Doppler time series processing here.
        /// Do both spectra and xcorrelations so that they can use
        ///     the same modified time series while still keeping original series for recording
        ///     without having to duplicate the entire huge timeseries array.
        /// </summary>
        /// <param name="timeSeries"></param>
        /// <param name="XCorrMag"></param>
        /// <param name="XCorrRatio"></param>
        /// <param name="maxLag"></param>
        /// <param name="firstSpec"></param>
        /// <param name="nSpecToDo"></param>
        private void ProcessDopplerTimeSeries(Ipp64fc[][][][] timeSeries,
                                                double[][][] spectra,
                                                double[][][] XCorrMag,
                                                int maxLag,
                                                int firstSpec, 
                                                int nSpecToDo) {

            Stopwatch watch = new Stopwatch();
            double secPerTick = 0.0;
            if (_parameters.Debug.DebugToFile) {
                bool isHiRes = Stopwatch.IsHighResolution;
                long freq = Stopwatch.Frequency;
                long nanosecPerTick = (1000L * 1000L * 1000L) / Stopwatch.Frequency;
                secPerTick = 1.0 / Stopwatch.Frequency;
            }

            watch.Start();
            long startTick = watch.ElapsedTicks;

            bool DebugWithNewInput = false;
            bool DebugWithNewInput2 = false;
            if (!_parameters.ReplayPar.Enabled && _noHardware && _parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCwSA) {
                if (DebugWithNewInput) {
                    Random rr = new Random(83627);
                    double valr,valq;
                    double N = 50.0;
                    for (int irx = 0; irx < _nRx; irx++) {
                        for (int iht = 0; iht < _nHts; iht++) {
                            double ssr = 0.0;
                            double ssq = 0.0;
                            int cnt = 0;
                            for (int i = 0; i < 4 * N; i++) {
                                valr = rr.NextDouble();
                                valq = rr.NextDouble();
                                ssr = ((N - 1.0) * ssr + valr) / N;  // a running average
                                ssq = ((N - 1.0) * ssq + valq) / N;
                            }
                            for (int ispec = 0; ispec < _nSpec; ispec++) {
                                for (int ipt = 0; ipt < _nPts; ipt++) {
                                    valr = rr.NextDouble();
                                    valq = rr.NextDouble();
                                    cnt++;
                                    ssr = ((N - 1.0) * ssr + valr) / N;  // a running average
                                    //timeSeries[irx][ispec][iht][ipt].re = (ipt + ispec * _nPts) * iht;  // a ramp
                                    timeSeries[irx][ispec][iht][ipt].re = ssr;

                                    ssq = ((N - 1.0) * ssq + valq) / N;
                                    //timeSeries[irx][ispec][iht][ipt].im = (ipt + ispec * _nPts) * iht;
                                    timeSeries[irx][ispec][iht][ipt].im = ssq;
                                }
                            }
                        }
                    }

                }
                if (DebugWithNewInput2) {
                    int pp1, pp2;
                    int newipt, newispec;
                    for (int iht = 0; iht < _nHts; iht++) {
                        for (int ispec = 0; ispec < _nSpec; ispec++) {
                            for (int ipt = 0; ipt < _nPts; ipt++) {
                                pp1 = ipt + _nPts * ispec;
                                pp1 += 100;
                                pp1 = pp1 % (_nSpec * _nPts);
                                newipt = pp1 % _nPts;
                                newispec = pp1 / _nPts;
                                timeSeries[1][ispec][iht][ipt].re = timeSeries[0][newispec][iht][newipt].re;
                                timeSeries[1][ispec][iht][ipt].im = timeSeries[0][newispec][iht][newipt].im;
                                pp2 = ipt + _nPts * ispec;
                                pp2 += 200;
                                pp2 = pp2 % (_nSpec * _nPts);
                                newipt = pp2 % _nPts;
                                newispec = pp2 / _nPts;
                                timeSeries[2][ispec][iht][ipt].re = timeSeries[0][newispec][iht][newipt].re;
                                timeSeries[2][ispec][iht][ipt].im = timeSeries[0][newispec][iht][newipt].im;
                            }
                        }
                    }
                }
            }

            DopplerTSArgs tsArgs = new DopplerTSArgs(timeSeries,
                                                    _parameters,
                                                    _nRx, _nHts, _nPts, _nSpec, _nXCPtMult,
                                                    firstSpec,
                                                    maxLag,
                                                    spectra,
                                                    XCorrMag,
                                                    _dataPackage.WaveletClutterTransform);

            DopplerTSProcessor tsProc = null;

            for (int ispec = 0; ispec < nSpecToDo; ispec++) {

                if (_parameters.Debug.DoParallelTasks) {

                    ParallelLoopResult result = Parallel.For<DopplerTSProcessor>(0, _nHts,          // loop index range
                                                   () => new DopplerTSProcessor(tsArgs),                                // initialization
                                                   (iht, loop, aProc) => { aProc.Compute(iht, ispec); return aProc; },  // body of loop
                                                                                                    // iht: value of loop index
                                                                                                    // loop: ParallelLoopState object provided by Parallel class
                                                                                                    // aProc: thread local variable (type DopplerTSProcessor)
                                                   (x) => {
                                                       _dataPackage.WaveletClippedNpts = x.WaveletClippedNpts;
                                                       _dataPackage.WaveletOutputNpts = x.WaveletOutputNpts;
                                                       _dataPackage.XCorrNPts = x.XCorrNPts;
                                                       _dataPackage.XCorrNAvgs = x.XCorrNAvgs;
                                                       x = null;
                                                   });                                     // thread cleanup: argument x is aProc variable returned by body
                    
                }
                else {
                    if (tsProc == null) {
                        tsProc = new DopplerTSProcessor(tsArgs);
                    }
                    for (int iht = 0; iht < _nHts; iht++) {
                        tsProc.Compute(iht, ispec);
                    }  // end iht loop
                    _dataPackage.WaveletClippedNpts = tsProc.WaveletClippedNpts;
                    _dataPackage.WaveletOutputNpts = tsProc.WaveletOutputNpts;
                    _dataPackage.XCorrNPts = tsProc.XCorrNPts;
                    _dataPackage.XCorrNAvgs = tsProc.XCorrNAvgs;
                }

            }  // end ispec loop

            if (_parameters.Debug.DebugToFile) {
                watch.Stop();
                long endTicks = watch.ElapsedTicks;
                TimeSpan elapsedTime = watch.Elapsed;
                double sec1 = (endTicks - startTick) * secPerTick;
                SendStatusString("     Elapsed time for ProcDopp: " + elapsedTime.ToString());
            }

        }  // end method ProcessDopplerTimeSeries()


        /// <summary>
        /// 
        /// </summary>
        /// <param name="firstSpec"></param>
        /// <param name="nSpecToDo"></param>
        /// <returns></returns>
        private PopCommands ProcessRawSamples(int firstSpec, int nSpecToDo) {

            PopCommands command;

            if (_endOfData) {
                command = CheckCommand();
                return command;
            }

            Communicator.UpdateStatus(new PopStatusMessage(_status));

            //double memUsedMB = GC.GetTotalMemory(false) / 1000000.0;
            //SendStatusString("In ProcessSamples1: " + memUsedMB.ToString("F0") + " MB used");

            BreadCrumbs.Drop(201, "= ProcessSamples.");
            if ((!_parameters.ReplayPar.Enabled || _parameters.ReplayPar.ProcessRawSamples) &&
                _dataPackage.TransformedTimeSeries != null) {
                // Do FmCw transformation of sampled data
                if (_parameters.ReplayPar.Enabled) {
                    // check, in case did not read a transformed time series
                    //BreadCrumbs.Drop(202, "= ProcessSamples; < AllocateTTS.");
                    //AllocateTTSArray();  // POPREV 3.26.1
                }
                if (_dataPackage.SampledTimeSeries != null) {
                    //BreadCrumbs.Drop(201, "= ProcessSamples; < TransformFM.");
                    TransformFMSamples(_dataPackage.SampledTimeSeries, _dataPackage.TransformedTimeSeries, firstSpec, nSpecToDo);
                    //BreadCrumbs.Drop(202, "= ProcessSamples; > TransformFM.");
                }

                /**/
                    
                if (!_parameters.ReplayPar.Enabled) {
                    if (((_daqBoard==null) || _noHardware)) {
                        // test data
                        // apply envelope to Doppler time series to try to emulate spaced antenna delay
                        //if (_dataPackage.Parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCwSA) {
                        if (false) {
                            double delta, factor;
                            for (int isp = 0; isp < nSpecToDo; isp++) {
                                for (int iht = 0; iht < _nHts; iht++) {
                                    for (int ipt = 0; ipt < _nPts; ipt++) {
                                        delta = Math.Abs(_nPts / 2 - ipt);
                                        factor = 50.0 / (delta + 50.0);
                                        //factor = Math.Exp(-(_nPts / 2 - ipt) * (_nPts / 2 - ipt)/100000.0);
                                        _dataPackage.TransformedTimeSeries[0][isp][iht][ipt].re *= factor;
                                        _dataPackage.TransformedTimeSeries[0][isp][iht][ipt].im *= factor;
                                        delta = Math.Abs(_nPts / 2 + 75 - ipt);
                                        factor = 50.0 / (delta + 50.0);
                                        //factor = Math.Exp(-(_nPts / 2 - ipt + 100) * (_nPts / 2 - ipt + 100) / 100000.0);
                                        _dataPackage.TransformedTimeSeries[1][isp][iht][ipt].re *= factor;
                                        _dataPackage.TransformedTimeSeries[1][isp][iht][ipt].im *= factor;
                                        delta = Math.Abs(_nPts / 2 - 125 - ipt);
                                        factor = 50.0 / (delta + 50.0);
                                        //factor = Math.Exp(-(_nPts / 2 - ipt - 100) * (_nPts / 2 - ipt - 100) / 100000.0);
                                        _dataPackage.TransformedTimeSeries[2][isp][iht][ipt].re *= factor;
                                        _dataPackage.TransformedTimeSeries[2][isp][iht][ipt].im *= factor;
                                    }
                                }
                            }
                            
                        }  // end if SA
                    }
                }
                /**/
            }

            Communicator.UpdateStatus(new PopStatusMessage(_status));
            command = CheckCommand();
            return command;

        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Do FmCw transformation of sampled data
        /// </summary>
        /// <remarks>
        /// Input is SampledTimeSeries;
        /// Process according to _parameters
        /// Output is TransformedTimeSeries;
        /// </remarks>
        /// <param name="SampledTimeSeries"></param>
        private IntelMath RawSpec = null;
        private void TransformFMSamples(double[][][][] sampledTimeSeries, Ipp64fc[][][][] transformedTimeSeries, int firstSpec, int nSpecToDo) {

            if (transformedTimeSeries== null) {
                return;
            }

            if (RawSpec == null) {
                RawSpec = new IntelMath();
            }

            Ipp64fc[] outArrayRC;
            double[] tsr;
            if (_useAlloc) {
                outArrayRC = null;
                outArrayRC = new Ipp64fc[_nHts];
                tsr = new double[_nSamples];
            }
            else {
                outArrayRC = null;
                outArrayRC = new Ipp64fc[_nHts];
                tsr = new double[_nSamples];
            }

            //double memUsedMB = GC.GetTotalMemory(false) / 1000000.0;
            //SendStatusString("In TransformFMSamples1: " + memUsedMB.ToString("F0") + " MB used");
            
            // DC Filter samples before any other processing
            //  (2nd filter added rev 2.13)
            // DAC NOTE: DC filter modifies time series arrays
            if (_parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleDcFilter) {
                // DC filter samples of one IPP
                for (int irx = 0; irx < _nRx; irx++) {
                    for (int ispec = 0; ispec < nSpecToDo; ispec++) {
                        for (int ipt = 0; ipt < _nPts; ipt++) {
                            RawSpec.ApplyDCFilter(sampledTimeSeries[irx][ispec][ipt], _nSamples);
                        }
                    }
                }
            }
            if (_parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleDcFilter2) {
                // DC filter over npts for each sample gate
                for (int irx = 0; irx < _nRx; irx++) {
                    for (int ispec = 0; ispec < nSpecToDo; ispec++) {
                        for (int igate = 0; igate < _nSamples; igate++) {
                            double sum = 0;
                            for (int ipt = 0; ipt < _nPts; ipt++) {
                                sum += sampledTimeSeries[irx][ispec][ipt][igate];
                            }
                            double mean = sum / _nPts;
                            for (int ipt = 0; ipt < _nPts; ipt++) {
                                sampledTimeSeries[irx][ispec][ipt][igate] -= mean;
                            }
                        }
                    }
                }
            }

            //memUsedMB = GC.GetTotalMemory(false) / 1000000.0;
            //SendStatusString("In TransformFMSamples2: " + memUsedMB.ToString("F6") + " MB used");

            for (int irx = 0; irx < _nRx; irx++) {
                for (int ispec = 0; ispec < nSpecToDo; ispec++) {
                    for (int ipt = 0; ipt < _nPts; ipt++) {

                        if (ipt <= 1 ) {
                            //memUsedMB = GC.GetTotalMemory(false) / 1000000.0;
                            //SendStatusString("  In TransformFMSamples2.1: " + memUsedMB.ToString("F6") + " MB used");
                        }

                        if (_parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleWindow == PopParameters.WindowType.Rectangular) {
                            if (FFT.IsPowerOf2(_nSamples)) {
                                RawSpec.FFT(sampledTimeSeries[irx][ispec][ipt], outArrayRC, _nSamples);
                            }
                            else {
                                RawSpec.DFT(sampledTimeSeries[irx][ispec][ipt], outArrayRC, _nSamples);
                            }
                        }
                        else {
                            if (_parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleWindow == PopParameters.WindowType.Hamming) {
                                RawSpec.ApplyHammingWindow(sampledTimeSeries[irx][ispec][ipt], tsr, _nSamples);
                            }
                            else if (_parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleWindow == PopParameters.WindowType.Hanning) {
                                RawSpec.ApplyHanningWindow(sampledTimeSeries[irx][ispec][ipt], tsr, _nSamples);
                            }
                            else if (_parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleWindow == PopParameters.WindowType.Blackman) {
                                RawSpec.ApplyBlackmanWindow(sampledTimeSeries[irx][ispec][ipt], tsr, _nSamples);
                            }
                            else /*if (__parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleWindow == PopParameters.WindowType.Riesz)*/ {
                                RawSpec.ApplyRieszWindow(sampledTimeSeries[irx][ispec][ipt], tsr, _nSamples);
                            }
                            if (FFT.IsPowerOf2(_nSamples)) {
                                RawSpec.FFT(tsr, outArrayRC, _nSamples);
                            }
                            else {
                                RawSpec.DFT(tsr, outArrayRC, _nSamples);
                            }
                        }

                        if (ipt <= 1) {
                            //memUsedMB = GC.GetTotalMemory(true) / 1000000.0;
                            //SendStatusString("  In TransformFMSamples2.2: " + memUsedMB.ToString("F6") + " MB used");
                        }

                        // apply factor to Doppler time series to account for the fact that
                        //  the power in the raw, sampled TS is distributed over NSamples
                        //  number of Doppler time series.
                        // This will scale the Doppler power spectra to have integrated power
                        //  equal to variance of the raw time series.
                        // This value actually makes the spectral power 2x too large,
                        //  because power spectra is power in I plus power in Q.
                        // So apply factor of 2 later to Doppler spectra if we want
                        //  power spectra to equal variance of one channel.
                        double PopFactor = Math.Sqrt(_parameters.GetSamplesPerIPP(0));

                        for (int i = 0; i < _nHts; i++) {
                            // output of FFT is Doppler time series.
                            // reverse I and Q for proper Doppler sense.
                            // dac rev 2.10 -- unless negative offset
                            if (_parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepOffsetHz >= 0.0) {
                                transformedTimeSeries[irx][ispec][i][ipt].im = PopFactor * outArrayRC[i].re;
                                transformedTimeSeries[irx][ispec][i][ipt].re = PopFactor * outArrayRC[i].im;
                            }
                            else {
                                transformedTimeSeries[irx][ispec][i][ipt].im = PopFactor * outArrayRC[i].im;
                                transformedTimeSeries[irx][ispec][i][ipt].re = PopFactor * outArrayRC[i].re;
                            }
                        }

                        if (ipt <= 1) {
                            //memUsedMB = GC.GetTotalMemory(true) / 1000000.0;
                            //SendStatusString("  In TransformFMSamples2.3: " + memUsedMB.ToString("F6") + " MB used");
                        }
                        
                        // test verifies whether FFT routine preserves power
                        bool doTest = false;
                        if (doTest) {
                            double tsPower = RawSpec.TotalPowerTS(sampledTimeSeries[irx][ispec][ipt], _nSamples);
                            double fftPower = RawSpec.TotalPowerFFT(outArrayRC, _nSamples/2 + 1);
                            double ratio1 = tsPower / fftPower;
                        }

                        // dac added rev 2.9:
                        if (_parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepOffsetHz < 0) {
                            // negative rx freq offset:
                            // invert order of FFT points from 0 to nHTs-1
                            int offsetGates = -(int)_parameters.GetGateOffset() + 1;
                            //int nHts = _parameters.SystemPar.RadarPar.BeamParSet[0].NHts;
                            ipp.Ipp64fc save;
                            int j;
                            for (int i = 0; i < offsetGates; i++) {
                                j = offsetGates - i - 1;
                                if (j > i) {
                                    save = transformedTimeSeries[irx][ispec][i][ipt];
                                    transformedTimeSeries[irx][ispec][i][ipt] = transformedTimeSeries[irx][ispec][j][ipt];
                                    transformedTimeSeries[irx][ispec][j][ipt] = save;
                                }
                                else {
                                    break;
                                }
                            }
                        }  // end if TxSweepOffsetHz < 0

                        if (ipt <= 1) {
                            //memUsedMB = GC.GetTotalMemory(true) / 1000000.0;
                            //SendStatusString("  In TransformFMSamples2.4: " + memUsedMB.ToString("F6") + " MB used");
                        }

                    }  //end for ipt
                }  // end for ispec
            }  // end for irx

            //memUsedMB = GC.GetTotalMemory(false) / 1000000.0;
            //SendStatusString("In TransformFMSamples3: " + memUsedMB.ToString("F0") + " MB used");
        
        }  // end TransformFMSamples() method


        /// <summary>
        /// Create filter factors from coefficients that are in text file.
        /// </summary>
        private void CreateFilterFactors() {

            TextFile inputFile = null;
            double[] lowPassCoeffs = null, highPassCoeffs = null;
            double f0 = double.MaxValue;
            double nLoPass, nHiPass;
            double maxCorrection = -40.0;

            if ((_filterFactors == null)  || (_filterFactors.GetLength(0) != _nHts)) {
                _filterFactors = null;
                _filterFactors = new double[_nHts];
            }

            for (int iht = 0; iht < _nHts; iht++) {
                _filterFactors[iht] = 1.0;
            }
            
            bool readCoeffs = _parameters.SystemPar.RadarPar.FmCwParSet[0].ApplyFilterCorrection &&
                                _parameters.SystemPar.RadarPar.FmCwParSet[0].UseFilterCoeffs;
            bool readGain = _parameters.SystemPar.RadarPar.FmCwParSet[0].ApplyFilterCorrection &&
                                _parameters.SystemPar.RadarPar.FmCwParSet[0].UseFreqResp;
            if (readCoeffs) {
                inputFile = new TextFile();
                string inputPath = Path.Combine(_appDirectory, _parameters.SystemPar.RadarPar.FmCwParSet[0].FilterFile);
                bool openOK = inputFile.OpenForReading(inputPath);
                if (openOK) {
                    try {
                        GetNextValue(inputFile, out f0);
                        GetNextValue(inputFile, out maxCorrection);
                        GetNextValue(inputFile, out nHiPass);
                        highPassCoeffs = new double[(int)nHiPass];
                        for (int i = 0; i < (int)nHiPass; i++) {
                            GetNextValue(inputFile, out highPassCoeffs[i]);
                        }
                        GetNextValue(inputFile, out nLoPass);
                        lowPassCoeffs = new double[(int)nLoPass];
                        for (int i = 0; i < (int)nLoPass; i++) {
                            GetNextValue(inputFile, out lowPassCoeffs[i]);
                        }

                        if (inputFile != null) {
                            inputFile.Close();
                        }

                        // now compute the filter factors from the filter coefficients:
                        if (lowPassCoeffs == null || highPassCoeffs == null) {
                            for (int iht = 0; iht < _nHts; iht++) {
                                _filterFactors[iht] = 1.0;
                            }
                        }
                        else {
                            int nGates = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts;
                            int gateSpacingNs = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleSpacingNs;
                            double deltaFreqPoint = 1.0e9 / (double)nGates / (double)gateSpacingNs;
                            for (int iht = 0; iht < _nHts; iht++) {
                                double dF = iht * deltaFreqPoint;
                                if (dF < f0) {
                                    // high-pass region
                                    double sum = 0.0;
                                    int nCoeffs = highPassCoeffs.Length;
                                    for (int i = nCoeffs; i > 0; i--) {
                                        sum = sum * dF + highPassCoeffs[i - 1];
                                    }
                                    _filterFactors[iht] = sum;
                                }
                                else {
                                    // low-pass region
                                    double sum = 0.0;
                                    int nCoeffs = lowPassCoeffs.Length;
                                    for (int i = nCoeffs; i > 0; i--) {
                                        sum = sum * dF + lowPassCoeffs[i - 1];
                                    }
                                    _filterFactors[iht] = sum;
                                }

                                //and limit correction to 40 dB
                                if (_filterFactors[iht] <= (-maxCorrection / 2.0)) {
                                    _filterFactors[iht] = -maxCorrection / 2.0;
                                }
                                _filterFactors[iht] = Math.Pow(10.0, _filterFactors[iht] / 10.0);
                            }
                        }
                    }
                    catch (Exception e) {
                        // error reading coefficients
                        SendStatusString("Error reading filter coeffs.");
                        SendStatusString(e.Message);
                        PopNLogger.WriteEntry(e.Message);
                        //MessageBoxEx.Show(e.Message, 5000);
                    }
                }
                else {
                    string message = "Error opening filter coeff file: " + _parameters.SystemPar.RadarPar.FmCwParSet[0].FilterFile;
                    SendStatusString(message);
                    PopNLogger.WriteEntry(message);
                    //MessageBoxEx.Show(message, 5000);
                }
            }
            else if (readGain) {

                inputFile = new TextFile();
                string inputPath = Path.Combine(_appDirectory, _parameters.SystemPar.RadarPar.FmCwParSet[0].FilterFile);
                bool openOK = inputFile.OpenForReading(inputPath);
                if (!inputPath.ToLower().EndsWith(".gain")) {
                    openOK = false;
                }

                if (openOK) {
                    try {
                        double sampleSpacing = -1.0;
                        int fileNHts = -1;
                        string label;
                        int nValuesFound;
                        double[] values = new double[2];
                        bool readOK;
                        int linesRead = 0;
                        int index = 0;
                        int increment = 1;
                        int skip = 1;
                        do {
                            readOK = GetNextLabelLine(inputFile, out label, out nValuesFound, values);
                            if (readOK) {
                                if (label.ToLower().Contains("spacing")) {
                                    if (nValuesFound > 0) {
                                        sampleSpacing = values[0];
                                    }
                                }
                                else if (label.ToLower().Contains("nhts") ||
                                         label.ToLower().Contains("npts")) {
                                     if (nValuesFound > 0) {
                                        fileNHts = (int)values[0];
                                    }
                                }
                                else if (nValuesFound == 2) {
                                    if (fileNHts > 0) {
                                        // See if we need to skip pts in file
                                        // because it is larger than nhts.
                                        // NOTE: we are not interpolating if file is smaller than nhts.
                                        increment = (fileNHts - 1) / (_nHts - 1);
                                        if (increment < 1) {
                                            increment = 1;
                                        }
                                    }
                                    // if 2 values found, use second as gain factor
                                    skip--;
                                    if (skip == 0) {
                                        if (index < _filterFactors.GetLength(0)) {
                                            _filterFactors[index++] = values[1];
                                            skip = increment;
                                        }
                                    }
                                    linesRead++;
                                }
                            }
                        } while (readOK);
                    }
                    catch (Exception e) {
                        // error reading gain factors
                        SendStatusString("Error reading gain factors.");
                        SendStatusString(e.Message);
                        PopNLogger.WriteEntry(e.Message);
                        //MessageBoxEx.Show(e.Message, 5000);
                    }
                }
                else {
                    string message = "Error opening filter gain file: " + _parameters.SystemPar.RadarPar.FmCwParSet[0].FilterFile;
                    SendStatusString(message);
                    PopNLogger.WriteEntry(message);
                    //MessageBoxEx.Show(message, 5000);
                }
            }



            return;

        }

        /// <summary>
        /// Reads the next line that can be interpreted without error as a double value;
        /// Therefore all comments must be on separate lines and blank lines are allowed.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool GetNextValue(TextFile reader, out double value) {
            bool foundValue = false;
            string line;
            value = 0.0;
            do {
                line = reader.ReadLine();
                if (line == null) {
                    throw new ApplicationException("Error reading values in filter coeff file.");
                    //return false;
                }
                foundValue = double.TryParse(line, out value);
            } while (!foundValue);
            return true;

        }

        /// <summary>
        /// Read a line in a text file
        /// Assign valid double values to values array (up to size of the array)
        /// Assign the first non-double (or int) item as a label string.
        /// values array is dimensioned by the caller.
        /// Blank lines are skipped without errror.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="label"></param>
        /// <param name="nValuesFound"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        private bool GetNextLabelLine(TextFile reader, out string label, out int nValuesFound, double[] values) {

            char[] delim = {' ',',','\t'};
            string[] strArray;
            bool foundLabel = false;
            //int nValuesFound = 0;
            string line;
            int maxValues = values.GetLength(0);
            bool parseOK;
            int itemCount;

            do {
                label = "";
                nValuesFound = 0;
                itemCount = 0;
                line = reader.ReadLine();
                if (line == null) {
                    //throw new ApplicationException("Error reading values in filter coeff file.");
                    return false;
                }
                strArray = line.Split(delim);
                if (strArray != null) {
                    double value;
                    itemCount = strArray.GetLength(0);
                    if (itemCount > 0) {
                        for (int i = 0; i<itemCount; i++) {
                            if (!string.IsNullOrEmpty(strArray[i])) {
                                parseOK = double.TryParse(strArray[i], out value);
                                if (!parseOK && !foundLabel) {
                                    label = strArray[i];
                                    foundLabel = true;
                                }
                                else if (nValuesFound < maxValues) {
                                    values[nValuesFound] = value;
                                    nValuesFound++;
                                }
                            }
                        }
                    }
                }
            } while (itemCount == 0);
            return true;

        }


	}  // end class PopNDwellWorker

	///////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// 
	/// </summary>
	public class CommandQueue {

		private Queue _queue;

		public CommandQueue() {
			_queue = new Queue();
		}

		public int Count {
			get { return _queue.Count; }
		}

        public void Clear() {
            lock (_queue.SyncRoot) {
                _queue.Clear();
            }
        }

		public void Enqueue(PopCommands item) {
			lock (_queue.SyncRoot) {
				_queue.Enqueue(item);
			}
		}

		public PopCommands Dequeue() {
			PopCommands command = PopCommands.None;
			lock (_queue.SyncRoot) {
				if (_queue.Count > 0) {
					command = (PopCommands)_queue.Dequeue();
				}
				else {
					command = PopCommands.None;
				}
			}
			return command;
		}

		public PopCommands Peek() {
			PopCommands command = PopCommands.None;
			lock (_queue.SyncRoot) {
				if (_queue.Count > 0) {
					command = (PopCommands)_queue.Peek();
				}
				else {
					command = PopCommands.None;
				}
			}
			return command;
		}

	}  // end class CommandQueue

    //////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 
    /// </summary>
    static class BreadCrumbs {
        static private StringBuilder text;
        static public bool Enabled;
        static public string DebugFileName = "DebugStatus.txt";
        static BreadCrumbs() {
            text = new StringBuilder(500);
            Enabled = false;
        }
        public static void Drop(int num, string message) {
            if (Enabled) {
                if (num == 0) {
                    text = new StringBuilder(500);
                }
                else {
                    text.AppendLine(num.ToString() + ": " + message + "; " + DateTime.Now.ToString());
                    TextFile.WriteLineToFile(DebugFileName, text.ToString(), false);
                }

            }
        }
        public static void Drop(int num) {
            if (Enabled) {
                if (num == 0) {
                    text = new StringBuilder(500);
                }
                else {
                    text.AppendLine(num.ToString() + "; " + DateTime.Now.ToString());
                    TextFile.WriteLineToFile(DebugFileName, text.ToString(), false);
                }

            }
        }
    }  // end class BreadCrumbs

    public static class ParxFileArchiver {

        private static string _loggedFileName;
        private static PopParameters _loggedParams;
        private static PopParameters _newParams;

        static ParxFileArchiver() {
            _loggedFileName = "";
            _loggedParams = null;
        }

        public static void Archive(string newFile) {
            try {
                _newParams = PopParameters.ReadFromFile(newFile);
            }
            catch (Exception ee) {
                throw new ApplicationException("param file error in Archive");
            }
            if (_loggedFileName != String.Empty) {
                //_loggedParams = PopParameters.ReadFromFile(_loggedFileName);
                if (_newParams.Equals(_loggedParams)) {
                    // same parameter file, do not archive
                    return;
                }
            }

            DateTime now = DateTime.Now;

            string folder = "";
            if (_newParams.SystemPar.RadarPar.ProcPar.PopFiles[0].FileWriteEnabled) {
                folder = _newParams.SystemPar.RadarPar.ProcPar.PopFiles[0].FileFolder;
            }
            else if (_newParams.SystemPar.RadarPar.ProcPar.PopFiles[1].FileWriteEnabled) {
                folder = _newParams.SystemPar.RadarPar.ProcPar.PopFiles[1].FileFolder;
            }
            if (String.IsNullOrEmpty(folder)) {
                return;
            }

            string station = _newParams.SystemPar.StationName;
            string year = String.Format("{0:D2}", now.Year % 100);
            string day = String.Format("{0:D3}", now.DayOfYear);
            string hour = String.Format("{0:D2}", now.Hour);
            string minute = String.Format("{0:D2}", now.Minute);
            string second = String.Format("{0:D2}", now.Second);
            string archFileName = station + "_" + year + day + "_" + hour + minute + second + ".parx";
            string archFullPath = Path.Combine(folder, archFileName);
            _loggedFileName = archFullPath;
            _loggedParams = _newParams.DeepCopy();

            _loggedParams.WriteToFile(_loggedFileName);

            PopNLogger.WriteEntry("  Archiving parx file.");

            return;
        }

    }  // end class ParxFileArchiver


    public static class PopNLogger {

        public static string LogFolder;

        public static void WriteEntry(string message) {
            DacLogger.WriteEntry(message, LogFolder);
        }
    }  // end class PopNLogger


}  // end namespace POPN4Service
