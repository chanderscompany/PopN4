using System;
using System.Collections.Generic;
using System.Linq;

namespace DACarter.Utilities.Maths {

	class Tangent : FunctionBase {

		public Tangent(string value)
			: base(value) {
		}

		public override double Calculate(params double[] args) {
			return Math.Tan(args[0]);
		}

	}

	class TangentD : FunctionBase {

		public TangentD(string value)
			: base(value) {
		}

		public override double Calculate(params double[] args) {
			return Math.Tan(args[0] * Math.PI/180.0);
		}

	}
}
