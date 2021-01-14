using System;
using System.Collections.Generic;
using System.Text;

namespace DACarter.NOAA.Hardware {
    public  class DataAcquisitionDevice : IDisposable {
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		} 

		protected virtual void Dispose(bool disposing) {
		}

		~DataAcquisitionDevice() {
			Dispose(false);
		}
	}
}
