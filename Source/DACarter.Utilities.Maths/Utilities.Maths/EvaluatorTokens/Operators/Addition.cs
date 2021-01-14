using System;
using System.Collections.Generic;
using System.Linq;

namespace DACarter.Utilities.Maths {

    class Addition:OperatorBase {

        public Addition(string value):base(value){
        }

        public override double Calculate(double left,double right) {
            return left+right;
        }
    }
}
