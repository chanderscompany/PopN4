using System;
using System.Collections.Generic;
using System.Text;

namespace DACarter.NOAA {
	public class SurfaceData : DacData {

		public SurfaceData() {
			Clear();
		}

		public double DmuHeading;	// Crossbow DMU
		public double SmoHeading;	// Scripps Marine Observatory GPS

		public override void Clear() {
			Notes = "";
			TimeStamp = DateTime.MinValue;
			DmuHeading = Double.NaN;
			SmoHeading = Double.NaN;
		}
	}
}
