using System;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Collections.Generic;

using DACarter.Utilities;

namespace DACarter.NOAA
{
	/// <summary>
	/// Summary description for DacPdtFile.
	/// </summary>
	public class DacPdtFile : WindsFileBase
	{
		private string _header = "";
		private StringDictionary HeaderLines;
		private List<string> DataLabelList;
		private List<string> DataUnitsList;
		private List<string> DataFormatList;
		private List<string> DataMissingStringList;
		private List<double> DataMissingNumberList;
		private int _nDataColumns;

		private int _maxHts;
		private int _vertScales;
		private int _uIndex;
		private int _vIndex;
		private int _wIndex;
		private int _htIndex;
		private int _whtIndex;
		private int _snr1Index;
		private int _snr2Index;
		private int _snr3Index;

		public DacPdtFile()
		{
			HeaderLines = new StringDictionary();
			DataLabelList = new List<string>(30);
			DataUnitsList = new List<string>(30);
			DataFormatList = new List<string>(30);
			DataMissingNumberList = new List<double>(30);
			DataMissingStringList = new List<string>(30);
			_maxHts = -1;
			_vertScales = 0;
			_nDataColumns = -2;
			_uIndex = _vIndex = _wIndex = -2;
			_snr1Index = _snr2Index = _snr3Index = -2;
			_htIndex = _whtIndex = -2;
		}


		protected override bool CustomOpenInit() {
			return BaseOpenInit();
		}


		protected override OpenFileType GetOpenFileType() {
			return OpenFileType.TextReader;
		}

		// read file header after opening; return file position
		protected override long CustomReadFileHeader() {
				string line;
				long position = 0;
				line = _TReader.ReadLine();
				position += _TReader.LineLength;
				_header = line + '\n' ;
				char[] delim = new char[] {':'};
				string[] fields = line.Split(delim);
				int hlines = 1;
				if (fields[0].ToLower() == "#header lines") {
					hlines = Int32.Parse(fields[1]);
				}
				for (int i = 0; i < hlines-1; i++) {
					line = _TReader.ReadLine();
					position += _TReader.LineLength;
					if (line != null) {
						_header += line + '\n';
					}
				}
				//_bufferPosition = position;
				if (_header.Length > 0) {
					InitFromHeader(_header);
				}
				return position;
		}
		
		protected override bool CustomReadRecord(DacData data, out DateTime timeStamp, out long recordSize, out long filePositionChange) {
			string line;
			PdtData pdtData;
			int htIndex = 0;

			// initialize out parameters
			timeStamp = DateTime.MaxValue;
			recordSize = 0;
			filePositionChange = 0;

			pdtData = data as PdtData;
			// initialize data object
			if (pdtData != null) {

				pdtData.HasHorizontal = GetHorizontal;
				pdtData.HasVertical = GetVertical;
				pdtData.HasSNR = GetSNR;
				pdtData.HasWinds = GetWinds;
				if ((_vertScales == 2) && GetVertical) {
					pdtData.HasVerticalHts = true;
				}
				else {
					pdtData.HasVerticalHts = false;
				}
				pdtData.DataType = WindsData.WindsDataType.Components;
				pdtData.StationName = HeaderLines["Station"];
				pdtData.HdrDescription = HeaderLines["Description"];
				pdtData.HdrWMO = HeaderLines["WMO Number"];
				pdtData.RadarID = -1;
				pdtData.PulseWidthM = -1;
				pdtData.AltitudeKm = Double.Parse(HeaderLines["Altitude"])/1000.0;
				pdtData.LatitudeN = Double.Parse(HeaderLines["North latitude"]);
				pdtData.LongitudeE = Double.Parse(HeaderLines["East longitude"]);
				pdtData.SetSize(_maxHts);
			}

			// read first line, either from buffer or directly from file
			// recordSize is set to the length of the line.
			base.GetFirstLineOfRecord(out line, out recordSize, out filePositionChange);

			if (line == null) {
				// probably EOF
				timeStamp = DateTime.MaxValue;
				data = null;
				recordSize = 0;
				return false;
			}

			timeStamp = GetLineTime(line);
			ParseDataLine(line, pdtData,  htIndex++);
			if (pdtData != null) {
				pdtData.Notes = line.Substring(0, 63);
			}

			// read the rest of the record:

			DateTime lineTime;
			do {
				line = _TReader.ReadLine();
				if (line == null) {
					break;	// EOF
				}
				else {
					filePositionChange += _TReader.LineLength;
					lineTime = GetLineTime(line);
					if (lineTime == timeStamp) {
						recordSize += _TReader.LineLength;
						ParseDataLine(line, pdtData, htIndex++);
					}
				}
			} while (lineTime == timeStamp);

			if (pdtData != null) {
				pdtData.NHts = htIndex;
			}

			// adjust the contents of the line buffer for the next record, if necessary
			base.UpdateLineBuffer(line, filePositionChange);

			return true;
		}

		private void ParseDataLine(string line, PdtData data,  int htIndex) {
			//htIndex++;
			if (data == null) {
				return;
			}
			string[] dataParams = Regex.Split(line, @"\s+");	// split line into space delimited items
			if (dataParams.Length < _nDataColumns) {
				throw new Exception("Not enough items in data line.");
			}
			else {
				if (this.GetWinds && this.GetHorizontal) {
					data.UWind[htIndex] = float.Parse(dataParams[_uIndex]);
					data.VWind[htIndex] = float.Parse(dataParams[_vIndex]);
				}
				if (this.GetWinds && this.GetVertical) {
					data.WWind[htIndex] = float.Parse(dataParams[_wIndex]);
				}
				if (this.GetSNR && this.GetHorizontal) {
					float missing1 = (float)DataMissingNumberList[_snr1Index];
					float missing2 = (float)DataMissingNumberList[_snr2Index];
					float snr1 = float.Parse(dataParams[_snr1Index]);
					float snr2 = float.Parse(dataParams[_snr2Index]);
					if ((snr1 == missing1) && (snr1 == missing1)) {
						data.ObliqueSNR[htIndex] = missing1;
					}
					else if (snr1 == missing1) {
						data.ObliqueSNR[htIndex] = snr2;
					}
					else if (snr2 == missing2) {
						data.ObliqueSNR[htIndex] = snr1;
					}
					else {
						double lin1 = Math.Pow(10.0, snr1 / 10.0);
						double lin2 = Math.Pow(10.0, snr2 / 10.0);
						double snr = 10.0 * Math.Log10((lin1 + lin2) / 2.0);
						data.ObliqueSNR[htIndex] = (float)snr;
					}
				}
				if (this.GetSNR && this.GetVertical) {
					float missing = (float)DataMissingNumberList[_snr3Index];
					float snr = float.Parse(dataParams[_snr3Index]);
					data.VerticalSNR[htIndex] = (float)snr;
				}

				data.Hts[htIndex] = float.Parse(dataParams[_htIndex]) / 1000.0f;
				if (GetVertical && _vertScales == 2) {
					data.VertHts[htIndex] = float.Parse(dataParams[_whtIndex]) / 1000.0f;
				}
			}
		}

		protected override bool CustomSkipRecord(out long currentPosition) {
			throw new NotImplementedException();
		}

		protected override bool CustomReadRecordTime(out DateTime timeStamp, out long recSize, out long filePositionChange) {
			WindsData data = null;
			return CustomReadRecord(data, out timeStamp, out recSize, out filePositionChange);
		}



		private DateTime GetLineTime(string line) {
			DateTime timeStamp;
			//char[] delim = new char[] {' ',':',';'};
			//string[] fields = line.Split(delim);
			//string[] fields = line.Split(' ',':',';');
			string[] fields = Regex.Split(line, @"\s+");
	
			int year = 1;
			int day = 1;
			int hour = 0;
			int minute = 0;
			int second = 0;
	
			if (fields.Length >= 3) {
				try {
					year = Int32.Parse(fields[4]);
					day = Int32.Parse(fields[5]);
					hour = Int32.Parse(fields[6]);
					minute = Int32.Parse(fields[7]);
					second = 0;
				}
				catch (Exception e) {
					throw new ApplicationException("Parse Time error", e);
				}
			}
			else {
				throw new ApplicationException("Line Format error");
			}
	
			timeStamp = DacDateTime.FromDayOfYear(year, day, hour, minute, second);
			return timeStamp;
		}


		/// <summary>
		/// Extracts all information from the pdt file header.
		/// </summary>
		/// <param name="Hdr">String containing all header lines.</param>
		private void InitFromHeader(string Hdr) {

			HeaderLines.Clear();
			DataLabelList.Clear();
			DataUnitsList.Clear();
			DataFormatList.Clear();
			DataMissingNumberList.Clear();
			DataMissingStringList.Clear();
			_nDataColumns = 0;

			string headerPattern = @"^#(?<HdrLabel>[^:]+):\s*(?<HdrValue>\S[^\r\n]*)";

			string dataHeaderPattern = @"(?<DataIndex>\d+)," +
				@"\s*(?<DataLabel>[^,\(]*)" +
				@"\((?<DataUnits>[^\(\)]*)\)\s*," +
				@"\s*(?<DataFormat>[^,]*)," +
				@"\s*(?<DataMissing>[^,\r\n]*)";

			// Do a regular expression search through the header
			// to find all the header labels and their values.
			// The label is everything between the '#' at the beginning
			// of the line and the next ':'.
			// Its value is everything from the first non-whitespace character
			// after the ':' to the end of the line.
			Regex regex = new Regex(headerPattern,
				RegexOptions.Multiline |
				RegexOptions.ExplicitCapture);

			// pattern for extracting Data column elements
			Regex regex2 = new Regex(dataHeaderPattern);

			MatchCollection matches = regex.Matches(Hdr);

			// Put the found header labels and their values into a dictionary,
			// except for the "Data column" lines
			foreach (Match match in matches) {
				string hdrLabel = match.Groups["HdrLabel"].Value;
				string hdrValue = match.Groups["HdrValue"].Value;
				if (hdrLabel.ToLower() != "Data column".ToLower()) {
					HeaderLines.Add(hdrLabel, hdrValue);
				}
				else {
					Match match2 = regex2.Match(hdrValue);
					if (match2.Success) {
						int idx = Convert.ToInt32(match2.Groups["DataIndex"].Value);
						if (idx != ++_nDataColumns) {
							throw new ArgumentException("Header Data columns not in numerical order");
						}
						if (DataLabelList.Capacity < idx) {
							DataLabelList.Capacity = 2 * idx;
							DataUnitsList.Capacity = 2 * idx;
							DataFormatList.Capacity = 2 * idx;
							DataMissingNumberList.Capacity = 2 * idx;
							DataMissingStringList.Capacity = 2 * idx;
						}
						DataLabelList.Add(match2.Groups["DataLabel"].Value.Trim());
						DataUnitsList.Add(match2.Groups["DataUnits"].Value);
						DataFormatList.Add(match2.Groups["DataFormat"].Value);
						DataMissingStringList.Add(match2.Groups["DataMissing"].Value);
						// those missing values that are numbers: save to a number list
						//	so that we don't have to continually convert strings
						float result;
						bool parseOK = float.TryParse(match2.Groups["DataMissing"].Value, out result);
						if (parseOK) {
							DataMissingNumberList.Add(result);
						}
						else {
							// "missing" flag is not a number:
							DataMissingNumberList.Add(-9999.0f);
						}
					}
					else {
						throw new ArgumentException("Invalid '#Data column' header format " + hdrValue);
					}
				}
			}

			string alt = HeaderLines["Altitude"];
			string[] alt1 = alt.Split(' ');
			HeaderLines.Remove("Altitude");
			HeaderLines.Add("Altitude", alt1[0]);

			_maxHts = Convert.ToInt32(HeaderLines["Most heights"]);
			_vertScales = Convert.ToInt32(HeaderLines["Vertical scales"]);
			_uIndex = DataLabelList.IndexOf("u");
			_vIndex = DataLabelList.IndexOf("v");
			_wIndex = DataLabelList.IndexOf("w");
			_htIndex = DataLabelList.IndexOf("ht");
			_whtIndex = DataLabelList.IndexOf("wht");
			_snr1Index = DataLabelList.IndexOf("snr1");
			_snr2Index = DataLabelList.IndexOf("snr2");
			_snr3Index = DataLabelList.IndexOf("snr3");

			//int[] ary = new int[maxHts];
		}

		/*
		public void AddNextLine(string ss) {
			// this pattern extracts strings separated by whitespace or commas
			// and text including whitespace and commas inside of double quotes.
			// For now, multiple commas are treated as multiple whitespace.
			//string dataPattern = @"(?<alone>[^""\s]+)|(""(?<inside>.*)"")";
			string dataPattern = @"(?<data>[^""\s,]+)|(""(?<data>.*?)"")";
			Regex regex3 = new Regex(dataPattern, RegexOptions.ExplicitCapture);
			Match m = regex3.Match(ss);
			int column = 0;
			string value, format;
			while (m.Success) {
				column++;
				value = m.Groups["data"].Value;
				if (column <= DataFormatList.Count) {
					format = (string)DataFormatList[column - 1];
					if (format[0] == 'a') {
					}
					else if (format[0] == 'i') {
						int ii = Convert.ToInt32(value);
					}
					else if (format[0] == 'f') {
						double dd = Convert.ToDouble(value);
					}
					else {
						throw new ArgumentException("Invalid Format for data column" + column);
					}
				}
				m = m.NextMatch();
			}
		}
		*/
	}
}
