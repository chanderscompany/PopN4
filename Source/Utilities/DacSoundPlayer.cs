using System;
using System.Runtime.InteropServices;

namespace DACarter.Utilities
{
	/// <summary>
	/// Summary description for DacSoundPlayer.
	/// </summary>
	public class DacSoundPlayer
	{

		[DllImport("winmm.dll")]
		public extern static bool PlaySound(
			string pszName, IntPtr hModule, int dwFlags);

		[Flags]
		public enum SoundFlags {
			SndApplication = 0x80,   // look for application specific association
			SndAlias = 0x10000,     // name is a WIN.INI [sounds] entry
			SndAliasId = 0x110000,   // name is a WIN.INI [sounds] entry
			SndAsync = 0x1,       // play asynchronously
			SndFilename = 0x20000,   // lpszSoundName is a file name (null filename to stop sound)
			SndLoop = 0x8,       // loop the sound until stopped
			SndMemory = 0x4,      // lpszSoundName points to a memory file
			SndNoDefault = 0x2,     // Do not play default sound if sound file not found
			SndNoStop = 0x10,      // Do not stop currently playing sound
			SndNoWait = 0x2000,     // Do not wait if sound is currently playing
			SndPurge = 0x40,      // purge non-static events for task (use before exiting program)
			SndResource = 0x40004,   // lpszSoundName is a resource name or atom
			SndSync = 0x0        // play synchronously (default)
		}

		public static bool PlaySound(string fileName) {
			return PlaySound(fileName, IntPtr.Zero, (int)(SoundFlags.SndFilename | SoundFlags.SndAsync));
		}

		public static bool PlaySound(string fileName, SoundFlags flags) {
			return PlaySound(fileName, IntPtr.Zero, (int)(SoundFlags.SndFilename | flags));
		}

		private DacSoundPlayer()
		{
			//
			// TODO: Add constructor logic here
			//
		}
	}
}
