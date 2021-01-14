using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace POPN4 {
    public partial class PowerMeterDisplay : Form {

        //public double PowerReading;
        //public double TempReading;
        //public double PowerOffset;
        //public double FreqMHz;
        //public string TempUnits;

        public PowerMeterDisplay() {
            InitializeComponent();
        }

        public void DisplayReadings(double power, double temp, double offset, string units, double freq) {
            labelPower.Text = power.ToString("F2") + " dBm";
            labelTemp.Text = temp.ToString("F1") + " " + units;
            labelOffset.Text = "Offset = " + offset.ToString("F2") + " dB";
            labelFreq.Text = "Freq = " + freq.ToString("F0") + " MHz";
        }
    }
}
