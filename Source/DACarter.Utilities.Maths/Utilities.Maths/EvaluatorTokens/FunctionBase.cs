using System;
using System.Collections.Generic;
using System.Linq;

namespace DACarter.Utilities.Maths {

    abstract class FunctionBase: Token{

        public virtual int OperandsCount {
            get {
                return 1;
            }
        }

        public FunctionBase(string value)
            : base(value) {
        }

        public abstract double Calculate(params double[] args);

    }

}
