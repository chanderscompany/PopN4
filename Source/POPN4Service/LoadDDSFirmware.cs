using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;

namespace POPN4Service {
    static class LoadDDSFirmware {

        static string _output, _error;
        static string _exePath;

        static public void Run() {

            try {
                string appFolder = Application.StartupPath;
                _exePath = Path.Combine(appFolder, "fx2loader.exe");
                ProcessStartInfo psi = new ProcessStartInfo(_exePath, "-v 0456:EE06 AD9959_FW.hex");
                psi.WorkingDirectory = appFolder;

                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                psi.UseShellExecute = false;
                System.Diagnostics.Process fx2loader;
                fx2loader = System.Diagnostics.Process.Start(psi);
                System.IO.StreamReader myError = fx2loader.StandardError;
                System.IO.StreamReader myOutput = fx2loader.StandardOutput;
                fx2loader.WaitForExit(5000);
                if (fx2loader.HasExited) {
                    _error = myError.ReadToEnd();
                    _output = myOutput.ReadToEnd();
                    //Console.WriteLine(output);
                    //Console.WriteLine(error);
                }
            }
            catch (Exception ee) {
                _error = ee.Message;
                _output = "---***--- Exception: ";
            }
        }

        static public string GetResults() {
            //string path = _exePath + "\n";
            string results = "fx2loader: " + _output + " \n" + _error;
            return (results);
        }
   }
}
