using System;
using MathNet.Numerics;


using ipp;	// Intel IPP Math Library


namespace DACarter.Utilities.Maths {


	public unsafe /*static*/ class IntelMath : IDisposable {
    
		public static int IPP_FFT_DIV_FWD_BY_N = 1;
		public static int IPP_FFT_DIV_INV_BY_N = 2;
		public static int IPP_FFT_DIV_BY_SQRTN = 4;
		public static int IPP_FFT_NODIV_BY_ANY = 8;

		#region Private Members
        private int _FFTSizeRC;
        private int _FFTSizeCC;
        private int _DFTSizeRC;
        private int _DFTSizeCC;
        private double[] OutputCCS;
        private double[] OutputDFTCCS;
        private Ipp64fc[] outputfc;
		private Ipp64fc[] inputfc;
		private IppsFFTSpec_R_64f* pSpecsR;      // pointer to FFT specification structure
        private IppsFFTSpec_C_64fc* pSpecsC;
        private IppsDFTSpec_C_64fc* pDFTSpecsC;
        private IppsDFTSpec_R_64f* pDFTSpecsR;
        private byte[] _BufferR;
        private byte[] _BufferC;
        private int _BufferRSize;
        private int _BufferCSize;
        private byte[] _BufferDFTR;
        private byte[] _BufferDFTC;
        private int _BufferDFTRSize;
        private int _BufferDFTCSize;
        private IppStatus status;

		private double[] HannCoefs;
		private double[] HammingCoefs;
		private double[] BlackCoefs;
		private double[] RieszCoefs;

        // temp arrays for doing work
        // Methods can allocate and reuse to save time
        private Ipp64fc[] _workArrayC1;
        private Ipp64fc[] _workArrayC2;
        private Ipp64fc[] _workArrayC3;
        private double[] _workArrayD1;
        //
		#endregion

		#region Constructor
		//
		public IntelMath() {
            _FFTSizeRC = -1;
            _FFTSizeCC = -1;
            _DFTSizeRC = -1;
            _DFTSizeCC = -1;
            OutputCCS = null;
            OutputDFTCCS = null;
            //OutputC = null;
		}
		//
		#endregion Constructor

		#region Private Initializers
		///
		/// <summary>
		/// Initialization before first call to FFT(double, complex)
		/// </summary>
		/// <param name="size"></param>
		private void InitRC(int size) {
            OutputCCS = null;
			OutputCCS = new double[size + 2];
			//OutputC = new Complex[size];
			//IppsFFTSpec_R_64f specs = new IppsFFTSpec_R_64f();

			int FFTOrder = 0;
			int tsize = size;
			while (tsize > 1) {
				FFTOrder++;
				tsize /= 2;
			}

			// Initialize the spec structure and allocate memory for it
			fixed (IppsFFTSpec_R_64f** ppSpecs = &pSpecsR) {
				status = ipp.sp.ippsFFTInitAlloc_R_64f(ppSpecs,
												FFTOrder,
												IPP_FFT_DIV_FWD_BY_N,
												//IPP_FFT_DIV_BY_SQRTN,
												ipp.IppHintAlgorithm.ippAlgHintFast);
				if (status != IppStatus.ippStsNoErr) {
					throw new ApplicationException("Error in FFT64 ippsFFTInitAlloc_R_64f: " + status.ToString());
				}

				fixed (int* pBufferSize = &_BufferRSize) {
					// compute size of external work buffer

					status = ipp.sp.ippsFFTGetBufSize_R_64f(pSpecsR, pBufferSize);
					if (status != IppStatus.ippStsNoErr) {
						throw new ApplicationException("Error in FFT64 ippsFFTGetBufSize_R_64f: " + status.ToString());
					}
                    _BufferR = null;
					_BufferR = new byte[_BufferRSize];

				}
			}
			_FFTSizeRC = size;
		}

		private void InitCC(int size) {

			//outputfc = new Ipp64fc[size];
			//inputfc = new Ipp64fc[size];

			int FFTOrder = 0;
			int tsize = size;
			while (tsize > 1) {
				FFTOrder++;
				tsize /= 2;
			}

			// Initialize the spec structure and allocate memory for it
			fixed (IppsFFTSpec_C_64fc** ppSpecs = &pSpecsC) {
				status = ipp.sp.ippsFFTInitAlloc_C_64fc(ppSpecs,
												FFTOrder,
												IPP_FFT_DIV_FWD_BY_N,
												//IPP_FFT_DIV_BY_SQRTN,
												ipp.IppHintAlgorithm.ippAlgHintFast);
				if (status != IppStatus.ippStsNoErr) {
					throw new ApplicationException("Error in FFT64 ippsFFTInitAlloc_R_64f: " + status.ToString());
				}

				fixed (int* pBufferSize = &_BufferCSize) {
					// compute size of external work buffer

					status = ipp.sp.ippsFFTGetBufSize_C_64fc(pSpecsC, pBufferSize);
					if (status != IppStatus.ippStsNoErr) {
						throw new ApplicationException("Error in FFT64 ippsFFTGetBufSize_R_64f: " + status.ToString());
					}
                    _BufferC = null;
					_BufferC = new byte[_BufferCSize];

				}
			}
			_FFTSizeCC = size;
		}

        private void InitDFTCC(int size) {

            //outputfc = new Ipp64fc[size];
            //inputfc = new Ipp64fc[size];

            // Initialize the DFT structure and allocate memory for it
            fixed (IppsDFTSpec_C_64fc** ppSpecs = &pDFTSpecsC) {
                status = ipp.sp.ippsDFTInitAlloc_C_64fc(ppSpecs,
                                                size,
                                                IPP_FFT_DIV_FWD_BY_N,
                                                ipp.IppHintAlgorithm.ippAlgHintFast);
                if (status != IppStatus.ippStsNoErr) {
                    throw new ApplicationException("Error in FFT64 ippsFFTInitAlloc_C_64f: " + status.ToString());
                }

                fixed (int* pBufferSize = &_BufferDFTCSize) {
                    // compute size of external work buffer

                    status = ipp.sp.ippsDFTGetBufSize_C_64fc(pDFTSpecsC, pBufferSize);
                    if (status != IppStatus.ippStsNoErr) {
                        throw new ApplicationException("Error in FFT64 ippsDFTGetBufSize_C_64fc: " + status.ToString());
                    }
                    _BufferDFTC = null;
                    _BufferDFTC = new byte[_BufferDFTCSize];

                }
            }
            _DFTSizeCC = size;
        }

        private void InitDFTRC(int size) {

            OutputDFTCCS = null;
            OutputDFTCCS = new double[size + 2];  // max length of CCS array; length = size+1 if size is odd;
            
            // Initialize the DFT structure and allocate memory for it
            fixed (IppsDFTSpec_R_64f** ppSpecs = &pDFTSpecsR) {
                status = ipp.sp.ippsDFTInitAlloc_R_64f(ppSpecs,
                                                size,
                                                IPP_FFT_DIV_FWD_BY_N,
                                                ipp.IppHintAlgorithm.ippAlgHintFast);
                if (status != IppStatus.ippStsNoErr) {
                    throw new ApplicationException("Error in FFT64 ippsDFTInitAlloc_R_64f: " + status.ToString());
                }

                fixed (int* pBufferSize = &_BufferDFTRSize) {
                    // compute size of external work buffer

                    status = ipp.sp.ippsDFTGetBufSize_R_64f(pDFTSpecsR, pBufferSize);
                    if (status != IppStatus.ippStsNoErr) {
                        throw new ApplicationException("Error in FFT64 ippsDFTGetBufSize_R_64f: " + status.ToString());
                    }
                    _BufferDFTR = null;
                    _BufferDFTR = new byte[_BufferDFTRSize];

                }
            }
            _DFTSizeRC = size;
        }

        //
		#endregion Initializers

		#region Public Methods

        /// <summary>
        /// Static utility method
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static bool IsPowerOf2(int size) {

            int power = 0;
            int tsize = size;
            while (tsize > 1) {
                power++;
                tsize /= 2;
            }
            return (size == (int)Math.Pow(2.0, power));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="corr">output cross-correlation array</param>
        /// <param name="ts1">input array 1, ts1.Length >= length</param>
        /// <param name="ts2">input array 2</param>
        /// <param name="length">length of input data arrays</param>
        /// <param name="maxLag">maximum lag</param>
        public void XCorr(Ipp64fc[] corr, Ipp64fc[] ts1, Ipp64fc[] ts2, int length, int maxLag) {
            int corrLength = length - 2 * maxLag;
            Ipp64fc zero, product;
            zero.re = 0.0;
            zero.im = 0.0;
            DateTime startTime = DateTime.Now;
            for (int i = 0; i < 2 * maxLag + 1; i++) {
                corr[i] = zero;
                for (int j = 0; j < corrLength; j++) {
                    product = MultiplyConjFC(ts1[j + maxLag], ts2[j - i + 2 * maxLag]);
                    corr[i] = AddFC(product, corr[i]);
                }
                corr[i].re /= corrLength;
                corr[i].im /= corrLength;
            }
            DateTime endTime = DateTime.Now;
            TimeSpan duration = endTime - startTime;
            double seconds = duration.TotalSeconds;
        }


        /// <summary>
        /// Compute cross correlation magnitude via DFT
        /// </summary>
        /// <param name="corrMag">preallocated output array; size = 2*maxLag+1</param>
        /// <param name="ts1">input array</param>
        /// <param name="ts2">input array, same length as ts1</param>
        /// <param name="npts">Number of data pts in the input arrays</param>
        /// <param name="maxLag">Max lag to put in output array</param>
        public void XCorrDFT(double[] corrMag,
                    Ipp64fc[] ts1,
                    Ipp64fc[] ts2,
                    int npts,
                    int maxLag) {

            if (_workArrayC1 == null || _workArrayC1.Length < npts) {
                _workArrayC1 = null;
                _workArrayC1 = new Ipp64fc[npts];
            }

            if (_workArrayC2 == null || _workArrayC2.Length < npts) {
                _workArrayC2 = null;
                _workArrayC2 = new Ipp64fc[npts];
            }

            DFT(ts1, _workArrayC1, npts);
            DFT(ts2, _workArrayC2, npts);

            // remove mean from time series by removing DC from transforms
            _workArrayC1[0].re = 0.0;
            _workArrayC1[0].im = 0.0;
            _workArrayC2[0].re = 0.0;
            _workArrayC2[0].im = 0.0;

            double sum = 0.0;  // measure of cross-power for normalizing cross-correlation
            for (int i = 0; i < npts; i++) {
                sum += _workArrayC1[i].re * _workArrayC1[i].re + _workArrayC1[i].im * _workArrayC1[i].im;
                sum += _workArrayC2[i].re * _workArrayC2[i].re + _workArrayC2[i].im * _workArrayC2[i].im;
                // Modified the following to complex conjugate the second transform instead of the first,
                //  in order to flip the xcorr function to agree with the brute-force method XCorrMag.
                // A peak at positive lag means ts1 lags ts2.
                //_workArrayC1[i] = MultiplyConjFC(_workArrayC1[i], _workArrayC2[i]);
                _workArrayC1[i] = MultiplyConjFC(_workArrayC2[i], _workArrayC1[i]);
            }

            InvDFT(_workArrayC1, _workArrayC2, npts);

            sum /= 2.0;

            // put 2*maxlag+1 values in output array
            // zero lag is at corrMag[maxLag]
            for (int i = 0; i < maxLag + 1; i++) {
                corrMag[i + maxLag] = Math.Sqrt(_workArrayC2[i].re * _workArrayC2[i].re + _workArrayC2[i].im * _workArrayC2[i].im) / sum;
            }
            for (int i = npts - maxLag, j = 0; i < npts; i++, j++) {
                corrMag[j] = Math.Sqrt(_workArrayC2[i].re * _workArrayC2[i].re + _workArrayC2[i].im * _workArrayC2[i].im) / sum;
            }

        }

        /// <summary>
        /// Compute cross correlation magnitude via FFT
        /// </summary>
        /// <param name="corrMag">preallocated output array; size = 2*maxLag+1</param>
        /// <param name="ts1">input array</param>
        /// <param name="ts2">input array, same length as ts1</param>
        /// <param name="npts">Number of data pts in the input arrays</param>
        /// <param name="maxLag">Max lag to put in output array</param>
        public void XCorrFFT(double[] corrMag,
                    Ipp64fc[] ts1, 
                    Ipp64fc[] ts2, 
                    int npts, 
                    int maxLag) {

            int pow2 = Utilities.Tools.NextPowerOf2(npts);
            if (_workArrayC1 == null || _workArrayC1.Length < pow2) {
                _workArrayC1 = null;
                _workArrayC1 = new Ipp64fc[pow2];
            }

            if (_workArrayC2 == null || _workArrayC2.Length < pow2) {
                _workArrayC2 = null;
                _workArrayC2 = new Ipp64fc[pow2];
            }

            if (_workArrayC3 == null || _workArrayC3.Length < pow2) {
                _workArrayC3 = null;
                _workArrayC3 = new Ipp64fc[pow2];
            }
            if (npts != pow2) {
                PadArray(ts1, _workArrayC3, npts, pow2);
                FFT(_workArrayC3, _workArrayC1, pow2);
                PadArray(ts2, _workArrayC3, npts, pow2);
                FFT(_workArrayC3, _workArrayC2, pow2);
            }
            else {
                //ApplyHanningWindow(ts1, _workArrayC3, pow2);
                //FFT(_workArrayC3, _workArrayC1, pow2);
                //ApplyHanningWindow(ts2, _workArrayC3, pow2);
                //FFT(_workArrayC3, _workArrayC2, pow2);
                FFT(ts1, _workArrayC1, pow2);
                FFT(ts2, _workArrayC2, pow2);
            }

            // remove mean from time series by removing DC from transforms
            _workArrayC1[0].re = 0.0;
            _workArrayC1[0].im = 0.0;
            _workArrayC2[0].re = 0.0;
            _workArrayC2[0].im = 0.0;

            double sum = 0.0;  // measure of cross-power for normalizing cross-correlation
            for (int i = 0; i < pow2; i++) {
                sum += _workArrayC1[i].re * _workArrayC1[i].re + _workArrayC1[i].im * _workArrayC1[i].im;
                sum += _workArrayC2[i].re * _workArrayC2[i].re + _workArrayC2[i].im * _workArrayC2[i].im;
                // Modified the following to complex conjugate the second transform instead of the first,
                //  in order to flip the xcorr function to agree with the brute-force method XCorrMag.
                // A peak at positive lag means ts1 lags ts2.
                //_workArrayC1[i] = MultiplyConjFC(_workArrayC1[i], _workArrayC2[i]);
                _workArrayC1[i] = MultiplyConjFC(_workArrayC2[i], _workArrayC1[i]);
            }

            InvFFT(_workArrayC1, _workArrayC2, pow2);

            sum /= 2.0;

            // put 2*maxlag+1 values in output array
            // zero lag is at corrMag[maxLag]
            for (int i = 0; i < maxLag + 1; i++) {
                corrMag[i + maxLag] = Math.Sqrt(_workArrayC2[i].re * _workArrayC2[i].re + _workArrayC2[i].im * _workArrayC2[i].im) / sum;
            }
            for (int i = pow2 - maxLag, j=0; i < pow2; i++, j++) {
                corrMag[j] = Math.Sqrt(_workArrayC2[i].re * _workArrayC2[i].re + _workArrayC2[i].im * _workArrayC2[i].im) / sum;
            }
            
        }

        private void PadArray(Ipp64fc[] inArray, Ipp64fc[] outArray, int inSize, int outSize) {
            int pad = (outSize - inSize) / 2;
            for (int i = 0; i < pad; i++) {
                outArray[i].re = 0.0;
                outArray[i].im = 0.0;
            }
            for (int i = 0; i < inSize; i++) {
                outArray[i + pad] = inArray[i];
            }
            for (int i = 0; i < (outSize - pad - inSize); i++) {
                outArray[i].re = 0.0;
                outArray[i].im = 0.0;
            }
        }

        /// <summary>
        /// Computes the magnitude of the cross-correlation
        /// </summary>
        /// <param name="corrMag"></param>
        /// <param name="ts1"></param>
        /// <param name="ts2"></param>
        /// <param name="length"></param>
        /// <param name="maxLag"></param>
        /// <remarks>
        /// XCorr defined as
        /// XCorr(d) = Sum{conj[ts1(t)]*ts2(t-d)}
        /// So peak at lag of d>0 means ts2 leads ts1
        /// </remarks>
        public void XCorrMag(double[] corrMag, Ipp64fc[] ts1, Ipp64fc[] ts2, int length, int maxLag) {
            int corrLength = length - 2 * maxLag;
            if (corrLength <= 0) {
                throw new ApplicationException("MaxLags must be less than half of total points.");
            }
            Ipp64fc zero, product;
            double sumsq1, sumsq2;
            zero.re = 0.0;
            zero.im = 0.0;
            Ipp64fc xcorr;
            DateTime startTime = DateTime.Now;
            sumsq1 = 0.0;
            sumsq2 = 0.0;
            for (int i = 0; i < 2 * maxLag + 1; i++) {
                xcorr = zero;
                //if (i == 0) {
                    sumsq1 = 0.0;
                    sumsq2 = 0.0;
                //}
                for (int j = 0; j < corrLength; j++) {
                    product = MultiplyConjFC(ts1[j + maxLag], ts2[j - i + 2 * maxLag]);
                    xcorr = AddFC(product, xcorr);
                    //if (i == 0) {
                        // sumsq1 is independent of i
                        sumsq1 += MultiplyConjFC(ts1[j + maxLag], ts1[j + maxLag]).re;
                        // sumsq2 is hopefully pretty constant with i
                        sumsq2 += MultiplyConjFC(ts2[j - i + 2 * maxLag], ts2[j - i + 2 * maxLag]).re;
                    //}
                }
                //xcorr.re /= corrLength;
                //xcorr.im /= corrLength;
                corrMag[i] = Math.Sqrt(xcorr.re * xcorr.re + xcorr.im * xcorr.im);
                corrMag[i] = corrMag[i] / Math.Sqrt(sumsq1) / Math.Sqrt(sumsq2);
            }

            DateTime endTime = DateTime.Now;
            TimeSpan duration = endTime - startTime;
            double seconds = duration.TotalSeconds;
        }

        public void XCorrRatio(double[] xcorrRatio, double[] xcorrMag, int maxlag) {
            int length = 2 * maxlag + 1;
            double[] flip = new double[length];
            double[] quotient = new double[length];
            fixed (double* psrc = xcorrMag, pdest = flip) {
                ipp.sp.ippsFlip_64f(psrc, pdest, length);
            }
            IppStatus status;
            fixed (double* psrc1 = xcorrMag, psrc2 = flip, pdest = quotient) {
                status = ipp.sp.ippsDiv_64f(psrc1, psrc2, pdest, length);
            }
            fixed (double* psrc = quotient, pdest = xcorrRatio) {
                status = ipp.sp.ippsLn_64f(psrc, pdest, length);
            }
        }

        /// <summary>
        /// Fit a straight line to the middle "fitpts" 
        ///     of the xCorrRatio array.
        /// </summary>
        /// <param name="xcorrRatio"></param>
        /// <param name="length"></param>
        /// <param name="fitpts"></param>
        /// <param name="M"></param>
        /// <param name="B"></param>
        public void XCorrRatioLSQFit(double[] xcorrRatio, int length, int fitpts, out double M, out double B) {
            M = 0;
            B = 0;
            int npts = fitpts;
            if (npts % 2 == 0) {
                // make odd 
                npts++;
            }
            while (npts > length) {
                npts -= 2;
            }
            double[] x = new double[npts];
            double[] y = new double[npts];
            int index;
            for (int i = 0; i < fitpts; i++) {
                index = i - fitpts / 2 + length / 2;
                x[i] = index;
                y[i] = xcorrRatio[index];
            }

            FindLinearLeastSquaresFit(x, y, fitpts, out M, out B);
        }


		public void FFT(double[] inArray, Ipp64fc[] outC, int FFTSize) {
			FFT_CCS(inArray, FFTSize);
			// convert packed output to complex array
			CCSToIpp64fc(OutputCCS, outC, FFTSize);
		}

		/// <summary>
		/// Computes Complex FFT of an array of real input data series.
		/// </summary>
		/// <param name="inArray"></param>
		/// <param name="FFTSize"></param>
		/// <param name="useExternalBuffer"></param>
		public void FFT(double[] inArray, Complex[] outC, int FFTSize) {

			FFT_CCS(inArray, FFTSize);
			// convert packed output to complex array
			CCSToComplex(OutputCCS, outC, FFTSize);

			// TODO do this in destructor:
			// free the memory for the specification structure
			//status = ipp.sp.ippsFFTFree_R_64f(pSpecs);
			//if (status != IppStatus.ippStsNoErr) {
			//	throw new ApplicationException("Error in FFT64 ippsFFTFree_R_64f: " + status.ToString());
			//}
		}

		/// <summary>
		/// Computes Complex FFT of an array of Complex input data series.
		/// </summary>
		/// <param name="inArray"></param>
		/// <param name="FFTSize"></param>
		/// <param name="useExternalBuffer"></param>
		public void FFT(Complex[] inArray, Complex[] outputC, int FFTSize) {

			if (inputfc == null) {
				inputfc = new Ipp64fc[FFTSize];
			}
			else if (inputfc.Length != FFTSize) {
                inputfc = null;
				inputfc = new Ipp64fc[FFTSize];
			}

			ComplexToIpp64fc(inArray, inputfc);

			if (outputfc == null) {
				outputfc = new Ipp64fc[FFTSize];
			}
			else if (outputfc.Length != FFTSize) {
                outputfc = null;
				outputfc = new Ipp64fc[FFTSize];
			}

			FFT(inputfc, outputfc, FFTSize);

			// convert IPP output to complex array
			Ipp64fcToComplex(outputfc, outputC);

			// free the memory for the specification structure
			//status = ipp.sp.ippsFFTFree_C_64fc(pSpec);
			//if (status != IppStatus.ippStsNoErr) {
			//	throw new ApplicationException("Error in FFT64 ippsFFTFree_R_64f: " + status.ToString());
			//}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="inArray"></param>
		/// <param name="outArray"></param>
		/// <param name="FFTSize"></param>
        public void FFT(Ipp64fc[] inArray, Ipp64fc[] outArray, int FFTSize) {
            // was specification array previously initialized to proper size?
            if (_FFTSizeCC != FFTSize) {
                InitCC(FFTSize);
            }

            bool useExternalBuffer = true;

            IppStatus status = IppStatus.ippStsNoErr;

            // compute FFT
            fixed (byte* pBuffer = _BufferC) {
                if (useExternalBuffer) {
                    status = ipp.sp.ippsFFTFwd_CToC_64fc(inArray, outArray, pSpecsC, pBuffer);
                }
                else {
                    status = ipp.sp.ippsFFTFwd_CToC_64fc(inArray, outArray, pSpecsC, null);
                }
                if (status != IppStatus.ippStsNoErr) {
                    throw new ApplicationException("Error in FFT64 ippsFFTFwd_RToCCS_64f: " + status.ToString());
                }
            }

        }

        /// <summary>
        /// Inverse FFT
        /// </summary>
        /// <param name="inArray"></param>
        /// <param name="outArray"></param>
        /// <param name="FFTSize"></param>
        public void InvFFT(Ipp64fc[] inArray, Ipp64fc[] outArray, int FFTSize) {
            // was specification array previously initialized to proper size?
            if (_FFTSizeCC != FFTSize) {
                InitCC(FFTSize);
            }

            bool useExternalBuffer = true;

            IppStatus status = IppStatus.ippStsNoErr;

            // compute FFT
            fixed (byte* pBuffer = _BufferC) {
                if (useExternalBuffer) {
                    status = ipp.sp.ippsFFTInv_CToC_64fc(inArray, outArray, pSpecsC, pBuffer);
                }
                else {
                    status = ipp.sp.ippsFFTInv_CToC_64fc(inArray, outArray, pSpecsC, null);
                }
                if (status != IppStatus.ippStsNoErr) {
                    throw new ApplicationException("Error in FFT64 ippsFFTFwd_RToCCS_64f: " + status.ToString());
                }
            }

        }

        public void DFT(Ipp64fc[] inArray, Ipp64fc[] outArray, int DFTSize) {
            // was specification array previously initialized to proper size?
            if (_DFTSizeCC != DFTSize) {
                InitDFTCC(DFTSize);
            }

            bool useExternalBuffer = true;

            IppStatus status = IppStatus.ippStsNoErr;

            // compute DFT
            fixed (byte* pBuffer = _BufferDFTC) {
                if (useExternalBuffer) {
                    status = ipp.sp.ippsDFTFwd_CToC_64fc(inArray, outArray, pDFTSpecsC, pBuffer);
                }
                else {
                    status = ipp.sp.ippsDFTFwd_CToC_64fc(inArray, outArray, pDFTSpecsC, null);
                }
                if (status != IppStatus.ippStsNoErr) {
                    throw new ApplicationException("Error in DFT ippsDFTFwd_CToC_64fc: " + status.ToString());
                }
            }

        }

        public void DFT(double[] inArray, Ipp64fc[] outArray, int DFTSize) {

            // DFT with real input must follow this pattern:
            /*
            double* pSrc;
            Ipp64fc[] pDst;
            double* pCCS;
            IppsDFTSpec_R_64f *pDFTSpec;

	        ipp.sp.ippsDFTInitAlloc_R_64f( &pDFTSpec, DFTSize, IPP_FFT_DIV_FWD_BY_N, ipp.IppHintAlgorithm.ippAlgHintFast );
	        ipp.sp.ippsDFTFwd_RToCCS_64f(pSrc, pCCS, pDFTSpec, null );
	        ipp.sp.ippsConjCcs_64fc(pCCS, pDst, DFTSize);
	        ipp.sp.ippsDFTFree_R_64f(pDFTSpec);
            */


            // was specification array previously initialized to proper size?
            if (_DFTSizeRC != DFTSize) {
                InitDFTRC(DFTSize);
            }

            bool useExternalBuffer = true;

            IppStatus status = IppStatus.ippStsNoErr;

            // compute DFT
            fixed (byte* pBuffer = _BufferDFTR) {
				fixed (double* pInput = inArray, pOutput = OutputDFTCCS) {
                    if (useExternalBuffer) {
                        status = ipp.sp.ippsDFTFwd_RToCCS_64f(pInput, pOutput, pDFTSpecsR, pBuffer);
                    }
                    else {
                        status = ipp.sp.ippsDFTFwd_RToCCS_64f(pInput, pOutput, pDFTSpecsR, null);
                    }
                    if (status != IppStatus.ippStsNoErr) {
                        throw new ApplicationException("Error in DFT ippsDFTFwd_RToCCS_64f: " + status.ToString());
                    }

                    // CSS array contains (N/2)+1 frequency points, starting with DC, in double[N+2] array,
                    //  in order {R0, I0, R1, I1, R2, ... R(N/2), I(N/2)}
                    int nOutPts = outArray.Length;
                    if (nOutPts >= DFTSize) {
                        // extend CCS array to full wrap-around complex array
                        status = ipp.sp.ippsConjCcs_64fc(pOutput, outArray, DFTSize);
                        if (status != IppStatus.ippStsNoErr) {
                            throw new ApplicationException("Error in DFT ippsConjCcs_64fc: " + status.ToString());
                        }
                    }
                    else if (nOutPts <= DFTSize / 2 + 1) {
                        // fill complex output array with first part (or all) of CCS array, don't extend to negative freq
                        for (int i = 0; i < nOutPts; i++) {
                            outArray[i].re = OutputDFTCCS[2 * i];
                            outArray[i].im = OutputDFTCCS[2 * i + 1];
                        }
                    }
                    else {
                        // complex output array is bigger than CSS array;
                        // just copy what we have; don't wrap 
                        for (int i = 0; i < (DFTSize / 2 + 1); i++) {
                            outArray[i].re = OutputDFTCCS[2 * i];
                            outArray[i].im = OutputDFTCCS[2 * i + 1];
                        }
                    }
                    
                }
            }
            int nhts = DFTSize / 2 + 1;
            for (int i = 0; i < nhts; i++) {
                
            }

        }

        public void InvDFT(Ipp64fc[] inArray, Ipp64fc[] outArray, int DFTSize) {
            // was specification array previously initialized to proper size?
            if (_DFTSizeCC != DFTSize) {
                InitDFTCC(DFTSize);
            }

            bool useExternalBuffer = true;

            IppStatus status = IppStatus.ippStsNoErr;

            // compute Inverse DFT
            fixed (byte* pBuffer = _BufferDFTC) {
                if (useExternalBuffer) {
                    status = ipp.sp.ippsDFTInv_CToC_64fc(inArray, outArray, pDFTSpecsC, pBuffer);
                }
                else {
                    status = ipp.sp.ippsDFTInv_CToC_64fc(inArray, outArray, pDFTSpecsC, null);
                }
                if (status != IppStatus.ippStsNoErr) {
                    throw new ApplicationException("Error in FFT64 ippsFFTFwd_RToCCS_64f: " + status.ToString());
                }
            }

        }

        public void PowerSpec(Ipp64fc[] inArray, double[] outArray, int size) {
			IppStatus status;
			fixed (double *pOutput = outArray) {
				status = ipp.sp.ippsPowerSpectr_64fc(inArray, pOutput, size);
			}
			int shift = size/2;
			double value;
			for (int ipt = 0; ipt < shift; ipt++) {
				// reorder spectral points
				value = outArray[ipt];
				outArray[ipt] = outArray[ipt + shift];
				outArray[ipt + shift] = value;
			}
		}

		/// <summary>
		/// Find total power of complex FFT by 
		///		summing the squared magnitude of all complex points.
		/// </summary>
		/// <param name="inArray"></param>
		/// <returns></returns>
		public double TotalPowerFFT(Ipp64fc[] inArray, int npts) {
			//int npts = inArray.Length;
			double[] magArray = new double[npts];
			double[] magSqArray = new double[npts];
			double[] sumArray = new double[1];
			fixed (double* pMag = magArray, pMagSqr = magSqArray, pSum = sumArray) {
				ipp.sp.ippsMagnitude_64fc(inArray, pMag, npts);
				ipp.sp.ippsSqr_64f(pMag, pMagSqr, npts);
				ipp.sp.ippsSum_64f(pMagSqr, npts, pSum);
				//ipp.sp.ippsMean_64f(pMagSqr, npts, pSum);
			}
			return sumArray[0];
		}

		/// <summary>
		/// Find total power in complex time series by
		///		computing variance (mean squared value)
		/// </summary>
		/// <param name="inArray"></param>
		/// <returns></returns>
		public double TotalPowerTS(Ipp64fc[] inArray, int npts) {
			//int npts = inArray.Length;
			double[] magArray = new double[npts];
			double[] magSqArray = new double[npts];
			double[] sumArray = new double[1];
			fixed (double* pMag = magArray, pMagSqr = magSqArray, pSum = sumArray) {
				ipp.sp.ippsMagnitude_64fc(inArray, pMag, npts);
				ipp.sp.ippsSqr_64f(pMag, pMagSqr, npts);
				//ipp.sp.ippsSum_64f(pMagSqr, npts, pSum);
				ipp.sp.ippsMean_64f(pMagSqr, npts, pSum);
			}
			return sumArray[0];
		}

		/// <summary>
		/// Find total power in a real time series
		///		computing variance (mean squared value)
		/// </summary>
		/// <param name="inArray"></param>
		/// <returns></returns>
		public double TotalPowerTS(double[] inArray, int npts) {
            if (npts == 0) {
                npts = inArray.Length;
            }
			double[] magSqArray = new double[npts];
			double[] sumArray = new double[1];
			fixed (double* pMagSqr = magSqArray, pSum = sumArray, pInput = inArray) {
				ipp.sp.ippsSqr_64f(pInput, pMagSqr, npts);
				ipp.sp.ippsMean_64f(pMagSqr, npts, pSum);
			}
			return sumArray[0];
		}

        public void ApplyHanningWindow(Ipp64fc[] inData, Ipp64fc[] outData, int npts) {
            if (npts == 0) {
                npts = inData.Length;
            }
            if ((HannCoefs == null) || (HannCoefs.Length != npts)) {
                HannCoefs = null;
				HannCoefs = new double[npts];
				fixed (double* pWin = HannCoefs) {
					ipp.sp.ippsSet_64f(1.0, pWin, npts);
					ipp.sp.ippsWinHann_64f_I(pWin, npts);
				}
			}
			for (int i = 0; i < npts; i++) {
				outData[i].re = inData[i].re * HannCoefs[i];
				outData[i].im = inData[i].im * HannCoefs[i];
			}
		}

        public void ApplyHanningWindow(double[] inData, double[] outData, int npts) {
            if (npts == 0) {
                npts = inData.Length;
            }
            if ((HannCoefs == null) || (HannCoefs.Length != npts)) {
                HannCoefs = null;
				HannCoefs = new double[npts];
				fixed (double* pWin = HannCoefs) {
					ipp.sp.ippsSet_64f(1.0, pWin, npts);
					ipp.sp.ippsWinHann_64f_I(pWin, npts);
				}
			}
			for (int i = 0; i < npts; i++) {
				outData[i] = inData[i] * HannCoefs[i];
			}
		}

		public void ApplyHammingWindow(Ipp64fc[] inData, Ipp64fc[] outData, int npts) {
            if (npts == 0) {
                npts = inData.Length;
            }
            if ((HammingCoefs == null) || (HammingCoefs.Length != npts)) {
                HammingCoefs = null;
				HammingCoefs = new double[npts];
				fixed (double* pWin = HammingCoefs) {
					ipp.sp.ippsSet_64f(1.0, pWin, npts);
					ipp.sp.ippsWinHamming_64f_I(pWin, npts);
				}
			}
			for (int i = 0; i < npts; i++) {
				outData[i].re = inData[i].re * HammingCoefs[i];
				outData[i].im = inData[i].im * HammingCoefs[i];
			}
		}

		public void ApplyHammingWindow(double[] inData, double[] outData, int npts) {
            if (npts == 0) {
                npts = inData.Length;
            }
            if ((HammingCoefs == null) || (HammingCoefs.Length != npts)) {
                HammingCoefs = null;
				HammingCoefs = new double[npts];
				fixed (double* pWin = HammingCoefs) {
					ipp.sp.ippsSet_64f(1.0, pWin, npts);
					ipp.sp.ippsWinHamming_64f_I(pWin, npts);
				}
			}
			for (int i = 0; i < npts; i++) {
				outData[i] = inData[i] * HammingCoefs[i];
			}
		}

		public void ApplyBlackmanWindow(Ipp64fc[] inData, Ipp64fc[] outData, int npts) {
            if (npts == 0) {
                npts = inData.Length;
            }
            if ((BlackCoefs == null) || (BlackCoefs.Length != npts)) {
                BlackCoefs = null;
				BlackCoefs = new double[npts];
				fixed (double* pWin = BlackCoefs) {
					ipp.sp.ippsSet_64f(1.0, pWin, npts);
					ipp.sp.ippsWinBlackmanStd_64f_I(pWin, npts);
				}
			}
			for (int i = 0; i < npts; i++) {
				outData[i].re = inData[i].re * BlackCoefs[i];
				outData[i].im = inData[i].im * BlackCoefs[i];
			}
		}

		public void ApplyBlackmanWindow(double[] inData, double[] outData, int npts) {
            if (npts == 0) {
                npts = inData.Length;
            }
            if ((BlackCoefs == null) || (BlackCoefs.Length != npts)) {
                BlackCoefs = null;
				BlackCoefs = new double[npts];
				fixed (double* pWin = BlackCoefs) {
					ipp.sp.ippsSet_64f(1.0, pWin, npts);
					ipp.sp.ippsWinBlackmanStd_64f_I(pWin, npts);
				}
			}
			for (int i = 0; i < npts; i++) {
				outData[i] = inData[i] * BlackCoefs[i];
			}
		}

		public void ApplyRieszWindow(Ipp64fc[] inData, Ipp64fc[] outData, int npts) {
            if (npts == 0) {
                npts = inData.Length;
            }
            double ff;
			if ((RieszCoefs == null) || (RieszCoefs.Length != npts)) {
                RieszCoefs = null;
				RieszCoefs = new double[npts];
				for (int i = 0; i < npts; i++) {
					ff = (i - npts / 2.0) / (npts / 2.0);
					RieszCoefs[i] = 1.0 - ff * ff;
				}
			}
			for (int i = 0; i < npts; i++) {
				outData[i].re = inData[i].re * RieszCoefs[i];
				outData[i].im = inData[i].im * RieszCoefs[i];
			}
		}

		public void ApplyRieszWindow(double[] inData, double[] outData, int npts) {
            if (npts == 0) {
                npts = inData.Length;
            }
            double ff;
			if ((RieszCoefs == null) || (RieszCoefs.Length != npts)) {
                RieszCoefs = null;
				RieszCoefs = new double[npts];
				for (int i = 0; i < npts; i++) {
					ff = (i - npts / 2.0) / (npts / 2.0);
					RieszCoefs[i] = 1.0 - ff * ff;
				}
			}
			for (int i = 0; i < npts; i++) {
				outData[i] = inData[i] * RieszCoefs[i];
			}
		}

		public void ApplyDCFilter(Ipp64fc[] data, int npts) {
            if (npts == 0) {
                npts = data.Length;
            }
            Ipp64fc[] sum = new Ipp64fc[2];
			ipp.sp.ippsSum_64fc(data, npts, sum);
			sum[0].re = sum[0].re / npts;
			sum[0].im = sum[0].im / npts;
			ipp.sp.ippsSubC_64fc_I(sum[0], data, npts);
		}

		public void ApplyDCFilter(double[] data, int npts) {
            if (npts == 0) {
                npts = data.Length;
            }
            double sum = 0.0;
			for (int i = 0; i < npts; i++) {
				sum += data[i];
			}
			sum = sum / npts;
			for (int i = 0; i < npts; i++) {
				data[i] -= sum;
			}
		}


		//
		#endregion  Public Methods

		private void FFT_CCS(double[] inArray, int FFTSize) {
			// was specification array previously initialized to proper size?
			if (_FFTSizeRC != FFTSize) {
				InitRC(FFTSize);
			}

			bool useExternalBuffer = true;

			IppStatus status = IppStatus.ippStsNoErr;

			// compute FFT
			fixed (byte* pBuffer = _BufferR) {
				fixed (double* pInput = inArray, pOutput = OutputCCS) {
					if (useExternalBuffer) {
						status = ipp.sp.ippsFFTFwd_RToCCS_64f(pInput, pOutput, pSpecsR, pBuffer);
					}
					else {
						status = ipp.sp.ippsFFTFwd_RToCCS_64f(pInput, pOutput, pSpecsR, null);
					}
					if (status != IppStatus.ippStsNoErr) {
						throw new ApplicationException("Error in FFT64 ippsFFTFwd_RToCCS_64f: " + status.ToString());
					}
				}
			}
		}

		/// <summary>
		/// Takes the CCS Format packed array and converts to the positive half of Complex array
		///		containing point indices from 0 to N/2+1 (DC to positive Nyquist)
		///		NOTE: Values are multiplied by SQRT(2) so that power in this half spectrum
		///		will equal the total power of input
		/// </summary>
		/// <param name="packedArray"></param>
		/// <param name="complexArray"></param>
		/// <param name="fftSize"></param>
		private static void CCSToComplex(double[] packedArray, Complex[] complexArray, int fftSize) {
			int inSize = packedArray.Length;
			int outSize = complexArray.Length;
			if (inSize != fftSize + 2) {
				throw new ApplicationException("In IntelMath, Packed to Complex array conversion error1");
			}
			if (outSize < fftSize / 2 + 1) {
				throw new ApplicationException("In IntelMath, Packed to Complex array conversion error2");
			}
			double sqrt2 = Math.Sqrt(2);
			for (int i = 0; i < fftSize / 2 + 1; i++) {
				complexArray[i].Real = sqrt2*packedArray[2 * i];
				complexArray[i].Imag = sqrt2*packedArray[(2 * i) + 1];
			}
			return;
			/*
			// do negative frequencies:
			for (int i = 0; i < outSize / 2 - 1; i++) {
				int nyq = outSize / 2;
				complexArray[nyq + 1 + i].Real = packedArray[outSize - 2 - (2 * i)];
				complexArray[nyq + 1 + i].Imag = -packedArray[outSize - 2 - (2 * i) + 1];
			}
			*/

		}

		/// <summary>
		/// Takes the CCS Format packed array and converts to the positive half of Complex array
		///		(Ipp64fc) containing point indices from 0 to N/2+1 (DC to positive Nyquist)
		///		NOTE: Values are multiplied by SQRT(2) so that power in this half spectrum
		///		will equal the total power of input
		/// </summary>
		/// <param name="packedArray"></param>
		/// <param name="complexArray"></param>
		/// <param name="fftSize"></param>
		private void CCSToIpp64fc(double[] packedArray, Ipp64fc[] complexArray, int fftSize) {
			int inSize = packedArray.Length;
			int outSize = complexArray.Length;
			if (inSize != fftSize + 2) {
				throw new ApplicationException("In IntelMath, Packed to Complex array conversion error1");
			}
			if (outSize < fftSize / 2 + 1) {
				throw new ApplicationException("In IntelMath, Packed to Complex array conversion error2");
			}
			double sqrt2 = Math.Sqrt(2);
			for (int i = 0; i < fftSize / 2 + 1; i++) {
				complexArray[i].re = sqrt2 * packedArray[2 * i];
				complexArray[i].im = sqrt2 * packedArray[(2 * i) + 1];
			}
			return;
		}


		public static void Ipp64fcToComplex(Ipp64fc[] IntelArray, Complex[] complexArray) {
			for (int i = 0; i < IntelArray.Length; i++) {
				complexArray[i].Real = IntelArray[i].re;
				complexArray[i].Imag = IntelArray[i].im;
			}
		}

		public static void ComplexToIpp64fc(Complex[] complexArray, Ipp64fc[] IntelArray) {
			for (int i = 0; i < complexArray.Length; i++) {
				IntelArray[i].re = complexArray[i].Real;
				IntelArray[i].im = complexArray[i].Imag;
			}
		}

        /// <summary>
        /// Multiplies two Ipp64fc complex scalars:
        /// mul1 * mul2
        /// </summary>
        /// <param name="mul1"></param>
        /// <param name="mul2"></param>
        /// <returns></returns>
        public static Ipp64fc MultiplyFC(Ipp64fc mul1, Ipp64fc mul2) {
            Ipp64fc prod;
            prod.re = (mul1.re * mul2.re) - (mul1.im * mul2.im);
            prod.im = (mul1.re * mul2.im) + (mul1.im * mul2.re);
            return prod;
        }

        /// <summary>
        /// Multiplies two Ipp64fc complex scalars:
        /// Conj(mul1) * mul2
        /// </summary>
        /// <param name="mul1"></param>
        /// <param name="mul2"></param>
        /// <returns></returns>
        public static Ipp64fc MultiplyConjFC(Ipp64fc mul1, Ipp64fc mul2) {
            Ipp64fc prod;
            prod.re = (mul1.re * mul2.re) + (mul1.im * mul2.im);
            prod.im = (mul1.re * mul2.im) - (mul1.im * mul2.re);
            return prod;
        }

        /// <summary>
        /// Adds 2 Ipp64fc complex scalars
        /// </summary>
        /// <param name="add1"></param>
        /// <param name="add2"></param>
        /// <returns></returns>
        public static Ipp64fc AddFC(Ipp64fc add1, Ipp64fc add2) {
            Ipp64fc sum;
            sum.re = add1.re + add2.re;
            sum.im = add1.im + add2.im;
            return sum;
        }

        //
        public static bool FindLinearLeastSquaresFit(double[] x, double[] y, int npts, out double m, out double b) {
            double sigM = 0.0;
            double sigB = 0.0;
            return FindLinearLeastSquaresFit(x, y, npts, out m, out b, out sigM, out sigB);
        }

        /// <summary>
        /// Computes coefficients m and b of best-fit straight line y = mx + b
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="npts"></param>
        /// <param name="m"></param>
        /// <param name="b"></param>
        /// <param name="sigM">probable uncertainty of m</param>
        /// <param name="sigB">probable uncertainty of b</param>
        /// <returns></returns>
        public static bool FindLinearLeastSquaresFit(double[] x, double[] y, int npts, out double m, out double b, out double sigM, out double sigB) {

            m = double.NaN;
            b = double.NaN;

            double Sx = 0;
            double Sy = 0;

            /*
            double Sxx = 0;
            double Sxy = 0;
            for (int i = 0; i < npts; i++) {                
                Sx += x[i];
                Sy += y[i];
                Sxx += x[i] * x[i];
                Sxy += x[i] * y[i];
            }

            // Solve for m and b.
            if (npts > 1 && Sxx > 0.0) {
                m = (Sxy * npts - Sx * Sy) / (Sxx * npts - Sx * Sx);
                b = (Sxx * Sy - Sx * Sxy) / (Sxx * npts - Sx * Sx);
                return true;
            }
            else {
                return false;
            }
             * */

            // Alternative method from Numerical Recipes
            //  (said to reduce round-off problems)

            double Stt = 0;
            m = 0;

            for (int i = 0; i < npts; i++) {
                Sx += x[i];
                Sy += y[i];
            }
            double Ss = (double)npts;
            double sxoss = Sx / Ss;

            double t;
            for (int i = 0; i < npts; i++) {
                t = x[i] - sxoss;
                Stt += t * t;
                m += t * y[i];
            }

            m /= Stt;
            b = (Sy - Sx * m) / Ss;

            sigM = Math.Sqrt(1.0/Stt);
            sigB = Math.Sqrt((1.0+Sx*Sx/(Ss*Stt))/Ss);

            double chi2 = 0;
            for (int i = 0; i < npts; i++) {
                chi2 += Math.Pow((y[i] - b - m * x[i]), 2.0);
            }

            double sigDat = Math.Sqrt(chi2/(npts-2));
            sigM *= sigDat;
            sigB *= sigDat;
            
            return true;

        }

        //
        // Implement IDispose interface
        //  Use it to dispose of unmanaged memory allocations
        //

        public void Dispose() {
            CleanUpManagedResources();
            CleanUpNativeResources();
            GC.SuppressFinalize(this);
        }

        protected virtual void CleanUpManagedResources() {
        }

        protected virtual void CleanUpNativeResources() {
            if (pSpecsC != null) {
                ipp.sp.ippsFFTFree_C_64fc(pSpecsC);
                pSpecsC = null;
            }
            if (pDFTSpecsC != null) {
                ipp.sp.ippsDFTFree_C_64fc(pDFTSpecsC);
                pDFTSpecsC = null;
            }
            if (pSpecsR != null) {
                ipp.sp.ippsFFTFree_R_64f(pSpecsR);
                pSpecsR = null;
            }
            if (pDFTSpecsR != null) {
                ipp.sp.ippsDFTFree_R_64f(pDFTSpecsR);
                pDFTSpecsR = null;
            }
        }

        ~IntelMath() {
            CleanUpNativeResources();
        }

        //private IppsFFTSpec_R_64f* pSpecsR;      // pointer to FFT specification structure
        //private IppsFFTSpec_C_64fc* pSpecsC;
        //private IppsDFTSpec_C_64fc* pDFTSpecsC;
        //private IppsDFTSpec_R_64f* pDFTSpecsR;



	}  // end class IntelMath

}
