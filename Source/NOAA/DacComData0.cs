using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using DACarter.Utilities;

namespace DACarter.NOAA {


	public class ComData {
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
			Noise
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
					if (Hdr.Length == 0) {
						throw new InvalidOperationException("ComData header has length zero");
					}
					DateTime dt = DacDateTime.FromDayOfYear(
						Hdr[(int)HdrId.Year],
						Hdr[(int)HdrId.Doy],
						Hdr[(int)HdrId.Hour],
						Hdr[(int)HdrId.Minute],
						Hdr[(int)HdrId.Second]);
					return dt;
				}
			}
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public ComData(){
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
		public ComData(int npar, int nhts, int nrx){
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
		public ComData Copy() {
			ComData data = new ComData(_npar,_nhts,_nrx);
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
		public void CopyTo(ComData dest) {
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

		/// <summary>
		/// Names for the header parameters
		/// that can be used in place of array indices.
		/// </summary>
		/// 
		public enum HdrId {
			Npar,
			OrigSize,
			Nhts,
			Nrx,
			Npts,
			Nspec,
			Nrej,
			Nci,
			Ippus,
			Pwclk,
			Delayclk,
			Spacingclk,
			Nsam,
			Delay2,
			Spacing2,
			Nsam2,
			Year,
			Doy,
			Hour,
			Minute,
			Second,
			Nmin,
			Nthld,
			Spec,
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
			SysDly,
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
			SiteId,	// index 63
			MetFlag,
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
	/// ComData objects (each is ht profile of data from one dwell)
	/// 
	/// </summary>
	public class ComDataBlockModeSet : IEnumerable {

		private ArrayList _dataBlockModeList;  // contains ComDataBlockSeries items
		public ModeAccess Mode;

		public ComDataBlockModeSet() {
			_dataBlockModeList = new ArrayList();
			Mode = new ModeAccess(this);
		}


		public void Add(ComData data) {
			if (_dataBlockModeList.Count == 0) {
				// no blocks in list, create one to match this data
				_dataBlockModeList.Add(new ComDataBlockMode());
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
		private ArrayList _dataBlockList;  // contains ComDataBlock items
		#endregion 


		public BeamAccess Beam;

		public ComDataBlockMode() {
			_dataBlockList = new ArrayList();
			Beam = new BeamAccess(this);
			_initialize(Int16.MaxValue,Int16.MaxValue,Int16.MaxValue,
				Int16.MaxValue,Int16.MaxValue,Int16.MaxValue);
		}

		private void _initialize(Int16 pw, Int16 pcode,
			Int16 nrx, Int16 delay, Int16 spacing, Int16 clock) {
			_pw = pw;
			_pcode = pcode;
			_nrx = nrx;
			_delay = delay;
			_spacing = spacing;
			_clock = clock;
		}
		
		public bool DataIsMatch(ComData data) {
			if (_dataBlockList.Count == 0) {
				return true;
			}
			if ((data.Hdr[(int)ComData.HdrId.Delayclk] != _delay) ||
				(data.Hdr[(int)ComData.HdrId.Spacingclk] != _spacing) ||
				(data.Hdr[(int)ComData.HdrId.Pwclk] != _pw) ||
				(data.Hdr[(int)ComData.HdrId.PCode] != _pcode) ||
				(data.Hdr[(int)ComData.HdrId.Nrx] != _nrx) ) {
				return false;
			}
			else {
				return true;
			}
		}

		public void Add(ComData data) {
			if (!DataIsMatch(data)) {
				throw new ArgumentException("ComDataBlockMode: Add data type does not match data in series");
			}
			if (_dataBlockList.Count == 0) {
				// no blocks in list, create one to match this data
				_dataBlockList.Add(new ComDataBlock());
				_initialize(
					data.Hdr[(int)ComData.HdrId.Pwclk],
					data.Hdr[(int)ComData.HdrId.PCode],
					data.Hdr[(int)ComData.HdrId.Nrx],
					data.Hdr[(int)ComData.HdrId.Delayclk],
					data.Hdr[(int)ComData.HdrId.Spacingclk],
					data.Hdr[(int)ComData.HdrId.Clockns]);
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

/*
	/////////////////////////////////////////////////////////////////////
	/// <summary>
	/// Contains a time series of ComDataBlock items.
	/// It is intended to represent the same beam and mode, but
	/// the height parameters (first, spacing, and number)
	/// can change from one ComDataBlock to the next in the series.
	/// Azimuth and elevation are the same for all 
	/// ComDataBlocks in the series.
	/// </summary>
	public class ComDataBlockSeries {
		private Int16 _azimuth;
		private Int16 _elevation;
		private Int16 _pw;
		private Int16 _pcode;
		private Int16 _nrx;

		private ArrayList _dataBlockList;  // contains ComDataBlock items

		public ComDataBlockSeries() {
			_dataBlockList = new ArrayList();
			_initialize(Int16.MaxValue,Int16.MaxValue,Int16.MaxValue,
				Int16.MaxValue,Int16.MaxValue);
		}

		public void Add(ComData data) {
			if (!DataIsMatch(data)) {
				throw new ArgumentException("ComDataBlockSeries: Add data type does not match data in series");
			}
			if (_dataBlockList.Count == 0) {
				_dataBlockList.Add(new ComDataBlock());
				_initialize(
					data.Hdr[(int)ComData.HdrId.Az],
					data.Hdr[(int)ComData.HdrId.Elev],
					data.Hdr[(int)ComData.HdrId.Pwclk],
					data.Hdr[(int)ComData.HdrId.PCode],
					data.Hdr[(int)ComData.HdrId.Nrx]);
			}
			int cnt = _dataBlockList.Count;
			ComDataBlock lastBlock = (ComDataBlock)(_dataBlockList[cnt-1]);
			if (lastBlock.DataIsMatch(data)) {
				lastBlock.Add(data);
			}
			else {
				ComDataBlock newBlock = new ComDataBlock();
				newBlock.Add(data);
				_dataBlockList.Add(newBlock);
			}
		}

		private void _initialize(Int16 az, Int16 elev,
			Int16 pw, Int16 pcode, Int16 nrx) {
			_azimuth = az;
			_elevation = elev;
			_pw = pw;
			_pcode = pcode;
			_nrx = nrx;
		}
		
		public bool DataIsMatch(ComData data) {
			// this version accepts data into the series that
			// has same az, elev, pw, pcode, and nrx.
			// May want to restrict how different ht array can be.
			if (_dataBlockList.Count == 0) {
				return true;
			}
			if ((data.Hdr[(int)ComData.HdrId.Az] != _azimuth) ||
				(data.Hdr[(int)ComData.HdrId.Elev] != _elevation) ||
				(data.Hdr[(int)ComData.HdrId.Pwclk] != _pw) ||
				(data.Hdr[(int)ComData.HdrId.PCode] != _pcode) ||
				(data.Hdr[(int)ComData.HdrId.Nrx] != _nrx) ) {
				return false;
			}
			else {
				return true;
			}
		}

		public ComDataBlock this[int i] {
			get {
				return (ComDataBlock)_dataBlockList[i];
			}
		}
		public int Azimuth {
			get {
				if (_dataBlockList.Count == 0) {
					throw new InvalidOperationException("ComDataBlockSeries: Azimuth not yet defined");
				}
				else {
					return _azimuth;
				}
			}
		}
		public int ElevationComFile {
			get {
				if (_dataBlockList.Count == 0) {
					throw new InvalidOperationException("ComDataBlockSeries: Elevation not yet defined");
				}
				else {
					return _elevation;
				}
			}
		}
		public double ElevationDegrees {
			get {
				if (_dataBlockList.Count == 0) {
					throw new InvalidOperationException("ComDataBlockSeries: Elevation not yet defined");
				}
				else {
					if (_elevation <= 90) {
						return (double)_elevation;
					}
					else {
						return (double)_elevation/100.0;
					}
				}
			}
		}
	}
*/

	/////////////////////////////////////////////////////////////////////

	/// <summary>
	/// Contains velocity, SNR, noise, and width data
	/// in a 2-D array (ht vs time).
	/// The data is for one mode with a constant
	/// first height, height spacing, NHTS, az, and elev,
	/// 
	/// </summary>
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

		public ComDataBlock() {
			_createArrays();
			_initialize(null,Int16.MaxValue,Int16.MaxValue,
				Int16.MaxValue,Int16.MaxValue,Int16.MaxValue);
		}

		private void _createArrays() {
			int initialCapacity = 250;
			Times = new ArrayList(initialCapacity);
			VelBlock = new ArrayList(initialCapacity);
			SnrBlock = new ArrayList(initialCapacity);
			NoiseBlock = new ArrayList(initialCapacity);
			WidthBlock = new ArrayList(initialCapacity);
		}

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

		public bool DataIsMatch(ComData data) {
			if (Hts == null) {
				return true;
			}
			int nhts = Hts.Length;
			if ( (data.NHts != nhts) ||
				(data.Ht[0] != Hts[0]) ||
				(data.Ht[nhts-1] != Hts[nhts-1]) ||
				(data.Hdr[(int)ComData.HdrId.Az] != _azimuth) ||
				(data.Hdr[(int)ComData.HdrId.Elev] != _elevation) ||
				(data.Hdr[(int)ComData.HdrId.Pwclk] != _pw) ||
				(data.Hdr[(int)ComData.HdrId.PCode] != _pcode) ||
				(data.Hdr[(int)ComData.HdrId.Nrx] != _nrx) ) {
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
		public void Add(ComData data) {
			if (!DataIsMatch(data)) {
				throw new ArgumentException("ComDataBlock: Add data type does not match data in block");
			}

			if (Hts == null) {
				_initialize(data.Ht,
							data.Hdr[(int)ComData.HdrId.Az],
							data.Hdr[(int)ComData.HdrId.Elev],
							data.Hdr[(int)ComData.HdrId.Pwclk],
							data.Hdr[(int)ComData.HdrId.PCode],
							data.Hdr[(int)ComData.HdrId.Nrx]);
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
	}
}


