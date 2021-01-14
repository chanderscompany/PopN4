using System;
using System.Timers;
using System.IO;

using DACarter.Utilities;

namespace DACarter.NOAA.Hardware {

    /// <summary>
    /// Class to control MiniCircuits USB Power Meter
    /// /// </summary>
    /// <remarks>
    /// Can make individual readings of power and temperature or
    ///     can start a timer to take readings at set intervals.
    /// Power (dBm) stored in property PowerReading.
    ///     PowerReading has OffsetDB added to the value from the meter.
    /// Temperature stored in property TempReading 
    ///     (units specified by property TempFormat ("F" or "C").
    /// Readings can be written to a text file.
    ///     Folder given by property OutputPath.
    ///     The 3-letter site code for file name is in property FileNamePrefix.
    ///     Property bool MakeHourFiles specifies hourly or daily files.
    /// </remarks>
    /// <example>
    /// Useage:
    ///     MCPowerMeter PowMeter = new MCPowerMeter();
    ///     PMStatus status = PowMeter.Status;
    ///                                         // set properties
    ///     PowMeter.FreqMHz = 2835.0;          // default value is 2835.0
    ///     PowMeter.OffsetDB = 100.0;          // default value is 0.0
    ///     PowMeter.TempFormat = "F";          // default value is "F"
    ///     PowMeter.OutputPath = "c:\\data";   // default is "";
    ///     PowMeter.FileNamePrefix = "ABC";    // default = "TST"
    ///     PowMeter.MakeHourFiles = false;     // default is false;
    ///                                         // take readings
    ///     PowMeter.ReadMeter();               // values available in properties PowerReading and TempReading
    ///     PowMeter.WriteToFile();
    ///     PowMeter.Close();                   // close device when finished
    /// </example>
    public class MCPowerMeter {

        private System.Timers.Timer _timer;

        private mcl_pm64.usb_pm _powMeter;

        private string _tempFormat;
        private double _freqMHz;
        private double _nullPower = -99.9;
        private double _nullTemp = -99.9;

        public enum PMStatus {
            Error = 0,
            OK = 1,
            AlreadyOpen = 2,
            NoSuchSN = 3
        }

        public PMStatus Status {
            get;
            set;
        }

        public string OutputPath {
            get;
            set;
        }

        public int WriteIntervalSec {
            get;
            set;
        }

        public double OffsetDB {
            get;
            set;
        }

        public double FreqMHz {
            get {return _freqMHz;}
            set {
                _freqMHz = value;
                if (_powMeter != null) {
                    _powMeter.Freq = _freqMHz;
                }
            }
        }

        public string TempFormat {
            get {return _tempFormat;}
            set {_tempFormat = value;}
        }

        public string FileNamePrefix {
            get;
            set;
        }

        public bool MakeHourFiles {
            get;
            set;
        }

        public double PowerReading {
            get;
            set;
        }

        public double TempReading {
            get;
            set;
        }

        // constructor
        public MCPowerMeter() {
            long mem0 = GC.GetTotalMemory(false) / 1000000;
            _powMeter = new mcl_pm64.usb_pm();
            long mem1 = GC.GetTotalMemory(false) / 1000000;    // at this point, 800 MB memory allocated
            Status = (PMStatus)_powMeter.Open_AnySensor();
            //long mem2 = GC.GetTotalMemory(false) / 1000000;    // sometime later, 800 MB memory freed
            OutputPath = "";
            WriteIntervalSec = 30;
            OffsetDB = 0.0;
            FreqMHz = 2835.0;
            TempFormat = "F";
            FileNamePrefix = "TST";
            MakeHourFiles = false;
            long mem3 = GC.GetTotalMemory(false) / 1000000;
        }

        /// <summary>
        /// 
        /// </summary>
        public void ReadMeter() {
            ReadPower();
            ReadTemp();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public double ReadPower() {
            PowerReading = _nullPower;
            if (Status == PMStatus.Error) {
                // see if device has been attached since last call
                Status = (PMStatus)_powMeter.Open_AnySensor();
                if (Status != PMStatus.Error) {
                    FreqMHz = _freqMHz;         // reset frequency on device
                }
            }
            if (Status != PMStatus.Error) {
                if (_powMeter != null) {
                    PowerReading = _powMeter.ReadPower();
                    PowerReading += OffsetDB;
                }
            }
            return PowerReading;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public double ReadTemp() {
            TempReading = _nullTemp;
            if (Status == PMStatus.OK || Status == PMStatus.AlreadyOpen) {
                if (_powMeter != null) {
                    TempReading = _powMeter.GetDeviceTemperature(ref _tempFormat);
                }
            }
            return TempReading;
        }

        /// <summary>
        /// 
        /// </summary>
        public void WriteToFile() {

            DateTime now = DateTime.Now;
            string fileName = "D" + FileNamePrefix;
            int yr2 = now.Year % 100;
            fileName += yr2.ToString("00") + now.DayOfYear.ToString("000");

            if (MakeHourFiles) {
                fileName += now.Hour.ToString("00");
            }

            fileName += "pwr.txt";
            string fullPath = Path.Combine(OutputPath, fileName);

            string dataString = "";
            dataString += now.Year.ToString() + ", " + now.DayOfYear.ToString("000") + ", " +
                            now.Hour.ToString("00") + ", " + now.Minute.ToString("00") + ", " + now.Second.ToString("00");
            dataString += ", " + TempReading.ToString("F1") + ", " + PowerReading.ToString("F2");

            TextFile.WriteLineToFile(fullPath, dataString);
        }

        public bool StartTimer() {

            if (_timer == null) {
                _timer = new System.Timers.Timer(WriteIntervalSec * 1000);
                _timer.Elapsed += OnTimedEvent;
                _timer.AutoReset = true;
            }
            else {
                _timer.Stop();
            }

            _timer.Interval = WriteIntervalSec * 1000;

            if (_powMeter == null) {
                _powMeter = new mcl_pm64.usb_pm();
            }
            else {
                _powMeter.Close_Sensor();
            }
            short Status = 0;
            Status = _powMeter.Open_AnySensor();
            if (Status == (int)PMStatus.Error) {
                return false;
            }

            OnTimedEvent(null, null);   // do one event right away
            _timer.Start();             // then start timer 

            return true;
        }

        public void StopTimer() {
            if (_timer != null) {
                _timer.Stop();
            }
        }

        public void Close() {
            StopTimer();
            if (_timer != null) {
                _timer.Close();
            }
            if (_powMeter != null) {
                _powMeter.Close_Sensor();
            }
        }

        /// <summary>
        /// called when the timer fires
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        private void OnTimedEvent(object source, ElapsedEventArgs args) {

            ReadMeter();
            WriteToFile();

        }

    }

}
