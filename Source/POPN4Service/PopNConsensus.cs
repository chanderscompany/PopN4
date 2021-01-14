using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using DACarter.Utilities;

//
// Classes to make a collection of dwell cycles (triads) to use in
//	computing ship-motion-corrected consensus.
//
// Example of how to use in data processing section of code:
//
//			private PopNConsensus _consensus;
//			private DwellData _cnsDwell;
//
//			_consensus = new PopNConsensus();
//			_consensus.useTriads = true;
//			_consensus.TriadBeams = 5;
//			_cnsDwell = new DwellData();
//
//			foreach(dwell) {
//
//				data = AcquireData();
//				//...
//				CalculateMoments(data);
//	
//				_cnsDwell.Azimuth =			...RadarPar.BeamDirections[0].Azimuth;
//				_cnsDwell.Elevation =		...RadarPar.BeamDirections[0].Elevation;
//				_cnsDwell.IppMicroSec =		...RadarPar.BeamParSet[0].IppMicroSec;
//				_cnsDwell.NCode =			...RadarPar.BeamParSet[0].NCode;
//				_cnsDwell.PulseWidthNs =	...RadarPar.BeamParSet[0].PulseWidthNs;
//				_cnsDwell.RadialDoppler =	data.MeanDoppler;
//				_cnsDwell.TimeStamp =		data.RecordTimeStamp;
//				_cnsDwell.NyquistMS =		data.Parameters.GetBeamParNyquist(0);
//				_cnsDwell.Heights =			data.Parameters.GetBeamParHeightsM(0, _cnsDwell.Elevation);
//				_consensus.Add(_cnsDwell);
//			}
//
// NOTE:
// Current version is hardwired for 5-beam dwell mode. -- FIXED
// Current version outputs triad wind vectors to file ("traids.txt", hardwired)
//		for ship correction and consensus processing by external program (see TriadsToCns project).
//


namespace POPN4Service {

	/// <summary>
	/// PopNConsensus
	/// Main class for consensus processing.
	/// Actually at the moment it does no consensus processing.
	/// It only adds dwells to a collection of triads for external processing.
	/// </summary>
	public class PopNConsensus {

		public bool useTriads;
        private int _triadBeams;
        private bool _useVerticalCorrection;

        public bool UseVerticalCorrection {
            get { return _useVerticalCorrection; }
            set { 
                _useVerticalCorrection = value;
                Triads.UseVerticalCorrection = value;
            }
        }

        public int TriadBeams {
            get { return _triadBeams; }
            set {
                _triadBeams = value;
                Triads.TriadBeams = value;
            }
        }

		private Triads Triads;
		private TimeSpan maxTimeDelta;

		public PopNConsensus() {
			maxTimeDelta = new TimeSpan(0, 3, 0);
			useTriads = true;
			_triadBeams = 5;
			Triads = new Triads();
			Triads.MaxTimeDelta = maxTimeDelta;
			Triads.TriadBeams = 5;
            Triads.UseVerticalCorrection = false;
		}

		public void Add(DwellData dwell) {
			DwellData dwellCopy = new DwellData(dwell);
			if (useTriads) {
				Triads.AddDwell(dwellCopy);
			}
		}
	}  // end class PopNConsensus

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
			//RadialDoppler = data.RadialDoppler;
            RadialDoppler = new double[data.RadialDoppler.Length];
			data.RadialDoppler.CopyTo(RadialDoppler, 0);
			NyquistMS = data.NyquistMS;
			//Heights = data.Heights;
            Heights = new double[data.Heights.Length];
			data.Heights.CopyTo(Heights, 0);
            CnsOutputFolder = data.CnsOutputFolder;
		}
		public DateTime TimeStamp;
		public double Elevation, Azimuth;
		public double IppMicroSec;
        public int PulseWidthNs;
		public int NCode;
		public double NyquistMS;		// Doppler at spectrum full scale
		public double[] Heights;
		public double[] RadialDoppler;
		public double[] SNR;
        public string CnsOutputFolder;
	}  // end class DwellData

	//////////////////////////////////////////////////////////////////
	/// <summary>
	/// Triads class
	/// A collection of Triad objects.
	/// It can add a DwellData object to the proper triad in the collection.
	/// </summary>
	class Triads {

		private List<Triad0> TriadList;
		public TimeSpan MaxTimeDelta;
		public int TriadBeams;
        public bool UseVerticalCorrection;

		public Triads() {
			TriadList = new List<Triad0>();
			TriadBeams = 5;
            UseVerticalCorrection = false;
		}

        public Triads(int nBeams) {
            TriadList = new List<Triad0>();
            TriadBeams = nBeams;
            UseVerticalCorrection = false;
        }

        public Triads(int nBeams, bool useVertCorr) {
            TriadList = new List<Triad0>();
            TriadBeams = nBeams;
            UseVerticalCorrection = useVertCorr;
        }

        /// <summary>
		/// Add dwell to the proper mode triad.
		/// If complete triad, compute UV and delete triad.
		/// </summary>
		/// <param name="dwell"></param>
		public void AddDwell(DwellData dwell) {

			// first check all triads to see if any are done
			foreach (Triad0 triad in TriadList) {
				if ((dwell.TimeStamp - triad.LastTime) > MaxTimeDelta) {
					if (!triad.IsComplete()) {
						// not complete, but too much time gap; start over
						triad.Clear();
					}
				}
				if (triad.IsComplete()) {
					triad.MakeUV();
				}
			}

			// then add new dwell
			int index = FindMatchingTriad(dwell);
			if (index < 0) {
				TriadList.Add(new Triad0(TriadBeams, UseVerticalCorrection));
				int newOne = TriadList.Count - 1;
				AddDwellToTriad(dwell, TriadList[newOne]);
			}
			else {
				AddDwellToTriad(dwell, TriadList[index]);
			}
		}

		private void AddDwellToTriad(DwellData dwell, Triad0 triad) {
			triad.Add(dwell);
			if (triad.IsComplete()) {
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
			foreach (Triad0 triad in TriadList) {
				index++;
				if (triad.MatchesMode(dwell)) {
					return index;
				}
			}
			index = -1;
			foreach (Triad0 triad in TriadList) {
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
	class Triad0 {

		private List<DwellData> _dwells;
		private int _nBeamsReq;
        private string cnsFolder;
        private TextFile _TriadWriter;
        private TextFile _TriadVCWriter;
        private string _triadLastFileName;
        private bool _useVerticalCorrection;

		public Triad0() {
			_dwells = new List<DwellData>();
			_nBeamsReq = 5;
            _useVerticalCorrection = false;
            
		}

        public Triad0(int nBeams) {
            _dwells = new List<DwellData>();
            _nBeamsReq = nBeams;
            _useVerticalCorrection = false;
        }

        public Triad0(int nBeams, bool useVertCorr) {
            _dwells = new List<DwellData>();
            _nBeamsReq = nBeams;
            _useVerticalCorrection = useVertCorr;
        }

        public void Add(DwellData data) {
			_dwells.Add(data);
            cnsFolder = data.CnsOutputFolder;
		}

		public void Clear() {
			_dwells.Clear();
		}

		public void MakeUV() {
			// assumes has passed IsComplete() test
			if (_nBeamsReq < 4) {
				//throw new ApplicationException("Cns Triad beams < 4 not supported yet.");
			}
            double[] horiz1, horiz2;
            double[] horiz1v, horiz2v;  // horiz values after vertical correction to radials
            double[] rad1a, rad2a;
			double[] vert1, vert2, vert;
			int nhts = _dwells[0].Heights.Length;
			double az1 = -999.0, az2= -999.0;
			DateTime firstTime = DateTime.MinValue;
			DateTime lastTime = DateTime.MinValue;
			TimeSpan duration = new TimeSpan(0,0,0);
			bool invert2 = false;
			int obliqueIndex = 0;

            horiz1 = new double[nhts];
            horiz2 = new double[nhts];
            horiz1v = new double[nhts];
            horiz2v = new double[nhts];
            rad1a = new double[nhts];
			rad2a = new double[nhts];
			vert = new double[nhts];
			vert1 = new double[nhts];
			vert2 = new double[nhts];
            bool[] isInvalid = new bool[nhts];

			double sigPower;
			int sigCount = 0;

            for (int i = 0; i < nhts; i++) {
                isInvalid[i] = false;
            }

			int index = -1;
            double obliqueElev = 0.0;
			foreach (DwellData dwell in _dwells) {
				index++;
				if (firstTime == DateTime.MinValue) {
					// timestamp of first dwell of dwell cycle
					firstTime = dwell.TimeStamp;
				}
				lastTime = dwell.TimeStamp;
				duration = lastTime - firstTime;

                for (int i = 0; i < nhts; i++) {
                    if (dwell.RadialDoppler[i] > 2.0) {
                        // if Doppler > 2 Nyq it is MPP missing value (3.2767)
                        isInvalid[i] = true;
                    }
                }

				if (dwell.Elevation != 90.0) {
                    obliqueElev = dwell.Elevation;
					if (az1 < 0.0) {
						obliqueIndex = index;
						az1 = dwell.Azimuth;
						if (az1 < 0.0) {
							az1 += 360.0;
						}
						dwell.RadialDoppler.CopyTo(rad1a, 0);
						for (int iht = 0; iht < nhts; iht++) {
							 rad1a[iht] = dwell.RadialDoppler[iht] * dwell.NyquistMS;
                             if (_nBeamsReq == 3) {
                                 // there will be no opposing radial beam
                                 horiz1[iht] = (rad1a[iht]) / Math.Cos(dwell.Elevation * Math.PI / 180.0);
                                 //vert1[iht] = (rad1a[iht] + dwell.RadialDoppler[iht] * dwell.NyquistMS) / 2.0 / Math.Sin(dwell.Elevation * Math.PI / 180.0);
                             }
						}
					}
					else {
						double diff1 = Math.Abs(dwell.Azimuth - az1);
						if ((diff1 == 180.0)) {
                            // combine opposing beams
							for (int iht = 0; iht < nhts; iht++) {
								horiz1[iht] = (rad1a[iht] - dwell.RadialDoppler[iht] * dwell.NyquistMS) / 2.0 / Math.Cos(dwell.Elevation * Math.PI / 180.0);
								vert1[iht] =  (rad1a[iht] + dwell.RadialDoppler[iht] * dwell.NyquistMS) / 2.0 / Math.Sin(dwell.Elevation * Math.PI / 180.0);
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
                                    if (_nBeamsReq == 3) {
                                        // there will be no opposing radial beam
                                        horiz2[iht] = (rad2a[iht]) / Math.Cos(dwell.Elevation * Math.PI / 180.0);
                                        if (invert2) {
                                            horiz2[iht] = -horiz2[iht];
                                        }
                                        //vert2[iht] = (rad2a[iht] + dwell.RadialDoppler[iht] * dwell.NyquistMS) / 2.0 / Math.Sin(dwell.Elevation * Math.PI / 180.0);
                                    }
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
									vert2[iht] =  (rad2a[iht] + dwell.RadialDoppler[iht] * dwell.NyquistMS) / 2.0 / Math.Sin(dwell.Elevation * Math.PI / 180.0);
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

            if (_nBeamsReq == 3) {
                // compute vertical correction to radials
                for (int iht = 0; iht < nhts; iht++) {
                    if (!isInvalid[iht]) {
                        double rad1 = horiz1[iht] * Math.Cos(obliqueElev * Math.PI / 180.0);
                        double rad2 = horiz2[iht] * Math.Cos(obliqueElev * Math.PI / 180.0);
                        double v1 = vert[iht];
                        double v2 = v1;
                        if (invert2) {
                            v2 = -v2;
                        }
                        horiz1v[iht] = (rad1 - v1 * Math.Sin(obliqueElev * Math.PI / 180.0)) / Math.Cos(obliqueElev * Math.PI / 180.0);
                        horiz2v[iht] = (rad2 - v2 * Math.Sin(obliqueElev * Math.PI / 180.0)) / Math.Cos(obliqueElev * Math.PI / 180.0);
                    }
                    else {
                        // invalid vert
                        horiz1v[iht] = horiz1[iht];
                        horiz2v[iht] = horiz2[iht];
                    }
                }                
            }
			string timeLine = String.Format("{0:0000} {1:000} {2:00} {3:00} {4:00} {5,5:d} {6,5:d} {7,4:d}", 
										firstTime.Year, firstTime.DayOfYear, firstTime.Hour, firstTime.Minute, firstTime.Second,
										(int)duration.TotalSeconds, _dwells[0].PulseWidthNs, (int)_dwells[0].IppMicroSec);

            string yyddd = (firstTime.Year % 100).ToString("00") + firstTime.DayOfYear.ToString("000");
            string fileName = "triads." + yyddd + ".txt";
            string fileNameVC = "triadsVC." + yyddd + ".txt";
            string filePathName = Path.Combine(cnsFolder, fileName);
            string filePathNameVC = Path.Combine(cnsFolder, fileNameVC);  // for vertical corrected radials
            
            // TODO someday, use permanent object to speed up writing
            /*
            if (_TriadWriter == null) {
                try {
                    _TriadWriter = new TextFile(filePathName, openForWriting: true, append: false);
                }
                catch (Exception ee) {
                    if (File.Exists(filePathName)) {
                        filePathName = filePathName.Replace(".txt", "x.txt");
                        _TriadWriter = new TextFile(filePathName, openForWriting: true, append: false);
                    }
                }
            }
            if (_nBeamsReq == 3) {
                if (_TriadVCWriter == null) {
                    try {
                        _TriadVCWriter = new TextFile(filePathNameVC, openForWriting: true, append: false);
                    }
                    catch (Exception ee) {
                        if (File.Exists(filePathNameVC)) {
                            filePathNameVC = filePathNameVC.Replace(".txt", "x.txt");
                            _TriadVCWriter = new TextFile(filePathNameVC, openForWriting: true, append: false);
                        }
                    }
                }
            }
            */

            string line, linev;
            double speed, dir;
            double speedv, dirv;
            double x, y;
            double xv, yv;
            for (int iht = 0; iht < nhts; iht++) {
                x = horiz1[iht];
                y = horiz2[iht];
                speed = Math.Sqrt(x * x + y * y);
                dir = Math.Atan2(y, x) * 180.0 / Math.PI + az1;
                if (dir < 0.0) {
                    dir += 360.0;
                }
                xv = horiz1v[iht];
                yv = horiz2v[iht];
                speedv = Math.Sqrt(xv * xv + yv * yv);
                dirv = Math.Atan2(yv, xv) * 180.0 / Math.PI + az1;
                if (dirv < 0.0) {
                    dirv += 360.0;
                }

                if (isInvalid[iht]) {
                    speed = 999.0;
                    dir = 999.0;
                    speedv = 999.0;
                    dirv = 999.0;
                }

                line = timeLine + String.Format(" {0,7:f0} {1,6:f2} {2,6:f1} {3,6:f2} {4,6:f2} {5,6:f2} ",
                                _dwells[obliqueIndex].Heights[iht], speed, dir, vert1[iht], vert2[iht], vert[iht]);
                linev = timeLine + String.Format(" {0,7:f0} {1,6:f2} {2,6:f1} {3,6:f2} {4,6:f2} {5,6:f2} ",
                                _dwells[obliqueIndex].Heights[iht], speedv, dirv, vert1[iht], vert2[iht], vert[iht]);
                if (_nBeamsReq == 3 && _useVerticalCorrection) {
                    DACarter.Utilities.TextFile.WriteLineToFile(filePathNameVC, linev, true);
                }
                else {
                    DACarter.Utilities.TextFile.WriteLineToFile(filePathName, line, true);
                }
                /*
                _TriadWriter.WriteLine(line);
                if (_nBeamsReq == 3) {
                    _TriadVCWriter.WriteLine(linev);
                }
                 * */
            }
			Clear();
			return;
		}

		public bool IsEmpty {
			get {
				return (_dwells.Count == 0);
			}
		}

		public bool IsComplete() {
			bool have0 = false;
			bool have90 = false;
			bool have180 = false;
			bool have270 = false;
			bool haveVert = false;
			double azimuth0 = -999.0; 
			foreach (DwellData dwell in _dwells) {
				if (dwell.Elevation != 90.0) {
					if (!have0) {
						// first oblique
						azimuth0 = dwell.Azimuth;
						have0 = true;
					}
					else {
						if (dwell.Azimuth < azimuth0) {
							dwell.Azimuth += 360.0;
						}
						if (dwell.Azimuth == azimuth0 + 90.0) {
							have90 = true;
						}
						else if (dwell.Azimuth == azimuth0 + 180.0) {
							have180 = true;
						}
						else if (dwell.Azimuth == azimuth0 + 270.0) {
							have270 = true;
						}
					}
				}
				else {
					haveVert = true;
				}
			}

			if ((_nBeamsReq != 5) && (_nBeamsReq != 3)) {
				// vertical not required
				haveVert = true;
			}

			if (_nBeamsReq > 3) {
				// need all 4 obliques
				if (have0 && have90 && have180 && have270 && haveVert) {
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
					(data.NCode == _dwells[0].NCode) &&
                    (data.Heights.Length == _dwells[0].Heights.Length)) {
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

	}  //end class Triad

	//////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// BeamDwells class
	/// Intended to be used in consensus computation.
	/// Not used or tested yet.
	/// </summary>
	class BeamDwells {

		private List<DwellData> _dwells;
		private int _dwellRequired;

		public BeamDwells() {
			_dwells = new List<DwellData>();
		}

		public BeamDwells(int minDwells) {
			_dwells = new List<DwellData>();
			_dwellRequired = minDwells;
		}

		public void Add(DwellData data) {
			_dwells.Add(data);
		}

		public void Clear() {
			_dwells.Clear();
		}

		public void DoConsensus() {
			// 
			double[] rad1a, sigPower;
			int nhts = _dwells[0].RadialDoppler.Length;
			DateTime firstTime = DateTime.MinValue;
			DateTime lastTime = DateTime.MinValue;
			TimeSpan duration = new TimeSpan(0, 0, 0);

			rad1a = new double[nhts];
			sigPower = new double[nhts];

			int count = 0;

			for (int iht = 0; iht < nhts; iht++) {
				rad1a[iht] = 0.0;
				sigPower[iht] = 0.0;
			}
			foreach (DwellData dwell in _dwells) {
				count++;
				if (firstTime == DateTime.MinValue) {
					firstTime = dwell.TimeStamp;
				}
				lastTime = dwell.TimeStamp;
				duration = lastTime - firstTime;

				for (int iht = 0; iht < nhts; iht++) {
					rad1a[iht] += dwell.RadialDoppler[iht] * dwell.NyquistMS;
				}

			}
			Clear();
			return;
		}

		public bool IsEmpty {
			get {
				return (_dwells.Count == 0);
			}
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
					(data.NCode == _dwells[0].NCode) &&
					(data.Elevation == _dwells[0].Elevation) &&
					(data.Azimuth == _dwells[0].Azimuth) &&
					(data.Heights[0] == _dwells[0].Heights[0])) {
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


	}  // end class BeamDwells

}
