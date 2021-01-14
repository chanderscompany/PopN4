using System;
using System.Collections.Generic;
using System.Text;

namespace DACarter.NOAA {
	public abstract class WindsFileBase : DacDataFileBase {
		public bool GetVertical;
		public bool GetHorizontal;
		public bool GetSNR;
		public bool GetWinds;

		public WindsFileBase() {
			GetVertical = true;
			GetHorizontal = true;
			GetSNR = false;
			GetWinds = true;
		}
	
	}

}
