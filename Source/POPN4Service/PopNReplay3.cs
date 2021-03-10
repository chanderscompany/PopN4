using System;

using DACarter.NOAA;
using DACarter.Utilities;
using DACarter.PopUtilities;

namespace POPN {

    public enum ReplayStatus {
        OK = 0,
        EOF = 1,
        BeforeStartTime = 2,
        AfterEndTime = 4
    }

    /// <summary>
    /// extension method for ReplayStatus
    /// that tests for status that may be combined with others
    /// usage:  Status.Includes(status2)
    /// </summary>
    public static class ReplayStatusExtensions {
        public static bool Includes(this ReplayStatus status, ReplayStatus status2) {
            return (status & status2) == status2;
        }
    }

    public class PopNReplay : IDisposable {

        public bool HasStatus(ReplayStatus statusFlag) {
            if (Status == 0 && statusFlag == ReplayStatus.OK) {
                return true;
            }
            else if ((statusFlag & Status) != 0) {
                return true;
            }
            else {
                return false;
            }
        }

        public void SetStatus(ReplayStatus statusFlag) {
            if (statusFlag == ReplayStatus.OK) {
                Status = ReplayStatus.OK;
            }
            else {
                Status = Status | statusFlag;
            }
        }



		private PopDataPackage3 _popNData;
		private string _fileName;
		private DacPopFile _popFile;
		private PopData _pop5Data;
        private bool _isRassRecord;
        private bool _useMemAllocator;
        private PopNAllocator _memAllocator;
        private PopParameters.ReplayMode _replayPar;

        private PopParameters _processingPar;   // parameters not read from data file that tell how to process data

        public bool UseMemAllocator {
            get { return _useMemAllocator; }
            set { _useMemAllocator = value; }
        }

        private double _progress;

        private DateTime _startDateTime, _endDateTime;
        private TimeSpan _startTimeSpan, _endTimeSpan;
        private int _startDay, _endDay;

        public ReplayStatus Status;

		public PopDataPackage3 DataPackage {
			get { return _popNData; }
		}

        public PopParameters Parameters {
            get { return _popNData.Parameters; }
        }

        public PopParameters ProcessingPar {
            // parameters not read from data file that tell how to process data
            get { return _processingPar; }
            set { _processingPar = value; }
        }

        public DateTime RecordTimeStamp {
            get { return _popNData.RecordTimeStamp; }
        }

        public double[][] MeanDoppler {
            get { return _popNData.MeanDoppler; }
        }

        public double[][] Noise {
            get { return _popNData.Noise; }
        }

        public double[][] Power {
            get { return _popNData.Power; }
        }

        public double[][] Width {
            get { return _popNData.Width; }
        }

        public double[][][] Spectra {
            get { return _popNData.Spectra; }
        }

        public string FileName {
			get { return _fileName; }
			set { 
                _fileName = value;
                Init();
            }
		}

		public double ProgressFraction {
			get {
				return _progress;
				/*
				DateTime dt = _popNData.RecordTimeStamp;
				DateTime dt0 = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0);
				TimeSpan ts = dt - dt0;
				TimeSpan ts24 = new TimeSpan(24, 0, 0);

				return (ts.TotalMinutes / ts24.TotalMinutes) ; 
				*/
			}
		}

        public bool IsRassRecord {
            get { return _isRassRecord; }
            set { _isRassRecord = value; }
        }

		private PopNReplay() {
			// cannot call default constructor
		}

		public PopNReplay(string fileName) {
            _startTimeSpan = new TimeSpan(0, 0, 0);
            _endTimeSpan = new TimeSpan(24, 0, 0);
            _startDay = 0;
            _endDay = 0;
			FileName = fileName;
		}

        public PopNReplay(PopParameters.ReplayMode replayPar) {
            _replayPar = replayPar;
            _startTimeSpan = replayPar.StartTime;
            _startDay = replayPar.StartDay;
            _endTimeSpan = replayPar.EndTime;
            _endDay = replayPar.EndDay;
            bool _processRawSamples = replayPar.ProcessRawSamples;
            bool _processDopplerTS = replayPar.ProcessTimeSeries;
            bool _processSpectra = replayPar.ProcessSpectra;
            bool _processMoments = replayPar.ProcessMoments;
            FileName = replayPar.InputFile;
            _startDateTime = DateTime.MinValue;
            _endDateTime = DateTime.MaxValue;
        }

        public PopNReplay(string fileName, TimeSpan startTime, int startDay, TimeSpan endTime, int endDay) {
            _startTimeSpan = startTime;
            _startDay = startDay;
            _endTimeSpan = endTime;
            _endDay = endDay;
            FileName = fileName;
            _startDateTime = DateTime.MinValue;
            _endDateTime = DateTime.MaxValue;
        }

        public void SetStartStopTimes(TimeSpan startTime, int startDay, TimeSpan endTime, int endDay) {
            _startTimeSpan = startTime;
            _startDay = startDay;
            _endTimeSpan = endTime;
            _endDay = endDay;
        }

        private void Init() {
			_popNData = new PopDataPackage3();
			// create empty parameter set of default size:
			_popNData.Parameters = new DACarter.PopUtilities.PopParameters();
			_pop5Data = new PopData();
			_popFile = new DacPopFile();
			_popFile.OpenFileReader(_fileName);
            _isRassRecord = false;
            _progress = 0.0;
            //_startDateTime = DateTime.MinValue;
            //_endDateTime = DateTime.MaxValue;
            _memAllocator = new PopNAllocator();
		}

		public bool ReadRecord() {
			
			//_popNData.RecordTimeStamp = DateTime.Now;

            bool isOK = true;

            // number of file records to combine for one processing dwell:
            int nRecords;
            if (_processingPar.ReplayPar.ProcessRawSamples) {
                // only using this in raw TS mode for now
                nRecords = _processingPar.ReplayPar.NumberRecordsAtOnce;
                if (nRecords < 1) {
                    nRecords = 1;
                }
            }
            else {
                nRecords = 1;
                nRecords = _processingPar.ReplayPar.NumberRecordsAtOnce;
                if (nRecords < 1) {
                    nRecords = 1;
                }
            }

            bool NoRecalc = false;
            if (!_processingPar.ReplayPar.ProcessMoments &&
                !_processingPar.ReplayPar.ProcessRawSamples &&
                !_processingPar.ReplayPar.ProcessSpectra &&
                !_processingPar.ReplayPar.ProcessTimeSeries &&
                !_processingPar.ReplayPar.ProcessXCorr) {
                    NoRecalc = true;
            }

            PopParameters firstPar;
            bool firstRec = true;
            DateTime firstTimeStamp = DateTime.MinValue;
            int actualRecords = 0;

            for (int irec = 0; irec < nRecords; irec++) {

                do {
                    Status = ReplayStatus.OK;

                    try {
                        isOK = _popFile.ReadNextRecord(_pop5Data);
                    }
                    catch (Exception ex) {
                        throw;
                        try {
                            // TODO change MessageBox to status error message
                            MessageBoxEx.Show("Unhandled error reading data file. \n" + ex.Message, 5000);
                        }
                        catch {
                        }
                        isOK = false;
                    }

                    if (isOK) {

                        DateTime dt = _pop5Data.TimeStamp;

                        if (_startDateTime == DateTime.MinValue) {
                            // we need to set start date/time

                            bool close2Year = false;

                            int startYear = dt.Year;

                            DateTime dtNewYear = new DateTime(startYear + 1, 1, 1, 0, 0, 0);
                            TimeSpan tsShort = new TimeSpan(0, 2, 0);
                            TimeSpan ts2Year = dtNewYear - dt;
                            if (ts2Year < tsShort && ts2Year > TimeSpan.Zero) {
                                // close to end of year, continue into next year
                                close2Year = true;
                            }

                            if (_startDay == 0) {
                                // if start day specified as 0, use current day
                                _startDay = _pop5Data.TimeStamp.DayOfYear;
                            }
                            else {
                                if (close2Year) {
                                    // use specified day of next year
                                    startYear++;
                                    close2Year = false;  // don't adjust year again
                                }
                            }
                            DateTime startDateTimeDay = DacDateTime.FromDayOfYear(startYear, _startDay, 0, 0, 0);
                            _startDateTime = startDateTimeDay + _startTimeSpan;

                            
                            TimeSpan ts2Start = _startDateTime - dt;
                            TimeSpan tsLong = new TimeSpan(360, 0, 0, 0);
                            /*
                            if (ts2Start > tsLong && ts2Start > TimeSpan.Zero) {
                                // first record is nearly a year before start time;
                                //  maybe this should be next year.
                                startYear++;
                                startDateTimeDay = DacDateTime.FromDayOfYear(startYear, _startDay, 0, 0, 0);
                                _startDateTime = startDateTimeDay + _startTimeSpan;
                            }
                             * */

                            if (_endDay == 0) {
                                // if end day specified as 0 make same as start
                                _endDay = _startDay;
                            }
                            int endYear = startYear;
                            if (close2Year) {
                                endYear = dt.Year + 1;
                            }
                            DateTime stopDateTimeDay = DacDateTime.FromDayOfYear(endYear, _endDay, 0, 0, 0);
                            _endDateTime = stopDateTimeDay + _endTimeSpan;

                            if (_startDateTime > _endDateTime) {
                                endYear++;
                                stopDateTimeDay = DacDateTime.FromDayOfYear(endYear, _endDay, 0, 0, 0);
                                _endDateTime = stopDateTimeDay + _endTimeSpan;
                            }

                            TimeSpan ts2End = _endDateTime - dt;
                            /*
                            if (-ts2Start > tsLong && ts2Start < TimeSpan.Zero) {
                                // start time is nearly a year before first record
                                TimeSpan tsLong2 = new TimeSpan(10, 0, 0, 0);
                                if (-ts2End > tsLong2 && ts2End < TimeSpan.Zero) {
                                    // and end time is long before first record
                                    startYear++;
                                    startDateTimeDay = DacDateTime.FromDayOfYear(startYear, _startDay, 0, 0, 0);
                                    _startDateTime = startDateTimeDay + _startTimeSpan;
                                    endYear = startYear;
                                    stopDateTimeDay = DacDateTime.FromDayOfYear(endYear, _endDay, 0, 0, 0);
                                    _endDateTime = stopDateTimeDay + _endTimeSpan;
                                }
                            }
                             * */
                        }

                        // do this so that timeStamp can be read externally,
                        //   even if no other data is kept:
                        DataPackage.RecordTimeStamp = dt;

                        DateTime dt0 = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0);
                        TimeSpan ts = dt - dt0;
                        TimeSpan ts24 = new TimeSpan(24, 0, 0);


                        _progress = (ts.TotalMinutes / ts24.TotalMinutes);

                        if ((_pop5Data.TimeStamp < _startDateTime)) {
                            // record is before beginning
                            SetStatus(ReplayStatus.BeforeStartTime);
                            return false;
                            // keep reading
                        }
                        if ((_pop5Data.TimeStamp > _endDateTime)) {
                            // record is after end time
                            SetStatus(ReplayStatus.AfterEndTime);
                            return false;
                            // done reading
                        }
                        // record is within desired interval
                        if (firstRec) {
                            /*
                            // time before end of desired time interval:
                            TimeSpan ts2End = _endDateTime - dt;
                            TimeSpan tsMin = new TimeSpan(0, 2, 0);
                            if (ts2End < tsMin && ts2End > TimeSpan.Zero) {
                                // first rec is too close to end;
                                // probably want to start with next record
                                TimeSpan tsDay = new TimeSpan(1, 0, 0, 0);
                                //_startDateTime += tsDay;
                                _startDay = _startDateTime.DayOfYear;
                                if (_endDay < _startDay) {
                                    _endDay++;
                                }
                                _startDateTime = DateTime.MinValue;
                                _endDateTime = DateTime.MinValue;
                                //SetStatus(ReplayStatus.BeforeStartTime);
                                // return false;
                                // keep processing
                            }
                             * */
                            firstTimeStamp = _pop5Data.TimeStamp;
                            firstRec = false;
                        }
                    }
                    else {
                        //SetStatus(ReplayStatus.EOF);
                        //return false;

                        // skip this until GetNextFileOfType fixes multiple suffix problem
                        // POPREV: fixed GetNextFile...() for raw.ts files rev 3.17.2
                        // start doing next day
                        string currentInputFile = FileName;
                        string nextFile = DacDataFileBase.GetNextDayFileOfType(currentInputFile);
                        SetStatus(ReplayStatus.EOF);
                        if ((nextFile == currentInputFile) || String.IsNullOrEmpty(nextFile)) {
                            // there is no more
                            return false;
                        }
                        else {
                            // use next file and continue read cycle
                            //_parameters.ReplayPar.InputFile = nextFile;
                            //_replay = new PopNReplay(_parameters.ReplayPar.InputFile);
                            //foundTime = false;
                            FileName = nextFile;
                        }
                    }
                } while (HasStatus(ReplayStatus.EOF));


                if (_pop5Data.Hdr.RassIsOn) {
                    _isRassRecord = true;
                    // POPREV starting with 4.6, handling RASS, at least for passing to output file
                    // TODO: cannot handle RASS; skipping for now (prior to 4.6)
                    //return true;
                }
                else if (_pop5Data.Hdr.WindsSpectrumNumPoints != _pop5Data.Hdr.NPts) {
                    // some files have only partial spectral data, but RASS mode not specified
                    _isRassRecord = true;
                }
                else {
                    _isRassRecord = false;
                }

                if (isOK) {
                    Pop5toPopN(irec, nRecords);
                    actualRecords++;
                }

            }  // end for nRecords loop

            _popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].NSpec *= actualRecords;
            _popNData.RecordTimeStamp = firstTimeStamp;
            if (actualRecords != nRecords) {
                // TODO: we didn't use all nRecords, so spectral average is scaled wrong, but otherwise OK
            }
            
            return isOK;
		}

        /////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        private void Pop5toPopN() {
            Pop5toPopN(0, 1);
        }

        /// <summary>
        /// Converts data read from POP5 file into POPN format
        /// </summary>
        /// <param name="iRec">index of which consecutive record this is</param>
        /// <param name="nRec">Total number of file records to read into one data set</param>
        /// <remarks>
        /// NOTE: selecting multiple records for time series processing (nRec > 1) simply
        ///     multiplies the number of spectral averages by nRec.
        /// </remarks>
		private void Pop5toPopN(int iRec, int nRec) {

			int currentBeam = _pop5Data.CurrentBeamIndex;
			
			_popNData.RecordTimeStamp = _pop5Data.TimeStamp;
			_popNData.Parameters.SystemPar.Altitude = _pop5Data.Hdr.Altitude;
			_popNData.Parameters.SystemPar.Latitude = _pop5Data.Hdr.LatitudeN;
			_popNData.Parameters.SystemPar.Longitude = _pop5Data.Hdr.LongitudeE;
			_popNData.Parameters.SystemPar.MinutesToUT = _pop5Data.Hdr.MinutesToUT;
			_popNData.Parameters.SystemPar.NumberOfRadars = 1;
			_popNData.Parameters.SystemPar.StationName = _pop5Data.Hdr.StationName;

			_popNData.Parameters.SystemPar.RadarPar.BeamDirections[0].Azimuth = _pop5Data.Hdr.Azimuth;
			_popNData.Parameters.SystemPar.RadarPar.BeamDirections[0].Elevation = _pop5Data.Hdr.Elevation;
			_popNData.Parameters.SystemPar.RadarPar.BeamDirections[0].Label = _pop5Data.Hdr.DirName;
			_popNData.Parameters.SystemPar.RadarPar.BeamDirections[0].SwitchCode = _pop5Data.Hdr.AntSwitchCode;
			for (int idir = 1; idir < _popNData.Parameters.ArrayDim.MAXDIRECTIONS; idir++) {
				_popNData.Parameters.SystemPar.RadarPar.BeamDirections[idir].Azimuth = 0.0;
				_popNData.Parameters.SystemPar.RadarPar.BeamDirections[idir].Elevation = 0.0;
				_popNData.Parameters.SystemPar.RadarPar.BeamDirections[idir].Label = "_";
			}

			_popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].AttenuatedGates = _pop5Data.Hdr.Atten;
			_popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].BwCode = _pop5Data.Hdr.BwCode;
            // POPREV 4.14.2 integer truncation error corrected 
            //_popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].IppMicroSec = _pop5Data.Hdr.IPP / 1000;
            _popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].IppMicroSec = _pop5Data.Hdr.IPP / 1000.0;
            //
            _popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].NCI = _pop5Data.Hdr.NCI;
			_popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].NCode = _pop5Data.Hdr.NCode;
			_popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].NHts = _pop5Data.Hdr.NHts;

            if (_pop5Data.Hdr.HasFMRawTimeSeries) {
                // for POP compatibility, #samples is stored as nhts in raw.ts files
                int nSamp = _pop5Data.Hdr.NHts;
                _popNData.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts = nSamp;
                int numHts = nSamp / 2 + 1;
                _popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].NHts = numHts;
            }
            else {
                _popNData.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts = 0;
            }

			_popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].NPts = _pop5Data.Hdr.NPts;
			_popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].NSpec = _pop5Data.Hdr.NSpec;
			_popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].PulseWidthNs = _pop5Data.Hdr.PW;
			_popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].SampleDelayNs = _pop5Data.Hdr.Delay;
			_popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].SpacingNs = _pop5Data.Hdr.Spacing;
			_popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].SystemDelayNs = _pop5Data.Hdr.SysDelay;
			_popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].TxPhaseFlipIsOn = _pop5Data.Hdr.Flip == 1 ? true : false;
			for (int ipar = 1; ipar < _popNData.Parameters.ArrayDim.MAXBEAMPAR; ipar++) {
				_popNData.Parameters.SystemPar.RadarPar.BeamParSet[ipar].AttenuatedGates = 0;
				_popNData.Parameters.SystemPar.RadarPar.BeamParSet[ipar].BwCode = 0;
				_popNData.Parameters.SystemPar.RadarPar.BeamParSet[ipar].IppMicroSec = 0;
				_popNData.Parameters.SystemPar.RadarPar.BeamParSet[ipar].NCI = 0;
				_popNData.Parameters.SystemPar.RadarPar.BeamParSet[ipar].NCode = 0;
				_popNData.Parameters.SystemPar.RadarPar.BeamParSet[ipar].NHts = 0;
				_popNData.Parameters.SystemPar.RadarPar.BeamParSet[ipar].NPts = 0;
				_popNData.Parameters.SystemPar.RadarPar.BeamParSet[ipar].NSpec = 0;
				_popNData.Parameters.SystemPar.RadarPar.BeamParSet[ipar].PulseWidthNs = 0;
				_popNData.Parameters.SystemPar.RadarPar.BeamParSet[ipar].SampleDelayNs = 0;
				_popNData.Parameters.SystemPar.RadarPar.BeamParSet[ipar].SpacingNs = 0;
				_popNData.Parameters.SystemPar.RadarPar.BeamParSet[ipar].SystemDelayNs = 0;
				_popNData.Parameters.SystemPar.RadarPar.BeamParSet[ipar].TxPhaseFlipIsOn = false;
			}

			_popNData.Parameters.SystemPar.RadarPar.BeamSequence[0].DirectionIndex = 0;
			_popNData.Parameters.SystemPar.RadarPar.BeamSequence[0].NumberOfReps = 1;
			_popNData.Parameters.SystemPar.RadarPar.BeamSequence[0].ParameterIndex = 0;
			// POPREV dac ver 2.1 fixed typo, [ibm] in for loop was [0] previously
			for (int ibm = 1; ibm < _popNData.Parameters.ArrayDim.MAXBEAMS; ibm++) {
				_popNData.Parameters.SystemPar.RadarPar.BeamSequence[ibm].DirectionIndex = 0;
				_popNData.Parameters.SystemPar.RadarPar.BeamSequence[ibm].NumberOfReps = 0;
				_popNData.Parameters.SystemPar.RadarPar.BeamSequence[ibm].ParameterIndex = 0;
			}

			_popNData.Parameters.SystemPar.RadarPar.MaxTxDutyCycle = 0;		// not available in Hdr struct
			_popNData.Parameters.SystemPar.RadarPar.MaxTxLengthUsec = 0;	// not available in Hdr struct
			_popNData.Parameters.SystemPar.RadarPar.MinIppUsec = 0;			// not available in Hdr struct

			_popNData.Parameters.SystemPar.RadarPar.PBConstants.PBPostBlank = _pop5Data.Hdr.PBPostBlank;
			_popNData.Parameters.SystemPar.RadarPar.PBConstants.PBPostTR = _pop5Data.Hdr.PBPostTR;
			_popNData.Parameters.SystemPar.RadarPar.PBConstants.PBPreBlank = _pop5Data.Hdr.PBPreBlank;
			_popNData.Parameters.SystemPar.RadarPar.PBConstants.PBPreTR = _pop5Data.Hdr.PBPreTR;
			_popNData.Parameters.SystemPar.RadarPar.PBConstants.PBSynch = _pop5Data.Hdr.PBSynch;

            if (!_pop5Data.Hdr.HasRassSpectra) {
                // if we have no rass spectra in file, rass regions are meaningless
                _popNData.Parameters.SystemPar.RadarPar.ProcPar.Dop0 = _pop5Data.Hdr.WindsSpectrumBeginIndex + 1;
                _popNData.Parameters.SystemPar.RadarPar.ProcPar.Dop1 = _pop5Data.Hdr.WindsSpectrumNumPoints;
                _popNData.Parameters.SystemPar.RadarPar.ProcPar.Dop2 = 1;
                _popNData.Parameters.SystemPar.RadarPar.ProcPar.Dop3 = 0;
                // say that this is not a RASS record:
                _popNData.Parameters.SystemPar.RadarPar.ProcPar.RassSourceParams[0] = 0;
                _pop5Data.Hdr.RassIsOn = false;
            }
            else {
                // if we have rass spectra, these parameters determine size of spectra;
                //  BUT if we recompute spectra this will cause trouble !!
                _popNData.Parameters.SystemPar.RadarPar.ProcPar.Dop0 = _pop5Data.Hdr.WindsSpectrumBeginIndex + 1;
                _popNData.Parameters.SystemPar.RadarPar.ProcPar.Dop1 = _pop5Data.Hdr.WindsSpectrumNumPoints;
                _popNData.Parameters.SystemPar.RadarPar.ProcPar.Dop2 = _pop5Data.Hdr.RassSpectrumBeginIndex + 1;
                _popNData.Parameters.SystemPar.RadarPar.ProcPar.Dop3 = _pop5Data.Hdr.RassSpectrumNumPoints;
            }
            // POPREV ver 4.6 save RASS source parameters for rewrite to output file
            if (_pop5Data.Hdr.RassIsOn) {
                _popNData.Parameters.SystemPar.RadarPar.ProcPar.RassSourceParams[0] = 1;
            }
            else {
                _popNData.Parameters.SystemPar.RadarPar.ProcPar.RassSourceParams[0] = 0;
            }
            _popNData.Parameters.SystemPar.RadarPar.ProcPar.RassSourceParams[1] = _pop5Data.Hdr.RassLowFrequencyHz;
            _popNData.Parameters.SystemPar.RadarPar.ProcPar.RassSourceParams[2] = _pop5Data.Hdr.RassHighFrequencyHz;
            _popNData.Parameters.SystemPar.RadarPar.ProcPar.RassSourceParams[3] = _pop5Data.Hdr.RassStepHz;
            _popNData.Parameters.SystemPar.RadarPar.ProcPar.RassSourceParams[4] = _pop5Data.Hdr.RassDwellMs;
            _popNData.Parameters.SystemPar.RadarPar.ProcPar.RassSourceParams[5] = _pop5Data.Hdr.RassSweep;
            //
			_popNData.Parameters.SystemPar.RadarPar.ProcPar.IsDcFiltering = _pop5Data.Hdr.DcFilter > 0 ? true : false;
            // POPREV 3.19.5 fixed test for ICRA specAvg == 1 instead of > 1
			_popNData.Parameters.SystemPar.RadarPar.ProcPar.IsIcraAvg = _pop5Data.Hdr.SpecAvg == 1 ? true : false;
			_popNData.Parameters.SystemPar.RadarPar.ProcPar.IsWindowing = _pop5Data.Hdr.Window > 0 ? true : false;
            // POPREV DAC corrected factor from *10 to /10 for POPN 3.2 20110929
			_popNData.Parameters.SystemPar.RadarPar.ProcPar.MaxClutterHtKm = _pop5Data.Hdr.CltrHt / 10.0; // TODO check multiplier
            //TextFile.WriteLineToFile("GCLog.txt", "READING  Clutter Ht km  = " + _popNData.Parameters.SystemPar.RadarPar.ProcPar.MaxClutterHtKm.ToString(), true);
            //TextFile.WriteLineToFile("GCLog.txt", "READING  HDR CltrHt Ht  = " + _pop5Data.Hdr.CltrHt.ToString(), true);
            _popNData.Parameters.SystemPar.RadarPar.NumOtherInstruments = _pop5Data.Hdr.NMet;
            if (_pop5Data.Hdr.NMet > 0) {
                _popNData.Parameters.SystemPar.RadarPar.OtherInstrumentCodes = new int[_pop5Data.Hdr.NMet];
                if (_pop5Data.Hdr.MetCodes != null) {
                    for (int i = 0; i < _pop5Data.Hdr.NMet; i++) {
                        _popNData.Parameters.SystemPar.RadarPar.OtherInstrumentCodes[i] = _pop5Data.Hdr.MetCodes[i];
                    }
                }
            }
            _popNData.Parameters.SystemPar.RadarPar.ProcPar.NumberOfRx = _pop5Data.Hdr.NRx;
			//_popNData.Parameters.SystemPar.RadarPar.ProcPar.IsWritingPopFile = 
			//_popNData.Parameters.SystemPar.RadarPar.ProcPar.PopFilePathName =

			_popNData.Parameters.SystemPar.RadarPar.RadarID = _pop5Data.Hdr.RadarID;
			_popNData.Parameters.SystemPar.RadarPar.RadarName = _pop5Data.Hdr.RadarName;
			_popNData.Parameters.SystemPar.RadarPar.RxBw[0].BwDelayNs = _pop5Data.Hdr.SysDelay;
			_popNData.Parameters.SystemPar.RadarPar.RxBw[0].BwPwNs = _pop5Data.Hdr.PW;
			// we are putting the relevant sysdelay into index 0 of RxBw[]
			_popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].BwCode = 0;
			for (int ibw = 1; ibw < _popNData.Parameters.ArrayDim.MAXBW; ibw++) {
				_popNData.Parameters.SystemPar.RadarPar.RxBw[ibw].BwDelayNs = 0;
				_popNData.Parameters.SystemPar.RadarPar.RxBw[ibw].BwPwNs = 0;
			}

			_popNData.Parameters.SystemPar.RadarPar.TxFreqMHz = _pop5Data.Hdr.TxFreq/1.0e6;
			_popNData.Parameters.SystemPar.RadarPar.TxIsOn = _pop5Data.Hdr.TxIsOn;

            _popNData.Parameters.ReplayPar = _replayPar;

            //
            // allocate data arrays in _popNData object
            //

            /////////////
            // What follows puts some parameters from the replay parameter file into the parameter object that goes with the data from the data file.
            // Most of the processing parameters are copied over in PopNDwellWorker.GetReplayData, but some are done here.
            // I have not yet verified if these can be safely moved to that later location.

            // pass cross-corr lag, because it is not in POP data file
            //  but it is required here to allocate cross-corr array
            _popNData.Parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrMaxLag = _processingPar.SystemPar.RadarPar.FmCwParSet[0].XCorrMaxLag;
            _popNData.Parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrUseFFT = _processingPar.SystemPar.RadarPar.FmCwParSet[0].XCorrUseFFT;
            _popNData.Parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrLineFitPts = _processingPar.SystemPar.RadarPar.FmCwParSet[0].XCorrLineFitPts;
            _popNData.Parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrFilterFraction = _processingPar.SystemPar.RadarPar.FmCwParSet[0].XCorrFilterFraction;
            _popNData.Parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrPolyFitOrder = _processingPar.SystemPar.RadarPar.FmCwParSet[0].XCorrPolyFitOrder;
            _popNData.Parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrLagsToCurveFit = _processingPar.SystemPar.RadarPar.FmCwParSet[0].XCorrLagsToCurveFit;
            _popNData.Parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrLagsToInterpolate = _processingPar.SystemPar.RadarPar.FmCwParSet[0].XCorrLagsToInterpolate;
            _popNData.Parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrAdjustBase = _processingPar.SystemPar.RadarPar.FmCwParSet[0].XCorrAdjustBase;

            // put processing parameters in with replay data parameters
            _popNData.Parameters.SystemPar.RadarPar.ProcPar.AllocTSOnly = _processingPar.SystemPar.RadarPar.ProcPar.AllocTSOnly;
            _popNData.Parameters.SystemPar.RadarPar.ProcPar.CnsPar[0].CnsEnable = _processingPar.SystemPar.RadarPar.ProcPar.CnsPar[0].CnsEnable;
            _popNData.Parameters.SystemPar.RadarPar.ProcPar.CnsPar[0].CnsFilePath = _processingPar.SystemPar.RadarPar.ProcPar.CnsPar[0].CnsFilePath;
            _popNData.Parameters.SystemPar.RadarPar.AntSpacingM = _processingPar.SystemPar.RadarPar.AntSpacingM;
            _popNData.Parameters.SystemPar.RadarPar.ASubH = _processingPar.SystemPar.RadarPar.ASubH;
            _popNData.Parameters.SystemPar.RadarPar.ProcPar.DoAutoCorr1Rx = _processingPar.SystemPar.RadarPar.ProcPar.DoAutoCorr1Rx;

            _popNData.Parameters.SystemPar.RadarPar.RadarType = _processingPar.SystemPar.RadarPar.RadarType;


            /**/
            // use nspec on fmcw page for timeseries processing
            if (_processingPar.ReplayPar.UseFMCWNSpecOnReplay) {
                int oldNpts = _popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].NPts;
                int oldNSpec = _popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].NSpec;
                int totalPts = oldNpts * oldNSpec;
                int newNpts = totalPts / _processingPar.SystemPar.RadarPar.FmCwParSet[0].DopplerNSpec;
                _popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].NPts = newNpts;
                //_popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].NSpec = _processingPar.SystemPar.RadarPar.BeamParSet[0].NSpec;
                _popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].NSpec = _processingPar.SystemPar.RadarPar.FmCwParSet[0].DopplerNSpec;
                // TODO: assuming no RASS:
                _popNData.Parameters.SystemPar.RadarPar.ProcPar.Dop0 = 1;
                _popNData.Parameters.SystemPar.RadarPar.ProcPar.Dop1 = newNpts;
            }
             /* */

            int nSamples = _popNData.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts;
			int nhts = _popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].NHts;
			int npts = _popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].NPts;
            int nptsInArray;
            //
            int nrx = _popNData.Parameters.SystemPar.RadarPar.ProcPar.NumberOfRx;
            int nspec = _popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].NSpec;
            if (_isRassRecord) {
                nptsInArray = _popNData.Parameters.SystemPar.RadarPar.ProcPar.Dop1 +
                                _popNData.Parameters.SystemPar.RadarPar.ProcPar.Dop3;
            }
            else {
                nptsInArray = npts;
            }
            if (_popNData.Parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.PulsedTx) {
                // For RASS-type records not indicated as RASS:
                // this is number of points read in from spectral array
                // BUT total points in Doppler spec is 
                //  _popNData.Parameters.SystemPar.RadarPar.BeamParSet[0].NPts
                nptsInArray = _popNData.Parameters.SystemPar.RadarPar.ProcPar.Dop1 +
                        _popNData.Parameters.SystemPar.RadarPar.ProcPar.Dop3;
            }

            _memAllocator.Parameters = _popNData.Parameters;
            _memAllocator.AllocateDataArrays(_popNData);

            /*
            _popNData.MeanDoppler = _memAllocator.MeanDoppler;
            _popNData.Noise = _memAllocator.Noise;
            _popNData.Power = _memAllocator.Power;
            _popNData.Width = _memAllocator.Width;
            _popNData.ClutterPoints = _memAllocator.ClutterPoints;

            _popNData.Spectra = _memAllocator.Spectra;
            _popNData.XCorrelation = _memAllocator.XCorrelation;
            _popNData.SampledTimeSeries = _memAllocator.SampledTimeSeries;
            _popNData.TransformedTimeSeries = _memAllocator.DopplerTimeSeries;
             * */

            /*

			_popNData.MeanDoppler = new double[nrx][];
            _popNData.Noise = new double[nrx][];
            _popNData.Power = new double[nrx][];
            _popNData.Width = new double[nrx][];
            _popNData.ClutterPoints = new int[nrx][];

            if (_pop5Data.Spectra != null) {
                _popNData.Spectra = new double[nrx][][];
            }
            int numTS = 0;
            if (_pop5Data.Hdr.HasFullTimeSeries) {
                numTS = _pop5Data.Hdr.NSpec;
            }
            else if (_pop5Data.Hdr.HasShortTimeSeries) {
                numTS = 1;
            }
            if (numTS > 0) {
                if (_pop5Data.Hdr.HasFMRawTimeSeries) {
                    _popNData.SampledTimeSeries = new double[nrx][][][];
                    // raw time series file has nhts equal to number of samples
                    //nhts = nhts / 2 + 1;
                }
                else {
                    _popNData.TransformedTimeSeries = new ipp.Ipp64fc[nrx][][][];
                }
            }

            for (int irx = 0; irx < nrx; irx++) {
                _popNData.MeanDoppler[irx] = new double[nhts];
                _popNData.Noise[irx] = new double[nhts];
                _popNData.Power[irx] = new double[nhts];
                _popNData.Width[irx] = new double[nhts];
                _popNData.ClutterPoints[irx] = new int[nhts];
                if (_pop5Data.Spectra != null) {
                    _popNData.Spectra[irx] = new double[nhts][];
                }
                if (numTS > 0) {
                    if (_pop5Data.Hdr.HasFMRawTimeSeries) {
                        _popNData.SampledTimeSeries[irx] = new double[numTS][][];
                        for (int its = 0; its < numTS; its++) {
                            _popNData.SampledTimeSeries[irx][its] = new double[npts][];
                            for (int ipt = 0; ipt < npts; ipt++) {
                                _popNData.SampledTimeSeries[irx][its][ipt] = new double[nSamples];
                            }
                        }
                    }
                    else {
                        _popNData.TransformedTimeSeries[irx] = new ipp.Ipp64fc[numTS][][];
                        for (int its = 0; its < numTS; its++) {
                            _popNData.TransformedTimeSeries[irx][its] = new ipp.Ipp64fc[nhts][];
                            for (int iht = 0; iht < nhts; iht++) {
                                _popNData.TransformedTimeSeries[irx][its][iht] = new ipp.Ipp64fc[npts];
                            }
                       }
                    }
                }
            }
            */

            int numTS = 0;
            if (_pop5Data.Hdr.HasFMRawTimeSeries) {
                numTS = nspec;
            }
            else if (_pop5Data.Hdr.HasFullTimeSeries) {
                //numTS = _pop5Data.Hdr.NSpec;
                numTS = nspec;
            }
            else if (_pop5Data.Hdr.HasShortTimeSeries) {
                numTS = 1;
            }

			double Nyq = _pop5Data.Nyquist;
            // POPREV as of 4.6, allow RASS
            //if (!_pop5Data.Hdr.RassIsOn) {
            if (true) {

                // if we read moment data, transfer them
                if (_pop5Data.Vel != null) {
                    for (int irx = 0; irx < nrx; irx++) {
                        for (int iht = 0; iht < nhts; iht++) {
                            // converting vel m/s to Nyquists
                            _popNData.MeanDoppler[irx][iht] = _pop5Data.Vel[irx, 0, iht] / Nyq;
                            _popNData.Width[irx][iht] = _pop5Data.Width[irx, 0, iht] / Nyq;
                            _popNData.Noise[irx][iht] = _pop5Data.Noise[irx, 0, iht];
                            _popNData.Power[irx][iht] = npts * _popNData.Noise[irx][iht] * Math.Pow(10.0, _pop5Data.Snr[irx, 0, iht] / 10.0);
                        }
                    }
                }
                if (_pop5Data.Hdr.RassIsOn) {
                    if (_pop5Data.Vel != null) {
                        for (int irx = 0; irx < nrx; irx++) {
                            for (int iht = 0; iht < nhts; iht++) {
                                // converting vel m/s to Nyquists
                                _popNData.RassMeanDoppler[irx][iht] = _pop5Data.Vel[irx, 1, iht] / Nyq;
                                _popNData.RassWidth[irx][iht] = _pop5Data.Width[irx, 1, iht] / Nyq;
                                _popNData.RassPower[irx][iht] = npts * _popNData.Noise[irx][iht] * Math.Pow(10.0, _pop5Data.Snr[irx, 1, iht] / 10.0);
                                //_popNData.RassTemp[irx][iht] = _pop5Data.Temp[irx, 1, iht];  // temp has not been carried over from input file
                            }
                        }
                    }
                }

                // get spectra data if they exist
                if (_pop5Data.Spectra == null) {
                    if (!_replayPar.ProcessRawSamples && !_replayPar.ProcessTimeSeries) {
                        // we will need spectral array if processing time series, otherwise:
                        _popNData.Spectra = null;
                    }
                }
                if (_pop5Data.Spectra != null) {
                    int ub = _pop5Data.Spectra.GetUpperBound(2);
                    if (_pop5Data.Spectra.GetUpperBound(2) >= (nptsInArray-1)) {
                        // Don't read spectra if we have changed the dimension of spectral pts.
                        //  That should mean we are recalculating spectra anyway.
                        for (int irx = 0; irx < nrx; irx++) {
                            for (int iht = 0; iht < nhts; iht++) {
                                for (int ipts = 0; ipts < nptsInArray; ipts++) {
                                    if (_pop5Data.Spectra != null) {
                                        if (iRec == 0) {
                                            _popNData.Spectra[irx][iht][ipts] = _pop5Data.Spectra[irx, iht, ipts] / nRec;
                                        }
                                        else {
                                            // POPREV 4.12.4  allow averaging of spectra across records
                                            _popNData.Spectra[irx][iht][ipts] += _pop5Data.Spectra[irx, iht, ipts] / nRec;
                                        }
                                    }
                                    else {
                                        _popNData.Spectra[irx][iht][ipts] = 0.0;
                                    }
                                }
                            }
                        }
                    }
                }

                for (int irx = 0; irx < nrx; irx++)	{

                    if (numTS > 0) {
                        // we have timeseries data
                        if (_pop5Data.Hdr.HasFMRawTimeSeries) {
                            // raw time series
                            for (int isam = 0; isam < nSamples; isam++) {
                                for (int ispec = 0; ispec < numTS; ispec++) {
                                    for (int ipt = 0; ipt < npts; ipt++) {
                                        _popNData.SampledTimeSeries[irx][ispec + iRec * nspec][ipt][isam] = _pop5Data.TimeSeries[irx, isam, ispec * npts + ipt, 0];
                                    }
                                }
                            }
                        }
                        else {
                            // we have Doppler time series
                            for (int iht = 0; iht < nhts; iht++) {
                                for (int ispec = 0; ispec < numTS; ispec++) {
                                    for (int ipt = 0; ipt < npts; ipt++) {
                                        _popNData.TransformedTimeSeries[irx][ispec + iRec * nspec][iht][ipt].re = _pop5Data.TimeSeries[irx, iht, ispec * npts + ipt, 0];
                                        _popNData.TransformedTimeSeries[irx][ispec + iRec * nspec][iht][ipt].im = _pop5Data.TimeSeries[irx, iht, ispec * npts + ipt, 1];
                                    }
                                }
                            }
                        }

                    }  // end get time series data
                    else {
                        _popNData.SampledTimeSeries = null;
                        _popNData.TransformedTimeSeries = null;
                    }
                }  // end for irx loop
            }  // end if not RASS

			DateTime dt = _popNData.RecordTimeStamp;
			DateTime dt0 = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0);
			TimeSpan ts = dt - dt0;
			TimeSpan ts24 = new TimeSpan(24, 0, 0);

			_progress = (ts.TotalMinutes / ts24.TotalMinutes); 


		}  // end of Pop5toPopN

        // POPREV: Close() added rev 3.15.1

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
            }
            if (_popFile != null) {
                _popFile.Close();
            }
        }

        public void Close() {
            Dispose();
        }

        ~PopNReplay() {
            Dispose(false);
        }

    }
}
