using System;
using System.ServiceProcess;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading;

namespace DACarter.ClientServer {

    /// <summary>
    /// class ServiceControllerHelper
    ///   Contains useful methods for installing, starting, stopping, etc a service.
    /// </summary>
    class ServiceControllerHelper {

        private string _serviceName;
        private ServiceController _serviceController;

        private ServiceControllerHelper() {
            // no default ctor allowed
        }

        public ServiceControllerHelper(string serviceName) {
            _serviceName = serviceName;
            _serviceController = GetServiceController();
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Starts the service.
        /// Additionally, this methods checks that correct service is installed,
        ///     and if necessary, installs service before starting.
        /// </summary>
        public void InitService() {

            bool isInstalled = true;

            //ServiceController sc = GetServiceController(_serviceName);
            Microsoft.Win32.RegistryKey rk1 = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                                                @"System\CurrentControlSet\Services\" + _serviceName);
            if (rk1 != null) {
                string executable = (string)rk1.GetValue("ImagePath");
                executable = executable.Trim('\"');
                string servicePath = Path.GetDirectoryName(executable);

                string appFolder = Path.GetDirectoryName(Application.ExecutablePath);

                if (servicePath != appFolder) {
                    // the currently running service is not from the executable in this directory
                    //  so uninstall the previous service
                    bool isUnInstalled = InstallService("-u");
                }
            }

            // make sure service is running
            //MessageBox.Show("StartService...");
            bool isRunning = StartService();
            //MessageBox.Show("return from StartService");
            if (!isRunning) {
                // could not start, try installing
                // If first install fails, try uninstalling
                // then reinstalling
                int nTries = 3;
                for (int iTry = 0; iTry < nTries; iTry++) {
                    if (iTry == 0 || iTry == 2) {

                        string text = "Installing service.";
                        //SendMessageToListLog(text);
                        isInstalled = InstallService();
                        if (isInstalled) {
                            // is installed, try to start again
                            isRunning = StartService();
                            if (!isRunning) {
                                text = "Installed OK -- Failed to start service.";
                                //SendMessageToListLog(text);
                            }
                            else {
                                // successful install
                                break;
                            }
                        }
                        else {
                            text = "Failed to install service.";
                            //SendMessageToListLog(text);
                        }
                    }  // end if iTry
                    else {
                        // iTry == 1
                        string text = "UnInstalling service...";
                        //UpdateMessageList(text);
                        //SendMessageToListLog(text);
                        bool isUnInstalled = InstallService("-u");
                        if (!isUnInstalled) {
                            text = "Uninstall Failed.";
                            //SendMessageToListLog(text);
                        }
                        else {
                            text = "Try again...";
                            //SendMessageToListLog(text);
                        }
                    }
                } // end for iTry
            }
            else {
                //SendMessageToListLog("POPN3 service is running.");
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Installs, uninstalls, stops, starts a service
        /// </summary>
        /// <param name="args">
        /// no arg = install service
        /// arg = "-u" = uninstall the service
        /// arg = "-stop" = stop the service
        /// arg = "-start" = start the service (?)
        /// </param>
        /// <returns>
        /// returns true if successful
        /// </returns>
        public bool InstallService(string args = "") {

            bool successful = false;
            string executableName = _serviceName + ".exe";
            string folder = Path.GetDirectoryName(Application.ExecutablePath);
            string servicePath = Path.Combine(folder, executableName);
            string arguments = args;
            if (!File.Exists(servicePath)) {
                // exe is not here, maybe it has a shortcut
                servicePath += ".lnk";
                executableName += ".lnk";
            }
            if (File.Exists(servicePath)) {

                using (Process proc = new Process()) {

                    proc.StartInfo.FileName = executableName;
                    proc.StartInfo.Arguments = arguments;
                    proc.StartInfo.WorkingDirectory = "";
                    proc.StartInfo.UseShellExecute = true;
                    proc.Start();

                    proc.WaitForExit();

                    //runResults.ExitCode = proc.ExitCode;

                    successful = true;
                }

            }
            else {
                //throw new ArgumentException( ("Cannot find service program file named " + executablePath));
                successful = false;
            }
            return successful;
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        /// 

        public ServiceController GetServiceController() {
            return GetServiceController(_serviceName);
        }

        public static ServiceController GetServiceController(string serviceName) {

            ServiceController sc = null;
            ServiceController[] scServices;
            scServices = ServiceController.GetServices();
            foreach (ServiceController scTemp in scServices) {

                if (scTemp.ServiceName == serviceName) {
                    sc = scTemp;
                    break;
                }
            }
            return sc;
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool StopService() {

            int count = 0;
            int timeOut = 10;

            bool isSuccessful = false;

            _serviceController.Refresh();
            if (_serviceController.Status != ServiceControllerStatus.Stopped) {
                _serviceController.Stop();
            }

            while (_serviceController.Status != ServiceControllerStatus.Stopped) {
                _serviceController.Refresh();
                count++;
                if (count >= timeOut) {
                    break;
                }
                Thread.Sleep(1000);
            }

            if (_serviceController.Status == ServiceControllerStatus.Stopped) {
                isSuccessful = true;
            }

            return isSuccessful;
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public bool StartService() {

            // check if service is stopped; if so try to start
            bool isSuccessful = false;

            int count = 0;
            int timeOut = 10;

            if (_serviceController.Status == ServiceControllerStatus.StopPending) {
                Thread.Sleep(3000);
                _serviceController.Refresh();
                if (_serviceController.Status == ServiceControllerStatus.StopPending) {
                    // only known way out of this state is murder -- kill the process:
                    using (Process proc = new Process()) {

                        proc.StartInfo.FileName = "taskkill";
                        string processName = _serviceName + ".exe";
                        proc.StartInfo.Arguments = "/IM " + _serviceName + " /F";
                        proc.StartInfo.WorkingDirectory = "";
                        proc.StartInfo.UseShellExecute = true;
                        proc.Start();

                        proc.WaitForExit();
                        _serviceController.Refresh();
                    }
                }

            }

            if (_serviceController.Status == ServiceControllerStatus.Stopped) {
                //SendMessageToListLog("Trying to start service...");
                try {
                    //TextFile.WriteLineToFile("DebugStatus2.txt", "-------------- starting service... " + DateTime.Now.ToString(), false);
                    _serviceController.Start();
                    //sc.WaitForStatus(ServiceControllerStatus.Running);
                }
                catch (Exception e) {
                    //MessageBoxEx.Show("Start service exception = " + e.Message,4000);
                    if (e.InnerException != null) {
                        if (e.InnerException.Message != null) {
                            //MessageBoxEx.Show("Start service inner exception = " + e.InnerException.Message, 5000);
                        }
                    }
                    return isSuccessful = false;
                }
                while (_serviceController.Status != ServiceControllerStatus.Running) {
                    Thread.Sleep(1000);
                    _serviceController.Refresh();
                    count++;
                    if (count >= timeOut) {
                        //SendMessageToListLog("Cannot start service. Status = " + sc.Status.ToString());
                        break;
                    }
                }
            }
            else {
                //SendMessageToListLog("Service already running.");
            }
            if (_serviceController.Status == ServiceControllerStatus.Running) {
                isSuccessful = true;
            }
            //SendMessageToListLog("Service status = " + sc.Status.ToString());
            return isSuccessful;
        }

        public ServiceControllerStatus ServiceStatus {
            get {return _serviceController.Status; }
        }
    }
}
