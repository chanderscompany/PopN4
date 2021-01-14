namespace DACarter.NOAA {
	
	
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

		public bool DataIsMatch(ComData data) {
			if (_dataBlockModeList.Count == 0) {
				return true;
			}
			else if (data.Hdr[(int)ComData.HdrId.SiteId] != _stationID) {
				return false;
			}
			else {
				return true;
			}
		}


		public void Add(ComData data) {
			
			if (!DataIsMatch(data)) {
				throw new ArgumentException("ComDataBlockModeSet: Add data type does not match station in series");
			}
			if (_dataBlockModeList.Count == 0) {
				// no mode blocks in list, use this data to set station
				//_dataBlockModeList.Add(new ComDataBlockMode());
				_stationID = data.Hdr[(int)ComData.HdrId.SiteId];
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
								int mode, int beam, ComData.ComDataType type, int snrThreshold) {

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

			if (type == ComData.ComDataType.SNR) {
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
				if (type == ComData.ComDataType.NoiseLevel) {
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
					if (type == ComData.ComDataType.Velocity) {
						shortBlock = this.Mode[mode].Beam[beam].VelBlock;
					}
					else if (type == ComData.ComDataType.SNR) {
						shortBlock = this.Mode[mode].Beam[beam].SnrBlock;
					}
					else if (type == ComData.ComDataType.Width) {
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
	
}
