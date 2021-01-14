using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

using DACarter.Utilities.Graphics;
using ZedGraph;
using MathNet.Numerics;
using MathNet.Numerics.Transformations;


namespace DACarter.Utilities.Maths { 

	/// <summary>
	/// Class that wraps various wavelet transforms.
	/// </summary>
	public class Wavelet {


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
		public Complex[] InverseWavelet;
		public double[] Scale;
		public double[] Period;
		private Complex[] _input;
		private int _nPts;			// number of original input data points
		private int _npad;			// number of input points after padding with zeros
		private int _nscales;		// number of scale sizes 
		private double _dt;			// sample interval
		private double _s0;			// smallest scale
		private double _dj;			// spacing between discrete scales
		private bool _isPadded;
		private int _param;
		private string _title, _xlabel, _ylabel;
		private QuickPlotZ _plotCwv;

		public ContinuousWavelet(Complex[] y, bool pad, double dt, int param) {
			Init(y, pad, dt, param);
		}

		public ContinuousWavelet(Complex[] y, bool pad, double dt) {
			Init(y, pad, dt);
		}

		public ContinuousWavelet(Complex[] y, bool pad) {
			Init(y, pad);
		}

		public ContinuousWavelet(Complex[] y) {
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
		public void Init(Complex[] y, bool pad, double dt, int param) {
			_Init(y, pad, dt, param);
		}

		public void Init(Complex[] y, bool pad, double dt) {
			_Init(y, pad, dt, 6);
		}

		public void Init(Complex[] y, bool pad) {
			_Init(y, pad, 1.0, 6);
		}

		public void Init(Complex[] y) {
			_Init(y, false, 1.0, 6);
		}

		private void _Init(Complex[] y, bool pad, double dt, int param) {
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
		/// see BAMS, 1998:
		/// http://ams.allenpress.com/archive/1520-0477/79/1/pdf/i1520-0477-79-1-61.pdf
		/// 
		/// </remarks>
		public void Transform() {

			_s0 = 2*_dt;		// smallest scale
			_dj = 0.25;		// spacing between discrete scales
			_nscales = 1 + (int)((int)(Math.Log((double)_nPts * _dt / _s0, 2.0) + 0.5) / _dj);	//the number of scales

			Wavelet = new Complex[_nPts, _nscales];
			Scale = new double[_nscales];
			Period = new double[_nscales];

			Complex[] yfft = new Complex[_npad];
			double[] kwave = new double[_npad];

			// remove mean
			Complex ymean = Complex.Zero;
			for (int i = 0; i < _nPts; i++) {
				ymean = ymean + _input[i];
			}
			ymean = ymean / _nPts;
			for (int i = 0; i < _nPts; i++) {
				yfft[i] = _input[i] - ymean;
			}

			// pad with extra zeroes
			for (int i = _nPts; i < _npad; i++) {
				yfft[i] = Complex.Zero;
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
			for (int k = 0; k < _npad / 2 + 1; k++) {
				kwave[k] = k * freq1;
			}
			for (int k = _npad / 2 + 1; k < _npad; k++) {
				//kwave[k] = -kwave[_npad - k];
				//*****************************************
				// modified by dac 2007.9.13 from T&Campo
				kwave[k] = k * freq1 - Math.PI / _dt;
				//*****************************************
			}

			// main wavelet loop
			for (int j = 0; j < _nscales; j++) {
				double period1;
				Complex[] daughter;
				Scale[j] = _s0 * Math.Pow(2.0, (double)(j * _dj));
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

		private void waveFunction(int nk, double dt, int param, double scale1, double[] kwave,
										out double period1, out Complex[] daughter) {
			daughter = new Complex[nk];

			Complex norm;
			double expnt, fourierFactor;

			norm = Math.Sqrt(2.0 * Math.PI * scale1 / dt) * (Math.Pow(Math.PI, -0.25));

			// *********** modified by dac 2007.9.13 *****************
			// ***  computed daughter over all nk points *************
			// ***  rather than nk/2
			//for (int k = 0; k <= nk / 2; k++) {
			for (int k = 0; k < nk; k++) {
				expnt = -0.5 * (scale1 * kwave[k] - param) * (scale1 * kwave[k] - param);
				daughter[k] = norm * Math.Exp(expnt);
			}
			/*
			for (int k = nk / 2 + 1; k < nk; k++) {
				daughter[k] = Complex.Zero;
			}
			*/
			// ********************************************************
			
			fourierFactor = 4.0 * Math.PI / (param + Math.Sqrt(2.0 + param * param));
			period1 = scale1 * fourierFactor;

		}

		/// <summary>
		/// Computes the inverse continuous transform
		/// </summary>
		public void InverseTransform() {
			InverseWavelet = new Complex[_nPts];
			for (int i = 0; i < _nPts; i++) {
				InverseWavelet[i] = Complex.Zero;
				double factor = _dj*Math.Sqrt(_dt)/0.776/(Math.Pow(Math.PI, -0.25));
				double fudge = 0.5;
				for (int j = 0; j < _nscales; j++) {
					InverseWavelet[i] += fudge*factor*Wavelet[i, j]/Math.Sqrt(Scale[j]);
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

			_plotCwv = new QuickPlotZ();
			ColorScale colorScale4 = new ColorScale(0.0, 1.0, ColorScale.ColorScheme.RainbowETL_52, false);
			colorScale4.ExtendLower = false;
			colorScale4.RepeatUpper = false;
			colorScale4.BackgroundColor = Color.WhiteSmoke;
			colorScale4.MinValue = 0.0;
			colorScale4.ContourStep = 0.01*maxValAll;
			_plotCwv.GraphControl.GraphPane.YAxis.Scale.IsReverse = true;  // must call before BoxPlot()
			if (useContours) {
				_plotCwv.ColorContourPlot(x, yc, zc, colorScale4, true);
			}
			else {
				_plotCwv.ColorBoxPlot(x, yc, zc, colorScale4, true);
			}
			//ZedGraph.LineItem lic = plotCwv.AddCurve("MaxX", maxX, yc, System.Drawing.Color.White, ZedGraph.SymbolType.Circle);
			Color symbolColor = Color.FromArgb(33, Color.Yellow);
			ZedGraph.LineItem lic2 = _plotCwv.AddCurve("", x, maxY, symbolColor, ZedGraph.SymbolType.XCross);
			lic2.Symbol.Size = 1.0f;
			lic2.Line.Width = 1.0f;

			_plotCwv.SetTitles(_title, _xlabel, _ylabel);
			_plotCwv.Display();

			// Plot inverse transform
			/*
			if (InverseWavelet != null) {
				QuickPlotZ plotICW = new QuickPlotZ();
				double[] xi = new double[InverseWavelet.Length];
				for (int i = 0; i < InverseWavelet.Length; i++) {
					xi[i] = (double)i * _dt;
				}
				ZedGraph.LineItem curveI = plotICW.AddCurve("Inverse CWavelet I", xi, FFT.RealFromComplexArray(InverseWavelet), Color.Blue, ZedGraph.SymbolType.None);
				ZedGraph.LineItem curveQ = plotICW.AddCurve("Inverse CWavelet Q", xi, FFT.ImagFromComplexArray(InverseWavelet), Color.Lime, ZedGraph.SymbolType.None);
				plotICW.Display();
			}
			*/

			// Plot wavelet coeffs as stacked plot
			QuickPlotZ plotStack1 = new QuickPlotZ();
			plotStack1.GraphControl.GraphPane.YAxis.Scale.IsReverse = true; 
			plotStack1.StackedPlot(x, yc, zc, false, true);
			plotStack1.Display();
		}

		public Complex WaveletAtSecLogScale(double sec, double logScale) {
			double x, y, scale;
			// convert seconds to x point number
			x = sec / _dt;
			// convert log of scale to y point number
			scale = Math.Pow(10.0, logScale);
			y = Math.Log10(scale / _s0) / Math.Log10(2) / _dj;
			int ix = (int)(Math.Floor(x + 0.5));
			int iy = (int)(Math.Floor(y + 0.5));
			ix = (ix < 0) ? 0 : ix;
			ix = (ix >= _nPts) ? _nPts - 1 : ix;
			iy = (iy < 0) ? 0 : iy;
			iy = (iy >= _nscales) ? _nscales - 1 : iy;
			return Wavelet[ix, iy];
		}

		public int XSecIndex(double sec) {
			double x;
			// convert seconds to x point number
			x = sec / _dt;
			int ix = (int)(Math.Floor(x + 0.5));
			ix = (ix < 0) ? 0 : ix;
			ix = (ix >= _nPts) ? _nPts - 1 : ix;
			return ix;
		}

		public int YLogScaleIndex(double logScale) {
			double y, scale;
			// convert log of scale to y point number
			scale = Math.Pow(10.0, logScale);
			y = Math.Log10(scale / _s0) / Math.Log10(2) / _dj;
			int iy = (int)(Math.Floor(y + 0.5));
			iy = (iy < 0) ? 0 : iy;
			iy = (iy >= _nscales) ? _nscales - 1 : iy;
			return iy;
		}

		public int YScaleIndex(double scale) {
			double y;
			// convert scale to y point number
			y = Math.Log10(scale / _s0) / Math.Log10(2) / _dj;
			int iy = (int)(Math.Floor(y + 0.5));
			iy = (iy < 0) ? 0 : iy;
			iy = (iy >= _nscales) ? _nscales - 1 : iy;
			return iy;
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

		/// <summary>
		/// property to return the current displayed YAxis max scale value
		/// </summary>
		public double YAxisMax {
			get {
				if (_plotCwv != null) {
					return _plotCwv.GraphControl.GraphPane.YAxis.Scale.Max;
				}
				else {
					return(0.0);
				}
			}
		}

		public double YAxisMin {
			get {
				if (_plotCwv != null) {
					return _plotCwv.GraphControl.GraphPane.YAxis.Scale.Min;
				}
				else {
					return (0.0);
				}
			}
		}

		public double XAxisMax {
			get {
				if (_plotCwv != null) {
					return _plotCwv.GraphControl.GraphPane.XAxis.Scale.Max;
				}
				else {
					return (0.0);
				}
			}
		}

		public double XAxisMin {
			get {
				if (_plotCwv != null) {
					return _plotCwv.GraphControl.GraphPane.XAxis.Scale.Min;
				}
				else {
					return (0.0);
				}
			}
		}

	}  // end of ContinuousWavelet class


	// ************************************************************************
	/// <summary>
	/// DaubechiesWavelet class
	/// </summary>
	public class DaubechiesWavelet {

		public double[] Wavelet;				// output array for wavelet transform
		public double[] FilteredWavelet;
		public double[] InverseWavelet;			// output array for wavelet inverse transform
		public int NumLevels;				// number of levels of wavelets
		public int[] LevelIndexStart;		// the index for starting coeff at each level
		public int[] LevelNumCoefs;			// the number of coeffs in each level

		private string _title, _xlabel, _x2label, _ylabel;
		private int _nPts, _nPad;
		private int _nCoeffs;
		private double _sampleTime;
		private QuickPlotZ _plotDwv;
        private int _iSmallestLevel;


		public DaubechiesWavelet() {
			Init();
		}


		private void Init() {
			_title = "Daubechies Wavelet Transform Power";
			_xlabel = "Time (seconds)";
			_x2label = "Time (points)";
			_ylabel = "Wavelet Level";
			_nCoeffs = 20;
		}

        public double[] Transform(double[] f, int N) {
            return Transform(f, N, 1.0);
        }

        public double[] TransformXM(double[] f, int N) {
            return TransformXM(f, N, 1.0);
        }

        /// <summary>
		/// Compute the Daubechies wavelet transform using wavelets of order N
		/// </summary>
		/// <param name="f">Real input array.</param>
		/// <param name="N">Order of Daubechies wavelet (2-20), i.e. # coeffs in mother wavelet.</param>
		/// <returns>Array of wavelet transform coefficients.</returns>
		public double[] Transform(double[] f, int N, double sampleTime) {
			_nCoeffs = N;
			_sampleTime = sampleTime;
			int M;	// number of points in data set (must be power of 2)
			int n;	// M = 2^n
			double[] coeffs;	// array of N Daubechies coefficients
			double[] clr;		// reversed array of coeffs

			M = f.Length;
			n = (int)(Math.Log10(M) / Math.Log10(2) + 1.0e-10);	// M >= 2^n
            _nPts = (int)(Math.Pow(2.0, n) + 0.5);  // truncates to power of 2 length

			coeffs = DCoeffs(N);
			clr = Fliplr(coeffs);
			for (int j = 01; j < N; j = j + 2) {
				clr[j] = -clr[j];
			}

			Wavelet = new double[_nPts];
			Array.Copy(f, Wavelet, _nPts);
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
			if (FilteredWavelet == null) {
				FilteredWavelet = new double[_nPts];
			}
			// make copy to modify coeffs
			Wavelet.CopyTo(FilteredWavelet,0);

			// number of levels of wavelets
			NumLevels = (int)(Math.Log10(_nPts) / Math.Log10(2) + 1.5);
			LevelIndexStart = new int[NumLevels];
			LevelNumCoefs = new int[NumLevels];
			LevelNumCoefs[0] = 1;		// first level (-1) has one coeff
			LevelIndexStart[0] = 0;
			for (int i = 1; i < NumLevels; i++) {
				// levels 0,1,2,3... have 1,2,4,8... coeffs
				LevelNumCoefs[i] = (int)(Math.Pow(2.0, i-1) + 0.5);
				LevelIndexStart[i] = LevelIndexStart[i-1] + LevelNumCoefs[i-1];
			}

			return Wavelet;
		}

        		/// <summary>
		/// Compute the Daubechies wavelet transform using wavelets of order N
        /// *** Using method from LapXM code ***
        /// *** Only does order = 20 ***
        /// *** Assumes(?) input array is length 1024 or greater ***
		/// </summary>
		/// <param name="f">Real input array.</param>
		/// <param name="N">Order of Daubechies wavelet (2-20), i.e. # coeffs in mother wavelet.</param>
		/// <returns>Array of wavelet transform coefficients.</returns>
        public double[] TransformXM(double[] f, int N, double sampleTime) {

            N = 20;
            _nCoeffs = N;
            _sampleTime = sampleTime;
            int M;	// number of points in data set (must be power of 2)
            int n;	// M = 2^n
            double[] coeffs;	// array of N Daubechies coefficients
            double[] clr;		// reversed array of coeffs

            M = f.Length;
            n = (int)(Math.Log10(M) / Math.Log10(2) + 1.0e-10);	// M >= 2^n
            _nPts = (int)(Math.Pow(2.0, n) + 0.5);
            Wavelet = new double[_nPts];

            coeffs = DCoeffs(N);
            double sqrt2 = Math.Sqrt(2.0);
            for (int i = 0; i < N; i++) {
                coeffs[i] /= sqrt2;
            }
            clr = Fliplr(coeffs);
            for (int j = 01; j < N; j = j + 2) {
                clr[j] = -clr[j];
            }

            Array.Copy(f, Wavelet, _nPts);

            // use LapXM variable names
            int iLength = _nPts;
            _iSmallestLevel = 4;
            int iNCoeff = N;
            double[] vecCC = coeffs;
            double[] vecCR = clr;
            int iIoff = -(iNCoeff >> 1); // Handle wrap-around of wavelets. iIoff and iJoff are
            int iJoff = -(iNCoeff >> 1); // here identical to center the 'support' of wavelets.
            int iNMod, iNN1, iNH;
            int iNI, iNJ, iJF, iJR;
            double dAi, dAi1;
            double[] vecWksp;

            for (int iNN = iLength; iNN >= _iSmallestLevel; iNN >>= 1) {
                vecWksp = null;
                vecWksp = new double[iNN];
                iNMod = iNCoeff * iNN;
                iNN1 = iNN - 1;
                iNH = iNN >> 1;
                for (int ii = 0, i = 0; i < iNN; i += 2, ii++) {
                    iNI = i + 1 + iNMod + iIoff;
                    iNJ = i + 1 + iNMod + iJoff;
                    for (int k = 0; k < iNCoeff; k++) {
                        iJF = iNN1 & (iNI + k + 1);
                        iJR = iNN1 & (iNJ + k + 1);
                        if (ii + iNH == 1023) {
                            int b = 0;
                        }
                        vecWksp[ii] += vecCC[k] * Wavelet[iJF];
                        vecWksp[ii + iNH] += vecCR[k] * Wavelet[iJR];
                    }
                }
                for (int i = 0; i < iNN; i++) {
                    Wavelet[i] = vecWksp[i];
                }
            }

            return Wavelet;
        }



		/// <summary>
		/// Inverse Daubechies wavelet transform.
		/// </summary>
		/// <returns>Array of real time series.</returns>
		public double[] InverseTransform( ) {
			int M;	// number of points in data set (must be power of 2)
			int n;	// M = 2^n
			int N = _nCoeffs;
			double[] coeffs;	// array of N Daubechies coefficients
			double[,] c1 = new double[2, N / 2];
			double[,] c2 = new double[2, N / 2];

			M = Wavelet.Length;
			n = (int)(Math.Log10(M) / Math.Log10(2) + 0.5);
			coeffs = DCoeffs(N);
            double sqrt2 = Math.Sqrt(2.0);
            for (int i = 0; i < N; i++) {
                coeffs[i] /= sqrt2;
            }

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
        /// 
        /// </summary>
        /// <returns></returns>
        public double[] InverseTransformXM() {

            int M;	// number of points in data set (must be power of 2)
            int n;	// M = 2^n
            int N = _nCoeffs;
            double[] coeffs;	// array of N Daubechies coefficients
            double[] clr;		// reversed array of coeffs
            double[,] c1 = new double[2, N / 2];
            double[,] c2 = new double[2, N / 2];

            M = _nPts;
            n = (int)(Math.Log10(M) / Math.Log10(2) + 0.5);
            coeffs = DCoeffs(N);
            double sqrt2 = Math.Sqrt(2.0);
            for (int i = 0; i < N; i++) {
                coeffs[i] /= sqrt2;
            }
            clr = Fliplr(coeffs);
            for (int j = 01; j < N; j = j + 2) {
                clr[j] = -clr[j];
            }

            // use LapXM variable names
            int iLength = _nPts;
            _iSmallestLevel = 4;
            int iNCoeff = N;
            double[] vecCC = coeffs;
            double[] vecCR = clr;
            int iIoff = -(iNCoeff >> 1); // Handle wrap-around of wavelets. iIoff and iJoff are
            int iJoff = -(iNCoeff >> 1); // here identical to center the 'support' of wavelets.
            int iNMod, iNN1, iNH;
            int iNI, iNJ, iJF, iJR;
            double dAi, dAi1;
            double[] vecWksp;

            InverseWavelet = new double[_nPts];
            Array.Copy(Wavelet, InverseWavelet, _nPts);

            for (int iNN = _iSmallestLevel; iNN <= iLength; iNN <<= 1) {
                vecWksp = null;
                vecWksp = new double[iNN];
                iNMod = iNCoeff * iNN;
                iNN1 = iNN - 1;
                iNH = iNN >> 1;
                for (int ii = 0, i = 0; i < iNN; i += 2, ii++) {
                    dAi = InverseWavelet[ii];
                    dAi1 = InverseWavelet[ii + iNH];
                    iNI = i + 1 + iNMod + iIoff;
                    iNJ = i + 1 + iNMod + iJoff;
                    for (int k = 0; k < iNCoeff; k++) {
                        iJF = iNN1 & (iNI + k + 1);
                        iJR = iNN1 & (iNJ + k + 1);
                        vecWksp[iJF] += vecCC[k] * dAi;
                        vecWksp[iJR] += vecCR[k] * dAi1;
                    }
                }
                for (int i = 0; i < iNN; i++) {
                    InverseWavelet[i] = vecWksp[i];
                }
            }

            return InverseWavelet;

        }




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

		/// <summary>
		/// Make plots of D-wavelet
		/// </summary>
		/// <param name="useContours"></param>
		public void Plot(bool useContours) {

			int nPoints = Wavelet.Length;
			//int nC = 1;	// number of coeffs at a level
			int iC = 0;
			/*
			for (int iS = 0; iS < NumLevels; iS++) {
				int levelNumber = iS - 1;
				int ix = 0;
				for (int j = 0; j < nC; j++) {
					//Wavelet[iC];
					iC++;	// move to next coeff
				}
				nC = (int)(Math.Pow(2.0, iS) + 0.5);	// next number of coeffs for next scale (1,1,2,4,8...)
			}
			*/

			// single line plot
			_plotDwv = new QuickPlotZ();
			double[] x = new double[Wavelet.Length];
			for (int i = 0; i < Wavelet.Length; i++) {
				x[i] = (double)i;
			}
			//ZedGraph.LineItem liH3 = _plotDwv.AddCurve("D-Wavelet Coeffs", x, Wavelet, System.Drawing.Color.Blue, ZedGraph.SymbolType.XCross);
			//liH3.Symbol.Size = 3.0f;

			double[][] xL = new double[NumLevels][];
			double[][] yL = new double[NumLevels][];
			ZedGraph.LineItem[] curve = new ZedGraph.LineItem[NumLevels];
			for (int i = 0; i < NumLevels; i++) {
				xL[i] = new double[LevelNumCoefs[i]];
				yL[i] = new double[LevelNumCoefs[i]];
				for (int j = 0; j < LevelNumCoefs[i]; j++) {
					xL[i][j] = LevelIndexStart[i] + j;
					yL[i][j] = Wavelet[LevelIndexStart[i] + j];
				}
				if (i%3 == 0) {
					curve[i] = _plotDwv.AddCurve("D"+_nCoeffs+" Lev" + (i - 1), xL[i], yL[i], System.Drawing.Color.Red, ZedGraph.SymbolType.Circle);
				}
				else if (i % 3 == 1) {
					curve[i] = _plotDwv.AddCurve("D" + _nCoeffs + " Lev" + (i - 1), xL[i], yL[i], System.Drawing.Color.Blue, ZedGraph.SymbolType.Circle);
				}
				else {
					curve[i] = _plotDwv.AddCurve("D" + _nCoeffs + " Lev" + (i - 1), xL[i], yL[i], System.Drawing.Color.Lime, ZedGraph.SymbolType.Circle);
				}
				curve[i].Symbol.Size = 3.0f;
			}
			_plotDwv.SetTitles("Daubechies Wavelet Coefficients", "", "");
			_plotDwv.Display();

			// 2-d plot
			//int nScales = (int)(Math.Log10(nPoints) / Math.Log10(2) + 1.5);
			double[,] zh = new double[nPoints, NumLevels];
			double[] xh = new double[nPoints];
			//double[] xt = new double[nPoints];
			double[] y = new double[NumLevels];
			//nC = 1;	// number of coeffs at a level
			int nP;		// number of repetitions of coeff at a level
			//int iP;		// counter of repeated coeffs
			iC = 0;
			for (int iL = 0; iL < NumLevels; iL++) {
				y[iL] = iL - 1;
				nP = nPoints / LevelNumCoefs[iL];  // number of times to repeat coeff
				int ix = 0;
				for (int j = 0; j < LevelNumCoefs[iL]; j++) {
					for (int iP = 0; iP < nP; iP++) {
						zh[ix++, iL] = Wavelet[iC] * Wavelet[iC];
					}
					iC++;	// move to next coeff
				}
				//nC = (int)(Math.Pow(2.0, iL) + 0.5);	// next number of coeffs for next scale (1,1,2,4,8...)
			}
			for (int k = 0; k < nPoints; k++) { 
				xh[k] = k * _sampleTime;
			}

			// color-filled plot
			QuickPlotZ plotDwv2 = new QuickPlotZ();
			if (useContours) {
				ColorScale colorScale = new ColorScale(0.0, 1.0, ColorScale.ColorScheme.RainbowETL_52, false);
				colorScale.ExtendLower = false;
				colorScale.RepeatUpper = false;
				colorScale.BackgroundColor = Color.WhiteSmoke; 
				colorScale.MinValue = 0.0;
				//colorScale.ContourStep = 0.01;
				plotDwv2.ColorContourPlot(xh, y, zh, colorScale, true);
			}
			else {
				plotDwv2.ColorBoxPlot(xh, y, zh, new ColorScale(0.0, 10.0, ColorScale.ColorScheme.RainbowETL_52, true), true);
			}

			plotDwv2.SetTitles(_title, _xlabel, _ylabel);
			plotDwv2.AddX2Axis("", 0.0, nPoints);
			plotDwv2.Display();

			// stacked line plot
			QuickPlotZ plotDwvStack = new QuickPlotZ();
			plotDwvStack.StackedPlot(xh, y, zh, false, false);
			plotDwvStack.SetTitles(_title, _xlabel, _ylabel);
			//plotDwvStack.AddX2Axis("", 0.0, nPoints);
			plotDwvStack.Display();
		}

		/// <summary>
		/// property to return the current displayed YAxis max scale value
		/// </summary>
		public double YAxisMax {
			get { return _plotDwv.GraphControl.GraphPane.YAxis.Scale.Max; }
		}

		public double YAxisMin {
			get { return _plotDwv.GraphControl.GraphPane.YAxis.Scale.Min; }
		}

		public double XAxisMax {
			get { return _plotDwv.GraphControl.GraphPane.XAxis.Scale.Max; }
		}

		public double XAxisMin {
			get { return _plotDwv.GraphControl.GraphPane.XAxis.Scale.Min; }
		}

        ///////////////////////////////////////////////////////////////////////
        /// <summary>
        /// "Exact" copy of LapXM Debauchies wavelet transform
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="iLength"></param>
        /// <param name="iMethod"></param>
        /// <param name="cFilter"></param>
        /// <param name="iGate"></param>
        /// <returns></returns>
        public bool DiscreteWaveletTransform(double[] Data,        // data array I/O
                                              int iLength,    // length of data
                                              int iMethod,    // 1,2 = transform -1,-2 = inverse tr.
                                              string cFilter)  // name of wavelet filter
        {
  
            int iSmallestLevel = 4;
            if (iMethod == -2 || iMethod == 2) {
                iSmallestLevel = iLength;
            }

            int iNCoeff;
            double[] vecCC; // coefficients of the smoothing filter
            double[] vecCR; // coefficients of the wavelet function

            cFilter = cFilter.ToLower();

            if (cFilter.Contains("haar")) {
                // Haar wavelet = Daubechies 2
                iNCoeff = 2;
                vecCC = new double[iNCoeff];
                vecCC[0] = 0.70710678118655;
                vecCC[1] = 0.70710678118655;
                vecCR = new double[iNCoeff];
                vecCR[0] = 0.70710678118655;
                vecCR[1] = -0.70710678118655;
            }
            else if (cFilter.Contains("daub20")) {
                // Daubechies filter with 20 coefficients.
                iNCoeff = 20;
                vecCC = null;
                vecCC = new double[iNCoeff];
                vecCC[0] = 0.026670057901; vecCC[1] = 0.188176800078; vecCC[2] = 0.527201188932;
                vecCC[3]  =  0.688459039454; vecCC[4]  =  0.281172343661; vecCC[5]  = -0.249846424327;
                vecCC[6]  = -0.195946274377; vecCC[7]  =  0.127369340336; vecCC[8]  =  0.093057364604;
                vecCC[9]  = -0.071394147166; vecCC[10] = -0.029457536822; vecCC[11] =  0.033212674059;
                vecCC[12] =  0.003606553567; vecCC[13] = -0.010733175483; vecCC[14] =  0.001395351747;
                vecCC[15] =  0.001992405295; vecCC[16] = -0.000685856695; vecCC[17] = -0.000116466855;
                vecCC[18] =  0.000093588670; vecCC[19] = -0.000013264203;
                double dMult = -1.0;
                vecCR = null;
                vecCR = new double[iNCoeff];
                for (int i = 0; i < iNCoeff; i++) {
                    vecCR[iNCoeff-1-i] = dMult * vecCC[i];
                    dMult = -dMult;
                }
            }
            else {
                return false; // filter name invalid.
            }
  
            int iIoff = -(iNCoeff >> 1); // Handle wrap-around of wavelets. iIoff and iJoff are
            int iJoff = -(iNCoeff >> 1); // here identical to center the 'support' of wavelets.
  
            double[] vecWksp;
            int iNMod, iNN1, iNH;
            int iNI, iNJ, iJF, iJR;
            double dAi, dAi1;
  
            if (iMethod >= 0) {
                // wavelet transform
                for (int iNN = iLength; iNN >= iSmallestLevel; iNN >>= 1) {
                    vecWksp = new double[iNN];
                    iNMod = iNCoeff * iNN;
                    iNN1 = iNN - 1;
                    iNH = iNN >> 1;
                    for (int ii = 0, i = 0; i < iNN; i += 2, ii++) {
                        iNI = i+1+iNMod+iIoff;
                        iNJ = i+1+iNMod+iJoff; 
                        for (int k = 0; k < iNCoeff; k++) {
                            iJF = iNN1 & (iNI+k+1);
                            iJR = iNN1 & (iNJ+k+1);
                            if (ii + iNH == 1023) {
                                int b = 0;
                            }
                            vecWksp[ii] += vecCC[k] * Data[iJF];
                            vecWksp[ii+iNH] += vecCR[k] * Data[iJR];
                        }
                    }
                    for (int i = 0; i < iNN; i++) {
                        Data[i] = vecWksp[i];
                    }
                }
            }
            else {
                // inverse wavelet transform
                for (int iNN = iSmallestLevel; iNN <= iLength; iNN <<= 1) {
                    vecWksp = new double[iNN];
                    iNMod = iNCoeff * iNN;
                    iNN1 = iNN - 1;
                    iNH = iNN >> 1;
                    for (int ii = 0, i = 0; i < iNN; i += 2, ii++) {
                        dAi = Data[ii];
                        dAi1 = Data[ii+iNH];
                        iNI = i+1+iNMod+iIoff;
                        iNJ = i+1+iNMod+iJoff; 
                        for (int k = 0; k < iNCoeff; k++) {
                            iJF = iNN1 & (iNI+k+1);
                            iJR = iNN1 & (iNJ+k+1);
                            vecWksp[iJF] += vecCC[k] * dAi;
                            vecWksp[iJR] += vecCR[k] * dAi1;
                        }
                    }
                    for (int i = 0; i < iNN; i++) {
                        Data[i] = vecWksp[i];
                    }
                }
            }
  
            return true;
        }


        ///////////////////////////////////////////////////////////////////////

	}  // end of class DaubechiesWavelet



    // ************************************************************************
    /// <summary>
    /// 
    /// </summary>
    public class DaubechiesWaveletXM {

        private int _nPts;
        private int _nCoeffs;
        private int _powOf2;
        private double[] _input;
        private double[] _wavelet;
        private double[] _invWavelet;
        private int _iSmallestLevel;
        private double[] _coeffs, _clr;
        private double[] _vecWksp;

        public int SmallestLevel {
            get { return _iSmallestLevel; }
            set { _iSmallestLevel = value; }
        }

        public DaubechiesWaveletXM(int size, int order) {

            _nPts = size;
            _nCoeffs = order;
            _iSmallestLevel = 4;

            _powOf2 = (int)(Math.Log10(_nPts) / Math.Log10(2) + 1.0e-10);	// nPts >= 2^n
            int M = (int)(Math.Pow(2.0, _powOf2) + 0.5);
            if (M != _nPts) {
                throw new ArgumentException("Input nPts must be power of 2 for DaubechiesWaveletXM");
            }

            _input = new double[_nPts];
            _wavelet = new double[_nPts];
            _invWavelet = new double[_nPts];
            _vecWksp = new double[_nPts];

            _coeffs = DCoeffs(_nCoeffs);
            double sqrt2 = Math.Sqrt(2.0);
            for (int i = 0; i < _nCoeffs; i++) {
                _coeffs[i] /= sqrt2;
            }
            _clr = Fliplr(_coeffs);
            for (int j = 1; j < _nCoeffs; j = j + 2) {
                _clr[j] = -_clr[j];
            }
        }

        public double[] Transform(double[] data) {

            // use LapXM variable names
            int iLength = _nPts;
            int iNCoeff = _nCoeffs;
            double[] vecCC = _coeffs;
            double[] vecCR = _clr;
            int iIoff = -(iNCoeff >> 1); // Handle wrap-around of wavelets. iIoff and iJoff are
            int iJoff = -(iNCoeff >> 1); // here identical to center the 'support' of wavelets.
            int iNMod, iNN1, iNH;
            int iNI, iNJ, iJF, iJR;

            Array.Copy(data, _wavelet, _nPts);

            if (_nCoeffs == 2) {
                // Haar wavelet
                // use shortcuts for more economical computation

                iNH = iLength / 2;
                for (int i = 0; i < iLength; i++) {
                    _vecWksp[i] = 0.0;
                }

                for (int i = 0; i < iNH - 1; i++) {
                    _vecWksp[i] = (_wavelet[2 * i + 1] + _wavelet[2 * i + 2]) * 0.70710678118655;
                    _vecWksp[i + iNH] = (_wavelet[2 * i + 1] - _wavelet[2 * i + 2]) * 0.70710678118655;
                }
                _vecWksp[iNH - 1] = (_wavelet[iLength - 1] + _wavelet[0]) * 0.70710678118655;
                _vecWksp[iLength - 1] = (_wavelet[iLength - 1] - _wavelet[0]) * 0.70710678118655;

                for (int i = 0; i < iLength; i++) {
                    _wavelet[i] = _vecWksp[i];
                }

            }
            else {

                for (int iNN = iLength; iNN >= _iSmallestLevel; iNN >>= 1) {
                    //_vecWksp = new double[iNN];
                    for (int i = 0; i < iNN; i++) {
                        _vecWksp[i] = 0.0;
                    }
                    iNMod = iNCoeff * iNN;
                    iNN1 = iNN - 1;
                    iNH = iNN >> 1;
                    for (int ii = 0, i = 0; i < iNN; i += 2, ii++) {
                        iNI = i + 1 + iNMod + iIoff;
                        iNJ = i + 1 + iNMod + iJoff;
                        for (int k = 0; k < iNCoeff; k++) {
                            iJF = iNN1 & (iNI + k + 1);
                            iJR = iNN1 & (iNJ + k + 1);
                            if (ii + iNH == 1023) {
                                int b = 0;
                            }
                            _vecWksp[ii] += vecCC[k] * _wavelet[iJF];
                            _vecWksp[ii + iNH] += vecCR[k] * _wavelet[iJR];
                        }
                    }
                    for (int i = 0; i < iNN; i++) {
                        _wavelet[i] = _vecWksp[i];
                    }
                }
            }

            return _wavelet;
        }

        public double[] InverseTransform() {
            return InverseTransform(_wavelet);
        }

        public double[] InverseTransform(double[] data) {

            int iLength = _nPts;
            int iNCoeff = _nCoeffs;
            double[] vecCC = _coeffs;
            double[] vecCR = _clr;
            int iIoff = -(iNCoeff >> 1); // Handle wrap-around of wavelets. iIoff and iJoff are
            int iJoff = -(iNCoeff >> 1); // here identical to center the 'support' of wavelets.
            int iNMod, iNN1, iNH;
            int iNI, iNJ, iJF, iJR;
            double dAi, dAi1;

            Array.Copy(data, _invWavelet, _nPts);

            if (_nCoeffs == 2) {
                // Haar wavelet
                // use shortcuts for more economical computation

                iNH = iLength / 2;
                for (int i = 0; i < iLength; i++) {
                    _vecWksp[i] = 0.0;
                }

                for (int i = 0, j = 0; i < iNH - 1; i++, j += 2) {
                    _vecWksp[j + 1] = (_invWavelet[i] + _invWavelet[i + iNH]) * 0.70710678118655;
                    _vecWksp[j + 2] = (_invWavelet[i] - _invWavelet[i + iNH]) * 0.70710678118655;
                }
                _vecWksp[0] = (_invWavelet[iNH - 1] - _invWavelet[iLength - 1]) * 0.70710678118655;
                _vecWksp[iLength - 1] = (_invWavelet[iNH - 1] + _invWavelet[iLength - 1]) * 0.70710678118655;

                for (int i = 0; i < iLength; i++) {
                    _invWavelet[i] = _vecWksp[i];
                }

            }
            else {
                for (int iNN = _iSmallestLevel; iNN <= iLength; iNN <<= 1) {
                    _vecWksp = null;
                    _vecWksp = new double[iNN];
                    iNMod = iNCoeff * iNN;
                    iNN1 = iNN - 1;
                    iNH = iNN >> 1;
                    for (int ii = 0, i = 0; i < iNN; i += 2, ii++) {
                        dAi = _invWavelet[ii];
                        dAi1 = _invWavelet[ii + iNH];
                        iNI = i + 1 + iNMod + iIoff;
                        iNJ = i + 1 + iNMod + iJoff;
                        for (int k = 0; k < iNCoeff; k++) {
                            iJF = iNN1 & (iNI + k + 1);
                            iJR = iNN1 & (iNJ + k + 1);
                            _vecWksp[iJF] += vecCC[k] * dAi;
                            _vecWksp[iJR] += vecCR[k] * dAi1;
                        }
                    }
                    for (int i = 0; i < iNN; i++) {
                        _invWavelet[i] = _vecWksp[i];
                    }
                }
            }

            return _invWavelet;

        }

        /// <summary>
        /// Used on general Daubechies wavelet
        ///     to remove persistent clutter
        /// </summary>
        /// <param name="threshold"></param>
        public void Clip(double threshold) {

            // Determine Threshold Value
            double dThresholdRatio_DaubClip = 0.1;
            dThresholdRatio_DaubClip = threshold;
            double dThreshold = 0.0;
            int iNumPoints, iStartPoint;

            int iNumberOfSegments = 4;
            int iFirstSegment = 3;
            if (iNumberOfSegments == 3) {
                iFirstSegment = 2;
            }
            // Check up to four ranges 0-(Npts/8-1), Npts/8-(Npts/4-1), Npts/4-(Npts/2-1), Npts/2-(Npts-1)
            // If only 3 segments requested, skip first range 0-(Npts/8-1).
            for (int iSegment = iFirstSegment; iSegment >= 0; iSegment--) {
                // Set number of points in this segment

                if (iSegment == 3) {
                    iNumPoints = _nPts / 8;
                    iStartPoint = 0;
                }
                else {
                    iNumPoints = (int)(Math.Pow((double)2, (_powOf2 - iSegment - 1)));
                    iStartPoint = (int)(Math.Pow((double)2, (_powOf2 - iSegment - 1)));
                }

                // Create buffer to hold data for sorting
                double[] buffer = new double[iNumPoints];

                // Move I + Q data into one buffer and change to absolute values
                for (int iPoint = 0; iPoint < iNumPoints; iPoint++) {
                    buffer[iPoint] = Math.Abs(_wavelet[iStartPoint + iPoint]);
                    //buffer[iNumPoints + iPoint] = fabs(pdDataQ[iStartPoint + iPoint]);
                }

                // Sort from smallest to largest
                Array.Sort(buffer);
                //qsort((void*)pdBuffer, (size_t)(2 * iNumPoints), (size_t)sizeof(double), compare_double);

                // Find value of the median point
                double dMedian = buffer[iNumPoints / 2];

                // Step through the points until the median point/ largest point < .1
                for (int iPoint = 1; iPoint <= iNumPoints; iPoint++) {
                    if (dMedian / buffer[(iNumPoints - iPoint)] > dThresholdRatio_DaubClip) {

                        // Set the threshold to this largest point if larger than previous threshold
                        if (buffer[(iNumPoints - iPoint)] > dThreshold) {
                            dThreshold = buffer[(iNumPoints - iPoint)];
                        }

                        break;
                    }
                }

            } // Next Segment

            for (int iPoint = 0; iPoint < _nPts; iPoint++) {
                // Clip 
                if (Math.Abs(_wavelet[iPoint]) > dThreshold) {
                    _wavelet[iPoint] = (_wavelet[iPoint] / Math.Abs(_wavelet[iPoint])) * dThreshold;
                }

            }
        }  // end Clip()

        /// <summary>
        /// DeSpike
        /// Used on Haar (Daubechies order=2) wavelet
        ///     to remove intermittent large signals
        /// </summary>
        /// <param name="threshold"></param>
        public void DeSpike(double threshold) {

            double dThresholdRatio_DeSpike = 0.1;
            dThresholdRatio_DeSpike = threshold;
            // Determine Threshold Value
            double dThreshold = 0.0;
            int iNumPoints = 64;                   // Number of points in a segment
            int iNSegments = _nPts / (2 * iNumPoints); // Number of segments
            int iStartPoint; // Where in the array of lNpts points the segment begins

            // Check 128 point groups from range (lNpts/2 - lNpts-1) * 64 Is & 64 Qs
            for (int iSegment = 0; iSegment < iNSegments; iSegment++) {

                // Set the point in the array where this segment begins
                iStartPoint = _nPts / 2 + iSegment * iNumPoints;

                // Create buffer to hold data for sorting. This includes both Is & Qs
                double[] buffer = new double[iNumPoints];

                // Move  data into one buffer and change to absolute values
                for (int iPoint = 0; iPoint < iNumPoints; iPoint++) {
                    buffer[iPoint] = Math.Abs(_wavelet[iStartPoint + iPoint]);
                }

                // Sort from smallest to largest
                Array.Sort(buffer);
                //qsort((void*)pdBuffer, (size_t)(2 * iNumPoints), (size_t)sizeof(double), compare_double);

                // Find value of the median point
                double dMedian = buffer[iNumPoints/2];

                // Starting with the largest numbers, find where the median point/ largest point < .1
                // The value at this point is considered the threshold
                for (int iPoint = (iNumPoints - 1); iPoint >= 0; iPoint--) {
                    if (dMedian / buffer[iPoint] > dThresholdRatio_DeSpike) {
                        // Set fThreshold to threshold found
                        dThreshold = 2.0 * buffer[iPoint];

                        // Remove Spike - This steps through points in this segment looking for values 
                        // above the threshold. When found, sets that point to zero and replaces 
                        // the corresponding point on the left side (point-NPTS/2) with the 
                        // previous value (point-NPTS/2-1).

                        // Count first how many values are above threshold. If this number is
                        // unusually large, skip the thresholding. Signal may contain components
                        // that are not well handled with DeSpike.
                        int iClippedValues = 0;
                        int iMaxValuesToClip = 5;
                        for (int iPointArray = iStartPoint; iPointArray < (iStartPoint + iNumPoints); iPointArray++) {
                            if (_wavelet[iPointArray] > dThreshold
                              || _wavelet[iPointArray] < (-1.0 * dThreshold)) {
                                iClippedValues++;
                            }
                        }
                        if (iClippedValues > iMaxValuesToClip) {
                            iClippedValues = -1;
                        }

                        for (int iPointArray = iStartPoint; iPointArray < (iStartPoint + iNumPoints); iPointArray++) {
                            // Clip the data
                            if ((_wavelet[iPointArray] > dThreshold
                              || _wavelet[iPointArray] < (-1.0 * dThreshold))
                              && iClippedValues > 0) {
                                  _wavelet[iPointArray] = 0.0;
                                if (iPointArray > _nPts / 2) {
                                    _wavelet[iPointArray - _nPts / 2] = _wavelet[iPointArray - _nPts / 2 - 1];
                                }
                                else {
                                    _wavelet[iPointArray - _nPts / 2] = _wavelet[iPointArray - _nPts / 2 + 1];
                                }
                            }

                        }

                        break;

                    }
                }

            } // Next Segment

        }  // end DeSpike()

        /// <summary>
        /// Compute coefficients for Daubechies wavelets of order N
        /// </summary>
        /// <param name="N">Wavelet Order (2-20)</param>
        /// <returns>N coefficients required by the wavelet transform.  Null if invalid N.</returns>
        private double[] DCoeffs(int N) {
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

        private double[] Fliplr(double[] c) {
            int N = c.Length;
            double[] clr = new double[N];
            for (int i = 0; i < N / 2; i++) {
                clr[i] = c[N - 1 - i];
                clr[N - 1 - i] = c[i];
            }
            return clr;
        }  // end of method Fliplr


    }  // end class DaubechiesWavelet

    // ************************************************************************

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
		//private Complex[] xx;
		private double _sampleTime;

		public HarmonicWavelet() {
			_title = "Harmonic Wavelet Transform Power";
			_xlabel = "Time (points)";
			_ylabel = "Wavelet Level";
			_sampleTime = 1.0;
		}

		public HarmonicWavelet(double sampleTime) {
			_title = "Harmonic Wavelet Transform Power";
			_xlabel = "Time (sec)";
			_ylabel = "Wavelet Level";
			_sampleTime = sampleTime;
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
			ZedGraph.LineItem sss = plotHwv.AddCurve("", x, FFT.MagFromComplexArray(Wavelet), Color.Blue, ZedGraph.SymbolType.Plus);
			liH3.Symbol.Size = 3.0f;
			plotHwv.Display();

			// 2-d plot
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
				xh[k] = k * _sampleTime;
			}

			// color-filled plot
			QuickPlotZ plotHwv2 = new QuickPlotZ();
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
			plotHwv2.AddX2Axis("", 0.0, nPoints);
			plotHwv2.Display();

			// stacked line plot
			QuickPlotZ plotHwvStack = new QuickPlotZ();
			plotHwvStack.StackedPlot(xh, y, zh, false, false);
			plotHwvStack.SetTitles(_title, _xlabel, _ylabel);
			//plotDwvStack.AddX2Axis("", 0.0, nPoints);
			plotHwvStack.Display();
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

