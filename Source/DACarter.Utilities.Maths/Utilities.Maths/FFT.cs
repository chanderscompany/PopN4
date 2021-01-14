using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

using MathNet.Numerics;
using MathNet.Numerics.Transformations;

using DACarter.Utilities.Graphics;

namespace DACarter.Utilities.Maths {

	/// <summary>
	/// Class that wraps some FFT functionality from the MathNet Iridium library.
	/// </summary>
	public static class FFT {

        /// <summary>
        /// Determines whether "size" is an exact power of 2
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
		/// Does an "outplace" forward FFT
		///   Uses the MathNet forward FFT, which is an inplace transform.
		/// </summary>
		/// <param name="samples">The input data series</param>
		/// <param name="conv">The MathNet TransformConvention</param>
		/// <returns>The output complex transform.</returns>
		public static Complex[] Transform(Complex[] samples, TransformationConvention conv) {

			// set up the MathNet complex FFT class
			ComplexFourierTransformation CF = new ComplexFourierTransformation();
			CF.Convention = conv;

			// create the output transform array
			Complex[] tform = (Complex[])samples.Clone();

			CF.TransformForward(tform);

			return tform;
		}

		/// <summary>
		/// Does an "outplace" forward FFT.
		///   Since no MathNet TransformConvention is specified,
		///   a forward scaling of 1/N is used, which should preserve power.
		/// </summary>
		/// <param name="samples">The input data series</param>
		/// <returns>The output complex transform.</returns>
		public static Complex[] Transform(Complex[] samples) {

			Complex[] tform = Transform(samples, TransformationConvention.NoScaling);
			int N = tform.Length;
			for (int i = 0; i < N; i++) {
				tform[i] = tform[i] / N;
			}
			return tform;
		}

		/// <summary>
		/// Does an inplace forward FFT with 1/N scaling
		/// </summary>
		/// <param name="samples"></param>
		public static void TransformInPlace(Complex[] samples) {

			ComplexFourierTransformation CF = new ComplexFourierTransformation();
			CF.Convention = TransformationConvention.NoScaling;
			CF.TransformForward(samples);
			int N = samples.Length;
			for (int i = 0; i < N; i++) {
				samples[i] = samples[i] / N;
			}
		}

		/// <summary>
		/// Does an "outplace" inverse FFT
		///   Uses the MathNet backward FFT, which is an inplace transform.
		/// </summary>
		/// <param name="tform">The transform input array</param>
		/// <param name="conv">The MathNet TransformConvention</param>
		/// <returns>The output complex inverse transform.</returns>
		public static Complex[] InvTransform(Complex[] tform, TransformationConvention conv) {

			// set up the MathNet complex FFT class
			ComplexFourierTransformation CF = new ComplexFourierTransformation();
			CF.Convention = conv;

			// create the output transform array
			Complex[] data = (Complex[])tform.Clone();

			CF.TransformBackward(data);

			return data;
		}

		/// <summary>
		/// Does an "outplace" inverse FFT
		///   Uses the MathNet backward FFT, which is an inplace transform.
		///   Does no scaling on inverse transform, since scaling was assumed
		///		done during forward transform with FFT.Transform(samples).
		/// </summary>
		/// <param name="tform">The transform input array</param>
		/// <param name="conv">The MathNet TransformConvention</param>
		/// <returns>The output complex inverse transform.</returns>
		public static Complex[] InvTransform(Complex[] tform) {
			return InvTransform(tform, TransformationConvention.NoScaling);
		}

        public static double[] PowerSpectra(double[] data) {
            Complex[] timeSeries = new Complex[data.Length];
            for (int i = 0; i < data.Length; i++) {
                timeSeries[i].Real = data[i];
                timeSeries[i].Imag = 0.0;
            }
            return PowerSpectra(timeSeries);
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="timeSeries">Input timeseries is unaltered on output</param>
		/// <returns>The power spectrum with DC at the N/2 index</returns>
		public static double[] PowerSpectra(Complex[] timeSeries) {

			int nPts, nPad;
			bool isPadded = false;

			nPts = nPad = timeSeries.Length;
	//
			int n = (int)(Math.Log10(nPts) / Math.Log10(2) + 1.0e-10); // 2^n <= nPts
			if (nPts > (int)(Math.Pow(2.0, n) + 0.5)) {
				// if _nPts not power of two, then pad with zeros
				isPadded = true;
			}
			if (isPadded) {
				// pad with zeros to next power of 2 
				nPad = (int)(Math.Pow(2.0, n + 1) + 0.5);
			}

			Complex[] ts = new Complex[nPad];
			Array.Copy(timeSeries, ts, nPts);

			for (int i = nPts; i < nPad; i++) {
				ts[i] = 0.0;
			}
	//
	
			DCFilter( ts);
			HannWindow( ts);
			// correct power for Hann window:
			for (int i = 0; i < nPad; i++) {
				ts[i] *= 1.633;
			}
			TransformInPlace(ts);			// ts now contains transform
			Complex temp;
			for (int i = 0; i < nPad / 2; i++) {
				temp = ts[i];
				ts[i] = ts[i + nPad / 2];
				ts[i + nPad / 2] = temp;
			}
			return Mag2FromComplexArray(ts);
		}

		public static void QuickPlotSpectrum(double[] spec) {
			QuickPlotZ plot = new QuickPlotZ();
			int N = spec.Length;
			double[] x = new double[N];
			for (int i = 0; i < N; i++) {
				x[i] = i - N / 2;
			}
			plot.AddCurve("", x, spec, Color.Blue);
			plot.SetTitles("Power Spectrum", "Doppler Index", "Power");
			plot.Display();
		}


		/// <summary>
		/// Modifies the timeseries with a Hann window
		/// </summary>
		/// <param name="timeSeries"></param>
		public static void HannWindow( Complex[] timeSeries) {
			int N = timeSeries.Length;
			double pi2N = 2.0 * Math.PI / (N - 1);
			double func;
			for (int i = 0; i < N; i++) {
				//func = 0.53836 - 0.46164 * Math.Cos(pi2N * i);  // Hamming
				func = 0.5 - 0.5 * Math.Cos(pi2N * i);  // Hann
				timeSeries[i] = timeSeries[i] * func;
			}
			return;
		}

		/// <summary>
		/// Subtracts mean value from the time series;
		/// </summary>
		/// <param name="timeSeries"></param>
		public static void DCFilter( Complex[] timeSeries) {
			double sumI = 0.0;
			double sumQ = 0.0;
			for (int i = 0; i < timeSeries.Length; i++) {
				sumI += timeSeries[i].Real;
				sumQ += timeSeries[i].Imag;
			}
			double meanI = sumI / timeSeries.Length;
			double meanQ = sumQ / timeSeries.Length;
			for (int i = 0; i < timeSeries.Length; i++) {
				timeSeries[i].Real -= meanI;
				timeSeries[i].Imag -= meanQ;
			}
		}

		/// <summary>
		/// Compute noise level of spectrum via Hildebrand & Sekhon method.
		/// </summary>
		/// <param name="spectrum">The array of spectral points</param>
		/// <param name="navg">Number of spectral averages</param>
		/// <param name="numNoise">Number of points used in noise average</param>
		/// <param name="peakNoise">The highest valued point used in the noise</param>
		/// <returns></returns>
		public static double HSNoiseLevel(double[] spectrum, int navg, out int numNoise, out double peakNoise) {
			/*
			 *  Algorithm:
			 *      Uses the property of power spectra of gaussian signals that
			 *      variance = mean squared / navg ,
			 *      which can be expanded to
			 *      sumsq/n - mean*mean = mean*mean/navg ,
			 *      or simplifying
			 *      n*sumsq - sum*sum = sum*sum/navg ,
			 *      where sum and sumsq are the sums over
			 *      n spectral points of the spectral power
			 *      and of the power squared, respectively.
			 *              
			 *      After sorting the spectral points in
			 *      order of ascending power,
			 *      the algorithm goes through all values of n
			 *      from 1 to numpts and for each n computes the
			 *      values of both sides of the above equation.
			 *      For early values of n, the left side is
			 *      generally less than the right and then becomes
			 *      greater than the right side as higher-valued 
			 *      points that do not belong to the original
			 *      distribution are included in the n points.
			 *      The last crossing from less than to greater than
			 *      is considered the division between the desired set 
			 *      of points and "outliers" (e.g. noise vs. signal; 
			 *      noise or signal vs. interference, etc.).
			 *
			*/
			
			List<double> points = new List<double>();
			for (int i = 0; i < spectrum.Length; i++) {
				points.Add(spectrum[i]);
			}
			points.Sort();

			int np = 1;
			double sum = 0.0;
			double sumsq = 0.0;
			double val;
			double rhs, lhs;
			for (int i = 0; i < spectrum.Length; i++) {
				val = spectrum[i];
				sum += val;
				sumsq += val*val;
				lhs = i * sumsq - sum * sum;
				rhs = sum * sum / navg;
				if (lhs < rhs) {
					np = i+1;
				}
			}

			double noiseLevel = 0.0;
			for (int i = 0; i < np; i++) {
				noiseLevel += points[i];
			}
			noiseLevel = noiseLevel / np;
			numNoise = np;
			peakNoise = points[np - 1];

			return noiseLevel;
		}

		/// <summary>
		/// Compute noise level of spectrum via Hildebrand & Sekhon method.
		/// </summary>
		/// <param name="spectrum"></param>
		/// <param name="navg"></param>
		/// <returns></returns>
		public static double HSNoiseLevel(double[] spectrum, int navg) {
			int numNoise;
			double peakNoise;
			return HSNoiseLevel(spectrum, navg, out numNoise, out peakNoise);
		}

		//
		// Helper functions
		// Convert between real/imag arrays and Complex array
		// TODO: May want to move out of FFT class eventually.
		// 

		public static Complex[] ComplexFromRealArray(double[] real) {
			Complex[] c = new Complex[real.Length];
			for (int i = 0; i < real.Length; i++) {
				c[i] = Complex.FromRealImaginary(real[i], 0.0);
			}
			return c;
		}

		public static Complex[] ComplexFromRealImagArrays(double[] real, double[] imag) {
			int len = Math.Min(real.Length, imag.Length);
			Complex[] c = new Complex[len];
			for (int i = 0; i < len; i++) {
				c[i] = Complex.FromRealImaginary(real[i], imag[i]);
			}
			return c;
		}

		public static double[] RealFromComplexArray(Complex[] cmplx) {
			double[] real = new double[cmplx.Length];
			for (int i = 0; i < cmplx.Length; i++) {
				real[i] = cmplx[i].Real;
			}
			return real;
		}

		public static double[] ImagFromComplexArray(Complex[] cmplx) {
			double[] imag = new double[cmplx.Length];
			for (int i = 0; i < cmplx.Length; i++) {
				imag[i] = cmplx[i].Imag;
			}
			return imag;
		}

		public static double[] MagFromComplexArray(Complex[] cmplx) {
			double[] mag = new double[cmplx.Length];
			for (int i = 0; i < cmplx.Length; i++) {
				mag[i] = cmplx[i].Modulus;
			}
			return mag;
		}

		public static double[] Mag2FromComplexArray(Complex[] cmplx) {
			double[] mag2 = new double[cmplx.Length];
			for (int i = 0; i < cmplx.Length; i++) {
				mag2[i] = cmplx[i].ModulusSquared;
			}
			return mag2;
		}


	}
}
