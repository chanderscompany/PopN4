using System;
using System.Collections.Generic;
using System.IO;

using DACarter.PopUtilities;
using DACarter.Utilities;

namespace DACarter.NOAA.Hardware {
    /// <summary>
    /// Base Class DAQDevice
    /// </summary>
    public abstract class DAQDevice : IDisposable {

        #region abstract methods
        public abstract void Start();
        public abstract void Stop();
        public abstract void Close();
        public abstract void Setup();
        #endregion abstract methods

        #region protected members
        protected bool _cancel;
        protected bool _disposed;
        protected int _totalScans;
        protected bool _needsSetup;
        protected int _numDevices;
        //protected int _nSpecTotal;
        protected Array _dataSubArray;
        protected float[] _dataArray;
        protected short[][] _intDataArray;
        protected bool _testWithoutPbx;
        protected double[][][][] _externalMatrix;
        protected int _nrx, _nspec, _npts, _ngates;
        protected bool _allDataCompleted;
        protected int _maxTotalDataAtOnce;  // limit for # points to acquire at once (32-bit only?)
        protected List<string> _deviceNames;
        protected List<string> _deviceTypes;
        protected List<string> _deviceSerialNumberLabels;
        protected List<int> _serialNumbers;
        protected double _progress;			 // fractional progress completed (0.0 - 1.0)
        protected VoltageRange _maxAnalogInput;
        protected MeasurementUnits _analogInputUnits;
        protected int _sampleRate;
        protected bool _aborted;
        protected Exception _acqException;
        protected static PopParameters _parameters;
        protected static string _DAQLibrary;
        #endregion protected members

        #region Public Enums
        //
        public enum VoltageRange {
            Volts5,
            Volts10
        }

        public enum MeasurementUnits {
            Volts,
            Raw
        }

        public enum DIOPorts {
            NullPort,
            PortA,
            PortB,
            PortC
        }
        //
        #endregion


        #region public properties

        public PopUtilities.PopParameters.RxIDParameters[] RxID;

        public string DAQLibraryName {
            get { return _DAQLibrary;}
            //set { _DAQLibrary = value;}
        }

        public List<int> SerialNumbers {
            get { return _serialNumbers; }
            set { _serialNumbers = value; }
        }

        public bool UserCancel {
            get { return _cancel; }
            set { _cancel = value; }
        }

        public bool DataIsAvailable {
            get { return _allDataCompleted; }
        }

        public bool TestWithoutPbx {
            get { return _testWithoutPbx; }
            set { _testWithoutPbx = value; }
        }

        public Array BufferForSamples {
            get { return _dataSubArray; }
            set { _dataSubArray = value; }
        }

        public PopParameters Parameters {
            get { return _parameters;}
            set { _parameters = value;}
        }

        /// <summary>
        /// This float array is created internally
        /// to hold all acquired data values.
        /// </summary>
        /// <remarks>
        /// IOTech version provides float data
        /// </remarks>
        public float[] DataArray {
            get { return _dataArray; }
        }

        /// <summary>
        /// This integer array is created internally
        /// to hold all acquired data values.
        /// </summary>
        /// <remarks>
        /// MCC version provides integer data.
        /// </remarks>
        public short[][] IntDataArray {
            get { return _intDataArray; }
        }

        /// <summary>
        /// if this array is provided by caller,
        /// then do not create internal DataArray
        /// </summary>
        public double[][][][] OutputMultiDimArray {
            get { return _externalMatrix; }
            set {
                //throw new ApplicationException("DAQBoard external OutputArray not supported as of rev 3.17.");
                _externalMatrix = value;
                _nrx = _externalMatrix.GetLength(0);
                _nspec = _externalMatrix.GetLength(1);
                _npts = _externalMatrix.GetLength(2);
                _ngates = _externalMatrix.GetLength(3);
            }
        }

        public int NumDevices {
            get {
                // set in constructor
                return _numDevices;
            }
        }

        public int InternalSampleRate {
            // only relevant if internal clock used
            get { return _sampleRate; }
            set { _sampleRate = value; }
        }

        public int NDataSamplesPerDevice {
            // number of samples taken on each daq device
            get { return _totalScans; }
            set {
                int oldValue = _totalScans;
                // need to call Setup() if property changed
                if (oldValue != value) {
                    _totalScans = value;
                    _needsSetup = true;
                }
            }
        }

        virtual public double Progress {
            get { return _progress; }
        }

        public List<string> DeviceNames {
            get { return _deviceNames; }
        }

        public List<string> DeviceTypes {
            get { return _deviceTypes; }
        }

        public List<string> DeviceSerialNumbers {
            get { return _deviceSerialNumberLabels; }
        }

        public VoltageRange MaxAnalogInput {
            get { return _maxAnalogInput; }
            set {
                VoltageRange oldValue = _maxAnalogInput;
                // need to call Setup() if property changed
                if (oldValue != value) {
                    _maxAnalogInput = value;
                    _needsSetup = true;
                }
            }
        }
        public MeasurementUnits AnalogInputUnits {
            get { return _analogInputUnits; }
            set {
                MeasurementUnits oldValue = _analogInputUnits;
                // need to call Setup() if property changed
                if (oldValue != value) {
                    _analogInputUnits = value;
                    _needsSetup = true;
                }
            }
        }

        public Exception AcqException {
            get { return _acqException; }
        }

        public bool Aborted {
            get { return _aborted; }
        }
        
        #endregion properties

        /// <summary>
        /// Constructor
        /// </summary>
        public DAQDevice() {
            _externalMatrix = null;
            _nrx = _nspec = _npts = _ngates = -1;
            _allDataCompleted = false;
            _testWithoutPbx = false;
            _needsSetup = true;
            _disposed = false;
            _deviceNames = new List<string>();
            _deviceTypes = new List<string>();
            _deviceSerialNumberLabels = new List<string>();
            _serialNumbers = new List<int>();
            _analogInputUnits = MeasurementUnits.Raw;
            _sampleRate = 1000000;
            _aborted = false;
            _acqException = null;
            _parameters = null;
            _DAQLibrary = "Unknown";
        }

        public static DAQDevice GetAttachedDAQ() {
            //MessageBox.Show("In GetAttachedDAQ()");
            PopParameters nopar = null;
            DAQDevice dev = null;
            //try {
                dev = GetAttachedDAQ(nopar);
            //}
            //catch (FileNotFoundException ee) {
            //    MessageBoxEx.Show("Exception 1" + " -- Exception calling GetAttachedDAQ: \n" + ee.Message, 5000);
            //}
            //catch (Exception ee) {
            //    MessageBoxEx.Show("Exception 2" + " -- Exception calling GetAttachedDAQ: \n" + ee.Message, 5000);
            //}
            //MessageBox.Show("GetAttachedDAQ dev==null is " + (dev==null).ToString());
            //if (dev != null) {
            //    MessageBox.Show("devices = " + (dev.NumDevices).ToString());
            //}
            return dev;
        }

        static int flag = 0;

        /// <summary>
        /// This method calls the method GetAttachedDAQActual()
        ///     that actually contains the code.
        /// But here we try to catch exceptions that for some strange reason
        ///     cannot be caught within that method and propogate up to here.
        /// If MCCDaqLib.dll is missing or is wrong version
        ///     GetAttachedDAQActual() throws exception when it is called,
        ///     WITHOUT executing ANY code inside that method,
        ///     as long as there is a line
        ///     daq = new DAQBoardMCC(_parameters) in the routine.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static DAQDevice GetAttachedDAQ(PopParameters parameters) {
            DAQDevice daq = null;
            flag += 1;
            try {
                daq = GetAttachedDAQActual(parameters);
            }
            catch (FileNotFoundException ee) {
                MessageBoxEx.Show("Exception 3" + " -- Exception calling GetAttachedDAQ: \n" + ee.Message, 5000);
                //MessageBox.Show("Flag = " + flag.ToString());  // flag == 1 here
                throw;
            }
            catch (Exception ee) {
                MessageBoxEx.Show("Exception 4" + " -- Exception calling GetAttachedDAQ: \n" + ee.Message, 5000);
                //MessageBox.Show(ee.StackTrace);
                throw;
            }
            //MessageBox.Show("Flag = " + flag.ToString());
            return daq;
        }

        /// <summary>
        /// Static method to get the currently attached DAQDevice object
        /// This method contains the actual code for the GetAttachedDAQ method,
        /// </summary>
        /// <returns></returns>
        public static DAQDevice GetAttachedDAQActual(PopParameters parameters) {

            flag += 2;
            //return null;  // no exeception
            _parameters = parameters;

            DAQDevice daq = null;
            int bits = IntPtr.Size * 8;
            //MessageBox.Show("Number of bits == " + bits.ToString());


            // return null;  // no exeception

            if (bits == 64) {
                // 64-bit systems need MCC board
                //MessageBox.Show("Handling Number of bits ==  64 ");
                //
                //
                try {
                    flag += 4;
                    // commenting this out eliminates exception on 32-bit system:
                    daq = new DAQBoardMCC(_parameters);
                }
                catch (FileNotFoundException ee) {
                    //MessageBox.Show("Exception in 64bit MCC");
                    return null;
                }
                //
                //
            }
            else if (bits == 32) {
                // 32-bit systems can use either board
                //MessageBox.Show("Handling Number of bits ==  32 ");
                flag += 8;
                try {
                    // see if MCC board is attached:
                    daq = new DAQBoardMCC(_parameters);
                    //MessageBox.Show("Return from MCC, daq==null is " + (daq == null).ToString());
                    if (daq != null) {
                        //MessageBox.Show("Num devices = " + (daq.NumDevices).ToString());
                    }
                    if ((daq == null) || ((daq != null) && (daq.NumDevices < 1))) {
                        daq = null;
                        //MessageBox.Show("Try IOTech...");
                        daq = new DAQBoardIOTech(_parameters);
                        //MessageBox.Show("constructed IOTechDAQ 1 " + (daq == null).ToString());
                    }
                }
                catch (FileNotFoundException ee) {
                    //MessageBox.Show("Ready to construct IOTechDAQ 2a");
                    daq = new DAQBoardIOTech(_parameters);
                }
                catch (Exception ex) {
                    //MessageBox.Show("Ready to construct IOTechDAQ 2");
                    daq = new DAQBoardIOTech(_parameters);
                }
            }
            else {
                //MessageBox.Show("Handling Number of bits ==  neither ");
            }

            if ((daq != null) && (daq.NumDevices < 1)) {
                daq = null;
            }

            return daq;
        }

        public void Dispose() {
            // not virtual; do not override;
            //Console.WriteLine("In Base Dispose()");
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            //Console.WriteLine("In Base Dispose(" + disposing.ToString() + ")");
            if (_disposed) {
                return;
            }
            //....
            _disposed = true;
        }

        ~DAQDevice() {
            // this calls Dispose(false) in derived class:
            //Console.WriteLine("In Base Finalizer");
            Dispose(false);
        }

    }  // end DAQDevice base class
}
