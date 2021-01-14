using System;
using System.Collections.Generic;
using System.Linq;

namespace DACarter.Utilities.Maths {

	class ArcSinus : FunctionBase {

		public ArcSinus(string value)
			: base(value) {
		}

		public override double Calculate(params double[] args) {
			return Math.Asin(args[0]);
		}

	}

	class ArcSinusD : FunctionBase {

		public ArcSinusD(string value)
			: base(value) {
		}

		public override double Calculate(params double[] args) {
			return Math.Asin(args[0]) * 180.0 / Math.PI;
		}

	}
}
