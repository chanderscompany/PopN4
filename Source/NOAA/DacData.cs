using System;

namespace DACarter.NOAA
{
	/// <summary>
	/// Summary description for DacData.
	/// </summary>
	public abstract class DacData {
		public string Notes;
		public DateTime TimeStamp;

		// each data class must implement a Clear() method
		//	to reset all data values before reading a new record
		//	(or at least all data values that might not get rewritten)
		public abstract void Clear();
	}
}
