using System.Collections.ObjectModel;
//using System.Speech.Synthesis;
using DACarter.PopUtilities;
using DACarter.Utilities;
using System.IO;
using System;
using System.Threading;

using POPN4Service;
using System.Text;

namespace POPN {

	class PopFileWriter3 {

		private PopDataPackage3 _dataPackage;
		private PopParameters _parameters;
        private PopParameters[] _prevParameters;
        private long[] _prevHeaderPos;
		private PopParameters.PopFileParameters[] _fileParams;
		private BinaryWriter _dWriter, _hWriter;
        private string _logFolder;

		private const Int16 MAXRADAR = 1;	// max array sizes for POP files
		private const Int16 MAXBMPAR = 4;
		private const Int16 MAXBM = 10;
		private const Int16 MAXDIR = 9;
		private const Int16 MAXBW = 4;

        public string ErrorMessage;
        public string StatusMessage;

		public PopDataPackage3 DataPackage {
			get { return _dataPackage; }
			set {
				_dataPackage = value;
				_parameters = _dataPackage.Parameters;
				_fileParams = _dataPackage.Parameters.SystemPar.RadarPar.ProcPar.PopFiles;
                _logFolder = _dataPackage.Parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].LogFileFolder;
			}
		}
		public PopParCurrentIndices CurrentParIndices { get; set; }

		//private SpeechSynthesizer _speaker;

		public PopFileWriter3(PopDataPackage3 dataPackage) {
			DataPackage = dataPackage;
            _prevParameters = new PopParameters[PopParameters.PopFileDim];
            _prevHeaderPos = new long[PopParameters.PopFileDim];
            _prevHeaderPos[0] = -1;
            _prevHeaderPos[1] = -1;
            _prevParameters[0] = null;
            _prevParameters[1] = null;
            //_speaker = new SpeechSynthesizer();
			//_speaker.SelectVoiceByHints(VoiceGender.Male);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
		public bool WritePopRecord() {

			//_speaker.SpeakAsync("The Write data file record method is under construction.");
			/*
			ReadOnlyCollection<InstalledVoice> voices = _speaker.GetInstalledVoices();
			foreach (InstalledVoice voice in voices) {
				string name = voice.VoiceInfo.Name;
				_speaker.SelectVoice(name);
				_speaker.Speak("Write data record is not implemented yet.");
			}
			*/

            ErrorMessage = "";
            StatusMessage = "";

			int nOutputFiles = _fileParams.Length;
		
			for (int iFile = 0; iFile < nOutputFiles; iFile++) {

				if (_fileParams[iFile].FileWriteEnabled) {

                    int nrx0 = _parameters.SystemPar.RadarPar.ProcPar.NumberOfRx;

                    bool writingBinary;  // 
                    if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[iFile].IncludeSpectra ||
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[iFile].IncludeMoments ||
                        (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[iFile].IncludeACorr) ||
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[iFile].IncludeXCorr ||
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[iFile].IncludeSingleTS ||
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[iFile].IncludeFullTS ||
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[iFile].WriteRawTSFile) {
                        writingBinary = true;
                        if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[iFile].IncludeACorr && nrx0 == 1) {
                            // if only 1 rx we would only have autocorr and not xcorr
                            //  but it will be stored as the only element in xcorr data array
                            writingBinary = false;
                        }
                        if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[iFile].IncludeXCorr) {
                            _parameters.SystemPar.RadarPar.ProcPar.PopFiles[iFile].IncludeMoments = false;
                        }
                   }
                    else {
                        writingBinary = false;
                    }

					string fullPath = GetFilePath(iFile);
					string headerPath = GetHeaderFilePath(fullPath);
                    string dataFolder = Path.GetDirectoryName(fullPath);
                    if (!Directory.Exists(dataFolder)) {
                        Directory.CreateDirectory(dataFolder);
                    }

                    if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[iFile].WriteSingleTSTextFile ||
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[iFile].WriteFullTSTextFile ||
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[iFile].WriteRawTSTextFile) {

                        // write time series text files

                        string textFile;
                        string baseName = Path.GetFileNameWithoutExtension(fullPath);
                        while (Path.HasExtension(baseName)) {
                            baseName = Path.GetFileNameWithoutExtension(baseName);
                        }
                        string folder = Path.GetDirectoryName(fullPath);

                        bool appendMode = !_parameters.SystemPar.RadarPar.ProcPar.PopFiles[iFile].WriteModeOverwrite;

                        string textExtension;
                        if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[iFile].WriteRawTSTextFile) {
                            textExtension = ".raw.ts.txt";
                            textFile = Path.Combine(folder, baseName + textExtension);
                            try {
                                using (StreamWriter writer = new StreamWriter(textFile, appendMode)) {
                                    if (_dataPackage.SampledTimeSeries != null) {
                                        DateTime time = _dataPackage.RecordTimeStamp;
                                        string line;
                                        //int nrx = _dataPackage.SampledTimeSeries.Length;
                                        int nrx = _dataPackage.Parameters.SystemPar.RadarPar.ProcPar.NumberOfRx;
                                        //int nspec = _dataPackage.SampledTimeSeries[0].Length;
                                        int nspec = _dataPackage.Parameters.SystemPar.RadarPar.BeamParSet[0].NSpec;
                                        //int npts = _dataPackage.SampledTimeSeries[0][0].Length;
                                        int npts = _dataPackage.Parameters.SystemPar.RadarPar.BeamParSet[0].NPts;
                                        //int nsamp = _dataPackage.SampledTimeSeries[0][0][0].Length;
                                        int nsamp = _dataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts;
                                        for (int irx = 0; irx < nrx; irx++) {
                                            for (int ispec = 0; ispec < nspec; ispec++) {
                                                for (int ipt = 0; ipt < npts; ipt++) {
                                                    line = String.Format("{0}, {1}, {2}, {3}, {4}", time.Year, time.DayOfYear, time.Hour, time.Minute, time.Second);
                                                    writer.Write(line);
                                                    line = String.Format(", {0}, {1}, {2}, {3}", irx, ispec, ipt, nsamp);
                                                    writer.Write(line);
                                                    for (int isamp = 0; isamp < nsamp; isamp++) {
                                                        writer.Write(", {0:F2}", _dataPackage.SampledTimeSeries[irx][ispec][ipt][isamp]);
                                                    }
                                                    writer.WriteLine();
                                                }
                                            }
                                        }
                                        
                                    }
                                }
                            }
                            catch (Exception e) {
                            }
                        }
                        if ((_parameters.SystemPar.RadarPar.ProcPar.PopFiles[iFile].WriteSingleTSTextFile) ||
                                 (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[iFile].WriteFullTSTextFile)) {
                            textExtension = ".ts.txt";
                            textFile = Path.Combine(folder, baseName + textExtension);
                            try {
                                using (StreamWriter writer = new StreamWriter(textFile, appendMode)) {
                                    if (_dataPackage.TransformedTimeSeries != null) {
                                        DateTime time = _dataPackage.RecordTimeStamp;
                                        string line;
                                        //int nrx = _dataPackage.TransformedTimeSeries.Length;
                                        int nrx = _dataPackage.Parameters.SystemPar.RadarPar.ProcPar.NumberOfRx;
                                        //int nspec = _dataPackage.TransformedTimeSeries[0].Length;
                                        int nspec = _dataPackage.Parameters.SystemPar.RadarPar.BeamParSet[0].NSpec;
                                        if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[iFile].WriteSingleTSTextFile) {
                                            nspec = 1;
                                        }
                                        //int nhts = _dataPackage.TransformedTimeSeries[0][0].Length;
                                        int nhts = _dataPackage.Parameters.SystemPar.RadarPar.BeamParSet[0].NHts;
                                        //int npts = _dataPackage.TransformedTimeSeries[0][0][0].Length;
                                        int npts = _dataPackage.Parameters.SystemPar.RadarPar.BeamParSet[0].NPts;
                                        for (int irx = 0; irx < nrx; irx++) {
                                            for (int ispec = 0; ispec < nspec; ispec++) {
                                                for (int iht = 0; iht < nhts; iht++) {
                                                    line = String.Format("{0}, {1}, {2}, {3}, {4}", time.Year, time.DayOfYear, time.Hour, time.Minute, time.Second);
                                                    writer.Write(line);
                                                    line = String.Format(", {0}, {1}, {2}, {3}", irx, ispec, iht, npts);
                                                    writer.Write(line);
                                                    for (int ipt = 0; ipt < npts; ipt++) {
                                                        writer.Write(", {0:F2}", _dataPackage.TransformedTimeSeries[irx][ispec][ipt][ipt].re);
                                                    }
                                                    writer.WriteLine();
                                                }
                                            }
                                        }

                                    }
                                }
                            }
                            catch (Exception e) {
                            }
                        }
                        else {
                            // not possible
                        }


                    }

                    if (writingBinary) {
                        
					    ulong totalBytes, totalFreeBytes;
					    ulong minFreeBytes = (ulong)2.0e6;
					    DacDiskInfo.GetDiskFreeSpace(fullPath, out totalBytes, out totalFreeBytes);
					    if (totalFreeBytes < minFreeBytes) {
						    Console.Beep(100,100);
						    Thread.Sleep(100);
						    Console.Beep(100,100);
						    Thread.Sleep(100);
						    Console.Beep(50, 100);
                            ErrorMessage = "Not enough space on disk.";
					    }
					    else {
                            WriteBinaryFile(iFile, fullPath, headerPath);
                        }  // end of else enough disk space to write binary file

                    }  // end if writingBinary

				}  // end if fileWriteEnabled

			}  // end for iFile

            return true;

		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iFile"></param>
        /// <param name="fullPath"></param>
        /// <param name="headerPath"></param>
        private void WriteBinaryFile(int iFile, string fullPath, string headerPath) {
            bool repeat = false;
            int retries = 3;
            do {
                try {
                    _dWriter = null;
                    _hWriter = null;
                    if (_fileParams[iFile].WriteModeOverwrite) {
                        _dWriter = new BinaryWriter(File.Open(fullPath, FileMode.Create, FileAccess.Write, FileShare.Read));
                        _hWriter = new BinaryWriter(File.Open(headerPath, FileMode.Create, FileAccess.Write, FileShare.Read));
                    }
                    else {
                        _dWriter = new BinaryWriter(File.Open(fullPath, FileMode.Append, FileAccess.Write, FileShare.Read));
                        _hWriter = new BinaryWriter(File.Open(headerPath, FileMode.Append, FileAccess.Write, FileShare.Read));
                    }
                }
                catch (Exception e) {
                    // file open error
                    // IOException if file in use
                    // DirectoryNotFoundException if invalid path
                    if ((e is IOException) && retries > 0) {
                        retries--;
                        repeat = true;
                        DacLogger.WriteEntry("PopWriteRecord Error Retry", _logFolder);
                        Thread.Sleep(1000);
                        continue;
                    }
                    repeat = false;
                    //_speaker.SpeakAsync("File open error. " + Path.GetFileName(fullPath));
                    string message = "File open error. " + Path.GetFileName(fullPath) + "\n" + e.Message;
                    //MessageBoxEx.Show(message, "PopFileWriter Error", 5000);
                    DacLogger.WriteEntry("WritePopRecord Exception: " + e.Message, _logFolder);
                    ErrorMessage = e.Message;
                    return ;
                }

            } while (repeat);

            long dataRecordPos = _dWriter.BaseStream.Position;
            long currentHeaderPos = _hWriter.BaseStream.Position;

            if (_parameters.SystemPar.RadarPar.BeamParSet[0].NSpec == 15) {
                int x = 0;
            }
            if (_prevParameters[0] != null) {
                if (_prevParameters[0].SystemPar.RadarPar.BeamParSet[0].NSpec == 15) {
                    int x = 0;
                }
            }
            if ((_prevParameters[iFile] == null) ||
                (_prevParameters[iFile] != _parameters) ||
                (_prevHeaderPos[iFile] < 0) ||
                (currentHeaderPos == 0)) {

                    if (_prevParameters[iFile] == _parameters) {
                        int x = 0;
                    }

                // write a new header
                _prevHeaderPos[iFile] = currentHeaderPos;
                // _prevHeaderPos points to end of last header record
                // Append a new header record:
                WriteHeaderRecord(iFile, dataRecordPos);
                // now _prevHeaderPos points to beginning of last header record
                _prevParameters[iFile] = _parameters.DeepCopy();
                //StatusMessage += "Writing new Header.  ";
            }
            else {
                // no need to write new header
                // _prevHeaderPos still points to beginning of last header record
                //StatusMessage += "Not writing new Header.  ";
            }
            WriteDataRecord(iFile, _prevHeaderPos[iFile]);

            if (_parameters.SystemPar.RadarPar.BeamParSet[0].NSpec == 15) {
                int x = 0;
            }
            if (_prevParameters[0] != null) {
                if (_prevParameters[0].SystemPar.RadarPar.BeamParSet[0].NSpec == 15) {
                    int x = 0;
                }
            }
            _dWriter.Close();
            _hWriter.Close();
        }

		///////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		/// <param name="headerRecordPos"></param>
		private void WriteDataRecord(int indx, long headerRecordPos) {

			bool hasRass = (_parameters.SystemPar.RadarPar.ProcPar.RassSourceParams[0] != 0) ? true : false;
			if (hasRass) {
                // POPREV as of 4.6, allowing writing RASS file
				//throw new NotImplementedException("RASS not supported in PopFileWriter.");
			}
			bool hasSpec = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[indx].IncludeSpectra;
            bool hasXCorr = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[indx].IncludeXCorr;
            bool hasACorr = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[indx].IncludeACorr;
            bool hasMom = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[indx].IncludeMoments;
			bool hasFullTS = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[indx].IncludeFullTS;
			bool hasShortTS = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[indx].IncludeSingleTS;
			bool hasRawTS = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[indx].WriteRawTSFile;
			if (hasRawTS) {
				// Raw time series is special format, no other data allowed
                hasXCorr = hasSpec = hasMom = hasFullTS = hasShortTS = false;
            }
			int recordType = 0;
			int dataType = -1;

			//
			// dataType as defined by Vaisala for LapXM (POP extensions)
			//	0 = moments only
			//	1 = spectra and moments
			//	2 = spectra only
			//	3 = full time series only
			//	4 = short time series (first or last?) and moments
			//	5 = short time series, spectra, and moments
			//	6 = full time series and moments
			//	7 = full time series and spectra
			//	8 = full time series, spectra, and moments
			//
			//	for combinations not included above,
			//		i.e. short time series alone and with only spectra:
			//	Use alternate bit-wise-and notation added to 1000
			//	1 = spectra
			//	2 = moments
			//	4 = short time series
			//	8 = full time series
			//
			//  New times series type for FMCW: RawTimeSeriesOnly (WriteRawTSFile = true)
			//		Written as *.raw.ts file (type = 3)
			//		No moments or spectra allowed.
			//		Header file is faked so that nhts and npts are adjusted.
			//
            bool hasSortOfSpec = hasSpec || hasXCorr || hasACorr;
            if (hasXCorr || hasACorr) {
                hasFullTS = false;
                hasShortTS = false;
                hasRawTS = false;
            }
            AssignTypes(hasRass, hasSortOfSpec, hasMom, hasFullTS, hasShortTS, hasRawTS, ref recordType, ref dataType);

			if ((recordType < 3000) || (dataType < 0)) {
				throw new ApplicationException("RecordType - DataType error in PopFileWriter");
			}

            int nMetInst = _parameters.SystemPar.RadarPar.NumOtherInstruments;
            PopParameters.BeamParameters curBmPar = _parameters.SystemPar.RadarPar.BeamParSet[CurrentParIndices.ParI];
			int nHts = curBmPar.NHts;
			int iHtFirst = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateFirst;
			int iHtLast = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateLast;
            // TODO: for nonreplay mode nhts is only the saved hts
            /*
            if (iHtFirst >= nHts) {
                iHtFirst = 0;
            }
            if (iHtLast >= nHts) {
                iHtLast = nHts - 1;
            }
             * */
            int gatesToWrite = iHtLast - iHtFirst + 1;
            if (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.PulsedTx) {
                gatesToWrite = _parameters.SystemPar.RadarPar.BeamParSet[0].NHts;
            }
			int nPts;
            int nPtsInArray;
            if (hasXCorr || hasACorr) {
                nPts = 2 * _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrMaxLag + 1;
            }
            else {
                nPts = curBmPar.NPts;
            }
            if (_parameters.SystemPar.RadarPar.ProcPar.RassSourceParams[0] != 0) {
                // RASS record
                nPtsInArray = _parameters.SystemPar.RadarPar.ProcPar.Dop1 + _parameters.SystemPar.RadarPar.ProcPar.Dop3;
            }
            else {
                // not truly RASS, but check for truncated spectra
                if (_parameters.SystemPar.RadarPar.ProcPar.Dop1 + _parameters.SystemPar.RadarPar.ProcPar.Dop3 < nPts) {
                    nPtsInArray = _parameters.SystemPar.RadarPar.ProcPar.Dop1 + _parameters.SystemPar.RadarPar.ProcPar.Dop3;
                }
                else {
                    nPtsInArray = nPts;
                }
            }
			int nSpec = curBmPar.NSpec;
            int nRx = _parameters.SystemPar.RadarPar.ProcPar.NumberOfRx;

			int nBytes = 28;
			if (hasRass) {
                // POPREV 4.6 allows rass
				//throw new ApplicationException("RASS not supported computing nBytes.");
			}
			if (nMetInst != 0) {
				//throw new ApplicationException("Met instruments not supported computing nBytes.");
			}
			nBytes += nMetInst * 4;
			if (hasMom) {
                nBytes += 8 * nRx * gatesToWrite;
                if (hasRass) {
                    nBytes += 8 * nRx * gatesToWrite;
                }
			}
			if (hasSortOfSpec) {
                nBytes += 4 * nRx * gatesToWrite * nPtsInArray;
			}
			if (hasRawTS) {
				// for raw TS, raw nPts replaces nHts as array dimension
				nBytes += 8 * nRx * nPts * nSpec * _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts;
			}
			else if (hasFullTS) {
                nBytes += 8 * nRx * nPts * nHts * nSpec;
			}
			else if (hasShortTS) {
				// if has full, cannot have short
                nBytes += 8 * nRx * gatesToWrite * nPts;
			}

			_dWriter.Write((Int16)recordType);
			_dWriter.Write((Int32)nBytes);
			_dWriter.Write((Int16)dataType);
			_dWriter.Write((Int32)headerRecordPos);

			// write time stamp
			DateTime baseTime = new DateTime(1970, 1, 1);
			TimeSpan cTime = _dataPackage.RecordTimeStamp - baseTime;
			_dWriter.Write((Int32)cTime.TotalSeconds);

			_dWriter.Write((Int16)0);	// radar index always 0
			_dWriter.Write((Int16)CurrentParIndices.BmSeqI);	// current beam in sequence
			_dWriter.Write((Int16)curBmPar.NCI);
			_dWriter.Write((Int16)curBmPar.NSpec);

			// Write data arrays now:
			// moments
			//double noise;
            int nSpecPts = curBmPar.NPts;
			if (hasMom) {
                for (int irx = 0; irx < nRx; irx++) {
                    for (int i = 0; i < gatesToWrite; i++) {
                        int iht = iHtFirst + i;
                        //noise = _dataPackage.Noise[iht];
                        _dWriter.Write((Int16)(Math.Floor(_dataPackage.MeanDoppler[irx][iht] * 1.0e4 + 0.5)));
                        _dWriter.Write((Int16)(Math.Floor(_dataPackage.Width[irx][iht] * 1.0e4 + 0.5)));
                        _dWriter.Write((Int16)(Math.Floor(1000.0 * Math.Log10(_dataPackage.Power[irx][iht] / (_dataPackage.Noise[irx][iht] * nSpecPts)) + 0.5)));
                        _dWriter.Write((Int16)(Math.Floor(Math.Log10(_dataPackage.Noise[irx][iht]) * 1000.0 + 0.5)));
                        //vel = (short int) floor((Dopp[i]/nyquist) * 1e4 + 0.5);
                        //wwidth = (short int) floor((spWidth[i]/nyquist) * 1e4 + 0.5);
                        //wsnr = (short int) floor(Snr[i] * 100.0 + 0.5);
                        //wnois = (short int) floor(log10((double) Noise[i]) * 1000.0 + 0.5);
                    }
                }
			}
			if (hasRass) {
                // POPREV as of 4.6 RASS is allowed
                if (hasMom) {
                    for (int irx = 0; irx < nRx; irx++) {
                        for (int i = 0; i < gatesToWrite; i++) {
                            int iht = iHtFirst + i;
                            //noise = _dataPackage.Noise[iht];
                            _dWriter.Write((Int16)(Math.Floor(_dataPackage.RassMeanDoppler[irx][iht] * 1.0e4 + 0.5)));
                            _dWriter.Write((Int16)(Math.Floor(_dataPackage.RassWidth[irx][iht] * 1.0e4 + 0.5)));
                            _dWriter.Write((Int16)(Math.Floor(1000.0 * Math.Log10(_dataPackage.RassPower[irx][iht] / (_dataPackage.Noise[irx][iht] * nSpecPts)) + 0.5)));
     /*fix temp */          _dWriter.Write((Int16)(Math.Floor(Math.Log10(_dataPackage.Noise[irx][iht]) * 1000.0 + 0.5)));
                            //vel = (short int) floor((Dopp[i]/nyquist) * 1e4 + 0.5);
                            //wwidth = (short int) floor((spWidth[i]/nyquist) * 1e4 + 0.5);
                            //wsnr = (short int) floor(Snr[i] * 100.0 + 0.5);
                            //wnois = (short int) floor(log10((double) Noise[i]) * 1000.0 + 0.5);
                        }
                    }
                }
            }

            if (nMetInst > 0) {
                for (int i = 0; i < nMetInst; i++) {
                    if (_parameters.SystemPar.RadarPar.OtherInstrumentCodes[i] == 0x5453) {
                        float fracSeconds = (float)(_dataPackage.RecordTimeStamp.Millisecond / 1000.0);
                        _dWriter.Write(fracSeconds);
                    }
                    else {
                        _dWriter.Write((float)123.0);
                    }
                }
            }

            if (hasSpec && _dataPackage.Spectra != null && _dataPackage.Spectra[0] != null) {
                // TODO: fix this for replay moments file (revised if-test above helps)
                for (int irx = 0; irx < nRx; irx++) {
                    for (int i = 0; i < gatesToWrite; i++) {
                        int iht = iHtFirst + i;
                        for (int ipt = 0; ipt < nPtsInArray; ipt++) {
                            _dWriter.Write((float)_dataPackage.Spectra[irx][iht][ipt]);
                        }

                    }
                }
            }
            if (hasXCorr && _dataPackage.XCorrMag != null && _dataPackage.XCorrMag[0] != null) {
                for (int irx = 0; irx < nRx; irx++) {
                    for (int i = 0; i < gatesToWrite; i++) {
                        int iht = iHtFirst + i;
                        for (int ipt = 0; ipt < nPts; ipt++) {
                            _dWriter.Write((float)_dataPackage.XCorrMag[irx][iht][ipt]);
                        }

                    }
                }
            }
            if (nRx==3 && hasACorr && _dataPackage.XCorrMag != null && _dataPackage.XCorrMag[0] != null) {
                for (int irx = 0; irx < nRx; irx++) {
                    for (int i = 0; i < gatesToWrite; i++) {
                        int iht = iHtFirst + i;
                        for (int ipt = 0; ipt < nPts; ipt++) {
                            _dWriter.Write((float)_dataPackage.XCorrMag[irx+3][iht][ipt]);
                        }

                    }
                }
            }
            if (hasRawTS && _dataPackage.SampledTimeSeries != null) {
                // raw time series in *.ts file
				float re, im;
				int rawNpts = _dataPackage.Parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts;
                for (int irx = 0; irx < nRx; irx++) {
                    for (int iht = 0; iht < rawNpts; iht++) {
                        for (int ispec = 0; ispec < nSpec; ispec++) {
                            for (int ipt = 0; ipt < nPts; ipt++) {
                                re = (float)_dataPackage.SampledTimeSeries[irx][ispec][ipt][iht];
                                im = (float)0.0;
                                _dWriter.Write(re);
                                _dWriter.Write(im);
                            }
                        }
                    }
                }
			}
            //else if ((hasFullTS || hasShortTS) && (!hasSpec && !hasMom) && _dataPackage.TransformedTimeSeries != null) {
            //}
            else if (hasFullTS && _dataPackage.TransformedTimeSeries != null) {
                // Doppler time series in *.mom or *.spc file
                //throw new ApplicationException("Full Time Series not supported writing data to file.");
                float re, im;
                for (int irx = 0; irx < nRx; irx++) {
                    for (int i = 0; i < gatesToWrite; i++) {
                        int iht = iHtFirst + i;
                        for (int ispec = 0; ispec < nSpec; ispec++) {
                            for (int ipt = 0; ipt < nPts; ipt++) {
                                re = (float)_dataPackage.TransformedTimeSeries[irx][ispec][iht][ipt].re;
                                im = (float)_dataPackage.TransformedTimeSeries[irx][ispec][iht][ipt].im;
                                _dWriter.Write(re);
                                _dWriter.Write(im);
                            }
                        }
                    }
                }
            }
            else if (hasShortTS && _dataPackage.TransformedTimeSeries != null) {
                //throw new ApplicationException("Short Time Series not supported writing data to file.");
                float re, im;
                for (int irx = 0; irx < nRx; irx++) {
                    for (int i = 0; i < gatesToWrite; i++) {
                        int iht = iHtFirst + i;
                        for (int ipt = 0; ipt < nPts; ipt++) {
                            re = (float)_dataPackage.TransformedTimeSeries[irx][0][iht][ipt].re;
                            im = (float)_dataPackage.TransformedTimeSeries[irx][0][iht][ipt].im;
                            _dWriter.Write(re);
                            _dWriter.Write(im);
                        }
                    }
                }
            }

			_dWriter.Write((Int32)nBytes);
            long endPos = _dWriter.BaseStream.Position;
		}

		private static void AssignTypes(bool hasRass, bool hasSpec, bool hasMom, bool hasFullTS, bool hasShortTS, bool hasRawTS,
										ref int recordType, ref int dataType) {
			if (hasSpec) {
				if (hasMom) {
					if (hasRass) {
						if (hasFullTS) {
							recordType = 3117;
							dataType = 8;
						}
						else if (hasShortTS) {
							recordType = 3117;
							dataType = 5;
						}
						else {  // no TS
							recordType = 3117;
							dataType = 1;
						}
					}
					else {  // no RASS
						if (hasFullTS) {
							recordType = 3115;
							dataType = 8;
						}
						else if (hasShortTS) {
							recordType = 3115;
							dataType = 5;
						}
						else {  // no TS
							recordType = 3115;
							dataType = 1;
						}
					}
				}
				else {  // no moments
					if (hasRass) {
						if (hasFullTS) {
							recordType = 3119;
							dataType = 7;
						}
						else if (hasShortTS) {
							recordType = 3119;
							dataType = 1005;	// alternate notation
						}
						else {  // no TS
							recordType = 3119;
							dataType = 2;
						}
					}
					else {  // no RASS
						if (hasFullTS) {
							recordType = 3118;
							dataType = 7;
						}
						else if (hasShortTS) {
							recordType = 3118;
							dataType = 1005;	// alternate notation
						}
						else {  // no TS
							recordType = 3118;
                            dataType = 2;
						}
					}
				}
			}
			else {  // no spectra
				if (hasMom) {
					if (hasRass) {
						if (hasFullTS) {
							recordType = 3116;
							dataType = 6;
						}
						else if (hasShortTS) {
							recordType = 3116;
							dataType = 4;
						}
						else {  // no TS
							recordType = 3116;
							dataType = 0;
						}
					}
					else {  // no RASS
						if (hasFullTS) {
							recordType = 3115;
							dataType = 6;
						}
						else if (hasShortTS) {
							recordType = 3115;
							dataType = 4;
						}
						else {  // no TS
							recordType = 3115;
							dataType = 0;
						}
					}
				}
				else {  // no moments
					if (hasRass) {
						if (hasFullTS || hasRawTS) {
							recordType = 3121;
							dataType = 3;
						}
						else if (hasShortTS) {
							recordType = 3121;
							dataType = 1004;	// alternate notation
						}
						else {  // no TS
							// no data to write
						}
					}
					else {  // no RASS
						if (hasFullTS || hasRawTS) {
							recordType = 3120;
							dataType = 3;
						}
						else if (hasShortTS) {
							recordType = 3120;
							dataType = 1004;	// alternate notation
						}
						else {  // no TS
							// no data to write
						}
					}
				}
			}
		}

		///////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		private void WriteHeaderRecord(int indx, long dataPos) {

            int numInst = _parameters.SystemPar.RadarPar.NumOtherInstruments;   
            Int16[] instTypes = null;
            if (numInst > 0) {
                instTypes = new Int16[numInst];
                instTypes[0] = 0x5453;  // code for extended timestamp in fractional seconds ("TS")
            }

			_hWriter.Write((Int16)103);		// version #
            int hdrBytes = 580 + 2 * numInst;
            _hWriter.Write((Int16)hdrBytes);		// bytes in header record (no met instruments)
            _hWriter.Write((Int16)numInst);		    // # met instruments
			
			_hWriter.Write((Int16)MAXRADAR);		// write 5 array dimensions of file
			_hWriter.Write((Int16)MAXBMPAR);
			_hWriter.Write((Int16)MAXBM);
			_hWriter.Write((Int16)MAXDIR);
			_hWriter.Write((Int16)MAXBW);

			// write system parameter structure
			//
			// write 30-char station name plus null
			//string station = _parameters.SystemPar.StationName;
			char[] station = FixStringSize(_parameters.SystemPar.StationName, 30);
			_hWriter.Write(station);
			_hWriter.Write((char)0);	// null for C-style strings
			_hWriter.Write((char)0);	// null for word alignment

			_hWriter.Write((Int16)(_parameters.SystemPar.Latitude * 100.0 + 0.5));
			_hWriter.Write((Int16)(_parameters.SystemPar.Longitude * 100.0 + 0.5));
			_hWriter.Write((Int16)(_parameters.SystemPar.MinutesToUT));
			_hWriter.Write((Int16)(_parameters.SystemPar.Altitude));
			_hWriter.Write((Int16)1);  // number of radars

			// write radar structure
			//
			char[] radarName = FixStringSize(_parameters.SystemPar.RadarPar.RadarName, 30);
			_hWriter.Write(radarName);
			_hWriter.Write((char)0);
			_hWriter.Write((char)0);
			_hWriter.Write((Int16)_parameters.SystemPar.RadarPar.RadarID);
			Int32 freq = (Int32)(_parameters.SystemPar.RadarPar.TxFreqMHz * 100.0 + 0.5);
			_hWriter.Write(freq);
			_hWriter.Write((float)_parameters.SystemPar.RadarPar.MaxTxDutyCycle);
			_hWriter.Write((Int16)_parameters.SystemPar.RadarPar.MaxTxLengthUsec);
			int txon = _parameters.SystemPar.RadarPar.TxIsOn ? 1 : 0;
			_hWriter.Write((Int16)txon);

			// find number of used beam directions
			//	(find first direction with no label)
			int numDir = 0;
			string label;
			for (int i = 0; i < _parameters.SystemPar.RadarPar.BeamDirections.Length; i++) {
				label = _parameters.SystemPar.RadarPar.BeamDirections[i].Label.Trim();
				if ((label != String.Empty) && (!label.StartsWith("_"))) {
					numDir++;
				}
				else {
					break;
				}
			}
			_hWriter.Write((Int16)numDir);

			// find number of beam positions in sequence
			//	(find first position with nreps==0)
			int numBms = 0;
			for (int i = 0; i < _parameters.SystemPar.RadarPar.BeamSequence.Length; i++) {
				if (_parameters.SystemPar.RadarPar.BeamSequence[i].NumberOfReps > 0) {
					numBms++;
				}
				else {
					break;
				}
			}
			_hWriter.Write((Int16)numBms);

			// number of beam parameter sets
			_hWriter.Write((Int16)_parameters.SystemPar.RadarPar.BeamParSet.Length);

			// write beam parameters
			//
            PopParameters.PopFileParameters pp = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[indx];
            bool writeShortTS = pp.IncludeSingleTS;
            bool writeSpectra = pp.IncludeSpectra;
            bool writeXCorr = pp.IncludeXCorr;
            bool writeACorr = pp.IncludeACorr;
            bool writeMoments = pp.IncludeMoments;
			bool writeRawTS = pp.WriteRawTSFile;

            int iHtFirst, iHtLast;

			for (int i = 0; i < MAXBMPAR; i++) {
				if (_parameters.SystemPar.RadarPar.BeamParSet.Length > i) {
					int nhts;
                    int nspec;
                    if (writeRawTS) {
                        // when writing raw TS, raw nPts replaces nHts
                        nhts = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts;
                        iHtFirst = 0;
                        iHtLast = nhts - 1;
                    }
                    else {
                        nhts = _parameters.SystemPar.RadarPar.BeamParSet[i].NHts;
                        iHtFirst = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateFirst;
                        iHtLast = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateLast;
                        // TODO in nonreplay nhts is the actual hts to save to disk
                        if (_parameters.ReplayPar.Enabled) {
                            iHtFirst = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateFirst;
                            if (iHtFirst >= nhts) {
                                iHtFirst = 0;
                            }
                            iHtLast = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateLast;
                            if (iHtLast >= nhts) {
                                iHtLast = nhts - 1;
                            }
                            int gatesToWrite = iHtLast - iHtFirst + 1;
                            nhts = gatesToWrite;
                        }
                        
                    }
                    if (writeShortTS && !writeMoments && !writeSpectra) {
                        // when writing short timeseries only, ts file, fake nspec for display programs
                        nspec = 1;
                    }
                    else {
                        nspec = _parameters.SystemPar.RadarPar.BeamParSet[i].NSpec;
                    }
                    int numPts;
                    if (writeACorr || writeXCorr) {
                        numPts = 2 * _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrMaxLag + 1;
                    }
                    else {
                        numPts = _parameters.SystemPar.RadarPar.BeamParSet[i].NPts;
                    }
                    _hWriter.Write((Int32)(_parameters.SystemPar.RadarPar.BeamParSet[i].IppMicroSec * 1000));
					_hWriter.Write((Int32)_parameters.SystemPar.RadarPar.BeamParSet[i].PulseWidthNs);
                    int delay0 = _parameters.SystemPar.RadarPar.BeamParSet[i].SampleDelayNs;
                    int spacing = _parameters.SystemPar.RadarPar.BeamParSet[i].SpacingNs;
                    // on replay need to adjust delay
                    if (_parameters.ReplayPar.Enabled) {
                        _hWriter.Write((Int32)delay0 + iHtFirst * spacing);
                    }
                    else {
                        _hWriter.Write((Int32)delay0);
                    }
                    _hWriter.Write((Int32)_parameters.SystemPar.RadarPar.BeamParSet[i].SpacingNs);
					_hWriter.Write((Int16)nhts);
					_hWriter.Write((Int16)_parameters.SystemPar.RadarPar.BeamParSet[i].NCI);
					_hWriter.Write((Int16)nspec);
					_hWriter.Write((Int16)numPts);
					_hWriter.Write((Int16)_parameters.SystemPar.RadarPar.BeamParSet[i].SystemDelayNs);	// TODO: SysDelay
					_hWriter.Write((Int16)_parameters.SystemPar.RadarPar.BeamParSet[i].BwCode);
					_hWriter.Write((Int16)_parameters.SystemPar.RadarPar.BeamParSet[i].AttenuatedGates);
					_hWriter.Write((Int16)_parameters.SystemPar.RadarPar.BeamParSet[i].NCode);
				}
				else {
					_hWriter.Write((Int32)0);
					_hWriter.Write((Int32)0);
					_hWriter.Write((Int32)0);
					_hWriter.Write((Int32)0);
					_hWriter.Write((Int16)0);
					_hWriter.Write((Int16)0);
					_hWriter.Write((Int16)0);
					_hWriter.Write((Int16)0);
					_hWriter.Write((Int16)0);
					_hWriter.Write((Int16)0);
					_hWriter.Write((Int16)0);
					_hWriter.Write((Int16)0);
				}
			}
			
			// write beam sequence
			//
			for (int i = 0; i < MAXBM; i++) {
				if (_parameters.SystemPar.RadarPar.BeamSequence.Length > i) {
					_hWriter.Write((Int16)_parameters.SystemPar.RadarPar.BeamSequence[i].DirectionIndex);
					_hWriter.Write((Int16)_parameters.SystemPar.RadarPar.BeamSequence[i].ParameterIndex);
					_hWriter.Write((Int16)_parameters.SystemPar.RadarPar.BeamSequence[i].NumberOfReps);
				}
				else {
					_hWriter.Write((Int16)0);
					_hWriter.Write((Int16)0);
					_hWriter.Write((Int16)0);
				}
			}

			// write pulse box constants
			//
			_hWriter.Write((Int32)_parameters.SystemPar.RadarPar.PBConstants.PBPreTR);
			_hWriter.Write((Int32)_parameters.SystemPar.RadarPar.PBConstants.PBPostTR);
			_hWriter.Write((Int32)_parameters.SystemPar.RadarPar.PBConstants.PBSynch);
			_hWriter.Write((Int32)_parameters.SystemPar.RadarPar.PBConstants.PBPreBlank);
			_hWriter.Write((Int32)_parameters.SystemPar.RadarPar.PBConstants.PBPostBlank);

			// write direction
			//
			char[] directionLabel;
			for (int i = 0; i < MAXDIR; i++) {
				if (_parameters.SystemPar.RadarPar.BeamDirections.Length > i) {
					directionLabel = FixStringSize(_parameters.SystemPar.RadarPar.BeamDirections[i].Label, 10);
					_hWriter.Write(directionLabel);
					_hWriter.Write((char)0);
					_hWriter.Write((char)0);
					_hWriter.Write((Int16)(_parameters.SystemPar.RadarPar.BeamDirections[i].Azimuth + 0.5));
					_hWriter.Write((Int16)(_parameters.SystemPar.RadarPar.BeamDirections[i].Elevation*100.0 + 0.5));
					_hWriter.Write((Int16)(_parameters.SystemPar.RadarPar.BeamDirections[i].SwitchCode));
				}
				else {
					char[] nullLabel = FixStringSize("xxx", 12);
					_hWriter.Write(nullLabel);
					_hWriter.Write((Int16)0);
					_hWriter.Write((Int16)0);
					_hWriter.Write((Int16)0);
				}
			}

			// write receiver bandwidth choices
			//
			for (int i = 0; i < MAXBW; i++) {
				if (_parameters.SystemPar.RadarPar.RxBw.Length > i) {
					_hWriter.Write((Int16)_parameters.SystemPar.RadarPar.RxBw[i].BwPwNs);
				}
				else {
					_hWriter.Write((Int16)0);
				}
			}
			for (int i = 0; i < MAXBW; i++) {
				if (_parameters.SystemPar.RadarPar.RxBw.Length > i) {
					_hWriter.Write((Int16)_parameters.SystemPar.RadarPar.RxBw[i].BwDelayNs);
				}
				else {
					_hWriter.Write((Int16)0);
				}
			}

			// write processing parameters
			//
            int windowFlag;  // LAPXM-defined window flags
            PopParameters.WindowType windowType = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerWindow;
            if (!_parameters.SystemPar.RadarPar.ProcPar.IsWindowing) {
                windowFlag = 0;
            }
            else {
                if (windowType == PopParameters.WindowType.Rectangular) {
                    windowFlag = 0;
                }
                else if (windowType == PopParameters.WindowType.Hanning) {
                    windowFlag = 1;
                }
                else if (windowType == PopParameters.WindowType.Hamming) {
                    windowFlag = 6;
                }
                else if (windowType == PopParameters.WindowType.Blackman) {
                    windowFlag = 5;
                }
                else if (windowType == PopParameters.WindowType.Riesz) {
                    // no defined file flag for Riesz
                    windowFlag = 1;
                }
                else {
                    windowFlag = 1;
                }
            }

			_hWriter.Write((Int16)(_parameters.SystemPar.RadarPar.ProcPar.IsDcFiltering ? 1 : 0));
			_hWriter.Write((Int16)windowFlag);
			_hWriter.Write((Int16)0);	// pts omitted at DC (obsolete)
			_hWriter.Write((Int16)0);	// hts pts omitted
            if (writeXCorr || writeACorr) {                        
                int numPts = 2 * _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrMaxLag + 1;
                _hWriter.Write((Int16)1);  // pt# (1-Npts) to start winds spectral interval
                _hWriter.Write((Int16)numPts);	 // number of points in winds interval
                _hWriter.Write((Int16)1);  // pt# (1-Npts) to start rass spectral interval
                _hWriter.Write((Int16)0);	 // number of points in winds interval
            }
            else {
                _hWriter.Write((Int16)_parameters.SystemPar.RadarPar.ProcPar.Dop0);  // pt# (1-Npts) to start winds spectral interval
                _hWriter.Write((Int16)_parameters.SystemPar.RadarPar.ProcPar.Dop1);	 // number of points in winds interval
                _hWriter.Write((Int16)_parameters.SystemPar.RadarPar.ProcPar.Dop2);  // pt# (1-Npts) to start rass spectral interval
                _hWriter.Write((Int16)_parameters.SystemPar.RadarPar.ProcPar.Dop3);	 // number of points in winds interval
            }
			// then RASS params
			for (int i = 0; i < 6; i++) {
				_hWriter.Write((Int16)_parameters.SystemPar.RadarPar.ProcPar.RassSourceParams[i]);
			}
			_hWriter.Write((Int16)(_parameters.SystemPar.RadarPar.ProcPar.MaxClutterHtKm * 10.0 + 0.5));
			_hWriter.Write((Int16)(_parameters.SystemPar.RadarPar.ProcPar.IsIcraAvg ? 1 : 0));
			_hWriter.Write((Int16)_parameters.SystemPar.RadarPar.ProcPar.NumberOfRx);
			_hWriter.Write((Int16)0);			// number of met instruments

            // POPREV 3.19 added LapXM extended parameters to header file, including rx multiplexed/parallel
            // room for future expansion, keep proc par section = 80 bytes
            // This section is 44 bytes, includes parameters added by Lapxm
            //
            bool isFlip = _parameters.SystemPar.RadarPar.BeamParSet[0].TxPhaseFlipIsOn;
            int iFlip = isFlip ? 1 : 0;
            int sampleClockMz_X100 = (int)(1.0 / (_parameters.SystemPar.RadarPar.PBConstants.PBClock * 1.0e-5));
            //
            _hWriter.Write((Int16)0);       // iOverlap, set when overlapping Doppler times series
            _hWriter.Write((Int16)0);       // iConcatenatedTimeSeries; time series has been concatenated
            _hWriter.Write((Int16)0);       // iVertCorrectHw; hardware vertical range correction used
            _hWriter.Write((Int16)0);       // iVertCorrectHWAngleDeg_X100; angle used to correct vertical in hardware
            _hWriter.Write((Int16)(-1));    // iSampleClockMHz_X100; Clock rate used to generate range sample timing
                                            //      Note: for fmcw, do not use actual sample clock, because gate spacing
                                            //      is not determined by the sample spacing and Lapxm Console will use incorrect spacing.
            _hWriter.Write((Int16)iFlip);   // iFlip; set if flip of tx phase used
            _hWriter.Write((Int16)4);       // iReceiverMode; has flag 4 (3rd lsb) set if parallel sampling of receivers;
                                            //      flag 4 cleared if multiplexed (when word = -1, rx's were multiplexed).
                                            //      Other modes have been defined by Lapxm.
            int NMISC = 15;						
			for (int i = 0; i < NMISC; i++) {
				_hWriter.Write((Int16)(-1));				
			}
            //
            /////////////////////////////////////////////////////////////

			_hWriter.Write((Int32)dataPos);		// start byte in data file for records using this header record

            // write instrument types
            if (numInst > 0) {
                for (int i = 0; i < numInst; i++) {
                    _hWriter.Write(instTypes[i]);
                }
            }

		}  // end WriteHeaderRecord()

		/// <summary>
		/// Makes the given string contain exactly numChars 
		///		number of characters either by 
		///		truncating or padding with nulls.
		/// </summary>
		/// <param name="station"></param>
		/// <param name="numChars"></param>
		/// <returns></returns>
		private char[] FixStringSize(string theString, int numChars) {
			if (theString.Length > numChars) {
				theString = theString.Substring(0, numChars);
			}
			else {
				theString = theString.PadRight(numChars, (char)0);
			}
			return theString.ToCharArray();
		}
		
		///////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		/// <param name="indx"></param>
		/// <returns></returns>
		public string GetFilePath(int indx) {

			string fullName = null;

			DateTime dt = _dataPackage.RecordTimeStamp;
			string year = String.Format("{0:D2}", dt.Year % 100);
			string day = String.Format("{0:D3}", dt.DayOfYear);
			string hour = String.Format("{0:D2}", dt.Hour);

			if (indx >= _fileParams.Length) {
				return fullName;
			}
				
			string ext;
			if (_fileParams[indx].WriteRawTSFile) {
				ext = ".raw.ts";
			}
            else if (_fileParams[indx].IncludeSpectra) {
                ext = ".spc";
            }
            else if (_fileParams[indx].IncludeXCorr) {
                ext = ".spc";
            }
            else if (_fileParams[indx].IncludeACorr) {
                ext = ".spc";
            }
            else if (_fileParams[indx].IncludeMoments) {
				ext = ".mom";
			}
			else if (_fileParams[indx].IncludeFullTS || _fileParams[indx].IncludeSingleTS) {
				ext = ".ts";
			}
            else {
				ext = "";
			}

			string suffix = _fileParams[indx].FileNameSuffix;
			if (suffix == null) {
				suffix = "a";
			}
			else {
				if (suffix.Length > 1) {
					suffix = suffix.Substring(0, 1);
				}
				if (suffix == String.Empty) {
					suffix = "a";
				}
                if (_fileParams[indx].IncludeACorr) {
                    suffix = "ac" + suffix;
                }
                else if (_fileParams[indx].IncludeXCorr) {
                    suffix = "xc" + suffix;
                }
            }
			//_fileParams[indx].FileNameSuffix = suffix;

			string site = _fileParams[indx].FileNameSite;
			if (site == null) {
				site = "xxx";
				//FillOutputPage();
			}
			else {
				if (site.Length > 3) {
					site = site.Substring(0, 3);
				}
				if (site.Length < 3) {
					site = "xxx";
					//FillOutputPage();
				}
			}
			_fileParams[indx].FileNameSite = site;

			bool lapxm = _fileParams[indx].UseLapxmFileName;
			String fileName;
			if (lapxm) {
				fileName = "D" + site + year + day;
				if (_fileParams[indx].WriteHourlyFiles) {
					fileName += hour;
				}
			}
			else {
				fileName = "D" + year + day;
				if (_fileParams[indx].WriteHourlyFiles) {
					char letter = 'c';
					int code = letter + dt.Hour;
					letter = (char)code;
					suffix = letter.ToString();
				}
			}
			fileName += suffix + ext;
			fullName = Path.Combine(_fileParams[indx].FileFolder, fileName);

			return fullName;

		}  // end GetFilePath()

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dataFile"></param>
		/// <returns></returns>
		public string GetHeaderFilePath(string dataFile) {
			string headerPath = null;
			string fileName = Path.GetFileName(dataFile);
			string folder = Path.GetDirectoryName(dataFile);
			fileName = fileName.Remove(0, 1);  // remove first letter
			fileName = fileName.Insert(0, "H");
			headerPath = Path.Combine(folder, fileName);
			return headerPath;

		}  // end GetHeaderFilePath()

	}
}
