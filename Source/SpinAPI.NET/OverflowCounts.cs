// -----------------------------------------------------------------------
// <copyright file="OverflowCounts.cs" company="SpinCore Technologies, Inc">
//     Copyright (c) SpinCore Technologies, Inc.
// </copyright>
// -----------------------------------------------------------------------

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
    /// <summary>
    /// Overflow counters
    /// </summary>
    public class OverflowCounts
    {
        /// <summary>
        /// Initializes a new instance of the OverflowCounts class.
        /// </summary>
        /// <param name="adc">Number of overflows that occur when sampling data at the ADC.</param>
        /// <param name="cic">Number of overflows that occur after the CIC filter.</param>
        /// <param name="fir">Number of overflows that occur after the FIR filter.</param>
        /// <param name="average">Number of overflows that occur during the averaging process.</param>
        internal OverflowCounts(int adc, int cic, int fir, int average)
        {
            this.AdcOverflows = adc;
            this.CicOverflows = cic;
            this.FirOverflows = fir;
            this.AverageOverflows = average;
        }

        /// <summary>
        /// Gets the number of overflows that occur when sampling data at the ADC.
        /// </summary>
        public int AdcOverflows { get; private set; }

        /// <summary>
        /// Gets the number of overflows that occur after the CIC filter.
        /// </summary>
        public int CicOverflows { get; private set; }

        /// <summary>
        /// Gets the number of overflows that occur after the FIR filter.
        /// </summary>
        public int FirOverflows { get; private set; }

        /// <summary>
        /// Gets the number of overflows that occur during the averaging process.
        /// </summary>
        public int AverageOverflows { get; private set; }
    }
}
