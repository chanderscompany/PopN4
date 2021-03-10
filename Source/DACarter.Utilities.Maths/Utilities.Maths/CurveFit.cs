using System;

using NMA = NumericalMethods.Approximation;
using NM = NumericalMethods;

namespace DACarter.Utilities.Maths {

    //////////////////////////////////////////////////

    /// <summary>
    /// Object for nonlinear fitting by the Levenberg-Marquardt method.
    /// From Numerical Recipes.
    /// Also includes the ability to hold specified parameters at fixed, specified values.
    /// Call constructor to bind data vectors and fitting functions and 
    /// to input an initial parameter guess.
    /// Then call any combinations of hold, free, and fit as often as desired.
    /// fit() sets the output quantities a, covar, alpha, and chisq.
    /// </summary>
    public class Fitmrq {

        public delegate void funcdelegate(double x, double[] coeffs, out double y, double[] dyda);

        protected int _numCoeffs;

        const int NDONE = 4, ITMAX = 1000;   // convergence parameters
        int _nDataPts, _numCoeffsToCalc;
        double tol;
        private bool[] _parameterIsFree;

        // internal working arrays
        double[] _dyda;
        private double[][] _oneda;
        private double[][] _temp;
        private double[] atry;
        private double[] beta;
        private double[] da;
        private int[] _indxc;
        private int[] _indxr;
        private int[] _ipiv;

        // inputs:
        private double[] _x, _y;     // measured values, y(x)
        private double[] _stdDev;    // estimated std dev of measurments; i.e. noise estimate.
        public funcdelegate funcs;   // function of type funcdelegate that computes y for given x of fitting function

        // input and output:
        public double[] Coeffs;     // coeffs of fitting function: on input, first guess; on output, final fit values.
                                    // size of array is numCoeffs.

        // outputs:
        public double[][] Covar;    // covariance matrix
        public double[][] Alpha;    // curvature matrix
        public double Chisq;
        public int Iterations;

        /// <summary>
        /// Constructor
        /// Binds references to the data arrays xx, yy, and ssig,
        ///     and to a user-supplied function funks that calculates
        ///     the nonlinear fitting function and its derivatives.
        /// Also inputs the initial parameters guess aa (which is not modified)
        ///     and an optional convergence tolerance TOL.
        /// Initializes all parameters as free (not held).
        /// </summary>
        /// <param name="xx"></param>
        /// <param name="yy"></param>
        /// <param name="ssig"></param>
        /// <param name="guess"></param>
        /// <param name="funks"></param>
        /// <param name="TOL"></param>
        public Fitmrq(double[] xx, double[] yy, double[] ssig, double[] guess,
                    funcdelegate funks, double TOL = 1.0e-3) {

            funcs = funks;
            tol = TOL;

            Setup(xx, yy, ssig, guess);

        }

        /// <summary>
        /// use this constructor especially if you are going to do
        ///     multiple fits of the same function to multiple data sets.
        /// </summary>
        /// <param name="funks"></param>
        /// <param name="TOL"></param>
        public Fitmrq(funcdelegate funks, double TOL = 1.0e-3) {
            funcs = funks;
            tol = TOL;
            _parameterIsFree = null;
        }

        /// <summary>
        /// use this constructor if you want to hold one of the coeffs fixed
        ///     by specifying the coeffsFree array
        /// </summary>
        /// <param name="funks"></param>
        /// <param name="coeffsFree"></param>
        /// <param name="TOL"></param>
        public Fitmrq(funcdelegate funks, bool[] coeffsFree, double TOL = 1.0e-3) {
            funcs = funks;
            tol = TOL;
            _parameterIsFree = coeffsFree;
        }

        /// <summary>
        /// This constructor is good to use in derived classed that
        ///     provide their own fitting function, funks,
        ///     and that specify _numcoeffs in the derived constructor.
        /// </summary>
        public Fitmrq() {
            tol = 1.0e-3;
            InitFreeList();
        }

        /// <summary>
        /// This constructor is good to use in derived classed that
        ///     provide their own fitting function, funks.
        /// </summary>
        /// <param name="nCoeffs"></param>
        /// <param name="TOL"></param>
        public Fitmrq(int nCoeffs, double TOL = 1.0e-3) {
            tol = TOL;
            _numCoeffs = nCoeffs;
            InitFreeList();
        }

        public void Setup(double[] xx, double[] yy, double[] ssig, double[] guess) {
            _nDataPts = xx.Length;
            _numCoeffs = guess.Length;
            _x = xx;
            _y = yy;
            _stdDev = ssig;
            Dimension(ref Covar, _numCoeffs, _numCoeffs);
            Dimension(ref Alpha, _numCoeffs, _numCoeffs);
            Coeffs = guess;
            InitFreeList();
        }

        protected void InitFreeList() {
            if (_parameterIsFree == null) {
                // if array not defined, set every coeff to free (true)
                Dimension(ref _parameterIsFree, _numCoeffs);
                for (int i = 0; i < _numCoeffs; i++) {
                    _parameterIsFree[i] = true;
                }
            }
            else {
                if (_parameterIsFree.Length != _numCoeffs) {
                    throw new ApplicationException("Fitmrq parameterIsFree array wrong dimension.");
                }
            }
        }

        T[][] Dimension<T>(int cols, int rows) {
            T[][] mat = new T[cols][];
            for (int i = 0; i < cols; i++) {
                mat[i] = new T[rows];
            }
            return mat;
        }

        /// <summary>
        /// Allocates array of type T[][]
        ///     unless it is already of correct size.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ary"></param>
        /// <param name="cols"></param>
        /// <param name="rows"></param>
        void Dimension<T>(ref T[][] ary, int cols, int rows) {
            if (ary == null) {
                ary = Dimension<T>(cols, rows);
            }
            else if ((ary.Length != cols) || (ary[0].Length != rows)) {
                ary = null;
                ary = Dimension<T>(cols, rows);
            }
        }

        /// <summary>
        /// Allocates array of type T[]
        ///     unless it is already of correct size.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ary"></param>
        /// <param name="npts"></param>
        void Dimension<T>(ref T[] ary, int npts) {
            if (ary == null) {
                ary = new T[npts];
            }
            else if (ary.Length != npts) {
                ary = null;
                ary = new T[npts];
            }
        }

        public void Hold(int i, double val) {
            _parameterIsFree[i] = false;
            if (Coeffs != null) {
                if (Coeffs.Length > i) {
                    Coeffs[i] = val;
                }
            }
        }

        
        public void Hold(int i) {
            // will use value in guess
            _parameterIsFree[i] = false;
        }

        public void Free(int i) {
            _parameterIsFree[i] = true;
        }

        public void Fit(double[] xx, double[] yy, double[] ssig, double[] guess) {
            Setup(xx, yy, ssig, guess);
            Fit();
        }

        public void Fit() {
            //int j, k, l, iter;
            Iterations = 0;
            int done = 0;
            double alamda = 0.001;
            double oldchisq;
            Dimension(ref atry, _numCoeffs);
            Dimension(ref beta, _numCoeffs);
            Dimension(ref da, _numCoeffs);
            _numCoeffsToCalc = 0;
            for (int j = 0; j < _numCoeffs; j++) {
                if (_parameterIsFree[j]) {
                    _numCoeffsToCalc++;
                }
            }
            Dimension(ref _oneda, _numCoeffsToCalc, 1);
            Dimension(ref _temp, _numCoeffsToCalc, _numCoeffsToCalc);
            mrqcof(Coeffs, Alpha, beta);
            for (int j = 0; j < _numCoeffs; j++) {
                atry[j] = Coeffs[j];
            }
            oldchisq = Chisq;
            for (int iter = 0; iter < ITMAX; iter++) {
                Iterations = iter;
                if (done == NDONE) {
                    // last pass
                    alamda = 0.0;
                }
                for (int j = 0; j < _numCoeffsToCalc; j++) {
                    // alter linearized fitting matrix
                    //  by augmenting diagonal elements.
                    for (int k = 0; k < _numCoeffsToCalc; k++) {
                        Covar[j][k] = Alpha[j][k];
                    }
                    Covar[j][j] = Alpha[j][j] * (1.0 + alamda);
                    for (int k = 0; k < _numCoeffsToCalc; k++) {
                        _temp[j][k] = Covar[j][k];
                    }
                    _oneda[j][0] = beta[j];
                }
                gaussj(_temp, _oneda);    // matrix solution
                for (int j = 0; j < _numCoeffsToCalc; j++) {
                    for (int k = 0; k < _numCoeffsToCalc; k++) {
                        Covar[j][k] = _temp[j][k];
                    }
                    da[j] = _oneda[j][0];
                }
                if (done == NDONE) {
                    // converged; clean up and return
                    covsrt(Covar);
                    covsrt(Alpha);
                    return;
                }
                // did the trial succeed?
                for (int j = 0, l = 0; l < _numCoeffs; l++) {
                    if (_parameterIsFree[l]) {
                        atry[l] = Coeffs[l] + da[j++];
                    }
                }
                mrqcof(atry, Covar, da);
                if (Math.Abs(Chisq - oldchisq) < Math.Max(tol, tol * Chisq)) {
                    done++;
                }
                if (Chisq < oldchisq) {
                    // success; accept the new solution
                    alamda *= 0.1;
                    oldchisq = Chisq;
                    for (int j = 0; j < _numCoeffsToCalc; j++) {
                        for (int k = 0; k < _numCoeffsToCalc; k++) {
                            Alpha[j][k] = Covar[j][k];
                        }
                        beta[j] = da[j];
                    }
                    for (int l = 0; l < _numCoeffs; l++) {
                        Coeffs[l] = atry[l];
                    }
                }
                else {
                    // failure; increase alamda
                    alamda *= 10.0;
                    Chisq = oldchisq;
                }
            }
            throw (new ApplicationException("Fitmrq too many iterations."));
        }

        void Swap<T>(ref T x, ref T y) {
            T t = y;
            y = x;
            x = t;
        }

        private void covsrt(double[][] covar) {
            for (int i = _numCoeffsToCalc; i < _numCoeffs; i++) {
                for (int j = 0; j < i + 1; j++) {
                    covar[i][j] = covar[j][i] = 0.0;
                }
            }
            int k = _numCoeffsToCalc - 1;
            for (int j = _numCoeffs - 1; j >= 0; j--) {
                if (_parameterIsFree[j]) {
                    for (int i = 0; i < _numCoeffs; i++) {
                        Swap(ref covar[i][k], ref covar[i][j]);
                    }
                    for (int i = 0; i < _numCoeffs; i++) {
                        Swap(ref covar[k][i], ref covar[j][i]);
                    }
                    k--;
                }
            }
        }

        /// <summary>
        /// Linear equation solution bu Gauss-Jordan elimination.
        /// The input matrix is a[n][n]
        /// b[n][m] is input containing the m righthand side vectors.
        /// On output a is replaced by its matrix inverse, and
        /// b is replaced by the set of solution vectors.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        private void gaussj(double[][] a, double[][] b) {
            int icol = 0, irow = 0;
            int n = a.Length;       // number of rows in a
            int m = b[0].Length;    // number of columns in b
            double big, dum, pivinv;
            Dimension(ref _indxc, n);
            Dimension(ref _indxr, n);
            Dimension(ref _ipiv, n);
            for (int j = 0; j < n; j++) {
                _ipiv[j] = 0;
            }
            for (int i = 0; i < n; i++) {
                big = 0.0;
                for (int j = 0; j < n; j++) {
                    if (_ipiv[j] != 1) {
                        for (int k = 0; k < n; k++) {
                            if (_ipiv[k] == 0) {
                                if (Math.Abs(a[j][k]) >= big) {
                                    big = Math.Abs(a[j][k]);
                                    irow = j;
                                    icol = k;
                                }
                            }
                        }
                    }
                }
                ++(_ipiv[icol]);
                if (irow != icol) {
                    for (int l = 0; l < n; l++) {
                        Swap(ref a[irow][l], ref a[icol][l]);
                    }
                    for (int l = 0; l < m; l++) {
                        Swap(ref b[irow][l], ref b[icol][l]);
                    }
                }
                _indxr[i] = irow;
                _indxc[i] = icol;
                if (a[icol][icol] == 0.0) {
                    throw new ApplicationException("Gaussj singular matrix.");
                }
                pivinv = 1.0 / a[icol][icol];
                a[icol][icol] = 1.0;
                for (int l = 0; l < n; l++) {
                    a[icol][l] *= pivinv;
                }
                for (int l = 0; l < m; l++) {
                    b[icol][l] *= pivinv;
                }
                for (int ll = 0; ll < n; ll++) {
                    if (ll != icol) {
                        dum = a[ll][icol];
                        a[ll][icol] = 0.0;
                        for (int l = 0; l < n; l++) {
                            a[ll][l] -= a[icol][l] * dum;
                        }
                        for (int l = 0; l < m; l++) {
                            b[ll][l] -= b[icol][l] * dum;
                        }
                    }
                }
            }
            for (int l = n - 1; l >= 0; l--) {
                if (_indxr[l] != _indxc[l]) {
                    for (int k = 0; k < n; k++) {
                        Swap(ref a[k][_indxr[l]], ref a[k][_indxc[l]]);
                    }
                }
            }

        }

        private void mrqcof(double[] a, double[][] alpha, double[] beta) {
            double ymod, wt, sig2i, dy;
            Dimension(ref _dyda, _numCoeffs);
            for (int j = 0; j < _numCoeffsToCalc; j++) {
                // initialize (symmetric) alpha, beta
                for (int k = 0; k <= j; k++) {
                    alpha[j][k] = 0.0;
                }
                beta[j] = 0.0;
            }
            Chisq = 0.0;
            for (int i = 0; i < _nDataPts; i++) {
                funcs(_x[i], a, out ymod, _dyda);
                sig2i = 1.0 / (_stdDev[i] * _stdDev[i]);
                dy = _y[i] - ymod;
                for (int j = 0, l = 0; l < _numCoeffs; l++) {
                    if (_parameterIsFree[l]) {
                        wt = _dyda[l] * sig2i;
                        for (int k = 0, m = 0; m < l + 1; m++) {
                            if (_parameterIsFree[m]) {
                                alpha[j][k++] += wt * _dyda[m];
                            }
                        }
                        beta[j++] += dy * wt;
                    }
                }
                Chisq += dy * dy * sig2i;
            }
            for (int j = 0; j < _numCoeffsToCalc; j++) {
                for (int k = 0; k < j; k++) {
                    alpha[k][j] = alpha[j][k];
                }
            }
        }



    }  //end class Fitmrq

    /// <summary>
    /// GaussFit: class to compute Gaussian fit to data points.
    /// Derived from class Fitmrq
    /// The function coeffs are, in order, Amp, Mean, Width, Offset (constant added to y);
    /// If only 3 coeffs given, the offset is 0.
    /// For multiple gaussian fits, multiple sets of 3 coeffs are used,
    ///     with one optional offset parameter at the very end.
    /// The array sd is the estimate of std dev (noise) of each y value.
    /// If the number of coefficients does not change, you can use
    ///     the same GaussFit object for multiple fits, and
    ///     no new memory allocations will take place.
    /// </summary>
    /// <remarks>
    ///     usage:
    ///        if (_myFit == null) {
    ///            // pass the number of coeffs to fit in the constructor
    ///            _myFit = new GaussFit(guessCoeffs.Length);
    ///        }

    ///        //myFit.Hold(3);  // set 4th guessCoeff to be fixed, not fit
    ///        _myFit.Fit(x, y, sd, guessCoeffs);

    ///        double a0 = _myFit.coeffs[0];
    ///        double a1 = _myFit.coeffs[1];
    ///        double a2 = _myFit.coeffs[2];
    ///        int iter = _myFit.Iterations;
    /// </remarks>

    public class GaussFit : Fitmrq {

        private int iminWidth;
        private int xminWidth;  // minimum half-width of gaussian guess
        private double[] derivs;
        //private double _slopeAtZero;

        public double SlopeAtZero {
            get { return GetSlopeAtZero(); }
            //set { _slopeAtZero = value; }
        }

        private double GetSlopeAtZero() {
            // deriative of gaussian wrt t is same as minus derivative wrt B coeff
            if (derivs == null || derivs.Length != _numCoeffs) {
                derivs = null;
                derivs = new double[_numCoeffs];
            }
            double y;
            double slope = 0.0;
            fgauss(0.0, Coeffs, out y, derivs);
            slope = -derivs[1];
            return slope;
        }

        private GaussFit() {
            funcs = fgauss;
            _numCoeffs = 4;
            InitFreeList();
        }

        public GaussFit(int coeffs, double TOL = 1.0e-3) : base(coeffs) {
            funcs = fgauss;
        }

        /// <summary>
        /// This is the method that defines the function to fit.
        /// Elements of the coefficient array are sets of 3 values
        ///     (amplitude, mean, width) repeated for each gaussian, 
        ///     followed by an optional dc offset value.
        /// </summary>
        /// <param name="x">input x value</param>
        /// <param name="coeffs">input array of function coeffs</param>
        /// <param name="y">output y value at x</param>
        /// <param name="dyda">output array derivatives of the coeffs at x</param>
        public void fgauss(double x, double[] coeffs, out double y, double[] dyda) {

            int na = coeffs.Length;
            double dc;
            int numCoeffs = 3;
            if (na % 3 == 0) {
                dc = 0.0;
            }
            else if (na % 3 == 1) {
                // dc coeff is the last in the list
                dc = coeffs[na - 1];
            }
            else {
                throw new ApplicationException("Invalid number of Gauss Fit coeffs: " + na.ToString());
            }
            int nCurves = na / numCoeffs;
            double fac, ex, arg;
            y = 0.0;
            for (int k = 0, i=0; k < nCurves; k++, i += numCoeffs) {
                arg = (x - coeffs[i + 1]) / coeffs[i + 2];
                ex = Math.Exp(-arg * arg);
                fac = coeffs[i] * ex * 2.0 * arg;
                y += coeffs[i] * ex ;
                dyda[i] = ex;
                dyda[i + 1] = fac / coeffs[i + 2];
                dyda[i + 2] = fac * arg / coeffs[i + 2];
            }
            // constant coeff and derivative:
            y += dc;
            if (na % 3 == 1) {
                dyda[na-1] = 1;
            }

        }  // end fgauss()

        public void MakeAGuess(double[] x, double[] y, double[] guess) {
            MakeAGuess(x, y, guess, 0.0);
        }

        public void MakeAGuess(double[] x, double[] y, double[] guess, double xwidthmin) {

            int npts = x.Length;
            int iwidthmin = (int)((xwidthmin / (x[1] - x[0])) + 0.5);

            double maxy = -1.0e6;
            double maxx = 0;
            int imax = 0;
            for (int i = 0; i < npts; i++) {
                if (y[i] > maxy) {
                    maxy = y[i];
                    maxx = x[i];
                    imax = i;
                }
            }
            int iwidth = 0;
            for (int i = imax; i < npts; i++) {
                if (y[i] < 0.5*maxy) {
                    iwidth = i - imax;
                    break;
                }
            }
            if (iwidth < iwidthmin) {
                iwidth = iwidthmin;
            }
            if (iwidth + imax >= x.Length) {
                iwidth = x.Length - imax - 1;
            }

            double amp = y[imax];
            double mean = x[imax];
            double width = x[imax+iwidth] - x[imax];

            if (guess != null) {
                int nc = guess.Length;
                if (nc >=3) {
                    guess[0] = amp;
                    guess[1] = mean;
                    guess[2] = width;
                }
                if (nc % 3 == 1) {
                    guess[nc - 1] = 0.0;
                }
            }

        }
   }

    /////////////////////////////////////////////////

    public class GaussFit0 {

        //public double[] xx, yy, sd;
        //public double[] guess;
        private Fitmrq nlfit;

        public double[] coeffs;
        public int Iterations;

        public GaussFit0() {
        }

         public GaussFit0(int coeffs, double TOL = 1.0e-3) {
            nlfit = new Fitmrq(coeffs, TOL);
            nlfit.funcs = fgauss;
        }
  

        public void Fit(double[] xx, double[] yy, double[] sd, double[] guess) {
            nlfit.Fit(xx, yy, sd, guess);
            coeffs = nlfit.Coeffs;
            Iterations = nlfit.Iterations;
        }

        /// <summary>
        /// Computes y value for given x value
        ///     for a gaussian function with coeffs a0,a1,a2,a3
        ///     where a0 is amplitude
        ///     a1 is x-value of the peak,
        ///     a2 is the sigma (width)
        ///     a3 is the DC offset
        /// if only 3 coeffs are passed, a3 is assumed 0;
        /// Also computes derivatives of function relative to coeffs;
        /// </summary>
        /// <param name="x"></param>
        /// <param name="coeffs"></param>
        /// <param name="y"></param>
        /// <param name="dyda"></param>
        public void fgauss(double x, double[] coeffs, out double y, double[] dyda) {
            int na = coeffs.Length;
            double dc;
            int numCoeffs;
            if (na % 3 == 0) {
                numCoeffs = 3;
            }
            else if (na % 4 == 0) {
                numCoeffs = 4;
            }
            else {
                throw new ApplicationException("Gauss Fit coeffs not 3 or 4.");
            }
            int nLoops = na / numCoeffs;
            double fac, ex, arg;
            y = 0.0;
            for (int k = 0, i=0; k < nLoops; k++, i += numCoeffs) {
                if (numCoeffs == 4) {
                    dc = coeffs[i + 3];
                }
                else {
                    dc = 0.0;
                }
                arg = (x - coeffs[i + 1]) / coeffs[i + 2];
                ex = Math.Exp(-arg * arg);
                fac = coeffs[i] * ex * 2.0 * arg;
                y += coeffs[i] * ex + dc;
                dyda[i] = ex;
                dyda[i + 1] = fac / coeffs[i + 2];
                dyda[i + 2] = fac * arg / coeffs[i + 2];
                if (numCoeffs == 4) {
                    dyda[i + 3] = 1;
                }
            }
        }

    }  // end class GaussFit

    ///////////////////////////////////////////////////////////////
    //
    // Linear Least Squares Fit to polynomial
    //
    ///////////////////////////////////////////////////////////////

    /// <summary>
    /// PolyFit
    /// </summary>
    /// <remarks>
    /// Sample useage:
    /// //double[npts] x, double[npts] y, double[order+1] c;
    /// lsq = new PolyFit(order);
    /// lsq.SetData(npts, x, y);
    /// lsq.GetCoeffs(c);
    /// lsq.SetData(...) //to start new fit to new data
    /// </remarks>
    public class PolyFit {

        private NMA.PolynomialLeastSquareFit _solver;
        //private NMA.EstimatedPolynomial poly;
        private bool _isSolved;
        private NM.DhbFunctionEvaluation.PolynomialFunction[] poly;
        private NM.DhbFunctionEvaluation.PolynomialFunction _deriv, _deriv2;
        private int _order;
        private double[] _roots;
        private double[] _minmax;  // array of x-values of min/max pts of polynomial
        private int _npts;
        private double _xmin, _xmax, _xInterval;
        private double[] _xData;
        private int numIter;

        public double[] Roots {
            get { return _roots; }
            set { _roots = value; }
        }

        public int Order {
            get { return _order; }
            set { _order = value; }
        }

        public PolyFit(int order) {

            _order = order;
            _solver = new NMA.PolynomialLeastSquareFit(_order);
            poly = new NM.DhbFunctionEvaluation.PolynomialFunction[_order+1];
            _isSolved = false;

        }

        public void CreatePoly(params double[] coeffs) {
            CreatePoly(0.001, coeffs);
        }

        public void CreatePoly(double resolution, params double[] coeffs) {
            // instead of fitting data, you can create your own polynomial (c0 + c1*x + c2*x^2 + ...)
            NM.DhbFunctionEvaluation.PolynomialFunction pp = new NM.DhbFunctionEvaluation.PolynomialFunction(coeffs);
            poly[0] = pp;
            _isSolved = true;
            _xInterval = resolution;
        }

        public void SetData(int npts, double[] x, double[] y) {

            if (_xData == null || _xData.Length != npts) {
                _xData = null;
                _xData = new double[npts];
            }
            x.CopyTo(_xData, 0);
            _npts = npts;
            _xmin = x[0];
            _xmax = x[npts - 1];
            _xInterval = (_xmax - _xmin) / (npts - 1);
            
            _solver.Reset();
            _isSolved = false;
            for (int i = 0; i < npts; i++) {
                _solver.AccumulatePoint(x[i], y[i]);
            }

        }

        public void Solve() {
            // poly[0] is the LSqF polynomial
            poly[0] = _solver.Evaluate();
            _isSolved = true;
        }

        public void GetCoeffs(double[] coeffs) {

            if (coeffs.Length < _order + 1) {
                throw new ApplicationException("Coeff array not large enough in PolyFit.");
            }
            if (!_isSolved) {
                Solve();
            }
            for (int i = 0; i < _order; i++) {
                poly[i + 1] = poly[i].Deflate(0.0);
            }
            for (int i = 0; i <= _order; i++) {
                coeffs[i] = poly[i].Value(0.0);
            }
        }

        public double[] GetCoeffs() {
            double[] coeffs = new double[_order+1];
            GetCoeffs(coeffs);
            return coeffs;
        }

        // returns y-value of the fit polynomial at x
        public double PolyValue(double x) {
            return poly[0].Value(x);
        }

        public double[] FindRoots() {
            _roots = poly[0].Roots();
            return _roots;
        }

        public void FindDerivative() {
            _deriv = poly[0].Derivative();
        }

        public double[] FindMax2() {
            // finds the largest of all local maxima
            numIter = 0;
            double[] maxPt = null;
            _deriv = poly[0].Derivative();  // 1st derivative
            double slope1, slope2;
            double ymax = double.NegativeInfinity;
            double xmax = 0;
            double left, right = 0.0;
            double delta;
            double yzero, xzero;
            double stopDelta = _xInterval / 100;
            for (int i = 0; i < _npts - 1; i++) {
                // compare adjacent pts of input data array
                slope1 = _deriv.Value(_xData[i]);
                if (slope1 > 0.0) {
                    slope2 = _deriv.Value(_xData[i + 1]);
                    if (slope2 < 0.0) {
                        // if slope changes from + to -, there is a local maximum
                        left = _xData[i];
                        right = _xData[i + 1];
                        if (poly[0].Value(left) > ymax) {
                            //
                            numIter = 0;
                            delta = right - left;
                            while (delta > stopDelta) {
                                delta = right - left;
                                left = left + delta / 2.0;
                                if (_deriv.Value(left) < 0.0) {
                                    right = left;
                                    left = right - delta / 2.0;
                                }
                                delta = right - left;
                                numIter++;
                            }
                            xzero = left + delta / 2;
                            yzero = poly[0].Value(xzero);
                            if (yzero > ymax) {
                                ymax = yzero;
                                xmax = left + delta / 2;
                            }
                        }
                    }
                }
            }

            if (double.IsNegativeInfinity(ymax)) {
                maxPt = null;
            }
            else {
                maxPt = new double[2];
                maxPt[0] = xmax;
                maxPt[1] = ymax;
            }

            return maxPt;

        }

        public double[] FindMax() {
            // finds the largest of all local maxima
            _deriv = poly[0].Derivative();  // 1st derivative
            _deriv2 = _deriv.Derivative();  // 2nd derivative
            _minmax = _deriv.Roots();       // pts where 1st deriv is 0
            int numCriticalPts = _minmax.Length;
            double[] values = new double[numCriticalPts];
            for (int i = 0; i < numCriticalPts; i++) {
                values[i] = poly[0].Value(_minmax[i]);
            }
            double ymax = double.NegativeInfinity;
            double xmax = 0;
            for (int i = 0; i < numCriticalPts; i++) {
                if (_minmax[i] > _xmin && _minmax[i] < _xmax) {
                    if (values[i] > ymax) {
                        if (_deriv2.Value(_minmax[i]) < 0.0) {
                            // pt is local max
                            ymax = values[i];
                            xmax = _minmax[i];
                        }
                    }
                }
                else {
                    int debug = 1;
                }
            }
            double[] maxPt;
            if (double.IsNegativeInfinity(ymax)) {
                maxPt = null;
            }
            else {
                maxPt = new double[2];
                maxPt[0] = xmax;
                maxPt[1] = ymax;
            }
            double[] maxPt2 = FindMax2();
            if (maxPt != null && maxPt[0] > 0.3) {
                double slope1 = _deriv.Value(-0.15);
                double slope2 = _deriv.Value(-0.05);
                int r = 0;
            }
            return maxPt;
        }

        // the following static methods can be used to solve any polynomial

        public static NM.DhbFunctionEvaluation.PolynomialFunction FindDerivative(params double[] coeffs) {
            NM.DhbFunctionEvaluation.PolynomialFunction polynom = new NM.DhbFunctionEvaluation.PolynomialFunction(coeffs);
            return polynom.Derivative();
        }

        public static NM.DhbFunctionEvaluation.PolynomialFunction FindDerivative(NM.DhbFunctionEvaluation.PolynomialFunction polynom) {
            return polynom.Derivative();
        }

        public static double[] FindRoots(params double[] coeffs) {
            NM.DhbFunctionEvaluation.PolynomialFunction polynom = new NM.DhbFunctionEvaluation.PolynomialFunction(coeffs);
            return polynom.Roots();
        }

        public static double[] FindRoots(NM.DhbFunctionEvaluation.PolynomialFunction polynom) {
            return polynom.Roots();
        }

        public static NM.DhbFunctionEvaluation.PolynomialFunction CreatePolynomial(params double[] coeffs) {
            // create your own polynomial (c0 + c1*x + c2*x^2 + ...)
            NM.DhbFunctionEvaluation.PolynomialFunction pp = new NM.DhbFunctionEvaluation.PolynomialFunction(coeffs);
            return pp;
        }

        public static double FindIntersection(NM.DhbFunctionEvaluation.PolynomialFunction p0,
                                                NM.DhbFunctionEvaluation.PolynomialFunction p1,
                                                double xmin, double xmax, double resolution) {
            double tau = -99999.99;

            return tau;
        }

    }

}
