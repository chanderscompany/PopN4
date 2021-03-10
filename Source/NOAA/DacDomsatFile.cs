using System;
using System.Text.RegularExpressions;

using DACarter.Utilities;

namespace DACarter.NOAA
{
	/// <summary>
	/// Summary description for DacDomsatFile.
	/// </summary>
	public class DacDomsatFile : WindsFileBase
	{
		private static Regex regexDomsatHeader = new Regex(@"^(?<site>[a-f,0-9]{8})(?<year>\d\d)(?<day>\d\d\d)(?<hour>\d\d)(?<minute>\d\d)(?<second>\d\d)",
			RegexOptions.IgnoreCase |
			RegexOptions.ExplicitCapture | 
				RegexOptions.Compiled);

		public DacDomsatFile()
		{
			//
			// Add constructor logic here
			//
		}

		protected override long CustomReadFileHeader() {
			return 0;
		}

		protected override OpenFileType GetOpenFileType() {
			return OpenFileType.TextReader;
		}

		/*
			Proprietary message header
		   gzary[0] = ngzhd;                            // words in header 
		   gzary[1] = pbdate->tm_year;
		   gzary[2] = pbdate->tm_yday + 1;
		   gzary[3] = pbdate->tm_mon + 1;
		   gzary[4] = pbdate->tm_mday;
		   gzary[5] = pbdate->tm_hour;
		   gzary[6] = info->cnsavgmin;     /* cns avg time in minutes 
		   gzary[7] = info->cnscnt;        /* number of profiles used 
		   gzary[8] = vscmms;              /* vel scale mm/sec 
		   gzary[9] = (int)(info->pw*.15); /* pw in meters range 
		   gzary[10] = 1;                  /* sample groups 
		   gzary[11] = nhts;               /* sample hts 
		   gzary[12] = (int)(ht[0]*1000.0);   /* 1st ht in meters 
		   if (nhts <= 1)
			  gzary[13] = 0;               /* spacing 
		   else
			  gzary[13] = (int)(1000.0*(ht[nhts-1]-ht[0])/(nhts-1));  /* spacing 
		   gzary[14] = gzary[11];
		   gzary[15] = gzary[12];
		   gzary[16] = gzary[13];
		   gzary[17] = 999;                 /* GOES error code last tx 
		   gzary[18] = pbdate->tm_min;		// minute added msg type #2 DAC 960711
		   gzary[19] = info->freq;          // TX freq MHz added msg type #4 980205
		   gzary[20] = 0;
		   gzary[21] = 0;
		   gzary[22] = 999;              /* E SNR at ht #5 
		   gzary[23] = 999;              /* N SNR at ht #5 
		   gzary[24] = 999;              /* E noise at top ht 
		   gzary[25] = 999;              /* N noise at top ht 
		*/

		protected override bool CustomReadRecord(DacData data, out DateTime timeStamp, out long recordLength, out long filePositionChange) {
			string line;
			recordLength = 0;
			filePositionChange = 0;
			timeStamp = DateTime.MinValue;

			// read first line of DOMSAT message (header from DOMSAT)
			line = ReadNonBlankLine(ref recordLength);
			if (line == null) {
				return false;
			}
			if (line[0] == '#') {
				line = ReadNonBlankLine(ref recordLength);
			}
			Match match = regexDomsatHeader.Match(line);
			if (match.Success) {
				string siteMatch = match.Groups["site"].Value;
				string yearMatch = match.Groups["year"].Value;
				string dayMatch = match.Groups["day"].Value;
				string hourMatch = match.Groups["hour"].Value;
				string minuteMatch = match.Groups["minute"].Value;
				string secondMatch = match.Groups["second"].Value;
				int year = Int32.Parse(yearMatch);
				if (year < 80) {
					year += 2000;
				}
				else if (year < 200) {
					year += 1900;
				}
				int day = Int32.Parse(dayMatch);
				int hour = Int32.Parse(hourMatch);
				int minute = Int32.Parse(minuteMatch);
				int second = Int32.Parse(secondMatch);
				timeStamp = DacDateTime.FromDayOfYear(year, day, hour, minute, second);
			}
			else {
				throw new System.Exception("Cannot Parse DOMSAT header.");
			}
			if (data != null) {
				data.Notes = line;
			}

			// read rest of message
			string dataMsg = "";
			do {
				line = ReadNonBlankLine(ref recordLength);
				if (line == null) {
					break;
				}
				if (line.StartsWith(@"\D")) {
					// AL Proprietary coded message
					dataMsg = line;
					do {
						line = ReadNonBlankLine(ref recordLength);
						dataMsg += line;
						if (line.EndsWith("=")) {
							break;
						}
					} while (true);

					int[] header = new int[26];
					for (int i=0; i<26; i++) {
						header[i] = DecodeAscii(dataMsg, 3*i+2);
					}
				}

			} while (line[0] != '#');

			filePositionChange = recordLength;
			if ((line == null) && recordLength==0){
				// for single-record domsat files, we
				// don't neccesarily know when we are done with record
				// (there is no '#')
				// so if we have read any bytes at all before EOF
				// then call this a complete record
				return false;
			}
			else {
				return true;
			}
		}

		private short DecodeAscii(string dataMsg, int index) {
			string triplet = dataMsg.Substring(index,3);
			ushort i0 = (ushort)(triplet[0] & 0x3f);
			ushort i1 = (ushort)(triplet[1] & 0x3f);
			ushort i2 = (ushort)(triplet[2] & 0x3f);
			ushort temp=0;
			temp = (ushort)((0xf000)&(i0 << 12));
			temp |= (ushort)((0x0fc0)&(i1 << 6));
			temp |= (ushort)((0x003f)&(i2));
			return (short)(temp);
		}

		private string ReadNonBlankLine(ref long recLength) {
			string line;
			//int lineLength = 0;
			do {
				line = _TReader.ReadLine();
				recLength += _TReader.LineLength;
				if (line == null) {
					break;
				}
				line = line.Trim(" ".ToCharArray());
			} while (line == String.Empty);
			return line;
		}

		protected override bool CustomReadRecordTime(out DateTime timeStamp, out long recordLength, out long filePositionChange) {
			WindsData data = null;
			return CustomReadRecord(data, out timeStamp, out recordLength, out filePositionChange);
		}

		protected override bool CustomSkipRecord(out long currentPosition) {
			throw new NotImplementedException();
		}
	}
}
