//-----------------------------------------------------------------------
// <copyright file="SpinAPI.cs" company="SpinCore Technologies, Inc">
//     Copyright (c) SpinCore Technologies, Inc.
// </copyright>
//-----------------------------------------------------------------------

/* Copyright (c) SpinCore Technologies, Inc.
 *
 * This software is provided 'as-is', without any express or implied warranty. 
 * In no event will the authors be held liable for any damages arising from the 
 * use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose, 
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 * claim that you wrote the original software. If you use this software in a
 * product, an acknowledgment in the product documentation would be appreciated
 * but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 * misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 */

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace SpinCore.SpinAPI
{
    /// <summary>
    /// Instruction or Opcode specify the type of instructions to be performed.
    /// </summary>
    public enum OpCode
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
    public enum ProgramTarget
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
    /// Timing unit used for specifying delay between instructions.
    /// </summary>
    public enum TimeUnit
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
    /// Status of the board accessible from the Status property
    /// </summary>
    /// <remarks>Not all boards support this, see the manual. </remarks>
    public enum EStatus_Bit
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

    public enum Device
    {
        DEVICE_SHAPE = 0x099000,
        DEVICE_DDS = 0x099001
    }

    /// <summary>
    /// RadioProcessor control word defines
    /// </summary>
    [Flags]
    public enum ControlWord
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

    public enum PhaseRegister
    {
        PHASE000 = 0,
        PHASE090 = 1,
        PHASE180 = 2,
        PHASE270 = 3
    }

    public static class FrequencyUnit
    {
        public const double MHz = 1.0;
        public const double Khz = 0.001;
        public const double Hz = 0.000001;
    }

    /// <summary>
    /// The latest version of spinapi can be downloaded form http://www.spincore.com/support
    /// For more information about our latest products, please visit our website at: http://www.spincore.com
    /// </summary>
    sealed public class SpinAPI
    {
        #region Declarations
        private int currentBoard = 0;
        Thread monitorBoardCountThread;
        /// <summary>
        /// maximum number of boards that can be supported
        /// </summary>
        public const int MAXIMUM_NUM_BOARDS = 32;
        #endregion

        #region Properties
        /// <summary>
        /// Returns spinAPI version information as string
        /// </summary>
        public string Version
        {
            get
            {
                return Marshal.PtrToStringAnsi(NativeMethods.pb_get_version());
            }
        }

        /// <summary>
        /// Gets or sets the current board starting from 0
        /// </summary>
        public int CurrentBoard
        {
            get
            {
                return currentBoard;
            }

            set
            {
                // Select the board
                int retval = NativeMethods.pb_select_board(value);
                if (retval < 0)
                {
                    throw new SpinAPIException(Error);
                }

                currentBoard = value;
            }
        }

        /// <summary>
        /// Gets the number of boards supported by SpinAPI
        /// </summary>
        public int BoardCount
        {
            get
            {
                // Get the board count
                int count = NativeMethods.pb_count_boards();
                if (count < 0)
                {
                    return 0;
                }

                // Return the count
                return count;
            }
        }

        /// <summary>
        /// Gets the current board status
        /// </summary>
        public int Status
        {
            get
            {
                lock (this)
                {
                    return NativeMethods.pb_read_status();
                }
            }
        }

        /// <summary>
        /// Gets the most recent error string
        /// </summary>
        public string Error
        {
            get
            {
                return Marshal.PtrToStringAnsi(NativeMethods.pb_get_error());
            }
        }
        #endregion

        #region Constructors
        public SpinAPI()
        {
            monitorBoardCountThread = new Thread(MonitorBoardCount);
            monitorBoardCountThread.IsBackground = true;
            monitorBoardCountThread.Start();
        }
        #endregion

        #region Events
        /// <summary>
        /// Event handler notifies if the number of boards is changed
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="BoardNumber">Return newly acquired device number</param>
        public delegate void BoardCountChangedHandler(object Sender, int BoardNumber);

        /// <summary>
        /// Board count changed event
        /// </summary>
        public event EventHandler BoardCountChanged;
        #endregion

        #region Functions
        /// <summary>
        /// If multiple boards from SpinCore Technologies are present in your system, this function allows you to select which board to talk to. 
        /// Once this function is called, all subsequent commands (such as pb_init(), pb_core_clock(), etc.) will be sent to the selected board. 
        /// You may change which board is selected at any time.
        /// </summary>
        /// <param name="boardNum">Specifies which board to select. Counting starts from 0.</param>
        public void SelectBoard(int boardNum)
        {
            lock (this)
            {
                if (NativeMethods.pb_select_board(boardNum) < 0)
                {
                    throw new SpinAPIException(Error);
                }
            }
        }

        /// <summary>
        /// Initializes the board. This must be called before any other functions are used which communicate with the board. 
        /// If you have multiple boards installed in your system, pb_select_board() may be called first to select which board to initialize.
        /// </summary>
        public int Init()
        {
            lock (this)
            {
                return NativeMethods.pb_init();
            }
        }

        /// <summary>
        /// Stops program execution and returns to the beginning of the program, waiting for a trigger.
        /// </summary>
        public void Reset()
        {
            lock (this)
            {
                NativeMethods.pb_reset();
            }
        }

        /// <summary>
        /// Get the firmware version of a specific board.
        /// </summary>
        /// <param name="boardNum">Board number to query</param>
        /// <returns>Returns the boards firmware id</returns>
        public int GetFirmwareID(int boardNum)
        {
            lock (this)
            {
                try
                {
                    // Set the board chosen in the library before we can get the firmware id
                    SelectBoard(boardNum);
                    Init();

                    // Return the firmware ID
                    return NativeMethods.pb_get_firmware_id();
                }
                finally
                {
                    // Select the board listed in this object so everything is consistent.
                    NativeMethods.pb_select_board(currentBoard);
                }
            }
        }

        /// <summary>
        /// This function tells the board to start programming one of the onboard devices. 
        /// For all the devices, the method of programming follows the following form:
        /// a call to pb_start_programming(), a call to one or more functions which transfer 
        /// the actual data, and a call to pb_stop_programming(). 
        /// Only one device can be programmed at a time.
        /// </summary>
        /// <param name="programmingType">Specifies which device to start programming</param>
        public void StartProgramming(ProgramTarget programmingType)
        {
            lock (this)
            {
                if (NativeMethods.pb_start_programming((int)programmingType) < 0)
                {
                    throw new SpinAPIException(Error);
                }
            }
        }

        /// <summary>
        /// Stop programming method must be called before start running any instructions
        /// </summary>
        public void StopProgramming()
        {
            lock (this)
            {
                if (NativeMethods.pb_stop_programming() < 0)
                {
                    throw new SpinAPIException(Error);
                }
            }
        }

        /// <summary>
        /// Stop running currently programmed instructions. 
        /// </summary>
        /// <remarks>
        /// Note that output bits may maintatin their last state. 
        /// </remarks>
        public void Stop()
        {
            lock (this)
            {
                if (NativeMethods.pb_stop() < 0)
                {
                    throw new SpinAPIException(Error);
                }
            }
        }

        /// <summary>
        /// Send a software trigger to the board. This will start execution of a pulse program. 
        /// It will also restart (trigger) a program which is currently paused due to a WAIT instruction. Triggering can also be accomplished through hardware, please see your board's manual for how to accomplish this.
        /// </summary>
        public void Start()
        {
            lock (this)
            {
                if (NativeMethods.pb_start() < 0)
                {
                    throw new SpinAPIException(Error);
                }
            }
        }

        /// <summary>
        /// Tell the library what clock frequency the board uses. 
        /// This should be called at the beginning of each program, right after you initialize the board with pb_init(). 
        /// Note that this does not actually set the clock frequency, it simply tells the driver what frequency the board is using, since this cannot (currently) be autodetected.
        /// </summary>
        /// <param name="clock_freq">clock_freq: Frequency of the clock in MHz.</param>
        public void SetClock(double clock_freq)
        {
            NativeMethods.pb_core_clock(clock_freq);
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
        /// <returns> Address of programmed instruction. </returns>
        public int PBInst(int flags, OpCode inst, int inst_data, double length, TimeUnit sec)
        {
            lock (this)
            {
                int ret;
                if ((ret = NativeMethods.pb_inst_pbonly(flags, (int)inst, inst_data, length * (double)sec)) < 0)
                {
                    throw new SpinAPIException(Error);
                }
                return ret;
            }
        }

        /// <summary>
        /// This is the instruction programming function for boards without a DDS. 
        /// </summary>
        /// <param name="flags">i/o output flag</param>
        /// <param name="inst">Instruction Type</param>
        /// <param name="inst_data">Instruction Data</param>
        /// <param name="length">Delay length</param>
        public void PBInstDirect(int flags, OpCode inst, int inst_data, int length)
        {
            lock (this)
            {
                unsafe
                {
                    if (NativeMethods.pb_inst_direct(&flags, (int)inst, inst_data, length) < 0)
                    {
                        throw new SpinAPIException(Error);
                    }
                }
            }
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
        public void SetDefaults()
        {
            lock (this)
            {
                if (NativeMethods.pb_set_defaults() < 0)
                {
                    throw new SpinAPIException(Error);
                }
            }
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
        /// <param name="reset">Set to true to reset the overflow counters</param>
        /// <returns>Overflow counts if not resetting</returns>
        public OverflowCounts Overflow(bool reset)
        {
            lock (this)
            {
                // Get the overflow data
                NativeMethods.PB_OVERFLOW_STRUCT of = new NativeMethods.PB_OVERFLOW_STRUCT();
                if (NativeMethods.pb_overflow(reset ? 1 : 0, of) < 0)
                {
                    throw new SpinAPIException(Error);
                }

                // On reset just return null
                if (reset)
                {
                    return null;
                }

                // Return the overflow counts
                return new OverflowCounts(of.adc, of.cic, of.fir, of.average);
            }
        }

        /// <summary>
        /// Get the current value of the scan count register, or reset the register to 0. 
        /// This function can be used to monitor the progress of an experiment if multiple scans are being performed.
        /// </summary>
        /// <param name="reset">If this parameter is set to 1, this function will reset the scan counter to 0. 
        /// If reset is 0, this function will return the current value of the scan counter</param>
        /// <returns>The number of scans performed since the last reset is returned when reset = false</returns>
        public int ScanCount(bool reset)
        {
            lock (this)
            {
                // Get the count
                int count = NativeMethods.pb_scan_count(reset ? 1 : 0);
                if (count < 0)
                {
                    throw new SpinAPIException(Error);
                }

                // Return the scan count
                return count;
            }
        }

        /// <summary>
        /// Retrieve the captured data from the board's memory. Data is returned as a signed 32 bit integer. Data can be accessed at any time, even while the data from a scan is being captured. However, this is not recommened since there is no way to tell what data is part of the current scan and what is part of the previous scan.
        /// pb_read_status() can be used to determine whether or not a scan is currently in progress.
        /// It takes approximately 160ms to transfer all 16k complex points.
        /// </summary>
        /// <param name="numPoints">Number of complex points to read from RAM</param>
        /// <param name="realData">Real data from RAM is stored into this array</param>
        /// <param name="imagData">Imag data from RAM is stored into this array</param>
        public void GetData(int numPoints, int[] realData, int[] imagData)
        {
            lock (this)
            {
                if (NativeMethods.pb_get_data(numPoints, realData, imagData) < 0)
                {
                    throw new SpinAPIException(Error);
                }
            }
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
        public void GetFFTData(
            int numPoints,
            int[] realData,
            int[] imaginaryData,
            double[] realFFT,
            double[] imaginaryFFT,
            double[] magnitudeFFT)
        {
            lock (this)
            {
                if (NativeMethods.pb_fft(numPoints, realData, imaginaryData, realFFT, imaginaryFFT, magnitudeFFT) < 0)
                {
                    throw new SpinAPIException(Error);
                }
            }
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
        public void DDSLoad(float[] data, Device device)
        {
            lock (this)
            {
                if (NativeMethods.pb_dds_load(data, (int)device) < 0)
                {
                    throw new SpinAPIException(Error);
                }
            }
        }

        /// <summary>
        /// Set the value of one of the amplitude registers.
        /// </summary>
        /// <param name="amplitude">Amplitude value. 0.0-1.0</param>
        /// <param name="address">Address of register to write to</param>
        public void SetAmplitude(float amplitude, int address)
        {
            lock (this)
            {
                if (NativeMethods.pb_set_amp(amplitude, address) < 0)
                {
                    throw new SpinAPIException(Error);
                }
            }
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
            lock (this)
            {
                int ret = NativeMethods.pb_setup_filters(spectralWidth, scanRepetition, (int)cmd);
                if (ret < 0)
                {
                    throw new SpinAPIException(Error);
                }

                return ret;
            }
        }

        /// <summary>
        /// Set the number of complex points to capture. This is typically set to the size of the onboard RAM, but a smaller value can be used if all points are not needed.
        /// </summary>
        /// <param name="numPoints"> The number of complex points to capture</param>
        public void SetNumberPoints(int numPoints)
        {
            lock (this)
            {
                if (NativeMethods.pb_set_num_points(numPoints) < 0)
                {
                    throw new SpinAPIException(Error);
                }
            }
        }

        /// <summary>
        /// Write the given frequency to a frequency register on a DDS enabled board. To do this, first call pb_start_programming(), and pass it FREQ_REGS. 
        /// The first call pb_set_freq() will then program frequency register 0, the second call will program frequency register 1, etc. 
        /// When you have programmed all the registers you intend to, call pb_stop_programming()
        /// </summary>
        /// <param name="frequency">The frequency in MHz to be programmed to the register.</param>
        public void SetFrequency(double frequency)
        {
            lock (this)
            {
                if (NativeMethods.pb_set_freq(frequency) < 0)
                {
                    throw new SpinAPIException(Error);
                }
            }
        }

        /// <summary>
        /// Write the given phase to a phase register on DDS enabled boards. 
        /// To do this, first call pb_start_programming(), and specify the appropriate bank of phase registers (such as TX_PHASE, RX_PHASE, etc) as the argument. 
        /// The first call pb_set_phase() will then program phase register 0, the second call will program phase register 1, etc. 
        /// When you have programmed all the registers you intend to, call pb_stop_programming() 
        /// The given phase value may be rounded to fit the precision of the board.
        /// </summary>
        /// <param name="phase">The phase in degrees to be programmed to the register.</param>
        public void SetPhase(double phase)
        {
            lock (this)
            {
                if (NativeMethods.pb_set_phase(phase) < 0)
                {
                    throw new SpinAPIException(Error);
                }
            }
        }

        /// <summary>
        /// Program an instruction of the pulse program.
        /// </summary>
        /// <param name="freq">Selects which frequency register to use.</param>
        /// <param name="cos_phase">Selects which phase register to use for the cos (real) channel.</param>
        /// <param name="sin_phase">Selects which phase register to use for the sin (imaginary) channel.</param>
        /// <param name="tx_phase">Selects which phase register to use for the TX channel.</param>
        /// <param name="bTX_enable">When this is true, the TX channel will be output on the Analog Out connector. When this is false, Analog Out channel will be turned off.</param>
        /// <param name="bPhase_reset">When this is true, the phase of all DDS channels will be reset to their time=0 phase. They will stay in this state until the value of this bit returns to 0.</param>
        /// <param name="bTrigger_scan">When this is true, a scan will be triggered. To start a second scan, this bit must be set to false and then back to true.</param>
        /// <param name="flags">Controls the state of the user available digital out pins.</param>
        /// <param name="inst">Which instruction to use.</param>
        /// <param name="inst_data">Some instructions require additional data. This allows that data to be specified.</param>
        /// <param name="duration">Time until the next instruction is executed in nanoseconds.</param>
        public void InstructionRadio(
            int freq,
            PhaseRegister cos_phase,
            PhaseRegister sin_phase,
            int tx_phase,
            bool bTX_enable,
            bool bPhase_reset,
            bool bTrigger_scan,
            int flags,
            OpCode inst,
            int inst_data,
            double duration)
        {
            lock (this)
            {
                if (NativeMethods.pb_inst_radio(
                    freq,
                    (int)cos_phase,
                    (int)sin_phase,
                    tx_phase,
                    bTX_enable ? 1 : 0,
                    bPhase_reset ? 1 : 0,
                    bTrigger_scan ? 1 : 0,
                    flags,
                    (int)inst,
                    inst_data,
                    duration) < 0)
                {
                    throw new SpinAPIException(Error);
                }
            }
        }

        /// <summary>
        /// Write an instruction that makes use of the pulse shape feature of some RadioProcessor boards.
        /// </summary>
        /// <param name="freq">Selects which frequency register to use.</param>
        /// <param name="cos_phase">Selects which phase register to use for the cos (real) channel.</param>
        /// <param name="sin_phase">Selects which phase register to use for the sin (imaginary) channel.</param>
        /// <param name="tx_phase">Selects which phase register to use for the TX channel.</param>
        /// <param name="bTX_enable">When this is true, the TX channel will be output on the Analog Out connector. When this is false, Analog Out channel will be turned off.</param>
        /// <param name="bPhase_reset">When this is true, the phase of all DDS channels will be reset to their time=0 phase. They will stay in this state until the value of this bit returns to 0.</param>
        /// <param name="bTrigger_scan">When this is true, a scan will be triggered. To start a second scan, this bit must be set to false and then back to true.</param>
        /// <param name="bUse_shape">Select whether or not to use shaped pulses.</param>
        /// <param name="amp">Select which amplitude register to use.</param>
        /// <param name="flags">Controls the state of the user available digital out pins.</param>
        /// <param name="inst">Which instruction to use.</param>
        /// <param name="inst_data">Some instructions require additional data. This allows that data to be specified.</param>
        /// <param name="duration">Time until the next instruction is executed in nanoseconds.</param>
        public void InstructionRadioShape(
            int freq,
            PhaseRegister cos_phase,
            PhaseRegister sin_phase,
            int tx_phase,
            bool bTX_enable,
            bool bPhase_reset,
            bool bTrigger_scan,
            bool bUse_shape,
            int amp,
            int flags,
            OpCode inst,
            int inst_data,
            double duration)
        {
            lock (this)
            {
                if (NativeMethods.pb_inst_radio_shape(
                    freq,
                    (int)cos_phase,
                    (int)sin_phase,
                    tx_phase,
                    bTX_enable ? 1 : 0,
                    bPhase_reset ? 1 : 0,
                    bTrigger_scan ? 1 : 0,
                    bUse_shape ? 1 : 0,
                    amp,
                    flags,
                    (int)inst,
                    inst_data,
                    duration) < 0)
                {
                    throw new SpinAPIException(Error);
                }
            }
        }

        /// <summary>
        /// Write an instruction that makes use of the pulse shape feature of some RadioProcessor boards.
        /// </summary>
        /// <param name="freq">Selects which frequency register to use.</param>
        /// <param name="cos_phase">Selects which phase register to use for the cos (real) channel.</param>
        /// <param name="sin_phase">Selects which phase register to use for the sin (imaginary) channel.</param>
        /// <param name="tx_phase">Selects which phase register to use for the TX channel.</param>
        /// <param name="bTX_enable">When this is true, the TX channel will be output on the Analog Out connector. When this is false, Analog Out channel will be turned off.</param>
        /// <param name="bPhase_reset">When this is true, the phase of all DDS channels will be reset to their time=0 phase. They will stay in this state until the value of this bit returns to 0.</param>
        /// <param name="bTrigger_scan">When this is true, a scan will be triggered. To start a second scan, this bit must be set to false and then back to true.</param>
        /// <param name="bUse_shape">Select whether or not to use shaped pulses.</param>
        /// <param name="amp">Select which amplitude register to use.</param>
        /// <param name="bReal_add_sub"></param>
        /// <param name="bImag_add_sub"></param>
        /// <param name="bChannel_swap"></param>
        /// <param name="flags">Controls the state of the user available digital out pins.</param>
        /// <param name="inst">Which instruction to use.</param>
        /// <param name="inst_data">Some instructions require additional data. This allows that data to be specified.</param>
        /// <param name="duration">Time until the next instruction is executed in nanoseconds.</param>
        public void InstructionRadioShapeCyclops(
            int freq,
            PhaseRegister cos_phase,
            PhaseRegister sin_phase,
            int tx_phase,
            bool bTX_enable,
            bool bPhase_reset,
            bool bTrigger_scan,
            bool bUse_shape,
            int amp,
            bool bReal_add_sub,
            bool bImag_add_sub,
            bool bChannel_swap,
            int flags,
            OpCode inst,
            int inst_data,
            double duration)
        {
            lock (this)
            {
                if (NativeMethods.pb_inst_radio_shape_cyclops(
                    freq,
                    (int)cos_phase,
                    (int)sin_phase,
                    tx_phase,
                    bTX_enable ? 1 : 0,
                    bPhase_reset ? 1 : 0,
                    bTrigger_scan ? 1 : 0,
                    bUse_shape ? 1 : 0,
                    amp,
                    bReal_add_sub ? 1 : 0,
                    bImag_add_sub ? 1 : 0,
                    bChannel_swap ? 1 : 0,
                    flags,
                    (int)inst,
                    inst_data,
                    duration) < 0)
                {
                    throw new SpinAPIException(Error);
                }
            }
        }

        /// <summary>
        /// Board count monitoring thread function
        /// </summary>
        private void MonitorBoardCount()
        {
            // Get a board count
            int oldBoardCount = BoardCount;

            // Loop forever
            while (true)
            {
                // Get the board count
                int currentBoardCount = BoardCount;

                // Report board count change as event
                if (currentBoardCount != oldBoardCount)
                {
                    oldBoardCount = currentBoardCount;

                    // Fire the event
                    if (BoardCountChanged != null)
                    {
                        BoardCountChanged(this, EventArgs.Empty);
                    }
                }

                // Delay for 100ms
                Thread.Sleep(100);
            }
        }
        #endregion
    }
}
