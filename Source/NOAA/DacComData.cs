using System;
using System.Collections;
using DACarter.Utilities;

namespace DACarter.NOAA {


	public class ComDataX {
		public Int16[] Hdr;
		public Int16[] Ht;
		public Int16[,] Vel;
		public Int16[,] Snr;
		public Int32[,] Noise;
		public Int16[,] Width;
		public enum ComDataType {
			Velocity,
			SNR,
			Width,
			NoiseLevel
		}

		// These 3 values are set only in the 'Set..Size' methods
		// and represent the array sizes, not the parameter values:
		private int _npar, _nhts, _nrx;

		public int NPar {
			get {
				lock(this) {
					return Hdr[(int)HdrId.Npar];
				}
			}
		}
		public int NHts {
			get {
				lock(this) {
					return Hdr[(int)HdrId.Nhts];
				}
			}
		}
		public int NRx {
			get {
				lock(this) {
					return Hdr[(int)HdrId.Nrx];
				}
			}
		}
		public DateTime TimeStamp {
			get {
				lock (this) { 
					DateTime dt;
					if (Hdr.Length == 0) {
						//throw new InvalidOperationException("ComData header has length zero");
						dt = DateTime.MinValue;
					} else {
						dt = DacDateTime.FromDayOfYear(
							Hdr[(int)HdrId.Year],
							Hdr[(int)HdrId.Doy],
							Hdr[(int)HdrId.Hour],
							Hdr[(int)HdrId.Minute],
							Hdr[(int)HdrId.Second]);
					}
					return dt;
				}
			}
		}

		public double Nyquist {
			get {
				lock(this) {
					double nyq = (2.997925e3/Hdr[(int)HdrId.Freq])/(4.0*Hdr[(int)HdrId.Nci]*Hdr[(int)HdrId.Ippus]*1.0e-6);
					return nyq;
				}
			}
		}

		public double DefaultSNRThreshold {
			// Tony's formula for SNR threshold
			get {
				lock(this) {
					int nspec = Hdr[(int)HdrId.Nspec];
					int npts = Hdr[(int)HdrId.Npts];
					double qq = nspec - 2.3125 + 170.0/npts;
					if (qq < 0.2) {
						qq = 0.2;
					}
					qq = 25.0 * Math.Sqrt(qq)/(nspec*npts);
					return (10.0 * Math.Log10(qq));
				}
			}
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public ComDataX(){
			// force arrays to be created:
			_npar = -1;
			_nhts = -1;
			_nrx = -1;
			// create zero-length arrays:
			SetSize(0,0,0);
		}

		/// <summary>
		/// Constructor with array dimensions as arguments
		/// </summary>
		/// <param name="npar"></param>
		/// <param name="nhts"></param>
		/// <param name="nrx"></param>
		public ComDataX(int npar, int nhts, int nrx){
			// force arrays to be created:
			_npar = -1;
			_nhts = -1;
			_nrx = -1;
			SetSize(npar, nhts, nrx);
		}

		/// <summary>
		/// Redimension data arrays if necessary
		/// </summary>
		/// <param name="npar"></param>
		/// <param name="nhts"></param>
		/// <param name="nrx"></param>
		public void SetSize(int npar, int nhts, int nrx) {
			SetHeaderSize(npar);
			SetDataSize(nhts,nrx);
		}

		/// <summary>
		/// Sets up internal array sizes based on values in a supplied
		/// header array.  Copies the first npar=hdr[0] elements
		/// of the parameter header array into the internal header.
		/// </summary>
		/// <remarks>
		/// This method is equivalent to calling SetSize() and then
		/// assigning values to the Hdr[] array, but this method will
		/// guarrantee that the internal array dimensions agree with
		/// the header parameters.
		/// </remarks>
		/// <param name="hdr"></param>
		public void InitFromHeader(Int16[] hdr) {
			int npar = hdr[(int)HdrId.Npar];
			if (hdr.Length < npar) {
				throw new ArgumentException("Header length not equal to NPar");
			}
			// set header and data array sizes
			SetSize(npar,hdr[(int)HdrId.Nhts],hdr[(int)HdrId.Nrx]);
			// copy header
			for (int ipar=0; ipar<npar; ipar++) {
				Hdr[ipar] = hdr[ipar];
			}
		}

		public void SetHeaderSize(int npar) {
			if (npar < 0) {
				throw new ArgumentException("SetHeaderSize size is less than 0");
			}
			if (_npar != npar) {
				Hdr = new Int16[npar];
			}
			_npar = npar;
		}

		public void SetDataSize(int nhts, int nrx) {
			if ((nhts < 0) || (nrx < 0)) {
				throw new ArgumentException("SetDataSize size is less than 0");
			}
			if (_nhts != nhts) {
				Ht = new Int16[nhts];
			}
			if ((_nhts != nhts) || (_nrx != nrx)) {
				Vel = new Int16[nrx,nhts];
				Snr = new Int16[nrx,nhts];
				Noise = new Int32[nrx,nhts];
				Width = new Int16[nrx,nhts];
			}
			_nhts = nhts;
			_nrx = nrx;
		}

		/// <summary>
		/// Returns a new ComData object whose contents are identical to the original.
		/// </summary>
		/// <returns>The new ComData object.</returns>
		public ComDataX Copy() {
			ComDataX data = new ComDataX(_npar,_nhts,_nrx);
			for (int i=0; i<_npar; i++) {
				data.Hdr[i] = this.Hdr[i];
			}
			for (int iht=0; iht<_nhts; iht++) {
				data.Ht[iht] = this.Ht[iht];
				for (int irx=0; irx<_nrx; irx++) {
					data.Vel[irx,iht] = this.Vel[irx,iht];
					data.Snr[irx,iht] = this.Snr[irx,iht];
					data.Noise[irx,iht] = this.Noise[irx,iht];
					data.Width[irx,iht] = this.Width[irx,iht];
				}
			}
			return data;
		}

		/// <summary>
		/// Copies all the elements of this object to another existing object.
		/// </summary>
		/// <param name="dest">The destination ComData object.</param>
		public void CopyTo(ComDataX dest) {
			if (dest == null) {
				throw new ArgumentNullException("ComData CopyTo() argument is null");
			}
			dest.SetSize(_npar, _nhts, _nrx);
			for (int i=0; i<_npar; i++) {
				dest.Hdr[i] = this.Hdr[i];
			}
			for (int iht=0; iht<_nhts; iht++) {
				dest.Ht[iht] = this.Ht[iht];
				for (int irx=0; irx<_nrx; irx++) {
					dest.Vel[irx,iht] = this.Vel[irx,iht];
					dest.Snr[irx,iht] = this.Snr[irx,iht];
					dest.Noise[irx,iht] = this.Noise[irx,iht];
					dest.Width[irx,iht] = this.Width[irx,iht];
				}
			}
		}


		/// <summary>
		/// Computes height in km for a given range gate.
		/// </summary>
		/// <param name="iht">Height index.</param>
		/// <param name="isASL">true for height above sea level, false for above ground.</param>
		/// <returns>Height in km.</returns>
		public double GetKm(int iht, bool isASL) {
			double fctr,km;
			int nc;
			double elev;
			int sdelay,pw,clock;
			int delay,spacing,altitude;
			Int16 hdrElev;

			hdrElev = Hdr[(int)HdrId.Elev];
			if (hdrElev>0) {
				if (hdrElev > 90)
					elev = hdrElev/100.0;
				else
					elev = (double)hdrElev;
				fctr = (float)Math.Sin(3.141592*elev/180.0);
			}   
			else {
				fctr = (float)1.0;
			}

			clock = Hdr[(int)HdrId.Clockns];
			if (clock < 1) {
				clock = 1000;
			}
			sdelay = Hdr[(int)HdrId.SysDly];
			pw = Hdr[(int)HdrId.Pwclk]*clock;
			delay = Hdr[(int)HdrId.Delayclk]*clock;
			spacing = Hdr[(int)HdrId.Spacingclk]*clock;
			altitude = Hdr[(int)HdrId.Alt];
	
			nc = 1;
			if (sdelay == -1) {
				// old CXI50 ID=42 <1991Day10
				// not sure what sysdelay should be
				sdelay = pw/2;
			}
			// if SYSDELAY is negative, units are microsec:
			if (sdelay < 0) {
				sdelay = -sdelay*1000;
			}

			km = ((delay+iht*spacing)-sdelay+(nc-1)*pw)*.149896/1000.0*fctr;

			if (isASL) {
				km += (double)altitude/1000.0;
			}

			return(km);
		}

		public string StationLabel {
			get {
				lock(this) {
					int site = Hdr[(int)HdrId.SiteId];
					string label;
					switch (site) {
						case 100:
							label = "Boulder Test";
							break;
						case 103:
							label = "Mt. Isa";
							break;
						case 104:
							label = "Darwin 50";
							break;
						case 105:
							label = "Christmas 915";
							break;
						case 106:
							label = "Pohnpei 50";
							break;
						case 107:
							label = "Saipan 50";
							break;
						case 109:
							label = "Harp 915";
							break;
						case 110:
							label = "Saipan 915";
							break;
						case 112:
							label = "Christmas 50";
							break;
						case 113:
							label = "Piura 50";
							break;
						case 114:
							label = "Biak 50";
							break;
						case 118:
							if (Hdr[(int)HdrId.Year] < 2001) {
								label = "Flatland 915";
							}
							else {
								label = "Pease 915";
							}
							break;
						case 119:
							label = "Manus 915";
							break;
						case 121:
							label = "Darwin 920";
							break;
						case 123:
							label = "Kapinga 915";
							break;
						case 124:
							label = "Kavieng 915";
							break;
						case 126:
							label = "Ship Sci1";
							break;
						case 127:
							label = "Ship Exp3";
							break;
						case 128:
							label = "Nauru 915";
							break;
						case 130:
							label = "Flatland 50";
							break;
						case 131:
							label = "Moana Wave 915";
							break;
						case 133:
							label = "Tarawa 915";
							break;
						case 135:
							label = "Galapagos 915";
							break;
						case 136:
							label = "Kapinga 915";
							break;
						case 139:
							label = "PACS RHB";
							break;
						case 140:
							label = "Biak 915";
							break;
						case 141:
							label = "MCTEX SBand";
							break;
						case 142:
							label = "Manus SBand";
							break;
						case 143:
							label = "Garden Pt";
							break;
						case 144:
							label = "Ships";
							break;
						case 146:
							label = "TRMM SBand";
							break;
						case 147:
							label = "RHB Ship SBand";
							break;
						default:
							label = "Unknown";
							break;
					}
					return label;
				}
			}
		}

		/// <summary>
		/// Names for the header parameters
		/// that can be used in place of array indices.
		/// </summary>
		/// 
		public enum HdrId {
			Npar,
			OrigSize,
			Nhts,		// same as Nsam
			Nrx,
			Npts,
			Nspec,
			Nrej,		// not really used
			Nci,
			Ippus,
			Pwclk,
			Delayclk,
			Spacingclk,
			Nsam,
			Delay2,		// same as DelayClk
			Spacing2,	// same as Spacingclk
			Nsam2,		// same as Nsam
			Year,
			Doy,
			Hour,
			Minute,
			Second,
			Nmin,		// not used
			Nthld,		// not used
			Spec,		// ==1 if spectra in file
			Mom,
			X25,
			X26,
			X27,
			X28,
			Az,
			Freq,
			X31,
			Alt,
			PCode,
			Elev,
			X35,
			Icra,
			SysDly,		// nsec, unless <0 then usec
			UTmin,
			Dop0,
			Dop1,
			Dop2,
			Dop3,
			X43,X44,X45,X46,X47,X48,X49,X50,
			X51,X52,X53,X54,X55,X56,X57,
			Clockns,
			X59,
			Lat,
			Lng,
			Rev,
			SiteId,		// index 63
			MetFlag,	// if met data follow, this is 16 bit flags to indicate which instruments present
			ShipLat = 83,
			ShipLong,
			ShipSpeed,
			ShipHeading,
			ShipCompass

		}
	}

	/// <summary>
	/// A general class to collect ComDataBlock objects of any beam or mode.
	/// This class contains a list of ComDataBlockMode objects, each of which 
	/// contains ComDataBlocks corresponding to one particular mode.
	/// The ComDataBlockMode objects can be retrieved
	/// through an indexer on a ComDataBlockModeSet object.
	/// A ComDataBlockMode object contains a list of ComDataBlocks that
	/// correspond to all the beams in that mode.
	/// ComDataBlock objects can be retrieved through an indexer on the
	/// ComDataBlockMode object.
	/// Data from ComData items can be written to ComDataBlocks collected
	/// in this ComDataBlockModeSet object via the Add method.
	/// 
	/// ComDataBlockModeSet (ht/time arrays of all modes)
	///      contains a list of
	/// ComDataBlockMode objects (each is ht/time data from all beams of one mode)
	///      each of which contains a list of 
	/// ComDataBlock objects (each is ht/time data from one mode/beam)
	///      each of which contains lists of vectors of data from
	/// ComData objects (each is ht profile of data from one time (one dwell))
	/// 
	/// </summary>
	public class ComDataBlockModeSet : IEnumerable {

		private ArrayList _dataBlockModeList;  // contains ComDataBlockSeries items
		public ModeAccess Mode;
		private int _stationID;
		private string _stationLabel;

		public ComDataBlockModeSet() {
			_dataBlockModeList = new ArrayList();
			Mode = new ModeAccess(this);
			_stationID = -1;
			_stationLabel = "??";
		}

		public bool DataIsMatch(ComDataX data) {
			if (_dataBlockModeList.Count == 0) {
				return true;
			}
			else if (data.Hdr[(int)ComDataX.HdrId.SiteId] != _stationID) {
				return false;
			}
			else {
				return true;
			}
		}


		public void Add(ComDataX data) {
			
			if (!DataIsMatch(data)) {
				throw new ArgumentException("ComDataBlockModeSet: Add data type does not match station in series");
			}
			if (_dataBlockModeList.Count == 0) {
				// no mode blocks in list, use this data to set station
				//_dataBlockModeList.Add(new ComDataBlockMode());
				_stationID = data.Hdr[(int)ComDataX.HdrId.SiteId];
				_stationLabel = data.StationLabel;
			}
			
			// find a data block mode (a ComDataBlockMode item)
			// in list that matches this data
			// and add data to that mode
			bool matchingModeFound = false;
			//ComDataBlockMode mode;
			foreach (ComDataBlockMode mode in _dataBlockModeList) {
				//mode = obj as ComDataBlockMode;
				if (mode.DataIsMatch(data)) {
					mode.Add(data);
					matchingModeFound = true;
					break;
				}
			}
			// if no matching block found, start a new one
			if (!matchingModeFound) {
				ComDataBlockMode mode = new ComDataBlockMode();
				mode.Add(data);
				_dataBlockModeList.Add(mode);

			}
		}


		/// <summary>
		/// Extracts x,y,z arrays (time, height, data value)
		/// for a particular data type 
		/// and a specific mode and beam.
		/// </summary>
		/// <param name="tData"></param>
		/// <param name="hData"></param>
		/// <param name="zData"></param>
		/// <param name="mode"></param>
		/// <param name="beam"></param>
		/// <param name="type"></param>
		public void GetTHZArrays(out DateTime[] tData, out double[] hData, out double[,] zData,
								int mode, int beam, ComDataX.ComDataType type, int snrThreshold) {

			int ptCount = this.Mode[mode].Beam[beam].Ntimes;
			int htCount = this.Mode[mode].Beam[beam].Nhts;

			int maxHts = this.Mode[mode].Beam[beam].Hts.Length;

			hData = new double[htCount];
			tData = new DateTime[ptCount];
			zData = new double[ptCount, htCount];

			// default index ranges
			int firstHtIndex = 0;
			int lastHtIndex = htCount-1;
			int firstTimeIndex = 0;
			int lastTimeIndex = ptCount-1;

			bool filterOn = false;
			float filterValue = float.MinValue;

			filterOn = true;
			filterValue = (float)snrThreshold * 100.0f;

			if (type == ComDataX.ComDataType.SNR) {
				if ((filterOn) && (filterValue < -5900.0)) {
					// contour plot blows up if "bad" values of -60 dB are plotted
					filterValue = -5900.0f;
				}
			}

			// set up height and time arrays
			for (int iht = firstHtIndex; iht <= lastHtIndex; iht++) {
				hData[iht] = (double)(this.Mode[mode].Beam[beam].Hts[iht]/100.0);
			}

			for (int ipt = firstTimeIndex; ipt <= lastTimeIndex; ipt++) {
				tData[ipt] = (DateTime)(this.Mode[mode].Beam[beam].Times[ipt]);
			}

			// calculate z array
			for (int iht = firstHtIndex; iht <= lastHtIndex; iht++) {
				
				ArrayList shortBlock;   // one of the short integer (Int16) blocks inside ComDataBlock
				Int16[,] dataArray;		// one of the short integer data arrays in shortBlock
				//ArrayList times;


				// handle Int16 and Int32 data separately
				if (type == ComDataX.ComDataType.NoiseLevel) {
					ArrayList noiseBlock = this.Mode[mode].Beam[beam].NoiseBlock;
					Int32[,] noiseArray;
					for (int i=0; i<ptCount; i++) {
						noiseArray = (Int32[,])noiseBlock[i];
						zData[i,iht] = noiseArray[0,iht];
					}
				}
				else {
					ArrayList snrBlock = this.Mode[mode].Beam[beam].SnrBlock;
					Int16[,] snrArray;
					if (type == ComDataX.ComDataType.Velocity) {
						shortBlock = this.Mode[mode].Beam[beam].VelBlock;
					}
					else if (type == ComDataX.ComDataType.SNR) {
						shortBlock = this.Mode[mode].Beam[beam].SnrBlock;
					}
					else if (type == ComDataX.ComDataType.Width) {
						shortBlock = this.Mode[mode].Beam[beam].WidthBlock;
					}
					else {
						throw new InvalidOperationException("StackedPlot: unexpected plot data type.");
					}
					for (int i=0; i<ptCount; i++) {
						snrArray = (Int16[,])snrBlock[i];
						dataArray = (Int16[,])shortBlock[i];
						if (filterOn && (snrArray[0,iht] < filterValue)) {
							zData[i,iht] = float.MaxValue;
						}
						else {
							zData[i,iht] = (float)(dataArray[0,iht]/100.0);
						}
					}
				}
			}	// end for(iht)
		}	// end of GetTHZArrays()


		// define indexer so we can access blocks in the list
		// through an index on a ComDataBlockModeSet object.
		public ComDataBlockMode this[int i] {
			get {
				return (ComDataBlockMode)_dataBlockModeList[i];
			}
		}

		// An alternative way to access blocks in the list
		// e.g. ComDataBlockMode mode;
		// e.g. ComDataBlock blk = mode.Beam[i];
		public class ModeAccess {
			ComDataBlockModeSet _set;
			public ModeAccess(ComDataBlockModeSet set) {
				_set = set;
			}
			public ComDataBlockMode this[int i] {
				get {
					return (ComDataBlockMode)_set._dataBlockModeList[i];
				}
			}
		}


		public int Count {
			get {
				return (_dataBlockModeList.Count);
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

		// implement IEnumerable so we can use foreach on ComDataBlockModeSet.
		#region IEnumerable Members
		public IEnumerator GetEnumerator() {
			return new MyCollectionEnumerator(this);
		}
		private class MyCollectionEnumerator : IEnumerator {

			private int _lastCount;
			private ComDataBlockModeSet _col;
			int _index;

			public MyCollectionEnumerator(ComDataBlockModeSet col) {
				_col = col;
				_lastCount = col.Count;
				_index = -1;
			}
			#region IEnumerator Members

			public void Reset() {
				if (_lastCount != _col.Count) {
					throw new InvalidOperationException();
				}
				_index = -1;
			}

			public object Current {
				get {
					return _col[_index];
				}
			}

			public bool MoveNext() {
				if (_lastCount != _col.Count) {
					throw new InvalidOperationException();
				}
				if (++_index >= _col.Count) {
					return false;
				}
				return true;
			}

			#endregion
		}
		#endregion


	}
	
	/////////////////////////////////////////////////////////////////////

	/// <summary>
	/// Contains a list of ComDataBlock items all representing the same mode.
	/// A mode is defined as all having the same
	/// pw, pcode, nrx, delay, and spacing.
	/// This version - nhts can vary.
	/// </summary>
	public class ComDataBlockMode : IEnumerable {
		#region private class members
		private Int16 _pw;
		private Int16 _pcode;
		private Int16 _nrx;
		private Int16 _delay;
		private Int16 _spacing;
		private Int16 _clock;
		private int _stationID;
		private string _stationLabel;
		private ArrayList _dataBlockList;  // contains ComDataBlock items
		#endregion 


		public BeamAccess Beam;

		public ComDataBlockMode() {
			_dataBlockList = new ArrayList();
			Beam = new BeamAccess(this);
			//_initialize(Int16.MaxValue,Int16.MaxValue,Int16.MaxValue,Int16.MaxValue,Int16.MaxValue,Int16.MaxValue);
			initialize(null);
		}

		private void initialize(ComDataX data) {
			if (data == null) {
				_pw = Int16.MaxValue;
				_pcode = Int16.MaxValue;
				_nrx = Int16.MaxValue;
				_delay = Int16.MaxValue;
				_spacing = Int16.MaxValue;
				_clock = Int16.MaxValue;
				_stationID = -1;
				_stationLabel = "??";
			}
			else {
				_pw = data.Hdr[(int)ComDataX.HdrId.Pwclk];
				_pcode = data.Hdr[(int)ComDataX.HdrId.PCode];
				_nrx = data.Hdr[(int)ComDataX.HdrId.Nrx];
				_delay = data.Hdr[(int)ComDataX.HdrId.Delayclk];
				_spacing = data.Hdr[(int)ComDataX.HdrId.Spacingclk];
				_clock = data.Hdr[(int)ComDataX.HdrId.Clockns];
				_stationID = data.Hdr[(int)ComDataX.HdrId.SiteId];
				_stationLabel = data.StationLabel;
			}
		}

		/*
		private void _initialize(Int16 pw, Int16 pcode,
			Int16 nrx, Int16 delay, Int16 spacing, Int16 clock) {
			_pw = pw;
			_pcode = pcode;
			_nrx = nrx;
			_delay = delay;
			_spacing = spacing;
			_clock = clock;
		}
		*/
		
		public bool DataIsMatch(ComDataX data) {
			if (_dataBlockList.Count == 0) {
				return true;
			}
			if ((data.Hdr[(int)ComDataX.HdrId.Delayclk] != _delay) ||
				(data.Hdr[(int)ComDataX.HdrId.Spacingclk] != _spacing) ||
				(data.Hdr[(int)ComDataX.HdrId.Pwclk] != _pw) ||
				(data.Hdr[(int)ComDataX.HdrId.PCode] != _pcode) ||
				(data.Hdr[(int)ComDataX.HdrId.Nrx] != _nrx) ) {
				return false;
			}
			else {
				return true;
			}
		}

		public void Add(ComDataX data) {
			if (!DataIsMatch(data)) {
				throw new ArgumentException("ComDataBlockMode: Add data type does not match data in series");
			}
			if (_dataBlockList.Count == 0) {
				// no blocks in list, create one to match this data
				_dataBlockList.Add(new ComDataBlock());
				initialize(data);
			}
			// find a data block in list that matches this data
			// and add data to that block
			bool matchingBlockFound = false;
			foreach (ComDataBlock blk in _dataBlockList) {
				if (blk.DataIsMatch(data)) {
					blk.Add(data);
					matchingBlockFound = true;
					break;
				}
			}
			// if no matching block found, start a new one
			if (!matchingBlockFound) {
				ComDataBlock blk = new ComDataBlock();
				blk.Add(data);
				_dataBlockList.Add(blk);

			}
		}

		// define indexer so we can access blocks in the list
		// through an index on a ComDataBlockMode object.
		// e.g. ComDataBlockMode mode;
		// e.g. ComDataBlock blk = mode[i];
		public ComDataBlock this[int i] {
			get {
				return (ComDataBlock)_dataBlockList[i];
			}
		}

		// An alternative way to access blocks in the list
		// e.g. ComDataBlockMode mode;
		// e.g. ComDataBlock blk = mode.Beam[i];
		public class BeamAccess {
			ComDataBlockMode _mode;
			public BeamAccess(ComDataBlockMode mode) {
				_mode = mode;
			}
			public ComDataBlock this[int i] {
				get {
					return (ComDataBlock)_mode._dataBlockList[i];
				}
			}
		}

		//
		// properties
		//
		public int Count {
			get {
				return (_dataBlockList.Count);
			}
		}
		public int PwClk {
			get {
				if (_dataBlockList.Count == 0) {
					throw new InvalidOperationException("ComDataBlockSeries: PW not yet defined");
				}
				else {
					return _pw;
				}
			}
		}
		public int PwNs {
			get {
				if (_dataBlockList.Count == 0) {
					throw new InvalidOperationException("ComDataBlockSeries: PW not yet defined");
				}
				else {
					return _pw*_clock;
				}
			}
		}
		public int DelayClk {
			get {
				if (_dataBlockList.Count == 0) {
					throw new InvalidOperationException("ComDataBlockSeries: Delay not yet defined");
				}
				else {
					return _delay;
				}
			}
		}
		public int SpacingClk {
			get {
				if (_dataBlockList.Count == 0) {
					throw new InvalidOperationException("ComDataBlockSeries: Spacing not yet defined");
				}
				else {
					return _spacing;
				}
			}
		}
		public int PCode {
			get {
				if (_dataBlockList.Count == 0) {
					throw new InvalidOperationException("ComDataBlockSeries: Pulse Code not yet defined");
				}
				else {
					return _pcode;
				}
			}
		}
		public int Nrx {
			get {
				if (_dataBlockList.Count == 0) {
					throw new InvalidOperationException("ComDataBlockSeries: NRx not yet defined");
				}
				else {
					return _nrx;
				}
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

		// implement IEnumerable so we can use foreach on ComDataBlockModeSet.
		#region IEnumerable Members
		public IEnumerator GetEnumerator() {
			return new MyCollectionEnumerator(this);
		}
		private class MyCollectionEnumerator : IEnumerator {

			private int _lastCount;
			private ComDataBlockMode _col;
			int _index;

			public MyCollectionEnumerator(ComDataBlockMode col) {
				_col = col;
				_lastCount = col.Count;
				_index = -1;
			}
			#region IEnumerator Members

			public void Reset() {
				if (_lastCount != _col.Count) {
					throw new InvalidOperationException();
				}
				_index = -1;
			}

			public object Current {
				get {
					return _col[_index];
				}
			}

			public bool MoveNext() {
				if (_lastCount != _col.Count) {
					throw new InvalidOperationException();
				}
				if (++_index >= _col.Count) {
					return false;
				}
				return true;
			}

			#endregion
		}
		#endregion


	}


	public class ComDataBlock {
		public Int16[] Hts;
		public ArrayList Times;
		public ArrayList VelBlock;
		public ArrayList SnrBlock;
		public ArrayList NoiseBlock;
		public ArrayList WidthBlock;
		//public Int16[,,] Vel;	// indices: irx,iht,itime
		//public Int16[,,] Snr;
		//public Int32[,,] Noise;
		//public Int16[,,] Width;

		private Int16 _azimuth;
		private Int16 _elevation;
		private Int16 _pw;
		private Int16 _pcode;
		private Int16 _nrx;
		private double _defaultThreshold;
		private int _stationID;
		private string _stationLabel;

		public ComDataBlock() {
			_createArrays();
			initialize(null);
			//_initialize(null,Int16.MaxValue,Int16.MaxValue,
			//	Int16.MaxValue,Int16.MaxValue,Int16.MaxValue);
		}

		private void _createArrays() {
			int initialCapacity = 250;
			Times = new ArrayList(initialCapacity);
			VelBlock = new ArrayList(initialCapacity);
			SnrBlock = new ArrayList(initialCapacity);
			NoiseBlock = new ArrayList(initialCapacity);
			WidthBlock = new ArrayList(initialCapacity);
		}

		private void initialize(ComDataX data) {
			if (data == null) {
				Hts = null;
				_pw = Int16.MaxValue;
				_pcode = Int16.MaxValue;
				_nrx = Int16.MaxValue;
				_azimuth = Int16.MaxValue;
				_elevation = Int16.MaxValue;
				_defaultThreshold = -99;
				_stationID = -1;
				_stationLabel = "??";
				//_clock = Int16.MaxValue;
			}
			else {
				if (Hts != null) {
					throw new InvalidOperationException("ComDataBlock already initialized");
				}
				if (data.Ht != null) {
					Hts = (Int16[])(data.Ht.Clone());
				}
				else {
					Hts = null;
				}
				_pw = data.Hdr[(int)ComDataX.HdrId.Pwclk];
				_pcode = data.Hdr[(int)ComDataX.HdrId.PCode];
				_nrx = data.Hdr[(int)ComDataX.HdrId.Nrx];
				_azimuth = data.Hdr[(int)ComDataX.HdrId.Az];
				_elevation = data.Hdr[(int)ComDataX.HdrId.Elev];
				_defaultThreshold = data.DefaultSNRThreshold;
				_stationID = data.Hdr[(int)ComDataX.HdrId.SiteId];
				_stationLabel = data.StationLabel;
				//_clock = data.Hdr[(int)ComData.HdrId.Clockns];
			}
		}

		/*
		private void _initialize(Int16[] hts, Int16 az, Int16 elev,
								Int16 pw, Int16 pcode, Int16 nrx) {
			if (Hts != null) {
				throw new InvalidOperationException("ComDataBlock already initialized");
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

		public bool DataIsMatch(ComDataX data) {
			if (Hts == null) {
				return true;
			}
			int nhts = Hts.Length;
			if ( (data.NHts != nhts) ||
				(data.Ht[0] != Hts[0]) ||
				(data.Ht[nhts-1] != Hts[nhts-1]) ||
				(data.Hdr[(int)ComDataX.HdrId.Az] != _azimuth) ||
				(data.Hdr[(int)ComDataX.HdrId.Elev] != _elevation) ||
				(data.Hdr[(int)ComDataX.HdrId.Pwclk] != _pw) ||
				(data.Hdr[(int)ComDataX.HdrId.PCode] != _pcode) ||
				(data.Hdr[(int)ComDataX.HdrId.Nrx] != _nrx) ||
				(data.Hdr[(int)ComDataX.HdrId.SiteId] != _stationID) ) {
				return false;
			}
			else {
				return true;
			}

		}

		/// <summary>
		/// Add data from one ComData object to the end of the block
		///   of data within this ComDataBlock object.
		/// Height array of ComData object must match the height array
		///   of this ComDataBlock object.
		/// </summary>
		/// <param name="data">Source ComData object</param>
		public void Add(ComDataX data) {
			if (!DataIsMatch(data)) {
				throw new ArgumentException("ComDataBlock: Add data type does not match data in block");
			}

			if (Hts == null) {
				initialize(data);
				/*
				_initialize(data.Ht,
							data.Hdr[(int)ComData.HdrId.Az],
							data.Hdr[(int)ComData.HdrId.Elev],
							data.Hdr[(int)ComData.HdrId.Pwclk],
							data.Hdr[(int)ComData.HdrId.PCode],
							data.Hdr[(int)ComData.HdrId.Nrx]);
				_defaultThreshold = data.DefaultSNRThreshold;
				*/
			}

			Int16[,] vel = new Int16[data.NRx,data.NHts];
			Array.Copy(data.Vel,vel,vel.Length);
			VelBlock.Add(vel);

			Int16[,] snr = new Int16[data.NRx,data.NHts];
			Array.Copy(data.Snr,snr,snr.Length);
			SnrBlock.Add(snr);

			Int16[,] wid = new Int16[data.NRx,data.NHts];
			Array.Copy(data.Width,wid,wid.Length);
			WidthBlock.Add(wid);

			Int32[,] noise = new Int32[data.NRx,data.NHts];
			Array.Copy(data.Noise,noise,noise.Length);
			NoiseBlock.Add(noise);

			Times.Add(data.TimeStamp);
		}

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
		public int Azimuth {
			get {
				return _azimuth;
			}
		}
		public int ElevationComFile {
			get {
				return _elevation;
			}
		}
		public double ElevationDegrees {
			get {
				if (_elevation <= 90) {
					return (double)_elevation;
				}
				else {
					return (double)_elevation/100.0;
				}
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
	}


}


