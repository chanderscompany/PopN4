using System;
using System.Text.RegularExpressions;

namespace DACarter.NOAA {
	public class DacSmoGpsFile : DacDataFileBase {

		private DateTime _baseDate;

		public DacSmoGpsFile() {
			_baseDate = new DateTime(1970, 1, 1, 0, 0, 0);
		}

		protected override long CustomReadFileHeader() {
			return 0L;
		}

		protected override bool CustomReadRecord(DacData data, out DateTime timeStamp, out long recordLength, out long filePositionChange) {

			string line;
			string[] fields;

			// initialize out parameters
			timeStamp = DateTime.MaxValue;
			recordLength = 0;
			filePositionChange = 0;

			SurfaceData surfData = data as SurfaceData;

			line = _TReader.ReadLine();
			if (line == null) {
				return false;
			}

			recordLength = _TReader.LineLength;
			filePositionChange = recordLength;

			fields = Regex.Split(line, @"\s+");

			if (fields.Length != 9) {
				return false;
			}

			int secondsSince1970;
			double buoyAzimuth;

			if (!Int32.TryParse(fields[0], out secondsSince1970)) {
				throw new Exception("Read invalid secondsSince1970 in GPS1.OUT");
			}
			if (!Double.TryParse(fields[6], out buoyAzimuth)) {
				throw new Exception("Read invalid buoyAzimuth in GPS1.OUT");
			}

			timeStamp = _baseDate.AddSeconds(secondsSince1970);

			if (surfData != null) {
				surfData.SmoHeading = buoyAzimuth;
				surfData.Notes = "Buoy Az = " + buoyAzimuth.ToString("f2");
				surfData.TimeStamp = timeStamp;
			}

			return true;
		}

		protected override bool CustomSkipRecord(out long currentPosition) {
			throw new Exception("The method or operation is not implemented.");
		}

		protected override bool CustomReadRecordTime(out DateTime timeStamp, out long recordLength, out long filePositionChange) {
			//throw new Exception("The method or operation is not implemented.");
			return CustomReadRecord(null, out timeStamp, out recordLength, out filePositionChange);
		}

		protected override DacDataFileBase.OpenFileType GetOpenFileType() {
			return OpenFileType.TextReader;
		}
	}
}
