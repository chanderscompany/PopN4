// 
// DacClientServer ServiceHelper.cs
//  This file contains helper classes for creating
// custom services.
//
// To create a service called MyService:
//  Create the service class and override Start() and Stop().
//  Also create a ProjectInstaller class.
/*
    using System;
    using System.ServiceProcess;
    using System.Configuration.Install;
    using System.ComponentModel;
    using DacClientServer;
  
	[RunInstaller(true)]
	public class ProjectInstaller : Installer {

		public ProjectInstaller() {
			DacProjectInstaller dacInstaller = new DacProjectInstaller();
            dacInstaller.Install(typeof(MyService), Installers);
		}
	}

	public class TestService : DacServiceBase {
		protected override void OnStart(string[] args) {
        }
        protected override void OnStop() {
        }
    }
*/
//  In your executable:
//      NOTE: Remember to change propeties to "Run As Administrator."
/*
    using System;
    using DacClientServer;

 	static void Main(string[] args) {

		MyService.ServiceOfficialName = "NameForMyService";
		MyService.ServiceDescription = "Add description here.";
		MyService.ServiceStartType = System.ServiceProcess.ServiceStartMode.Automatic;
        MyService.AssemblyName = System.Reflection.Assembly.GetExecutingAssembly().FullName;
        ServiceHelper<MyService> helper = new ServiceHelper<MyService>(args);
        helper.Run();

        return;
    }
*/

// Some comments here added 17Feb2011 dac
// now some more.

using System;
using System.ServiceProcess;
using System.Text;
using System.Reflection;
using System.IO;
using System.Configuration.Install;
using System.ComponentModel;
using System.Windows.Forms;

using System.Runtime.InteropServices;
using System.Diagnostics;

using System.Threading;

using DACarter.Utilities;

namespace DACarter.ClientServer {

	/// <summary>
	/// Use this as the base class for your custom Service class
	/// This class inherits from System.ServiceProcess.ServiceBase
	///		and defines static properties of your service.
	///	This should be OK since you only want 1 instance of your
	///		service running with this name.
	///	Assign values to these static properties before calling
	///		ServiceHelper.
	/// </summary>
	public class DacServiceBase : ServiceBase {
		public static string ServiceOfficialName { get; set;}   // value given to Service Name
		public static string ServiceDisplayName { get; set; }   // if not set, it is same as Service Name
		public static string ServiceDescription { get; set; }
		public static ServiceStartMode ServiceStartType { get; set; }
        public static string AssemblyName { get; set; }
        public static string Version { get; set; }
	}

	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TheServiceClass">
	/// TheServiceClass is the class type of your custom service.
	/// </typeparam>
    public class ServiceHelper<TheServiceClass> where TheServiceClass : DacServiceBase, new() {

        private string[] _args;

        public string ServiceName { get; set; }
        public string ServiceDisplayName { get; set; }
        public string ServiceDescription { get; set; }
        public ServiceStartMode ServiceStartType { get; set; }
        public string AssemblyName { get; set; }

        private ServiceHelper() {
        }

        public ServiceHelper(string[] args) {
            _args = args;
            //Type classType = typeof(TheServiceClass );
            Type baseType = typeof(TheServiceClass).BaseType;
            PropertyInfo pi = baseType.GetProperty("ServiceOfficialName");
            ServiceName = (string)(pi.GetValue(null, null));
            PropertyInfo pi2 = baseType.GetProperty("AssemblyName");
            AssemblyName = (string)(pi2.GetValue(null, null));
            if (AssemblyName == null) {
                AssemblyName = "No assembly info.";
            }
        }

        public void Run() {
            // Started by user
            if (System.Environment.UserInteractive) {

                bool noService = false;
                ServiceController sc = new ServiceController(ServiceName);
                try {
                    string s = sc.Status.ToString();  // throws exception if service does not exist
                    Console.Out.WriteLine("***********");
                    Console.Out.WriteLine("Service Status = " + s);
                }
                catch (Exception e) {
                    noService = true;
                }

                // Show version information
                string assemblyName = System.Reflection.Assembly.GetExecutingAssembly().FullName;
                assemblyName = ShortAssemblyName(assemblyName);
                AssemblyName = ShortAssemblyName(AssemblyName);
                Console.WriteLine(Environment.NewLine + "ServiceHelper DLL:  " + assemblyName + ".");
                Console.WriteLine("Service assembly info:  " + AssemblyName + ".");
                Thread.Sleep(2000);

                // Parsing command line
                if (_args == null || _args.Length == 0) {
                    if (noService) {
                        Console.Out.WriteLine("***********");
                        Console.Out.WriteLine("Installing service...");
                        SelfInstaller.InstallMe();
                    }
                    //else {
                    if (sc.Status == ServiceControllerStatus.Stopped) {
                        Console.Out.WriteLine("***********");
                        Console.Out.WriteLine("Starting service...");
                        sc.Start();
                        Thread.Sleep(3000);
                    }
                    else if (sc.Status == ServiceControllerStatus.StopPending) {
                        Console.Out.WriteLine("***********");
                        Console.Out.WriteLine("Waiting for service to stop...");
                        Thread.Sleep(5000);
                        if (sc.Status == ServiceControllerStatus.StopPending) {
                            Console.Out.WriteLine("***********");
                            Console.Out.WriteLine("Service did not stop...");
                            // TODO: probably need to kill process
                        }
                        else if (sc.Status == ServiceControllerStatus.Stopped) {
                            Console.Out.WriteLine("***********");
                            Console.Out.WriteLine("Starting service...");
                            sc.Start();
                            Thread.Sleep(3000);
                        }
                        else {
                            Console.Out.WriteLine("***********");
                            Console.Out.WriteLine("Service status is " + sc.Status.ToString());
                        }
                    }
                    else {
                        Console.Out.WriteLine("***********");
                        Console.Out.WriteLine("Service is already " + sc.Status.ToString());
                    }
                    Thread.Sleep(3000);
                    return;
                }
                else if (_args != null && _args.Length >= 1) {
                    string arg0 = _args[0].ToLower();
                    char initial = arg0[0];
                    // argument minus the initial:
                    string arg = arg0.Substring(1);
                    if ((initial == '-') || (initial == '/')) {
                        if (arg[0] == 'i') {
                            // InstallHelper with /u switch set adds CRLF here, but with /i switch set it does not. So...
                            Console.WriteLine();
                            if (noService) {
                                SelfInstaller.InstallMe();
                            }
                            else {
                                if (sc.Status != ServiceControllerStatus.Stopped) {
                                    Console.Out.WriteLine("***********");
                                    Console.Out.WriteLine("Stopping service...");
                                    sc.Stop();
                                }
                                SelfInstaller.UninstallMe();
                                SelfInstaller.InstallMe();
                            }
                            Thread.Sleep(3000);
                            return;
                        }
                        else if (arg[0] == 'u') {
                            if (!noService) {
                                if (sc.Status != ServiceControllerStatus.Stopped) {
                                    sc.Stop();
                                    Console.Out.WriteLine("***********");
                                    Console.Out.WriteLine("Stopping service...");
                                }
                                SelfInstaller.UninstallMe();
                            }
                            Thread.Sleep(3000);
                            return;
                        }
                        else if (arg.StartsWith("stop")) {
                            if (sc.Status != ServiceControllerStatus.Stopped) {
                                sc.Stop();
                                Console.Out.WriteLine("***********");
                                Console.Out.WriteLine("Stopping service...");
                            }
                            Thread.Sleep(3000);
                            return;
                        }
                    }
                }

                // Show usage
                Console.WriteLine(Environment.NewLine + Environment.NewLine + "Usage: MyService.exe [/i | /u | /stop]" + Environment.NewLine + Environment.NewLine + "Where:");
                Console.WriteLine("       /i - install service;");
                Console.WriteLine("       /u - uninstall service.");
                Console.WriteLine("       /stop - stop service.");
                Console.WriteLine("       no argument - start service (install if required).");
                Thread.Sleep(5000);
            }
            else {
                // non-interactive call
                    ServiceBase.Run(new TheServiceClass());
            }

        }  // end Run()

        private static string ShortAssemblyName(string assemblyName) {
            int indx = assemblyName.IndexOf(", Culture");
            assemblyName = assemblyName.Substring(0, indx);
            return assemblyName;
        }
    }  // end class ServiceHelper

    public class DacProjectInstaller {

        private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller serviceInstaller;

        public void Install(string serviceName, string serviceDescription, InstallerCollection installers) {

            serviceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            serviceInstaller = new System.ServiceProcess.ServiceInstaller();

            this.serviceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            serviceProcessInstaller.Password = null;
            serviceProcessInstaller.Username = null;

            serviceInstaller.ServiceName = serviceName;
            serviceInstaller.Description = serviceDescription;
            serviceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;

            installers.Add(serviceProcessInstaller);
            installers.Add(serviceInstaller);
        }

        public void Install(Type ServiceClass, InstallerCollection installers) {

            serviceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            serviceInstaller = new System.ServiceProcess.ServiceInstaller();

            this.serviceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            serviceProcessInstaller.Password = null;
            serviceProcessInstaller.Username = null;

            Type baseType = ServiceClass.BaseType;
            PropertyInfo pi1 = baseType.GetProperty("ServiceOfficialName");
            string serviceName = (string)(pi1.GetValue(null, null));
            PropertyInfo pi2 = baseType.GetProperty("ServiceDescription");
            string serviceDescription = (string)(pi2.GetValue(null, null));
            PropertyInfo pi3 = baseType.GetProperty("ServiceStartType");
            ServiceStartMode serviceStartType = (ServiceStartMode)(pi3.GetValue(null, null));

            serviceInstaller.ServiceName = serviceName;
            serviceInstaller.Description = serviceDescription;
            serviceInstaller.StartType = serviceStartType;

            installers.Add(serviceProcessInstaller);
            installers.Add(serviceInstaller);

            serviceInstaller.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.ProjectInstaller_AfterInstall);
        }

        /// <summary>
        /// This method is called by the system after service installation.
        /// We use it to set the recovery options
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProjectInstaller_AfterInstall(object sender,
            System.Configuration.Install.InstallEventArgs e) {
            //Our code goes in this event because it is the only one that will do
            //a proper job of letting the user know that an error has occurred,
            //if one indeed occurs. Installation will be rolled back if an error occurs.

            int iSCManagerHandle = 0;
            int iSCManagerLockHandle = 0;
            int iServiceHandle = 0;
            bool bChangeServiceConfig = false;
            bool bChangeServiceConfig2 = false;
            modAPI.SERVICE_DESCRIPTION ServiceDescription;
            modAPI.SERVICE_FAILURE_ACTIONS ServiceFailureActions;
            modAPI.SC_ACTION[] ScActions = new modAPI.SC_ACTION[3];	//There should be one element for each
            //action. The Services snap-in shows 3 possible actions.

            bool bCloseService = false;
            bool bUnlockSCManager = false;
            bool bCloseSCManager = false;

            IntPtr iScActionsPointer = new IntPtr();

            try {
                //Obtain a handle to the Service Control Manager, with appropriate rights.
                //This handle is used to open the relevant service.
                iSCManagerHandle = modAPI.OpenSCManagerA(null, null,
                    modAPI.ServiceControlManagerType.SC_MANAGER_ALL_ACCESS);

                //Check that it's open. If not throw an exception.
                if (iSCManagerHandle < 1) {
                    throw new Exception("Unable to open the Services Manager.");
                }

                //Lock the Service Control Manager database.
                iSCManagerLockHandle = modAPI.LockServiceDatabase(iSCManagerHandle);

                //Check that it's locked. If not throw an exception.
                if (iSCManagerLockHandle < 1) {
                    throw new Exception("Unable to lock the Services Manager.");
                }

                //Obtain a handle to the relevant service, with appropriate rights.
                //This handle is sent along to change the settings. The second parameter
                //should contain the name you assign to the service.
                iServiceHandle = modAPI.OpenServiceA(iSCManagerHandle, "PopNService",
                    modAPI.ACCESS_TYPE.SERVICE_ALL_ACCESS);

                //Check that it's open. If not throw an exception.
                if (iServiceHandle < 1) {
                    throw new Exception("Unable to open the Service for modification.");
                }

                /*
                //Call ChangeServiceConfig to update the ServiceType to SERVICE_INTERACTIVE_PROCESS.
                //Very important is that you do not leave out or change the other relevant
                //ServiceType settings. The call will return False if you do.
                //Also, only services that use the LocalSystem account can be set to
                //SERVICE_INTERACTIVE_PROCESS.
                bChangeServiceConfig = modAPI.ChangeServiceConfigA(iServiceHandle,
                    modAPI.ServiceType.SERVICE_WIN32_OWN_PROCESS | modAPI.ServiceType.SERVICE_INTERACTIVE_PROCESS,
                    modAPI.SERVICE_NO_CHANGE, modAPI.SERVICE_NO_CHANGE, null, null,
                    0, null, null, null, null);

                //If the call is unsuccessful, throw an exception.
                if (bChangeServiceConfig == false) {
                    throw new Exception("Unable to change the Service settings.");
                }
                */
                /*
                //To change the description, create an instance of the SERVICE_DESCRIPTION
                //structure and set the lpDescription member to your desired description.
                ServiceDescription.lpDescription = "This is my custom description for my Windows Service Application!";

                //Call ChangeServiceConfig2 with SERVICE_CONFIG_DESCRIPTION in the second
                //parameter and the SERVICE_DESCRIPTION instance in the third parameter
                //to update the description.
                bChangeServiceConfig2 = modAPI.ChangeServiceConfig2A(iServiceHandle,
                    modAPI.InfoLevel.SERVICE_CONFIG_DESCRIPTION, ref ServiceDescription);

                //If the update of the description is unsuccessful it is up to you to
                //throw an exception or not. The fact that the description did not update
                //should not impact the functionality of your service.
                if (bChangeServiceConfig2 == false) {
                    throw new Exception("Unable to set the Service description.");
                }
                */
                /**/
                //To change the Service Failure Actions, create an instance of the
                //SERVICE_FAILURE_ACTIONS structure and set the members to your
                //desired values. See MSDN for detailed descriptions.
                ServiceFailureActions.dwResetPeriod = 600;
                ServiceFailureActions.lpRebootMsg = "Service failed to start! Rebooting...";
                ServiceFailureActions.lpCommand = "SomeCommand.exe Param1 Param2";
                ServiceFailureActions.cActions = ScActions.Length;

                //The lpsaActions member of SERVICE_FAILURE_ACTIONS is a pointer to an
                //array of SC_ACTION structures. This complicates matters a little,
                //and although it took me a week to figure it out, the solution
                //is quite simple.

                //First order of business is to populate our array of SC_ACTION structures
                //with appropriate values.
                ScActions[0].Delay = 10000;
                ScActions[0].SCActionType = modAPI.SC_ACTION_TYPE.SC_ACTION_RESTART;
                ScActions[1].Delay = 2000;
                ScActions[1].SCActionType = modAPI.SC_ACTION_TYPE.SC_ACTION_RESTART;
                //ScActions[1].Delay = 30000;
                //ScActions[1].SCActionType = modAPI.SC_ACTION_TYPE.SC_ACTION_REBOOT;
                ScActions[2].Delay = 2000;
                ScActions[2].SCActionType = modAPI.SC_ACTION_TYPE.SC_ACTION_RESTART;

                // if SC_ACTION_REBOOT is specified above,
                //  we must set proper privilege:
                EnableToken("SeShutdownPrivilege");
                // POPREV: added 3.18 to see if it helps mmf creation from service:
                //EnableToken("SeCreateGlobalPrivilege");


                //Once that's done, we need to obtain a pointer to a memory location
                //that we can assign to lpsaActions in SERVICE_FAILURE_ACTIONS.
                //We use 'Marshal.SizeOf(New modAPI.SC_ACTION) * 3' because we pass 
                //3 actions to our service. If you have less actions change the * 3 accordingly.
                iScActionsPointer = Marshal.AllocHGlobal(Marshal.SizeOf(new modAPI.SC_ACTION()) * 3);

                //Once we have obtained the pointer for the memory location we need to
                //fill the memory with our structure. We use the CopyMemory API function
                //for this. Please have a look at it's declaration in modAPI.
                modAPI.CopyMemory(iScActionsPointer, ScActions, Marshal.SizeOf(new modAPI.SC_ACTION()) * 3);

                //We set the lpsaActions member of SERVICE_FAILURE_ACTIONS to the integer
                //value of our pointer.
                ServiceFailureActions.lpsaActions = iScActionsPointer.ToInt32();

                //We call bChangeServiceConfig2 with the relevant parameters.
                bChangeServiceConfig2 = modAPI.ChangeServiceConfig2A(iServiceHandle,
                modAPI.InfoLevel.SERVICE_CONFIG_FAILURE_ACTIONS, ref ServiceFailureActions);

                //If the update of the failure actions are unsuccessful it is up to you to
                //throw an exception or not. The fact that the failure actions did not update
                //should not impact the functionality of your service.
                if (bChangeServiceConfig2 == false) {
                    MessageBoxEx.Show("Unable to set the Service Failure Actions.", 3000);
                    //throw new Exception("Unable to set the Service Failure Actions.");
                }
                /**/
            }
            catch (Exception ex) {
                //Throw the exception again so the installer can get to it
                throw new Exception(ex.Message);
            }
            finally {
                //Close the handles if they are open.
                Marshal.FreeHGlobal(iScActionsPointer);

                if (iServiceHandle > 0) {
                    bCloseService = modAPI.CloseServiceHandle(iServiceHandle);
                }

                if (iSCManagerLockHandle > 0) {
                    bUnlockSCManager = modAPI.UnlockServiceDatabase(iSCManagerLockHandle);
                }

                if (iSCManagerHandle != 0) {
                    bCloseSCManager = modAPI.CloseServiceHandle(iSCManagerHandle);
                }
            }
            //When installation is done go check out your handy work using Computer Management!
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // The following section contains code required to set the privileges so that
        //  the service is able to set its recovery action to reboot the computer.

        /// <summary>
        /// An LUID is a 64-bit value guaranteed to be unique only on the system on which it was generated. The uniqueness of a locally unique identifier (LUID) is guaranteed only until the system is restarted.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct LUID {
            /// <summary>
            /// The low order part of the 64 bit value.
            /// </summary>
            public int LowPart;
            /// <summary>
            /// The high order part of the 64 bit value.
            /// </summary>
            public int HighPart;
        }
        /// <summary>
        /// The LUID_AND_ATTRIBUTES structure represents a locally unique identifier (LUID) and its attributes.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct LUID_AND_ATTRIBUTES {
            /// <summary>
            /// Specifies an LUID value.
            /// </summary>
            public LUID pLuid;
            /// <summary>
            /// Specifies attributes of the LUID. This value contains up to 32 one-bit flags. Its meaning is dependent on the definition and use of the LUID.
            /// </summary>
            public int Attributes;
        }
        /// <summary>
        /// The TOKEN_PRIVILEGES structure contains information about a set of privileges for an access token.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct TOKEN_PRIVILEGES {
            /// <summary>
            /// Specifies the number of entries in the Privileges array.
            /// </summary>
            public int PrivilegeCount;
            /// <summary>
            /// Specifies an array of LUID_AND_ATTRIBUTES structures. Each structure contains the LUID and attributes of a privilege.
            /// </summary>
            public LUID_AND_ATTRIBUTES Privileges;
        }

        /// <summary>Required to enable or disable the privileges in an access token.</summary>
        private const int TOKEN_ADJUST_PRIVILEGES = 0x20;
        /// <summary>Required to query an access token.</summary>
        private const int TOKEN_QUERY = 0x8;
        /// <summary>The privilege is enabled.</summary>
        private const int SE_PRIVILEGE_ENABLED = 0x2;
        /// <summary>Specifies that the function should search the system message-table resource(s) for the requested message.</summary>
        private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x1000;
        /// <summary>
        /// Specifies the type of restart options that an application can use.
        /// </summary>
        public enum RestartOptions {
            /// <summary>
            /// Shuts down all processes running in the security context of the process that called the ExitWindowsEx function. Then it logs the user off.
            /// </summary>
            LogOff = 0,
            /// <summary>
            /// Shuts down the system and turns off the power. The system must support the power-off feature.
            /// </summary>
            PowerOff = 8,
            /// <summary>
            /// Shuts down the system and then restarts the system.
            /// </summary>
            Reboot = 2,
            /// <summary>
            /// Shuts down the system to a point at which it is safe to turn off the power. All file buffers have been flushed to disk, and all running processes have stopped. If the system supports the power-off feature, the power is also turned off.
            /// </summary>
            ShutDown = 1,
            /// <summary>
            /// Suspends the system.
            /// </summary>
            Suspend = -1,
            /// <summary>
            /// Hibernates the system.
            /// </summary>
            Hibernate = -2,
        }
        /// <summary>Forces processes to terminate. When this flag is set, the system does not send the WM_QUERYENDSESSION and WM_ENDSESSION messages. This can cause the applications to lose data. Therefore, you should only use this flag in an emergency.</summary>
        private const int EWX_FORCE = 4;

        /// <summary>
        /// The OpenProcessToken function opens the access token associated with a process.
        /// </summary>
        /// <param name="ProcessHandle">Handle to the process whose access token is opened.</param>
        /// <param name="DesiredAccess">Specifies an access mask that specifies the requested types of access to the access token. These requested access types are compared with the token's discretionary access-control list (DACL) to determine which accesses are granted or denied.</param>
        /// <param name="TokenHandle">Pointer to a handle identifying the newly-opened access token when the function returns.</param>
        /// <returns>If the function succeeds, the return value is nonzero.<br></br><br>If the function fails, the return value is zero. To get extended error information, call Marshal.GetLastWin32Error.</br></returns>
        [DllImport("advapi32.dll", EntryPoint = "OpenProcessToken", CharSet = CharSet.Ansi)]
        private static extern int OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, ref IntPtr TokenHandle);
        /// <summary>
        /// The AdjustTokenPrivileges function enables or disables privileges in the specified access token. Enabling or disabling privileges in an access token requires TOKEN_ADJUST_PRIVILEGES access.
        /// </summary>
        /// <param name="TokenHandle">Handle to the access token that contains the privileges to be modified. The handle must have TOKEN_ADJUST_PRIVILEGES access to the token. If the PreviousState parameter is not NULL, the handle must also have TOKEN_QUERY access.</param>
        /// <param name="DisableAllPrivileges">Specifies whether the function disables all of the token's privileges. If this value is TRUE, the function disables all privileges and ignores the NewState parameter. If it is FALSE, the function modifies privileges based on the information pointed to by the NewState parameter.</param>
        /// <param name="NewState">Pointer to a TOKEN_PRIVILEGES structure that specifies an array of privileges and their attributes. If the DisableAllPrivileges parameter is FALSE, AdjustTokenPrivileges enables or disables these privileges for the token. If you set the SE_PRIVILEGE_ENABLED attribute for a privilege, the function enables that privilege; otherwise, it disables the privilege. If DisableAllPrivileges is TRUE, the function ignores this parameter.</param>
        /// <param name="BufferLength">Specifies the size, in bytes, of the buffer pointed to by the PreviousState parameter. This parameter can be zero if the PreviousState parameter is NULL.</param>
        /// <param name="PreviousState">Pointer to a buffer that the function fills with a TOKEN_PRIVILEGES structure that contains the previous state of any privileges that the function modifies. This parameter can be NULL.</param>
        /// <param name="ReturnLength">Pointer to a variable that receives the required size, in bytes, of the buffer pointed to by the PreviousState parameter. This parameter can be NULL if PreviousState is NULL.</param>
        /// <returns>If the function succeeds, the return value is nonzero. To determine whether the function adjusted all of the specified privileges, call Marshal.GetLastWin32Error.</returns>
        [DllImport("advapi32.dll", EntryPoint = "AdjustTokenPrivileges", CharSet = CharSet.Ansi)]
        private static extern int AdjustTokenPrivileges(IntPtr TokenHandle, int DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, int BufferLength, ref TOKEN_PRIVILEGES PreviousState, ref int ReturnLength);
        /// <summary>
        /// The LookupPrivilegeValue function retrieves the locally unique identifier (LUID) used on a specified system to locally represent the specified privilege name.
        /// </summary>
        /// <param name="lpSystemName">Pointer to a null-terminated string specifying the name of the system on which the privilege name is looked up. If a null string is specified, the function attempts to find the privilege name on the local system.</param>
        /// <param name="lpName">Pointer to a null-terminated string that specifies the name of the privilege, as defined in the Winnt.h header file. For example, this parameter could specify the constant SE_SECURITY_NAME, or its corresponding string, "SeSecurityPrivilege".</param>
        /// <param name="lpLuid">Pointer to a variable that receives the locally unique identifier by which the privilege is known on the system, specified by the lpSystemName parameter.</param>
        /// <returns>If the function succeeds, the return value is nonzero.<br></br><br>If the function fails, the return value is zero. To get extended error information, call Marshal.GetLastWin32Error.</br></returns>
        [DllImport("advapi32.dll", EntryPoint = "LookupPrivilegeValueA", CharSet = CharSet.Ansi)]
        private static extern int LookupPrivilegeValue(string lpSystemName, string lpName, ref LUID lpLuid);
        /// <summary>
        /// The FormatMessage function formats a message string. The function requires a message definition as input. The message definition can come from a buffer passed into the function. It can come from a message table resource in an already-loaded module. Or the caller can ask the function to search the system's message table resource(s) for the message definition. The function finds the message definition in a message table resource based on a message identifier and a language identifier. The function copies the formatted message text to an output buffer, processing any embedded insert sequences if requested.
        /// </summary>
        /// <param name="dwFlags">Specifies aspects of the formatting process and how to interpret the lpSource parameter. The low-order byte of dwFlags specifies how the function handles line breaks in the output buffer. The low-order byte can also specify the maximum width of a formatted output line.</param>
        /// <param name="lpSource">Specifies the location of the message definition. The type of this parameter depends upon the settings in the dwFlags parameter.</param>
        /// <param name="dwMessageId">Specifies the message identifier for the requested message. This parameter is ignored if dwFlags includes FORMAT_MESSAGE_FROM_STRING.</param>
        /// <param name="dwLanguageId">Specifies the language identifier for the requested message. This parameter is ignored if dwFlags includes FORMAT_MESSAGE_FROM_STRING.</param>
        /// <param name="lpBuffer">Pointer to a buffer for the formatted (and null-terminated) message. If dwFlags includes FORMAT_MESSAGE_ALLOCATE_BUFFER, the function allocates a buffer using the LocalAlloc function, and places the pointer to the buffer at the address specified in lpBuffer.</param>
        /// <param name="nSize">If the FORMAT_MESSAGE_ALLOCATE_BUFFER flag is not set, this parameter specifies the maximum number of TCHARs that can be stored in the output buffer. If FORMAT_MESSAGE_ALLOCATE_BUFFER is set, this parameter specifies the minimum number of TCHARs to allocate for an output buffer. For ANSI text, this is the number of bytes; for Unicode text, this is the number of characters.</param>
        /// <param name="Arguments">Pointer to an array of values that are used as insert values in the formatted message. A %1 in the format string indicates the first value in the Arguments array; a %2 indicates the second argument; and so on.</param>
        /// <returns>If the function succeeds, the return value is the number of TCHARs stored in the output buffer, excluding the terminating null character.<br></br><br>If the function fails, the return value is zero. To get extended error information, call Marshal.GetLastWin32Error.</br></returns>
        [DllImport("user32.dll", EntryPoint = "FormatMessageA", CharSet = CharSet.Ansi)]
        private static extern int FormatMessage(int dwFlags, IntPtr lpSource, int dwMessageId, int dwLanguageId, StringBuilder lpBuffer, int nSize, int Arguments);
        /// <summary>
        /// The ExitWindowsEx function either logs off the current user, shuts down the system, or shuts down and restarts the system. It sends the WM_QUERYENDSESSION message to all applications to determine if they can be terminated.
        /// </summary>
        /// <param name="uFlags">Specifies the type of shutdown.</param>
        /// <param name="dwReserved">This parameter is ignored.</param>
        /// <returns>If the function succeeds, the return value is nonzero.<br></br><br>If the function fails, the return value is zero. To get extended error information, call Marshal.GetLastWin32Error.</br></returns>
        [DllImport("user32.dll", EntryPoint = "ExitWindowsEx", CharSet = CharSet.Ansi)]
        private static extern int ExitWindowsEx(int uFlags, int dwReserved);

		/// <summary>
		/// Formats an error number into an error message.
		/// </summary>
		/// <param name="number">The error number to convert.</param>
		/// <returns>A string representation of the specified error number.</returns>
		protected static string FormatError(int number ) {
			try {
				StringBuilder buffer =new StringBuilder(255);
				FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM, IntPtr.Zero, number, 0, buffer, buffer.Capacity, 0);
				return buffer.ToString();
			} 
            catch (Exception) {
				return "Unspecified error [" + number.ToString() + "]";
			}
		}
	
	    /// <summary>
	    /// The exception that is thrown when an error occures when requesting a specific privilege.
	    /// </summary>
	    public class PrivilegeException : Exception {
		    /// <summary>
		    /// Initializes a new instance of the PrivilegeException class.
		    /// </summary>
		    public PrivilegeException () : base() {}
		    /// <summary>
		    /// Initializes a new instance of the PrivilegeException class with a specified error message.
		    /// </summary>
		    /// <param name="message">The message that describes the error.</param>
		    public PrivilegeException (string message ) :base(message) {}
	    }


        /// <summary>
        /// Tries to enable the specified privilege.
        /// </summary>
        /// <param name="privilege">The privilege to enable.</param>
        /// <exception cref="PrivilegeException">There was an error while requesting a required privilege.</exception>
        /// <remarks>Thanks to Michael S. Muegel for notifying us about a bug in this code.</remarks>
        protected static void EnableToken(string privilege) {
            //if (Environment.OSVersion.Platform != PlatformID.Win32NT || !CheckEntryPoint("advapi32.dll", "AdjustTokenPrivileges"))
            //    return;
            IntPtr tokenHandle = IntPtr.Zero;
            LUID privilegeLUID = new LUID();
            TOKEN_PRIVILEGES newPrivileges = new TOKEN_PRIVILEGES();
            TOKEN_PRIVILEGES tokenPrivileges;
            if (OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref tokenHandle) == 0)
                throw new PrivilegeException(FormatError(Marshal.GetLastWin32Error()));
            if (LookupPrivilegeValue("", privilege, ref privilegeLUID) == 0)
                throw new PrivilegeException(FormatError(Marshal.GetLastWin32Error()));
            tokenPrivileges.PrivilegeCount = 1;
            tokenPrivileges.Privileges.Attributes = SE_PRIVILEGE_ENABLED;
            tokenPrivileges.Privileges.pLuid = privilegeLUID;
            int size = 4;
            if (AdjustTokenPrivileges(tokenHandle, 0, ref tokenPrivileges, 4 + (12 * tokenPrivileges.PrivilegeCount), ref newPrivileges, ref size) == 0)
                throw new PrivilegeException(FormatError(Marshal.GetLastWin32Error()));
        }

        // End of code to set privilege tokens
        // //////////////////////////////////////////////////////////////////////////////////////////////////////////
        // 

        /// <summary>
        /// Exits windows (and tries to enable any required access rights, if necesarry).
        /// We could use this method to force a reboot.
        /// </summary>
        /// <param name="how">One of the RestartOptions values that specifies how to exit windows.</param>
        /// <param name="force">True if the exit has to be forced, false otherwise.</param>
        /// <remarks>This method cannot hibernate or suspend the system.</remarks>
        /// <exception cref="PrivilegeException">There was an error while requesting a required privilege.</exception>
        protected static void ExitWindows(int how, bool force) {
            EnableToken("SeShutdownPrivilege");
            if (force)
                how = how | EWX_FORCE;
            if (ExitWindowsEx(how, 0) == 0)
                throw new PrivilegeException(FormatError(Marshal.GetLastWin32Error()));
        }

    }


    public static class SelfInstaller {

        // GetExecutingAssembly().Location is this dll 
        // Application.ExecutablePath is location of exe file.
        // InstallHelper wants _exePath to be location of ProjectInstaller class and
        //		location of service executable ( i.e. ProjectInstaller must be in
        //		the service class in executable and not in a dll.)
        //private static readonly string _exePath = Assembly.GetExecutingAssembly().Location;
        private static readonly string _exePath = Application.ExecutablePath;

        public static bool InstallMe() {
            try {
                Console.Out.WriteLine("***********");
                Console.Out.WriteLine("Installing service...");
                ManagedInstallerClass.InstallHelper(
                    new string[] { _exePath });
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                return false;
            }
            return true;
        }

        public static bool UninstallMe() {
            try {
                Console.Out.WriteLine("***********");
                Console.Out.WriteLine("Uninstalling service...");
                ManagedInstallerClass.InstallHelper(
                    new string[] { "/u", _exePath });
            }
            catch {
                return false;
            }
            return true;
        }
    }


    public class modAPI {

        [DllImport("advapi32.dll")]
        public static extern int LockServiceDatabase(int hSCManager);

        [DllImport("advapi32.dll")]
        public static extern bool UnlockServiceDatabase(int hSCManager);

        [DllImport("kernel32.dll")]
        public static extern void CopyMemory(IntPtr pDst, SC_ACTION[] pSrc, int ByteLen);

        [DllImport("advapi32.dll")]
        public static extern bool ChangeServiceConfigA(
            int hService, ServiceType dwServiceType, int dwStartType,
            int dwErrorControl, string lpBinaryPathName, string lpLoadOrderGroup,
            int lpdwTagId, string lpDependencies, string lpServiceStartName,
            string lpPassword, string lpDisplayName);

        [DllImport("advapi32.dll")]
        public static extern bool ChangeServiceConfig2A(
            int hService, InfoLevel dwInfoLevel,
            [MarshalAs(UnmanagedType.Struct)] ref SERVICE_DESCRIPTION lpInfo);

        [DllImport("advapi32.dll")]
        public static extern bool ChangeServiceConfig2A(
            int hService, InfoLevel dwInfoLevel,
            [MarshalAs(UnmanagedType.Struct)] ref SERVICE_FAILURE_ACTIONS lpInfo);

        [DllImport("advapi32.dll")]
        public static extern int OpenServiceA(
            int hSCManager, string lpServiceName, ACCESS_TYPE dwDesiredAccess);

        [DllImport("advapi32.dll")]
        public static extern int OpenSCManagerA(
            string lpMachineName, string lpDatabaseName, ServiceControlManagerType dwDesiredAccess);

        [DllImport("advapi32.dll")]
        public static extern bool CloseServiceHandle(
            int hSCObject);

        [DllImport("advapi32.dll")]
        public static extern bool QueryServiceConfigA(
            int hService, [MarshalAs(UnmanagedType.Struct)] ref QUERY_SERVICE_CONFIG lpServiceConfig, int cbBufSize,
            int pcbBytesNeeded);

        public const int STANDARD_RIGHTS_REQUIRED = 0xF0000;
        public const int GENERIC_READ = -2147483648;
        public const int ERROR_INSUFFICIENT_BUFFER = 122;
        public const int SERVICE_NO_CHANGE = -1;
        //public const int SERVICE_NO_CHANGE = 0xFFFF;

        public enum ServiceType {
            SERVICE_KERNEL_DRIVER = 0x1,
            SERVICE_FILE_SYSTEM_DRIVER = 0x2,
            SERVICE_WIN32_OWN_PROCESS = 0x10,
            SERVICE_WIN32_SHARE_PROCESS = 0x20,
            SERVICE_INTERACTIVE_PROCESS = 0x100,
            SERVICETYPE_NO_CHANGE = SERVICE_NO_CHANGE
        }

        public enum ServiceStartType : int {
            SERVICE_BOOT_START = 0x0,
            SERVICE_SYSTEM_START = 0x1,
            SERVICE_AUTO_START = 0x2,
            SERVICE_DEMAND_START = 0x3,
            SERVICE_DISABLED = 0x4,
            SERVICESTARTTYPE_NO_CHANGE = SERVICE_NO_CHANGE
        }

        public enum ServiceErrorControl : int {
            SERVICE_ERROR_IGNORE = 0x0,
            SERVICE_ERROR_NORMAL = 0x1,
            SERVICE_ERROR_SEVERE = 0x2,
            SERVICE_ERROR_CRITICAL = 0x3,
            msidbServiceInstallErrorControlVital = 0x8000,
            SERVICEERRORCONTROL_NO_CHANGE = SERVICE_NO_CHANGE
        }

        public enum ServiceStateRequest : int {
            SERVICE_ACTIVE = 0x1,
            SERVICE_INACTIVE = 0x2,
            SERVICE_STATE_ALL = (SERVICE_ACTIVE + SERVICE_INACTIVE)
        }

        public enum ServiceControlType : int {
            SERVICE_CONTROL_STOP = 0x1,
            SERVICE_CONTROL_PAUSE = 0x2,
            SERVICE_CONTROL_CONTINUE = 0x3,
            SERVICE_CONTROL_INTERROGATE = 0x4,
            SERVICE_CONTROL_SHUTDOWN = 0x5,
            SERVICE_CONTROL_PARAMCHANGE = 0x6,
            SERVICE_CONTROL_NETBINDADD = 0x7,
            SERVICE_CONTROL_NETBINDREMOVE = 0x8,
            SERVICE_CONTROL_NETBINDENABLE = 0x9,
            SERVICE_CONTROL_NETBINDDISABLE = 0xA,
            SERVICE_CONTROL_DEVICEEVENT = 0xB,
            SERVICE_CONTROL_HARDWAREPROFILECHANGE = 0xC,
            SERVICE_CONTROL_POWEREVENT = 0xD,
            SERVICE_CONTROL_SESSIONCHANGE = 0xE,
        }

        public enum ServiceState : int {
            SERVICE_STOPPED = 0x1,
            SERVICE_START_PENDING = 0x2,
            SERVICE_STOP_PENDING = 0x3,
            SERVICE_RUNNING = 0x4,
            SERVICE_CONTINUE_PENDING = 0x5,
            SERVICE_PAUSE_PENDING = 0x6,
            SERVICE_PAUSED = 0x7,
        }

        public enum ServiceControlAccepted : int {
            SERVICE_ACCEPT_STOP = 0x1,
            SERVICE_ACCEPT_PAUSE_CONTINUE = 0x2,
            SERVICE_ACCEPT_SHUTDOWN = 0x4,
            SERVICE_ACCEPT_PARAMCHANGE = 0x8,
            SERVICE_ACCEPT_NETBINDCHANGE = 0x10,
            SERVICE_ACCEPT_HARDWAREPROFILECHANGE = 0x20,
            SERVICE_ACCEPT_POWEREVENT = 0x40,
            SERVICE_ACCEPT_SESSIONCHANGE = 0x80
        }

        public enum ServiceControlManagerType : int {
            SC_MANAGER_CONNECT = 0x1,
            SC_MANAGER_CREATE_SERVICE = 0x2,
            SC_MANAGER_ENUMERATE_SERVICE = 0x4,
            SC_MANAGER_LOCK = 0x8,
            SC_MANAGER_QUERY_LOCK_STATUS = 0x10,
            SC_MANAGER_MODIFY_BOOT_CONFIG = 0x20,
            SC_MANAGER_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED + SC_MANAGER_CONNECT + SC_MANAGER_CREATE_SERVICE + SC_MANAGER_ENUMERATE_SERVICE + SC_MANAGER_LOCK + SC_MANAGER_QUERY_LOCK_STATUS + SC_MANAGER_MODIFY_BOOT_CONFIG
        }

        public enum ACCESS_TYPE : int {
            SERVICE_QUERY_CONFIG = 0x1,
            SERVICE_CHANGE_CONFIG = 0x2,
            SERVICE_QUERY_STATUS = 0x4,
            SERVICE_ENUMERATE_DEPENDENTS = 0x8,
            SERVICE_START = 0x10,
            SERVICE_STOP = 0x20,
            SERVICE_PAUSE_CONTINUE = 0x40,
            SERVICE_INTERROGATE = 0x80,
            SERVICE_USER_DEFINED_CONTROL = 0x100,
            SERVICE_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED + SERVICE_QUERY_CONFIG + SERVICE_CHANGE_CONFIG + SERVICE_QUERY_STATUS + SERVICE_ENUMERATE_DEPENDENTS + SERVICE_START + SERVICE_STOP + SERVICE_PAUSE_CONTINUE + SERVICE_INTERROGATE + SERVICE_USER_DEFINED_CONTROL
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SERVICE_STATUS {
            public int dwServiceType;
            public int dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct QUERY_SERVICE_CONFIG {
            public int dwServiceType;
            public int dwStartType;
            public int dwErrorControl;
            public string lpBinaryPathName;
            public string lpLoadOrderGroup;
            public int dwTagId;
            public string lpDependencies;
            public string lpServiceStartName;
            public string lpDisplayName;
        }

        public enum SC_ACTION_TYPE : int {
            SC_ACTION_NONE = 0,
            SC_ACTION_RESTART = 1,
            SC_ACTION_REBOOT = 2,
            SC_ACTION_RUN_COMMAND = 3,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SC_ACTION {
            public SC_ACTION_TYPE SCActionType;
            public int Delay;
        }

        public enum InfoLevel : int {
            SERVICE_CONFIG_DESCRIPTION = 1,
            SERVICE_CONFIG_FAILURE_ACTIONS = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SERVICE_DESCRIPTION {
            public string lpDescription;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SERVICE_FAILURE_ACTIONS {
            public int dwResetPeriod;
            public string lpRebootMsg;
            public string lpCommand;
            public int cActions;
            public int lpsaActions;
        }
    }


}
