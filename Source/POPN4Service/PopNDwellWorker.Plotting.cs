using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.MemoryMappedFiles;

using POPCommunication;
using DACarter.NOAA.Hardware;
using DACarter.PopUtilities;
using ipp;
using DACarter.Utilities.Maths;
using System.Windows.Forms;
using System.IO;


namespace POPN4Service {
    partial class PopNDwellWorker {

        public void ParamsRequested(object sender, POPCommunicator.PopParamArgs arg) {
            arg.Params = _parameters;
        }

        public void PowerMeterRequested(object sender, POPCommunicator.PopPowerMeterArgs arg) {
            if (_powerMeter != null) {
                arg.PowerReading = _powerMeter.PowerReading;
                arg.TempReading = _powerMeter.TempReading;
                arg.FreqMHz = _powerMeter.FreqMHz;
                arg.PowerOffset = _powerMeter.OffsetDB;
                arg.TempUnits = _powerMeter.TempFormat;
            }
            else {
                arg.PowerReading = -9999.9;
                arg.TempReading = -999.9;
                arg.FreqMHz = 0.0;
                arg.PowerOffset = 0.0;
                arg.TempUnits = "-";
            }
        }

        public void SamplesRequested(object sender, POPCommunicator.PopSamplesArgs arg) {
            int irx = arg.IRx;
            int nIPP = 2;
            if (_dataPackage != null && _dataPackage.SampledTimeSeries != null) {
                int nspec = _dataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNSpec;
                int specDim = _dataPackage.SampledTimeSeries[0].Length;
                if (specDim < nspec) {
                    nspec = specDim;
                }
                int npts = _dataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNPts;
                int nGates = _dataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts;
                arg.SampTS = null;
                //arg.SampTS = new double[npts];
                //Array.Copy(_dataPackage.SampledTimeSeries[irx][0][0], arg.SampTS, npts);
                //_dataPackage.SampledTimeSeries[irx][0][0].CopyTo(arg.SampTS, 0);
                double[][] ary = new double[nIPP][];
                for (int ipp = 0; ipp < nIPP; ipp++) {
                    ary[ipp] = new double[nGates];
                    for (int igate = 0; igate < nGates; igate++) {
                        ary[ipp][igate] = _dataPackage.SampledTimeSeries[irx][0][ipp][igate];
                    }
                }
                int count = 0;
                double sumsq = 0.0;
                double sum = 0.0;
                double val;
                for (int ispec = 0; ispec < nspec; ispec++) {
                    for (int ipt = 0; ipt < npts; ipt++) {
                        for (int igate = 0; igate < nGates; igate++) {
                            val = _dataPackage.SampledTimeSeries[irx][ispec][ipt][igate];
                            sum += val;
                            sumsq += val * val;
                            count++;
                        }
                    }
                }
                double stdDev = Math.Sqrt(sumsq / count - (sum * sum / count / count));
                //
                // fill in plot arrays in mmfile
                /**/
                using (MemoryMappedFile mmf1 = MemoryMappedFile.OpenExisting("Global\\PopNRawSamplesPlotMMF", MemoryMappedFileRights.FullControl)) {
                    long size = mmf1.CreateViewStream().Capacity;
                    using (MemoryMappedViewAccessor view = mmf1.CreateViewAccessor()) {
                        view.Write<Int32>(0, ref nIPP);
                        view.Write<Int32>(4, ref nGates);
                        view.Write<Double>(8, ref stdDev);
                        for (int j = 0; j < nIPP; j++) {
                            int offset = 16 + j * nGates * sizeof(double);
                            view.WriteArray<double>(offset, ary[j], 0, nGates);
                        }
                        view.Flush();
                    }
                }
                /**/
            }
            else {
                arg.SampTS = null;
            }
        }

        public void NumDaqDevicesRequested(object sender, POPCommunicator.NumDaqDevicesArgs arg) {

            if (!_noHardware) {
                int nDev = 0;
                arg.NumDaqDevices = nDev;
                arg.DaqDeviceNames = new String[nDev];

                

                DAQDevice daq = null;
                if (_daqBoard == null) {
                    daq = DAQDevice.GetAttachedDAQ();
                    //MessageBox.Show("Plotting: devices = " + (daq.NumDevices).ToString());
                }
                else {
                    daq = _daqBoard;
                }

                if (daq != null) {
                    string daqLibName = daq.DAQLibraryName;
                    //MessageBox.Show(daqLibName);
                }

                if (daq == null) {
                    nDev = 0;
                    arg.NumDaqDevices = nDev;
                    arg.DaqDeviceNames = new String[nDev];
                    for (int i = 0; i < nDev; i++) {
                        arg.DaqDeviceNames[i] = "----";
                    }
                }
                else {
                    nDev = daq.NumDevices;
                    arg.NumDaqDevices = nDev;
                    arg.DaqDeviceNames = new String[nDev];
                    //MessageBox.Show("# daq.DeviceSerialNumbers = " + ((daq.DeviceSerialNumbers).Count.ToString()));
                    for (int i = 0; i < nDev; i++) {
                        if (daq.DeviceSerialNumbers.Count > i) {
                            arg.DaqDeviceNames[i] = daq.DeviceSerialNumbers[i];
                        }
                    }
                }
                /*
                int bits = 8 * IntPtr.Size;
                if (bits == 32) {
                    DAQCOMLib.DaqSystem daqSys = new DaqSystem();
                    DAQCOMLib.Acq daqAcq = daqSys.Add();
                    DAQCOMLib.AvailableDevices sysDevices = daqAcq.AvailableDevices;
                    arg.NumDaqDevices = sysDevices.Count;
                    arg.DaqDeviceNames = new String[sysDevices.Count];
                    for (int i = 0; i < sysDevices.Count; i++) {
                        arg.DaqDeviceNames[i] = sysDevices[i + 1].Name;
                    }
                }
                else {
                }
                */
            }

            //MessageBox.Show("Returning from NumDaqDevicesRequested");

            return;

        }

        public void AScanRequested(object sender, POPCommunicator.PopAScanArgs arg) {
            //
            // POPREV: modified PlotAscan to use MemoryMappedFile, 3.18
            //  since complex arrays bigger than 32k bytes crashed the POPCommunicator
            //
            //SampledTimeSeries[0][0][0].CopyTo(arg.SampTS, 0);

            /*
            try {
                MemoryMappedFile mmf1 = MemoryMappedFile.OpenExisting("Global\\Junk123");
                SendStatusString("Opened Junk123");
            }
            catch (Exception e) {
                SendStatusString("MMF failed to open: " + e.Message);
            }
            */

            int irx = arg.IRx;
            int ipt = arg.IPt;
            if (_dataPackage != null && _dataPackage.TransformedTimeSeries != null) {
                //int nhts = _dataPackage.TransformedTimeSeries[irx][0].Length;
                int nhts;
                if (_dataPackage.Parameters.ReplayPar.Enabled) {
                    // number of hts written to POP file
                    nhts = _dataPackage.Parameters.SystemPar.RadarPar.BeamParSet[0].NHts;
                }
                else {
                    // all hts computed
                    nhts = _dataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts / 2 + 1;
                }
                arg.NGates = nhts;
                arg.AScan = null;
                //arg.AScan = new Ipp64fc[nhts];
                Ipp64fc[] ary = new Ipp64fc[nhts];
                for (int iht = 0; iht < nhts; iht++) {
                    //arg.AScan[iht] = _dataPackage.TransformedTimeSeries[irx][0][iht][ipt];
                    ary[iht] = _dataPackage.TransformedTimeSeries[irx][0][iht][ipt];
                }
                //
                // fill in plot arrays in mmfile
                /**/
                using (MemoryMappedFile mmf1 = MemoryMappedFile.OpenExisting("Global\\PopNAScanPlotMMF", MemoryMappedFileRights.FullControl)) {
                    long size = mmf1.CreateViewStream().Capacity;
                    using (MemoryMappedViewAccessor view = mmf1.CreateViewAccessor()) {
                        view.Write<Int32>(0, ref nhts);
                        view.WriteArray<Ipp64fc>(4, ary, 0, nhts);
                        view.Flush();
                    }
                }
                /**/
                //Console.Beep(800, 300);

                //Thread.Sleep(1000);
                //
            }
            else {
                arg.AScan = null;
            }
            //Console.Beep(440, 300);
        }

        public void SpectrumRequested(object sender, POPCommunicator.PopSpectrumArgs arg) {
            int irx = arg.IRx;
            int iht = arg.IHt;
            if (_dataPackage != null && _dataPackage.Spectra != null) {
                //int npts = _dataPackage.Spectra[irx][iht].GetLength(0);
                int npts;
                if (_dataPackage.Parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCw) {
                    npts = _dataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNPts;
                }
                else if (_dataPackage.RassIsOn) {
                    npts = _dataPackage.Parameters.SystemPar.RadarPar.ProcPar.Dop1 +
                                 _dataPackage.Parameters.SystemPar.RadarPar.ProcPar.Dop3;
                }
                else {
                    npts = _dataPackage.Parameters.SystemPar.RadarPar.BeamParSet[0].NPts;
                }
                arg.NPts = npts;
                arg.Spectrum = null;
                //arg.Spectrum = new double[npts];
                double[] ary = new double[npts];
                Array.Copy(_dataPackage.Spectra[irx][iht], ary, npts);

                // to debug noise algorithm:
                //Array.Sort(ary);

                // new comment 4.12.1
                using (MemoryMappedFile mmf1 = MemoryMappedFile.OpenExisting("Global\\PopNSpecPlotMMF", MemoryMappedFileRights.FullControl)) {
                    long size = mmf1.CreateViewStream().Capacity;
                    using (MemoryMappedViewAccessor view = mmf1.CreateViewAccessor()) {
                        view.Write<Int32>(0, ref npts);
                        view.WriteArray<double>(4, ary, 0, npts);
                        view.Flush();
                    }
                }
            }
            else {
                arg.Spectrum = null;
            }
        }

        /// <summary>
        /// Called when UI wants to plot magnitude of the cross-correlation
        ///     at a particular ht
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arg"></param>
        public void CrossCorrRequested(object sender, POPCommunicator.PopCrossCorrArgs arg) {
            int irx = arg.IRx;
            int iht = arg.IHt;
            if (_dataPackage != null && _dataPackage.XCorrMag != null) {
                //int npts = _dataPackage.Spectra[irx][iht].GetLength(0);
                int nLags = _dataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrMaxLag;
                int npts = _dataPackage.XCorrNPts;
                int navgs = _dataPackage.XCorrNAvgs;
                nLags = 2 * nLags + 1;
                arg.NLags = nLags;
                arg.NPts = npts;
                arg.NAvgs = navgs;
                arg.AutoCorr = null;
                arg.AutoCorr = new double[nLags];
                if (_nRx == 3) {
                    arg.GaussCoeffs = null;
                    arg.GaussCoeffs = new double[4];
                    arg.CrossCorr = null;
                    arg.CrossCorr = new double[nLags];
                    arg.FcaLags = null;
                    arg.FcaLags = new double[3];
                    arg.PolyFitOrder = _polyFitOrder;
                    arg.PolyFitCoeffsA = new double[_polyFitOrder + 1];
                    arg.PolyFitCoeffsX = new double[_polyFitOrder + 1];
                    arg.AutoPolyFitPts = _polyFitPts;
                    arg.XCorrPeakI = _xcorrPeakI[irx][iht];
                    arg.AutoBaseline = _acBase[irx][iht];
                    arg.CrossBaseline = _xcBase[irx][iht];
                    arg.AntennaDeltaX = _parameters.SystemPar.RadarPar.AntSpacingM;
                    
                }

                if (_nRx == 1) {
                    Array.Copy(_dataPackage.XCorrMag[0][iht], arg.AutoCorr, nLags);
                }
                else {
                    Array.Copy(_dataPackage.XCorrMag[irx][iht], arg.CrossCorr, nLags);
                    Array.Copy(_dataPackage.XCorrPolyCoeffs[irx][iht], arg.PolyFitCoeffsX, _polyFitOrder + 1);
                    if (_dataPackage.XCorrMag.Length >= irx + 4) {
                        Array.Copy(_dataPackage.XCorrMag[irx + 3][iht], arg.AutoCorr, nLags);
                        Array.Copy(_dataPackage.XCorrPolyCoeffs[irx + 3][iht], arg.PolyFitCoeffsA, _polyFitOrder + 1);
                    }
                    else {
                        arg.AutoCorr = null;
                    }
                    for (int i = 0; i < 4; i++) {
                        arg.GaussCoeffs[i] = _dataPackage.XCorrGaussCoeffs[irx][iht][i];
                    }
                    for (int i = 0; i < 3; i++) {
                        arg.FcaLags[i] = _dataPackage.XCorrFcaLags[irx][iht][i];
                    }
                    arg.SlopeAtZero = _dataPackage.XCorrSlope0[irx][iht];
                    arg.NPts = _dataPackage.XCorrNPts;
                    arg.NAvgs = _dataPackage.XCorrNAvgs;
                }

            }
            else {
                arg.CrossCorr = null;
                arg.AutoCorr = null;
                arg.GaussCoeffs = null;
                arg.FcaLags = null;
            }
        }

        /// <summary>
        /// Called when UI wants to plot the cross-correlation ratio
        ///     at a particular ht
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arg"></param>
        public void CrossCorrRatioRequested(object sender, POPCommunicator.PopCrossCorrArgs arg) {
            int irx = arg.IRx;
            int iht = arg.IHt;
            if (_dataPackage != null && _dataPackage.XCorrRatio != null) {
                //int npts = _dataPackage.Spectra[irx][iht].GetLength(0);
                int npts = _dataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrMaxLag;
                npts = 2 * npts + 1;
                arg.NLags = npts;
                arg.CrossCorr = null;
                arg.CrossCorr = new double[npts];

                Array.Copy(_dataPackage.XCorrRatio[irx][iht], arg.CrossCorr, npts);
                arg.Line = _dataPackage.XCorrRatioLine[irx][iht];

            }
            else {
                arg.CrossCorr = null;
                arg.Line.B = 0.0;
                arg.Line.M = 0.0;
            }
        }

        public void CltrWvltRequested(object sender, POPCommunicator.PopCltrWvltArgs arg) {

            int irx = arg.IRx;
            int iht = arg.IHt;
            bool doClutterWavelets = _parameters.SystemPar.RadarPar.ProcPar.DoClutterWavelet;

            if (doClutterWavelets) {
                if (_dataPackage != null && _dataPackage.WaveletClutterTransform != null) {
                    //int nWvltPts = DWavelet.SizeOfTransformArrays;
                    //int nWvltPts = DACarter.Utilities.Tools.NextPowerOf2(_nPts);
                    int nWvltPts = _dataPackage.WaveletOutputNpts;
                    //int nptsClipped = DWavelet.NumPointsInClippedSegment;
                    int nptsClipped = _dataPackage.WaveletClippedNpts;
                    int nCurves = 2;
                    //double[] ary = new double[npts];
                    int nn;
                    //double[] ary;
                    //UnfilteredWaveletTransform(irx, iht, out nn, out ary);
                    //Array.Copy(_dataPackage.WaveletClutterTransform[irx][iht], ary, npts);
                    using (MemoryMappedFile mmf1 = MemoryMappedFile.OpenExisting("Global\\PopNCltrWvltPlotMMF", MemoryMappedFileRights.ReadWrite)) {
                        using (MemoryMappedViewAccessor view = mmf1.CreateViewAccessor()) {
                            view.Write<Int32>(0, ref nWvltPts);
                            view.Write<Int32>(4, ref nptsClipped);
                            view.Write<Int32>(8, ref nCurves);
                            view.WriteArray<double>(12, _dataPackage.WaveletClutterTransform[irx][iht], 0, 2*nWvltPts);
                            //int offset = nWvltPts * sizeof(double);
                            //view.WriteArray<double>(12 + offset, ary, 0, nWvltPts);
                            view.Flush();
                        }
                    }
                }
                else {
                    // no valid data; send zero points
                    using (MemoryMappedFile mmf1 = MemoryMappedFile.OpenExisting("Global\\PopNCltrWvltPlotMMF", MemoryMappedFileRights.ReadWrite)) {
                        using (MemoryMappedViewAccessor view = mmf1.CreateViewAccessor()) {
                            int npts = 0;
                            int nCurves = 0;
                            view.Write<Int32>(0, ref npts);
                            view.Write<Int32>(4, ref npts);
                            view.Write<Int32>(8, ref nCurves);
                            //view.WriteArray<double>(8, ary, 0, npts);
                            view.Flush();
                        }
                    }
                }

            }
            else {
                // not doing wavelet filtering; show unfiltered transform
                int npts;
                double[] ary;
                UnfilteredWaveletTransform(irx, iht, out npts, out ary);
                if (npts == 0 || ary == null) {
                    return;
                }
                int nptsClipped = 0;
                int nCurves = 1;
                using (MemoryMappedFile mmf1 = MemoryMappedFile.OpenExisting("Global\\PopNCltrWvltPlotMMF", MemoryMappedFileRights.ReadWrite)) {
                    using (MemoryMappedViewAccessor view = mmf1.CreateViewAccessor()) {
                        view.Write<Int32>(0, ref npts);
                        view.Write<Int32>(4, ref nptsClipped);
                        view.Write<Int32>(8, ref nCurves);
                        view.WriteArray<double>(12, ary, 0, npts);
                        view.Flush();
                    }
                }

            }
        }

        public void DopplerTSRequested(object sender, POPCommunicator.PopDopplerTSArgs arg) {
            //
            // POPREV: modified DopplerTSRequested to use MMF  3.18
            //  since complex arrays bigger than 32k bytes crashed the POPCommunicator
            //
            int irx = arg.IRx;
            int iht = arg.IHt;
            if (_dataPackage != null && _dataPackage.TransformedTimeSeries != null) {
                int npts = _dataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNPts;
                arg.NPts = npts;
                arg.DopplerTS = null;
                //arg.DopplerTS = new Ipp64fc[npts];
                // TODO: move array alloc from DopplerTSRequested() to PopNAllocator
                Ipp64fc[] ary = new Ipp64fc[npts];
                Array.Copy(_dataPackage.TransformedTimeSeries[irx][0][iht], ary, npts);
                //_dataPackage.TransformedTimeSeries[irx][0][iht].CopyTo(arg.DopplerTS, 0);
                //
                int nspec = _dataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNSpec;
                int specDim = _dataPackage.TransformedTimeSeries[0].Length;
                if (specDim < nspec) {
                    nspec = specDim;
                }
                int count = 0;
                double sumsq = 0.0;
                double sum = 0.0;
                double val;
                for (int ispec = 0; ispec < nspec; ispec++) {
                    for (int ipt = 0; ipt < npts; ipt++) {
                        val = _dataPackage.TransformedTimeSeries[irx][ispec][iht][ipt].re;
                        sum += val;
                        sumsq += val * val;
                        count++;
                    }                 
                }
                double stdDev = Math.Sqrt(sumsq/count - (sum*sum/count/count));
                //
                // fill in plot arrays in mmfile
                /**/
                using (MemoryMappedFile mmf1 = MemoryMappedFile.OpenExisting("Global\\PopNDoppTSPlotMMF", MemoryMappedFileRights.ReadWrite)) {
                    using (MemoryMappedViewAccessor view = mmf1.CreateViewAccessor()) {
                        view.Write<Int32>(0, ref npts);
                        view.Write<Double>(4, ref stdDev);
                        view.WriteArray<Ipp64fc>(12, ary, 0, npts);
                        view.Flush();
                    }
                }
                /**/
            }
            else {
                arg.DopplerTS = null;
            }
        }

        public void CrossCorrProfileRequested(object sender, POPCommunicator.PopProfileArgs arg) {
            int irx = arg.IRx;
            if (_dataPackage != null && _dataPackage.XCorrSlope0 != null) {
                //if (_dataPackage.XCorrRatioLine != null) {
                int nhts;  // = _dataPackage.Power[irx].Length;
                if (_dataPackage.Parameters.ReplayPar.Enabled) {
                    // number of hts written to POP file
                    nhts = _dataPackage.Parameters.SystemPar.RadarPar.BeamParSet[0].NHts;
                }
                else {
                    // all hts computed
                    nhts = _dataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts / 2 + 1;
                }
                arg.NHts = nhts;
                arg.Data = null;
                arg.Data = new double[nhts];
                for (int iht = 0; iht < nhts; iht++) {
                    arg.Data[iht] = _dataPackage.XCorrSlope0[irx][iht][1];
                    //arg.Data[iht] = _dataPackage.XCorrRatioLine[irx][iht].M;
                }
                arg.Data2 = null;
                arg.Data2 = new double[nhts];
                double ah = _parameters.SystemPar.RadarPar.ASubH;
                double dx = _parameters.SystemPar.RadarPar.AntSpacingM;
                if (_dataPackage.XCorrRatioLine != null) {
                    for (int iht = 0; iht < nhts; iht++) {
                        arg.Data2[iht] = _dataPackage.XCorrRatioLine[irx][iht].M / 2.0 / ah / ah / dx;
                        // arbitrary sign change to agree with slope at zero lag direction:
                        arg.Data2[iht] = -arg.Data2[iht];
                    }
                }
                //_dataPackage.Noise[irx].CopyTo(arg.Noise, 0);
                //_dataPackage.Power[irx].CopyTo(arg.Power, 0);
                //_dataPackage.MeanDoppler[irx].CopyTo(arg.Doppler, 0);
            }
            else {
                arg.Data = null;
            }
        }

        public void MomentsRequested(object sender, POPCommunicator.PopMomentsArgs arg) {
            int irx = arg.IRx;
            if (_dataPackage != null &&
                _dataPackage.Power != null &&
                _dataPackage.Noise != null &&
                _dataPackage.MeanDoppler != null) {
                int nhts;  // = _dataPackage.Power[irx].Length;
                if (_dataPackage.Parameters.ReplayPar.Enabled) {
                    // number of hts written to POP file
                    nhts = _dataPackage.Parameters.SystemPar.RadarPar.BeamParSet[0].NHts;
                }
                else {
                    // all hts computed
                    nhts = _dataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts / 2 + 1;
                }
                arg.NHts = nhts;
                arg.Power = null;
                arg.Power = new double[nhts];
                arg.Noise = null;
                arg.Noise = new double[nhts];
                arg.Doppler = null;
                arg.Doppler = new double[nhts];
                Array.Copy(_dataPackage.Noise[irx], arg.Noise, nhts);
                Array.Copy(_dataPackage.Power[irx], arg.Power, nhts);
                Array.Copy(_dataPackage.MeanDoppler[irx], arg.Doppler, nhts);
                //_dataPackage.Noise[irx].CopyTo(arg.Noise, 0);
                //_dataPackage.Power[irx].CopyTo(arg.Power, 0);
                //_dataPackage.MeanDoppler[irx].CopyTo(arg.Doppler, 0);
            }
            else {
                arg.Power = null;
                arg.Noise = null;
                arg.Doppler = null;
            }
        }


        private void UnfilteredWaveletTransform(int irx, int iht, out int npts, out double[] ary) {
            DaubechiesWavelet DWavelet2 = null;
            ary = null;
            npts = 0;
            if (_dataPackage.TransformedTimeSeries == null) {
                return;
            }
            DWavelet2 = new DaubechiesWavelet(_nPts, 20);
            // TODO: ispec index of _nSpec-1 doesn't work when doing partial processing;
            //  find a way to get nspecAtATime in here;
            // using 0 for now:
            DWavelet2.Transform(_dataPackage.TransformedTimeSeries[irx][0][iht]);
            npts = DWavelet2.SizeOfTransformArrays;
            ary = new double[npts];
            DWavelet2.CopyTransformTo(ary);
        }

    }
}
