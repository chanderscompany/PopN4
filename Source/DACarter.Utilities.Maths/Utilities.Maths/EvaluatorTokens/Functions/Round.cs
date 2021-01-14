using System;
using System.Collections.Generic;
using System.Linq;

namespace DACarter.Utilities.Maths {

    class Round: FunctionBase {

        public Round(string value)
            : base(value) {
        }

        public override double Calculate(params double[] args) {
            return Math.Round(args[0]);
        }

    }
}
