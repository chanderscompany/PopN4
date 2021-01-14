using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SpinCore;


namespace TestSpinApi {
    class Program {
        static void Main(string[] args) {

            SpinCore.SpinAPI.SpinAPI _spinApi = new SpinCore.SpinAPI.SpinAPI();
            int count = _spinApi.BoardCount;
            Console.WriteLine("SpinCore boards = " + count.ToString());
            Console.WriteLine("Press a key..");
            Console.ReadKey();

        }
    }
}
