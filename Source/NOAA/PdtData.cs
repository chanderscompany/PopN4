using System;

namespace DACarter.NOAA {

	public class PdtData : WindsData {

		public string HdrDescription;
		public string HdrStation;
		public string HdrPlatform;
		public string HdrWMO;
		public string HdrLocationType;
		//public double HdrNLatitude, HdrELongitude;
		//public int HdrAltitude;
		public DateTime HdrCreationDate;
		public string HdrSoftware;
		public string HdrContact;
		public int HdrDataVersion;
		public DateTime HdrStartTime;
		public DateTime HdrEndTime;
		public int HdrMaxHeights;
		public int HdrVerticalScales;
		public int HdrLowHeight1, HdrLowHeight2;
		public int HdrHighHeight1, HdrHighHeight2;

		public PdtData(){
		}

		public override void Clear() {
			base.Clear();
		}
	}
}
