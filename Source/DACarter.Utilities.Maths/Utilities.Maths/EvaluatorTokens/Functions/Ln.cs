using System;
using System.Collections.Generic;
using System.Linq;

namespace DACarter.Utilities.Maths {

    class Logarithm: FunctionBase {

        public Logarithm(string value)
            : base(value) {
        }

        public override double Calculate(params double[] args) {
            return Math.Log(args[0]);
        }
    }
}
