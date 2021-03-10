using System;
using System.Text.RegularExpressions;

namespace DACarter.NOAA
{
	/// <summary>
	/// Summary description for DacCnsFile.
	/// </summary>
	public class DacCnsFile : WindsFileBase
	{
		private int _currentHtIndex;
		private int _verticalDataIndex;
		private int _snrDataIndex;
		private int[] _cnsThreshold;
		private const int MAXBEAMS = 20;
		private int _nBeams;

		public DacCnsFile()
		{
			_cnsThreshold = new Int32[MAXBEAMS];
		}

		protected override long CustomReadFileHeader() {
			return 0;
		}

		protected override OpenFileType GetOpenFileType() {
			return OpenFileType.TextReader;
		}

		protected override bool CustomOpenInit() {
			_currentHtIndex = 0;
			_verticalDataIndex = -1;
			_nBeams = 0;
			return base.CustomOpenInit();
		}

		protected override bool CustomReadRecord(DacData data, out DateTime timeStamp, out long recordLength, out long filePositionChange) {
			/*
				Line 1: Station Name
				Line 2: Data type (WINDS or RASS)
				Line 3: N. Latitude, W. Longitude and altitude
				Line 4: Start Year, Month, Day, Hour, Minute, Second, Minutes to add to get UTC.
				Line 5: Avg time in minutes, # beams, # hts in this record.
				Line 6: For 2 obliques, then vertical:
					# required to meet cns: total number of records (m/s cns window)
				Line 7: Parameter pairs (oblique/vertical):
					coherent avgs, spectral avgs, pulse width (ns), IPP (us)
				Line 8: Nyquist in m/s (pair), Vertical used to correct radials? (1=yes, 0=no),
					delay to first sample in ns (pair), # samples (pair), sample spacing (ns) (pair)
				Line 9: Azimuth and elevation (vertical, oblique1, oblique2)
				Line 10: Data column label
				Lines 11 thru 10+NHTS: Data
					ht (km), wind speed (m/s), wind direction, 
					radial velocities (vert, oblq1, oblq2),
					cns count (vert, oblq1, oblq2),
					snr (dB) (vert, oblq1, oblq2).
				Last Line of record: "$"

				Note: if number of beams is not 3, the pairs and trios of values above are modified accordingly.
					Parameter pairs become 1 value if only 1 beam.  Groups of 3 become groups of "#beams".
					Oblique/vertical pairs remain the same if #beams > 3

			*/
			string line;
			recordLength = 0;
			filePositionChange = 0;
			timeStamp = DateTime.MinValue;
			_currentHtIndex = 0;
			_verticalDataIndex = -1;
			_snrDataIndex = -1;

			WindsData windsData = null;

			if (data != null) {
				windsData = data as WindsData;
				if (windsData != null) {
					windsData.DataType = WindsData.WindsDataType.SpeedDirection;
				}
			}

			windsData.HasHorizontal = GetHorizontal;
			windsData.HasVertical = GetVertical;
			//windsData.HasSNR = GetSNR;
			windsData.HasSNR = true;
			windsData.HasWinds = GetWinds;
			windsData.HasVerticalHts = false;

			// read first line of header, skipping leading blank lines
			do {
				line = _TReader.ReadLine();
				if (line == null) {
					filePositionChange = recordLength;
					return false;
				}
				recordLength += _TReader.LineLength;
			//} while (line.Trim(new char[] {' '}) == " ");
			} while (line.Trim(" ".ToCharArray()) == String.Empty);

			if (data != null) {
				string site = line + " ";
				data.Notes = site;
				if (windsData != null) {
					windsData.StationName = line.Trim(' ');
				}
			}

			// read rest of header and data
			try {
				line = GetNextLine(ref recordLength);	// line 2, data type
				if (data != null) {
					data.Notes += line;
				}
				if (line.ToLower().IndexOf("winds") < 0) {
					throw new Exception("This file is not a winds cns file.");
				}
				line = GetNextLine(ref recordLength);	// line 3, location and altitude
				ParseLine3(line, windsData);
				line = GetNextLine(ref recordLength);	// line 4, date/time
				timeStamp = ParseTimeStamp(line);
				if (windsData != null) {
					// save base class member
					windsData.TimeStamp = timeStamp;
				}
				line = GetNextLine(ref recordLength);	// line 5, avgtime, nbeams, nhts
				int avgTime, nBeams, nHts;
				ParseLine5(line, out avgTime, out nBeams, out nHts, windsData);
				_nBeams = nBeams;
				_snrDataIndex = 3 + 2 * _nBeams;		// beginning  of snr data
				line = GetNextLine(ref recordLength);	// line 6, cns threshold and window
				ParseLine6(line, windsData);
				line = GetNextLine(ref recordLength);	// line 7, nci, nspec, pw, ipp
				ParseLine7(line, windsData);
				line = GetNextLine(ref recordLength);	// line 8, nyquist, vert sub
				line = GetNextLine(ref recordLength);	// line 9, beam directions
				ParseLine9(line, windsData);
				line = GetNextLine(ref recordLength);	// line 10, labels

				// read data lines:
				for (int i = 0; i < nHts; i++) {
					line = GetNextLine(ref recordLength);
					if (windsData != null) {
						// parse the data values if we were given
						//   a data object to put them into
						ParseDataLine(line, windsData);
					}
				}
				// read end of record delimiter "$"
				line = GetNextLine(ref recordLength);
				filePositionChange = recordLength;
				return true;
			}
			catch (Exception) {
				if (line == null) {
					// eof that we are not expecting
					filePositionChange = recordLength;
					return false;
				}
				else {
					filePositionChange = recordLength;
					throw;
				}
			}
		}

		private void ParseLine3(string line, WindsData windsData) {
			string[] items = Regex.Split(line, @"\s+");	// split line into space delimited items
			if (items.Length < 3) {
				throw new Exception("Not enough items in data line.");
			}
			if (windsData != null) {
				windsData.LatitudeN = Double.Parse(items[0]);
				windsData.LongitudeE = Double.Parse(items[1]);
				windsData.AltitudeKm = Double.Parse(items[2]) / 1000.0;
			}
		}

		private void ParseLine6(string line, WindsData windsData) {
			string[] items = Regex.Split(line, @":|\s+");	// split line into space and ':' delimited items
			int nbeams = items.Length / 3;
			for (int i = 0; i < nbeams; i++) {
				if (i<MAXBEAMS) {
					_cnsThreshold[i] = Int32.Parse(items[3 * i]);
				}
			}
		}

		private void ParseDataLine(string line, WindsData windsData) {
			string[] dataParams = Regex.Split(line, @"\s+");	// split line into space delimited items
			if (dataParams.Length < 3 + _nBeams*3) {
				throw new Exception("Not enough items in data line.");
			}
			else {
				if (windsData != null) {
					windsData.Hts[_currentHtIndex] = float.Parse(dataParams[0]) + (float)windsData.AltitudeKm;
					if (windsData.HasWinds) {
						if (windsData.HasHorizontal) {
							windsData.Speed[_currentHtIndex] = float.Parse(dataParams[1]);
							windsData.Direction[_currentHtIndex] = float.Parse(dataParams[2]);
						}
						if (windsData.HasVertical) {
							if ((_verticalDataIndex >= 0) && (_verticalDataIndex < MAXBEAMS)) {
								// for vertical speed, use vertical radial if count > threshold
								try {
									int cnsCount = Int32.Parse(dataParams[3 + _nBeams + _verticalDataIndex]);
									if (cnsCount >= _cnsThreshold[_verticalDataIndex]) {
										windsData.WWind[_currentHtIndex] = (-1.0f) * float.Parse(dataParams[3 + _verticalDataIndex]);
									}
									else {
										windsData.WWind[_currentHtIndex] = 9999.0f;
									}
								}
								catch (Exception) {
									throw new Exception("Cannot parse vertical velocity data.");
								}
							}
							else {
								windsData.WWind[_currentHtIndex] = 9999.0f;
							}

						}
						
					} if (windsData.HasSNR) {
						if (windsData.HasHorizontal) {
							// average 1st 2 oblique snr's if available
							float missing = -9999.0f;
							float snr1 = missing;
							float snr2 = missing;
							for (int i = 0; i < _nBeams; i++) {
								if (i == _verticalDataIndex) {
									continue;
								}
								if (snr1 == missing) {
									snr1 = float.Parse(dataParams[_snrDataIndex+i]);
								}
								else if (snr2 == missing) {
									snr2 = float.Parse(dataParams[_snrDataIndex+i]);
									break;
								}
							}
							if ((snr1 == missing) && (snr1 == missing)) {
								windsData.ObliqueSNR[_currentHtIndex] = missing;
							}
							else if (snr1 == missing) {
								windsData.ObliqueSNR[_currentHtIndex] = snr2;
							}
							else if (snr2 == missing) {
								windsData.ObliqueSNR[_currentHtIndex] = snr1;
							}
							else {
								double lin1 = Math.Pow(10.0, snr1 / 10.0);
								double lin2 = Math.Pow(10.0, snr2 / 10.0);
								double snr = 10.0 * Math.Log10((lin1 + lin2) / 2.0);
								windsData.ObliqueSNR[_currentHtIndex] = (float)snr;
							}
						}
						if (windsData.HasVertical) {
							windsData.VerticalSNR[_currentHtIndex] = float.Parse(dataParams[_snrDataIndex + _verticalDataIndex]);
						}
					}
				} 
				_currentHtIndex++;
			}
		}

		private void ParseLine5(string line, out int avgMinutes, out int nBeams, out int nHts, WindsData windsData) {

			string[] items = Regex.Split(line, @"\s+");	// split line into space delimited items
			if (items.Length != 3) {
				throw new Exception("Unexpected parameters in header line 5.");
			}
			avgMinutes = Int32.Parse(items[0]);
			nBeams = Int32.Parse(items[1]);
			nHts = Int32.Parse(items[2]);
			if (windsData != null) {
				windsData.NHts = nHts;
			}
			return;
		}

		private void ParseLine7(string line, WindsData windsData) {

			int nci, nspec, pw, ipp;
			string[] items = Regex.Split(line, @"\s+");	// split line into space delimited items
			if ((items.Length != 4) && (items.Length != 8)) {
				throw new Exception("Unexpected parameters in header line 7.");
			}
			if (items.Length == 4) {
				nci = Int32.Parse(items[0]);
				nspec = Int32.Parse(items[1]);
				pw = Int32.Parse(items[2]);
				ipp = Int32.Parse(items[3]);
			}
			else {
				nci = Int32.Parse(items[0]);
				nspec = Int32.Parse(items[2]);
				pw = Int32.Parse(items[4]);
				ipp = Int32.Parse(items[6]);
			}
			if (windsData != null) {
				windsData.PulseWidthM = (int)(pw * 0.15 + 0.5);
			}
			return;
		}

		private void ParseLine9(string line, WindsData windsData) {
			string[] items = Regex.Split(line, @"\s+");	// split line into space delimited items
			int nbeams = items.Length / 2;
			int nvert = 0;
			int nhorz = 0;
			_verticalDataIndex = -1;
			for (int i = 0; i < nbeams; i++) {
				double elev = double.Parse(items[2*i + 1]);
				if (elev > 91.0) {
					continue;
				}
				else if (elev > 89.9) {
					nvert++;
					if (_verticalDataIndex < 0) {
						// keep track of where 1st vertical data is
						_verticalDataIndex = i;
					}
				}
				else {
					nhorz++;
				}
			}
			if (windsData != null) {
				if (nvert == 0) {
					windsData.HasVertical = false;
				}
				if (nhorz < 2) {
					windsData.HasHorizontal = false;
				}
				
			}

		}

		private DateTime ParseTimeStamp(string line) {
			//string[] timeParams = line.Split(" ".ToCharArray());
			string[] timeParams = Regex.Split(line,@"\s+");	// split line into space delimited items
			if (timeParams.Length != 7) {
				throw new Exception("Unexpected parameters in header time line.");
			}
			int year = Int32.Parse(timeParams[0]);
			if (year < 80) {
				year += 2000;
			}
			else if (year < 200) {
				year += 1900;
			}
			int month = Int32.Parse(timeParams[1]);
			int day = Int32.Parse(timeParams[2]);
			int hour = Int32.Parse(timeParams[3]);
			int minute = Int32.Parse(timeParams[4]);
			int second = Int32.Parse(timeParams[5]);
			int min2UT = Int32.Parse(timeParams[6]);
			return new DateTime(year, month, day, hour, minute,second);
		}

		private string GetNextLine(ref long recordLength) {
			string nextLine;
			nextLine = _TReader.ReadLine();	// line 2
			recordLength += _TReader.LineLength;
			if (nextLine != null) {
				nextLine = nextLine.Trim(" ".ToCharArray());
			}
			return nextLine;
		}

		protected override bool CustomSkipRecord(out long currentPosition) {
			throw new NotImplementedException();
		}

		protected override bool CustomReadRecordTime(out DateTime timeStamp, out long recSize, out long FilePositionChange) {
			WindsData data = null;
			return CustomReadRecord(data, out timeStamp, out recSize, out FilePositionChange);
		}

	}
}
