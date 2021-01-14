using System;
using System.Collections.Generic;
using System.Linq;

namespace DACarter.Utilities.Maths {

    class Pi: NumberBase {

        public override double Value {
            get {
                return Math.PI;
            }
        }

        public Pi(string value)
            :base(value){
        }

        
    }
}
