using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Windows.Forms;
using DACarter.Utilities;
//using LibUsbDotNet;
//using LibUsbDotNet.Main;

namespace DACarter.NOAA.Hardware {

	class AD9959ConfigFile {

		private AD9959EvalBd _evalBd;
		private string _filePath;
		private char[] comma;
		private char[] quotes;

		private TextFile _writer;

		public AD9959EvalBd EvalBoard {
			get { return _evalBd;}
			set { _evalBd = value;}
		}

		public AD9959ConfigFile ()	{
			comma = new char[] {','};
			quotes = new char[] { '\"' };
		}

		public AD9959ConfigFile (AD9959EvalBd board) : this()	{
			_evalBd = board;
		}

		///////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Saves the current setup to a file
		/// </summary>
		/// <param name="FilePath">Full path to the file to save the current settings to</param>
		/// <param name="DumpRegs">
		/// DumpRegs - If true then it reads and saves the actual register data from the DDS
	    /// If false then it saves the current software buffer values for the registers
		/// </param>
		public void SaveSetup(string FilePath, bool bDumpRegs) {

			_filePath = FilePath;
			string[] RegNames = new string[25];
			int StartCnt;
			int EndCnt;

			if (_writer != null) {
				_writer.Close();
				_writer = null;
			}
			_writer = new TextFile(FilePath, true, false);
			
			//'Initialize the register names
			RegNames[0] = "CSR";
			RegNames[1] = "FR1";
			RegNames[2] = "FR2";
			RegNames[3] = "CFR";
			RegNames[4] = "CTW0";
			RegNames[5] = "CPOW";
			RegNames[6] = "ACR";
			RegNames[7] = "LSR";
			RegNames[8] = "RDW";
			RegNames[9] = "FDW";
			RegNames[10] = "CTW1";
			RegNames[11] = "CTW2";
			RegNames[12] = "CTW3";
			RegNames[13] = "CTW4";
			RegNames[14] = "CTW5";
			RegNames[15] = "CTW6";
			RegNames[16] = "CTW7";
			RegNames[17] = "CTW8";
			RegNames[18] = "CTW9";
			RegNames[19] = "CTW10";
			RegNames[20] = "CTW11";
			RegNames[21] = "CTW12";
			RegNames[22] = "CTW13";
			RegNames[23] = "CTW14";
			RegNames[24] = "CTW15";
			
			
			//'Show hourglass mouse pointer until finished
			System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
			
			// Write the Header
			WriteSettingValue("AD9958/59 Eval Software Settings File", 1, false); // first line does not append to file
			WriteSettingValue("Product:", _evalBd.ChipID);
			WriteHeaderLine("");
			
			//'Store any non register related info
			//'Store the external frequency
			WriteSettingValue( "External Frequency(MHz)", (int)_evalBd.RefClockMHz);
			WriteSettingValue( "AutoIOUpdate", _evalBd.AutoIOUpdate);
			
			//'Write a blank line
			WriteHeaderLine("");
			//'Store the register map
			WriteHeaderLine("Register Map Values (Binary)");
			
			//'Readback and write to file the Chip-Level Control registers
			for (int cntr = 0; cntr < 3; cntr++) {
				if (bDumpRegs) {
					//'If dumping the regval then read directly from the DDS
					WriteSettingValue( RegNames[cntr], _evalBd.USBSerialRead(cntr));
				} else {
					//'Otherwize read from the registermap software buffer
					WriteSettingValue( RegNames[cntr], _evalBd.GetRegMapValue(AD9959EvalBd.evb9959_RegMaps.rm9959_CurRegMapVals, cntr));
				}
			}
			
			
			StartCnt = 0;
			EndCnt = 3;
			string chnlHeader;

			//'Loop through each channel
			for (int ChnlCntr = StartCnt; ChnlCntr <= EndCnt; ChnlCntr++) {
				//'Write a Blank Line
				WriteHeaderLine("");
				//'Write a header first
				chnlHeader = "CH" + ChnlCntr.ToString() + " Register Values";
				WriteHeaderLine(chnlHeader);
				//'Loop through and write each channel register
				for (int cntr = 3; cntr <= 24; cntr++) {
					if (bDumpRegs) {
						//'Select the channel to read from
						_evalBd.SelectChannel(ChnlCntr);
						//'If dumping the regval then read directly from the DDS
						WriteSettingValue(RegNames[cntr], _evalBd.USBSerialRead(cntr));
					} else {
						//'Otherwize read from the registermap software buffer
						WriteSettingValue( RegNames[cntr], _evalBd.GetRegMapValue(AD9959EvalBd.evb9959_RegMaps.rm9959_CurRegMapVals, cntr, ChnlCntr));
					}
				}
			}
			
			//'Store the current pin values
			//'Write a Blank Line
				WriteHeaderLine("");
			//'Write a header first
			WriteHeaderLine( "Data and Power Down Pins");
			// 'Store the data pin values
			WriteSettingValue( "P0", _evalBd.P0Bit);
			WriteSettingValue( "P1", _evalBd.P1Bit);
			WriteSettingValue( "P2", _evalBd.P2Bit);
			WriteSettingValue( "P3", _evalBd.P3Bit);
			WriteSettingValue( "RURD0", _evalBd.RURD_0Bit);
			WriteSettingValue( "RURD1", _evalBd.RURD_1Bit);
			WriteSettingValue( "RURD2", _evalBd.RURD_2Bit);
			WriteSettingValue( "Power Down", _evalBd.PowerDownBit);
						
			// 'Show default mouse pointer when finished
			System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;

			if (_writer != null) {
				_writer.Close();
			}
		}  // end method SaveConfigFile


		////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Loads either an AD9958 or AD9959 file into the current device
		/// </summary>
		/// <param name="FilePath"></param>
		public void LoadSetup(string FilePath) {

			//Dim FileHandle As Short 'Handle to the file opened
			//Dim cntr As Short
			//Dim iLoopCntr As Short
			string sDummyString;
			string sValue;
			//Dim ActForm As System.Windows.Forms.Form
			//Dim ActControl As System.Windows.Forms.Control
			
			string sProduct;
			string sChannelMask;
			
			string FileType;
			double FileRevNum;
			
			//bool AutoIOUpdate;
			bool OldAutoIOUpdate, newAutoUpdate;
			
			TextFile textFile = new TextFile();
			bool openOK = textFile.OpenForReading(FilePath);
			if (!openOK) {
				throw new ApplicationException("Cannot open AD9959 config file: " + FilePath);
			}
			
			System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
			
			// Store the AutoIOUpdate setting again
			OldAutoIOUpdate = _evalBd.AutoIOUpdate;
			
			// Turn autofud off
			if (_evalBd.AutoIOUpdate) {
				_evalBd.AutoIOUpdate = false;
			}
									
			//Readback the first line which is the header
			ReadSettingNumber(textFile, out FileType, out FileRevNum);
			
			// Get the product
			ReadSettingString(textFile, out sDummyString, out sProduct);
			
			// Get blank line
			ReadHeaderLine(textFile);
			
			// Check the file version
			if (FileType == "AD9958/59 Eval Software Settings File") {
				if (FileRevNum == 1.0) {
					// Get the control windows settings
					double refClock;
					ReadSettingNumber(textFile, out sDummyString, out refClock);
					//_evalBd.RefClockMHz = refClock;
					double currentRefClock = _evalBd.RefClockMHz;
					if (refClock != currentRefClock) {
						throw new ApplicationException("Ref Clock in config file must equal board ref clock: " +
														currentRefClock.ToString());
					}
					bool autoIOUpdate;
					ReadSettingBool(textFile, out sDummyString, out autoIOUpdate);
					newAutoUpdate = autoIOUpdate;
					
					//Get one empty line
					ReadHeaderLine(textFile);
					// Get the register map header
					ReadHeaderLine(textFile);
					
					//With EvBd
					// Loop through all of the non-channel registers
					for (int cntr = 0; cntr < 3; cntr++) {
						ReadSettingString(textFile, out sDummyString, out sValue);
						_evalBd.SetRegMapValue(AD9959EvalBd.evb9959_RegMaps.rm9959_NewRegMapVals, cntr, sValue);
						_evalBd.SetRegMapValue(AD9959EvalBd.evb9959_RegMaps.rm9959_CurRegMapVals, cntr, sValue);
						// if an eval board is attached then load the data
						// TODO: we are not testing here for board attached; only works if board is attached
						_evalBd.USBSerialLoad(cntr, _evalBd.GetRegMapValue(AD9959EvalBd.evb9959_RegMaps.rm9959_NewRegMapVals, cntr));
					}
						
					// Load all channels
					int loopCount = 0;
					int regCount = 0;
					for (int iLoopCntr = 0; iLoopCntr < 4; iLoopCntr++) {
						sChannelMask = _evalBd.GetChMaskVal(iLoopCntr);
						// if an eval board is attached, select the channel
						_evalBd.SelectChannel(iLoopCntr);
						
						// Get blank line
						sDummyString = ReadHeaderLine(textFile);
						if (sDummyString != String.Empty) {
							// Exit the for loop
							throw new ApplicationException("No blank line before Channel Register Values in AD9959 config file.");
							//break;
						}
						
						// Get Channel Header Line
						sDummyString = ReadHeaderLine(textFile);
						if (sDummyString == "Data and Power Down Pins") {
							// Exit the for loop
							break;		//	This may happen if they load a AD9958 Setup into a AD9959
						}
						
						// Load each of the channel registers
						regCount = 0;
						for (int cntr = 0x03; cntr <= 0x18; cntr++) {
							ReadSettingString(textFile, out sDummyString, out sValue);
							_evalBd.SelectChannel(iLoopCntr);
							_evalBd.SelectChannel(iLoopCntr);
							_evalBd.SelectChannel(iLoopCntr);
							_evalBd.SelectChannel(iLoopCntr);
							_evalBd.SetRegMapValue(AD9959EvalBd.evb9959_RegMaps.rm9959_NewRegMapVals, cntr, sValue, sChannelMask);
							_evalBd.SetRegMapValue(AD9959EvalBd.evb9959_RegMaps.rm9959_CurRegMapVals, cntr, sValue, sChannelMask);
							// If an eval board is attached then load the data
							_evalBd.USBSerialLoad(cntr, _evalBd.GetRegMapValue(AD9959EvalBd.evb9959_RegMaps.rm9959_NewRegMapVals, cntr, iLoopCntr));
							regCount++;
						}
						if (regCount != 22) {
							throw new ApplicationException("AD9959 config file error: Incorrect number of registers, channel " + iLoopCntr.ToString());
						}

						loopCount++;
						
					}

					if (loopCount != 4) {
						throw new ApplicationException("AD9959 config file error: Incorrect number of channels: " + loopCount.ToString());
					}
						
					// All of the registers have been loaded so do an IO_Update
					_evalBd.SendIOUpdate();
					
					// Turn autofud back on
					_evalBd.AutoIOUpdate = newAutoUpdate;
						
					// Get blank line
					sDummyString = ReadHeaderLine(textFile);
					// Get Pins Header Line
					sDummyString = ReadHeaderLine(textFile);
						
					if (sDummyString != "Data and Power Down Pins") {
						// This may be needed if the modifies a AD9959 file to load on a AD9958
						// Loop until we have found the correct spot in the file
						do { 
							//Input the next line
							sDummyString = ReadHeaderLine(textFile);
						} while ((sDummyString != null) && (sDummyString != "Data and Power Down Pins"));
					}

					if (sDummyString == null) {
						throw new ApplicationException("AD9959 config file error: No Pin Settings");
					}
					
					// read in the pin settings
					_evalBd.P0Bit = ReadSettingBitValue(textFile, out sDummyString);
					_evalBd.P1Bit = ReadSettingBitValue(textFile, out sDummyString);
					_evalBd.P2Bit = ReadSettingBitValue(textFile, out sDummyString);
					_evalBd.P1Bit = ReadSettingBitValue(textFile, out sDummyString);
					_evalBd.RURD_0Bit = ReadSettingBitValue(textFile, out sDummyString);
					_evalBd.RURD_1Bit = ReadSettingBitValue(textFile, out sDummyString);
					_evalBd.RURD_2Bit = ReadSettingBitValue(textFile, out sDummyString);
					_evalBd.PowerDownBit = ReadSettingBitValue(textFile, out sDummyString);

					
					// Update the evalboard pinvalues
					_evalBd.USBWritePortBuffVal(AD9959EvalBd.fx2GPIO.fx2_PortD);
					// Update the evalboard pinvalues
					_evalBd.USBWritePortBuffVal(AD9959EvalBd.fx2GPIO.fx2_PortA);
					
					System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;

				} // end if FileRev==1
			} else {
				throw new ApplicationException("Wrong product type in AD9959 config file: " + FileType);
			}
		
			textFile.Close();
			System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;

		}  // end LoadSetup()

		//////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		/// <param name="setting"></param>
		/// <param name="value"></param>
		/// <param name="append"></param>
		private void WriteSettingValue(string setting, object value, bool append) {
			// enclose strings in quotes to be compatible with original file written by VisualBasic.
			string line = "\"" + setting + "\",";
			if (value is string) {
				line += "\"" + value + "\"";
			}
			else if (value is int) {
				line += value.ToString();
			}
			else if (value is bool) {
				line += "#" + value.ToString().ToUpper() + "#";
			}
			else if (value is AD9959EvalBd.adiBitValues) {
				AD9959EvalBd.adiBitValues bitVal = (AD9959EvalBd.adiBitValues)value;
				int bit = (int)bitVal;
				line += bit.ToString();
			}
			else {
				throw new ApplicationException("Error in WriteSettingValue() in AD9959ConfigFile.cs");
			}
			//TextFile.WriteLineToFile(_filePath, line, append);
			_writer.WriteLine(line);
		}

		private void WriteSettingValue(string setting, object value) {
			WriteSettingValue(setting, value, true);
		}
		
		private void WriteHeaderLine(string header) {
			WriteHeaderLine(header, true);
		}
		private void WriteHeaderLine(string header, bool append) {
			string line = "\"" + header + "\"";
			//TextFile.WriteLineToFile(_filePath, line, append);
			_writer.WriteLine(line);
		}

		private string ReadHeaderLine(TextFile file) {
			string line = file.ReadLine();
			return line.Trim(quotes);
		}

		private void ReadSettingValue(TextFile file, out string setting, out string value) {
			string line = file.ReadLine();
			string[] items = line.Split(comma);
			if (items.Length != 2) {
				throw new ApplicationException("Improper config file format. Not 2 items per line.");
			}
			items[0] = items[0].Trim(quotes);
			setting = items[0];
			value = items[1];
		}

		private void ReadSettingString(TextFile file, out string setting, out string value) {
			ReadSettingValue(file, out setting, out value);
			if (!value.StartsWith("\"")) {
				throw new ApplicationException("Improper STRING format in AD9959 config file at " + setting);
			}
			value = value.Trim(quotes);
		}

		private void ReadSettingBool(TextFile file, out string setting, out bool value) {
			string sValue;
			ReadSettingValue(file, out setting, out sValue);
			if (!sValue.StartsWith("#")) {
				throw new ApplicationException("Improper BOOL format in AD9959 config file at " + setting);
			}
			if (sValue.ToLower() == "#true#") {
				value = true;
			}
			else {
				value = false;
			}
		}

		private bool ReadSettingBool(TextFile file, out string setting) {
			bool value;
			ReadSettingBool(file, out setting, out value);
			return value;
		}

		private void ReadSettingNumber(TextFile file, out string setting, out double value) {
			string sValue;
			ReadSettingValue(file, out setting, out sValue);
			bool parseOK = double.TryParse(sValue, out value);
			if (!parseOK) {
				throw new ApplicationException("Improper DOUBLE format in AD9959 config file at " + setting);
			}
		}

		private double ReadSettingNumber(TextFile file, out string setting) {
			double value;
			ReadSettingNumber(file, out setting, out value);
			return value;
		}

		private void ReadSettingBitValue(TextFile file, out string setting, out AD9959EvalBd.adiBitValues value) {
			string sValue;
			int iValue;
			ReadSettingValue(file, out setting, out sValue);
			bool parseOK = int.TryParse(sValue, out iValue);
			if (!parseOK) {
				throw new ApplicationException("Improper BIT VALUE format in AD9959 config file at " + setting);
			}
			if (iValue == 0) {
				value = AD9959EvalBd.adiBitValues.abvLow;
			}
			else if (iValue == 1) {
				value = AD9959EvalBd.adiBitValues.abvHigh;
			}
			else {
				throw new ApplicationException("Improper BIT VALUE format in AD9959 config file at " + setting);
			}
		}

		private AD9959EvalBd.adiBitValues ReadSettingBitValue(TextFile file, out string setting) {
			AD9959EvalBd.adiBitValues value;
			ReadSettingBitValue(file, out setting, out value);
			return value;
		}

	}  // end class AD9959ConfigFile
}
