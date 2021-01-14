using System;
using System.Collections.Generic;
using System.Linq;

namespace DACarter.Utilities.Maths {

    class ABS: FunctionBase {

        public ABS(string value)
            : base(value) {
        }

        public override double Calculate(params double[] args) {
            return Math.Abs(args[0]);
            
        }

    }
}
