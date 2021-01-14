using System;
using System.Collections;

namespace DACarter.NOAA {
	public class PopDataBlockModeSet : IEnumerable {

		private ArrayList _dataBlockModeList;  // contains PopDataBlockSeries items
		public PopModeAccessor Mode;
		private int _stationID;
		private string _stationLabel;
        private bool _combineAllBeams;

        public PopDataBlockModeSet() {
            _combineAllBeams = false;
            _dataBlockModeList = new ArrayList();
            Mode = new PopModeAccessor(this);
            _stationID = -1;
            _stationLabel = "??";
        }

        public PopDataBlockModeSet(bool combineAllBeams) {
            _combineAllBeams = combineAllBeams;
            _dataBlockModeList = new ArrayList();
            Mode = new PopModeAccessor(this);
            _stationID = -1;
            _stationLabel = "??";
        }

        public bool DataIsMatch(PopData data) {
			if (_dataBlockModeList.Count == 0) {
				return true;
			}
			else if (data.Hdr.RadarID != _stationID) {
				return false;
			}
			else {
				return true;
			}
		}


		public void Add(PopData data) {

			if (!DataIsMatch(data)) {
				throw new ArgumentException("PopDataBlockModeSet: Add data type does not match station in series");
			}
			if (_dataBlockModeList.Count == 0) {
				// no mode blocks in list, use this data to set station
				//_dataBlockModeList.Add(new PopDataBlockMode());
				_stationID = data.Hdr.RadarID;
				_stationLabel = data.StationLabel;
			}

			// find a data block mode (a PopDataBlockMode item)
			// in list that matches this data
			// and add data to that mode
			bool matchingModeFound = false;
			//PopDataBlockMode mode;
			foreach (PopDataBlockMode mode in _dataBlockModeList) {
				//mode = obj as PopDataBlockMode;
				if (mode.DataIsMatch(data)) {
					mode.Add(data);
					matchingModeFound = true;
					break;
				}
			}
			// if no matching block found, start a new one
			if (!matchingModeFound) {
				PopDataBlockMode mode = new PopDataBlockMode(_combineAllBeams);
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
		/// <param name="setIndex"></param>
		public void GetTHZArray(out DateTime[] tData, out double[] hData, out double[,] zData,
								int mode, int beam, PopData.PopDataType type, int snrThreshold, int setIndex) {

			int ptCount = this.Mode[mode].Beam[beam].Ntimes;
			int htCount = this.Mode[mode].Beam[beam].Nhts;

			hData = new double[htCount];
			tData = new DateTime[ptCount];
			zData = new double[ptCount, htCount];

			this.Mode[mode].Beam[beam].GetTHZArray(out tData, out hData, out zData, type, snrThreshold, setIndex);

		}	// end of GetTHZArrays()


		// define indexer so we can access blockModes in the list
		// through an index on a PopDataBlockModeSet object.
		// e.g. PopDataBlockModeSet modeSet;
		// e.g. PopDataBlockMode mode = modeSet[i];
		public PopDataBlockMode this[int i] {
			get {
				return (PopDataBlockMode)_dataBlockModeList[i];
			}
		}

		// An alternative way to access blocks in the list
		// e.g. PopDataBlockModeSet modeSet;
		// e.g. PopDataBlockMode mode = modeSet.Mode[i];
		public class PopModeAccessor {
			PopDataBlockModeSet _set;
			public PopModeAccessor(PopDataBlockModeSet set) {
				_set = set;
			}
			public PopDataBlockMode this[int i] {
				get {
					return (PopDataBlockMode)_set[i];
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

		// implement IEnumerable so we can use foreach on PopDataBlockModeSet.
		// New implementation for .NET 2.0
		public IEnumerator GetEnumerator() {
			for (int i = 0; i < this.Count; i++) {
				yield return this[i];
			}
		}

		// implement IEnumerable so we can use foreach on PopDataBlockModeSet.
		/*
		#region IEnumerable Members
		
		public IEnumerator GetEnumerator() {
			return new MyCollectionEnumerator(this);
		}
		private class MyCollectionEnumerator : IEnumerator {

			private int _lastCount;
			private PopDataBlockModeSet _col;
			int _index;

			public MyCollectionEnumerator(PopDataBlockModeSet col) {
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
