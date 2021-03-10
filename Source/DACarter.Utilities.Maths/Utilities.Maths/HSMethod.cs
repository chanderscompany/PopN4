using System;

namespace DACarter.Utilities.Maths {
	public class HSMethod {

/*
 *      Algorithm:
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
		public static bool Noise(double[] data, int nspec, out double noise, out double stdev, out int numNoise) {
			stdev = 0.0;
			numNoise = 0;
			noise = 0.0;

			double[] sortedData = (double[])data.Clone();
			Array.Sort(sortedData);

			double sum = 0.0;
			double sumsq = 0.0;
			double rhs, lhs;
			double lastSum = 0.0;
			double lastSumSq = 0.0;

			for (int i = 0; i < sortedData.Length; i++) {
				sum += sortedData[i];
				sumsq += sortedData[i] * sortedData[i];
				rhs = sum * sum;
				lhs = nspec * ((i + 1) * sumsq - rhs);
				if (lhs < rhs) {
					numNoise = i + 1;
					lastSum = sum;
					lastSumSq = sumsq;
				}
			}

			noise = lastSum / numNoise;
			stdev = Math.Sqrt(lastSumSq/numNoise - (lastSum*lastSum/numNoise/numNoise));			

			return true;
		}
	}
}
