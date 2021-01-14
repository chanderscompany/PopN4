using System;
using System.IO;
using DACarter.NOAA;

namespace DACarter.NOAA
{

	/// <summary>
	/// Summary description for DacPopFile.
	/// </summary>
	public class DacPopFile : DacDataFileBase
	{

        // this is the OtherInstruments Code for fractional seconds in the Time stamp.
        public const Int16 FRAC_SEC_INST_CODE = 0x5453;

		public enum PFData {
			Moments = 1,
			Spectra = 2,
			ShortTimeSeries = 4,
			FullTimeSeries = 8,
			RassSpectra = 16,
			RassMoments = 32,
			StdPopSpectra = 3,
			StdPopMoments = 1
		}

		public enum PFNameType {
			StdPopName,
			LapxmName
		}

		private int _lastHdrPos;
		private long _hdrReaderPosition;
		private BinaryReader _HReader;
		//private PopHeader _headerData;
		private PopHeaderFileStruct _currentHeader;
		private TimeSpan _zeroTimeSpan;
		private int _dataRev;

		private bool _hasWindMoments;
		private bool _hasRassMoments;
		private bool _hasFullSpectra;
		private bool _hasRassSpectra;
		private bool _hasShortTimeSeries;
		private bool _hasFullTimeSeries;

		public PFData PopFileData;

		public DacPopFile() {
			//
			// TODO: Add constructor logic here
			//
			_lastHdrPos = -1;
			_hdrReaderPosition = 0;
			_HReader = null;
			_zeroTimeSpan = new TimeSpan(0, 0, 0);
		}

		protected override long CustomReadFileHeader() {
			return 0;
		}

		protected override bool CustomReadRecord(DacData data, out DateTime timeStamp, out long recordSize, out long filePositionChange) {

			int fileID, spc;
			int hdrPos;
			long time;
			//int junk;
			int nCI, nSpec;
			int currentRadar, currentBeam;
			long expectedBytes = 0;
			////long nBytesAdjustment = 0;

			PopData popData;
			popData = data as PopData;
			timeStamp = new DateTime(1970,1,1,0,0,0);
			recordSize = filePositionChange = 0;

			long pos0 = _BReader.BaseStream.Position;

			int bytesRead = 0;
			try {
				fileID = _BReader.ReadInt16();
				if (fileID == 0) {
					return false;
				}
				bytesRead += 2;
				_dataRev = fileID;
				filePositionChange = recordSize = _BReader.ReadInt32();
				bytesRead += 4;
				if (fileID >= 3000) {
					spc = _BReader.ReadInt16();
					bytesRead += 2;
				}
				else {
					spc = 1;  // or 0 if *.mom file
				}
				hdrPos = _BReader.ReadInt32();
				//time = _BReader.ReadInt32();
				time = _BReader.ReadUInt32();
				bytesRead += 8;
			}
			catch (EndOfStreamException) {
				timeStamp = DateTime.MaxValue;
				fileID = 9999;
				//data = null;
				return false;
			}

			timeStamp = timeStamp.AddSeconds((double)time);
			if (timeStamp.Year >= 2038) {
				// time is probably from era when it was measured from 1900 instead of 1970
				timeStamp = new DateTime(1899, 12, 31, 0, 0, 0);
				timeStamp = timeStamp.AddSeconds((double)time);
			}
			if (data != null) {
				// store data base class values
				data.Notes = "File type = " + fileID.ToString() + ", spc = " + spc.ToString();
				data.TimeStamp = timeStamp;
			}


			long skipBytes = recordSize - bytesRead;
			if (popData == null) {
				// can't store any data in data object so skip to end of record
				_BReader.BaseStream.Seek(skipBytes, SeekOrigin.Current);
			}
			else {
				// read the rest of the file preamble
				currentRadar = _BReader.ReadInt16();
				currentBeam = _BReader.ReadInt16();
				bytesRead += 4;

				long pos1 = _BReader.BaseStream.Position;
				expectedBytes = pos1 - pos0;

				// do we need to read a new header?
				if (_lastHdrPos != hdrPos) {
					ReadNewHeader(hdrPos);
					_lastHdrPos = hdrPos;
				}

				SetHeaderParameters(currentBeam, popData, fileID, spc, recordSize, bytesRead);
                if ((popData.Hdr.HasFullTimeSeries) && (FileName.ToLower().EndsWith(".raw.ts"))) {
                    popData.Hdr.HasFMRawTimeSeries = true;
                }
                else {
                    popData.Hdr.HasFMRawTimeSeries = false;
                }

				bool readThese = IsNciNspecThere(popData, fileID, recordSize);
				if (readThese) {
					nCI = _BReader.ReadInt16();
					nSpec = _BReader.ReadInt16();
				}

/*				// determine what data sets should be in data file
				_hasWindMoments = false;
				_hasRassMoments = false;
				_hasFullSpectra = false;
				_hasRassSpectra = false;
				_hasShortTimeSeries = false;
				_hasFullTimeSeries = false;

				if (popData.Hdr.NMet > 0) {
					expectedBytes += 4 * popData.Hdr.NMet;
				}

				if (fileID == 3115 || fileID == 3116 || fileID == 3117) {
					_hasWindMoments = true;
					expectedBytes += 8 * popData.Hdr.NHts;
				}
				if (fileID == 3116 || fileID == 3117) {
					if (popData.NSets != 2) {
						System.Windows.Forms.MessageBox.Show("NSets should be 2 for type 3116 or 3117 (RASS).");
					}
					_hasRassMoments = true;
					expectedBytes += 8 * popData.Hdr.NHts;
				}
				if (fileID == 3115 || fileID == 3118) {
					if ((spc != 1) && (spc != 2) && (spc != 5) && (spc != 7) && (spc != 8)) {
						System.Windows.Forms.MessageBox.Show("Spc must be 1,2,5,7,or 8 for types 3115 or 3118.");
					}
					_hasFullSpectra = true;
					expectedBytes += 4 * popData.Hdr.NHts * popData.Hdr.NPts;
					// if numpts = dop1 rather than NPts:
					nBytesAdjustment = 4 * popData.Hdr.NHts * popData.Hdr.Dop1 - (4 * popData.Hdr.NHts * popData.Hdr.NPts);
				}
				else if (fileID == 3117 || fileID == 3119) {
					_hasRassSpectra = true;
					expectedBytes += 4 * popData.Hdr.NHts * (popData.Hdr.Dop1 + popData.Hdr.Dop3);
				}
				if ((spc == 4) || (spc == 5)) {
					_hasShortTimeSeries = true;
					expectedBytes += 8 * popData.Hdr.NHts * popData.Hdr.NPts;
				}
				else if ((spc == 3) || ((spc > 5) && (spc < 9))) {
					_hasFullTimeSeries = true;
					expectedBytes += 8 * popData.Hdr.NHts * popData.Hdr.NPts * popData.Hdr.NSpec;
 				}

				if (recordSize != expectedBytes+4) {
					if (recordSize != expectedBytes+4+nBytesAdjustment) {
						throw new Exception("Data record nbytes not expected.");
					}
				}
*/
				for (int iht = 0; iht < popData.NHts; iht++) {
					popData.Hts[iht] = (float)popData.GetHtKm(iht, true);
				}

				float nyquistUnit = (float)(popData.Nyquist/10000.0);

				if (_hasWindMoments) {
					if ((_dataRev == 2015) || (_dataRev == 3015) || (_dataRev == 3016)) {
                        for (int irx = 0; irx < popData.Hdr.NRx; irx++) {
						    for (int iht = 0; iht < popData.Hdr.NHts; iht++) {
							    popData.Vel[irx, 0, iht] = _BReader.ReadInt16() * nyquistUnit;		// Doppler (1 unit = Nyquist/1e4)
                                popData.Width[irx, 0, iht] = (SByte)_BReader.ReadByte() * nyquistUnit / 100.0f;	// Width (1 unit = Nyquist/1e2)
                                popData.Snr[irx, 0, iht] = (SByte)_BReader.ReadByte();					// SNR (1 unit = 1 dB = decibels)
							    SByte ns = (SByte)_BReader.ReadByte();
							    int ins = (int)ns;
                                popData.Noise[irx, 0, iht] = (float)Math.Pow(10.0, ns * 0.1f);	// Noise ( == 10*log10(noise level))
						    }
                        }
                    }
					else {
						// read 2-byte wind moments
                        for (int irx = 0; irx < popData.Hdr.NRx; irx++) {
                            for (int i = 0; i < popData.Hdr.NHts; i++) {
                                popData.Vel[irx, 0, i] = _BReader.ReadInt16() * nyquistUnit;		// Doppler (1 unit = Nyquist/1e4)
                                popData.Width[irx, 0, i] = _BReader.ReadInt16() * nyquistUnit;	// Width
                                popData.Snr[irx, 0, i] = _BReader.ReadInt16() * 0.01f;				// SNR (1 unit = 0.01 dB = millibels)
                                popData.Noise[irx, 0, i] = (float)Math.Pow(10.0, _BReader.ReadInt16() * 0.001f);	// Noise ( == 1000*log10(noise level) = millibels)
                            }
                        }
					}
				}
				if (_hasRassMoments) {
					if ((_dataRev == 3016)) {
						throw new Exception("Data Version 3016 not supported yet.");
					}
					else {
						// read 2-byte RASS temp moments
                        for (int irx = 0; irx < popData.Hdr.NRx; irx++) {
                            for (int i = 0; i < popData.Hdr.NHts; i++) {
                                popData.Vel[irx, 1, i] = _BReader.ReadInt16() * nyquistUnit;		// Doppler
                                popData.Width[irx, 1, i] = _BReader.ReadInt16() * nyquistUnit;	// Width
                                popData.Snr[irx, 1, i] = _BReader.ReadInt16() * 0.01f;			// SNR
                                _BReader.ReadInt16();	// Temperature (1 unit = 0.1 degree C)(default -9999)
                            }
                        }
					}
				}

				// read Met instrument data;
				for (int i = 0; i < popData.Hdr.NMet; i++) {
					float nn = _BReader.ReadSingle();
                    if (popData.Hdr.MetCodes[i] == FRAC_SEC_INST_CODE) {
                        popData.TimeStamp = popData.TimeStamp.AddMilliseconds(1000.0 * nn);
                    }
                    //
                    // TEMPORARY FIX TO READ BAD POP FILES
                    //  (extra instrument reading was written)
                    //
                    /*
                    float mm = _BReader.ReadSingle();
                    if (mm != 123.0) {
                        throw new ApplicationException("Using temporary fix version on wrong file (DACarter.NOAA.DacPopFile)");
                    }
                    */
                }

				// read spectra
				if (_hasFullSpectra) {
					// read full spectra
					long pos = _BReader.BaseStream.Position;
					long len = _BReader.BaseStream.Length;
                    for (int irx = 0; irx < popData.Hdr.NRx; irx++) {
                        for (int iht = 0; iht < popData.Hdr.NHts; iht++) {
                            for (int ipt = 0; ipt < popData.Hdr.WindsSpectrumNumPoints; ipt++) {
                                popData.Spectra[irx, iht, ipt] = _BReader.ReadSingle();
                            }
                        }
                    }
				}
				else if (_hasRassSpectra) {
					// read RASS spectra
                    for (int irx = 0; irx < popData.Hdr.NRx; irx++) {
                        for (int iht = 0; iht < popData.Hdr.NHts; iht++) {
                            for (int ipt = 0; ipt < (popData.Hdr.WindsSpectrumNumPoints + popData.Hdr.RassSpectrumNumPoints); ipt++) {
                                popData.Spectra[irx, iht, ipt] = _BReader.ReadSingle();
                            }
                        }
                    }
				}

				// read time series
				if (_hasShortTimeSeries) {
					// read single time series
                    for (int irx = 0; irx < popData.Hdr.NRx; irx++) {
                        for (int iht = 0; iht < popData.Hdr.NHts; iht++) {
                            for (int ipt = 0; ipt < popData.Hdr.NPts; ipt++) {
                                _BReader.ReadSingle();
                                _BReader.ReadSingle();
                            }
                        }
                    }
				}
				else if (_hasFullTimeSeries) {
					// read full time series
					bool storeThisTimeSeries = true; ;
					int maxPts = popData.TimeSeries.GetUpperBound(2);	// how many do we have room for?
                    for (int irx = 0; irx < popData.Hdr.NRx; irx++) {
                        for (int iht = 0; iht < popData.Hdr.NHts; iht++) {
                            for (int ipt = 0; ipt < popData.Hdr.NPts * popData.Hdr.NSpec; ipt++) {
                                if (ipt > maxPts) {
                                    storeThisTimeSeries = false;
                                }
                                if (storeThisTimeSeries) {
                                    popData.TimeSeries[irx, iht, ipt, 0] = _BReader.ReadSingle();
                                    popData.TimeSeries[irx, iht, ipt, 1] = _BReader.ReadSingle();
                                }
                                else {
                                    _BReader.ReadSingle();
                                    _BReader.ReadSingle();
                                }
                            }
                        }
                    }
				}

				int nBytes = _BReader.ReadInt32();

                long endPos = _BReader.BaseStream.Position;

				popData.Notes += ", Dir=" + popData.Hdr.DirName + " (" + popData.Hdr.Azimuth.ToString("f0") + "deg) PW=" + popData.Hdr.PW;
			}

			return true;
			
		}

		private static bool IsNciNspecThere(PopData popData, int fileID, long recordSize) {
			bool readThese = true;
			if (fileID == 2015) {
				// Some 2015 files do not have nci and nspec.
				// This is the size of files without nci and nspec:
				int nb = 22 + (5 * popData.Hdr.NHts) + (4 * popData.Hdr.NHts * popData.Hdr.NPts);
				if (nb == recordSize) {
					readThese = false;
				}
			}
			return readThese;
		}

		private void SetHeaderParameters(int currentBeam, PopData popData, int fileID, int spc, long recSize, int bytesAlready) {

			long nBytesAdjustment = 0;

			long expectedBytes = bytesAlready;  // bytes already read at this point

			// assign _currentHeader to popData
			int cbm = currentBeam;
			int cdir = _currentHeader.SysPar.Beams[cbm].DirIndex;
			int cpar = _currentHeader.SysPar.Beams[cbm].ParameterIndex;
			int cbw = _currentHeader.SysPar.BeamParams[cpar].BWIndex;
			popData.CurrentBeamIndex = currentBeam;
			popData.Hdr.Altitude = _currentHeader.SysPar.Altitude;
			popData.Hdr.Atten = _currentHeader.SysPar.BeamParams[cpar].NAtten;
			popData.Hdr.AveragingTime = _zeroTimeSpan;
			popData.Hdr.Azimuth = _currentHeader.SysPar.Directions[cdir].Azimuth;
			popData.Hdr.AntSwitchCode = _currentHeader.SysPar.Directions[cdir].SwitchCode;  // added dac 2009Feb25
			popData.Hdr.BwCode = cbw;
			popData.Hdr.CltrHt = _currentHeader.SysPar.Processing.CltrHt;
			popData.Hdr.DcFilter = _currentHeader.SysPar.Processing.DcFilter;
			popData.Hdr.Delay = _currentHeader.SysPar.BeamParams[cpar].Delay;
			popData.Hdr.DirName = _currentHeader.SysPar.Directions[cdir].DirectionLabel;
			popData.Hdr.Elevation = _currentHeader.SysPar.Directions[cdir].Elevation;
			popData.Hdr.IPP = _currentHeader.SysPar.BeamParams[cpar].IPP;
			popData.Hdr.LatitudeN = _currentHeader.SysPar.Latitude;
			popData.Hdr.LongitudeE = _currentHeader.SysPar.Longitude;
			popData.Hdr.MinutesToUT = _currentHeader.SysPar.MinutesToUT;
			popData.Hdr.NCI = _currentHeader.SysPar.BeamParams[cpar].NCI;
			popData.Hdr.NCode = _currentHeader.SysPar.BeamParams[cpar].NCode;
            //popData.Hdr.Flip = 0;  // TODO where is this?
            popData.Hdr.Flip = _currentHeader.SysPar.BeamParams[cpar].Flip;
			popData.Hdr.NHts = _currentHeader.SysPar.BeamParams[cpar].NHts;
			popData.Hdr.NMet = _currentHeader.NumInstruments;
            popData.Hdr.MetCodes = new int[popData.Hdr.NMet];
            if (_currentHeader.HdrInstrumentCodes != null) {
                for (int i = 0; i < popData.Hdr.NMet; i++) {
                    popData.Hdr.MetCodes[i] = _currentHeader.HdrInstrumentCodes[i];
                }
            }
            popData.Hdr.RxMode = _currentHeader.ReceiverMode;
			popData.Hdr.NPts = _currentHeader.SysPar.BeamParams[cpar].NPts;
			popData.Hdr.NRx = _currentHeader.SysPar.Processing.NRx;
			popData.Hdr.NRxMode = 0;
			if (_currentHeader.SysPar.Processing.RassParams.RassIsOn) {
				popData.Hdr.NSets = 2;
			}
			else {
				popData.Hdr.NSets = 1;
			}
			popData.Hdr.NSpec = _currentHeader.SysPar.BeamParams[cpar].NSpec;
			popData.Hdr.PBPostBlank = _currentHeader.SysPar.PbxConstants.PBPostBlank;
			popData.Hdr.PBPostTR = _currentHeader.SysPar.PbxConstants.PBPostTR;
			popData.Hdr.PBPreBlank = _currentHeader.SysPar.PbxConstants.PBPreBlank;
			popData.Hdr.PBPreTR = _currentHeader.SysPar.PbxConstants.PBPreTR;
			popData.Hdr.PBSynch = _currentHeader.SysPar.PbxConstants.PBSynch;
			popData.Hdr.PW = _currentHeader.SysPar.BeamParams[cpar].PW;
			popData.Hdr.RadarID = _currentHeader.SysPar.RadarID;
			popData.Hdr.RadarName = _currentHeader.SysPar.RadarName;
			popData.Hdr.RassDwellMs = _currentHeader.SysPar.Processing.RassParams.RassDwell;
			popData.Hdr.RassHighFrequencyHz = _currentHeader.SysPar.Processing.RassParams.RassEndFreq;
			popData.Hdr.RassIsOn = _currentHeader.SysPar.Processing.RassParams.RassIsOn;
			popData.Hdr.RassLowFrequencyHz = _currentHeader.SysPar.Processing.RassParams.RassBeginFreq;
			popData.Hdr.RassStepHz = _currentHeader.SysPar.Processing.RassParams.RassStep;
			popData.Hdr.RassSweep = _currentHeader.SysPar.Processing.RassParams.RassSweep;
			popData.Hdr.Spacing = _currentHeader.SysPar.BeamParams[cpar].Spacing;
			popData.Hdr.SpecAvg = _currentHeader.SysPar.Processing.SpecAvg;
			popData.Hdr.StationName = _currentHeader.SysPar.StationName;
			popData.Hdr.SysDelay = _currentHeader.SysPar.RxBw[cbw].RxDelay;
			popData.Hdr.TimeConvention = 0;
			popData.Hdr.TxFreq = _currentHeader.SysPar.Frequency;
			popData.Hdr.TxIsOn = _currentHeader.SysPar.TxIsOn;
			popData.Hdr.Window = _currentHeader.SysPar.Processing.Window;

			// these are assuming dop1 is correct;  see below for correction.
			popData.Hdr.WindsSpectrumBeginIndex = _currentHeader.SysPar.Processing.Dop0 - 1;
			popData.Hdr.WindsSpectrumNumPoints = _currentHeader.SysPar.Processing.Dop1;
			if (popData.Hdr.WindsSpectrumBeginIndex < 0) {
				// for old files with no Dop values (all == -1)
				popData.Hdr.WindsSpectrumBeginIndex = 0;
				popData.Hdr.WindsSpectrumNumPoints = popData.Hdr.NPts;
			}
			popData.Hdr.RassSpectrumBeginIndex = _currentHeader.SysPar.Processing.Dop2 - 1;
			popData.Hdr.RassSpectrumNumPoints = _currentHeader.SysPar.Processing.Dop3;

			// determine what data sets should be in data file
			_hasWindMoments = false;
			_hasRassMoments = false;
			_hasFullSpectra = false;
			_hasRassSpectra = false;
			_hasShortTimeSeries = false;
			_hasFullTimeSeries = false;

			if (IsNciNspecThere(popData, fileID, recSize)) {
				// we will be reading nci and nspec
				expectedBytes += 4;
			}

			if (popData.Hdr.NMet > 0) {
				expectedBytes += 4 * popData.Hdr.NMet;
			}

            if (fileID == 3115) {
                if (popData.Hdr.NPts > popData.Hdr.WindsSpectrumNumPoints) {
                    // even though not run as RASS, spectral data is stored like RASS
                }
            }

			if (fileID == 3115 || fileID == 3116 || fileID == 3117) {
                if (spc == 0 ||
                    spc == 1 ||
                    spc == 4 ||
                    spc == 5 ||
                    spc == 6 ||
                    spc == 8 ||
                    (spc > 1000 && ((spc - 1000) & 2) != 0)) {
                    _hasWindMoments = true;
                    expectedBytes += 8 * (popData.Hdr.NHts * popData.Hdr.NRx);
                }
                else {
                    _hasWindMoments = false;
                }
			}
			else if (fileID == 2015 || fileID == 3015 || fileID == 3016) {
				_hasWindMoments = true;
				expectedBytes += 5 * (popData.Hdr.NHts * popData.Hdr.NRx);
			}
			else if (fileID < 3000) {
				throw new ApplicationException("DacPopFile not expecting data format type " + fileID );
			}
			if (fileID == 3116 || fileID == 3117) {
				if (popData.NSets != 2) {
					System.Windows.Forms.MessageBox.Show("NSets should be 2 for type 3116 or 3117 (RASS).");
				}
				_hasRassMoments = true;
                expectedBytes += 8 * (popData.Hdr.NHts * popData.Hdr.NRx);
			}
			else if (fileID == 3016) {
				if (popData.NSets != 2) {
					System.Windows.Forms.MessageBox.Show("NSets should be 2 for type 3016 (RASS).");
				}
				_hasRassMoments = true;
                expectedBytes += 6 * (popData.Hdr.NHts * popData.Hdr.NRx);
			}
			if (fileID == 3115 || fileID == 3118 || fileID == 3015 || fileID == 2015) {
				if ((spc != 0) && (spc != 1) && (spc != 2) && (spc != 5) && (spc != 7) && (spc != 8)) {
                    // removed dac 2011-04-01 to allow non-pop-compliant files
					//System.Windows.Forms.MessageBox.Show("Spc must be 0,1,2,5,7,or 8 for types 3115 or 3118.");
				}
                if (spc == 1 ||
                    spc == 2 ||
                    spc == 5 ||
                    spc == 7 ||
                    spc == 8 ||
                    (spc > 1000 && ((spc - 1000) & 1) != 0)) {
                    _hasFullSpectra = true;
					expectedBytes += 4 * popData.Hdr.NHts * popData.Hdr.WindsSpectrumNumPoints * popData.Hdr.NRx;
				}
				else {
					_hasFullSpectra = false;
				}
				//expectedBytes += 4 * popData.Hdr.NHts * popData.Hdr.WindsSpectrumNumPoints;
				// if dop1 is incorrect and should be equal to NPts:
                nBytesAdjustment = 4 * popData.Hdr.NHts * popData.Hdr.NRx * (popData.Hdr.NPts - popData.Hdr.WindsSpectrumNumPoints);
			}
			else if (fileID == 3117 || fileID == 3119) {
				if (spc != 0) {
					_hasRassSpectra = true;
                    expectedBytes += 4 * popData.Hdr.NHts * popData.Hdr.NRx * (popData.Hdr.WindsSpectrumNumPoints + popData.Hdr.RassSpectrumNumPoints);
				}
				else {
					_hasRassSpectra = false;
				}
			}
            if (spc == 4 ||
                spc == 5 ||
                (spc > 1000 && ((spc - 1000) & 4) != 0)) {
                _hasShortTimeSeries = true;
                expectedBytes += 8 * popData.Hdr.NHts * popData.Hdr.NRx * popData.Hdr.NPts;
			}
            else {
                 _hasShortTimeSeries = false;
            }

            if (spc == 3 ||
                spc == 6 ||
                spc == 7 ||
                spc == 8 ||
                (spc > 1000 && ((spc - 1000) & 8) != 0)) {
                _hasFullTimeSeries = true;
                expectedBytes += 8 * popData.Hdr.NHts * popData.Hdr.NRx * popData.Hdr.NPts * popData.Hdr.NSpec;
            }
            else {
                _hasFullTimeSeries = false;
            }

			popData.Hdr.HasFullSpectra = _hasFullSpectra;
			popData.Hdr.HasFullTimeSeries = _hasFullTimeSeries;
			popData.Hdr.HasRassMoments = _hasRassMoments;
			popData.Hdr.HasRassSpectra = _hasRassSpectra;
			popData.Hdr.HasShortTimeSeries = _hasShortTimeSeries;
			popData.Hdr.HasWindMoments = _hasWindMoments;

			// Adjust Dop1 (WindsSpectrumNumPoints) for the case of winds only, full spectra
			// where NPTS is correct but dop1 is not.
			// (This may occur in files written by original POP4, non-rass modes,
			//		where NPTS changes between modes -- dop1 is not updated --
			//		and the only way we can tell is from size of spectra in file.)
			if (recSize != expectedBytes + 4) {
				if (recSize != expectedBytes + 4 + nBytesAdjustment) {
					throw new Exception("Data record nbytes not expected.");
				}
				else {
					popData.Hdr.WindsSpectrumBeginIndex = 0;
					popData.Hdr.WindsSpectrumNumPoints = popData.Hdr.NPts;
					popData.Hdr.RassSpectrumBeginIndex = 0;
					popData.Hdr.RassSpectrumNumPoints = 0;
				}
			}

			// now initialize the internal arrays of the data object
			popData.InitFromHeader();

		}

		private void ReadNewHeader(long hdrPos) {

			if (hdrPos != _hdrReaderPosition) {
				long actualPos = _HReader.BaseStream.Seek(hdrPos, SeekOrigin.Begin);
				if (actualPos != hdrPos) {
					throw new Exception("Cannot move to header given in data file.");
				}
				_hdrReaderPosition = hdrPos;
			}

			//long pos = _HReader.BaseStream.Position;

			

			int nBytesRead = 0;
			_currentHeader.RevLevel = _HReader.ReadInt16();
			_currentHeader.HdrBytes = _HReader.ReadInt16();
			if (_currentHeader.RevLevel >= 103) {
				_currentHeader.NumInstruments = _HReader.ReadInt16();
			}
			else {
				_currentHeader.NumInstruments = 0;
			}
			if (_currentHeader.RevLevel >= 103) {
				_HReader.ReadBytes(10);	// skip array dimensions
			}
			long pos1 = _HReader.BaseStream.Position;
			char[] StationNameChars = _HReader.ReadChars(32);
			string name = new string(StationNameChars);
			int index = name.IndexOf('\0');
			if (index >= 0) {
				_currentHeader.SysPar.StationName = name.Remove(index);
			}
			else {
				_currentHeader.SysPar.StationName = name;
			}
			//
			// TODO sometimes file pointer has advanced an extra 2 bytes
			//
			long pos2 = _HReader.BaseStream.Position;
			_currentHeader.SysPar.Latitude = (double)_HReader.ReadInt16() / 100.0;
			_currentHeader.SysPar.Longitude = (double)_HReader.ReadInt16() / 100.0;
			_currentHeader.SysPar.MinutesToUT = _HReader.ReadInt16();
			_currentHeader.SysPar.Altitude = _HReader.ReadInt16();
			int nradars =  _HReader.ReadInt16();
			char[] RadarNameChars = _HReader.ReadChars(32);
			name = new string(RadarNameChars);
			index = name.IndexOf('\0');
			if (index >= 0) {
				_currentHeader.SysPar.RadarName = name.Remove(index);
			}
			else {
				_currentHeader.SysPar.RadarName = name;
			}
			//_currentHeader.SysPar.RadarName = name.Trim('\0');
			_currentHeader.SysPar.RadarID = _HReader.ReadInt16();
			_currentHeader.SysPar.Frequency = (double)_HReader.ReadInt32() * 1.0e4;
			_HReader.ReadBytes(6);	// skip max duty cycle and max TX
			int txon = _HReader.ReadInt16();
			if (txon == 0) {
				_currentHeader.SysPar.TxIsOn = false;
			}
			else {
				_currentHeader.SysPar.TxIsOn = true;
			}
			_currentHeader.SysPar.NumDirections = _HReader.ReadInt16();
			_currentHeader.SysPar.NumBeams = _HReader.ReadInt16();
			_currentHeader.SysPar.NumParamSets = _HReader.ReadInt16();

			// read HdrBeamParameters[4] BeamParams;
			for (int i = 0; i < 4; i++ ) {
				_currentHeader.SysPar.BeamParams[i].IPP = _HReader.ReadInt32();
				_currentHeader.SysPar.BeamParams[i].PW = _HReader.ReadInt32();
				_currentHeader.SysPar.BeamParams[i].Delay = _HReader.ReadInt32();
				_currentHeader.SysPar.BeamParams[i].Spacing = _HReader.ReadInt32();
				_currentHeader.SysPar.BeamParams[i].NHts = _HReader.ReadInt16();
				_currentHeader.SysPar.BeamParams[i].NCI = _HReader.ReadInt16();
				_currentHeader.SysPar.BeamParams[i].NSpec = _HReader.ReadInt16();
				_currentHeader.SysPar.BeamParams[i].NPts = _HReader.ReadInt16();
				_currentHeader.SysPar.BeamParams[i].SysDelay = _HReader.ReadInt16();
				_currentHeader.SysPar.BeamParams[i].BWIndex = _HReader.ReadInt16();
				_currentHeader.SysPar.BeamParams[i].NAtten = _HReader.ReadInt16();
				_currentHeader.SysPar.BeamParams[i].NCode = _HReader.ReadInt16();
			}

			// read HdrBeamControl[10] Beams;	
			for (int i = 0; i < 10; i++) {
				_currentHeader.SysPar.Beams[i].DirIndex = _HReader.ReadInt16();
				_currentHeader.SysPar.Beams[i].ParameterIndex = _HReader.ReadInt16();
				_currentHeader.SysPar.Beams[i].Repetitions = _HReader.ReadInt16();
			}

			// read HdrPbxConstants PbxConstants; 
			_currentHeader.SysPar.PbxConstants.PBPreTR = _HReader.ReadInt32();
			_currentHeader.SysPar.PbxConstants.PBPostTR = _HReader.ReadInt32();
			_currentHeader.SysPar.PbxConstants.PBSynch = _HReader.ReadInt32();
			_currentHeader.SysPar.PbxConstants.PBPreBlank = _HReader.ReadInt32();
			_currentHeader.SysPar.PbxConstants.PBPostBlank = _HReader.ReadInt32();

			// read HdrDirections[9] Directions;	
			for (int i = 0; i < 9; i++) {
				char[] dirChars = _HReader.ReadChars(12);
				name = new string(dirChars);
				// Lapxm allows non null characters after end of string
				//_currentHeader.SysPar.Directions[i].DirectionLabel = name.Trim('\0');
				index = name.IndexOf('\0');
				if (index >= 0) {
					_currentHeader.SysPar.Directions[i].DirectionLabel = name.Remove(index);
				}
				else {
					_currentHeader.SysPar.Directions[i].DirectionLabel = name;
				}
				_currentHeader.SysPar.Directions[i].Azimuth = (double)_HReader.ReadInt16();
				int elev = _HReader.ReadInt16();
				if (elev <= 90) {
					_currentHeader.SysPar.Directions[i].Elevation = (double)elev;
				}
				else {
					_currentHeader.SysPar.Directions[i].Elevation = (double)elev / 100.0;
				}
				_currentHeader.SysPar.Directions[i].SwitchCode = _HReader.ReadInt16();
			}

			// read HdrRxBandwidth[4] RxBw;	
			for (int i = 0; i < 4; i++) {
				_currentHeader.SysPar.RxBw[i].PulseWidth = _HReader.ReadInt16();
			}
			for (int i = 0; i < 4; i++) {
				_currentHeader.SysPar.RxBw[i].RxDelay = _HReader.ReadInt16();
			}
			_currentHeader.SysPar.Processing.DcFilter = _HReader.ReadInt16();
			_currentHeader.SysPar.Processing.Window = _HReader.ReadInt16();
			_currentHeader.SysPar.Processing.DcOmit = _HReader.ReadInt16();
			_currentHeader.SysPar.Processing.OmitHts = _HReader.ReadInt16();
			_currentHeader.SysPar.Processing.Dop0 = _HReader.ReadInt16();
			_currentHeader.SysPar.Processing.Dop1 = _HReader.ReadInt16();
			_currentHeader.SysPar.Processing.Dop2 = _HReader.ReadInt16();
			_currentHeader.SysPar.Processing.Dop3 = _HReader.ReadInt16();
			if (_currentHeader.SysPar.Processing.Dop2 < 0) {
				_currentHeader.SysPar.Processing.Dop2 = 1;
			}
			if (_currentHeader.SysPar.Processing.Dop3 < 0) {
				_currentHeader.SysPar.Processing.Dop3 = 0;
			}
			int rxon = _HReader.ReadInt16();
			if (rxon <= 0) {
				_currentHeader.SysPar.Processing.RassParams.RassIsOn = false;
			}
			else {
				_currentHeader.SysPar.Processing.RassParams.RassIsOn = true;
			}
			_currentHeader.SysPar.Processing.RassParams.RassBeginFreq = _HReader.ReadInt16();
			_currentHeader.SysPar.Processing.RassParams.RassEndFreq = _HReader.ReadInt16();
			_currentHeader.SysPar.Processing.RassParams.RassStep = _HReader.ReadInt16();
			_currentHeader.SysPar.Processing.RassParams.RassDwell = _HReader.ReadInt16();
			_currentHeader.SysPar.Processing.RassParams.RassSweep = _HReader.ReadInt16();
			_currentHeader.SysPar.Processing.CltrHt = _HReader.ReadInt16();
			_currentHeader.SysPar.Processing.SpecAvg = _HReader.ReadInt16();
			_currentHeader.SysPar.Processing.NRx = _HReader.ReadInt16();
			if (_currentHeader.SysPar.Processing.NRx < 1) {
				_currentHeader.SysPar.Processing.NRx = 1;
			}
			_currentHeader.SysPar.Processing.NMet = _HReader.ReadInt16();
			if ((_currentHeader.SysPar.Processing.NMet < 1) || (_currentHeader.SysPar.Processing.NMet > 1000)) {
				_currentHeader.SysPar.Processing.NMet = _currentHeader.NumInstruments;
				if (_currentHeader.SysPar.Processing.NMet < 1) {
					_currentHeader.SysPar.Processing.NMet = 0;
					_currentHeader.NumInstruments = 0;
				}
			}
			if (_currentHeader.SysPar.Processing.NMet != _currentHeader.NumInstruments) {
				throw new ApplicationException("Number of met instruments mismatched");
			}

            // Lapxm additions to "reserved for future use" block
            /*
            _hWriter.Write((Int16)0);       // iOverlap, set when overlapping Doppler times series
            _hWriter.Write((Int16)0);       // iConcatenatedTimeSeries; time series has been concatenated
            _hWriter.Write((Int16)0);       // iVertCorrectHw; hardware vertical range correction used
            _hWriter.Write((Int16)0);       // iVertCorrectHWAngleDeg_X100; angle used to correct vertical in hardware
            _hWriter.Write((Int16)0);       // iSampleClockMHz_X100; Clock rate used to generate range sample timing
            _hWriter.Write((Int16)0);       // iFlip; set if flip of tx phase used
            _hWriter.Write((Int16)4);       // iReceiverMode; has flag 4 (3rd lsb) set if parallel sampling of receivers;
            //      flag 4 cleared if multiplexed (when word = -1, rx's were multiplexed).
            //      Other modes have been defined by Lapxm.
            int NMISC = 15;
            for (int i = 0; i < NMISC; i++) {
                _hWriter.Write((Int16)(-1));
            }
            */

            Int16 iOverlap = _HReader.ReadInt16();
            Int16 iConcatenatedTimeSeries = _HReader.ReadInt16();
            Int16 iVertCorrecteHw = _HReader.ReadInt16();
            Int16 iVertCorrectHwAngleDeg_X100 = _HReader.ReadInt16();
            Int16 iSampleClockMHz_X100 = _HReader.ReadInt16();
            Int16 iFlip = _HReader.ReadInt16();
            Int16 iReceiverMode = _HReader.ReadInt16();
            Int16 ffu;
			for (int i = 0; i < 15; i++) {
                // reserved "for future use"
				ffu = _HReader.ReadInt16();
			}
            //
            if (iFlip < 1) {
                iFlip = 0;
            }
            else {
                iFlip = 1;
            }
            for (int i = 0; i < 4; i++) {
                _currentHeader.SysPar.BeamParams[i].Flip = iFlip;
            }
            _currentHeader.ReceiverMode = iReceiverMode;

            //

			_currentHeader.DataFilePosition = _HReader.ReadInt32();

			if (_currentHeader.RevLevel >= 103) {
				nBytesRead = 580;
			}
			else if (_currentHeader.RevLevel > 100) {
				nBytesRead = 568;
			}
			else {
				// RevLevel <= 100
				nBytesRead = 572;
			}

			// read met instrument codes
            Int16 instCode;
            int numInst = _currentHeader.NumInstruments;
			for (int i = 0; i < numInst; i++) {
                instCode = _HReader.ReadInt16();
                if (_currentHeader.HdrInstrumentCodes == null) {
                    _currentHeader.HdrInstrumentCodes = new int[numInst];
                }
                if (_currentHeader.HdrInstrumentCodes.Length >= (i + 1)) {
                    _currentHeader.HdrInstrumentCodes[i] = instCode;
                }
				nBytesRead += 2;
			}

			if (_currentHeader.RevLevel <= 100) {
				_HReader.ReadInt32();
			}

			_hdrReaderPosition += nBytesRead;

		}

		protected override bool CustomSkipRecord(out long currentPosition) {
			throw new NotImplementedException();
		}

		protected override bool CustomReadRecordTime(out DateTime timeStamp, out long recSize, out long filePositionChange) {
			PopData data = null;
			return CustomReadRecord(data, out timeStamp, out recSize, out filePositionChange);
		}

		protected override bool CustomOpenInit() {

			// get header file name;
			string dataFileName = FileName;
			string fname = Path.GetFileName(dataFileName);
			string folder = Path.GetDirectoryName(dataFileName);
			string hname = 'H' + fname.Substring(1);
			string headerFileName = Path.Combine(folder, hname);

			// Create arrays of structs in header structure.
			//	This will contain all info in current header record.
			_currentHeader.SysPar.BeamParams = new HdrBeamParameters[4];
			_currentHeader.SysPar.Beams = new HdrBeamControl[10];
			_currentHeader.SysPar.Directions = new HdrDirections[9];
			_currentHeader.SysPar.RxBw = new HdrRxBandwidth[4];

			// open header file
			FileStream fs = null;
			try {
				fs = new FileStream(headerFileName, FileMode.Open, FileAccess.Read);
				//_HReader = new BinaryReader(fs);
				_HReader = new BinaryReader(fs, System.Text.Encoding.ASCII);
			}
			catch (Exception) {
				//Console.Write(e);
				if (fs != null) {
					fs.Close();
				}
				if (_HReader != null) {
					_HReader.Close();
				}
				return false;
			}

			_lastHdrPos = -1;
			return true;
		}

		protected override OpenFileType GetOpenFileType() {

			return OpenFileType.BinaryReader;

			// adding write option like this does not work,
			//		_BReader is not defined yet.
			/*
			if (_BReader != null) {
				return OpenFileType.BinaryReader;
			}
			else if (_BWriter != null) {
				return OpenFileType.BinaryWriter;
			}
			else {
				throw new ApplicationException("OpenFileType error in DacPopFile");
			}
			*/
		}

        // POPREV: added Close() 2012/05/24
        public void Close() {
            if (_HReader != null) {
                _HReader.Close();
                _HReader = null;
            }
            _lastHdrPos = 0;
            _hdrReaderPosition = 0;
            base.CloseFileReader(GetOpenFileType());
        }



	}
}
