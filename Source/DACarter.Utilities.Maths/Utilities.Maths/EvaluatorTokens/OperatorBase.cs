using System;
using System.Collections.Generic;
using System.Linq;

namespace DACarter.Utilities.Maths {

    enum Associativity {
        Left, Right
    }

    abstract class OperatorBase: Token{

        public virtual int Precedence {
            get {
                return 1;
            }
        }

        public virtual Associativity Associativity {
            get {
                return Associativity.Left;
            }
        }

        public OperatorBase(string value)
            :base(value) {
        }

        public abstract double Calculate(double left,double right);

    }

}
