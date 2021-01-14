using System;
using System.Collections.Generic;

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
        protected List<string> _deviceSerialNumbers;
        protected double _progress;			 // fractional progress completed (0.0 - 1.0)
        protected VoltageRange _maxAnalogInput;
        protected MeasurementUnits _analogInputUnits;
        protected int _sampleRate;
        protected bool _aborted;
        protected Exception _acqException;
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

        public double Progress {
            get { return _progress; }
        }

        public List<string> DeviceNames {
            get { return _deviceNames; }
        }

        public List<string> DeviceTypes {
            get { return _deviceTypes; }
        }

        public List<string> DeviceSerialNumbers {
            get { return _deviceSerialNumbers; }
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
            _deviceSerialNumbers = new List<string>();
            _analogInputUnits = MeasurementUnits.Raw;
            _sampleRate = 1000000;
            _aborted = false;
            _acqException = null;
        }

        /// <summary>
        /// Static method to get the currently attached DAQDevice object
        /// </summary>
        /// <returns></returns>
        public static DAQDevice GetAttachedDAQ() {

            DAQDevice daq = null;
            int bits = IntPtr.Size * 8;
            if (bits == 64) {
                // 64-bit systems need MCC board
                daq = new DAQBoardMCC();
            }
            else if (bits == 32) {
                // 32-bit systems can use either board
                try {
                    daq = new DAQBoardMCC();
                    // see if MCC board is attached:
                    if ((daq != null) && (daq.NumDevices < 1)) {
                        daq = null;
                        daq = new DAQBoardIOTech();
                    }
                }
                catch {
                    daq = new DAQBoardIOTech();
                }
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
