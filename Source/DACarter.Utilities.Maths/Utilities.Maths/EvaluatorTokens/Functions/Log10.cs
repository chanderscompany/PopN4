using System;
using System.Collections.Generic;
using System.Linq;

namespace DACarter.Utilities.Maths {

    class Logarithm10: FunctionBase {

        public Logarithm10(string value)
            : base(value) {
        }

        public override double Calculate(params double[] args) {
            return Math.Log10(args[0]);
        }
    }
}
