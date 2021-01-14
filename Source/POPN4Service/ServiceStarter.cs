using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using DACarter.ClientServer;

namespace POPN4Service {

    class ServiceStarter {

        [STAThread]
        static void Main(string[] args) {

            if (args.Length > 0 && args[0].ToLower() == "-noservice") {
                // when running with this param from debugger, 
                //  do not start service, just call start method.
				Console.WriteLine("Starting POPN4Service as Console Mode program - not a service.");
                POPN4Service service = new POPN4Service();
				// DAC removed following because PublicOnStart is called from CreateComm called by POPNForm_Load
                // -- so basically in -noService mode, this program does nothing;
                //    POPN4Service is created by POPN4
                //service.PublicOnStart();
                System.Threading.Thread.Sleep(20000);
                // Put a breakpoint on the following line to always catch
                // your service when it has finished its work
                System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
                //System.Threading.Thread.Sleep(2000);
            }
            else {
                // install and start the service
                //
                POPN4Service.ServiceOfficialName = "POPNService";
                POPN4Service.ServiceDescription = "NOAA/ESRL/PSD2 POPN4 Main Service.";
                POPN4Service.ServiceStartType = System.ServiceProcess.ServiceStartMode.Automatic;
                POPN4Service.AssemblyName = System.Reflection.Assembly.GetExecutingAssembly().FullName;
                ServiceHelper<POPN4Service> helper = new ServiceHelper<POPN4Service>(args);
                //MessageBox.Show("ServiceHelper.Run()...");
                helper.Run();
            }

            return;
        }
    }
}
