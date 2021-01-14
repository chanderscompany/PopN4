using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Threading;
using System.IO;
using DACarter.Utilities;

namespace DACarter.Utilities
{
	/// <summary>
	/// This is a basic form which is intended to serve as the basis for a
	/// simple .NET comm port reader program.
	/// Use this as the base class instead of System.Windows.Forms.Form
	/// in your windows application.
	/// 
	/// This form consists of these components:
	/// tbxMessages - a text box to display messages.
	/// tbxDataString - a text box to display the data string from comm port.
	/// btnPause - a button to pause/restart reading from the comm port.
	/// timer1 - a timer that simply auto starts the program.
	/// 
	/// Derived class needs to call
	///		SetIniFile(string iniFileName, string iniFileSection);
	///		SetInstrumentName(string inst);
	///		_autoStart = false;  // iff do not want auto start of comm port
	/// 
	/// Override:
	///		protected void OnDataReady(object sender, EventArgs ea)
	///	to handle data string from comm port,
	///	which is available in _readerThread.DataPackage.DataString .
	///	
	///	Override:
	///		bool initializeSettingsFromFileEx()
	///	to read any other values from inifile.
	///	_iniReader, _iniFileSection are accessible, so derived class can call e.g.
	///		_userID = _iniReader.ReadInteger(_iniFileSection,"UserID",999)
	///		
	/// IniFile entries used (with default values given after '='):
	///		Baudrate=4800
	///		CommPort=COM3
	///		Debug=NO
	///		
	/// </summary>
	public class CommPortReaderForm : System.Windows.Forms.Form {

		public System.Windows.Forms.TextBox tbxMessages;
		public System.Windows.Forms.TextBox tbxDataString;

		protected CommPortWorkerThread _readerThread;
		protected bool _isDebugMode;
		protected bool _programRunning;
		protected bool _autoStart;
		protected string _iniFileName, _iniPathFile;
		protected string _iniFileSection;
		protected string _instrumentName;
		protected IniReader _iniReader;

		private System.Windows.Forms.Timer timer1;
		public System.Windows.Forms.Button btnPause;
		private System.ComponentModel.IContainer components;

		public CommPortReaderForm() {
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//

			_readerThread = new CommPortWorkerThread(this);
			// connect event handlers for standard thread events
			_readerThread.Completed += new EventHandler(WorkFinished);
			_readerThread.Cancelled += new EventHandler(WorkCancelled);
			_readerThread.Failed += new System.Threading.ThreadExceptionEventHandler(WorkFailed);
			_readerThread.MessageEvent += new CommPortWorkerThread.MessageEventHandler(OnMessageReady);
			_readerThread.DataReadyEvent += new EventHandler(OnDataReady);

			_isDebugMode = false;
			_autoStart = true;

			_iniFileSection = "";
			_iniFileName = "";

			//startUp();

		}

		protected void startUp() {
			setProgramRunning(false);

			outputMessage(MessageLevel.Control,"Initializing...");
			tbxDataString.Text = "";

			bool initOK = initializeSettingsFromFile();
			bool init2OK = initializeSettingsFromFileEx();
			_readerThread.DeviceName = _instrumentName;

			if ((initOK) && (init2OK)) {
				timer1.Interval = 1000;
			}
			else {
				timer1.Interval = 7000;
			}
			timer1.Start();

		}

		protected void timer1_Tick(object sender, System.EventArgs e) {
			timer1.Stop();
			Go();
		}

		protected void Go() {
			outputMessage(MessageLevel.Control,"Starting COMM port...");
			//textBox1.Text = "Starting COMM port...";
			setProgramRunning(true);
			/*
						RS232 newPort = new RS232();
						newPort.BaudRate = RS232.BaudRates.Baud9600;
						newPort.CommPort = 4;
						newPort.DataSize = RS232.DataSizes.Size8;
						newPort.Handshaking = RS232.Handshakes.None;
						newPort.StopBit = RS232.StopBits.Bit1;
						newPort.Parity = RS232.Parities.None;
						newPort.InputMode = RS232.InputModes.Text;
						try {
							newPort.PortOpen = true;
							newPort.DTREnable = true;
							for (int i=0; i<100; i++) {
								string data = newPort.Input;
								Thread.Sleep(1000);
							}
						}
						catch (Exception e) {
							string error = e.Message;
						}
						if (newPort.PortOpen) {
							newPort.PortOpen = false;
						}
			*/			
			
			_readerThread.Start();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing ) {
			if( disposing ) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			this.tbxMessages = new System.Windows.Forms.TextBox();
			this.tbxDataString = new System.Windows.Forms.TextBox();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.btnPause = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// tbxMessages
			// 
			this.tbxMessages.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.tbxMessages.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.tbxMessages.Location = new System.Drawing.Point(8, 8);
			this.tbxMessages.Multiline = true;
			this.tbxMessages.Name = "tbxMessages";
			this.tbxMessages.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.tbxMessages.Size = new System.Drawing.Size(360, 40);
			this.tbxMessages.TabIndex = 0;
			this.tbxMessages.Text = "";
			// 
			// tbxDataString
			// 
			this.tbxDataString.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.tbxDataString.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.tbxDataString.Location = new System.Drawing.Point(8, 72);
			this.tbxDataString.Multiline = true;
			this.tbxDataString.Name = "tbxDataString";
			this.tbxDataString.Size = new System.Drawing.Size(360, 64);
			this.tbxDataString.TabIndex = 1;
			this.tbxDataString.Text = "";
			// 
			// timer1
			// 
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			// 
			// btnPause
			// 
			this.btnPause.BackColor = System.Drawing.Color.PowderBlue;
			this.btnPause.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnPause.Location = new System.Drawing.Point(280, 152);
			this.btnPause.Name = "btnPause";
			this.btnPause.TabIndex = 2;
			this.btnPause.Text = "Pause";
			this.btnPause.Click += new System.EventHandler(this.btnPause_Click);
			// 
			// CommPortReaderForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.BackColor = System.Drawing.Color.BlanchedAlmond;
			this.ClientSize = new System.Drawing.Size(376, 405);
			this.Controls.Add(this.btnPause);
			this.Controls.Add(this.tbxDataString);
			this.Controls.Add(this.tbxMessages);
			this.Name = "CommPortReaderForm";
			this.Text = "CommPortReader Form";
			this.Closing += new System.ComponentModel.CancelEventHandler(this.CommPortReaderForm_Closing);
			this.Load += new System.EventHandler(this.CommPortReaderForm_Load);
			this.ResumeLayout(false);

		}
		#endregion


		/// <summary>
		/// Event handler for worker thread 'Completed' event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected virtual void WorkFinished(object sender, EventArgs e) {
			setProgramRunning(false);
			outputMessage(MessageLevel.Control, "Worker thread finished.");
		}

		/// <summary>
		/// Event handler for worker thread 'Cancelled' event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected virtual void WorkCancelled(object sender, EventArgs e) {
			setProgramRunning(false);
			outputMessage(MessageLevel.Control, "Worker thread canceled.");
		}

		/// <summary>
		/// Event handler for worker thread 'Failed' event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected virtual void WorkFailed(object sender, System.Threading.ThreadExceptionEventArgs e) {
			setProgramRunning(false);
			outputMessage(MessageLevel.Error, "Worker thread failed: " + e.Exception.Message);
		}



		protected virtual void outputMessage(MessageLevel level, string message) {
			string notice;
			if (_isDebugMode) {
				notice = "   *** DEBUG MODE *** Phoney Data ***";
			}
			else {
				notice = "";
			}
			tbxMessages.Text = message + notice;
		}

		/// <summary>
		/// Receives message string from worker thread and 
		/// displays it.
		/// </summary>
		/// <param name="msg"></param>
		protected virtual void OnMessageReady(MessageLevel level, string msg) {
			outputMessage(level, msg);

		}

		protected virtual void OnDataReady(object sender, EventArgs ea) {

			string line = _readerThread.DataPackage.DataString;
			if (line == null) {
				tbxDataString.AppendText(".");
			}
			else if (line.Length == 0) {
				//tbxDataString.AppendText("x");
			}
			else {
				tbxDataString.Text = line;
			}
		}

		private void CommPortReaderForm_Load(object sender, System.EventArgs e) {
			btnPause.Focus();
			if (_autoStart) {
				startUp();
			}
		}

		private void CommPortReaderForm_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			_readerThread.Cancel();
			//textBox1.Text = "Terminating worker thread...";
			while (!_readerThread.IsDone) {
				Thread.Sleep(0);
			}
			//textBox1.Text = "Done";
			Thread.Sleep(500);
		}

		private void setProgramRunning(bool isRunning) {
			
			_programRunning = isRunning;
			if (!isRunning) {
				btnPause.Text = "Start";
				btnPause.BackColor = Color.Yellow;
			}
			else {
				btnPause.Text = "Pause";
				btnPause.BackColor = Color.PaleTurquoise;
			}
			
		}

		private void btnPause_Click(object sender, System.EventArgs e) {
			if (_programRunning) {
				_readerThread.Cancel();
				outputMessage(MessageLevel.Control,"Terminating worker thread...");
				while (!_readerThread.IsDone) {
					Thread.Sleep(0);
				}
				setProgramRunning(false);
			}
			else {
				startUp();
			}
		}

		protected void SetIniFile(string iniFileName, string iniFileSection) {
			_iniFileName = iniFileName;
			_iniFileSection = iniFileSection;

			string path = Application.StartupPath;
			if (_iniFileName.StartsWith(@"\")) {
				_iniPathFile = path + _iniFileName;
			}
			else {
				_iniPathFile = path + @"\" + _iniFileName;
			}
			//string s1 = INIFileInterop.INIWrapper.GetINIValue(iniFile,"GPS","xAllowInvalidTimes");
			_iniReader = new IniReader(_iniPathFile);
		}

		protected void SetInstrumentName(string instrument) {
			_instrumentName = instrument;
		}

		private bool initializeSettingsFromFile() {
			bool success = true;

			if (!File.Exists(_iniPathFile)) {
				outputMessage(MessageLevel.Error, "Ini file " + _iniPathFile + " does not exist -- using default settings.");
				success = false;
			}
			_readerThread.DataPackage.BaudRate = _iniReader.ReadInteger(_iniFileSection,"BaudRate",4800);
			_readerThread.DataPackage.CommPort = _iniReader.ReadString(_iniFileSection,"CommPort","COM9");
			_readerThread.DataPackage.DataBits = _iniReader.ReadInteger(_iniFileSection,"DataBits",8);
			string stopbits	= _iniReader.ReadString(_iniFileSection,"StopBits","1");
			_readerThread.DataPackage.StopBits = Double.Parse(stopbits);
			_readerThread.DataPackage.Parity = _iniReader.ReadString(_iniFileSection,"Parity","None");

			string debugMode = _iniReader.ReadString(_iniFileSection,"Debug","NO");
			debugMode = debugMode.ToUpper();
			_isDebugMode = false;
			if (debugMode.Length>0) {
				if (debugMode[0] == 'Y') {
					_isDebugMode = true;
				}
			}
			_readerThread.DataPackage.IsDebugMode = _isDebugMode;

			return success;
		}

		// to be overridden in derived class if needed
		protected virtual bool initializeSettingsFromFileEx() {
			return true;
		}

	}	// end of class CommPortReaderForm
}
