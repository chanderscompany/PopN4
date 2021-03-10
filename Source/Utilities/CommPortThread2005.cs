using System;
using System.ComponentModel;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO.Ports;

namespace DACarter.Utilities
{
	/// <summary>
	/// Class for worker thread that reads comm port
	/// This version uses RS232 class in DBComm namespace
	/// (see project DBComm).
	/// </summary>
	public class CommPortWorkerThread2005 : AsyncOperation
	{

		private const int BUFSIZE = 4096;
		private SerialPort _commPort;
		private string _instrumentName;
		//private StringBuilder _buffer = new StringBuilder(3000);
		private char[] _buffer = new char[BUFSIZE];
		private char _endChar, _beginChar;

		/// <summary>
		/// public constructor - must pass Control object that will receive events.
		/// </summary>
		/// <param name="isi">Control object (e.g. a Form) to receive events from worker thread.</param>
		public CommPortWorkerThread2005(ISynchronizeInvoke isi, string instrument)
			: base(isi)
		{
			DataPackage = new CommPortDataPackage();
			DeviceName = instrument;
			_beginChar = (char)0;
			_endChar = '\n';
		}

		public CommPortWorkerThread2005(ISynchronizeInvoke isi)
			: base(isi)
		{
			DataPackage = new CommPortDataPackage();
			DeviceName = "Device";
			_beginChar = (char)0;
			_endChar = '\n';
		}

		/// <summary>
		/// private default constructor - cannot be called.
		/// </summary>
		private CommPortWorkerThread2005() : base(null) { }

		[DllImport("kernel32.dll")]
		private static extern bool Beep(int frequency, int duration);


		/// <summary>
		/// This is where the work will be done.
		/// The base class calls this method on the worker
		/// thread when the Start method is called.
		/// Thread terminates when this method returns.
		/// </summary>
		protected override void DoWork()
		{
			/*
			// periodically check for CancelRequested
			if (CancelRequested) {
				if (_commPort.IsOpen) {
					_commPort.Close();
				}
				NotifyMessageReady(MessageLevel.Info,DeviceName +" Comm port closed");
				return;
			}
			*/

			_commPort = new SerialPort();
			//_commPort.ReceivedEvent += new SerialEventHandler(gotIncomingText);
			//_commPort.ErrorEvent += new SerialEventHandler(gotIncomingError);
			//_commPort.ErrorEvent += new SerialEventHandler(gotIncomingPinChange);

			SetUpCommPort(_commPort, DataPackage.CommPort,
							DataPackage.BaudRate,
							DataPackage.DataBits,
							DataPackage.StopBits,
							DataPackage.Parity);


			try
			{
				if (!DataPackage.IsDebugMode)
				{
					_commPort.Open();
					_commPort.DtrEnable = true;
					_commPort.RtsEnable = false;
				}
			}
			catch (Exception e)
			{
				string emsg = "Error opening " + DeviceName + " on " + _commPort.PortName + " - " + e.Message;
				NotifyMessageReady(MessageLevel.Error, emsg);
				Beep(440, 500);
				Thread.Sleep(5000);
				return;
			}
			string msg = "Reading " + DeviceName + " on " + _commPort.PortName;
			NotifyMessageReady(MessageLevel.Info, msg);

			//_commPort.OnComm += new RS232.OnCommEventHandler(OnReceivedChar);

			//_commPort.InputLength = 0;	// read entire buffer
			string oldData = _commPort.ReadExisting();
			while (true)
			{
				if (CancelRequested)
				{
					break;
				}
				string line;
				if (DataPackage.IsDebugMode)
				{
					line = getDebugLine();
					if (_endChar == 0)
					{
						Thread.Sleep(200);
					}
					else
					{
						for (int i = 0; i < 2; i++)
						{
							if (CancelRequested)
							{
								break;
							}
							Thread.Sleep(1000);
						}
					}
				}
				else
				{
					if (_endChar != 0)
					{
						// if we specified _endChar, read a block
						line = ReadCommPortBlock(_beginChar, _endChar);
					}
					else
					{
						// else read whatever is in buffer
						Thread.Sleep(200);
						line = _commPort.ReadExisting();
					}
				}
				lock (DataPackage)
				{
					DataPackage.DataString = line;
				}
				if (CancelRequested)
				{
					break;
				}
				if (line != string.Empty)
				{
					NotifyDataReady();
				}
			}

			if (_commPort.IsOpen)
			{
				_commPort.Close();
			}
			NotifyMessageReady(MessageLevel.Info, DeviceName + " Comm port closed");

			return;

		}	// end DoWork()

		/// <summary>
		/// Reads in a block of characters from the serial port up to and
		///		including the specified end character.
		/// </summary>
		/// <param name="endChar"></param>
		/// <returns></returns>
		private string ReadCommPortBlock(char beginChar, char endChar)
		{

			_commPort.ReadTimeout = 100;		// I do not think this affects anything
			//_commPort.ReceiveThreshold = 0;	// turn off receive events
			bool fillingBuffer = false;

			if (beginChar == (char)0) {
				fillingBuffer = true;
			}

			char chr = '?';

			int count = 0;
			/*
			// first, read everything in rx buffer
			//   but we are discarding it.
			_commPort.InputLength = 0;
			data = _commPort.Input;
			count = data.Length;
			if (count != 0) {
				int x = 0;  // just checking
			}
			if (count < BUFSIZE) {
			}
			count = 0;
			*/

			// then read one char at a time
			//_commPort.InputLength = 1;
			do
			{
				// exit if canceled
				if (CancelRequested) {
					break;
				}
				// read char
				if (_commPort.BytesToRead > 0) {
					chr = (char)_commPort.ReadChar();
				}
				else {
					// if nothing there, wait and try again
					Thread.Sleep(1);
					continue;
				}
				// check for begin character
				if (!fillingBuffer)
				{
					if (chr == beginChar) {
						fillingBuffer = true;
					}
				}
				// save the char in buffer
				if (fillingBuffer) {
					if (count < BUFSIZE) {
						_buffer[count++] = chr;
					}
					else {
						break; // buffer full
					}
				}
				// if end character, quit
				if (chr == endChar) {
					break;
				}
			} while (true);

			string data = new String(_buffer).Substring(0, count);
			return data;
		}

		/*
		void OnReceivedChar(object sender, DBComm.CommEventArgs cea) {
			
			if ( cea.Event == CommEventArgs.CommEvents.Receive) {
				string data;
				_commPort.InputLength = 0;
				string ch = _commPort.Input;
				if (ch.Length == 0) {
					return;
				}
				_buffer[_bufferCount++] = ch[0];
				if (ch[0] == '\n') {
					data = new String(_buffer).Substring(0,_bufferCount);
					_readLineInProgress = false;
				}
			}
		}
		*/



		// helper function
		public static void SetUpCommPort(SerialPort commPort,
									string portname,
									int baudrate,
									int databits,
									double stopbits,
									string parity)
		{

			/*
			commPort.CommPort = 4;
			commPort.BaudRate = RS232.BaudRates.Baud9600;
			commPort.DataSize = RS232.DataSizes.Size8;
			commPort.Handshaking = RS232.Handshakes.None;
			commPort.StopBit = RS232.StopBits.Bit1;
			commPort.Parity = RS232.Parities.None;
			commPort.InputMode = RS232.InputModes.Text;
			*/

			commPort.PortName = "COM" + Int32.Parse(portname.Substring(3, 1));

			commPort.BaudRate = baudrate;

			commPort.DataBits = databits;


			if (stopbits < 1.2)
			{
				commPort.StopBits = StopBits.One;
			}
			else if (stopbits > 1.8)
			{
				commPort.StopBits = StopBits.Two;
			}
			else
			{
				commPort.StopBits = StopBits.OnePointFive;
			}


			if (parity[0] == 'N')
			{
				commPort.Parity = Parity.None;
			}
			else if (parity[0] == 'S')
			{
				commPort.Parity = Parity.Space;
			}
			else if (parity[0] == 'M')
			{
				commPort.Parity = Parity.Mark;
			}
			else if (parity[0] == 'E')
			{
				commPort.Parity = Parity.Even;
			}
			else if (parity[0] == 'O')
			{
				commPort.Parity = Parity.Odd;
			}
			else
			{
				commPort.Parity = Parity.None;
			}

			commPort.Handshake = Handshake.None;

			//commPort.InputMode = CommPort.InputModes.Text;
		}

		/// <summary>
		/// Object used to exchange data between threads
		/// </summary>
		public CommPortDataPackage DataPackage;

		public delegate void MessageEventHandler(MessageLevel level, string msg);
		//public delegate void DataReadyEventHandler(bool commandIsFinished);

		/// <summary>
		/// Event fired to UI thread when worker thread has a message to send to UI.
		/// </summary>
		public event MessageEventHandler MessageEvent;

		/// <summary>
		/// Event fired to UI thread when worker thread has data ready to send to UI.
		/// </summary>
		public event EventHandler DataReadyEvent;


		/// <summary>
		/// Send a <code>MessageEvent</code> event to the main thread.
		/// </summary>
		/// <remarks>
		/// 
		/// </remarks>
		/// <param name="msg">The message to be sent to the main thread.</param>
		private void NotifyMessageReady(MessageLevel level, string msg)
		{
			lock (this)
			{
				// call base class method
				// (see class AsyncOperation)
				FireAsync(MessageEvent, level, msg);
			}
		}

		private void NotifyDataReady()
		{

			if (DataReadyEvent != null)
			{
				lock (this)
				{
					FireAsync(DataReadyEvent);
				}
			}

		}

		protected virtual string getDebugLine()
		{
			string line = DateTime.Now.ToString();
			Thread.Sleep(1000);
			return line;
		}


		public string DeviceName
		{
			get
			{
				lock (this) { return _instrumentName; }
			}
			set
			{
				lock (this) { _instrumentName = value; }
			}
		}

		public char ReadBlockStartChar
		{
			get
			{
				lock (this) { return _beginChar; }
			}
			set
			{
				lock (this) { _beginChar = value; }
			}
		}

		public char ReadBlockEndChar
		{
			get
			{
				lock (this) { return _endChar; }
			}
			set
			{
				lock (this) { _endChar = value; }
			}
		}

	/// <summary>
	/// This class packages the data that we want to exchange between the
	/// user interface thread and the worker thread.
	/// </summary>
	public class CommPortDataPackage
	{
		private string _commPort;
		private string _dataString;
		private int _baudRate;
		private bool _isDebugMode;
		private int _dataBits;
		private double _stopBits;
		private string _parity;

		public CommPortDataPackage()
		{
			_commPort = "COM3";
			_dataString = "???";
			_baudRate = 4800;
			_isDebugMode = false;
			_dataBits = 8;
			_stopBits = 1;
			_parity = "NONE";
		}

		public string CommPort
		{
			get
			{
				lock (this) { return _commPort; }
			}
			set
			{
				lock (this) { _commPort = value; }
			}
		}

		public int BaudRate
		{
			get
			{
				lock (this) { return _baudRate; }
			}
			set
			{
				lock (this) { _baudRate = value; }
			}
		}

		public int DataBits
		{
			get
			{
				lock (this) { return _dataBits; }
			}
			set
			{
				lock (this) { _dataBits = value; }
			}
		}

		public double StopBits
		{
			get
			{
				lock (this) { return _stopBits; }
			}
			set
			{
				lock (this) { _stopBits = value; }
			}
		}

		public string Parity
		{
			get
			{
				lock (this) { return _parity; }
			}
			set
			{
				lock (this) { _parity = value.ToUpper(); }
			}
		}

		public bool IsDebugMode
		{
			get
			{
				lock (this) { return _isDebugMode; }
			}
			set
			{
				lock (this) { _isDebugMode = value; }
			}
		}

		public string DataString
		{
			get
			{
				lock (this) { return _dataString; }
			}
			set
			{
				lock (this) { _dataString = value; }
			}
		}

	}

	/// <summary>
	/// This class packages the data that we want to exchange between the
	/// user interface thread and the worker thread.
	/// </summary>
	/*
	public class CommPortDataPackage {
		private string _commPort;
		private string _dataString;
		private int _baudRate;
		private bool _isDebugMode;
		private int _dataBits;
		private double _stopBits;
		private string _parity;

		public CommPortDataPackage() {
			_commPort = "COM3";
			_dataString = "???";
			_baudRate = 4800;
			_isDebugMode = false;
			_dataBits = 8;
			_stopBits = 1;
			_parity = "NONE";
		}

		public string CommPort {
			get {
				lock(this) {return _commPort;}
			}
			set {
				lock(this) {_commPort = value;}
			}
		}

		public int BaudRate {
			get {
				lock(this) {return _baudRate;}
			}
			set {
				lock(this) {_baudRate = value;}
			}
		}

		public int DataBits {
			get {
				lock(this) {return _dataBits;}
			}
			set {
				lock(this) {_dataBits = value;}
			}
		}

		public double StopBits {
			get {
				lock(this) {return _stopBits;}
			}
			set {
				lock(this) {_stopBits = value;}
			}
		}

		public string Parity {
			get {
				lock(this) {return _parity;}
			}
			set {
				lock(this) {_parity = value.ToUpper();}
			}
		}

		public bool IsDebugMode {
			get {
				lock(this) {return _isDebugMode;}
			}
			set {
				lock(this) {_isDebugMode = value;}
			}
		}

		public string DataString {
			get {
				lock(this) {return _dataString;}
			}
			set {
				lock(this) {_dataString = value;}
			}	
		}

	}	// end of class CommPortDataPackage
	*/


	
	}	// end of CommPortWorkerThread class

}
