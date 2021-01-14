using System;
using System.Collections.Generic;
using System.Linq;

namespace DACarter.Utilities.Maths {

	class Sinus : FunctionBase {

		public Sinus(string value)
			: base(value) {
		}

		public override double Calculate(params double[] args) {
			return Math.Sin(args[0]);
		}

	}

	class SinusD : FunctionBase {

		public SinusD(string value)
			: base(value) {
		}

		public override double Calculate(params double[] args) {
			return Math.Sin(args[0] * Math.PI/180.0);
		}

	}
}
