using System;
using DACarter.NOAA;

namespace DACarter.NOAA
{
	/// <summary>
	/// Summary description for DacPdtData.
	/// </summary>
	public class DacPdtData : DacData
	{
		//public string Contents;
		//public DateTime TimeStamp;

		public DacPdtData()
		{
			Notes = "Data contents not initialized";
			TimeStamp = DateTime.MinValue;
		}

	}
}
