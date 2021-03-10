using System;
using System.Collections.Generic;

using DACarter.Utilities;

namespace DACarter.NOAA.Hardware {

    /// <summary>
    /// Creates CB.CFG file for any MCC DAQ USB-2523 boards attached.
    ///     File is identical to that created by Instacal program.
    /// NOTE: if we switch to another DAQ board, need to run MCC Instacal
    ///     program and see what modifications need to be made to CB.CFG file,
    ///     and modify this class accordingly.
    /// NOTE2: Starting with mccdaq version 2.6, the mcc dll looks in
    ///     \program files NOT \program data folder.
    /// </summary>
    public class DaqMccConfigFile  {

        private int _numBoards;
        private int _numHeaderLines;
        private List<int> _serialNumbers;

        private string[] line;

        public int NumBoards {
            get { return _numBoards; }
            set { _numBoards = value; }
        }

        public List<int> SerialNumbers {
            get { return _serialNumbers; }
            set { _serialNumbers = value; }
        }
        
        public DaqMccConfigFile() {

            _serialNumbers = GetDaqSerialNumbers();

            _numHeaderLines = 8;
            line = new string[_numHeaderLines];

            line[0] = "File Type: Measurement Computing Configuration";
            line[1] = "File Format: ASCII";
            line[2] = "File Version Number: 4";
            line[3] = "Maximum Number Of Boards: 200";
            line[4] = "Maximum Number Of Expansion Boards: 128";
            line[5] = "Current Board: 0";
            line[6] = "Current Board Type: b1";
            line[7] = "";


        }

        public List<int> GetDaqSerialNumbers() {

            List<int> serialNumbers = new List<int>();

            Microsoft.Win32.RegistryKey HKLM, UsbDaqLib, DaqDevice;
            HKLM = Microsoft.Win32.Registry.LocalMachine;
            UsbDaqLib = HKLM.OpenSubKey(@"System\CurrentControlSet\Services\USBDAQLIB\Enum");
            if (UsbDaqLib == null) {
                return serialNumbers;
            }
            int count = (int)UsbDaqLib.GetValue("Count");
            string[] devices = new string[count];
            for (int i = 0; i < count; i++) {
                int serialNumber;
                try {
                    devices[i] = (string)UsbDaqLib.GetValue(i.ToString());
                    string path = @"System\CurrentControlSet\Enum\" + devices[i];
                    DaqDevice = HKLM.OpenSubKey(path);
                    serialNumber = (int)DaqDevice.GetValue("UINumber");
                    serialNumbers.Add(serialNumber);
                }
                catch (Exception ee) {
                    devices[i] = "??";
                    serialNumbers.Add(0);
                }
            }
            return serialNumbers;
        }

        public void Write(string fileName) {
            TextFile.WriteTextToFile(fileName, "", false, false);  // clears file contents
            for (int i = 0; i < _numHeaderLines; i++) {
                DACarter.Utilities.TextFile.WriteLineToFile(fileName, line[i]);
            }
            _numBoards = 0;
            int boardNumber = 0;
            foreach (int sn in _serialNumbers) {
                _numBoards++;
                BoardConfig board = new BoardConfig(boardNumber, sn);
                board.Write(fileName);
                boardNumber++;
            }
        }

    }

    /// <summary>
    /// 
    /// </summary>
    class BoardConfig {

        private int _numDigitalDevices;
        private int _numCounterDevices;
        private int _numInfoLines;
        private int _numMiscOptionLines;
        private int _serialNumber;
        private int _boardNumber;
        private string[] boardInfoLines;
        private string[] optionLines;

        public int SerialNumber {
            get { return _serialNumber; }
            set { _serialNumber = value; }
        }

        private BoardConfig() {
        }

        public BoardConfig(int boardNum, int serialNumber) {
            _numInfoLines = 15;
            _numMiscOptionLines = 14;
            _numDigitalDevices = 3;
            _numCounterDevices = 6;
            _serialNumber = serialNumber;
            _boardNumber = boardNum;

            boardInfoLines = new string[_numInfoLines];
            optionLines = new string[_numMiscOptionLines];

            boardInfoLines[0] = "   Board #" + _boardNumber.ToString();
            boardInfoLines[1] = "   Board Type: USB-2523";
            boardInfoLines[2] = "   Board ID (hex): b1";
            boardInfoLines[3] = "   Base Address (hex): 0";
            boardInfoLines[4] = "   Number Of I/O Ports Used By Board: 0";
            boardInfoLines[5] = "   Interrupt Level: 0";
            boardInfoLines[6] = "   DMA Channel: 0";
            boardInfoLines[7] = "   Clock Speed (MHz): 10";
            boardInfoLines[8] = "   A/D Range: NOT USED";
            boardInfoLines[9] = "   Wait States: DISABLED";
            boardInfoLines[10] = "   Number Of A/D Channels: 8";
            boardInfoLines[11] = "   Number Of D/A Channels: 0";
            boardInfoLines[12] = "   Number Of Digital Devices: " + _numDigitalDevices.ToString();
            boardInfoLines[13] = "   Number Of Counter Devices: " + _numCounterDevices.ToString();
            boardInfoLines[14] = "   Uses Expansion Boards: NO";

            short serNumLo, serNumHi;
            // upper and lower 16-bits of serial number
            serNumHi = (short)(serialNumber >> 16);
            serNumLo = (short)(serialNumber - (serNumHi << 16));

            optionLines[0] = "   Misc Option[0]: " + serNumLo.ToString();
            optionLines[1] = "   Misc Option[1]: " + serNumHi.ToString();
            optionLines[2] = "   Misc Option[2]: 0";
            optionLines[3] = "   Misc Option[3]: 1";
            optionLines[4] = "   Misc Option[4]: 0";
            optionLines[5] = "   Misc Option[5]: 1";
            optionLines[6] = "   Misc Option[6]: 0";
            optionLines[7] = "   Misc Option[7]: 1";
            optionLines[8] = "   Misc Option[8]: 1";
            optionLines[9] = "   Misc Option[9]: 1";
            optionLines[10] = "   Misc Option[10]: 1";
            optionLines[11] = "   Misc Option[11]: 0";
            optionLines[12] = "   Misc Option[12]: 256";
            optionLines[13] = "   Misc Option[13]: 1";

        }

        public void Write(string fileName) {

            for (int i = 0; i < _numInfoLines; i++) {
                TextFile.WriteLineToFile(fileName, boardInfoLines[i]);
            }
            for (int i = 0; i < _numMiscOptionLines; i++) {
                TextFile.WriteLineToFile(fileName, optionLines[i]);
            }
            for (int idev = 0; idev < _numDigitalDevices; idev++) {
                DigitalDevice digDev = new DigitalDevice(_boardNumber, idev, _numDigitalDevices);
                digDev.Write(fileName);
            }

            for (int icnt = 0; icnt < _numCounterDevices; icnt++) {
                CounterDevice cntDev = new CounterDevice(_boardNumber, icnt, _numCounterDevices);
                cntDev.Write(fileName);
            }
            TextFile.WriteLineToFile(fileName, "   End Board #" + _boardNumber.ToString());
            TextFile.WriteLineToFile(fileName, "");
        }

    }

    /// <summary>
    /// 
    /// </summary>
    class DigitalDevice {

        private int _deviceNumber;
        private int _numLines;
        private string[] lines;

        private DigitalDevice() {
        }

        public DigitalDevice(int boardNum, int deviceNumberOnBoard, int numDigitalDev) {
            _deviceNumber = deviceNumberOnBoard + boardNum * numDigitalDev;
            _numLines = 9;
            lines = new string[_numLines];

            lines[0] = "";
            lines[1] = "      Digital Device #" + _deviceNumber.ToString(); ;
            lines[2] = "      Base Address (hex): ffff (hex)";
            lines[3] = "      Type Of Device: 1st Port ";
            if (deviceNumberOnBoard == 0) {
                lines[3] += "A";
            }
            else if (deviceNumberOnBoard == 1) {
                lines[3] += "B";
            }
            else if (deviceNumberOnBoard == 2) {
                lines[3] += "CL";
            }
            lines[4] = "      Device Bit Mask: ff";
            lines[5] = "      Read Before Writing Required: NO";
            lines[6] = "      Device Is Configurable: INPUT & OUTPUT";
            lines[7] = "      Number Of I/O Bits For Device: 8";
            lines[8] = "      End Digital Device #" + _deviceNumber.ToString();
        }

        public void Write(string fileName) {
            for (int i = 0; i < _numLines; i++) {
                TextFile.WriteLineToFile(fileName, lines[i]);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    class CounterDevice {

        private int _deviceNumber;
        private int _boardNumber;
        private int _deviceNumberOnChip;
        private int _numScanCounters;
        private int _numTimers;

        private CounterDevice() {
        }

        public 
            CounterDevice(int boardNumber, int deviceNumberOnChip, int numCounterDev) {

            _boardNumber = boardNumber;
            _deviceNumberOnChip = deviceNumberOnChip;
            _deviceNumber = deviceNumberOnChip + boardNumber * numCounterDev;
            _numScanCounters = 4;
            _numTimers = 2;
        }

        public void Write(string fileName) {
            if (_deviceNumberOnChip < _numScanCounters) {
                ScanCounterDevice cntrDev = new ScanCounterDevice(_deviceNumberOnChip, _deviceNumber);
                cntrDev.Write(fileName);
            }
            else if (_deviceNumberOnChip < (_numScanCounters + _numTimers)) {
                TimerDevice timerDev = new TimerDevice(_deviceNumberOnChip - _numScanCounters, _deviceNumber);
                timerDev.Write(fileName);
            }
        }

    }

    class ScanCounterDevice {

        private int _deviceNumber;
        private int _numLines;
        private string[] lines;

        public ScanCounterDevice(int deviceNumberOnChip, int deviceNumber) {
            _deviceNumber = deviceNumber;
            _numLines = 6;
            lines = new string[_numLines];

            lines[0] = "";
            lines[1] = "      Counter Device #" + _deviceNumber.ToString();
            lines[2] = "      Base Address (hex): ffff (hex)";
            lines[3] = "      Type Of Device: Scan Counter";
            lines[4] = "      Counter number (on chip): " + deviceNumberOnChip.ToString();
            lines[5] = "      End Counter Device #" + _deviceNumber.ToString();
        }

        public void Write(string fileName) {
             for (int i = 0; i < _numLines; i++) {
                TextFile.WriteLineToFile(fileName, lines[i]);
             }
        }
    }

    class TimerDevice {

        private int _deviceNumber;
        private int _numLines;
        private string[] lines;

        public TimerDevice(int deviceNumberOnChip, int deviceNumber) {
            _deviceNumber = deviceNumber;
            _numLines = 6;
            lines = new string[_numLines];

            lines[0] = "";
            lines[1] = "      Counter Device #" + _deviceNumber.ToString();
            lines[2] = "      Base Address (hex): ffff (hex)";
            lines[3] = "      Type Of Device: Timer";
            lines[4] = "      Counter number (on chip): " + deviceNumberOnChip.ToString();
            lines[5] = "      End Counter Device #" + _deviceNumber.ToString();
	    }

        public void Write(string fileName) {
            for (int i = 0; i < _numLines; i++) {
                TextFile.WriteLineToFile(fileName, lines[i]);
            }
        }
    }
}
