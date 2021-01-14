using System;
using System.Collections.Generic;
using System.Text;

namespace WrapSpectralTest {

	class Program {

		static double[] LapxmData = new double[8];

		static void Main(string[] args) {

			LapxmData[0] = -1.0;
			LapxmData[1] = -1.0;
			LapxmData[2] = -1.0;
			LapxmData[3] = -1.0;
			LapxmData[4] = -1.0;
			LapxmData[5] = -1.0;
			LapxmData[6] = -1.0;
			LapxmData[7] = 10.0;

			int minFreq, maxFreq, oldMaxFreq;

			int maxPeak = -1;
			double maxValue = -1.0;
			for (int i = 0; i < 8; i++) {
				if (LapxmData[i] > maxValue) {
					maxValue = LapxmData[i];
					maxPeak = i;
					break;
				}
			}

			FindPeakRegion(LapxmData, 0, true, maxPeak, 0.0, out minFreq, out maxFreq, out oldMaxFreq);

			Console.WriteLine("Min, Max = (old) " + minFreq + ", " + oldMaxFreq + ";  (new) " + minFreq + ", " + maxFreq);
		}

		static bool FindPeakRegion(double[] LapxmData,
							int iGate,
							bool bWind,
							int iMaxPeak,
							double fNoiseLevel,
							out int iMinFreq,
							out int iMaxFreq,
							out int iOldMaxFreq)
		{
			iMinFreq = -99;
			iMaxFreq = -98;
			iOldMaxFreq = -97;
			
			int lNumPointsInSpectrum = 8;
			int lWindNumPoints = 8;
			int lWindBeginPoint = 0;
			int lRassNumPoints = 0;
			int lRassBeginPoint = 0;
			int iDcPoint = (int)(lNumPointsInSpectrum / 2); // truncates
			int iFirstSpectralPoint = lWindBeginPoint - 1;
			int iNumberOfPoints = lWindNumPoints;
			int iZeroPointIndex = iGate * (lWindNumPoints + lRassNumPoints);
			int iStartPointIndex = (iZeroPointIndex + lRassNumPoints);

			// If there is a spectral peak very close to the largest or smallest Doppler velocity, 
			// the peak may actually  "alias" to the opposite end of the Doppler velocities.
			// This is only allowed when all the spectral points have been retained. 
			// Thus we are checking here to see if we are working with the full number of points 
			// or only a section of them.
			bool bIsAliasingAllowed = false;
			if (iNumberOfPoints == lNumPointsInSpectrum) {
				bIsAliasingAllowed = true;
			}

			bool bFoundMinFreq = false;
			bool bFoundMaxFreq = false;

			// Left Side - Determine where the peak's region crosses the noise floor.
			for (int iPoint = iMaxPeak - 1; iPoint >= 0; iPoint--) {
				if (LapxmData[iStartPointIndex + iPoint] <= fNoiseLevel) {
					iMinFreq = iPoint;
					bFoundMinFreq = true;
					break;
				}
			}


			if (!bFoundMinFreq) {
				if (bIsAliasingAllowed == true) {
					// Wrap around and keep looking from the right side down to the MaxPeak
					for (int iPoint = iNumberOfPoints - 1; iPoint > iMaxPeak + 1; iPoint--) {
						if (LapxmData[iStartPointIndex + iPoint] <= fNoiseLevel) {
							iMinFreq = iPoint - iNumberOfPoints;
							break;
						}
					}
				}
				else {
					// If aliasing is not allowed, then use the minimum spectral point
					iMinFreq = iFirstSpectralPoint;
				}
			}


			// Right Side - Determine where the peak's region crosses the noise floor.
			for (int iPoint = iMaxPeak + 1; iPoint < iNumberOfPoints; iPoint++) {
				if (LapxmData[iStartPointIndex + iPoint] <= fNoiseLevel) {
					iMaxFreq = iPoint;
					iOldMaxFreq = iPoint;
					bFoundMaxFreq = true;
					break;
				}
			}

			if (!bFoundMaxFreq) {
				if (bIsAliasingAllowed == true) {
					// Wrap around and keep looking from the left side up to the MaxPeak
					for (int iPoint = 0; iPoint < iMaxPeak - 1; iPoint++) {
						if (LapxmData[iStartPointIndex + iPoint] <= fNoiseLevel) {
							//RHL - was previously:
							iOldMaxFreq = (iNumberOfPoints-1) + iPoint;
							iMaxFreq = iNumberOfPoints + iPoint;
							break;
						}
					}
				}
				else {
					// If aliasing is not allowed, then use the minimum spectral point
					//dac - changed:
					iMaxFreq = iNumberOfPoints;
					iOldMaxFreq = iNumberOfPoints - 1;
				}
			}


			return true;

		}  // end method FindPeakRegion

	}  // end class Program

}  // end namespace
