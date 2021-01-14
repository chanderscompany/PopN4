using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using ZedGraph;

namespace DACarter.Utilities.Graphics {

	public class QuickPlotZ { 

		public enum PlotType {
			None,
			StackedLine,
			FilledBox,
			Contour
		}

		private QuickPlotForm _form;
		private ZedGraph.ZedGraphControl _z1;
		private GraphPane _legendPane, _graphPane;
		private bool _needsLegend;
		private ColorScale _colorScale;
		private double[] _x, _y;
		private double[,] _z;
		//private double _minContour;
		//private double _stepContour;
		private bool _isFilled;
		//private int _flag, _prevFlag;
		private bool _isClipped;
		private int _userChangedColorHash, _prevHash;
		private bool _addX2Axis;
		private string _X2AxisTitle;
		private double _X2AxisMin, _X2AxisMax;
		private string _commentLabel = "";
		private Fill _paneFill;

        public Form Form {
            get { return _form; }
        }

		public Fill PaneFill {
			get { return _paneFill; }
			set { _paneFill = value; }
		}

		public string CommentLabel {
			get { return _commentLabel; }
			set { _commentLabel = value; }
		}


		private PlotType _plotType;

		public PlotType TypeOfPlot {
			get { return _plotType; }
			set { _plotType = value; }
		}

		public ColorScale PlotColorScale {
			get { return _colorScale; }
			set { _colorScale = value; }
		}

		public ZedGraph.ZedGraphControl GraphControl {
			get { return _z1; }
			set { _z1 = value; }
		}

		/*
		public int Flag {
			// Flag can be set to anything by calling routine.
			// If flag is changed, ColorScale reverts to default values.
			get { return _flag; }
			set { _flag = value; }
		}
		*/

		public QuickPlotZ() {
			InitGraphObjects();
		}

		private void InitGraphObjects() {
			_form = new QuickPlotForm();

			// _form_FormClosed() method will handle _form Closed event
			_form.FormClosed += new FormClosedEventHandler(_form_FormClosed);

			_z1 = new ZedGraph.ZedGraphControl();

			// default background color outside of plot area
			PaneFill = new ZedGraph.Fill(Color.WhiteSmoke, Color.Lavender, 0F);

			_z1.MasterPane = new MasterPane();
			// pane for main plot
			_graphPane = new GraphPane();
			_z1.MasterPane.Add(_graphPane);
			_z1.MasterPane.Margin.All = 0.0f;
			_z1.MasterPane.InnerPaneGap = 1.0f;
			_z1.MasterPane.Title.IsVisible = false;

			// pane for contour legend
			_legendPane = new GraphPane();
			_legendPane.Title.Text = "Legend";
			_legendPane.XAxis.IsVisible = false;
			_legendPane.YAxis.MinorTic.IsOutside = true;
			_legendPane.YAxis.MinorTic.IsInside = false;
			_legendPane.YAxis.MinorTic.IsOpposite = false;
			_legendPane.YAxis.MajorTic.IsInside = false;
			_legendPane.YAxis.MajorTic.IsCrossOutside = false;
			_legendPane.YAxis.MajorTic.IsOpposite = false;
			_legendPane.YAxis.MajorGrid.IsZeroLine = false;
			//_legendPane.BaseDimension = 2.5f;
			_legendPane.BaseDimension = 1.8f;
			_legendPane.BaseDimension = 2.3f;
			_legendPane.Margin.Right = 30f;
			//_legendPane.MarginTop = 50f;
			_legendPane.Margin.Top = 100f;
			_legendPane.Margin.Bottom = 150f;
			//_legendPane.IsFontsScaled = false;

			_graphPane.Border = new Border(false, Color.Empty, 1.0f);
			_legendPane.Border = new Border(false, Color.Empty, 1.0f);


			//_z1.MasterPane.Add(_legendPane);

			_needsLegend = false;
			_colorScale = null;
			_plotType = PlotType.None;
			_colorScale = null;
			//_flag = _prevFlag = -99;
			_userChangedColorHash = _prevHash = -1;

			// Tell ZedGraph to auto layout all the panes
			System.Drawing.Graphics g = _z1.CreateGraphics();
			// -- specify 2 columns,  proportional column widths
			//_z1.MasterPane.AutoPaneLayout(g, false, new int[] { 1, 3 }, new float[] { 10f, 1f });	// 3 panes in last column
			_z1.MasterPane.SetLayout(g, false, new int[] { 1, 1 }, new float[] { 8f, 1f });	// 1 pane in each column
			//_z1.MasterPane.SetLayout(g, false, new int[] { 1, 1 }, new float[] { 8f, 3f });	// 1 pane in each column
			_z1.MasterPane.AxisChange(g);
			g.Dispose();

			// OPTIONAL: Show tooltips when the mouse hovers over a point
			_z1.IsShowPointValues = true;
			_z1.PointValueEvent += new ZedGraphControl.PointValueHandler(MyPointValueHandler);

			// OPTIONAL: Add a custom context menu item
			_z1.ContextMenuBuilder += new ZedGraphControl.ContextMenuBuilderEventHandler(MyContextMenuBuilder);

			_addX2Axis = false;

		}

		private void _form_FormClosed(object sender, FormClosedEventArgs e) {
			// do what we can to release resources
			_legendPane = null;
			_graphPane = null;
			_form = null;
			_z1.GraphPane.GraphObjList.Clear();
			_z1.GraphPane = null;
			_z1.Dispose();
			//int gen = GC.GetGeneration(_z1);
			_z1 = null;
			//GC.Collect(gen);
		}

		/// <summary>
		/// Display customized tooltips when the mouse hovers over a point
		/// </summary>
		private string MyPointValueHandler(ZedGraphControl control, GraphPane pane,
						CurveItem curve, int iPt) {
			// Get the PointPair that is under the mouse
			PointPair pt = curve[iPt];

			return curve.Label.Text + " is " + pt.Y.ToString("f2") + " y-units at " + pt.X.ToString("f1") + " x-units";
		}

		/// <summary>
		/// Customize the context menu by adding a new item to the end of the menu
		/// </summary>
		private void MyContextMenuBuilder(ZedGraphControl control, ContextMenuStrip menu, Point mousePt, ZedGraphControl.ContextMenuObjectState objState) {
			MenuItem menuItem = new MenuItem();
			//menuItem.Index = menu.MenuItems.Count + 1;
			menuItem.Index = menu.Items.Count + 1;
			menuItem.Text = "Color Scheme...";

			menu.Items.Add(menuItem.Text);
			menu.Items[menu.Items.Count - 1].Click += new System.EventHandler(ColorMenuItemHandler);
		}

		/// <summary>
		/// Handle the new context menu item. 
		/// </summary>
		private void ColorMenuItemHandler(object sender, EventArgs args) {
			//ClearCurves(); 
			ZoomState currentZoomState = null;
			bool isZoomed = _z1.GraphPane.IsZoomed;

			if (isZoomed) {
				// if the graph was zoomed, save the axes states
				currentZoomState = new ZoomState(_z1.GraphPane, ZoomState.StateType.Zoom);
			}
			CurveList c1 = _z1.GraphPane.CurveList;
			ColorPropertyForm propForm = new ColorPropertyForm();
			propForm.PlotPropertyGrid.SelectedObject = PlotColorScale;
			propForm.ShowDialog();
			CurveList c2 = _z1.GraphPane.CurveList;

			// mark the plot that has color changed
			//_userChangedColorHash = GetHashCode();

			if (_plotType == PlotType.Contour) {
				//DrawContourGraph(_z1.GraphPane, _x, _y, _z, _colorScale, _isFilled);
			}
			else if (_plotType == PlotType.FilledBox) {
				//DrawFilledBoxGraph(_z1.GraphPane, _x, _y, _z, _colorScale, _isClipped);
				//ColorSurfacePlot( _x, _y, _z, _colorScale, _isClipped);
			}
			if (isZoomed) {
				// if graph was zoomed, apply the zoomed axes states to the redrawn display
				currentZoomState.ApplyState(_z1.GraphPane);
			}
			Display();
		}

		public override int GetHashCode() {
			return _x.Length ^ _y.Length ^ _z.Length ^ (int)_plotType ^ _z[0,0].ToString().GetHashCode();
		}

		public void Display() {

            //_form.SetDesktopBounds(1000, 500, 700, 300);
            //_form.DesktopBounds = new Rectangle(1000, 500, 700, 300);

            // When we explicitly display the plot, always draw the image.
			//	(When the control repaints, don't necessariy need to redraw.)
			_z1.OkToRedraw = true;
			
			if (_needsLegend) {
				//double max = PlotColorScale.MaxValue;
				if (_colorScale.UserSetDataMinMax == false) {
					// need to make sure data in legend plot ranges 
					// over colorscale min/max
					// Normally, if not in user set min/max mode,
					//	min/max are determined from data at display time.
					// We need to find min/max now, to set-up legend.
					double min, max;
					if ((_plotType == PlotType.FilledBox)) {
						ZedGraph.SurfaceColorPlot surf = (ZedGraph.SurfaceColorPlot)_z1.MasterPane.PaneList[0].CurveList[0];
						surf.FindDataMinMax(out min, out max);
						_colorScale.MinValue = min;
						_colorScale.MaxValue = max;
					}
					else if ((_plotType == PlotType.Contour)) {
						ZedGraph.SurfaceContourPlot cntr = (ZedGraph.SurfaceContourPlot)_z1.MasterPane.PaneList[0].CurveList[0];
						cntr.FindDataMinMax(out min, out max);
						_colorScale.MinValue = min;
						_colorScale.MaxValue = max;
					}

				}
				DrawLegend(_colorScale);
			}
			else {
				if (_z1.MasterPane.PaneList.Count > 1) {
					_z1.MasterPane.PaneList.RemoveAt(1);
				}
			}

			if ((_plotType == PlotType.Contour) || (_plotType == PlotType.FilledBox)) {
				// need to specify a possibly new plot background fill before plot is drawn.
				_z1.GraphPane.Chart.Fill = new Fill(_colorScale.BackgroundColor);
				if (_legendPane != null) {
					_legendPane.Chart.Fill = _z1.GraphPane.Chart.Fill;
				}
			}
			_z1.MasterPane.Fill = PaneFill;
			foreach (GraphPane pane in _z1.MasterPane.PaneList) {
				// so that MasterPane fill shows through the plot and legend panes
				pane.Fill = new Fill(Color.Transparent);
			}

			if (CommentLabel != String.Empty) {
				TextObj commentObj = new TextObj(CommentLabel, 0.01, 0.99);
				commentObj.Location.CoordinateFrame = CoordType.PaneFraction;
				commentObj.Location.AlignH = AlignH.Left;
				commentObj.Location.AlignV = AlignV.Bottom;
				commentObj.FontSpec.Size = 6;
				commentObj.FontSpec.Border.IsVisible = false;
				commentObj.FontSpec.Fill.IsVisible = false;
				_z1.GraphPane.GraphObjList.Add(commentObj);
			}
			//_z1.GraphPane.Chart.Fill = new ZedGraph.Fill(Color.FromArgb(255, 255, 245));
			//	Color.FromArgb(255, 255, 190), 90F);
			_form.GraphControl = _z1;
			_form.GraphControl.AxisChange();
            _form.StartPosition = FormStartPosition.Manual;
            //_form.DesktopLocation = new Point(0, 0);
            //_form.Location = new Point(700, 300);
            Point loc1 = _form.DesktopLocation;
            _form.Show();
            Point loc = _form.DesktopLocation;
		}

        public void Hide() {
            if (_form != null) {
                _form.Hide();
            }
        }

		private void DrawLegend(ColorScale colorScale) {

			ClearLegend();
			_legendPane.Chart.Fill = new Fill(colorScale.BackgroundColor);

			int nPts = 200;
			double[] xx = new double[2];
			double[] yy = new double[nPts];
			double[,] zz = new double[2, nPts];

			xx[0] = 1.0;
			xx[1] = 2.0;
			double maxY = colorScale.MaxValue;
			double minY = colorScale.MinValue;
			double delta = (maxY - minY) / 15.0;
			_legendPane.YAxis.Scale.Min = minY - delta;
			_legendPane.YAxis.Scale.Max = maxY + delta;
			for (int iy = 0; iy < nPts; iy++) {
				yy[iy] = minY - delta + iy * (maxY - minY + 2 * delta) / (nPts - 1);
				for (int ix = 0; ix < 2; ix++) {
					zz[ix, iy] = yy[iy];
				}
			}
			//colorScale.UserSetDataMinMax = true;
			_legendPane.XAxis.Scale.Min = xx[0];
			_legendPane.XAxis.Scale.Max = xx[1];

			if ((_legendPane.YAxis.Scale.Max < 99.0)) {
				_legendPane.BaseDimension = 1.8f;
			}
			else {
				// try to keep legend bar from shrinking when labels get big
				_legendPane.BaseDimension = 2.3f;
			}
			if (_plotType == PlotType.FilledBox) {
				//ColorScale colorScale = new ColorScale(this.PlotColorScale);
				//colorScale.UserSetDataMinMax = true;
				//ColorSurfacePlot(xx, yy, zz, colorScale, true);
				DrawColorSurfacePlot(_legendPane, xx, yy, zz, colorScale, true);
				if (_z1.MasterPane.PaneList.Count == 1) {
					_z1.MasterPane.Add(_legendPane);
				}

			}
			if (_plotType == PlotType.Contour) {
				/*
				double maxY = PlotColorScale.MaxValue;
				double minY = PlotColorScale.MinValue;
				double delta = (maxY - minY) / 15.0;
				_legendPane.YAxis.Scale.Min = minY - delta;
				_legendPane.YAxis.Scale.Max = maxY + delta;
				if ((_legendPane.YAxis.Scale.Max < 99.0)) {
					_legendPane.BaseDimension = 1.8f;
				}
				else {
					// try to keep legend bar from shrinking when labels get big
					_legendPane.BaseDimension = 2.3f;
				}
				*/
				/*
				int nPts = 200;
				double[] xx = new double[2];
				double[] yy = new double[nPts];
				double[,] zz = new double[2, nPts];
				xx[0] = 1.0;
				xx[1] = 2.0;
				for (int iy = 0; iy < nPts; iy++) {
					yy[iy] = minY - delta + iy * (maxY - minY + 2 * delta) / (nPts - 1);
					for (int ix = 0; ix < 2; ix++) {
						zz[ix, iy] = yy[iy];
					}
				}
				//ColorScale colorScale = new ColorScale(this.PlotColorScale);
				colorScale.UserSetDataMinMax = true;
				*/
				//DrawFilledBoxGraph(_legendPane, xx, yy, zz, colorScale, true);
				DrawContourSurfacePlot(_legendPane, xx, yy, zz, colorScale, _isFilled);
				//DrawContourGraph(_legendPane, xx, yy, zz, colorScale, true);
				if (_z1.MasterPane.PaneList.Count == 1) {
					_z1.MasterPane.Add(_legendPane);
				}

			}
		}

        public void SetTitles(string graphTitle, string xTitle, string yTitle) {
            _z1.GraphPane.Title.Text = graphTitle;
            _z1.GraphPane.XAxis.Title.Text = xTitle;
            _z1.GraphPane.YAxis.Title.Text = yTitle;
        }

        public void SetTitles(string windowTitle, string graphTitle, string xTitle, string yTitle) {
            _form.Text = windowTitle;
            _z1.GraphPane.Title.Text = graphTitle;
            _z1.GraphPane.XAxis.Title.Text = xTitle;
            _z1.GraphPane.YAxis.Title.Text = yTitle;
        }

        public void AddX2Axis(string title, double min, double max) {
			_addX2Axis = true;
			_X2AxisTitle = title;
			_X2AxisMin = min;
			_X2AxisMax = max;
			addX2Axis();
		}

		private void addX2Axis() {
			_z1.GraphPane.X2Axis.IsVisible = true;
			_z1.GraphPane.X2Axis.MajorTic.IsOpposite = false;
			_z1.GraphPane.X2Axis.MinorTic.IsOpposite = false;
			_z1.GraphPane.XAxis.MajorTic.IsOpposite = false;
			_z1.GraphPane.XAxis.MinorTic.IsOpposite = false;
			_z1.GraphPane.X2Axis.Scale.Min = _X2AxisMin;
			_z1.GraphPane.X2Axis.Scale.Max = _X2AxisMax;
			_z1.GraphPane.X2Axis.Title.Text = _X2AxisTitle;
		}

		public void ClearPaneCurves(GraphPane pane) {
			if (pane == null) {
				// Trying to access ZedGraphControl that does not exist;
				//		probably window was closed by user.
				InitGraphObjects();
			}
			pane.CurveList.Clear();
			pane.GraphObjList.Clear();
			pane.XAxis.Scale.MinAuto = true;
			pane.XAxis.Scale.MaxAuto = true;
			pane.YAxis.Scale.MinAuto = true;
			pane.YAxis.Scale.MaxAuto = true;
			_addX2Axis = false;
			pane.X2Axis.IsVisible = false;
		}

		public void ClearLegend() {
			ClearPaneCurves(_legendPane);
		}

		public void ClearPlot() {
			_needsLegend = false;
			if (_z1 != null) {
				ClearPaneCurves(_z1.GraphPane);
			}
			else {
				InitGraphObjects();
			}
		}

		public ZedGraph.LineItem AddCurve(string label, double[] x, double[] y, Color color) {
			if (_z1 == null) {
				// Trying to access ZedGraphControl that does not exist;
				//		probably window was closed by user.
				InitGraphObjects();
			}
			if (x == null) {
				x = new double[y.Length];
				for (int i = 0; i < y.Length; i++) {
					x[i] = (double)i;
				}
			}
			return _z1.GraphPane.AddCurve(label, x, y, color);
		}

		public ZedGraph.LineItem AddCurve(string label, double[] x, double[] y,
											Color color, ZedGraph.SymbolType symbolType) {
			if (_z1 == null) {
				// Trying to access ZedGraphControl that does not exist;
				//		probably window was closed by user.
				InitGraphObjects();
			}
			if (x == null) {
				x = new double[y.Length];
				for (int i = 0; i < y.Length; i++) {
					x[i] = (double)i;
				}
			}
			return _z1.GraphPane.AddCurve(label, x, y, color, symbolType);
		}

		public ZedGraph.LineItem AddCurve(string label, ZedGraph.PointPairList list,
											Color color, ZedGraph.SymbolType symbolType) {
			return _z1.GraphPane.AddCurve(label, list, color, symbolType);
		}

		public ZedGraph.LineItem AddCurve(string label, ZedGraph.PointPairList list, Color color) {
			return _z1.GraphPane.AddCurve(label, list, color);
		}

        public void SetWindowTitle(string title) {
            _form.Text = title;
        }

		public void setSize(int x, int y, bool fixedAspectRatio) {
			_form.FixedAspectRatio = fixedAspectRatio;
			if ((x != 0) && (y != 0)) {
				_z1.Size = new Size(x, y);
				if (fixedAspectRatio) {
					_form.FitGraphToWindow = true;
				}
				else {
					_form.FitGraphToWindow = false;
				}
			}
			else {
				_form.FitGraphToWindow = true;
				_form.FixedAspectRatio = false;
			}
		}

        public void setPosition(Point location, Size size) {
            //setSize(size.Width, size.Height, false);
            if (!size.IsEmpty) {
                _form.SetDesktopBounds(location.X, location.Y, size.Width, size.Height);
            }
            //_form.SetDesktopLocation(location.X, location.Y);
        }

        public void getPosition(out Point location, out Size size) {
            size = _form.DesktopBounds.Size;
            location = _form.DesktopBounds.Location;
        }


		public void StackedPlot(double[] x, double[] y, double[,] zz, bool isLogPlot, bool removeMean, bool isNormalized) {

			if (_z1 == null) {
				// Trying to access ZedGraphControl that does not exist;
				//		probably window was closed by user.
				InitGraphObjects();
			}

			_plotType = PlotType.StackedLine;

			double[] z = new double[x.Length];
			int numCurves = y.Length;
			int npts = x.Length;

			double yMax, yMin;
			double deltaY;
			yMin = y[0];
			if (numCurves > 1) {
				yMax = y[numCurves - 1] + (y[numCurves - 1] - y[numCurves - 2]);
				deltaY = (yMax - yMin) / numCurves;
			}
			else {
				deltaY = 1.0;
				yMax = yMin + deltaY;
			}
			double scale = deltaY;
			double yStart = yMin;
			if (removeMean) {
				scale = deltaY / 2.0;
				yStart = yMin - scale;
			}

			double ZMax = double.NegativeInfinity;
			if (!isNormalized) {
				for (int iplot = 0; iplot < numCurves; iplot++) {
					for (int ipt = 0; ipt < npts; ipt++) {
						if (isLogPlot) {
							double logZ = Math.Log10(zz[ipt, iplot]);
							if (logZ > ZMax) {
								ZMax = logZ;
							}
						}
						else {
							if (zz[ipt, iplot] > ZMax) {
								ZMax = zz[ipt, iplot];
							}
						}
					}
				}
			}
			for (int iplot = 0; iplot < numCurves; iplot++) {
				z = new double[x.Length];
				for (int ipt = 0; ipt < npts; ipt++) {
					if (isLogPlot) {
						z[ipt] = Math.Log10(zz[ipt, iplot]);
					}
					else {
						z[ipt] = zz[ipt, iplot];
					}
				}
				if (removeMean) {
					double sum = 0.0;
					for (int ipt = 0; ipt < npts; ipt++) {
						sum += z[ipt];
					}
					double mean = sum / npts;
					for (int ipt = 0; ipt < npts; ipt++) {
						z[ipt] = z[ipt] - mean;
					}

				}
				double zMax = double.NegativeInfinity;
				double zMin = double.PositiveInfinity;
				for (int ipt = 0; ipt < npts; ipt++) {
					double zabs = Math.Abs(z[ipt]);
					if (z[ipt] > zMax) {
						zMax = z[ipt];
					}
					if (z[ipt] < zMin) {
						zMin = z[ipt];
					}
				}
				//zMax = Math.Abs(zMax);
				double y0;
				if (numCurves > 1) {
					double maxZ;
					zMax = Math.Max(Math.Abs(zMin), Math.Abs(zMax));
					if (isNormalized) {
						maxZ = zMax;  // max value of this curve
					}
					else {
						maxZ = ZMax;  // max value of all curves
					}
					y0 = yMin + iplot * deltaY;
					for (int ipt = 0; ipt < npts; ipt++) {
						z[ipt] = y0 + (z[ipt] / maxZ) * scale;
					}
				}
				else {
					yStart = zMin;
					yMax = zMax;
					// if only one curve, use actual z values on y scale
				}
				LineItem curve = _z1.GraphPane.AddCurve("", x, z, Color.Black);
				curve.Symbol.Type = SymbolType.None;
			}

			_z1.GraphPane.XAxis.Scale.Min = x[0];
			_z1.GraphPane.XAxis.Scale.Max = x[x.Length - 1];
			_z1.GraphPane.YAxis.Scale.Min = yStart;
			_z1.GraphPane.YAxis.Scale.Max = yMax;

			_z1.GraphPane.XAxis.MajorGrid.IsZeroLine = true;
			_z1.GraphPane.XAxis.MajorTic.IsOpposite = true;
			_z1.GraphPane.XAxis.MinorTic.IsOpposite = true;
			_z1.GraphPane.YAxis.MajorTic.IsOpposite = true;
			_z1.GraphPane.YAxis.MinorTic.IsOpposite = true;

			/*
			if (_addX2Axis) {
				addX2Axis();
			}
			*/
		}

		public void StackedPlot(double[] x, double[] y, double[,] zz, bool isLogPlot, bool removeMean ) {

			StackedPlot(x, y, zz, isLogPlot, removeMean, true);

		}

		public void ColorSurfacePlot(double[] x, double[] y, double[,] z, ColorScale colorScale, bool isClipped) {
			_plotType = PlotType.FilledBox;
			_needsLegend = true;
			_colorScale = colorScale;
			_isClipped = isClipped;
			ZedGraph.SurfaceColorPlot surf = DrawColorSurfacePlot(_z1.GraphPane, x, y, z, colorScale, isClipped);
			if (colorScale.UserSetDataMinMax == false) {
				double min, max;
				surf.FindDataMinMax(out min, out max);
				colorScale.MinValue = min;
				colorScale.MaxValue = max;
			}
			DrawLegend(colorScale);
		}

		public ZedGraph.SurfaceColorPlot DrawColorSurfacePlot(GraphPane pane, double[] x, double[] y, double[,] z, ColorScale colorScale, bool isClipped) {
			pane.Chart.Fill = new Fill(colorScale.BackgroundColor);
			// prototype for new method call
			ZedGraph.SurfaceColorPlot surfacePlotDac;
			surfacePlotDac = new ZedGraph.SurfaceColorPlot(x, y, z, colorScale);
			//_z1.GraphPane.CurveList.Add(surfacePlotDac);
			pane.CurveList.Add(surfacePlotDac);
			return surfacePlotDac;
		}

		public void ContourSurfacePlot(double[] x, double[] y, double[,] z, ColorScale colorScale,  bool isFilled) {
			_plotType = PlotType.Contour;
			_needsLegend = true;
			_colorScale = colorScale;
			_isFilled = isFilled;
			ZedGraph.SurfaceContourPlot plt = DrawContourSurfacePlot(_z1.GraphPane, x, y, z, colorScale, isFilled);
			if (colorScale.UserSetDataMinMax == false) {
				double min, max;
				plt.FindDataMinMax(out min, out max);
				colorScale.MinValue = min;
				colorScale.MaxValue = max;
			}
			DrawLegend(colorScale);
		}

		public ZedGraph.SurfaceContourPlot DrawContourSurfacePlot(GraphPane pane, double[] x, double[] y, double[,] z, ColorScale colorScale, bool isFilled) {
			pane.Chart.Fill = new Fill(colorScale.BackgroundColor);
			// prototype for new method call
			ZedGraph.SurfaceContourPlot contourPlotDac;
			contourPlotDac = new ZedGraph.SurfaceContourPlot(x, y, z, colorScale, isFilled);
			//_z1.GraphPane.CurveList.Add(surfacePlotDac);
			pane.CurveList.Add(contourPlotDac);
			return contourPlotDac;
		}

		/// <summary>
		/// Plots z values on an x-y grid by drawing a color-filled box
		///		centered on the x-y point
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <param name="colorScale"></param>
        public void ColorBoxPlot(double[] x, double[] y, double[,] z, ColorScale colorScale, bool isClipped) {

			throw new NotImplementedException("ColorBoxPlot method in QuickPlotZ no longer implemented.");

			if (_z1 == null) {
				// Trying to access ZedGraphControl that does not exist;
				//		probably window was closed by user.
				InitGraphObjects();
			}

			_plotType = PlotType.FilledBox;
			_needsLegend = true;
			_x = x;
			_y = y;
			_z = z;
			_isClipped = isClipped;
			if ((_colorScale == null) || (GetHashCode() != _prevHash)) {
				// if colorscale not set, save external colorScale as initial value;
				// If user modified colorscale, do not alter that.
				_colorScale = colorScale;
				_isClipped = isClipped;
			}
			_prevHash = GetHashCode();

			DrawFilledBoxGraph(_z1.GraphPane, _x, _y, _z, _colorScale, _isClipped);

			/*
			if (_addX2Axis) {
				addX2Axis();
			}
			*/
		}

		private static void DrawFilledBoxGraph(GraphPane pane, double[] x, double[] y, double[,] z, ColorScale colorScale, bool isClipped) {
			throw new NotImplementedException("DrawFilledBoxGraph method in QuickPlotZ no longer implemented.");
			int numXPoints = x.Length;
			int numYPoints = y.Length;
			if ((numXPoints < 2) || (numYPoints < 2)) {
				return;
			}
			double avgDeltaX = (float)(x[numXPoints - 1] - x[0]) / (numXPoints - 1);
			double avgDeltaY = (float)(y[numYPoints - 1] - y[0]) / (numYPoints - 1);
			double deltaX, deltaY;
			pane.GraphObjList.Clear();

			if (!colorScale.UserSetDataMinMax) {
				double maxValue = double.MinValue;
				for (int yi = 0; yi < numYPoints; yi++) {
					for (int xi = 0; xi < numXPoints; xi++) {
						if (!Double.IsNaN(z[xi,yi])) {
							if (z[xi, yi] > maxValue) {
								maxValue = z[xi, yi];
							}
						}
					}
				}
				colorScale.MaxValue = Math.Ceiling(maxValue);
			}

			int count = 0;
			for (int yi = 0; yi < numYPoints; yi++) {
				if (yi == 0) {
					deltaY = Math.Abs(y[1] - y[0]);
				}
				else {
					deltaY = Math.Abs(y[yi] - y[yi - 1]);
				}
				if (deltaY > 2.0f * avgDeltaY) {
					deltaY = avgDeltaY;
				}
				for (int xi = 0; xi < numXPoints; xi++) {
					count++;
					if ((count % 100000) == 0) {
						Console.Beep(440, 15);
					}

					// If data value is "NoData" then
					//	do not draw a box, just use background color
					if (double.IsNaN(z[xi,yi])) {
						continue;
					}

					if (xi == 0) {
						deltaX = Math.Abs(x[1] - x[0]);
					}
					else if (xi == (numXPoints - 1)) {
						deltaX = Math.Abs(x[numXPoints-1] - x[numXPoints-2]);
					}
					else {
						deltaX = Math.Abs(x[xi+1] - x[xi - 1])/2.0;
					}
					if (deltaX > 2.0f * avgDeltaX) {
						deltaX = avgDeltaX;
					}
					Color boxColor = colorScale.GetColor(z[xi, yi]);

					if (boxColor == colorScale.BackgroundColor) {
						// if box is same color as background, skip drawing
						continue;
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
					ZedGraph.BoxObj box1 = new ZedGraph.BoxObj(x[xi] - halfWidth, y[yi] + halfHeight,
						1.05 * deltaX, 1.05 * deltaY, Color.Empty, boxColor);
					/*   old form:
					ZedGraph.BoxItem box1 = new ZedGraph.BoxObj(new RectangleF((float)x[xi] - (float)deltaX / 2.0f, (float)y[yi] + (float)deltaY / 2.0f, deltaX, deltaY),
												Color.Empty, boxColor);
					*/
					if (isClipped) {
						box1.IsClippedToChartRect = true;  // retired property $Revision:1.0$
					}
					//box1.Border = boxBorder;
					box1.ZOrder = ZedGraph.ZOrder.E_BehindCurves;
					//box1.Fill = new ZedGraph.Fill(colorScale.GetColor(z[xi, yi]));
					//box1.Fill = new ZedGraph.Fill(Color.Pink);
					// location of box seems to be for the upper left corner, regardless of the Align setting
					//box1.Location = new ZedGraph.Location((float)x[xi]-deltaX/2.0f, (float)y[yi]+deltaY/2.0f, deltaX, deltaY, ZedGraph.CoordType.AxisXYScale,
					//                                        ZedGraph.AlignH.Center, ZedGraph.AlignV.Center);
					pane.GraphObjList.Add(box1);
				}
			}

			pane.XAxis.Scale.Min = x[0] - (x[1] - x[0]) / 2.0;
			pane.XAxis.Scale.Max = x[x.Length - 1] + (x[x.Length - 1] - x[x.Length - 2]) / 2.0;
			pane.YAxis.Scale.Min = y[0] - (y[1] - y[0]) / 2.0;
			pane.YAxis.Scale.Max = y[y.Length - 1] + (y[y.Length - 1] - y[y.Length - 2]) / 2.0;
			//Axis.Default.MaxGrace = 0.05;

			return;
		}

		public void ColorContourPlot(double[] x, double[] y, double[,] z,
									ColorScale colorScale, /*double minContour, double stepContour,*/ bool isFilled) {

			throw new NotImplementedException("ColorContourPlot method in QuickPlotZ no longer implemented.");

			if (_z1 == null) {
				// Trying to access ZedGraphControl that does not exist;
				//		probably window was closed by user.
				InitGraphObjects();
			}

			//double[] c = new double[4];
			//PointF[] vertices = new PointF[5];
			_needsLegend = true;
			_x = x;
			_y = y;
			_z = z;
			//_minContour = minContour;
			//_stepContour = stepContour;
			_isFilled = isFilled;
			_plotType = PlotType.Contour;
			if ((_colorScale == null) || (GetHashCode() != _prevHash)) {
				// if colorscale not set, save external colorScale as initial value;
				// If user modified colorscale, do not alter that.
				_colorScale = colorScale;
			}
			_prevHash = GetHashCode();


			DrawContourGraph(_z1.GraphPane, _x, _y, _z, _colorScale, _isFilled);
			/*
			if (_addX2Axis) {
				addX2Axis();
			}
			*/
		
		}

		private void DrawContourGraph(GraphPane pane, double[] x, double[] y, double[,] z, ColorScale colorScale, bool isFilled) {

			throw new NotImplementedException("DrawContourGraph method in QuickPlotZ no longer implemented.");

			// find maximum contour
			double stepContour = colorScale.ContourStep;
			double minContour = colorScale.MinValue;
			int contourCount = 1;
			double maxContour = minContour;
			double maxZ = double.NegativeInfinity;
			for (int iy = 0; iy < y.Length; iy++) {
				for (int ix = 0; ix < x.Length; ix++) {
					if (z[ix, iy] > maxZ) {
						maxZ = z[ix, iy];
					}
				}
			}
			if (stepContour <= 0.0) {
				int numColors = colorScale.ColorSchemeDefinition.ColorValue.Count;
				if (colorScale._extendLower) {
					numColors -= 1;
				}
				//stepContour = (maxZ - minContour) / numColors;
				stepContour = (colorScale.MaxValue - minContour) / numColors;
				colorScale.ContourStep = stepContour;
			}
			while (maxZ > maxContour) {
				maxContour += stepContour;
				contourCount++;
			}

			// make colorScale match the contours
			pane.GraphObjList.Clear();
			if (colorScale.ContourStep == 0.0) {
				colorScale.ContourStep = 1.0;
			}
			if (!colorScale.UserSetDataMinMax) {
				colorScale.MaxValue = maxContour;
				/*
				if (colorScale.FixContourColors) {
					if (colorScale.ExtendLower) {
						// first color is below first contour, so there is one less color in the range
						colorScale.MaxValue = minContour + (colorScale.ColorSchemeDefinition.ColorValue.Count - 1) * stepContour;
					}
					else {
						colorScale.MaxValue = minContour + colorScale.ColorSchemeDefinition.ColorValue.Count * stepContour;
					}
				}
				*/
			}

			// examine color scale
			// number of contour colors needed to cover entire range (round up)
			int numContours = (int)Math.Ceiling((_colorScale.MaxValue - _colorScale.MinValue) / _colorScale.ContourStep);
			if (colorScale._extendLower) {
				numContours -= 1;
			}
			if (numContours > _colorScale.ColorSchemeDefinition.ColorValue.Count) {
				// need more colors than available,
				// must either repeat colors or interpolate colors
				if (!_colorScale.BlendColors && !_colorScale.UseEachContourColorStep) {
					// neither option was set, so force one 
					_colorScale.BlendColors = true;
				}

			}

			ContourBox contourBox = new ContourBox(pane, x, y, z, colorScale, _z1, isFilled);

			//List<PointF[]> _polygons;


			for (int iy = 0; iy < y.Length - 1; iy++) {
				for (int ix = 0; ix < x.Length - 1; ix++) {

					bool done = false;
					int contourCounter = contourCount;
					double contour = maxContour;
					//int nContours = 0;		// number of contours drawn = 0 
					//int nPasses = 0;		// number of passes thru contour loop 
					double deltaX = (float)Math.Abs(x[ix] - x[ix + 1]);
					double deltaY = (float)Math.Abs(y[iy] - y[iy + 1]);

					//_polygons.Clear();

					// start drawing highest contour polygons first,
					//	because Zedgraph graphobjlist draws the last items first.
					//	First items in list will appear on top of following items.
					while (!done) {

						done = contourBox.DrawContour(ix, iy, contour, contourCounter);

						contour -= stepContour;    /* set next contour level */
						contourCounter--;

					}   // end while !done

					//if (iy >0)
					//goto xyz;

				}   // end for xi loop
			}   // end for yi loop

			//xyz:
			pane.XAxis.Scale.Min = x[0];
			pane.XAxis.Scale.Max = x[x.Length - 1];
			pane.YAxis.Scale.Min = y[0];
			pane.YAxis.Scale.Max = y[y.Length - 1];
			pane.XAxis.MajorGrid.IsZeroLine = true;
			pane.XAxis.MajorTic.IsOpposite = true;
			pane.XAxis.MinorTic.IsOpposite = true;
			//pane.YAxis.MajorTic.IsOpposite = true;
			//pane.YAxis.MinorTic.IsOpposite = true;

		}  // end ColorContourPlot()


/*
		private double xintrp(double a, double b) {
			return 0.1;
			//((fxinc)*(a)/((a)-(b)))+xx0;
		}
*/

		/// <summary>
		/// 
		/// </summary>
		private class ContourBox {

			//public int XIndex, YIndex;
			//public double Contour;

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

			public ContourBox(ZedGraph.GraphPane pane, double[] x, double[] y, double[,] z, ColorScale colorScale,
								ZedGraph.ZedGraphControl z1, bool isFilled) {

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
				a.CopyTo(array,0);
				a[0] = 99.0;
				double g = array[0];

				_contourLocation = new Location();
				_contourLocation.CoordinateFrame = CoordType.AxisXYScale;
			}

			public bool DrawContour(int XIndex, int YIndex, double Contour, int contourNumber) {

				X0 = X[XIndex];
				X1 = X[XIndex + 1];
				Y0 = Y[YIndex];
				Y1 = Y[YIndex + 1];
				Z0 = Z[XIndex, YIndex];
				Z1 = Z[XIndex, YIndex+1];
				Z2 = Z[XIndex + 1, YIndex+1];
				Z3 = Z[XIndex + 1, YIndex];

				bool done = false;
				bool skip = false;

				// If any of the 4 Z's are NaN (no data) then
				//	fill entire box with NoData color
				//	which is the background color of the graph pane.
				if (double.IsNaN(Z0) || double.IsNaN(Z1) || double.IsNaN(Z2) || double.IsNaN(Z3)) {
					done = true;
					return done;
				}


				//PointF[] vertices = new PointF[5];

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
				}

				switch (cflag) {
					case 0xf:    // contour above all pts, skip to next contour 
						skip = true;
						break;

					case 0x1:    // lo,lo,lo,hi 
					case 0xe:    // hi,hi,hi,lo 
						case6(Contour, contourNumber);
						break;

					case 0x2:    // lo,lo,hi,lo 
					case 0xd:    // hi,hi,lo,hi 
						case4(Contour, contourNumber);
						break;

					case 0x3:    // lo,lo,hi,hi 
					case 0xc:    // hi,hi,lo,lo 
						case3(Contour, contourNumber);
						break;

					case 0x4:    // lo,hi,lo,lo 
					case 0xb:    // hi,lo,hi,hi 
						case2(Contour, contourNumber);
						break;

					case 0x5:    // lo,hi,lo,hi 
					case 0xa:    // hi,lo,hi,lo 
						casex(Contour, contourNumber);
						break;

					case 0x6:    // lo,hi,hi,lo 
					case 0x9:    // hi,lo,lo,hi 
						case5(Contour, contourNumber);
						break;

					case 0x7:    // lo,hi,hi,hi 
					case 0x8:    // hi,lo,lo,lo 
						case1(Contour, contourNumber);
						break;

					case 0x0:	// contour below all pts
						done = true;
						case0(Contour, contourNumber);
						break;
				}    // end of switch 

				if (!skip) {
					//_count++;
					//DrawPolygon(Contour, contourNumber);
				}

				//return done;
				return done;

			}

			private void DrawPolygon(double Contour, int contourNumber) {
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
					int percentColor = (int)(100.0 * (contourNumber - 1) / (totalContours-1) + 0.5);
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

				//  for testing
				//polyColor = polyColor2;

				{
					if (_isFilled) {
						int cc = contourNumber;
						_contourPolygon = new PointD[_polygon.Length];
						_polygon.CopyTo(_contourPolygon, 0);
						//ZedGraph.PolyObj poly = new ZedGraph.PolyObj(_contourPolygon, Color.Empty, polyColor);
						ZedGraph.PolyObj poly = new ZedGraph.PolyObj(_contourPolygon, Color.Empty, polyColor);
						poly.IsClippedToChartRect = true;
						//box1.Border = boxBorder;
						poly.ZOrder = ZedGraph.ZOrder.E_BehindCurves;
						_pane.GraphObjList.Add(poly);

					}
					else {
						if (_contourLocation.Width != 0) {
							ArrowObj contourLine = new ArrowObj();
							contourLine.Location = (Location)_contourLocation.Clone();
							contourLine.IsArrowHead = false;
							//contourLine.Style = System.Drawing.Drawing2D.DashStyle.Solid;  // removed $Revision:1.0$
							//contourLine.Color = polyColor;  // removed $Revision:1.0$
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

	}
}
