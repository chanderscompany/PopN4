using System;
using System.Threading;
using DBComm;

namespace DACarter.Utilities
{
	/// <summary>
	/// 
	/// </summary>
	public class CommPort : DBComm.RS232
	{
		public CommPort()
		{
			// 
			// TODO: Add constructor logic here
			//
		}

		private const int BUFSIZE = 4096;
		private char[] _buffer = new char[BUFSIZE];

		public void SetUp(
			string portname,
			int baudrate,
			int databits,
			double stopbits,
			string parity) {

			/*
			commPort.CommPort = 4;
			commPort.BaudRate = RS232.BaudRates.Baud9600;
			commPort.DataSize = RS232.DataSizes.Size8;
			commPort.Handshaking = RS232.Handshakes.None;
			commPort.StopBit = RS232.StopBits.Bit1;
			commPort.Parity = RS232.Parities.None;
			commPort.InputMode = RS232.InputModes.Text;
			*/

			this.CommPort = Int32.Parse(portname.Substring(3,1));

			this.Settings = baudrate.ToString() + ", "
							+ parity[0].ToString() + ", " 
							+ databits.ToString() + ", "
							+ stopbits.ToString();


			this.Handshaking = RS232.Handshakes.None;

			this.InputMode = RS232.InputModes.Text;

			/*
			string baudName = Enum.GetName(typeof(RS232.BaudRates),baudrate);
			if (baudName == null) {
				throw new ApplicationException("Unsupported baudrate");
			}
			this.BaudRate = (RS232.BaudRates)Enum.Parse(typeof(RS232.BaudRates),baudName);
	
			//commPort.DTREnable = true;
			//commPort.RTSEnable = false;
		
			//commPort.Encoding = new System.Text.ASCIIEncoding();
					
			string databitsName = Enum.GetName(typeof(RS232.DataSizes),databits);
			if (databitsName == null) {
				throw new ApplicationException("Unsupported data bits");
			}
			this.DataSize = (RS232.DataSizes)Enum.Parse(typeof(RS232.DataSizes),databitsName);

			
			if (stopbits < 1.2) {
				this.StopBit = RS232.StopBits.Bit1;
			}
			else if (stopbits > 1.8) {
				this.StopBit = RS232.StopBits.Bit2;
			}
			else {
				this.StopBit = RS232.StopBits.Bit1_5;
			}
			

			if (parity[0] == 'N') {
				this.Parity = RS232.Parities.None;
			}
			else if (parity[0] == 'S') {
				this.Parity = RS232.Parities.Space;
			}
			else if (parity[0] == 'M') {
				this.Parity = RS232.Parities.Mark;
			}
			else if (parity[0] == 'E') {
				this.Parity = RS232.Parities.Even;
			}
			else if (parity[0] == 'O') {
				this.Parity = RS232.Parities.Odd;
			}
			else {
				this.Parity = RS232.Parities.None;
			}
			*/
		}

		/// <summary>
		/// Reads and returns a string of characters from the COM port
		/// until the string 'prompt' is read.
		/// Will throw timeout exception if no characters received for
		///   'waitTimeout' millisec.
		/// </summary>
		/// <param name="prompt"></param>
		/// <param name="waitTimeout"></param>
		/// <returns></returns>
		public string WaitForInput( string prompt, int waitTimeout, bool flushBuffer ) {
			int match=0, chcnt=0;
			string inputChar;
			string inputString;
			int bufCount = 0;
			InputLength = 1;
			//int waitTimeout = 5000;	// msec

			int prevTO = Timeout;

			chcnt = prompt.Length;

			DateTime startClock = DateTime.Now;
			bool InputReceived = true;

			while( match < chcnt ) {
				Timeout = waitTimeout;
				// this can throw an exception:
				inputChar = Input ;
				Timeout = prevTO;

				/*
				try {
					data = Input ;
				}
				catch (Exception e) {
					return -1;
				}
				*/
				if (inputChar.Length == 0) {
					if (InputReceived) {
						// first time nothing received,
						// start timeout clock
						startClock = DateTime.Now;
						InputReceived = false;
					}
					// have we been waiting too long?
					TimeSpan waitTime = DateTime.Now - startClock;
					if (waitTime.TotalMilliseconds > waitTimeout) {
						throw new IOTimeoutException("Time out in WaitForInput");
					}
					Thread.Sleep(1);
					continue;
				}

				// have received a character;
				// store it and check for match
				InputReceived = true;
				if (bufCount < BUFSIZE) {
					_buffer[bufCount++] = inputChar[0];
				}
				else {
					throw new ApplicationException("WaitForInput buffer full error"); // buffer full
				}

				if ((inputChar[0] > 0x7e) || (inputChar[0] == 0)) {
					match = 0;
					continue;
				}
				if (prompt[match] == inputChar[0])
					match++;
				else
					match = 0;
			}

			// return all characters read
			inputString = new String(_buffer).Substring(0,bufCount);
			if (flushBuffer) {
				Thread.Sleep(200);
				string flush = Read(0);
				inputString += flush;
			}
			return(inputString);
		}

		/// <summary>
		/// Reads in and returns a string of characters from the COM port
		/// that begins with 'beginChar' and ends with 'endChar'.
		/// If beginChar is the null character, start reading immediately.
		/// Will throw timeout exception if no characters received for
		///   'timeout' millisec.
		/// </summary>
		/// <param name="beginChar"></param>
		/// <param name="endChar"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		private string ReadBlock(char beginChar, char endChar, int timeout) {

			throw new NotImplementedException("Use ReadCommBlock in ComPortThread2");

			int prevTO = Timeout;
			Timeout = timeout; // I do not think this affects anything

			ReceiveThreshold = 0;	// turn off receive events

			bool fillingBuffer = false;

			if (beginChar == (char)0) {
				fillingBuffer = true;
			}

			string data = "qwerty";

			int count = 0;

			DateTime startClock = DateTime.Now;
			bool InputReceived = true;

			// read one char at a time
			InputLength = 1;
			do {
				// read char
				data = Input;
				// if nothing there, wait and try again
				if (data.Length == 0) {
					if (InputReceived) {
						// first time nothing received,
						// start timeout clock
						startClock = DateTime.Now;
						InputReceived = false;
					}
					// have we been waiting too long?
					TimeSpan waitTime = DateTime.Now - startClock;
					if (waitTime.TotalMilliseconds > timeout) {
						throw new IOTimeoutException("Time out in WaitForInput");
					}
					Thread.Sleep(1);
					continue;
				}
 
				InputReceived = true;

				// check for begin character
				if (!fillingBuffer) {
					if (data[0] == beginChar) {
						fillingBuffer = true;
					}
				}
				// save the char in buffer
				if (fillingBuffer) {
					if (count < BUFSIZE) {
						_buffer[count++] = data[0];
					}
					else {
						Timeout = prevTO;
						throw new ApplicationException("ReadBlock buffer full error"); // buffer full
					}
				}
				// if end character, quit
				if (data[0] == endChar) {
					break;
				}
			} while (true);

			Timeout = prevTO;
			data = new String(_buffer).Substring(0,count);
			return data;
		}

		/// <summary>
		/// Just an alias for the base class's Output method.
		/// This is a more intuitive name.
		/// </summary>
		/// <param name="ss"></param>
		public void Write(string ss) {
			Output(ss);
		}

		/// <summary>
		/// Read nChars characters from the COM port.
		/// If less than nChars are in the buffer, returns buffer contents.
		/// If nChars==0, read entire buffer.
		/// </summary>
		/// <param name="nChars"></param>
		/// <returns></returns>
		public string Read(int nChars) {
			InputLength = nChars;
			return Input;
		}
	}


}
