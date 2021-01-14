using System;
using System.Collections.Generic;
using System.Text;


namespace DACarter.Utilities.Maths {

	/// <summary>
	/// Class that wraps various wavelet transforms.
	/// </summary>
	public class Wavelet {

		/// <summary>
		/// Compute coefficients for Daubechies wavelets of order N
		/// </summary>
		/// <param name="N">Wavelet Order (2-20)</param>
		/// <returns>N coefficients required by the wavelet transform.  Null if invalid N.</returns>
		public double[] DCoeffs(int N) {
			double[] coeffs = new double[N];
			if (N == 2) {
				coeffs[0] = 1.0;
				coeffs[1] = 1.0;
			}
			else if (N == 4) {
				coeffs[0] = (1.0 + Math.Sqrt(3.0)) / 4.0;
				coeffs[1] = (3.0 + Math.Sqrt(3.0)) / 4.0;
				coeffs[2] = (3.0 - Math.Sqrt(3.0)) / 4.0;
				coeffs[3] = (1.0 - Math.Sqrt(3.0)) / 4.0;
			}
			else if (N == 6) {
				double s = Math.Sqrt(5.0 + 2.0 * Math.Sqrt(10.0));
				coeffs[0] = (1.0 + Math.Sqrt(10.0) + s) / 16.0;
				coeffs[1] = (5.0 + Math.Sqrt(10.0) + 3.0 * s) / 16.0;
				coeffs[2] = (5.0 - Math.Sqrt(10.0) + s) / 8.0;
				coeffs[3] = (5.0 - Math.Sqrt(10.0) - s) / 8.0;
				coeffs[4] = (5.0 + Math.Sqrt(10.0) - 3.0 * s) / 16.0;
				coeffs[5] = (1.0 + Math.Sqrt(10.0) - s) / 16.0;
			}
			else if (N == 8) {
				coeffs[0] = 0.325803428051;
				coeffs[1] = 1.010945715092;
				coeffs[2] = 0.892200138246;
				coeffs[3] = -0.039575026236;
				coeffs[4] = -0.264507167369;
				coeffs[5] = 0.043616300475;
				coeffs[6] = 0.046503601071;
				coeffs[7] = -0.014986989330;
			}
			else if (N == 20) {
				coeffs[0] = 0.037717157593;
				coeffs[1] = 0.266122182794;
				coeffs[2] = 0.745575071487;
				coeffs[3] = 0.973628110734;
				coeffs[4] = 0.397637741770;
				coeffs[5] = -0.353336201794;
				coeffs[6] = -0.277109878720;
				coeffs[7] = 0.180127448534;
				coeffs[8] = 0.131602987102;
				coeffs[9] = -0.100966571196;
				coeffs[10] = -0.041659248088;
				coeffs[11] = 0.046969814097;
				coeffs[12] = 0.005100436968;
				coeffs[13] = -0.015179002335;
				coeffs[14] = 0.001973325365;
				coeffs[15] = 0.002817686590;
				coeffs[16] = -0.000969947840;
				coeffs[17] = -0.000164709006;
				coeffs[18] = 0.000132354366;
				coeffs[19] = -0.000018758416;
			}
			else {
				coeffs = null;
			}
			return coeffs;
		}  // end of method DCoeffs


		/// <summary>
		/// Compute the Daubechies wavelet transform using wavelets of order N
		/// </summary>
		/// <param name="f">Real input array.</param>
		/// <param name="N">Order of Daubechies wavelet (2-20)</param>
		/// <returns>Array of wavelet transform coefficients.</returns>
		public double[] TransformDN(double[] f, int N) {
			int M;	// number of points in data set (must be power of 2)
			int n;	// M = 2^n
			double[] coeffs;	// array of N Daubechies coefficients
			double[] clr;		// reversed array of coeffs
			double[] a;			// output array for wavelet transform

			M = f.Length;
			n = (int)(Math.Log10(M) / Math.Log10(2) + 0.5);
			coeffs = DCoeffs(N);
			clr = Fliplr(coeffs);
			for (int j = 0; j < N; j = j + 2) {
				clr[j] = -clr[j];
			}
			a = (double[])f.Clone();
			int[] K = new int[N];
			double[] z = new double[N];
			for (int k = n; k > 0; k--) {
				int m = (int)(Math.Pow(2.0, k - 1) + 0.5);
				double[] x = new double[m];
				double[] y = new double[m];
				for (int i = 0; i < m; i++) {
					for (int j = 0; j < N; j++) {
						K[j] = 2 * (i + 1) - 2 + (j + 1);
						while (K[j] > 2 * m) {
							K[j] = K[j] - 2 * m;
						}
					}
					for (int p = 0; p < N; p++) {
						z[p] = a[K[p] - 1];
					}
					x[i] = 0.0;
					for (int q = 0; q < N; q++) {
						x[i] += coeffs[q] * z[q];
						y[i] += clr[q] * z[q];
					}
				}
				for (int pp = 0; pp < m; pp++) {
					a[pp] = x[pp] / 2.0;
					a[pp + m] = y[pp] / 2.0;
				}
			}
			return a;
		}

		private double[] Fliplr(double[] c) {
			int N = c.Length;
			double[] clr = new double[N];
			for (int i = 0; i < N / 2; i++) {
				clr[i] = c[N - 1 - i];
				clr[N - 1 - i] = c[i];
			}
			return clr;
		}

		/// <summary>
		/// Inverse Daubechies wavelet transform.
		/// </summary>
		/// <param name="a">Array of wavelet transform.</param>
		/// <param name="N">Order of Daubechies wavelet (2-20).</param>
		/// <returns>Array of real time series.</returns>
		public double[] InvTransformDN(double[] a, int N) {
			int M;	// number of points in data set (must be power of 2)
			int n;	// M = 2^n
			double[] coeffs;	// array of N Daubechies coefficients
			double[] f;			// output array for wavelet inverse transform
			double[,] c1 = new double[2, N / 2];
			double[,] c2 = new double[2, N / 2];

			M = a.Length;
			n = (int)(Math.Log10(M) / Math.Log10(2) + 0.5);
			coeffs = DCoeffs(N);

			f = new double[M];
			f[0] = a[0];

			for (int j = 0; j < N / 2; j++) {
				c1[0, j] = -coeffs[2 * j + 1];
				c1[1, j] = coeffs[2 * j];
				c2[0, j] = coeffs[N - 2 * j - 2];
				c2[1, j] = coeffs[N - 2 * j - 1];
			}
			int[] K = new int[N / 2];
			double[] z = new double[N / 2];
			double[] zz = new double[N / 2];
			double[] x = new double[M];
			double[] xx = new double[M];
			for (int k = 0; k < n; k++) {
				int m = (int)(Math.Pow(2.0, (k)) + 0.5);
				for (int i = 0; i < m; i++) {
					for (int j = 0; j < N / 2; j++) {
						K[j] = m + (i + 1) - N / 2 + (j + 1);
						while (K[j] < m + 1) {
							K[j] += m;
						}
					}
					for (int p = 0; p < N / 2; p++) {
						z[p] = a[K[p] - 1];
						zz[p] = f[K[p] - 1 - m];
					}
					x[2 * i] = 0.0;
					x[2 * i + 1] = 0.0;
					xx[2 * i] = 0.0;
					xx[2 * i + 1] = 0.0;
					for (int q = 0; q < N / 2; q++) {
						x[2 * i] += c1[0, q] * z[q];
						x[2 * i + 1] += c1[1, q] * z[q];
						xx[2 * i] += c2[0, q] * zz[q];
						xx[2 * i + 1] += c2[1, q] * zz[q];
					}
				}
				for (int h = 0; h < 2 * m; h++) {
					f[h] = x[h] + xx[h];
				}
			}
			return f;
		} // end of InvTransformDN

	}  // end of class Wavelet

}  // end of namespace DACarter.Utilities.Maths

