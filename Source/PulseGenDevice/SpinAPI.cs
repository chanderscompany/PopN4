/*
    Copyright 2009, SpinCore Technolgies, Inc.

    This file is part of $safeprojectname$.

    $safeprojectname$ is free software: you can redistribute it
    and/or modify it under the terms of the GNU General Public License as
    published by the Free Software Foundation, either version 3 of the License,
    or (at your option) any later version.

    $safeprojectname$ is distributed in the hope that it will be
    useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with $safeprojectname$.  If not, see
    <http://www.gnu.org/licenses/>.
*/

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace SpinAPI_NET
{
    /// <summary>
    /// Instruction or Opcode specify the type of instructions to be performed.
    /// </summary>
    public enum OpCode : int
    {
        /// <summary>
        /// Continue to next instruction
        /// </summary>
        CONTINUE = 0,
        /// <summary>
        /// Stop excution
        /// </summary>
        STOP = 1,
        /// <summary>
        /// Beginning of a loop and repeat number of times specified by the instruction data.
        /// </summary>
        LOOP = 2,
        /// <summary>
        /// End of a loop
        /// </summary>
        END_LOOP = 3,
        /// <summary>
        /// Jump to a sub routine
        /// </summary>
        JSR = 4,
        /// <summary>
        /// Return from a sub routine
        /// </summary>
        RTS = 5,
        /// <summary>
        /// Branch
        /// </summary>
        BRANCH = 6,
        /// <summary>
        /// Long delay
        /// </summary>
        LONG_DELAY = 7,
        /// <summary>
        /// Wait for trigger
        /// </summary>
        WAIT = 8,
        /// <summary>
        /// 
        /// </summary>
        RTI = 9
    }
    /// <summary>
    /// Specifies which device to start programming. Valid devices are:
    /// </summary>
    public enum ProgramTarget : int
    {
        /// <summary>
        /// The pulse program will be programmed using one of the pb_inst* instructions.
        /// </summary>
        PULSE_PROGRAM = 0,
        /// <summary>
        /// The frequency registers will be programmed using the pb_set_freq() function. (DDS and RadioProcessor boards only)
        /// </summary>
        FREQ_REGS = 1,
        /// <summary>
        /// The phase registers for the TX channel will be programmed using pb_set_phase() (DDS and RadioProcessor boards only)
        /// </summary>
        TX_PHASE_REGS = 2,
        /// <summary>
        /// The phase registers for the RX channel will be programmed using pb_set_phase() (DDS enabled boards only)
        /// </summary>
        RX_PHASE_REGS = 3,
        /// <summary>
        /// The phase registers for the cos (real) channel (RadioProcessor boards only)
        /// </summary>
        COS_PHASE_REGS = 4,
        /// <summary>
        /// The phase registers for the sine (imaginary) channel (RadioProcessor boards only)
        /// </summary>
        SIN_PHASE_REGS = 5
    }
    /// <summary>
    /// Scan counter reset
    /// </summary>
    public enum EScanCounterReset : int
    {
        /// <summary>
        /// Do not reset the counter value
        /// </summary>
        NO_RESET = 0,
        /// <summary>
        /// Reset the scan counter value to 0
        /// </summary>
        RESET = 1
    }
    /// <summary>
    /// Timing unit used for specifying delay between instructions.
    /// </summary>
    public enum TimeUnit : int
    {
        /// <summary>
        /// nanoseconds
        /// </summary>
        ns = 1,
        /// <summary>
        /// microseconds
        /// </summary>
        us = 1000,
        /// <summary>
        /// miliseconds
        /// </summary>
        ms = 1000000
    }
    /// <summary>
    /// Status of the board when ReadStatus() is called.
    /// </summary>
    /// <remarks>Not all boards support this, see the manual. </remarks>
    public enum EStatus_Bit : int
    {
        Stopped = 0,
        Reset = 1,
        Running = 2,
        Waiting = 3,
        Scanning = 4,
        status1 = 5,
        status2 = 6,
        status3 = 7
    }
    public enum Device : int
    {
        DEVICE_SHAPE = 0x099000,
        DEVICE_DDS = 0x099001
    }
    /// <summary>
    /// RadioProcessor control word defines
    /// </summary>
    public enum ControlWord : int
    {
        TRIGGER = 0x0001,
        PCI_READ = 0x0002,
        BYPASS_AVERAGE = 0x0004,
        NARROW_BW = 0x0008,
        FORCE_AVG = 0x0010,
        BNC0_CLK = 0x0020,
        DO_ZERO = 0x0040,
        BYPASS_CIC = 0x0080,
        BYPASS_FIR = 0x0100,
        BYPASS_MULT = 0x0200,
        SELECT_AUX_DDS = 0x0400,
        DDS_DIRECT = 0x0800,
        SELECT_INTERNAL_DDS = 0x1000,
        DAC_FREEDTHROUGH = 0x2000,
        OVERFLOW_RESET = 0x4000,
        RAM_DIRECT = 0x8000 | ControlWord.BYPASS_CIC | ControlWord.BYPASS_MULT
    }
    public enum PhaseRegister : int
    {
        PHASE000 = 0,
        PHASE090 = 1,
        PHASE180 = 2,
        PHASE270 = 3
    }
    public struct FrequencyUnit
    {
        public const double MHz = 1.0;
        public const double Khz = 0.001;
        public const double Hz = 0.000001;
    }

    public class SpinAPIException : Exception
    {
    }

    /// <summary>
    /// The latest version of spinapi can be downloaded form http://www.spincore.com/support
    /// For more information about our latest products, please visit our website at: http://www.spincore.com
    /// </summary>
    sealed public class SpinAPI
    {
        #region DLLImports
        /// <summary>
        /// Import SpinAPI functions from spinapi.dll.
        /// </summary>
        /// <returns></returns>
        [DllImport("spinapi.dll")]
        private static extern int pb_count_boards();
        [DllImport("spinapi.dll")]
        private static extern string pb_get_version();
        [DllImport("spinapi.dll")]
        private static extern int pb_init();
        [DllImport("spinapi.dll")]
        private static extern int pb_select_board(int BoardNumber);
        [DllImport("spinapi.dll")]
        private static extern int pb_get_firmware_id();
        [DllImport("spinapi.dll")]
        private static extern int pb_close();
        [DllImport("spinapi.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void pb_core_clock(double ClockFrequency);
        [DllImport("spinapi.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pb_start_programming(int Device);
        [DllImport("spinapi.dll")]
        private static extern int pb_stop_programming();
        [DllImport("spinapi.dll")]
        private static extern int pb_reset();
        [DllImport("spinapi.dll")]
        private static extern void pb_start();
        [DllImport("spinapi.dll")]
        private static extern int pb_read_status();
        [DllImport("spinapi.dll")]
        private static extern int pb_stop();
        [DllImport("spinapi.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pb_inst_pbonly(int Flags, int OpCode, int Data, double Duration);
        [DllImport("spinapi.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pb_inst(int Flags, int OpCode, int Data, double Duration);
        [DllImport("spinapi.dll", CallingConvention = CallingConvention.Cdecl)]
        unsafe private static extern int pb_inst_direct(int* Flags, int OpCode, int Data, int Duration);
        [DllImport("spinapi.dll")]
        private static extern int pb_set_defaults();
        [DllImport("spinapi.dll")]
        private static extern int pb_zero_ram();
        [DllImport("spinapi.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pb_overflow(int reset, int of);
        [DllImport("spinapi.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pb_scan_count(int reset);
        [DllImport("spinapi.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pb_get_data(int num_Points,
             [In, Out] int[] realData,
            [In, Out] int[] imagData);
        [DllImport("spinapi.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pb_dds_load(
            [In] float[] data, int device);
        [DllImport("spinapi.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pb_set_amp(float amp, int addr);
        [DllImport("spinapi.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pb_setup_filters(double spectral_width, int scan_repetitions, int cmd);
        [DllImport("spinapi.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pb_set_num_points(int num_points);
        [DllImport("spinapi.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pb_set_freq(double freq);
        [DllImport("spinapi.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pb_set_phase(double phase);
        [DllImport("spinapi.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pb_inst_radio_shape(int freq, int cos_phase, int sin_phase, int tx_phase, int tx_enable,
            int phase_reset, int trigger_scan, int use_shape, int amp, int flags, int inst, int inst_data, double length);
        [DllImport("spinapi.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pb_fft(int numberPoints,
            [In] int[] realData,
            [In] int[] imagData,
            [Out] double[] realFFT,
            [Out] double[] imagFFT,
            [Out] double[] magFFT);

        #endregion

        #region Declarations
        private int _CurrentBoard = 0;
        OpCode _InstructionType;
        ProgramTarget _ProgrammingType;
        TimeUnit _TimingUnit;
        private double _ClockFrequency = 100.0;
        Thread MonitorBoardCountThread;
        //Thread statusThread;
        //bool bStatusThreadRunning = false;
        private int _Status = 0;
        /// <summary>
        /// maximum number of boards that can be supported
        /// </summary>
        public const int MAXIMUM_NUM_BOARDS = 32;
        #endregion

        #region Properties

        /// <summary>
        /// Select the board currently using
        /// starting from 0
        /// </summary>
        public int CurrentBoard
        {
            get
            {
                return _CurrentBoard;
            }
            set
            {
                int retval = pb_select_board(value);
                if (retval < 0)
                    throw new ArgumentException();

                _CurrentBoard = value;
            }
        }
        /// <summary>
        /// Returns number of boards supported by SpinAPI as unsigned integer
        /// </summary>
        public int BoardCount
        {
            get
            {
                int BoardCount = pb_count_boards();

                if (BoardCount < 0)
                    throw new SpinAPIException();

                return BoardCount;
            }
        }
        /// <summary>
        /// Returns spinAPI version information as string
        /// </summary>
        public string Version
        {
            get
            {
                return pb_get_version();
            }
        }
        /// <summary>
        /// Return current board status as int
        /// </summary>
        public int Status
        {
            get
            {
                return _Status;
            }
        }
        //public bool StatusMonitor
        //{
        //    set
        //    {
        //        if (value == true)
        //        {
        //            if (!bStatusThreadRunning)   //If status monitor thread is not running
        //            {
        //                bStatusThreadRunning = true;
        //                statusThread = new Thread(DeviceStatusMonitor);
        //                statusThread.Start();
        //            }
        //        }
        //        else
        //        {
        //            bStatusThreadRunning = false;
        //        }
        //    }
        //    get
        //    {
        //        return bStatusThreadRunning;
        //    }
        //}
        /// <summary>
        /// Specifies timing unit
        /// </summary>
        public TimeUnit TimingUnit
        {
            get { return _TimingUnit; }
            set { _TimingUnit = value; }
        }
        public OpCode InstructionType
        {
            get { return _InstructionType; }
            set { _InstructionType = value; }
        }
        public ProgramTarget ProgrammingType
        {
            get { return _ProgrammingType; }
            set { _ProgrammingType = value; }
        }
        public double ClockFrequency
        {
            get { return _ClockFrequency; }
            set { _ClockFrequency = value; }
        }
        #endregion

        #region Constructors
        public SpinAPI()
        {
            MonitorBoardCountThread = new Thread(MonitorBoardCount);
            MonitorBoardCountThread.IsBackground = true;
            MonitorBoardCountThread.Start();
        }
        #endregion

        #region Events
        /// <summary>
        /// Event handler notifies if the number of boards is changed
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="BoardNumber">Return newly acquired device number</param>
        public delegate void BoardCountChangedHandler(object Sender, int BoardNumber);
        public event EventHandler BoardCountChanged;
        /// <summary>
        /// Device status event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="status">return status as integer</param>
        public delegate void DeviceStatusChangedDelegate(object sender, int status);
        public event DeviceStatusChangedDelegate DeviceStatusChangedEvent;
        #endregion

        #region Functions
        /// <summary>
        /// Get the firmware version of the board specified by (uint)boardNum.
        /// This function will change the current board number in the API and not change it back.
        /// </summary>
        /// <param name="boardNum"></param>
        /// <returns>Returns the firmware id ad uint</returns>
        public int GetFirmwareID(int boardNum)
        {
            // set the board chosen in the library before we can get the firmware id
            pb_select_board(boardNum);
            pb_init();

            int ret = pb_get_firmware_id();

            // select the board listed in this object so everything is consistent.
            pb_select_board(_CurrentBoard);

            return ret;
        }
        /// <summary>
        /// If multiple boards from SpinCore Technologies are present in your system, this function allows you to select which board to talk to. 
        /// Once this function is called, all subsequent commands (such as pb_init(), pb_core_clock(), etc.) will be sent to the selected board. 
        /// You may change which board is selected at any time.
        /// </summary>
        /// <param name="boardNum">Specifies which board to select. Counting starts from 0</param>
        /// <returns>Negative number returned on failure. 0 is returned on success</returns>
        public int SelectBoard(int boardNum)
        {

            return pb_select_board(boardNum);

        }
        /// <summary>
        /// Initializes the board. This must be called before any other functions are used which communicate with the board. 
        /// If you have multiple boards installed in your system, pb_select_board() may be called first to select which board to initialize.
        /// </summary>
        /// <returns></returns>
        public int Init()
        {

            int ret = pb_init();

            return ret;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int Reset()
        {
            return pb_reset();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int Close() {
            return pb_close();
        }
        /// <summary>
        /// This function tells the board to start programming one of the onboard devices. 
        /// For all the devices, the method of programming follows the following form:
        /// a call to pb_start_programming(), a call to one or more functions which transfer 
        /// the actual data, and a call to pb_stop_programming(). 
        /// Only one device can be programmed at a time.
        /// </summary>
        /// <param name="programmingType">Specifies which device to start programming</param>
        /// <returns></returns>
        public bool StartProgramming(ProgramTarget programmingType)
        {
            return (pb_start_programming((int)programmingType) >= 0);
        }
        /// <summary>
        /// Stop programming method must be called before start running any instructions
        /// </summary>
        /// <returns>Return FALSE if failed to stop programming.</returns>
        public bool StopProgramming()
        {
            return (pb_stop_programming() >= 0);
        }
        /// <summary>
        /// Stop running currently programmed instructions. 
        /// </summary>
        /// <remarks>
        /// Note that output bits may maintatin their last state.
        /// MODIFIED DAC 20121114 to reset after stop to clear output lines.
        ///     DAC 20121128: actually reset does not clear outputs,
        ///     but does reset program to first instruction, so next start() will work.
        /// </remarks>
        /// <returns>Return FALSE if failed to stop running.</returns>
        public bool Stop()
        {
            int rr = pb_stop();
            pb_reset();
            return (rr >= 0);

        }
        /// <summary>
        /// Send a software trigger to the board. This will start execution of a pulse program. 
        /// It will also restart (trigger) a program which is currently paused due to a WAIT instruction. Triggering can also be accomplished through hardware, please see your board's manual for how to accomplish this.
        /// </summary>
        public void Start()
        {
            pb_start();

        }
        /// <summary>
        /// Tell the library what clock frequency the board uses. 
        /// This should be called at the beginning of each program, right after you initialize the board with pb_init(). 
        /// Note that this does not actually set the clock frequency, it simply tells the driver what frequency the board is using, since this cannot (currently) be autodetected.
        /// </summary>
        /// <param name="clock_freq">clock_freq: Frequency of the clock in MHz.</param>
        public void SetClock(double clock_freq)
        {

            pb_core_clock(clock_freq);

        }
        /// <summary>
        /// This is the instruction programming function for boards without a DDS. 
        /// (for example PulseBlaster and PulseBlasterESR boards). 
        /// Syntax is identical to that of pb_inst_tworf(), 
        /// except that the parameters pertaining to the analog outputs are not used. 
        /// </summary>
        /// <param name="flags">i/o output flag</param>
        /// <param name="inst">Instruction Type</param>
        /// <param name="inst_data">Instruction Data</param>
        /// <param name="length">Delay length</param>
        /// <param name="sec">timing unit (ms, us or ns)</param>
        /// <returns></returns>
        public int PBInst(int flags, OpCode inst, int inst_data, double length, TimeUnit sec)
        {

            int ret = 0;
            if (MonitorBoardCountThread.IsAlive)
            {
                MonitorBoardCountThread.Suspend();
                ret = pb_inst_pbonly(flags, (int)inst, inst_data, length * (double)sec);
                MonitorBoardCountThread.Resume();
            }

            return ret;
        }
        public int PBInstDirect(int flags, OpCode inst, int inst_data, int length)
        {
            int retval = 0;
            if (MonitorBoardCountThread.IsAlive)
            {
                MonitorBoardCountThread.Suspend();
                unsafe
                {
                    retval = pb_inst_direct(&flags, (int)inst, inst_data, length);
                }
                MonitorBoardCountThread.Resume();
            }
            //unsafe private static extern int pb_inst_direct(int* pflags, int inst, int inst_data_direct, int length);
            return 0;
        }
        /// <summary>
        /// Many parameters to funciton in the API are given as full precision double values, such as the length of an instruction, or the phase to be programmed to a phase register. Since the hardware does not have the full precision of a double value, the paremeters are rounded to match the internal precision. This function allows you to see what to what value the parameters were rounded.
        /// </summary>
        /// <returns></returns>
        public int ReadStatus()
        {

            return pb_read_status();

        }
        /// <summary>
        /// This function sets the RadioProcessor to its default state. 
        /// It has no effect on any other SpinCore product. 
        /// This function should generally be called after pb_init() to make sure the RadioProcessor is in a usable state. 
        /// It is REQUIRED that this be called at least once after the board is powered on. 
        /// <remarks>However, there are a few circumstances when you would not call this function. 
        /// In the case where you had one program that configured the RadioProcessor, and another seperate program which simply called pb_start() to start the experiment, 
        /// you would NOT call pb_set_defaults() in the second program because this would overwrite the configuration set by the first program.
        /// </remarks>
        /// </summary>
        /// <returns>A negative number is returned on failure, and spinerr is set to a description of the error. 0 is returned on success.</returns>
        public int SetDefaults()
        {

            int ret = pb_set_defaults();

            return ret;
        }
        /// <summary>
        /// clear RAM to all zeros
        /// </summary>
        /// <returns></returns>
        public int ZeroRam()
        {

            int ret = pb_zero_ram();

            return ret;
        }
        /// <summary>
        /// Retrieve the contents of the overflow registers. 
        /// This can be used to find out if the ADC is being driven with to large of a signal. 
        /// In addition, the RadioProcessor must round data values at certain points during the processing of the signal. 
        /// By default, this rounding is done in such a way that overflows cannot occur. 
        /// However, if you change the rounding procedure, this function will allow you to determine if overflows have occurred. 
        /// Each overflow register counts the number of overflows up to 65535. 
        /// If more overflows than this occur, the register will remain at 65535. 
        /// The overflow registers can reset by setting the reset argument of this function to 1. 
        /// </summary>
        /// <param name="reset">Set to 1 to reset the overflow counters</param>
        /// <param name="of">Pointer to a PB_OVERFLOW_STRUCT which will hold the values of the overflow counter. 
        /// This can be a NULL pointer if you are using this function to reset </param>
        /// <returns></returns>
        public int Overflow(int reset, int of)
        {

            return pb_overflow(reset, of);

        }
        /// <summary>
        /// Get the current value of the scan count register, or reset the register to 0. 
        /// This function can be used to monitor the progress of an experiment if multiple scans are being performed.
        /// </summary>
        /// <param name="reset">If this parameter is set to 1, this function will reset the scan counter to 0. 
        /// If reset is 0, this function will return the current value of the scan counter</param>
        /// <returns>The number of scans performed since the last reset is returned when reset=0. 
        /// -1 is returned on error</returns>
        public int ScanCount(EScanCounterReset reset)
        {

            return pb_scan_count((int)reset);

        }
        /// <summary>
        /// Retrieve the captured data from the board's memory. Data is returned as a signed 32 bit integer. Data can be accessed at any time, even while the data from a scan is being captured. However, this is not recommened since there is no way to tell what data is part of the current scan and what is part of the previous scan.
        /// pb_read_status() can be used to determine whether or not a scan is currently in progress.
        /// It takes approximately 160ms to transfer all 16k complex points.
        /// </summary>
        /// <param name="num_Points">Number of complex points to read from RAM</param>
        /// <param name="RealData">Real data from RAM is stored into this array</param>
        /// <param name="ImagData">Imag data from RAM is stored into this array</param>
        /// <returns>A negative number is returned on failure, and spinerr is set to a description of the error. 0 is returned on success.</returns>
        public int GetData(int num_Points, ref int[] RealData, ref int[] ImaginaryData)
        {

            int ret = pb_get_data(num_Points, RealData, ImaginaryData);

            return ret;
        }
        /// <summary>
        /// Calculates the Fourier transform of a given set of real and imaginary points
        /// </summary>
        /// <param name="numPoints">Number of points for FFT.</param>
        /// <param name="realData">Array of real points for FFT calculation</param>
        /// <param name="imaginaryData">Array of imaginary points for FFT calculation</param>
        /// <param name="realFFT">Real part of FFT output</param>
        /// <param name="imaginaryFFT">Imaginary part of FFT output</param>
        /// <param name="magnitudeFFT">Magnitude of the FFT output</param>
        /// <returns>Returns zero.</returns>
        public int GetFFTData(int numPoints, int[] realData, int[] imaginaryData,
                                ref double[] realFFT, ref double[] imaginaryFFT,
                                ref double[] magnitudeFFT)
        {

            int ret = pb_fft(numPoints, realData, imaginaryData, realFFT, imaginaryFFT, magnitudeFFT);

            return ret;
        }

        /// <summary>
        /// Load the DDS with the given waveform. There are two different waveforms that can be loaded.
        /// <list type="bullet">
        ///     <item>
        ///         <term>DEVICE_DDS</term> 
        ///             <description>
        ///             This is for the DDS module itself. By default, it is loaded with a sine wave, and if you don't wish to change that or use shaped pulses, you do not need to use this function. Otherwise this waveform can be loaded with any arbitrary waveform that will be used instead of a sine wave.
        ///             </description>
        ///     </item>
        ///     <item>
        ///         <term>DEVICE_SHAPE</term> 
        ///             <description>
        ///             This waveform is for the shape function. This controls the shape used, if you enable the use_shape parameters of pb_inst_radio_shape(). For example, if you wish to use soft pulses, this could be loaded with the values for the sinc function.
        ///             </description>
        ///     </item>
        /// </list>
        /// </summary>
        /// <param name="data">This should be an array of 1024 floats that represent a single period of the waveform you want to have loaded. The range for each data point is from -1.0 to 1.0</param>
        /// <param name="device">Device you wish to program the waveform to. Can be DEVICE_SHAPE or DEVICE_DDS</param>
        /// <returns>A negative number is returned on failure, and spinerr is set to a description of the error. 0 is returned on success.</returns>
        public int DDSLoad(float[] data, Device device)
        {

            int ret = pb_dds_load(data, (int)device);

            return ret;
        }
        /// <summary>
        /// Set the value of one of the amplitude registers.
        /// </summary>
        /// <param name="amplitude">Amplitude value. 0.0-1.0</param>
        /// <param name="address">Address of register to write to</param>
        /// <returns>A negative number is returned on failure, and spinerr is set to a description of the error. 0 is returned on success.</returns>
        public int SetAmplitude(float amplitude, int address)
        {

            int ret = pb_set_amp(amplitude, address);

            return ret;
        }
        /// <summary>
        /// Program the onboard filters to capture data and reduce it to a baseband signal with the given spectral width. 
        /// This function will automatically set the filter parameters and decimation factors. 
        /// For greater control over the filtering process, the filters can be specified manually by using the pb_setup_cic() and pb_setup_fir() functions.
        /// </summary>
        /// <param name="spectralWidth">Desired spectral width (in MHz) of the stored baseband data. 
        /// The decimation factor used is the return value of this function, so that can be checked to determine the exact spectral width used. If the FIR filter is used, this value must be the ADC clock divided by a multiple of 8. 
        /// The value will be rounded appropriately if this condition is not met.</param>
        /// <param name="scanRepetition">Number of scans intended to be performed. This number is used only for internal rounding purposes. 
        /// The actual number of scans performed is determined entirely by how many times the scan_trigger control line is enabled in the pulse program. However, if more scans are performed than specified here, there is a chance that the values stored in RAM will overflow.</param>
        /// <param name="cmd">This paramater provides additional options for this function. Multiple options can be sent by ORing them together. If you do not wish to invoke any of the available options, use the number zero for this field. Valid options are:
        /// <list>
        ///     <item >BYPASS_FIR - Incoming data will not pass through the FIR filter. This eliminates the need to decimate by a multiple of 8. This is useful to obtain large spetral widths, or in circumstances where the FIR is deemed unecessary. Please see the RadioProcessor manual for more information about this option.</item>
        ///     <item >NARROW_BW - Configure the CIC filter so that it will have a narrower bandwidth (the CIC filter will be configured to have three stages rather than the default of one). Please see your board's product manual for more specific information on this feature.</item>
        /// </list>
        /// </param>
        /// <returns></returns>
        public int SetupFilters(double spectralWidth, int scanRepetition, ControlWord cmd)
        {

            int ret = pb_setup_filters(spectralWidth, scanRepetition, (int)cmd);

            return ret;
        }
        /// <summary>
        /// Set the number of complex points to capture. This is typically set to the size of the onboard RAM, but a smaller value can be used if all points are not needed.
        /// </summary>
        /// <param name="numPoints"> The number of complex points to capture</param>
        /// <returns>A negative number is returned on failure, and spinerr is set to a description of the error. 0 is returned on success.</returns>
        public int SetNumberPoints(int numPoints)
        {

            int ret = pb_set_num_points(numPoints);

            return ret;
        }
        /// <summary>
        /// Write the given frequency to a frequency register on a DDS enabled board. To do this, first call pb_start_programming(), and pass it FREQ_REGS. 
        /// The first call pb_set_freq() will then program frequency register 0, the second call will program frequency register 1, etc. 
        /// When you have programmed all the registers you intend to, call pb_stop_programming()
        /// </summary>
        /// <param name="frequency">The frequency in MHz to be programmed to the register.</param>
        /// <returns>A negative number is returned on failure, and spinerr is set to a description of the error. 0 is returned on success.</returns>
        public int SetFrequency(double frequency)
        {

            int ret = pb_set_freq(frequency);

            return ret;
        }
        /// <summary>
        /// Write the given phase to a phase register on DDS enabled boards. 
        /// To do this, first call pb_start_programming(), and specify the appropriate bank of phase registers (such as TX_PHASE, RX_PHASE, etc) as the argument. 
        /// The first call pb_set_phase() will then program phase register 0, the second call will program phase register 1, etc. 
        /// When you have programmed all the registers you intend to, call pb_stop_programming() 
        /// The given phase value may be rounded to fit the precision of the board.
        /// </summary>
        /// <param name="phase">The phase in degrees to be programmed to the register.</param>
        /// <returns>A negative number is returned on failure, and spinerr is set to a description of the error. 0 is returned on success.</returns>
        public int SetPhase(double phase)
        {

            int ret = pb_set_phase(phase);

            return ret;
        }
        public int InstructionRadioShape(int freq,
                                        PhaseRegister cos_phase,
                                        PhaseRegister sin_phase,
                                        int tx_phase,
                                        bool bTX_enable,
                                        bool bPhase_reset,
                                        bool bTrigger_scan,
                                        bool bUse_shape,
                                        int iAmp,
                                        int iFlags,
                                        OpCode inst,
                                        int inst_data,
                                        double dlength)
        {

            int ret = pb_inst_radio_shape(freq,
                                        (int)cos_phase,
                                        (int)sin_phase,
                                        tx_phase,
                                        bTX_enable ? 1 : 0,
                                        bPhase_reset ? 1 : 0,
                                        bTrigger_scan ? 1 : 0,
                                        bUse_shape ? 1 : 0,
                                        iAmp,
                                        iFlags,
                                        (int)inst,
                                        inst_data,
                                        dlength);

            return ret;
        }

        private void MonitorBoardCount()
        {
            int CurrentBoardCount = pb_count_boards();
            int OldBoardCount = CurrentBoardCount;

            while (true)
            {
                CurrentBoardCount = pb_count_boards();

                if (CurrentBoardCount < 0)
                    throw new SpinAPIException();

                if (CurrentBoardCount != OldBoardCount)
                {
                    OldBoardCount = CurrentBoardCount;
                    if (BoardCountChanged != null) {
                        BoardCountChanged(this, EventArgs.Empty);
                    }
                }

                Thread.Sleep(100);
            }
        }
        #endregion
    }
}
