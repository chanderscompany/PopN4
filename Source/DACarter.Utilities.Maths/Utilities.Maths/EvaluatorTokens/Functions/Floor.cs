using System;
using System.Collections.Generic;
using System.Linq;

namespace DACarter.Utilities.Maths {

    class Floor: FunctionBase {

        public Floor(string value)
            : base(value) {
        }

        public override double Calculate(params double[] args) {
            return Math.Floor(args[0]);
        }

    }
}
