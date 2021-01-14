using System;
using System.Collections.Generic;
using System.Linq;

namespace DACarter.Utilities.Maths {

    class Ceil: FunctionBase {

        public Ceil(string value)
            : base(value) {
        }

        public override double Calculate(params double[] args) {
            return Math.Ceiling(args[0]);
        }

    }
}
