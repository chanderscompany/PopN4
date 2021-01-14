using System;
using System.Runtime;

namespace DACarter.Utilities {

    /// <summary>
    /// DacMemory class
    /// Has static methods to examine memory usage.
    /// </summary>
    public static class DacMemory {

        public static bool EnoughMemoryIsAvailable(int reqMemMB, out int totalUsedMB, out int largestAvailMb) {

            largestAvailMb = LargestBlockMB();

            bool success;
            success = EnoughMemoryIsAvailable(reqMemMB, out totalUsedMB);
            return success;
        }

        public static bool EnoughMemoryIsAvailable(int reqMemMB, out int totalUsedMB) {

            long memBefore = GC.GetTotalMemory(false);
            totalUsedMB = (int)(memBefore/1000000.0 + 0.5);

            //Console.WriteLine("Want to allocate:");
            //Console.WriteLine("   " + reqMemMB.ToString() + " MB");
            //Console.WriteLine("   " + "{0} MB Used", totalUsedMB);
            //Console.WriteLine("   " + "{0} MB largest available", largestAvailMb);

            bool success = true;

            if (reqMemMB > 0) {

                MemoryFailPoint mfp = null;
                try {
                    mfp = new MemoryFailPoint(reqMemMB);
                }
                catch {
                    success = false;
                }
                finally {
                    if (mfp != null) {
                        mfp.Dispose();
                    }
                }
            }

            return success;
        }

        public static int LargestBlockMB() {
            //int availMb = 0;
            int reqMb;
            int step1Mb = 200;
            int step2Mb = 20;
            int step3Mb = 2;
            int startMb = 2000;

            reqMb = FindLargestBlockMb(startMb, step1Mb);
            reqMb += step1Mb;
            reqMb = FindLargestBlockMb(reqMb, step2Mb);
            reqMb += step2Mb;
            reqMb = FindLargestBlockMb(reqMb, 1);
            return reqMb;
        }

        private static int FindLargestBlockMb(int startMb, int stepMb) {
            int reqMb;
            bool done = false;
            for (reqMb = startMb; reqMb > 1; reqMb -= stepMb) {
                MemoryFailPoint mfp = null;
                try {
                    mfp = new MemoryFailPoint(reqMb);
                    done = true;
                }
                catch (InsufficientMemoryException e) {
                    done = false;
                }
                finally {
                    if (mfp != null) {
                        mfp.Dispose();
                    }
                }
                if (done) {
                    break;
                }
            }
            return reqMb;
        }

    }
}
