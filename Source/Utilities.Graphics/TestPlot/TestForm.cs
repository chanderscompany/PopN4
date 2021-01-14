using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
//using DACarter.Utilities;
using DACarter.Utilities.Graphics;
using ZedGraph;

namespace TestPlot {
	public partial class TestForm : Form {

		private QuickPlotZ _plot;
		private int _int;
		private Color _plotBackgroundColor;

		public TestForm() {
			InitializeComponent();
			_int = 0;
		}

		private void TestForm_Load(object sender, EventArgs e) {
			Color color = Color.OrangeRed;
			int argb = color.ToArgb();
			_plot = new QuickPlotZ();
			Type ColorScheme = typeof(ColorScale.ColorScheme);
			foreach (string colorName in Enum.GetNames(ColorScheme)) {
				this.comboBoxColor.Items.Add(colorName);
			}
			comboBoxColor.SelectedIndex = 2;
			Type plotType = typeof(QuickPlotZ.PlotType);
			foreach (string plot in Enum.GetNames(plotType)) {
				this.comboBoxPlotType.Items.Add(plot);
			}
			comboBoxPlotType.SelectedIndex = 2;
			_plotBackgroundColor = Color.Black;
			labelBackground.Text = _plotBackgroundColor.Name;
		}

		private void buttonPlot_Click(object sender, EventArgs e) {

			ColorScale colorScale;
			_plot.SetTitles("Testing ZedGraph", "My X-Axis", "My Y-Axis");
			_plot.ClearCurves();

			string plotName = this.comboBoxPlotType.Text;
			string colorName = this.comboBoxColor.Text;
			

			ColorScale.ColorScheme selectedColorScheme = (ColorScale.ColorScheme)Enum.Parse(typeof(ColorScale.ColorScheme), colorName);
			QuickPlotZ.PlotType selectedPlotType = (QuickPlotZ.PlotType)Enum.Parse(typeof(QuickPlotZ.PlotType), plotName);
			bool blendColors = checkBoxBlend.Checked;

			int NPLOTS = 2;

			int NPTS;
			double[] xx;
			double[] yy;
			double[,] zz;

			NPTS = 140;
			xx = new double[NPTS];
			yy = new double[NPTS];
			zz = new double[NPTS, NPTS];
			for (int icol = 0; icol < NPTS; icol++) {
				xx[icol] = (double)icol - 20.0;
			}
			for (int irow = 0; irow < NPTS; irow++) {
				yy[irow] = 100.0 * irow;
				for (int icol = 0; icol < NPTS; icol++) {
					zz[icol, irow] = xx[icol];
				}
			}

			_plot.SetTitles("Testing ZedGraph", "My X-Axis", "My Y-Axis");
			colorScale = new ColorScale(0.0, 100.0, selectedColorScheme, blendColors);
			colorScale.ExtendLower = checkBoxExtend.Checked;
			colorScale.RepeatUpper = checkBoxRepeat.Checked;
			colorScale.BackgroundColor = _plotBackgroundColor;
			if (selectedPlotType == QuickPlotZ.PlotType.FilledBox) {
				_plot.ColorBoxPlot(xx, yy, zz, colorScale, true);
			}
			else if (selectedPlotType == QuickPlotZ.PlotType.Contour) {
				colorScale.MinValue = 20.0;
				colorScale.ContourStep = 5.0;
				_plot.ColorContourPlot(xx, yy, zz, colorScale, true);
			}

			else {
				if (_int % NPLOTS == 0) {
					ZedGraph.PointPairList list = new ZedGraph.PointPairList();
					for (int i = 0; i < 100; i++) {
						list.Add(i, i * i);
					}
					//_plot.GraphControl.GraphPane.CurveList[0];

					ZedGraph.PointPairList list2 = new ZedGraph.PointPairList();
					for (int i = 0; i < 100; i++) {
						list2.Add(i, 100.0 * i);
					}

					ZedGraph.LineItem curve1 = _plot.AddCurve("Curve 1", list, Color.Red);
					ZedGraph.LineItem curve2 = _plot.AddCurve("Curve 2." + _int, list2, Color.Black);
					curve2.Line.IsVisible = true;
					curve1.Line.Fill = new ZedGraph.Fill(Color.White, Color.FromArgb(150, 60, 190, 50), 90F);

				}
				else {

					if (_int % NPLOTS == 1) {
						NPTS = 3;
						xx = new double[NPTS];
						yy = new double[NPTS];
						zz = new double[NPTS, NPTS];
						xx[0] = 0.0;
						xx[1] = 1.0;
						xx[2] = 2.0;
						yy[0] = 100.0;
						yy[1] = 110.0;
						yy[2] = 120.0;
						/*
						zz[0, 0] = 23.0;
						zz[0, 1] = 33.0;
						zz[0, 2] = 43.0;
						zz[1, 0] = 3.0;
						zz[1, 1] = 24.0;
						zz[1, 2] = 34.0;
						zz[2, 0] = 5.0;
						zz[2, 1] = 11.0;
						zz[2, 2] = 18.0;
						*/
						zz[0, 0] = 43.0;
						zz[0, 1] = 53.0;
						zz[0, 2] = 63.0;
						zz[1, 0] = 33.0;
						zz[1, 1] = 47.0;
						zz[1, 2] = 54.0;
						zz[2, 0] = 39.0;
						zz[2, 1] = 46.0;
						zz[2, 2] = 53.0;

						//colorScale = new ColorScale(0.0, 60.0, ColorScale.ColorScheme.MatlabRainbow, false);
						colorScale = new ColorScale(-20.0, 60.0, ColorScale.ColorScheme.Rainbow2_18, false, 10.0);
						//_plot.ColorBoxPlot(xx, yy, zz, new ColorScale(0.0, (double)NPTS, ColorScale.ColorScheme.GreenWhiteMagenta, true));
						_plot.ColorContourPlot(xx, yy, zz, colorScale,  true);
					}
				}
				
			}
			_plot.setSize(0, 0, true);
			_plot.Display();
			_int++;
		}

		private void button1_Click(object sender, EventArgs e) {
			colorDialog1.Color = panel1.BackColor;
			DialogResult rr = colorDialog1.ShowDialog();
			if (rr == DialogResult.OK) {
				Color color = colorDialog1.Color;
				panel1.BackColor = color;
			}
		}

		private void button2_Click(object sender, EventArgs e) {
			colorDialog1.Color = panel2.BackColor;
			DialogResult rr = colorDialog1.ShowDialog();
			if (rr == DialogResult.OK) {
				Color color = colorDialog1.Color;
				panel2.BackColor = color;
			}
		}

		private void button3_Click(object sender, EventArgs e) {
			colorDialog1.Color = panel3.BackColor;
			DialogResult rr = colorDialog1.ShowDialog();
			if (rr == DialogResult.OK) {
				Color color = colorDialog1.Color;
				panel3.BackColor = color;
			}
		}

		private void buttonBack_Click(object sender, EventArgs e) {
			_int = _int-2;
			buttonPlot_Click(null, null);
		}

		private void buttonBackground_Click(object sender, EventArgs e) {
			if (_plot.PlotColorScale != null) {
				colorDialog1.Color = _plot.PlotColorScale.BackgroundColor;
				
			} DialogResult rr = colorDialog1.ShowDialog();
			if (rr == DialogResult.OK) {
				Color color = colorDialog1.Color;
				_plotBackgroundColor = color;
				labelBackground.Text = color.Name;
			}
		}

		private void buttonRGB_Click(object sender, EventArgs e) {

			ColorScale colorScale = _plot.PlotColorScale;
			_plot.ClearCurves();

			string colorName = this.comboBoxColor.Text;

			ColorScale.ColorScheme selectedColorScheme = (ColorScale.ColorScheme)Enum.Parse(typeof(ColorScale.ColorScheme), colorName);

			//colorScale = new ColorScale(0.0, 100.0, selectedColorScheme, false);

			int numColors = colorScale.ColorSchemeDefinition.ColorValue.Count;

			double[] x = new double[numColors];
			double[] y = new double[numColors];

			_plot.SetTitles(colorName, "Color Index", "Value");

			for (int i = 0; i < numColors; i++) {
				x[i] = (double)i;
				y[i] = colorScale.ColorSchemeDefinition.ColorValue[i].R;
			}
			ZedGraph.LineItem curveR = _plot.AddCurve("R", x, y, Color.Red);

			for (int i = 0; i < numColors; i++) {
				x[i] = (double)i;
				y[i] = colorScale.ColorSchemeDefinition.ColorValue[i].G;
			}
			ZedGraph.LineItem curveG = _plot.AddCurve("G", x, y, Color.Green);
			curveG.Symbol.Type = SymbolType.Triangle;

			for (int i = 0; i < numColors; i++) {
				x[i] = (double)i;
				y[i] = colorScale.ColorSchemeDefinition.ColorValue[i].B;
			}
			ZedGraph.LineItem curveB = _plot.AddCurve("B", x, y, Color.Blue);
			curveB.Symbol.Type = SymbolType.Star;

			for (int i = 0; i < numColors; i++) {
				x[i] = (double)i;
				y[i] = colorScale.ColorSchemeDefinition.ColorValue[i].GetHue();
			}
			ZedGraph.LineItem curveH = _plot.AddCurve("Hue", x, y, Color.Black);
			curveH.Symbol.Type = SymbolType.TriangleDown;


			_plot.setSize(0, 0, true);
			_plot.Display();
		}

		private void buttonTest_Click(object sender, EventArgs e) {

			int npts = 60;
			double[] x = new double[npts];
			double[] y = new double[npts];
			PointD[] vertices = new PointD[npts];
			for (int i = 0; i < npts; i++) {
				x[i] = new XDate(2006, 5, 9, 12, i, 0);
				y[i] = Math.Sin(i * 2.0 * Math.PI / 60.0);
				vertices[i].X = (float)x[i];
				vertices[i].Y = (float)(y[i] + 0.1);
			}
			PolyObj poly = new PolyObj(vertices);
			_plot.ClearCurves();
			LineItem curve = _plot.AddCurve("Sine Wave", x, y, Color.Blue);
			/*GraphObj polygon =*/ _plot.GraphControl.GraphPane.GraphObjList.Add(poly);
			//polygon.ZOrder = ZOrder.E_BehindCurves;
			_plot.GraphControl.GraphPane.XAxis.Type = AxisType.Date;
			_plot.setSize(0, 0, false);
			_plot.Display();
		}
	}
}
