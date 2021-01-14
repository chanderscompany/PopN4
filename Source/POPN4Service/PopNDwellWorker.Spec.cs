using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MathNet.Numerics;
using ipp;

using DACarter.PopUtilities;
using DACarter.Utilities.Maths;
using POPCommunication;

using POPN;

//
//////////////////////////
//
// PopNDwellWorker.Spec
//
// Classes and methods that relate to Doppler spectra and cross-correlations are in this file, including
//  Calculating spectral moments;
//  Calculating SA method winds from xcorrelations;
//
//////////////////////////


namespace POPN4Service {

    partial class PopNDwellWorker {

        private GaussFit _gaussFit;
        private PolyFit _lsqPoly;
        private int _polyFitOrder;
        private bool _xcorrAdjustBase;
        private int _xcorrLagsToFit;
        private int _xcorrLagsToInterp;
        private int _polyFitPts;
        private int[][] _xcorrPeakI;
        private double[][] _xcBase, _acBase;

        /// <summary>
        /// Process spectra and compute moments
        /// </summary>
        /// <returns></returns>
        private PopCommands ProcessSpectra() {

            PopCommands command;

            if (_endOfData) {
                // TODO modify if doing consensus avg
                command = CheckCommand();
                return command;
            }


            if ((!_parameters.ReplayPar.Enabled ||
                _parameters.ReplayPar.ProcessRawSamples ||
                _parameters.ReplayPar.ProcessTimeSeries ||
                _parameters.ReplayPar.ProcessSpectra) &&
                _dataPackage.Spectra != null) {

                // Apply filter (gain) factor to spectra
                //
                double filterFactor;
                for (int irx = 0; irx < _nRx; irx++) {
                    for (int iht = 0; iht < _nHts; iht++) {
                        if (_noHardware) {
                            filterFactor = 1.0;
                            filterFactor = _filterFactors[iht];
                        }
                        else {
                            if (_parameters.SystemPar.RadarPar.FmCwParSet[0].ApplyFilterCorrection) {
                                filterFactor = _filterFactors[iht];
                            }
                            else {
                                filterFactor = 1.0;
                            }
                        }

                        int mPts = _dataPackage.Spectra[0][0].Length;
                        for (int j = 0; j < mPts; j++) {
                            _dataPackage.Spectra[irx][iht][j] = _dataPackage.Spectra[irx][iht][j] / filterFactor;
                        }
                    }
                }

                if ((_dataPackage != null) && (_dataPackage.Spectra != null)) {
                    CalculateMoments(_dataPackage.Spectra);
                }

                /*
                // TODO: add consensus processing
                // TODO: cns set for 1 rx only (and only works in replay mode)
                if (_parameters.SystemPar.RadarPar.ProcPar.CnsPar[0].CnsEnable) {
                    _cnsDwell.Azimuth = _dataPackage.Parameters.SystemPar.RadarPar.BeamDirections[0].Azimuth;
                    _cnsDwell.Elevation = _dataPackage.Parameters.SystemPar.RadarPar.BeamDirections[0].Elevation;
                    _cnsDwell.IppMicroSec = _dataPackage.Parameters.SystemPar.RadarPar.BeamParSet[0].IppMicroSec;
                    _cnsDwell.NCode = _dataPackage.Parameters.SystemPar.RadarPar.BeamParSet[0].NCode;
                    _cnsDwell.PulseWidthNs = _dataPackage.Parameters.SystemPar.RadarPar.BeamParSet[0].PulseWidthNs;
                    _cnsDwell.TimeStamp = _dataPackage.RecordTimeStamp;
                    _cnsDwell.NyquistMS = _dataPackage.Parameters.GetBeamParNyquist(0);
                    _cnsDwell.Heights = _dataPackage.Parameters.GetBeamParHeightsM(0, _cnsDwell.Elevation);
                    //_cnsDwell.RadialDoppler = _dataPackage.MeanDoppler[0];  // _dataPackage array could be larger than nhts, so:
                    int nhts = _cnsDwell.Heights.Length;
                    _cnsDwell.RadialDoppler = new double[nhts];
                    Array.Copy(_dataPackage.MeanDoppler[0], _cnsDwell.RadialDoppler, nhts);
                    _cnsDwell.CnsOutputFolder = _dataPackage.Parameters.SystemPar.RadarPar.ProcPar.CnsPar[0].CnsFilePath;
                    //sampledDataPackage.Parameters.SystemPar.RadarPar.BeamParSet.n
                    _consensus.Add(_cnsDwell);
                }
                 * */

            }

            // reset previous status:
            Communicator.UpdateStatus(new PopStatusMessage(_status));
            command = CheckCommand();
            return command;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="dp"></param>
        private void CalculateMoments(double[][][] spectra) {
            //
            // Moments
            //
            if (spectra == null ||
                spectra[0] == null ||
                spectra[0][0] == null) {
                // note: won't be any spectra if replaying moments file,
                //	so use moments as read into dp
                return;
            }
            if (_parameters.ReplayPar.Enabled &&
                    !_parameters.ReplayPar.ProcessSpectra &&
                    !_parameters.ReplayPar.ProcessRawSamples &&
                    !_parameters.ReplayPar.ProcessTimeSeries) {
                // not recomputing moments so leave original moments
                return;
            }

            // if DC filtering, then skip (interpolate) DC when calculating moments:
            bool skipDC = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerDcFilter;
            bool isWindow = (_parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerWindow != PopParameters.WindowType.Rectangular);
            bool originalWindow = _parameters.SystemPar.RadarPar.ProcPar.IsWindowing;  // should have been set equal to above in GetReplayData()
            // POPREV dac Starting with rev 3.2, always interpolate across DC
            //skipDC = true;
            // POPREV: dac: Starting with rev 3.12.6, go back to allowing peak at DC if not DC filtering (pej)
            // POPREV: 3.15 In noise calc, skip dcpts if DCfiltering, skip 3 pts if also windowing
            int numSkipDC = 0;
            if (skipDC) {
                if (isWindow) {
                    numSkipDC = 3;
                }
                else {
                    numSkipDC = 1;
                }
            }

            // Identify GC regions here
            // Ground clutter removal
            // identify ground clutter in spectra and save info in PopDataPackage.ClutterPoints[]
            if (_dataPackage.Parameters.SystemPar.RadarPar.ProcPar.RemoveClutter) {
                GroundClutter.Identify(_dataPackage);
            }

            int nHts = _parameters.SystemPar.RadarPar.BeamParSet[0].NHts;  // this is # gates written to file
            if (!_parameters.ReplayPar.Enabled) {
                nHts = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts / 2 + 1;  // this is # gates calculated
            }
            //int iHtFirst = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateFirst;
            //int iHtLast = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateLast;
            int nSpec = _parameters.SystemPar.RadarPar.BeamParSet[0].NSpec;
            //int nPts = spectra[0][0].Length;
            int nPts = _parameters.SystemPar.RadarPar.BeamParSet[0].NPts;
            double sysDelay = _parameters.SystemPar.RadarPar.RxBw[0].BwDelayNs * PopParameters.MperNs;
            double gate0m = _parameters.SystemPar.RadarPar.BeamParSet[0].SampleDelayNs * PopParameters.MperNs - sysDelay;
            double gateSpacing = _parameters.SystemPar.RadarPar.BeamParSet[0].SpacingNs * PopParameters.MperNs;
            double hzms = (149.896 / _parameters.SystemPar.RadarPar.TxFreqMHz);
            double ipp = _parameters.SystemPar.RadarPar.BeamParSet[0].IppMicroSec;
            int nci = _parameters.SystemPar.RadarPar.BeamParSet[0].NCI;
            double nyquist = (float)(0.5e6 * hzms / ipp / (double)nci);    // nyquist freq in m/s 
            for (int irx = 0; irx < _nRx; irx++) {
                for (int iht = 0; iht < nHts; iht++) {
                    try {
                        double noise, stdev, maxNoise;
                        int numNoise;

                        Moments.GetNoise(spectra[irx][iht], _nPts, nSpec, numSkipDC, out noise, out stdev, out maxNoise, out numNoise);
                        _dataPackage.Noise[irx][iht] = noise;

                        double power = 0.0;
                        double meanDop = 0.0;
                        double width = 0.0;
                        int sigPts;
                        //bool momentsAreDone = false;

                        double[] spec;
                        if ((_parameters.ExcludeMomentIntervals.Enabled) ||
                            (_parameters.SystemPar.RadarPar.ProcPar.RemoveClutter &&
                             _parameters.SystemPar.RadarPar.ProcPar.KeepOriginalSpectra)) {
                            // in these cases we will modify spectra for moments calc,
                            //  but then want to return to original spectra;
                            //  so we save a copy of the original.
                            //  spec[] is the copy that we modify for moments calc.
                            spec = (double[])spectra[irx][iht].Clone();
                        }
                        else {
                            // otherwise, just give spectrum an alias name
                            spec = spectra[irx][iht];
                        }

                        if (_parameters.SystemPar.RadarPar.ProcPar.RemoveClutter) {
                            GroundClutter.Remove(spec, _dataPackage.ClutterPoints[irx][iht], _parameters);
                        }

                        if (_parameters.ExcludeMomentIntervals.Enabled) {
                            // NOTE: this is special processing done in POPN2 for ship
                            //  It is based on a special parx file section that specifies spectral regions to exclude from moment calc
                            //  It is not the same as restricting search for signal peak (below)
                            if (_parameters.ExcludeMomentIntervals.AllModesExcludeIntervals != null) {
                                int gateOffset = 0;
                                if ((_parameters.SystemPar.RadarPar.RadarType & PopParameters.TypeOfRadar.FmCw) != PopParameters.TypeOfRadar.Unknown) {
                                    gateOffset = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateFirst;
                                }
                                int nModes = _parameters.ExcludeMomentIntervals.AllModesExcludeIntervals.Length;
                                for (int i = 0; i < nModes; i++) {
                                    double az0 = _parameters.ExcludeMomentIntervals.AllModesExcludeIntervals[i].Mode.Azimuth;
                                    double az1 = _parameters.SystemPar.RadarPar.BeamDirections[0].Azimuth;
                                    double elev0 = _parameters.ExcludeMomentIntervals.AllModesExcludeIntervals[i].Mode.Elevation;
                                    double elev1 = _parameters.SystemPar.RadarPar.BeamDirections[0].Elevation;
                                    int ncode0 = _parameters.ExcludeMomentIntervals.AllModesExcludeIntervals[i].Mode.NCode;
                                    int ncode1 = _parameters.SystemPar.RadarPar.BeamParSet[0].NCode;
                                    int pw0 = _parameters.ExcludeMomentIntervals.AllModesExcludeIntervals[i].Mode.PulseWidthNs;
                                    int pw1 = _parameters.SystemPar.RadarPar.BeamParSet[0].PulseWidthNs;
                                    if (((az0 == az1) || (Double.IsNaN(az0)) || (az0 < 0.0)) &&
                                        ((ncode0 == ncode1) || (ncode0 < 0)) &&
                                        ((elev0 == elev1) || (Double.IsNaN(elev0)) || (elev0 < 0.0)) &&
                                        ((pw0 == pw1) || (pw0 < 0))) {
                                        //spec = (double[])spectra[irx][iht].Clone();
                                        int intervals = _parameters.ExcludeMomentIntervals.AllModesExcludeIntervals[i].MomentExcludeIntervals.Length;
                                        for (int j = 0; j < intervals; j++) {
                                            double vel1 = _parameters.ExcludeMomentIntervals.AllModesExcludeIntervals[i].MomentExcludeIntervals[j].VelLowMS;
                                            double vel2 = _parameters.ExcludeMomentIntervals.AllModesExcludeIntervals[i].MomentExcludeIntervals[j].VelHighMS;
                                            int ivel1 = (int)Math.Floor((vel1 + nyquist) / (2.0 * nyquist / nPts) + 0.5);
                                            int ivel2 = (int)Math.Floor((vel2 + nyquist) / (2.0 * nyquist / nPts) + 0.5);
                                            if (ivel1 < 0) { ivel1 = 0; }
                                            if (ivel2 < 0) { ivel2 = 0; }
                                            if (ivel1 >= nPts) { ivel1 = nPts - 1; }
                                            if (ivel2 >= nPts) { ivel2 = nPts - 1; }
                                            double ht1 = _parameters.ExcludeMomentIntervals.AllModesExcludeIntervals[i].MomentExcludeIntervals[j].HtLowM;
                                            double ht2 = _parameters.ExcludeMomentIntervals.AllModesExcludeIntervals[i].MomentExcludeIntervals[j].HtHighM;
                                            int iht1 = (int)((ht1 - gate0m) / gateSpacing + 0.999) + gateOffset;
                                            int iht2 = (int)((ht2 - gate0m) / gateSpacing + 0.0) + gateOffset;
                                            if (iht1 < 0) { iht1 = 0; }
                                            if (iht2 < 0) { iht2 = 0; }
                                            if (iht1 >= nHts) { iht1 = nHts - 1; }
                                            if (iht2 >= nHts) { iht2 = nHts - 1; }
                                            if ((iht >= iht1) && (iht <= iht2)) {
                                                for (int ipt = ivel1; ipt <= ivel2; ipt++) {
                                                    spec[ipt] = noise;
                                                }
                                            }
                                        }
                                        //Moments.GetMoments(spec, noise, 0.0, skipDC, out power, out meanDop, out width, out sigPts);
                                        //momentsAreDone = true;
                                        //break;
                                    }
                                }
                            }
                        }  // end special exclude regions from moments


                        if (!_parameters.SignalPeakSearchRange.Enabled) {
                            Moments.GetMoments(spec, _nPts, noise, 0.0, skipDC, out power, out meanDop, out width, out sigPts);
                        }
                        else {
                            int npts = _parameters.SystemPar.RadarPar.BeamParSet[0].NPts;
                            double nyq = _parameters.GetBeamParNyquist(0);
                            // convert Doppler limits for signal search from m/s to FFT  points (relative to dc (at index = npts/2)).
                            //  i.e. limitPt = 0 is dc;
                            double dLowLimitPt = npts * (_parameters.SignalPeakSearchRange.VelLowMS) / (2.0 * nyq);
                            double dHighLimitPt = npts * (_parameters.SignalPeakSearchRange.VelHighMS) / (2.0 * nyq);
                            int lowLimitPt = (int)Math.Ceiling(dLowLimitPt);
                            int highLimitPt = (int)Math.Floor(dHighLimitPt);
                            /*
                            if (lowLimitPt < 0) {
                                lowLimitPt += npts;
                            }
                            if (lowLimitPt >= npts) {
                                lowLimitPt -= npts;
                            }
                            if (highLimitPt < 0) {
                                highLimitPt += npts;
                            }
                            if (highLimitPt >= npts) {
                                highLimitPt -= npts;
                            }
                            */

                            Moments.GetMoments(spec, _nPts, noise, 0.0, skipDC, lowLimitPt, highLimitPt, out power, out meanDop, out width, out sigPts);
                        }

                        _dataPackage.Power[irx][iht] = power;
                        _dataPackage.MeanDoppler[irx][iht] = meanDop;
                        _dataPackage.Width[irx][iht] = width;
                    }
                    catch (Exception e) {
                        string ss = e.Message;
                    }
                }  // end for ihts
            } // end for irx

            // do cross-correlation "moments" here:

            int nTotalLags = 2 * _maxLag + 1;
            int lineFitPts = 0;

            if (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCwSA ||
                _parameters.SystemPar.RadarPar.ProcPar.DoAutoCorr1Rx) {
                // interpolate auto-corr here:
                int nipts = _xcorrLagsToInterp / 2;         // pts on each side to replace with interpolated value
                int nsets = _dataPackage.XCorrMag.Length;
                double interpVal;
                for (int iht = 0; iht < _nHts; iht++) {
                    for (int irx = 0; irx < nsets; irx++) {
                        // auto- and cross-correlation interp about zero
                        interpVal = (_dataPackage.XCorrMag[irx][iht][_maxLag - nipts - 1] +
                                            _dataPackage.XCorrMag[irx][iht][_maxLag + nipts + 1]) / 2.0;
                        for (int k = 0; k < nipts; k++) {
                            _dataPackage.XCorrMag[irx][iht][_maxLag + k + 1] = _dataPackage.XCorrMag[irx][iht][_maxLag - k - 1] = interpVal;
                        }
                        if (_xcorrLagsToInterp > 0) {
                            _dataPackage.XCorrMag[irx][iht][_maxLag] = interpVal;
                        }
                    }
                }

            }

            if (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCwSA) {

                if (_nRx != 3) {
                    throw new ApplicationException("Must have 3 receivers for Spaced Antenna method.");
                }


                ///////////  gaussian fit to xcorr magnitude ////////////

                double[] x, sig;
                double[] x1a, x1x;  // x values for fitted polyno,ials
                x = new double[1];
                x1a = new double[1];
                x1x = new double[1];
                sig = new double[1];
                double[] guess = new double[4];
                double oneLag = _parameters.SystemPar.RadarPar.BeamParSet[0].IppMicroSec * 1.0e-6;
                _polyFitPts = _dataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrLagsToCurveFit;
                _xcorrPeakI = new Int32[_nRx][];
                _xcBase = new Double[_nRx][];
                _acBase = new Double[_nRx][];
                for (int i = 0; i < _nRx; i++) {
                    _xcorrPeakI[i] = new Int32[_nHts];
                    _xcBase[i] = new double[_nHts];
                    _acBase[i] = new double[_nHts];
                }


                if (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCwSA) {
                    if (_dataPackage.XCorrMag != null && _dataPackage.XCorrMag.Length >= 3) {
                        if (_gaussFit == null) {
                            _gaussFit = new GaussFit(4);
                        }
                        if ((_lsqPoly == null) || (_lsqPoly.Order != _polyFitOrder)) {
                            _lsqPoly = null;
                            _lsqPoly = new PolyFit(_polyFitOrder);
                            //_polyCoeffs = new double[_polyFitOrder + 1];
                        }
                        //
                        int npts = _dataPackage.XCorrMag[0][0].Length;
                        x = new double[npts];
                        x1a = new double[_polyFitPts];
                        x1x = new double[_polyFitPts];
                        sig = new double[npts];
                        for (int i = 0; i < npts; i++) {
                            x[i] = (i - npts / 2) * oneLag;
                            sig[i] = 0.1;
                            //y[i] = crossCorrMag[i];
                        }
                        for (int i = 0; i < _polyFitPts; i++) {
                            x1a[i] = (i - _polyFitPts / 2) * oneLag;
                        }

                    }

                }
                ///////////  end gaussian fit ////////////////

                lineFitPts = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrLineFitPts;

                if (XCorrelations == null) {
                    XCorrelations = new IntelMath();
                }

                int gateFirst = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateFirst;
                int gateLast = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateLast;
                double m, b;

                double[][][] XCorrMag = _dataPackage.XCorrMag;
                double[][][] XCorrRatio = _dataPackage.XCorrRatio;
                LineFit[][] XCorrRatioLine = _dataPackage.XCorrRatioLine;
                double slope;
                double[] xcMax = new double[2];
                double[] acMax = new double[2];  // x,y value of max
                double xcScale, acScale;
                double xFirstLag = x[0];
                double xLastLag = x[nTotalLags - 1];
                double taup;    // peak lag of cross-corr
                double taui;    // lag of intersection of cross- and auto-corr
                double taux;    // lag where auto-corr(taux) equals cross-corr(0)

                for (int iht = 0; iht < nHts; iht++) {

                    if (iht >= gateFirst && iht <= gateLast) {
                        // only do calculations for requested gates

                        /// Gaussian fit ////
                        for (int irx = 0; irx < _nRx; irx++) {
                            _gaussFit.MakeAGuess(x, _dataPackage.XCorrMag[irx][iht], guess, 0.1);
                            try {
                                int sz4 = iht;
                                try {
                                    _gaussFit.Fit(x, _dataPackage.XCorrMag[irx][iht], sig, guess);

                                }
                                catch (Exception ee) {
                                    string msg = "GaussFit error at (iht, irx) = " + iht.ToString() + " " + irx.ToString();
                                    //throw;
                                }
                                for (int i = 0; i < 4; i++) {
                                    _dataPackage.XCorrGaussCoeffs[irx][iht][i] = _gaussFit.Coeffs[i];
                                }
                                slope = _gaussFit.SlopeAtZero;
                                _dataPackage.XCorrSlope0[irx][iht][0] = slope;
                                double xCorr0 = _gaussFit.Coeffs[0] * Math.Exp(-_gaussFit.Coeffs[1] * _gaussFit.Coeffs[1] / _gaussFit.Coeffs[2] / _gaussFit.Coeffs[2]) + _gaussFit.Coeffs[3];
                                double ah = _parameters.SystemPar.RadarPar.ASubH;
                                double dx = _parameters.SystemPar.RadarPar.AntSpacingM;
                                _dataPackage.XCorrSlope0[irx][iht][1] = slope / xCorr0 / ah / ah / dx;

                                // do polynomial fits

                                int nCorrPts = _dataPackage.XCorrMag[0][0].Length;
                                // CrossCorr
                                _lsqPoly.SetData(nCorrPts, x, _dataPackage.XCorrMag[irx][iht]);
                                _lsqPoly.GetCoeffs(_dataPackage.XCorrPolyCoeffs[irx][iht]);
                                xcMax = _lsqPoly.FindMax2();
                                if (xcMax == null) {
                                    xcMax = MaxOverInterval(_lsqPoly, x, nCorrPts);
                                }

                                // now that we know where the cross-correlation is,
                                //  interpolate and fit another polynomial to central pts:

                                // interpolate cross_corr:
                                //  From before, nipts is # pts to interpolate on each side of peak.
                                //  Find which array index is at peak.
                                double diff;
                                double minDiff = double.MaxValue;
                                _xcorrPeakI[irx][iht] = 0;
                                for (int i = 0; i < nTotalLags; i++) {
                                    diff = Math.Abs(x[i] - xcMax[0]);
                                    if (diff < minDiff) {
                                        minDiff = diff;
                                        _xcorrPeakI[irx][iht] = i;
                                    }
                                }

                                //// if peak close to edge, move away from edge so we can interpolate the same way
                                //if (_xcorrPeakI[irx][iht] < (nipts + 1)) {
                                //    _xcorrPeakI[irx][iht] = nipts + 1;
                                //}
                                //if (_xcorrPeakI[irx][iht] > ((nTotalLags - 1) - (nipts + 1))) {
                                //    _xcorrPeakI[irx][iht] = (nTotalLags - 1) - (nipts + 1);
                                //}

                                //interpVal = (_dataPackage.XCorrMag[irx][iht][_xcorrPeakI[irx][iht] - nipts - 1] +
                                //                    _dataPackage.XCorrMag[irx][iht][_xcorrPeakI[irx][iht] + nipts + 1]) / 2.0;
                                //for (int k = 0; k < nipts; k++) {
                                //    _dataPackage.XCorrMag[irx][iht][_xcorrPeakI[irx][iht] + k + 1] = _dataPackage.XCorrMag[irx][iht][_xcorrPeakI[irx][iht] - k - 1] = interpVal;
                                //}
                                //if (_xcorrLagsToInterp > 0) {
                                //    _dataPackage.XCorrMag[irx][iht][_xcorrPeakI[irx][iht]] = interpVal;
                                //}

                                // if peak close to edge, move away from edge so we can fit polynomial in same way for all
                                if (_xcorrPeakI[irx][iht] < (_polyFitPts / 2)) {
                                    _xcorrPeakI[irx][iht] = _polyFitPts / 2;
                                }
                                if (_xcorrPeakI[irx][iht] > ((nTotalLags - 1) - (_polyFitPts / 2))) {
                                    _xcorrPeakI[irx][iht] = (nTotalLags - 1) - (_polyFitPts / 2);
                                }

                                // x and y values for fitted polynomial
                                double[] yf = new double[_polyFitPts];
                                for (int i = 0; i < _polyFitPts; i++) {
                                    x1x[i] = x1a[i] + (_xcorrPeakI[irx][iht] - _maxLag) * oneLag;
                                    yf[i] = _dataPackage.XCorrMag[irx][iht][(i + _xcorrPeakI[irx][iht] - _polyFitPts / 2)];
                                }

                                _lsqPoly.SetData(_polyFitPts, x1x, yf);
                                _lsqPoly.GetCoeffs(_dataPackage.XCorrPolyCoeffs[irx][iht]);
                                xcMax = _lsqPoly.FindMax2();
                                if (xcMax == null) {
                                    xcMax = MaxOverInterval(_lsqPoly, x, nCorrPts);
                                }

                                // NOT: use HS method to determine base level
                                // YES: smooth the xcorr and look for lowest pt for base level
                                double[] yn = new double[nCorrPts];
                                for (int ipt = 0; ipt < nCorrPts; ipt++) {
                                    yn[ipt] = _dataPackage.XCorrMag[irx][iht][ipt];
                                }
                                if (_parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrAdjustBase) {
                                    // TODO: NOTE: nspec arbitrarily increased because xcorr noise is smaller than expected
                                    //DACarter.Utilities.Maths.HSMethod.Noise(yn, (_nPts * _nSpec) / 200, out _xcBase[irx][iht], out stdev, out numNoise);
                                    _xcBase[irx][iht] = FindBaseLevel(_dataPackage.XCorrMag[irx][iht], nCorrPts);
                                }
                                else {
                                    _xcBase[irx][iht] = 0.0;  // try without offsetting
                                }

                                //xcScale = 1.0 / (xcMax[1] - xcBase);
                                xcScale = 1.0;

                                // AutoCorr
                                for (int i = 0; i < _polyFitPts; i++) {
                                    yf[i] = _dataPackage.XCorrMag[irx + 3][iht][(i + _maxLag - _polyFitPts / 2)];
                                }

                                _lsqPoly.SetData(_polyFitPts, x1a, yf);
                                _lsqPoly.GetCoeffs(_dataPackage.XCorrPolyCoeffs[irx + 3][iht]);

                                //_lsqPoly.SetData(nCorrPts, x, _dataPackage.XCorrMag[irx + 3][iht]);
                                //_lsqPoly.GetCoeffs(_dataPackage.XCorrPolyCoeffs[irx+3][iht]);
                                acMax = _lsqPoly.FindMax2();
                                if (acMax == null) {
                                    acMax = MaxOverInterval(_lsqPoly, x, nCorrPts);
                                }


                                // NO: use HS method to determine base level
                                if (_parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrAdjustBase) {
                                    // NOTE: nspec arbitrarily increased because xcorr noise is smaller than expected
                                    //DACarter.Utilities.Maths.HSMethod.Noise(_dataPackage.XCorrMag[irx + 3][iht], (_nPts * _nSpec) / 200, out _acBase[irx][iht], out stdev, out numNoise);
                                    _acBase[irx][iht] = FindBaseLevel(_dataPackage.XCorrMag[irx + 3][iht], nCorrPts);
                                }
                                else {
                                    _acBase[irx][iht] = 0.0;  // try without offsetting
                                }

                                acScale = 1.0;                  // normalize to 1.0
                                //acScale = (xcMax[1] - xcBase) / (acMax[1] - acBase);  // normalize to xcorr peak
                                // normalize correlation functions
                                if (acScale > 0.0 && xcScale > 0.0) {
                                    _dataPackage.XCorrPolyCoeffs[irx][iht][0] = _dataPackage.XCorrPolyCoeffs[irx][iht][0] - _xcBase[irx][iht];
                                    _dataPackage.XCorrPolyCoeffs[irx + 3][iht][0] = _dataPackage.XCorrPolyCoeffs[irx + 3][iht][0] - _acBase[irx][iht];
                                    for (int jj = 0; jj < _polyFitOrder + 1; jj++) {
                                        _dataPackage.XCorrPolyCoeffs[irx][iht][jj] = xcScale * _dataPackage.XCorrPolyCoeffs[irx][iht][jj];
                                        _dataPackage.XCorrPolyCoeffs[irx + 3][iht][jj] = acScale * _dataPackage.XCorrPolyCoeffs[irx + 3][iht][jj];
                                    }
                                    for (int jj = 0; jj < nCorrPts; jj++) {
                                        //_dataPackage.XCorrMag[irx][iht][jj] = xcScale * (_dataPackage.XCorrMag[irx][iht][jj] - xcBase);
                                        //_dataPackage.XCorrMag[irx + 3][iht][jj] = acScale * (_dataPackage.XCorrMag[irx + 3][iht][jj] - acBase);
                                    }
                                    // find intersection 
                                    Polynomial xcPoly = new Polynomial(_dataPackage.XCorrPolyCoeffs[irx][iht]);
                                    Polynomial acPoly = new Polynomial(_dataPackage.XCorrPolyCoeffs[irx + 3][iht]);
                                    int order = xcPoly.Order;
                                    double stopDelta = oneLag / 10.0;
                                    double x0 = acMax[0];
                                    double x1 = xcMax[0];
                                    //double xdelta = (x1 - x0) / 4.0;
                                    double xdelta = oneLag * Math.Sign(x1);
                                    int ysign0, ysign1;
                                    ysign0 = Math.Sign(xcPoly.Evaluate(x0) - acPoly.Evaluate(x0));
                                    int numIter = 0;
                                    while (Math.Abs(xdelta) > stopDelta) {
                                        numIter++;
                                        x1 = x0 + xdelta;
                                        ysign1 = Math.Sign(xcPoly.Evaluate(x1) - acPoly.Evaluate(x1));
                                        if (ysign1 == 0) {
                                            break;
                                        }
                                        if (ysign1 == ysign0) {
                                            x0 = x1;
                                        }
                                        else {
                                            xdelta = xdelta / 2.0;
                                        }
                                        if (numIter > 1000) {
                                            break;
                                        }
                                    }
                                    taui = x1;
                                    _dataPackage.XCorrFcaLags[irx][iht][0] = taui;
                                    taup = xcMax[0];
                                    _dataPackage.XCorrFcaLags[irx][iht][1] = taup;

                                    int sz5 = iht;

                                    // find lag where auto-corr(taux) equals cross-corr(0)
                                    double y0 = xcPoly.Evaluate(0.0);
                                    x1 = acMax[0];
                                    xdelta = (xcMax[0] - acMax[0]) / 4.0;
                                    int dir = Math.Sign(xdelta);
                                    ysign0 = Math.Sign(acPoly.Evaluate(x1) - y0);
                                    x1 = x1 + xdelta;
                                    int numIter2 = 0;
                                    while (Math.Abs(xdelta) > stopDelta) {
                                        numIter2++;
                                        ysign1 = Math.Sign(acPoly.Evaluate(x1) - y0);
                                        if (ysign1 == 0) {
                                            break;
                                        }
                                        if (ysign1 == ysign0) {
                                            x1 = x1 + xdelta;
                                        }
                                        else {
                                            x1 = x1 - xdelta;
                                            xdelta = xdelta / 2.0;
                                        }
                                        if (x1 < xFirstLag || x1 > xLastLag) {
                                            x1 = 0.0;  // this indicates invalid taux
                                            break;
                                        }
                                    }
                                    taux = x1;
                                    _dataPackage.XCorrFcaLags[irx][iht][2] = taux;


                                    // adjust polynomial baseline back up to match with original data plot
                                    // Do not do any FCA calculations after this because polynomial is not the right one to use.
                                    // The xc polynomial should be a good fit with the xc data, ac poly has been normalized to xc poly;
                                    _dataPackage.XCorrPolyCoeffs[irx][iht][0] = _dataPackage.XCorrPolyCoeffs[irx][iht][0] + (_xcBase[irx][iht] * xcScale);
                                    _dataPackage.XCorrPolyCoeffs[irx + 3][iht][0] = _dataPackage.XCorrPolyCoeffs[irx + 3][iht][0] + (_xcBase[irx][iht] * xcScale);
                                }
                            }
                            catch (Exception ee) {
                                string msg = ee.Message;
                                //throw new ApplicationException("Polynomial Fit error: " + msg);
                            }

                        }
                        /// end gaussian fit ////
                        /// 
                        int sz = iht;

                        for (int irx = 0; irx < _nRx; irx++) {
                            XCorrelations.XCorrRatio(XCorrRatio[irx][iht], XCorrMag[irx][iht], _maxLag);
                        }

                        if (lineFitPts > 0) {
                            XCorrelations.XCorrRatioLSQFit(XCorrRatio[0][iht], nTotalLags, lineFitPts, out m, out b);
                            XCorrRatioLine[0][iht].B = b;
                            XCorrRatioLine[0][iht].M = m / oneLag;
                            XCorrelations.XCorrRatioLSQFit(XCorrRatio[1][iht], nTotalLags, lineFitPts, out m, out b);
                            XCorrRatioLine[1][iht].B = b;
                            XCorrRatioLine[1][iht].M = m / oneLag;
                            XCorrelations.XCorrRatioLSQFit(XCorrRatio[2][iht], nTotalLags, lineFitPts, out m, out b);
                            XCorrRatioLine[2][iht].B = b;
                            XCorrRatioLine[2][iht].M = m / oneLag;
                        }
                    }
                    else {
                        for (int irx = 0; irx < _nRx; irx++) {
                            XCorrRatioLine[irx][iht].B = 0.0;
                            XCorrRatioLine[irx][iht].M = 0.0;
                            for (int j = 0; j < nTotalLags; j++) {
                                XCorrRatio[irx][iht][j] = 0.0;
                            }
                        }
                    }

                    int sz1 = iht;

                }  // end for iht loop

            }
        }

        private double FindBaseLevel(double[] p, int npts) {
            // smooth the data pts and find lowest value
            double level = double.MaxValue;
            double sum;
            int nsmooth = npts / 50; ;
            int delta = nsmooth / 2;
            int firstPt = delta;
            int lastPt = npts - delta - 1;
            for (int i = firstPt; i <= lastPt; i++) {
                sum = 0.0;
                for (int j = 1; j <= delta; j++) {
                    sum += p[i - j] + p[i + j];
                }
                sum += p[i];
                if (sum < level) {
                    level = sum;
                }
            }
            return (level / (2 * delta + 1));
        }

        private double[] MaxOverInterval(PolyFit poly, double[] x, int npts) {
            double maxVal = double.NegativeInfinity;
            double value;
            int idx = -999;
            for (int ii = 0; ii < npts; ii++) {
                value = poly.PolyValue(x[ii]);
                if (value > maxVal) {
                    maxVal = value;
                    idx = ii;
                }
            }
            double[] maxPt = new double[2];
            maxPt[0] = x[idx];
            maxPt[1] = maxVal;
            return maxPt;
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Do processing of moments, e.g. melting layer, etc.
        /// </summary>
        /// <returns></returns>
        private PopCommands ProcessMoments() {

            PopCommands command = PopCommands.None;

            if (_parameters.ReplayPar.Enabled &&
                !_parameters.ReplayPar.ProcessRawSamples &&
                !_parameters.ReplayPar.ProcessTimeSeries &&
                !_parameters.ReplayPar.ProcessSpectra &&
                !_parameters.ReplayPar.ProcessMoments) {

                command = CheckCommand();
                return command;
            }

            if (_dataPackage == null) {
                command = CheckCommand();
                return command;
            }

            ////// Consensus //////////////////////////

            // TODO: add consensus processing
            // TODO: cns set for 1 rx only (and only works in replay mode)
            if (_parameters.SystemPar.RadarPar.ProcPar.CnsPar[0].CnsEnable) {
                _cnsDwell.Azimuth = _dataPackage.Parameters.SystemPar.RadarPar.BeamDirections[0].Azimuth;
                _cnsDwell.Elevation = _dataPackage.Parameters.SystemPar.RadarPar.BeamDirections[0].Elevation;
                _cnsDwell.IppMicroSec = _dataPackage.Parameters.SystemPar.RadarPar.BeamParSet[0].IppMicroSec;
                _cnsDwell.NCode = _dataPackage.Parameters.SystemPar.RadarPar.BeamParSet[0].NCode;
                _cnsDwell.PulseWidthNs = _dataPackage.Parameters.SystemPar.RadarPar.BeamParSet[0].PulseWidthNs;
                _cnsDwell.TimeStamp = _dataPackage.RecordTimeStamp;
                _cnsDwell.NyquistMS = _dataPackage.Parameters.GetBeamParNyquist(0);
                _cnsDwell.Heights = _dataPackage.Parameters.GetBeamParHeightsM(0, _cnsDwell.Elevation);
                //_cnsDwell.RadialDoppler = _dataPackage.MeanDoppler[0];  // _dataPackage array could be larger than nhts, so:
                int nhts = _cnsDwell.Heights.Length;
                _cnsDwell.RadialDoppler = new double[nhts];
                Array.Copy(_dataPackage.MeanDoppler[0], _cnsDwell.RadialDoppler, nhts);  // units of MeanDoppler are Nyquists
                _cnsDwell.CnsOutputFolder = _dataPackage.Parameters.SystemPar.RadarPar.ProcPar.CnsPar[0].CnsFilePath;
                //sampledDataPackage.Parameters.SystemPar.RadarPar.BeamParSet.n
                _consensus.Add(_cnsDwell);
            }

            ////// End Consensus //////////////////////

            ////// Melting Layer ///////////////////
            try {
                if (_parameters.MeltingLayerPar.Enable) {

                    DateTime timeStamp;
                    bool endOfInterval;

                    if (!_endOfData) {
                        // we have current data to process
                        timeStamp = _dataPackage.RecordTimeStamp;
                        if ((_meltingLayer == null)) {
                            _meltingLayer = new MeltingLayerCalculator3(_parameters);
                            _meltingLayer.StartNewInterval(timeStamp);
                        }
                        endOfInterval = _meltingLayer.PastEndOfInterval(timeStamp);
                    }
                    else {
                        // no more data, but need to finish layer from last time interval
                        if (_meltingLayer == null) {
                        }
                        endOfInterval = true;
                        timeStamp = DateTime.MaxValue;   // need dummy value for compiler
                    }

                    if (_meltingLayer != null) {

                        if (endOfInterval ||
                            (_meltingLayer.Parameters != _dataPackage.Parameters)) {

                            // have gone past end of averaging interval or params have changed,
                            // so terminate this interval
                            // and computer layer
                            _meltingLayer.CalculateMeltingLayer(_meltingLayer);

                            if (!_endOfData) {

                                if (_meltingLayer.Parameters != _dataPackage.Parameters) {
                                    // new params; reset calculator
                                    _meltingLayer.Init();
                                    _meltingLayer.Parameters = _dataPackage.Parameters;
                                }

                                _meltingLayer.StartNewInterval(timeStamp);

                            }
                        }  // end of if terminating previous interval

                        if (!_endOfData) {
                            // add current record to current interval
                            _meltingLayer.Add(_dataPackage);

                        }
                    }  // end of if _meltingLayer calculator exists

                    /*
                    if (_meltingLayer != null) { 
                        // calculate melting layer from previous interval
                        if (_meltingLayer.TimeToCalculate(timeStamp, _endOfData) ||
                            (_meltingLayer.Parameters != _dataPackage.Parameters)) {
                            MeltingLayerCalculator3.CalculateMeltingLayer(_meltingLayer);
                        }
                    }

                    if (!_endOfData) {
                        if (_meltingLayer.Parameters != _dataPackage.Parameters) {
                            // parameters have changed, start new calculator
                            _meltingLayer = new MeltingLayerCalculator3(_parameters);
                            // need to call this to init new interval:
                            _meltingLayer.TimeToCalculate(timeStamp, _endOfData);
                        }

                        if (_dataPackage != null) {
                            _meltingLayer.Add(_dataPackage);
                        }
                    }
                    */
                }
            }
            catch (Exception exc) {
                //string ss = exc.Message;
                //MessageBoxEx.Show(ss, "Melting Layer Error", 5000);
                SendStatusException("*** Melting Layer Error.");
                SendStatusString(exc.Message);
                SendStatusString(exc.StackTrace);
            }

            ////////// end melting layer ////////////////////////

            command = CheckCommand();
            return command;
        }


    }  // end class PopNDwellWorker

}  // end namespace
