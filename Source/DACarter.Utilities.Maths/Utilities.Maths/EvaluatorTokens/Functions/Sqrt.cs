using System;
using System.Collections.Generic;
using System.Linq;

namespace DACarter.Utilities.Maths {

    class Sqrt: FunctionBase {

        public Sqrt(string value)
            : base(value) {
        }

        public override double Calculate(params double[] args) {
            return Math.Sqrt(args[0]);
        }

    }
}
