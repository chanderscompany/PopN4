using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace DACarter.NOAA.Hardware {

	/// <summary>
	/// This class accesses the FT2232 or FT4232 chip,
	/// (such as on the DLP USB1232 card)
	/// to provide an SPI interface to an external device.
	/// </summary>
	public class FT2232SPI {

		private enum HI_SPEED_DEVICE_TYPES// : uint
		{
			FT2232H_DEVICE_TYPE = 1,
			FT4232H_DEVICE_TYPE = 2
		};

		private const uint FTC_SUCCESS = 0;
		private const uint FTC_DEVICE_IN_USE = 27;

		private const uint MAX_NUM_DEVICE_NAME_CHARS = 100;
		private const uint MAX_NUM_CHANNEL_CHARS = 5;

		private const uint MAX_NUM_DLL_VERSION_CHARS = 10;
		private const uint MAX_NUM_ERROR_MESSAGE_CHARS = 100;

		// To communicate with the 93LC56(2048 word) EEPROM, the maximum frequency the clock can be set is 1MHz 
		private const uint MAX_FREQ_93LC56_CLOCK_DIVISOR = 29;    // equivalent to 1MHz

		private const bool ADBUS3ChipSelect = false;

		private const uint ADBUS2DataIn = 0;

		private const int WRITE_CONTROL_BUFFER_SIZE = 4;
		private const int WRITE_DATA_BUFFER_SIZE = 4;
		private const int READ_DATA_BUFFER_SIZE = 65536;
		private const int READ_CMDS_DATA_BUFFER_SIZE = 131071;

		private const uint SPIEWENCmdIndex = 0;
		private const uint SPIEWDSCmdIndex = 1;
		private const uint SPIERALCmdIndex = 2;

		private const uint MAX_SPI_93LC56_CHIP_SIZE_IN_WORDS = 128;

		private const uint NUM_93LC56B_CMD_CONTOL_BITS = 11;
		private const uint NUM_93LC56B_CMD_CONTOL_BYTES = 2;

		private const uint NUM_93LC56B_CMD_DATA_BITS = 16;
		private const uint NUM_93LC56B_CMD_DATA_BYTES = 2;

		//**************************************************************************
		//
		// TYPE DEFINITIONS
		//
		//**************************************************************************

		public struct FTC_CHIP_SELECT_PINS {
			public bool bADBUS3ChipSelectPinState;
			public bool bADBUS4GPIOL1PinState;
			public bool bADBUS5GPIOL2PinState;
			public bool bADBUS6GPIOL3PinState;
			public bool bADBUS7GPIOL4PinState;
		}

		public struct FTC_INPUT_OUTPUT_PINS {
			public bool bPin1InputOutputState;
			public bool bPin1LowHighState;
			public bool bPin2InputOutputState;
			public bool bPin2LowHighState;
			public bool bPin3InputOutputState;
			public bool bPin3LowHighState;
			public bool bPin4InputOutputState;
			public bool bPin4LowHighState;
		}

		public struct FTH_INPUT_OUTPUT_PINS {
			public bool bPin1InputOutputState;
			public bool bPin1LowHighState;
			public bool bPin2InputOutputState;
			public bool bPin2LowHighState;
			public bool bPin3InputOutputState;
			public bool bPin3LowHighState;
			public bool bPin4InputOutputState;
			public bool bPin4LowHighState;
			public bool bPin5InputOutputState;
			public bool bPin5LowHighState;
			public bool bPin6InputOutputState;
			public bool bPin6LowHighState;
			public bool bPin7InputOutputState;
			public bool bPin7LowHighState;
			public bool bPin8InputOutputState;
			public bool bPin8LowHighState;
		}

		public struct FTC_LOW_HIGH_PINS {
			public bool bPin1LowHighState;
			public bool bPin2LowHighState;
			public bool bPin3LowHighState;
			public bool bPin4LowHighState;
		}

		public struct FTH_LOW_HIGH_PINS {
			public bool bPin1LowHighState;
			public bool bPin2LowHighState;
			public bool bPin3LowHighState;
			public bool bPin4LowHighState;
			public bool bPin5LowHighState;
			public bool bPin6LowHighState;
			public bool bPin7LowHighState;
			public bool bPin8LowHighState;
		}

		public struct FTC_INIT_CONDITION {
			public bool bClockPinState;
			public bool bDataOutPinState;
			public bool bChipSelectPinState;
			public bool ChipSelectPin;
		}

		public struct FTC_WAIT_DATA_WRITE {
			public bool bWaitDataWriteComplete;
			public uint WaitDataWritePin;
			public bool bDataWriteCompleteState;
			public uint DataWriteTimeoutmSecs;
		}

		public struct FTC_HIGHER_OUTPUT_PINS {
			public bool bPin1State;
			public bool bPin1ActiveState;
			public bool bPin2State;
			public bool bPin2ActiveState;
			public bool bPin3State;
			public bool bPin3ActiveState;
			public bool bPin4State;
			public bool bPin4ActiveState;
		}

		public struct FTH_HIGHER_OUTPUT_PINS {
			public bool bPin1State;
			public bool bPin1ActiveState;
			public bool bPin2State;
			public bool bPin2ActiveState;
			public bool bPin3State;
			public bool bPin3ActiveState;
			public bool bPin4State;
			public bool bPin4ActiveState;
			public bool bPin5State;
			public bool bPin5ActiveState;
			public bool bPin6State;
			public bool bPin6ActiveState;
			public bool bPin7State;
			public bool bPin7ActiveState;
			public bool bPin8State;
			public bool bPin8ActiveState;
		}

		public struct FTC_CLOSE_FINAL_STATE_PINS {
			public bool bTCKPinState;
			public bool bTCKPinActiveState;
			public bool bTDIPinState;
			public bool bTDIPinActiveState;
			public bool bTMSPinState;
			public bool bTMSPinActiveState;
		}

		public int ChipSelectLine;


		//**************************************************************************
		//
		// FUNCTION IMPORTS FROM FTCI2C DLL
		//
		//**************************************************************************

		// Built-in Windows API functions to allow us to dynamically load our own DLL.
		[DllImportAttribute("ftcspi.dll", EntryPoint = "SPI_GetDllVersion", CallingConvention = CallingConvention.Cdecl)]
		static extern uint GetDllVersion(byte[] pDllVersion, uint buufferSize);

		[DllImportAttribute("ftcspi.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint SPI_GetErrorCodeString(string language, uint statusCode, byte[] pErrorMessage, uint bufferSize);

		[DllImportAttribute("ftcspi.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint SPI_GetNumHiSpeedDevices(ref uint NumHiSpeedDevices);

		[DllImportAttribute("ftcspi.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint SPI_GetHiSpeedDeviceNameLocIDChannel(uint deviceNameIndex, byte[] pDeviceName, uint deviceNameBufferSize, ref uint locationID, byte[] pChannel, uint channelBufferSize, ref uint hiSpeedDeviceType);

		[DllImportAttribute("ftcspi.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint SPI_OpenHiSpeedDevice(string DeviceName, uint locationID, string channel, ref IntPtr pftHandle);

		[DllImportAttribute("ftcspi.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint SPI_GetHiSpeedDeviceType(IntPtr ftHandle, ref uint hiSpeedDeviceType);

		[DllImportAttribute("ftcspi.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint SPI_Close(IntPtr ftHandle);

		[DllImportAttribute("ftcspi.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint SPI_CloseDevice(IntPtr ftHandle, ref FTC_CLOSE_FINAL_STATE_PINS pCloseFinalStatePinsData);

		[DllImportAttribute("ftcspi.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint SPI_InitDevice(IntPtr ftHandle, uint clockDivisor);

		[DllImportAttribute("ftcspi.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint SPI_TurnOnDivideByFiveClockingHiSpeedDevice(IntPtr ftHandle);

		[DllImportAttribute("ftcspi.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint SPI_TurnOffDivideByFiveClockingHiSpeedDevice(IntPtr ftHandle);

		[DllImportAttribute("ftcspi.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint SPI_SetDeviceLatencyTimer(IntPtr ftHandle, byte timerValue);

		[DllImportAttribute("ftcspi.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint SPI_GetDeviceLatencyTimer(IntPtr ftHandle, ref byte timerValue);

		[DllImportAttribute("ftcspi.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint SPI_GetHiSpeedDeviceClock(uint ClockDivisor, ref uint clockFrequencyHz);

		[DllImportAttribute("ftcspi.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint SPI_GetClock(uint clockDivisor, ref uint clockFrequencyHz);

		[DllImportAttribute("ftcspi.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint SPI_SetClock(IntPtr ftHandle, uint clockDivisor, ref uint clockFrequencyHz);

		[DllImportAttribute("ftcspi.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint SPI_SetLoopback(IntPtr ftHandle, bool bLoopBackState);

		[DllImportAttribute("ftcspi.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint SPI_SetHiSpeedDeviceGPIOs(IntPtr ftHandle, ref FTC_CHIP_SELECT_PINS pChipSelectsDisableStates, ref FTH_INPUT_OUTPUT_PINS pHighInputOutputPinsData);

		[DllImportAttribute("ftcspi.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint SPI_GetHiSpeedDeviceGPIOs(IntPtr ftHandle, out FTH_LOW_HIGH_PINS pHighPinsInputData);

        [DllImportAttribute("ftcspi.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern uint SPI_WriteHiSpeedDevice(IntPtr ftHandle, ref FTC_INIT_CONDITION pWriteStartCondition, bool bClockOutDataBitsMSBFirst, bool bClockOutDataBitsPosEdge, uint numControlBitsToWrite, byte[] pWriteControlBuffer, uint numControlBytesToWrite, bool bWriteDataBits, uint numDataBitsToWrite, byte[] pWriteDataBuffer, uint numDataBytesToWrite, ref FTC_WAIT_DATA_WRITE pWaitDataWriteComplete, ref FTH_HIGHER_OUTPUT_PINS pHighPinsWriteActiveStates);

        [DllImportAttribute("ftcspi.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern uint SPI_AddHiSpeedDeviceWriteCmd(IntPtr ftHandle, ref FTC_INIT_CONDITION pWriteStartCondition, bool bClockOutDataBitsMSBFirst, bool bClockOutDataBitsPosEdge, uint numControlBitsToWrite, byte[] pWriteControlBuffer, uint numControlBytesToWrite, bool bWriteDataBits, uint numDataBitsToWrite, byte[] pWriteDataBuffer, uint numDataBytesToWrite, ref FTH_HIGHER_OUTPUT_PINS pHighPinsWriteActiveStates);

        [DllImportAttribute("ftcspi.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint SPI_ReadHiSpeedDevice(IntPtr ftHandle, ref FTC_INIT_CONDITION pReadStartCondition, bool bClockOutControBitsMSBFirst, bool bClockOutControBitsPosEdge, uint numControlBitsToWrite, byte[] pWriteControlBuffer, uint numControlBytesToWrite, bool bClockInDataBitsMSBFirst, bool bClockInDataBitsPosEdge, uint numDataBitsToRead, byte[] pReadDataBuffer, out uint pnumDataBytesReturned, ref FTH_HIGHER_OUTPUT_PINS pHighPinsReadActiveStates);

		[DllImportAttribute("ftcspi.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint SPI_ClearDeviceCmdSequence(IntPtr ftHandle);

		[DllImportAttribute("ftcspi.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint SPI_AddHiSpeedDeviceReadCmd(IntPtr ftHandle, ref FTC_INIT_CONDITION pReadStartCondition, bool bClockOutControBitsMSBFirst, bool bClockOutControBitsPosEdge, uint numControlBitsToWrite, byte[] pWriteControlBuffer, uint numControlBytesToWrite, bool bClockInDataBitsMSBFirst, bool bClockInDataBitsPosEdge, uint numDataBitsToRead, ref FTH_HIGHER_OUTPUT_PINS pHighPinsReadActiveStates);

		[DllImportAttribute("ftcspi.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern uint SPI_ExecuteDeviceCmdSequence(IntPtr ftHandle, byte[] pReadCmdSequenceDataBuffer, out uint pnumDataBytesReturned);

		uint ftStatus = FTC_SUCCESS;
		uint numHiSpeedDevices = 0; // 32-bit unsigned integer
		uint hiSpeedDeviceIndex = 0;
		uint locationID = 0;
		uint hiSpeedDeviceType = 0;
		string hiSpeedDeviceName = null;
		string hiSpeedChannel = null;
		//byte timerValue = 0;
		//bool bLoopBackState = false;
		FTC_CHIP_SELECT_PINS ChipSelectsDisableStates;
		FTH_INPUT_OUTPUT_PINS HighInputOutputPins;
		FTH_LOW_HIGH_PINS HighPinsInputData;
		FTC_INIT_CONDITION WriteStartCondition;
		FTC_WAIT_DATA_WRITE WaitDataWriteComplete;
		FTH_HIGHER_OUTPUT_PINS HighPinsWriteActiveStates;
		//short DataWord = 0; // short is a Signed 16-bit integer
		//uint NumDataBitsToWrite = 0;
		//uint NumDataBytesToWrite = 0;
		//int WriteDataWordAddress;
		//int ControlLocAddress1;
		//int ControlLocAddress2;
		//bool bWriteWait = true;

		FTC_INIT_CONDITION ReadStartCondition;
		int ReadDataIndex = 0;
		int ReadDataWordAddress = 0;
		FTH_HIGHER_OUTPUT_PINS HighPinsReadActiveStates;
		int ReadWordValue;
		uint NumDataBytesReturned = 0;
		short DataWordWritten = 0;
		int CharCntr = 0;
		int iLoopCntr = 0;
		string mismatchMsg = null;
		string selectedChannel;
		string DllVersion;
		String ErrorMessage;

		IntPtr ftHandle = IntPtr.Zero;
		byte[] byteHiSpeedDeviceName;
		byte[] byteHiSpeedDeviceChannel;
		byte[] ReadCmdSequenceDataBuffer;
		byte[] ReadDataBuffer;
		byte[] ReadWordData;
		byte[] WriteControlBuffer;
		byte[] WriteDataBuffer;
		byte[] byteDllVersion;
		byte[] byteErrorMessage;


		public FT2232SPI() {
			ChipSelectLine = 4;
			selectedChannel = "A";
			byteHiSpeedDeviceName = new byte[MAX_NUM_DEVICE_NAME_CHARS];
			byteHiSpeedDeviceChannel = new byte[MAX_NUM_CHANNEL_CHARS];
			ReadCmdSequenceDataBuffer = new byte[READ_CMDS_DATA_BUFFER_SIZE];
			ReadDataBuffer = new byte[READ_DATA_BUFFER_SIZE];
			ReadWordData = new byte[MAX_SPI_93LC56_CHIP_SIZE_IN_WORDS];
			WriteControlBuffer = new byte[WRITE_CONTROL_BUFFER_SIZE];
			WriteDataBuffer = new byte[WRITE_DATA_BUFFER_SIZE];
			byteDllVersion = new byte[MAX_NUM_DLL_VERSION_CHARS];
			byteErrorMessage = new byte[MAX_NUM_ERROR_MESSAGE_CHARS];

			ftStatus = GetDllVersion(byteDllVersion, MAX_NUM_DLL_VERSION_CHARS);
			DllVersion = Encoding.ASCII.GetString(byteDllVersion);
			// Trim strings to first occurrence of a null terminator character
			DllVersion = DllVersion.Substring(0, DllVersion.IndexOf("\0"));

			ftStatus = SPI_GetNumHiSpeedDevices(ref numHiSpeedDevices);
			if ((ftStatus != FTC_SUCCESS) || (numHiSpeedDevices < 1)) {
				throw new ApplicationException("Cannot find FT2232 device.");
			}

			do {
				ftStatus = SPI_GetHiSpeedDeviceNameLocIDChannel(hiSpeedDeviceIndex, byteHiSpeedDeviceName, MAX_NUM_DEVICE_NAME_CHARS, ref locationID, byteHiSpeedDeviceChannel, MAX_NUM_CHANNEL_CHARS, ref hiSpeedDeviceType);

				if (ftStatus == FTC_SUCCESS) {
					hiSpeedChannel = Encoding.ASCII.GetString(byteHiSpeedDeviceChannel);
					// Trim strings to first occurrence of a null terminator character
					hiSpeedChannel = hiSpeedChannel.Substring(0, hiSpeedChannel.IndexOf("\0"));
				}
				else {
					throw new ApplicationException("Cannot get FT2232 LocIDChannel.");
				}

				hiSpeedDeviceIndex = hiSpeedDeviceIndex + 1;
			} while ((ftStatus == FTC_SUCCESS) &&
					(hiSpeedDeviceIndex < numHiSpeedDevices) &&
					((hiSpeedChannel != null) &&
					(hiSpeedChannel != selectedChannel)));

			if (ftStatus == FTC_SUCCESS) {
				if ((hiSpeedChannel != null) && (hiSpeedChannel != selectedChannel)) {
					ftStatus = FTC_DEVICE_IN_USE;
					throw new ApplicationException("FT2232 is in use.");
				}
			}

			if (ftStatus == FTC_SUCCESS) {
				hiSpeedDeviceName = Encoding.ASCII.GetString(byteHiSpeedDeviceName);
				// Trim strings to first occurrence of a null terminator character
				hiSpeedDeviceName = hiSpeedDeviceName.Substring(0, hiSpeedDeviceName.IndexOf("\0"));

				// The ftHandle parameter is a pointer to a variable of type DWORD ie 32-bit unsigned integer
				ftStatus = SPI_OpenHiSpeedDevice(hiSpeedDeviceName, locationID, hiSpeedChannel, ref ftHandle);

				if (ftStatus == FTC_SUCCESS) {
					ftStatus = SPI_GetHiSpeedDeviceType(ftHandle, ref hiSpeedDeviceType);
				}
				else {
					throw new ApplicationException("Cannot open FT2232 device.");
				}
			}

			if ((ftHandle == IntPtr.Zero) || (ftStatus != FTC_SUCCESS)) {
				throw new ApplicationException("Error opening FT2232 device.");
			}

			InitDevice(MAX_FREQ_93LC56_CLOCK_DIVISOR);

		}  // end constructor


		/// <summary>
		/// 
		/// </summary>
		/// <param name="clockDivisor">SCLK freq is 30 MHz / (clockDivisor+1)</param>
		public void InitDevice(uint clockDivisor) {
			ftStatus = SPI_InitDevice(ftHandle, clockDivisor);
			// Must set the chip select disable states for all the SPI devices connected to the FT2232C dual device
			// DAC chip select state is disabled high (true):
			ChipSelectsDisableStates.bADBUS3ChipSelectPinState = true;
			ChipSelectsDisableStates.bADBUS4GPIOL1PinState = true;
			ChipSelectsDisableStates.bADBUS5GPIOL2PinState = true;
			ChipSelectsDisableStates.bADBUS6GPIOL3PinState = true;
			ChipSelectsDisableStates.bADBUS7GPIOL4PinState = true;

			// only 1st 5 GPIOH lines used in DLP-USB1232H
			HighInputOutputPins.bPin1InputOutputState = true;		// set pin1 to input(false) or output(true) mode
			if (ChipSelectLine == 0) {
				HighInputOutputPins.bPin1LowHighState = true;		// if output mode, set hi(true) or low(false)
			}
			else {
				HighInputOutputPins.bPin1LowHighState = false;		// if output mode, set hi(true) or low(false)
			}

			HighInputOutputPins.bPin2InputOutputState = true;
			if (ChipSelectLine == 1) {
				HighInputOutputPins.bPin2LowHighState = true;
			}
			else {
				HighInputOutputPins.bPin2LowHighState = false;
			}

			HighInputOutputPins.bPin3InputOutputState = true;
			if (ChipSelectLine == 2) {
				HighInputOutputPins.bPin3LowHighState = true;
			}
			else {
				HighInputOutputPins.bPin3LowHighState = false;
			}

			HighInputOutputPins.bPin4InputOutputState = true;
			if (ChipSelectLine == 3) {
				HighInputOutputPins.bPin4LowHighState = true;
			}
			else {
				HighInputOutputPins.bPin4LowHighState = false;
			}

			HighInputOutputPins.bPin5InputOutputState = true;
			if (ChipSelectLine == 4) {
				HighInputOutputPins.bPin5LowHighState = true;
			}
			else {
				HighInputOutputPins.bPin5LowHighState = false;
			}

			ftStatus = SPI_SetHiSpeedDeviceGPIOs(ftHandle, ref ChipSelectsDisableStates, ref HighInputOutputPins);
		}

		/// <summary>
		/// Output a pulse (low-high-low) on a GPIOHx line.
		/// All other lines are set low and remain low.
        /// except chip select line is brought low.
		/// Only lines 0-4 are valid for DLP-USB1232H.
		/// </summary>
		/// <param name="line"></param>
		public void PulseGPIOLine(int line) {
			if (line < 0 || line > 4) {
				throw new ApplicationException("PulseGPIO() line must be 0-4");
			}
			if (line == ChipSelectLine) {
				throw new ApplicationException("PulseGPIO() line must not be Chip Select Line");
			}
			ClearGPIO(true);
                Thread.Sleep(1);
            ClearGPIO(false);
                Thread.Sleep(1);
            SetGPIOLine(line);
			    Thread.Sleep(10);
			ClearGPIO(false);
                Thread.Sleep(1);
            ClearGPIO(true);
		}

        // same as PulseGPIOLine
        //  except chip select is left high.
        public void ToggleGPIOLine(int line) {
            if (line < 0 || line > 4) {
                throw new ApplicationException("ToggleGPIO() line must be 0-4");
            }
            if (line == ChipSelectLine) {
                throw new ApplicationException("ToggleGPIO() line must not be Chip Select Line");
            }
            ClearGPIO(true);
            if (line == 0) {
                HighInputOutputPins.bPin1LowHighState = true;
            }
            else if (line == 1) {
                HighInputOutputPins.bPin2LowHighState = true;
            }
            else if (line == 2) {
                HighInputOutputPins.bPin3LowHighState = true;
            }
            else if (line == 3) {
                HighInputOutputPins.bPin4LowHighState = true;
            }
            else if (line == 4) {
                HighInputOutputPins.bPin5LowHighState = true;
            }
            else {
                throw new ApplicationException("ToggleGPIO() line must be 0-4");
            }
            ftStatus = SPI_SetHiSpeedDeviceGPIOs(ftHandle, ref ChipSelectsDisableStates, ref HighInputOutputPins);
            ClearGPIO(true);
        }


		private void ClearGPIO(bool CSelectState) {
			//ChipSelectsDisableStates.bADBUS3ChipSelectPinState = CSelectState;
			HighInputOutputPins.bPin1LowHighState = false;		
			HighInputOutputPins.bPin2LowHighState = false;
			HighInputOutputPins.bPin3LowHighState = false;
			HighInputOutputPins.bPin4LowHighState = false;
			HighInputOutputPins.bPin5LowHighState = false;
			if (ChipSelectLine == 0) {
				HighInputOutputPins.bPin1LowHighState = CSelectState;		
			}
			if (ChipSelectLine == 1) {
				HighInputOutputPins.bPin2LowHighState = CSelectState;		
			}
			if (ChipSelectLine == 2) {
				HighInputOutputPins.bPin3LowHighState = CSelectState;		
			}
			if (ChipSelectLine == 3) {
				HighInputOutputPins.bPin4LowHighState = CSelectState;		
			}
			if (ChipSelectLine == 4) {
				HighInputOutputPins.bPin5LowHighState = CSelectState;		
			}
			ftStatus = SPI_SetHiSpeedDeviceGPIOs(ftHandle, ref ChipSelectsDisableStates, ref HighInputOutputPins);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="line">valid lines are 0-4</param>
		private void SetGPIOLine(int line) {
			//HighInputOutputPins.bPin5LowHighState = false;
			if (line == 0) {
				HighInputOutputPins.bPin1LowHighState = true;		
			}
			else if (line == 1) {
				HighInputOutputPins.bPin2LowHighState = true;
			}
			else if (line == 2) {
				HighInputOutputPins.bPin3LowHighState = true;
			}
			else if (line == 3) {
				HighInputOutputPins.bPin4LowHighState = true;
			}
			else if (line == 4) {
				HighInputOutputPins.bPin5LowHighState = true;
			}
			else {
				throw new ApplicationException("PulseGPIO() line must be 0-4");
			}
			if (ChipSelectLine == 0) {
				HighInputOutputPins.bPin1LowHighState = false;
			}
			if (ChipSelectLine == 1) {
				HighInputOutputPins.bPin2LowHighState = false;
			}
			if (ChipSelectLine == 2) {
				HighInputOutputPins.bPin3LowHighState = false;
			}
			if (ChipSelectLine == 3) {
				HighInputOutputPins.bPin4LowHighState = false;
			}
			if (ChipSelectLine == 4) {
				HighInputOutputPins.bPin5LowHighState = false;
			}
			ftStatus = SPI_SetHiSpeedDeviceGPIOs(ftHandle, ref ChipSelectsDisableStates, ref HighInputOutputPins);
		}

		/// <summary>
		/// Writes a single-byte control word followed by
		/// nBytes data word.
		/// </summary>
		/// <param name="control"></param>
		/// <param name="data"></param>
		/// <param name="nBytes"></param>
		public void WriteBytes(Int32 control, Int32 data, int nBytes) {

			WriteStartCondition.bClockPinState = false;      // normally false  //////////////////////////////////////////
			WriteStartCondition.bDataOutPinState = false;
			WriteStartCondition.bChipSelectPinState = true;
			WriteStartCondition.ChipSelectPin = ADBUS3ChipSelect;

			WaitDataWriteComplete.bWaitDataWriteComplete = true;
			WaitDataWriteComplete.WaitDataWritePin = ADBUS2DataIn;
			WaitDataWriteComplete.bDataWriteCompleteState = false;  // seems to need to be false if ext device does not indicate write complete
			WaitDataWriteComplete.DataWriteTimeoutmSecs = 5000;

			HighPinsWriteActiveStates.bPin1State = false;		// false = do not set pin1 state during write
			HighPinsWriteActiveStates.bPin1ActiveState = true;
			HighPinsWriteActiveStates.bPin2State = false;
			HighPinsWriteActiveStates.bPin2ActiveState = true;
			HighPinsWriteActiveStates.bPin3State = false;
			HighPinsWriteActiveStates.bPin3ActiveState = true;
			HighPinsWriteActiveStates.bPin4State = false;
			HighPinsWriteActiveStates.bPin4ActiveState = true;
			HighPinsWriteActiveStates.bPin5State = false;
			HighPinsWriteActiveStates.bPin5ActiveState = false;

			WriteControlBuffer[0] = (byte)(control & 0xFF);

			if (nBytes > 4) {
				throw new ApplicationException(" FT2232 SPI Cannot write more than 4-byte data");
			}

			for (int i = 0; i < nBytes; i++) {
				// start with most sig byte and shift to lower 8-bits and mask it.
				int bits8 = data >> (nBytes-1-i) * 8;
				WriteDataBuffer[i] = (byte)(bits8 & 0xFF);
			}

			ClearGPIO(false);
            ///*
            ftStatus = SPI_WriteHiSpeedDevice(
                                    ftHandle,
                                    ref WriteStartCondition,
                                    true,           // clock out data bits MSB first
                                    false,           // clock out data bits pos edge (normally false)
                                    8,
                                    WriteControlBuffer,
                                    1,
                                    true,
                                    (uint)(nBytes * 8),
                                    WriteDataBuffer,
                                    (uint)nBytes,
                                    ref WaitDataWriteComplete,
                                    ref HighPinsWriteActiveStates);
            if (ftStatus != FTC_SUCCESS) {
                throw new ApplicationException("Error writing in FT2232SPI #" + ftStatus.ToString());
            }
            //*/
            /*
            ftStatus = SPI_ClearDeviceCmdSequence(ftHandle);
            ftStatus = SPI_AddHiSpeedDeviceWriteCmd(
                        ftHandle,
                        ref WriteStartCondition,
                        true,
                        false,
                        8,
                        WriteControlBuffer,
                        1,
                        true,
                        (uint)(nBytes * 8),
                        WriteDataBuffer,
                        (uint)nBytes,
                        //ref WaitDataWriteComplete,
                        ref HighPinsWriteActiveStates);
            uint numBytesRead;
            ftStatus = SPI_ExecuteDeviceCmdSequence(
                ftHandle,
                ReadCmdSequenceDataBuffer,
                out numBytesRead
            );
            */
            ClearGPIO(true);
		}
	

		public int ReadBytes(int control, int nBytes) {

			ReadStartCondition.bClockPinState = false;   // normally false
			ReadStartCondition.bDataOutPinState = false;
			ReadStartCondition.bChipSelectPinState = true;
			ReadStartCondition.ChipSelectPin = ADBUS3ChipSelect;

			HighPinsReadActiveStates.bPin1State = false;
			HighPinsReadActiveStates.bPin1ActiveState = false;
			HighPinsReadActiveStates.bPin2State = false;
			HighPinsReadActiveStates.bPin2ActiveState = false;
			HighPinsReadActiveStates.bPin3State = false;
			HighPinsReadActiveStates.bPin3ActiveState = false;
			HighPinsReadActiveStates.bPin4State = false;
			HighPinsReadActiveStates.bPin4ActiveState = false;
			HighPinsReadActiveStates.bPin5State = false;
			HighPinsReadActiveStates.bPin5ActiveState = false;
			HighPinsReadActiveStates.bPin6State = false;
			HighPinsReadActiveStates.bPin6ActiveState = false;
			HighPinsReadActiveStates.bPin7State = false;
			HighPinsReadActiveStates.bPin7ActiveState = false;
			HighPinsReadActiveStates.bPin8State = false;
			HighPinsReadActiveStates.bPin8ActiveState = false;

			WriteControlBuffer[0] = (byte)(control & 0xFF);

			ClearGPIO(false);
            SPI_ClearDeviceCmdSequence(ftHandle);
            ftStatus = SPI_ReadHiSpeedDevice(
                                ftHandle,
                                ref ReadStartCondition,
                                true,                   // clock out MSB first
                                false,                  // clock out control bits pos edge (false)
                                8,
                                WriteControlBuffer,
                                1,
                                true,                   // clock in MSB first
                                false,                  // clock in data bits pos edge (false)
                                (uint)(nBytes * 8),
                                ReadDataBuffer,
                                out NumDataBytesReturned,
                                ref HighPinsReadActiveStates);
            ClearGPIO(true);
            if (ftStatus != FTC_SUCCESS) {
                throw new ApplicationException("Error in ReadBytes() in FT2232SPI #" + ftStatus.ToString());
            }

			int value = 0;
			for (int i = 0; i < nBytes; i++) {
				value = value << 8;
				value += ReadDataBuffer[i];
			}
			return value;
		}

	}  // end class

}  // end namespace
