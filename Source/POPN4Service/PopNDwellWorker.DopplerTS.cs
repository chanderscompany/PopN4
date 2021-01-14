using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using DACarter.NOAA.Hardware;
using DACarter.PopUtilities;
using ipp;
using DACarter.Utilities.Maths;
using POPCommunication;
//
//////////////////////////
//
// PopNDwellWorker.DopplerTS
//
// Classes and methods that relate to Doppler time series are in this file, including
//  Creating test time series;
//  Filtering and Processing of the Doppler time series
//
//////////////////////////

namespace POPN4Service {

    partial class PopNDwellWorker {

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Creates test data
        /// </summary>
        private PopCommands CreateTestTimeSeries(int firstSpec, int nSpecToDo) {

            PopCommands command = PopCommands.None;
            double factor1 = GetTSeriesScaleFactor();

            double signalGate = 20.5;           // ht index at which echo appears
            double clutterGate = 20.5;
            double signalDopplerInNyq = 0.5;   // position of Doppler shift in fraction of Nyquist
            double signalDopplerInNyq2 = 0.25;   // position of Doppler shift in fraction of Nyquist
            signalDopplerInNyq = ((int)(signalDopplerInNyq * (_nPts / 2)) + 0.5) / (_nPts / 2.0);

            if (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.PulsedTx) {
                int bmSeqI = _dataPackage.CurrentParIndices.BmSeqI;
                int dirI = _dataPackage.CurrentParIndices.DirI;
                int parI = _dataPackage.CurrentParIndices.ParI;
                double elev = _parameters.SystemPar.RadarPar.BeamDirections[dirI].Elevation;
                double az = _parameters.SystemPar.RadarPar.BeamDirections[dirI].Azimuth;
                if (_parameters.SystemPar.RadarPar.BeamDirections[dirI].Elevation == 90.0) {
                    signalDopplerInNyq2 = 0.05;
                }
                else if (az == 180.0 || az == 270.0) {
                    signalDopplerInNyq2 = -0.5;
                }
                else {
                    signalDopplerInNyq2 = 0.5;
                }
            }

            // use this line to get new random gaussian distribution each time:
            MathNet.Numerics.Distributions.NormalDistribution gaussian = new MathNet.Numerics.Distributions.NormalDistribution(0.0, 1.0);
            // use this to get the same random sequence each time:
            //Random rnd = new Random(73711);
            //Random gaussian = rnd;  // not really a gaussian, of course

            double noise1, noise2, noise3;
            double signal, clutter;
            _progress = 0;
            double amp0 = 25.0;
            double amp;
            double dc = 0.0;

            for (int k = 0; k < nSpecToDo; k++) {
                _progress = (int)(100.0 * (double)(k + firstSpec) / _nSpec);
                Communicator.UpdateStatus(new PopStatusMessage(_status, _progress));
                command = CheckCommand();
                if ((command.Includes(PopCommands.Stop)) || (command.Includes(PopCommands.Kill))) {
                    _status = PopStatus.Stopped;
                    Communicator.UpdateStatus(new PopStatusMessage(_status));
                    break;
                }
                for (int j = 0; j < _nPts; j++) {
                    for (int i = 0; i < _nSamples; i++) {

                        amp = amp0;

                        noise1 = 10.0 * gaussian.NextDouble();
                        noise2 = 10.0 * gaussian.NextDouble();
                        noise3 = 10.0 * gaussian.NextDouble();

                        signal = TestSignal(1.0 * amp, Phase(signalDopplerInNyq, j), signalGate, i, _nSamples) +
                                 TestSignal(0.0 * amp, Phase(signalDopplerInNyq2, j), signalGate, i, _nSamples);
                        // now add component with nonchanging phase between ipps to represent leakage or GC:
                        clutter = TestSignal(0.0 * amp, Phase(0.01, j), clutterGate, i, _nSamples);
                        //clutter = 0.0;

                        //noise1 = 0.0;
                        _dataPackage.SampledTimeSeries[0][k][j][i] = noise1 +  signal  + clutter + dc;
                        // if wanting just noise:
                        //_dataPackage.SampledTimeSeries[0][k][j][i] = noise1;

                        if ((_nRx > 1) && _dataPackage.SampledTimeSeries.GetLength(0) > 1) {
                            _dataPackage.SampledTimeSeries[1][k][j][i] = noise2 + TestSignal(amp, Phase(signalDopplerInNyq, j), signalGate, i, _nSamples) + 0.1 * clutter;
                        }
                        if ((_nRx > 2) && _dataPackage.SampledTimeSeries.GetLength(0) > 2) {
                            _dataPackage.SampledTimeSeries[2][k][j][i] = noise3 + TestSignal(amp, Phase(signalDopplerInNyq, j), signalGate, i, _nSamples);
                        }
                        //SampledTimeSeries[k][j][i] = 16384.0 + nn;
                    }
                }
            }

            _progress = (int)(100.0 * (double)(nSpecToDo + firstSpec) / _nSpec);
            Communicator.UpdateStatus(new PopStatusMessage(_status, _progress));
            return command;

        }

        private double Phase(double dopplerShiftInNyq, int j) {
            //double dopplerShiftInNyq = 0.4;		// position of Doppler shift in fraction of Nyquist
            double ph = 2.0 * Math.PI * j / (2.0 / dopplerShiftInNyq);
            ph += Math.PI / 4.0;
            return ph;
        }

        private double TestSignal(double phase, double gate, int i, int nSamples) {
            double amp = 30.0;
            double rawCyclesPerIPP = gate;  // should be ht index at which echo appears
            double signal = amp * Math.Sin(phase + 2.0 * Math.PI * i * rawCyclesPerIPP / nSamples);
            return signal;

        }

        private double TestSignal(double amp, double phase, double gate, int i, int nSamples) {
            double rawCyclesPerIPP = gate;  // should be ht index at which echo appears
            double signal = amp * Math.Sin(phase + 2.0 * Math.PI * i * rawCyclesPerIPP / nSamples);
            return signal;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="phase"></param>
        /// <param name="gate"></param>
        /// <param name="i"></param>
        /// <param name="nSamples"></param>
        /// <param name="offset">number of pts away from middle for center of envelope</param>
        /// <returns></returns>
        private double TestSignal(double phase, double gate, int i, int nSamples, int offset) {
            double amp = 10.0;
            double rawCyclesPerIPP = gate;  // should be ht index at which echo appears
            double signal = amp * Math.Sin(phase + 2.0 * Math.PI * i * rawCyclesPerIPP / nSamples);
            int delta = Math.Abs(nSamples / 2 + offset - i);
            signal = signal * (1 - delta) / nSamples;
            return signal;

        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////

    public class DopplerTSArgs {

        public Ipp64fc[][][][] TimeSeries {get; set;}
        public PopParameters Params { get; set; }
        public int NRx { get; set; }
        public int NPts { get; set; }
        public int NXCPtMult { get; set; }
        public int NHts { get; set; }
        public int NSpec { get; set; }
        public int FirstSpec { get; set; }
        public int MaxLag { get; set; }
        public double[][][] Spectra { get; set; }
        public double[][][] XCorrMag { get; set; }
        public double[][][] WaveletTransform { get; set; }

        public DopplerTSArgs(
            Ipp64fc[][][][] timeSeries,
            PopParameters par,
            int nrx, int nhts, int npts, int nspec, int nxcptmult,
            int firstSpec,
            int maxLag,
            double[][][] spectra,
            double[][][] xCorrMag,
            double[][][] waveletTransform) {

                TimeSeries = timeSeries;
                Params = par;
                NRx = nrx;
                NHts = nhts;
                NPts = npts;
                NSpec = nspec;
                NXCPtMult = nxcptmult;
                FirstSpec = firstSpec;
                MaxLag = maxLag;
                Spectra = spectra;
                XCorrMag = xCorrMag;
                WaveletTransform = waveletTransform;

        }

        public override bool Equals(Object obj) {

            DopplerTSArgs rhs = obj as DopplerTSArgs;
            if (rhs == null) {
                return false;
            }

            if (TimeSeries.Length != rhs.TimeSeries.Length) {
                return false;
            }
            if (TimeSeries[0].Length != rhs.TimeSeries[0].Length) {
                return false;
            }
            if (TimeSeries[0][0].Length != rhs.TimeSeries[0][0].Length) {
                return false;
            }
            if (TimeSeries[0][0][0].Length != rhs.TimeSeries[0][0][0].Length) {
                return false;
            }

            if (Spectra.Length != rhs.Spectra.Length) {
                return false;
            }
            if (Spectra[0].Length != rhs.Spectra[0].Length) {
                return false;
            }
            if (Spectra[0][0].Length != rhs.Spectra[0][0].Length) {
                return false;
            }

            if (XCorrMag.Length != rhs.XCorrMag.Length) {
                return false;
            }
            if (XCorrMag[0].Length != rhs.XCorrMag[0].Length) {
                return false;
            }
            if (XCorrMag[0][0].Length != rhs.XCorrMag[0][0].Length) {
                return false;
            }

            if (Params != rhs.Params) {
                return false;
            }

            return true;
        }

        public static bool operator ==(DopplerTSArgs a, DopplerTSArgs b) {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b)) {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null)) {
                return false;
            }

            // Return true if the fields match:
            return a.Equals(b);
        }

        public static bool operator !=(DopplerTSArgs a, DopplerTSArgs b) {
            return !(a == b);
        }

    }

    public class DopplerTSProcessor {

        private DopplerSpectraMachine _specMachine;
        private CrossCorrelationMachine _xcorrMachine;
        private ClutterWaveletFilter _wvletFilter;
        private HSLowPassFilter _hsFilter;
        private DopplerTSArgs _args;
        private int _waveletClippedNpts;
        private int _waveletOutputNpts;

        private int _nRx, _nPts, _nHts, _nSpec;
        private int _keepGateFirst, _keepGateLast;   // Doppler gates to keep
        private bool _doClutterWavelets;
        private bool _doXCorr;
        private bool _doXCorrFilter;
        private double _wvltClutterCutoffNyq;
        private double _wvltClutterThldMedianRatio;
        private int _wvltClutterGateFirst, _wvltClutterGateLast;  // gates for wvlt clutter filter
        private int _maxLag;
        private int _hsFilterKeepPts;

        private Ipp64fc[][] _tSeries;
        private Ipp64fc[][][] _xCorrTSeries;

        public int WaveletOutputNpts {
            get { return _waveletOutputNpts; }
            set { _waveletOutputNpts = value; }
        }

        public int WaveletClippedNpts {
            get { return _waveletClippedNpts; }
            set { _waveletClippedNpts = value; }
        }

        public int XCorrNPts {
            get { return (_args.NPts * _args.NXCPtMult); }
        }

        public int XCorrNAvgs {
            get { return (_args.NSpec / _args.NXCPtMult); }
        }

        public DopplerTSProcessor(Ipp64fc[][] tseriesx,
                                    PopParameters par,
                                    int nrx, int nhts, int npts, int nspec,
                                    int firstGate, int lastGate,
                                    int firstSpec,
                                    bool doXCorr,
                                    bool doXCorrFilter,
                                    bool doClutterWvlt,
                                    int maxLag,
                                    double wvltClutterCutoffNyq,
                                    double wvltClutterThldMedianRatio,
                                    int wvltClutterGateFirst, 
                                    int wvltClutterGateLast,
                                    int hsFilterKeepPts,
                                    double[][][] spectra,
                                    double[][][] xCorrMag,
                                    double[][][] wvltTransform
                                    ) {

            throw new NotImplementedException("Long form of DopplerTSProcessor constructor has been deprecated.");

            _specMachine = new DopplerSpectraMachine(par, nrx, npts, nspec, firstSpec, spectra);
            if (doXCorr) {
                _xcorrMachine = new CrossCorrelationMachine(par, nrx, npts, nspec, maxLag, firstGate, lastGate, firstSpec, xCorrMag);
            }
            if (doClutterWvlt) {
                _wvletFilter = new ClutterWaveletFilter(nrx, nhts, npts, wvltClutterCutoffNyq, wvltClutterThldMedianRatio, wvltClutterGateFirst, wvltClutterGateLast, wvltTransform);
            }
            if (doXCorrFilter) {
                _hsFilter = new HSLowPassFilter(nrx, npts, hsFilterKeepPts);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        public DopplerTSProcessor(DopplerTSArgs args) {
            _args = args;

            Init(args);

            _tSeries = new Ipp64fc[_args.NRx][];
            for (int irx = 0; irx < _args.NRx; irx++) {
                _tSeries[irx] = new Ipp64fc[_args.NPts];
            }

            // if xcorr using longer time series
            if (args.NXCPtMult > 1) {
                _xCorrTSeries = new Ipp64fc[_args.NHts][][];
                for (int iht = 0; iht < _args.NHts; iht++) {
                    _xCorrTSeries[iht] = new Ipp64fc[_args.NRx][];
                    for (int irx = 0; irx < _args.NRx; irx++) {
                        _xCorrTSeries[iht][irx] = new Ipp64fc[_args.NPts * _args.NXCPtMult];
                    }
                }
            }
            else {
                _xCorrTSeries = null;
            }

            _specMachine = new DopplerSpectraMachine(_args.Params, 
                                                    _args.NRx, 
                                                    _args.NPts,
                                                    _args.NSpec,
                                                    _args.FirstSpec,
                                                    _args.Spectra);
            if (_doXCorr) {
                _xcorrMachine = new CrossCorrelationMachine(_args.Params,
                                                            _args.NRx,
                                                            _args.NPts * _args.NXCPtMult,  // length of time series for xcorr
                                                            _args.NSpec / _args.NXCPtMult,  // number of averages
                                                            _args.MaxLag,
                                                            _keepGateFirst,
                                                            _keepGateLast,
                                                            _args.FirstSpec,
                                                            _args.XCorrMag); 
            }
            if (_doClutterWavelets) {
                _wvletFilter = new ClutterWaveletFilter(_args.NRx, 
                                                        _args.NHts,
                                                        _args.NPts, 
                                                        _wvltClutterCutoffNyq, 
                                                        _wvltClutterThldMedianRatio, 
                                                        _wvltClutterGateFirst, 
                                                        _wvltClutterGateLast,
                                                        _args.WaveletTransform);
            }
            if (_doXCorrFilter) {
                _hsFilter = new HSLowPassFilter(_args.NRx, 
                                                _args.NPts, 
                                                _hsFilterKeepPts);
            }
        }

        /// <summary>
        /// Calculate useful things from PopParameters object
        /// </summary>
        /// <param name="args"></param>
        private void Init(DopplerTSArgs args) {

            _nHts = args.NHts;
            _nPts = args.NPts;
            _nRx = args.NRx;
            _nSpec = args.NSpec;
            _maxLag = args.MaxLag;

            ////////////////////

            _keepGateFirst = args.Params.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateFirst;
            _keepGateLast = args.Params.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateLast;

            ////////////////////

            _doXCorr = false;
            _doXCorrFilter = false;
            _hsFilterKeepPts = _nPts;

            if (args.Params.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCwSA) {
                _doXCorr = true;
                //lineFitPts = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrLineFitPts;
                if (args.Params.SystemPar.RadarPar.FmCwParSet[0].XCorrFilterFraction < 0.9999) {
                    _doXCorrFilter = true;
                    _hsFilterKeepPts = (int)(_nPts * args.Params.SystemPar.RadarPar.FmCwParSet[0].XCorrFilterFraction + 0.5);
                }
                if (_doXCorr && _nRx != 3) {
                    throw new ApplicationException("Must have 3 receivers for Spaced Antenna method.");
                }
            }

            if (_nRx == 1 && args.Params.SystemPar.RadarPar.ProcPar.DoAutoCorr1Rx) {
                _doXCorr = true;
            }

            ////////////////////

            _doClutterWavelets = args.Params.SystemPar.RadarPar.ProcPar.DoClutterWavelet;

            int wvltClutterNHts = 0;
            _wvltClutterThldMedianRatio = 0.0;
            double wvltClutterCutoffMps = 0.0;
            _wvltClutterCutoffNyq = 0.0;
            int gateOffset = 0;
            _wvltClutterGateFirst = 0;
            _wvltClutterGateLast = _nHts - 1;
            int gateFirst = args.Params.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateFirst;
            int gateLast = args.Params.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateLast;

            if (_doClutterWavelets) {
                if ((args.Params.SystemPar.RadarPar.RadarType & PopParameters.TypeOfRadar.FmCw) != PopParameters.TypeOfRadar.Unknown) {
                    gateOffset = (int)args.Params.SystemPar.RadarPar.FmCwParSet[0].GateOffset;
                }
                wvltClutterNHts = (int)(args.Params.SystemPar.RadarPar.ProcPar.WaveletClutterMaxHt + 0.5);
                //_parameters.SystemPar.RadarPar.BeamParSet[0].
                if (wvltClutterNHts > _nHts) {
                    wvltClutterNHts = _nHts;
                }
                if (wvltClutterNHts < 1) {
                    wvltClutterNHts = 0;
                    _doClutterWavelets = false;
                }
                else {
                    _wvltClutterThldMedianRatio = args.Params.SystemPar.RadarPar.ProcPar.WaveletClutterThldMed;
                    wvltClutterCutoffMps = args.Params.SystemPar.RadarPar.ProcPar.WaveletClutterCutoffMps;
                    double nyquist = args.Params.GetBeamParNyquist(0);
                    _wvltClutterCutoffNyq = wvltClutterCutoffMps / nyquist;

                    // number of gates to apply clutter starts above offset gate
                    _wvltClutterGateFirst = gateOffset;
                    _wvltClutterGateLast = wvltClutterNHts + gateOffset - 1;
                    if (_wvltClutterGateFirst < gateFirst) {
                        // only start wvlt clutter with first gate we are keeping
                        _wvltClutterGateFirst = gateFirst;
                    }
                    if (_wvltClutterGateLast > gateLast) {
                        _wvltClutterGateLast = gateLast;
                    }
                    wvltClutterNHts = _wvltClutterGateLast - _wvltClutterGateFirst + 1;
                }

            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iht"></param>
        /// <param name="ispec"></param>
        public void Compute(int iht, int ispec) {

            // TODO: fix so that doLongTermDC works with segmented TS processing 
            //  (i.e. when only doing partial nspec at a time)
            bool doLongTermDC = false;
            if (_args.Params.SystemPar.RadarPar.FmCwParSet[0].DopplerDcFilter && doLongTermDC) {
                if (ispec == 0) {
                    for (int irx = 0; irx < _args.NRx; irx++) {
                        Ipp64fc longSum = new Ipp64fc();
                        for (int jspec = 0; jspec < _args.NSpec; jspec++) {
                            for (int ipt = 0; ipt < _args.NPts; ipt++) {
                                longSum.re += _args.TimeSeries[irx][jspec][iht][ipt].re;
                                longSum.im += _args.TimeSeries[irx][jspec][iht][ipt].im;
                            }
                        }
                        longSum.re = longSum.re / (_args.NPts * _args.NSpec);
                        longSum.im = longSum.im / (_args.NPts * _args.NSpec);
                        for (int jspec = 0; jspec < _args.NSpec; jspec++) {
                            for (int ipt = 0; ipt < _args.NPts; ipt++) {
                                _args.TimeSeries[irx][jspec][iht][ipt].re -= longSum.re;
                                _args.TimeSeries[irx][jspec][iht][ipt].im -= longSum.im;
                            }
                        }
                    }

                }
            }

            // put original timeSeries into _tseries[] where all processing is done
            //  except for longterm DC which we did above.
            for (int irx = 0; irx < _args.NRx; irx++) {
                Array.Copy(_args.TimeSeries[irx][ispec][iht], _tSeries[irx], _args.NPts);
            }

            if (_doClutterWavelets) {
                _wvletFilter.RunFilter(_tSeries, iht);
                _waveletClippedNpts = _wvletFilter.WaveletClippedNpts;
                _waveletOutputNpts = _wvletFilter.WaveletOutputNpts;
            }
            else {
                // apply DC filter if not done via wavelets
                if (_args.Params.SystemPar.RadarPar.FmCwParSet[0].DopplerDcFilter && !doLongTermDC) {
                    for (int irx = 0; irx < _args.NRx; irx++) {
                        Ipp64fc[] sum = new Ipp64fc[2];
                        ipp.sp.ippsSum_64fc(_tSeries[irx], _args.NPts, sum);
                        sum[0].re = sum[0].re / _args.NPts;
                        sum[0].im = sum[0].im / _args.NPts;
                        ipp.sp.ippsSubC_64fc_I(sum[0], _tSeries[irx], _args.NPts);
                    }
                }
            }

            if (_doXCorrFilter) {
                if ((iht >= _keepGateFirst) && (iht <= _keepGateLast)) {
                    _hsFilter.RunFilter(_tSeries, iht);
                }
            }

            if (_args.Params.Debug.SaveFilteredTS) {
                for (int irx = 0; irx < _args.NRx; irx++) {
                    // this puts modified timeseries back into archive array
                    Array.Copy(_tSeries[irx], _args.TimeSeries[irx][ispec][iht], _args.NPts);
                }
            }

            if (_doXCorr) {
                if (iht == 20) {
                    int debug = 1;
                }
                if (_args.NXCPtMult == 1) {
                    _xcorrMachine.Compute(_tSeries, ispec, iht);
                }
                else {
                    // need to wait for longer time series for xcorr
                    int iset = ispec % _args.NXCPtMult;
                    for (int irx = 0; irx < _args.NRx; irx++) {
                        for (int ipt = 0; ipt < _args.NPts; ipt++) {
                            _xCorrTSeries[iht][irx][ipt + iset * _args.NPts] = _tSeries[irx][ipt];
                        }
                    }
                    if (iset == (_args.NXCPtMult - 1)) {
                        int iavg = ispec / _args.NXCPtMult;
                        _xcorrMachine.Compute(_xCorrTSeries[iht], iavg, iht); 
                    }
                }
            }

            _specMachine.Compute(_tSeries, ispec, iht);

        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// DopplerSpectraCalculator
    /// A class that totally encapsulates the calculation of Power spectra
    ///     from Doppler time series.
    /// No global variables are used
    /// </summary>
    /// <remarks>
    /// Useful for parallel processing.  Each task can (safely) use one of these objects.
    /// </remarks>
    public class DopplerSpectraMachine {

        // input and output data:
       // private Ipp64fc[][] TSeries;   // external array for complex time series input, TSeries[nRx][nPts]
        private double[][][] Spectra;  // external array for power spectra sums output, Spectra[nRx][nHts][nPts]
        // input parameters:
        private int _nPts;
        private int _nRx;
        private int _nSpec;
        private int _firstSpec;
        private PopParameters _parameters;
        // working arrays:
        private Ipp64fc[] _outArrayCC;  // temp array for single complex FFT output
        private Ipp64fc[] _tsc;     // temp array for single spectrum's time series
        private double[] _spec1;    // temp array for single spectrum
        // object that calculates FFTs, etc.:
        private IntelMath DopSpec;

        public DopplerSpectraMachine(PopParameters par, int nrx, int npts, int nspec, int firstSpec, double[][][] spectra) {
            _parameters = par.DeepCopy();
            _nRx = nrx;
            _nPts = npts;
            _nSpec = nspec;
            _firstSpec = firstSpec;
            Spectra = spectra;
            _outArrayCC = new Ipp64fc[npts];
            _tsc = new Ipp64fc[npts];
            _spec1 = new double[npts];
            DopSpec = new IntelMath();
        }

        /// <summary>
        /// Computes Doppler spectra from time series for a particular ht (iht)
        ///     and particular ispec for all receivers.
        /// </summary>
        /// <param name="tseries"></param>
        /// <param name="ispec"></param>
        /// <param name="iht"></param>
        public void Compute(Ipp64fc[][] tseries, int ispec, int iht) {

            for (int irx = 0; irx < _nRx; irx++) {

                // windowing and then FFT
                if ((_parameters.SystemPar.RadarPar.ProcPar.IsWindowing) &&
                     (_parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerWindow != PopParameters.WindowType.Rectangular)) {
                    if (_parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerWindow == PopParameters.WindowType.Hanning) {
                        DopSpec.ApplyHanningWindow(tseries[irx], _tsc, _nPts);
                    }
                    else if (_parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerWindow == PopParameters.WindowType.Hamming) {
                        DopSpec.ApplyHammingWindow(tseries[irx], _tsc, _nPts);
                    }
                    else if (_parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerWindow == PopParameters.WindowType.Blackman) {
                        DopSpec.ApplyBlackmanWindow(tseries[irx], _tsc, _nPts);
                    }
                    else if (_parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerWindow == PopParameters.WindowType.Riesz) {
                        DopSpec.ApplyRieszWindow(tseries[irx], _tsc, _nPts);
                    }
                    if (FFT.IsPowerOf2(_nPts)) {
                        DopSpec.FFT(_tsc, _outArrayCC, _nPts);
                    }
                    else {
                        DopSpec.DFT(_tsc, _outArrayCC, _nPts);
                    }

                    // this puts modified timeseries back into archive array
                    //Array.Copy(tsc, timeSeries[irx][ispec][iht], _nPts);
                }
                else {
                    if (FFT.IsPowerOf2(_nPts)) {
                        DopSpec.FFT(tseries[irx], _outArrayCC, _nPts);
                    }
                    else {
                        DopSpec.DFT(tseries[irx], _outArrayCC, _nPts);
                    }

                }

                DopSpec.PowerSpec(_outArrayCC, _spec1, _nPts);

                // spectral pts are now ordered so that dc is at index npts/2

                // test verifies that FFT routine preserves power
                /*
                bool doTest = false;
                if (doTest) {
                    double tsPower = DopSpec.TotalPowerTS(TSeries[irx], _nPts);
                    double fftPower = DopSpec.TotalPowerFFT(_outArrayCC, _nPts);
                    double specPower = 0.0;
                    for (int i = 0; i < _nPts; i++) {
                        specPower += _spec1[i];
                    }
                    double ratio1 = tsPower / fftPower;
                    double ratio2 = fftPower / specPower;
                }
                */

                // scaling factor of 0.5 applied so that total power equals variance of ONE channel of Doppler time series
                double scalingFactor = 0.5;
                for (int j = 0; j < _nPts; j++) {
                    // TODO: be careful here; multiple tasks working on the
                    //  same irx,iht,ipt but different ispec might clash
                    if (ispec == 0 && _firstSpec == 0) {
                        Spectra[irx][iht][j] = _spec1[j] * scalingFactor / _nSpec;
                    }
                    else {
                        Spectra[irx][iht][j] += _spec1[j] * scalingFactor / _nSpec;
                    }
                }

            }  // end spectral irx loop
        }


        public static double Add(ref double location1, double value) {
            double newCurrentValue = 0;
            while (true) {
                double currentValue = newCurrentValue;
                double newValue = currentValue + value;
                newCurrentValue = Interlocked.CompareExchange(ref location1, newValue, currentValue);
                if (newCurrentValue == currentValue)
                    return newValue;
            }
        }
    }  // end class

    /////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// CrossCorrelationProcessor
    /// A class that encapsulates the calculation of cross- and auto-correlations
    /// </summary>
    public class CrossCorrelationMachine {

        // input and output data:
        //private Ipp64fc[][] TSeries;   // external array for complex time series input, TSeries[nRx][nPts]
        private double[][][] XCorrMag; // external array for Cross/AutoCorrelation sums output, XCorrMag[nRx][nHts][lagPts]
        // input parameters
        private PopParameters _parameters;
        private int _nPts, _nRx, _nSpec;
        private int _maxLag, gateFirst, gateLast;
        private int _firstSpec;
        // local variables
        private int _nTotalCorr;    // total cross and auto correlations
        private int _nTotalLags;
        // helpers
        private IntelMath XCorrelations;
        private double[][] xcorr1;

        public CrossCorrelationMachine(PopParameters par, int nrx, int npts, int nspec,
                                        int maxLag, int firstGate, int lastGate,
                                        int firstSpec, double[][][] xCorrMag) {

            XCorrelations = new IntelMath();
            XCorrMag = xCorrMag;
            _parameters = par.DeepCopy();
            _nRx = nrx;
            _nPts = npts;
            _nSpec = nspec;
            _maxLag = maxLag;
            gateFirst = firstGate;
            gateLast = lastGate;
            //_nAutoCorr = _nRx;
            _nTotalCorr = 0;
            _nTotalLags = 2 * _maxLag + 1;
            _firstSpec = firstSpec;

            if (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCwSA) {
                _nTotalCorr = 6;
                if (_nRx != 3) {
                    throw new ApplicationException("Must have 3 receivers for Spaced Antenna method.");
                }
            }

            if (_nRx == 1 && _parameters.SystemPar.RadarPar.ProcPar.DoAutoCorr1Rx) {
                _nTotalCorr = 1;    // only autocorr for 1 rx;
            }

            xcorr1 = new double[_nTotalCorr][];    // a single cross-correlation
            for (int irx = 0; irx < _nTotalCorr; irx++) {
                xcorr1[irx] = new double[_nTotalLags];
            }

            if ((_nTotalCorr != 6) && (_nTotalCorr != 1)) {
                throw new ApplicationException("_nTotalCorr must be 6 or 1 in CrossCorrelationMachine.");
                
            }
        }

        public void Compute(Ipp64fc[][] TSeries, int ispec, int iht) {

            bool useDFT = false;

            if (iht >= gateFirst && iht <= gateLast) {
                // only do calculations for requested gates
                //for (int i = 0; i < _nPts; i++) {
                //    TSeries[0][i].im = 0.0;
                //}
                if (_parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrUseFFT) {
                    if (useDFT) {
                        if (_nTotalCorr == 6) {
                            XCorrelations.XCorrDFT(xcorr1[0], TSeries[0], TSeries[1], _nPts, _maxLag);  // cross-corr
                            XCorrelations.XCorrDFT(xcorr1[1], TSeries[1], TSeries[2], _nPts, _maxLag);
                            XCorrelations.XCorrDFT(xcorr1[2], TSeries[2], TSeries[0], _nPts, _maxLag);
                            XCorrelations.XCorrDFT(xcorr1[3], TSeries[0], TSeries[0], _nPts, _maxLag);  // auto-corr
                            XCorrelations.XCorrDFT(xcorr1[4], TSeries[1], TSeries[1], _nPts, _maxLag);
                            XCorrelations.XCorrDFT(xcorr1[5], TSeries[2], TSeries[2], _nPts, _maxLag);  
                        }
                        else if (_nTotalCorr == 1) {
                            XCorrelations.XCorrDFT(xcorr1[0], TSeries[0], TSeries[0], _nPts, _maxLag);  // autocorr only
                        }
                    }
                    else {
                        if (_nTotalCorr == 6) {
                            XCorrelations.XCorrFFT(xcorr1[0], TSeries[0], TSeries[1], _nPts, _maxLag);  // cross-corr
                            XCorrelations.XCorrFFT(xcorr1[1], TSeries[1], TSeries[2], _nPts, _maxLag);
                            XCorrelations.XCorrFFT(xcorr1[2], TSeries[2], TSeries[0], _nPts, _maxLag);
                            XCorrelations.XCorrFFT(xcorr1[3], TSeries[0], TSeries[0], _nPts, _maxLag);  // auto-corr
                            XCorrelations.XCorrFFT(xcorr1[4], TSeries[1], TSeries[1], _nPts, _maxLag);
                            XCorrelations.XCorrFFT(xcorr1[5], TSeries[2], TSeries[2], _nPts, _maxLag);
                        }
                        else if (_nTotalCorr == 1) {
                            XCorrelations.XCorrFFT(xcorr1[0], TSeries[0], TSeries[0], _nPts, _maxLag);  // autocorr only
                        }
                    }
                }
                else {
                    if (_nTotalCorr == 6) {
                        XCorrelations.XCorrMag(xcorr1[0], TSeries[0], TSeries[1], _nPts, _maxLag);  // cross-corr
                        XCorrelations.XCorrMag(xcorr1[1], TSeries[1], TSeries[2], _nPts, _maxLag);
                        XCorrelations.XCorrMag(xcorr1[2], TSeries[2], TSeries[0], _nPts, _maxLag);
                        XCorrelations.XCorrMag(xcorr1[3], TSeries[0], TSeries[0], _nPts, _maxLag);  // auto-corr
                        XCorrelations.XCorrMag(xcorr1[4], TSeries[1], TSeries[1], _nPts, _maxLag);
                        XCorrelations.XCorrMag(xcorr1[5], TSeries[2], TSeries[2], _nPts, _maxLag);
                    }
                    else if (_nTotalCorr == 1) {
                        XCorrelations.XCorrMag(xcorr1[0], TSeries[0], TSeries[0], _nPts, _maxLag);  // auto-corr only
                    }
                }

                for (int irx = 0; irx < _nTotalCorr; irx++) {
                    if (XCorrMag.Length >= irx + 1) {
                        // first interpolate across zero lag
                        //xcorr1[irx][_maxLag] = (xcorr1[irx][_maxLag - 1] + xcorr1[irx][_maxLag + 1]) / 2.0;
                        for (int j = 0; j < _nTotalLags; j++) {
                            if (ispec == 0 && _firstSpec == 0) {
                                XCorrMag[irx][iht][j] = xcorr1[irx][j] / _nSpec;
                            }
                            else {
                                XCorrMag[irx][iht][j] += xcorr1[irx][j] / _nSpec;
                            }
                        }
                    }
                }



            }
            else {

                for (int irx = 0; irx < _nRx; irx++) {
                    for (int j = 0; j < _nTotalLags; j++) {
                        XCorrMag[irx][iht][j] = 0.0;
                    }
                }

            }
        }

    }


    /////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// ClutterWaveletProcessor
    /// A class that encapsulates the filtering of Doppler time series data via
    ///     the Daubeshies wavelet transform for clutter removal.
    /// The time series array is modified by the call to Filter()
    /// </summary>
    public class ClutterWaveletFilter {

        // input and output data:
        //private Ipp64fc[][] TSeries;   // external array for complex time series input, TSeries[nrx][npts]
        //private Ipp64fc[] TSeries1;   // external array for complex time series input, TSeries1[npts]
        //private double[][][] ClutterWaveletTransform; // external array for wavelet transform output
        //
        private int _waveletInputNpts;
        private int _waveletOutputNpts;
        private int _waveletClippedNpts;

        private int _wvltClutterGateFirst;
        private int _wvltClutterGateLast;
        private double _wvltClutterCutoffNyq;
        private double _wvltClutterThldMedianRatio;
        private double[][][] _wvltTransform;
        private int _nRx;
        private int _nHts;

        private DaubechiesWavelet DWavelet;

        public int WaveletOutputNpts {
            get { return _waveletOutputNpts; }
            set { _waveletOutputNpts = value; }
        }

        public int WaveletClippedNpts {
            get { return _waveletClippedNpts; }
            set { _waveletClippedNpts = value; }
        }

        /// <summary>
        /// constructor for 2-D time series tseries[nrx][npts]
        /// </summary>
        /// <param name="tseries"></param>
        /// <param name="nrx"></param>
        /// <param name="npts"></param>
        /// <param name="wvltClutterCutoffNyq"></param>
        /// <param name="wvltClutterThldMedianRatio"></param>
        /// <param name="wvltClutterGateFirst"></param>
        /// <param name="wvltClutterGateLast"></param>
        public ClutterWaveletFilter(int nrx, int nhts, int npts,
                            double wvltClutterCutoffNyq, double wvltClutterThldMedianRatio,
                            int wvltClutterGateFirst, int wvltClutterGateLast, double[][][] wvltTransform) {

            //TSeries = tseries;
            //TSeries1 = null;
            _nRx = nrx;
            _nHts = nhts;
            Init(npts, wvltClutterCutoffNyq, wvltClutterThldMedianRatio, wvltClutterGateFirst, wvltClutterGateLast, wvltTransform);
        }

        ///// <summary>
        ///// constructor for 1-D time series tseries[npts]
        ///// </summary>
        ///// <param name="tseries"></param>
        ///// <param name="npts"></param>
        ///// <param name="wvltClutterCutoffNyq"></param>
        ///// <param name="wvltClutterThldMedianRatio"></param>
        ///// <param name="wvltClutterGateFirst"></param>
        ///// <param name="wvltClutterGateLast"></param>
        //private ClutterWaveletFilter(Ipp64fc[] tseries, int npts,
        //                    double wvltClutterCutoffNyq, double wvltClutterThldMedianRatio,
        //                    int wvltClutterGateFirst, int wvltClutterGateLast, double[][][] wvltTransform) {

        //    //TSeries1 = tseries;
        //    //TSeries = null;
        //    _nRx = 1;
        //    Init(npts, wvltClutterCutoffNyq, wvltClutterThldMedianRatio, wvltClutterGateFirst, wvltClutterGateLast, wvltTransform);
        //}

        private void Init(int npts, double wvltClutterCutoffNyq, double wvltClutterThldMedianRatio, int wvltClutterGateFirst, int wvltClutterGateLast, double[][][] wvlt) {
            DWavelet = new DaubechiesWavelet(npts, 20);
            _waveletInputNpts = npts;
            _waveletOutputNpts = DaubechiesWavelet.OutputSizeForInputOfSize(_waveletInputNpts);
            _wvltClutterCutoffNyq = wvltClutterCutoffNyq;
            _wvltClutterThldMedianRatio = wvltClutterThldMedianRatio;
            _wvltClutterGateFirst = wvltClutterGateFirst;
            _wvltClutterGateLast = wvltClutterGateLast;
            _wvltTransform = wvlt;
        }

        /// <summary>
        /// Applies the wavelet filter to all nrx receivers of a single time series from height iht
        /// </summary>
        /// <param name="iht"></param>
        public void RunFilter(Ipp64fc[][] tseries, int iht) {

            //Ipp64fc[] tseries;
            if ((iht >= _wvltClutterGateFirst) && (iht <= _wvltClutterGateLast)) {
                for (int irx = 0; irx < _nRx; irx++) {
                    DWavelet.ClutterFilter(tseries[irx], _wvltClutterThldMedianRatio, _wvltClutterCutoffNyq);
                    if (_waveletOutputNpts == _waveletInputNpts) {
                        DWavelet.CopyInvTransformTo(tseries[irx]);
                    }
                    else {
                        // output array is larger (padded with zeros); only copy first part of it
                        Ipp64fc[] temp = DWavelet.GetCopyOfInvTransform();
                        for (int ipt = 0; ipt < _waveletInputNpts; ipt++) {
                            tseries[irx][ipt] = temp[ipt];
                        }
                    }
                    
                    //if (_wvltTransform == null ||
                    //   _wvltTransform.Length < _nRx ||
                    //   _wvltTransform[0].Length < _nHts ||
                    //   _wvltTransform[0][0].Length < 2*_waveletOutputNpts) 
                    //{
                    //    _wvltTransform = null;
                    //    _wvltTransform = new double[_nRx][][];
                    //    for (int i = 0; i < _nRx; i++) {
                    //        _wvltTransform[i] = new double[_nHts][];
                    //        for (int j = 0; j < _nHts; j++) {
                    //            _wvltTransform[i][j] = new double[2*_waveletOutputNpts];
                    //        }
                    //    }
                    //}

                    _waveletClippedNpts = DWavelet.NumPointsInClippedSegment;

                    // put original wavelet transform followed by clipped transform in output array
                    DWavelet.CopyClippedTransformTo(_wvltTransform[irx][iht]);
                    for (int i = 0; i < _waveletOutputNpts; i++) {
                        _wvltTransform[irx][iht][i + _waveletOutputNpts] = _wvltTransform[irx][iht][i];
                    }
                    DWavelet.CopyTransformTo(_wvltTransform[irx][iht]);
                    
                }

            }
            
        }

        /// <summary>
        /// Static method to compute a wavelet transform of a single time series
        /// </summary>
        /// <param name="tseries"></param>
        /// <param name="irx"></param>
        /// <param name="inNPts"></param>
        /// <param name="outNPts"></param>
        /// <param name="ary"></param>
        static public void UnfilteredWaveletTransform(Ipp64fc[] tseries, int inNPts, out int outNPts, out double[] ary) {
            DaubechiesWavelet DWavelet2 = null;
            DWavelet2 = new DaubechiesWavelet(inNPts, 20);
            DWavelet2.Transform(tseries);
            outNPts = DWavelet2.SizeOfTransformArrays;
            ary = new double[outNPts];
            DWavelet2.CopyTransformTo(ary);
        }

        /// <summary>
        /// Static method to compute a clipped wavelet transform of a single time series
        /// </summary>
        /// <param name="tseries"></param>
        /// <param name="thresholdRatio"></param>
        /// <param name="cutoffVelNyq"></param>
        /// <param name="inNPts"></param>
        /// <param name="outNPts"></param>
        /// <param name="ary"></param>
        static public void FilteredWaveletTransform(Ipp64fc[] tseries,  double thresholdRatio, double cutoffVelNyq, int inNPts, out int outNPts, out double[] ary) {
            DaubechiesWavelet DWavelet2 = null;
            DWavelet2 = new DaubechiesWavelet(inNPts, 20);
            DWavelet2.Transform(tseries);
            DWavelet2.Clip(thresholdRatio, cutoffVelNyq);
            outNPts = DWavelet2.SizeOfTransformArrays;
            ary = new double[outNPts];
            DWavelet2.CopyClippedTransformTo(ary);
        }


    }

    /////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// HSFilter
    /// Applies a filter to the real and imaginary time series by
    ///     transforming to frequency domain,
    ///     keeping keepPts around DC,
    ///     then with the remaining pts
    ///     using H&S method on the square of the time series to identify signals,
    ///     removing signals,
    ///     and transforming back to time series.
    /// </summary>
    public class HSLowPassFilter {

        private IntelMath HSFilterTransform = null;
        //private Ipp64fc[][] TSeries;
        private int _nPts;
        private int _nRx;
        private int _keepPts;
        Ipp64fc[] _workingArray;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tseries"></param>
        /// <param name="npts"></param>
        /// <param name="keepPts"></param>
        public HSLowPassFilter(int nrx, int npts, int keepPts) {

            //TSeries = tseries;
            _nPts = npts;
            _nRx = nrx;
            _keepPts = keepPts;

            _workingArray = new Ipp64fc[_nPts];

            HSFilterTransform = new IntelMath();

        }

        public void RunFilter(Ipp64fc[][] tseries, int iht) {

            double noise;
            double stdev;
            double maxNoise;
            int numNoise;

            int lim1 = _keepPts / 2;
            int lim2 = _nPts - _keepPts / 2;
            int nspts = lim2 - lim1;
            double[] powArray = new double[nspts];

            for (int irx = 0; irx < _nRx; irx++) {
                if (HSFilterTransform == null) {
                    HSFilterTransform = new IntelMath();
                }
                if (IntelMath.IsPowerOf2(_nPts)) {
                    HSFilterTransform.FFT(tseries[irx], _workingArray, _nPts);
                }
                else {
                    HSFilterTransform.DFT(tseries[irx], _workingArray, _nPts);
                }

                try {
                    // trying to find weird failure
                    for (int i = lim1, j = 0; i < lim2; i++, j++) {
                        powArray[j] = _workingArray[i].re * _workingArray[i].re +
                                        _workingArray[i].im * _workingArray[i].im;
                    }

                    Moments.GetNoise(powArray, nspts, 1, 0, out noise, out stdev, out maxNoise, out numNoise);
                }
                catch (Exception ee) {
                    string msg = "GetNoise failed in HSLowPassFilter.RunFilter: " + ee.Message + " \n";
                    //throw new Exception("GetNoise failed in FilterTimeSeriesHS: " + ee.Message);
                    msg += ("powArray[" + powArray.Length.ToString() + "], nspts = " + nspts.ToString() + " \n");
                    msg += ("lim1, lim2 = " + lim1.ToString() + ", " + lim2.ToString());
                    throw new Exception(msg);
                }

                for (int i = lim1, j = 0; i < lim2; i++, j++) {
                    if (powArray[j] > maxNoise) {
                        _workingArray[i].re = 0.0;
                        _workingArray[i].im = 0.0;
                    }
                }

                /*
                // for imag array
                for (int i = lim1, j = 0; i < lim2; i++, j++) {
                    powArray[j] = workingArray[i].im * workingArray[i].im;
                }

                Moments.GetNoise(powArray, nspts, 1, 0, out noise, out stdev, out maxNoise, out numNoise);

                for (int i = lim1, j = 0; i < lim2; i++, j++) {
                    if (powArray[j] > maxNoise) {
                        workingArray[i].im = 0.0;
                    }
                }
                */

                if (IntelMath.IsPowerOf2(_nPts)) {
                    HSFilterTransform.InvFFT(_workingArray, tseries[irx], _nPts);
                }
                else {
                    HSFilterTransform.InvDFT(_workingArray, tseries[irx], _nPts);
                }
                
            }
        }

    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
    // Note: These commented out routines are old filter functions no longer used.
    //  Kept here for future reference

    /*
        private IntelMath DiscreteTransforms = null;
        /// <summary>
        /// Applies a Tukey (tapered cosine) window in the frequeny domain.
        /// alpha specifies the degree of taper
        ///     0 = rectangular window
        ///     1 = Hann window
        ///     x => x/2 = fraction of total pts tapered at each end.
        /// The total width of the window is selected so that keepPts is the width
        ///     between half-power points
        /// </summary>
        /// <param name="tseries"></param>
        /// <param name="workingArray"></param>
        /// <param name="size"></param>
        /// <param name="keepPts"></param>
        private void FilterTimeSeriesTukey(Ipp64fc[] tseries, Ipp64fc[] workingArray, int size, int keepPts) {

            double alpha = 0.5;
            int winPts = (int)(keepPts / (1.0 - alpha / 2.0));
            int taperPts = (int)(alpha * winPts / 2.0);    // number of tapered pts at each end
            int topPts = winPts / 2 - taperPts;   // half of the number of untapered (flat) pts

            if (DiscreteTransforms == null) {
                DiscreteTransforms = new IntelMath();
            }

            DiscreteTransforms.DFT(tseries, workingArray, size);


            double factor = 1.0;
            // workingArray[0] is the DC point.
            // Apply the filter identically to the far left and far right ends of working array
            for (int i = 0; i < size / 2; i++) {
                if (i > winPts / 2) {
                    // pts outside of the window width are zeroed
                    workingArray[i].re = 0.0;
                    workingArray[i].im = 0.0;
                    workingArray[size - i - 1].re = 0.0;
                    workingArray[size - i - 1].im = 0.0;
                }
                else if (i > topPts) {
                    // points beyond the inner, flat top are tapered
                    int j = winPts / 2 - i;
                    factor = 0.5 * (1.0 + Math.Cos(Math.PI * (2.0 * j / (alpha * (winPts - 1)) - 1.0)));
                    // because this is in amplitude and not power:
                    factor = Math.Sqrt(factor);
                    workingArray[i].re *= factor;
                    workingArray[i].im *= factor;
                    workingArray[size - i - 1].re *= factor;
                    workingArray[size - i - 1].im *= factor;
                }

            }

            DiscreteTransforms.InvDFT(workingArray, tseries, size);
        }

        private void FilterTimeSeriesGauss(Ipp64fc[] tseries, Ipp64fc[] workingArray, int size, int keepPts) {

            if (DiscreteTransforms == null) {
                DiscreteTransforms = new IntelMath();
            }

            int sigma = keepPts / 2;
            int lim2 = size - keepPts / 2;
            double factor = 1.0;
            DiscreteTransforms.DFT(tseries, workingArray, size);
     * 
            // Debug: to help look at shape of filter in Doppler spectra
            //Random rr = new Random(739226);
            //for (int i = 0; i < size; i++) {
            //    workingArray[i].re = rr.NextDouble();
            //    workingArray[i].im = rr.NextDouble();
            //}
     * 
            for (int i = 0; i < size / 2; i++) {
                if (i > 3*sigma) {
                    workingArray[i].re = 0.0;
                    workingArray[i].im = 0.0;
                    workingArray[size-i-1].re = 0.0;
                    workingArray[size-i-1].im = 0.0;
                }
                else {
                    //factor = Math.Exp(-((double)i * i) / (2.0 * (double)sigma * (double)sigma));
                    // remove a power of 2 because this is amplitude not power; want power response to be gaussian
                    factor = Math.Exp(-((double)i * i) / (4.0 * (double)sigma * (double)sigma));
                    if (i == 205) {
                        //int x = 0;
                    }
                    workingArray[i].re *= factor;
                    workingArray[i].im *= factor;
                    workingArray[size - i - 1].re *= factor;
                    workingArray[size - i - 1].im *= factor;
                }
            }
            DiscreteTransforms.InvDFT(workingArray, tseries, size);
        }

        private void FilterTimeSeriesRect(Ipp64fc[] tseries, Ipp64fc[] workingArray, int size, int keepPts) {

            if (DiscreteTransforms == null) {
                DiscreteTransforms = new IntelMath();
            }

            int lim1 = keepPts / 2;
            int lim2 = size - keepPts / 2;
            DiscreteTransforms.DFT(tseries, workingArray, size);
            for (int i = lim1; i < lim2; i++) {
                workingArray[i].re = 0.0;
                workingArray[i].im = 0.0;
            }
          
            DiscreteTransforms.InvDFT(workingArray, tseries, size);
        }

    */

}
