//-----------------------------------------------------------------------
// <copyright file="NativeMethods.cs" company="SpinCore Technologies, Inc">
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

namespace SpinCore.SpinAPI
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// SpinAPI Native Methods
    /// </summary>
    internal static class NativeMethods
    {
        #if WIN64
            private const string dllname = "spinapi64.dll";
        #else
            private const string dllname = "spinapi.dll";
        #endif
        /// <summary>
        /// Overflow structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct PB_OVERFLOW_STRUCT
        {
            /// <summary>
            /// Number of overflows that occur when sampling data at the ADC.
            /// </summary>
            public int adc;

            /// <summary>
            /// Number of overflows that occur after the CIC filter.
            /// </summary>
            public int cic;

            /// <summary>
            /// Number of overflows that occur after the FIR filter.
            /// </summary>
            public int fir;

            /// <summary>
            /// Number of overflows that occur during the averaging process.
            /// </summary>
            public int average;
        }

        /// <summary>
        /// Return the number of SpinCore boards present in your system.
        /// </summary>
        /// <returns>The number of boards present is returned. -1 is returned on error.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_count_boards();

        /// <summary>
        /// Get the version of this library.
        /// </summary>
        /// <returns>A string indicating the version of this library is returned.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pb_get_version();

        /// <summary>
        /// Return the most recent error string.
        /// </summary>
        /// <returns>A string describing the last error is returned.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pb_get_error();

        /// <summary>
        /// Initializes the board.
        /// </summary>
        /// <returns>A negative number is returned on failure.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_init();

        /// <summary>
        /// Stops program execution and returns to the beginning of the program, waiting for a trigger.
        /// </summary>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pb_reset();

        /// <summary>
        /// Select which board to talk to.
        /// </summary>
        /// <param name="board_num">Specifies which board to select.</param>
        /// <returns>A negative number is returned on failure.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_select_board(
            int board_num);

        /// <summary>
        /// Get the firmware version on the board.
        /// </summary>
        /// <returns>Returns the firmware id.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_get_firmware_id();

        /// <summary>
        /// End communication with the board.
        /// </summary>
        /// <returns>A negative number is returned on failure.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_close();

        /// <summary>
        /// Tell the library what clock frequency the board uses.
        /// </summary>
        /// <param name="clockFrequency">Frequency of the clock in MHz.</param>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pb_core_clock(
            double clockFrequency);

        /// <summary>
        /// This function tells the board to start programming one of the onboard devices.
        /// </summary>
        /// <param name="device">Specifies which device to start programming.</param>
        /// <returns>A negative number is returned on failure.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_start_programming(
            int device);

        /// <summary>
        /// Finishes the programming for a specific onboard devices which was started by pb_start_programming.
        /// </summary>
        /// <returns>A negative number is returned on failure.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_stop_programming();

        /// <summary>
        /// Send a software trigger to the board.
        /// </summary>
        /// <returns>A negative number is returned on failure.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_start();

        /// <summary>
        /// Read status from the board.
        /// </summary>
        /// <returns>Word that indicates the state of the current board.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_read_status();

        /// <summary>
        /// Stops output of board.
        /// </summary>
        /// <returns>A negative number is returned on failure.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_stop();

        /// <summary>
        /// This is the instruction programming function for boards without a DDS.
        /// </summary>
        /// <param name="flags">Set every bit to one for each flag you want to set high.</param>
        /// <param name="inst">Specify the instruction you want.</param>
        /// <param name="inst_data">Instruction specific data.</param>
        /// <param name="length">Length of this instruction in nanoseconds.</param>
        /// <returns>A negative number is returned on failure.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_inst_pbonly(
            int flags, 
            int inst, 
            int inst_data, 
            double length);

        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern int pb_inst_direct(
            int* flags, 
            int inst, 
            int inst_data, 
            int length);

        /// <summary>
        /// This function sets the RadioProcessor to its default state.
        /// </summary>
        /// <returns>A negative number is returned on failure.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_set_defaults();

        /// <summary>
        /// Retrieve the contents of the overflow registers.
        /// </summary>
        /// <param name="reset">Set this to one to reset the overflow counters.</param>
        /// <param name="of">Pointer to a PB_OVERFLOW_STRUCT which will hold the values of the overflow counter.</param>
        /// <returns>A negative number is returned on failure.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_overflow(
            int reset,
            [Out] PB_OVERFLOW_STRUCT of);

        /// <summary>
        /// Get the current value of the scan count register.
        /// </summary>
        /// <param name="reset">If this parameter is set to 1, this function will reset the scan counter to 0.</param>
        /// <returns>The number of scans performed. A negative number is returned on failure.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_scan_count(
            int reset);

        /// <summary>
        /// Retrieve the captured data from the board's memory.
        /// </summary>
        /// <param name="num_points">Number of complex points to read from RAM.</param>
        /// <param name="real_data">Real data from RAM is stored into this array.</param>
        /// <param name="imag_data">Imag data from RAM is stored into this array.</param>
        /// <returns>A negative number is returned on failure.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_get_data(
            int num_points,
            [Out] int[] real_data,
            [Out] int[] imag_data);

        /// <summary>
        /// Load the DDS with the given waveform.
        /// </summary>
        /// <param name="data">An array of 1024 floats that represent a single period of the waveform you want to have loaded.</param>
        /// <param name="device">Device you wish to program the waveform to.</param>
        /// <returns>A negative number is returned on failure.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_dds_load(
            [In] float[] data, 
            int device);

        /// <summary>
        /// Set the value of one of the amplitude registers.
        /// </summary>
        /// <param name="amp">Amplitude value. 0.0 - 1.0</param>
        /// <param name="addr">Address of register to write to.</param>
        /// <returns>A negative number is returned on failure.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_set_amp(
            float amp, 
            int addr);

        /// <summary>
        /// Program the onboard filters to capture data and reduce it to a baseband signal with the given spectral width.
        /// </summary>
        /// <param name="spectral_width">Desired spectral width (in MHz) of the stored baseband data.</param>
        /// <param name="scan_repetitions">Number of scans intended to be performed.</param>
        /// <param name="cmd">This paramater provides additional options for this function.</param>
        /// <returns>A negative number is returned on failure.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_setup_filters(
            double spectral_width, 
            int scan_repetitions, 
            int cmd);

        /// <summary>
        /// Set the number of complex points to capture.
        /// </summary>
        /// <param name="num_points">The number of complex points to capture.</param>
        /// <returns>A negative number is returned on failure.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_set_num_points(
            int num_points);

        /// <summary>
        /// Write the given frequency to a frequency register on a DDS enabled board.
        /// </summary>
        /// <param name="freq">The frequency in MHz to be programmed to the register.</param>
        /// <returns>A negative number is returned on failure.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_set_freq(
            double freq);

        /// <summary>
        /// Write the given phase to a phase register on DDS enabled boards.
        /// </summary>
        /// <param name="phase">The phase in degrees to be programmed to the register.</param>
        /// <returns>A negative number is returned on failure.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_set_phase(
            double phase);

        /// <summary>
        /// Program an instruction of the pulse program.
        /// </summary>
        /// <param name="freq">Selects which frequency register to use.</param>
        /// <param name="cos_phase">Selects which phase register to use for the cos (real) channel.</param>
        /// <param name="sin_phase">Selects which phase register to use for the sin (imaginary) channel.</param>
        /// <param name="tx_phase">Selects which phase register to use for the TX channel.</param>
        /// <param name="tx_enable">When this is 1, the TX channel will be output on the Analog Out connector. When this is 0, Analog Out channel will be turned off.</param>
        /// <param name="phase_reset">When this is 1, the phase of all DDS channels will be reset to their time=0 phase. They will stay in this state until the value of this bit returns to 0.</param>
        /// <param name="trigger_scan">When this is 1, a scan will be triggered. To start a second scan, this bit must be set to 0 and then back to 1.</param>
        /// <param name="flags">Controls the state of the user available digital out pins.</param>
        /// <param name="inst">Which instruction to use.</param>
        /// <param name="inst_data">Some instructions require additional data. This allows that data to be specified.</param>
        /// <param name="length">Time until the next instruction is executed in nanoseconds.</param>
        /// <returns>A negative number is returned on failure.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_inst_radio(
            int freq, 
            int cos_phase, 
            int sin_phase,
            int tx_phase,
            int tx_enable,
            int phase_reset,
            int trigger_scan,
            int flags, 
            int inst,
            int inst_data,
            double length);

        /// <summary>
        /// Write an instruction that makes use of the pulse shape feature of some RadioProcessor boards.
        /// </summary>
        /// <param name="freq">Selects which frequency register to use.</param>
        /// <param name="cos_phase">Selects which phase register to use for the cos (real) channel.</param>
        /// <param name="sin_phase">Selects which phase register to use for the sin (imaginary) channel.</param>
        /// <param name="tx_phase">Selects which phase register to use for the TX channel.</param>
        /// <param name="tx_enable">When this is 1, the TX channel will be output on the Analog Out connector. When this is 0, Analog Out channel will be turned off.</param>
        /// <param name="phase_reset">When this is 1, the phase of all DDS channels will be reset to their time=0 phase. They will stay in this state until the value of this bit returns to 0.</param>
        /// <param name="trigger_scan">When this is 1, a scan will be triggered. To start a second scan, this bit must be set to 0 and then back to 1.</param>
        /// <param name="use_shape">Select whether or not to use shaped pulses.</param>
        /// <param name="amp">Select which amplitude register to use.</param>
        /// <param name="flags">Controls the state of the user available digital out pins.</param>
        /// <param name="inst">Which instruction to use.</param>
        /// <param name="inst_data">Some instructions require additional data. This allows that data to be specified.</param>
        /// <param name="length">Time until the next instruction is executed in nanoseconds.</param>
        /// <returns>A negative number is returned on failure.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_inst_radio_shape(
            int freq, 
            int cos_phase, 
            int sin_phase,
            int tx_phase, 
            int tx_enable,
            int phase_reset,
            int trigger_scan,
            int use_shape,
            int amp, 
            int flags,
            int inst,
            int inst_data,
            double length);

        /// <summary>
        /// Write an instruction that makes use of the pulse shape feature of some RadioProcessor boards.
        /// </summary>
        /// <param name="freq">Selects which frequency register to use.</param>
        /// <param name="cos_phase">Selects which phase register to use for the cos (real) channel.</param>
        /// <param name="sin_phase">Selects which phase register to use for the sin (imaginary) channel.</param>
        /// <param name="tx_phase">Selects which phase register to use for the TX channel.</param>
        /// <param name="tx_enable">When this is 1, the TX channel will be output on the Analog Out connector. When this is 0, Analog Out channel will be turned off.</param>
        /// <param name="phase_reset">When this is 1, the phase of all DDS channels will be reset to their time=0 phase. They will stay in this state until the value of this bit returns to 0.</param>
        /// <param name="trigger_scan">When this is 1, a scan will be triggered. To start a second scan, this bit must be set to 0 and then back to 1.</param>
        /// <param name="use_shape">Select whether or not to use shaped pulses.</param>
        /// <param name="amp">Select which amplitude register to use.</param>
        /// <param name="real_add_sub"></param>
        /// <param name="imag_add_sub"></param>
        /// <param name="channel_swap"></param>
        /// <param name="flags">Controls the state of the user available digital out pins.</param>
        /// <param name="inst">Which instruction to use.</param>
        /// <param name="inst_data">Some instructions require additional data. This allows that data to be specified.</param>
        /// <param name="length">Time until the next instruction is executed in nanoseconds.</param>
        /// <returns>A negative number is returned on failure.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_inst_radio_shape_cyclops(
            int freq,
            int cos_phase,
            int sin_phase,
            int tx_phase,
            int tx_enable,
            int phase_reset,
            int trigger_scan,
            int use_shape,
            int amp,
            int real_add_sub,
            int imag_add_sub,
            int channel_swap,
            int flags,
            int inst,
            int inst_data,
            double length);

        /// <summary>
        /// Calculates the Fourier transform of a given set of real and imaginary points.
        /// </summary>
        /// <param name="numberPoints">Number of points for FFT (must be a power of 2).</param>
        /// <param name="real_in">Array of real points for FFT calculation.</param>
        /// <param name="imag_in">Array of imaginary points for FFT calculation.</param>
        /// <param name="real_out">Real part of FFT output.</param>
        /// <param name="imag_out">Imaginary part of FFT output.</param>
        /// <param name="mag_fft">Magnitude of the FFT output.</param>
        /// <returns>A negative number is returned on failure.</returns>
        [DllImport(dllname, CallingConvention = CallingConvention.Cdecl)]
        public static extern int pb_fft(
            int numberPoints,
            [In] int[] real_in,
            [In] int[] imag_in,
            [Out] double[] real_out,
            [Out] double[] imag_out,
            [Out] double[] mag_fft);
    }
}
