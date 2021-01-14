using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using DACarter.PopUtilities;
using DACarter.Utilities;

namespace POPN {

	class MeltingLayerCalculator3 {

		//public PopDataPackage data;
		//private PopDataPackage _data;

		// DAC added 20091130 for POPN 2.7
		// In FMCW real-time mode, the sample delay from the parameter set is really
		//	the delay to the first gate that will be saved to the data file.
		//	If we are not saving the first 'n' gates, then the gate index offset is 'n',
		//	since in these routines the "unsaved" gates are still at the beginning 
		//	of the arrays.
		private int _gateIndexOffset;

		private PopParameters _parameters;
		private PopParameters _firstParameters;
		private PopParameters.MeltingLayerParameters _meltParameters;
		private int _intervalMinutes;
		private DateTime _calcTime;
		private int _nRecords;
		private int _missingMomentsCount;
		private bool _LogFileIsEnabled;
		private List<int> _brightBandDwellIndexList;
		private int m_iBrightBandHeightM_PreviousHour;

		private string _logFileFullPath;

		//private const double MperNs = 0.149896229;  // meters per nsec delay (vaccuum)
		private const double MperNs = 0.149852322;  // meters per nsec delay (air at STP)

		// Constant that might change to a parameter in the future
		private const double _minDvvBb = 0.8;

		private double[] MinSnrRain, MinSnrBB, DvvBbOnlyMinSnr;


		public string ErrorMessage;

		private List<double[]> _noiseLevList;
		private List<double[]> _mDopList;
		private List<double[]> _powList;
		private List<double[]> _snrList;
		private List<double[]> _widthList;
		private List<DateTime> _timeStampList;

		private struct structBrightBand {
			public int iDwellIndex;
			public DateTime lTimeStamp;
			public int iGate;
			public double fPeakSnr;
			public double fHeight;
		}

		private List<structBrightBand> _brightBandList;

		public struct structBrightBandInfo {
			public string strModeName;
			public double       fBrightBandHeightM;
			public int iNumBbInAverage;
			public int  iNumVertProfiles;
			public int iNumVertBb;
			public int iNumOblBb;
			public int iNumPassRainSnr;
			public int iNumPassRainDvv;
			public int iNumPassRainTotal;
			public int iNumPassBbDeltaSnr;
			public int iNumPassBbDeltaDvv;
			public int iNumPassBbDeltaTotal;
			public int iNumPassBbMinSnr;
			public int iNumPassBbMinDvv;
			public int iNumPassBbMinTotal;
			public int iNumPassBbTotal;
			public int iNumRainGatesAboveBb;
			public DateTime dataMiddleTime;
		}

		public structBrightBandInfo BrightBandInfo;

		public PopParameters Parameters {
			set {
                _parameters = value;
                _meltParameters = _parameters.MeltingLayerPar;
                _intervalMinutes = _parameters.MeltingLayerPar.CalculateEveryMinute;
                _logFileFullPath = _parameters.MeltingLayerPar.LogFileFolder;
                _logFileFullPath = Path.Combine(_parameters.MeltingLayerPar.LogFileFolder, "MeltingLayerLog.txt");
                if (_logFileFullPath.Trim() == String.Empty) {
                    _LogFileIsEnabled = false;
                }
                else {
                    _LogFileIsEnabled = _parameters.MeltingLayerPar.WriteLogFile;
                }
                // calculate gate offset due to not saving all gates in data file
                _gateIndexOffset = 0;
                if (!_parameters.ReplayPar.Enabled &&
                    (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCw)) {
                    _gateIndexOffset = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateFirst;
                }


            }
			get {return _parameters;}
		}

		public MeltingLayerCalculator3() {
            _parameters = null;
            Init();
            // this reset only in ctor, not when reinitializing:
            PreviousBrightBandHeightM = -1;
        }

		/// <summary>
		/// public constructor
		/// </summary>
		/// <param name="par"></param>
		public MeltingLayerCalculator3(PopParameters par) {
			Parameters = par;
            Init();
            // this reset only in ctor, not when reinitializing:
            PreviousBrightBandHeightM = -1;
		}

        public void StartNewInterval(DateTime currentTime) {
            ClearData();
            CalculateNextTime(currentTime);
        }

        public void ClearData() {
            // don't put anything here that depends upon parameters; use property set()
            _noiseLevList.Clear();
            _mDopList.Clear();
            _widthList.Clear();
            _powList.Clear();
            _snrList.Clear();
            _timeStampList.Clear();
            _brightBandDwellIndexList.Clear();
            _nRecords = 0;
            _missingMomentsCount = 0;
        }

        public void Init() {
            // don't put anything here that depends upon parameters; use property set()
            _noiseLevList = new List<double[]>();
            _mDopList = new List<double[]>();
            _powList = new List<double[]>();
            _snrList = new List<double[]>();
            _widthList = new List<double[]>();
            _timeStampList = new List<DateTime>();
            _brightBandList = new List<structBrightBand>();
            _brightBandDwellIndexList = new List<int>();
            _calcTime = DateTime.MinValue;
            ClearData();
        }


		//////////////////////////////////////////////////////////////////////////////
		///
		/// <summary>
		/// Determine the next time to calculate melting layer
		/// </summary>
		/// <param name="currentTime"></param>
		private void CalculateNextTime(DateTime currentTime) {
			DateTime beginHour = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, 0, 0);
			DateTime nextHour = beginHour.AddMinutes(60);
			DateTime time = beginHour;
			while (time <= currentTime) {
				time = time.AddMinutes(_intervalMinutes);
				if (time > nextHour) {
					time = nextHour;
				}
			}
			_calcTime = time;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentTime"></param>
        /// <param name="endOfData"></param>
        /// <returns></returns>
        public bool PastEndOfInterval(DateTime currentTime, bool endOfData = false) {
            if (endOfData || currentTime >= _calcTime) {
                // end of current interval, start new one.
                return true;
            }
            else {
                return false;
            }
        }

        //////////////////////////////////////////////////////////////////////////////
		///
		/// <summary>
		/// Is it time to calculate melting layer?
		/// </summary>
		/// <param name="currentTime"></param>
		/// <returns></returns>
        /*
		public bool TimeToCalculate(DateTime currentTime, bool endOfData) { 
			if (endOfData || currentTime >= _calcTime) {
				// end of current interval, start new one.
				// save middle time of current calculation:
				if (_nRecords != 0) {
					BrightBandInfo.dataMiddleTime = _calcTime.AddMinutes(-_intervalMinutes / 2.0);
				}
                if (!endOfData) {
                    CalculateNextTime(currentTime);
                }
				if (_nRecords == 0) {
					// went past end time without getting any data
					return false;
				}
				else {
					return true;
				}
			}
			else {
				return false;
			}
		
        */

		//////////////////////////////////////////////////////////////////////////////
		///
		/// <summary>
		/// Add current dwell to list of dwells for current averaging interval
		/// </summary>
		/// <param name="data"></param>
		public void Add(PopDataPackage3 data) {
			// make sure all dwells added have same parameters as first one
			if (_nRecords == 0) {
				_firstParameters = data.Parameters;
				_parameters = data.Parameters;
			}
			else if (_firstParameters != data.Parameters) {
				return;
			}
            double nyquist = data.Parameters.GetBeamParNyquist(0);
			//double nyquist = GetNyquist(data);
            // TODO: dac account for multiple rx
			double[] powArray = (double[])data.Power[0].Clone();
			double[] noiseArray = (double[])data.Noise[0].Clone();
			double[] velocity = (double[])data.MeanDoppler[0].Clone();
			double[] width = (double[])data.Width[0].Clone();
			for (int i = 0; i < velocity.Length; i++) {
				velocity[i] = velocity[i] * nyquist;
				width[i] = width[i] * nyquist;
			}
			_noiseLevList.Add(noiseArray);
			_powList.Add(powArray);
			_mDopList.Add(velocity);
			_widthList.Add(width);
			_snrList.Add(CreateSNR(powArray, noiseArray, data.Parameters.SystemPar.RadarPar.BeamParSet[0].NPts));
			_timeStampList.Add(data.RecordTimeStamp);
			_nRecords++;
		}

		private double[] CreateSNR(double[] power, double[] noise, int npts) {
			int nhts = power.Length;
			//int npts = _parameters.SystemPar.RadarPar.BeamParSet[0].NPts;
            //double[] newNoiseArray;

            double newNoise = 0.0;
            if (_parameters.MeltingLayerPar.ModifyNoiseLevels) {
                // use mean of upper nGates as noise level everywhere
                //newNoiseArray = new double[nhts];
                int loGate = _parameters.MeltingLayerPar.NoiseGateLoIndex;
                int hiGate = _parameters.MeltingLayerPar.NoiseGateHiIndex;
                if (hiGate > nhts - 1) {
                    hiGate = nhts - 1;
                }
                if (hiGate < 0) {
                    hiGate = 0;
                }
                if (loGate > nhts - 1) {
                    loGate = nhts - 1;
                }
                if (loGate < 0) {
                    loGate = 0;
                }
                int nGates = hiGate - loGate + 1;
                if (nGates > 0) {
                    double sum = 0.0;
                    for (int i = loGate; i <= hiGate; i++) {
                        sum += noise[i];
                    }
                    newNoise = sum / nGates;
                }
            }

			double[] snr = new double[nhts];
            double noiseLevel;
			for (int i = 0; i < nhts; i++) {
                if (_parameters.MeltingLayerPar.ModifyNoiseLevels) {
                    noiseLevel = newNoise;
                }
                else {
                    noiseLevel = noise[i];
                }
				snr[i] = 10.0*Math.Log10(power[i] / (noiseLevel * npts));
			}
			return snr;
		}

		private double GetNyquist(PopDataPackage3 data) {
			double hzms = (149.896 / data.Parameters.SystemPar.RadarPar.TxFreqMHz);
			double ipp = data.Parameters.SystemPar.RadarPar.BeamParSet[0].IppMicroSec;
			double nci = data.Parameters.SystemPar.RadarPar.BeamParSet[0].NCI;
			if (nci == 0) {
				nci = 1;
			}
			double nyq = 0.0;
			if (ipp != 0.0) {
				nyq = (float)(0.5e6 * hzms / ipp / nci);    // nyquist freq in m/s 
			}
			return nyq;
		}

		//////////////////////////////////////////////////////////////////////////////
		///
		/// <summary>
		/// CalculateResult(): Calls the methods necessary to find the Bright Band in this 
		/// calculation period
		/// </summary>
		/// <param name="iBrightBandHeightM_PreviousHour"></param>
		public void CalculateResult(int iBrightBandHeightM_PreviousHour) {

			m_iBrightBandHeightM_PreviousHour = iBrightBandHeightM_PreviousHour;

			// Initialize Variables
			BrightBandInfo.fBrightBandHeightM = -9999.0f;
			BrightBandInfo.iNumBbInAverage = -99;
			BrightBandInfo.iNumVertBb = 0;
			BrightBandInfo.iNumOblBb = 0;
			BrightBandInfo.iNumPassBbDeltaDvv = 0;
			BrightBandInfo.iNumPassBbDeltaSnr = 0;
			BrightBandInfo.iNumPassBbDeltaTotal = 0;
			BrightBandInfo.iNumPassBbMinDvv = 0;
			BrightBandInfo.iNumPassBbMinSnr = 0;
			BrightBandInfo.iNumPassBbMinTotal = 0;
			BrightBandInfo.iNumPassBbTotal = 0;
			BrightBandInfo.iNumPassRainDvv = 0;
			BrightBandInfo.iNumPassRainSnr = 0;
			BrightBandInfo.iNumPassRainTotal = 0;
			BrightBandInfo.iNumVertProfiles = 0;
			BrightBandInfo.iNumRainGatesAboveBb = -99;
			BrightBandInfo.strModeName = "??";
			_missingMomentsCount = 0;

			if (_nRecords == 0) {
				return;
			}

			if(_LogFileIsEnabled) {
				DateTime firstTime = _timeStampList[0];
				_logFileFullPath = _parameters.MeltingLayerPar.LogFileFolder;
				_logFileFullPath = Path.Combine(_parameters.MeltingLayerPar.LogFileFolder,
									String.Format("MeltingLayer_{0,0000}_{1,000}.log", firstTime.Year, firstTime.DayOfYear));
				LogFileWriteLine("\n------------------------------------------------\n"); 
				LogFileWriteLine(String.Format("First Dwell in calculation period starts at: {0}, Day {1}, {2:HH:mm:ss}\n",
					firstTime.Year,
					firstTime.DayOfYear,
					firstTime)); 
			}

			// Apply range correction
			CorrectRange();

			// Find vertical dwells that contain Bright Band
			FindVerticalBrightBand();

			// Did any verticals contain Bright Band
			if (_brightBandList.Count == 0) {
				ClearData();
				return;
			}

			// Find the peak SNR in the obliques closest to the verticals that contain Bright Band
			//FindBrightBandInObliques();

			// Are there enough Bright Bands to continue?
			// dac note: Here, BrightBandPercent is applied to TOTAL number of BB's found
			//		including those that might later be rejected as outliers.
			int iTotalVerticalBrightBands = _brightBandList.Count;
			if (iTotalVerticalBrightBands < (int)((BrightBandInfo.iNumVertProfiles * (_meltParameters.BrightBandPercent / 100.0)) + 0.5)) {
				ClearData();
				return;
			}

			// Calculate Bright Band Heights
			CalculateBrightBandHeights();

			// Perform Rain QC algorithm
			if (BrightBandInfo.fBrightBandHeightM > 0) {
				RainQC();
			}

			ClearData();

		}

		//////////////////////////////////////////////////////////////////////////////
		///
		/// <summary>
		/// RainQC: When the acceptance test is passed for a particular mode 
		/// (as determined by iAcceptPercent), the QC algorithm is applied to assess 
		/// the quality of the derived bright-band height (BBH). In doing so, the algorithm 
		/// first calculates a vertically averaged profile of range-corrected SNR and vertical 
		/// radial velocity based on the vertical profiles that were used in the BBH.  
		/// Next, based on the value of iMinSnrRain and the rain-vertical-velocity threshold (2.5 m/s), 
		/// each gate, in the vertically averaged profile above the BBH, is tested for the 
		/// existence of rain.  If the total number of gates with rain above the BBH equals or 
		/// exceeds a threshold (as defined by iQcMaxRainAboveBb), the BBH is a candidate for rejection; 
		/// otherwise the BBH is accepted as good data. The remaining rejection candidates are then 
		/// compared against the final-answer BBH of the previous hour. If a final-answer BBH did NOT 
		/// exist for the previous hour, the BBH in question is rejected. If a final-answer BBH did exist 
		/// for the previous hour, the BBH in question is rejected if it does NOT exist 
		/// within +/- 400 meters of the previous-hour BBH; otherwise the BBH in question is 
		/// accepted as good data.
		/// From code WRITTEN BY: Coy Chanders 10-25-2005
		/// </summary>
		///
		private bool RainQC() {

			// Current hard coded tolerance level
			double fBbHtToleranceM = 400.0;

			// Perform Rain QC algorithm
			double dSnr;
			List<double> vec_fAverageVelocity = new List<double>();
			List<double> vec_fSnrVelocity = new List<double>();

			PopParameters.BeamParameters beamPar = _parameters.SystemPar.RadarPar.BeamParSet[0];
			PopParameters.MeltingLayerParameters meltLayerPar = _parameters.MeltingLayerPar;

			// Find a vertical dwell to get information from
			int iNumRangeGates = 0;
			double fFirstRangeM = -999.0;
			double fGateSpacingM = 999.0;
            double sysDelayCorrectionM = 0.0;
            //LapxmDataStructure * pLapxmData;
			bool isVertical = false;
			for(int iDwellIndex = 0; iDwellIndex < _nRecords; iDwellIndex++) {
				//pLapxmData = &m_vecLapxmData.at(iDwellIndex);
				//float fElevationDeg = pLapxmData->pBeam[0].Dwell.fElevationDeg;
				int iDir = _parameters.SystemPar.RadarPar.BeamSequence[0].DirectionIndex;
				double elevationDeg = _parameters.SystemPar.RadarPar.BeamDirections[iDir].Elevation;

				// Is this a vertical dwell
				if ((elevationDeg > 88.0) && (elevationDeg < 92.0)) {
                    sysDelayCorrectionM = beamPar.SystemDelayNs * PopParameters.MperNs;
                    iNumRangeGates = beamPar.NHts; // pLapxmData->pBeam[0].Pulse.lNumRangeGates;
					fFirstRangeM = beamPar.SampleDelayNs * MperNs - sysDelayCorrectionM; // pLapxmData->pBeam[0].Dwell.fFirstRangeM;
					fGateSpacingM = beamPar.SpacingNs * MperNs; // pLapxmData->pBeam[0].Dwell.fGateSpacingM;
					isVertical = true;
					break;
				}
			}

			if (!isVertical) {
				return false;
			}

			// Calculate the first gate, which is completly above the average bright band
			//	fFirstRangeM is range to first gate written to data file, so add gateIndexOffset to get position in array
			int iStartGate = (int)Math.Ceiling((BrightBandInfo.fBrightBandHeightM - fFirstRangeM + (0.5 * fGateSpacingM)) / fGateSpacingM);
			iStartGate += _gateIndexOffset;

			// Find the average SNR and average Velocity for each gate for dwells that contain a vertical bright band.
			// Do not include the gates below the bright band height
			for(int iGate = iStartGate; iGate < iNumRangeGates+_gateIndexOffset; iGate++) {
				double dAverageVelocity = 0.0;
				double dAverageSnr = 0.0;
				int iNumberOfVerticalDwells = _brightBandDwellIndexList.Count;
				for(int iIndex = 0; iIndex < iNumberOfVerticalDwells; iIndex++) {
					int iDwellIndex = _brightBandDwellIndexList[iIndex];
					//pLapxmData = &m_vecLapxmData.at(iDwellIndex);
					dAverageVelocity += _mDopList[iDwellIndex][iGate];
					dSnr = _snrList[iDwellIndex][iGate];
					dAverageSnr += (double)Math.Pow(10.0,dSnr / 10.0);
				}
				vec_fAverageVelocity.Add((dAverageVelocity / iNumberOfVerticalDwells));
				vec_fSnrVelocity.Add((10.0 * Math.Log10(dAverageSnr / iNumberOfVerticalDwells)));
			}

			// Find the number of gates whose average SNR and Velocity are above the minimum
			int iNumRainGatesAboveBb = 0;
			for(int iGate = 0; iGate < vec_fAverageVelocity.Count; iGate++) {
				double fAveragedVelocity = vec_fAverageVelocity[iGate];
				double fAveragedSnr = vec_fSnrVelocity[iGate];

				if((fAveragedVelocity >= 2.5) && (fAveragedSnr >= MinSnrRain[iGate])) {
					iNumRainGatesAboveBb++;
				}
			}

			// Store the information for later use
			BrightBandInfo.iNumRainGatesAboveBb = iNumRainGatesAboveBb;

			// If the number of rain gate above the bright band is above the threshold
			// then reject this bright band if there was not a bright band in the previous
			// hour that was within +-400 meter.
			double fBrightBandThisHour = BrightBandInfo.fBrightBandHeightM;
			double fDifference = -1;
			if(iNumRainGatesAboveBb >= _meltParameters.QcMaxRainAboveBb) {
				if(m_iBrightBandHeightM_PreviousHour < 0) {
					// Reject because there was not a bright band in the previous hour
					BrightBandInfo.fBrightBandHeightM = -9999.0;
				}
				else {
					fDifference = Math.Abs(m_iBrightBandHeightM_PreviousHour - BrightBandInfo.fBrightBandHeightM);

					if(fDifference > fBbHtToleranceM) {
						// Reject because the bright band is not within fBbHtToleranceM (+-400 meters) of the bright band
						// from the previous hour.
						BrightBandInfo.fBrightBandHeightM = -9999.0;
					}
				}
			}

			if(_LogFileIsEnabled) {

				LogFileWriteLine("\nRain QC");

				LogFileWriteLine(String.Format("Bright Band of Previous Hour = {0} m", m_iBrightBandHeightM_PreviousHour));
				LogFileWriteLine(String.Format("Bright Band of This Hour = {0:0.0} m", fBrightBandThisHour));
				LogFileWriteLine(String.Format("Start Gate = {0}", iStartGate)); 
				double fBottomOfStartGate = (fFirstRangeM - (0.5 * fGateSpacingM) + (iStartGate * fGateSpacingM));
				LogFileWriteLine(String.Format("Bottom of Start Gate = {0:0.0} m", fBottomOfStartGate));
				LogFileWriteLine(String.Format("Bottom of Gate below Start Gate = {0:0.0} m", fBottomOfStartGate - fGateSpacingM));
				LogFileWriteLine(String.Format("Number of Rain Gate above Bright Band = {0}", iNumRainGatesAboveBb));
				LogFileWriteLine(String.Format("Difference between current and previous Bright Band heights = {0:0.0} m", fDifference));
				LogFileWriteLine(String.Format("Final Bright Band = {0:0.0} m", BrightBandInfo.fBrightBandHeightM)); 
			}

			return true;
		}

		//////////////////////////////////////////////////////////////////////////////
		///
		/// <summary>
		/// CalculateBrightBandHeights(): Calculates the average Bright Band height by averaging together all 
		/// Bright Band heights that are within 211 meters of the median.
		/// From code WRITTEN BY: Coy Chanders 10-25-2005
		/// </summary>
		/// <returns></returns>
		private bool CalculateBrightBandHeights() {

			_brightBandDwellIndexList.Clear();

			// Loop through all the vertical Bright Bands heights that were found and add 
			// them to an vector
			List<double> vec_fBrightBandHeights = new List<double>();;
			for (int iIndex = 0; iIndex < _brightBandList.Count; iIndex++) {
				double fHeight = _brightBandList[iIndex].fHeight;
				vec_fBrightBandHeights.Add(_brightBandList[iIndex].fHeight);
			}

			// Loop through all the oblique Bright Bands heights that were found and add 
			// them to the same vector
			/*
			for(int iIndex = 0; iIndex < m_ObliqueBrightBand.size(); iIndex++) {
				float fHeight = m_ObliqueBrightBand.at(iIndex).fHeight;
				vec_fBrightBandHeights.push_back(m_ObliqueBrightBand.at(iIndex).fHeight);
			}
			 * */

			// Sort all the Bright Band heights from smallest to largest
			vec_fBrightBandHeights.Sort();
			//std::sort(vec_fBrightBandHeights.begin(),vec_fBrightBandHeights.end());

			// Find the median
			double fMedianBrightBandHeight = vec_fBrightBandHeights[vec_fBrightBandHeights.Count/2];

			// Average together all bright band heights that are within 211 meters of the median
			BrightBandInfo.fBrightBandHeightM = -9999.0f;
			double fBrightBandHeightM = 0.0;
			int iNumBrightBandsInAverage = 0;
			int iNumVerticalBrightBandsInAverage = 0;
			for (int iIndex = 0; iIndex < _brightBandList.Count; iIndex++) {
				double fHeight = _brightBandList[iIndex].fHeight;
				if( Math.Abs(fHeight - fMedianBrightBandHeight) < _parameters.MeltingLayerPar.AcceptHeightRangeM) {
					fBrightBandHeightM += (int)fHeight;
					iNumBrightBandsInAverage++;
					iNumVerticalBrightBandsInAverage++;
					_brightBandDwellIndexList.Add(_brightBandList[iIndex].iDwellIndex);
				}
			}

			/*
			for( int iIndex = 0; iIndex < m_ObliqueBrightBand.size(); iIndex++) {
				float fHeight = m_ObliqueBrightBand.at(iIndex).fHeight;
				if( abs(fHeight - fMedianBrightBandHeight) < m_iAcceptHeightRange) {
					fBrightBandHeightM += (int)fHeight;
					iNumBrightBandsInAverage++;
				}
			}
			 * */

			BrightBandInfo.iNumBbInAverage = iNumBrightBandsInAverage;

			// Did enough of all the Bright Band heights fall within 211 meters of the median?
			// Were enough bright bands accepted out of total number of profiles?
			double fFractionAccepted = 0.0;
			double fractionOfTotalProfiles = 0.0;
			if(iNumVerticalBrightBandsInAverage > 0) {
				fFractionAccepted = (double)BrightBandInfo.iNumBbInAverage / (double)(BrightBandInfo.iNumVertBb + BrightBandInfo.iNumOblBb);
				fractionOfTotalProfiles = (double)BrightBandInfo.iNumBbInAverage / (double) BrightBandInfo.iNumVertProfiles;
				if(fFractionAccepted >= _parameters.MeltingLayerPar.AcceptPercent/100.0) {
					// dac note: consider adding this test:
					//if (fractionOfTotalProfiles >= _parameters.MeltingLayerPar.BrightBandPercent/100.0) {
						BrightBandInfo.fBrightBandHeightM = (fBrightBandHeightM / iNumBrightBandsInAverage);
					//}
				}
			}


			if(_LogFileIsEnabled) {
				
				LogFileWriteLine("");

				LogFileWriteLine(String.Format("Median Bright Band Height = {0:0.0}", fMedianBrightBandHeight)); 

				LogFileWriteLine("\nBright Band Heights Used");
				foreach(structBrightBand bb in _brightBandList) {
					LogFileWriteLine(String.Format("Bright Band used in calculation - Height = {0:0.0}", bb.fHeight)); 
				}

				LogFileWriteLine("\r\nFinal Numbers");

				LogFileWriteLine(String.Format("Total Number of Bright Bands Found = {0}", (BrightBandInfo.iNumOblBb + BrightBandInfo.iNumVertBb)));

				LogFileWriteLine(String.Format("Number of Bright Bands in Average = {0}", BrightBandInfo.iNumBbInAverage));

				LogFileWriteLine(String.Format("Number in Average / Total Number = {0:0.00}", fFractionAccepted * 100));

				LogFileWriteLine(String.Format("Average Bright Band Height (meters) = {0:0.00}", BrightBandInfo.fBrightBandHeightM)); 
				
			}

			return true;
		}

		//////////////////////////////////////////////////////////////////////////////
		///
		/// <summary>
		/// DESCRIPTION: Finds the Bright Bands in the vertical dwells
		/// Changes in POPN relative to LAPXM version:
		/// 1) Corrected SNR does not use noise level at upper gates;
		///		uses SNR at each gate to correct for r-squared.
		///	2) (Optionally) Uses range-variable SNR threshold in place of
		///		minSnrBb and minSnrRain constant values.
		///	3) Has parameters to limit ht interval over which SNR and DVV data is used.
		///		Data above and below limits are set to default small values.
		///	4) Has option to set moments of gates with widths less than specified value to
		///		default small values.
		/// </summary>
		/// <returns></returns>
		private bool FindVerticalBrightBand() {


			// DAC 20091130 - heights adjusted for _gateIndexOffset
            // POPREV dac 20130529 - heights adjusted for systemDelay

			// pLapxmData is _parameters.SystemPar.RadarPar.BeamParSet[0]
			// m_vecLapxmData is _noiseLevList, _mDopList, powList, and snrList

			PopParameters.BeamParameters beamPar = _parameters.SystemPar.RadarPar.BeamParSet[0];
			PopParameters.MeltingLayerParameters meltLayerPar = _parameters.MeltingLayerPar;

            int totalDelayNs = beamPar.SampleDelayNs - beamPar.SystemDelayNs;

			if(_LogFileIsEnabled) {
				LogFileWriteLine("Vertical Bright Bands Found:\n");
			}

			// Clear out data from last calculation period
			_brightBandList.Clear();

			// Create range-dependent MinSnrBB threshold
			if ((MinSnrBB == null) || MinSnrBB.Length < beamPar.NHts) {
				MinSnrBB = new Double[beamPar.NHts];
				MinSnrRain = new Double[beamPar.NHts];
				DvvBbOnlyMinSnr = new Double[beamPar.NHts];
			}
			if (_parameters.MeltingLayerPar.UseRangeCorrectedMinSnr) {
				for (int iGate = 0; iGate < beamPar.NHts; iGate++) {
					double offsetDb = _parameters.MeltingLayerPar.RangeCorrectedMinSnrOffset;
                    double heightM = (totalDelayNs + ((iGate) * beamPar.SpacingNs)) * MperNs;
					MinSnrBB[iGate] = offsetDb + 20.0 * Math.Log10(heightM);
					if (Double.IsNaN(MinSnrBB[iGate])) {
						// heightM was negative
						MinSnrBB[iGate] = -50.0;
					}
					MinSnrRain[iGate] = MinSnrBB[iGate];
					DvvBbOnlyMinSnr[iGate] = MinSnrBB[iGate];
				}
			}
			else {
				for (int iht = 0; iht < beamPar.NHts; iht++) {
					MinSnrBB[iht] = _parameters.MeltingLayerPar.MinSnrBb;
					MinSnrRain[iht] = _parameters.MeltingLayerPar.MinSnrRain;
					DvvBbOnlyMinSnr[iht] = _parameters.MeltingLayerPar.DvvBbOnlyMinSnr;
				}
			}

			// Loop through all the dwells and find all vertical beams that contain a Bright Band
			//LapxmDataStructure * pLapxmData;
			double elevationDeg;
			for( int iDwellIndex = 0; iDwellIndex < _nRecords; iDwellIndex++) {

				//pLapxmData = &m_vecLapxmData.at(iDwellIndex);
				int iDir = _parameters.SystemPar.RadarPar.BeamSequence[0].DirectionIndex;
				elevationDeg = _parameters.SystemPar.RadarPar.BeamDirections[iDir].Elevation;

				// Is this a vertical dwell
				if((elevationDeg > 88.0) && (elevationDeg < 92.0)) {

					if (_snrList[iDwellIndex].Length < beamPar.NHts) {
						// Oh-oh, some of our dwells had different parameters!
						//	Here we are only catching possible nht overflows.
						continue;
					}

					// Increment the count of vertical dwells in this calculation period
					BrightBandInfo.iNumVertProfiles++;

					// Clear out data above and below height limits
					// added dac 2009Jan27 rev 1.9
					// And also gates with narrow widths 2009Jan28 rev 1.10
					if (_parameters.MeltingLayerPar.UseDataRegionOnly) {
                        int iMaxGate = (int)Math.Ceiling((meltLayerPar.MaxDataHtM - totalDelayNs * MperNs) / (beamPar.SpacingNs * MperNs));
                        int iMinGate = (int)Math.Ceiling((meltLayerPar.MinDataHtM - totalDelayNs * MperNs) / (beamPar.SpacingNs * MperNs));
						iMaxGate += _gateIndexOffset;  // now iMaxGate is index from beginning of array
						iMinGate += _gateIndexOffset;
						iMinGate = (iMinGate < beamPar.NHts) ? iMinGate : beamPar.NHts - 1 ;
						iMaxGate = (iMaxGate >= 0) ? iMaxGate : 0;
						for (int iht = 0; iht < iMinGate; iht++) {
							_mDopList[iDwellIndex][iht] = -999.0;
							_snrList[iDwellIndex][iht] = -999.0;
						}
						for (int iht = iMaxGate; iht < _mDopList[iDwellIndex].Length; iht++) {
							_mDopList[iDwellIndex][iht] = -999.0;
							_snrList[iDwellIndex][iht] = -999.0;
						}
						
					}					
					if (_parameters.MeltingLayerPar.SkipNarrowWidths) {
						double narrowWidth = _parameters.MeltingLayerPar.NarrowWidthMS;
						for (int iht = 0; iht < _snrList[iDwellIndex].Length; iht++) {
							if (_widthList[iDwellIndex][iht] < narrowWidth) {
								_mDopList[iDwellIndex][iht] = -999.0;
								_snrList[iDwellIndex][iht] = -999.0;
							}
						}
					}

					// Determine the top gate that is below 2Km
					// dac note: modfied from lapxm so that iTopGate is an index
					// and is less than 2 km ht
                    int iTopGate = (int)((2000.0 - totalDelayNs * MperNs) / (beamPar.SpacingNs * MperNs));
					iTopGate += _gateIndexOffset;

					// Make sure we have some data below 2Km
					if(iTopGate <= 0) {
						ErrorMessage = "No data between ground level and 2000 meters";
						return false;
					}

					// Make sure we do not go above the top gate
					// dac note: modfied from lapxm so that iTopGate is an index,
					//  and max value is nhts-1
					int iNumRangeGates = beamPar.NHts;
					if(iTopGate >= iNumRangeGates + _gateIndexOffset) {
						iTopGate = iNumRangeGates + _gateIndexOffset - 1;
					}

					// Check lowest 2Km of gates for a Doppler Vertical Velocity (DVV)>= 2.5 and SNR >= m_iMinSnrRain
					double fDVV;
					double fSNR;
					int iRainSnrPassCount = 0;
					int iRainDvvPassCount = 0;
					int iRainSnrDvvPassCount = 0;

					int iBBgate;
					for (int iDataGate = _gateIndexOffset; iDataGate <= iTopGate; iDataGate++) {
						iBBgate = iDataGate - _gateIndexOffset;
						// remember: snrList has all heights, MinSnrRain has only saved hts
						fDVV = _mDopList[iDwellIndex][iDataGate]; // pLapxmData->pData[iGate];
						fSNR = _snrList[iDwellIndex][iDataGate];
						if (fSNR >= MinSnrRain[iBBgate]) {
							iRainSnrPassCount++;
						}
						if(fDVV >= 2.5) {
							iRainDvvPassCount++;
						}
						if ((fSNR >= MinSnrRain[iBBgate]) && (fDVV >= 2.5)) {
							iRainSnrDvvPassCount++;
						}
					}

					// ???
					// Increment counter for diagnostics in output file
					if (iRainSnrPassCount > meltLayerPar.MinSnrDvvPairs) {
						BrightBandInfo.iNumPassRainSnr++;
					}

					// Increment counter for diagnostics in output file
					if (iRainDvvPassCount > meltLayerPar.MinSnrDvvPairs) {
						BrightBandInfo.iNumPassRainDvv++;
					}

					// Check to see if enough instances were found
					if (iRainSnrDvvPassCount < meltLayerPar.MinSnrDvvPairs) {
						continue;
					}

					// Increment counter for diagnostics in output file
					BrightBandInfo.iNumPassRainTotal++;


					// From bottom up, look for a single instance of an increase in SNR >= m_fDeltaSnrBb 
					// and decrease in DVV =< m_fDeltaDvvBb over a 210 meter spacing
					// SNR must be at least => m_fMinSnrBb for both values


					// Determine the number of gates that make up 210 meters
					int iNumberOfGatesInBrightBandSearch = (int)Math.Ceiling(210.0 / (beamPar.SpacingNs * MperNs));

					// Make sure the 210 meter spacing does not exceed the parameter m_iGateSpaceResolution
					if(meltLayerPar.GateSpacingResolution < iNumberOfGatesInBrightBandSearch) {
						iNumberOfGatesInBrightBandSearch = meltLayerPar.GateSpacingResolution;
					}

					// Determine the number of gates that make up 500 meters
					int iNumberOfGatesInPeakSnrSearch = (int)(500.0 / (beamPar.SpacingNs * MperNs));

					double fDVV_Bottom, fDVV_Top, fSNR_Bottom, fSNR_Top;
					int iGate_BottomOfBrightBand = -1;
					int iPassBbDeltaSnrCount = 0;
					int iPassBbDeltaDvvCount = 0;
					int iPassBbDeltaTotalCount = 0;
					int iPassBbMinSnrCount = 0;
					int iPassBbMinDvvCount = 0;
					int iPassBbMinTotalCount = 0;

					// Perform the gradiant test from gate 0 up to the last gate that falls 500 meters below the top gate
					// dac note: TODO: make top gate no higher than meltLayerPar.MaxHeightM
					int iEndGradiantGate = iNumRangeGates - iNumberOfGatesInPeakSnrSearch + iNumberOfGatesInBrightBandSearch;
					int iStartGradiantGate = iNumberOfGatesInBrightBandSearch;
					iEndGradiantGate += _gateIndexOffset;
					iStartGradiantGate += _gateIndexOffset;
                    // POPREV limit upper gate of ML search rev 4.5 dac 20140611
                    if (iEndGradiantGate >= iNumRangeGates) {
                        iEndGradiantGate = iNumRangeGates - 1;
                    }
					for(int iGate = iStartGradiantGate; iGate <= iEndGradiantGate; iGate++) {

						int iGateBottom = iGate - iNumberOfGatesInBrightBandSearch;
                        fDVV_Bottom = 0.0;
                        fSNR_Bottom = 0.0;
                        fDVV_Top = 0.0;
                        fSNR_Top = 0.0;
                        try {
                            fDVV_Bottom = _mDopList[iDwellIndex][iGateBottom];
                            fSNR_Bottom = _snrList[iDwellIndex][iGateBottom];
                            fDVV_Top = _mDopList[iDwellIndex][iGate];
                            fSNR_Top = _snrList[iDwellIndex][iGate];
                        }
                        catch (Exception ee) {
                            int x = 0;
                        }

						if((fDVV_Top - fDVV_Bottom)<= meltLayerPar.DeltaDvvBb) {
							iPassBbDeltaDvvCount++;
						}

						if((fSNR_Top - fSNR_Bottom)>= meltLayerPar.DeltaSnrBb) {
							iPassBbDeltaSnrCount++;
						}

						if (((fDVV_Top - fDVV_Bottom) <= meltLayerPar.DeltaDvvBb) &&
							((fSNR_Top - fSNR_Bottom) >= meltLayerPar.DeltaSnrBb)) {
							iPassBbDeltaTotalCount++;

							// dac note: replaced MinSnrBb with range dependent value
							if (fSNR_Bottom >= MinSnrBB[iGateBottom - _gateIndexOffset]) {
								iPassBbMinSnrCount++;
							}

							if(fDVV_Bottom >= _minDvvBb) {
								iPassBbMinDvvCount++;
							}

							// dac note: replaced MinSnrBb with range dependent value
							if ((fSNR_Bottom >= MinSnrBB[iGateBottom - _gateIndexOffset]) && (fDVV_Bottom >= _minDvvBb)) {
								iPassBbMinTotalCount++;
								BrightBandInfo.iNumPassBbTotal++;
								iGate_BottomOfBrightBand = iGate;
								break;
							}
						}
					} // end for iGate

					// Increment counter for diagnostics in output file
					if(iPassBbDeltaDvvCount > 0) {
						BrightBandInfo.iNumPassBbDeltaDvv++;
					}

					if(iPassBbDeltaSnrCount > 0) {
						BrightBandInfo.iNumPassBbDeltaSnr++;
					}

					if(iPassBbDeltaTotalCount > 0) {
						BrightBandInfo.iNumPassBbDeltaTotal++;
					}

					if(iPassBbMinSnrCount > 0) {
						BrightBandInfo.iNumPassBbMinSnr++;
					}

					if(iPassBbMinDvvCount > 0) {
						BrightBandInfo.iNumPassBbMinDvv++;
					}

					if(iPassBbMinTotalCount > 0) {
						BrightBandInfo.iNumPassBbMinTotal++;
					}

					int iPassedDvvBbOnly = 0;


					// Try again using only the Dvv?
					if((meltLayerPar.DvvBbOnlyMaxHeightM > 0) && (iGate_BottomOfBrightBand == -1)) {
						// Do not go past the max gate set in the configuration file
						// dac: shouldn't we account for range to 1st gate?  Done: 20091130 rev 2.7
                        int iMaxDvvOnlyGate = (int)((meltLayerPar.DvvBbOnlyMaxHeightM - totalDelayNs * MperNs) /
													(beamPar.SpacingNs * MperNs));
						iMaxDvvOnlyGate += _gateIndexOffset;
						iMaxDvvOnlyGate = (iEndGradiantGate > iMaxDvvOnlyGate) ? iMaxDvvOnlyGate : iEndGradiantGate;

						// From bottom up, look for a single instance of an increase in SNR >= m_fDeltaSnrBb 
						// and decrease in DVV =< m_fDeltaDvvBb over a 210 meter spacing
						// SNR must be at least => m_fMinSnrBb for both values
						for(int iGate = iStartGradiantGate; iGate <= iMaxDvvOnlyGate; iGate++) {
							fDVV_Bottom = _mDopList[iDwellIndex][iGate - iNumberOfGatesInBrightBandSearch];
							fSNR_Bottom = _snrList[iDwellIndex][iGate - iNumberOfGatesInBrightBandSearch];
							fDVV_Top    = _mDopList[iDwellIndex][iGate];
							fSNR_Top    = _snrList[iDwellIndex][iGate];

							if((fDVV_Top - fDVV_Bottom)<= meltLayerPar.DeltaDvvBb) {

								if ((fSNR_Bottom >= DvvBbOnlyMinSnr[iGate - _gateIndexOffset]) && (fDVV_Bottom >= 0.8) && (fDVV_Top >= 0.8)) {
									iGate_BottomOfBrightBand = iGate;
									BrightBandInfo.iNumPassBbTotal++;
									iPassedDvvBbOnly = 1;
									break;
								}
							}
						} // end for iGate
					} // end if m_iDvvBbOnlyMaxHeightM

					// Try again using only the DVV and a smaller resolution
					if((meltLayerPar.DvvBbOnlyMaxHeightM > 0) && (iGate_BottomOfBrightBand == -1)) {

						int iResolutionReduction = 1;
						double fDeltaDvvBbReduced = meltLayerPar.DeltaDvvBb * (iNumberOfGatesInBrightBandSearch - iResolutionReduction)/iNumberOfGatesInBrightBandSearch;

						// Do not go pass the max gate set in the configuration file
						// dac: shouldn't we account for range to 1st gate?  Done: 20091130 rev 2.7
                        int iMaxDvvOnlyGate = (int)((meltLayerPar.DvvBbOnlyMaxHeightM - totalDelayNs * MperNs) /
													(beamPar.SpacingNs * MperNs));
						iMaxDvvOnlyGate += _gateIndexOffset;
						iMaxDvvOnlyGate = (iEndGradiantGate > iMaxDvvOnlyGate) ? iMaxDvvOnlyGate : iEndGradiantGate;

						// From bottom up, look for a single instance of an increase in SNR >= m_fDeltaSnrBb 
						// and decrease in DVV =< m_fDeltaDvvBb over a 210 meter spacing
						// SNR must be at least => m_fMinSnrBb for both values
						for(int iGate = iStartGradiantGate - iResolutionReduction; iGate <= iMaxDvvOnlyGate; iGate++) {

							fDVV_Bottom = _mDopList[iDwellIndex][iGate - iNumberOfGatesInBrightBandSearch + iResolutionReduction];
							fSNR_Bottom = _snrList[iDwellIndex][iGate - iNumberOfGatesInBrightBandSearch + iResolutionReduction];
							fDVV_Top    = _mDopList[iDwellIndex][iGate];
							fSNR_Top	= _snrList[iDwellIndex][iGate];

							if((fDVV_Top - fDVV_Bottom)<= fDeltaDvvBbReduced) {

								if ((fSNR_Bottom >= DvvBbOnlyMinSnr[iGate - _gateIndexOffset]) && (fDVV_Bottom >= 0.8) && (fDVV_Top >= 0.8)) {
									iGate_BottomOfBrightBand = iGate;
									BrightBandInfo.iNumPassBbTotal++;
									iPassedDvvBbOnly = 2;
									break;
								}
							}
						}  // end for iGate
					}  // end if m_iDvvBbOnlyMaxHeightM

					// Was Bright Band Criteria Met?
					if(iGate_BottomOfBrightBand == -1) {
						continue;
					}

					// Find Peak SNR in the 500m above the bottom gate identified in Step 5
					structBrightBand BrightBand;
					BrightBand.fPeakSnr = 0.0f;
					BrightBand.iGate = 0;
					BrightBand.lTimeStamp = DateTime.MinValue;
					BrightBand.iDwellIndex = -1;
					BrightBand.fHeight = 0.0;
					int iBottomSnrSearchGate = ((iGate_BottomOfBrightBand - iNumberOfGatesInBrightBandSearch) < 0) ?
												0 : (iGate_BottomOfBrightBand - iNumberOfGatesInBrightBandSearch);
					int iTopSnrSearchGate = ((iBottomSnrSearchGate + iNumberOfGatesInPeakSnrSearch) > iNumRangeGates) ?
												iNumRangeGates : (iBottomSnrSearchGate + iNumberOfGatesInPeakSnrSearch);
					for(int iGate = iBottomSnrSearchGate; iGate < iTopSnrSearchGate; iGate++) {
						if (_snrList[iDwellIndex][iGate] > BrightBand.fPeakSnr) {
							BrightBand.iDwellIndex = iDwellIndex;
							BrightBand.fPeakSnr = _snrList[iDwellIndex][iGate];
							BrightBand.iGate = iGate;
							BrightBand.lTimeStamp = _timeStampList[iDwellIndex];
                            BrightBand.fHeight = (totalDelayNs + ((iGate - _gateIndexOffset) * beamPar.SpacingNs)) * MperNs;
						}
					}

					// Store the Bright Band height
					_brightBandList.Add(BrightBand);

					// Increment counter for the number of vertical bright bands found
					BrightBandInfo.iNumVertBb++;

					if(_LogFileIsEnabled) {
					 
						//CLapTime LapTime;
						LogFileWriteLine(String.Format("Vertical Bright Band Found at Time = {0}, Gate = #{1}, Height = {2:0.}, Peak SNR = {3:0.},  Passed DVV Only = {4}",
							BrightBand.lTimeStamp.ToString("HH:mm:ss"),
                                        // adjusted for gate offset POPN 3.3:
                                        BrightBand.iGate + 1 - _gateIndexOffset,
										BrightBand.fHeight,
										BrightBand.fPeakSnr,
										iPassedDvvBbOnly)); 

						}
					}  // end if vertical
			}

			return true;
		}

		//////////////////////////////////////////////////////////////////////////////
		///
		/// <summary>
		/// 
		/// </summary>
		private void CorrectRange() {

			// DAC 20091130 - heights adjusted for _gateIndexOffset

			/***********************************************************************************/
			float LAPXM_INVALID_FLOAT = 3.402823466e+38f;  //  DAC TODO: find out what this value is
			float MAX_FLOAT = float.MaxValue;
			double LAPXM_INVALID_VALUE = (double)LAPXM_INVALID_FLOAT;
			string max_float = string.Format("{0,45:f}{1,45:f}{2,45:f}", LAPXM_INVALID_FLOAT, MAX_FLOAT, LAPXM_INVALID_VALUE);
			string max_float2 = string.Format("{0,25:e}{1,25:e}{2,35:e}", LAPXM_INVALID_FLOAT, MAX_FLOAT, LAPXM_INVALID_VALUE);
			// verify that testing for this value works:
			if (LAPXM_INVALID_FLOAT == MAX_FLOAT) {
				bool OK = true;
			}
			if (LAPXM_INVALID_VALUE == 3.40282346638529e+38) {
				bool OK = true;
			}
			if (LAPXM_INVALID_VALUE == (double)MAX_FLOAT) {
				bool OK = true;
			}
			/***********************************************************************************/

			// Loop through all the dwells
			for (int iIndex = 0; iIndex < _nRecords; iIndex++) {
				// Get the next dwell's worth of data
				//LapxmDataStructure * pLapxmData = &m_vecLapxmData.at(iIndex);
				double[] noiseLev = _noiseLevList[iIndex];
				double[] meanDop = _mDopList[iIndex];
				double[] power = _powList[iIndex];
				double[] snr = _snrList[iIndex];

				// POPN version eliminates the use of upper gate noise level to correct the SNR.
				//	We will use the original SNR from the spectral moments and correct for range
				/*
				// Average Noise Power over top 5 gates that do not have missing values created by the Multiple Peak Picking module
				double dAverageNoisePower = 0.0;
				double dNoisePower = 0.0;
				long lNumFFTPoints = _parameters.SystemPar.RadarPar.BeamParSet[0].NPts;
				int iNumRangeGates = _parameters.SystemPar.RadarPar.BeamParSet[0].NHts;
				int iGate = iNumRangeGates - 1;
				int iGateCount = 0;

				while((iGate >= 0) && (iGateCount < 5))
				{
				  if(meanDop[iGate] != LAPXM_INVALID_FLOAT) {
					dNoisePower = noiseLev[iGate];
					dAverageNoisePower += ((dNoisePower/5.0) * lNumFFTPoints);
					iGateCount++;
				  }
				  iGate--; 
				}

				dAverageNoisePower = 10.0*Math.Log10(dAverageNoisePower);  // dac note: convert to dB

				// Range Correct SNR data
				int m_iReplaceSnrdb = -30;
				// dac note: original lapxm code has <= in for loop test condition
				for(int jGate = 0; jGate < iNumRangeGates; jGate++) {
				  // Check for an invalid value for the Velocity
				  if(meanDop[jGate] == LAPXM_INVALID_FLOAT)
				  {
					_missingMomentsCount++;

					// Replace SNR with a user defined noise level
					//snr[jGate] = (float)(Math.Pow(10.0,((double)m_iReplaceSnrdb/10.0))); // dac note: should't this be dB instead of linear?
					snr[jGate] = m_iReplaceSnrdb;
				  }
				  else
				  {
					double dPower = power[jGate];
					//double dRange = pLapxmData->pBeam[0].Dwell.fFirstRangeM + (jGate * pLapxmData->pBeam[0].Dwell.fGateSpacingM);
					double dRange = _parameters.SystemPar.RadarPar.BeamParSet[0].SampleDelayNs * MperNs +
									jGate * _parameters.SystemPar.RadarPar.BeamParSet[0].SpacingNs * MperNs;
					double rangeCorrection = 20.0 * Math.Log10(dRange);
					snr[jGate] = (float)(10.0 * Math.Log10(dPower) - dAverageNoisePower + rangeCorrection);
				  }
				}
				*/

				int nGates = _parameters.SystemPar.RadarPar.BeamParSet[0].NHts;
                int sysdelay = _parameters.SystemPar.RadarPar.BeamParSet[0].SystemDelayNs;
				double firstGate = (_parameters.SystemPar.RadarPar.BeamParSet[0].SampleDelayNs - sysdelay) * MperNs;
				double spacing = _parameters.SystemPar.RadarPar.BeamParSet[0].SpacingNs * MperNs;
				int jGate;
				// snr array has data from all original hts
				// nGates is the number of gates actually used (first _gateIndexOffset are thrown away)
				for (int j = 0; j < nGates; j++) {
					jGate = j + _gateIndexOffset;
					double dRange = firstGate + (j) * spacing;
					double rangeCorrection = 20.0 * Math.Log10(dRange);
					if (Double.IsNaN(rangeCorrection)) {
						// dRange was negative
						rangeCorrection = -99.9;
					}
					snr[jGate] = snr[jGate] + rangeCorrection;
				}
			}

			return;
		}

		//////////////////////////////////////////////////////////////////////////////
		///
		public void GetBrightBandInfo() {
		}

		///////////////////////////////////////////////////////////////////////////
		//
		// /*Static*/ Methods to make final determination of Melting Layer.
		//	In LapXM this method can handle multiple modes
		//

		public /*static*/ int PreviousBrightBandHeightM;

		public /*static*/ void CalculateMeltingLayer(MeltingLayerCalculator3 meltingLayer) {

            BrightBandInfo.dataMiddleTime = _calcTime.AddMinutes(-_intervalMinutes / 2.0);

            // calculate a layer, sending previous layer height as argument
			meltingLayer.CalculateResult(PreviousBrightBandHeightM);

			// If there was a bright band found in the previous calculation, use only modes with height ranges that contain the previous calculation
			// If a bright band is not found within these height limitations, then take the mode with the greatest number of bright bands.
			// If a bright band was not found in the previous calculation, take the mode with greatest number of bright bands.
			// dac note: this implementation has only 1 mode
			int iBrightBandIndex = -1;
			int iBrightBandHeightM = (int)meltingLayer.BrightBandInfo.fBrightBandHeightM;
			int iNumberOfBrightBands = meltingLayer.BrightBandInfo.iNumBbInAverage;
			bool brightBandFound = false;
			if (iBrightBandHeightM > 0) {
				float min = (float)meltingLayer.Parameters.MeltingLayerPar.MinHeightM;
				float max = (float)meltingLayer.Parameters.MeltingLayerPar.MaxHeightM;
				if ((PreviousBrightBandHeightM > 0) && ((PreviousBrightBandHeightM >= min) && (PreviousBrightBandHeightM <= max))) {
					PreviousBrightBandHeightM = iBrightBandHeightM;
					iBrightBandIndex = 0;
					brightBandFound = true;
				}
			}
			if (!brightBandFound) {
				iBrightBandHeightM = (int)meltingLayer.BrightBandInfo.fBrightBandHeightM;
				iNumberOfBrightBands = meltingLayer.BrightBandInfo.iNumBbInAverage;
				if (iBrightBandHeightM > 0) {
					PreviousBrightBandHeightM = iBrightBandHeightM;
					iBrightBandIndex = 0;
					brightBandFound = true;
				}
			}

			if (!brightBandFound) {
				PreviousBrightBandHeightM = -1;
			}

			// Output the data to a text file
			//_middleCalculationTime = meltingLayer.BrightBandInfo.dataEndTime.AddMinutes(-meltingLayer.Parameters.MeltingLayerPar.CalculateEveryMinute / 2);

			WriteMeltingLayerData(iBrightBandIndex, meltingLayer);
		}

		///////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		/// <param name="iBrightBandIndex"></param>
		/// <param name="structBrightBandInfo"></param>
		private /*static*/ bool WriteMeltingLayerData(int iBrightBandIndex, MeltingLayerCalculator3 meltingLayer) {

			int iNumberOfModes = 1;
			bool writeHeader = meltingLayer.Parameters.MeltingLayerPar.IncludeHeader;

			
			// Calculate the file name

			DateTime dataTime = meltingLayer.BrightBandInfo.dataMiddleTime;

			int iYear = dataTime.Year; 
			int iJulianDay = dataTime.DayOfYear; 
			int iHour = dataTime.Hour; 
			int iMinute = dataTime.Minute;
			int iMinUtcToLocal = meltingLayer.Parameters.SystemPar.MinutesToUT;
			double fTime = (double)(iHour + (iMinute / 60.0));

			//string cFileName;
			string outputPath = meltingLayer.Parameters.MeltingLayerPar.OutputFileFolder;
            string stationID;
            //POPREV 3.25 Use site prefix form output page:
            //stationID = meltingLayer.Parameters.SystemPar.StationName.Substring(0, 3);
            // If file output enabled, use given Lapxm site name
            // If not enabled, use first Lapxm site name
            // If not valid site code, use first 3 letters of full site name
            stationID = "";
            if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].FileWriteEnabled) {
                stationID = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].FileNameSite;
            }
            else if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].FileWriteEnabled) {
                stationID = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].FileNameSite;
            }
            if (stationID == String.Empty) {
                stationID = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].FileNameSite;
            }
            stationID = stationID.Trim();
            if (stationID.Length != 3) {
                stationID = _parameters.SystemPar.StationName.Substring(0, 3);                
            }
			string yyddd = string.Format("{0:00}{1:000}", iYear%100, iJulianDay);

			// file name format: sssyyddd.ml or sssyydddhh.ml
			string fileName = stationID + yyddd;

			if (outputPath.Trim() == string.Empty) {
				return false;
			}

			if(meltingLayer.Parameters.MeltingLayerPar.WriteHourlyFiles) {
				fileName += string.Format("{0:00}", iHour);
			}
			fileName += ".ml";
			string fileNameFullPath = Path.Combine(outputPath, fileName);


			if (!File.Exists(fileNameFullPath)) {
				// new file, so write header and title line
				StringBuilder text = new StringBuilder();
				if (writeHeader) {
					text.Append("date = The year, day number (001 = January 1st), and 'minutes to local time' the data was acquired\r\n");
					text.Append("time = Time stamp, centered within the 'mlh' averaging period (fractional hour)\r\n");
					text.Append("mlh  = Final melting layer altitude AGL in km\r\n");
					text.Append("nbba = Final number of vertical and oblique profiles in the 'mlh' average\r\n");
					text.Append("nm   = Number of dwell modes in the melting layer analysis\r\n");
					text.Append("\r\n");
					text.Append("The following are repeated for each radar mode (nm):\r\n");
					text.Append("mode = Dwell mode name (e.g., WA, WB, etc.)\r\n");
					text.Append("mlh  = Melting layer altitude AGL in km\r\n");
					text.Append("nbba = Number of vertical and oblique profiles resulting from the acceptance test\r\n");
					text.Append("nvp  = Number of available vertical profiles\r\n");
					text.Append("nprs = Number of vertical profiles that passed the SNR rain test\r\n");
					text.Append("nprd = Number of vertical profiles that passed the DVV rain test\r\n");
					text.Append("nprt = Number of vertical profiles that passed both the SNR and DVV rain test\r\n");
					text.Append("npds = Number of vertical profiles that passed the DeltaSNR bright-band test\r\n");
					text.Append("npdd = Number of vertical profiles that passed the DeltaDVV bright-band test\r\n");
					text.Append("npdt = Number of vertical profiles that passed both the DeltaSNR and DeltaDVV bright-band tests\r\n");
					text.Append("npms = Number of vertical profiles that passed Min SNR test\r\n");
					text.Append("npmd = Number of vertical profiles that passed the Min Dvv test\r\n");
					text.Append("npmt = Number of vertical profiles that passed both the Min SNR and Min Dvv tests\r\n");
					text.Append("npbt = Number of vertical profiles used in the acceptance test\r\n");
					text.Append("npra = Number of averaged-vertical-profile gates with rain above the 'mlh'. Used by the QC test\r\n");
					text.Append("\r\n");
					text.Append("edit = User editable version of 'mlh'\r\n");
					text.Append("qc   = Quality control value for the 'edit' column\r\n");
					text.Append("\r\n");
					text.Append("Interpreting missing flags:\r\n");
					text.Append("mlh  = -9.999 when the vertical-bright-band test, the acceptance test, or the QC test fails\r\n");
					text.Append("nbba = -99 when the vertical-bright-band test fails\r\n");
					text.Append("npra = -99 when the acceptance test fails\r\n");
					text.Append("edit = -9.999 when the melting layer height could not be determined\r\n");
					text.Append("qc   =  9 when the 'edit' value could not be determined\r\n");
					text.Append("\r\n");
				}
				// Print Title Line
				text.Append("Melting Layer\r\n");

				// Print Time Date Line
				text.Append(String.Format("{0:0000}:{1:000}:{2}\r\n\r\n", iYear, iJulianDay, iMinUtcToLocal));

				// Print Title Line
				text.Append("      time       mlh      nbba        nm");
				for (int iMode = 0; iMode < iNumberOfModes; iMode++) {
					text.Append("      mode       mlh      nbba       nvp      nprs      nprd      nprt      npds      npdd      npdt      npms      npmd      npmt      npbt      npra");
				}
				text.Append("      edit        qc");
				//text.Append("\r\n");

				DACarter.Utilities.TextFile.WriteLineToFile(fileNameFullPath, text.ToString(), false);
			}

			// Print the data for this calculation period
			StringBuilder data = new StringBuilder();
			if (iBrightBandIndex == -1) {
				data.Append(String.Format("{0,10:0.000}",fTime));
				data.Append(String.Format("{0,10:0.000}",-9.999));
				data.Append(String.Format("{0,10:d}",-99));
				data.Append(String.Format("{0,10:d}", iNumberOfModes));
			}
			else {
				data.Append(String.Format("{0,10:0.000}", fTime));
				data.Append(String.Format("{0,10:0.000}", meltingLayer.BrightBandInfo.fBrightBandHeightM / 1000.0));
				data.Append(String.Format("{0,10:d}", meltingLayer.BrightBandInfo.iNumBbInAverage));
				data.Append(String.Format("{0,10:d}", iNumberOfModes));
			}


			for (int iMode = 0; iMode < iNumberOfModes; iMode++) {
				data.Append(String.Format("{0,10}",meltingLayer.BrightBandInfo.strModeName));
				data.Append(String.Format("{0,10:0.000}", meltingLayer.BrightBandInfo.fBrightBandHeightM / 1000.0));

				data.Append(String.Format("{0,10}",meltingLayer.BrightBandInfo.iNumBbInAverage));
				data.Append(String.Format("{0,10}",meltingLayer.BrightBandInfo.iNumVertProfiles));
				data.Append(String.Format("{0,10}",meltingLayer.BrightBandInfo.iNumPassRainSnr));
				data.Append(String.Format("{0,10}",meltingLayer.BrightBandInfo.iNumPassRainDvv));
				data.Append(String.Format("{0,10}",meltingLayer.BrightBandInfo.iNumPassRainTotal));
				data.Append(String.Format("{0,10}",meltingLayer.BrightBandInfo.iNumPassBbDeltaSnr));
				data.Append(String.Format("{0,10}",meltingLayer.BrightBandInfo.iNumPassBbDeltaDvv));
				data.Append(String.Format("{0,10}",meltingLayer.BrightBandInfo.iNumPassBbDeltaTotal));
				data.Append(String.Format("{0,10}",meltingLayer.BrightBandInfo.iNumPassBbMinSnr));
				data.Append(String.Format("{0,10}",meltingLayer.BrightBandInfo.iNumPassBbMinDvv));
				data.Append(String.Format("{0,10}",meltingLayer.BrightBandInfo.iNumPassBbMinTotal));
				data.Append(String.Format("{0,10}",meltingLayer.BrightBandInfo.iNumPassBbTotal));
				data.Append(String.Format("{0,10}",meltingLayer.BrightBandInfo.iNumRainGatesAboveBb));

			}

			// Write out the defaulted user editable columns
			data.Append(String.Format("{0,10:0.000}{1,10}", -9.999, 9));

			DACarter.Utilities.TextFile.WriteLineToFile(fileNameFullPath, data.ToString(), true);

			return true;

		}   // end WriteMeltingLayerData()

		/// <summary>
		/// LogFileWriteLine
		/// </summary>
		/// <param name="line"></param>
		private void LogFileWriteLine(string line) {
			if (_logFileFullPath.Trim() != String.Empty) {
				TextFile.WriteLineToFile(_logFileFullPath, line, true);
			}
		}

	}  // end class MeltingLayerCalculator

}
