using System;
using System.Collections.Generic;
using System.Text;

namespace DACarter.Utilities {

	public class Astronomy {

		public static double GetMoonAge() {

			// this formula is pretty bad
			// accuracy is only +/- 1 day

			double synodicPeriod = 29.530588853;
			//  
			//DateTime baseDateUT = new DateTime(2005, 12, 31, 3, 12, 0);
			DateTime baseDateUT = new DateTime(2005, 5, 8, 8, 45, 0);
			//DateTime newTime = new DateTime(2006, 2, 27, 17, 31, 0);
			//DateTime newTimeUT = new DateTime(2010, 11, 6, 4, 52, 0);
			DateTime nowUT = DateTime.Now.ToUniversalTime();
			TimeSpan daysOld = nowUT - baseDateUT;
			//TimeSpan daysOld2 = newTimeUT - baseDateUT;
			//double period = daysOld2.TotalDays / 60.0;
			//double age2 = daysOld2.TotalDays % synodicPeriod;
			return daysOld.TotalDays % synodicPeriod;
		}


	}
}
