using System;
using System.Text.RegularExpressions;

namespace DACarter.Utilities
{
	/// <summary>
	/// 
	/// </summary>
	public class GpsParser {
		private Regex _gprmcRegEx, _gpggaRegEx;
		private string _gpsLine;
		private DateTime _gpsDateTime;
		private bool _gpsIsValid;
		private double _gpsLatitude, _gpsLongitude, _gpsAltitude;
		private int _gpsSatellites;
		private double _gpsMagVar;
		private double _gpsSOG, _gpsCOG;

		public GpsParser() {
			// 
			// TODO: Add constructor logic here
			//
			_gprmcRegEx = new Regex(@"^\$GPRMC,(?<UTC>[^,]*),(?<status>[^,]*),"+
				@"(?<latitude>[^,]*),(?<latSign>[^,]*),"+
				@"(?<longitude>[^,]*),(?<longSign>[^,]*),"+
				@"(?<speed>[^,]*),(?<heading>[^,]*),(?<date>[^,]*),"+
				@"(?<magVar>[^,]*),(?<magVarSign>[^,*]*)", 
				RegexOptions.ExplicitCapture | 
				RegexOptions.Compiled);	

			_gpggaRegEx = new Regex(@"^\$GPGGA,(?<UTC>[^,]*),(?<latitude>[^,]*),"+
				@"(?<latSign>[^,]*),(?<longitude>[^,]*),"+
				@"(?<longSign>[^,]*),(?<posFix>[^,]*),"+
				@"(?<satUsed>[^,]*),(?<hdop>[^,]*),(?<altitude>[^,]*),"+
				@"(?<altUnits>[^,]*),(?<geoidSep>[^,]*),"+
				@"(?<geoidSepUnits>[^,]*),(?<dgpsAge>[^,*]*),(?<dgpsID>[^,*]*)", 
				RegexOptions.ExplicitCapture | 
				RegexOptions.Compiled);
		}

		public void Parse(string line) {
			_gpsLine = line;
			Parse();
		}

		public void Parse() {
			bool successful;
			if (_gpsLine.StartsWith("$GPRMC")) {
				successful = parseGPRMC();
			}
			else if (_gpsLine.StartsWith("$GPGGA")) {
				successful = parseGPGGA();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		private bool parseGPRMC() {
			Match m = _gprmcRegEx.Match(_gpsLine);
			string gpsTime, gpsDate, gpsValid;

			if (m.Success) {

				// date and time
				gpsTime = m.Groups["UTC"].Value;
				gpsDate = m.Groups["date"].Value;

				try {
					if ((gpsDate.Length > 0) && (gpsTime.Length > 0)) {
						int date = Int32.Parse(gpsDate);
						int day = date/10000;
						int month = (date - day*10000)/100;
						int year = (date % 100)+2000;

						int time = (int)(Double.Parse(gpsTime));
						int hour = time/10000;
						int minute = (time-hour*10000)/100;
						int second = (time%100);
						_gpsDateTime = new DateTime(year,month,day,hour,minute,second);
					}
					else {
						_gpsDateTime = new DateTime(1,1,1,0,0,0);
					}

				}
				catch (Exception e) {
					_gpsDateTime = new DateTime(1,1,1,0,0,0);
				}

				// GPS status
				_gpsIsValid = false;
				gpsValid = m.Groups["status"].Value;
				if (gpsValid.Length > 0) {
					if (gpsValid[0] == 'A') {
						_gpsIsValid = true;
					}
				}

				// latitude and longitude
				string sLat = m.Groups["latitude"].Value;
				string sLatSign = m.Groups["latSign"].Value;
				if (sLat.Length > 0) {
					try {
						double fLat = Double.Parse(sLat);
						_gpsLatitude = (double)(((int)fLat)/100);  // whole degrees
						_gpsLatitude += (fLat - _gpsLatitude*100.0)/60.0;  // add fraction degrees
						if (sLatSign.Length>0) {
							if (sLatSign[0] == 'S') {
								_gpsLatitude = -_gpsLatitude;
							}
						}
					}
					catch (Exception e) {
						_gpsLatitude = -999.0;
					}
				}
				else {
					_gpsLatitude = -999.0;
				}

				string sLong = m.Groups["longitude"].Value;
				string sLongSign = m.Groups["longSign"].Value;
				if (sLong.Length > 0) {
					try {
						double fLong = Double.Parse(sLong);
						_gpsLongitude = (double)(((int)fLong)/100);  // whole degrees
						_gpsLongitude += (fLong - _gpsLongitude*100.0)/60.0;  // add fraction degrees
						if (sLongSign.Length>0) {
							if (sLongSign[0] == 'W') {
								_gpsLongitude = -_gpsLongitude;
							}
						}
					}
					catch (Exception e) {
						_gpsLongitude = -999.0;
					}
				}
				else {
					_gpsLongitude = -999.0;
				}

				// Speed over ground and Course over ground
				string sogValue = m.Groups["speed"].Value;
				if (sogValue.Length > 0) {
					try {
						_gpsSOG = Double.Parse(sogValue);
					}
					catch (Exception e) {
						_gpsSOG = -999.0;
					}
				}
				else {
					_gpsSOG = -999.0;
				}

				string cogValue = m.Groups["heading"].Value;
				if (cogValue.Length > 0) {
					try {
						_gpsCOG = Double.Parse(cogValue);
					}
					catch (Exception e) {
						_gpsCOG = -999.0;
					}
				}
				else {
					_gpsCOG = -999.0;
				}

				// magnetic variation
				string sMagVar = m.Groups["magVar"].Value;
				string sMagVarSign = m.Groups["magVarSign"].Value;
				if (sMagVar.Length > 0) {
					try {
						_gpsMagVar = Double.Parse(sMagVar);
						if (sMagVarSign.Length>0) {
							if (sMagVarSign[0] == 'W') 
							{
								_gpsMagVar = -_gpsMagVar;
							}
						}
					}
					catch (Exception e) {
						_gpsMagVar = -999.0;
					}
				}
				else {
					_gpsMagVar = -999.0;
				}

				return true;

			}
			else {
				// couldn't find matches in the input string
				_gpsDateTime = new DateTime(1,1,1,0,0,0);
				_gpsIsValid = false;
				_gpsLatitude = -999.0;
				_gpsLongitude = -999.0;
				_gpsMagVar = -999.0;
				return false;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns>bool indicating whether match was successful</returns>
		private bool parseGPGGA() {

			Match m = _gpggaRegEx.Match(_gpsLine);

			if (m.Success) {

				string gpsSats = m.Groups["satUsed"].Value;
				if (gpsSats.Length > 0) {
					try {
						_gpsSatellites = Int32.Parse(gpsSats);
					}
					catch (Exception e) {
						_gpsSatellites = -99;
					}
				}
				else {
					_gpsSatellites = 0;
				}

				// altitude
				string sAltitude = m.Groups["altitude"].Value;
				if (sAltitude.Length > 0) {
					try {
						_gpsAltitude = Double.Parse(sAltitude);
					}
					catch (Exception e) {
						_gpsAltitude = -999.0;
					}
				}
				else {
					_gpsAltitude = -999.0;
				}
				return true;
			}
			else {
				_gpsSatellites = -99;
				_gpsAltitude = -999.0;
				return false;
			}
		}

		//
		// Properties
		//

		public string GpsLine {
			get {return _gpsLine;}
			set {_gpsLine = value;}	
		}

		public DateTime GpsDateTime {
			get {return _gpsDateTime;}
		}

		public bool GpsIsValid {
			get {return _gpsIsValid;}
		}

		public int GpsSatellites {
			get {return _gpsSatellites;}
		}

		public double GpsLatitude {
			get {return _gpsLatitude;}
		}

		public double GpsLongitude {
			get {return _gpsLongitude;}
		}

		public double GpsAltitude {
			get {return _gpsAltitude;}
		}

		public double GpsMagVar {
			get {return _gpsMagVar;}
		}

		public double GpsSog {
			get {return _gpsSOG;}
		}

		public double GpsCog {
			get {return _gpsCOG;}
		}

	}	// end GpsParser class

}	// end namespace
