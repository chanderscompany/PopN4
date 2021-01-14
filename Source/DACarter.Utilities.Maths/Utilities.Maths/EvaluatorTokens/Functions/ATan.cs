using System;
using System.Collections.Generic;
using System.Linq;

namespace DACarter.Utilities.Maths {

	class ArcTangent : FunctionBase {

		public ArcTangent(string value)
			: base(value) {
		}

		public override double Calculate(params double[] args) {
			return Math.Atan(args[0]);
		}

	}

	class ArcTangentD : FunctionBase {

		public ArcTangentD(string value)
			: base(value) {
		}

		public override double Calculate(params double[] args) {
			return Math.Atan(args[0]) * 180.0 / Math.PI;
		}

	}
}
