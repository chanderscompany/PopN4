//
// DacClientServer DacClientServer.cs
//
// This file contains helper classes for creating
//  WCF client-server duplex messaging .
//
// Example:
// Server called MyServer with method ServerMethod
// Client called MyClient with callback method CallbackMethod
//
// ****************************************************************
// COMMON INTERFACE
//
// First define client and server interfaces.
//  IMyServer must include Subscribe and Unsubscribe methods
//      in addition to custom methods.
//      Subscribe and Unsubscribe are already implemented in server base class.
//  ICallback methods must be "oneway", i.e. no return value or out parameters.
/*
      public interface ICallback {
          [OperationContract(IsOneWay = true)]
          void CallbackMethod(MyData data);

          [OperationContract(IsOneWay = true)]
          void AnotherCallback(MyData data);
      }

      [ServiceContract(CallbackContract = typeof(ICallback), SessionMode = SessionMode.Required)]
      public interface IMyServer  {

          [OperationContract]
          string ServerMethod(string command);
          
          [OperationContract]
          bool Subscribe();

          [OperationContract]
          bool Unsubscribe();          
      }
*/
// And define data objects that are transferred, for example
/*
      [Serializable]
      public struct MyData {
          public string Message;
          public int Count;
      }
*/
// The above assembly must be referenced by both client and server.
//
// ****************************************************************
// CLIENT
//
// In the client, implement the callback methods and create client class.
/*
    public class CallbackImpl :  ICallback {
        public void CallbackMethod(MyData data) {
          // do something
        }
    	public void AnotherCallback(MyData data) {
          // do something
    	}
    }

    public class MyClient<I,C> : DacClientBase<I,C>  where C : new() {

      private IMyServer _proxy;

      // get reference to MyServer methods in ctor
      public MyClient(TransportMethod transport) : base( transport) {
          _proxy = GetProxy() as IMyServer;
      }
      public void SomeMethod() {
          // call server method at some point:
          string returnValue = _proxy.ServerMethod(string arg);
          // ...
      }
    }
*/
// In the client executable program:
//  Note: client contains public member CallbackObject
//      that references the callback class implementation object.
/*
        static void Main(string[] args) {
            // ...
            MyClient<IMyServer, CallbackImpl> client = new MyClient<IMyServer, CallbackImpl>(TransportMethod.NamedPipes);
            client.SomeMethod();
            // ...
        }
*/
//
// ****************************************************************
// SERVER
//
// Define your server class, implement the server method,
//  and make callbacks to clients:
/*
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class MyServer<IC> : DacServerBase<IC>, IMyServer {
        // ...
        public string ServerMethod(string command) {
            // do your thing ...
            // callback:
            CallbackToClients("CallbackMethod", data);
            // ...

    }
*/
// ****************************************************************
// HOST
//
// The host for the server can be a windows or console app or a service.
//  Create DacServerHost object.
//  Server is active as long as DacServerHost object lives.
//  Must call Dispose() at the end of all paths
/*
            TransportMethod transport = TransportMethod.NamedPipes;
            DacServerHost<MyServer<ICallback>, IMyServer> dacHost = null;
            try {
                dacHost = new DacServerHost<MyServer<ICallback>, IMyServer>(transport);
            }
            catch (Exception e) {
                if (dacHost != null) {
                    dacHost.Dispose();
                }
                return;
            }
            if (dacHost.IsRunning) {
                // Do your thing...
            }
            // Before terminating:
            dacHost.Dispose();

*/

using System;
using System.ServiceModel;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;

namespace DACarter.ClientServer {

    public enum TransportMethod { 
        TCP,
        NamedPipes
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="CallbackInterface"></typeparam>
    public class DacServerBase<CallbackInterface> {

        protected readonly List<CallbackInterface> Subscribers = new List<CallbackInterface>();
        protected int ServiceID = 0;

        public DacServerBase() { 
            Random rr = new Random();
            ServiceID = rr.Next();
            //int x = 0;
        }

        //And to provide the subscribe and unsubscribe mechanisms we use the following implementation;

        public bool Subscribe() {
            int threadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
            try {
                CallbackInterface callback = OperationContext.Current.GetCallbackChannel<CallbackInterface>();
                if (!Subscribers.Contains(callback))
                    Console.WriteLine("Adding subscriber on thread " + threadID.ToString() + " server ID = " + ServiceID.ToString());
                    Subscribers.Add(callback);
					ICommunicationObject obj = (ICommunicationObject)callback;
					obj.Closed += new EventHandler(OnClientClosed);
					//obj.Closing += new EventHandler(EventService_Closing);
					Console.WriteLine("# of subscribers = " + Subscribers.Count.ToString());
                    Debug.WriteLine("# of subscribers = " + Subscribers.Count.ToString());
                    return true;
            }
            catch {
                return false;
            }
        }

		public int SubscriberCount {
			get { return Subscribers.Count; }
		}

		void OnClientClosed(object sender, EventArgs e) {
			CallbackInterface callback = (CallbackInterface)sender;
			if (Subscribers.Contains(callback)) {
				Subscribers.Remove(callback);
			}
			Console.WriteLine("Closed Client Removed!");
			Console.WriteLine("# of subscribers = " + Subscribers.Count.ToString());
		}

		public bool Unsubscribe() {
            try {
                CallbackInterface callback = OperationContext.Current.GetCallbackChannel<CallbackInterface>();
				if (Subscribers.Contains(callback)) {
					Subscribers.Remove(callback);
				}
                return true;
            }
            catch {
                return false;
            }
        }

        protected void CleanUpSubscriberList() {
            Subscribers.ForEach(RemoveMissingClients);
        }

        protected void RemoveMissingClients(CallbackInterface callback) {
            if (((ICommunicationObject)callback).State != CommunicationState.Opened) {
                Console.WriteLine("removing client #" + callback.GetHashCode().ToString());
                Subscribers.Remove(callback);
				Console.WriteLine("# of subscribers = " + Subscribers.Count.ToString());
			}
        }

        protected void CallbackToClients(string methodName, object arg) {
            object[] args = new object[1] { arg };
            CallbackToClients(methodName, args);
        }

        protected void CallbackToClients(string methodName, object[] args) {
            CleanUpSubscriberList();
            //Console.WriteLine("# of subscribers = " + Subscribers.Count.ToString());
            //object[] args = new object[1] { arg };
            MethodInfo callbackMethod = null;
            try {
                foreach (CallbackInterface callback in Subscribers) {
                    MethodInfo[] methodInfo = callback.GetType().GetMethods();
                    foreach (System.Reflection.MethodInfo info in methodInfo) {
                        if (info.Name == methodName) {
                            callbackMethod = info;
                            try {
                                callbackMethod.Invoke(callback, args);
                            }
                            catch (CommunicationObjectAbortedException e) {
                                // client no longer there
                                // CleanUpSubscriberList() should make this rare
                                throw (e);
                            }
                            catch (Exception e) {
                                throw e;
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception e) {
                string msg = e.Message;
            }
        }

        /*
        private void MakeCallback(CallbackInterface callback) {
            Console.WriteLine("sending to client " + callback.GetHashCode().ToString());
            _data.Message = "Callback from server to client #" + callback.GetHashCode().ToString() + ": I am executing command:" + _command;
            if (((ICommunicationObject)callback).State == CommunicationState.Opened) {
                Console.WriteLine("callback");
                callback.OnCallbackTriggered(_data);
            }
            else {
                //Console.WriteLine("removing client #" + _data.Count.ToString());
                // this will remove one from list so continuation of ForEach will not work.
                // That is why we added RemoveClosedClients
                // subscribers.Remove(callback);
            }
        }
         * */

    }



    /// <summary>
    /// Any program that wants to be client to server
    ///		must create object derived from class DacClientBase.
    ///	Constructor will take care of connecting to server.
    /// </summary>
    public class DacClientBase<svrInterfaceType, callBackImplClass> where callBackImplClass : new() {

        // TODO create pipe name

        private svrInterfaceType _proxy;

        public callBackImplClass CallbackObject = new callBackImplClass();

        protected svrInterfaceType GetProxy() {
            return _proxy;
        }

        public DacClientBase(TransportMethod transport) {

            EndpointAddress ep;
            System.ServiceModel.Channels.Binding binding;
            DuplexChannelFactory<svrInterfaceType> cf;

            // make a name for server by removing 'I' from interface name
            Type si = typeof(svrInterfaceType);
            string iName = si.Name;
            string svrName = iName.Substring(1);

            if (transport == TransportMethod.NamedPipes) {
                string uri = "net.pipe://" + svrName;
                ep = new EndpointAddress(uri);
                binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
                binding.ReceiveTimeout = TimeSpan.MaxValue;

                cf = new DuplexChannelFactory<svrInterfaceType>(
                            CallbackObject,
                            binding,
                            ep);
            }
            else {
                string uri = "net.tcp://localhost/" + svrName;
                ep = new EndpointAddress(uri);
                binding = new NetTcpBinding();

                cf = new DuplexChannelFactory<svrInterfaceType>(
                            CallbackObject,
                            binding,
                            ep);
            }

            
            _proxy = cf.CreateChannel();
 
            ((IContextChannel)_proxy).OperationTimeout = new TimeSpan(0, 0, 10);  // 10 sec timeout rather than 1 minute
            MethodInfo subscribeMethod = null;
            try {
                // call _proxy.Subscribe()
                MethodInfo[] methodInfo = _proxy.GetType().GetMethods();
                foreach (System.Reflection.MethodInfo info in methodInfo) {
                    if (info.Name == "Subscribe") {
                        subscribeMethod = info;
                        subscribeMethod.Invoke(_proxy, null);
                        Console.WriteLine("Subscribed to server (msg0)");
                        break;
                    }
                }
                // _proxy.Subscribe();
            }
            catch (EndpointNotFoundException e) {
                // no server listening, wait and try again
                Console.WriteLine("Host not available; Trying again (msg1)...");
                System.Threading.Thread.Sleep(1000);
                try {
                    _proxy = cf.CreateChannel();
                    subscribeMethod.Invoke(_proxy, null);
                    Console.WriteLine("Subscribed to server (msg1)");
                }
                catch {
                    throw new ApplicationException("Client/Server Connection error1");
                }
            }
            catch (TargetInvocationException ee) {
                //Console.Beep(880, 1000);
                //Console.WriteLine("Exception: " + ee.ToString());
                bool isConnected = false;
                for (int i = 0; i < 10; i++) {
                    Console.WriteLine("Problems connecting to host; Trying again (msg2)...");
                    Debug.WriteLine("Problems connecting to host; Trying again (msg2)...");
                    System.Threading.Thread.Sleep(2000);
                    try {
                        _proxy = cf.CreateChannel();
                        subscribeMethod.Invoke(_proxy, null);
                        Console.WriteLine("Subscribed to server (msg2)");
                        isConnected = true;
                        break;
                    }
                    catch {
                        //throw new ApplicationException("Client/Server Connection error2");
                    }
                }
                if (!isConnected) {
                    throw new ApplicationException("Client/Server Connection error2");
                }
            }


        }  // end DacClientBase()

    }  // end class DacClientBase


    /// <summary>
    /// Implement a host for the server by contructing a 
    ///     DacServerHost
    /// </summary>
    public class DacServerHost<serverType, interfacetype> where serverType : new() {

        // private string _uri;
        //private Type _serverType;
        private Type _interfaceType;
        private TransportMethod _transport = TransportMethod.NamedPipes;
        private static ServiceHost _host;
        //private object _serverInstance;

        public serverType ServerInstance { get; set; }

        public bool IsRunning = false;

        public DacServerHost( TransportMethod transport) {
            //_serverType = serverType;
            _interfaceType = typeof(interfacetype);
            _transport = transport;
           // _serverInstance = null;
            ServerInstance = new serverType();
            if (ServerInstance == null) {
                throw new ApplicationException("DacServerHost cannot create serverType: " + typeof(serverType).ToString());
            }
            Open();
        }

        public void Open() {

            string uri = "";
            string iName = _interfaceType.Name;
            string svrName = iName.Substring(1);
            if (_transport == TransportMethod.NamedPipes) {
                uri = "net.pipe://" + svrName;
            }
            else {
                throw new ApplicationException("only pipes supported in dachost.");
            }

            if (ServerInstance == null) {
                _host = new ServiceHost(typeof(serverType), new Uri(uri));
            }
            else {
                _host = new ServiceHost(ServerInstance, new Uri(uri));
            }
            if (_transport == TransportMethod.NamedPipes) {
                NetNamedPipeBinding binding2 = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
                binding2.ReceiveTimeout = TimeSpan.MaxValue;

                _host.AddServiceEndpoint(_interfaceType,
                                        binding2,
                                        uri);
            }
            else {
                throw new ApplicationException("only pipes supported in dachost.");
            }
            try {
                _host.Open();
            }
            catch (System.ServiceModel.AddressAlreadyInUseException e) {
                Console.WriteLine("Host is already running.");
                System.Threading.Thread.Sleep(2000);
				IsRunning = true;
				return;
            }
            IsRunning = true;
        }

        public void Dispose() {
            Console.WriteLine("Terminating host.....");
            // System.Threading.Thread.Sleep(2000);
            if ((_host != null) && (_host.State != CommunicationState.Faulted)) {
                // must not call any ServiceHost method if in Faulted state
                ((IDisposable)_host).Dispose();
            }
            Console.WriteLine("Done.");
        }
    }



}  // end of namespace
