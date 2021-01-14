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
	/// The color of btnPause can be set via property PauseButtonColor
	/// 
	/// Derived class needs to call
	///		SetIniFile(string iniFileName, string iniFileSection);
	///		SetInstrumentName(string inst);
	///	and from event handlers:
	///		startUp()	// called from form's Load event handler (if autostart)
	///		shutDown()	// called from form's Closing event handler
	///	
	///	startUp() and shutDown() can be overridden.
	///		If so, base.StartUp() should probably be the last statement of the override.
	/// 
	/// Override:
	///		protected override void OnDataReady(object sender, EventArgs ea)
	///	to handle data string from comm port,
	///	which is available in _readerThread.DataPackage.DataString .
	///	
	///	Override:
	///		protected override bool initializeSettingsFromFileEx()
	///	to read any other values from inifile.
	///	_iniReader is accessible, so derived class can call e.g.
	///		_userID = _iniReader.ReadInteger("UserID",999)
	///		
	///	initializeSettingsFromFile() reads basic comm parameters from the ini file.
	///		It is called from this.startUp().
	///		If the derived class needs these settings before calling base.startUp(),
	///		it can call initializeSettingsFromFile directly.
	///		
	/// IniFile entries used (with default values given after '='):
	///		CommPort=COM3
	///		Baudrate=4800
	///		DataBits=8
	///		StopBits=1.0
	///		Parity=NONE
	///		Debug=NO
	///		
	/// </summary>
	public class CommPortReaderForm : System.Windows.Forms.Form {

		public System.Windows.Forms.TextBox tbxMessages;
		public System.Windows.Forms.TextBox tbxDataString;

		protected CommPortWorkerThread2005 _readerThread;
		protected bool _isDebugMode;
		protected bool _programRunning;
		protected string _iniFileName, _iniPathFile;
		protected string _iniFileSection;
		protected string _instrumentName;
		protected IniReader _iniReader;

		private Color _pauseButtonColor;

		private System.Windows.Forms.Timer timer1;
		protected System.Windows.Forms.Button btnPause;
		private System.ComponentModel.IContainer components;

		private char _beginChar, _endChar;

		public CommPortReaderForm() {
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//

			_readerThread = new CommPortWorkerThread2005(this);
			// connect event handlers for standard thread events
			_readerThread.Completed += new EventHandler(WorkFinished);
			_readerThread.Cancelled += new EventHandler(WorkCancelled);
			_readerThread.Failed += new System.Threading.ThreadExceptionEventHandler(WorkFailed);
			_readerThread.MessageEvent += new CommPortWorkerThread2005.MessageEventHandler(OnMessageReady);
			_readerThread.DataReadyEvent += new EventHandler(OnDataReady);

			_isDebugMode = false;

			_iniFileSection = "";
			_iniFileName = "";

			_pauseButtonColor = Color.PaleTurquoise;
			// default readblock chars to read line ending in '\n':
			_beginChar = (char)0;
			_endChar = '\n';

		}

		protected virtual void startUp() {
			setProgramRunning(false);

			outputMessage(MessageLevel.Control,"Initializing...");
			tbxDataString.Text = "";

			bool initOK = initializeSettingsFromFile();
			bool init2OK = initializeSettingsFromFileEx();
			_readerThread.DeviceName = _instrumentName;
			_readerThread.ReadBlockEndChar = _endChar;
			_readerThread.ReadBlockStartChar = _beginChar;

			if ((initOK) && (init2OK)) {
				timer1.Interval = 1000;
			}
			else {
				timer1.Interval = 7000;
			}
			timer1.Start();

		}

		protected virtual void shutDown() {
			_readerThread.Cancel();
			while (!_readerThread.IsDone) {
				Thread.Sleep(0);
			}
			Thread.Sleep(500);
		}

		protected void timer1_Tick(object sender, System.EventArgs e) {
			timer1.Stop();
			Go();
		}

		protected void Go() {
			outputMessage(MessageLevel.Control,"Starting COMM port...");
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
			this.tbxMessages.Size = new System.Drawing.Size(340, 40);
			this.tbxMessages.TabIndex = 0;
			// 
			// tbxDataString
			// 
			this.tbxDataString.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tbxDataString.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.tbxDataString.Location = new System.Drawing.Point(8, 72);
			this.tbxDataString.Multiline = true;
			this.tbxDataString.Name = "tbxDataString";
			this.tbxDataString.Size = new System.Drawing.Size(340, 64);
			this.tbxDataString.TabIndex = 1;
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
			this.btnPause.Size = new System.Drawing.Size(75, 23);
			this.btnPause.TabIndex = 2;
			this.btnPause.Text = "Pause";
			this.btnPause.UseVisualStyleBackColor = false;
			this.btnPause.Click += new System.EventHandler(this.btnPause_Click);
			// 
			// CommPortReaderForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.BackColor = System.Drawing.Color.BlanchedAlmond;
			this.ClientSize = new System.Drawing.Size(356, 401);
			this.Controls.Add(this.btnPause);
			this.Controls.Add(this.tbxDataString);
			this.Controls.Add(this.tbxMessages);
			this.Name = "CommPortReaderForm";
			this.Text = "CommPortReader Form";
			this.ResumeLayout(false);
			this.PerformLayout();

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

			string line;
			if (_isDebugMode) {
				line = "Test Data string: " + DateTime.Now.ToString();
			}
			else {
				line = _readerThread.DataPackage.DataString;
			}
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

		// derived classes must set up load and closing event handlers
		/*
		private void CommPortReaderForm_Load(object sender, System.EventArgs e) {
			startUp();
		}

		private void CommPortReaderForm_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			shutDown();
		}
		*/

		private void setProgramRunning(bool isRunning) {
			
			_programRunning = isRunning;
			if (!isRunning) {
				btnPause.Text = "Start";
				btnPause.BackColor = Color.Yellow;
			}
			else {
				btnPause.Text = "Pause";
				btnPause.BackColor = _pauseButtonColor;
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
			_iniReader.Section = _iniFileSection;
		}

		protected void SetInstrumentName(string instrument) {
			_instrumentName = instrument;
		}

		protected bool initializeSettingsFromFile() {
			bool success = true;

			if (!File.Exists(_iniPathFile)) {
				outputMessage(MessageLevel.Error, "Ini file " + _iniPathFile + " does not exist -- using default settings.");
				success = false;
			}
			_readerThread.DataPackage.BaudRate = _iniReader.ReadInteger("BaudRate",4800);
			_readerThread.DataPackage.CommPort = _iniReader.ReadString("CommPort","COM9");
			_readerThread.DataPackage.DataBits = _iniReader.ReadInteger("DataBits",8);
			string stopbits	= _iniReader.ReadString("StopBits","1");
			_readerThread.DataPackage.StopBits = Double.Parse(stopbits);
			_readerThread.DataPackage.Parity = _iniReader.ReadString("Parity","None");

			string debugMode = _iniReader.ReadString("Debug","NO");
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

		protected Color PauseButtonColor {
			get {
				return _pauseButtonColor;
			}
			set {
				_pauseButtonColor = value;
			}
		}

		protected char ReadBlockStartChar {
			get {
				lock(this) {return _beginChar;}
			}
			set {
				lock(this) {_beginChar = value;}
			}
		}

		protected char ReadBlockEndChar {
			get {
				lock(this) {return _endChar;}
			}
			set {
				lock(this) {_endChar = value;}
			}
		}

	}	// end of class CommPortReaderForm
}
