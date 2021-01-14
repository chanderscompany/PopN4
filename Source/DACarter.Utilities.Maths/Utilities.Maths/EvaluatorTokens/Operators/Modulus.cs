using System;
using System.Collections.Generic;
using System.Linq;

namespace DACarter.Utilities.Maths {

    class Modulus:OperatorBase {

        public override int Precedence {
            get {
                return 2;
            }
        }

        public Modulus(string value): base(value) {
        }

        public override double Calculate(double left,double right) {
            return left%right;
        }
    }
}
