using System;
using System.Collections.Generic;
using System.Linq;

namespace DACarter.Utilities.Maths {

    class E: NumberBase {

        public override double Value {
            get {
                return Math.E;
            }
        }

        public E(string value)
            :base(value){
        }

        
    }
}
