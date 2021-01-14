using System;
using System.Collections.Generic;
using System.Linq;

namespace DACarter.Utilities.Maths {

    class Exp: FunctionBase {

        public Exp(string value)
            : base(value) {
        }

        public override double Calculate(params double[] args) {
            return Math.Exp(args[0]);
        }

    }
}
