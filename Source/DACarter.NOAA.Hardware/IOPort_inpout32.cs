using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace DACarter.NOAA.Hardware {

	/// <summary>
	/// Class that handle port IO by using inpout32.dll
	/// </summary>
	class IOPort_inpout32 : IOPortBase {

		public override ushort ReadPort16(ushort port) {
			ushort value = PortAccess.Input(port);
			return value;
		}
		public override void WritePort16(ushort port, ushort value) {
			PortAccess.Output(port, value);
		}

		public override byte ReadPort8(ushort port) {
			throw new Exception("The method or operation is not implemented.");
		}

		public override void WritePort8(ushort port, byte value) {
			throw new Exception("The method or operation is not implemented.");
		}

		private class PortAccess {
			[DllImport("inpout32.dll", EntryPoint = "Out32")]
			public static extern void Output(int address, int value);
			[DllImport("inpout32.dll", EntryPoint = "Inp32")]
			public static extern ushort Input(int address);

			/*
			// info added by DAC:
			// to access Win32 DeviceIoControl function:
			[System.Runtime.InteropServices.DllImport("Kernel32.dll", SetLastError = true)]
			public extern static int DeviceIoControl(IntPtr hDevice, uint IoControlCode,
			IntPtr lpInBuffer, uint InBufferSize,
			IntPtr lpOutBuffer, uint nOutBufferSize,
			ref uint lpBytesReturned,
			IntPtr lpOverlapped);
			*/

		}
	}


}
