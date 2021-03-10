/* -----------------------------------------------------------------
 * 
 * LED initialization code written by Levent S. 
 * E-mail: ls@izdir.com
 * 
 * This code is provided without implied warranty so the author is
 * not responsible about damages by the use of the code.
 * 
 * You can use this code for any purpose even in any commercial 
 * distributions by referencing my name. 
 * 
 * ! Don't remove or alter this notice in any distribution !
 * 
 * -----------------------------------------------------------------*/
using System.Runtime.InteropServices;

public class PortAccess
{
	[DllImport("inpout32.dll", EntryPoint="Out32")]
	public static extern void Output(int adress, int value);
	[DllImport("inpout32.dll", EntryPoint = "Inp32")]
	public static extern int Input(int address);

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
