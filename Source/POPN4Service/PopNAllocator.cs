using System;
using System.Runtime;

using ipp;
using DACarter.PopUtilities;

namespace POPN {

    /// <summary>
    /// Class to handle allocation of data arrays.
    /// Memory is re-allocated before read-only parameters 
    /// for arrays and array dimensions are read, if required based on changes to the
    /// properties Parameters and UseFullArrays.
    /// So it is not necessary to explicitly call Allocate method.
    /// </summary>
    class PopNAllocator {

        #region Private Members
        //
        private bool _useFullArrays;
        private PopParameters _parameters;

        // This member is set true at construction time
        // and any time a parameter that affects 
        // array sizes is changed;
        // It is set false after memory is allocated;
        private bool _needReallocate;

        private int _nRx, _nSamples, _nPts, _nSpec, _nHts, _nLags;
        //private int _nSpectralPts;
        private int _nPtsSavedInArray;
        private bool _isRass, _isPartialSpec;
        private int _nSpecAtATime;
        private bool _allocTSOnly;

        private int _STSnSpec, _DTSnSpec;
        private bool _savingRawTS, _savingDopplerTS, _savingProcessedProducts;

        private Array _daqBuffer;                   // float[_nrx*_nSpec*_nPts*_nSamples] of sampled data in time sampled order
        private double[][][][] _sampledTimeSeries;   // double[_nRx][_nSpec][_nPts][_nSamples] sampled data for each IPP
        private Ipp64fc[][][][] _dopplerTimeSeries;  // Ipp64fc[_nRx][_nSpec][_nHts][_nPts] Doppler time series for each height
        private double[][][] _spectra;              // Double[_nRx][_nHts][_nPts]  Doppler spectra
        private double[][][] _clutterWavelets;      // Double[_nRx][_nHts][npts]  clutter wavelet transforms
        private Ipp64fc[][][] _xCorr;               // Ipp64fc[_nRx][_nHts][2*lag+1]  complex crosscorrelation , order: xc12, xc13, xc23
        private double[][][] _xCorrMag;             // Double[_nRx][_nHts][_nPts] magnitude of crosscorrelation
        private double[][][] _xCorrRatio;           // Double[_nRx][_nHts][_nPts] crosscorrelation ratio for SA winds
        private double[][][] _xCorrGaussCoeffs;     // Double[_nRx][_nHts][4] Gauss fits coeffs (amp, mean, width, DCoffset)
        private double[][][] _xCorrPolyCoeffs;      // Double[2*_nRx][_nHts][4] Polynomial fit coeffs for cross and auto correlations
        private double[][][] _xCorrFcaLags;         // double[nrx][_nHts][3] taui, taup, taux
        private LineFit[][] _xCorrRatioLine;
        private double[][][] _xCorrSlope0;            // double[_nrx][_nhts] slope of gaussian fit at zero lag
        private double[][] _noise;                  // Double[_nRx][_nHts] noise level
        private double[][] _meanDoppler;
        private double[][] _width;
        private double[][] _power;
        private double[][] _rassMeanDoppler;
        private double[][] _rassWidth;
        private double[][] _rassPower;
        private double[][] _rassTemp;
        private int[][] _clutterPoints;

        //private MemoryMappedViewAccessor _AscanPlotView;
        //private MemoryMappedFile _AscanPlotMmf;
        
        //
        #endregion Private Members

        #region Public Properties

        public Action<string> SendMessageAction;

        //
        /// <summary>
        /// Radar parameters
        /// </summary>
        public PopParameters Parameters {
            get { return _parameters; }
            set {
                _parameters = value;
                _needReallocate = true;

                if (_parameters.SystemPar.RadarPar.ProcPar.RassSourceParams[0] > 0) {
                    _isRass = true;
                    _isPartialSpec = true;
                }
                else {
                    _isRass = false;
                    _isPartialSpec = false;
                    if (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.PulsedTx) {
                        if (_parameters.SystemPar.RadarPar.BeamParSet[0].NPts > _parameters.SystemPar.RadarPar.ProcPar.Dop1 +
                                    _parameters.SystemPar.RadarPar.ProcPar.Dop3) {
                                        _isPartialSpec = true;
                        }
                    }
                }

                if (_parameters.ReplayPar.Enabled) {
                    // replay mode
                    _nSamples = 0;
                    _nSamples = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts;
                    if (_nSamples > 0) {
                        // in rev 3.21, the following has been done in PopNReplay module:
                        //_nHts = _nSamples / 2 + 1;
                        //_parameters.SystemPar.RadarPar.BeamParSet[0].NHts = _nHts;
                        _nHts = _parameters.SystemPar.RadarPar.BeamParSet[0].NHts;
                    }
                    else {
                        _nHts = _parameters.SystemPar.RadarPar.BeamParSet[0].NHts;
                        _nSamples = _nHts;
                    }
                    _nPts = _parameters.SystemPar.RadarPar.BeamParSet[0].NPts;  
                    if (_isRass || _isPartialSpec) {
                        _nPtsSavedInArray = _parameters.SystemPar.RadarPar.ProcPar.Dop1 +
                                    _parameters.SystemPar.RadarPar.ProcPar.Dop3;
                    }
                    else {
                        _nPtsSavedInArray = _nPts;
                    }
                    _nSpec = _parameters.SystemPar.RadarPar.BeamParSet[0].NSpec * _parameters.ReplayPar.NumberRecordsAtOnce;
                    _nRx = _parameters.SystemPar.RadarPar.ProcPar.NumberOfRx;
                    _nLags = 2*_parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrMaxLag + 1;
                    _nSpecAtATime = _nSpec;  // assuming all raw time series pts in file
                }
                else {
                    // real-time mode
                    _nRx = _parameters.SystemPar.RadarPar.ProcPar.NumberOfRx;
                    _nSamples = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts;
                    _nPts = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNPts;
                    _nPtsSavedInArray = _nPts;
                    _nSpec = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNSpec;
                    _nHts = _nSamples / 2 + 1;
                    _nLags = 2 * _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrMaxLag + 1;
                    _nSpecAtATime = _parameters.SystemPar.RadarPar.ProcPar.NSpecAtATime;
                    if (_parameters.Debug.UseAllocator == false) {
                        // if we said no allocator (even tho we are using it ),
                        //  we must not want small block size
                        _nSpecAtATime = _nSpec;
                    }
                    if (_nSpecAtATime > _nSpec) {
                        _nSpecAtATime = _nSpec;
                    }
                    if (_nSpecAtATime < 1) {
                        _nSpecAtATime = 1;
                    }
                }

                // TODO: check for replay mode with no time series input
                PopParameters.ProcessingParameters procPar = _parameters.SystemPar.RadarPar.ProcPar;
                if ((procPar.PopFiles[0].FileWriteEnabled && procPar.PopFiles[0].WriteRawTSFile) ||
                    (procPar.PopFiles[1].FileWriteEnabled && procPar.PopFiles[1].WriteRawTSFile)) {
                    _savingRawTS = true;
                }
                else {
                    _savingRawTS = false;
                }
                if ((procPar.PopFiles[0].FileWriteEnabled && procPar.PopFiles[0].IncludeFullTS) ||
                    (procPar.PopFiles[1].FileWriteEnabled && procPar.PopFiles[1].IncludeFullTS))
                {
                    _savingDopplerTS = true;
                }
                else
                {
                    _savingDopplerTS = false;
                }

                if ((procPar.PopFiles[0].FileWriteEnabled && procPar.PopFiles[0].IncludeACorr) ||
                    (procPar.PopFiles[1].FileWriteEnabled && procPar.PopFiles[1].IncludeACorr) ||
                    (procPar.PopFiles[0].FileWriteEnabled && procPar.PopFiles[0].IncludeMoments) ||
                    (procPar.PopFiles[1].FileWriteEnabled && procPar.PopFiles[1].IncludeMoments) ||
                    (procPar.PopFiles[0].FileWriteEnabled && procPar.PopFiles[0].IncludeSpectra) ||
                    (procPar.PopFiles[1].FileWriteEnabled && procPar.PopFiles[1].IncludeSpectra) ||
                    (procPar.PopFiles[0].FileWriteEnabled && procPar.PopFiles[0].IncludeXCorr) ||
                    (procPar.PopFiles[1].FileWriteEnabled && procPar.PopFiles[1].IncludeXCorr) )     {
                    _savingProcessedProducts = true;
                }
                else {
                    _savingProcessedProducts = false;
                }

                _allocTSOnly = _parameters.SystemPar.RadarPar.ProcPar.AllocTSOnly;
                if (_savingProcessedProducts) {
                    _allocTSOnly = false;
                }

                //SetArrayDim();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool UseFullArrays {
            get { return _useFullArrays; }
            set {
                if (!_needReallocate && (value != _useFullArrays)) {
                    // if value has changed, reallocate
                    _needReallocate = true;
                }
                _useFullArrays = value;

                // use this in Parameter property too.
                //SetArrayDim();
            }
        }

        #endregion properties

        #region Read-only Properties
        //
        /// <summary>
        /// double[_nRx][_nSpec][_nPts][_nSamples] sampled data for each IPP
        /// </summary>
        public double[][][][] SampledTimeSeries {
            get {
                SetArrayDim();
                return _sampledTimeSeries;
            }
        }

        /// <summary>
        /// pre-allocated buffer for DAQ floats
        /// in time sampled order
        /// </summary>
        public Array DaqBuffer {
            get {
                SetArrayDim();
                return _daqBuffer;
            }
        }

        /// <summary>
        /// Ipp64fc[_nRx][_nSpec][_nHts][_nPts] Doppler time series for each height
        /// </summary>
        public Ipp64fc[][][][] DopplerTimeSeries {
            get {
                SetArrayDim();
                return _dopplerTimeSeries;
            }
        }

        /// <summary>
        /// Double[_nRx][_nHts][_nPts]  Doppler spectra
        /// </summary>
        public double[][][] Spectra {
            get {
                SetArrayDim();
                return _spectra;
            }
        }

        /// <summary>
        /// Double[_nRx][_nHts][_nPts]  clutter wavelet transforms
        /// </summary>
        public double[][][] WaveletClutterTransform {
            get {
                SetArrayDim();
                return _clutterWavelets;
            }
        }

        /// <summary>
        /// Ipp64fc[_nRx][_nHts][2*lag+1]  Cross-correlations
        /// </summary>
        public Ipp64fc[][][] XCorrelation {
            get {
                SetArrayDim();
                return _xCorr;
            }
        }

        /// <summary>
        /// Double[_nRx][_nHts][2*maxlag+1] Magnitude of Cross-correlations
        /// </summary>
        public double[][][] XCorrMag {
            get {
                SetArrayDim();
                return _xCorrMag;
            }
        }

        public double[][][] XCorrGaussCoeffs {
            get {
                SetArrayDim();
                return _xCorrGaussCoeffs;
            }
        }

        public double[][][] XCorrPolyCoeffs {
            get {
                SetArrayDim();
                return _xCorrPolyCoeffs;
            }
        }

        public double[][][] XCorrFcaLags {
            get {
                SetArrayDim();
                return _xCorrFcaLags;
            }
        }

        /// <summary>
        /// Double[_nRx][_nHts][2*maxlag+1]  Cross-correlation Ratio
        /// </summary>
        public double[][][] XCorrRatio {
            get {
                SetArrayDim();
                return _xCorrRatio;
            }
        }

        /// <summary>
        /// Double[_nRx][_nHts]  Slope of Cross-correlation Ratio line fit
        /// </summary>
        public LineFit[][] XCorrRatioLine {
            get {
                SetArrayDim();
                return _xCorrRatioLine;
            }
        }

        public double[][][] XCorrSlope0 {
            get {
                SetArrayDim();
                return _xCorrSlope0;
            }
        }

        /// <summary>
        /// Double[_nRx][ihts] noise level
        /// </summary>
        public double[][] Noise {
            get {
                SetArrayDim();
                return _noise;
            }
        }

        public double[][] RassTemp {
            get {
                SetArrayDim();
                return _rassTemp;
            }
        }

        /// <summary>
        /// double[_nRx][ihts] meanDoppler as fraction of nyquist/2
        /// </summary>
        public double[][] MeanDoppler {
            get {
                SetArrayDim();
                return _meanDoppler;
            }
        }

        public double[][] RassMeanDoppler {
            get {
                SetArrayDim();
                return _rassMeanDoppler;
            }
        }

        /// <summary>
        /// double[_nRx][ihts] spectral width as fraction of nyq/2
        /// </summary>
        public double[][] Width {
            get {
                SetArrayDim();
                return _width;
            }
        }

        public double[][] RassWidth {
            get {
                SetArrayDim();
                return _rassWidth;
            }
        }

        /// <summary>
        /// double[_nRx][ihts] signal power
        /// </summary>
        public double[][] Power {
            get {
                SetArrayDim();
                return _power;
            }
        }

        public double[][] RassPower {
            get {
                SetArrayDim();
                return _rassPower;
            }
        }

        /// <summary>
        /// int[_nRx][_nHts] GC pts in spectrum
        /// </summary>
        public int[][] ClutterPoints {
            get {
                SetArrayDim();
                return _clutterPoints;
            }
        }


        public int HtsDim {
            get {
                SetArrayDim();
                return _nHts;
            }
            //set { _nHts = value; }
        }

        public int SpecDim {
            get {
                SetArrayDim();
                return _nSpec;
            }
            //set { _nSpec = value; }
        }

        // number of spectra stored in SampledTimeSeries array
        public int SampledTSSpecDim {
            get {
                SetArrayDim();
                return _STSnSpec;
            }
        }

        // number of spectra stored in DopplerTimeSeries array
        public int DopplerTSSpecDim {
            get {
                SetArrayDim();
                return _DTSnSpec;
            }
        }

        public int PtsDim {
            get {
                SetArrayDim();
                return _nPtsSavedInArray;
            }
        }

        public int SamplesDim {
            get {
                SetArrayDim();
                return _nSamples;
            }
        }

        public int RxDim {
            get {
                SetArrayDim();
                return _nRx;
            }
        }

        public int NSpecAtATime {
            get {
                return _nSpecAtATime;
            }
        }

        //
        #endregion Read-only Properties

        ///////////////////////////////////////////////////////////////////////////////

        #region Constructors

        //
        public PopNAllocator() {
            _needReallocate = true;
            _parameters = null;
            _useFullArrays = false;   // could be either T/F
            // setting these initial values indicates Parameters and SetArrayDim have not been set
            _nRx = _nPts = _nHts = _nSamples = _nSpec = -1;
            _STSnSpec = _DTSnSpec = -1;
            _savingDopplerTS = _savingRawTS = false;
        }

        public PopNAllocator(PopParameters param) : this(param, false) { }

        public PopNAllocator(PopParameters param, bool useFullArrays) {
            _needReallocate = true;
            Parameters = param;
            // setting this property causes SetArrayDim() and Allocate() to be called
            UseFullArrays = useFullArrays;
        }
        //
        #endregion constructors

        /// ////////////////////////////////////////////////////////////////////////////////////

        #region Public Methods

        public void Allocate() {
            SetArrayDim();
        }

        ///////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataPackage"></param>
        /// <param name="parameters"></param>
        public void AllocateDataArrays(PopDataPackage3 dataPackage /*, PopParameters parameters */) {

            UseFullArrays = false;

            if (_parameters.ReplayPar.Enabled) {
                UseFullArrays = true;
            }

            //double memUsedMB = GC.GetTotalMemory(true) / 1000000.0;
            //SendStatusString("Before Allocator: " + memUsedMB.ToString("F0") + " MB used");

            // use "parameters" argument here because this could be parameters read from data file:
            //_memoryAllocator.Parameters = parameters;

            dataPackage.SampledTimeSeries = SampledTimeSeries;
            dataPackage.TransformedTimeSeries = DopplerTimeSeries;
            dataPackage.Spectra = Spectra;
            dataPackage.WaveletClutterTransform = WaveletClutterTransform;
            dataPackage.XCorrelation = XCorrelation;
            dataPackage.XCorrMag = XCorrMag;
            dataPackage.XCorrGaussCoeffs = XCorrGaussCoeffs;
            dataPackage.XCorrFcaLags = XCorrFcaLags;
            dataPackage.XCorrPolyCoeffs = XCorrPolyCoeffs;
            dataPackage.XCorrRatio = XCorrRatio;
            dataPackage.XCorrRatioLine = XCorrRatioLine;
            dataPackage.XCorrSlope0 = XCorrSlope0;
            dataPackage.Noise = Noise;
            dataPackage.Power = Power;
            dataPackage.MeanDoppler = MeanDoppler;
            dataPackage.Width = Width;
            dataPackage.ClutterPoints = ClutterPoints;

            dataPackage.RassMeanDoppler = RassMeanDoppler;
            dataPackage.RassWidth = RassWidth;
            dataPackage.RassPower = RassPower;
            dataPackage.RassTemp = RassTemp;

            /*
            if (!parameters.ReplayPar.Enabled) {
                // TODO: for now, in replay do not do filter factors yet;
                //  wait until we straighten out nhts and nsamples;
                //  fix this later.
                CreateFilterFactors();
            }
            */

            //memUsedMB = GC.GetTotalMemory(false) / 1000000.0;
            //SendStatusString(" After Allocator: " + memUsedMB.ToString("F0") + " MB used");
        }


        //
        #endregion public methods

        //////////////////////////////////////////////////////////////////////////////////

        #region Private Methods

        //  SetArrayDim()
        /// <summary>
        /// Call this method before any data array or array dimension is read
        ///     or any time that a change is made
        ///     that could possibly alter array dimensions.
        /// </summary>
        private void SetArrayDim() {

            if (_parameters == null) {
                return;
            }

            if (!_needReallocate) {
                return;
            }

            if (_useFullArrays) {
                _STSnSpec = _nSpec;
                _DTSnSpec = _nSpec;
            }
            else {
                if (_savingRawTS || _savingDopplerTS) {
                    _STSnSpec = _nSpec;
                    _DTSnSpec = _nSpec;
                }
                else {
                    _STSnSpec = _nSpecAtATime;
                    _DTSnSpec = _nSpecAtATime;
                }
                /*
                if (_savingDopplerTS) {
                    _DTSnSpec = _nSpec;
                }
                else {
                    _DTSnSpec = _nSpecAtATime;
                }
                */
            }

            AllocateArrays();
        }
        //

        //
        /// <summary>
        /// 
        /// </summary>
        /// <returns>bool value indicating success</returns>
        private bool AllocateArrays() {
            bool isOK = true;

            if (_nRx < 1 || _nPts < 1 || _nSamples < 1 || _DTSnSpec < 1 || _STSnSpec < 1 || _nHts < 1) {
                isOK = false;
            }
            else {
                AllocateDaqBuffer();
                AllocateSTSArray();
                if (!_allocTSOnly) {
                    AllocateDTSArray();
                    AllocateClutterWaveletArray();
                    AllocateSpecArray();
                    AllocateXCorrArrays();
                    AllocateMomentArrays();
                }
            }

            _needReallocate = false;

            return isOK;
        }

        /// <summary>
        /// 
        /// </summary>
        private void AllocateXCorrArrays() {

            if (_nRx < 1 || _nHts < 1 || _nLags < 1) {
                return;
            }

            if (_nRx == 1 && !_parameters.SystemPar.RadarPar.ProcPar.DoAutoCorr1Rx) {
                return;
            }

            int rxDim;

            if (_nRx == 1) {
                rxDim = 1;  // only can do autocorr for 1 rx
            }
            else {
                rxDim = 2 * _nRx;  // do both auto and cross (only really correct for nrx==3)
            }
            int polyFitOrder = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrPolyFitOrder;

            if (_xCorr == null || _xCorr.Length < rxDim) {
                _xCorr = null;
                _xCorr = new Ipp64fc[rxDim][][];
            }
            if (_xCorr[0] == null || _xCorr[0].Length < _nHts ||
                _xCorr[rxDim - 1] == null || _xCorr[rxDim - 1].Length < _nHts) {
                for (int k = 0; k < rxDim; k++) {
                    _xCorr[k] = null;
                    _xCorr[k] = new Ipp64fc[_nHts][];
                }
            }
            if (_xCorr[0][0] == null || _xCorr[0][0].Length < _nPts ||
                _xCorr[0][_nHts - 1] == null || _xCorr[0][_nHts - 1].Length < _nLags) {
                for (int k = 0; k < rxDim; k++) {
                    for (int i = 0; i < _nHts; i++) {
                        _xCorr[k][i] = null;
                        _xCorr[k][i] = new Ipp64fc[_nLags];
                    }
                }
            }

            if (_xCorrMag == null || _xCorrMag.Length < rxDim) {
                _xCorrMag = null;
                _xCorrMag = new double[rxDim][][];
            }
            if (_xCorrMag[0] == null || _xCorrMag[0].Length < _nHts ||
                _xCorrMag[rxDim - 1] == null || _xCorrMag[rxDim - 1].Length < _nHts) {
                for (int k = 0; k < rxDim; k++) {
                    _xCorrMag[k] = null;
                    _xCorrMag[k] = new double[_nHts][];
                }
            }
            if (_xCorrMag[0][0] == null || _xCorrMag[0][0].Length < _nPts ||
                _xCorrMag[0][_nHts - 1] == null || _xCorrMag[0][_nHts - 1].Length < _nLags) {
                for (int k = 0; k < rxDim; k++) {
                    for (int i = 0; i < _nHts; i++) {
                        _xCorrMag[k][i] = null;
                        _xCorrMag[k][i] = new double[_nLags];
                    }
                }
            }

            if (_xCorrGaussCoeffs == null || _xCorrGaussCoeffs.Length < _nRx) {
                _xCorrGaussCoeffs = null;
                _xCorrGaussCoeffs = new double[_nRx][][];
            }
            if (_xCorrGaussCoeffs[0] == null || _xCorrGaussCoeffs[0].Length < _nHts ||
                _xCorrGaussCoeffs[_nRx - 1] == null || _xCorrGaussCoeffs[_nRx - 1].Length < _nHts) {
                for (int k = 0; k < _nRx; k++) {
                    _xCorrGaussCoeffs[k] = null;
                    _xCorrGaussCoeffs[k] = new double[_nHts][];
                }
            }
            if (_xCorrGaussCoeffs[0][0] == null || _xCorrGaussCoeffs[0][0].Length != 4 ||
                _xCorrGaussCoeffs[0][_nHts - 1] == null || _xCorrGaussCoeffs[0][_nHts - 1].Length != 4) {
                for (int k = 0; k < _nRx; k++) {
                    for (int i = 0; i < _nHts; i++) {
                        _xCorrGaussCoeffs[k][i] = null;
                        _xCorrGaussCoeffs[k][i] = new double[4];
                    }
                }
            }

            if (_xCorrFcaLags == null || _xCorrFcaLags.Length < _nRx) {
                _xCorrFcaLags = null;
                _xCorrFcaLags = new double[_nRx][][];
            }
            if (_xCorrFcaLags[0] == null || _xCorrFcaLags[0].Length < _nHts ||
                _xCorrFcaLags[_nRx - 1] == null || _xCorrFcaLags[_nRx - 1].Length < _nHts) {
                for (int k = 0; k < _nRx; k++) {
                    _xCorrFcaLags[k] = null;
                    _xCorrFcaLags[k] = new double[_nHts][];
                }
            }
            if (_xCorrFcaLags[0][0] == null || _xCorrFcaLags[0][0].Length != 3 ||
                _xCorrFcaLags[0][_nHts - 1] == null || _xCorrFcaLags[0][_nHts - 1].Length != 3) {
                for (int k = 0; k < _nRx; k++) {
                    for (int i = 0; i < _nHts; i++) {
                        _xCorrFcaLags[k][i] = null;
                        _xCorrFcaLags[k][i] = new double[3];
                    }
                }
            }

            if (_xCorrPolyCoeffs == null || _xCorrPolyCoeffs.Length < _nRx) {
                _xCorrPolyCoeffs = null;
                _xCorrPolyCoeffs = new double[2*_nRx][][];
            }
            if (_xCorrPolyCoeffs[0] == null || _xCorrPolyCoeffs[0].Length < _nHts ||
                _xCorrPolyCoeffs[2*_nRx - 1] == null || _xCorrPolyCoeffs[2*_nRx - 1].Length < _nHts) {
                for (int k = 0; k < 2*_nRx; k++) {
                    _xCorrPolyCoeffs[k] = null;
                    _xCorrPolyCoeffs[k] = new double[_nHts][];
                }
            }
            if (_xCorrPolyCoeffs[0][0] == null || _xCorrPolyCoeffs[0][0].Length != (polyFitOrder+1) ||
                _xCorrPolyCoeffs[0][_nHts - 1] == null || _xCorrPolyCoeffs[0][_nHts - 1].Length != (polyFitOrder + 1)) {
                for (int k = 0; k < 2*_nRx; k++) {
                    for (int i = 0; i < _nHts; i++) {
                        _xCorrPolyCoeffs[k][i] = null;
                        _xCorrPolyCoeffs[k][i] = new double[(polyFitOrder + 1)];
                    }
                }
            }

            if (_xCorrSlope0 == null || _xCorrSlope0.Length < _nRx) {
                _xCorrSlope0 = null;
                _xCorrSlope0 = new double[_nRx][][];
            }
            if (_xCorrSlope0[0] == null || _xCorrSlope0[0].Length < _nHts ||
                _xCorrSlope0[_nRx - 1] == null || _xCorrSlope0[_nRx - 1].Length < _nHts) {
                for (int k = 0; k < _nRx; k++) {
                    _xCorrSlope0[k] = null;
                    _xCorrSlope0[k] = new double[_nHts][];
                    for (int i = 0; i < _nHts; i++) {
                        _xCorrSlope0[k][i] = null;
                        _xCorrSlope0[k][i] = new double[2];
                    }
                }
            }

            if (_xCorrRatio == null || _xCorrRatio.Length < _nRx) {
                _xCorrRatio = null;
                _xCorrRatio = new double[_nRx][][];
                _xCorrRatioLine = new LineFit[_nRx][];
            }
            if (_xCorrRatio[0] == null || _xCorrRatio[0].Length < _nHts ||
                _xCorrRatio[_nRx - 1] == null || _xCorrRatio[_nRx - 1].Length < _nHts) {
                for (int k = 0; k < _nRx; k++) {
                    _xCorrRatio[k] = null;
                    _xCorrRatio[k] = new double[_nHts][];
                    _xCorrRatioLine[k] = new LineFit[_nHts];
                }
            }
            if (_xCorrRatio[0][0] == null || _xCorrRatio[0][0].Length < _nPts ||
                _xCorrRatio[0][_nHts - 1] == null || _xCorrRatio[0][_nHts - 1].Length < _nLags) {
                for (int k = 0; k < _nRx; k++) {
                    for (int i = 0; i < _nHts; i++) {
                        _xCorrRatio[k][i] = null;
                        _xCorrRatio[k][i] = new double[_nLags];
                    }
                }
            }

        }

        //

        private void AllocateDaqBuffer() {

            if (_nRx < 1 || _nPts < 1 || _nSamples < 1) {
                return;
            }

            if (_parameters.ReplayPar.Enabled) {
                // Do not need DaqBuffer in Replay mode
                _daqBuffer = null;
                return;
            }

            bool reallocate = false;

            int neededSize = _nRx * _STSnSpec * _nPts * _nSamples;
            if (_daqBuffer == null) {
                reallocate = true;
            }
            else if (_daqBuffer.Length < neededSize) {
                reallocate = true;
            }

            if (reallocate) {
                //SendMessage("--Allocating DAQ array: " + (neededSize/1000000.0).ToString("F0") + " MFloats");
                _daqBuffer = null;
                _daqBuffer = new float[neededSize];
            }

        }

        /// <summary>
        /// 
        /// </summary>
        private void AllocateSTSArray() {

            if (_nRx < 1 || _nPts < 1 || _nSamples < 1) {
                return;
            }

            /*
            bool reallocate = false;
            if (_sampledTimeSeries == null) {
                reallocate = true;
            }
            else if ((_sampledTimeSeries.Length < _nRx) ||
                    (_sampledTimeSeries[0].Length < _STSnSpec) ||
                    (_sampledTimeSeries[0][0].Length < _nPts) ||
                    (_sampledTimeSeries[0][0][0].Length < _nSamples)) {
                reallocate = true;
            }
             * */

            //if (reallocate) {

                
                int usedMemoryMB;
                int requestMemMB = (int)((_nRx * _STSnSpec * _nPts * _nSamples * 8) >> 20);
                
                /*
                bool memOK = WeHaveEnoughMemory(requestMemMB, out usedMemoryMB);
                if (!memOK) {
                    throw new ApplicationException("NOT enough memory to allocate " + requestMemMB.ToString() + " MiB for STS array");
                    // but it is not allocated all in one big array
                }
                 * */

                if (_sampledTimeSeries == null || _sampledTimeSeries.Length < _nRx) {
                    //SendMessage("--Allocating STS RX array[" + _nRx.ToString() + "]");
                    _sampledTimeSeries = null;
                    _sampledTimeSeries = new double[_nRx][][][];
                }
                if (_sampledTimeSeries[0] == null || _sampledTimeSeries[0].Length < _STSnSpec ||
                    _sampledTimeSeries[_nRx-1] == null || _sampledTimeSeries[_nRx-1].Length < _STSnSpec) {
                    //SendMessage("--Allocating STS NSpec arrays: " + _nRx.ToString() + " x " + _STSnSpec.ToString());
                    for (int k = 0; k < _nRx; k++) {
                        _sampledTimeSeries[k] = null;
                        _sampledTimeSeries[k] = new double[_STSnSpec][][];
                    }
                }
                if (_sampledTimeSeries[0][0] == null || (_sampledTimeSeries[0][0].Length < _nPts) ||
                    _sampledTimeSeries[0][_STSnSpec-1] == null || _sampledTimeSeries[0][_STSnSpec-1].Length < _nPts) {
                    //SendMessage("--Allocating STS NPts arrays[" + _nPts.ToString() + "]");
                    for (int k = 0; k < _nRx; k++) {
                        for (int j = 0; j < _STSnSpec; j++) {
                            _sampledTimeSeries[k][j] = null;
                            _sampledTimeSeries[k][j] = new double[_nPts][];
                        }
                    }
                }
                if (_sampledTimeSeries[0][0][0] == null || _sampledTimeSeries[0][0][0].Length < _nSamples ||
                            _sampledTimeSeries[0][0][_nPts-1].Length < _nSamples) {
                    //SendMessage("--Allocating STS NSamples arrays[" + _nSamples.ToString() + "]");
                    for (int k = 0; k < _nRx; k++) {
                        for (int j = 0; j < _STSnSpec; j++) {
                            for (int i = 0; i < _nPts; i++) {
                                _sampledTimeSeries[k][j][i] = null;
                                _sampledTimeSeries[k][j][i] = new double[_nSamples];
                            }
                        }
                    }
                }

                /*
                for (int k = 0; k < _nRx; k++) {
                    _sampledTimeSeries[k] = null;
                    _sampledTimeSeries[k] = new double[_STSnSpec][][];
                    for (int j = 0; j < _STSnSpec; j++) {
                        _sampledTimeSeries[k][j] = null;
                        _sampledTimeSeries[k][j] = new double[_nPts][];
                        for (int i = 0; i < _nPts; i++) {
                            _sampledTimeSeries[k][j][i] = null;
                            _sampledTimeSeries[k][j][i] = new double[_nSamples];
                        }
                    }
                }
                */
           // }
        }

        private void SendMessage(string msg) {
            if (SendMessageAction != null) {
                SendMessageAction(msg);
            }
        }


        private void AllocateDTSArray() {

            if (_nRx < 1 || _nPts < 1 || _nHts < 1) {
                return;
            }

            //Properties.Settings.Default.Location = 4;
            /*
            bool reallocate = false;
            if (_dopplerTimeSeries == null) {
                reallocate = true;
            }
            else if ((_dopplerTimeSeries.Length < _nRx) ||
                    (_dopplerTimeSeries[0].Length < _DTSnSpec) ||
                    (_dopplerTimeSeries[0][0].Length < _nHts) ||
                    (_dopplerTimeSeries[0][0][0].Length < _nPts)) {
                reallocate = true;
            }
            */

            //if (reallocate) {

                int usedMemoryMB;
                int requestMemMB = (int)((_nRx * _DTSnSpec * _nHts * _nPts * 16) >> 20);
                /*
                bool memOK = WeHaveEnoughMemory(requestMemMB, out usedMemoryMB);
                if (!memOK) {
                    throw new ApplicationException("NOT enough memory to allocate " + requestMemMB.ToString() + " MiB for DTS array");
                }
                 * */

                if (_dopplerTimeSeries == null || _dopplerTimeSeries.Length < _nRx) {
                    //SendMessage("++Allocating DTS RX array.");
                    _dopplerTimeSeries = null;
                    _dopplerTimeSeries = new Ipp64fc[_nRx][][][];
                }
                if (_dopplerTimeSeries[0] == null || _dopplerTimeSeries[0].Length < _DTSnSpec ||
                    _dopplerTimeSeries[_nRx-1] == null || _dopplerTimeSeries[_nRx-1].Length < _DTSnSpec) {
                    //SendMessage("++Allocating DTS NSpec arrays.");
                    for (int k = 0; k < _nRx; k++) {
                        _dopplerTimeSeries[k] = null;
                        _dopplerTimeSeries[k] = new Ipp64fc[_DTSnSpec][][];
                    }
                }
                if (_dopplerTimeSeries[0][0] == null || (_dopplerTimeSeries[0][0].Length < _nHts) ||
                    _dopplerTimeSeries[0][_DTSnSpec-1]==null  ||  _dopplerTimeSeries[0][_DTSnSpec - 1].Length < _nHts) {
                    //SendMessage("++Allocating DTS NHts arrays[" + _nHts.ToString() + "]");
                    for (int k = 0; k < _nRx; k++) {
                        for (int j = 0; j < _DTSnSpec; j++) {
                            _dopplerTimeSeries[k][j] = null;
                            _dopplerTimeSeries[k][j] = new Ipp64fc[_nHts][];
                        }
                    }
                    
                    
                }
                if (_dopplerTimeSeries[0][0][0] == null || _dopplerTimeSeries[0][0][0].Length < _nPts ||
                    _dopplerTimeSeries[0][0][_nHts - 1]==null || _dopplerTimeSeries[0][0][_nHts - 1].Length < _nPts) {
                    //SendMessage("++Allocating DTS NPts arrays[" + _nPts.ToString()+"]");
                    for (int k = 0; k < _nRx; k++) {
                        for (int j = 0; j < _DTSnSpec; j++) {
                            for (int i = 0; i < _nHts; i++) {
                                _dopplerTimeSeries[k][j][i] = null;
                                _dopplerTimeSeries[k][j][i] = new Ipp64fc[_nPts];
                            }
                        }
                    }
                }

            /*
             SendMessage("++Allocating DTS arrays: ");
             _dopplerTimeSeries = null;
             _dopplerTimeSeries = new Ipp64fc[_nRx][][][];
             for (int k = 0; k < _nRx; k++) {
                 _dopplerTimeSeries[k] = null;
                 _dopplerTimeSeries[k] = new Ipp64fc[_DTSnSpec][][];
                 for (int j = 0; j < _DTSnSpec; j++) {
                     _dopplerTimeSeries[k][j] = null;
                     _dopplerTimeSeries[k][j] = new Ipp64fc[_nHts][];
                     for (int i = 0; i < _nHts; i++) {
                         //SendMessage("--Allocating DTS NPts arrays: " + _nPts.ToString());
                         _dopplerTimeSeries[k][j][i] = null;
                         _dopplerTimeSeries[k][j][i] = new Ipp64fc[_nPts];
                     }
                 }
             }
             */
            //}
        }

        private void AllocateClutterWaveletArray() {
            
            if (_clutterWavelets == null || _clutterWavelets.Length < _nRx) {
                //SendMessage("--Allocating Spec RX array.");
                _clutterWavelets = null;
                _clutterWavelets = new double[_nRx][][];
            }
            if (_clutterWavelets[0] == null || _clutterWavelets[0].Length < _nHts ||
                _clutterWavelets[_nRx - 1] == null || _clutterWavelets[_nRx - 1].Length < _nHts) {
                //SendMessage("--Allocating Spec NHts arrays.");
                for (int k = 0; k < _nRx; k++) {
                    _clutterWavelets[k] = null;
                    _clutterWavelets[k] = new double[_nHts][];
                }
            }
            int pow2 = DACarter.Utilities.Tools.NextPowerOf2(_nPtsSavedInArray);
            if (_clutterWavelets[0][0] == null || _clutterWavelets[0][0].Length < 2 * pow2 ||
                _clutterWavelets[0][_nHts - 1] == null || _clutterWavelets[0][_nHts - 1].Length < 2 * pow2) {
                //SendMessage("--Allocating Spec NPts arrays.");
                for (int k = 0; k < _nRx; k++) {
                    for (int i = 0; i < _nHts; i++) {
                        _clutterWavelets[k][i] = null;
                        _clutterWavelets[k][i] = new double[2 * pow2];
                    }
                }
            }

            /*
            if (_dataPackage.WaveletClutterTransform == null ||
               _dataPackage.WaveletClutterTransform.Length < _nRx ||
               _dataPackage.WaveletClutterTransform[0].Length < _nHts ||
               _dataPackage.WaveletClutterTransform[0][0].Length < _waveletOutputNpts) {

                _dataPackage.WaveletClutterTransform = null;
                _dataPackage.WaveletClutterTransform = new double[_nRx][][];
                for (int i = 0; i < _nRx; i++) {
                    _dataPackage.WaveletClutterTransform[i] = new double[_nHts][];
                    for (int j = 0; j < _nHts; j++) {
                        _dataPackage.WaveletClutterTransform[i][j] = new double[_waveletInputNpts];
                    }
                }
            }
             * */
        }

        private void AllocateSpecArray() {

            

            if (_spectra == null || _spectra.Length < _nRx) {
                //SendMessage("--Allocating Spec RX array.");
                _spectra = null;
                _spectra = new double[_nRx][][];
            }
            if (_spectra[0] == null || _spectra[0].Length < _nHts ||
                _spectra[_nRx-1] == null || _spectra[_nRx-1].Length < _nHts) {
                //SendMessage("--Allocating Spec NHts arrays.");
                for (int k = 0; k < _nRx; k++) {
                    _spectra[k] = null;
                    _spectra[k] = new double[_nHts][];
                }
            }
            if (_spectra[0][0] == null || _spectra[0][0].Length < _nPtsSavedInArray ||
                _spectra[0][_nHts - 1] == null || _spectra[0][_nHts - 1].Length < _nPtsSavedInArray) {
                //SendMessage("--Allocating Spec NPts arrays.");
                for (int k = 0; k < _nRx; k++) {
                    for (int i = 0; i < _nHts; i++) {
                        _spectra[k][i] = null;
                        _spectra[k][i] = new double[_nPtsSavedInArray];
                    }
                }
            }

            /*
            bool reallocate = false;
            if (_spectra == null) {
                reallocate = true;
            }
            else if ((_spectra.Length < _nRx) ||
                    (_spectra[0].Length < _nHts) ||
                    (_spectra[0][0].Length < _nPts)) {
                reallocate = true;
            }
            if (reallocate) {
                //SendMessage("--Allocating Spec arrays.");
                _spectra = null;
                _spectra = new double[_nRx][][];
                for (int k = 0; k < _nRx; k++) {
                    _spectra[k] = null;
                    _spectra[k] = new double[_nHts][];
                    for (int i = 0; i < _nHts; i++) {
                        //SendMessage("--Allocating Spec NPts arrays.");
                        _spectra[k][i] = null;
                        _spectra[k][i] = new double[_nPts];
                    }
                }
            }
            */
        }

        private bool WeHaveEnoughMemory(int reqMemInMB, out int totalMemInMB) {
            long memBefore = GC.GetTotalMemory(false);
            totalMemInMB = (int)(memBefore >> 20);
            MemoryFailPoint mfp = null;
            try {
                mfp = new MemoryFailPoint(reqMemInMB);
            }
            catch (InsufficientMemoryException e) {
                return false;
            }
            finally {
                if (mfp != null) {
                    // POPREV: added 3.16.4
                    mfp.Dispose();
                }
            }
            return true;
        }


        private void AllocateMomentArrays() {
            bool reallocate = false;
            if ((_noise == null) ||
                (_power == null) ||
                (_meanDoppler == null) ||
                (_width == null)) {
                reallocate = true;
            }
            else if ((_noise.Length < _nRx) ||
               (_power.Length < _nRx) ||
                (_meanDoppler.Length < _nRx) ||
                (_width.Length < _nRx)) {
                reallocate = true;
            }
            else if ((_noise[0].Length < _nHts) ||
               (_power[0].Length < _nHts) ||
                (_meanDoppler[0].Length < _nHts) ||
                (_width[0].Length < _nHts)) {
                reallocate = true;
            }

            if (_isRass) {
            if ((_rassTemp == null) ||
                (_rassPower == null) ||
                (_rassMeanDoppler == null) ||
                (_rassWidth == null)) {
                reallocate = true;
            }
            else if ((_rassTemp.Length < _nRx) ||
               (_rassPower.Length < _nRx) ||
                (_rassMeanDoppler.Length < _nRx) ||
                (_rassWidth.Length < _nRx)) {
                reallocate = true;
            }
            else if ((_rassTemp[0].Length < _nHts) ||
               (_rassPower[0].Length < _nHts) ||
                (_rassMeanDoppler[0].Length < _nHts) ||
                (_rassWidth[0].Length < _nHts)) {
                reallocate = true;
            }
            }

            if (reallocate) {

                _noise = null;
                _power = null;
                _meanDoppler = null;
                _width = null;
                _rassTemp = null;
                _rassPower = null;
                _rassMeanDoppler = null;
                _rassWidth = null;
                _clutterPoints = null;

                _noise = new double[_nRx][];
                _power = new double[_nRx][];
                _meanDoppler = new double[_nRx][];
                _width = new double[_nRx][];
                _clutterPoints = new int[_nRx][];
                if (_isRass) {
                    _rassTemp = new double[_nRx][];
                    _rassPower = new double[_nRx][];
                    _rassMeanDoppler = new double[_nRx][];
                    _rassWidth = new double[_nRx][];
                }

                for (int k = 0; k < _nRx; k++) {

                    _noise[k] = null;
                    _power[k] = null;
                    _meanDoppler[k] = null;
                    _width[k] = null;
                    _clutterPoints[k] = null;
                    if (_isRass) {
                        _rassTemp[k] = null;
                        _rassPower[k] = null;
                        _rassMeanDoppler[k] = null;
                        _rassWidth[k] = null;
                    }

                    _noise[k] = new double[_nHts];
                    _power[k] = new double[_nHts];
                    _meanDoppler[k] = new double[_nHts];
                    _width[k] = new double[_nHts];
                    _clutterPoints[k] = new int[_nHts];
                    if (_isRass) {
                        _rassTemp[k] = new double[_nHts];
                        _rassPower[k] = new double[_nHts];
                        _rassMeanDoppler[k] = new double[_nHts];
                        _rassWidth[k] = new double[_nHts];
                    }
                }
            }
        }


        #endregion private methods

    }
}
