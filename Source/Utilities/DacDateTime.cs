using System;

namespace DACarter.Utilities {
	/// <summary>
	/// This class provides static methods to return a DateTime object
	/// based on the day of year.
	/// </summary>
	/// <remarks>This class has only static methods and cannot be instantiated.</remarks>
	public class DacDateTime  {

		/// <summary>
		/// Private constructor
		/// </summary>
		private DacDateTime(){}

		/// <summary>
		/// Returns a DateTime object representing midnight of the specified
		/// year and day of year.
		/// </summary>
		/// <param name="year">4-digit year</param>
		/// <param name="doy">Day of year (doy=1 is Jan. 1)</param>
		/// <returns>DateTime object</returns>
		public static DateTime FromDayOfYear(int year, int doy) {
			return FromDayOfYear(year, doy, 0, 0, 0);
		}

		/// <summary>
		/// Returns a DateTime object representing the time in the supplied arguments  
		/// </summary>
		/// <param name="year">4-digit year</param>
		/// <param name="doy">Day of year (doy=1 is Jan. 1)</param>
		/// <param name="hour">Hour (0-23)</param>
		/// <param name="minute">Minute (0-59)</param>
		/// <param name="second">Second (0-59)</param>
		/// <returns>DateTime object for the requested date and time.</returns>
		/// <remarks>NOTE that Day of year must be 1-366, but if not leap year, then doy=366 is changed to 365 without error.</remarks>
		public static DateTime FromDayOfYear(int year, int doy, int hour, int minute, int second) {
			int maxDoy;
			if ((doy < 1) || (doy > 366)) {
				throw new ArgumentOutOfRangeException("Day of year must be in range 1-366");
			}
			if ((doy == 366) && !DateTime.IsLeapYear(year)) {
				doy = 365;
			}
			DateTime Jan1 = new DateTime(year,1,1,hour,minute, second);		// Jan. 1 of the year
			TimeSpan days = new TimeSpan(doy-1,0,0,0);	// number of days past Jan. 1
			DateTime newDate = Jan1 + days;
			return newDate;
		}

		/// <summary>
		/// Converts a DateTime to a decimal day of year
		/// with the hour, minute, second being converted to
		/// the fractional part of the day.
		/// </summary>
		/// <param name="dt">DateTime object to convert</param>
		/// <returns>The DateTime converted to a decimal day of year</returns>
		public static double ToDecimalDay(DateTime dt) {
			double second = (double)dt.Second;
			double minute = (double)dt.Minute + second/60.0;
			double hour = (double)dt.Hour + minute/60.0;
			double day = (double)dt.DayOfYear + hour/24.0;
			return day;
		}

		/// <summary>
		/// Converts a DateTime to a decimal hour of the day
		/// with the minute and second being converted to
		/// the fractional part of the hour.
		/// </summary>
		/// <param name="dt">DateTime object to convert</param>
		/// <returns>The DateTime converted to a decimal hour of the day</returns>
		public static double ToDecimalHour(DateTime dt) {
			double second = (double)dt.Second;
			double minute = (double)dt.Minute + second/60.0;
			double hour = (double)dt.Hour + minute/60.0;
			return hour;
		}

	}

}
