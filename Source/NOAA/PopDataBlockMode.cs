using System;
using System.Collections;

namespace DACarter.NOAA {
	public class PopDataBlockMode : IEnumerable {

		#region private class members
		private int _pw;
		private int _ncode;
		private int _nsets;
		private int _delay;
		private int _spacing;
		private double _firstRangeKm;
		//private Int16 _clock;
		private int _stationID;
		private string _stationLabel;
		private ArrayList _dataBlockList;  // contains PopDataBlock items
        private bool _combineAllBeams;
		#endregion


		public BeamAccessor Beam;

        public PopDataBlockMode() {
            _combineAllBeams = false;
            _dataBlockList = new ArrayList();
            Beam = new BeamAccessor(this);
            //_initialize(Int16.MaxValue,Int16.MaxValue,Int16.MaxValue,Int16.MaxValue,Int16.MaxValue,Int16.MaxValue);
            initialize(null);
        }

        public PopDataBlockMode(bool combineAllBeams) {
            _combineAllBeams = combineAllBeams;
            _dataBlockList = new ArrayList();
            Beam = new BeamAccessor(this);
            //_initialize(Int16.MaxValue,Int16.MaxValue,Int16.MaxValue,Int16.MaxValue,Int16.MaxValue,Int16.MaxValue);
            initialize(null);
        }

        private void initialize(PopData data) {
			if (data == null) {
				_pw = int.MaxValue;
				_ncode = int.MaxValue;
				_nsets = int.MaxValue;
				_delay = int.MaxValue;
				_spacing = int.MaxValue;
				_stationID = -1;
				_stationLabel = "??";
				_firstRangeKm = 0.0;
			}
			else {
				_pw = data.Hdr.PW;
				_ncode = data.Hdr.NCode;
				_nsets = data.Hdr.NSets;
				_delay = data.Hdr.Delay;
				_spacing = data.Hdr.Spacing;
				_stationID = data.Hdr.RadarID;
				_stationLabel = data.StationLabel;
				_firstRangeKm = data.GetRangeKm(0);
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

		public bool DataIsMatch(PopData data) {
			if (_dataBlockList.Count == 0) {
				return true;
			}
			if ((data.Hdr.Delay != _delay) ||
				(data.Hdr.Spacing != _spacing) ||
				(data.Hdr.PW != _pw) ||
				(data.Hdr.NCode != _ncode) ||
				(data.Hdr.NSets != _nsets)) {

				return false;
			}
			else {
				return true;
			}
		}

		public void Add(PopData data) {
			if (!DataIsMatch(data)) {
				throw new ArgumentException("PopDataBlockMode: Add data type does not match data in series");
			}
			if (_dataBlockList.Count == 0) {
				// no blocks in list, create one to match this data
				_dataBlockList.Add(new PopDataBlock(_combineAllBeams));
				initialize(data);
			}
			// find a data block in list that matches this data
			// and add data to that block
			bool matchingBlockFound = false;
			foreach (PopDataBlock blk in _dataBlockList) {
				if (blk.DataIsMatch(data)) {
					blk.Add(data);
					matchingBlockFound = true;
					break;
				}
			}
			// if no matching block found, start a new one
			if (!matchingBlockFound) {
				PopDataBlock blk = new PopDataBlock(_combineAllBeams);
				blk.Add(data);
				_dataBlockList.Add(blk);

			}
		}

		// define indexer so we can access blocks in the list
		// through an index on a PopDataBlockMode object.
		// e.g. PopDataBlockMode mode;
		// e.g. PopDataBlock blk = mode[i];
		public PopDataBlock this[int i] {
			get {
				return (PopDataBlock)_dataBlockList[i];
			}
		}

		// An alternative way to access blocks in the list
		// e.g. PopDataBlockMode mode;
		// e.g. PopDataBlock blk = mode.Beam[i];
		public class BeamAccessor {
			PopDataBlockMode _mode;
			public BeamAccessor(PopDataBlockMode mode) {
				_mode = mode;
			}
			public PopDataBlock this[int i] {
				get {
					return (PopDataBlock)_mode[i];
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
		public int Pw {
			get {
				if (_dataBlockList.Count == 0) {
					throw new InvalidOperationException("PopDataBlockSeries: PW not yet defined");
				}
				else {
					return _pw;
				}
			}
		}
		public double FirstRangeKm {
			get {
				if (_dataBlockList.Count == 0) {
					throw new InvalidOperationException("PopDataBlockSeries: Delay not yet defined");
				}
				else {
					return _firstRangeKm;
				}
			}
		}
		public int Delay {
			get {
				if (_dataBlockList.Count == 0) {
					throw new InvalidOperationException("PopDataBlockSeries: Delay not yet defined");
				}
				else {
					return _delay;
				}
			}
		}
		public int Spacing {
			get {
				if (_dataBlockList.Count == 0) {
					throw new InvalidOperationException("PopDataBlockSeries: Spacing not yet defined");
				}
				else {
					return _spacing;
				}
			}
		}
		public int NCode {
			get {
				if (_dataBlockList.Count == 0) {
					throw new InvalidOperationException("PopDataBlockSeries: Pulse Code not yet defined");
				}
				else {
					return _ncode;
				}
			}
		}
		public int NSets {
			get {
				if (_dataBlockList.Count == 0) {
					throw new InvalidOperationException("PopDataBlockSeries: NRx not yet defined");
				}
				else {
					return _nsets;
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


		// implement IEnumerable so we can use foreach on PopDataBlockMode.
		// New implementation for .NET 2.0
		public IEnumerator GetEnumerator() {
			for (int i = 0; i < this.Count; i++) {
				yield return this[i];
			}
		}

		// implement IEnumerable so we can use foreach on PopDataBlockMode.
		// Old implementation for .NET 1.1
		/*
		#region IEnumerable Members
		public IEnumerator GetEnumerator() {
			return new MyCollectionEnumerator(this);
		}
		private class MyCollectionEnumerator : IEnumerator {

			private int _lastCount;
			private PopDataBlockMode _col;
			int _index;

			public MyCollectionEnumerator(PopDataBlockMode col) {
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
		*/

	}
	
}
