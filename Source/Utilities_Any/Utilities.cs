using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.IO;

namespace DACarter.Utilities {
	public enum MessageLevel {
		None=0,
		_0_None=0,
		Error=1,
		_1_Error=1,
		Control=2,
		_2_Control=2,
		Info=3,
		_3_Info=3,
		Data=4,
		_4_Data=4,
		Debug=5,
		_5_Debug=5,
		All=6,
		_6_All
	};

	public static class Tools {


		/// <summary>
		/// Returns the DateTime of the end of current time interval of the day.
		/// The day is divided up into successive intervals (timeInterval long).
		/// The return value is the end of the interval that includes currentTime.
		/// </summary>
		/// <param name="currentTime"></param>
		/// <param name="timeInterval"></param>
		/// <returns></returns>
		public static DateTime GetTimeIntervalBoundary(DateTime currentTime, TimeSpan timeInterval) {

			TimeSpan span1Hr = new TimeSpan(0, 1, 0, 0);
			TimeSpan span1Day = new TimeSpan(1, 0, 0, 0);
			TimeSpan span1Min = new TimeSpan(0, 0, 1, 0);

			DateTime searchTime;
			TimeSpan roundedTimeInterval;
			bool searchBackwards = false;

			if (timeInterval < TimeSpan.Zero) {
				timeInterval = timeInterval.Duration();
				searchBackwards = true;
			}

			if (timeInterval < span1Min) {
				int boundary = timeInterval.Seconds;
				if (boundary == 0) {
					// if interval < 1 second, round up to 1 sec
					boundary = 1;
				}
				roundedTimeInterval = new TimeSpan(0, 0, 0, boundary);
				// truncate current time to previous timeInterval second boundary
				int prevSecond = (currentTime.Second / boundary) * boundary;
				searchTime = new DateTime(currentTime.Year,
											currentTime.Month,
											currentTime.Day,
											currentTime.Hour,
											currentTime.Minute,
											prevSecond);
			}
			else if (timeInterval < span1Hr) {
				int boundary = timeInterval.Minutes;
				roundedTimeInterval = new TimeSpan(0, 0, boundary, 0);
				// truncate current time to previous timeInterval minute boundary
				int prevMinute = (currentTime.Minute / boundary) * boundary;
				searchTime = new DateTime(currentTime.Year,
											currentTime.Month,
											currentTime.Day,
											currentTime.Hour,
											prevMinute,
											0);
			}
			else if (timeInterval < span1Day) {
				int boundary = timeInterval.Hours;
				roundedTimeInterval = new TimeSpan(0, boundary, 0, 0);
				// truncate current time to previous timeInterval hour boundary
				int prevHour = (currentTime.Hour / boundary) * boundary;
				searchTime = new DateTime(currentTime.Year,
											currentTime.Month,
											currentTime.Day,
											prevHour,
											0,
											0);
			}
			else {
				// truncate current time to previous day boundary
				roundedTimeInterval = new TimeSpan(timeInterval.Days, 0, 0, 0);
				searchTime = new DateTime(currentTime.Year,
											currentTime.Month,
											currentTime.Day,
											0, 0, 0);
			}

			if (searchBackwards) {
				// if moving backwards, use this searchTime as is
				//	unless it is the same as current time,
				//	then go back another interval.
				if (searchTime == currentTime) {
					searchTime -= roundedTimeInterval;
				}
			}
			else {
				// if moving forward, 
				// move timeInterval forward
				searchTime += roundedTimeInterval;
			}
			return searchTime;

		}

		/// <summary>
		/// Split a string into an array of the items in the string
		///		that are separated by white space.
		///	The standard String.Split(string, " ") does not work as intended
		///		because multiple space chars separate multiple (null) items.
		///	That is, we want "A  B" to split int "A" and "B", not "A","","B"
		/// </summary>
		/// <param name="ss"></param>
		/// <returns></returns>
		public static string[] SplitWhiteSpace(string ss) {
			return Regex.Split(ss.Trim(), @"\s+");
		}

		/// <summary>
		/// Convert a decimal or hexadecimal string to an Int32;
		/// Hexadecimal numbers start with "0x" or "0X"
		/// </summary>
		/// <param name="regText"></param>
		/// <returns></returns>
		public static Int32 ConvertDecHexString(string regText) {
			Int32 register;
			regText = regText.Trim().ToLower();
			if (regText.StartsWith("0x")) {
				// hexadecimal 
				regText.Remove(0, 2);
				register = Convert.ToInt32(regText, 16);
			}
			else {
				// decimal
				register = Convert.ToInt32(regText, 10);
			}
			return register;
		}

		/// <summary>
		/// Round a double to the specified number of places
		///		to the right of the decimal point.
		/// </summary>
		/// <param name="num"></param>
		/// <param name="places"></param>
		/// <returns></returns>
		public static double RoundToDecimalPlaces(double num, int places) {
			double factor = Math.Pow(10.0, places);
			return Math.Floor(num * factor + 0.5) / factor;
		}

		public static double RoundToSignificantDigits(double num, int places) {
			int leftPlaces = (int)Math.Ceiling( Math.Log10(num));
			return RoundToDecimalPlaces(num, places - leftPlaces);
		}

        [DllImport("shlwapi.dll", SetLastError = true)]
        private static extern int PathRelativePathTo(StringBuilder pszPath,
            string pszFrom, int dwAttrFrom, string pszTo, int dwAttrTo);

        private const int FILE_ATTRIBUTE_DIRECTORY = 0x10;
        private const int FILE_ATTRIBUTE_NORMAL = 0x80;

        public static string GetRelativePath(string fromPath, string toPath) {
            StringBuilder path = new StringBuilder(260);
            if (PathRelativePathTo(path, fromPath, FILE_ATTRIBUTE_DIRECTORY, toPath, FILE_ATTRIBUTE_DIRECTORY) == 0) {
                throw new ArgumentException("Target folder and base folder must have common prefix.");
            }
            return path.ToString();
        }

        /// <summary>
        /// Given the current folder and a full or relative target folder name,
        ///     output the full path of that folder and the relative (to current folder) path of that folder.
        /// </summary>
        /// <param name="currentFolder"></param>
        /// <param name="targetFolder"></param>
        /// <param name="fullFolderPath"></param>
        /// <param name="relFolderPath"></param>
        public static void GetFullRelPath(string currentFolder, string targetFolder, out string fullFolderPath, out string relFolderPath) {

            if (string.IsNullOrWhiteSpace(targetFolder)) {
                targetFolder = @".\";
            }

            if (Path.IsPathRooted(targetFolder)) {
                // parxFolder is a full path
                fullFolderPath = targetFolder;
                try {
                    relFolderPath = Tools.GetRelativePath(currentFolder, fullFolderPath);
                }
                catch (Exception ex) {
                    // full path given does not have common prefix with _currentDirectory
                    relFolderPath = fullFolderPath;
                }
            }
            else {
                // is relative path
                relFolderPath = targetFolder;
                fullFolderPath = Path.Combine(currentFolder, relFolderPath);
                if (fullFolderPath.EndsWith("\\")) {
                    // since StartupPath() returns string without slash at end
                    //  make that the standard for other folders
                    //fullFolderPath = fullFolderPath.Substring(0, fullFolderPath.Length - 1); ;
                }

                // do this to simplify possible navigation in path (e.g. ..\..):
                fullFolderPath = Path.GetFullPath(fullFolderPath);

                relFolderPath = Tools.GetRelativePath(currentFolder, fullFolderPath);

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static bool IsPowerOf2(int size) {

            int power = 0;
            int tsize = size;
            while (tsize > 1) {
                power++;
                tsize /= 2;
            }
            return (size == (int)Math.Pow(2.0, power));
        }

        /// <summary>
        /// Returns the power of 2 that is equal to or greater than npts
        /// </summary>
        /// <param name="npts"></param>
        /// <returns></returns>
        public static int NextPowerOf2(int npts) {

            int exp = 0;
            int tsize = npts;
            while (tsize > 1) {
                exp++;
                tsize /= 2;
            }
            int pow2 = (int)Math.Pow(2.0, exp);
            if (npts == pow2) {
                return npts;
            }
            else {
                return 2*pow2;
            }
        }
	}

}
