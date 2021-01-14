using System;
using System.Collections.Generic;
using System.Linq;

namespace DACarter.Utilities.Maths {

	class Cosinus : FunctionBase {

		public Cosinus(string value)
			: base(value) {
		}

		public override double Calculate(params double[] args) {
			return Math.Cos(args[0]);
		}

	}

	class CosinusD : FunctionBase {

		public CosinusD(string value)
			: base(value) {
		}

		public override double Calculate(params double[] args) {
			return Math.Cos(args[0] * Math.PI/180.0);
		}

	}
}
