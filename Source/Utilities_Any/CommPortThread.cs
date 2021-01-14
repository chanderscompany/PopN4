using System;
using System.ComponentModel;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;
using System.IO.Ports;
using System.Text;

namespace DACarter.Utilities
{
	/// <summary>
	/// Class for worker thread that reads comm port
	/// This version uses SerialPort class in System.IO.Ports
	/// (see project SerialPortFet).
	/// </summary>
	public class CommPortWorkerThread : AsyncOperation {

		private SerialPort _commPort;
		private string _instrumentName;

		/// <summary>
		/// public constructor - must pass Control object that will receive events.
		/// </summary>
		/// <param name="isi">Control object (e.g. a Form) to receive events from worker thread.</param>
		public CommPortWorkerThread(ISynchronizeInvoke isi, string instrument) : base(isi) {
			DataPackage = new CommPortDataPackage();
			DeviceName = instrument;
		}

		public CommPortWorkerThread(ISynchronizeInvoke isi) : base(isi) {
			DataPackage = new CommPortDataPackage();
			DeviceName = "Device";
		}

		/// <summary>
		/// private default constructor - cannot be called.
		/// </summary>
		private CommPortWorkerThread():base(null) {}

		[DllImport("kernel32.dll")]
		private static extern bool Beep(int frequency, int duration);


		/// <summary>
		/// This is where the work will be done.
		/// The base class calls this method on the worker
		/// thread when the Start method is called.
		/// Thread terminates when this method returns.
		/// </summary>
		protected override void DoWork() {
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


			try {
				if (!DataPackage.IsDebugMode) {
					_commPort.Open();
					_commPort.DtrEnable = true;
				}
			}
			catch (Exception e) {
				string emsg = "Error opening "+DeviceName+" on " + _commPort.PortName + " - " + e.Message;
				NotifyMessageReady(MessageLevel.Error,emsg);
				Beep(440,500);
				Thread.Sleep(5000);
				return;
			}
			string msg = "Reading "+DeviceName+" on " + _commPort.PortName;
			NotifyMessageReady(MessageLevel.Info,msg);

			while (true) {
				if (CancelRequested) {
					break;
				}
				string line;
				if (DataPackage.IsDebugMode) {
					line = getDebugLine();
				}
				else {
					_commPort.ReadTimeout = 1000;
					//int ch = _commPort.;
					line = _commPort.ReadLine();
				}
				lock (DataPackage) {
					DataPackage.DataString = line;
				}
				NotifyDataReady();
			}

			if (_commPort.IsOpen) {
				_commPort.Close();
			}
			NotifyMessageReady(MessageLevel.Info,DeviceName+" Comm port closed");

			return;
		}

		// helper function
		public static void SetUpCommPort(SerialPort commPort,
									string portname,
									int baudrate,
									int databits,
									double stopbits,
									string parity) {
			commPort.PortName = portname;
			commPort.BaudRate = baudrate;
		
			commPort.DtrEnable = false;
			commPort.RtsEnable = false;
		
			commPort.Encoding = new System.Text.ASCIIEncoding();
					
			commPort.DataBits = databits;

			if (stopbits < 1.2) {
				commPort.StopBits = StopBits.One;
			}
			else if (stopbits > 1.8) {
				commPort.StopBits = StopBits.Two;
			}
			else {
				commPort.StopBits = StopBits.OnePointFive;
			}

			if (parity[0] == 'N') {
				commPort.Parity = Parity.None;
			}
			else if (parity[0] == 'S') {
				commPort.Parity = Parity.Space;
			}
			else if (parity[0] == 'M') {
				commPort.Parity = Parity.Mark;
			}
			else if (parity[0] == 'E') {
				commPort.Parity = Parity.Even;
			}
			else if (parity[0] == 'O') {
				commPort.Parity = Parity.Odd;
			}
			else {
				commPort.Parity = Parity.None;
			}

			commPort.Handshake = Handshake.None;
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
		private void NotifyMessageReady(MessageLevel level, string msg) {
			lock(this) {
				// call base class method
				// (see class AsyncOperation)
				FireAsync(MessageEvent, level, msg);
			}
		}

		private void NotifyDataReady() {
							
			if (DataReadyEvent != null) {
				lock(this) {
					FireAsync(DataReadyEvent);
				}
			}
			
		}

		protected virtual string getDebugLine() {
			string line = DateTime.Now.ToString();
			Thread.Sleep(100);
			return line;
		}
		

		public string DeviceName {
			get {
				lock(this) {return _instrumentName;}
			}
			set {
				lock(this) {_instrumentName = value;}
			}
		}

	}	// end of CommPortWorkerThread class


	/// <summary>
	/// This class packages the data that we want to exchange between the
	/// user interface thread and the worker thread.
	/// </summary>
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

	}

}
