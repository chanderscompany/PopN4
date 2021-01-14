using System;
using System.Collections.Generic;
using System.Linq;

namespace DACarter.Utilities.Maths {

    abstract class NumberBase: Token {

        public abstract double Value {
            get;
        }
    
        public NumberBase(string value)
            :base(value){
        }

    }
}
