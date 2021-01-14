using System;
using System.Collections.Generic;
using System.Text;

namespace DACarter.NOAA {
	class XYZArray {

		public List<double> X;
		public List<double> Y;
		public List<double> Z;
		public List<double> Z2;
		public List<DateTime> T;

		public XYZArray() {
			X = new List<double>();
			Y = new List<double>();
			Z = new List<double>();
			Z2 = new List<double>();
			T = new List<DateTime>();
		}

		/// <summary>
		/// Returns count of number of data points contained
		///   in this instance;
		/// </summary>
		public int Count {
			// Assumes each data point has either an X or a T value;
			get {
				if (T.Count == 0) {
					return X.Count;
				}
				else {
					return T.Count;
				}
			}
		}

		public void Clear() {
			X.Clear();
			Y.Clear();
			Z.Clear();
			Z2.Clear();
			T.Clear();
		}

	}
}
