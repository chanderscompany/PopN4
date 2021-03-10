using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;

namespace DACarter.NOAA {
	class DacRadiosonde1File : WindsFileBase {

		private Regex _regexDate;
		private string[] _months;

		public DacRadiosonde1File() {
			_regexDate = new Regex(@"date:(?<hhmm>\d{4})z\s+(?<dayOfMonth>\d{2})\s+(?<month>\w+)\s+(?<year2>\d{2})",
								RegexOptions.IgnoreCase |
								RegexOptions.ExplicitCapture |
								RegexOptions.Compiled);
			DateTimeFormatInfo dtInfo = new DateTimeFormatInfo();
			_months = dtInfo.AbbreviatedMonthNames;
		}

		protected override bool CustomOpenInit() {
			return base.CustomOpenInit();
		}

		protected override long CustomReadFileHeader() {
			return 0;
		}

		protected override bool CustomReadRecord(DacData data, out DateTime timeStamp, out long recordLength, out long filePositionChange) {
			//
			// The Radiosonde1 file type contains a set of header lines which include a line of the form:
			//		Date:hhmmZ dd mmm yy , where
			//			hhmm is  UTC
			//			dd is the day of month
			//			mmm is alphabetic month
			//			yy is 2-digit year
			// Data lines have 15 fields:
			//		data[0] = level index
			//		data[1] = pressure (mb)
			//		data[2] = height (m)
			//		data[3] = temperature (C)
			//		data[8] = wind direction
			//		data[9] = wind speed (kt)
			//
			string line;
			//string[] fields;

			// initialize out parameters
			timeStamp = DateTime.MaxValue;
			recordLength = 0;
			filePositionChange = 0;

			WindsData windData = data as WindsData;

			do {
				line = GetNextLine(ref recordLength, ref filePositionChange);
				if (line == null) {
					return false;
				}

			} while (!line.ToLower().Contains("date:"));

			// interpret the date line:
			Match match = _regexDate.Match(line);
			if (match.Success) {
				string dayMatch = match.Groups["dayOfMonth"].Value;
				string monthMatch = match.Groups["month"].Value;
				string yyMatch = match.Groups["year2"].Value;
				string hhmmMatch = match.Groups["hhmm"].Value;

				int monthNumber = -1;
				int dayNumber = -1;
				int yyNumber = -1;
				int hhmmNumber = -1;
				int year = -1;
				CultureInfo culture = new CultureInfo("en-us");
				for (int i = 0; i < 12; i++) {
					if (monthMatch.StartsWith(_months[i], true, culture)) {
						monthNumber = i+1;
					}
				}
				Int32.TryParse(dayMatch, out dayNumber);
				Int32.TryParse(yyMatch, out yyNumber);
				Int32.TryParse(hhmmMatch, out hhmmNumber);
				if ((monthNumber != -1) && (dayNumber != -1) && (yyNumber != -1) && (hhmmNumber != -1)) {
					int hour = hhmmNumber / 100;
					int minute = hhmmNumber % 100;
					if (yyNumber < 70) {
						year = yyNumber + 2000;
					}
					else {
						year = yyNumber + 1900;
					}
	
					timeStamp = new DateTime(year, monthNumber, dayNumber, hour, minute, 0);
				}
				else {
					timeStamp = DateTime.MinValue;
				}

			}

			// go to the data
			int dashCount = 0;
			do {
				line = GetNextLine(ref recordLength, ref filePositionChange);
				if (line == null) {
					return false;
				}
				if (line.StartsWith("---")) {
					dashCount++;
				}
			} while (dashCount < 2);

			// read the data
			const double NODATA = -999.0;
			double htm = NODATA;
			double windDir = NODATA;
			double windKt = NODATA;
			int level = -1;
			string htString = "";
			string dirString = "";
			string ktString = "";
			string levString = "";
			// we don't know how many data hts, so use list instead of array
			List<double> HtList = new List<double>(50);
			List<double> DirList = new List<double>(50);
			List<double> SpeedList = new List<double>(50);

			bool done = false;
			do {
				line = GetNextLine(ref recordLength, ref filePositionChange);
				if (line == null) {
					return false;
				}

				// Extract height, wind speed and direction
				//	if it looks like the data is there.
				//	Skip special levels except SFC.
				if (line.Trim() != string.Empty) {
					if (line.Length > 48) {
						levString = line.Substring(0, 3);
						if ((levString == "SFC") || Int32.TryParse(levString, out level)) {
							//fields = Regex.Split(line, @"\s+");
							htString = line.Substring(9, 5);
							dirString = line.Substring(42, 3);
							ktString = line.Substring(46, 3);
							if ((htString.Trim() != string.Empty) &&
								(dirString.Trim() != string.Empty) &&
								(ktString.Trim() != string.Empty))
							{
								double.TryParse(htString, out htm);
								double.TryParse(dirString, out windDir);
								double.TryParse(ktString, out windKt);
								if ((htm != NODATA) && (windDir != NODATA) && (windKt != NODATA)) {
									HtList.Add(htm/1000.0);
									DirList.Add(windDir);
									SpeedList.Add(windKt * 0.5144);
								} // end if no data
							} // end if data strings not empty
						} // end if level
					} // end if line length
				} // end if line not empty
				else {
					// reached blank line
					done = true;
				}
			} while (!done);

			if (windData != null) {
				// configure the data object
				windData.HasHorizontal = true;
				windData.HasSNR = false;
				windData.HasVertical = false;
				windData.HasWinds = true;
				windData.DataType = WindsData.WindsDataType.SpeedDirection;

				// load data object with the data
				int nhts = HtList.Count;
				windData.SetSize(nhts);
				if (nhts > 0) {
					for (int i = 0; i < nhts; i++) {
						windData.Hts[i] = (float)HtList[i];
						windData.Direction[i] = (float)DirList[i];
						windData.Speed[i] = (float)SpeedList[i];
					}

					if ((windData.DataType == WindsData.WindsDataType.SpeedDirection) ||
						(windData.DataType == WindsData.WindsDataType.Both)) {
						windData.Notes = "First ht = " + windData.Hts[0].ToString("f3") + " km," +
											" wind = " + windData.Speed[0].ToString("f2") +
											" m/s at " + windData.Direction[0].ToString("f0");
					}
					else if ((windData.DataType == WindsData.WindsDataType.Components)) {
						windData.Notes = "First ht = " + windData.Hts[0].ToString("f3") + " km," +
											" u = " + windData.UWind[0].ToString("f2") +
											" v = " + windData.VWind[0].ToString("f2");
					}
				}

				windData.TimeStamp = timeStamp;
				
			}
			return true;

		}

		private string GetNextLine(ref long recordLength, ref long filePositionChange) {
			string line1 = _TReader.ReadLine();
			if (line1 != null) {
				recordLength = _TReader.LineLength;
				filePositionChange = recordLength;
			}

			return line1;
		}

		protected override bool CustomSkipRecord(out long currentPosition) {
			throw new Exception("The method or operation is not implemented.");
		}

		protected override bool CustomReadRecordTime(out DateTime timeStamp, out long recordLength, out long filePositionChange) {
			return CustomReadRecord(null, out timeStamp, out recordLength, out filePositionChange);
		}

		protected override DacDataFileBase.OpenFileType GetOpenFileType() {
			return OpenFileType.TextReader;
		}
	}
}
