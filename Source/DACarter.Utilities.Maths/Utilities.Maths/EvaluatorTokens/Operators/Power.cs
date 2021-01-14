using System;
using System.Collections.Generic;
using System.Linq;

namespace DACarter.Utilities.Maths {

    class Power:OperatorBase {

        public override int Precedence {
            get {
                return 3;
            }
        }

        public override Associativity Associativity {
            get {
                return Associativity.Right;
            }
        }

        public Power(string value): base(value) {
        }

        public override double Calculate(double left,double right) {
            return Math.Pow(left,right);
        }
    }
}
