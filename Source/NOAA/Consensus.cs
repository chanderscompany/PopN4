using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace DACarter.NOAA {

	///////////////////////////////////////////////////////////////////////
	/// <summary>
	/// DwellData class
	/// Contains moment data (Doppler and SNR)
	/// and info about the data (timestamp, heights, beam parameters, etc.)
	/// for a single dwell record.
	/// </summary>
	public class DwellData {
		public DwellData() {
		}
		public DwellData(DwellData data) {
			TimeStamp = data.TimeStamp;
			Elevation = data.Elevation;
			Azimuth = data.Azimuth;
			IppMicroSec = data.IppMicroSec;
			PulseWidthNs = data.PulseWidthNs;
			RadialDoppler = data.RadialDoppler;
			//data.RadialDoppler.CopyTo(RadialDoppler, 0);
			NyquistMS = data.NyquistMS;
			Heights = data.Heights;
			//data.Heights.CopyTo(Heights, 0);
			SNR = data.SNR;
			shipCOG = data.shipCOG;
			shipSOG = data.shipSOG;
			shipHeading = data.shipHeading;
		}

		public DateTime TimeStamp;
		public double Elevation, Azimuth;
		public int IppMicroSec, PulseWidthNs;
		public int NCode;
		public double NyquistMS;		// Doppler at spectrum full scale
		public double[] Heights;
		public double[] RadialDoppler;
		public double[] SNR;

		public double shipSOG, shipCOG, shipHeading;
	}  // end class DwellData


	//////////////////////////////////////////////////////////////////
	/// <summary>
	/// Triads class
	/// A collection of Triad objects.
	/// It can add a DwellData object to the proper triad in the collection.
	/// </summary>
	class Triads {

		private List<Triad> TriadList;
		//public TimeSpan MaxTimeDelta;
		public int TriadBeams;

		public Triads() {
			TriadList = new List<Triad>();
			TriadBeams = 5;
		}

		public Triads(int nBeams) {
			TriadList = new List<Triad>();
			TriadBeams = nBeams;
		}

		/// <summary>
		/// Add dwell to the proper mode triad.
		/// If complete triad, compute UV and delete triad.
		/// </summary>
		/// <param name="dwell"></param>
		public void AddDwell(DwellData dwell) {

			// first check all triads to see if any are done
			foreach (Triad triad in TriadList) {
				if ((dwell.TimeStamp - triad.LastTime) > triad.MaxTimeDelta) {
					if (!triad.IsComplete) {
						// not complete, but too much time gap; start over
						triad.Clear();
					}
				}
				if (triad.IsComplete) {
					triad.MakeUV();
				}
			}

			// then add new dwell
			int index = FindMatchingTriad(dwell);
			if (index < 0) {
				TriadList.Add(new Triad(TriadBeams));
				int newOne = TriadList.Count - 1;
				AddDwellToTriad(dwell, TriadList[newOne]);
			}
			else {
				AddDwellToTriad(dwell, TriadList[index]);
			}
		}

		private void AddDwellToTriad(DwellData dwell, Triad triad) {
			triad.Add(dwell);
			if (triad.IsComplete) {
				triad.MakeUV();
			}
		}

		/// <summary>
		/// Find triad that matches this dwell's mode;
		/// Or find an empty triad;
		/// Or return negative value;
		/// </summary>
		/// <param name="dwell"></param>
		/// <returns>Value of index of matching triad.</returns>
		private int FindMatchingTriad(DwellData dwell) {
			int index = -1;
			foreach (Triad triad in TriadList) {
				index++;
				if (triad.MatchesMode(dwell)) {
					return index;
				}
			}
			index = -1;
			foreach (Triad triad in TriadList) {
				index++;
				if (triad.IsEmpty) {
					return index;
				}
			}
			return -1;
		}

	}  // end class Triads


	//////////////////////////////////////////////////////////////////
	/// <summary>
	/// Triad class
	/// Contains the DwellData objects that make up a single dwell cycle,
	/// one DwellData object for each beam position of the dwell cycle.
	/// MakeUV() method computes the wind vectors for a completed dwell cycle
	///		and writes to a text file, "triads.txt".
	/// </summary>
	class Triad {

		private List<DwellData> _dwells;
		private int _nBeamsReq;
		private bool have0;
		private bool have90;
		private bool have180;
		private bool have270;
		private bool haveVert;
		private double azimuth0;
		private int _vertIndex;

		public bool IsComplete;
		public TimeSpan MaxTimeDelta;

		public Triad() {
			_dwells = new List<DwellData>();
			_nBeamsReq = 5;
			Init();
		}

		public Triad(int nBeams) {
			_dwells = new List<DwellData>();
			_nBeamsReq = nBeams;
			Init();
		}

		private void Init() {
			MaxTimeDelta = new TimeSpan(0, 10, 0);
			Clear();
		}

		public void Add(DwellData data) {
			if (!this.MatchesMode(data) || this.IsDuplicate(data)) {
				throw new ApplicationException("Dwell data added to wrong triad.");
			}
			if (AssignDwell(data)) {
				_dwells.Add(data);
			}
			if (_triadIsComplete()) {
				IsComplete = true;
			}
			else {
				IsComplete = false;
			}
			if (IsComplete) {
				Compute();
			}
		}

		private void Compute() {
			//
			// oblique beams relative to ship at azimuths az1, az2, elevation e.
			// radial velocities measured in az1, az2, and vertical: r1, r2, w
			// ship heading, speed, and course during beam 1: hd1, sog1, cog1
			//
			double w;
			if ((_vertIndex >= 0) && ((_nBeamsReq != 2) && ((_nBeamsReq != 4)))) {
				// if we have a vertical and if we require a vertical
				// TODO dac: do for all hts:
				w = _dwells[_vertIndex].RadialDoppler[0];
			}
			else {
				w = 0.0;
			}
			if (_nBeamsReq > 3) {
				throw new NotImplementedException("More than 2 obliques not handles in Traid class");
			}
			int nOblique = 0;
			foreach (DwellData dwell in _dwells) {
				if (dwell.Elevation != 90.0) {
					nOblique++;
					
				}
			}

			// re = ((r2-D2)*cos(T1)-(r1-D1)*cos(T2)) / sin(T2-T1) + w*sin(e)
			// rn = ((r1-D1)*sin(T2)-(r2-D2)*sin(T1)) / sin(T2-T1) + w*sin(e)
			// where
			// D1 = w*sin(e) + ship1*cos(e)
			// D2 = w*sin(e) + ship2*cos(e)
			// ship1 = sog1*cos(cog1-T1)
			// ship2 = sog2*cos(cog2-T2)
			// T1 = az1 + hd1
			// T2 = az2 + hd2
		}

		public void Clear() {
			_dwells.Clear();
			IsComplete = false;
			have0 = false;
			have90 = false;
			have180 = false;
			have270 = false;
			if ((_nBeamsReq == 2) || (_nBeamsReq == 4)) {
				// beams == 2 or 4; vertical not required
				haveVert = true;
			}
			else {
				haveVert = false;
			}
			azimuth0 = -999.0;
			_vertIndex = -1;
		}


		public bool IsEmpty {
			get {
				return (_dwells.Count == 0);
			}
		}

		private bool _triadIsComplete() {

			if (_nBeamsReq > 3) {
				// need all 4 obliques
				if (have0 && have90 && have180 && have270 && haveVert) {
					return true;
				}
				else {
					return false;
				}

			}
			else if (_nBeamsReq == 1) {
				if (haveVert) {
					return true;
				}
				else {
					return false;
				}
			}
			else {
				// need first beam and one perpendicular
				if (have0 && have90 && haveVert) {
					return true;
				}
				else if (have0 && have270 && haveVert) {
					return true;
				}
				else {
					return false;
				}
			}
		}

		private bool AssignDwell(DwellData dwell) {
			// figure out which dwell this is
			if (dwell.Elevation != 90.0) {
				if (!have0) {
					// first oblique
					azimuth0 = dwell.Azimuth;
					have0 = true;
				}
				else {
					double newAz = dwell.Azimuth;
					if (newAz < azimuth0) {
						newAz += 360.0;
					}
					if (newAz == azimuth0 + 90.0) {
						have90 = true;
					}
					else if (newAz == azimuth0 + 180.0) {
						have180 = true;
					}
					else if (newAz == azimuth0 + 270.0) {
						have270 = true;
					}
					else if ((newAz != azimuth0) && (_nBeamsReq <= 3)) {
						// Not an orthogonal beam.
						// Assume this was intentional,
						// if we are only using 2 obliques,
						// and keep this one.
						have90 = true;
					}
					else {
						// this does not fit in anywhere,
						// don't keep;
						return false;
					}
				}
			}
			else {
				haveVert = true;
				_vertIndex = _dwells.Count;
				// we are requiring that we add this data to _dwells next.
			}
			return true;
		}

		public DateTime LastTime {
			get {
				int last = _dwells.Count - 1;
				if (last >= 0) {
					return _dwells[last].TimeStamp;
				}
				else {
					return DateTime.MaxValue;
				}
			}
		}

		public bool MatchesMode(DwellData data) {
			if (_dwells.Count > 0) {
				if ((data.IppMicroSec == _dwells[0].IppMicroSec) &&
					(data.PulseWidthNs == _dwells[0].PulseWidthNs) &&
					(data.NCode == _dwells[0].NCode)) {
					return true;
				}
				else {
					return false;
				}
			}
			else {
				return false;
			}
		}

		public bool IsDuplicate(DwellData data) {
			for (int i = 0; i < _dwells.Count; i++) {
				if ((data.Elevation == _dwells[i].Elevation) &&
					(data.Azimuth == _dwells[i].Azimuth)) {
					return true;
				}
			}
			return false;
		}

		public void MakeUV() {
			// assumes has passed IsComplete() test
			if (_nBeamsReq < 4) {
				throw new ApplicationException("Cns Triad beams < 4 not supported yet.");
			}
			double[] horiz1, horiz2;
			double[] rad1a, rad2a;
			double[] vert1, vert2, vert;
			int nhts = _dwells[0].RadialDoppler.Length;
			double az1 = -999.0, az2 = -999.0;
			DateTime firstTime = DateTime.MinValue;
			DateTime lastTime = DateTime.MinValue;
			TimeSpan duration = new TimeSpan(0, 0, 0);
			bool invert2 = false;
			int obliqueIndex = 0;

			horiz1 = new double[nhts];
			horiz2 = new double[nhts];
			rad1a = new double[nhts];
			rad2a = new double[nhts];
			vert = new double[nhts];
			vert1 = new double[nhts];
			vert2 = new double[nhts];

			//double sigPower;
			//int sigCount = 0;

			int index = -1;
			foreach (DwellData dwell in _dwells) {
				index++;
				if (firstTime == DateTime.MinValue) {
					// timestamp of first dwell of dwell cycle
					firstTime = dwell.TimeStamp;
				}
				lastTime = dwell.TimeStamp;
				duration = lastTime - firstTime;
				if (dwell.Elevation != 90.0) {
					if (az1 < 0.0) {
						obliqueIndex = index;
						az1 = dwell.Azimuth;
						if (az1 < 0.0) {
							az1 += 360.0;
						}
						dwell.RadialDoppler.CopyTo(rad1a, 0);
						for (int iht = 0; iht < nhts; iht++) {
							rad1a[iht] = dwell.RadialDoppler[iht] * dwell.NyquistMS;
						}
					}
					else {
						double diff1 = Math.Abs(dwell.Azimuth - az1);
						if ((diff1 == 180.0)) {
							for (int iht = 0; iht < nhts; iht++) {
								horiz1[iht] = (rad1a[iht] - dwell.RadialDoppler[iht] * dwell.NyquistMS) / 2.0 / Math.Cos(dwell.Elevation * Math.PI / 180.0);
								vert1[iht] = (rad1a[iht] + dwell.RadialDoppler[iht] * dwell.NyquistMS) / 2.0 / Math.Sin(dwell.Elevation * Math.PI / 180.0);
							}
						}
						else if ((diff1 == 90.0) || (diff1 == 270.0)) {
							if (az2 < 0.0) {
								az2 = dwell.Azimuth;
								if (az2 < 0.0) {
									az2 += 360.0;
								}
								double az = az1 + 90.0;
								if (az > 360.0) {
									az -= 360.0;
								}
								if (az2 == az) {
									invert2 = false;
								}
								else {
									invert2 = true;
								}
								for (int iht = 0; iht < nhts; iht++) {
									rad2a[iht] = dwell.RadialDoppler[iht] * dwell.NyquistMS;
								}
							}
							else {
								double diff2 = Math.Abs(dwell.Azimuth - az2);
								if (diff2 != 180.0) {
									throw new ApplicationException("Cns Triad MakeUV Error: diff2!=180.");
								}
								for (int iht = 0; iht < nhts; iht++) {
									horiz2[iht] = (rad2a[iht] - dwell.RadialDoppler[iht] * dwell.NyquistMS) / 2.0 / Math.Cos(dwell.Elevation * Math.PI / 180.0);
									if (invert2) {
										horiz2[iht] = -horiz2[iht];
									}
									vert2[iht] = (rad2a[iht] + dwell.RadialDoppler[iht] * dwell.NyquistMS) / 2.0 / Math.Sin(dwell.Elevation * Math.PI / 180.0);
								}
							}
						}
					}
				}
				else {
					// vertical beam
					for (int iht = 0; iht < nhts; iht++) {
						vert[iht] = dwell.RadialDoppler[iht] * dwell.NyquistMS;
					}
				}
			}
			string timeLine = String.Format("{0:0000} {1:000} {2:00} {3:00} {4:00} {5,5:d} {6,5:d} {7,4:d}",
										firstTime.Year, firstTime.DayOfYear, firstTime.Hour, firstTime.Minute, firstTime.Second,
										(int)duration.TotalSeconds, _dwells[0].PulseWidthNs, _dwells[0].IppMicroSec);
			string line;
			double speed, dir;
			double x, y;
			for (int iht = 0; iht < nhts; iht++) {
				x = horiz1[iht];
				y = horiz2[iht];
				speed = Math.Sqrt(x * x + y * y);
				dir = Math.Atan2(y, x) * 180.0 / Math.PI + az1;
				if (dir < 0.0) {
					dir += 360.0;
				}
				line = timeLine + String.Format(" {0,7:f0} {1,6:f2} {2,6:f1} {3,6:f2} {4,6:f2} {5,6:f2} ",
								_dwells[obliqueIndex].Heights[iht], speed, dir, vert1[iht], vert2[iht], vert[iht]);
				DACarter.Utilities.TextFile.WriteLineToFile("Triads.txt", line, true);
			}
			Clear();
			return;

		}  // end MakeUV()

	}  //end class Triad


}
