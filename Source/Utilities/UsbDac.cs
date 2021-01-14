using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace Utilities {

	/// <summary>
	/// USB Routines
	///		Based on EZUSB VisualBasic class included with AD9959ev board software.
	///		No citations given for original authors of this code.
	/// </summary>
	unsafe public class UsbDac {

		//public constants
		public const short MAX_PIPES = 16;
		
		public const string EZUSB_DevName  = "Ezusb";
		public const string EZSSP_DevName = "Ezssp";

		// private constants
		private const uint GENERIC_READ  = 0x80000000;
		private const uint GENERIC_WRITE = 0x40000000;
		private const uint FILE_SHARE_READ = 0x1;
		private const uint FILE_SHARE_WRITE = 0x2;
		private const uint OPEN_EXISTING = 0x3;
		
		private const short METHOD_BUFFERED = 0;
		private const short METHOD_IN_DIRECT = 1;
		private const short METHOD_OUT_DIRECT = 2;
		
		private const short MAX_USB_DEV_NUMBER = 9;


		//
		// DLL IMPORTS
		//
		[DllImport("kernel32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern SafeFileHandle CreateFile(
			  string lpFileName,
			  uint dwDesiredAccess,
			  uint dwShareMode,
			  IntPtr lpSecurityAttributes,
			  uint dwCreationDisposition,
			  uint dwFlagsAndAttributes,
			  IntPtr hTemplateFile
			  );

		[DllImport("kernel32.dll", EntryPoint = "CloseHandle")]
		public static extern IntPtr CloseHandle(IntPtr handle);

		[DllImport("kernel32.dll")]
		public static extern uint GetSystemDirectory([Out] StringBuilder lpBuffer,
		   uint uSize);

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
		public static extern int DeviceIoControl(
			IntPtr hDevice, 
			uint dwIoControlCode,
			IntPtr lpInBuffer, 
			uint nInBufferSize,
			IntPtr lpOutBuffer, 
			uint nOutBufferSize,
			out uint lpBytesReturned, 
			IntPtr lpOverlapped);

		public int GetLastError() {
			return Marshal.GetLastWin32Error();
		}

		//
		// Public Definitions
		//
		public enum EZUSB_ReadOrWrite {
			ezWrite = 3,
			ezRead = 5
		}

		public enum EZUSB_PipeEnum {
			eUsbdPipeTypeControl = 0,
			eUsbdPipeTypeIsochronous,
			eUsbdPipeTypeBulk,
			eUsbdPipeTypeInterrupt
		}

		public struct GetStringDescriptorIn {
			byte Index;
			short LanguageId;
		}

		public struct VENDOR_OR_CLASS_REQUEST_CONTROL {
			// transfer direction (0=host to device, 1=device to host)
			byte bDirection;
			// request type (1=class, 2=vendor)
			byte bRequestType;
			// recipient (0=device,1=interface,2=endpoint,3=other)
			byte bRecepient;
			//
			// see the USB Specification for an explanation of the
			// following paramaters.
			//
			byte bRequestTypeReservedBits;
			byte bRequest;
			byte fill;
			short uiValue;
			short uiIndex;
		}

		public struct EZUSB_DeviceDescriptorType {
			byte bDescriptorLength;
			byte bDescriptor;
			short iSpecRelease;
			byte bDeviceClass;
			byte bDeviceSubClass;
			byte bDeviceProtocol;
			byte bMaxPacketSize;
			short iVendorID;
			short iProductID;
			short iDeviceRelease;
			byte bManufacturer;
			byte bProduct;
			byte bSerialNumber;
			byte bNumberConfigurations;
			fixed byte fill[128];		// requires unsafe
		}

		public struct EZUSB_BulkTransferControlType {
			int lPipeNum;
		}

		public struct EZUSB_PipeInformationType {
			// OUTPUT
			// These fields are filled in by USBD
			//
			short iMaximumPacketSize;	//Maximum packet size for this pipe
			byte bEndpointAddress;		// 8 bit USB endpoint address (includes direction)
										// taken from endpoint descriptor
			byte bInterval;				// Polling interval in ms if interrupt pipe
			EZUSB_PipeEnum PipeType;	// PipeType identifies type of transfer valid for this pipe
			int lPipeHandle;
			//
			// INPUT
			// These fields are filled in by the client driver
			//
			int lMaximumTransferSize;	// Maximum size for a single request
										// in bytes.
			int lPipeFlags;
		}

		public struct EZUSB_InterfaceInformationType {
			//
			// Must call Initialize() before using
			//
			short iLength;	// Length of this structure, including
							// all pipe information structures that
							// follow.
			//
			// INPUT
			//
			// Interface number and Alternate setting this
			// structure is associated with
			//
			byte bInterfaceNumber;
			byte bAlternateSetting;
			//
			// OUTPUT
			// These fields are filled in by USBD
			byte bClass;
			byte bSubClass;
			byte bProtocol;
			byte bReserved;
			int lInterfaceHandle;
			int lNumberOfPipes;
			EZUSB_PipeInformationType[] Pipes;
			//<VBFixedArray(MAX_PIPES)> Dim Pipes() As EZUSB_PipeInformationType
			public void Initialize() {
				Pipes = new EZUSB_PipeInformationType[MAX_PIPES];
			}
			
		}

		//
		// private definitions
		//
		private struct SECURITY_ATTRIBUTES {
			int nLength;
			int lpSecurityDescriptor;
			bool bInheritHandle;
		}

		private const uint Ezusb_IOCTL_INDEX = 0x800;
		private const uint IOCTL_Ezusb_GET_PIPE_INFO = 0x220000 + METHOD_BUFFERED + (Ezusb_IOCTL_INDEX + 0) * 4;
		private const uint IOCTL_Ezusb_GET_DEVICE_DESCRIPTOR = 0x220000 + METHOD_BUFFERED + (Ezusb_IOCTL_INDEX + 1) * 4;
		private const uint IOCTL_EZUSB_BULK_READ = 0x220000 + METHOD_OUT_DIRECT + (Ezusb_IOCTL_INDEX + 19) * 4;
		private const uint IOCTL_EZUSB_BULK_WRITE = 0x220000 + METHOD_IN_DIRECT + (Ezusb_IOCTL_INDEX + 20) * 4;
		private const uint IOCTL_EZUSB_VENDOR_OR_CLASS_REQUEST = 0x220000 + METHOD_IN_DIRECT + (Ezusb_IOCTL_INDEX + 22) * 4;
		private const uint IOCTL_Ezusb_GET_STRING_DESCRIPTOR = 0x220000 + METHOD_BUFFERED + (Ezusb_IOCTL_INDEX + 17) * 4;

		//Used by IsWinNT
		private struct OSVERSIONINFO {
			int dwOSVersionInfoSize; //Set to 148
			int dwMajorVersion;
			int dwMinorVersion;
			int dwBuildNumber;
			int dwPlatformId;
			//  Maintenance string for PSS usage
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)]
			public string szCSDVersion;
			//<VBFixedString(128),System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValArray,SizeConst:=128)> Public szCSDVersion() As Char 
		}
		private const int VER_PLATFORM_WIN32_NT = 2;
		private const int VER_PLATFORM_WIN32_WINDOWS = 1;
		private const int VER_PLATFORM_WIN32s = 0;
		//Private Declare Function GetVersionEx Lib "kernel32"  Alias "GetVersionExA"(ByRef lpVersionInformation As OSVERSIONINFO) As Integer
		[DllImport("kernel32.Dll")] private static extern short GetVersionEx(ref OSVERSIONINFO o);
		// end used by NT

	}
}
