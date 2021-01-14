using System;
using System.Windows.Forms;
using System.Threading;

using DACarter.Utilities;
using DACarter.PopUtilities;
using System.Diagnostics;
using System.Reflection;
using System.IO;

using POPN4Service;

namespace POPN4 {
    static class Program {

        // default log folder is previous one
        //  until new one read from parx file
        private static string _logFolder;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppDomain.CurrentDomain.UnhandledException +=
              new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            Application.ThreadException +=
              new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);

            _logFolder = PopNStateFile.GetLogFolder();
            
            //SingletonApp.Run(new POPN4MainForm(args));
            
            // Run POP4 GUI only as a singleton
            if (RunningInstance() == null) {
                Application.Run(new POPN4MainForm(args));
            }
            
            //Application.Run(new POPN4MainForm(args));
        }

        public static Process RunningInstance() {

            Process current = Process.GetCurrentProcess();

            Process[] processes = Process.GetProcessesByName(current.ProcessName);



            //Loop through the running processes in with the same name

            foreach (Process process in processes) {

                //Ignore the current process

                if (process.Id != current.Id) {

                    //Make sure that the process is running from the exe file.

                    string location = Assembly.GetExecutingAssembly().Location.Replace("/", "\\");
                    if (location == current.MainModule.FileName) {

                        //Return the other process instance.

                        Console.Beep(880, 1000);
                        string appName = Path.GetFileName(Application.ExecutablePath);
                        MessageBoxEx.Show(appName + " is already running.\nClosing this instance...", "SingletonApp", 5000);
                        return process;

                    }

                }

            }

            //No other instance was found, return null.

            return null;

        }




        /// <summary>
        /// This event is fired every time there is an unhandled exception
        /// that propagates all the way to the top of your application.
        /// There is no way to "catch" the exception at this point
        /// and let the app keep on running.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs ea) {
            try {
                Exception e = (Exception)ea.ExceptionObject;

                DacLogger.WriteEntry("Unhandled Exception in CurrentDomain_UnhandledException: " + e.Message, _logFolder);
                DacLogger.WriteEntry("------  Source:  " + e.Source, _logFolder);
                DacLogger.WriteEntry("------  TargetSite:" + e.TargetSite.Name, _logFolder);
                DacLogger.WriteEntry("------  StackTrace: " + e.StackTrace, _logFolder);

                MessageBoxEx.Show("Whoops! Please contact DAC "
                      + "with the following information:\n  (Also in Log file)\n\n"
                      + e.Message + e.StackTrace, "Fatal Error",
                      20000);
            }
            finally {
                Application.DoEvents();
                //RestartApplication();
                //Application.Exit();
            }
        }

        /// <summary>
        /// With this method hooked to the Application.ThreadException,
        /// unhandled exceptions on the main application thread
        /// will not hit the UnhandledException event on the AppDomain - 
        /// and the app will no longer terminate by default.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs ea) {
            DialogResult result = DialogResult.OK;
            try {
                Exception e = ea.Exception;

                DacLogger.WriteEntry("Unhandled Exception in Application_ThreadException: " + e.Message, _logFolder);
                DacLogger.WriteEntry("------  Source:  " + e.Source, _logFolder);
                DacLogger.WriteEntry("------  TargetSite:" + e.TargetSite.Name, _logFolder);
                DacLogger.WriteEntry("------  StackTrace: " + e.StackTrace, _logFolder);

                int delay = 10;
                result = MessageBoxEx.Show("POPN Unhandled Exception\n"
                + "Press ABORT to avoid auto restart attempt in " + delay.ToString() + " seconds...\n\n"
                + ea.Exception.Message + ea.Exception.StackTrace,
                "Application Error", MessageBoxButtons.AbortRetryIgnore,
                MessageBoxIcon.Stop, MessageBoxDefaultButton.Button2, (uint)(1000 * delay));
            }
            finally {
                Application.DoEvents();
                if (result == DialogResult.Abort) {
                    //RestartApplication();
                    Application.Exit();
                }
                else {
                    RestartApplication();
                }
            }
        }


        public static void RestartApplication() {
            DacLogger.WriteEntry("Attempting auto restart...", _logFolder);
            //PopStateFile.SetAutoStart(true);
            Application.Restart();
        }

    }


}
