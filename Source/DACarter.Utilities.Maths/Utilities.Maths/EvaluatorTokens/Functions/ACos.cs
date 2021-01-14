using System;
using System.Collections.Generic;
using System.Linq;

namespace DACarter.Utilities.Maths {

	class ArcCosinus : FunctionBase {

		public ArcCosinus(string value)
			: base(value) {
		}

		public override double Calculate(params double[] args) {
			return Math.Acos(args[0]);
		}

	}

	class ArcCosinusD : FunctionBase {

		public ArcCosinusD(string value)
			: base(value) {
		}

		public override double Calculate(params double[] args) {
			return Math.Acos(args[0]) * 180.0 / Math.PI;
		}

	}
}
