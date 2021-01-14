using System;
using DACarter.NOAA;

namespace DACarter.NOAA
{

	/// <summary>
	/// Summary description for WindsData.
	/// </summary>
	public class WindsData : DacData
	{

		#region WindsDataType definition
		public enum WindsDataType {
			SpeedDirection,		// data object contains speed and direction data
			Components,			// data object contains u and v components
			Both,				// data object contains both
			None				// data object content type not specified
		}

		
		#endregion

		#region Public Data Members
		// ////////////////
		// remember DateTime TimeStamp in base class
		public string StationName;
		public int RadarID;
		public int NHts {
			get { return _nhts; }
			set {
				_nhts = value;
				// when NHts is set, arrays resize
				SetSize(_nhts);
			}
		}
		public WindsData.WindsDataType DataType {
			get { return _dataType; }
			set {
				if ((value != _dataType) && (_dataType != WindsDataType.None)) {
					throw new ApplicationException("Cannot change WindsData DataType after creation.");
				}
				else {
					_dataType = value;
				}
			}
		}
		public int PulseWidthM;
		public bool HasHorizontal;
		public bool HasVertical;
		public bool HasVerticalHts;
		public bool HasWinds;
		public bool HasSNR;
		public double LatitudeN;
		public double LongitudeE;
		public double AltitudeKm;

		//public bool HasVertical;
		//public bool HasHorizontal;
		//public bool HasSNR;
		//public bool HasWinds;

		public float[] Hts;			// dimension is [NHts], units are km ASL
		public float[] VertHts;
		public float[] Speed;		// units are m/s
		public float[] Direction;
		public float[] UWind;
		public float[] VWind;
		public float[] WWind;
		public float[] ObliqueSNR;	// average of all oblique beams, dB
		public float[] VerticalSNR;
		// //////////////////
		#endregion

		#region Public Method
		public override void Clear() {
			Notes = "";
			TimeStamp = DateTime.MinValue;
			HasHorizontal = false;
			HasVertical = false;
			HasVerticalHts = false;
			HasSNR = false;
			HasWinds = false;
			StationName = "";
			RadarID = -99;
			NHts = 0;
			PulseWidthM = 0;
		}
		#endregion

		#region Private Fields
		private WindsDataType _dataType;
		private int _nhts;
		
		#endregion

		#region Public Constructors
		public WindsData() {
			Init(WindsDataType.None, 0);
		}

		public WindsData(int nhts) {
			Init(WindsDataType.None, nhts);
		}

		public WindsData(WindsDataType type) {
			Init(type, 0);
		}

		public WindsData(WindsDataType type, int nhts) {
			Init(type, nhts);
		}
		
		#endregion

		private void Init(WindsDataType type, int nhts) {
			Clear();
			_dataType = type;
			SetSize(nhts);
		}

		/// <summary>
		/// Redimension data arrays if their size needs changing.
		/// </summary>
		/// <param name="nhts"></param>
		public void SetSize(int nhts) {
			if (nhts < 0)  {
				throw new ArgumentException("SetSize size is less than 0");
			}

			_nhts = nhts;

			// get current size of arrays
			// (-1 if arrays not created yet)
			int htsSize = -1;
			int vhtsSize = -1;
			int speedSize = -1;
			int uSize = -1;
			int zSize = -1;
			int osnrSize = -1;
			int vsnrSize = -1;
			if (Hts != null) {
				htsSize = Hts.Length;
			}
			if (VertHts != null) {
				vhtsSize = VertHts.Length;
			}
			if (Speed != null) {
				speedSize = Speed.Length;
			}
			if (UWind != null) {
				uSize = UWind.Length;
			}
			if (WWind != null) {
				zSize = WWind.Length;
			}
			if (ObliqueSNR != null) {
				osnrSize = ObliqueSNR.Length;
			}
			if (VerticalSNR != null) {
				vsnrSize = VerticalSNR.Length;
			}

			// redimension arrays if need to be larger;
			// set to zero length if not using the array;
			if (htsSize < nhts) {
				Hts = new float[nhts];
			}
			if (HasVerticalHts) {
				if (vhtsSize < nhts) {
					VertHts = new float[nhts];
				}
			}
			else {
				VertHts = new float[0];
			}
			if (HasVertical && HasWinds) {
				if (zSize < nhts) {
					WWind = new float[nhts];
				}
			}
			else {
				if (zSize != 0) {
					WWind = new float[0];
				}
			}
			if (DataType == WindsDataType.SpeedDirection) {
				if (HasWinds && HasHorizontal) {
					if (speedSize < nhts) {
						Speed = new float[nhts];
						Direction = new float[nhts];
					}
				}
				else {
					if (speedSize != 0) {
						Speed = new float[0];
						Direction = new float[0];
					}
				}
				if (uSize != 0) {
					UWind = new float[0];
					VWind = new float[0];
				}
			}
			else if (DataType == WindsDataType.Components) {
				if (HasWinds && HasHorizontal) {
					if (uSize < nhts) {
						UWind = new float[nhts];
						VWind = new float[nhts];
					}
				}
				else {
					if (uSize != 0) {
						UWind = new float[0];
						VWind = new float[0];
					}
				}
				if (speedSize != 0) {
					Speed = new float[0];
					Direction = new float[0];
				}
			}
			else if (DataType == WindsDataType.Both) {
				if (HasWinds && HasHorizontal) {
					if (speedSize < nhts) {
						Speed = new float[nhts];
						Direction = new float[nhts];
					}
					if (uSize < nhts) {
						UWind = new float[nhts];
						VWind = new float[nhts];
					}
				}
				else {
					if (speedSize != 0) {
						Speed = new float[0];
						Direction = new float[0];
					}
					if (uSize != 0) {
						UWind = new float[0];
						VWind = new float[0];
					}
				}
			}
			else {
				if (nhts > 0) {
					string ss = String.Format("Trying to SetSize({0}) of WindsDataType.None.", nhts);
					throw new InvalidOperationException(ss);
				}
			}

			if (HasSNR && HasHorizontal) {
				if (osnrSize < nhts) {
					ObliqueSNR = new float[nhts];
				}
			}
			else {
				ObliqueSNR = new float[0];
			}
			if (HasSNR && HasVertical) {
				if (vsnrSize < nhts) {
					VerticalSNR = new float[nhts];
				}
			}
			else {
				VerticalSNR = new float[0];
			}
		}

	}
}
