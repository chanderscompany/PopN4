//==============================================================================
// Surface Plotting classes
//	for use in DAC version of ZedGraph Library, ZGraphDac.dll
// Classes SurfaceColorPlot and SurfaceContourPlot
//	are derived from CurveItem.
// They are added to a ZedGraph plot as if they were curves (CurveItems):
//	graphPane.CurveList.Add(surfaceContourPlot)
//==============================================================================

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
//using DACarter.Utilities.Graphics;


namespace ZedGraph {

	#region Abstract base class SurfaceItem
	abstract public class SurfaceItem : CurveItem {

		#region Fields

		protected double[] _x, _y;

		protected double[,] _z;

		protected ColorScale _colorScale;

		protected bool _isClipped;

		#endregion

		#region Constructors

		public SurfaceItem(): base() {
			Init();
		}

		public SurfaceItem(double[] x, double[] y, double[,] z, ColorScale colorScale, bool isClipped)
				: base("", x, y)
		{
			Init();
			_x = x;
			_y = y;
			_z = z;
			_colorScale = colorScale;
			_isClipped = isClipped;
		}

		public SurfaceItem(double[] x, double[] y, double[,] z, ColorScale colorScale)
			: base("", x, y) {
			Init();
			_x = x;
			_y = y;
			_z = z;
			_colorScale = colorScale;
		}

		/// <summary>
		/// Internal initialization routine thats sets some initial values to defaults.
		/// </summary>
		private void Init(  ) {
			_isClipped = true;
		}
		
		#endregion

		#region Properties

		public double[] Y {
			get { return _y; }
			set { _y = value; }
		}

		public double[] X {
			get { return _x; }
			set { _x = value; }
		}

		protected double[,] Z {
			get { return _z; }
			set { _z = value; }
		}

		protected ColorScale ColorScale {
			get { return _colorScale; }
			set { _colorScale = value; }
		}

		protected bool IsClipped {
			get { return _isClipped; }
			set { _isClipped = value; }
		}
		#endregion

		#region Implementation of abstract base class methods

		/// <summary>
		/// Gets a flag indicating if the X axis is the independent axis for this <see cref="CurveItem" />
		/// </summary>
		/// <param name="pane">The parent <see cref="GraphPane" /> of this <see cref="CurveItem" />.
		/// </param>
		/// <value>true if the X axis is independent, false otherwise</value>
		override internal bool IsXIndependent(GraphPane pane) {
			return true;
		}

		/// <summary>
		/// Gets a flag indicating if the Z data range should be included in the axis scaling calculations.
		/// </summary>
		/// <param name="pane">The parent <see cref="GraphPane" /> of this <see cref="CurveItem" />.
		/// </param>
		/// <value>true if the Z data are included, false otherwise</value>
		override internal bool IsZIncluded(GraphPane pane) {
			return false;
		}

		/// <summary>
		/// Determine the coords for the rectangle associated with a specified point for 
		/// this <see cref="CurveItem" />
		/// </summary>
		/// <param name="pane">The <see cref="GraphPane" /> to which this curve belongs</param>
		/// <param name="i">The index of the point of interest</param>
		/// <param name="coords">A list of coordinates that represents the "rect" for
		/// this point (used in an html AREA tag)</param>
		/// <returns>true if it's a valid point, false otherwise</returns>
		override public bool GetCoords(GraphPane pane, int i, out string coords) {

			coords = string.Empty;
			return true;
		}

		
		#endregion

	}	// End class SurfaceItem

	#endregion

	#region SurfaceColorPlot class

	public class SurfaceColorPlot : SurfaceItem {

		private double _min, _max;

		#region Constructors
		public SurfaceColorPlot(): base() {
			_min = double.NaN;
			_max = double.NaN;
		}
		public SurfaceColorPlot(double[] x, double[] y, double[,] z, ColorScale colorScale) 
			: base(x, y, z, colorScale)  {
			_min = double.NaN;
			_max = double.NaN;
		}

		#endregion

		#region Implementation of abstract base class methods
		/// <summary>
		/// Do all rendering associated with this <see cref="LineItem"/> to the specified
		/// <see cref="Graphics"/> device.  This method is normally only
		/// called by the Draw method of the parent <see cref="ZedGraph.CurveList"/>
		/// collection object.
		/// </summary>
		/// <param name="g">
		/// A graphic device object to be drawn into.  This is normally e.Graphics from the
		/// PaintEventArgs argument to the Paint() method.
		/// </param>
		/// <param name="pane">
		/// A reference to the <see cref="ZedGraph.GraphPane"/> object that is the parent or
		/// owner of this object.
		/// </param>
		/// <param name="pos">The ordinal position of the current <see cref="Bar"/>
		/// curve.</param>
		/// <param name="scaleFactor">
		/// The scaling factor to be used for rendering objects.  This is calculated and
		/// passed down by the parent <see cref="ZedGraph.GraphPane"/> object using the
		/// <see cref="PaneBase.CalcScaleFactor"/> method, and is used to proportionally adjust
		/// font sizes, etc. according to the actual size of the graph.
		/// </param>
		override public void Draw(Graphics g, GraphPane pane, int pos, float scaleFactor) {


			int numXPoints = _x.Length;
			int numYPoints = _y.Length;
			if ((numXPoints < 2) || (numYPoints < 2)) {
				return;
			}
			double avgDeltaX = (float)(_x[numXPoints - 1] - _x[0]) / (numXPoints - 1);
			double avgDeltaY = (float)(_y[numYPoints - 1] - _y[0]) / (numYPoints - 1);
			double deltaX, deltaY;
			//pane.GraphObjList.Clear();

			//pane.Chart.Fill.Type = FillType.Solid;
			//pane.Chart.Fill.Color = _colorScale.BackgroundColor;
			
/*
			pane.XAxis.Scale.Min = _x[0] - (_x[1] - _x[0]) / 2.0;
			pane.XAxis.Scale.Max = _x[_x.Length - 1] + (_x[_x.Length - 1] - _x[_x.Length - 2]) / 2.0;
			pane.YAxis.Scale.Min = _y[0] - (_y[1] - _y[0]) / 2.0;
			pane.YAxis.Scale.Max = _y[_y.Length - 1] + (_y[_y.Length - 1] - _y[_y.Length - 2]) / 2.0;
*/
			if (!_colorScale.UserSetDataMinMax) {
				double maxValue = double.MinValue;
				for (int yi = 0; yi < numYPoints; yi++) {
					for (int xi = 0; xi < numXPoints; xi++) {
						if (!Double.IsNaN(_z[xi, yi])) {
							if (_z[xi, yi] > maxValue) {
								maxValue = _z[xi, yi];
							}
						}
					}
				}
				_colorScale.MaxValue = Math.Ceiling(maxValue);
			}
			//return;

			int count = 0;
			for (int yi = 0; yi < numYPoints; yi++) {
				if (yi == 0) {
					deltaY = Math.Abs(_y[1] - _y[0]);
				}
				else {
					deltaY = Math.Abs(_y[yi] - _y[yi - 1]);
				}
				if (deltaY > 2.0f * avgDeltaY) {
					deltaY = avgDeltaY;
				}
				for (int xi = 0; xi < numXPoints; xi++) {
					count++;
					if ((count == 50000) || (count % 250000) == 0) {
						Console.Beep(440, 15);
					}

					Color boxColor = _colorScale.GetColor(_z[xi, yi]);

					if (boxColor == _colorScale.BackgroundColor) {
						// if box is same color as background, skip drawing
						continue;
					}

					// If data value is "NoData" then
					//	do not draw a box, just use background color
					if (double.IsNaN(_z[xi, yi])) {
						continue;
					}

					if (xi == 0) {
						deltaX = Math.Abs(_x[1] - _x[0]);
					}
					else if (xi == (numXPoints - 1)) {
						deltaX = Math.Abs(_x[numXPoints - 1] - _x[numXPoints - 2]);
					}
					else {
						deltaX = Math.Abs(_x[xi + 1] - _x[xi - 1]) / 2.0;
					}
					if (deltaX > 2.0f * avgDeltaX) {
						deltaX = avgDeltaX;
					}
					//
					// NOTE: be careful of y +/- deltaY -- depends on direction of y axis ???? !!!
					//	For BoxObj(x,y,...), (x,y) is always upper left corner of box.
					//	So XAxis.IsReverse and YAxis.IsReverse must be specified before calling BoxPlot,
					//		so that we can draw the box centered on the proper coordinate point.
					//
					double halfWidth = deltaX / 2.0;
					double halfHeight = deltaY / 2.0;
					if (pane.YAxis.Scale.IsReverse) {
						halfHeight = -halfHeight;
					}
					if (pane.XAxis.Scale.IsReverse) {
						halfWidth = -halfWidth;
					}
					BoxObj box1 = new BoxObj(_x[xi] - halfWidth, _y[yi] + halfHeight,
						1.05 * deltaX, 1.05 * deltaY, Color.Empty, boxColor);
					/*   old form:
					ZedGraph.BoxItem box1 = new ZedGraph.BoxObj(new RectangleF((float)x[xi] - (float)deltaX / 2.0f, (float)y[yi] + (float)deltaY / 2.0f, deltaX, deltaY),
												Color.Empty, boxColor);
					*/
					if (_isClipped) {
						box1.IsClippedToChartRect = true;  // retired property $Revision:1.7$
					}
					//box1.Border = boxBorder;
					box1.ZOrder = ZOrder.E_BehindCurves;
					//box1.Fill = new ZedGraph.Fill(colorScale.GetColor(z[xi, yi]));
					//box1.Fill = new ZedGraph.Fill(Color.Pink);
					// location of box seems to be for the upper left corner, regardless of the Align setting
					//box1.Location = new ZedGraph.Location((float)x[xi]-deltaX/2.0f, (float)y[yi]+deltaY/2.0f, deltaX, deltaY, ZedGraph.CoordType.AxisXYScale,
					//                                        ZedGraph.AlignH.Center, ZedGraph.AlignV.Center);

					box1.Draw(g, pane, 1.0f);
					//pane.GraphObjList.Add(box1);
				}
			}

			//Axis.Default.MaxGrace = 0.05;

			//Fill fill = pane.Chart.Fill = new Fill(_colorScale.BackgroundColor);

			return;
		}


		/// <summary>
		/// Draw a legend key entry for this <see cref="LineItem"/> at the specified location.
		/// This is called from the Draw() method of the GraphPane's Legend object.
		/// It only draws the specific curve's line and symbol properties.
		/// Specify an empty label in the base constructor for this curve in order to avoid creating
		///		a standard curve legend.
		/// </summary>
		/// <param name="g">
		/// A graphic device object to be drawn into.  This is normally e.Graphics from the
		/// PaintEventArgs argument to the Paint() method.
		/// </param>
		/// <param name="pane">
		/// A reference to the <see cref="ZedGraph.GraphPane"/> object that is the parent or
		/// owner of this object.
		/// </param>
		/// <param name="rect">The <see cref="RectangleF"/> struct that specifies the
		/// location for the legend key</param>
		/// <param name="scaleFactor">
		/// The scaling factor to be used for rendering objects.  This is calculated and
		/// passed down by the parent <see cref="ZedGraph.GraphPane"/> object using the
		/// <see cref="PaneBase.CalcScaleFactor"/> method, and is used to proportionally adjust
		/// font sizes, etc. according to the actual size of the graph.
		/// </param>
		override public void DrawLegendKey(Graphics g, GraphPane pane, RectangleF rect, float scaleFactor) {
			;
		}
		#endregion (abstract draw method implementations)

		#region Public methods
		//
		public void FindDataMinMax(out double min, out double max) {
			//min = double.NaN;
			//max = double.NaN;
			if ((double.IsNaN(_min)) || (double.IsNaN(_max))) {
				int cx = _x.Length;
				int cy = _y.Length;
				_min = double.MaxValue;
				_max = double.MinValue;
				for (int i = 0; i < cx; i++) {
					for (int j = 0; j < cy; j++) {
						if (!double.IsNaN(_z[i,j])) {
							if (_z[i,j] < _min) {
								_min = _z[i, j];
							}
							if (_z[i,j] > _max) {
								_max = _z[i, j];
							}
						}
					}
				}
			}
			min = _min;
			max = _max;
		}
		//
		#endregion Public methods

	}	// end of class SurfaceColorPlot 

	#endregion  (class SurfaceColorPlot)

	#region SurfaceContourPlot class

	public class SurfaceContourPlot : SurfaceItem {

		private double _min, _max;

		#region Fields
		private bool _isFilled;

		#endregion

		#region Properties
		protected bool IsFilled {
			get { return _isFilled; }
			set { _isFilled = value; }
		}
		#endregion

		#region Constructors

		public SurfaceContourPlot(): base() {
			Init();
		}

		public SurfaceContourPlot(double[] x, double[] y, double[,] z, ColorScale colorScale, bool isFilled)
			: base(x, y, z, colorScale)  {
			Init();
			_isFilled = isFilled;
		}

		public SurfaceContourPlot(double[] x, double[] y, double[,] z, ColorScale colorScale) 
			: base(x, y, z, colorScale)  {
			Init();
		}

		/// <summary>
		/// Internal initialization routine thats sets some initial values to defaults.
		/// </summary>
		private void Init() {
			_isFilled = true;
		}

		#endregion

		#region Implementation of abstract base class methods
		/// <summary>
		/// Do all rendering associated with this <see cref="LineItem"/> to the specified
		/// <see cref="Graphics"/> device.  This method is normally only
		/// called by the Draw method of the parent <see cref="ZedGraph.CurveList"/>
		/// collection object.
		/// </summary>
		/// <param name="g">
		/// A graphic device object to be drawn into.  This is normally e.Graphics from the
		/// PaintEventArgs argument to the Paint() method.
		/// </param>
		/// <param name="pane">
		/// A reference to the <see cref="ZedGraph.GraphPane"/> object that is the parent or
		/// owner of this object.
		/// </param>
		/// <param name="pos">The ordinal position of the current <see cref="Bar"/>
		/// curve.</param>
		/// <param name="scaleFactor">
		/// The scaling factor to be used for rendering objects.  This is calculated and
		/// passed down by the parent <see cref="ZedGraph.GraphPane"/> object using the
		/// <see cref="PaneBase.CalcScaleFactor"/> method, and is used to proportionally adjust
		/// font sizes, etc. according to the actual size of the graph.
		/// </param>
		override public void Draw(Graphics g, GraphPane pane, int pos, float scaleFactor) {

			// find maximum contour
			double stepContour = _colorScale.ContourStep;
			double minContour = _colorScale.MinValue;
			int contourCount = 1;
			double maxContour = minContour;
			double maxZ = double.NegativeInfinity;
			for (int iy = 0; iy < _y.Length; iy++) {
				for (int ix = 0; ix < _x.Length; ix++) {
					if (_z[ix, iy] > maxZ) {
						maxZ = _z[ix, iy];
					}
				}
			}
			if (stepContour <= 0.0) {
				int numColors = _colorScale.ColorSchemeDefinition.ColorValue.Count;
				if (_colorScale._extendLower) {
					numColors -= 1;
				}
				//stepContour = (maxZ - minContour) / numColors;
				stepContour = (_colorScale.MaxValue - minContour) / numColors;
				_colorScale.ContourStep = stepContour;
			}
			while (maxZ > maxContour) {
				maxContour += stepContour;
				contourCount++;
			}

			// make colorScale match the contours
			if (_colorScale.ContourStep == 0.0) {
				_colorScale.ContourStep = 1.0;
			}
			if (!_colorScale.UserSetDataMinMax) {
				_colorScale.MaxValue = maxContour;
				/*
				if (_colorScale.FixContourColors) {
					if (_colorScale.ExtendLower) {
						// first color is below first contour, so there is one less color in the range
						_colorScale.MaxValue = minContour + (colorScale.ColorSchemeDefinition.ColorValue.Count - 1) * stepContour;
					}
					else {
						_colorScale.MaxValue = minContour + colorScale.ColorSchemeDefinition.ColorValue.Count * stepContour;
					}
				}
				*/
			}

			// examine color scale
			// number of contour colors needed to cover entire range (round up)
			int numContours = (int)Math.Ceiling((_colorScale.MaxValue - _colorScale.MinValue) / _colorScale.ContourStep);
			if (_colorScale._extendLower) {
				numContours -= 1;
			}
			if (numContours > _colorScale.ColorSchemeDefinition.ColorValue.Count) {
				// need more colors than available,
				// must either repeat colors or interpolate colors
				if (!_colorScale.BlendColors && !_colorScale.UseEachContourColorStep) {
					// neither option was set, so force one 
					//_colorScale.BlendColors = true;
					_colorScale.UseEachContourColorStep = true;
					_colorScale.RepeatUpper = true;
				}

			}

			ContourBox contourBox = new ContourBox(g, pane, _x, _y, _z, _colorScale, _isFilled);


			for (int iy = 0; iy < _y.Length - 1; iy++) {
				for (int ix = 0; ix < _x.Length - 1; ix++) {
					contourBox.DrawContours(ix, iy);

				}   
			}   

		}  // end of Draw method


		/// <summary>
		/// 
		/// </summary>
		private class ContourBox {

			//public int XIndex, YIndex;
			//public double Contour;

			private Graphics _g;
			private bool _canSkip;	// don't need to draw if same color as background
			private bool _firstContour;

			private double[] X, Y;
			private double[,] Z;
			private ColorScale _colorScale;
			private double X0, X1;
			private double Y0, Y1;
			private double Z0, Z1, Z2, Z3;
			//private ZedGraph.ZedGraphControl _zg;
			private bool _isFilled;
			private GraphPane _pane;


			private PointD[] _polygon3;
			private PointD[] _polygon4;
			private PointD[] _polygon5;
			private PointD[] _polygon;
			private PointD[] _contourPolygon;
			private double[] c;
			private Location _contourLocation;

			//private double _Contour;
			//private int _contourNumber;

			//private int _count;
			private float DELTA = 0.000f;

			public ContourBox(Graphics g, GraphPane pane, double[] x, double[] y, double[,] z,
								ColorScale colorScale, bool isFilled) {
				_g = g;

				X = x;
				Y = y;
				Z = z;
				_pane = pane;
				//_zg = z1;
				_isFilled = isFilled;
				_colorScale = colorScale;
				_polygon3 = new PointD[3];
				_polygon4 = new PointD[4];
				_polygon5 = new PointD[5];
				c = new double[4];
				//_count = 0;

				double[] a = new double[3];
				a[0] = 3.0;
				double[] b = new double[5];
				b[0] = 5.0;
				double[] array;
				array = new double[a.Length];
				a.CopyTo(array, 0);
				a[0] = 99.0;
				double ff = array[0];

				_contourLocation = new Location();
				_contourLocation.CoordinateFrame = CoordType.AxisXYScale;

				if (colorScale.ColorBelowMin == colorScale.BackgroundColor) {
					_canSkip = true;
				}
				else {
					_canSkip = false;
				}

			}

			/// <summary>
			/// Draw all the contours passing through an elemental box.
			/// </summary>
			/// <param name="XIndex"></param>
			/// <param name="YIndex"></param>
			/// <remarks>
			/// We start with the lowest contour level, colorScale.MinValue,
			///		(contourNumber = 1) and work upwards.
			/// When we reach the first contour level that passes through the box,
			///		we fill the box with the previous contour color,
			///		color[contourNumber-1], where color[0] is the 
			///		colorScale.ColorBelowMin.
			/// Then for this contour level and all others that also pass through
			///		the box, we draw a polygon over the higher z-valued part
			///		of the box with color[contourNumber].
			/// </remarks>
			public void DrawContours(int XIndex, int YIndex) {


				X0 = X[XIndex];
				X1 = X[XIndex + 1];
				Y0 = Y[YIndex];
				Y1 = Y[YIndex + 1];
				Z0 = Z[XIndex, YIndex];
				Z1 = Z[XIndex, YIndex + 1];
				Z2 = Z[XIndex + 1, YIndex + 1];
				Z3 = Z[XIndex + 1, YIndex];

				double deltaX = (X1 - X0) / 20.0;
				double deltaY = (Y1 - Y0) / 20.0;

				bool done = false;
				_firstContour = true;

				// If any of the 4 Z's are NaN (no data) then
				//	fill entire box with NoData color
				//	which is the background color of the graph pane.
				if (double.IsNaN(Z0) || double.IsNaN(Z1) || double.IsNaN(Z2) || double.IsNaN(Z3)) {
					return;
				}

				double Contour = _colorScale.MinValue;
				int contourNumber = 1;

				while (!done) {

					X0 -= 0.0 * deltaX * contourNumber;
					X1 += 0.0 * deltaX * contourNumber;
					Y0 -= 0.0 * deltaY * contourNumber;
					Y1 += 0.0 * deltaY * contourNumber;

					int cflag = 0;
					// c[] 0->3 lower left clockwise
					// diff between contour and data value at corner
					if ((c[0] = Contour - Z0) >= 0.0)
						cflag |= 0x8;	// lower left corner is below contour

					if ((c[1] = Contour - Z1) >= 0.0)
						cflag |= 0x4;	// upper left

					if ((c[2] = Contour - Z2) >= 0.0)
						cflag |= 0x2;	// upper right

					if ((c[3] = Contour - Z3) >= 0.0)
						cflag |= 0x1;	// lower right

					if (contourNumber == 0) {
						// have done all contours (have gone past the lowest one)
						//	so, pretend this contour is below all data
						//	and do not draw any more contours (just fill background)
						cflag = 0;
						if (_canSkip) {
							return ;
						}
					}

					switch (cflag) {
						case 0xf:    // contour above all pts, finished
							if (_firstContour) {
								case0(Contour, contourNumber - 1);
								_firstContour = false;
							}
							done = true;
							//skip = true;
							break;

						case 0x1:    // lo,lo,lo,hi 
						case 0xe:    // hi,hi,hi,lo 
							if (_firstContour) {
								case0(Contour, contourNumber - 1);
								_firstContour = false;
							}
							case6(Contour, contourNumber);
							break;

						case 0x2:    // lo,lo,hi,lo 
						case 0xd:    // hi,hi,lo,hi 
							if (_firstContour) {
								case0(Contour, contourNumber - 1);
								_firstContour = false;
							}
							case4(Contour, contourNumber);
							break;

						case 0x3:    // lo,lo,hi,hi 
						case 0xc:    // hi,hi,lo,lo 
							if (_firstContour) {
								case0(Contour, contourNumber - 1);
								_firstContour = false;
							}
							case3(Contour, contourNumber);
							break;

						case 0x4:    // lo,hi,lo,lo 
						case 0xb:    // hi,lo,hi,hi 
							if (_firstContour) {
								case0(Contour, contourNumber - 1);
								_firstContour = false;
							}
							case2(Contour, contourNumber);
							break;

						case 0x5:    // lo,hi,lo,hi 
						case 0xa:    // hi,lo,hi,lo 
							if (_firstContour) {
								case0(Contour, contourNumber - 1);
								_firstContour = false;
							}
							casex(Contour, contourNumber);
							break;

						case 0x6:    // lo,hi,hi,lo 
						case 0x9:    // hi,lo,lo,hi 
							if (_firstContour) {
								case0(Contour, contourNumber - 1);
								_firstContour = false;
							}
							case5(Contour, contourNumber);
							break;

						case 0x7:    // lo,hi,hi,hi 
						case 0x8:    // hi,lo,lo,lo 
							if (_firstContour) {
								case0(Contour, contourNumber - 1);
								_firstContour = false;
							}
							case1(Contour, contourNumber);
							break;

						case 0x0:	// contour below all pts
							//case0(Contour, contourNumber);
							break;
					}    // end of switch 

					contourNumber++;
					Contour += _colorScale.ContourStep;

					
				}  // end of while(!done)

				return;

			}

			private void DrawPolygon(double Contour, int contourNumber) {
				// (aren't really using Contour parameter any more)
				Color polyColor, polyColor2;
				polyColor2 = _colorScale.GetColor(Contour);

				if (_colorScale.UseEachContourColorStep) {
					// get color by using each defined color index per contour interval

					int colorIndex = contourNumber - 1;
					if (_colorScale.ExtendLower) {
						// first contour uses 2nd color, 1st color is background
						colorIndex = colorIndex + 1;
					}
					if (colorIndex >= _colorScale.ColorSchemeDefinition.ColorValue.Count) {
						if (!_colorScale.RepeatUpper) {
							colorIndex = _colorScale.ColorSchemeDefinition.ColorValue.Count - 1;
						}
						else {
							while (colorIndex >= _colorScale.ColorSchemeDefinition.ColorValue.Count) {
								colorIndex -= _colorScale.ColorSchemeDefinition.ColorValue.Count;
							}
						}
					}
					if (colorIndex >= 0) {
						polyColor = _colorScale.ColorSchemeDefinition.ColorValue[colorIndex];
					}
					else {
						polyColor = _colorScale.ColorBelowMin;
					}
				}
				else {
					//
					// Get color by interpolating across min/max range of values
					//
					int totalContours = (int)((_colorScale.MaxValue - _colorScale.MinValue) / _colorScale.ContourStep + 0.5);
					if (_colorScale.ExtendLower) {
						contourNumber += 1;
					}
					int percentColor = (int)(100.0 * (contourNumber - 1) / (totalContours - 1) + 0.5);
					if (contourNumber > 0) {
						if (percentColor < 0) {
							polyColor = _colorScale.ColorBelowMin;
						}
						else if (percentColor > 100) {
							if (_colorScale.RepeatUpper) {
								percentColor = percentColor % 100;
								polyColor = _colorScale.GetColor(percentColor);
							}
							else {
								polyColor = _colorScale.GetColor(100);
							}
						}
						else {
							polyColor = _colorScale.GetColor(percentColor);
						}
					}
					else {
						polyColor = _colorScale.ColorBelowMin;
					}
				}
				//
				//

				if (_firstContour) {
					if (polyColor == _colorScale.BackgroundColor) {
						return;
					}
				}
				//  for testing
				//polyColor = polyColor2;

				{
					if (_isFilled) {
						int cc = contourNumber;
						_contourPolygon = new PointD[_polygon.Length];
						_polygon.CopyTo(_contourPolygon, 0);
						//ZedGraph.PolyObj poly = new ZedGraph.PolyObj(_contourPolygon, Color.Empty, polyColor);
						PolyObj poly = new PolyObj(_contourPolygon, Color.Empty, polyColor);
						poly.IsClippedToChartRect = true;
						//box1.Border = boxBorder;
						poly.ZOrder = ZOrder.E_BehindCurves;
						//_pane.GraphObjList.Add(poly);
						poly.Draw(_g, _pane, 1.0f);

					}
					else {
						if (_contourLocation.Width != 0) {
							ArrowObj contourLine = new ArrowObj();
							contourLine.Location = (Location)_contourLocation.Clone();
							contourLine.IsArrowHead = false;
							//contourLine.Style = System.Drawing.Drawing2D.DashStyle.Solid;  // removed $Revision:1.7$
							//contourLine.Color = polyColor;  // removed $Revision:1.7$
							contourLine.ZOrder = ZOrder.E_BehindCurves;
							contourLine.IsClippedToChartRect = true;
							_pane.GraphObjList.Add(contourLine);

						}
					}
				}
			}  // end method DrawContour

			/// <summary>
			/// Interpolates to find the coordinate of the zero value
			/// between two coordinates x1 and x2 that have
			/// data values v1 and v2
			/// </summary>
			/// <param name="v1"></param>
			/// <param name="v2"></param>
			/// <param name="x1"></param>
			/// <param name="x2"></param>
			/// <returns></returns>
			private double interpolate(double v1, double v2, double x1, double x2) {
				double frac = -v1 / (v2 - v1);
				return x1 + frac * (x2 - x1);
			}
			//#define yintrp(a,b)  (short int)((fyinc)*(a)/((a)-(b)))+yy0-yinc

			/// <summary>
			/// Fill in entire box with background color
			/// </summary>
			private void case0(double Contour, int contourNumber) {
				_polygon4[0].X = (float)X0;
				_polygon4[0].Y = (float)Y0;
				_polygon4[1].X = (float)X0;
				_polygon4[1].Y = (float)Y1;
				_polygon4[2].X = (float)X1;
				_polygon4[2].Y = (float)Y1;
				_polygon4[3].X = (float)X1;
				_polygon4[3].Y = (float)Y0;
				_contourLocation.X1 = 0.0f;
				_contourLocation.Y1 = 0.0f;
				_contourLocation.Width = 0.0f;
				_contourLocation.Height = 0.0f;
				double width = _contourLocation.Width;
				_polygon = _polygon4;
				DrawPolygon(Contour, contourNumber);
			}

			/// <summary>
			/// Draw contour from left side to bottom side .
			/// </summary>
			private void case1(double Contour, int contourNumber) {

				double Y2 = interpolate(c[1], c[0], Y1, Y0);
				double X2 = interpolate(c[0], c[3], X0, X1);

				_contourLocation.X1 = (float)X0;
				_contourLocation.Y1 = (float)Y2;
				_contourLocation.Width = (float)X2 - _contourLocation.X1;
				_contourLocation.Height = (float)Y0 - _contourLocation.Y1;

				if (c[0] < (float)0.0) {
					// lower left corner is high
					_polygon3[0].X = (float)X0;
					_polygon3[0].Y = (float)Y0;
					_polygon3[1].X = (float)X0;
					_polygon3[1].Y = (float)Y2;
					_polygon3[2].X = (float)X2;
					_polygon3[2].Y = (float)Y0;
					_polygon = _polygon3;
					//pDC->Polygon(side, 3);
				}
				else {
					// lower left corner is low
					_polygon5[0].X = (float)X0;
					_polygon5[0].Y = (float)Y2;
					_polygon5[1].X = (float)X2;
					_polygon5[1].Y = (float)Y0;
					_polygon5[2].X = (float)X1;
					_polygon5[2].Y = (float)Y0;
					_polygon5[3].X = (float)X1;
					_polygon5[3].Y = (float)Y1;
					_polygon5[4].X = (float)X0;
					_polygon5[4].Y = (float)Y1;
					_polygon = _polygon5;
					//pDC->Polygon(side, 5);
				}
				DrawPolygon(Contour, contourNumber);
			}

			/// <summary>
			/// Draw contour from left side to top side .
			/// </summary>
			private void case2(double Contour, int contourNumber) {

				double Y2 = interpolate(c[1], c[0], Y1, Y0);
				double X2 = interpolate(c[1], c[2], X0, X1);

				_contourLocation.X1 = (float)X0;
				_contourLocation.Y1 = (float)Y2;
				_contourLocation.Width = (float)X2 - _contourLocation.X1;
				_contourLocation.Height = (float)Y1 - _contourLocation.Y1;

				if (c[1] < (float)0.0) {
					// upper left corner is high
					_polygon3[0].X = (float)X0;
					_polygon3[0].Y = (float)Y1 + DELTA;
					_polygon3[1].X = (float)X0;
					_polygon3[1].Y = (float)Y2;
					_polygon3[2].X = (float)X2;
					_polygon3[2].Y = (float)Y1 + DELTA;
					_polygon = _polygon3;
					//pDC->Polygon(side, 3);
				}
				else {
					// upper left corner is low
					_polygon5[0].X = (float)X0;
					_polygon5[0].Y = (float)Y2;
					_polygon5[1].X = (float)X2;
					_polygon5[1].Y = (float)Y1;
					_polygon5[2].X = (float)X1;
					_polygon5[2].Y = (float)Y1;
					_polygon5[3].X = (float)X1;
					_polygon5[3].Y = (float)Y0;
					_polygon5[4].X = (float)X0;
					_polygon5[4].Y = (float)Y0;
					_polygon = _polygon5;
					//pDC->Polygon(side, 5);
				}
				DrawPolygon(Contour, contourNumber);
			}

			/// <summary>
			/// Draw contour from top to bottom .
			/// </summary>
			private void case3(double Contour, int contourNumber) {

				double X2H = interpolate(c[1], c[2], X0, X1);
				double X2L = interpolate(c[0], c[3], X0, X1);

				_contourLocation.X1 = (float)X2H;
				_contourLocation.Y1 = (float)Y1;
				_contourLocation.Width = (float)X2L - _contourLocation.X1;
				_contourLocation.Height = (float)Y0 - _contourLocation.Y1;

				if (c[1] < (float)0.0) {
					// left side is high
					// fill to left
					_polygon4[0].X = (float)X0;
					_polygon4[0].Y = (float)Y0;
					_polygon4[1].X = (float)X0;
					_polygon4[1].Y = (float)Y1;
					_polygon4[2].X = (float)X2H;
					_polygon4[2].Y = (float)Y1;
					_polygon4[3].X = (float)X2L;
					_polygon4[3].Y = (float)Y0;
					_polygon = _polygon4;
					//pDC->Polygon(side, 4);
				}
				else {
					// right side is high
					_polygon4[0].X = (float)X2L;
					_polygon4[0].Y = (float)Y0;
					_polygon4[1].X = (float)X2H;
					_polygon4[1].Y = (float)Y1;
					_polygon4[2].X = (float)X1;
					_polygon4[2].Y = (float)Y1;
					_polygon4[3].X = (float)X1;
					_polygon4[3].Y = (float)Y0;
					_polygon = _polygon4;
					//pDC->Polygon(side, 4);
				}
				DrawPolygon(Contour, contourNumber);
			}

			/// <summary>
			/// Draw contour from top to right .
			/// </summary>
			private void case4(double Contour, int contourNumber) {

				double Y2 = interpolate(c[3], c[2], Y0, Y1);
				double X2 = interpolate(c[1], c[2], X0, X1);

				_contourLocation.X1 = (float)X2;
				_contourLocation.Y1 = (float)Y1;
				_contourLocation.Width = (float)X1 - _contourLocation.X1;
				_contourLocation.Height = (float)Y2 - _contourLocation.Y1;

				if (c[2] < (float)0.0) {
					// upper right corner is high
					_polygon3[0].X = (float)X2;
					_polygon3[0].Y = (float)Y1;
					_polygon3[1].X = (float)X1;
					_polygon3[1].Y = (float)Y1;
					_polygon3[2].X = (float)X1;
					_polygon3[2].Y = (float)Y2;
					_polygon = _polygon3;
					//pDC->Polygon(side, 3);
				}
				else {
					// upper right corner is low
					_polygon5[0].X = (float)X0;
					_polygon5[0].Y = (float)Y0;
					_polygon5[1].X = (float)X0;
					_polygon5[1].Y = (float)Y1;
					_polygon5[2].X = (float)X2;
					_polygon5[2].Y = (float)Y1;
					_polygon5[3].X = (float)X1;
					_polygon5[3].Y = (float)Y2;
					_polygon5[4].X = (float)X1;
					_polygon5[4].Y = (float)Y0;
					_polygon = _polygon5;
					//pDC->Polygon(side, 5);
				}
				DrawPolygon(Contour, contourNumber);
			}

			/// <summary>
			/// Draw contour from left to right .
			/// </summary>
			private void case5(double Contour, int contourNumber) {

				double Y2L = interpolate(c[0], c[1], Y0, Y1);
				double Y2R = interpolate(c[3], c[2], Y0, Y1);

				_contourLocation.X1 = (float)X0;
				_contourLocation.Y1 = (float)Y2L;
				_contourLocation.Width = (float)X1 - _contourLocation.X1;
				_contourLocation.Height = (float)Y2R - _contourLocation.Y1;

				if (c[0] < (float)0.0) {
					// bottom side is high
					// fill to bottom
					_polygon4[0].X = (float)X0;
					_polygon4[0].Y = (float)Y0;
					_polygon4[1].X = (float)X0;
					_polygon4[1].Y = (float)Y2L;
					_polygon4[2].X = (float)X1;
					_polygon4[2].Y = (float)Y2R;
					_polygon4[3].X = (float)X1;
					_polygon4[3].Y = (float)Y0;
					_polygon = _polygon4;
					//pDC->Polygon(side, 4);
				}
				else {
					// top side is high
					_polygon4[0].X = (float)X0;
					_polygon4[0].Y = (float)Y2L;
					_polygon4[1].X = (float)X0;
					_polygon4[1].Y = (float)Y1;
					_polygon4[2].X = (float)X1;
					_polygon4[2].Y = (float)Y1;
					_polygon4[3].X = (float)X1;
					_polygon4[3].Y = (float)Y2R;
					_polygon = _polygon4;
					//pDC->Polygon(side, 4);
				}
				DrawPolygon(Contour, contourNumber);
			}

			/// <summary>
			/// Draw contour from right to bottom .
			/// </summary>
			private void case6(double Contour, int contourNumber) {

				double Y2 = interpolate(c[3], c[2], Y0, Y1);
				double X2 = interpolate(c[0], c[3], X0, X1);

				_contourLocation.X1 = (float)X2;
				_contourLocation.Y1 = (float)Y0;
				_contourLocation.Width = (float)X1 - _contourLocation.X1;
				_contourLocation.Height = (float)Y2 - _contourLocation.Y1;

				if (c[3] < (float)0.0) {
					// lower right corner is high
					_polygon3[0].X = (float)X2;
					_polygon3[0].Y = (float)Y0;
					_polygon3[1].X = (float)X1;
					_polygon3[1].Y = (float)Y0;
					_polygon3[2].X = (float)X1;
					_polygon3[2].Y = (float)Y2;
					_polygon = _polygon3;
					//pDC->Polygon(side, 3);
				}
				else {
					// lower right corner is low
					_polygon5[0].X = (float)X0;
					_polygon5[0].Y = (float)Y0;
					_polygon5[1].X = (float)X0;
					_polygon5[1].Y = (float)Y1;
					_polygon5[2].X = (float)X1;
					_polygon5[2].Y = (float)Y1;
					_polygon5[3].X = (float)X1;
					_polygon5[3].Y = (float)Y2;
					_polygon5[4].X = (float)X2;
					_polygon5[4].Y = (float)Y0;
					_polygon = _polygon5;
					//pDC->Polygon(side, 5);
				}
				DrawPolygon(Contour, contourNumber);
			}

			/// <summary>
			/// Draw 2 contour lines .
			/// Always make a valley, rather than ridge
			/// </summary>
			private void casex(double Contour, int contourNumber) {
				if (c[0] < (float)0.0) {
					// lower left corner is high
					case1(Contour, contourNumber);
					case4(Contour, contourNumber);
				}
				else {
					// lower left corner is low
					case2(Contour, contourNumber);
					case6(Contour, contourNumber);
				}
			}


		}  // end class ContourBox

		/// <summary>
		/// Draw a legend key entry for this <see cref="LineItem"/> at the specified location
		/// </summary>
		/// <param name="g">
		/// A graphic device object to be drawn into.  This is normally e.Graphics from the
		/// PaintEventArgs argument to the Paint() method.
		/// </param>
		/// <param name="pane">
		/// A reference to the <see cref="ZedGraph.GraphPane"/> object that is the parent or
		/// owner of this object.
		/// </param>
		/// <param name="rect">The <see cref="RectangleF"/> struct that specifies the
		/// location for the legend key</param>
		/// <param name="scaleFactor">
		/// The scaling factor to be used for rendering objects.  This is calculated and
		/// passed down by the parent <see cref="ZedGraph.GraphPane"/> object using the
		/// <see cref="PaneBase.CalcScaleFactor"/> method, and is used to proportionally adjust
		/// font sizes, etc. according to the actual size of the graph.
		/// </param>
		override public void DrawLegendKey(Graphics g, GraphPane pane, RectangleF rect, float scaleFactor) {
			;
		}
		#endregion (abstract draw method implementations)

		#region Public methods
		//
		public void FindDataMinMax(out double min, out double max) {
			//min = double.NaN;
			//max = double.NaN;
			if ((double.IsNaN(_min)) || (double.IsNaN(_max))) {
				int cx = _x.Length;
				int cy = _y.Length;
				_min = double.MaxValue;
				_max = double.MinValue;
				for (int i = 0; i < cx; i++) {
					for (int j = 0; j < cy; j++) {
						if (!double.IsNaN(_z[i,j])) {
							if (_z[i, j] < _min) {
								_min = _z[i, j];
							}
							if (_z[i, j] > _max) {
								_max = _z[i, j];
							}
						}
					}
				}
			}
			min = _min;
			max = _max;
		}
		//
		#endregion Public methods



	}	// end of class SurfaceContourPlot 

	#endregion SurfaceContourPlot class
}
