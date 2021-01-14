using System;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Diagnostics;

using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

//using System.Runtime.Remoting.Channels;

namespace DACarter.Utilities
{
	/// <summary>
	/// 
	/// </summary>
	public class SingletonApp {
		static Mutex _Mutex;
        static string _PopProcesses; 

		public static bool Run() {
            if (IsFirstInstance()) {
                Application.ApplicationExit += new EventHandler(OnExit);
                Application.Run();
                return true;
            }
            else {
                return false;
            }
		}
		public static bool Run(ApplicationContext context) {
			if(IsFirstInstance()) {
                // comment following out so we don't delay exit
                //   call SingletonApp.ReleaseMutex externally before closing
				Application.ApplicationExit += new EventHandler(OnExit);
				Application.Run(context);
                return true;
            }
            else {
                return false;
            }
        }
		public static bool Run(Form mainForm, out string processes) {
            _PopProcesses = "";
            processes = "";
			if(IsFirstInstance()) {
				Application.ApplicationExit += new EventHandler(OnExit);
				Application.Run(mainForm);
                return true;
            }
            else {
                processes = _PopProcesses;
                return false;
            }
        }

		public static bool IsFirstInstance() {
			bool isFirstInstance;
			string safeName = Application.ProductName +  "_Singleton_Mutex";
			_Mutex = new Mutex(true, safeName, out isFirstInstance);
			
			if (!isFirstInstance) {
                bool gotIt = _Mutex.WaitOne(5000);
                if (gotIt) {
                    // not first instance, but still got ownership
                    return true;
                }
				string appName = Path.GetFileName(Application.ExecutablePath);
				Console.Beep(880, 1000);
				MessageBoxEx.Show(appName + " is already running.\nClosing this instance...", "SingletonApp", 3000);
                _PopProcesses = "";
                Process[] processes  = System.Diagnostics.Process.GetProcesses();
                int i = 0;
                foreach (Process process in processes) {
                    if (process.ProcessName.ToLower().Contains("pop")) {
                        i++;
                        _PopProcesses += "  " + i.ToString() + ") " + process.ProcessName;
                    }
                }
                MessageBoxEx.Show("Running processes:  " + _PopProcesses, "SingletonApp", 5000);
            }
			 
			return isFirstInstance ;
		}

		/*
		static bool IsFirstInstance() {
			string appName = Path.GetFileName(Application.ExecutablePath);
			try {
				m_Mutex = new Mutex(false,appName+"Mutext");
			}
			catch (Exception) {
				// mutex with same name created by another owner
				return false;
			}
			bool owned = false;
			owned = m_Mutex.WaitOne(TimeSpan.Zero,false);
			return owned ;
		}
		*/

		static void OnExit(object sender,EventArgs args) {
			try {
                _Mutex.ReleaseMutex();
                //_Mutex.ReleaseMutex();
                //_Mutex.ReleaseMutex();
            }
			catch (Exception e) {
                int x = 0;
				// if this thread does not own mutex.
				// Probably never get here, 
				// IsFirstInstance() is what throws exception sometimes.
			}
			_Mutex.Close();
		}

        public static void ReleaseMutex() {
            try {
                _Mutex.ReleaseMutex();
            }
            catch (Exception e) {
                int x = 0;
                // if this thread does not own mutex.
                // Probably never get here, 
                // IsFirstInstance() is what throws exception sometimes.
            }
        }
	}

	//==================================================================================

	// Signature of method to call when another instance is detected
	public delegate void OtherInstanceCallback(string[] args);

	public class SingletonApp2 {
		Form _form;
		string[] _args;
		OtherInstanceCallback _fn;

		public SingletonApp2(Form mainForm, string[] args, OtherInstanceCallback fn) {
			_form = mainForm;
			_args = args;
			_fn = fn;
		}

		//
		// Not sure if these to overrides of Run( ) are correct:
		//
		/*
		public static void Run() {
			if(IsFirstInstance()) {
				//Application.ApplicationExit += new EventHandler(OnExit);
				Application.Run();
			}
		}
		public static void Run(ApplicationContext context) {
			if(IsFirstInstance()) {
				//Application.ApplicationExit += new EventHandler(OnExit);
				Application.Run(context);
			}
		}
		*/

		public /*static*/ void Run(Form mainForm) {

			// Check for initial instance, registering callback to consume args from other instances
			// Main form will be activated automatically
			OtherInstanceCallback callback = new OtherInstanceCallback(_fn);
			if( InitialInstanceActivator.Activate(mainForm, callback, _args) ) return;

			Application.Run(mainForm);

			/*
			if(IsFirstInstance()) {
				//Application.ApplicationExit += new EventHandler(OnExit);
				Application.Run(mainForm);
			}
			*/
		}

		/*
		static bool IsFirstInstance() {
			bool isFirstInstance;
			string safeName = Application.ProductName + "_Singleton_Mutex";
			Mutex mutex = new Mutex(true, safeName, out isFirstInstance);
			//string appName = Path.GetFileName(Application.ExecutablePath);
			return isFirstInstance ;
		}
		*/

	}

	//==================================================================================


	// InitialInstanceActivator.cs
	// Inspired by Mike Woodring
	// Copyright (c) 2003, Chris Sells
	// Notes:
	// -Uses Application.UserAppDataPath to pick a unique string composed
	//  of the app name, the app version and the user name. This
	//  gets us a unique mutex name, channel name and port number for each
	//  user running each app of a specific version.
	// Usage:
	/*
	...
	static void Main(string[] args) {
	  // Check for initial instance, registering callback to consume args from other instances
	  // Main form will be activated automatically
	  OtherInstanceCallback callback = new OtherInstanceCallback(OnOtherInstance);
	  if( InitialInstanceActivator.Activate(mainForm, callback, args) ) return;
  
	  // Check for initial instance w/o registering a callback
	  // Main form will still be activated automatically
	  if( InitialInstanceActivator.Activate(mainForm) ) return;

	  // Check for initial instance, registering callback to consume args from other instances
	  // Main form from ApplicationContext will be activated automatically
	  OtherInstanceCallback callback = new OtherInstanceCallback(OnOtherInstance);
	  if( InitialInstanceActivator.Activate(context, callback, args) ) return;

	  TODO: Run application
	}

	// Called from other instances
	static void OnOtherInstance(string[] args) {
	  TODO: Handle args from other instance
	}
	*/


	public class InitialInstanceActivator {
		public static int Port {
			get {
				// Pick a port based on an application-specific string
				// that also falls into an acceptable range
				return Math.Abs(ChannelName.GetHashCode()/2)%(short.MaxValue - 1024) + 1024;
			}
		}

		public static string ChannelName {
			get {
				// This allows multiple users to be running the program simultaneously:
				//	(also creates new directories in ...\settings\application data )
				//return Application.UserAppDataPath.ToLower().Replace(@"\", "_");

				// Allows multiple apps to run from diff directories:
				return Application.ExecutablePath.ToLower().Replace(@"\", "_").Replace(@":", "_");

				// Allows only one of this app to be running:
				//return Application.ProductName + "_Singleton_Mutex";
			}
		}

		public static string MutexName {
			get {
				return ChannelName;
			}
		}

		public static bool Activate(Form mainForm) {
			return Activate(new ApplicationContext(mainForm), null, null);
		}

		public static bool Activate(Form mainForm, OtherInstanceCallback callback, string[] args) {
			return Activate(new ApplicationContext(mainForm), callback, args);
		}

		private static Mutex mutex;
		private static bool firstInstance = false;

		public static bool Activate(ApplicationContext context, OtherInstanceCallback callback, string[] args) {
			// Check for existing instance
			/*bool */firstInstance = false;
			/*Mutex*/ mutex = new Mutex(true, MutexName, out firstInstance);

			if( !firstInstance ) {
				// Open remoting channel exposed from initial instance
				MainFormActivator activator;
				try {
					string url = string.Format("tcp://localhost:{0}/{1}", Port, ChannelName);
					activator = (MainFormActivator)RemotingServices.Connect(typeof(MainFormActivator), url);
				}
				catch (Exception e) {
					throw( new ApplicationException("Exception opening remoting channel in Activate()", e));
				}
				// Send arguments to initial instance and exit this one
				activator.OnOtherInstance(args);
				return true;
			}

			// Expose remoting channel to accept arguments from other instances
			try {
				//return false;
				ChannelServices.RegisterChannel(new TcpChannel(Port));
			}
			catch (RemotingException e) {
				throw( new ApplicationException("Exception exposing remoting channel in Activate()", e));
			}
			RemotingServices.Marshal(new MainFormActivator(context, callback), ChannelName);
			return false;
		}

		public class MainFormActivator : MarshalByRefObject {
			public MainFormActivator(ApplicationContext context, OtherInstanceCallback callback) {
				this.context = context;
				this.callback = callback;
			}

			public override object InitializeLifetimeService() {
				// We want an infinite lifetime as far as the
				// remoting infrastructure is concerned
				// (Thanks for Mike Woodring for pointing this out)
				ILease lease = (ILease)base.InitializeLifetimeService();
				lease.InitialLeaseTime = TimeSpan.Zero;
				return(lease);
			}

			public void OnOtherInstance(string[] args) {
				// Transition to the UI thread
				if( this.context.MainForm.InvokeRequired ) {
					OtherInstanceCallback callback = new OtherInstanceCallback(OnOtherInstance);
					this.context.MainForm.Invoke(callback, new object[] { args });
					return;
				}

				// Let the UI thread know about the other instance
				if( this.callback != null ) this.callback(args);

				// Activate the main form
				context.MainForm.Activate();
			}

			ApplicationContext context;
			OtherInstanceCallback callback;
		}
	}

}
