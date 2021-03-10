using System;

using DACarter.PopUtilities;
using DACarter.Utilities;


namespace POPN {

    // comment

	static class GroundClutter {

        static bool EnableGCLog = true;  // for debugging

		/// <summary>
		/// Class Identify()
		/// Detects ground clutter and notes its Doppler extent in PopDataPackage.ClutterPoints[iht] array
		/// </summary>
		/// <param name="dp"></param>
		/// <remarks>
		/// Derived from LAPXM module written by Coy Chanders.
		/// LAPXM description:
		/// CUSTOM DESCRIPTION Receives Spectra Data and removes the ground clutter
		/// using Tony Riddle's algorithm, which is also used in Pop.
		///
		/// Riddle's Ground Clutter Algorithm
		/// 
		/// 1) Calculate the number of gates below the clutter start height.
		/// 2) Start one gate above the first height.
		/// 3) Find the noise level, the max peak and the max peak's region above the noise floor.
		/// 4) Determine if the region includes the DC point.
		/// 5) Loop through all the gates below the starting gate.
        /// 6) Determine if there is any ground clutter at this gate.
        ///    a) Start at the DC point
        ///    b) Check each pair of points moving out from DC for ground clutter?
        ///	      1) The 1st pair of points on each side of zero must be within 
        ///          iTimesBigger times of each other in magnitude.
        ///	      2) The remaining pairs of points must be:
        ///          a) Within iTimesBigger times of each other in magnitude.
        ///	         b) iPercentLess or less in magnitude than the previous point closer to zero.
        /// 7) Did the region of the previous gate's max peak  include the DC point?
        ///    a) No  - Remove all ground clutter points using interpolation.
        ///    b) Yes - Do nothing.
        /// 8) Find the max peak, noise level and region for this gate for reference on the next gate.
		/// 9) Move on to the next gate --> step 6).
		///
		/// WRITTEN BY: Coy Chanders 10-26-02
		///
		/// LAST MODIFIED BY: Raisa Lehtinen 2003-06-02
		/// Raisa Lehtinen 2003-06-19: If lowest range gate is above clutter region, return silently.
		/// Raisa Lehtinen 2004-10-13: Removed sin() term from oblique range gate computation.
		////////////////////////////////////////////////////////////////////////////////
		/// </remarks>
		/// <returns></returns>
		public static bool Identify(PopDataPackage3 dp) {

            EnableGCLog = true;
            if (!dp.Parameters.Debug.DebugToFile) {
                EnableGCLog = false;
            }

            WriteGCLog("Entering Identify()", false);
            WriteGCLog(dp.RecordTimeStamp.ToString(), true);

			if (!dp.Parameters.SystemPar.RadarPar.ProcPar.RemoveClutter) {
				// clutter removal is off
				return true;
			}


			int nhts = dp.Parameters.SystemPar.RadarPar.BeamParSet[0].NHts;
			int allpts = dp.Parameters.SystemPar.RadarPar.BeamParSet[0].NPts;
            int specpts = dp.Spectra[0][0].Length;
            int nrx = dp.Parameters.SystemPar.RadarPar.ProcPar.NumberOfRx;
            int nspec = dp.Parameters.SystemPar.RadarPar.BeamParSet[0].NSpec;

            for (int irx = 0; irx < nrx; irx++) {
                for (int iht = 0; iht < nhts; iht++) {
                    dp.ClutterPoints[irx][iht] = 0;
                }
            }

            // TODO: should snrThld use all RASS points or just wind points for npts
            double snrThld = 25 * Math.Sqrt(nspec - 2.3125 + (170.0 / allpts)) / (allpts * nspec);
            double snrThldDB = 10.0 * Math.Log10(snrThld);
            WriteGCLog("  SNR Thld = " + snrThld.ToString("f5") + "  " + snrThldDB.ToString("F2") + " dB", true);

			int iDcPoint = specpts / 2;
			double MperNs = PopParameters.MperNs;
			double clutterHtM = dp.Parameters.SystemPar.RadarPar.ProcPar.MaxClutterHtKm * 1000.0;
            // POPREV sysdelay correction added to sample delay for GC hts dac 20130529
            int sysdelay = dp.Parameters.SystemPar.RadarPar.BeamParSet[0].SystemDelayNs;
            double sampleDelayM = (dp.Parameters.SystemPar.RadarPar.BeamParSet[0].SampleDelayNs - sysdelay) * MperNs;
			double sampleSpacingM = dp.Parameters.SystemPar.RadarPar.BeamParSet[0].SpacingNs * MperNs;

            double sigSNR;
            double snrThldMultiplier = 0.01;
            bool signalIsSmall;

			// start gate is first gate above clutter height
			// first gate is gate just below start gate
			double dClutterFirstGate = (clutterHtM - sampleDelayM) / sampleSpacingM;
			int clutterFirstGate = (int)dClutterFirstGate;
			int clutterStartGate = clutterFirstGate + 1;

            WriteGCLog("First first Gate = " + clutterFirstGate.ToString(), true);
            WriteGCLog("  Clutter Ht km  = " + dp.Parameters.SystemPar.RadarPar.ProcPar.MaxClutterHtKm.ToString(), true);
            WriteGCLog("  Clutter Ht M   = " + clutterHtM.ToString(), true);
            WriteGCLog("  Sample delay M = " + sampleDelayM.ToString(), true);
            WriteGCLog("  Sample Spac  M = " + sampleSpacingM.ToString(), true);
            
            if (clutterFirstGate < 0) {
				return true;
			}
			if (clutterStartGate > nhts - 1) {
				clutterStartGate = nhts - 1;
				clutterFirstGate = clutterStartGate - 1;
			}

            WriteGCLog("Start Gate = " + clutterStartGate.ToString(), true);
            WriteGCLog("First Gate = " + clutterFirstGate.ToString(), true);
            WriteGCLog("DC point = " + iDcPoint.ToString(), true);

            int nClutterGates = clutterFirstGate + 1;

			int iFirstGateForThisReceiver = 0;

			// clutter algorithm parameters in Lapxm
			bool UseMaxNoiseLevel = true;
            bool restrictGCExtent;
            bool restrictGCExtentDefault = true;
            double TimesBigger;
			double TimesBiggerDefault = 3.0;
			double PercentLess;
            double PercentLessDefault = 20.0;
            // new parameters
            bool restrictIfDcInPrev;
            bool restrictIfDcInPrevDefault = true;
            double minSnrThldDB;
            double minSnrThldDBDefault = -99.0;

            restrictGCExtent = dp.Parameters.SystemPar.RadarPar.ProcPar.GCRestrictExtent;
            restrictIfDcInPrev = dp.Parameters.SystemPar.RadarPar.ProcPar.GCRestrictIfDcInPrev;
            minSnrThldDB = dp.Parameters.SystemPar.RadarPar.ProcPar.GCMinSigThldDB;
            TimesBigger = dp.Parameters.SystemPar.RadarPar.ProcPar.GCTimesBigger;
            if (TimesBigger == 0.0) {
                // GC parameters probably not defined in parx file
                TimesBigger = TimesBiggerDefault;
                //restrictGCExtent = restrictGCExtentDefault;
                //restrictIfDcInPrev = restrictIfDcInPrevDefault;
                //minSnrThldDB = minSnrThldDBDefault;
            }
            PercentLess = dp.Parameters.SystemPar.RadarPar.ProcPar.GCPercentLess;
            if (PercentLess < 1.0) {
                // GC parameters probably not defined in parx file
                PercentLess = PercentLessDefault;
                //restrictGCExtent = restrictGCExtentDefault;
                //restrictIfDcInPrev = restrictIfDcInPrevDefault;
                //minSnrThldDB = minSnrThldDBDefault;
            }

            snrThldMultiplier = Math.Pow(10.0, minSnrThldDB / 10.0);

			// ********************************************************************
			// First check to see if the start gate contains a clear air peak. 
			// If the max peak's region contains the DC point, we can not remove 
			// ground clutter from the next gate below.
			// ********************************************************************
            for (int irx = 0; irx < nrx; irx++) {
                double fStartGateMaxNoiseLevel = 0.0;
                double fStartGateMeanNoiseLevel = 0.0;
                int iStartGateMaxPeak = 0;
                int iStartGateMinFreq = 0;
                int iStartGateMaxFreq = 0;
                int iPreviousGateMaxPeak = 0;
                bool bDidPreviousGateIncludeDc = false;

                bool hr;
                // Find the max noise level for the start gate. 
                hr = FindNoiseLevels(dp, clutterStartGate, irx, out fStartGateMaxNoiseLevel,
                                        out fStartGateMeanNoiseLevel);

                // Find the max peak for the start gate.
                hr = FindMaxPeak(dp, clutterStartGate, irx, out iStartGateMaxPeak);

                // Store max peak for use in testing of next gate
                iPreviousGateMaxPeak = iStartGateMaxPeak;

                // Find the max peak's region for the start gate.
                double fNoiseLevel = (UseMaxNoiseLevel) ? fStartGateMaxNoiseLevel : fStartGateMeanNoiseLevel;
                hr = FindPeakRegion(dp, clutterStartGate, irx, iStartGateMaxPeak, 
                                    fNoiseLevel, out iStartGateMinFreq, out iStartGateMaxFreq, out sigSNR);

                if (sigSNR < snrThldMultiplier * snrThld) {
                    signalIsSmall = true;
                }
                else {
                    signalIsSmall = false;
                }

                // Does the max peak's region contain the DC point?
                if ((iStartGateMinFreq < iDcPoint) && (iStartGateMaxFreq > iDcPoint)) {
                    if (!signalIsSmall) {
                        bDidPreviousGateIncludeDc = true;
                    }
                    else {
                        // signal is small; don't use its peak
                        bDidPreviousGateIncludeDc = false;
                        iPreviousGateMaxPeak = 0;
                    }
                }
                else {
                    bDidPreviousGateIncludeDc = false;
                }

                WriteGCLog("Start Gate Min Freq = " + iStartGateMinFreq.ToString(), true);
                WriteGCLog("Start Gate Max Freq = " + iStartGateMaxFreq.ToString(), true);

                // ********************************************************************
                // Apply algorithm to each Gate below the start gate
                // ********************************************************************

                for (int iGate = (clutterFirstGate + iFirstGateForThisReceiver); iGate >= iFirstGateForThisReceiver; iGate--) {

                    // ********************************************************************
                    // Always check for ground clutter around DC
                    //
                    // Check each pair of points moving out from DC
                    // Consider those points ground clutter if each pair is:
                    // A) Within a factor of m_lTimesBigger of each other.  
                    // B) The power of each point is at least m_lPercentLess than the previous point.
                    // ********************************************************************

                    double fRightPoint;
                    double fLeftPoint;
                    double fRightPointPrev;
                    double fLeftPointPrev;
                    //int   iZeroPointIndex       = 0;
                    //int   iStartPointIndex      = 0;
                    int iDcPointIndex = iDcPoint;
                    int iMaxLimit = specpts / 4;


                    WriteGCLog("  iGate = " + iGate.ToString(), true);

                    // Do not let the search go past the last doppler peak value
                    if (restrictGCExtent) {
                        if (iMaxLimit > Math.Abs(iPreviousGateMaxPeak - iDcPoint)) {
                            iMaxLimit = Math.Abs(iPreviousGateMaxPeak - iDcPoint);
                        }
                    }

                    WriteGCLog("    iMaxLimit = " + iMaxLimit.ToString(), true);

                    double noise = dp.Noise[irx][iGate];
                    WriteGCLog("    Noise = " + noise.ToString("F6"), true);

                    int iNumberOfGroundClutterPoints;
                    for (iNumberOfGroundClutterPoints = 1;
                        iNumberOfGroundClutterPoints < iMaxLimit;
                        iNumberOfGroundClutterPoints++) {
                        // Get next pair of points along with the last pair for comparison
                        // Calculation on winds section only
                        // POPREV 3.27 remove noise from spectral pt before GC tests
                        fRightPoint = dp.Spectra[irx][iGate][iDcPointIndex + iNumberOfGroundClutterPoints] - noise;
                        fLeftPoint = dp.Spectra[irx][iGate][iDcPointIndex - iNumberOfGroundClutterPoints] - noise;
                        fRightPointPrev = dp.Spectra[irx][iGate][iDcPointIndex + iNumberOfGroundClutterPoints - 1] - noise;
                        fLeftPointPrev = dp.Spectra[irx][iGate][iDcPointIndex - iNumberOfGroundClutterPoints + 1] - noise;
                        if (fRightPoint < 0.0) {
                            WriteGCLog("    iPt " + (iDcPointIndex + iNumberOfGroundClutterPoints).ToString() + " is below noise.", true);
                            break;  // if reached noise level, then this is not GC
                        }
                        if (fLeftPoint < 0.0) {
                            WriteGCLog("    iPt " + (iDcPointIndex - iNumberOfGroundClutterPoints).ToString() + " is below noise.", true);
                            break;
                        }
                        if ((fRightPointPrev < 0.0) && (iNumberOfGroundClutterPoints != 1)) {
                            WriteGCLog("    iPtPrev " + (iDcPointIndex + iNumberOfGroundClutterPoints - 1).ToString() + " is below noise.", true);
                            break;
                        }
                        if ((fLeftPointPrev < 0.0) && (iNumberOfGroundClutterPoints != 1)) {
                            WriteGCLog("    iPtPrev " + (iDcPointIndex - iNumberOfGroundClutterPoints + 1).ToString() + " is below noise.", true);
                            break;
                        }

                        // Are the two points within m_lTimesBigger times of each other
                        if (fRightPoint > TimesBigger * fLeftPoint
                                || fLeftPoint > TimesBigger * fRightPoint) {
                            WriteGCLog("    Is timesBigger at pt " + iNumberOfGroundClutterPoints.ToString()
                                                                        + " : " + (fLeftPoint/fRightPoint).ToString("F3"), true);
                            break;
                        }

                        // Are the two points both closer to zero by m_lPercentLess in magnitude 
                        // than the previous point. Do not perform the m_lPercentLess test on the 
                        // first pair of points since DC removal would cause it to fail.
                        if (iNumberOfGroundClutterPoints != 1) {
                            float fPercentDown = (float)((100.0 - (float)PercentLess) / 100.0);
                            if (fRightPoint > fPercentDown * fRightPointPrev
                                || fLeftPoint > fPercentDown * fLeftPointPrev) {
                                WriteGCLog(
                                    "    " + (iNumberOfGroundClutterPoints).ToString() + " Is greater than percentDown " + fPercentDown.ToString("F3") + ": " +
                                    fLeftPoint.ToString("F2") + ":" + fLeftPointPrev.ToString("F2") + "  " +
                                    fRightPoint.ToString("F2") + ":" + fRightPointPrev.ToString("F2") + " ns: " + (dp.Noise[irx][iGate]).ToString("F2"),
                                    true);
                                break;
                            }
                        }
                    }

                    WriteGCLog("    Number of GC points = " + (iNumberOfGroundClutterPoints-1).ToString(), true);
                    //dp.ClutterPoints[irx][iGate] = iNumberOfGroundClutterPoints - 1;
                    
                    // ********************************************************************
                    // Is there some ground clutter?
                    // 
                    // Since the DC point and the first set of points do not count,
                    // there must be at least 2 sets of Ground Clutter points.
                    // ********************************************************************
                    if (iNumberOfGroundClutterPoints > 2) {
                        // Did the previous gate's region NOT include the zero frequency point?
                        if (!bDidPreviousGateIncludeDc || !restrictIfDcInPrev) {

                            // Remove all the ground clutter
                            WriteGCLog("    +++Remove ground clutter points", true);
                            dp.ClutterPoints[irx][iGate] = iNumberOfGroundClutterPoints - 1;

                            /*
                            int iLeftNonGroundClutterPoint = iDcPointIndex - iNumberOfGroundClutterPoints;
                            int iRightNonGroundClutterPoint = iDcPointIndex + iNumberOfGroundClutterPoints;
                            int iNumberOfPointsToInterpolate = iRightNonGroundClutterPoint - iLeftNonGroundClutterPoint;

                            // Interpolate out the ground clutter
                            double fSlope = dp.Spectra[irx][iGate][iRightNonGroundClutterPoint];
                            fSlope = (fSlope - dp.Spectra[irx][iGate][iLeftNonGroundClutterPoint]) / iNumberOfPointsToInterpolate;
                            for (int iPointIndex = iLeftNonGroundClutterPoint + 1; iPointIndex < iRightNonGroundClutterPoint; iPointIndex++) {
                                if (iClutterRemovalMethod == 0) {
                                    // Set all clutter points to zero
                                    // These zeros will be replaced with the noise level soon
                                    dp.Spectra[irx][iGate][iPointIndex] = 0;
                                    vec_IndexOfZeroValues.Add(iPointIndex);
                                }
                                else if (iClutterRemovalMethod == 1) {
                                    // Interpolate from one side of the clutter region to the other.
                                    dp.Spectra[irx][iGate][iPointIndex] = dp.Spectra[irx][iGate][iPointIndex - 1] + fSlope;
                                }
                                else {
                                    // Mirror 
                                }
                            }
                            */
                        }
                        else {
                            WriteGCLog("    ---Previous gate included DC; do not remove GC.", true);
                            dp.ClutterPoints[irx][iGate] = 0;
                        }
                    }
                    else {
                        WriteGCLog("    ---GC points < 3; do not remove GC.", true);
                        dp.ClutterPoints[irx][iGate] = 0;
                    }

                    double fThisGateMaxNoiseLevel = 0.0;
                    double fThisGateMeanNoiseLevel = 0.0;
                    int iThisGateMaxPeak = 0;
                    int iThisGateMinFreq = 0;
                    int iThisGateMaxFreq = 0;

                    // Find the max noise level at this gate 
                    hr = FindNoiseLevels(dp, iGate, irx, out fThisGateMaxNoiseLevel,
                                            out fThisGateMeanNoiseLevel);

                    /*
                    // Replace zeros with the noise level
                    int numGC = vec_IndexOfZeroValues.Count;
                    if (numGC > 0) {
                        WriteGCLog("    Replacing " + numGC.ToString() + " GC points" , true);
                    }
                    for (int iPointIndex = 0; iPointIndex < vec_IndexOfZeroValues.Count; iPointIndex++) {
                        dp.Spectra[irx][iGate][vec_IndexOfZeroValues[iPointIndex]] = fThisGateMeanNoiseLevel;
                    }
                    */

                    // Find max peak at this gate
                    hr = FindMaxPeak(dp, iGate, irx, out iThisGateMaxPeak);

                    // Find the max peak's region at this gate
                    fNoiseLevel = (UseMaxNoiseLevel) ? fThisGateMaxNoiseLevel : fThisGateMeanNoiseLevel;
                    hr = FindPeakRegion(dp, iGate, irx, iThisGateMaxPeak, fNoiseLevel,
                                        out iThisGateMinFreq, out iThisGateMaxFreq, out sigSNR);

                    double snrDB = 10.0 * Math.Log10(sigSNR);
                    WriteGCLog("    SNR = " + snrDB.ToString("F2") + " (" + snrThldDB.ToString("F2") + ")", true);
                    if (sigSNR < snrThldMultiplier * snrThld) {
                        signalIsSmall = true;
                        WriteGCLog("    Signal is too small", true);
                    }
                    else {
                        signalIsSmall = false;
                    }

                    // Store max peak for use in testing of next gate
                    iPreviousGateMaxPeak = iThisGateMaxPeak;
                    // POPREV 3.26 added test for small signal on PreviousGateMaxPeak
                    if (signalIsSmall) {
                        iPreviousGateMaxPeak = 0;
                    }

                    // Does the max peak's region contain the DC point? 
                    // Used to determine if ground clutter can be removed from the next gate.
                    if ((iThisGateMinFreq < iDcPoint) && (iThisGateMaxFreq > iDcPoint)) {
                        if (!signalIsSmall) {
                            bDidPreviousGateIncludeDc = true;
                        }
                        else {
                            // signal is small; don't use its peak
                            bDidPreviousGateIncludeDc = false;
                            iPreviousGateMaxPeak = 0;
                        }
                    }
                    else {
                        bDidPreviousGateIncludeDc = false;
                    }

                    WriteGCLog("    This gate peak signal at " + iThisGateMaxPeak.ToString(), true);
                    WriteGCLog("    This gate Signal Region = " + iThisGateMinFreq.ToString() + " - " + iThisGateMaxFreq.ToString(), true);
               
                } // Move on to the next gate
            }  // next receiver

            //GroundClutter.Remove(dp);

			return true;

		}  // end method Identify()

        //////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Removes GC in all spectra
        /// </summary>
        /// <param name="dp"></param>
        /// <returns></returns>
        static public bool Remove(PopDataPackage3 dp) {

            int nhts = dp.Parameters.SystemPar.RadarPar.BeamParSet[0].NHts;
            int nrx = dp.Parameters.SystemPar.RadarPar.ProcPar.NumberOfRx;
            int iNumberOfGroundClutterPoints;

            for (int irx = 0; irx < nrx; irx++) {
                for (int iGate = 0; iGate < nhts; iGate++) {

                    iNumberOfGroundClutterPoints = dp.ClutterPoints[irx][iGate];

                    Remove(dp.Spectra[irx][iGate], iNumberOfGroundClutterPoints, dp.Parameters);


                }  // end for iGate
            }  // end for irx

            return true;

        }

        //////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Removes GC in a single spectrum
        /// </summary>
        /// <param name="spec"></param>
        /// <param name="dp"></param>
        /// <returns></returns>
        
        static public bool Remove(double[] spec, int iNumberOfGroundClutterPoints, PopParameters param) {

            // added this fix for no ground clutter in ver 3.4
            if (iNumberOfGroundClutterPoints == 0) {
                return true;
            }

            int nhts = param.SystemPar.RadarPar.BeamParSet[0].NHts;
            int npts = param.SystemPar.RadarPar.BeamParSet[0].NPts;
            int allpts = param.SystemPar.RadarPar.BeamParSet[0].NPts;
            int specpts = spec.Length;
            int nrx = param.SystemPar.RadarPar.ProcPar.NumberOfRx;

            npts = specpts;

            int iDcPoint = npts / 2;

            int iClutterRemovalMethod = param.SystemPar.RadarPar.ProcPar.GCMethod;

            //int iNumberOfGroundClutterPoints;
            double fThisGateMaxNoiseLevel = 0.0;
            double fThisGateMeanNoiseLevel = 0.0;

            //iNumberOfGroundClutterPoints = dp.ClutterPoints[irx][igate];

            int iLeftNonGroundClutterPoint = iDcPoint - iNumberOfGroundClutterPoints - 1;
            int iRightNonGroundClutterPoint = iDcPoint + iNumberOfGroundClutterPoints + 1;
            int iNumberOfPointsToInterpolate = iRightNonGroundClutterPoint - iLeftNonGroundClutterPoint;

            // Interpolate out the ground clutter
            if (iClutterRemovalMethod == 0) {
                // Set all clutter points to noise level
                fThisGateMaxNoiseLevel = 0.0;
                fThisGateMeanNoiseLevel = 0.0;
                FindNoiseLevels(spec, param, out fThisGateMaxNoiseLevel, out fThisGateMeanNoiseLevel);
                for (int iPointIndex = iLeftNonGroundClutterPoint + 1; iPointIndex < iRightNonGroundClutterPoint; iPointIndex++) {
                    spec[iPointIndex] = fThisGateMeanNoiseLevel;
                }
            }
            else if (iClutterRemovalMethod == 1) {
                // Interpolate from one side of the clutter region to the other.
                double fSlope = spec[iRightNonGroundClutterPoint];
                fSlope = (fSlope - spec[iLeftNonGroundClutterPoint]) / iNumberOfPointsToInterpolate;
                for (int iPointIndex = iLeftNonGroundClutterPoint + 1; iPointIndex < iRightNonGroundClutterPoint; iPointIndex++) {
                    spec[iPointIndex] = spec[iPointIndex - 1] + fSlope;
                }
            }
            else {
                // Mirror subtraction
            }

            return true;

        }

        //
        ///////////////////////////////////////////////////////////////////////////////////////////////
        //

		private static bool FindPeakRegion(PopDataPackage3 dp, int igate, int irx, int iPeak, double fNoiseLevel,
                                            out int iMinFreq, out int iMaxFreq, out double snr) {

			iMinFreq = 0;
			iMaxFreq = 0;
            snr = 1.0e6;

			int npts = dp.Parameters.SystemPar.RadarPar.BeamParSet[0].NPts;
            int allpts = dp.Parameters.SystemPar.RadarPar.BeamParSet[0].NPts;
            int specpts = dp.Spectra[0][0].Length;
            npts = specpts;

            int iDcPoint = npts / 2;
			bool bIsAliasingAllowed = true;
            int nGCpts = dp.ClutterPoints[irx][igate];

			bool bFoundMinFreq = false;
			bool bFoundMaxFreq = false;

            double totalPower = 0.0;

			// Left Side - Determine where the peak's region crosses the noise floor.
			for (int iPoint = iPeak - 1; iPoint >= 0; iPoint--) {
                double power = dp.Spectra[irx][igate][iPoint];
                if (iPoint == iDcPoint) {
                    power = (dp.Spectra[irx][igate][iPoint - 1] + dp.Spectra[irx][igate][iPoint + 1]) / 2.0;
                }
                if (nGCpts > 1) {
                    // if we have ground clutter, stop peak region at ground clutter
                    if ((iPoint >= iDcPoint - nGCpts) && (iPoint <= iDcPoint + nGCpts)) {
                        power = 0.0;
                    }
                }
                if (power <= fNoiseLevel) {
                    iMinFreq = iPoint;
                    bFoundMinFreq = true;
                    break;
                }
                else {
                    totalPower += power;
                }
			}

			if (!bFoundMinFreq) {
				if (bIsAliasingAllowed == true) {
					// Wrap around and keep looking from the right side down to the MaxPeak
                    // TODO: add aliased points to power!
					for (int iPoint = npts - 1; iPoint > iPeak + 1; iPoint--) {
                        if (dp.Spectra[irx][igate][iPoint] <= fNoiseLevel) {
							iMinFreq = iPoint - npts;
							break;
						}
					}
				}
				else {
					// If aliasing is not allowed, then use the minimum spectral point
					iMinFreq = 0;
				}
			}


			// Right Side - Determine where the peak's region crosses the noise floor.
			for (int iPoint = iPeak + 1; iPoint < npts; iPoint++) {
                double power = dp.Spectra[irx][igate][iPoint];
                if (iPoint == iDcPoint) {
                    power = (dp.Spectra[irx][igate][iPoint - 1] + dp.Spectra[irx][igate][iPoint + 1]) / 2.0;
                }
                // POPREV 3.26 added this missing test to right side check (stop region check at GC):
                if (nGCpts > 1) {
                    // if we have ground clutter, stop peak region at ground clutter
                    if ((iPoint >= iDcPoint - nGCpts) && (iPoint <= iDcPoint + nGCpts)) {
                        power = 0.0;
                    }
                }
                if (power <= fNoiseLevel) {
					iMaxFreq = iPoint;
					bFoundMaxFreq = true;
					break;
				}
                else {
                    totalPower += power;
                }
            }

			if (!bFoundMaxFreq) {
				if (bIsAliasingAllowed == true) {
					// Wrap around and keep looking from the left side up to the MaxPeak
                    // TODO: add aliased points to power!
                    for (int iPoint = 0; iPoint < iPeak - 1; iPoint++) {
                        if (dp.Spectra[irx][igate][iPoint] <= fNoiseLevel) {
							iMaxFreq = npts + iPoint;
							break;
						}
					}
				}
				else {
					// If aliasing is not allowed, then use (one past) the maximum spectral point
					iMaxFreq = npts;
				}
			}

            snr = totalPower / (npts * fNoiseLevel);

			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dp"></param>
		/// <param name="igate"></param>
		/// <param name="iPeak"></param>
		/// <returns></returns>
        /// <remarks>
        /// Note that this method searches the entire spectrum, even if Restrict Moments is selected
        /// </remarks>
		private static bool FindMaxPeak(PopDataPackage3 dp, int igate, int irx, out int iPeak) {
			iPeak = 0;
			// For simplicity, uses shorter names
			int npts = dp.Parameters.SystemPar.RadarPar.BeamParSet[0].NPts;
            // POPREV: 4.14.1:
            int allpts = dp.Parameters.SystemPar.RadarPar.BeamParSet[0].NPts;
            int specpts = dp.Spectra[irx][igate].Length;
            npts = specpts;
            
            int nGCpts = dp.ClutterPoints[irx][igate];

			// Loop through all the points looking for the peak with the maximum value.
			double peakValue = -999.0;
            double specVal;
            int iDCpt = npts / 2;
			for (int iPoint = 0; iPoint < npts; iPoint++)
			{
                specVal = dp.Spectra[irx][igate][iPoint];
                if ((iPoint >= iDCpt - nGCpts) && (iPoint <= iDCpt + nGCpts)) {
                    specVal = 0.0;
                }
			    // See if this point is larger than the last recorded max peak.
				if(specVal > peakValue) {
					iPeak = iPoint;
					peakValue = specVal;
				}
			}

			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dp"></param>
		/// <param name="igate"></param>
		/// <param name="maxNoiseLevel"></param>
		/// <param name="meanNoiseLevel"></param>
		/// <returns></returns>
		private static bool FindNoiseLevels(PopDataPackage3 dp, int igate, int irx, out double maxNoiseLevel, out double meanNoiseLevel) {
			meanNoiseLevel = 0.0;
			maxNoiseLevel = 0.0;
			int numNoise = -1;
			double stdev = 0.0;
			int nspec = dp.Parameters.SystemPar.RadarPar.BeamParSet[0].NSpec;
            int npts = dp.Parameters.SystemPar.RadarPar.BeamParSet[0].NPts;
            int allpts = dp.Parameters.SystemPar.RadarPar.BeamParSet[0].NPts;
            int specpts = dp.Spectra[0][0].Length;
            npts = specpts;

            //DACarter.Utilities.Maths.Moments.GetNoise(dp.Spectra[irx][igate], npts, nspec, out meanNoiseLevel, out stdev, out numNoise);
            // POPREV: 4.14.1:
            DACarter.Utilities.Maths.Moments.GetNoise(dp.Spectra[irx][igate], npts, nspec, out meanNoiseLevel, out stdev, out numNoise);
            maxNoiseLevel = meanNoiseLevel + stdev;
			return true;
		}

        private static bool FindNoiseLevels(double[] spec, PopParameters param, out double maxNoiseLevel, out double meanNoiseLevel) {
            meanNoiseLevel = 0.0;
            maxNoiseLevel = 0.0;
            int numNoise = -1;
            double stdev = 0.0;
            int nspec = param.SystemPar.RadarPar.BeamParSet[0].NSpec;
            int npts = param.SystemPar.RadarPar.BeamParSet[0].NPts;
            int allpts = param.SystemPar.RadarPar.BeamParSet[0].NPts;
            int specpts = spec.Length;
            npts = specpts;

            //DACarter.Utilities.Maths.Moments.GetNoise(spec, npts, nspec, out meanNoiseLevel, out stdev, out numNoise);
            // POPREV: 4.14.1:
            DACarter.Utilities.Maths.Moments.GetNoise(spec, npts, nspec, out meanNoiseLevel, out stdev, out numNoise);
            maxNoiseLevel = meanNoiseLevel + stdev;
            return true;
        }

        static private void WriteGCLog(string text, bool append) {
            if (EnableGCLog) {
                TextFile.WriteLineToFile("GCLog.txt", text, append);
            }
        }


	}  // end class GroundClutter

}
