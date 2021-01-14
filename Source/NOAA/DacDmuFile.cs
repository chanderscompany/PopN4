using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using DACarter.Utilities;

namespace DACarter.NOAA 
{
	class DacDmuFile : DacDataFileBase
	{
		private DateTime _baseTime;
		private int _baseYear, _baseDoy, _baseHour, _baseMinute, _baseSecond;

		private enum DmuLineType {
			Data,
			Day,
			StartTime,
			EndTime,
			NewMinute,
			Nothing,
			EOF,
			Unknown
		}

		public DacDmuFile() {
			_baseYear = -1;
			ClearBaseDate();
		}

		private void ClearBaseDate() {
			_baseTime = DateTime.MinValue;
			_baseDoy = -1;
			ClearBaseTime();
		}

		private void ClearBaseTime() {
			_baseHour = -1;
			_baseMinute = -1;
			_baseSecond = -1;
		}

		private void UpdateBaseDate() {
			if ((_baseYear > 0) && (_baseDoy > 0) && (_baseHour >= 0) && (_baseMinute >= 0) && (_baseSecond >= 0)) {
				_baseTime = DacDateTime.FromDayOfYear(_baseYear, _baseDoy, _baseHour, _baseMinute, _baseSecond);
			}
			else {
				_baseTime = DateTime.MinValue;
			}
		}

		private string ReadNextDmuLine(out DmuLineType lineType, out string[] fields) {
			string line;
			line = _TReader.ReadLine();
			if (line == null) {
				lineType = DmuLineType.EOF;
				fields = null;
				return line;
			}

			string lineTrimmed = line.Trim();

			string startLabel = "Start Time:";
			string julianLabel = "Julian day";
			string endLabel = "End Time:";
			int julianDayIndex = line.IndexOf(julianLabel, StringComparison.OrdinalIgnoreCase);
			int startIndex = line.IndexOf(startLabel, StringComparison.OrdinalIgnoreCase);
			int endIndex = line.IndexOf(endLabel, StringComparison.OrdinalIgnoreCase);
			fields = Regex.Split(line, @"\s+");

			if (lineTrimmed.Length == 0) {
				lineType = DmuLineType.Nothing;
			}
			else if (fields.Length == 14) {
				lineType = DmuLineType.Data;
			}

			else if (julianDayIndex >= 0) {
				ClearBaseDate();
				string dateString = line.Substring(julianDayIndex + julianLabel.Length);
				Int32.TryParse(dateString, out _baseDoy);
				lineType = DmuLineType.Day;
			}

			else if (startIndex >= 0) {
				string timeString = line.Substring(startIndex + startLabel.Length);
				string[] timeFields = timeString.Split(':');
				if (timeFields.Length == 3) {
					_baseHour = Int32.Parse(timeFields[0]);
					_baseMinute = Int32.Parse(timeFields[1]);
					_baseSecond = Int32.Parse(timeFields[2]);
					UpdateBaseDate();
					lineType = DmuLineType.StartTime;
				}
				else {
					lineType = DmuLineType.Unknown;
				}
			}

			else if (line[0] == '#') {
				string timeString = line.Substring(2);
				string[] timeFields = timeString.Split(' ');
				if (timeFields.Length == 3) {
					_baseHour = Int32.Parse(timeFields[0]);
					_baseMinute = Int32.Parse(timeFields[1]);
					_baseSecond = Int32.Parse(timeFields[2]);
					UpdateBaseDate();
					lineType = DmuLineType.NewMinute;
				}
				else {
					lineType = DmuLineType.Unknown;
				}
			}

			else if (endIndex >= 0) {
				lineType = DmuLineType.EndTime;
			}

			else {
				lineType = DmuLineType.Unknown;
			}

			return line;
		}

		protected override long CustomReadFileHeader() {

			//throw new Exception("The method or operation is not implemented.");

			// only place to get year is from the file name
			ParsedFileNameStruct info;
			GetFileTypeFromName(this.FileName, out info);
			_baseYear = info.TimeStamp.Year;

			// read header up to first "Start Time:"
			long position = 0;
			string line;
			string[] fields;
			DmuLineType lineType;
			do {
				line = ReadNextDmuLine(out lineType, out fields);
				position += _TReader.LineLength;
			} while ((lineType != DmuLineType.EOF) && (lineType != DmuLineType.StartTime));

			return position;
		}

		protected override bool CustomReadRecord(DacData data, out DateTime timeStamp, out long recordLength, out long filePositionChange) {
			//throw new Exception("The method or operation is not implemented.");

			string line;
			DmuLineType lineType;
			string[] fields;

			// initialize out parameters
			timeStamp = DateTime.MaxValue;
			recordLength = 0;
			filePositionChange = 0;

			SurfaceData surfData = data as SurfaceData;

			bool needAnother = false;

			do {
				needAnother = false;
				line = ReadNextDmuLine(out lineType, out fields);

				if (lineType == DmuLineType.EOF) {
					timeStamp = DateTime.MaxValue;
					//data = null;
					recordLength = 0;
					filePositionChange = 0;
					return false;
				}
				else {
					recordLength += _TReader.LineLength;
					filePositionChange = recordLength;
					if (lineType == DmuLineType.Data) {
						int millisec;
						if (Int32.TryParse(fields[13], out millisec)) {
							timeStamp = _baseTime.AddMilliseconds(millisec);
							// timestamp inside DMU file is 1 day early
							timeStamp = timeStamp.AddDays(1);
							if (surfData != null) {
								surfData.Notes = "Direction = " + fields[2];
								surfData.DmuHeading = -9999.0;
								Double.TryParse(fields[2], out surfData.DmuHeading);
								surfData.TimeStamp = timeStamp;
							}
						}
						return true;
					}
					else {
						needAnother = true;
						if (lineType == DmuLineType.Unknown) {
							throw new Exception("DacDmuFile has read a line of Unknown type");
						}
					}
				}
			} while (needAnother);
			return true;
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
