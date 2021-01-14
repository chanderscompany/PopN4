using System;
//using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Forms;
using System.Threading;

using LibUsbDotNet;
using LibUsbDotNet.Main;

using DACarter.Utilities;
using DACarter.PopUtilities;


namespace DACarter.NOAA.Hardware {

	/// <summary>
	/// Class to access the Analog Devices AD9959 Evaluation Board
	///		via USB with the board jumpered to "PC" mode.
	///	Uses LibUsbDotNet library
	/// http://sourceforge.net/projects/libusbdotnet/ 
	/// Must have LibUsbDotNet.dll referenced
	///   Probably must install LibUsb-win32 first
	///	  run inf-wizard to create *.inf file for specific device
	///	  then install *.inf file
	///	
	/// </summary>
    public class AD9959EvalBd {

		#region Private Fields
		//
		//private bool mEventsEnabled;
		private bool mvarEnabled;
		private bool mvarAutoIOUpdate;
		private bool mvarAutoCSBMode;
		private string mvarDDSType;
		private spiIOMode mvarSPI_IOMode;
		//private bool mvarSPI_LSB_First;

        PopParameters _param;

        private double _ippUs;
		private double _refClockMHz;
		private double _sysClockMHz;
		private double _syncClockMHz;
		private double _deltaTGranularityUsec;
		private double _deltaFGranularityHz;
		private int _clockMultiplier;
		private double _syncPeriodUsec;

		private bool _firstCallToSerialRead = true;

		private uint _msgTime = 6000;

		private short[] evbBitVals;
		private short[] fx2PortVals;
		private short[] RegLength;
		private string[] sRegMapValsNew;
		private string[] sRegMapVals;
		private string[] sDefaultRegMapVals;
		private string [,] sDefChnlRegVals;
		private string [,] sChnlRegVals;
		private string [,] sChnlRegValsNew;

		// frequency sweep parameters
		// calculated:
		private double[] _startFreqMHz, _endFreqMHz, _deltaFreqMHz, _deltaTimeUsec;
		// specified:
		private double[] _centerFreqMHz;

        private bool[] _usingThisChannel;

		private UsbDevice _usbDevice;
		private UsbDeviceFinder MyUsbFinder;
		private string _deviceName;
		private UsbEndpointReader UsbReaderEP1, UsbReaderDDS;
		private UsbEndpointWriter UsbWriterEP1, UsbWriterDDS;
	    //Stores the value of the Reset Pin before it was changed with USBWritePortBuffVal
		private adiBitValues _oldReset;
		//
		#endregion private fields

		#region Constants and Enums
		//
		private int _vendorID = 0x456;
		private int _productID = 0xEE07;

		//Binary Values
		public enum adiBitValues {
			abvHigh = 1,
			abvLow = 0
		}

		public enum spiIOMode {
			spi2Wire = 0,
			spi3Wire = 1,
			spi2BitSerial = 2,
			spi4BitSerial = 3
		}

		//EZUSB-FX2 Port Enum
		public enum fx2GPIO {
			fx2_PortD = 0,
			fx2_PortA = 1,
			fx2_CTLLines = 2,
			fx2_PortB = 3,
			fx2_PortC = 4,
			fx2_PortE = 5
		}

		//Registermaps - Used to specify which register map is being
		//accessed by sSetRegMapVal and sGetRegMapVal
		public enum evb9959_RegMaps {
			rm9959_NewRegMapVals = 0,
			rm9959_CurRegMapVals = 1,
			rm9959_BothRegMapVals = 2
		}

		//Bit Number Constants for the signals on the
		//FX2s PortA's Pin
		const short bnUSB_USB_Status = 0;
		const short bnUSB_CSB = 1;
		const short bnUSB_RESET = 2;
		const short bnUSB_PWRDWN = 3;
		const short bnUSB_CLKMDSEL = 4;
		const short bnUSB_PA5 = 5;
		const short bnUSB_PA6 = 6;
		const short bnUSB_PA7 = 7;

		//Bit Number Constants for the signals on the
		//FX2s PortB's Pin
		const short bnUSB_SDIO_0 = 0; //SDI in 1-Bit 3-Wire I/O mode
		const short bnUSB_SDIO_1 = 1;
		const short bnUSB_SDIO_2 = 2; //SDO in 1-Bit 3-Wire I/O Mode
		const short bnUSB_SDIO_3 = 3; //SyncIO in 1 and 2 Bit I/O modes
		const short bnUSB_PB4 = 4;
		const short bnUSB_PB5 = 5;
		const short bnUSB_PB6 = 6;
		const short bnUSB_PB7 = 7;

		//Bit Number Constants for the signals on the
		//FX2s PortD's Pin
		const short  bnUSB_P1 = 0;
		const short bnUSB_P2 = 1;
		const short bnUSB_P3 = 2;
		const short bnUSB_P4 = 3;
		const short bnUSB_IOUpdate = 4;
		const short bnUSB_RURD0 = 5;
		const short bnUSB_RURD1 = 6;
		const short bnUSB_RURD2 = 7;

		//Bit Number constants for the signals on the
		//FX2s CTL lines
		const short bnUSB_SCLK = 0;
		const short bnUSB_CTL1 = 1;
		const short bnUSB_CTL2 = 2;

		//
		#endregion	Constants and Enums	
		
		# region Public Properties
		//

		public bool AutoCSBMode {
			get {
				return mvarAutoCSBMode;
			}
			set {
				mvarAutoCSBMode = value;
				if (mvarAutoCSBMode == true) {
					SetAutoCSBMode(1);
				} else {
					SetAutoCSBMode(0);
				}
			}
		}

		public bool AutoIOUpdate {
			get {
				return mvarAutoIOUpdate;
			}
			set { 
				mvarAutoIOUpdate = value;
			}
		}


		public int VendorID {
			get { return _vendorID; }
		}

		public int ProductID {
			get { return _productID; }
		}

		public string DeviceName {
			get { return _deviceName; }
			set {_deviceName = value;}
		}

		public string ChipID {
			get { return mvarDDSType; }
			set { mvarDDSType = value; }
		}

		public double RefClockMHz {
			get { return _refClockMHz; }
			//set { _refClockMHz = value; }
		}

		public double SysClockMHz {
			get { return _sysClockMHz; }
		}

		public double DeltaTGranularityUsec {
			get { return _deltaTGranularityUsec; }
		}

		public double DeltaFGranularityHz {
			get { return _deltaFGranularityHz; }
		}

		// sweep start freq for channel 0
		public double SweepStartFreq0MHz {
			get {
				// debug check: should be identical:
				double freq = GetFreq0(0);
				return _startFreqMHz[0];
			}
		}

		// sweep end freq for channel 0
		public double SweepEndFreq0MHz {
			get {
				double freq = GetFreq0(0);
				return _endFreqMHz[0];
			}
		}

		public double SweepCenterFreq0MHz {
			get { return _centerFreqMHz[0]; }
		}

		public double SweepCenterFreq1MHz {
			get { return _centerFreqMHz[1]; }
		}

		public double SweepDeltaFreq0MHz {
			get {
				double freq = GetFreq0(0);
				return _deltaFreqMHz[0];
			}
		}

		public double SweepDeltaTime0Usec {
			get {
				return _deltaTimeUsec[0];
			}
		}

		// sweep start freq for channel 1
		public double SweepStartFreq1MHz {
			get {
				double freq = GetFreq1(1);
				return _startFreqMHz[1];
			}
		}

		// sweep end freq for channel 1
		public double SweepEndFreq1MHz {
			get {
				double freq = GetFreq1(1);
				return _endFreqMHz[1];
			}
		}

		public double SweepDeltaFreq1MHz {
			get {
				double freq = GetFreq1(1);
				return _deltaFreqMHz[1];
			}
		}

		public double SweepDeltaTime1Usec {
			get {
				return _deltaTimeUsec[1];
			}
		}


		//
		# endregion Properties

		#region Constructors and Initialzers
		//
		////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Constructor
		/// </summary>
		/// 

		// default constructor only used to force RESET from anywhere
		private AD9959EvalBd() {
		}

		// make this the required constructor
		//	so that clocks are defined before any registers are set.
		//	if initDevice arg is true then
		//		USB and DDS are automatically initialized and
		//		device must be connected and turned on.
		//	if initDevice arg is false, then
		//		InitDevice() must be called before using the DDS board.
		//
        public AD9959EvalBd(double refClockMHz, int clockMultiplier, double ipp, bool initDevice) :
                        this(refClockMHz, clockMultiplier, ipp, initDevice, null) {
        }

        public AD9959EvalBd(PopParameters param, bool initDevice) :
            this(param.SystemPar.RadarPar.FmCwParSet[0].DDSRefClockMHz,
                        param.SystemPar.RadarPar.FmCwParSet[0].DDSMultiplier,
                        param.SystemPar.RadarPar.FmCwParSet[0].IppMicroSec,
                        initDevice,
                        param) {
        }


		public AD9959EvalBd (double refClockMHz, int clockMultiplier, double ippUs, bool initDevice, PopParameters param) {

            _param = param;

            _usingThisChannel = new bool[4];
            _usingThisChannel[0] = true;
            _usingThisChannel[1] = true;
            _usingThisChannel[2] = false;
            _usingThisChannel[3] = false;
            if (param != null) {
                double freqHz3 = param.SystemPar.RadarPar.FmCwParSet[0].DDS3FreqStartHz;
                if (freqHz3 > 0.0) {
                    _usingThisChannel[2] = true;
                }
                double freqHz4 = param.SystemPar.RadarPar.FmCwParSet[0].DDS4FreqStartHz;
                if (freqHz4 > 0.0) {
                    _usingThisChannel[3] = true;
                }
            }

			_firstCallToSerialRead = true;
			mvarAutoIOUpdate = true;
			mvarEnabled = true;
			mvarAutoIOUpdate = true;
			mvarAutoCSBMode = true;

            // POPREV: rev 3.11 do all 4 channels now
			_startFreqMHz = new double[4];
			_endFreqMHz = new double[4];
			_deltaFreqMHz = new double[4];
			_deltaTimeUsec = new double[4];
			_centerFreqMHz = new double[4];


			if (clockMultiplier == 0) {
				// both 0 and 1 are valid values to disable multiplier
				// but setting to 1 helps us keep track of what we are 
				// actually multiplying by.
				clockMultiplier = 1;
			}
			_clockMultiplier = clockMultiplier;  // store multiplier in this instance of this class

            _ippUs = ippUs;

			_refClockMHz = refClockMHz;
			_sysClockMHz = _refClockMHz * clockMultiplier;
			_syncClockMHz = _sysClockMHz / 4.0;
			if (_syncClockMHz != 0.0) {
				_syncPeriodUsec = 1.0 / _syncClockMHz;
			}

			//Initialize the bitvals array
			evbBitVals = new short[8];
			int dum = 1;
			for (int i = 0; i < 8; i++) {
				evbBitVals[i] = (short)dum;
				dum = dum << 1;
			}

			//Setup the evaluation board buffers values, but call InitIOBuffs once
			//the device has been selected
			fx2PortVals = new short[6];
			for (int i = 0; i < 6; i++) {
				fx2PortVals[i] = 0;
			}


			//Setup the register lengths
			RegLength = new short[28];
			RegLength[0] = 8;
			RegLength[1] = 24;
			RegLength[2] = 16;
			RegLength[3] = 24;
			RegLength[4] = 32;
			RegLength[5] = 16;
			RegLength[6] = 24;
			RegLength[7] = 16;
			RegLength[8] = 32;
			RegLength[9] = 32;
			RegLength[10] = 32;
			RegLength[11] = 32;
			RegLength[12] = 32;
			RegLength[13] = 32;
			RegLength[14] = 32;
			RegLength[15] = 32;
			RegLength[16] = 32;
			RegLength[17] = 32;
			RegLength[18] = 32;
			RegLength[19] = 32;
			RegLength[20] = 32;
			RegLength[21] = 32;
			RegLength[22] = 32;
			RegLength[23] = 32;
			RegLength[24] = 32;
			RegLength[25] = 16;
			RegLength[26] = 32;
			RegLength[27] = 32;

			//Initialize the register map buffers
			InitRegMap();

			if (initDevice) {
				InitDevice();
			}

		}  // end constructor

        /// <summary>
        /// GetRegisterValues
        ///     Public access to values sent to DDS registers.
        ///     Values are represented as 32 bit integer values
        /// </summary>
        /// <returns>
        /// Returns 2-D array of integer values.
        ///     First index is channel, second is register.
        ///     register 1 is start freq
        ///     register 4 is sweep time delta
        ///     register 5 is sweep freq delta
        ///     register 7 is sweep end freq
        /// </returns>
        public int[,] GetRegisterValues() {
            if (sChnlRegValsNew == null) {
                return null;
            }
            else {
                int nChnls = sChnlRegValsNew.GetLength(0);
                int nRegs = sChnlRegValsNew.GetLength(1);
                int[,] copy = new int[nChnls, nRegs];
                for (int i = 0; i < nChnls; i++) {
                    for (int j = 0; j < nRegs; j++) {
                        copy[i, j] = BinString2Int(sChnlRegValsNew[i, j]);
                    }
                }
                return copy;
            }
        }

		/// <summary>
		/// Once clock is specified,
		///		setup and initialize the USB and DDS
		/// </summary>
		public void InitDevice() {

			//SetPCMode(true);

			InitUSB();

			//mvarSPI_LSB_First = false;

			//ResetDDS();

			InitIOBuffs();

			mvarDDSType = DetectDDS();

			SetChipClock();


			_deltaTGranularityUsec = 1.0 / (SysClockMHz / 4.0);
			_deltaFGranularityHz = SysClockMHz * 1.0e6 / Math.Pow(2.0, 32.0);
		}

		/// <summary>
		/// Need to call this after resets, write tests, etc.
		/// so that reg 1 is set correctly
		/// </summary>
		private void SetChipClock() {
			ClockMultiplier = _clockMultiplier;  // this sets multiplier in reg map
			if (_sysClockMHz > 255.0) {
				VCOGainOn = true;
			}
			if (_sysClockMHz > 500.0) {
				throw new ApplicationException("SysClock must be less than 500 MHz.");
			}

			LoadChipRegisters();  // loads multiplier onto chip register
		}

        /*
		public void SetPCMode(bool turnOnPCMode) {
			SetPCMode(turnOnPCMode, null);
		}
        */

        /*
		/// <summary>
		/// NOTE: this routine is no longer used or needed.
		/// We are not switching PC mode on DDS anymore.
		/// </summary>
		/// <param name="turnOnPCMode"></param>
		/// <param name="pbxExisting"></param>
		public void SetPCMode(bool turnOnPCMode, PbxControllerCard pbxExisting) {
			throw new NotSupportedException("AD9959EvalBd.SetPCMode is obsolete.");
			MessageBox.Show("Entering setPCMode " + turnOnPCMode.ToString());
			bool useExistingPbx = true;
			PbxControllerCard pbx = new PbxControllerCard(useExistingPbx);
			
            //if (pbx == null) {
            //    // create new access to existing pbx hardware
            //    pbx = new PbxControllerCard(useExistingPbx);
            //}
			
			// can only change BW when pbx is not busy:
			bool pbxIsBusy = false;
			if (pbx.PbxIsBusy()) {
				MessageBox.Show("pbx is busy, stop pulses.");
				pbxIsBusy = true;
			}
			pbx.StopPulses();
			// PCMode on AD9959 Eval Board is active low.
			if (turnOnPCMode) {
				pbx.PbxWriteBW(0);
				//MessageBox.Show("Just called PbxWriteBW(0) on new pbx");
			}
			else {
				pbx.PbxWriteBW(1);
				//MessageBox.Show("Just called PbxWriteBW(1) on new pbx");
			}
			if (pbxIsBusy) {
				MessageBox.Show("Restart pulses...");
				pbx.StartPulses();
			}
		}
        */

		/////////////////////////////////////////////////////////////////////
		/// <summary>
		/// InitUSB()
		/// </summary>
		private void InitUSB() {

			//UsbDevice MyUsbDevice;
			bool hasDriver = LibUsbDotNet.LibUsb.LibUsbDevice.HasLibUsbDriver;   // ver 2.2.5 dll
			//bool hasDriver = UsbGlobals.HasLibUsbDriver;   // ver 2.0.3 dll

			if (!hasDriver) {
				throw new ApplicationException("Lib USB Driver not found for AD9959.");
			}

			// A bug in LibUsb extends the sign of the product ID.
			// So we do this here, because assigning hex constant above
			//  does not sign extend
			int pid = (int)(short)ProductID;
			int vid = (int)(short)VendorID;
			//MyUsbFinder = new UsbDeviceFinder(vid, pid);
			MyUsbFinder = new UsbDeviceFinder(0x456, 0xEE07);
			//
			// To find all attached devices:
			//UsbRegDeviceList mDevList = UsbGlobals.AllDevices;
			// Find particular device in list:
			//UsbRegistry regDevice =  mDevList.Find(MyUsbFinder);
			//regDevice.Open(out _usbDevice);
			//

			if ((MyUsbFinder == null)) {
				throw new ApplicationException("MyUsbFinder failed ") ;
			}

			_usbDevice = UsbDevice.OpenUsbDevice(MyUsbFinder);   // ver 2.2.5 dll
			//_usbDevice = LibUsbDotNet.LibUsb.LibUsbDevice.OpenUsbDevice(MyUsbFinder);   // ver 2.2.5 dll
			//_usbDevice = UsbGlobals.OpenUsbDevice(MyUsbFinder);   // ver 2.0.3 dll

			if (_usbDevice == null) {
				throw new ApplicationException("Cannot open USB device for AD9959.");
			}

			_deviceName = _usbDevice.Info.ProductString;

			// If this is a "whole" usb device (libusb-win32, linux libusb)
			// it will have an IUsbDevice interface. If not (WinUSB) the 
			// variable will be null indicating this is an interface of a 
			// device.
			IUsbDevice wholeUsbDevice = _usbDevice as IUsbDevice;
			if (!ReferenceEquals(wholeUsbDevice, null)) {
				// This is a "whole" USB device. Before it can be used, 
				// the desired configuration and interface must be selected.
				//MessageBox.Show("This is whole device.");

				// Select config #1
				wholeUsbDevice.SetConfiguration(1);

				// Claim interface #0.
				wholeUsbDevice.ClaimInterface(0);
			}


			// MyUsbDevice.Configs shows all USB configurations of the device;
			//	  There is only 1 configuration for this board.
			//	  SetConfiguration is sent "1", not "0", for index to use.
			//ReadOnlyCollection<UsbConfigInfo> configs = MyUsbDevice.Configs;
			//_usbDevice.SetConfiguration(1);   // ver 2.0.3 dll
			//_usbDevice.???     // ver 2.2.5 dll

			UsbReaderEP1 = _usbDevice.OpenEndpointReader(ReadEndpointID.Ep01);	// USB endpoint 1 to read USB chip registers
			UsbWriterEP1 = _usbDevice.OpenEndpointWriter(WriteEndpointID.Ep01); // USB endpoint 1 to write USB chip registers
			UsbReaderDDS = _usbDevice.OpenEndpointReader(ReadEndpointID.Ep08);  // USB endpoint 8 to read DDS registers
			UsbWriterDDS = _usbDevice.OpenEndpointWriter(WriteEndpointID.Ep04); // USB endpoint 4 to write DDS registers
		}

		////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		private void InitRegMap() {

			//Loop through all of the registers and assign 0 value to them
			sRegMapVals = new string[28];
			sDefaultRegMapVals = new string[28];
			sRegMapValsNew = new string[28];
            // POPREV: rev 3.11 using all 4 channels now:
			sDefChnlRegVals = new string[4, 22];
			sChnlRegVals = new string[4, 22];
			sChnlRegValsNew = new string[4, 22];
			string sDummy;
			for (int cntr = 0; cntr < sRegMapVals.Length; cntr++) {
				//Create a binary string with a value of 0
				sDummy = new String('0', RegLength[cntr]);
				sDefaultRegMapVals[cntr] = sDummy;

				// Initialize the channel register values
				if (cntr >= 3 && cntr <= 24) {
					sDefChnlRegVals[0, cntr - 3] = sDummy;
					sDefChnlRegVals[1, cntr - 3] = sDummy;
                    // POPREV: rev 3.11 using all 4 channels now:
					sDefChnlRegVals[2, cntr - 3] = sDummy;
					sDefChnlRegVals[3, cntr - 3] = sDummy;
				}
			}

			// Set the default values for registers that don't
			// have all zeros in them
			sDefaultRegMapVals[0] = Int2BinString(0xf0, 8);
			sDefaultRegMapVals[3] = Int2BinString(0x302, 24);
			sDefChnlRegVals[0, 0] = Int2BinString(0x302, 24);
			sDefChnlRegVals[1, 0] = Int2BinString(0x302, 24);
            // POPREV: rev 3.11 resetting channels 3-4 now:
			sDefChnlRegVals[2, 0] = Int2BinString(0x302, 24);
			sDefChnlRegVals[3, 0] = Int2BinString(0x302, 24);
			//Loop through all of the registers and assign 0 value to them
			for (int i = 0; i < sRegMapVals.Length; i++) {
				sRegMapValsNew[i] = sDefaultRegMapVals[i];
				sRegMapVals[i] = sDefaultRegMapVals[i];
			}

			//Initialize the channel registers
			for (int i = 0; i <= sChnlRegVals.GetUpperBound(0); i++) {
				for (int j = 0; j <= sChnlRegVals.GetUpperBound(1); j++) {
					//Initialize the channel register current values
					sChnlRegVals[i, j] = sDefChnlRegVals[i, j];
					//Initialize the channel register new values
					sChnlRegValsNew[i, j] = sDefChnlRegVals[i, j];
				}
			}
		}

		/////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		public void InitIOBuffs() {
			//Get the current port values from USB chip
			//	and store them in the IO buffers
			fx2PortVals[0] = GetPortDValue();
			fx2PortVals[1] = GetPortAValue();
			fx2PortVals[2] = GetCtlValues(); 
			fx2PortVals[3] = GetPortBValue();
			fx2PortVals[4] = 0x00;
			fx2PortVals[5] = 0x00;
		}

		/////////////////////////////////////////////////////////////////////
		/// <summary>
		/// DetectDDS()
		/// </summary>
		/// <returns></returns>
		public string DetectDDS() {

			string sRegVal = "0";
			string[] sTestVal = new string[4];
			sTestVal[3] = "000000000000001100000010";
			sTestVal[0] = "11110000";
			string DDSType = "NONE";

			//Reset the DDS
			ResetDDS();

			// read a register and check for default value
			// to verify that we are communicating with DDS.
			//
			// dac note:
			// The repeat tries here were required since
			//	USBSerialRead failed the first time that it
			//	was called after a device power-up.
			//
			// USBSerialRead1Bit has since been modified
			//	to handle this issue internally.
			// So the retry loop here may no longer be needed.
			//
			int reg = 3;		// choose reg 0 or 3 to test
			int maxTries = 10;
			int iTry;
			bool retry;

			for (iTry = 0; iTry < maxTries; iTry++) {
				retry = false;
				try {
					sRegVal = USBSerialRead(reg, 1);
				}
				catch {
					retry = true;
				}
				if (!retry) {
					break;
				}
			}

			if (iTry != 0) {
				if (iTry < 3) {
					// iTry normally is 2
					MessageBoxEx.Show("Number of retries on first USBSerialRead = " + iTry.ToString(),
											"DetectDDS",
											MessageBoxButtons.OK,
											MessageBoxIcon.Information,
											2000);
				}
				else if (iTry == maxTries) {
					string errMsg = "Number of retries on first USBSerialRead = " + iTry.ToString();
					MessageBoxEx.Show(errMsg,
											"DetectDDS",
											MessageBoxButtons.OK,
											MessageBoxIcon.Error,
											6000);
					throw new ApplicationException(errMsg);
				}
				else {
					MessageBoxEx.Show("Number of retries on first USBSerialRead = " + iTry.ToString(),
											"DetectDDS",
											MessageBoxButtons.OK,
											MessageBoxIcon.Warning,
											6000);
				}
				
			}

			//Read the value of register 0 or 3
			sRegVal = USBSerialRead(reg, 1);

			//Test the value returned with the value
			if (sRegVal != sTestVal[reg]) {
				DDSType = "None";
				throw new ApplicationException("Read/write register test did not detect DDS.");
			}
			else {
				DDSType = GetProductType();
			}

			//Get the chip ID
			mvarDDSType = DDSType;
			return DDSType;

		}  // end DetectDDS()

		public bool ReadTest() {
			int iTry;
			int maxTries = 500;
			int nErrors = 0;
			string testVal = "000000000000001100000010";
			string regVal;

			ResetDDS();

			for (iTry = 0; iTry < maxTries; iTry++) {
				try {
					regVal = USBSerialRead(3, 1);
					if (regVal != testVal) {
						nErrors++;
					}
				}
				catch {
					nErrors++;
					if (nErrors > 5) {
						break;
					}

				}
			}

			if (nErrors == 0) {
				MessageBoxEx.Show("Number of errors USBSerialRead = " + nErrors.ToString() + " / " + iTry.ToString(),
										"AD9959EvalBd",
										MessageBoxButtons.OK,
										MessageBoxIcon.Information,
										1500);
			}
			else {
				if (iTry != maxTries) {
					iTry++;
				}
				MessageBoxEx.Show("Number of errors USBSerialRead = " + nErrors.ToString() + " / " + iTry.ToString(),
										"AD9959EvalBd",
										MessageBoxButtons.OK,
										MessageBoxIcon.Error,
										6000);
			}

			if (nErrors > 0) {
				return false;
			}
			else {
				return true;
			}
		}

		public bool WriteTest() {
			int iTry;
			int maxTries = 500;
			int nErrors = 0;
			string testVal = "00010101010101010101010101010101";
			string regVal;
			int reg = 0x18;

			ResetDDS();

			for (iTry = 0; iTry < maxTries; iTry++) {
				try {
					USBSerialLoad(reg, testVal);
					regVal = USBSerialRead(reg, 1);
					if (regVal != testVal) {
						nErrors++;
					}
				}
				catch {
					nErrors++;
					if (nErrors > 5) {
						break;
					}

				}
			}

			if (nErrors == 0) {
				MessageBoxEx.Show("Number of errors USBSerialWrite = " + nErrors.ToString() + " / " + iTry.ToString(),
										"AD9959EvalBd",
										MessageBoxButtons.OK,
										MessageBoxIcon.Information,
										1500);
			}
			else {
				if (iTry != maxTries) {
					iTry++;
				}
				MessageBoxEx.Show("Number of errors USBSerialWrite = " + nErrors.ToString() + " / " + iTry.ToString(),
										"AD9959EvalBd",
										MessageBoxButtons.OK,
										MessageBoxIcon.Error,
										6000);
			}

			if (nErrors > 0) {
				return false;
			}
			else {
				return true;
			}
		}

		//
		#endregion Constructors and Initializers

        #region Static Methods

        // POPREV:  static methods created for POPN3 rev 3.14
        //  The goal is to create a concise method for calculating
        //  DDS register values regardless of being attached to DDS hardware.

        public struct DDSInputValues {
            public double RefClockMHz;
            public int RefClockMultiplier;
            public double CenterFreqMHz;
            public double SweepRateHzUsec;
            public double GateOffset;
            public double FreqOffsetHz;
            public int DeltaTPeriods;
            public int NSamples;
            public double SampleSpacingNs;
            public double SampleDelayNs;
            public double SystemDelayNs;
            public double SweepBeyondSamplesNs;
            public bool IsSpecifyingGateOffset;
        }

        public struct DDSCalculatedValues {
            public double SystemClockMHz;
            public double SyncClockMHz;
            public double SyncPeriodNs;
            public double SweepRateHzUsec;
            public double DeltaFreqHz;
            public int DeltaFreqRegValue;
            public int DeltaTRegValue;     // same as DDSInputValues.DeltaTPeriods
            public double DeltaTNsec;
            public double StartFreq1MHz;
            public int StartFreq1RegValue;
            public double EndFreq1MHz;
            public int EndFreq1RegValue;
            public double StartFreq2MHz;
            public int StartFreq2RegValue;
            public double EndFreq2MHz;
            public int EndFreq2RegValue;
            public double OffsetFreqMHz;
            public double OffsetGate;
            public int SweepDurationRegValue;
            public double SweepDurationUsec;
        }

        public static void CalculateDDSValues(DDSInputValues DDSin, out DDSCalculatedValues DDSout) {
            DDSout = new DDSCalculatedValues();
            if (DDSin.RefClockMultiplier <= 0) {
                DDSin.RefClockMultiplier = 1;
            }
            DDSout.SystemClockMHz = DDSin.RefClockMHz * DDSin.RefClockMultiplier;
            DDSout.SyncClockMHz = DDSout.SystemClockMHz / 4.0;
            DDSout.SyncPeriodNs = 1.0e3 / DDSout.SyncClockMHz;

            int nSamp = DDSin.NSamples;
            double delayNs = DDSin.SampleDelayNs;
            double spacingNs = DDSin.SampleSpacingNs;
            double deltaTimeUs = DDSin.DeltaTPeriods * DDSout.SyncPeriodNs / 1000.0;
            DDSout.DeltaTNsec = deltaTimeUs * 1000.0;
            DDSout.DeltaTRegValue = DDSin.DeltaTPeriods;
            double deltaFreqHz = DDSin.SweepRateHzUsec * deltaTimeUs;
            double sweepBeyondSamplesNs = DDSin.SweepBeyondSamplesNs;

            double txNs = 1000.0;

            double deltaFreqMHzActual;
            FreqMHzToRegister(deltaFreqHz / 1.0e6, DDSout.SystemClockMHz, out deltaFreqMHzActual, out DDSout.DeltaFreqRegValue);
            DDSout.DeltaFreqHz = deltaFreqMHzActual * 1.0e6;
            DDSout.SweepRateHzUsec = DDSout.DeltaFreqHz / deltaTimeUs;

            double timeToCenterFreqUsec = (delayNs + txNs + (nSamp - 1) * spacingNs / 2.0) / 1.0e3;
            double startFreqMHz = DDSin.CenterFreqMHz - DDSout.SweepRateHzUsec * timeToCenterFreqUsec / 1.0e6;
            FreqMHzToRegister(startFreqMHz, DDSout.SystemClockMHz, out DDSout.StartFreq1MHz, out DDSout.StartFreq1RegValue);

            double sweepTimeUsec = (delayNs + txNs + ((nSamp - 1) * spacingNs) + sweepBeyondSamplesNs) / 1.0e3;
            DDSout.SweepDurationRegValue = (int)Math.Floor(sweepTimeUsec / deltaTimeUs + 0.5);
            DDSout.SweepDurationUsec = DDSout.SweepDurationRegValue * DDSout.DeltaTNsec / 1000.0;
            double sweepRangeMHz = DDSout.SweepDurationRegValue * DDSout.DeltaFreqHz / 1.0e6;

            DDSout.EndFreq1MHz = DDSout.StartFreq1MHz + sweepRangeMHz;
            DDSout.EndFreq1RegValue = FreqRegFromFreqMHz(DDSout.EndFreq1MHz, DDSout.SystemClockMHz);

            int offsetFreqRegValue;

            if (DDSin.IsSpecifyingGateOffset) {
                // start with offset gate and comput offset freq
                double offsetHz = OffsetFreqHz(DDSin.GateOffset,
                                               DDSin.NSamples,
                                               DDSin.SampleSpacingNs,
                                               DDSin.SystemDelayNs,
                                               DDSout.SweepRateHzUsec);
                FreqMHzToRegister(offsetHz / 1.0e6, DDSout.SystemClockMHz, out DDSout.OffsetFreqMHz, out offsetFreqRegValue);

            }
            else {
                // start with freq, compute actual offset freq
                FreqMHzToRegister(DDSin.FreqOffsetHz / 1.0e6, DDSout.SystemClockMHz, out DDSout.OffsetFreqMHz, out offsetFreqRegValue);
            }

            // then compute actual gate offset
            DDSout.OffsetGate = OffsetGate(DDSout.OffsetFreqMHz * 1.0e6,
                                     DDSin.NSamples,
                                     DDSin.SampleSpacingNs,
                                     DDSin.SystemDelayNs,
                                     DDSout.SweepRateHzUsec);

            DDSout.StartFreq2RegValue = DDSout.StartFreq1RegValue + offsetFreqRegValue;
            DDSout.EndFreq2RegValue = DDSout.EndFreq1RegValue + offsetFreqRegValue;

            DDSout.StartFreq2MHz = FreqMHzFromFreqReg(DDSout.StartFreq2RegValue, DDSout.SystemClockMHz);
            DDSout.EndFreq2MHz = FreqMHzFromFreqReg(DDSout.EndFreq2RegValue, DDSout.SystemClockMHz);


        }

        public static void FreqMHzToRegister(double freqMHz, double sysClockMHz, out double newFreqMHz, out int freqRegValue) {
            //double freqValue;
            //double scaleFactor = Math.Pow(2.0, 32.0);
            //freqValue = freqMHz * scaleFactor / sysClockMHz;
            //freqRegValue = (int)(freqValue + 0.5);
            //newFreqMHz = (double)freqRegValue * sysClockMHz / scaleFactor;
            freqRegValue = FreqRegFromFreqMHz(freqMHz, sysClockMHz);
            newFreqMHz = FreqMHzFromFreqReg(freqRegValue, sysClockMHz);
        }

        public static int FreqRegFromFreqMHz(double freqMHz, double sysClockMhz) {
            double scaleFactor = Math.Pow(2.0, 32.0);
            int regVal = (int)Math.Floor(scaleFactor * freqMHz / sysClockMhz + 0.5);
            return regVal;
        }

        public static double FreqMHzFromFreqReg(int regValue, double sysClockMhz) {
            double scaleFactor = Math.Pow(2.0, 32.0);
            double freqMHz = sysClockMhz * ((double)regValue / scaleFactor);
            return freqMHz;
        }

        public static double OffsetFreqHz(double iGate0, int npts, double sampleSpacingNs, double sysDelayNs, double sweepRateHzUsec) {

            double sampleSpacingHz = 1.0e9 / (npts * sampleSpacingNs);
            double rangeSpacingNs = 1.0e3 * sampleSpacingHz / (sweepRateHzUsec);
            double offset = sampleSpacingHz * (iGate0 - sysDelayNs / rangeSpacingNs);
            return offset;
        }

        public static double OffsetGate(double offsetHz, int npts, double sampleSpacingNs, double sysDelayNs, double sweepRateHzUsec) {
            double sampleSpacingHz = 1.0e9 / (npts * sampleSpacingNs);
            double rangeSpacingNs = 1.0e3 * sampleSpacingHz / (sweepRateHzUsec);
            double gate = offsetHz / sampleSpacingHz + sysDelayNs / rangeSpacingNs;
            return gate;
        }

        /// <summary>
        /// Assigns DDS input parameters from values in a PopParameters object
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public static DDSInputValues DDSInput(PopParameters param) {
            DDSInputValues ddsIn;
            ddsIn.CenterFreqMHz = param.SystemPar.RadarPar.FmCwParSet[0].TxSweepCenterFreqMHz;
            ddsIn.DeltaTPeriods = param.SystemPar.RadarPar.FmCwParSet[0].TxSweepTimeStepClocks;
            ddsIn.FreqOffsetHz = param.SystemPar.RadarPar.FmCwParSet[0].TxSweepOffsetHz;
            ddsIn.GateOffset = param.SystemPar.RadarPar.FmCwParSet[0].GateOffset;
            ddsIn.IsSpecifyingGateOffset = true;
            ddsIn.NSamples = param.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts;
            ddsIn.RefClockMHz = param.SystemPar.RadarPar.FmCwParSet[0].DDSRefClockMHz;
            ddsIn.RefClockMultiplier = param.SystemPar.RadarPar.FmCwParSet[0].DDSMultiplier;
            ddsIn.SampleDelayNs = param.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleDelayNs;
            ddsIn.SampleSpacingNs = param.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleSpacingNs;
            ddsIn.SweepBeyondSamplesNs = 0.0;
            //ddsIn.SweepBeyondSamplesNs = param.SystemPar.RadarPar.FmCwParSet[0].SweepBeyondSamplesNs;
            if (ddsIn.SweepBeyondSamplesNs == 0.0) {
                ddsIn.SweepBeyondSamplesNs = 10 * 1000.0;
            }
            ddsIn.SweepRateHzUsec = param.SystemPar.RadarPar.FmCwParSet[0].TxSweepRateHzUSec;
            ddsIn.SystemDelayNs = param.SystemPar.RadarPar.RxBw[0].BwDelayNs;
            return ddsIn;
        }

        /// <summary>
        /// Updates PopParameters object with calculated DDS values.
        /// Does not modify other existing parameters.
        /// </summary>
        /// <param name="ddsOut">input: DDSCalculatedValues object containing
        /// calculated DDS register values and sweep parameters derived from them</param>
        /// <param name="param">PopParamters object to be modified with DDS calculated values</param>
        public static void GetParametersFromDDS(DDSCalculatedValues ddsOut, ref PopParameters param) {
            param.SystemPar.RadarPar.FmCwParSet[0].TxSweepTimeStepClocks = ddsOut.DeltaTRegValue;
            param.SystemPar.RadarPar.FmCwParSet[0].TxSweepOffsetHz = ddsOut.OffsetFreqMHz * 1.0e6;
            param.SystemPar.RadarPar.FmCwParSet[0].GateOffset = ddsOut.OffsetGate;
            param.SystemPar.RadarPar.FmCwParSet[0].TxSweepRateHzUSec = ddsOut.SweepRateHzUsec;
        }

        /// <summary>
        /// Update DDS values in a PopParameters object and 
        ///     make them consistent with calculated DDS register values.
        /// </summary>
        /// <param name="param"></param>
        public static void UpdateDDSParameters(ref PopParameters param) {
            //DDSInputValues ddsIn = DDSInput(param);
            DDSCalculatedValues ddsOut;
            CalculateDDSValues(DDSInput(param), out ddsOut);
            GetParametersFromDDS(ddsOut, ref param);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        //

        #endregion Static Methods

        #region Bit Pattern Strings
        /// <summary>
		/// Int2BinString()
		/// Converts integer to string of '0' and '1' characters
		/// </summary>
		/// <param name="number"></param>
		/// <param name="numbits"></param>
		/// <returns></returns>
		public string Int2BinString(int number, int numbits) {
			return Int2BinString0(number, numbits);
			/*
			string binString = Convert.ToString(number, 2);
			if (binString.Length > numbits) {
				binString.Substring(binString.Length - numbits, numbits);
			}
			else if (binString.Length < numbits) {
				string s0 = new string('0', numbits - binString.Length);
				binString = s0 + binString;
			}
			return binString;
			*/
		}

		public static string Int2BinString0(int number, int numbits) {
			string binString = Convert.ToString(number, 2);
			if (binString.Length > numbits) {
				binString.Substring(binString.Length - numbits, numbits);
			}
			else if (binString.Length < numbits) {
				string s0 = new string('0', numbits - binString.Length);
				binString = s0 + binString;
			}
			return binString;
		}

		public static int BinString2Int0(string binString) {
			int num = 0;
			int unit = 1;

			char[] chars = binString.ToCharArray();
			int cnt = chars.Length;
			for (int i = cnt-1; i >= 0; i--) {
				if (chars[i] == '1') {
					num += unit;
				}
				unit = unit * 2;
			}

			return num;
		}

        public int BinString2Int(string binString) {
            return BinString2Int0(binString);
        }

		/// <summary>
		/// Converts a byte array to a string.
		/// Each byte is converted to a single char in the string.
		/// </summary>
		/// <param name="barray"></param>
		/// <returns></returns>
		private string ByteArray2Str(byte[] barray) {
			short i;
			string sStr = "";

			// Convert to a string
			for (i = 0; i < barray.Length; i++) {
				sStr = sStr + Convert.ToString(barray[i]);
			}

			return sStr;
		}

		/// <summary>
		/// Converts sStr into a byte array
		/// Each character in the string becomes a byte in the array.
		/// </summary>
		/// <param name="sStr"></param>
		/// <returns></returns>
		private byte[] Str2ByteArray(string sStr) {

			byte[] barray = new byte[sStr.Length];
						
			// Convert to a byte buffer
			for (int i = 0; i < sStr.Length; i++) {
				barray[i] = Convert.ToByte(sStr.Substring( i, 1));
			} 
			return barray;
		}

		//
		#endregion Bit Pattern Strings

		#region Set Bits on USB Ports
		//

		public adiBitValues ResetBit {
			get { return USBGetPortBitVal(fx2GPIO.fx2_PortA, bnUSB_RESET); }
			set {
				USBSetPortBitVal(fx2GPIO.fx2_PortA, bnUSB_RESET, value);
				USBWritePortBuffVal(fx2GPIO.fx2_PortA);
			}
		}

		public adiBitValues IOUpdateBit {
			get { return USBGetPortBitVal(fx2GPIO.fx2_PortD, bnUSB_IOUpdate); }
			set {
				USBSetPortBitVal(fx2GPIO.fx2_PortD, bnUSB_IOUpdate, value);
				USBWritePortBuffVal(fx2GPIO.fx2_PortD);
			}
		}

		public adiBitValues ChipSelectBit {
			get { return USBGetPortBitVal(fx2GPIO.fx2_PortA, bnUSB_CSB); }
			set {
				USBSetPortBitVal(fx2GPIO.fx2_PortA, bnUSB_CSB, value);
				USBWritePortBuffVal(fx2GPIO.fx2_PortA);
			}
		}

		public adiBitValues SClockBit {
			get { return USBGetPortBitVal(fx2GPIO.fx2_CTLLines, bnUSB_SCLK); }
			set {
				USBSetPortBitVal(fx2GPIO.fx2_CTLLines, bnUSB_SCLK, value);
				USBWritePortBuffVal(fx2GPIO.fx2_CTLLines);
			}
		}

		public adiBitValues PowerDownBit {
			get { return USBGetPortBitVal(fx2GPIO.fx2_PortA, bnUSB_PWRDWN); }
			set {
				USBSetPortBitVal(fx2GPIO.fx2_PortA, bnUSB_PWRDWN, value);
				USBWritePortBuffVal(fx2GPIO.fx2_PortA);
			}
		}

		public adiBitValues P0Bit {
			get { return USBGetPortBitVal(fx2GPIO.fx2_PortD, bnUSB_P1); }
			set {
				USBSetPortBitVal(fx2GPIO.fx2_PortD, bnUSB_P1, value); 
				USBWritePortBuffVal(fx2GPIO.fx2_PortD);
			}
		}

		public adiBitValues P1Bit {
			get { return USBGetPortBitVal(fx2GPIO.fx2_PortD, bnUSB_P2); }
			set {
				USBSetPortBitVal(fx2GPIO.fx2_PortD, bnUSB_P2, value);
				USBWritePortBuffVal(fx2GPIO.fx2_PortD);
			}
		}

		public adiBitValues P2Bit {
			get { return USBGetPortBitVal(fx2GPIO.fx2_PortD, bnUSB_P3); }
			set {
				USBSetPortBitVal(fx2GPIO.fx2_PortD, bnUSB_P3, value);
				USBWritePortBuffVal(fx2GPIO.fx2_PortD);
			}
		}

		public adiBitValues P3Bit {
			get { return USBGetPortBitVal(fx2GPIO.fx2_PortD, bnUSB_P4); }
			set {
				USBSetPortBitVal(fx2GPIO.fx2_PortD, bnUSB_P4, value);
				USBWritePortBuffVal(fx2GPIO.fx2_PortD);
			}
		}

		public adiBitValues RURD_0Bit {
			get { return USBGetPortBitVal(fx2GPIO.fx2_PortD, bnUSB_RURD0); }
			set { 
				USBSetPortBitVal(fx2GPIO.fx2_PortD, bnUSB_RURD0, value);
				USBWritePortBuffVal(fx2GPIO.fx2_PortD);
			}
		}

		public adiBitValues RURD_1Bit {
			get { return USBGetPortBitVal(fx2GPIO.fx2_PortD, bnUSB_RURD1); }
			set {
				USBSetPortBitVal(fx2GPIO.fx2_PortD, bnUSB_RURD1, value);
				USBWritePortBuffVal(fx2GPIO.fx2_PortD);
			}
		}

		public adiBitValues RURD_2Bit {
			get { return USBGetPortBitVal(fx2GPIO.fx2_PortD, bnUSB_RURD2); }
			set {
				USBSetPortBitVal(fx2GPIO.fx2_PortD, bnUSB_RURD2, value);
				USBWritePortBuffVal(fx2GPIO.fx2_PortD);
			}
		}
		//
		#endregion Set Bits on USB Ports

		#region USB Chip Port Methods
		//
		///////////////////////////////////////////////////////////////////
		//
		// Methods to set/get values of ports on USB chip
		//	Set methods are called by USBWritePortBuffVal()
		//
		//////////////////////////////////////////////////////////////////

		/// <summary>
		/// This routine sets the value applied to PortA's pins on the EZUSB FX2 chip
		/// </summary>
		/// <param name="value"></param>
		/// <returns>True if successfull, False if unsuccessfull</returns>
		private bool SetPortAValue(byte value) {

			byte[] buf = new byte[2];
			byte[] Data = new byte[2];
			int nBytes;
			ErrorCode err;

			buf[0] = 0x01; // Comand value for Writing PortA Value
			buf[1] = value;

			//send 1 byte of data out on EP1OUT
			err = UsbWriterEP1.Write(buf, 2000, out nBytes);
			//result = clsEZUSBDev01.BulkXfer(0, buf, 2);
			if (err != ErrorCode.None)
			{
				MessageBoxEx.Show("Error: Unable to set PortA value: " + err.ToString(), _msgTime);
				if (err == ErrorCode.Win32Error)
				{
					Win32Exception ex = new Win32Exception();
					string errMsg = ex.Message;

				}
				//return false;
			}

			//read 1 byte back on EP1IN
			err = UsbReaderEP1.Read(Data, 2000, out nBytes);
			//result = clsEZUSBDev01.BulkXfer(1, Data, 1);
			if (err != ErrorCode.None)
			{
				MessageBoxEx.Show("Error: Unable to set PortA value:" + err.ToString(), _msgTime);
				if (err == ErrorCode.Win32Error)
				{
					Win32Exception ex = new Win32Exception();
					string errMsg = ex.Message;

				}
				return false;
			}

			if (Data[0] == buf[1])
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// This routine returns the value of PortA from the EZUSB-FX2 chip
		/// </summary>
		/// <returns></returns>
		private short GetPortAValue() {
			ErrorCode err;
			int nBytes;
			byte[] buf = new byte[2];
			byte[] Data = new byte[2];

			buf[0] = 0x02;

			//send 1 byte of data out on EP1OUT
			//result = clsEZUSBDev01.BulkXfer(0, buf, 1)
			err = UsbWriterEP1.Write(buf, 2000, out nBytes);
			if (err != ErrorCode.None)
			{
				MessageBoxEx.Show("Error: Unable to set PortA value: " + err.ToString(), _msgTime);
				return 0;
			}

			//read 1 byte back on EP1IN
			//result = clsEZUSBDev01.BulkXfer(1, Data, 1)
			err = UsbReaderEP1.Read(Data, 2000, out nBytes);
			if (err != ErrorCode.None)
			{
				MessageBoxEx.Show("Error: Unable to set PortA value: " + err.ToString(), _msgTime);
				return 0;
			}

			//Return value of A Port
			return Data[0];
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private bool SetPortBValue(byte value) {

			byte[] buf = new byte[2];
			byte[] Data = new byte[2];
			int nBytes;
			ErrorCode err;

			buf[0] = 0x0A; // Comand value for Writing PortB Value
			buf[1] = value;

			//send 1 byte of data out on EP1OUT
			err = UsbWriterEP1.Write(buf, 2000, out nBytes);
			//result = clsEZUSBDev01.BulkXfer(0, buf, 2);
			if (err != ErrorCode.None)
			{
				MessageBoxEx.Show("Error: Unable to set PortB value: " + err.ToString(), _msgTime);
				if (err == ErrorCode.Win32Error)
				{
					Win32Exception ex = new Win32Exception();
					string errMsg = ex.Message;

				}
				//return false;
			}

			//read 1 byte back on EP1IN
			err = UsbReaderEP1.Read(Data, 2000, out nBytes);
			//result = clsEZUSBDev01.BulkXfer(1, Data, 1);
			if (err != ErrorCode.None)
			{
				MessageBoxEx.Show("Error: Unable to set PortB value: " + err.ToString(), _msgTime);
				if (err == ErrorCode.Win32Error)
				{
					Win32Exception ex = new Win32Exception();
					string errMsg = ex.Message;

				}
				return false;
			}

			if (Data[0] == buf[1])
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// This routine returns the value of PortB from the EZUSB-FX2 chip
		/// </summary>
		/// <returns></returns>
		private short GetPortBValue() {
			ErrorCode err;
			int nBytes;
			byte[] buf = new byte[2];
			byte[] Data = new byte[2];

			buf[0] = 0x09;

			//send 1 byte of data out on EP1OUT
			//result = clsEZUSBDev01.BulkXfer(0, buf, 1)
			err = UsbWriterEP1.Write(buf, 2000, out nBytes);
			if (err != ErrorCode.None)
			{
				MessageBoxEx.Show("Error: Unable to set PortB value: " + err.ToString(), _msgTime);
				return 0;
			}

			//read 1 byte back on EP1IN
			//result = clsEZUSBDev01.BulkXfer(1, Data, 1)
			err = UsbReaderEP1.Read(Data, 2000, out nBytes);
			if (err != ErrorCode.None)
			{
				MessageBoxEx.Show("Error: Unable to set PortB value: " + err.ToString(), _msgTime);
				return 0;
			}

			//Return value of A Port
			return Data[0];
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private bool SetPortDValue(byte value) {

			byte[] buf = new byte[2];
			byte[] Data = new byte[2];
			int nBytes;
			ErrorCode err;

			buf[0] = 0x0C; // Comand value for Writing PortC Value
			buf[1] = value;

			//send 1 byte of data out on EP1OUT
			err = UsbWriterEP1.Write(buf, 2000, out nBytes);
			//result = clsEZUSBDev01.BulkXfer(0, buf, 2);
			if (err != ErrorCode.None)
			{
				MessageBoxEx.Show("Error: Unable to set PortD value: " + err.ToString(), _msgTime);
				if (err == ErrorCode.Win32Error)
				{
					Win32Exception ex = new Win32Exception();
					string errMsg = ex.Message;

				}
				//return false;
			}

			//read 1 byte back on EP1IN
			err = UsbReaderEP1.Read(Data, 2000, out nBytes);
			//result = clsEZUSBDev01.BulkXfer(1, Data, 1);
			if (err != ErrorCode.None)
			{
				MessageBoxEx.Show("Error: Unable to set PortD value: " + err.ToString(), _msgTime);
				if (err == ErrorCode.Win32Error)
				{
					Win32Exception ex = new Win32Exception();
					string errMsg = ex.Message;

				}
				return false;
			}

			if (Data[0] == buf[1])
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// This routine returns the value of PortD from the EZUSB-FX2 chip
		/// </summary>
		/// <returns></returns>
		private short GetPortDValue() {
			ErrorCode err;
			int nBytes;
			byte[] buf = new byte[2];
			byte[] Data = new byte[2];

			buf[0] = 0x0B;

			//send 1 byte of data out on EP1OUT
			//result = clsEZUSBDev01.BulkXfer(0, buf, 1)
			err = UsbWriterEP1.Write(buf, 2000, out nBytes);
			if (err != ErrorCode.None)
			{
				MessageBoxEx.Show("Error: Unable to set PortD value: " + err.ToString(), _msgTime);
				return 0;
			}

			//read 1 byte back on EP1IN
			//result = clsEZUSBDev01.BulkXfer(1, Data, 1)
			err = UsbReaderEP1.Read(Data, 2000, out nBytes);
			if (err != ErrorCode.None)
			{
				MessageBoxEx.Show("Error: Unable to set PortD value: " + err.ToString(), _msgTime);
				return 0;
			}

			//Return value of D Port
			return Data[0];
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private bool SetCtlValues(byte value) {

			byte[] buf = new byte[2];
			byte[] Data = new byte[2];
			int nBytes;
			ErrorCode err;

			buf[0] = 0x05; // Comand value for Writing PortA Value
			buf[1] = value;

			//send 1 byte of data out on EP1OUT
			err = UsbWriterEP1.Write(buf, 2000, out nBytes);
			//result = clsEZUSBDev01.BulkXfer(0, buf, 2);
			if (err != ErrorCode.None)
			{
				MessageBoxEx.Show("Error: Unable to set Ctl values: " + err.ToString(), _msgTime);
				if (err == ErrorCode.Win32Error)
				{
					Win32Exception ex = new Win32Exception();
					string errMsg = ex.Message;

				}
				//return false;
			}

			//read 1 byte back on EP1IN
			err = UsbReaderEP1.Read(Data, 2000, out nBytes);
			//result = clsEZUSBDev01.BulkXfer(1, Data, 1);
			if (err != ErrorCode.None)
			{
				MessageBoxEx.Show("Error: Unable to set Ctl values: " + err.ToString(), _msgTime);
				if (err == ErrorCode.Win32Error)
				{
					Win32Exception ex = new Win32Exception();
					string errMsg = ex.Message;

				}
				return false;
			}

			if (Data[0] == buf[1])
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		private short GetCtlValues() {
			//This routine returns the value of PortA from the EZUSB-FX2 chip
			ErrorCode err;
			int nBytes;
			byte[] buf = new byte[2];
			byte[] Data = new byte[2];

			buf[0] = 0x06;

			//send 1 byte of data out on EP1OUT
			//result = clsEZUSBDev01.BulkXfer(0, buf, 1)
			err = UsbWriterEP1.Write(buf, 2000, out nBytes);
			if (err != ErrorCode.None)
			{
				MessageBoxEx.Show("Error: Unable to set Ctl values: " + err.ToString(), _msgTime);
				return 0;
			}

			//read 1 byte back on EP1IN
			//result = clsEZUSBDev01.BulkXfer(1, Data, 1)
			err = UsbReaderEP1.Read(Data, 2000, out nBytes);
			if (err != ErrorCode.None)
			{
				MessageBoxEx.Show("Error: Unable to set Ctl values: " + err.ToString(), _msgTime);
				return 0;
			}

			//Return value of D Port
			return Data[0];
		}

		/////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// USBWritePortBuffVal() -- writes value in port buffer to USB chip port
		/// Writes the value specified by IOPort to the software buffer and the EZ-USB IO Port
		/// specifed by fx2PortVals.  If IOPortVal is not passed then it writes the value currently
		/// in the software buffer array "fx2PortVals()".
		/// </summary>
		public void USBWritePortBuffVal(fx2GPIO IOPort /*, short IOPortVal */ ) {

			short? IOPortVal = null;

			if (IOPortVal.HasValue) {
				//Set the register value
				fx2PortVals[(int)IOPort] = IOPortVal.Value;
			}


			switch (IOPort) {
				case fx2GPIO.fx2_PortA:
					if (_oldReset == adiBitValues.abvLow && USBGetPortBitVal(fx2GPIO.fx2_PortA, bnUSB_RESET) == adiBitValues.abvHigh) {
						// reset bit has just now been brought high
						// Clear out the registermap
						ResetRegMap(evb9959_RegMaps.rm9959_BothRegMapVals);
					}

					//Write the data to the EZUSB-FX2 PortA
					SetPortAValue((byte)fx2PortVals[(int)IOPort]);

					if (_oldReset == adiBitValues.abvHigh && USBGetPortBitVal(fx2GPIO.fx2_PortA, bnUSB_RESET) == adiBitValues.abvLow) {
						//If mEventsEnabled Then RaiseEvent AfterReset()
					}


					//            'Set the old values
					_oldReset = USBGetPortBitVal(fx2GPIO.fx2_PortA, bnUSB_RESET);
					break;
				case fx2GPIO.fx2_PortD:
					//Write the data to the EZUSB-FX2 PortD
					SetPortDValue((byte)fx2PortVals[(int)IOPort]);
					break;
				case fx2GPIO.fx2_CTLLines:
					//Write the value to the EZ-USB FX2 Chip
					SetCtlValues((byte)fx2PortVals[(int)IOPort]);
					break;
			}
		}

		/////////////////////////////////////////////////////////////////
		// 
		// Methods to set/get USB port values in software port buffer.
		//
		/////////////////////////////////////////////////////////////////

		/// <summary>
		/// USBSetPortBitVal - set individual lines on USB chip ports
		///   NOTE: is only setting the value in the software port buffer array.
		///   Follow this with call to USBWritePortBuffVal() to set lines on chip.
		/// </summary>
		/// <param name="IOPort"></param>
		/// <param name="bit"></param>
		/// <param name="value"></param>
		private void USBSetPortBitVal(fx2GPIO IOPort, short bit, adiBitValues value) {
			if (value != 0) {
				//Set the bit
				fx2PortVals[(int)IOPort] = (short)(fx2PortVals[(int)IOPort] | evbBitVals[bit]);
			}
			else {
				//Clear the bit
				fx2PortVals[(int)IOPort] = (short)(fx2PortVals[(int)IOPort] & ~evbBitVals[bit]);
			}
		}

		/// <summary>
		/// Get individual port bit value from port buffer
		/// </summary>
		/// <param name="IOPort"></param>
		/// <param name="bit"></param>
		/// <returns></returns>
		private adiBitValues USBGetPortBitVal(fx2GPIO IOPort, short bit) {
			//Test the bit
			if ((fx2PortVals[(int)IOPort] & evbBitVals[bit]) != 0) {
				//If a one the return 1
				return adiBitValues.abvHigh;
			}
			else {
				//If a zero then return 0
				return adiBitValues.abvLow;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		public void ResetDDS() {
			//Take the reset pin high the evaluation board
			ResetBit = adiBitValues.abvHigh;
			//USBWritePortBuffVal(fx2GPIO.fx2_PortA);
			//Take the reset pin low the evaluation board
			ResetBit = adiBitValues.abvLow;
			//USBWritePortBuffVal(fx2GPIO.fx2_PortA);

			// Manually reset profile pins after a reset
			P0Bit = adiBitValues.abvLow;
			P1Bit = adiBitValues.abvLow;
			P2Bit = adiBitValues.abvLow;
			P3Bit = adiBitValues.abvLow;
		}

		public void SendIOUpdate() {
			IOUpdateBit = adiBitValues.abvHigh;
			//USBWritePortBuffVal(fx2GPIO.fx2_PortD);
			IOUpdateBit = adiBitValues.abvLow;
			//USBWritePortBuffVal(fx2GPIO.fx2_PortD);
		}

		//
		#endregion USB Chip Ports

		#region Other USB Chip Commands
		//
		/// <summary>
		/// This subroutine sets the readback mode of the firmware
		/// Value = true  - Single byte read mode enabled
		/// Value = false - Single byte read mode disabled
		/// </summary>
		/// <param name="value"></param>
		public void SetRdBackMode(bool value) {

			byte[] buf = new byte[2];
			byte[] Data = new byte[2];
			int nBytes = 0;

			// Driver = USBDrvName
			buf[0] = 0x4;		// Command value for setting single read byte count mode.
			if (value == true) {
				buf[1] = 1;
			}
			else {
				buf[1] = 0;
			}

			// send 1 byte of data out on EP1OUT
			//result = clsEZUSBDev01.BulkXfer(0, buf, 2);
			ErrorCode err = UsbWriterEP1.Write(buf, 2000, out nBytes);
			if (err != ErrorCode.None) {
				MessageBoxEx.Show("Error: Setting single readback mode.", _msgTime);
				return;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public void SetRdBackByteCnt(short value) {
			// This subroutine sets the number of bytes that will be read back through
			// the GPIF interface in single byte Read Mode
			byte[] buf = new byte[3];
			byte[] Data = new byte[2];
			int nBytes = 0;

			//    Driver = USBDrvName
			buf[0] = 0x07;		//	Command value for setting single read byte count.
			buf[1] = MSB(value);
			buf[2] = LSB(value);

			// send 1 byte of data out on EP1OUT
			//result = clsEZUSBDev01.BulkXfer(0, buf, 3);
			ErrorCode err = UsbWriterEP1.Write(buf, 2000, out nBytes);
			if (err != ErrorCode.None) {
				MessageBoxEx.Show("Error: Unable to set Read Back Count.", _msgTime);
				return;
			}

		} // end method SetRdBackByteCnt()

		/// <summary>
		/// Sets the state of the AutoCSB Flag
		/// AutoCSB is turned ON when Value=1
		/// AutoCSB is turned OFF when Value=0
		/// </summary>
		public bool SetAutoCSBMode(byte value) {
			// 
			// Value = 1  - Auto CSB = On
			// Value = 0  - Auto CSB = Off
			byte[] buf = new byte[2];
			byte[] Data = new byte[2];

			buf[0] = 0x10;	 // Command value for setting auto CSB Mode.
			buf[1] = value;	 // Set the value

			// send 1 byte of data out on EP1OUT
			//result = clsEZUSBDev01.BulkXfer(0, buf, 2)
			int nBytes = 0;
			ErrorCode err = UsbWriterEP1.Write(buf, 2000, out nBytes);
			if (err != ErrorCode.None) {
				MessageBoxEx.Show("Error: Unable to set SetAutoCSBMode.", _msgTime);
				return false;
			}

			// read 1 byte back on EP1IN
			//result = clsEZUSBDev01.BulkXfer(1, Data, 1)
			err = UsbWriterEP1.Write(buf, 2000, out nBytes);
			if (err != ErrorCode.None) {
				MessageBoxEx.Show("Error: Unable to set SetAutoCSBMode.", _msgTime);
				return false;
			}
			else {
				if (Data[0] != 0) {
					return true;
				}
				else {
					return false;
				}
			}
		}

		//
		#endregion Other USB Commands

		#region DDS Register Methods
		//

		/// <summary>
		/// SetRegMapValue()
		/// Sets a new regmap value in the software's copy of the register map
		/// </summary>
		/// <param name="RegMap"></param>
		/// <param name="RegAddress"></param>
		/// <param name="strValue"></param>
		/// <param name="CHMask"></param>
		public void SetRegMapValue(evb9959_RegMaps RegMap, int RegAddress, string strValue, object CHMask) {

			if (RegAddress == 5) {
				strValue = "00" + strValue.Substring(2, 14);
			}

			// Test to see which type of register it is
			if ((RegAddress >= 0 && RegAddress <= 2) || (RegAddress >= 0x19 && RegAddress <= 0x1B)) {
				// Non channel register
				switch (RegMap) {
					case evb9959_RegMaps.rm9959_NewRegMapVals:
						sRegMapValsNew[RegAddress] = strValue;
						break;
					case evb9959_RegMaps.rm9959_CurRegMapVals:
						sRegMapVals[RegAddress] = strValue;
						break;
					case evb9959_RegMaps.rm9959_BothRegMapVals:
						sRegMapValsNew[RegAddress] = strValue;
						sRegMapVals[RegAddress] = strValue;
						break;
					default:
						sRegMapValsNew[RegAddress] = strValue;
						sRegMapVals[RegAddress] = strValue;
						break;
				}
			}
			else {
				// This is a channel register
				// See if they passed a channel mask value
				if (CHMask != null) {
					SetChRegValue(RegMap, RegAddress, strValue, (string)CHMask);
				}
				else {
					SetChRegValue(RegMap, RegAddress, strValue, ChIOEn_Mask);
				}
			}
		}

		public void SetRegMapValue(evb9959_RegMaps RegMap, int RegAddress, string strValue) {
			SetRegMapValue(RegMap, RegAddress, strValue, null);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="RegMap"></param>
		public void ResetRegMap(evb9959_RegMaps RegMap) {

			for (int cntr = 0; cntr < sDefaultRegMapVals.Length; cntr++) {
				if (cntr < 7 || cntr > 0x18) {
					switch ((int)RegMap) {
						case 0:
							//Set the default value
							sRegMapVals[cntr] = sDefaultRegMapVals[cntr];
							if (cntr >= 3 && cntr <= 24) {
								sChnlRegVals[0, cntr - 3] = sDefChnlRegVals[0, cntr - 3];
								sChnlRegVals[1, cntr - 3] = sDefChnlRegVals[1, cntr - 3];
                                // TODO: rev 3.11 using channels 3 4 now:
                               
								sChnlRegVals[2, cntr - 3] = sDefChnlRegVals[2, cntr - 3];
								sChnlRegVals[3, cntr - 3] = sDefChnlRegVals[3, cntr - 3];
							}
							break;
						case 1:
							//Set the default value
							sRegMapValsNew[cntr] = sDefaultRegMapVals[cntr];
							if (cntr >= 3 && cntr <= 24) {
								sChnlRegValsNew[0, cntr - 3] = sDefChnlRegVals[0, cntr - 3];
								sChnlRegValsNew[1, cntr - 3] = sDefChnlRegVals[1, cntr - 3];
                                // TODO: rev 3.11 using channels 3 4 now:
								sChnlRegValsNew[2, cntr - 3] = sDefChnlRegVals[2, cntr - 3];
								sChnlRegValsNew[3, cntr - 3] = sDefChnlRegVals[3, cntr - 3];
							}
							break;
						case 2:
							//Set the default value
							sRegMapValsNew[cntr] = sDefaultRegMapVals[cntr];
							sRegMapVals[cntr] = sDefaultRegMapVals[cntr];
							if (cntr >= 3 && cntr <= 24) {
								sChnlRegValsNew[0, cntr - 3] = sDefChnlRegVals[0, cntr - 3];
								sChnlRegValsNew[1, cntr - 3] = sDefChnlRegVals[1, cntr - 3];
                                // TODO: rev 3.11 using channels 3 4 now:
                                sChnlRegValsNew[2, cntr - 3] = sDefChnlRegVals[2, cntr - 3];
								sChnlRegValsNew[3, cntr - 3] = sDefChnlRegVals[3, cntr - 3];

								sChnlRegVals[0, cntr - 3] = sDefChnlRegVals[0, cntr - 3];
								sChnlRegVals[1, cntr - 3] = sDefChnlRegVals[1, cntr - 3];
                                // TODO: rev 3.11 using channels 3 4 now:
                                sChnlRegVals[2, cntr - 3] = sDefChnlRegVals[2, cntr - 3];
								sChnlRegVals[3, cntr - 3] = sDefChnlRegVals[3, cntr - 3];
							}
							break;
					}  // end switch

				}  // end if
			}  // end for
		}  // end method ResetRegMap()

		/// <summary>
		/// 
		/// </summary>
		/// <param name="RegMap"></param>
		/// <param name="RegAddr"></param>
		/// <param name="iChnl"></param>
		/// <returns></returns>
		public string GetRegMapValue(evb9959_RegMaps RegMap, int RegAddr, int iChnl) {
			//Test to see which type of register it is
			if ((RegAddr >= 0 && RegAddr <= 2) || (RegAddr >= 0x19 && RegAddr <= 0x1B)) {
				if (RegMap == evb9959_RegMaps.rm9959_NewRegMapVals) {
					return sRegMapValsNew[RegAddr];
				}
				else {
					return sRegMapVals[RegAddr];
				}
			}
			else {
				if (RegMap == evb9959_RegMaps.rm9959_NewRegMapVals) {
					return sChnlRegValsNew[iChnl, RegAddr - 3];
				}
				else {
					return sChnlRegVals[iChnl, RegAddr - 3];
				}
			}
		}  // end method GetRegMapValue()


		public string GetRegMapValue(evb9959_RegMaps RegMap, int RegAddr) {
			return GetRegMapValue(RegMap, RegAddr, 0);
		}  // end method GetRegMapValue()

		// Returns a single bit value from the register map
		public adiBitValues GetRegMapBitValue(evb9959_RegMaps RegMap, int RegAddress, int BitNum, int iChnl) {
			string BitValue;
			if (RegLength[RegAddress] > BitNum) {
				if ((RegAddress >= 0 && RegAddress <= 2) || (RegAddress >= 0x19 && RegAddress <= 0x1B)) {
					if (RegMap == evb9959_RegMaps.rm9959_NewRegMapVals) {
						// Return the bit value
						BitValue = sRegMapValsNew[RegAddress].Substring(RegLength[RegAddress] - BitNum - 1, 1);
					}
					else {
						// Return the bit value
						BitValue = sRegMapVals[RegAddress].Substring(RegLength[RegAddress] - BitNum - 1, 1);
					}

					// Return the value that was retrieved
					if (BitValue == "1") {
						return adiBitValues.abvHigh;
					}
					else {
						return adiBitValues.abvLow;
					}
				}
				else {
					return GetChRegBitValue(RegMap, RegAddress, BitNum, iChnl);
				}
			}
			else {
				MessageBoxEx.Show("The BIT being addressed does not exist in the addressed register location!", _msgTime);
				return adiBitValues.abvLow;
			}
		}

		public adiBitValues GetRegMapBitValue(evb9959_RegMaps RegMap, int RegAddress, int BitNum) {
			return GetRegMapBitValue(RegMap, RegAddress, BitNum, 0);
		}

		public void SetRegMapBitValue(evb9959_RegMaps RegMap, int RegAddress, int BitNum, adiBitValues value, object CHMask) {
			string RegVal;
			string BitVal;
			short RegLen;

			//Add 1 the bit number ??
			//BitNum = BitNum;

			if (RegLength[RegAddress] > BitNum) {
				//Test to see if they are setting a channel register or not
				if ((RegAddress >= 0 && RegAddress <= 2) || (RegAddress >= 0x19 && RegAddress <= 0x1B)) {
					if (value == adiBitValues.abvHigh) {
						BitVal = "1";
					}
					else {
						BitVal = "0";
					}
					if (RegMap == evb9959_RegMaps.rm9959_NewRegMapVals) {
						// Get the current value of the register
						RegVal = sRegMapValsNew[RegAddress];
						RegLen = RegLength[RegAddress];
						// Set the new bit value
						sRegMapValsNew[RegAddress] = RegVal.Substring(0, RegLen - BitNum - 1) + BitVal + RegVal.Substring(RegLen - BitNum, BitNum);
					}
					else if (RegMap == evb9959_RegMaps.rm9959_CurRegMapVals) {
						// Get the current value of the register
						RegVal = sRegMapVals[RegAddress];
						//BitVal = Convert.ToString(value);
						RegLen = RegLength[RegAddress];
						// Set the new the bit value
						sRegMapVals[RegAddress] = RegVal.Substring(0, RegLen - BitNum - 1) + BitVal + RegVal.Substring(RegLen - BitNum, BitNum);
					}
					else {
						// Get the current value of the register
						RegVal = sRegMapValsNew[RegAddress];
						//BitVal = Convert.ToString(value);
						RegLen = RegLength[RegAddress];
						// Set the new the bit value
						sRegMapVals[RegAddress] = RegVal.Substring(0, RegLen - BitNum - 1) + BitVal + RegVal.Substring(RegLen - BitNum, BitNum);
						sRegMapValsNew[RegAddress] = RegVal.Substring(0, RegLen - BitNum - 1) + BitVal + RegVal.Substring(RegLen - BitNum, BitNum);
					}
				}
				else {
					// See if they passed a channel mask value
					if (CHMask != null) {
						// Set the channel register
						SetChRegBitValue(RegMap, RegAddress, BitNum, value, (string)CHMask);
					}
					else {
						// Set the channel register
						SetChRegBitValue(RegMap, RegAddress, BitNum, value, ChIOEn_Mask);
					}
				}
			}
			else {
				MessageBoxEx.Show("The BIT being addressed does not exist in the addressed register location!", _msgTime);
			}
		}

		public void SetRegMapBitValue(evb9959_RegMaps RegMap, int RegAddress, int BitNum, adiBitValues value) {
			SetRegMapBitValue(RegMap, RegAddress, BitNum, value, null);
		}

		/// <summary>
		/// GetChRegBitValue - Retrieves the bit value from the specified software register map
		/// </summary>
		/// <param name="RegMap">0 = New Values,  1 = Current Values.</param>
		/// <param name="RegAddr">Address of the register you want to retrieve a value for. 3 - 24</param>
		/// <param name="BitNum">Number of the bit you want the value of. 0-XX</param>
		/// <param name="iChnl">Channel which you want to read register data from.</param>
		/// <returns>the bit value of the bit addressed</returns>
		public adiBitValues GetChRegBitValue(evb9959_RegMaps RegMap, int RegAddr, int BitNum, int iChnl) {
			string BitValue;

			if (RegLength[RegAddr] > BitNum) {
				if (RegMap == evb9959_RegMaps.rm9959_NewRegMapVals) {
					// Return the bit value
					BitValue = sChnlRegValsNew[iChnl, RegAddr - 3].Substring(RegLength[RegAddr] - BitNum - 1, 1);
				}
				else {
					// Return the bit value
					BitValue = sChnlRegVals[iChnl, RegAddr - 3].Substring(RegLength[RegAddr] - BitNum - 1, 1);
				}

				// Return the value that was retrieved
				if (BitValue == "1") {
					return adiBitValues.abvHigh;
				}
				else {
					return adiBitValues.abvLow;
				}
			}
			else {
				MessageBoxEx.Show("The BIT being addressed does not exist in the addressed register location!", _msgTime);
				return adiBitValues.abvLow;
			}
		}


		// Sets the bit value in the specified software register map
		// Inputs:
		// RegMap - 0 - New Values.
		//          1 - Current Values.
		// RegAddr - Address of the register you want to set a value for. 3 - 24
		// BitNum - Number of the bit you want the value of. 0-XX
		// CHMask - Binary string that represents the value of the Channel IO Enable bits.
		// BitVal - Binary value of the bit.
		// 
		// Outputs:
		//    None.
		public void SetChRegBitValue(evb9959_RegMaps RegMap, int RegAddr, int BitNum, adiBitValues BitValue, string CHMask) {
			string RegVal;
			string BitVal;
			short RegLen;
			short cntr;
			// Check to see if they are addressing a valid bit position
			if (RegLength[RegAddr] > BitNum) {
				if (BitValue == adiBitValues.abvHigh) {
					BitVal = "1";
				}
				else {
					BitVal = "0";
				}
				// Check all bits in the channel mask
                // TODO: rev 3.11 using all 4 channels now
				for (cntr = 0; cntr < 4; cntr++) {
					// If you find a 1 then exit the loop
					if (CHMask.Substring(cntr, 1) == "1") {
						// Set the value
						if (RegMap == evb9959_RegMaps.rm9959_NewRegMapVals) {
							// Get the current value of the channel register
							RegVal = sChnlRegValsNew[3 - cntr, RegAddr - 3];
							//BitVal = Convert.ToString(BitValue);
							RegLen = RegLength[RegAddr];
							// Set the new bit value
							sChnlRegValsNew[3 - cntr, RegAddr - 3] = RegVal.Substring(0, RegLen - BitNum - 1) + BitVal + RegVal.Substring(RegLen - BitNum, BitNum);
						}
						else if (RegMap == evb9959_RegMaps.rm9959_CurRegMapVals) {
							// Get the current value of the channel register
							RegVal = sChnlRegVals[3 - cntr, RegAddr - 3];
							//BitVal = Convert.ToString(BitValue);
							RegLen = RegLength[RegAddr];
							// Set the new bit value
							sChnlRegVals[3 - cntr, RegAddr - 3] = RegVal.Substring(0, RegLen - BitNum - 1) + BitVal + RegVal.Substring(RegLen - BitNum, BitNum);
						}
						else {
							// Get the current value of the channel register
							RegVal = sChnlRegValsNew[3 - cntr, RegAddr - 3];
							//BitVal = Convert.ToString(BitValue);
							RegLen = RegLength[RegAddr];
							// Set the new bit value
							sChnlRegValsNew[3 - cntr, RegAddr - 3] = RegVal.Substring(0, RegLen - BitNum - 1) + BitVal + RegVal.Substring(RegLen - BitNum, BitNum);
							sChnlRegVals[3 - cntr, RegAddr - 3] = RegVal.Substring(0, RegLen - BitNum - 1) + BitVal + RegVal.Substring(RegLen - BitNum, BitNum);
						}
					}
				}
			}
			else {
				MessageBoxEx.Show("The BIT being addressed does not exist in the addressed register location!", _msgTime);
			}
		}

		//'Retrieves the channel retister map value from the specified software register map
		//'Inputs:
		//'RegMap  - 0 - New Values.
		//'          1 - Current Values.
		//'RegAddr - Address of the register you want to retrieve a value for. 3 - 24
		//'BitNum  - Number of the bit you want the value of. 0-XX
		//'CHMask  - Binary string that represents the value of the Channel IO Enable bits.
		//'         If more than one channel register value is requested then the function
		//'         only returns the value of the lowest channel number.  This mimics the
		//'         behavior of the DDS.
		//'
		//'Outputs:
		//'Returns the bit value of the bit addressed.
		public string GetChRegValue(evb9959_RegMaps RegMap, int RegAddr, int iChnl) {
			
			//'Return the value from the specified registermap buffer
			if (RegMap == evb9959_RegMaps.rm9959_NewRegMapVals) {
				return sChnlRegValsNew[iChnl, RegAddr - 3];
			}
			else if (RegMap == evb9959_RegMaps.rm9959_CurRegMapVals) {
				return sChnlRegVals[iChnl, RegAddr - 3];
			}
			else {
				throw new ApplicationException("GetChRegValue() illegal regMap.");
			}
		}


		/// <summary>
		/// Retrieves the channel retister map value from the specified software register map
		/// Inputs:
		/// RegMap - 0 - New Values.
		///          1 - Current Values.
		/// RegAddr - Address of the register you want to retrieve a value for. 3 - 24
		/// CHMask - Binary string that represents the value of the Channel IO Enable bits.
		/// Value - Binary string of the registermap value.
		/// </summary>
		public void SetChRegValue(evb9959_RegMaps RegMap, int RegAddr, string sValue, string CHMask) {
			short cntr;
			// Check all bits in the channel mask
            // TODO: rev 3.11 using all 4 channels now:
			for (cntr = 3; cntr >= 0; cntr--) {
				// If you find a 1 then exit the loop
				if (CHMask.Substring(cntr, 1) == "1") {
					// Set the value
					if (RegMap == evb9959_RegMaps.rm9959_NewRegMapVals) {
						sChnlRegValsNew[3 - cntr, RegAddr - 3] = sValue;
					}
					else if (RegMap == evb9959_RegMaps.rm9959_CurRegMapVals) {
						sChnlRegVals[3 - cntr, RegAddr - 3] = sValue;
					}
					else {
						sChnlRegValsNew[3 - cntr, RegAddr - 3] = sValue;
						sChnlRegVals[3 - cntr, RegAddr - 3] = sValue;
					}
				}
			}
		}

		//
		#endregion DDS Register Methods

		#region DDS Register Read/Write
		//

		//USBSerialLoad
		//Sends data over the SPI port lines to the DDS
		public void USBSerialLoad(int RegAddress, string Data, object NoUpRegMap) {
			switch (SPI_IOMode) {
				case spiIOMode.spi3Wire:
				case spiIOMode.spi2Wire:
					if (NoUpRegMap == null) {
						USBSerialLoad1Bit(RegAddress, Data);
					}
					else {
						USBSerialLoad1Bit(RegAddress, Data, 1);
					}
					break;
				default:
					MessageBoxEx.Show("Unsupported SPI_Serial mode", _msgTime);
					break;
			}
		}

		public void USBSerialLoad(int RegAddress, string Data) {
			USBSerialLoad(RegAddress, Data, null);
		}

		private void USBSerialLoad1Bit(int RegAddress, string Data, object NoUpRegMap) {
			byte[] buffer;
			short i;
			int lDatalen;
			string SData;
			short NumBytes;

			string sInstructByte;

			// If the data is not the proper size
			if ((Data.Length > RegLength[RegAddress]) || (Data.Length < RegLength[RegAddress])) {
				// If VerboseIO is true then show an error message
				MessageBoxEx.Show("Error: Data is improperly sized for the addressed register.", _msgTime);

				return;
			}

			// Only work if the object is enabled
			if (mvarEnabled) {

				// Redim the buffer to be sent over the USB bus
				buffer = new byte[Data.Length + 8];

				// Make sure that sclk is high before starting the transfer
				if (SClockBit == adiBitValues.abvLow) {
					// Make it high again
					SClockBit = adiBitValues.abvHigh;
					//USBWritePortBuffVal(fx2GPIO.fx2_CTLLines);
				}

				// Build the instruction byte string
				// Get the number of bytes sent
				NumBytes = (short)(Data.Length / 8);

				// If the number of bytes to send is not a multiple of 8 or an invalid reg
				// address is being used then exit and do nothing
				if ((Data.Length % 8) == 0 && RegAddress <= 27) {

					// Build the instruction byte
					sInstructByte = BuildInstruct(0, RegAddress);

					// Copy the data into a buffer where it can be manipulated preserving the origional data
					SData = Data;

					// Check to see if the part is currently in LSB First Mode
					if (SPI_LSB_First) {
						// Rearrange the instruction byte into LSB First format
						sInstructByte = FlipString(sInstructByte);
						// Rearrange the data into LSB First format
						SData = FlipString(SData);
					}

					if (AutoCSBMode == false) {
						// Set CSB low in the software buffer
						ChipSelectBit = adiBitValues.abvLow;
						// Take CSB line low
						//USBWritePortBuffVal(fx2GPIO.fx2_PortA);
					}

					// Build the serial stream
					SData = sInstructByte + SData;

					// Convert to a byte buffer
					for (i = 0; i < SData.Length; i++) {
						buffer[i] = Convert.ToByte(SData.Substring(i, 1));
					}

					// Get the length of the data to be sent
					lDatalen = SData.Length;

					// Do the serial transfer
					//result = clsEZUSBDev01.BulkXfer(clsEZUSB.EZUSB_ReadOrWrite.ezWrite, buffer, lDatalen);
					int nBytes;
					ErrorCode err = UsbWriterDDS.Write(buffer, 2000, out nBytes);
					if (err != ErrorCode.None) {
						string errMsg2, errMsg;
						if (err == ErrorCode.Win32Error) {
							Win32Exception ex = new Win32Exception();
							errMsg = ex.Message;
							errMsg2 = "Error Writing to DDS Board: " + errMsg;
							MessageBoxEx.Show(errMsg2, "USBSerialLoad1Bit", _msgTime);
						}
						else {
							errMsg = Enum.GetName(typeof(ErrorCode), err);
							errMsg2 = "Error Writing to DDS Board: " + errMsg;
							MessageBoxEx.Show(errMsg2, "USBSerialLoad1Bit", _msgTime);
						}
						throw new ApplicationException(errMsg2);
						return;
					}

					if (!AutoCSBMode) {
						// Now return the CSB line high
						ChipSelectBit = adiBitValues.abvHigh;
						// Take CSB line high
						//USBWritePortBuffVal(fx2GPIO.fx2_PortA);
					}

					// If AutoIOUpdate is true then perform an IO Update
					if (mvarAutoIOUpdate) {
						SendIOUpdate();
					}

					// Store the value written to the register in a buffer
					if (NoUpRegMap == null) {
						SetRegMapValue(evb9959_RegMaps.rm9959_BothRegMapVals, RegAddress, Data);
					}

					// Now check to see if we are writing to the spi port control register
					if (RegAddress == 0) {
					}
				}
				else {
					// The data isn't valid or the address isn't valid
					MessageBoxEx.Show("USBSerialLoad1 data or address invalid", _msgTime);
				}
			}
		}

		private void USBSerialLoad1Bit(int RegAddress, string Data) {
			USBSerialLoad1Bit(RegAddress, Data, null);
		}

		public string USBSerialRead(int RegAddress, object NoUpRegMap) {
			switch (SPI_IOMode) {
				case spiIOMode.spi3Wire:
				case spiIOMode.spi2Wire:
					if (NoUpRegMap == null) {
						return USBSerialRead1Bit(RegAddress);
					}
					else {
						return USBSerialRead1Bit(RegAddress, 1);
					}
				//break;
				default:
					MessageBoxEx.Show("Unsupported SPI_Serial mode", _msgTime);
					return "";
				//break;
			}
		}

		public string USBSerialRead(int RegAddress) {
			return USBSerialRead(RegAddress, null);
		}

		private string USBSerialRead1Bit(int RegAddress) {
			return USBSerialRead1Bit(RegAddress, null);
		}

		//Do not try to read back more than 8 bytes of data because of a firmware limitation
		private string USBSerialRead1Bit(int RegAddress, object NoUpRegMap) {
			byte[] buffer;
			short i;
			int IDataLen;
			byte[] ReturnBuff;
			string sRetStr;
			byte MaskVal;  //Used to mask out either SDO or SDIO from the returned data

			short lBytes2Rd;

			string sInstructByte;

			// Validate the register address
			if ((RegAddress >= 0) && (RegAddress <= 27)) {
				// Build the instruction byte string
				sInstructByte = BuildInstruct(1, RegAddress);
				// Number of bytes to write instruction byte
				IDataLen = sInstructByte.Length;

				// Get the bits to read
				lBytes2Rd = RegLength[RegAddress];

				// Setup a buffer to receive data into
				// (each bit is returned in its own byte)
				ReturnBuff = new byte[lBytes2Rd];

				// Make sure that SCLK is High
				if (SClockBit == adiBitValues.abvLow) {
					SClockBit = adiBitValues.abvHigh;
					//USBWritePortBuffVal(fx2GPIO.fx2_CTLLines);
				}

				// Check to see if the part is currently in LSB First Mode
				if (SPI_LSB_First) {
					// Rearrange the instruction byte into LSB First format
					sInstructByte = FlipString(sInstructByte);
				}

				// Convert the binary string into a bytearray for sending via the USB
				buffer = Str2ByteArray(sInstructByte);

				if (!AutoCSBMode) {
					// Now take the CSB line Low
					ChipSelectBit = adiBitValues.abvLow;
					//USBWritePortBuffVal(fx2GPIO.fx2_PortA);
				}

				ErrorCode err;
				int nBytes;
				// Write the instruction byte
				//result = clsEZUSBDev01.BulkXfer(clsEZUSB.EZUSB_ReadOrWrite.ezWrite, buffer, IDataLen);
				if (_firstCallToSerialRead) {
					//
					// dac notes:
					// The very first time that this method is called following
					//	a power-up of the USB device, the following code is required
					//	in order for the next UsbWriterDDS.Write to work.
					//
					SetRdBackByteCnt(lBytes2Rd);
					// no clock or SDIO pulse occur here:
					err = UsbWriterDDS.Write(buffer, 2000, out nBytes);
					// this read times out:
					err = UsbReaderDDS.Read(ReturnBuff, 2000, out nBytes);
					SetRdBackByteCnt(lBytes2Rd);
					// apparently normal clock and SDIO pulses occur now:
					err = UsbWriterDDS.Write(buffer, 2000, out nBytes);
					// but this read times out:
					err = UsbReaderDDS.Read(ReturnBuff, 2000, out nBytes);
					// the next write and read will be OK
					// All subsequent calls to this method will work
					//	without the above code.
					_firstCallToSerialRead = false;
				}

				// Set number of bits to read
				SetRdBackByteCnt(lBytes2Rd);

				err = UsbWriterDDS.Write(buffer, 2000, out nBytes);
				if (err != ErrorCode.None) {
					if (err == ErrorCode.Win32Error) {
						Win32Exception ex = new Win32Exception();
						string errMsg = "Error Writing Instruction to Board: " + ex.Message;
						//MessageBoxEx.Show(errMsg, "USBSerialRead1Bit", _msgTime);
						throw new ApplicationException(errMsg);
					}
					else {
						string errMsg = "Error Writing Instruction to Board: " + Enum.GetName(typeof(ErrorCode), err);
						//MessageBoxEx.Show(errMsg, "USBSerialRead1Bit", _msgTime);
						throw new ApplicationException(errMsg);
					}
					return "";
				}

				// *** Note - This is now done automatically as long as you set the
				// rdBackCount before the sending the instruction byte
				// (SetRdBackByteCnt)
				// This line actually reads back the data into the EP8 Fifo and readys
				// it for readback by the PC using the Bulk Transfer below
				// Set the firmware in readback mode
				// SetRdBackMode(true)

				// Readback the register value
				//result = clsEZUSBDev01.BulkXfer(clsEZUSB.EZUSB_ReadOrWrite.ezRead, ReturnBuff, lBytes2Rd);
				err = UsbReaderDDS.Read(ReturnBuff, 2000, out nBytes);
				if (err != ErrorCode.None) {
					if (err == ErrorCode.Win32Error) {
						Win32Exception ex = new Win32Exception();
						string errMsg = "Error Readback register on Board: " + ex.Message;
						//MessageBoxEx.Show(errMsg, "USBSerialRead1Bit", _msgTime);
						throw new ApplicationException(errMsg);
					}
					else {
						string errMsg = "Error Readback register on Board: " + Enum.GetName(typeof(ErrorCode), err);
						//MessageBoxEx.Show(errMsg, "USBSerialRead1Bit", _msgTime);
						throw new ApplicationException(errMsg);
					}
					return "";
				}

				// Disable readback mode
				SetRdBackMode(false);

				// Now return the CSB line high
				if (!AutoCSBMode) {
					ChipSelectBit = adiBitValues.abvHigh;
					//USBWritePortBuffVal(fx2GPIO.fx2_PortA);
				}

				// Setup the bitmask for the returned data
				if (SPI_IOMode == spiIOMode.spi2Wire) {
					MaskVal = (byte)evbBitVals[bnUSB_SDIO_0];
				}
				else {
					MaskVal = (byte)evbBitVals[bnUSB_SDIO_2];
				}

				// Mask out the proper bit Bit0 = SDIO, Bit1 = SDO-ThreeWireMode
				for (i = 0; i < lBytes2Rd; i++) {
					if ((ReturnBuff[i] & MaskVal) != 0) {
						ReturnBuff[i] = 1;
					}
					else {
						ReturnBuff[i] = 0;
					}
				}

				// Convert the byte array "ReturnBuff" to a binary string
				sRetStr = ByteArray2Str(ReturnBuff);
				//sRetStr = Convert.ToString(

				// Check to see if the part is currently in LSB First Mode
				if (SPI_LSB_First) {
					// Rearrange the instruction byte into LSB First format
					sRetStr = FlipString(sRetStr);
				}

				// Store the value read from the register in the register map
				if (NoUpRegMap == null) {
					// sSetRegVals 2, RegAddress, sRetStr
					SetRegMapValue(evb9959_RegMaps.rm9959_BothRegMapVals, RegAddress, sRetStr);
				}

				// Return the cleaned up data to the user
				return sRetStr;

			}
			else {
				// Display an error message
				// The data isn't valid or the address isn't valid
				if (RegAddress < 0 || RegAddress > 0x1B) {
					string errMsg = "Error: Invalid address in USBSerialRead1Bit() = " + RegAddress.ToString();
					//MessageBoxEx.Show(errMsg, _msgTime);
					throw new ApplicationException(errMsg);
				}
				return "";
			}

		}  // end method USBSerialRead1Bit()

		/// <summary>
		/// Loads all register values from NewRegMapVals into DDS chip.
		/// </summary>
		public void LoadAllDDSRegisters() {
			LoadChipRegisters();
			LoadChannelRegisters();
		}

		/// <summary>
		/// Loads registers that control DDS chip or that relate to all channels;
		/// </summary>
		private void LoadChipRegisters() {
			AutoIOUpdate = false;
			USBSerialLoad(0, GetRegMapValue(AD9959EvalBd.evb9959_RegMaps.rm9959_NewRegMapVals, 0));
			USBSerialLoad(1, GetRegMapValue(AD9959EvalBd.evb9959_RegMaps.rm9959_NewRegMapVals, 1));
			USBSerialLoad(2, GetRegMapValue(AD9959EvalBd.evb9959_RegMaps.rm9959_NewRegMapVals, 2));
			AutoIOUpdate = true;
			SendIOUpdate();
			Thread.Sleep(1);  // wait 1 ms to allow PLL to lock if freq multiplier changed
		}

		/// <summary>
		/// Loads registers that control specific channels on the DDS.
		/// </summary>
		private void LoadChannelRegisters() {
			AutoIOUpdate = false;
			string sChRegVal;
            // TODO: rev 3.11 using all 4 channels now:
			for (int chnl = 0; chnl < 4; chnl++) {
                if (_usingThisChannel[chnl]) {
				    //'Select the channel
				    SelectChannel(chnl);
				    for (int ireg = 3; ireg < 0x19; ireg++) {
					    //'Get the channel register value
					    sChRegVal = GetChRegValue(AD9959EvalBd.evb9959_RegMaps.rm9959_NewRegMapVals, ireg, chnl);
					    //'Load the value
					    USBSerialLoad(ireg, sChRegVal);
				    }
                }
            }
			AutoIOUpdate = true;
			SendIOUpdate();
		}

		#endregion DDS register read/write

		#region Register 0 (CSR)
		//
		/// <summary>
		/// 
		/// </summary>
		public spiIOMode SPI_IOMode {
			get {
				string sRegVal;
				sRegVal = GetRegMapValue(evb9959_RegMaps.rm9959_CurRegMapVals, 0);
				string sMode = sRegVal.Substring(5, 2);

				mvarSPI_IOMode = (spiIOMode)Convert.ToInt16(sMode, 2);
				return mvarSPI_IOMode;
			}
			set {
				string sRegVal;
				// Get the current CSR register value
				sRegVal = GetRegMapValue(evb9959_RegMaps.rm9959_CurRegMapVals, 0);
				// Set the new bit value
				string ioMode = Convert.ToString((int)value, 2);
				ioMode = ioMode.Substring(ioMode.Length - 2, 2);
				sRegVal = sRegVal.Substring(0, 5) + ioMode + sRegVal.Substring(7, 1);
				// Set the modified register map value
				SetRegMapValue(evb9959_RegMaps.rm9959_NewRegMapVals, 0, sRegVal);
			}
		}

		/// <summary>
		/// Sets the proper bits in the CSR
		/// and loads them into the DDS
		/// Note: This subroutine only allows you to select one channel at a time
		/// </summary>
		public void SelectChannel(int iChnl) {
			string[] sChnlMask = new string[4];
			string sRegVal;
			bool bAutoIOUD;

			// Get the current state of AutoIOUpdate
			bAutoIOUD = AutoIOUpdate;

			// Setup the channel mask values
			sChnlMask[0] = "0001";
			sChnlMask[1] = "0010";
			sChnlMask[2] = "0100";
			sChnlMask[3] = "1000";

			// If they specified an invalid channel then exit and do nothing
			if (iChnl > 3) {
				MessageBoxEx.Show("Error: Invalid Channel in SelectChannel()", _msgTime);
				return;
			}

			// Get the current value
			sRegVal = GetRegMapValue(evb9959_RegMaps.rm9959_CurRegMapVals, 0);
			sRegVal = sChnlMask[iChnl] + sRegVal.Substring(4, 4);

			// Turn auto I/O Update off because you don't need it for the Channel I/O Enable bits
			AutoIOUpdate = false;

			// Send the new value
			USBSerialLoad(0, sRegVal);

			if (bAutoIOUD) {
				// Turn auto I/O Update on
				AutoIOUpdate = true;
			}
		}

		/// <summary>
		/// Get a mask string for particular channel number.
		/// </summary>
		/// <param name="iChnl"></param>
		/// <returns></returns>
		public string GetChMaskVal(int iChnl) {

			string[] sChnlMask = new String[4];

			// If they specified an invalid channel then exit and do nothing
			if (iChnl > 3) {
				throw new ApplicationException("Invalid channel in GetChMaskVal()");
			}

			// Setup the channel mask values
			sChnlMask[0] = "0001";
			sChnlMask[1] = "0010";
			sChnlMask[2] = "0100";
			sChnlMask[3] = "1000";

			// Return the value requested
			return sChnlMask[iChnl];
		}

		/// <summary>
		/// Get/Set the channel enable bits as 4-bit binary string.
		/// Does not load into DDS.
		/// </summary>
		public string ChIOEn_Mask {
			get {
				string sRegVal;

				// Get the current value
				sRegVal = GetRegMapValue(evb9959_RegMaps.rm9959_CurRegMapVals, 0);
				// Return the mask value
				return sRegVal.Substring(0, 4);
			}

			set {
				string sRegVal;

				if (value.Length != 4) {
					MessageBoxEx.Show("CHIOEn_Mask Property Set: Invalid Data!", _msgTime);
				}
				else {
					// Get the current new value
					sRegVal = GetRegMapValue(evb9959_RegMaps.rm9959_NewRegMapVals, 0);
					// Add in the new data
					sRegVal = value + sRegVal.Substring(4, 4); // Build the new values for CSR
					// Set the new regval but only in newregmapvals
					SetRegMapValue(evb9959_RegMaps.rm9959_NewRegMapVals, 0, sRegVal);
				}
			}

		}

		/// <summary>
		/// Get/Set software register map for LSBFirst bit Register0
		/// Does not load into DDS
		/// </summary>
		public bool SPI_LSB_First {
			get {
				int bit;
				bit = (int)GetRegMapBitValue(evb9959_RegMaps.rm9959_CurRegMapVals, 0, 0);
				if (bit == 1) {
					return true;
				}
				else {
					return false;
				}
			}
			set {
				// Set the value in the register map , note that you must perform a serial load
				// with that new register data for it to take effect
				if (value) {
					SetRegMapBitValue(evb9959_RegMaps.rm9959_NewRegMapVals, 0, 0, adiBitValues.abvHigh);
				}
				else {
					SetRegMapBitValue(evb9959_RegMaps.rm9959_NewRegMapVals, 0, 0, adiBitValues.abvLow);
				}
			}
		}

		//
		#endregion Register 0

		#region Register 1 (FR1)
		//
		public bool VCOGainOn {
			get {
				int VcoBit;
				VcoBit = (int)GetRegMapBitValue(evb9959_RegMaps.rm9959_CurRegMapVals, 1, 23);
				if (VcoBit == 1) {
					return true;
				}
				else {
					return false;
				}
			}
			set {
				if (value) {
					SetRegMapBitValue(AD9959EvalBd.evb9959_RegMaps.rm9959_NewRegMapVals, 1, 23, adiBitValues.abvHigh);
				}
				else {
					SetRegMapBitValue(AD9959EvalBd.evb9959_RegMaps.rm9959_NewRegMapVals, 1, 23, adiBitValues.abvLow);
				}
			}
		}

		public int ClockMultiplier {
			get {
				string sRegVal;
				sRegVal = GetRegMapValue(AD9959EvalBd.evb9959_RegMaps.rm9959_NewRegMapVals, 1);
				sRegVal = sRegVal.Substring(1, 5);
				return BinString2Int(sRegVal);
				
			}
			private set {
				// this is private so that it can only be set in constructor
				string sRegVal;
				sRegVal = GetRegMapValue(AD9959EvalBd.evb9959_RegMaps.rm9959_NewRegMapVals, 1);
				if ((value < 4) || (value > 20)) {
					sRegVal = sRegVal.Substring(0, 1) + "00001" + sRegVal.Substring(RegLength[1] - 18, 18);
					if ((value > 1) && (value < 4)) {
						throw new ApplicationException("Clock multiplier must be 0, 1, or 4 thru 20");
					}
				}
				else {
					sRegVal = sRegVal.Substring(0, 1) + 
								Int2BinString(value, 5) + 
								sRegVal.Substring(RegLength[1] - 18, 18);
				}
				SetRegMapValue(evb9959_RegMaps.rm9959_NewRegMapVals, 1, sRegVal);
			}
		}
		//
		#endregion Reg 1

		#region Register 2 (FR2)
		//
		public bool AutoClearAllSweep {
			get {
				int bit;
				bit = (int)GetRegMapBitValue(evb9959_RegMaps.rm9959_CurRegMapVals, 2, 15);
				if (bit == 1) {
					return true;
				}
				else {
					return false;
				}
			}
			set {
				// Set the value in the register map , note that you must perform a serial load
				// with that new register data for it to take effect
				if (value) {
					SetRegMapBitValue(evb9959_RegMaps.rm9959_NewRegMapVals, 2, 15, adiBitValues.abvHigh);
				}
				else {
					SetRegMapBitValue(evb9959_RegMaps.rm9959_NewRegMapVals, 2, 15, adiBitValues.abvLow);
				}
			}
		}
		//
		public bool AutoClearAllPhase {
			get {
				int bit;
				bit = (int)GetRegMapBitValue(evb9959_RegMaps.rm9959_CurRegMapVals, 2, 13);
				if (bit == 1) {
					return true;
				}
				else {
					return false;
				}
			}
			set {
				// Set the value in the register map , note that you must perform a serial load
				// with that new register data for it to take effect
				if (value) {
					SetRegMapBitValue(evb9959_RegMaps.rm9959_NewRegMapVals, 2, 13, adiBitValues.abvHigh);
				}
				else {
					SetRegMapBitValue(evb9959_RegMaps.rm9959_NewRegMapVals, 2, 13, adiBitValues.abvLow);
				}
			}
		}
		//
		#endregion Reg 2

		#region Register 3 (CFR)
		//
		public void EnableFreqSweepMode(int channel, bool noDwell) {

			SelectChannel(channel);
			// set sweep mode to "freq"
			string freqMode = "10";
			string sRegVal;
			sRegVal = GetRegMapValue(AD9959EvalBd.evb9959_RegMaps.rm9959_NewRegMapVals, 3, channel);
			sRegVal = freqMode + sRegVal.Substring(2);
			// set linear sweep nodwell and enable
			string linearSweep;
			if (noDwell) {
				linearSweep = "11000011";
			}
			else {
				linearSweep = "01000011";
			}
			sRegVal = sRegVal.Substring(0, 8) + linearSweep + sRegVal.Substring(16);
			// for nodwell mode, set auto clear phase, only
			// otherwise, set auto clear sweep, too
			string acClear;
			if (noDwell) {
				acClear = "00101";
			}
			else {
				acClear = "10101";
			}
			sRegVal = sRegVal.Substring(0, 19) + acClear;

			SetRegMapValue(evb9959_RegMaps.rm9959_NewRegMapVals, 3, sRegVal, GetChMaskVal(channel));
		}

		public void EnableSingleFreqMode(int channel) {
			SelectChannel(channel);
			// set sweep mode to "freq"
			string freqMode = "00000000";
            // autoclear phase accumulator
			string sRegVal = freqMode + "00000011" + "00000101";
			SetRegMapValue(evb9959_RegMaps.rm9959_NewRegMapVals, 3, sRegVal, GetChMaskVal(channel));
		}

		//
		#endregion Reg 3

		#region Registers 4 and up (Frequencies)
		//
		public void SetFreq0(double freqMHz, int channel) {
			double newFreq;
			SetFreq0(freqMHz, out newFreq, channel);
		}

		public void SetFreq0(double freqMHz, out double newFreq, int channel) {
			//double newFreq;
			string sRegVal = GetFreqBinaryString(freqMHz, out newFreq);
			SetRegMapValue(evb9959_RegMaps.rm9959_NewRegMapVals, 4, sRegVal, GetChMaskVal(channel));
			_startFreqMHz[channel] = newFreq;
		}

		public double GetFreq0(int channel) {
			string sRegVal = GetRegMapValue(evb9959_RegMaps.rm9959_CurRegMapVals, 4);
			double freq = GetFreqFromString(sRegVal);
			return freq;
		}

		public void SetFreq1(double freqMHz, int channel) {
			double newFreq;
			SetFreq1(freqMHz, out newFreq, channel);
		}

		public void SetFreq1(double freqMHz, out double newFreq, int channel) {
			//double newFreq;
			string sRegVal = GetFreqBinaryString(freqMHz, out newFreq);
			SetRegMapValue(evb9959_RegMaps.rm9959_NewRegMapVals, 0x0A, sRegVal, GetChMaskVal(channel));
			_endFreqMHz[channel] = newFreq;
		}

		public double GetFreq1(int channel) {
			string sRegVal = GetRegMapValue(evb9959_RegMaps.rm9959_CurRegMapVals, 0x0A);
			double freq = GetFreqFromString(sRegVal);
			return freq;
		}

		public void SetFreqSweepFreqDelta(double freqMHz, int channel) {
			double newFreq;
			SetFreqSweepFreqDelta(freqMHz, out newFreq, channel);
			//string sRegVal = GetFreqBinaryString(freqMHz, out newFreq);
			//SetRegMapValue(evb9959_RegMaps.rm9959_NewRegMapVals, 0x08, sRegVal, GetChMaskVal(channel));
		}

		public void SetFreqSweepFreqDelta(double freqMHz, out double newFreq, int channel) {
			//double newFreq;
			string sRegVal = GetFreqBinaryString(freqMHz, out newFreq);
			SetRegMapValue(evb9959_RegMaps.rm9959_NewRegMapVals, 0x08, sRegVal, GetChMaskVal(channel));
			_deltaFreqMHz[channel] = newFreq;
		}

		public void SetFreqSweepTimeDelta(double timeUsec, int channel) {
			double newTimeUsec;
			SetFreqSweepTimeDelta(timeUsec, out newTimeUsec, channel);
		}

		public void SetFreqSweepTimeDelta(double timeUsec, out double newTimeUsec, int channel) {
			//double newTimeUsec;
			string sRegVal = GetTimeBinaryString0(timeUsec, SysClockMHz, out newTimeUsec, channel);
			/*
			double timeVal = timeUsec * SysClockMHz / 4.0;
			int timeInt = (int)(timeVal + 0.5);
			if (timeInt > 255) {
				throw new ApplicationException("Ramp time delta > 255 clocks");
			}
			newTimeUsec = timeInt / (SysClockMHz / 4.0);
			if (Tools.RoundToSignificantDigits(timeVal, 5) != (double)timeInt) {
				throw new ApplicationException("Freq Sweep Time Delta NOT multiple of " + DeltaTGranularityUsec.ToString() + " usec");
			}
			string timeString = Int2BinString(timeInt, 8);
			string sRegVal = "00000000" + timeString; // no fall time set
			*/
			SetRegMapValue(evb9959_RegMaps.rm9959_NewRegMapVals, 0x07, sRegVal, GetChMaskVal(channel));
			_deltaTimeUsec[channel] = newTimeUsec;
		}

		public static string GetTimeBinaryString0(double timeUsec, double sysClockMHz, out double newTimeUsec, int channel) {
			double timeVal = timeUsec * sysClockMHz / 4.0;
			int timeInt = (int)(timeVal + 0.5);
			if (timeInt > 255) {
				throw new ApplicationException("Ramp time delta > 255 clocks");
			}
			newTimeUsec = timeInt / (sysClockMHz / 4.0);
			if (Tools.RoundToSignificantDigits(timeVal, 5) != (double)timeInt) {
				throw new ApplicationException("Freq Sweep Time Delta NOT multiple of Sync Clock period.");
			}
			string timeString = Int2BinString0(timeInt, 8);
			string sRegVal = "00000000" + timeString; // no fall time set
			return sRegVal;
		}

		public string junk(double f, double clock, out double newF) {
			return GetFreqBinaryString(f, out newF);
		}

		/// <summary>
		/// GetFreqBinaryString
		/// returns 32-character string of 1's and 0's
		/// that represent the binary value stored
		/// in DDS frequency registers
		/// </summary>
		/// <remarks>
		/// Made a static version so that an external program can
		/// find out how desired frequency will be modified (newFreqMHz)
		/// when set into registers - without having to access
		/// the actual DDS hardware.
		/// </remarks>
		/// <param name="freqMHz"></param>
		/// <param name="clockMHz">System clock frequency in MHz</param>
		/// <param name="newFreqMHz"></param>
		/// <returns></returns>
		public static string GetFreqBinaryString0(double freqMHz, double clockMHz, out double newFreqMHz) {
			string freqString;
			int freqInt;
			double freqValue;
			double scaleFactor = Math.Pow(2.0, 32.0);
			freqValue = freqMHz * scaleFactor / clockMHz;
			freqInt = (int)(freqValue + 0.5);
			newFreqMHz = (double)freqInt * clockMHz / scaleFactor;
			freqString = Int2BinString0((int)(freqValue + 0.5), 32);
			return freqString;
		}

		/// <summary>
		/// Non-static version of GetFreqBinaryString()
		/// </summary>
		/// <param name="freqMHz"></param>
		/// <param name="newFreqMHz"></param>
		/// <returns></returns>
		public string GetFreqBinaryString(double freqMHz, out double newFreqMHz) {
			return GetFreqBinaryString0(freqMHz, SysClockMHz, out newFreqMHz);
			/*
			string freqString;
			int freqInt;
			double freqValue;
			double scaleFactor = Math.Pow(2.0, 32.0);
			freqValue = freqMHz * scaleFactor / SysClockMHz;
			freqInt = (int)(freqValue + 0.5);
			newFreqMHz = (double)freqInt * SysClockMHz / scaleFactor;
			freqString = Int2BinString((int)(freqValue + 0.5), 32);
			return freqString;
			 * */
		}

		public double GetFreqFromString(string regVal) {
			double scaleFactor = Math.Pow(2.0, 32.0);
			int freqInt = BinString2Int(regVal);
			double newFreqMHz = (double)freqInt * SysClockMHz / scaleFactor;
			return newFreqMHz;
		}

        public static string GetPhaseBinaryString(double phaseDeg, out double newPhaseDeg) {
            // phase is a 14-bit value in 16-bit register 5
            double scaleFactor = Math.Pow(2.0, 14.0);
            double phaseValue = phaseDeg * (scaleFactor / 360.0);
            int phaseInt = (int)(phaseValue + 0.5);
            newPhaseDeg = (double)phaseInt * 360.0 / scaleFactor;
            string phaseString = Int2BinString0(phaseInt, 16);
            return phaseString;
        }

        public double GetPhaseFromString(string regVal) {
            double scaleFactor = Math.Pow(2.0, 14.0);
            int phaseInt = BinString2Int(regVal);
            double newPhaseDeg = (double)phaseInt * 360.0 / scaleFactor;
            return newPhaseDeg;
        }

		//
		#endregion register 4 and up

		#region Register 25 (0x19)

		// Reads from Register 19H which contains the Product ID Code
		public string GetProductType() {
			string sChipID;	// Variable to store the Product ID

			sChipID = USBSerialRead(0x19, 1);	// Read the Product ID Register
			sChipID = sChipID.Substring(0, 5);  // Trim the data down to the Product ID
			switch (sChipID) {
				case "11010":			// AD9959E - Evaluation Product
					sChipID = "AD9959E";
					mvarDDSType = sChipID;
					break;
				case "11011":			 // AD9959
					sChipID = "AD9959";
					mvarDDSType = sChipID;
					break;
				case "10011":			 // AD9958 - Channel 0 and 3, Channel 0 and 1 New Silicon
					sChipID = "AD9958 CH01";
					mvarDDSType = sChipID;
					break;
				case "11001":			 // AD9958 - Channel 1 and 2, Channel 2 and 3 New Silicon
					sChipID = "AD9958 CH23";
					mvarDDSType = sChipID;
					break;
				case "01011":			 // Spur reduction capable Concept Product
					sChipID = "AD9959Exp";
					mvarDDSType = sChipID;
					break;
				default:
					sChipID = "None";
					mvarDDSType = "None";
					break;
			}
			// Return the ChipID
			return sChipID;
		}

		#endregion Reg 25

		#region Frequency sweep
		//

		// if an external pbx is provided to create PCMode switch
		//private PbxControllerCard _pbx = null;

        ///////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Replaces RunFmCwFreqSweeps
        /// Uses universal static method CalculateDDSValues()
        /// </summary>
        public void StartAllFrequencies() {
            if (_param == null) {
                throw new ApplicationException("AD9959EvalBd.StartAllFrequencies() needs PopParameters");
            }

            DDSCalculatedValues ddsOut;
            CalculateDDSValues(DDSInput(_param), out ddsOut);

            _startFreqMHz[0] = ddsOut.StartFreq1MHz;
            _endFreqMHz[0] = ddsOut.EndFreq1MHz;
            _deltaFreqMHz[0] = ddsOut.DeltaFreqHz / 1.0e6;
            _deltaTimeUsec[0] = ddsOut.DeltaTNsec / 1.0e3;
            _startFreqMHz[1] = ddsOut.StartFreq2MHz;
            _endFreqMHz[1] = ddsOut.EndFreq2MHz;
            _deltaFreqMHz[1] = ddsOut.DeltaFreqHz / 1.0e6;
            _deltaTimeUsec[1] = ddsOut.DeltaTNsec / 1.0e3;

            LoadFreqSweep(0);
            LoadFreqSweep(1);

            SetSingleTones(_param);

            StartFreqSweep();
            StartSingleTones(_param);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////

		public void RunFmCwFreqSweeps(PopParameters param) {
			RunFmCwFreqSweeps(param, null);
		}

		public void RunFmCwFreqSweeps(PopParameters param, object pbx) {
			//_pbx = pbx;
			//if (_pbx == null) {
				//MessageBox.Show("pbx is null in RunFmCwFreqSweeps");
			//}
			double centerFreqMhz, offsetHz, sweepRateHzUsec, sweepTimeUsec, sweepTimeMidSamplesUsec;
			int timeStepClocks;
			GetSweepFromPopParameters(param,
							    		out centerFreqMhz,
						    			out sweepRateHzUsec,
					    				out offsetHz,
				    					out timeStepClocks,
			    						out sweepTimeUsec,
                                        out sweepTimeMidSamplesUsec);
			//_AD9959EvalBd.SetFreqSweepParameters(0, centerFreqMhz, sweepRateHzUsec, 0, timeStepClocks, sweepTimeUsec);
			//_AD9959EvalBd.SetFreqSweepParameters(1, centerFreqMhz, sweepRateHzUsec, offsetHz, timeStepClocks, sweepTimeUsec);
			SetFrequencySweep(0, centerFreqMhz, sweepRateHzUsec, 0, timeStepClocks, sweepTimeUsec, sweepTimeMidSamplesUsec);
			SetFrequencySweep(1, centerFreqMhz, sweepRateHzUsec, offsetHz, timeStepClocks, sweepTimeUsec, sweepTimeMidSamplesUsec);
            SetSingleTones(param);
			//MessageBox.Show("DDS loaded, in PC mode. Click to start running in manual mode.");
			StartFreqSweep();
            StartSingleTones(param);
		}

        private void StartSingleTones(PopParameters par) {
            double freqHz3 = par.SystemPar.RadarPar.FmCwParSet[0].DDS3FreqStartHz;
            if (freqHz3 > 0.0) {
                P2Bit = adiBitValues.abvHigh;
            }
            double freqHz4 = par.SystemPar.RadarPar.FmCwParSet[0].DDS4FreqStartHz;
            if (freqHz4 > 0.0) {
                P3Bit = adiBitValues.abvHigh;
            }

        }

        private void SetSingleTones(PopParameters par) {

            double freqHz = par.SystemPar.RadarPar.FmCwParSet[0].DDS3FreqStartHz;
            double phaseDeg = par.SystemPar.RadarPar.FmCwParSet[0].DDS3PhaseDeg;
            SetSingleToneChannel(2, freqHz, phaseDeg);
            freqHz = par.SystemPar.RadarPar.FmCwParSet[0].DDS4FreqStartHz;
            phaseDeg = par.SystemPar.RadarPar.FmCwParSet[0].DDS4PhaseDeg;
            SetSingleToneChannel(3, freqHz, phaseDeg);
        }

        private void SetSingleToneChannel(int channel, double freqHz, double phaseDeg) {
            if (freqHz > 0.0) {
                EnableSingleFreqMode(channel);
                SetFreq0(freqHz / 1.0e6, channel);
                SetPhase(phaseDeg, channel);
                LoadAllDDSRegisters();
            }
        }

        private void SetPhase(double phaseDeg, int channel) {
            double newPhase;
            string sRegVal = GetPhaseBinaryString(phaseDeg, out newPhase);
            SetRegMapValue(evb9959_RegMaps.rm9959_NewRegMapVals, 5, sRegVal, GetChMaskVal(channel));
        }

		public void SetFrequencySweep(int channel,
										double centerFreqMHz,
										double sweepRateHzUsec,
										double offsetHz,
										int deltaTClocks,
										double sweepTimeUsec,
                                        double sweepMidTimeUsec) {

			// make sure DDS board is in PCMode
			//SetPCMode(true, _pbx);
			//MessageBox.Show("Now start freq sweep channel " + channel.ToString());
			// compute startFreq, endFreq, deltaF, deltaT
			SetFreqSweepParameters(channel, centerFreqMHz, sweepRateHzUsec, offsetHz, deltaTClocks, sweepTimeUsec, sweepMidTimeUsec);

			// load DDS channel frequency sweep parameters onto board
			LoadFreqSweep(channel);
			
		}

		public void StartFreqSweep() {
			/*
			// after calling this method,
			//	cannot modify the DDS from the PC
			//	without calling setPCMode(true)
			//MessageBox.Show("Enable external P lines: Setting PCMode false");
			SetPCMode(false, _pbx);
			// now sweep will trigger off P0, P1 lines
			*/

			// We are now always running in PCMode.
			// To drive P0_U with external signal, we need USB P0_U to be high.
			P0Bit = adiBitValues.abvHigh;
			P1Bit = adiBitValues.abvHigh;
        }

		/// <summary>
		/// Set this object's frequency sweep properties
		///		startF, endF, deltaF, deltaT
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="sweepRateHzUsec"></param>
		/// <param name="offsetHz"></param>
		/// <param name="deltaTClocks"></param>
        /// <param name="sweepTimeUsec"></param>
        /// <param name="sweepCenterFreqTimeUsec">Time during the sweep where center freq should be.</param>
        public void SetFreqSweepParameters(int channel,
											double centerFreqMHz,
											double sweepRateHzUsec,
											double offsetHz,
											int deltaTClocks,
											double sweepTimeUsec,
                                            double sweepCenterFreqTimeUsec) {
			_centerFreqMHz[channel] = centerFreqMHz;
			double deltaTUsec = deltaTClocks * _syncPeriodUsec;
            // previous to 3.12 startFreq calculated to put centerFreq at half of sweep time from beginning of IPP
            // this was incorrect.
            // Modified to put center freq at middle of IPP
            //double startFreqMHz = centerFreqMHz - sweepRateHzUsec * sweepTimeUsec / 2.0e6 + (offsetHz / 1.0e6);  // before 3.12
            //double startFreqMHz = centerFreqMHz - sweepRateHzUsec * _ippUs / 2.0e6 + (offsetHz / 1.0e6);            // 3.12
            double startFreqMHz = centerFreqMHz - sweepRateHzUsec * sweepCenterFreqTimeUsec / 1.0e6 + (offsetHz / 1.0e6);            // after 3.12
            double steps = sweepTimeUsec / deltaTUsec;
			double rampStepFreqMHz = sweepRateHzUsec * deltaTUsec / 1.0e6;
			double endFreqMHz = startFreqMHz + steps * rampStepFreqMHz;
			double newStartFreqMHz, newEndFreqMHz, newDeltaFreqMHz;
			GetFreqBinaryString0(startFreqMHz, _sysClockMHz, out newStartFreqMHz);
			GetFreqBinaryString0(endFreqMHz, _sysClockMHz, out newEndFreqMHz);
			GetFreqBinaryString0(rampStepFreqMHz, _sysClockMHz, out newDeltaFreqMHz);
			_startFreqMHz[channel] = newStartFreqMHz;
			_endFreqMHz[channel] = newEndFreqMHz;
			_deltaFreqMHz[channel] = newDeltaFreqMHz;
			_deltaTimeUsec[channel] = deltaTClocks * _syncPeriodUsec;
		}

		/// <summary>
		/// Load DDS with freq sweep property values and start sweep
		/// </summary>
		/// <param name="channel"></param>
		public void LoadFreqSweep(int channel) {
			double newStartFreqMHz, newEndFreqMHz, newDeltaFreqMHz, newTimeDeltaUsec;
			EnableFreqSweepMode(channel, true);
			SetFreq0(_startFreqMHz[channel], out newStartFreqMHz, channel);
			SetFreq1(_endFreqMHz[channel], out newEndFreqMHz, channel);
			SetFreqSweepFreqDelta(_deltaFreqMHz[channel], out newDeltaFreqMHz, channel);
			SetFreqSweepTimeDelta(_deltaTimeUsec[channel], out newTimeDeltaUsec, channel);
			LoadAllDDSRegisters();
		}

		public void GetSweepFromPopParameters(PopParameters param,
												out double centerFreqMHz,
												out double sweepRateHzUsec,
												out double offsetHz,
												out int timeStepClocks,
												out double sweepTimeUs,
                                                out double midSweepTimeUs) {
			centerFreqMHz = param.SystemPar.RadarPar.FmCwParSet[0].TxSweepCenterFreqMHz;
			sweepRateHzUsec = param.SystemPar.RadarPar.FmCwParSet[0].TxSweepRateHzUSec;
			timeStepClocks = param.SystemPar.RadarPar.FmCwParSet[0].TxSweepTimeStepClocks;
			offsetHz = param.SystemPar.RadarPar.FmCwParSet[0].TxSweepOffsetHz;
			int nSamples = param.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts;
			int delayNs = param.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleDelayNs;
			int spacingNs = param.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleSpacingNs;
            double preTRUs = param.SystemPar.RadarPar.PBConstants.PBPreTR/1000.0;
            // POPREV: error in txUs fixed in rev 3.13 (tx in beamparset is resolution, not pulse length)
            //double txUs = param.SystemPar.RadarPar.BeamParSet[0].PulseWidthNs/1000.0;
            double txUs = 1.0;  // fixed pulse length in FMCW
            double ippUs = param.SystemPar.RadarPar.FmCwParSet[0].IppMicroSec;
			// dac:  add preTR and TX (1 usec) to lastSampleUs
			// and make sure sweepTimeUs is less than IPP
			double lastSampleUs = (delayNs + (nSamples - 1) * spacingNs) / 1000.0;
			sweepTimeUs = preTRUs + txUs + lastSampleUs + 10.0;
            if (sweepTimeUs >= ippUs) {
                throw new ApplicationException("Sweep extends beyond IPP");
            }
            // the time of the sweep where the middle of samples are
            midSweepTimeUs = preTRUs + txUs + delayNs / 1000.0 + (nSamples - 1) * spacingNs / 2000.0;
		}

		//
		#endregion Frequency sweep

		/////////////////////////////////////////////////////////////////////////////////////////


		// Builds the instruction byte
		// bWR = 0-Write, 1-Read
		// RegAddr = Valid Register Address
		public string BuildInstruct(int bWR , int RegAddr) {
			
			// Make sure that bWR is 0 or 1
			if (bWR != 0) {
				bWR = 1;
			} else {
				bWR = 0;
			}

			//return Convert.ToString(bWR) + Convert.ToString(RegAddr, 2);
			return Convert.ToString(bWR) + Int2BinString(RegAddr, 7);
		}



		// Gets the most significant byte from iVal and returns it
		private byte MSB(short iVal) {
			return (byte)(iVal / 256);
		}
		// Gets the lease significant byte from iVal and returns it
		private byte LSB(short iVal) {
			return (byte)(iVal & 0xFF);
		}

		// Flips a string
		public string FlipString(string MyStr) {
			string flipped = "";
			for (int cntr = MyStr.Length; cntr >=1; cntr--) {
				flipped = flipped + MyStr.Substring(cntr - 1, 1);
			}
			return flipped;
		}

		public int GetNumRegBits(int reg) {
			return RegLength[reg];
		}

		public void CloseUSB() {
			if (_usbDevice != null) {
				_usbDevice.Close();
			}

		}

	} // end class AD9959EvalBd

}

