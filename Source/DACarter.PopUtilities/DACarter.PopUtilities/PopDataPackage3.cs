using System;

using ipp;

//using POPCommunication;

namespace DACarter.PopUtilities {

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
	public class PopDataPackage3 {
		public PopParameters Parameters;	
		public bool NoHardware;
		//public string ParFileName;
		public DateTime RecordTimeStamp;
		public PopParCurrentIndices CurrentParIndices;
        public bool RassIsOn;

		public double[][][][] SampledTimeSeries;			// double[_nRx][_nSpec][_nPts][_nSamples] sampled data for each IPP
        public Ipp64fc[][][][] TransformedTimeSeries;		// Complex[_nRx][_nSpec][_nHts][_nPts] time series for each height
        public int WaveletClippedNpts;
        public int WaveletOutputNpts;
        public int XCorrNPts;
        public int XCorrNAvgs;
        public double[][][] WaveletClutterTransform;        // Double[_nrx]{_nHts][numPts] Daubechies20 wavelet transform of Doppler TS
        public double[][][] Spectra;						// Double[_nRx][_nHts][_nPts]  Doppler spectra
        public Ipp64fc[][][] XCorrelation;                  // Ipp64fc[_nRx][_nHts][_nLags] Cross-correlations; order: xc12, xc13, xc23
        public double[][][] XCorrMag;                       // Double[_nRx][_nHts][_nLags] cross-correlation magnitude
        public double[][][] XCorrRatio;                     // Double[_nRx][_nHts][_nLags] cross-correlation ratio for SA wind computations
        public double[][][] XCorrGaussCoeffs;               // Double[_nRx][_nHts][5] cross-correlation gauss fit coeffs for SA wind computations
        public LineFit[][] XCorrRatioLine;                  // result of LSQ fit to xcorr ratio, y = mx + b
        public double[][][] XCorrSlope0;                    // double[nrx][nht][2] slope, vel of xcorr gaussian at zero lag
        public double[][][] XCorrPolyCoeffs;                // double[2*_nRx][_nHts][nCoefs] Cross and auto corr poly fit coeffs
        public double[][][] XCorrFcaLags;                   // double[nrx][_nHts][3] taui, taup, taux
        public double[][] Noise;							// Double[_nRx][ihts] noise level
        public double[][] MeanDoppler;					    // double[_nRx][ihts] meanDoppler as fraction of nyquist/2
        public double[][] Width;							// double[_nRx][ihts] spectral width as fraction of nyq/2
        public double[][] Power;							// double[_nRx][ihts] signal power
        public int[][] ClutterPoints;						// int[_nRx][ihts] number of center points to consider to be clutter

        public double[][] RassMeanDoppler;					// double[_nRx][ihts] meanDoppler as fraction of nyquist/2
        public double[][] RassWidth;						// double[_nRx][ihts] spectral width as fraction of nyq/2
        public double[][] RassPower;						// double[_nRx][ihts] signal power
        public double[][] RassTemp;							// Double[_nRx][ihts] rass temp in deg C

		public PopDataPackage3() {
            RassIsOn = false;
		}
	}

    public struct LineFit {
        public double B;
        public double M;
    }

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
