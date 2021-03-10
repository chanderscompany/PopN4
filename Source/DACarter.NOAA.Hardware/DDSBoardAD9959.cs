using System;
using System.Threading;

namespace DACarter.NOAA.Hardware {

	/// <summary>
	/// This class uses DAQ board to create data and clock pulses
	/// to read/write AD9959 via SPI port.
	/// </summary>
	public class DDSBoardAD9959 {

		// DAQ may be used to write to DDS serial input
		public DaqBoard3000USB DAQ;
		public DaqBoard3000USB.DIOPorts SCLKPort, SDIOWritePort, SDIOReadPort, IOUpdatePort, SyncIOPort, CSPort;
		public int SCLKBit, SDIOWriteBit, SDIOReadBit, IOUpdateBit, SyncIOBit;
		public FT2232SPI SPI;
		public bool UseFT2232;
        private int _chipSelectBit;
        public int ResetBit;

        public int ChipSelectBit { 
            get {
                return _chipSelectBit;
            } 
            set {                
                _chipSelectBit = value;
                if (SPI != null)  {
                    SPI.ChipSelectLine = _chipSelectBit;
                }
            } 
        }

		public DDSBoardAD9959(bool useFT2232) {
			// Initialize DAQ ports with illegal values.
			// User must assign correct values.
			UseFT2232 = useFT2232;
			DAQ = null;
			SCLKPort = DaqBoard3000USB.DIOPorts.NullPort;
			SCLKBit = -1;
			SDIOWritePort = DaqBoard3000USB.DIOPorts.NullPort;
			SDIOWriteBit = -1;
			SDIOReadPort = DaqBoard3000USB.DIOPorts.NullPort;
			SDIOReadBit = -1;
			IOUpdatePort = DaqBoard3000USB.DIOPorts.NullPort;
			IOUpdateBit = -1;
			SyncIOPort = DaqBoard3000USB.DIOPorts.NullPort;
			SyncIOBit = -1;
			//
			SPI = null;
            if (useFT2232) {
                SPI = new FT2232SPI();
            }
		}

		public void IOUpdate() {
			if (UseFT2232) {
				SPI.PulseGPIOLine(IOUpdateBit);
			}
			else {
				DAQ.SetPortBit(IOUpdatePort, IOUpdateBit);
				DAQ.ClearPortBit(IOUpdatePort, IOUpdateBit);
			}
		}

        public void IOUpdateToggle() {
            if (UseFT2232) {
                SPI.ToggleGPIOLine(IOUpdateBit);
            }
        }

		public void SyncIOUpDown() {
			if (UseFT2232) {
				SPI.PulseGPIOLine(SyncIOBit);
			}
			else {
				DAQ.SetPortBit(SyncIOPort, SyncIOBit);
				DAQ.ClearPortBit(SyncIOPort, SyncIOBit);
			}
		}

        public void Reset()
        {
            if (UseFT2232) {
                SPI.PulseGPIOLine(ResetBit);
            }
        }

		private void SCLKUpDown() {
			if (!UseFT2232) {
				DAQ.SetPortBit(SCLKPort, SCLKBit);
				DAQ.ClearPortBit(SCLKPort, SCLKBit);
			}
		}

		private void SCLKDownUp() {
			if (!UseFT2232) {
				DAQ.ClearPortBit(SCLKPort, SCLKBit);
				DAQ.SetPortBit(SCLKPort, SCLKBit);
			}
		}

		private void SCLKUp() {
			if (!UseFT2232) {
				DAQ.SetPortBit(SCLKPort, SCLKBit);
			}
		}

		private void SCLKDown() {
			if (!UseFT2232) {
				DAQ.ClearPortBit(SCLKPort, SCLKBit);
			}
		}

		// Phase 1 of serial communication is instruction cycle
		// Write instuction byte is 0xxRRRRR
		public void WriteInstructionCycle(int register) {
			int data = register & 0x1F;
			WriteInstructionByte(data, false);
		}

		// Read instuction byte is 1xxRRRRR
		private void ReadInstructionCycle(int register) {
			int data = register & 0x1F;
			data = data | 0x80;
			WriteInstructionByte(data, true);
		}

		private void WriteInstructionByte(int data, bool isReadInstruction) {
			int mask = 0x80;
			// make sure clock starts down
			SCLKDown();
			SyncIOUpDown();
			for (int i = 0; i < 8; i++) {
				if ((data & mask) == mask) {
					DAQ.SetPortBit(SDIOWritePort, SDIOWriteBit);
				}
				else {
					DAQ.ClearPortBit(SDIOWritePort, SDIOWriteBit);
				}
				SCLKUp();
				if (!(isReadInstruction && (i==7))) {
					// leave SCLK up at end of read instruction
					//SCLKDown();
				}
				SCLKDown();
				mask = mask >> 1;
			}
		}

		public void WriteBytesToRegister(int register, int value, int nBytes) {
			if (nBytes > 4) {
				throw new ApplicationException("Can't write more than 4 bytes to AD9959 register.");
			}
			//SyncIOUpDown();
			WriteInstructionCycle(register);
			Thread.Sleep(20);
			int mask = 1;
			mask = 1 << (8 * nBytes) - 1;
			for (int i = 0; i < 8*nBytes; i++) {
				if ((value & mask) == mask) {
					DAQ.SetPortBit(SDIOWritePort, SDIOWriteBit);
					SCLKUpDown();
				}
				else  {
					DAQ.ClearPortBit(SDIOWritePort, SDIOWriteBit);
					SCLKUpDown();
				}
				mask = mask >> 1;
			}
		}

		public int ReadBytesFromRegister(int register, int nBytes) {
			if (nBytes > 4) {
				throw new ApplicationException("Can't read more than 4 bytes from AD9959 register.");
			}
			//SyncIOUpDown();
			ReadInstructionCycle(register);
			Thread.Sleep(20);
			// make sure clock is high, but if it wasn't then previous 
			//	down clock read a bit and we are out of sync.
			int bit;
			int value = 0;
			for (int i = 0; i < 8 * nBytes; i++) {
				//SCLKDown();
				bit = DAQ.ReadPortBit(SDIOReadPort, SDIOReadBit);
				value = value << 1;
				value = value + bit;
				SCLKUp();
				SCLKDown();
			}
			//SCLKDown();
			return value;
		}

		public void SPIWriteBytesToRegister(int register, int value, int nBytes) {
			if (SPI == null) {
				SPI = new FT2232SPI();
			}
            SPI.WriteBytes(register, value, nBytes);
		}

		public int SPIReadBytesFromRegister(int register, int nBytes) {
			if (SPI == null) {
				SPI = new FT2232SPI();
			}
			int control = 0x80 + register;
			return SPI.ReadBytes(control, nBytes);
		}

	}  // end class DDSBoardAD9959

}
