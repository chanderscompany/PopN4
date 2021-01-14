using System;
using System.Collections.Generic;
using System.Text;
//using MathNet.Numerics;

namespace DACarter.Utilities.Maths {

	public class Moments {

		/*
		 *      Noise Algorithm:
		 *              Uses the property of power spectra of gaussian signals that
		 *              variance = mean squared / navg ,
		 *              which can be expanded to
		 *              sumsq/n - mean*mean = mean*mean/navg ,
		 *              or to avoid any divisions
		 *              navg*(n*sumsq - sum*sum) = sum*sum ,
		 *              where sum and sumsq are the sums over
		 *              n spectral points of the spectral power
		 *              and of the power squared, respectively.
		 *              
		 *              After sorting the spectral points in
		 *              order of ascending power,
		 *              the algorithm goes through all values of n
		 *              from 1 to numpts and for each n computes the
		 *              values of both sides of the above equation.
		 *              For early values of n, the left side is
		 *              generally less than the right and then becomes
		 *              greater than the right side as higher-valued 
		 *              points that do not belong to the original
		 *              distribution are included in the n points.
		 *              The last crossing from less than to greater than
		 *              is considered the division between the desired set 
		 *              of points and "outliers" (e.g. noise vs. signal; 
		 *              noise or signal vs. interference, etc.).
		 * */

        public static bool GetNoise(double[] data, int npts, int nspec, out double noise, out double stdev, out int numNoise) {
            double mxNoise;
            int skipdc = 1;
            return GetNoise(data, npts, nspec, skipdc, out noise, out stdev, out mxNoise, out numNoise);
        }

        public static bool GetNoise(double[] data, int npts, int nspec, out double noise, out double stdev, out double maxNoise, out int numNoise) {
            int skipdc = 1;
            return GetNoise(data, npts, nspec, skipdc, out noise, out stdev, out maxNoise, out numNoise);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data">spectral array</param>
        /// <param name="npts">num pts in spectrum to process</param>
        /// <param name="nspec"></param>
        /// <param name="numSkipDc"></param>
        /// <param name="noise"></param>
        /// <param name="stdev"></param>
        /// <param name="maxNoise"></param>
        /// <param name="numNoise"></param>
        /// <returns></returns>
		public static bool GetNoise(double[] data, int npts, int nspec, int numSkipDc, out double noise, out double stdev, out double maxNoise, out int numNoise) {
            // skipdc is the number of DC points to skip; usually either 0,1, or 3
			stdev = 0.0;
			numNoise = 0;
			noise = 0.0;
			maxNoise = 0.0;

            //double[] sortedData = (double[])data.Clone();
            double[] sortedData = new double[npts];
            Array.Copy(data, sortedData, npts);

			// smooth across DC
            // TODO: GetNoise: Skip DC only if DC is filtered
            // TODO: GetNoise: skip 3 points if windowing 
			//sortedData[sortedData.Length / 2] = InterpolateDC(sortedData);
			/*
			//sortedData[sortedData.Length / 2] = 0.0;
			//sortedData[sortedData.Length / 2 + 1] = 0.0;
			//sortedData[sortedData.Length / 2 - 1] = 0.0;
			*/

            // POPREV: 3.15 skip selected num of dc points in computing noise level
            if (numSkipDc >= 1) {
			    sortedData[npts / 2] = -1.0;
            }
            if (numSkipDc >= 3) {
			    sortedData[npts / 2 + 1] = -1.0;
			    sortedData[npts / 2 - 1] = -1.0;
            }
			Array.Sort(sortedData);

			double sum = 0.0;
			double sumsq = 0.0;
			double rhs, lhs;
			double lastSum = 0.0;
			double lastSumSq = 0.0;
			maxNoise = 0.0;

			int count = 0;
			int minNoisePts = npts / 10;
			for (int i = 0; i < npts; i++) {
				if (sortedData[i] <= 0.0) {
                    // skipping selected points
					continue;
				}
				count++;
				sum += sortedData[i];
				sumsq += sortedData[i] * sortedData[i];
				rhs = sum * sum;
				lhs = nspec * (count * sumsq - rhs);
				if ((lhs < rhs)) {
					numNoise = count;
					lastSum = sum;
					lastSumSq = sumsq;
					maxNoise = sortedData[i];
				}
			}

			noise = lastSum / numNoise;
			stdev = Math.Sqrt(lastSumSq / numNoise - (lastSum * lastSum / numNoise / numNoise));

			if (numNoise <= minNoisePts) {
				// Using Line Method
				//int iDataLength = sortedData.Length;

                int skipPts = -1;
                int iMaxNoiseIndex = -1;

                try {
                    skipPts = 0;
                    for (int i = 0; i < npts; i++) {
                        if (sortedData[i] <= 0.0) {
                            skipPts++;
                        }
                        else {
                            break;
                        }
                    }
                    iMaxNoiseIndex = 0;
                    double dDelta = double.MinValue;
                    double dDifference = 0.0;
                    double dSlope = (sortedData[npts - 1] - sortedData[skipPts]) / (float)(npts - skipPts - 1);

                    bool allNegative = true;
                    for (int iDataIndex = skipPts; iDataIndex < npts; iDataIndex++) {
                        dDifference = (sortedData[skipPts] + (dSlope * (iDataIndex - skipPts))) - sortedData[iDataIndex];
                        if (dDifference > 0.0) {
                            allNegative = false;
                        }
                        if (dDifference >= dDelta) {
                            dDelta = dDifference;
                            iMaxNoiseIndex = iDataIndex;
                        }
                    }

                    //  if no differences are greater than 0, use all pts for noise
                    if (allNegative) {
                        iMaxNoiseIndex = npts - 1;
                    }

                    // Line Method answer
                    noise = sortedData[(int)((iMaxNoiseIndex - skipPts) / 2) + skipPts];
                    stdev = sortedData[iMaxNoiseIndex] - noise;
                    numNoise = iMaxNoiseIndex + 1;
                    maxNoise = sortedData[iMaxNoiseIndex];

                }
                catch (Exception ee) {
                    string msg = "npts = " + npts.ToString() + " skipPts = " + skipPts.ToString() + " iMaxMoiseIndex = " + iMaxNoiseIndex.ToString() + "  ";
                    throw new ApplicationException("GetNoise Line Method: " + msg + ee.Message);
                }
            }

			return true;
		}


		/// <summary>
		/// calculate moments between bounds of signal region.
		/// Subtract noise from spectrum
		/// compute Sum(y), sum(xy), sum(xxy)
		/// signal power = sum(y)
		/// mean Doppler = sum(xy)/sum(y)
		/// variance = sum(xxy)/sum(y) - mdop*mdop
		/// width = 2 * sqrt(var)
		/// mean Doppler and width are returned as fraction of Nyquist (half full width of spectrum)
		/// </summary>
		/// <param name="data"></param>
		/// <param name="noise"></param>
		/// <param name="power"></param>
		/// <param name="meanDopNyq"></param>
		/// <param name="widthNyq"></param>
		/// <param name="sigPts"></param>
        public static void GetMoments(double[] data,
                                        int npts,
                                        double noise,
										bool skipDC,
										out double power,
                                        out double meanDopNyq,
                                        out double widthNyq,
                                        out int sigPts) {
            GetMoments(data, npts, noise, 0.0, skipDC, 0, 0, out power, out meanDopNyq, out widthNyq, out sigPts);
        }

        public static void GetMoments(double[] data,
                                        int npts,
                                        double noise,
										bool skipDC,
										int SignalLimitLowPt,
                                        int SignalLimitHighPt,
                                        out double power,
                                        out double meanDopNyq,
                                        out double widthNyq,
                                        out int sigPts) {
            GetMoments(data, npts, noise, 0.0, skipDC, SignalLimitLowPt, SignalLimitHighPt, out power, out meanDopNyq, out widthNyq, out sigPts);
        }

        public static void GetMoments(double[] data,
                                        int npts,
                                        double noise,
                                        double deltanoise,
										bool skipDC,
										out double power,
                                        out double meanDopNyq,
                                        out double widthNyq,
                                        out int sigPts) {

            GetMoments(data, npts, noise, deltanoise, skipDC, 0, 0, out power, out meanDopNyq, out widthNyq, out sigPts);
        }
        
        /// <summary>
		/// calculate moments between bounds of signal region.
		/// Subtract noise from spectrum
		/// compute Sum(y), sum(xy), sum(xxy)
		/// signal power = sum(y)
		/// mean Doppler = sum(xy)/sum(y)
		/// variance = sum(xxy)/sum(y) - mdop*mdop
		/// width = 2 * sqrt(var)
		/// mean Doppler and width are returned as fraction of Nyquist (half full width of spectrum)
		/// Compute moments as above, but signal includes points down to noiselevel plus deltanoise.
		/// Signal power is still spectral power minus noise.
		/// </summary>
		/// <param name="data">spectral array</param>
        /// <param name="npts">num pts to process in spectral array; if 0 use array length</param>
        /// <param name="noise"></param>
        /// <param name="deltanoise"></param>
		/// <param name="power"></param>
		/// <param name="meanDop"></param>
		/// <param name="width"></param>
		/// <param name="sigPts"></param>
		public static void GetMoments(double[] data,
                                        int npts,
                                        double noise,
                                        double deltanoise,
										bool skipDC,
                                        int SignalLimitLowPt,
                                        int SignalLimitHighPt,
                                        out double power,
                                        out double meanDopNyq,
                                        out double widthNyq,
                                        out int sigPts) {

            if (npts == 0) {
                npts = data.Length;
            }

			double sumy = 0.0;
			double sumxy = 0.0;
			double sumxxy = 0.0;

			// find peak of spectrum, excluding DC
            int lowPt = SignalLimitLowPt;
            int highPt = SignalLimitHighPt;
            if ((lowPt==0) && (highPt==0)) {
                //include all pts
                lowPt = -npts;
                highPt = npts;
            }

			int maxi = -999;
			double maxp = -999.0;
            /*
            for (int ipt = 1; ipt < npts; ipt++) {
                if (data[ipt] > maxp) {
                    maxp = data[ipt];
                    maxi = ipt;
                }
            }
            */
			int ipt;	// index 0->npts-1 is from -nyq/2 up to but not including +nyq/2 
						//  dc is at npts/2
			int jpt, jp;	// index -npts/2 to npts/2-1, dc is at 0
            for (ipt = 0; ipt < npts; ipt++) {
				jpt = ipt - npts / 2;
				if (skipDC) {
					if (jpt == 0) {
						// skip dc pt
						continue;
					}
				}
				// only find peak between limits
				// limits are given relative to dc, -npts/2 through npts/2-1
				if ((jpt >= lowPt) && (jpt <= highPt)) {
                    if (data[ipt] > maxp) {
                        maxp = data[ipt];
                        maxi = ipt;
						jp = jpt;
                    }
                }
            }

            // when limiting peak search, resulting peak may be below noise:
            if (data[maxi] < noise) {
                // keep searching for nearest pt above noise
                if (maxi > npts/2) {
                    for (int i = maxi+1; i < npts-1; i++) {
                        if (data[i] > (noise + deltanoise)) {
                            maxi = i;
                            break;
                        }
                    }
                }
                else {
                    for (int i = maxi-1; i >= 0; i--) {
                        if (data[i] > (noise + deltanoise)) {
                            maxi = i;
                            break;
                        }
                    }
                }
            }

			// start at max pt and go right till noise level
			int ip = maxi;
			int ip0 = maxi;
			sigPts = 0;
			double y;
            while ((data[ip0] > (noise + deltanoise)) || (skipDC && (ip0 == npts / 2))) {
				// DAC added skipDC for rev 2.8.7:
				if (skipDC && (ip0 == npts / 2)) {
					y = InterpolateDC(data, npts) - noise;
					if (y <= 0.0) {
						// modified data point is now below noise
						break;
					}
				}
				else {
					y = data[ip0] - noise;
				}
				sigPts++;
				sumy += y;
				sumxy += ip * y;
				sumxxy += ip * ip * y;
				if (sigPts >= npts) {
					// something wrong, no noise pts.
					break;
				}
				ip++;
				ip0++;
				if (ip0 >= npts) {
					ip0 -= npts;
				}
			}
			// go from peak to left
			ip = maxi-1;
			ip0 = maxi-1;
			if (ip0 < 0) {
				ip0 += npts;
			}
            while ((data[ip0] > (noise + deltanoise)) || (skipDC && (ip0 == npts / 2))) {
				if (sigPts >= npts) {
					break;
				}
                if (skipDC && (ip0 == npts / 2)) {
					y = InterpolateDC(data, npts) - noise;
					if (y <= 0.0) {
						// modified data point is now below noise
						break;
					}
				}
				else {
					y = data[ip0] - noise;
				}
				sigPts++;
				sumy += y;
				sumxy += ip * y;
				sumxxy += ip * ip * y;
				ip--;
				ip0--;
				if (ip0 < 0) {
					ip0 += npts;
				}
			}

			power = sumy;
			double md0;
			if (power > 0.0) {
				md0 = sumxy / power;
			}
			else {
				md0 = maxi;
			}
			// adjust mean Doppler
			//	zero is at n/2 point
			//	scale to 1.0 at edge
			meanDopNyq = (md0 - npts / 2.0) / (npts / 2.0);

            // dac POPN rev 2.15 remove wrapping of mean Doppler
            /*
			if (meanDopNyq > 1.0) {
				meanDopNyq -= 2.0;
			}
			else if (meanDopNyq < -1.0) {
				meanDopNyq += 2.0;
			}
            */

			double var;
			if (power > 0.0) {
				var = sumxxy / power - md0 * md0;
			}
			else {
				var = 0.0 ;
			}
			widthNyq = 2 * Math.Sqrt(var) / (npts / 2.0);

		}



		private static double InterpolateDC(double[] data, int npts) {
            if (npts == 0) {
                npts = data.Length;
            }
			int idc = npts / 2;
			return (data[idc+1] + data[idc-1]) / 2.0;
		}

	}
}
