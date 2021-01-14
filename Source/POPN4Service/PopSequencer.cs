using System;
using System.Collections.Generic;
using System.Text;

using DACarter.Utilities;
using DACarter.PopUtilities;
using System.IO;

namespace POPN {

    /// <summary>
    /// Determines which parx file to use when
    /// working from a *.seq file
    /// </summary>
    class PopParFileSequencer {

        public List<string> ParFileList;

        private int _currentIndex;
        private string _seqFileFolder;

        private PopParFileSequencer() {
        }

        public PopParFileSequencer(string seqFilePath) {

            _currentIndex = -1;

            _seqFileFolder = Path.GetDirectoryName(seqFilePath);

            ParFileList = new List<string>();

            TextFile seqFile = new TextFile(seqFilePath, openForWriting: false);
            string fileName;
            do {
                fileName = seqFile.ReadLine();
                if (!string.IsNullOrWhiteSpace(fileName)) {
                    string fileFullPath = Path.Combine(_seqFileFolder, fileName);
                    fileFullPath = Path.GetFullPath(fileFullPath);  // to clean up relative path segments in path name
                    ParFileList.Add(fileFullPath);
                }
            } while (fileName != null);
            seqFile.Close();
        }

        /// <summary>
        /// Returns full path name of next parx file in the sequence.
        /// </summary>
        /// <returns></returns>
        public string NextParFile() {
            _currentIndex++;
            if (_currentIndex >= ParFileList.Count) {
                _currentIndex = 0;
            }
            return (string)ParFileList[_currentIndex];
        }
    }

    /// <summary>
    /// Class to determine next dwell in sequence
    /// based on parameter set and beam direction
    /// </summary>
	class PopDwellSequencer {

		// ----------------------------------
		#region Private Fields

		private PopParameters _parameters;
		private PopParameters.BeamPosition _curBeamPosition;
		//private int _curBeamSeqIndex;
		private PopParameters.Direction _curBeamDirection;
		//private int _curDirectionIndex;
		private PopParameters.BeamParameters _curBeamParameters;
		//private int _curBeamParIndex;

		private int _curRepIndex;
		private int _nRep;

		private PopParCurrentIndices _currIndices;

		#endregion Private Fields
		// ----------------------------------

		// ----------------------------------
		#region Properties

		public PopParCurrentIndices CurrentIndices {
			get { return _currIndices; }
			set {_currIndices = value; }
		}

		public PopParameters Parameters {
			get { return _parameters; }
			set {
				_parameters = value;
				Reset();
			}
		}

		public PopParameters.BeamPosition CurrentBeamPosition {
			get { return _curBeamPosition; }
			set { _curBeamPosition = value; }
		}

		public int CurrentBeamSeqIndex {
			get { return _currIndices.BmSeqI; }
			set { _currIndices.BmSeqI = value; }
		}

		public PopParameters.Direction CurrentBeamDirection {
			get { return _curBeamDirection; }
			set { _curBeamDirection = value; }
		}

		public int CurrentDirectionIndex {
			get { return _currIndices.DirI; }
			set { _currIndices.DirI = value; }
		}

		public PopParameters.BeamParameters CurrentBeamParameters {
			get { return _curBeamParameters; }
			set { _curBeamParameters = value; }
		}

		public int CurrentBeamParIndex {
			get { return _currIndices.ParI; }
			set { _currIndices.ParI = value; }
		}

		public int CurrentRepIndex {
			get { return _curRepIndex; }
			set { _curRepIndex = value; }
		}

		public int CurrentNRep {
			get { return _nRep; }
			set { _nRep = value; }
		}

		#endregion Properties
		// ----------------------------------

		// ----------------------------------
		#region Constructors
		public PopDwellSequencer() {
			// must set Parameters property and call Reset()
			// before calling Next();
		}
		public PopDwellSequencer(PopParameters paramSet) {
			_parameters = paramSet;
			Reset();
		}
		#endregion Constructors
		// ----------------------------------

		// ----------------------------------
		#region Public Methods

		// //////////////////////////////////
		/// <summary>
		/// Initializes internal values and
		/// Resets the sequence back to the beginning;
		/// Must be called after _parameters field is changed.
		/// </summary>
		public void Reset() {
			_currIndices.ParI = int.MaxValue;
			_currIndices.BmSeqI = 0;
			_currIndices.DirI = int.MaxValue;
			_curRepIndex = 0;
		}

		// //////////////////////////////////
		/// <summary>
		/// Determines the parameter values to use
		///		for the next dwell in the sequence.
		/// </summary>
		/// <returns>The index in the PopParameters.BeamSequence array.</returns>
		public int Next() {

			// check to see if we have just Reset -
			//	if so, start at initial 0 indices
			if (_currIndices.ParI != int.MaxValue) {
				// otherwise, increment to next dwell in sequence.
				_curRepIndex++;
			}
			// First, try to go to next rep at current position
			_nRep = _parameters.SystemPar.RadarPar.BeamSequence[_currIndices.BmSeqI].NumberOfReps;
			if (_curRepIndex >= _nRep) {
				// Have done all reps at this beam position.
				// Move to next position.
				_curRepIndex = 0;
				_currIndices.BmSeqI++;
				if (_currIndices.BmSeqI >= _parameters.GetBeamsInSequence()) {
					// Have gone past last beam position.
					_currIndices.BmSeqI = 0;
				}
			}

			// set parameter fields
			_curBeamPosition = _parameters.SystemPar.RadarPar.BeamSequence[_currIndices.BmSeqI];
			_currIndices.DirI = _curBeamPosition.DirectionIndex;
			_curBeamDirection = _parameters.SystemPar.RadarPar.BeamDirections[_currIndices.DirI];
			_currIndices.ParI = _curBeamPosition.ParameterIndex;
			_curBeamParameters = _parameters.SystemPar.RadarPar.BeamParSet[_currIndices.ParI];

			return _currIndices.BmSeqI;
		}

		#endregion Public Methods
		// ----------------------------------

	}
}
