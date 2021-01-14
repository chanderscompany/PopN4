using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

using DACarter.Utilities.Graphics;

using MathNet.Numerics;
using MathNet.Numerics.Transformations;


namespace DACarter.Utilities.Maths { 

	/// <summary>
	/// Class that wraps various wavelet transforms.
	/// </summary>
	public static class Wavelet {


		/// <summary>
		/// Computes harmonic wavelet transform
		/// </summary>
		/// <param name="f">Input data array</param>
		/// <returns>Array of coefficients</returns>
		public static Complex[] HarmonicTransform(Complex[] f) {
			int N;	// number of points in data set (must be power of 2)
			int n;	// N = 2^n
			Complex[] a;			// output array for wavelet transform
			Complex[] F;

			N = f.Length;
			n = (int)(Math.Log10(N) / Math.Log10(2) + 0.5);

			a = new Complex[N];

			//ComplexFourierTransformation fftc = new ComplexFourierTransformation();
			//fftc.Convention = TransformationConvention.Matlab;
			//fftc.TransformForward(f);
			F = FFT.Transform(f);
			a[0] = F[0];
			a[1] = F[1];
			for (int j = 1; j <= n - 2; j++) {
				int len = (int)(Math.Pow(2.0, j) + 0.5);
				int starti = len;
				int endi = (int)(Math.Pow(2.0, j + 1) + 0.5);
				Complex[] partF = new Complex[len];
				Complex[] parta;
				for (int i = 0; i < len; i++) {
					partF[i] = F[starti + i];
				}
				parta = FFT.InvTransform(partF);
				for (int i = 0; i < len; i++) {
					a[starti + i] = parta[i];
				}
				starti = N - endi + 1;
				for (int i = 0; i < len; i++) {
					partF[i] = F[starti + i];
				}
				parta = Fliplr(FFT.Transform(Fliplr(partF)));
				for (int i = 0; i < len; i++) {
					a[starti + i] = parta[i] * len;
				}

			}

			a[N / 2] = F[N / 2];
			a[N - 1] = F[N - 1];

			return a;
		}

		public static Complex[] InvHarmonicTransform(Complex[] a) {
			int N;	// number of points in data set (must be power of 2)
			int n;	// N = 2^n
			Complex[] F;			// internal temp array
			Complex[] f;			// output array for inverse transform

			N = a.Length;
			n = (int)(Math.Log10(N) / Math.Log10(2) + 0.5);

			F = new Complex[N];

			F[0] = a[0];
			F[1] = a[1];
			for (int j = 1; j <= n - 2; j++) {
				int len = (int)(Math.Pow(2.0, j) + 0.5);
				int starti = len;
				int endi = (int)(Math.Pow(2.0, j + 1) + 0.5);
				Complex[] parta = new Complex[len];
				Complex[] partF;
				for (int i = 0; i < len; i++) {
					parta[i] = a[starti + i];
				}
				partF = FFT.Transform(parta);
				for (int i = 0; i < len; i++) {
					F[starti + i] = partF[i];
				}
				starti = N - endi + 1;
				for (int i = 0; i < len; i++) {
					parta[i] = a[starti + i];
				}
				partF = Fliplr(FFT.InvTransform(Fliplr(parta)));
				for (int i = 0; i < len; i++) {
					F[starti + i] = partF[i] / len;
				}

			}

			F[N / 2] = a[N / 2];
			F[N - 1] = a[N - 1];
			f = FFT.InvTransform(F);

			return f;
		}


		private static Complex[] Fliplr(Complex[] c) {
			int N = c.Length;
			Complex[] clr = new Complex[N];
			for (int i = 0; i < N / 2; i++) {
				clr[i] = c[N - 1 - i];
				clr[N - 1 - i] = c[i];
			}
			return clr;
		}



	}  // end of class Wavelet


	// ************************************************************************
	/// <summary>
	/// ContinuousWavelet Class
	/// </summary>
	public class ContinuousWavelet {

		public Complex[,] Wavelet;
		public double[] Scale;
		public double[] Period;
		private int _nPts;
		private double _dt;
		private bool _isPadded;
		private int _param;
		private double[] _input;
		private int _npad;
		private string _title, _xlabel, _ylabel;

		public ContinuousWavelet(double[] y, bool pad, double dt, int param) {
			Init(y, pad, dt, param);
		}

		public ContinuousWavelet(double[] y, bool pad, double dt) {
			Init(y, pad, dt);
		}

		public ContinuousWavelet(double[] y, bool pad) {
			Init(y, pad);
		}

		public ContinuousWavelet(double[] y) {
			Init(y);
		}

		/// <summary>
		/// Init function called by constructor.
		/// Can also be called by user to reinitialize the parameters
		///   before doing a new transform.
		/// </summary>
		/// <param name="y"></param>
		/// <param name="pad"></param>
		/// <param name="dt"></param>
		/// <param name="param"></param>
		public void Init(double[] y, bool pad, double dt, int param) {
			_Init(y, pad, dt, param);
		}

		public void Init(double[] y, bool pad, double dt) {
			_Init(y, pad, dt, 6);
		}

		public void Init(double[] y, bool pad) {
			_Init(y, pad, 1.0, 6);
		}

		public void Init(double[] y) {
			_Init(y, false, 1.0, 6);
		}

		private void _Init(double[] y, bool pad, double dt, int param) {
			_input = y;
			_dt = dt;
			_param = param;
			if (param < 4) {
				param = 6;
			}

			_isPadded = pad;
			_nPts = _npad = _input.Length;
			double ff = Math.Log10(_nPts) / Math.Log10(2.0);
			int n = (int)(Math.Log10(_nPts) / Math.Log10(2) + 1.0e-10); // 2^n <= nPts
			if (_nPts > (int)(Math.Pow(2.0, n) + 0.5)) {
				// if _nPts not power of two, then pad with zeros
				_isPadded = true;
			}
			if (_isPadded) {
				// pad with zeros to next power of 2 
				_npad = (int)(Math.Pow(2.0, n + 1) + 0.5);
			}
			_title = "C-Wavelet Transform Power";
			_xlabel = "Time";
			_ylabel = "Log of Scale Size";
		}

		/// <summary>
		/// Computes the continuous Morlet wavelet transform.
		/// </summary>
		/// <remarks>
		//
		/// The following routines for Continuous Wavelet Transform
		///	were adopted from FORTRAN code of
		///	Torrence and Campo, 1998
		///	http://paos.colorado.edu/research/wavelets/
		///	  Copyright (C) 1998, Christopher Torrence
		/// 
		/// </remarks>
		public void Transform() {

			double s0 = _dt;		// smallest scale
			double dj = 0.25;	// spacing between discrete scales
			int jtot = 1 + (int)((int)(Math.Log((double)_nPts * _dt / s0, 2.0) + 0.5) / dj);	//the number of scales

			Wavelet = new Complex[_nPts, jtot];
			Scale = new double[jtot];
			Period = new double[jtot];

			Complex[] yfft = new Complex[_npad];
			double[] kwave = new double[_npad];

			// remove mean
			double ymean = 0.0;
			for (int i = 0; i < _nPts; i++) {
				ymean = ymean + _input[i];
			}
			ymean = ymean / _nPts;
			for (int i = 0; i < _nPts; i++) {
				yfft[i] = _input[i] - ymean;
			}

			// pad with extra zeroes
			for (int i = _nPts; i < _npad; i++) {
				yfft[i] = 0.0;
			}

			// find the FFT of the time series
			ComplexFourierTransformation CFFT = new ComplexFourierTransformation();
			CFFT.Convention = TransformationConvention.NoScaling;
			CFFT.TransformForward(yfft);
			for (int i = 0; i < _npad; i++) {
				yfft[i] = yfft[i] / _npad;
			}

			// construct the wavenumber array
			double freq1 = 2.0 * Math.PI / (double)(_npad * _dt);
			kwave[0] = 0.0;
			for (int i = 1; i < _npad / 2 + 1; i++) {
				kwave[i] = i * freq1;
			}
			for (int i = _npad / 2 + 1; i < _npad; i++) {
				kwave[i] = -kwave[_npad - i];
			}

			// main wavelet loop
			for (int j = 0; j < jtot; j++) {
				double period1;
				Complex[] daughter;
				Scale[j] = s0 * Math.Pow(2.0, (double)(j * dj));
				waveFunction(_npad, _dt, _param, Scale[j], kwave, out period1, out daughter);
				Period[j] = period1;
				// multiply the daughter by the time-series FFT
				for (int i = 0; i < _npad; i++) {
					daughter[i] = daughter[i] * yfft[i];
				}
				// inverse
				CFFT.TransformBackward(daughter);
				// store the wavelet transform, discard zero-padding at end
				for (int i = 0; i < _nPts; i++) {
					Wavelet[i, j] = daughter[i];
				}
			}
		}

		/// <summary>
		/// Plot 2-D graph of continuous wavelet transform
		/// </summary>
		/// <param name="useContours">If true, do contour plot; if false, do box plot.</param>
		public void Plot(bool useContours) {

			double[] x = new double[_nPts];
			for (int k = 0; k < _nPts; k++) {
				x[k] = k*_dt;
			}
			double[,] zc = new double[_nPts, Scale.Length];
			//double[,] temp = new double[Scale.Length, _nPts];  // for debugging
			double[] yc = new double[Scale.Length];
			double[] maxX = new double[Scale.Length];
			double maxValRow, maxValAll=0.0;
			for (int j = 0; j < Scale.Length; j++) {
				yc[j] = Math.Log10(Scale[j]);
				maxValRow = 0.0;
				maxX[j] = 0;
				for (int i = 0; i < _nPts; i++) {
					zc[i, j] = Wavelet[i, j].Real * Wavelet[i, j].Real + 
								Wavelet[i, j].Imag * Wavelet[i, j].Imag;
					//zc[i, j] = Math.Log10(zc[i,j]);
					if (zc[i, j] > maxValRow) {
						maxValRow = zc[i, j];
						maxX[j] = i*_dt;
						if (maxValRow > maxValAll) {
							maxValAll = maxValRow;
						}
					}
				}
			}

			double[] maxY = new double[_nPts];
			double maxValCol;
			for (int i = 0; i < _nPts; i++) {
				maxY[i] = 0.0;
				maxValCol = 0.0;
				for (int j = 0; j < Scale.Length; j++) {
					if (zc[i,j] > maxValCol) {
						maxValCol = zc[i, j];
						maxY[i] = yc[j];
					}
				}
			}

			QuickPlotZ plotCwv = new QuickPlotZ();
			ColorScale colorScale4 = new ColorScale(0.0, 1.0, ColorScale.ColorScheme.RainbowETL_52, false);
			colorScale4.ExtendLower = false;
			colorScale4.RepeatUpper = false;
			colorScale4.BackgroundColor = Color.WhiteSmoke;
			colorScale4.MinValue = 0.0;
			colorScale4.ContourStep = 0.01*maxValAll;
			plotCwv.GraphControl.GraphPane.YAxis.Scale.IsReverse = true;  // must call before BoxPlot()
			if (useContours) {
				plotCwv.ColorContourPlot(x, yc, zc, colorScale4, true);
			}
			else {
				plotCwv.ColorBoxPlot(x, yc, zc, colorScale4, true);
			}
			//ZedGraph.LineItem lic = plotCwv.AddCurve("MaxX", maxX, yc, System.Drawing.Color.White, ZedGraph.SymbolType.Circle);
			Color symbolColor = Color.FromArgb(33, Color.Yellow);
			ZedGraph.LineItem lic2 = plotCwv.AddCurve("", x, maxY, symbolColor, ZedGraph.SymbolType.XCross);
			lic2.Symbol.Size = 1.0f;
			lic2.Line.Width = 1.0f;

			plotCwv.SetTitles(_title, _xlabel, _ylabel);
			plotCwv.Display();
		}

		/// <summary>
		/// Sets labels for plot.
		///		Use null arguments to keep default label.
		/// </summary>
		/// <param name="title"></param>
		/// <param name="xlabel"></param>
		/// <param name="ylabel"></param>
		public void SetPlotLabels(string title, string xlabel, string ylabel) {
			if (title != null) {
				_title = title;
			}
			if (xlabel != null) {
				_xlabel = xlabel;
			}
			if (ylabel != null) {
				_ylabel = ylabel;
			}
		}


		private void waveFunction(int nk, double dt, int param, double scale1, double[] kwave,
										out double period1, out Complex[] daughter) {
			daughter = new Complex[nk];

			Complex norm;
			double expnt, fourierFactor;

			norm = Math.Sqrt(2.0 * Math.PI * scale1 / dt) * (Math.Pow(Math.PI, -0.25));
			for (int k = 0; k <= nk / 2; k++) {
				expnt = -0.5 * (scale1 * kwave[k] - param) * (scale1 * kwave[k] - param);
				daughter[k] = norm * Math.Exp(expnt);
			}
			for (int k = nk / 2 + 1; k < nk; k++) {
				daughter[k] = Complex.Zero;
			}
			fourierFactor = 4.0 * Math.PI / (param + Math.Sqrt(2.0 + param * param));
			period1 = scale1 * fourierFactor;

		}
	}  // end of ContinuousWavelet class


	// ************************************************************************
	/// <summary>
	/// DaubeshiesWavelet class
	/// </summary>
	public class DaubeshiesWavelet {

		public double[] Wavelet;				// output array for wavelet transform
		public double[] InverseWavelet;			// output array for wavelet inverse transform
		private string _title, _xlabel, _x2label, _ylabel;
		private int _order;
		private double _sampleTime;

		public DaubeshiesWavelet() {
			Init();
			_sampleTime = 1.0;
		}

		public DaubeshiesWavelet(double sampleTime) {
			Init();
			_sampleTime = sampleTime;
		}

		private void Init() {
			_title = "Daubeshies Wavelet Transform Power";
			_xlabel = "Time (seconds)";
			_x2label = "Time (points)";
			_ylabel = "Wavelet Level";
			_order = 20;
		}

		/// <summary>
		/// Compute the Daubechies wavelet transform using wavelets of order N
		/// </summary>
		/// <param name="f">Real input array.</param>
		/// <param name="N">Order of Daubechies wavelet (2-20), i.e. # coeffs in mother wavelet.</param>
		/// <returns>Array of wavelet transform coefficients.</returns>
		public double[] Transform(double[] f, int N) {
			_order = N;
			int M;	// number of points in data set (must be power of 2)
			int n;	// M = 2^n
			double[] coeffs;	// array of N Daubechies coefficients
			double[] clr;		// reversed array of coeffs

			M = f.Length;
			n = (int)(Math.Log10(M) / Math.Log10(2) + 1.0e-10);	// M >= 2^n
			coeffs = DCoeffs(N);
			clr = Fliplr(coeffs);
			for (int j = 0; j < N; j = j + 2) {
				clr[j] = -clr[j];
			}
			int nPts = (int)(Math.Pow(2.0, n) + 0.5);
			Wavelet = new double[nPts]; ;
			Array.Copy(f, Wavelet, nPts);
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
						z[p] = Wavelet[K[p] - 1];
					}
					x[i] = 0.0;
					for (int q = 0; q < N; q++) {
						x[i] += coeffs[q] * z[q];
						y[i] += clr[q] * z[q];
					}
				}
				for (int pp = 0; pp < m; pp++) {
					Wavelet[pp] = x[pp] / 2.0;
					Wavelet[pp + m] = y[pp] / 2.0;
				}
			}
			return Wavelet;
		}


		/// <summary>
		/// Inverse Daubechies wavelet transform.
		/// </summary>
		/// <param name="a">Array of wavelet transform.</param>
		/// <param name="N">Order of Daubechies wavelet (2-20).</param>
		/// <returns>Array of real time series.</returns>
		public double[] InverseTransform( ) {
			int M;	// number of points in data set (must be power of 2)
			int n;	// M = 2^n
			int N = _order;
			double[] coeffs;	// array of N Daubechies coefficients
			double[,] c1 = new double[2, N / 2];
			double[,] c2 = new double[2, N / 2];

			M = Wavelet.Length;
			n = (int)(Math.Log10(M) / Math.Log10(2) + 0.5);
			coeffs = DCoeffs(N);

			InverseWavelet = new double[M];
			InverseWavelet[0] = Wavelet[0];

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
						z[p] = Wavelet[K[p] - 1];
						zz[p] = InverseWavelet[K[p] - 1 - m];
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
					InverseWavelet[h] = x[h] + xx[h];
				}
			}
			return InverseWavelet;
		} // end of InvTransform


		/// <summary>
		/// Compute coefficients for Daubechies wavelets of order N
		/// </summary>
		/// <param name="N">Wavelet Order (2-20)</param>
		/// <returns>N coefficients required by the wavelet transform.  Null if invalid N.</returns>
		private static double[] DCoeffs(int N) {
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
				throw new Exception("Invalid value for order of Daubechies wavelet. Use 2,4,6,8,or 20.");
			}
			return coeffs;
		}  // end of method DCoeffs

		private  double[] Fliplr(double[] c) {
			int N = c.Length;
			double[] clr = new double[N];
			for (int i = 0; i < N / 2; i++) {
				clr[i] = c[N - 1 - i];
				clr[N - 1 - i] = c[i];
			}
			return clr;
		}  // end of method Fliplr


		public void Plot(bool useContours) {

			// single line plot
			QuickPlotZ plotCwv = new QuickPlotZ();
			double[] x = new double[Wavelet.Length];
			for (int i = 0; i < Wavelet.Length; i++) {
				x[i] = (double)i;
			}
			ZedGraph.LineItem liH3 = plotCwv.AddCurve("D-Wavelet Coeffs", x, Wavelet, System.Drawing.Color.Blue, ZedGraph.SymbolType.XCross);
			liH3.Symbol.Size = 3.0f;
			plotCwv.Display();

			// 2-d plot
			QuickPlotZ plotHwv2 = new QuickPlotZ();
			int nPoints = Wavelet.Length;
			int nScales = (int)(Math.Log10(nPoints) / Math.Log10(2) + 1.5);
			double[,] zh = new double[nPoints, nScales];
			double[] xh = new double[nPoints];
			//double[] xt = new double[nPoints];
			double[] y = new double[nScales];
			int nC = 1;	// number of coeffs at a scale
			int nP;		// number of repetitions of coeff at a scale
			//int iP;		// counter of repeated coeffs
			int iC = 0;
			for (int iS = 0; iS < nScales; iS++) {
				y[iS] = iS - 1;
				nP = nPoints / nC;  // number of times to repeat coeff
				int ix = 0;
				for (int j = 0; j < nC; j++) {
					for (int iP = 0; iP < nP; iP++) {
						zh[ix++, iS] = Wavelet[iC] * Wavelet[iC];
					}
					iC++;	// move to next coeff
				}
				nC = (int)(Math.Pow(2.0, iS) + 0.5);	// next number of coeffs for next scale (1,1,2,4,8...)
			}
			for (int k = 0; k < nPoints; k++) { 
				xh[k] = k * _sampleTime;
			}
			if (useContours) {
				ColorScale colorScale = new ColorScale(0.0, 1.0, ColorScale.ColorScheme.RainbowETL_52, false);
				colorScale.ExtendLower = false;
				colorScale.RepeatUpper = false;
				colorScale.BackgroundColor = Color.WhiteSmoke;
				colorScale.MinValue = 0.0;
				//colorScale.ContourStep = 0.01;
				plotHwv2.ColorContourPlot(xh, y, zh, colorScale, true);
			}
			else {
				plotHwv2.ColorBoxPlot(xh, y, zh, new ColorScale(0.0, 10.0, ColorScale.ColorScheme.RainbowETL_52, true), true);
			}

			plotHwv2.SetTitles(_title, _xlabel, _ylabel);

			plotHwv2.GraphControl.GraphPane.X2Axis.IsVisible = true;
			plotHwv2.GraphControl.GraphPane.X2Axis.MajorTic.IsOpposite = false;
			plotHwv2.GraphControl.GraphPane.X2Axis.MinorTic.IsOpposite = false;
			plotHwv2.GraphControl.GraphPane.XAxis.MajorTic.IsOpposite = false;
			plotHwv2.GraphControl.GraphPane.XAxis.MinorTic.IsOpposite = false;
			plotHwv2.GraphControl.GraphPane.X2Axis.Scale.Min = 0.0;
			plotHwv2.GraphControl.GraphPane.X2Axis.Scale.Max = nPoints;
			plotHwv2.GraphControl.GraphPane.X2Axis.Title.Text = _x2label;
			plotHwv2.GraphControl.GraphPane.X2Axis.AxisGap = 100.0f;


			plotHwv2.Display();
		}
	}  // end of class DaubechiesWavelet

	// ************************************************************************
	/// <summary>
	/// Class HarmonicWavelet
	/// </summary>
	public class HarmonicWavelet {

		public Complex[] Wavelet;  // output array of wavelet transform coefficients
		public Complex[] InverseWavelet;

		private string _title, _xlabel, _ylabel;
		private int _nPts, _npad;
		private bool _isPadded;
		private Complex[] xx;

		public HarmonicWavelet() {
			_title = "Harmonic Wavelet Transform Power";
			_xlabel = "Time (points)";
			_ylabel = "Wavelet Level";
		}

		public Complex[] Transform(Complex[] input) {
			int n;	// Npts = 2^n
			Complex[] F;

			_isPadded = false;
			_nPts = _npad = input.Length;
			n = (int)(Math.Log10(_nPts) / Math.Log10(2) + 1.0e-10); // 2^n <= nPts
			if (_nPts > (int)(Math.Pow(2.0, n) + 0.5)) {
				// if _nPts not power of two, then pad with zeros
				_isPadded = true;
			}
			if (_isPadded) {
				// pad with zeros to next power of 2 
				_npad = (int)(Math.Pow(2.0, n + 1) + 0.5);
			}

			// f[] is working array: input padded with zeros
			Complex[] f = new Complex[_npad];
			for (int i = 0; i < _nPts; i++) {
				f[i] = input[i];
			}
			for (int i = _nPts; i < _npad; i++) {
				f[i] = 0.0;
			}

			Wavelet = new Complex[_npad];

			//FFT.DCFilter(f);

			//ComplexFourierTransformation fftc = new ComplexFourierTransformation();
			//fftc.Convention = TransformationConvention.Matlab;
			//fftc.TransformForward(f);
			F = FFT.Transform(f);
			Wavelet[0] = F[0];
			Wavelet[1] = F[1];
			for (int j = 1; j <= n - 2; j++) {
				int len = (int)(Math.Pow(2.0, j) + 0.5);
				int starti = len;
				int endi = (int)(Math.Pow(2.0, j + 1) + 0.5);
				Complex[] partF = new Complex[len];
				Complex[] parta;
				for (int i = 0; i < len; i++) {
					partF[i] = F[starti + i];
				}
				parta = FFT.InvTransform(partF);
				for (int i = 0; i < len; i++) {
					Wavelet[starti + i] = parta[i];
				}
				starti = _nPts - endi + 1;
				for (int i = 0; i < len; i++) {
					partF[i] = F[starti + i];
				}
				parta = Fliplr(FFT.Transform(Fliplr(partF)));
				for (int i = 0; i < len; i++) {
					Wavelet[starti + i] = parta[i] * len;
				}

			}

			Wavelet[_nPts / 2] = F[_nPts / 2];
			Wavelet[_nPts - 1] = F[_nPts - 1];

			return Wavelet;
		}

		public  Complex[] InverseTransform() {
			int N;	// number of points in data set (must be power of 2)
			int n;	// N = 2^n
			Complex[] F;			// internal temp array

			N = Wavelet.Length;
			n = (int)(Math.Log10(N) / Math.Log10(2) + 0.5);

			F = new Complex[N];

			F[0] = Wavelet[0];
			F[1] = Wavelet[1];
			for (int j = 1; j <= n - 2; j++) {
				int len = (int)(Math.Pow(2.0, j) + 0.5);
				int starti = len;
				int endi = (int)(Math.Pow(2.0, j + 1) + 0.5);
				Complex[] parta = new Complex[len];
				Complex[] partF;
				for (int i = 0; i < len; i++) {
					parta[i] = Wavelet[starti + i];
				}
				partF = FFT.Transform(parta);
				for (int i = 0; i < len; i++) {
					F[starti + i] = partF[i];
				}
				starti = N - endi + 1;
				for (int i = 0; i < len; i++) {
					parta[i] = Wavelet[starti + i];
				}
				partF = Fliplr(FFT.InvTransform(Fliplr(parta)));
				for (int i = 0; i < len; i++) {
					F[starti + i] = partF[i] / len;
				}

			}

			F[N / 2] = Wavelet[N / 2];
			F[N - 1] = Wavelet[N - 1];
			InverseWavelet = FFT.InvTransform(F);

			return InverseWavelet;
		}

		public void Plot(bool useContours) {

			// single line plot
			QuickPlotZ plotHwv = new QuickPlotZ();
			double[] x = new double[Wavelet.Length];
			for (int i = 0; i < Wavelet.Length; i++) {
				x[i] = (double)i;
			}
			ZedGraph.LineItem liH3 = plotHwv.AddCurve("Mag H-Wavelet Coeffs", x, FFT.MagFromComplexArray(Wavelet), System.Drawing.Color.Blue, ZedGraph.SymbolType.XCross);
			liH3.Symbol.Size = 3.0f;
			plotHwv.Display();

			// 2-d plot
			QuickPlotZ plotHwv2 = new QuickPlotZ();
			int nPoints = Wavelet.Length;
			int nScales = (int)(Math.Log10(nPoints) / Math.Log10(2) + 1.5);
			double[,] zh = new double[nPoints, nScales];
			double[] xh = new double[nPoints];
			double[] y = new double[nScales];
			int nC = 1;	// number of coeffs at a scale
			int nP;		// number of repetitions of coeff at a scale
			//int iP;		// counter of repeated coeffs
			int iC = 0;
			for (int iS = 0; iS < nScales; iS++) {
				y[iS] = iS - 1;
				nP = nPoints / nC;
				int ix = 0;
				for (int j = 0; j < nC; j++) {
					for (int iP = 0; iP < nP; iP++) {
						zh[ix++, iS] = Wavelet[iC].Real * Wavelet[iC].Real + Wavelet[iC].Imag * Wavelet[iC].Imag;
					}
					iC++;	// move to next coeff
				}
				nC = (int)(Math.Pow(2.0, iS) + 0.5);	// next number of coeffs for next scale (1,1,2,4,8...)
			}
			for (int k = 0; k < nPoints; k++) {
				xh[k] = k;
			}
			if (useContours) {
				ColorScale colorScale = new ColorScale(0.0, 1.0, ColorScale.ColorScheme.RainbowETL_52, false);
				colorScale.ExtendLower = false;
				colorScale.RepeatUpper = false;
				colorScale.BackgroundColor = Color.WhiteSmoke;
				colorScale.MinValue = 0.0;
				//colorScale.ContourStep = 0.01;
				plotHwv2.ColorContourPlot(xh, y, zh, colorScale, true);
			}
			else {
				plotHwv2.ColorBoxPlot(xh, y, zh, new ColorScale(0.0, 10.0, ColorScale.ColorScheme.RainbowETL_52, true), true);
			}

			plotHwv2.SetTitles(_title, _xlabel, _ylabel);

			plotHwv2.Display();
		}

		/// <summary>
		/// Sets labels for plot.
		///		Use null arguments to keep default label.
		/// </summary>
		/// <param name="title"></param>
		/// <param name="xlabel"></param>
		/// <param name="ylabel"></param>
		public void SetPlotLabels(string title, string xlabel, string ylabel) {
			if (title != null) {
				_title = title;
			}
			if (xlabel != null) {
				_xlabel = xlabel;
			}
			if (ylabel != null) {
				_ylabel = ylabel;
			}
		}

		private Complex[] Fliplr(Complex[] c) {
			int N = c.Length;
			Complex[] clr = new Complex[N];
			for (int i = 0; i < N / 2; i++) {
				clr[i] = c[N - 1 - i];
				clr[N - 1 - i] = c[i];
			}
			return clr;
		}

	}  // end of class HarmonicWavelet

}  // end of namespace DACarter.Utilities.Maths

