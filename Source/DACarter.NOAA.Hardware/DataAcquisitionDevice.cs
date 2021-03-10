using System;

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
