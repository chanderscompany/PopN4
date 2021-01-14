using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ZedGraph;

namespace DACarter.Utilities.Graphics {

	public partial class QuickPlotForm : Form {

		private ZedGraph.ZedGraphControl _z1;
		public bool IsResizing;

		public bool FitGraphToWindow;
		public bool FixedAspectRatio;

		public ZedGraph.ZedGraphControl GraphControl {
			get { return _z1; }
			set {
				_z1 = value;
				if (FitGraphToWindow) {
					if (FixedAspectRatio) {
						double graphRatio = (double)_z1.Height / (double)_z1.Width;
						double widowRatio = (double)this.ClientRectangle.Height / (double)this.ClientRectangle.Width;
						if (graphRatio > widowRatio) {
							_z1.Height = this.ClientRectangle.Height;
							_z1.Width = (int)(_z1.Height / graphRatio);
						}
						else {
							_z1.Width = this.ClientRectangle.Width;
							_z1.Height = (int)(_z1.Width * graphRatio);
						}
					}
					else {
						_z1.Width = this.ClientRectangle.Width;
						_z1.Height = this.ClientRectangle.Height;
					}
				}
				if (this.Controls.Contains(_z1)) {
					this.Controls.Remove(_z1);
				}
				this.Controls.Add(_z1);

				/*
				if (_z1.MasterPane.PaneList.Count > 1) {

					GraphPane graphPane = _z1.MasterPane.PaneList[0];
					graphPane.PaneRect = new RectangleF(_z1.MasterPane.PaneRect.X,
														_z1.MasterPane.PaneRect.Y,
														_z1.MasterPane.PaneRect.Width - 100,
														_z1.MasterPane.PaneRect.Height);


					GraphPane legendPane = _z1.MasterPane.PaneList[1];
					legendPane.PaneRect = new RectangleF(_z1.MasterPane.PaneRect.X + _z1.MasterPane.PaneRect.Width - 100,
														_z1.MasterPane.PaneRect.Y,
														100,
														_z1.MasterPane.PaneRect.Height);
				}
				else {
					_z1.GraphPane.PaneRect = new RectangleF(_z1.MasterPane.PaneRect.X,
														_z1.MasterPane.PaneRect.Y,
														_z1.MasterPane.PaneRect.Width,
														_z1.MasterPane.PaneRect.Height);
				}
				*/
			}
		}

		public QuickPlotForm() {
			// IsResizing is set to true when we begin resize drag operation (see QuickPlotForm_ResizeBegin())..
			// If IsResizing is true, then plot is not updated (see Form1_ClientSizeChanged()).
			// When resize dragging is finished IsResizing is set to false (see QuickPlotForm_ResizeEnd()),
			//	and then we can update plot in the window.
			// Initialize IsResizing to true, so that resizing drawing will not
			//	be attempted during construction of this object -- which is what
			//	occurs if Windows Display Settings DPI has been changed -- because
			//	Form1_ClientSizeChanged() is then called from InitializeComponent().
			IsResizing = true;
			InitializeComponent();
			FitGraphToWindow = true;
			FixedAspectRatio = false;
		}

		private void Form1_Load(object sender, EventArgs e) {
            IsResizing = false;
            /*
            // get previous position settings from config file
            try {
                Size formSize = Properties.Settings.Default.ThisFormSize;
                if (!formSize.IsEmpty) {

                    this.DesktopBounds = new Rectangle(Properties.Settings.Default.ThisFormLocation, formSize);

                }
            }
            catch {
                //MessageBoxEx.ShowAsync("POPN3.exe.config file is missing.", 4000);
            }
             * */
        }



		private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
			// when canceled, form object remains and can be used for other plots
			//	then must close main program to close the plot.
			//e.Cancel = true;
			//this.Hide();
            // save window position
            /*
            try {
                FormWindowState winState = this.WindowState;
                if (winState == FormWindowState.Normal) {
                    Properties.Settings.Default.ThisFormSize = this.DesktopBounds.Size;
                    Properties.Settings.Default.ThisFormLocation = this.DesktopBounds.Location;
                }
                else {
                    // minimized of maximized
                    Properties.Settings.Default.ThisFormSize = this.RestoreBounds.Size;
                    Properties.Settings.Default.ThisFormLocation = this.RestoreBounds.Location;
                }

                Properties.Settings.Default.Save();
            }
            catch {
            }
             * */
        }

		private void Form1_ClientSizeChanged(object sender, EventArgs e) {
			if (IsResizing) {
				return;
			}
			if (FitGraphToWindow) {
				if (FixedAspectRatio) {
					double graphRatio = (double)_z1.Height / (double)_z1.Width;
					double windowRatio = (double)this.ClientRectangle.Height / (double)this.ClientRectangle.Width;
					if (graphRatio > windowRatio) {
						_z1.Height = this.ClientRectangle.Height;
						_z1.Width = (int)(_z1.Height / graphRatio);
					}
					else {
						_z1.Width = this.ClientRectangle.Width;
						_z1.Height = (int)(_z1.Width * graphRatio);
					}
				}
				else {
					_z1.Width = this.ClientRectangle.Width;
					_z1.Height = this.ClientRectangle.Height;
				}
			}
			if (_z1.MasterPane.PaneList.Count > 1) {
				GraphPane legendPane = _z1.MasterPane.PaneList[1];
				float margin = legendPane.Margin.Top;
				float ySize = legendPane.Rect.Height;
				float xSize = legendPane.Rect.Width;
				float length;
				float aspect = xSize / ySize;
				if (aspect > 1.5f) {
					length = 1.5f * ySize;
				}
				else {
					length = xSize;
				}
				float baseDim = legendPane.BaseDimension;
				// this keeps legend top and bottom at fixed pixels when resizing
				float scaleFactor = length / (baseDim * 72.0f);
				float actualPixels = ySize / 10.0f;
				legendPane.Margin.Top = actualPixels / scaleFactor;
				legendPane.Margin.Bottom = actualPixels / scaleFactor;
			}
			/*
			if (_z1.MasterPane.PaneList.Count > 1) {
				GraphPane graphPane = _z1.MasterPane.PaneList[0];
				graphPane.PaneRect = new RectangleF(_z1.MasterPane.PaneRect.X,
													_z1.MasterPane.PaneRect.Y,
													_z1.MasterPane.PaneRect.Width - 100,
													_z1.MasterPane.PaneRect.Height);


				GraphPane legendPane = _z1.MasterPane.PaneList[1];
				legendPane.PaneRect = new RectangleF(_z1.MasterPane.PaneRect.X + _z1.MasterPane.PaneRect.Width - 100,
													_z1.MasterPane.PaneRect.Y,
													100,
													_z1.MasterPane.PaneRect.Height);
			}
			else {
				//GraphPane graphPane = _z1.MasterPane.PaneList[0];
				_z1.GraphPane.PaneRect = new RectangleF(_z1.MasterPane.PaneRect.X,
													_z1.MasterPane.PaneRect.Y,
													_z1.MasterPane.PaneRect.Width,
													_z1.MasterPane.PaneRect.Height);
			}
			*/

			//_z1.AxisChange();

		}

		private void QuickPlotForm_FormClosed(object sender, FormClosedEventArgs e) {

		}

		private void QuickPlotForm_ResizeBegin(object sender, EventArgs e) {
			// make note of fact we are resizing, so ...Paint() 
			//	will not try to draw many times before resize is done.
			IsResizing = true;
		}

		private void QuickPlotForm_ResizeEnd(object sender, EventArgs e) {
			// Done resizing, now window can be repainted.
			IsResizing = false;
			Form1_ClientSizeChanged(null, null);
		}

		private void QuickPlotForm_Paint(object sender, PaintEventArgs e) {

		}

	}

}
