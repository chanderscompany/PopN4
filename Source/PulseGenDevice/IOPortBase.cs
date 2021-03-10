using System;

namespace DACarter.NOAA.Hardware {
	public abstract class IOPortBase {
		public abstract UInt16 ReadPort16(ushort port);
		public abstract void WritePort16(ushort port, UInt16 value);
		public abstract Byte ReadPort8(ushort port);
		public abstract void WritePort8(ushort port, Byte value);

		private bool _hardwareExists;

		public bool HardwareExists {
			get { return _hardwareExists; }
			set { _hardwareExists = value; }
		}

		public IOPortBase() {
			_hardwareExists = false;
		}
	}
}
