using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace DACarter.Utilities {

    ////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// groupbox with colored border
    /// </summary>
    /// <remarks>
    /// https://social.msdn.microsoft.com/Forums/windows/en-US/cfd34dd1-b6e5-4b56-9901-0dc3d2ca5788/changing-border-color-of-groupbox
    /// This class has problems with drawing the border when the control is only partially drawn, i.e. partially hidden or off-screen.
    /// </remarks>
    public class GroupBoxWithBorder : GroupBox {

        private Color _borderColor;

        public Color BorderColor {

            get { return _borderColor; }
            set { _borderColor = value; }

        }



        public GroupBoxWithBorder() {

            _borderColor = Color.Black;

        }



        protected override void OnPaint(PaintEventArgs e) {

            base.OnPaint(e);

            Size tSize = TextRenderer.MeasureText(this.Text, this.Font);
            Rectangle borderRect = e.ClipRectangle;
            borderRect.Y += tSize.Height / 2;
            borderRect.Height -= tSize.Height / 2;
            Rectangle clientRect = this.ClientRectangle;

            ControlPaint.DrawBorder(e.Graphics, borderRect, _borderColor, ButtonBorderStyle.Solid);
            Rectangle textRect = e.ClipRectangle;
            textRect.X += 6;
            textRect.Width = tSize.Width;
            textRect.Height = tSize.Height;

            e.Graphics.FillRectangle(new SolidBrush(this.BackColor), textRect);
            e.Graphics.DrawString(this.Text, this.Font, new SolidBrush(this.ForeColor), textRect);

            base.OnPaint(e);
        }

    }
}
