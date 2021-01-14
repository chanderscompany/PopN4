using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DACarter.Utilities.Graphics {

	public partial class GraphForm : Form {

		private ZedGraph.ZedGraphControl z1;

		public GraphForm() {
			InitializeComponent();
			z1 = new ZedGraph.ZedGraphControl();
			this.Controls.Add(z1);
		}

		private void Form1_Load(object sender, EventArgs e) {
			ZedGraph.PointPairList list = new ZedGraph.PointPairList();
			for (int i = 0; i < 100; i++) {
				list.Add(i, i * i);
			}
			z1.GraphPane.AddCurve("Curve 1", list, Color.Red);

			ZedGraph.PointPairList list2 = new ZedGraph.PointPairList();
			for (int i = 0; i < 100; i++) {
				list2.Add(i, 100.0*i);
			}
			z1.GraphPane.AddCurve("Curve 2", list2, Color.Black);
			z1.Width = this.ClientRectangle.Width;
			z1.Height = this.ClientRectangle.Height;
			z1.AxisChange();
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
			e.Cancel = true;
			this.Hide();
		}

		private void Form1_ClientSizeChanged(object sender, EventArgs e) {
			z1.Width = this.ClientRectangle.Width;
			z1.Height = this.ClientRectangle.Height ;
			z1.AxisChange();

		}

	}

}
