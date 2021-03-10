using System;

using DACarter.PopUtilities;

namespace POPN {

    public class PopDataInfo {
        public PopParameters Parameters;	
        public bool NoHardware;
        public DateTime RecordTimeStamp;
        public PopParCurrentIndices CurrentParIndices;
        public bool RassIsOn;
    }


	/// <summary>
	/// Info about the POP processing,
	/// to be passed between worker and UI threads.
	/// Since this is a class, everyone will be working
	/// on the same copy of the data fields.
	/// </summary>
    /// 
    /*
	public class PopDataPackage3 {
		public PopParameters Parameters;	
		public bool NoHardware;
		//public string ParFileName;
		public DateTime RecordTimeStamp;
		public PopParCurrentIndices CurrentParIndices;
        public bool RassIsOn;

		public double[][][][] SampledTimeSeries;			// double[_nRx][_nSpec][_nPts][_nSamples] sampled data for each IPP
        public Ipp64fc[][][][] TransformedTimeSeries;		// Complex[_nRx][_nSpec][_nHts][_nPts] time series for each height
        public double[][][] Spectra;						// Double[_nRx][_nHts][_nPts]  Doppler spectra
        public double[][] Noise;							// Double[_nRx][ihts] noise level
        public double[][] MeanDoppler;					    // double[_nRx][ihts] meanDoppler as fraction of nyquist/2
        public double[][] Width;							// double[_nRx][ihts] spectral width as fraction of nyq/2
        public double[][] Power;							// double[_nRx][ihts] signal power
        public int[][] ClutterPoints;						// int[_nRx][ihts] number of center points to consider to be clutter

		public PopDataPackage3() {
            RassIsOn = false;
		}
	}
     * */

	//class PopDataPackage {
	//}

    /*
	public enum PopStatus {
		Running,
		RunningPausePending,
		Paused,
		//ContinuingFromPause,
		Stopped
	}
    */
}
