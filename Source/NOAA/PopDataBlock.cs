using System;
using System.Collections;

namespace DACarter.NOAA {
	public class PopDataBlock {
		/*
		// PopData public arrays (for reference):
		public float[] Hts;			// dimension is [NHts], units are km
		public float[,,] Vel;		// dimensions are [NRx, NSets, NHTs]
		public float[,,] Snr;
		public float[,,] Noise;
		public float[,,] Width;
		public float[,,] Spectra;		// dimensions are [NRx, NHts, NPts]
		*/
		// PopDataBlock public arrays:
		public float[] Hts;
		public ArrayList Times;
		public ArrayList VelBlock;
		public ArrayList SnrBlock;
		public ArrayList NoiseBlock;
		public ArrayList WidthBlock;

		private double _azimuth;
		private double _elevation;
		private int _altitude;
		private int _pw;
		private int _ncode;
		private int _nrx;
		private int _nsets;
		private int _npts;
		private double _defaultThreshold;
		private int _stationID;
		private string _stationLabel;
		private double _nyquist;

        private bool _combineAllBeams;

        public PopDataBlock() {
            _combineAllBeams = false;
            _createArrays();
            initialize(null);
        }

        public PopDataBlock(bool combineAllBeams) {
            _combineAllBeams = combineAllBeams;
            _createArrays();
            initialize(null);
        }

        private void _createArrays() {
			int initialCapacity = 250;
			Times = new ArrayList(initialCapacity);
			VelBlock = new ArrayList(initialCapacity);
			SnrBlock = new ArrayList(initialCapacity);
			NoiseBlock = new ArrayList(initialCapacity);
			WidthBlock = new ArrayList(initialCapacity);
		}

		private void initialize(PopData data) {
			if (data == null) {
				Hts = null;
				_pw = Int16.MaxValue;
				_ncode = Int16.MaxValue;
				_nrx = 0;
				_nsets = 0;
				_npts = 0;
				_azimuth = Int16.MaxValue;
				_elevation = Int16.MaxValue;
				_altitude = Int16.MaxValue;
				_defaultThreshold = -99;
				_stationID = -1;
				_stationLabel = "??";
				//_clock = Int16.MaxValue;
			}
			else {
				if (Hts != null) {
					throw new InvalidOperationException("PopDataBlock already initialized");
				}
				if (data.Hts != null) {
                    if (_combineAllBeams) {
                        // if all beams combined into one data set, use range instead of height
                        for (int iht = 0; iht < data.NHts; iht++) {
                            data.Hts[iht] = (float)data.GetRangeKm(iht);
                        }
                    }
                    Hts = (float[])(data.Hts.Clone());
                }
				else {
					Hts = null;
				}
				_pw = data.Hdr.PW;
				_ncode = data.Hdr.NCode;
				_nrx = data.NRx;
				_nsets = data.NSets;
				_npts = data.NPts;
				_azimuth = data.Hdr.Azimuth;
				_elevation = data.Hdr.Elevation;
				_altitude = data.Hdr.Altitude;
				_defaultThreshold = data.DefaultSNRThreshold;
				_stationID = data.Hdr.RadarID;
				_stationLabel = data.StationLabel;
				_nyquist = data.Nyquist;
				//_clock = data.Hdr[(int)PopData.HdrId.Clockns];
			}
		}

		/*
		private void _initialize(Int16[] hts, Int16 az, Int16 elev,
								Int16 pw, Int16 pcode, Int16 nrx) {
			if (Hts != null) {
				throw new InvalidOperationException("PopDataBlock already initialized");
			}
			if (hts != null) {
				Hts = (Int16[])(hts.Clone());
			}
			else {
				Hts = null;
			}
			_azimuth = az;
			_elevation = elev;
			_pw = pw;
			_pcode = pcode;
			_nrx = nrx;
		}
		*/

		public bool DataIsMatch(PopData data) {
			if (Hts == null) {
				return true;
			}
			int nhts = Hts.Length;
			if (data.NHts != nhts) {
				return false;
			}
            if (!_combineAllBeams) {
                if ((data.Hdr.Azimuth != _azimuth) ||
                    (data.Hdr.Elevation != _elevation) ||
                    (data.Hts[0] != Hts[0]) ||
				    (data.Hts[nhts - 1] != Hts[nhts - 1]) ) {
                    return false;
                }
            }
			if ((data.Hdr.PW != _pw) ||
				(data.Hdr.NCode != _ncode) ||
				(data.Hdr.NRx != _nrx) ||
				(data.Hdr.NSets != _nsets) ||
				(data.Hdr.NPts != _npts) ||
				(data.Hdr.RadarID != _stationID)) {
				return false;
			}
			else {
				return true;
			}

		}

		/// <summary>
		/// Add data from one PopData object to the end of the block
		///   of data within this PopDataBlock object.
		/// Height array of PopData object must match the height array
		///   of this PopDataBlock object.
		/// </summary>
		/// <param name="data">Source PopData object</param>
		public void Add(PopData data) {
			if (!DataIsMatch(data)) {
				throw new ArgumentException("PopDataBlock: Add data type does not match data in block");
			}

			if (Hts == null) {
				initialize(data);
				/*
				_initialize(data.Ht,
							data.Hdr[(int)PopData.HdrId.Az],
							data.Hdr[(int)PopData.HdrId.Elev],
							data.Hdr[(int)PopData.HdrId.Pwclk],
							data.Hdr[(int)PopData.HdrId.PCode],
							data.Hdr[(int)PopData.HdrId.Nrx]);
				_defaultThreshold = data.DefaultSNRThreshold;
				*/
			}

			float[, ,] vel = new float[data.NRx, data.NSets, data.NHts];
			Array.Copy(data.Vel, vel, vel.Length);
			VelBlock.Add(vel);

			float[, ,] snr = new float[data.NRx, data.NSets, data.NHts];
			Array.Copy(data.Snr, snr, snr.Length);
			SnrBlock.Add(snr);

			float[, ,] wid = new float[data.NRx, data.NSets, data.NHts];
			Array.Copy(data.Width, wid, wid.Length);
			WidthBlock.Add(wid);

			float[, ,] noise = new float[data.NRx, data.NSets, data.NHts];
			Array.Copy(data.Noise, noise, noise.Length);
			NoiseBlock.Add(noise);

			Times.Add(data.TimeStamp);
		}

		/// <summary>
		/// Extracts x,y,z arrays (time, height, data value)
		/// for a particular data type 
		/// </summary>
		/// <param name="tData"></param>
		/// <param name="hData"></param>
		/// <param name="zData"></param>
		/// <param name="type"></param>
		/// <param name="snrThreshold"></param>
		/// <param name="setIndex"></param>
		public void GetTHZArray(out DateTime[] tData, out double[] hData, out double[,] zData,
								PopData.PopDataType type, int snrThreshold, int setIndex) {

			if ((setIndex < 0) || (setIndex > this.NSets - 1)) {
				throw new ArgumentException("PopDataBlock.GetTHZArray: Set number does not exist in data");
			}

			int ptCount = this.Ntimes;
			int htCount = this.Nhts;
			hData = new double[htCount];
			tData = new DateTime[ptCount];
			zData = new double[ptCount, htCount];

			// default index ranges
			int firstHtIndex = 0;
			int lastHtIndex = htCount - 1;
			int firstTimeIndex = 0;
			int lastTimeIndex = ptCount - 1;

			bool filterOn = false;
			float filterValue = float.MinValue;

			if (snrThreshold > -100.0) {
				filterOn = true;
				filterValue = (float)snrThreshold;
			}

			if (type == PopData.PopDataType.SNR) {
				if ((filterOn) && (filterValue < -59.0)) {
					// contour plot blows up if "bad" values of -60 dB are plotted
					filterValue = -59.0f;
				}
			}

			// set up height and time arrays
			for (int iht = firstHtIndex; iht <= lastHtIndex; iht++) {
				hData[iht] = this.Hts[iht];
			}

			for (int ipt = firstTimeIndex; ipt <= lastTimeIndex; ipt++) {
				tData[ipt] = (DateTime)(this.Times[ipt]);
			}

			// calculate z array
			if (type == PopData.PopDataType.NoiseLevel) {
				// get noise level without filtering
				for (int i = 0; i < ptCount; i++) {
					float[,] noiseArray;
					noiseArray = (float[,])NoiseBlock[i];
					for (int iht = firstHtIndex; iht <= lastHtIndex; iht++) {
						zData[i, iht] = noiseArray[setIndex, iht];
					}
				}
			}
			else {
				// get other data types with SNR filtering
				for (int i = 0; i < ptCount; i++) {
					float[,] snrArray;
					float[,] dataArray;
					snrArray = (float[,])SnrBlock[i];

					if (type == PopData.PopDataType.Velocity) {
						dataArray = (float[,])VelBlock[i];
					}
					else if (type == PopData.PopDataType.SNR) {
						dataArray = (float[,])SnrBlock[i];
					}
					else if (type == PopData.PopDataType.Width) {
						dataArray = (float[,])WidthBlock[i];
					}
					else {
						throw new ArgumentException("StackedPlot: unexpected plot data type.");
					}

					for (int iht = firstHtIndex; iht <= lastHtIndex; iht++) {
						if (filterOn && (snrArray[0, iht] < filterValue)) {
							zData[i, iht] = double.MaxValue;
						}
						else {
							zData[i, iht] = (dataArray[setIndex, iht]);
						}
					}
				}  // end for i
			}  // end else
		}  // end PopDataBlock.GetTHZArray()


		// ///////////////////////////
		// Public properties

		public int Nhts {
			get {
				if (Hts != null) {
					return Hts.Length;
				}
				else {
					return 0;
				}
			}
		}

		public int Ntimes {
			get {
				if (Times != null) {
					return Times.Count;
				}
				else {
					return 0;
				}
			}
		}
		public float NRx {
			get {
				return _nrx;
			}
		}
		public float NPts {
			get {
				return _npts;
			}
		}
		public float NSets {
			get {
				return _nsets;
			}
		}
		public double Azimuth {
			get {
				return _azimuth;
			}
		}
		public double Elevation {
			get {
				return _elevation;
			}
		}
		public double AltitudeKm {
			get {
				return _altitude/1000.0;
			}
		}

		public double DefaultSNRThreshold {
			get {
				return _defaultThreshold;
			}
		}

		public int StationID {
			get {
				return _stationID;
			}
		}

		public string StationLabel {
			get {
				return _stationLabel;
			}
		}

		public double Nyquist {
			get {
				return _nyquist;
			}
		}
	}
	
}
