using System;
using System.Drawing;
using System.Windows.Forms;
using System.ServiceProcess;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;
using System.Configuration;

using ipp;

//using CustomControls;

using POPCommunication;
using POPN4Service;

using DACarter.PopUtilities;
using DACarter.Utilities;
using DACarter.Utilities.Graphics;
using POPN;
using System.Text;

namespace POPN4 {
    public partial class POPN4MainForm : Form {

        private PopNSetup3 SetupForm;
        private SequenceForm SeqForm;

        private POPCommunicator _communicator;
        private POPN4Service.POPN4Service _service;
        //private delegate void UpdateTextCallback(string text);
		//private delegate void UpdateProgressCallback(int progress);
		//private delegate void UpdateStatusCallback(PopStatus status);
        //private delegate void UpdateTimeStampCallback(DateTime timeStamp);
        private System.Windows.Forms.Timer statusTimer;     // timer on UI thread to check for POP status msgs
        private System.Windows.Forms.Timer serviceTimer;     // timer on UI thread to check service
        private System.Timers.Timer _pingTimer;             // timer on sub-thread 
        private bool _timerIsBusy = false;
        private object _timerLock;
        private StatusQueue _statusQueue;

        private bool _usingSeqFile;  // not using a single parx file

        //private bool _showingPlotOptions;
        private QuickPlotZ _zPlotSamples, _zPlotTS, _zPlotSpec, _zPlotAScan;
        private QuickPlotZ _zPlotCltrWvlt;
        private QuickPlotZ _zPlotDoppler, _zPlotMoments;
        private QuickPlotZ _zPlotXCorr, _zPlotXCorrRatio, _zPlotXCorrProfile;
        private PowerMeterDisplay _powerMeterDisplay;
        PopParameters _param, _currentParameters;

		private PopStatus _currentStatus;
		//private PopDataPackage _status;
        private string _currentConfigFile;

		private string _currentParxFilePath, _previousParxFilePath;
		private string _currentDirectory;
        private string _logFolder;

        private bool _tryRestart;
        private bool _multipleNoService;
        bool _doParFileChangedMessage = true;
        DateTime _parFileUpdateTime;

        private string _parxFilePath;
        private string _parxFolderRelPath;
        private string _parxFolderFullPath;

        //private int buttonCount = 0;

        private bool _noService;
        //private bool _noHardware;
        //private bool _daqWithoutPbx;
        //private bool _debug;
        private bool _useAllocator;

        string[] XCorrLabel;

        //public System.Diagnostics.EventLog EventLogPOPN4Service;
        private POPN4Service.POPNEventLogWriter _eventLogWriter;

        private MemoryMappedViewAccessor _AscanPlotView;
        private MemoryMappedFile _AscanPlotMmf;
        private MemoryMappedViewAccessor _DoppTSPlotView;
        private MemoryMappedFile _DoppTSPlotMmf;
        private MemoryMappedFile _SpecPlotMmf;
        private MemoryMappedViewAccessor _SpecPlotView;
        private MemoryMappedFile _RawSamplesPlotMmf;
        private MemoryMappedViewAccessor _RawSamplesPlotView;
        private MemoryMappedViewAccessor _CltrWvltPlotView;
        private MemoryMappedFile _CltrWvltPlotMmf;

        private MemoryMappedViewAccessor _MomentsPlotView;
        private MemoryMappedFile _MomentsPlotMmf;
        private MemoryMappedViewAccessor _MeanDoppPlotView;
        private MemoryMappedFile _MeanDoppPlotMmf;


        public POPN4MainForm(string[] args) {

            // testing:
            //_mmf99 = MemoryMappedFile.CreateNew("Global\\Junk123", 1000, MemoryMappedFileAccess.ReadWrite);


            _tryRestart = true;
            _doParFileChangedMessage = true;
            _parFileUpdateTime = DateTime.MaxValue;
            _multipleNoService = false;
            _noService = false;
            //_noHardware = false;
            //_daqWithoutPbx = false;
            //_debug = false;
            _useAllocator = false;
            _logFolder = "";
            //_parxFileName = "";
            _currentParxFilePath = "";
            _previousParxFilePath = "";
            foreach (string arg in args) {
                if (arg.ToLower() == "-noservice") {
                    _noService = true;
                }
                else if (arg.ToLower() == "-nohardware") {
                    //_noHardware = true;
                }
                else if (arg.ToLower() == "-nopbx") {
                    //_daqWithoutPbx = true;
                }
                else if (arg.ToLower().StartsWith("-d")) {
                    //_debug = true;
                }
            }

            InitializeComponent();

            _eventLogWriter = new POPNEventLogWriter();

            System.Reflection.Assembly myAssembly = System.Reflection.Assembly.GetExecutingAssembly();
			//System.Resources.ResourceManager myManager = new System.Resources.ResourceManager("POPN2.PopNMainForm", myAssembly);
			string[] names = myAssembly.GetManifestResourceNames();

			//Stream ss = myAssembly.GetManifestResourceStream("POPN.POPN.ico");
			Stream ss = myAssembly.GetManifestResourceStream("POPN4.POPN4.ico");
			if (ss != null) {
				this.Icon = new Icon(ss);
			}

            _timerLock = new object();
			_currentStatus = PopStatus.None;
			_currentDirectory = Application.StartupPath;
            toolTip1.SetToolTip(labelConfigExpand, _currentDirectory);

            //string parxFolder = Properties.Settings.Default.ParxFolder;
            string parxFolder = ".";
            parxFolder = PopNStateFile.GetParFileFolderRelPath();
            if (string.IsNullOrWhiteSpace(parxFolder)) {
                parxFolder = @"..\parameters";
            }
            //parxFolder = "config";
            if (Path.IsPathRooted(parxFolder)) {
                // parxFolder is a full path
                _parxFolderFullPath = parxFolder;
                //Uri baseUri = new Uri(_currentDirectory + "\\");
                //Uri fileUri = new Uri(_parxFolderFullPath + "\\");
                ////_parxFolderFullPath = Uri.UnescapeDataString(fileUri.ToString());
                ////_parxFolderFullPath.Replace('/', Path.DirectorySeparatorChar);
                //Uri relUri = baseUri.MakeRelativeUri(fileUri);
                //string relPath = Uri.UnescapeDataString(relUri.ToString());
                //relPath.Replace('/', Path.DirectorySeparatorChar);
                try {
                    _parxFolderRelPath = Tools.GetRelativePath(_currentDirectory, _parxFolderFullPath);
                }
                catch (Exception ex) {
                    // full path given does not have common prefix with _currentDirectory
                    _parxFolderRelPath = _parxFolderFullPath;
                }
                //Properties.Settings.Default.ParxFolder = _parxFolderFullPath;
            }
            else {
                // is relative path
                _parxFolderRelPath = parxFolder;
                _parxFolderFullPath = Path.Combine(_currentDirectory, _parxFolderRelPath);
                if (_parxFolderFullPath.EndsWith("\\")) {
                    // since StartupPath() returns string without slash at end
                    //  make that the standard for other folders
                    //_parxFolderFullPath = _parxFolderFullPath.Substring(0, _parxFolderFullPath.Length - 1); ;
                }

                // do this to simplify possible navigation in path (e.g. ..\..):
                _parxFolderFullPath = Path.GetFullPath(_parxFolderFullPath);

                _parxFolderRelPath = Tools.GetRelativePath(_currentDirectory, _parxFolderFullPath);

                //Properties.Settings.Default.ParxFolder = _parxFolderRelPath;
            }



            // Put all available parx files in combobox dropdown
            //  and select last used parx file as default
            populateParxCombo(_parxFolderRelPath);
            string defaultPar = PopNStateFile.GetLastParFile();
            //toolTip1.SetToolTip(labelConfigExpand, defaultPar);
            if (!string.IsNullOrWhiteSpace(defaultPar) && (File.Exists(defaultPar))) {
                defaultPar = Path.GetFileName(defaultPar);
                int iFile = 0;
                foreach (string file in comboBoxConfigFile.Items) {
                    string listName = Path.GetFileName(file);
                    if (listName.ToLower() == defaultPar.ToLower()) {
                        break;
                    }
                    iFile++;
                }
                if ((iFile >= 0) && (iFile < comboBoxConfigFile.Items.Count)) {
                    comboBoxConfigFile.SelectedIndex = iFile;
                }
            }
            else if (comboBoxConfigFile.Items.Count > 0) {
                defaultPar = (string)comboBoxConfigFile.Items[0];
            }
            else {
                defaultPar = "";
            }
            comboBoxConfigFile.Text = defaultPar;

            PopStatus status = PopNStateFile.GetCurrentStatus();
            if (status.Includes(PopStatus.Running)) {
                _tryRestart = true;
            }

            checkBoxClutterWavelet.Checked = false;
            checkBoxDopplerTS.Checked = false;
            checkBoxSampledTS.Checked = false;
            checkBoxDopplerAScan.Checked = false;
            checkBoxDopplerSpec.Checked = false;
            checkBoxCrossCorr.Checked = false;

            //_showingPlotOptions = false;
            checkBoxPlotOptions.Checked = true;
            checkBoxPlotOptions.Checked = false;

            _zPlotSamples = new QuickPlotZ();
            _zPlotAScan = new QuickPlotZ();
            _zPlotSpec = new QuickPlotZ();
            _zPlotXCorr = new QuickPlotZ();
            _zPlotXCorrRatio = new QuickPlotZ();
            _zPlotXCorrProfile = new QuickPlotZ();
            _zPlotTS = new QuickPlotZ();
            _zPlotDoppler = new QuickPlotZ();
            _zPlotMoments = new QuickPlotZ();
            _zPlotCltrWvlt = new QuickPlotZ();

            _powerMeterDisplay = new PowerMeterDisplay();
            Point location1 = Properties.Settings.Default.PowerMeterLocation;
            _powerMeterDisplay.Location = location1;

            XCorrLabel = new string[3];
            XCorrLabel[0] = "Rx1-Rx2";
            XCorrLabel[1] = "Rx2-Rx3";
            XCorrLabel[2] = "Rx3-Rx1";
        }

        private void POPN4MainForm_Load(object sender, EventArgs e) {

            //SendMessageToListLog("Line1\r\n\rLine2");

            int id = Thread.CurrentThread.ManagedThreadId;
			// create thread that attempts to connect to comm server
			//textBoxClientStatus.Text = "Connecting to server...";
			EnableButtons(false);
            buttonServiceStatus_Click(null, null);
            SimpleWorkerThread thread1 = new SimpleWorkerThread();
            thread1.SetWorkerMethod(CreateComm);
            thread1.SetCompletedMethod(CreateCommCompleted);
            thread1.Go();
            statusTimer = new System.Windows.Forms.Timer();
            statusTimer.Tick += new EventHandler(_statusTimer_Tick);
            statusTimer.Interval = 500;
            statusTimer.Start();
            serviceTimer = new System.Windows.Forms.Timer();
            serviceTimer.Tick += new EventHandler(_serviceTimer_Tick);
            serviceTimer.Interval = 2500;
            serviceTimer.Start();

			_pingTimer = new System.Timers.Timer();
			_pingTimer.Interval = 5610;
			_pingTimer.Elapsed += new System.Timers.ElapsedEventHandler(_pingTimer_Tick);
            // dac TODO removed from 3.0.4 ; added back to 3.0.18:
			_pingTimer.Start();
           
            _statusQueue = new StatusQueue();

            /*
            if (!_noHardware) {   
                if (_daqWithoutPbx) {
                    string message = "DAQ running on INTERNAL trigger.";
                    SendMessageToListLog(message);
                }
            }
             * */
            //buttonServiceStatus_Click(null, null);

            // get previous position settings from config file
            try {
                // this is where user.config file is located:
                string path = Application.LocalUserAppDataPath;

                if (Properties.Settings.Default.UpgradeRequired) {
                    // if new version, get settings from previous ver, rather than default values
                    Properties.Settings.Default.Upgrade();
                    Properties.Settings.Default.UpgradeRequired = false;
                    Properties.Settings.Default.Save();
                }
                
                Size formSize = Properties.Settings.Default.MainFormSize;
                if (!formSize.IsEmpty) {

                    if (Properties.Settings.Default.OptionsAreExpanded) {
                        checkBoxPlotOptions.Checked = true;
                    }
                    int htIndex = Properties.Settings.Default.HtIndex;
                    if (htIndex < 0) {
                        htIndex = 0;
                    }
                    numericUpDownPlotHt.Maximum = htIndex;
                    numericUpDownPlotHt.Value = htIndex;
                    // to avoid throwing exception:
                    int rxVal = Properties.Settings.Default.RxNumber;
                    if (rxVal < 1) {
                        rxVal = 1;
                    }
                    numericUpDownPlotRx.Maximum = rxVal;
                    numericUpDownPlotRx.Value = rxVal;
                    checkBoxMoments.Checked = Properties.Settings.Default.ShowSNRProfile;
                    checkBoxDopplerTS.Checked = Properties.Settings.Default.ShowDopplerTimeSeries;
                    checkBoxDopplerSpec.Checked = Properties.Settings.Default.ShowDopplerSpectrum;
                    checkBoxCrossCorr.Checked = Properties.Settings.Default.ShowCrossCorr;
                    checkBoxDopplerAScan.Checked = Properties.Settings.Default.ShowDopplerAscan;
                    checkBoxDoppler.Checked = Properties.Settings.Default.ShowDopplerProfile;
                    checkBoxSampledTS.Checked = Properties.Settings.Default.ShowSampledTimeSeries;
                    checkBoxClutterWavelet.Checked = Properties.Settings.Default.ShowClutterWavelet;

                    this.DesktopBounds = new Rectangle(Properties.Settings.Default.MainFormLocation, formSize);

                }

                Point location;
                Size size;
                size = Properties.Settings.Default.DopplerProfSize;
                location = Properties.Settings.Default.DopplerProfLocation;
                _zPlotDoppler.setPosition(location, size);

                size = Properties.Settings.Default.SNRProfileSize;
                location = Properties.Settings.Default.SNRProfileLocation;
                _zPlotMoments.setPosition(location, size);

                size = Properties.Settings.Default.DopplerSpecSize;
                location = Properties.Settings.Default.DopplerSpecLocation;
                _zPlotSpec.setPosition(location, size);

                size = Properties.Settings.Default.CrossCorrSize;
                location = Properties.Settings.Default.CrossCorrLocation;
                _zPlotXCorr.setPosition(location, size);

                size = Properties.Settings.Default.CrossCorrRatioSize;
                location = Properties.Settings.Default.CrossCorrRatioLocation;
                _zPlotXCorrRatio.setPosition(location, size);

                size = Properties.Settings.Default.CrossCorrSlopeSize;
                location = Properties.Settings.Default.CrossCorrSlopeLocation;
                _zPlotXCorrProfile.setPosition(location, size);

                size = Properties.Settings.Default.ClutterWaveletSize;
                location = Properties.Settings.Default.ClutterWaveletLocation;
                _zPlotCltrWvlt.setPosition(location, size);

                size = Properties.Settings.Default.DopplerTSSize;
                location = Properties.Settings.Default.DopplerTSLocation;
                _zPlotTS.setPosition(location, size);

                size = Properties.Settings.Default.DopplerAScanSize;
                location = Properties.Settings.Default.DopplerAScanLocation;
                _zPlotAScan.setPosition(location, size);

                size = Properties.Settings.Default.SampledTSSize;
                location = Properties.Settings.Default.SampledTSLocation;
                _zPlotSamples.setPosition(location, size);

            }
            catch (Exception ee) {
                MessageBoxEx.ShowAsync("POPN4.exe.config file: " + ee.Message, 4000);
            }

            SetMessageListBoxSize(false);
        }

        void CreateCommCompleted(object o1, object o2) {
            buttonServiceStatus_Click(null, null);
            // send _communicator to SetupForm, so SetupForm can communicate with DAQ
            if (SetupForm != null) {
                SetupForm.Communicator = _communicator;
            }
        }

        private void SendMessageToListLog(string message, bool alsoWriteLog = true) {
            message += "  (" + DateTime.Now.DayOfYear.ToString("000 ") + DateTime.Now.ToString("HH:mm:ss") + ")";
            UpdateMessageList(message);
            string logFolder;
            if (!String.IsNullOrWhiteSpace(_logFolder)) {
                logFolder = _logFolder;
            }
            else {
                //logFolder = Path.GetDirectoryName(Application.ExecutablePath);
                logFolder = PopNStateFile.GetLogFolder();
            }
            if (alsoWriteLog) {
                DacLogger.WriteEntry(message, logFolder);
            }
        }

        /// <summary>
        /// worker thread routine to install/start/connect to service
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private object CreateComm(object arg) {

            if (_noService) {
                // configured not to run as service.
                // Service code is run inside this application.
                // To avoid conflicts, make sure service is not running.
                ServiceController sc = GetPopNService();
                if (sc != null) {
                    if (sc.Status == ServiceControllerStatus.Running) {
                        sc.Stop();
                    }
                }
                _service = new POPN4Service.POPN4Service();
                // run the service's startup code, but from here, not from service:
                _service.PublicOnStart(false);
                SendMessageToListLog("Starting WITHOUT running as a service.");
            }
            else {
                //UpdateMessageList("Initilize POPN4 service..");
                SendMessageToListLog("Initilize POPN4 service..");
                // start the service:
                InitService();
            }

            //if (_communicator == null) {
                
                string text = "Connecting to comm server, thread " + Thread.CurrentThread.ManagedThreadId.ToString();
                UpdateClientText(text);
                _communicator = new POPCommunicator(POPCommunicator.POPCommType.Client);
			    //Console.Beep(220, 300);
			    if (_communicator.Client == null) {
                    //MessageBox.Show("Failed to connect to server.");
                    text = "Failed to connect to comm server.";
				    //UpdateClientText(text);
                    //UpdateMessageList(text);
                    SendMessageToListLog(text);

                }
                else {
                    text = "Connected to comm server.";
				    //UpdateClientText(text);
                    //UpdateMessageList(text);
                    SendMessageToListLog(text);
                    _communicator.StatusUpdated += OnStatusUpdated; 

                }
            //}
            //Console.Beep(220, 200);

			// only now is it safe to start running

			EnableButtons(true);
            Console.Beep(3000, 60);

            return null;
        }

		private delegate void EnableButtonsCallback(bool enable);
		private void EnableButtons(bool enable) {
			if (this.buttonGo.InvokeRequired) {
				//EnableButtonsCallback d = new EnableButtonsCallback(EnableButtons);
				//this.Invoke(d, new object[] { enable });
                this.Invoke(new MethodInvoker(() => EnableButtons(enable)));
            }
			else {
				buttonGo.Enabled = enable;
				buttonKill.Enabled = enable;
				buttonStop.Enabled = enable;
				checkBoxPause.Enabled = enable;
			}
		}

        private void InitService() {
            bool isInstalled = true;

            //SendMessageToListLog("Inside InitService");
            ServiceController sc = GetPopNService();
            Microsoft.Win32.RegistryKey rk1 = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                                                @"System\CurrentControlSet\Services\PopNService");
            //SendMessageToListLog("Got registry key");
            if (rk1 != null) {
                string executable = (string)rk1.GetValue("ImagePath");
                executable = executable.Trim('\"');
                string servicePath = Path.GetDirectoryName(executable);
                //SendMessageToListLog("Service path = " + servicePath);

                string appFolder = Path.GetDirectoryName(Application.ExecutablePath);
                //SendMessageToListLog("appfolder = " + appFolder);

                if (servicePath != appFolder) {
                    // the currently running service is not from the executable in this directory
                    //  so uninstall the previous service
                    //SendMessageToListLog("Uninstalling...");
                    bool isUnInstalled = InstallService("-u");
                }
            }
            //SendMessageToListLog("Done checking path");

            // make sure service is running
            //MessageBox.Show("StartService...");
            bool isRunning = StartService();
            //MessageBox.Show("return from StartService");
            if (!isRunning) {
                // could not start, try installing
                // If first install fails, try uninstalling
                // then reinstalling
                int nTries = 3;
                for (int iTry = 0; iTry < nTries; iTry++) {
                    if (iTry == 0 || iTry == 2) {

                        string text = "Installing POPN4 service.";
                       // UpdateMessageList(text);
                        SendMessageToListLog(text);
                        isInstalled = InstallService();
                        if (isInstalled) {
                            // is installed, try to start again
                            isRunning = StartService();
                            if (!isRunning) {
                                text = "Installed OK -- Failed to start service.";
                                //UpdateClientText(text);
                                //UpdateMessageList(text);
                                SendMessageToListLog(text);
                            }
                            else {
                                // successful install
                                break;
                            }
                        }
                        else {
                            text = "Failed to install service.";
                            //UpdateClientText(text);
                            //UpdateMessageList(text);
                            SendMessageToListLog(text);
                        }
                    }  // end if iTry
                    else {
                        // iTry == 1
                        string text = "UnInstalling POPN4 service...";
                        //UpdateMessageList(text);
                        SendMessageToListLog(text);
                        bool isUnInstalled = InstallService("-u");
                        if (!isUnInstalled) {
                            text = "Uninstall Failed.";
                            //UpdateMessageList(text);
                            SendMessageToListLog(text);
                        }
                        else {
                            text = "Try again...";
                            //UpdateMessageList(text);
                            SendMessageToListLog(text);
                        }
                    }
                } // end for iTry
            }
            else {
                SendMessageToListLog("POPN4 service is running.");
            }
        }

		private static bool InstallService(string args = "") {
            bool successful = false;
            string executableName = "POPN4Service.exe"; 
            string folder = Path.GetDirectoryName(Application.ExecutablePath);
            string servicePath = Path.Combine(folder, executableName);
			string arguments = args;
            if (!File.Exists(servicePath)) {
                // exe is not here, maybe it has a shortcut
                servicePath += ".lnk";
                executableName += ".lnk";
            }
            if (File.Exists(servicePath)) {

				using (Process proc = new Process()) {

                    proc.StartInfo.FileName = executableName;
					proc.StartInfo.Arguments = arguments;
					proc.StartInfo.WorkingDirectory = "";
					proc.StartInfo.UseShellExecute = true;
					proc.Start();

					proc.WaitForExit();

					//runResults.ExitCode = proc.ExitCode;

                    successful = true;
                }

			}
			else {
				//throw new ArgumentException( ("Cannot find service program file named " + executablePath));
                successful = false;
			}
            return successful;
		}

        private ServiceController GetPopNService() {
            ServiceController sc = null;
            ServiceController[] scServices;
            scServices = ServiceController.GetServices();
            foreach (ServiceController scTemp in scServices) {

                if (scTemp.ServiceName == "POPNService") {
                    sc = scTemp;
                    break;
                }
            }
            return sc;
        }

        private void buttonServiceStatus_Click(object sender, EventArgs e) {
            ServiceController sc = GetPopNService();
            if (sc != null) {
                if (sc.Status == ServiceControllerStatus.Running) {
                    buttonServiceStatus.BackColor = Color.Aquamarine;
                    buttonServiceStatus.Text = "Running";
                }
                else if (sc.Status == ServiceControllerStatus.Stopped) {
                    buttonServiceStatus.BackColor = Color.Pink;
                    buttonServiceStatus.Text = "Stopped";
                }
                else if (sc.Status == ServiceControllerStatus.StopPending) {
                    buttonServiceStatus.BackColor = Color.Fuchsia;
                    buttonServiceStatus.Text = "StopPending";
                }
                else if (sc.Status == ServiceControllerStatus.StartPending) {
                    buttonServiceStatus.BackColor = Color.Fuchsia;
                    buttonServiceStatus.Text = "StartPending";
                }
                else {
                    buttonServiceStatus.BackColor = Color.OldLace;
                    buttonServiceStatus.Text = "Unknown";
                }
            }
            else {
                buttonServiceStatus.BackColor = Color.Fuchsia;
                buttonServiceStatus.Text = "No Service";
            }
        }

        private bool StartService() {
			// check if service is stopped; if so try to start
			bool isSuccessful = false;
			ServiceController[] scServices;
            //MessageBox.Show("GetServices...");
            scServices = ServiceController.GetServices();
            //MessageBox.Show("return from GetServices");
            foreach (ServiceController scTemp in scServices) {

				if (scTemp.ServiceName == "POPNService") {
                    SendMessageToListLog("Checking POPN4 service...");
					ServiceController sc = new ServiceController("POPNService");
                    int count = 0;
					int timeOut = 10;
                    if (sc.Status == ServiceControllerStatus.StopPending) {
                        // only known way out of this state is murder -- kill the process:
                        using (Process proc = new Process()) {

                            proc.StartInfo.FileName = "taskkill";
                            proc.StartInfo.Arguments = "/IM popn4Service.exe /F";
                            proc.StartInfo.WorkingDirectory = "";
                            proc.StartInfo.UseShellExecute = true;
                            proc.Start();

                            proc.WaitForExit();
                        }

                        break;
                    }
                    if (sc.Status == ServiceControllerStatus.Stopped) {
                        //MessageBox.Show("status = stopped");
                        //UpdateMessageList("Trying to start service...");
                        SendMessageToListLog("Trying to start service...");
                        try {
                            //MessageBox.Show("starting service...");
                            TextFile.WriteLineToFile("DebugStatus2.txt", "-------------- starting service... " + DateTime.Now.ToString(), false);
                            sc.Start();
                            //sc.WaitForStatus(ServiceControllerStatus.Running);
                        }
                        catch (Exception e) {
                            MessageBoxEx.Show("Start service exception = " + e.Message,4000);
                            if (e.InnerException != null) {
                                if (e.InnerException.Message != null) {
                                    MessageBoxEx.Show("Start service inner exception = " + e.InnerException.Message, 5000);
                                }
                            }
                            break;
                        }
                        while (sc.Status != ServiceControllerStatus.Running) {
                            Thread.Sleep(1000);
                            sc.Refresh();
                            count++;
                            if (count >= timeOut) {
                                SendMessageToListLog("Cannot start service. Status = " + sc.Status.ToString());
                                break;
                            }
                        }
                    }
                    else {
                        //MessageBox.Show("status = not stopped: " + sc.Status.ToString());
                        //UpdateMessageList("Service already running.");
                        SendMessageToListLog("Service already running.");
                    }
					if (sc.Status == ServiceControllerStatus.Running) {
						isSuccessful = true;
					}
                    //UpdateMessageList("Service status = " + sc.Status.ToString());
                    SendMessageToListLog("Service status = " + sc.Status.ToString());
                    break;
				}
			}
            //MessageBox.Show("isSuccessful = " + isSuccessful.ToString());
            return isSuccessful;
		}

        /// <summary>
        /// Receives status updates from dwell worker.
        /// Adds status to queue, which is read out during timer ticks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
		private void OnStatusUpdated(object sender, POPCommunicator.PopStatusArgs args) {
            int id = Thread.CurrentThread.ManagedThreadId;
            _statusQueue.Enqueue(args);
            //Thread.Sleep(400);
            return;
			//UpdateStatusText(args.Status.Message);
            //UpdateStatusProgress(args.Status.ProgressPercent);
            //UpdateStatus(args.Status.Status);
            //return;
        }

        /// <summary>
        /// This updates the display of the commanded parameter file.
        /// In the single parameter mode case, this is just the current parx file.
        /// In the multiple parameter mode case, this is the *.seq sequence file name.
        /// </summary>
        /// <param name="parFile"></param>
        private void UpdateStatusCmdParFile(string parFile) {
            if (String.IsNullOrWhiteSpace(parFile)) {
                return;
            }
            if (this.comboBoxConfigFile.IsDisposed) {
                return;
            }
            if (this.comboBoxConfigFile.InvokeRequired) {
                // being called from a different thread
                this.Invoke(new MethodInvoker(() => UpdateStatusCmdParFile(parFile)));
            }
            else {
                // being called from UI thread
                _currentParxFilePath = parFile;
                //_parxFileName = Path.GetFileName(parFile);
                comboBoxConfigFile.Text = Path.GetFileName(parFile);
                //toolTip1.SetToolTip(labelConfigExpand, parFile);
                if ((_param == null) ||
                    String.IsNullOrWhiteSpace(_previousParxFilePath) ||
                    (_previousParxFilePath != _currentParxFilePath)) {

                    // read in current parameters for first time
                    string ext = Path.GetExtension(_currentParxFilePath);
                    if (ext.ToLower().Contains("parx")) {
                        // POPREV: 3.15 see if we can get away with not reading parfile here:
                        //_param = PopParameters.ReadFromFile(_currentParxFilePath);
                        //_logFolder = _param.SystemPar.RadarPar.ProcPar.PopFiles[0].LogFileFolder;
                    }
                    _previousParxFilePath = _currentParxFilePath;
                }
            }
        }

        /// <summary>
        /// This updates the display of the current parx file name, when in 
        /// the multiple or sequence file mode.
        /// </summary>
        /// <param name="parFile"></param>
        private void UpdateStatusCurParFile(string parFile) {
            if (String.IsNullOrWhiteSpace(parFile)) {
                return;
            }
            if (this.labelCurrentParxFile.IsDisposed) {
                return;
            }
            if (this.labelCurrentParxFile.InvokeRequired) {
                // being called from a different thread
                this.Invoke(new MethodInvoker(() => UpdateStatusCurParFile(parFile)));
            }
            else {
                labelCurrentParxFile.Text = Path.GetFileName(parFile);
            }
        }


        private void UpdateStatusText(string text) {
            if (text == String.Empty) {
                return;
            }
            // this method can be called after form closed
            // if message sent from service
            if (this.listBoxMessages.IsDisposed) {
                return;
            }
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.listBoxMessages.InvokeRequired) {
                //UpdateTextCallback d = new UpdateTextCallback(UpdateStatusText);
                //this.Invoke(d, new object[] { text });
                this.Invoke(new MethodInvoker(() => UpdateStatusText(text)));
            }
            else {
                text = "Server: " + text;
                //this.textBoxServerStatusText.Text = text;
                SendMessageToListLog(text, alsoWriteLog: false);
                //this.listBoxMessages.Items.Add(text);
                //listBoxMessages.SetSelected(listBoxMessages.Items.Count - 1, true);
                //listBoxMessages.SetSelected(listBoxMessages.Items.Count - 1, false);
            }
        }

        private void UpdateStatusException(string text) {
            if ((text == String.Empty) || (text == null)) {
                return;
            }
            // this method can be called after form closed
            // if message sent from service
            if (this.listBoxMessages.IsDisposed) {
                return;
            }
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.listBoxMessages.InvokeRequired) {
                this.Invoke(new MethodInvoker(() => UpdateStatusException(text)));
            }
            else {
                text = "Exception: " + text;
                //this.textBoxServerStatusText.Text = text;
                
                SendMessageToListLog(text, alsoWriteLog: true);
                //this.listBoxMessages.Items.Add(text);
                //listBoxMessages.SetSelected(listBoxMessages.Items.Count - 1, true);
                //listBoxMessages.SetSelected(listBoxMessages.Items.Count - 1, false);
                for (int i = 0; i < 1; i++) {
                    Console.Beep(880, 400);
                    Console.Beep(440, 400);
                }
                listBoxMessages.BackColor = Color.Yellow;
            }
        }

        private void UpdateMessageList(string text) {
            string[] lines;
            if (this.listBoxMessages.IsDisposed) {
                return;
            }
            if (this.listBoxMessages.InvokeRequired) {
                //UpdateTextCallback d = new UpdateTextCallback(UpdateMessageList);
                //this.Invoke(d, new object[] { text });
                this.Invoke(new MethodInvoker(() => UpdateMessageList(text)));
            }
            else {
                lines = text.Split('\n');
                foreach (string msg in lines) {
                    this.listBoxMessages.Items.Add(msg);
                    listBoxMessages.SetSelected(listBoxMessages.Items.Count - 1, true);
                    listBoxMessages.SetSelected(listBoxMessages.Items.Count - 1, false);
                }
            }
        }

        private void UpdateTimeStamp(DateTime timeStamp) {
            if (timeStamp.Year > 1) {
                if (this.labelTimeStamp.InvokeRequired) {
                    //UpdateTimeStampCallback d = new UpdateTimeStampCallback(UpdateTimeStamp);
                    //this.Invoke(d, new object[] { timeStamp });
                    this.Invoke(new MethodInvoker(() => UpdateTimeStamp(timeStamp)));
                }
                else {
                    labelTimeStamp.Text = timeStamp.ToString("HH:mm:ss") +
                        "." + (timeStamp.Millisecond/100).ToString("0") +
                        " (" + timeStamp.DayOfYear.ToString("000") + ")";
                }
            }
        }

        private void UpdateStatus(PopStatus status) {
            if (status == PopStatus.None) {
                return;
            }
            if (this.xpProgressBar1.IsDisposed) {
                return;
            }
			if (this.xpProgressBar1.InvokeRequired) {
                //UpdateStatusCallback d = new UpdateStatusCallback(UpdateStatus);
                //this.Invoke(d, new object[] { status });
                this.Invoke(new MethodInvoker(() => UpdateStatus(status)));
            }
            else {
                // parFile combobox is disabled unless in stop state
                if (!status.Includes(PopStatus.Stopped)) {
                    if (comboBoxConfigFile.Focused) {
                        buttonGo.Focus();
                    }
                    comboBoxConfigFile.Enabled = false;
                }

                if (status.Includes(PopStatus.DataReady)) {
                    _communicator.RequestParameters(out _param);
                    PlotData(_param);
                }

                // testing for states that are mutually exclusive:
				if (status.Includes(PopStatus.Running)) {
					xpProgressBar1.ColorBackGround = Color.GreenYellow;
					xpProgressBar1.ColorBarBorder = Color.MediumSeaGreen;
					xpProgressBar1.ColorBarCenter = Color.Lime;
					xpProgressBar1.ColorText = Color.Black;
					xpProgressBar1.Text = "Running";
					buttonGo.Enabled = false;
					buttonGo.BackColor = Color.LightGreen;
					buttonStop.Enabled = true;
					buttonStop.BackColor = Color.OrangeRed;
					buttonStop.ForeColor = Color.Yellow;
					//buttonParameters.Enabled = false;
					//labelTimeStamp.Text = arg.Status.RecordTimeStamp.ToString("HH:mm:ss");
				}
				else if (status.Includes(PopStatus.Stopped)) {
                    if (!comboBoxConfigFile.Enabled) {
                        comboBoxConfigFile.Enabled = true;
                    }
                    xpProgressBar1.ColorBackGround = Color.Red;
					xpProgressBar1.ColorBarBorder = Color.Firebrick;
					xpProgressBar1.ColorBarCenter = Color.Red;
					xpProgressBar1.ColorText = Color.Yellow;
					xpProgressBar1.Position = 100;
					xpProgressBar1.Text = "Stopped";
					buttonGo.Enabled = true;
					buttonGo.BackColor = Color.Lime;
                    // POPREV: 4.2.2 keep abort button enabled so we can renumerate DDS if needed
					//buttonStop.Enabled = false;
					//buttonStop.BackColor = Color.BlanchedAlmond;
					//buttonParameters.Enabled = true;
					//labelTimeStamp.Text = arg.Status.RecordTimeStamp.ToString("HH:mm:ss");
				}
				else if (status.Includes(PopStatus.Paused)) {
					xpProgressBar1.ColorBackGround = Color.Yellow;
					xpProgressBar1.ColorBarBorder = Color.Yellow;
					xpProgressBar1.ColorBarCenter = Color.Yellow;
					xpProgressBar1.ColorText = Color.Black;
					xpProgressBar1.Position = 100;
					xpProgressBar1.Text = "Paused";
					buttonGo.Enabled = true;
					buttonGo.BackColor = Color.Lime;
					buttonStop.Enabled = true;
					buttonStop.BackColor = Color.OrangeRed;
					buttonStop.ForeColor = Color.Yellow;
					//buttonParameters.Enabled = false;
					//labelTimeStamp.Text = arg.Status.RecordTimeStamp.ToString("HH:mm:ss");
				}
				else if (status.Includes(PopStatus.RunningPausePending)) {
					xpProgressBar1.ColorBackGround = Color.GreenYellow;
					xpProgressBar1.ColorBarBorder = Color.Goldenrod;
					xpProgressBar1.ColorBarCenter = Color.Gold;
					xpProgressBar1.ColorText = Color.Black;
					xpProgressBar1.Text = "Pause Pending...";
					buttonGo.Enabled = false;
					buttonGo.BackColor = Color.LightGreen;
					buttonStop.Enabled = true;
					buttonStop.BackColor = Color.OrangeRed;
					buttonStop.ForeColor = Color.Yellow;
					//buttonParameters.Enabled = false;
					//labelTimeStamp.Text = arg.Status.RecordTimeStamp.ToString("HH:mm:ss");
				}
                else if (status.Includes(PopStatus.NoService)) {
                    if (!comboBoxConfigFile.Enabled) {
                        comboBoxConfigFile.Enabled = true;
                    }
                    xpProgressBar1.ColorBackGround = Color.Red;
					xpProgressBar1.ColorBarBorder = Color.Firebrick;
					xpProgressBar1.ColorBarCenter = Color.Red;
					xpProgressBar1.ColorText = Color.Yellow;
					xpProgressBar1.Position = 100;
					xpProgressBar1.Text = "No Service";
					buttonGo.Enabled = true;
					buttonGo.BackColor = Color.Lime;
					buttonStop.Enabled = false;
					buttonStop.BackColor = Color.BlanchedAlmond;
                }

				if (status.Includes(PopStatus.Computing)) {
					xpProgressBar1.Text = "Computing...";
				}
                if (status.Includes(PopStatus.Writing)) {
                    xpProgressBar1.Text = "Writing...";
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        /// <param name="par"></param>
        private void PlotData(PopParameters par) {


            if (par == null) {
                return;
            }
            int nrx = par.SystemPar.RadarPar.ProcPar.NumberOfRx;
            int nhts;
            if (par.ReplayPar.Enabled) {
                // number of hts written to POP file
                nhts = par.SystemPar.RadarPar.BeamParSet[0].NHts;
            }
            else {
                // all hts computed
                nhts = par.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts / 2 + 1;
            }
            int npts = par.SystemPar.RadarPar.FmCwParSet[0].DopplerNPts;
            numericUpDownPlotRx.Maximum = nrx;
            numericUpDownPlotHt.Maximum = nhts-1;
            int irx = (int)numericUpDownPlotRx.Value - 1;
            int iht = (int)numericUpDownPlotHt.Value;
            if (irx < 0) {
                irx = 0;
            }
            if (irx > nrx - 1) {
                irx = nrx + 1;
            }
            if (iht < 0) {
                iht = 0;
            }
            if (iht > nhts - 1) {
                iht = nhts - 1;
            }

            bool isAtLeastOnePlot = false;

            if (!par.ReplayPar.Enabled && par.SystemPar.RadarPar.PowMeterPar.Enabled) {
                DisplayPowerMeter();
            }

            if (checkBoxSampledTS.Checked) {
                PlotSampledTS(irx);
                isAtLeastOnePlot = true;

            }
            else {
                _zPlotSamples.Hide();
            }

            if (checkBoxDopplerAScan.Checked) {
                PlotDopplerAscan(irx);
                isAtLeastOnePlot = true;
            }
            else {
                _zPlotAScan.Hide();
            }

            if (checkBoxDopplerSpec.Checked) {
                PlotDopplerSpectrum(irx, iht);
                isAtLeastOnePlot = true;
            }
            else {
                _zPlotSpec.Hide();
            }

            if (par.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCwSA ||
                par.SystemPar.RadarPar.ProcPar.DoAutoCorr1Rx) {
                if (checkBoxCrossCorr.Checked) {
                    PlotCrossCorr(irx, iht);
                    isAtLeastOnePlot = true;
                }
                else {
                    _zPlotXCorr.Hide();
                    _zPlotXCorrRatio.Hide();
                    _zPlotXCorrProfile.Hide();
                }
            }
            else {
                //checkBoxCrossCorr.Checked = false;
                //checkBoxCrossCorr.Enabled = false;
            }

            if (checkBoxDopplerTS.Checked) {
                PlotDopplerTS(irx, iht);
                isAtLeastOnePlot = true;
            }
            else {
                _zPlotTS.Hide();
            }

            if (checkBoxClutterWavelet.Checked) {
                PlotClutterWaveletTransform(irx, iht);
                isAtLeastOnePlot = true;
            }
            else {
                _zPlotCltrWvlt.Hide();
            }

            if (checkBoxDoppler.Checked) {
                PlotMeanDoppler(irx);
                isAtLeastOnePlot = true;
            }
            else {
                _zPlotDoppler.Hide();
            }

            if (checkBoxMoments.Checked) {
                PlotMoments(irx, npts);
                isAtLeastOnePlot = true;
            }
            else {
                _zPlotMoments.Hide();
            }

            /*
            if ((_param.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCwSA) && isAtLeastOnePlot) {
                //MessageBox.Show("Parameters used for this Dwell:\n", "Parameters");
                MessageBoxEx.ShowAsync("Parameters used for this Dwell:\n  IPP = " + _param.SystemPar.RadarPar.BeamParSet[0].IppMicroSec.ToString("F3") + "\n" +
                    " NHts = " + _param.SystemPar.RadarPar.BeamParSet[0].NHts.ToString() + "\n" +
                    " NPts = " + _param.SystemPar.RadarPar.BeamParSet[0].NPts.ToString() + "\n" +
                    " NSpec = " + _param.SystemPar.RadarPar.BeamParSet[0].NSpec.ToString(),
                    "Parameters", 4000);
            }
             * */
        }

        private void DisplayPowerMeter() {

            double power, temp, offset, freq;
            string units;

            _communicator.RequestPowerMeter(out power, out temp, out offset, out units, out freq);
            if (_powerMeterDisplay.IsDisposed) {
                _powerMeterDisplay = null;
                _powerMeterDisplay = new PowerMeterDisplay();
                Point location = Properties.Settings.Default.PowerMeterLocation;
                _powerMeterDisplay.Location = location;
            }
            _powerMeterDisplay.DisplayReadings(power, temp, offset, units, freq);
            _powerMeterDisplay.Show();
        }

        //
        //  Plot routines data transfer method:
        //  Plot Moments:       args
        //  Plot MeanDoppler:   args
        //  Plot DopplerTS:     MMF
        //  Plot Doppler Spec:  MMF
        //  Plot DopplerAScan:  MMF
        //  Plot XCorr:         args
        //  Plot SampledTS:     MMF

        private void PlotMoments(int irx, int npts) {
            double[] doppler;
            double[] noise;
            double[] power;
            int nhts;

            int maxPts = 8 * 1024 + 1;
            int doubleSize = Marshal.SizeOf(typeof(double));
            if (_MomentsPlotMmf == null) {
                // create a permanent memory mapped file of large size
                try {
                    int totalSize = 3 * doubleSize * maxPts + sizeof(Int32);
                    _MomentsPlotMmf = MemoryMappedFile.CreateNew("Global\\PopNMomPlotMMF", totalSize, MemoryMappedFileAccess.ReadWrite);
                    _MomentsPlotView = _MomentsPlotMmf.CreateViewAccessor();
                }
                catch (Exception ee) {
                    MessageBoxEx.ShowAsync("Can't create MMF for Moments Plot: " + ee.Message, 3000);
                    return;
                }
            }

            _communicator.RequestMoments(irx, out noise, out power, out doppler, out nhts);

            if (noise != null &&
               power != null &&
               doppler != null) {

                if (nhts == 0) {
                    nhts = doppler.Length;
                }
                double[] xsnr = new double[nhts];
                double[] xn = new double[nhts];
                double[] xp = new double[nhts];
                double[] ysnr = new double[nhts];
                // POPREV 4.12 modified noise and signal plot to show noise power not noise level
                for (int i = 0; i < nhts; i++) {
                    ysnr[i] = i;
                    xn[i] = 10.0 * Math.Log10(noise[i]*npts);
                    xp[i] = 10.0 * Math.Log10(power[i]);
                    xsnr[i] = xp[i] - xn[i];
                }
                _zPlotMoments.ClearPlot();
                ZedGraph.LineItem curve3 = _zPlotMoments.AddCurve("SNR " + "Rx #" + (irx + 1).ToString(),
                                                                    xsnr, ysnr, Color.Blue, ZedGraph.SymbolType.None);
                ZedGraph.LineItem curve = _zPlotMoments.AddCurve("Noise " + "Rx #" + (irx + 1).ToString(),
                                                                    xn, ysnr, Color.Red, ZedGraph.SymbolType.None);
                ZedGraph.LineItem curve2 = _zPlotMoments.AddCurve("Rel Power " + "Rx #" + (irx + 1).ToString(),
                                                                    xp, ysnr, Color.Green, ZedGraph.SymbolType.None);
                curve3.Line.Width = 2.0f;
                _zPlotMoments.GraphControl.GraphPane.XAxis.Title.Text = "dB";
                _zPlotMoments.GraphControl.GraphPane.YAxis.Title.Text = "Ht #";
                _zPlotMoments.SetWindowTitle("SNR Profile");
                _zPlotMoments.Display();
            }
        }

        private void PlotMeanDoppler(int irx) {
            double[] doppler;
            double[] noise;
            double[] power;
            int nhts;
            _communicator.RequestMoments(irx, out noise, out power, out doppler, out nhts);

            if (doppler != null) {

                if (nhts == 0) {
                    nhts = doppler.Length;
                }
                double[] x = new double[nhts];
                double[] y = new double[nhts];
                for (int i = 0; i < nhts; i++) {
                    x[i] = i;
                    y[i] = doppler[i];
                }

                /*
                Point location;
                Size size;
                _zPlotDoppler.getPosition(out location, out size);
                if (location.IsEmpty) {
                    location = Properties.Settings.Default.DopplerProfLocation;
                    _zPlotDoppler.setPosition(location, size);
                }
                 * */
                _zPlotDoppler.ClearPlot();
                ZedGraph.LineItem curve = _zPlotDoppler.AddCurve("Mean Doppler " + "Rx #" + (irx + 1).ToString(),
                                                                    y, x, Color.Blue, ZedGraph.SymbolType.Circle);
                _zPlotDoppler.GraphControl.GraphPane.YAxis.Title.Text = "Ht #";
                _zPlotDoppler.GraphControl.GraphPane.XAxis.Title.Text = "Nyquist";
                curve.Line.IsVisible = false;
                _zPlotDoppler.GraphControl.GraphPane.XAxis.Scale.Max = 1.0;
                _zPlotDoppler.GraphControl.GraphPane.XAxis.Scale.Min = -1.0;
                _zPlotDoppler.SetWindowTitle("Doppler Profile");
                _zPlotDoppler.Display();
            }
        }

        /// <summary>
        /// PlotClutterWaveletTransform -- plot Daubechies wavelet transform for clutter removal at given ht.
        /// </summary>
        /// <param name="irx"></param>
        /// <param name="iht"></param>
        private void PlotClutterWaveletTransform(int irx, int iht) {
            double[] wavelet1, wavelet2;
            int npts;
            int filteredPts;
            int nCurves;
            double clipThld;

            int maxPts = 16*1024 + 1;
            if (_CltrWvltPlotMmf == null) {
                // create a permanent memory mapped file of large size
                  try {
                      int totalSize = sizeof(double) * maxPts + 2 * sizeof(Int32) + sizeof(double);
                    _CltrWvltPlotMmf = MemoryMappedFile.CreateNew("Global\\PopNCltrWvltPlotMMF", totalSize, MemoryMappedFileAccess.ReadWrite);
                    _CltrWvltPlotView = _CltrWvltPlotMmf.CreateViewAccessor();
                }
                catch (Exception ee) {
                    MessageBoxEx.ShowAsync("Can't create MMF for Clutter Wavelet Plot: " + ee.Message, 3000);
                    return;
                }
            }

            try {
                _communicator.RequestCltrWvlt(irx, iht);
                _CltrWvltPlotView.Read<Int32>(0, out npts);
                _CltrWvltPlotView.Read<Int32>(4, out filteredPts);
                _CltrWvltPlotView.Read<Int32>(8, out nCurves);
                if (npts > maxPts) {
                    npts = maxPts;
                }
                if (npts == 0) {
                    return;
                }
                wavelet1 = new double[npts];
                _CltrWvltPlotView.ReadArray<double>(12, wavelet1, 0, npts);
                wavelet2 = new double[1];  // to fool stupid compiler
                if (nCurves > 1) {
                    wavelet2 = null;
                    wavelet2 = new double[npts];
                    int offset = npts * sizeof(double);
                    _CltrWvltPlotView.ReadArray<double>(12 + offset, wavelet2, 0, npts);
                }
            }
            catch (Exception e) {
                MessageBoxEx.ShowAsync("PlotCltrWvlt: " + e.Message, 3000);
                return;
            }

            double[] x = new double[npts];
            //double[] ya = new double[npts];

            for (int i = 0; i < npts; i++) {
                x[i] = i;
                //ya[i] = wavelet[i];
            }

            _zPlotCltrWvlt.ClearPlot();
            string label = "Clipped Clutter Wavelet (" + filteredPts.ToString() + ") Rx #" + (irx + 1).ToString() + " Ht #" + (iht + 1).ToString();
            ZedGraph.LineItem curve = _zPlotCltrWvlt.AddCurve(label, x, wavelet2, Color.Red, ZedGraph.SymbolType.Circle);
            if (nCurves > 1) {
                _zPlotCltrWvlt.AddCurve("", x, wavelet1, Color.Blue, ZedGraph.SymbolType.XCross);
            }
            _zPlotCltrWvlt.SetWindowTitle("Clutter Wavelet Transform");
            _zPlotCltrWvlt.Display();
        }

        /// <summary>
        /// PlotDopplerTS -- plot Doppler time series at given ht.
        /// </summary>
        /// <param name="irx"></param>
        /// <param name="iht"></param>
        private void PlotDopplerTS(int irx, int iht) {
            Ipp64fc[] dopTS;
            int npts;
            double stdDev;

            int maxPts = 32 * 1024 + 1;
            int complexSize = Marshal.SizeOf(typeof(Ipp64fc));
            if (_DoppTSPlotMmf == null) {
                // create a permanent memory mapped file of large size
                try {
                    int totalSize = complexSize * maxPts + sizeof(Int32);
                    _DoppTSPlotMmf = MemoryMappedFile.CreateNew("Global\\PopNDoppTSPlotMMF", totalSize, MemoryMappedFileAccess.ReadWrite);
                    _DoppTSPlotView = _DoppTSPlotMmf.CreateViewAccessor();
                }
                catch (Exception ee) {
                    MessageBoxEx.ShowAsync("Can't create MMF for DoppTS Plot: " + ee.Message, 3000);
                    return;
                }
            }


            try {
               _communicator.RequestDopplerTS(irx, iht, out dopTS, out npts);
                // returned dopTS is null as of 3.18
                _DoppTSPlotView.Read<Int32>(0, out npts);
                if (npts < 1) {
                    return;
                }
                if (npts > maxPts) {
                    npts = maxPts;
                }
                _DoppTSPlotView.Read<Double>(4, out stdDev);
                dopTS = new Ipp64fc[npts];
                _DoppTSPlotView.ReadArray<Ipp64fc>(12, dopTS, 0, npts);
            }
            catch (Exception e) {
                MessageBoxEx.ShowAsync("PlotDopplerTS: " + e.Message, 3000);
                return;
            }


            if (dopTS != null) {
                if (npts == 0) {
                    npts = dopTS.Length;
                }
                double[] x = new double[npts];
                double[] ya = new double[npts];
                double[] yb = new double[npts];

                for (int i = 0; i < npts; i++) {
                    x[i] = i;
                    ya[i] = dopTS[i].re;
                    yb[i] = dopTS[i].im;
                }
                _zPlotTS.ClearPlot();
                ZedGraph.LineItem curve = _zPlotTS.AddCurve("Doppler TS " + "Rx #" + (irx + 1).ToString() + ", Ht #" + (iht + 1).ToString() + ", StdDev = " + stdDev.ToString("G3"),
                                                                    x, ya, Color.Blue, ZedGraph.SymbolType.None);
                curve = _zPlotTS.AddCurve("", x, yb, Color.Red, ZedGraph.SymbolType.None);
                _zPlotTS.SetWindowTitle("Doppler TS");
                _zPlotTS.Display();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="irx"></param>
        /// <param name="iht"></param>
        private void PlotDopplerSpectrum(int irx, int iht) {
            double[] spectrum;
            int npts;
            double totPow;

            //MessageBox.Show("in PlotDopplerSpectrum");

            int maxPts = 32 * 1024 + 1;
            int doubleSize = Marshal.SizeOf(typeof(double));
            if (_SpecPlotMmf == null) {
                // create a permanent memory mapped file of large size
                try {
                    int totalSize = doubleSize * maxPts + sizeof(Int32);
                    _SpecPlotMmf = MemoryMappedFile.CreateNew("Global\\PopNSpecPlotMMF", totalSize, MemoryMappedFileAccess.ReadWrite);
                    _SpecPlotView = _SpecPlotMmf.CreateViewAccessor();
                }
                catch (Exception ee) {
                    MessageBoxEx.ShowAsync("Can't create MMF for Spectral Plot: " + ee.Message, 3000);
                    return;
                }
            }
            else {
                //MessageBox.Show(" MMF not null");
            }

            //MessageBox.Show("RequestSpectrum call;  irx = " + irx.ToString());
            bool isOK = _communicator.RequestSpectrum(irx, iht, out spectrum, out npts);
            //MessageBox.Show("RequestSpectrum. ok = " + isOK.ToString());
            _SpecPlotView.Read<Int32>(0, out npts);
            if (npts > maxPts) {
                npts = maxPts;
            }
            spectrum = new double[npts];
            _SpecPlotView.ReadArray<double>(4, spectrum, 0, npts);

            totPow = 0.0;
            if (spectrum != null) {
                if (npts == 0) {
                    npts = spectrum.Length;
                }
                double[] x = new double[npts];
                double[] x2 = new double[npts];
                double[] y = new double[npts];
                for (int i = 0; i < npts; i++) {
                    x[i] = i - npts/2;
                    x2[i] = x[i] / (npts / 2);
                    y[i] = spectrum[i];
                    totPow += y[i];
                }
                _zPlotSpec.ClearPlot();
                ZedGraph.LineItem curve;
                //curve = _zPlotSpec.AddCurve("oops", x, y, Color.LimeGreen, ZedGraph.SymbolType.None);
                //curve.IsX2Axis = true;
                curve = _zPlotSpec.AddCurve("Spectrum " + "Rx #" + (irx + 1).ToString() + ", Ht #" + (iht + 1).ToString() + ",  Pow = " + totPow.ToString("G3"),
                                                                    x2, y, Color.Blue, ZedGraph.SymbolType.None);
              
                _zPlotSpec.GraphControl.GraphPane.X2Axis.IsVisible = true;
                _zPlotSpec.GraphControl.GraphPane.X2Axis.MajorTic.IsOpposite = false;
                _zPlotSpec.GraphControl.GraphPane.X2Axis.MinorTic.IsOpposite = false;
                _zPlotSpec.GraphControl.GraphPane.XAxis.MajorTic.IsOpposite = false;
                _zPlotSpec.GraphControl.GraphPane.XAxis.MinorTic.IsOpposite = false;
                _zPlotSpec.GraphControl.GraphPane.XAxis.Scale.Max = 1.0;
                _zPlotSpec.GraphControl.GraphPane.XAxis.Scale.Min = -1.0;
                _zPlotSpec.GraphControl.GraphPane.X2Axis.Scale.Max = npts/2;
                _zPlotSpec.GraphControl.GraphPane.X2Axis.Scale.Min = -npts / 2;
                _zPlotSpec.GraphControl.GraphPane.XAxis.MajorGrid.IsVisible = true;
                _zPlotSpec.GraphControl.GraphPane.XAxis.Title.Text = "Nyquist";
                _zPlotSpec.GraphControl.GraphPane.X2Axis.Title.Text = "Points";
                //_zPlotSpec.GraphControl.GraphPane.XAxis.Scale.Align = ZedGraph.AlignP.Inside;
                _zPlotSpec.GraphControl.GraphPane.X2Axis.Title.FontSpec.IsBold = false;
                _zPlotSpec.GraphControl.GraphPane.Title.Text = "Doppler Spectrum";
                _zPlotSpec.SetWindowTitle("Doppler Spectrum");
                _zPlotSpec.Display();
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="irx"></param>
        /// <param name="iht"></param>
        private void PlotCrossCorr(int irx, int iht) {

            double[] crossCorrMag;
            double[] autoCorrMag;
            double[] crossCorrGaussian;
            int npts;
            double[] slope0;
            double oneLag = _param.SystemPar.RadarPar.BeamParSet[0].IppMicroSec * 1.0e-6;
            int polyOrder;
            double[] polyCoeffsX;
            double[] polyCoeffsA;
            POPCommunicator.PopCrossCorrArgs xcorrArgs;
            double taui, taup, taux;
            int polyFitPts, xCorrPeakI;
            double autoBase, crossBase;
            double antDeltaX;

            _communicator.RequestCrossCorr2(irx, iht, out xcorrArgs);
            autoCorrMag = xcorrArgs.AutoCorr;
            npts = xcorrArgs.NLags;
            if (xcorrArgs.CrossCorr != null) {
                crossCorrMag = xcorrArgs.CrossCorr;
                crossCorrGaussian = xcorrArgs.GaussCoeffs;
                slope0 = xcorrArgs.SlopeAtZero;
                polyOrder = xcorrArgs.PolyFitOrder;
                polyCoeffsX = xcorrArgs.PolyFitCoeffsX;
                polyCoeffsA = xcorrArgs.PolyFitCoeffsA;
                taui = xcorrArgs.FcaLags[0];
                taup = xcorrArgs.FcaLags[1];
                taux = xcorrArgs.FcaLags[2];
                polyFitPts = xcorrArgs.AutoPolyFitPts;
                xCorrPeakI = xcorrArgs.XCorrPeakI;
                autoBase = xcorrArgs.AutoBaseline;
                crossBase = xcorrArgs.CrossBaseline;
                antDeltaX = xcorrArgs.AntennaDeltaX;
            }
            else {
                // assign these to something so compiler won't complain
                //   but we aren't using any of these if no xcorrs
                crossCorrMag = null;
                crossCorrGaussian = null;
                slope0 = null;
                polyCoeffsX = null;
                polyCoeffsA = null;
                polyOrder = 3;
                polyFitPts = 10;
                xCorrPeakI = 0;
                autoBase = 0;
                crossBase = 0;
                antDeltaX = 0; ;
                taui = 0;
                taup = 0;
                taux = 0;
                if (autoCorrMag != null) {
                    // plot auto-correlation only
                    double[] x = new double[npts];
                    double[] y = new double[npts];
                    for (int i = 0; i < npts; i++) {
                        x[i] = (i - npts / 2) * oneLag * 1000.0;  // lag time in msec
                    }
                    _zPlotXCorr.ClearPlot();
                    _zPlotXCorr.SetWindowTitle("AutoCorrelation");
                    _zPlotXCorr.GraphControl.GraphPane.XAxis.Title.Text = "Lag (msec)";
                    _zPlotXCorr.GraphControl.GraphPane.XAxis.Scale.Max = (npts / 2) * oneLag * 1000.0;  // time scale in msec
                    _zPlotXCorr.GraphControl.GraphPane.XAxis.Scale.Min = -(npts / 2) * oneLag * 1000.0;
                    _zPlotXCorr.GraphControl.GraphPane.YAxis.Scale.Min = 0.0;
                    for (int i = 0; i < npts; i++) {
                        y[i] = autoCorrMag[i];
                    }
                    ZedGraph.LineItem curveAuto = _zPlotXCorr.AddCurve("|AutoCorrelation| " + " Ht #" + (iht + 1).ToString(),
                                                                        x, y, Color.Blue, ZedGraph.SymbolType.None);
                    _zPlotXCorr.Display();
                    return;
                }
            }

            //_communicator.RequestCrossCorr(irx, iht, out crossCorrMag, out autoCorrMag, out crossCorrGaussian, out slope0, out npts,
            //                                    out polyOrder, out polyCoeffsX, out polyCoeffsA);

            if (crossCorrMag != null) {
                if (npts == 0) {
                    npts = crossCorrMag.Length;
                }
                double[] x = new double[npts];
                double[] x2 = new double[npts];
                double[] y = new double[npts];
                double[] g = new double[npts];
                double[] px = new double[npts];
                double[] pa = new double[npts];

                for (int i = 0; i < npts; i++) {
                    x[i] = (i - npts/2) * oneLag * 1000.0;  // lag time in msec
                    x2[i] = (i - npts/2);
                    y[i] = crossCorrMag[i];
                    double arg = (x[i]/1000.0 - crossCorrGaussian[1]) / crossCorrGaussian[2];
                    double ex = Math.Exp(-arg * arg);
                    g[i] = crossCorrGaussian[0] * ex + crossCorrGaussian[3];
                    px[i] = polyCoeffsX[0];
                    for (int ic = 1; ic < polyOrder + 1; ic++) {
                        px[i] += polyCoeffsX[ic] * Math.Pow(x[i]/1000.0, ic);
                    }
                    pa[i] = polyCoeffsA[0];
                    for (int ic = 1; ic < polyOrder + 1; ic++) {
                        pa[i] += polyCoeffsA[ic] * Math.Pow(x[i]/1000.0, ic);
                    }
                }
                for (int i = 0; i < npts; i++) {
                    if ((i < npts / 2 - polyFitPts / 2) || (i > npts / 2 + polyFitPts / 2)) {
                        pa[i] = autoBase;
                    }
                }
                for (int i = 0; i < npts; i++) {
                    if ((i < xCorrPeakI - polyFitPts / 2) || (i > xCorrPeakI + polyFitPts / 2)) {
                        px[i] = crossBase;
                    }
                }
                _zPlotXCorr.ClearPlot();

                // plot taui
                double[] x4 = new double[2];
                double[] y4 = new double[2];
                x4[0] = x4[1] = taui * 1000.0;
                y4[0] = 0.0;
                y4[1] = 1.0;
                // taup
                double[] x5 = new double[2];
                x5[0] = x5[1] = taup * 1000.0;
                // taux
                double[] x6 = new double[2];
                x6[0] = x6[1] = taux * 1000.0;
                double[] x7 = new double[2];
                x7[0] = x7[1] = 0.0;

                // computed velocities
                double veli = antDeltaX / (4.0 * taui);
                double velpx = antDeltaX * taup / (2 * taux * taux);
               
                // plot slope line fit:
                double[] x3 = new double[2];
                double[] y3 = new double[2];
                x3[0] = x[npts / 4];
                x3[1] = x[(3 * npts) / 4];
                double M = slope0[0];
                double B = 0.0;
                double arg1 = (x[npts/2]/1000.0 - crossCorrGaussian[1]) / crossCorrGaussian[2];
                double ex1 = Math.Exp(-arg1 * arg1);
                B = crossCorrGaussian[0] * ex1 + crossCorrGaussian[3];

                double dd = 0.5 * pa[npts / 2];
                if (dd > 0.15) {
                    dd = 0.15;
                }
                y4[1] = pa[npts / 2] + dd;
                
                int reps = 0;
                // compute y-values of slope line end points.
                // prevent steep slope line from having huge end point
                do {
                    reps++;
                    y3[0] = B + (x3[0] / 1000.0) * M;
                    y3[1] = B + (x3[1] / 1000.0) * M;
                    if (y3[0] > y4[1]) {
                        //x3[0] = x3[0] - (x3[0] - x3[1]) / 3.0;
                        //y3[0] = B + (x3[0] / 1000.0) * M;
                        y3[0] = crossCorrGaussian[0] + 0.1;
                        x3[0] = 1000.0 * (y3[0] - B) / M;
                    }
                    if (y3[1] > y4[1]) {
                        //x3[1] = x3[1] - (x3[1] - x3[0]) / 3.0;
                        //y3[1] = B + (x3[1] / 1000.0) * M;
                        y3[1] = crossCorrGaussian[0];
                        x3[1] = 1000.0 * (y3[1] - B) / M;
                    }
                } while ((y3[0] > y4[1] || y3[1] > y4[1]) && reps < 2);


                ZedGraph.LineItem curvepx = _zPlotXCorr.AddCurve("Poly Fit (" + polyOrder.ToString() + ")", x, px, Color.Blue, ZedGraph.SymbolType.None);
                curvepx.Line.Width = 2;

                ZedGraph.LineItem curvepa = _zPlotXCorr.AddCurve("", x, pa, Color.Green, ZedGraph.SymbolType.None);
                curvepa.Line.Width = 2;

                ZedGraph.LineItem curve3 = _zPlotXCorr.AddCurve("Gaussian Fit ", x, g, Color.Pink, ZedGraph.SymbolType.None);
                curve3.Line.Width = 2;

                ZedGraph.LineItem curve2 = _zPlotXCorr.AddCurve("Slope: " + M.ToString("E3"), x3, y3, Color.Magenta, ZedGraph.SymbolType.None);

                ZedGraph.LineItem curveti = _zPlotXCorr.AddCurve("TauI: " + (taui * 1000).ToString("F2") + " ms  (" + veli.ToString("F2") + " m/s)",
                                            x4, y4, Color.Green, ZedGraph.SymbolType.None);
                curveti.Line.Style = System.Drawing.Drawing2D.DashStyle.Dash;
                curveti.Line.Width = 2;
                ZedGraph.LineItem curvetp = _zPlotXCorr.AddCurve("TauP: " + (taup * 1000).ToString("F2") + " ms",
                                            x5, y4, Color.Blue, ZedGraph.SymbolType.None);
                curvetp.Line.Style = System.Drawing.Drawing2D.DashStyle.Dash;
                curvetp.Line.Width = 2;
                ZedGraph.LineItem curvetx = _zPlotXCorr.AddCurve("TauX: " + (taux * 1000).ToString("F2") + " ms  (" + velpx.ToString("F2") + " m/s)",
                                            x6, y4, Color.Red, ZedGraph.SymbolType.None);
                curvetx.Line.Style = System.Drawing.Drawing2D.DashStyle.Dash;
                curvetx.Line.Width = 2;
                ZedGraph.LineItem curve0 = _zPlotXCorr.AddCurve("", x7, y4, Color.Silver, ZedGraph.SymbolType.None);

                string title = "CrossCorrelation (" + xcorrArgs.NPts.ToString() + " pts * " + xcorrArgs.NAvgs.ToString() + " avgs)";
                _zPlotXCorr.SetWindowTitle(title);

                _zPlotXCorr.GraphControl.GraphPane.X2Axis.IsVisible = true;
                _zPlotXCorr.GraphControl.GraphPane.X2Axis.MajorTic.IsOpposite = false;
                _zPlotXCorr.GraphControl.GraphPane.X2Axis.MinorTic.IsOpposite = false;
                _zPlotXCorr.GraphControl.GraphPane.XAxis.MajorTic.IsOpposite = false;
                _zPlotXCorr.GraphControl.GraphPane.XAxis.MinorTic.IsOpposite = false;
                _zPlotXCorr.GraphControl.GraphPane.X2Axis.Scale.Max = npts / 2;
                _zPlotXCorr.GraphControl.GraphPane.X2Axis.Scale.Min = -npts / 2;
                _zPlotXCorr.GraphControl.GraphPane.XAxis.MajorGrid.IsVisible = true;
                _zPlotXCorr.GraphControl.GraphPane.X2Axis.Title.Text = "Lag (pts)";
                //_zPlotXCorr.GraphControl.GraphPane.XAxis.Scale.Align = ZedGraph.AlignP.Inside;
                _zPlotXCorr.GraphControl.GraphPane.X2Axis.Title.FontSpec.IsBold = false;
                _zPlotXCorr.GraphControl.GraphPane.X2Axis.Title.FontSpec.Size = 10.0F;

                _zPlotXCorr.GraphControl.GraphPane.XAxis.Title.Text = "Lag (msec)";
                _zPlotXCorr.GraphControl.GraphPane.XAxis.Scale.Max = (npts / 2) * oneLag * 1000.0;  // time scale in msec
                _zPlotXCorr.GraphControl.GraphPane.XAxis.Scale.Min = -(npts / 2) * oneLag * 1000.0;
                _zPlotXCorr.GraphControl.GraphPane.YAxis.Scale.Min = 0.0;

                ZedGraph.LineItem curve = _zPlotXCorr.AddCurve("|CrossCorrelation| " + XCorrLabel[irx] + " Ht #" + (iht + 1).ToString(),
                                                    x, y, Color.SkyBlue, ZedGraph.SymbolType.None);
                double[] y2;
                if (autoCorrMag != null) {
                    y2 = new double[npts];
                    for (int i = 0; i < npts; i++) {
                        y2[i] = autoCorrMag[i];
                    }
                    //y2[npts/2] = (y2[npts/2 + 1] + y2[npts/2 - 1])/2.0;
                    ZedGraph.LineItem curve4 = _zPlotXCorr.AddCurve("|AutoCorrelation| " + "Rx #" + (irx + 1).ToString(),
                                                                        x, y2, Color.Lime, ZedGraph.SymbolType.None);
                }


                _zPlotXCorr.Display();
            }

            // also plot xcorr ratio
            double[] crossCorrRatio;
            LineFit line;
            _communicator.RequestCrossCorrRatio(irx, iht, out crossCorrRatio, out npts, out line);
            if (crossCorrRatio != null) {
                if (npts == 0) {
                    npts = crossCorrRatio.Length;
                }
                double[] x1 = new double[npts];
                double[] y1 = new double[npts];
                for (int i = 0; i < npts; i++) {
                    x1[i] = (i - npts/2) * oneLag;
                    y1[i] = crossCorrRatio[i];
                }
                _zPlotXCorrRatio.ClearPlot();
                ZedGraph.LineItem curve1 = _zPlotXCorrRatio.AddCurve("CrossCorrelation Ratio " + XCorrLabel[irx] + " Ht #" + (iht + 1).ToString(),
                                                                    x1, y1, Color.Blue, ZedGraph.SymbolType.None);

                // plot line fit:
                double[] x2 = new double[2];
                double[] y2 = new double[2];
                x2[0] = x1[npts/4];
                x2[1] = x1[(3*npts)/4];
                // the fit line y = mx+b is relative to x=0 being the first pt.
                //  correction for x=0 in middle:
                //y2[0] = line.B + (x2[0] + oneLag*npts/2) * line.M;
                //y2[1] = line.B + (x2[1] + oneLag*npts/2) * line.M;
                // but we know the ratio line by definition goes through y=0 at x=0
                // so equivalently, with b = 0:
                y2[0] = x2[0] * line.M;
                y2[1] = x2[1] * line.M;
                ZedGraph.LineItem curve2 = _zPlotXCorrRatio.AddCurve("Slope: " + line.M.ToString("E3"), x2, y2, Color.Red, ZedGraph.SymbolType.None);

                _zPlotXCorrRatio.GraphControl.GraphPane.XAxis.Scale.Max = (npts / 2) * oneLag;
                _zPlotXCorrRatio.GraphControl.GraphPane.XAxis.Scale.Min = -(npts / 2) * oneLag;
                _zPlotXCorrRatio.SetWindowTitle("CrossCorrelation Ratio");
                _zPlotXCorrRatio.Display();

            }  //

            // plot profile of slopes
            double[] xCorrRatioSlopes;
            double[] xCorrSZLSlopes;
            int nhts;
            _communicator.RequestCrossCorrProfile(irx, out xCorrSZLSlopes, out xCorrRatioSlopes, out nhts);

            if (xCorrSZLSlopes != null) {

                int gateFirst, gateLast;

                if (nhts == 0) {
                    nhts = xCorrSZLSlopes.Length;
                }

                if (_param.SystemPar.RadarPar.FmCwParSet[0].SelectGatesToKeep) {
                    gateFirst = _param.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateFirst;
                    gateLast = _param.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateLast;
                }
                else {
                    gateFirst = 0;
                    gateLast = nhts-1;
                }

                nhts = gateLast - gateFirst + 1;

                double[] x3 = new double[nhts];
                double[] y3 = new double[nhts];
                double[] y4 = new double[nhts];
                for (int i = 0; i < nhts; i++) {
                    x3[i] = gateFirst+i;
                    y3[i] = xCorrSZLSlopes[gateFirst + i];
                }

                if (xCorrRatioSlopes != null) {
                    for (int i = 0; i < nhts; i++) {
                        y4[i] = xCorrRatioSlopes[gateFirst + i];
                    }
                }

                _zPlotXCorrProfile.ClearPlot();
                ZedGraph.LineItem curve = _zPlotXCorrProfile.AddCurve("XCorr Slope at 0 Lag " + XCorrLabel[irx],
                                                                    y3, x3, Color.Blue, ZedGraph.SymbolType.Circle);
                _zPlotXCorrProfile.GraphControl.GraphPane.YAxis.Title.Text = "Ht #";
                //_zPlotXCorrProfile.GraphControl.GraphPane.XAxis.Title.Text = "Nyquist";
                curve.Line.IsVisible = false;
                _zPlotXCorrProfile.SetWindowTitle("XCorr Slopes Profile");

                if (xCorrRatioSlopes != null) {
                    ZedGraph.LineItem curve2 = _zPlotXCorrProfile.AddCurve("XCorr Ratio Slope " + XCorrLabel[irx],
                                                                        y4, x3, Color.Green, ZedGraph.SymbolType.XCross);
                    curve2.Line.IsVisible = false;
                }
                _zPlotXCorrProfile.Display();
            }
            else {
                _zPlotXCorrProfile.Hide();
            }
        }

        private void PlotDopplerAscan(int irx) {
            //
            // POPREV: modified PlotAscan to use MMF, 3.18
            //
            Ipp64fc[] ascan;
            Ipp64fc[] ascan2;
            int nGates;
            //

            int maxGates = 16 * 1024 + 1;
            int complexSize = Marshal.SizeOf(typeof(Ipp64fc));
            if (_AscanPlotMmf == null) {
                // create a permanent memory mapped file of large size
                try {
                    int totalSize = complexSize * maxGates + sizeof(Int32);
                    _AscanPlotMmf = MemoryMappedFile.CreateNew("Global\\PopNAScanPlotMMF", totalSize, MemoryMappedFileAccess.ReadWrite);
                    _AscanPlotView = _AscanPlotMmf.CreateViewAccessor();
                }
                catch (Exception ee) {
                    MessageBoxEx.ShowAsync("Can't create MMF for Ascan Plot: " + ee.Message, 3000);
                    return;
                }
            }

            /**/
            try {
                //Console.Beep(440, 100);
                _communicator.RequestDopplerAScan(irx, 0, out ascan, out nGates);
                // returned ascan is null as of 3.18
                _AscanPlotView.Read<Int32>(0, out nGates);
                if (nGates < 1) {
                    return;
                }
                if (nGates > maxGates) {
                    nGates = maxGates;
                }
                ascan = new Ipp64fc[nGates];
                _AscanPlotView.ReadArray<Ipp64fc>(4, ascan, 0, nGates);
                /*
                using (MemoryMappedFile mmf1 = MemoryMappedFile.OpenExisting("Global\\PopNAScanPlotMMF2", MemoryMappedFileRights.FullControl)) {
                    using (MemoryMappedViewAccessor view = mmf1.CreateViewAccessor()) {
                        view.Read<Int32>(0, out nGates);
                        ascan = new Ipp64fc[nGates];
                        view.ReadArray<Ipp64fc>(4, ascan, 0, nGates);
                    }
                }
                 * */
                int nGates2;
                _communicator.RequestDopplerAScan(irx, 1, out ascan2, out nGates2);
                _AscanPlotView.Read<Int32>(0, out nGates2);
                if (nGates2 > maxGates) {
                    nGates2 = maxGates;
                }
                if (nGates != nGates2) {
                    MessageBoxEx.ShowAsync("Ascan Plot: nGates != nGates2", 3000);
                }
                ascan2 = new Ipp64fc[nGates2];
                _AscanPlotView.ReadArray<Ipp64fc>(4, ascan2, 0, nGates2);
                //Thread.Sleep(500);
                /*
                using (MemoryMappedFile mmf1 = MemoryMappedFile.OpenExisting("Global\\PopNAScanPlotMMF2", MemoryMappedFileRights.FullControl)) {
                    using (MemoryMappedViewAccessor view = mmf1.CreateViewAccessor(0, complexSize * nGates)) {
                        view.Read<Int32>(0, out nGates);
                        ascan2 = new Ipp64fc[nGates];
                        view.ReadArray<Ipp64fc>(4, ascan2, 0, nGates);
                    }
                }
                */
                //Console.Beep(440, 100);
            }
            catch (Exception e) {
                MessageBoxEx.ShowAsync("Ascan Plot: " + e.Message, 3000);
                return;
            }
             /* */
            /* */
            //
            if (ascan != null && ascan2 != null) {

                if (nGates == 0) {
                    nGates = ascan.Length;
                }

                double[] x = new double[nGates];
                double[] y = new double[nGates];
                double[] ya1 = new double[nGates];
                double[] ya2 = new double[nGates];
                double[] ya3 = new double[nGates];
                double[] ya4 = new double[nGates];
                double[] yb1 = new double[nGates];
                double[] yb2 = new double[nGates];
                double[] yb3 = new double[nGates];
                double[] yb4 = new double[nGates];
                for (int i = 0; i < nGates; i++) {
                    x[i] = i;
                }
                //_communicator.RequestDopplerAScan(irx, 0, out ascan);
                for (int j = 0; j < nGates; j++) {
                    ya1[j] = ascan[j].re;
                    yb1[j] = ascan[j].im;
                }

                //_communicator.RequestDopplerAScan(irx, 1, out ascan2);
                for (int j = 0; j < nGates; j++) {
                    ya2[j] = ascan2[j].re;
                    yb2[j] = ascan2[j].im;
                }

                _zPlotAScan.ClearPlot();

                ZedGraph.LineItem curve = _zPlotAScan.AddCurve("Doppler AScan " + "Rx #" + (irx + 1).ToString() + " Re  ",
                                                                    x, ya1, Color.Blue, ZedGraph.SymbolType.None);
                curve = _zPlotAScan.AddCurve("Im", x, yb1, Color.Red, ZedGraph.SymbolType.None);
                ZedGraph.LineItem curve2 = _zPlotAScan.AddCurve("", x, ya2, Color.Blue, ZedGraph.SymbolType.None);
                curve2 = _zPlotAScan.AddCurve("", x, yb2, Color.Red, ZedGraph.SymbolType.None);
                _zPlotAScan.SetWindowTitle("Doppler AScan");
                _zPlotAScan.Display();
            }
        }

        private void PlotSampledTS(int irx) {
            double[][] sampTS;
            int nsamples;
            int nGates;
            int nIPP;
            double stdDev;

            int maxSamples = 32 * 1024 + 1;
            if (_RawSamplesPlotMmf == null) {
                // create a permanent memory mapped file of large size
                try {
                    int totalSize = sizeof(Double) * maxSamples + 2*sizeof(Int32);
                    _RawSamplesPlotMmf = MemoryMappedFile.CreateNew("Global\\PopNRawSamplesPlotMMF", totalSize, MemoryMappedFileAccess.ReadWrite);
                    _RawSamplesPlotView = _RawSamplesPlotMmf.CreateViewAccessor();
                }
                catch (Exception ee) {
                    MessageBoxEx.ShowAsync("Can't create MMF for Raw Samples Plot: " + ee.Message, 3000);
                    return;
                }
            }


            try {
                double[] dummy;
                _communicator.RequestSampledTS(irx, out dummy, out nsamples);
                // returned sampTS(dummy) is null as of 4.0

                _RawSamplesPlotView.Read<Int32>(0, out nIPP);
                _RawSamplesPlotView.Read<Int32>(4, out nGates);
                _RawSamplesPlotView.Read<Double>(8, out stdDev);
                if (nIPP < 1 || nGates < 1) {
                    return;
                }
                nsamples = nIPP * nGates;
                if (nsamples > maxSamples) {
                    // if too many samples, try samples for just 1 IPP
                    nGates = nGates / nIPP;
                    nsamples = nGates;
                    nIPP = 1;
                    if (nsamples > maxSamples) {
                        nsamples = nGates = maxSamples;
                    }
                }
                sampTS = new double[nIPP][];
                for (int i = 0; i < nIPP; i++) {
                    sampTS[i] = new double[nGates];
                    int offset = 16 + i * nGates * sizeof(double);
                    _RawSamplesPlotView.ReadArray<double>(offset, sampTS[i], 0, nGates);
                }

            }
            catch (Exception e) {
                MessageBoxEx.ShowAsync("Raw Samples Plot: " + e.Message, 3000);
                return;
            }

            if (sampTS != null) {

                if (nGates == 0) {
                    nGates = sampTS[0].Length;
                }

                double[] x = new double[nGates];
                for (int i = 0; i < nGates; i++) {
                    x[i] = i;
                }

                _zPlotSamples.ClearPlot();


                for (int ipp = 0; ipp < nIPP; ipp++) {

                    if (ipp == 0) {
                        ZedGraph.LineItem curve = _zPlotSamples.AddCurve("Raw Samples " + "Rx #" + (irx + 1).ToString(),
                                                                            x, sampTS[ipp], Color.Blue, ZedGraph.SymbolType.None);
                    }
                    else {
                        ZedGraph.LineItem curve = _zPlotSamples.AddCurve("2nd IPP, StdDev = " + stdDev.ToString("G3"), x, sampTS[ipp], Color.Green, ZedGraph.SymbolType.None);
                    }
                }

                //_zPlotSamples.setSize();
                _zPlotSamples.SetWindowTitle("Sampled TS");
                _zPlotSamples.Display();
            }
        }

        private void UpdateStatusProgress(int progress) {
            //return;
            if (this.xpProgressBar1.IsDisposed) {
                return;
            }
			if (progress < 0 || progress > 100) {
				return;
			}
            if (this.xpProgressBar1.InvokeRequired) {
                //UpdateProgressCallback d = new UpdateProgressCallback(UpdateStatusProgress);
                //this.Invoke(d, new object[] { progress });
                this.Invoke(new MethodInvoker(() => UpdateStatusProgress(progress)));
            }
            else {

                //this.textBoxServerStatusProgress.Text = progress.ToString() + " %";

                //this.xpProgressBar1.ColorBarBorder = Color.Blue;
                //this.xpProgressBar1.ColorBarBorder = Color.Blue;

                //this.xpProgressBar1.Font = new Font("Arial", 10.0f);
                this.xpProgressBar1.Position = progress;
                if (progress == 0) {
                    int x = 0;
                }

            }
        }

        private System.Drawing.Font progressFont;


        
        // not used:
		private void OnClientMessage(object sender, string msg) {
			UpdateClientText(msg);
		}

		private void UpdateClientText(string text) {
			// InvokeRequired required compares the thread ID of the
			// calling thread to the thread ID of the creating thread.
			// If these threads are different, it returns true.
            if (this.listBoxMessages.InvokeRequired) {
				//UpdateTextCallback d = new UpdateTextCallback(UpdateClientText);
				//this.Invoke(d, new object[] { text });
                this.Invoke(new MethodInvoker(() => UpdateClientText(text)));
            }
			else {
                text = "Client: " + text + " (" + DateTime.Now.DayOfYear.ToString("000 ") + DateTime.Now.ToString("HH:mm:ss") + ")";
				//this.textBoxClientStatus.Text = text;
                SendMessageToListLog(text);
                //this.listBoxMessages.Items.Add(text);
                //listBoxMessages.SetSelected(listBoxMessages.Items.Count - 1, true);
                //listBoxMessages.SetSelected(listBoxMessages.Items.Count - 1, false);
            }
		}


        private void buttonGo_Click(object sender, EventArgs e) {
            //++buttonCount;
            listBoxMessages.BackColor = Color.White;
            _tryRestart = true;
            int id = Thread.CurrentThread.ManagedThreadId;
            //this.textBoxServerStatusText.Text = "";

            string fileName = GetParFileName();
            if (!File.Exists(_parxFilePath)) {
                MessageBoxEx.Show("File: \"" + fileName + "\" does not exist in directory <" + _currentDirectory + ">.", 5000);
                return;
            }

            PopCommands pauseState;
            if (checkBoxPause.Checked) {
                pauseState = PopCommands.PauseChecked;
            }
            else {
                pauseState = PopCommands.PauseUnchecked;
            }
            //bool result = _communicator.SendCommand(PopCommands.Go | pauseState);

            PopNStateFile.SetParFileCommand(_parxFilePath);
            PopNStateFile.SetParFileFolderRelPath(_parxFolderRelPath);

            string ext = Path.GetExtension(_parxFilePath);
            if (ext.ToLower().Contains("seq")) {
                _usingSeqFile = true;
                PopNStateFile.SetLogFolder("");
                PopNStateFile.SetDebug(true);

            }
            else {
                _usingSeqFile = false;
                try {
                    _param = PopParameters.ReadFromFile(_parxFilePath);
                }
                catch (Exception ee) {
                    throw new ApplicationException(">> Parfile is empty in buttonGo_click: " + _param);
                }
                _param = PopParameters.ReadFromFile(_parxFilePath);
                _logFolder = _param.SystemPar.RadarPar.ProcPar.PopFiles[0].LogFileFolder;
                PopNStateFile.SetLogFolder(_logFolder);
                PopNStateFile.SetDebug(_param.Debug.DebugToFile);
                int nrx = _param.SystemPar.RadarPar.ProcPar.NumberOfRx;
                int nhts = _param.SystemPar.RadarPar.BeamParSet[0].NHts;
                numericUpDownPlotRx.Maximum = nrx;
                if (_param.ReplayPar.Enabled) {
                    // number of hts written to POP file
                    nhts = _param.SystemPar.RadarPar.BeamParSet[0].NHts;
                }
                else {
                    // all hts computed
                    nhts = _param.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts / 2 + 1;
                }
                if (!_param.ReplayPar.Enabled) {
                    numericUpDownPlotHt.Maximum = nhts - 1;
                }
            }

            SetMessageListBoxSize(_usingSeqFile);


            // send command after everything updated
            Thread.Sleep(500);
            bool result = _communicator.SendCommand(PopCommands.Go | pauseState);

            if (result) {
                //SendMessageToListLog("Go Command Sent; using par file: " + Path.GetFileName(_parxFilePath));
            }
            else {
                SendMessageToListLog("Error sending Go Command ");
                SetNoServiceStatus();
               //CreateComm(null);
            }
        }

        private string GetParFileName() {
            string fileName = comboBoxConfigFile.Text;	// get par file name
            _parxFilePath = Path.Combine(_currentDirectory, fileName);
            _parxFilePath = Path.GetFullPath(_parxFilePath);
            return fileName;
        }

        private void SetMessageListBoxSize(bool usingSeqFile) {
            int yLocation, width, rightEdge, leftEdge;
            leftEdge = comboBoxConfigFile.Location.X;
            rightEdge = buttonParameters.Location.X + buttonParameters.Width;
            width = rightEdge - leftEdge;
            if (usingSeqFile) {
                yLocation = 114;
                labelCurrentParxFile.Text = "";
            }
            else {
                yLocation = 88;
            }
            listBoxMessages.Location = new Point(7, yLocation);
            int ysize = checkBoxPlotOptions.Location.Y - yLocation - 12;
            listBoxMessages.Size = new Size(width, ysize);
        }

 		private void checkBoxPause_CheckedChanged(object sender, EventArgs e) {
			bool result;
			//++buttonCount;
			if (checkBoxPause.Checked) {
				result = _communicator.SendCommand(PopCommands.PauseChecked);
			}
			else {
				result = _communicator.SendCommand(PopCommands.PauseUnchecked);
			}
			if (result) {
				//UpdateClientText("Pause Change Sent ");
			}
			else {
				UpdateClientText("Error sending Pause Command ");
                SetNoServiceStatus();
                //CreateComm(null);
			}
		}

		private void buttonStop_Click(object sender, EventArgs e) {
            //++buttonCount;
            //this.textBoxServerStatusText.Text = "";
            bool result = _communicator.SendCommand(PopCommands.Stop);
            if (result) {
                //UpdateClientText("Stop Command Sent ");
            }
            else {
	            UpdateClientText("Error sending Stop Command");
                SetNoServiceStatus();
                //CreateComm(null);
            }
            SetMessageListBoxSize(false);
        }

		private void buttonKill_Click(object sender, EventArgs e) {
			//++buttonCount;
			//this.textBoxServerStatusText.Text = "";
			bool result = _communicator.SendCommand(PopCommands.Kill);
			if (result) {
				UpdateClientText("Kill Command Sent ");
			}
			else {
				UpdateClientText("Error sending Kill Command");
                SetNoServiceStatus();
                //CreateComm(null);
			}
            SetMessageListBoxSize(false);
        }

        private void buttonStartService_Click(object sender, EventArgs e) {
            if (_service != null) {
                MessageBoxEx.Show("POPN is configured to NOT run as a service...\nStarting service anyway.", 2500);
                _service.PublicOnStop();
                InitService();
            }
            else {
                InitService();
                //InstallService();
                //CreateComm(null);
            }
            buttonServiceStatus_Click(null, null);
        }

        private void buttonStopService_Click(object sender, EventArgs e) {
            //_tryRestart = false;
            if (_service != null) {
                MessageBoxEx.Show("POPN is configured to NOT run as a service...\nStopping service anyway.", 2000);
                _service.PublicOnStop();
                InstallService("-stop");
            }
            else {
                InstallService("-stop");
            }
            buttonServiceStatus_Click(null, null);
        }

        private void buttonUninstall_Click(object sender, EventArgs e) {
            _tryRestart = false;
            if (_service != null) {
                MessageBoxEx.Show("Uninstalling service...", 1000);
                _service.PublicOnStop();
                InstallService("-u");
            }
            else {
                InstallService("-u");
            }
            buttonServiceStatus_Click(null, null);
        }

        private void _serviceTimer_Tick(Object myObject, EventArgs myEventArgs) {
            buttonServiceStatus_Click(null, null);
        }

        /// <summary>
        /// checks status message queue and updates status
        /// </summary>
        /// <param name="myObject"></param>
        /// <param name="myEventArgs"></param>
        private void _statusTimer_Tick(Object myObject, EventArgs myEventArgs) {
            int id = Thread.CurrentThread.ManagedThreadId;
            while (_statusQueue.Count > 0) {
                POPCommunicator.PopStatusArgs statusArg = _statusQueue.Dequeue();
                if (statusArg.Status.Status != PopStatus.None) {
                    _currentStatus = statusArg.Status.Status;
                }
                UpdateStatusText(statusArg.Status.Message);
                UpdateStatusException(statusArg.Status.ExceptionMessage);
                UpdateStatusProgress(statusArg.Status.ProgressPercent);
                UpdateStatus(statusArg.Status.Status);
                UpdateStatusCmdParFile(statusArg.Status.CommandParFile);
                UpdateStatusCurParFile(statusArg.Status.CurrentParFile);
                UpdateTimeStamp(statusArg.Status.TimeStamp);
            }

            if ((!_usingSeqFile) && (_param != null) && (!_param.ReplayPar.Enabled) ) {
                // can't really check for parameter changes in replay mode,
                //  because parameters are changed when record is read.

                if ((_currentStatus.Includes(PopStatus.Paused)) /*&& (!_param.ReplayPar.Enabled)*/) {
                    // check if parameter file has changed
                    if (_doParFileChangedMessage) {
                        DateTime fileTime = File.GetLastWriteTime(_currentParxFilePath);
                        if (_parFileUpdateTime == DateTime.MaxValue) {
                            _parFileUpdateTime = fileTime;
                        }
                        if (fileTime > _parFileUpdateTime) {
                            _parFileUpdateTime = fileTime;
                            PopParameters par = PopParameters.ReadFromFile(_currentParxFilePath);
                            if (!par.Equals(_param)) {
                                _doParFileChangedMessage = false;
                                Console.Beep(880, 100);
                                //Console.Beep(600, 100);
                                Console.Beep(440, 100);
                                Console.Beep(880, 100);
                                Console.Beep(440, 100);
                                UpdateMessageList("Par file has changed. Did you want to UNLOAD?");
                                MessageBoxEx.Show("Par file has changed. \nTo use these new parameters you must UNLOAD first.", "Par File Changed", 2500);
                                int x = 0;
                            }
                        }
                    }
                }
                else {
                    _doParFileChangedMessage = true;
                }
            }
            return;

            /*
            if (_communicator != null) {
                bool result = _communicator.SendCommand(PopCommands.Ping);
                if (!result) {
                    Console.Beep(1400, 300);
                    System.Threading.Thread.Sleep(6000);
                    Console.Beep(440, 300);
                }
            }
             * */
		}

        private void SetNoServiceStatus() {
            PopStatusMessage status = new PopStatusMessage();
            status.ProgressPercent = 0;
            status.TimeStamp = DateTime.Now;
            status.Status = PopStatus.NoService;
            status.Message = "";
            status.ExceptionMessage = "";

            POPCommunicator.PopStatusArgs statusArg = new POPCommunicator.PopStatusArgs(status);
            _statusQueue.Enqueue(statusArg);
            UpdateClientText("POPN Comm Server or POPN4Service Lost.");
            // try to restart service.
            // will need to resend command
            SimpleWorkerThread thread1 = new SimpleWorkerThread();
            thread1.SetWorkerMethod(CreateComm);
            thread1.SetCompletedMethod(CreateCommCompleted);
            thread1.Go();
        }

        private void _pingTimer_Tick(object sender, System.Timers.ElapsedEventArgs e) {
            int id = Thread.CurrentThread.ManagedThreadId;
            lock (_timerLock) {
                if (_timerIsBusy) {
                    return;
                }
                else {
                    _timerIsBusy = true;
                }
            }
            if (_communicator != null) {
                bool result = _communicator.SendCommand(PopCommands.Ping);
                if (!result) {
                    // If we have lost contact with service, wait a while before
                    //  trying to restart service. If we really wanted service stopped,
                    //  we must exit user interface before this time is up.
                    SendMessageToListLog(">>----Restarting service in 15 seconds");
                    SendMessageToListLog(">>----Exit now to prevent service start");
                    Thread.Sleep(15000);
                    if (!_multipleNoService) {
                        SetNoServiceStatus();
                        _multipleNoService = true;
                    }
                    /*
                    if (_tryRestart) {
                        SendMessageToListLog("Error sending Ping command.");
                        // Service has probably stopped,
                        //  try to restart
                        // do things to check for server:
                        SendMessageToListLog("Try to restart service.");
                        Console.Beep(1400, 300);
                        CreateComm(null);
                        //System.Threading.Thread.Sleep(6000);
                        Console.Beep(440, 300);
                    }
                     * */
                }
                else {
                    _multipleNoService = false;
                }
            }
            _timerIsBusy = false;
        }


        public class StatusQueue {

            private Queue _queue;

            public StatusQueue() {
                _queue = new Queue();
            }

            public int Count {
                get { return _queue.Count; }
            }

            public void Enqueue(POPCommunicator.PopStatusArgs item) {
                lock (_queue.SyncRoot) {
                    _queue.Enqueue(item);
                }
            }

            public POPCommunicator.PopStatusArgs Dequeue() {
                POPCommunicator.PopStatusArgs status = null;
                lock (_queue.SyncRoot) {
                    if (_queue.Count > 0) {
                        status = (POPCommunicator.PopStatusArgs)_queue.Dequeue();
                    }
                    else {
                        status = null;
                    }
                }
                return status;
            }

            public POPCommunicator.PopStatusArgs Peek() {
                POPCommunicator.PopStatusArgs command = null;
                lock (_queue.SyncRoot) {
                    if (_queue.Count > 0) {
                        command = (POPCommunicator.PopStatusArgs)_queue.Peek();
                    }
                    else {
                        command = null;
                    }
                }
                return command;
            }

		}  // end class CommandQueue

		private void buttonParameters_Click(object sender, EventArgs e) {

            string fileName = comboBoxConfigFile.Text;	// get par file name
            string parFilePath = Path.Combine(_currentDirectory, fileName);
            parFilePath = Path.GetFullPath(parFilePath);  // cleans up name
            if (!File.Exists(parFilePath)) {
                MessageBoxEx.Show("File: \"" + fileName + "\" does not exist in directory <" + _currentDirectory + ">.", 5000);
                return;
            }

            string ext = Path.GetExtension(fileName);

            if (ext.ToLower().Contains("parx")) {
                
                if (PopNSetup3.IsLiving) {
                    // Setup Form is already showing
                    if (SetupForm != null) {
                        PopParameters par = SetupForm.SetupParameters;
                        if (par.Source == parFilePath) {
                            // Setup is showing same par file, so leave it
                            SetupForm.WindowState = FormWindowState.Normal;
                            SetupForm.Focus();
                            SetupForm.Communicator = _communicator;
                            return;
                        }
                        else {
                            // new par file, close old setup screen
                            SetupForm.Close();
                            SetupForm = null;
                        }
                    }
                }

                // create new setup screen
			    SetupForm = new PopNSetup3();
                SetupForm.Communicator = _communicator;

                PopParameters parameters;
                try {
                    parameters = PopParameters.ReadFromFile(parFilePath);
                }
                catch (Exception ee) {
                    MessageBoxEx.Show("Creating new blank parameter file", "Error reading Parameter file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, 4000);
                    parameters = new PopParameters();
                }


                parameters.Source = parFilePath;
			    SetupForm.SetupParameters = parameters;
			    SetupForm.Show();

                /*

                //SetupForm.ShowDialog();
                
                // check to see if new par file was selected in SetupForm
                // --Only needed if dialog was modal (ShowDialog was called)
			    string parFile = Path.GetFileName(SetupForm.SetupParameters.Source);

                populateParxCombo(_parxFolderRelPath);

                // if comboBoxConfigFile is configured as a DropDownList
                //  i.e. the text box is read-only and selection must come from the list,
                //  then we must re-fill the text box after populateParxCombo().

			    if (_currentStatus.Includes(PopStatus.Stopped) || _currentStatus == PopStatus.None) {
				    // update the selected par file if radar not running
                    int iFile = 0;
                    foreach (string file in comboBoxConfigFile.Items) {
                        string listName = Path.GetFileName(file);
                        if (listName.ToLower() == parFile.ToLower()) {
                            break;
                        }
                        iFile++;
                    }
				    if (iFile >= 0) {
					    comboBoxConfigFile.SelectedIndex = iFile;
				    }
			    }
                
                */
            }
            else if (ext.ToLower().Contains("seq")) {
                SeqForm = new SequenceForm(parFilePath);
                SeqForm.Show();
            }
        }


		#region Private Methods
		//
        private void populateParxCombo() {
            if (!populateParxCombo(@"..\Parameters")) {
                if (!populateParxCombo(@".\Parameters")) {
                    populateParxCombo(".");
                }
            }
        }

		private bool populateParxCombo(string folderRelPath) {

            string parxFolder = Path.Combine(_currentDirectory, folderRelPath);
            parxFolder = Path.GetFullPath(parxFolder);

            if (!Directory.Exists(parxFolder)) {
                //parxFolder = _currentDirectory;
                return false;
            }

            string[] parxFiles = Directory.GetFiles(parxFolder, "*.parx");
            string[] seqFiles = Directory.GetFiles(parxFolder, "*.seq");
            /*
			foreach (string fileName in parxFiles) {
				string newFileName = Path.GetFileName(fileName);
				if (!comboBoxConfigFile.Items.Contains(newFileName)) {
					comboBoxConfigFile.Items.Add(newFileName);
				}
			}
            */
			//comboBoxConfigFile.Text = (string)comboBoxConfigFile.Items[0];
            comboBoxConfigFile.Items.Clear();
            foreach (string fileName in parxFiles) {
                string newFileName = Path.GetFileName(fileName);
                newFileName = Path.Combine(folderRelPath, newFileName);
                comboBoxConfigFile.Items.Add(newFileName);
            }
            foreach (string fileName in seqFiles) {
                string newFileName = Path.GetFileName(fileName);
                newFileName = Path.Combine(folderRelPath, newFileName);
                comboBoxConfigFile.Items.Add(newFileName);
            }
            return true;
        }

        private void POPNMainForm_FormClosing(object sender, FormClosingEventArgs e) {

            SendMessageToListLog("POPN Control Panel is Closing.");
            ServiceController sc = GetPopNService();
            if (sc != null) {
                if (sc.Status == ServiceControllerStatus.Running) {
                    DialogResult rr = MessageBoxEx.Show("You are closing ONLY the POPN Control Panel.\r\n\nThe POPN4 Service will continue to run.",
                            "POPN Control Panel Closing", MessageBoxButtons.OKCancel, 2000);
                    if (rr == System.Windows.Forms.DialogResult.Cancel) {
                        e.Cancel = true;
                    }
                }
            }

            GetParFileName();
            PopNStateFile.SetCurrentParFile(_parxFilePath);
            PopNStateFile.SetParFileFolderRelPath(_parxFolderRelPath);

            // save window position
            try {
                FormWindowState winState = this.WindowState;
                if (winState == FormWindowState.Normal) {
                    Properties.Settings.Default.MainFormSize = this.DesktopBounds.Size;
                    Properties.Settings.Default.MainFormLocation = this.DesktopBounds.Location;
                }
                else {
                    // minimized of maximized
                    Properties.Settings.Default.MainFormSize = this.RestoreBounds.Size;
                    Properties.Settings.Default.MainFormLocation = this.RestoreBounds.Location;
                }
                Properties.Settings.Default.OptionsAreExpanded = checkBoxPlotOptions.Checked;
                Properties.Settings.Default.ShowSampledTimeSeries = checkBoxSampledTS.Checked;
                Properties.Settings.Default.ShowDopplerAscan = checkBoxDopplerAScan.Checked;
                Properties.Settings.Default.ShowClutterWavelet = checkBoxClutterWavelet.Checked;
                Properties.Settings.Default.ShowDopplerTimeSeries = checkBoxDopplerTS.Checked;
                Properties.Settings.Default.ShowDopplerSpectrum = checkBoxDopplerSpec.Checked;
                Properties.Settings.Default.ShowCrossCorr = checkBoxCrossCorr.Checked;
                Properties.Settings.Default.ShowSNRProfile = checkBoxMoments.Checked;
                Properties.Settings.Default.ShowDopplerProfile = checkBoxDoppler.Checked;
                Properties.Settings.Default.RxNumber = (int)numericUpDownPlotRx.Value;
                Properties.Settings.Default.HtIndex = (int)numericUpDownPlotHt.Value;

                Point location;
                Size size;
                _zPlotDoppler.getPosition(out location, out size);
                RestoreIfMinimized(_zPlotDoppler, ref location, ref size);
                Properties.Settings.Default.DopplerProfLocation = location;
                Properties.Settings.Default.DopplerProfSize = size;

                _zPlotMoments.getPosition(out location, out size);
                RestoreIfMinimized(_zPlotMoments, ref location, ref size);
                Properties.Settings.Default.SNRProfileLocation = location;
                Properties.Settings.Default.SNRProfileSize = size;

                _zPlotSpec.getPosition(out location, out size);
                RestoreIfMinimized(_zPlotSpec, ref location, ref size);
                Properties.Settings.Default.DopplerSpecLocation = location;
                Properties.Settings.Default.DopplerSpecSize = size;

                _zPlotXCorr.getPosition(out location, out size);
                RestoreIfMinimized(_zPlotXCorr, ref location, ref size);
                Properties.Settings.Default.CrossCorrLocation = location;
                Properties.Settings.Default.CrossCorrSize = size;

                _zPlotXCorrRatio.getPosition(out location, out size);
                RestoreIfMinimized(_zPlotXCorrRatio, ref location, ref size);
                Properties.Settings.Default.CrossCorrRatioLocation = location;
                Properties.Settings.Default.CrossCorrRatioSize = size;

                _zPlotXCorrProfile.getPosition(out location, out size);
                RestoreIfMinimized(_zPlotXCorrProfile, ref location, ref size);
                Properties.Settings.Default.CrossCorrSlopeLocation = location;
                Properties.Settings.Default.CrossCorrSlopeSize = size;

                _zPlotCltrWvlt.getPosition(out location, out size);
                RestoreIfMinimized(_zPlotCltrWvlt, ref location, ref size);
                Properties.Settings.Default.ClutterWaveletLocation = location;
                Properties.Settings.Default.ClutterWaveletSize = size;

                _zPlotTS.getPosition(out location, out size);
                RestoreIfMinimized(_zPlotTS, ref location, ref size);
                Properties.Settings.Default.DopplerTSLocation = location;
                Properties.Settings.Default.DopplerTSSize = size;

                _zPlotAScan.getPosition(out location, out size);
                RestoreIfMinimized(_zPlotAScan, ref location, ref size);
                Properties.Settings.Default.DopplerAScanLocation = location;
                Properties.Settings.Default.DopplerAScanSize = size;

                _zPlotSamples.getPosition(out location, out size);
                RestoreIfMinimized(_zPlotSamples, ref location, ref size);
                Properties.Settings.Default.SampledTSLocation = location;
                Properties.Settings.Default.SampledTSSize = size;

                if (_powerMeterDisplay != null) {
                    location = _powerMeterDisplay.Location;
                    if (_powerMeterDisplay.WindowState == FormWindowState.Minimized) {
                        location = _powerMeterDisplay.RestoreBounds.Location;
                    }
                    else if (!_powerMeterDisplay.Visible) {
                        // not sure if this state ever happens,
                        //  but form does disappear sometimes
                        location = new Point(200, 200);
                    }
                    
                    Properties.Settings.Default.PowerMeterLocation = location;
                }

                // for reference, this is where the settings config file is kept:
                var path = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;

                Properties.Settings.Default.Save();
            }
            catch {
            }
        }

        private void RestoreIfMinimized(QuickPlotZ plot, ref Point location, ref Size size) {
            if (plot.Form.WindowState == FormWindowState.Minimized) {
                location = plot.Form.RestoreBounds.Location;
                size = plot.Form.RestoreBounds.Size;
            }
        }

        private void checkBoxPlotOptions_CheckedChanged(object sender, EventArgs e) {
            if (checkBoxPlotOptions.Checked) {
                //_showingPlotOptions = true;
                checkBoxPlotOptions.Text = "- Hide Options";
                int y0 = this.Size.Height - this.ClientRectangle.Size.Height;
                int y1 = this.groupBoxPlotOptions.Location.Y;
                int y2 = this.groupBoxPlotOptions.Size.Height;
                Size newSize = new Size(this.Size.Width, y0 + y1 + y2 + 8);
                checkBoxPlotOptions.Anchor = AnchorStyles.Top;
                groupBoxPlotOptions.Anchor = AnchorStyles.Top;
                buttonServiceStatus.Anchor = AnchorStyles.Top;
                buttonReconnect.Anchor = AnchorStyles.Top;
                labelServiceStatus.Anchor = AnchorStyles.Top;
                groupBoxService.Anchor = AnchorStyles.Top;
                buttonKill.Anchor = AnchorStyles.Top;
                listBoxMessages.Anchor = AnchorStyles.Top;
                this.Size = newSize;
                checkBoxPlotOptions.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                buttonServiceStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                buttonReconnect.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                labelServiceStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                groupBoxPlotOptions.Anchor = AnchorStyles.Bottom;
                groupBoxService.Anchor = AnchorStyles.Bottom;
                buttonKill.Anchor = AnchorStyles.Bottom;
                listBoxMessages.Anchor = AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            }
            else {
                //_showingPlotOptions = false;
                checkBoxPlotOptions.Text = "+ More Options";
                int y0 = this.Size.Height - this.ClientRectangle.Size.Height;
                int y1 = this.checkBoxPlotOptions.Location.Y;
                int y2 = this.checkBoxPlotOptions.Size.Height;
                Size newSize = new Size(this.Size.Width, y0 + y1 + y2 + 8);
                checkBoxPlotOptions.Anchor = AnchorStyles.Top;
                buttonServiceStatus.Anchor = AnchorStyles.Top;
                buttonReconnect.Anchor = AnchorStyles.Top;
                labelServiceStatus.Anchor = AnchorStyles.Top;
                groupBoxPlotOptions.Anchor = AnchorStyles.Top;
                groupBoxService.Anchor = AnchorStyles.Top;
                buttonKill.Anchor = AnchorStyles.Top;
                listBoxMessages.Anchor = AnchorStyles.Top;
                this.Size = newSize;
                checkBoxPlotOptions.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                buttonServiceStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                buttonReconnect.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                labelServiceStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                groupBoxPlotOptions.Anchor = AnchorStyles.Bottom;
                groupBoxService.Anchor = AnchorStyles.Bottom;
                buttonKill.Anchor = AnchorStyles.Bottom;
                listBoxMessages.Anchor = AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            }
        }

        private void buttonReplot_Click(object sender, EventArgs e) {
            PlotData(_param);
        }

        private void labelConfigExpand_Click(object sender, EventArgs e) {
            string path = Path.Combine(_currentDirectory, comboBoxConfigFile.Text);
            //toolTip1.Show(path, labelConfigExpand, 20, 20, 4000);
            toolTip1.Show(_currentDirectory, labelConfigExpand, 20, 20, 4000);
            //toolTip1.Show(path, labelConfigExpand, 8000);
        }

        private void comboBoxConfigFile_DropDown(object sender, EventArgs e) {
            _currentConfigFile = comboBoxConfigFile.Text;
            populateParxCombo(_parxFolderRelPath);
        }

        private void comboBoxConfigFile_DropDownClosed(object sender, EventArgs e) {
            string file = comboBoxConfigFile.Text;
            if (string.IsNullOrWhiteSpace(file)) {
                comboBoxConfigFile.Text = _currentConfigFile;
            }
            // take focus away from comboBox so keyboard cannot affect selected file
            buttonGo.Focus();
        }

        private void buttonReconnect_Click(object sender, EventArgs e) {
            SimpleWorkerThread thread1 = new SimpleWorkerThread();
            thread1.SetWorkerMethod(CreateComm);
            thread1.SetCompletedMethod(CreateCommCompleted);
            thread1.Go();
        }

        private void buttonBrowsePar_Click(object sender, EventArgs e) {
            string parFolder = _parxFolderFullPath;
            if (Directory.Exists(parFolder)) {
                folderBrowserDialog1.SelectedPath = parFolder;
            }
            DialogResult rr = folderBrowserDialog1.ShowDialog();
            if (rr == DialogResult.OK) {
                parFolder = folderBrowserDialog1.SelectedPath;
            }
            _parxFolderFullPath = parFolder;
            _parxFolderRelPath = Tools.GetRelativePath(_currentDirectory, _parxFolderFullPath);
            populateParxCombo(_parxFolderRelPath);
            PopNStateFile.SetParFileFolderRelPath(_parxFolderRelPath);
        }

        private void numericUpDownPlotHt_ValueChanged(object sender, EventArgs e) {

        }

        //
		#endregion Private methods

	}  // end class Form

    /*

    ////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// groupbox with colored border
    /// </summary>
    /// <remarks>
    /// https://social.msdn.microsoft.com/Forums/windows/en-US/cfd34dd1-b6e5-4b56-9901-0dc3d2ca5788/changing-border-color-of-groupbox
    /// </remarks>
    public class myGroupBox : GroupBox

    {

        private Color borderColor;

 

        public Color BorderColor

        {

            get { return this.borderColor; }

            set { this.borderColor = value; }

        }

 

        public myGroupBox()

        {

            this.borderColor = Color.Black;

        }

 

        protected override void OnPaint(PaintEventArgs e)

        {

            Size tSize = TextRenderer.MeasureText(this.Text, this.Font);

 

            Rectangle borderRect = e.ClipRectangle;

            borderRect.Y += tSize.Height / 2;

            borderRect.Height -= tSize.Height / 2;

            ControlPaint.DrawBorder(e.Graphics, borderRect, this.borderColor, ButtonBorderStyle.Solid);

 

            Rectangle textRect = e.ClipRectangle;

            textRect.X += 6;

            textRect.Width  = tSize.Width;

            textRect.Height = tSize.Height;

            e.Graphics.FillRectangle(new SolidBrush(this.BackColor), textRect);

            e.Graphics.DrawString(this.Text, this.Font, new SolidBrush(this.ForeColor), textRect);

        }

    }
    */

}
