using System;

using ipp;



namespace POPN {

	unsafe static class IntelIPP {

		// TODO:  Move this to DACarter.Utilities.Maths?
		//			Modify input to be single dim array
		//			Add output array
		//			Add CtoC version

		public static int IPP_FFT_DIV_FWD_BY_N = 1;
		public static int IPP_FFT_DIV_INV_BY_N = 2;
		public static int IPP_FFT_DIV_BY_SQRTN = 4;
		public static int IPP_FFT_NODIV_BY_ANY = 8;

		/// <summary>
		/// Computes Complex FFT of an array of real input data series.
		/// </summary>
		/// <param name="inArray"></param>
		/// <param name="FFTSize"></param>
		/// <param name="useExternalBuffer"></param>
		public static void FFT64_RC(double[] inArray, int FFTSize, bool useExternalBuffer) {

			IppStatus status = IppStatus.ippStsNoErr;
			int FFTOrder = 0;
			int size = FFTSize;
			while (size > 1) {
				FFTOrder++;
				size /= 2;
			}

			double[] Output64 = new double[FFTSize + 2];
						// Note: Output64 will have half of the
						//		complex transform.
						//		Output64[0],Output64[1] is Re,Im of DC
						//		Output64[FFTSize], Output64[FFTSize+1] is Re,Im of Nyquist.
						//		Negative freq pts go back toward 0 index,
						//		except that Im part is negated compared to
						//		the positive half of the transform, i.e.
						//		Re,Im of Nyq+1 is Output64[FFTSize/2-2],-Output64[FFTSize/2-1]
			Ipp64fc[] OutputC = new Ipp64fc[FFTSize];
						// Note: Ipp64fc is complex struture array
						//		with fields im and re
						//		OutputC[0] will be DC
						//		OutputC[FFTSize/2] is Nyquist

			// Create a FFT specification structure
			IppsFFTSpec_R_64f spec = new IppsFFTSpec_R_64f();
			IppsFFTSpec_R_64f* pSpec = &spec;

			// Initialize the spec structure and allocate memory for it
			status = ipp.sp.ippsFFTInitAlloc_R_64f(&pSpec,
													FFTOrder,
													IPP_FFT_DIV_FWD_BY_N,
													ipp.IppHintAlgorithm.ippAlgHintFast);
			if (status != IppStatus.ippStsNoErr) {
				throw new ApplicationException("Error in FFT64 ippsFFTInitAlloc_R_64f: " + status.ToString());
			}

			// compute size of external work buffer
			int BufferSize;
			int* pBufferSize = &BufferSize;
			status = ipp.sp.ippsFFTGetBufSize_R_64f(pSpec, pBufferSize);
			if (status != IppStatus.ippStsNoErr) {
				throw new ApplicationException("Error in FFT64 ippsFFTGetBufSize_R_64f: " + status.ToString());
			}
			byte[] Buffer = new byte[BufferSize];

			// loop of each FFT to do
			for (int i = 0; i < 1; i++) {


				// compute FFT
				fixed (byte* pBuffer = Buffer) {
					fixed (double* pInput = inArray, pOutput = Output64) {
						if (useExternalBuffer) {
							status = ipp.sp.ippsFFTFwd_RToCCS_64f(pInput, pOutput, pSpec, pBuffer);
						}
						else {
							status = ipp.sp.ippsFFTFwd_RToCCS_64f(pInput, pOutput, pSpec, null);
						}
						if (status != IppStatus.ippStsNoErr) {
							throw new ApplicationException("Error in FFT64 ippsFFTFwd_RToCCS_64f: " + status.ToString());
						}
						// convert packed output to complex array
						status = ipp.sp.ippsConjCcs_64fc(pOutput, OutputC, FFTSize);
						if (status != IppStatus.ippStsNoErr) {
							throw new ApplicationException("Error in FFT64 ippsConjCcs_64fc: " + status.ToString());
						}
					}
				}
			}  // end of for(i<dim0) loop

			// free the memory for the specification structure
			status = ipp.sp.ippsFFTFree_R_64f(pSpec);
			if (status != IppStatus.ippStsNoErr) {
				throw new ApplicationException("Error in FFT64 ippsFFTFree_R_64f: " + status.ToString());
			}
		}

	}
}
