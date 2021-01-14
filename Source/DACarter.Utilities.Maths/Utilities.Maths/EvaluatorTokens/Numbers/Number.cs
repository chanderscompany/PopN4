using System;
using System.Collections.Generic;
using System.Linq;

namespace DACarter.Utilities.Maths {

    class Number: NumberBase {

        public override double Value {
            get {
                return double.Parse(Entry);
            }
        }

        public Number(double value)
            :this(value.ToString()) {
        }

        public Number(string value)
            :base(value){
        }

        
    }
}
