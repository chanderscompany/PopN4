using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace DACarter.Utilities
{
	/// <summary>
	/// 
	/// </summary>
	public class DacDiskInfo
	{
		[ DllImport("kernel32", CharSet=CharSet.Auto, SetLastError = true) ]
		static extern uint SetErrorMode( uint mode); 
		[ DllImport("kernel32", CharSet=CharSet.Auto, SetLastError = true) ]
		static extern bool GetDiskFreeSpaceEx( string strRoot, out ulong userFreeBytes, out ulong totalBytes, out ulong totalFreeBytes); 
		[DllImport("kernel32.dll")]
		private static extern int GetDriveType(string driveLetter);
		[DllImport("kernel32.dll")]
		private static extern bool GetVolumeInformation
			(
			string strPathName,
			StringBuilder strVolumeNameBuffer,
			uint uintVolumeNameSize,
			out uint uintVolumeSerialNumber,
			out uint uintMaximumComponentLength,
			out uint uintFileSystemFlags,
			StringBuilder strFileSystemNameBuffer,
			uint uintFileSystemNameSize
			);		

		private static string GetRoot(string path) {
			string root;
			root = Path.GetPathRoot(path);
			if (!root.EndsWith(@"\")) {
				root += @"\";
			}
			return root;
		}

		public enum DiskType {
			Unknown,
			Error,
			Removable,
			Fixed,
			Network,
			CD,
			RAM
		};

		public DacDiskInfo()
		{
			// 
			// TODO: Add constructor logic here
			//
		}

		public static bool GetDiskFreeSpace(string path, out ulong totalBytes, out ulong totalFreeBytes) {
			ulong userFreeBytes;
			SetErrorMode(1);	// so dialog box does not appear when missing removable disk media
			string root = GetRoot(path);
			return (GetDiskFreeSpaceEx(root, out userFreeBytes, out totalBytes, out totalFreeBytes));
		}

		public static bool GetVolumeInfo(string path, out string volumeName, out string sysName) {

			uint serialNumber, maxComponentLength, fileSysFlags;
			StringBuilder _volumeName = new StringBuilder(256);
			StringBuilder _sysName = new StringBuilder(256);
			string root = GetRoot(path);

			SetErrorMode(1);	// so dialog box does not appear when missing removable disk media
			bool ret = GetVolumeInformation(root,
				_volumeName,
				(uint)_volumeName.Capacity,
				out serialNumber,
				out maxComponentLength,
				out fileSysFlags,
				_sysName,
				(uint)_sysName.Capacity);

			volumeName = _volumeName.ToString();
			sysName = _sysName.ToString();

			return ret;

			/*
			uint serialNumber, maxComponentLength, fileSysFlags;
			SetErrorMode(1);	// so dialog box does not appear when missing removable disk media
			bool ret = GetVolumeInformation(root,
										volumeName,
										(uint)volumeName.Capacity,
										out serialNumber,
										out maxComponentLength,
										out fileSysFlags,
										sysName,
										(uint)sysName.Capacity);
			return ret;
			*/
		}

		public static string GetDiskType(string path) {
			string root = GetRoot(path);
			int type = GetDriveType(root);
			if (Enum.IsDefined(typeof(DiskType),type)) {
				return Enum.GetName(typeof(DiskType),type);
			}
			else {
				return "Unknown";
			}
		}

		/*
		ulong userFreeBytes, totalBytes, totalFreeBytes;
		SetErrorMode(1);	// so dialog box does not appear when missing removable disk media
		bool diskFreeOK = GetDiskFreeSpaceEx(@"a:\", out userFreeBytes, out totalBytes, out totalFreeBytes);

		StringBuilder volname = new StringBuilder(256);
		uint sn;
		uint maxcomplen;//receives maximum component length
		uint sysflags;//receives file system flags
		StringBuilder sysname = new StringBuilder(256);//receives the file system name
		bool viOK = GetVolumeInformation(@"c:\", volname, (uint)volname.Capacity, out sn, out maxcomplen,
			out sysflags,sysname,(uint)sysname.Capacity);
		string volumeName = volname.ToString();
		string systemName = sysname.ToString();

		int type = GetDriveType(@"c:\");
		*/

	}
}
