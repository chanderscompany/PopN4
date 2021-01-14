//
//  POPCommunication namespace
//  Defines client and server classes for WCF communication
//      between POP controller program and POPN service.
//
using System;
using System.ServiceModel;
using System.Threading;
//using System.Collections.Generic;
using ipp;

using DACarter.ClientServer;
using DACarter.PopUtilities;
using System.IO;

namespace POPCommunication {

    public struct PopStatusMessage {
		// use optional and named arguments in VS2010
        public PopStatusMessage(PopStatus status = PopStatus.None,
                                int progress = -100,
                                string msg = "",
                                DateTime timeStamp = new DateTime(),
                                string cmdParFile = "",
                                string curParFile = "",
                                string excMsg = "") {
            ProgressPercent = progress;
            Status = status;
            Message = msg;
            TimeStamp = timeStamp;
            CommandParFile = cmdParFile;
            CurrentParFile = curParFile;
            ExceptionMessage = excMsg;
        }

        public PopStatusMessage(string msg,
                                PopStatus status = PopStatus.None,
                                int progress = -100,
                                DateTime timeStamp = new DateTime(),
                                string cmdParFile = "",
                                string curParFile = "",
                                string excMsg = "") {
            ProgressPercent = progress;
            Status = status;
            Message = msg;
            TimeStamp = timeStamp;
            CommandParFile = cmdParFile;
            CurrentParFile = curParFile;
            ExceptionMessage = excMsg;
        }

        /*
		public PopStatusMessage(int progress) {
			ProgressPercent = progress;
			Status = PopStatus.None;
			Message = "";
		}
        public PopStatusMessage(PopStatus status) {
            ProgressPercent = -100;
            Status = status;
            Message = "";
        }
        public PopStatusMessage(PopStatus status, int progress) {
            ProgressPercent = progress;
            Status = status;
            Message = "";
        }
        public PopStatusMessage(string msg) {
			ProgressPercent = -100;
			Status = PopStatus.None;
			Message = msg;
		}
         * */

		public PopStatus Status;
        public int ProgressPercent;
        public string Message;
        public DateTime TimeStamp;
        public string CommandParFile;   // In single mode this is the parx file,
                                        // in multiple mode this is the seq file;
        public string CurrentParFile;   // In multiple mode this is the current parx file;
        public string ExceptionMessage;
    }

    [Flags]
    public enum PopCommands {
 		None = 0x00,
        Go = 0x01,
        Stop = 0x02,
        Kill = 0x04,
        Ping = 0x08,
        PauseChecked = 0x10,
        PauseUnchecked = 0x20,
        Unknown = 0x1000
    }

	/// <summary>
	/// extension method for PopCommands
	/// that tests for command that may be combined with others
	/// usage:  command.Includes(command2)
	/// </summary>
    public static class CommandExtensions {
        public static bool Includes(this PopCommands command, PopCommands command2) {
            return (command & command2) == command2;
        }
    }

    [Flags]
    public enum PopStatus {
		None = 0x00,
        // mutually exclusive states:
        Running = 0x01,
        RunningPausePending = 0x04,
        Paused = 0x08,
        Stopped = 0x10,
        NoService = 0x20,
        // can be included with above:
		Computing = 0x200,
        DataReady = 0x400,
        Writing = 0x800
    }

    public static class StatusExtensions {
        public static bool Includes(this PopStatus status, PopStatus status2) {
            return (status & status2) == status2;
        }
        public static PopStatus ReplaceWith(this PopStatus status, PopStatus status1, PopStatus status2) {
            return ((status & ~status1) | status2);
        }
        public static PopStatus Add(this PopStatus status, PopStatus status2) {
            return (status | status2);
        }
        public static PopStatus Remove(this PopStatus status, PopStatus status1) {
            return (status & ~status1);
        }
    }


    // define interface that the client must implement
    //  to receive status updates from server
    public interface IStatusUpdateHandler {
        [OperationContract(IsOneWay = true)]
        void OnStatusUpdate(PopStatusMessage status);
    }

    // define interface that the server implements
    //  to receive commands from controller user interface.
    [ServiceContract(CallbackContract = typeof(IStatusUpdateHandler), SessionMode = SessionMode.Required)]
    public interface IPOPCommServer {

		[OperationContract]
		bool NewCommand(PopCommands command);

        [OperationContract]
        bool GetParameters(out PopParameters par);

        [OperationContract]
        bool GetPowerMeter(out double pow, out double temp, out double offset, out string units, out double freq);

        [OperationContract]
        bool GetNumDaqDevices(out int numDaq, out string[] deviceNames);

        [OperationContract]
        bool GetSampledTS(int irx, out double[] sampTS, out int nsamples);

        [OperationContract]
        bool GetCltrWvlt(int irx, int iht);

        [OperationContract]
        bool GetDopplerTS(int irx, int iht, out Ipp64fc[] dopTS, out int npts);

        [OperationContract]
        bool GetDopplerAScan(int irx, int ipt, out Ipp64fc[] ascan, out int npts);

        [OperationContract]
        bool GetSpectrum(int irx, int iht, out double[] spectrum, out int npts);

        [OperationContract]
        bool GetCrossCorr2(int irx, int iht, out POPCommunicator.PopCrossCorrArgs xcorrArgs);

        [OperationContract]
        bool GetCrossCorr3(int irx, int iht, out POPCommunicator.XCorrPlotArg arg);

        [OperationContract]
        bool GetCrossCorr(int irx, int iht, out double[] xcorrMagnitude, out double[] autoCorrMagnitude, out double[] gaussCoeffs, out double[] slope0, out int npts,
                                out int polyOrder, out double[] polyCoeffsX, out double[] polyCoeffsA);

        [OperationContract]
        bool GetCrossCorrRatio(int irx, int iht, out double[] xcorrRatio, out int npts, out LineFit line);

        [OperationContract]
        bool GetMoments(int irx, out double[] noise, out double[] power, out double[] doppler, out int nhts);

        [OperationContract]
        bool GetCrossCorrProfile(int irx, out double[] data, out double[] data2, out int nhts);

        [OperationContract]
        bool Subscribe();

        [OperationContract]
        bool Unsubscribe();
    }

    // define any custom data types
    [Serializable]
    public struct MyData {
        public string Message;
        public int Count;
    }

    /// <summary>
    /// POPCommServer: WCF server class
    /// Implements the server interface.
    /// </summary>
    /// <typeparam name="IC">callback interface type</typeparam>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class POPCommServer<IC> : DacServerBase<IC>, IPOPCommServer {

		// POPCommunicator handles communication between POPCommServer and POPService
		//	This reference is used to send received command notification.
		//	Communicator reference must be set by POPCommunicator
		//		after POPCommServer is created.
		public POPCommunicator Communicator;

		public POPCommServer() {
			Console.WriteLine("POPCommServer ctor");
			Communicator = null;
		}

        // IPOPCommServer implementation:

		public bool NewCommand(PopCommands command) {
			Communicator.FireCommandReceivedEvent(command);
			return true;
		}

        public bool GetParameters(out PopParameters par) {
            Communicator.FireParametersRequestedEvent(out par);
            return true;
        }

        public bool GetPowerMeter(out double power, out double temp, out double offset, out string units, out double freq) {
            Communicator.FirePowerMeterRequestedEvent(out power, out temp, out offset, out units, out freq);
            return true;
        }

        public bool GetNumDaqDevices(out int numDaq, out string[] deviceNames) {
            Communicator.FireNumDevicesRequestedEvent(out numDaq, out deviceNames);
            return true;
        }

        public bool GetSampledTS(int irx, out double[] sampTS, out int nsamples) {
            Communicator.FireSamplesRequestedEvent(irx, out sampTS, out nsamples);
            return true;
        }

        public bool GetCltrWvlt(int irx, int iht) {
            Communicator.FireCltrWvltRequestedEvent(irx, iht);
            return true;
        }

        public bool GetDopplerTS(int irx, int iht, out Ipp64fc[] dopTS, out int npts) {
            Communicator.FireDopplerTSRequestedEvent(irx, iht, out dopTS, out npts);
            return true;
        }

        public bool GetDopplerAScan(int irx, int ipt, out Ipp64fc[] ascan, out int npts) {
            Communicator.FireAScanRequestedEvent(irx, ipt, out ascan, out npts);
            return true;
        }

        public bool GetSpectrum(int irx, int iht, out double[] spectrum, out int npts) {
            //MessageBox.Show("GetSpectrum, irx= " + irx.ToString() + ", iht= " + iht.ToString());
            Communicator.FireSpectrumRequestedEvent(irx, iht, out spectrum, out npts);
            return true;
        }

        public bool GetCrossCorr3(int irx, int iht, out POPCommunicator.XCorrPlotArg arg) {
            arg = null;
            return true;
        }

        public bool GetCrossCorr2(int irx, int iht, out POPCommunicator.PopCrossCorrArgs xcorrArgs) {
            Communicator.FireCrossCorrRequestedEvent2(irx, iht, out xcorrArgs);
            return true;
        }

        public bool GetCrossCorr(int irx, int iht, out double[] xcorr, out double[] acorr, out double[] gauss, out double[] slope0, out int npts,
                                    out int polyOrder, out double[] polyCoeffsX, out double[] polyCoeffsA) {
            Communicator.FireCrossCorrRequestedEvent(irx, iht, out xcorr, out acorr, out gauss, out slope0, out npts, out polyOrder, out polyCoeffsX, out polyCoeffsA);
            return true;
        }

        public bool GetCrossCorrRatio(int irx, int iht, out double[] xcorrRatio, out int npts, out LineFit line) {
            Communicator.FireCrossCorrRatioRequestedEvent(irx, iht, out xcorrRatio, out npts, out line);
            return true;
        }

        public bool GetCrossCorrProfile(int irx, out double[] data, out double[] data2, out int nhts) {
            Communicator.FireCrossCorrProfileRequestedEvent(irx, out data, out data2, out nhts);
            return true;
        }

        public bool GetMoments(int irx, out double[] noise, out double[] power, out double[] doppler, out int nhts) {
            Communicator.FireMomentsRequestedEvent(irx, out noise, out power, out doppler, out nhts);
            return true;
        }

        // IStatusUpdateHandler callback
        public void UpdateStatus(PopStatusMessage status) {
            CallbackToClients("OnStatusUpdate", status);
        }
		/*
        public void UpdateStatusString(string status) {
            CallbackToClients("OnStatusUpdateText", status);
        }
		*/
    }

    /// <summary>
    /// POPCommServerHost: class to host the server.
    /// Host is created in the ctor.
    //  Server is active as long as DacServerHost object lives.
    /// Call Dispose() when done.
    /// </summary>
    public class POPCommServerHost {

        private Type serverType;
        private Type interfaceType;
        private TransportMethod transport;
        private DacServerHost<POPCommServer<IStatusUpdateHandler>, IPOPCommServer> dacHost = null;

        // the actual instance of the server created by the serverhost
        public POPCommServer<IStatusUpdateHandler> TheServer;

        public POPCommServerHost() {

            serverType = typeof(POPCommServer<IStatusUpdateHandler>);
            interfaceType = typeof(IPOPCommServer);
            transport = TransportMethod.NamedPipes;
            dacHost = null;

            try {
                dacHost = new DacServerHost<POPCommServer<IStatusUpdateHandler>, IPOPCommServer>(transport);
                TheServer = dacHost.ServerInstance;
				if (TheServer == null) {
					int x = 0;
				}
            }
            catch (Exception e) {
                if (dacHost != null) {
                    dacHost.Dispose();
                }
                //throw e;
            }
        }  // end ctor

        public void Dispose() {
            if (dacHost != null) {
                dacHost.Dispose();
            }
        }

    } //end class POPCommServerHost

    public class StatusUpdateHandler : IStatusUpdateHandler {

        public POPCommunicator Communicator;

        public void OnStatusUpdate(PopStatusMessage status) {
            Console.WriteLine("Status: " + status.Message);
            Communicator.FireStatusUpdatedEvent(status);
        }
		/*
        public void OnStatusUpdateText(string status) {
            Console.WriteLine("Status: " + status);
            PopStatusMessage statusMsg = new PopStatusMessage();
            statusMsg.Message = status;
            Communicator.FireStatusUpdatedEvent(statusMsg);
        }
		*/
    }

    /// <summary>
    /// WCF Client
    /// </summary>
    /// <typeparam name="I"></typeparam>
    /// <typeparam name="C"></typeparam>
    public class POPCommClient<I, C> : DacClientBase<I, C> where C : new() {

        private IPOPCommServer _proxy;

        // get reference to MyServer methods in ctor
        public POPCommClient(TransportMethod transport) : base(transport) {
            _proxy = GetProxy() as IPOPCommServer;
        }

        public void SendCommand(PopCommands command) {
            _proxy.NewCommand(command);
        }

        public void RequestParameters(out PopParameters par) {
            _proxy.GetParameters(out par);
        }

        public void RequestPowerMeter(out double power, out double temp, out double offset, out string units, out double freq) {
            _proxy.GetPowerMeter(out power, out temp, out offset, out units, out freq);
        }

        public void RequestNumDaqDevices(out int numDaq, out string[] deviceNames) {
            _proxy.GetNumDaqDevices(out numDaq, out deviceNames);
        }

        public void RequestSampledTS(int irx, out double[] sampTS, out int nsamples) {
            _proxy.GetSampledTS(irx, out sampTS, out nsamples);
        }

        public void RequestDopplerTS(int irx, int iht, out Ipp64fc[] dopTS, out int npts) {
            _proxy.GetDopplerTS(irx, iht, out dopTS, out npts);
        }

        public void RequestCltrWvlt(int irx, int iht) {
            _proxy.GetCltrWvlt(irx, iht);
        }

        public void RequestDopplerAScan(int irx, int ipt, out Ipp64fc[] ascan, out int npts) {
            _proxy.GetDopplerAScan(irx, ipt, out ascan, out npts);
        }

        public void RequestSpectrum(int irx, int iht, out double[] spectrum, out int npts) {
            _proxy.GetSpectrum(irx, iht, out spectrum, out npts);
        }

        public void RequestCrossCorr(int irx, int iht, out double[] xcorr, out double[] acorr, out double[] gaussCoeffs, out double[] slope0, out int npts,
                                        out int polyOrder, out double[] polyCoeffsX, out double[] polyCoeffsA) {
            _proxy.GetCrossCorr(irx, iht, out xcorr, out acorr, out gaussCoeffs, out slope0, out npts, out polyOrder, out polyCoeffsX, out polyCoeffsA);
        }

        public void RequestCrossCorr2(int irx, int iht, out POPCommunicator.PopCrossCorrArgs xcorrArgs) {
            _proxy.GetCrossCorr2(irx, iht, out xcorrArgs);
        }

        public void RequestCrossCorrRatio(int irx, int iht, out double[] xcorrRatio, out int npts, out LineFit line) {
            _proxy.GetCrossCorrRatio(irx, iht, out xcorrRatio, out npts, out line);
        }

        public void RequestMoments(int irx, out double[] noise, out double[] power, out double[] doppler, out int nhts) {
            _proxy.GetMoments(irx, out noise, out power, out doppler, out nhts);
        }

        public void RequestCrossCorrProfile(int irx, out double[] data, out double[] data2, out int nhts) {
            _proxy.GetCrossCorrProfile(irx, out data, out data2, out nhts);
        }
    }
	
	/// <summary>
	/// POPCommunicator: wrapper for client and server
	/// </summary>
    public class POPCommunicator {

		///////////////////////////////////////////////////////////////////////
		// CommandReceived Event
        // and StatusUpdate Event
		///////////////////////////////////////////////////////////////////////

		/// <summary>
		/// This class is derived from EventArgs
		/// to send PopCommands via PopCommander Events.
		/// </summary>
		public class PopCommandArgs : EventArgs {
			public PopCommandArgs(PopCommands command) {
				Command = command;
			}
			public PopCommands Command;
		}  // end of class PopCommandArgs

        public class PopStatusArgs : EventArgs {
            public PopStatusArgs(PopStatusMessage status) {
                Status = status;
            }
            public PopStatusMessage Status;
        }

        public class PopParamArgs : EventArgs {
            public PopParamArgs(PopParameters param) {
                Params = param;
            }
            public PopParameters Params;
        }

        public class PopPowerMeterArgs : EventArgs {
            public PopPowerMeterArgs(double power, double temp, double offset, string tunits, double freq) {
                PowerReading = power;
                TempReading = temp;
                TempUnits = tunits;
                PowerOffset = offset;
                FreqMHz = freq;
            }
            public double PowerReading;
            public double TempReading;
            public string TempUnits;
            public double PowerOffset;
            public double FreqMHz;
        }

        public class NumDaqDevicesArgs : EventArgs {
            public NumDaqDevicesArgs(int numDev) {
                NumDaqDevices = numDev;
                DaqDeviceNames = null;
            }
            public int NumDaqDevices;
            public string[] DaqDeviceNames;
        }

        public class PopSamplesArgs : EventArgs {
            public PopSamplesArgs(int irx, double[] sampTS, int nSamples = 0) {
                SampTS = sampTS;
                IRx = irx;
                NSamples = nSamples;
            }
            public double[] SampTS;
            public int IRx;
            public int NSamples;
        }

        public class PopAScanArgs : EventArgs {
            public PopAScanArgs(int irx, int ipt, Ipp64fc[] ascan, int nhts = 0) {
                AScan = ascan;
                IRx = irx;
                IPt = ipt;
                NGates = nhts;
            }
            public int IRx;          // requested receiver index
            public int IPt;          // requested Doppler point index
            public Ipp64fc[] AScan;  // returned Doppler ascan array
            public int NGates;         // returned size of Doppler ascan array
        }

        public class PopSpectrumArgs : EventArgs {
            public PopSpectrumArgs(int irx, int iht, double[] spectrum, int npts = 0) {
                Spectrum = spectrum;
                IRx = irx;
                IHt = iht;
                NPts = npts;
            }
            public double[] Spectrum;
            public int IRx;
            public int IHt;
            public int NPts;
        }

        public class XCorrPlotArg : EventArgs {
            public int x;
            public string ss;
            public double[] ary;
        }

        public class PopCrossCorrArgs : EventArgs {

            public PopCrossCorrArgs() {
                // needs to have default constructor defined to use 
                //  this class in [operation contract]
            }

            public PopCrossCorrArgs(int irx, int iht, double[] xcorr, int npts = 0) {
                CrossCorr = xcorr;
                IRx = irx;
                IHt = iht;
                NLags = npts;
            }
            public double[] CrossCorr;
            public double[] AutoCorr;
            public double[] GaussCoeffs;
            public double[] FcaLags;        // double[3]: taui, taup, taux
            public double[] SlopeAtZero;  // slope and vel
            public int IRx;
            public int IHt;
            public int NLags;
            public int NPts;  // total pts in time series
            public int NAvgs;
            public LineFit Line; // parameters of LSQ line fit to xc ratio, y = mx+b
            public int PolyFitOrder;
            public double[] PolyFitCoeffsX;  // coeffs for polynomial fit to XCorr
            public double[] PolyFitCoeffsA;  // coeffs for polynomial fit to autoCorr
            public double AutoBaseline, CrossBaseline;
            public int AutoPolyFitPts;
            public int XCorrPeakI;
            public double AntennaDeltaX;
        }

        public class PopCltrWvltArgs : EventArgs {
            public PopCltrWvltArgs(int irx, int iht) {
                IRx = irx;
                IHt = iht;
            }
            public int IRx;
            public int IHt;
        }

        public class PopDopplerTSArgs : EventArgs {
            public PopDopplerTSArgs(int irx, int iht, Ipp64fc[] dopTS, int npts = 0) {
                DopplerTS = dopTS;
                IRx = irx;
                IHt = iht;
                NPts = npts;
            }
            public Ipp64fc[] DopplerTS;
            public int IRx;
            public int IHt;
            public int NPts;
        }

        public class PopMomentsArgs : EventArgs {
            public PopMomentsArgs(int irx, double[] noise, double[] pow, double[] doppler, int nhts = 0) {
                Noise = noise;
                Power = pow;
                Doppler = doppler;
                IRx = irx;
                NHts = nhts;
            }
            public double[] Doppler;
            public double[] Power;
            public double[] Noise;
            public int IRx;
            public int NHts;
        }

        public class PopProfileArgs : EventArgs {
            public PopProfileArgs(int irx, double[] data, double[] data2, int nhts = 0) {
                Data = data;
                Data2 = data2;
                IRx = irx;
                NHts = nhts;
            }
            public double[] Data;
            public double[] Data2;
            public int IRx;
            public int NHts;
        }

        // declare handler for outsiders to connect to e.g.
        // define Event handler to notify outsiders when command is received (server).
        // define Event handler to notify outsiders when status is updated (client).
        // _communicator.CommandReceived2 += ReceivedCommand; 
        // _communicator.StatusUpdated2 += OnStatusUpdated; 
        public EventHandler<PopCommandArgs> CommandReceived;
        public EventHandler<PopStatusArgs> StatusUpdated;
        public EventHandler<PopParamArgs> ParamsRequested;
        public EventHandler<PopPowerMeterArgs> PowerMeterRequested;
        public EventHandler<NumDaqDevicesArgs> NumDaqDevicesRequested;
        public EventHandler<PopSamplesArgs> SamplesRequested;
        public EventHandler<PopDopplerTSArgs> DopplerTSRequested;
        public EventHandler<PopCltrWvltArgs> CltrWvltRequested;
        public EventHandler<PopAScanArgs> AScanRequested;
        public EventHandler<PopSpectrumArgs> SpectrumRequested;
        public EventHandler<PopCrossCorrArgs> CrossCorrRequested;
        public EventHandler<PopCrossCorrArgs> CrossCorrRatioRequested;
        public EventHandler<PopProfileArgs> CrossCorrProfileRequested;
        public EventHandler<PopMomentsArgs> MomentsRequested;

        //public delegate void PopCommandReceivedHandler(object sender, PopCommandArgs arg);
        // define Event handler to notify outsiders when status is updated (client).
        //public delegate void PopStatusUpdateHandler(object sender, PopStatusArgs arg);

		// declare handler for outsiders to connect to e.g.
		// _communicator.CommandReceived += ReceivedCommand; 
		// where
		// public void ReceivedCommand(object sender, PopCommandArgs arg) {}
		// public event PopCommandReceivedHandler CommandReceived;

        // _communicator.StatusUpdated += OnStatusUpdated; 
        // where
        // public void OnStatusUpdated(object sender, PopStatusArgs arg) {}
        //public event PopStatusUpdateHandler StatusUpdated;

        // Here is where we fire the CommandReceivedEvent when a command is received by server.
        public virtual void FireCommandReceivedEvent(PopCommands command) {
            EventHandler<PopCommandArgs> handler = CommandReceived;
            if (handler != null) {
                PopCommandArgs args = new PopCommandArgs(command);
                handler(this, args);
            }
        }

        // Here is where we fire the ParametersRequestedEvent when UI asks for parameters.
        public virtual void FireParametersRequestedEvent(out PopParameters param) {
            EventHandler<PopParamArgs> handler = ParamsRequested;
            param = null;
            if (handler != null) {
                PopParamArgs args = new PopParamArgs(param);
                handler(this, args);
                //param = args.Params.DeepCopy();
                param = args.Params;
            }
        }

        public virtual void FirePowerMeterRequestedEvent(out double power, out double temp, out double offset, out string units, out double freq) {
            EventHandler<PopPowerMeterArgs> handler = PowerMeterRequested;
            power = temp = offset = freq = 0.0;
            units = "";
            if (handler != null) {
                PopPowerMeterArgs args = new PopPowerMeterArgs(power, temp, offset, units, freq);
                handler(this, args);
                power = args.PowerReading;
                temp = args.TempReading;
                offset = args.PowerOffset;
                units = args.TempUnits;
                freq = args.FreqMHz;
            }
        }

        // Here is where we fire the NumDevicesRequestedEvent when UI asks for number of DAQ devices.
        public virtual void FireNumDevicesRequestedEvent(out int numDAQ, out string[] deviceNames) {
            EventHandler<NumDaqDevicesArgs> handler = NumDaqDevicesRequested;
            numDAQ = 0;
            deviceNames = null;
            if (handler != null) {
                NumDaqDevicesArgs args = new NumDaqDevicesArgs(numDAQ);
                try {
                    handler(this, args);
                }
                catch (FileNotFoundException ex) {
                    PopStatusMessage msg = new PopStatusMessage("Error finding or loading MCC or IOTech DAQ library.");
                    UpdateStatus(msg);
                    msg.Message = ex.Message;
                    UpdateStatus(msg);
                    numDAQ = 0;
                    deviceNames = null;
                    return;
                }
                catch (Exception ex) {
                    PopStatusMessage msg = new PopStatusMessage("Exception thrown requesting DAQ devices.");
                    UpdateStatus(msg);
                    msg.Message = ex.Message;
                    UpdateStatus(msg);
                    numDAQ = 0;
                    deviceNames = null;
                    return;
                }
                //param = args.Params.DeepCopy();
                numDAQ = args.NumDaqDevices;
                deviceNames = args.DaqDeviceNames;
            }
        }

        // Here is where we fire the ParametersRequestedEvent when UI asks for sampled time series.
        public virtual void FireSamplesRequestedEvent(int irx, out double[] sampTS, out int nsamples) {
            EventHandler<PopSamplesArgs> handler = SamplesRequested;
            sampTS = null;
            nsamples = 0;
            if (handler != null) {
                PopSamplesArgs args = new PopSamplesArgs(irx, sampTS);
                handler(this, args);
                sampTS = args.SampTS;
                nsamples = args.NSamples;
            }
            else {
                sampTS = null;
                nsamples = 0;
            }
        }

        public virtual void FireCltrWvltRequestedEvent(int irx, int iht) {
            EventHandler<PopCltrWvltArgs> handler = CltrWvltRequested;
            if (handler != null) {
                PopCltrWvltArgs args = new PopCltrWvltArgs(irx, iht);
                handler(this, args);
            }
        }

        public virtual void FireDopplerTSRequestedEvent(int irx, int iht, out Ipp64fc[] dopTS, out int npts) {
            EventHandler<PopDopplerTSArgs> handler = DopplerTSRequested;
            dopTS = null;
            npts = 0;
            if (handler != null) {
                PopDopplerTSArgs args = new PopDopplerTSArgs(irx, iht, dopTS);
                handler(this, args);
                dopTS = args.DopplerTS;
                npts = args.NPts;
            }
            else {
                dopTS = null;
                npts = 0;
            }
        }

        public virtual void FireAScanRequestedEvent(int irx, int ipt, out Ipp64fc[] ascan, out int ngates) {
            EventHandler<PopAScanArgs> handler = AScanRequested;
            ascan = null;
            ngates = 0;
            if (handler != null) {
                PopAScanArgs args = new PopAScanArgs(irx, ipt, ascan);
                handler(this, args);
                ascan = args.AScan;
                ngates = args.NGates;
            }
            else {
                ascan = null;
                ngates = 0;
            }
        }

        public virtual void FireSpectrumRequestedEvent(int irx, int iht, out double[] spectrum, out int npts) {
            //MessageBox.Show("FireSpectrumRequestEvent; irx = " + irx.ToString());
            EventHandler<PopSpectrumArgs> handler = SpectrumRequested;
            spectrum = null;
            npts = 0;
            if (handler != null) {
                PopSpectrumArgs args = new PopSpectrumArgs(irx, iht, spectrum);
                handler(this, args);
                spectrum = args.Spectrum;
                npts = args.NPts;
            }
            else {
                spectrum = null;
                npts = 0;
            }
        }

        public virtual void FireCrossCorrRequestedEvent2(int irx, int iht, out PopCrossCorrArgs xcorrArgs) {
            EventHandler<PopCrossCorrArgs> handler = CrossCorrRequested;
            xcorrArgs = null;
            double[] xcorr = null;
            if (handler != null) {
                PopCrossCorrArgs args = new PopCrossCorrArgs(irx, iht, xcorr);
                handler(this, args);
                xcorrArgs = args;
            }
        }

        public virtual void FireCrossCorrRequestedEvent(int irx, int iht, out double[] xcorr, out double[] acorr, out double[] gauss, out double[] slope0, out int npts,
                                                        out int polyOrder, out double[] polyCoeffsX, out double[] polyCoeffsA) {
            EventHandler<PopCrossCorrArgs> handler = CrossCorrRequested;
            xcorr = null;
            acorr = null;
            npts = 0;
            if (handler != null) {
                PopCrossCorrArgs args = new PopCrossCorrArgs(irx, iht, xcorr);
                handler(this, args);
                xcorr = args.CrossCorr;
                acorr = args.AutoCorr;
                gauss = args.GaussCoeffs;
                slope0 = args.SlopeAtZero;
                npts = args.NLags;
                polyCoeffsX = args.PolyFitCoeffsX;
                polyCoeffsA = args.PolyFitCoeffsA;
                polyOrder = args.PolyFitOrder;
            }
            else {
                xcorr = null;
                acorr = null;
                gauss = null;
                slope0 = null;
                npts = 0;
                polyCoeffsX = null;
                polyCoeffsA = null;
                polyOrder = 0;
            }
        }

        public virtual void FireCrossCorrRatioRequestedEvent(int irx, int iht, out double[] xcorrRatio, out int npts, out LineFit line) {
            EventHandler<PopCrossCorrArgs> handler = CrossCorrRatioRequested;
            xcorrRatio = null;
            npts = 0;
            if (handler != null) {
                PopCrossCorrArgs args = new PopCrossCorrArgs(irx, iht, xcorrRatio);
                handler(this, args);
                xcorrRatio = args.CrossCorr;
                npts = args.NLags;
                line = args.Line;
            }
            else {
                xcorrRatio = null;
                npts = 0;
                line.B = 0.0;
                line.M = 0.0;
            }
        }

        public virtual void FireCrossCorrProfileRequestedEvent(int irx, out double[]data, out double[] data2, out int nhts) {
            EventHandler<PopProfileArgs> handler = CrossCorrProfileRequested;
            data = null;
            data2 = null;
            nhts = 0;
            if (handler != null) {
                PopProfileArgs args = new PopProfileArgs(irx, data, data2, nhts);
                handler(this, args);
                data = args.Data;
                data2 = args.Data2;
                nhts = args.NHts;
            }
        }

        public virtual void FireMomentsRequestedEvent(int irx, out double[] noise, out double[] power, out double[] doppler, out int nhts) {
            EventHandler<PopMomentsArgs> handler = MomentsRequested;
            noise = null;
            power = null;
            doppler = null;
            nhts = 0;
            if (handler != null) {
                PopMomentsArgs args = new PopMomentsArgs(irx, noise, power, doppler);
                handler(this, args);
                noise = args.Noise;
                power = args.Power;
                doppler = args.Doppler;
                nhts = args.NHts;
            }
        }

        // Here is where the client fires the StatusUpdated event when the status is updated.
        public virtual void FireStatusUpdatedEvent(PopStatusMessage status) {
            EventHandler<PopStatusArgs> handler = StatusUpdated;
            if (handler != null) {
                PopStatusArgs args = new PopStatusArgs(status);
                handler(this, args);
            }
        }

        ///////////////////////////////////////////////////////////////////////
		// WCF Client/Server Setup
		///////////////////////////////////////////////////////////////////////

		public enum POPCommType {
            Client,
            Server,
			ClientServer
        }

        public POPCommType CommType;

		public int SubscriberCount {
			get { return _commServer.SubscriberCount; }
		}

        // for client
		// Client should be private, use another flag for failed connections
        public POPCommClient<IPOPCommServer, StatusUpdateHandler> Client;
        // for server
        private POPCommServerHost _host;
        private POPCommServer<IStatusUpdateHandler> _commServer;

        private POPCommunicator() {
        }

        // ctor constructs POPCommunicator of proper client/server type
        public POPCommunicator(POPCommType type) {
            if ((type == POPCommType.Client) || (type == POPCommType.ClientServer)) {
                try {
                    // create client and let callback handler object 
                    //  know about Communicator to be able to fire event.
                    Client = new POPCommClient<IPOPCommServer, StatusUpdateHandler>(TransportMethod.NamedPipes);
                    StatusUpdateHandler callbackHandler = Client.CallbackObject;
                    callbackHandler.Communicator = this;
                }
                catch {
                    Client = null;
                }
            }
            else {
                Client = null;
            }
			if ((type == POPCommType.Server) || (type == POPCommType.ClientServer)) {
                _host = new POPCommServerHost();
                _commServer = _host.TheServer;
                if (_commServer != null) {
                    _commServer.Communicator = this;
                }
			}
			else {
                _host = null;
                _commServer = null;
			}
        }

		// called by client to send command to server
		/*
		public bool SendCommand(string command) {
			int id = Thread.CurrentThread.ManagedThreadId;
			if (Client != null) {
				try {
					Client.SendCommand(command);
					return true;
				}
				catch (CommunicationObjectFaultedException) {
					return false;
				}
				catch (Exception e) {
					return false;
				}
			}
			else {
				return false;
			}
		}
		*/

        // called by client to send command to server
        public bool SendCommand(PopCommands command) {
            int id = Thread.CurrentThread.ManagedThreadId;
            if (Client != null) {
                try {
                    Client.SendCommand(command);
                    return true;
                }
                catch (CommunicationObjectFaultedException) {
                    return false;
                }
                catch (Exception e) {
                    return false;
                }
            }
            else {
                return false;
            }
        }

        // called by client to request number of DAQ devices from server
        public bool RequestNumDaqDevices(out int numDaq, out string[] deviceNames) {
            numDaq = 0;
            deviceNames = null;
            if (Client != null) {
                try {
                    Client.RequestNumDaqDevices(out numDaq, out deviceNames);
                    return true;
                }
                catch (CommunicationObjectFaultedException) {
                    return false;
                }
                catch (Exception e) {
                    return false;
                }
            }
            else {
                return false;
            }
        }

        // called by client to request parameters from server
        public bool RequestParameters(out PopParameters par) {
            par = null;
            int id = Thread.CurrentThread.ManagedThreadId;
            if (Client != null) {
                try {
                    Client.RequestParameters(out par);
                    return true;
                }
                catch (CommunicationObjectFaultedException) {
                    return false;
                }
                catch (Exception e) {
                    return false;
                }
            }
            else {
                return false;
            }
        }

        // called by client to request parameters from server
        public bool RequestPowerMeter(out double pow, out double temp, out double offset, out string units, out double freq) {
            pow = temp = offset = freq = 0.0;
            units = "";
            int id = Thread.CurrentThread.ManagedThreadId;
            if (Client != null) {
                try {
                    Client.RequestPowerMeter(out pow, out temp, out offset, out units, out freq);
                    return true;
                }
                catch (CommunicationObjectFaultedException) {
                    return false;
                }
                catch (Exception e) {
                    return false;
                }
            }
            else {
                return false;
            }
        }

        // called by client to request sampled time series from server
        public bool RequestSampledTS(int irx, out double[] sampTS, out int nsamples) {
            sampTS = null;
            nsamples = 0;
            int id = Thread.CurrentThread.ManagedThreadId;
            if (Client != null) {
                try {
                    Client.RequestSampledTS(irx, out sampTS,  out nsamples);
                    return true;
                }
                catch (CommunicationObjectFaultedException) {
                    return false;
                }
                catch (Exception e) {
                    return false;
                }
            }
            else {
                return false;
            }
        }

        public bool RequestDopplerAScan(int irx, int ipt, out Ipp64fc[] ascan, out int npts) {
            ascan = null;
            npts = 0;
            if (Client != null) {
                try {
                    Client.RequestDopplerAScan(irx, ipt, out ascan, out npts);
                    return true;
                }
                catch (CommunicationObjectFaultedException) {
                    return false;
                }
                catch (Exception e) {
                    return false;
                }
            }
            else {
                return false;
            }
        }

        public bool RequestSpectrum(int irx, int iht, out double[] spectrum, out int npts) {
            spectrum = null;
            npts = 0;
            if (Client != null) {
                try {
                    Client.RequestSpectrum(irx, iht, out spectrum, out npts);
                    return true;
                }
                catch (CommunicationObjectFaultedException) {
                    return false;
                }
                catch (Exception e) {
                    return false;
                }
            }
            else {
                return false;
            }
        }

        public bool RequestCrossCorr(int irx, int iht, out double[] xcorr, out double[] acorr, out double[] gaussCoeffs, out double[] slope0, out int npts,
                                        out int polyOrder, out double[] polyCoeffsX, out double[] polyCoeffsA ) {
            xcorr = null;
            acorr = null;
            gaussCoeffs = null;
            npts = 0;
            slope0 = null;
            polyOrder = 0;
            polyCoeffsA = null;
            polyCoeffsX = null;
            if (Client != null) {
                try {
                    Client.RequestCrossCorr(irx, iht, out xcorr, out acorr, out gaussCoeffs, out slope0, out npts, out polyOrder, out polyCoeffsX, out polyCoeffsA);
                    return true;
                }
                catch (CommunicationObjectFaultedException) {
                    return false;
                }
                catch (Exception e) {
                    return false;
                }
            }
            else {
                return false;
            }
        }

        public bool RequestCrossCorr2(int irx, int iht, out POPCommunicator.PopCrossCorrArgs xcorrArgs) {
            xcorrArgs = null;
            if (Client != null) {
                try {
                    Client.RequestCrossCorr2(irx, iht, out xcorrArgs);
                    return true;
                }
                catch (CommunicationObjectFaultedException) {
                    return false;
                }
                catch (Exception e) {
                    return false;
                }
            }
            else {
                return false;
            }
        }

        public bool RequestCrossCorrRatio(int irx, int iht, out double[] xcorrRatio, out int npts, out LineFit line) {
            xcorrRatio = null;
            npts = 0;
            line.B = 0.0;
            line.M = 0.0;
            if (Client != null) {
                try {
                    Client.RequestCrossCorrRatio(irx, iht, out xcorrRatio, out npts, out line);
                    return true;
                }
                catch (CommunicationObjectFaultedException) {
                    return false;
                }
                catch (Exception e) {
                    return false;
                }
            }
            else {
                return false;
            }
        }

        public bool RequestCltrWvlt(int irx, int iht) {
            if (Client != null) {
                try {
                    Client.RequestCltrWvlt(irx, iht);
                    return true;
                }
                catch (CommunicationObjectFaultedException) {
                    return false;
                }
                catch (Exception e) {
                    return false;
                }
            }
            else {
                return false;
            }
        }

        public bool RequestDopplerTS(int irx, int iht, out Ipp64fc[] dopTS, out int npts) {
            dopTS = null;
            npts = 0;
            if (Client != null) {
                try {
                    Client.RequestDopplerTS(irx, iht, out dopTS, out npts);
                    return true;
                }
                catch (CommunicationObjectFaultedException) {
                    return false;
                }
                catch (Exception e) {
                    return false;
                }
            }
            else {
                return false;
            }
        }

        public bool RequestMoments(int irx, out double[] noise, out double[] power, out double[] doppler, out int nhts) {
            noise = null;
            power = null;
            doppler = null;
            nhts = 0;
            if (Client != null) {
                try {
                    Client.RequestMoments(irx, out noise, out power, out doppler, out nhts);
                    return true;
                }
                catch (CommunicationObjectFaultedException) {
                    return false;
                }
                catch (Exception e) {
                    return false;
                }
            }
            else {
                return false;
            }
        }

        public bool RequestCrossCorrProfile(int irx, out double[] data, out double[] data2, out int nhts) {
            data = null;
            data2 = null;
            nhts = 0;
            if (Client != null) {
                try {
                    Client.RequestCrossCorrProfile(irx, out data, out data2, out nhts);
                    return true;
                }
                catch (CommunicationObjectFaultedException) {
                    return false;
                }
                catch (Exception e) {
                    return false;
                }
            }
            else {
                return false;
            }
        }

        // called by server to send status message to client
        public void UpdateStatus(PopStatusMessage status) {
            if (_host != null) {
                if (_host.TheServer != null) {
                    _host.TheServer.UpdateStatus(status);
                }
            }
        }
		/*
        public void UpdateStatus(string statusText) {
            if (_host != null) {
                _host.TheServer.UpdateStatusString(statusText);
            }
        }
		*/
    }

}
