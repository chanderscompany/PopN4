using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DACarter.Utilities;
using DACarter.PopUtilities;
using POPCommunication;

namespace POPN4Service {

    public static class PopNStateFile {

        private static PopNConfig _config;

        static PopNStateFile() {
            _config = new PopNConfig();
            // read in currently stored state, before making any mods
            //_config.Read();
        }

        public static void SetAutoStart(bool auto) {
            _config.Read();
            _config.AutoStart = auto;
            _config.Write();
        }

        public static void SetCurrentParFile(string fileName) {
            _config.Read();
            _config.LastParFile = fileName;
            _config.Write();
        }

        public static void SetParFileFolderRelPath(string relPath) {
            _config.Read();
            _config.ParFileFolderRelPath = relPath;
            _config.Write();
        }

        public static void SetParFileCommand(string fileName) {
            _config.Read();
            _config.ParFileCommand = fileName;

            /*
            // default is to set data and log file paths directly from par file
            PopParameters parameters = PopParameters.ReadFromFile(fileName);
            string logFolder = parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].LogFileFolder;
            _config.LogFileFolder = logFolder;

            bool writeEnable0 = parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].FileWriteEnabled;
            bool writeEnable1 = parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].FileWriteEnabled;
            string dataFolder = "";
            if (writeEnable0) {
                dataFolder = parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].FileFolder;
            }
            else if (writeEnable1) {
                dataFolder = parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].FileFolder;
            }
            //_config.DataFileFolder = dataFolder;
            */

            _config.Write();
        }

        public static void SetLogFolder(string folder) {
            _config.Read();
            _config.LogFileFolder = folder;
            _config.Write();
        }

        /*
        public static void SetDataFolder(string folder) {
            _config.Read();
            _config.DataFileFolder = folder;
            _config.Write();
        }
         * */

        public static void SetLastCommand(PopCommands command) {
            _config.Read();
            _config.LastCommand = command;
            _config.Write();
        }

        public static void SetCurrentStatus(PopStatus status) {
            _config.Read();
            _config.Status = status;
            _config.Write();
        }

        /*
        public static void SetNoHardware(bool noHardware) {
            _config.Read();
            _config.NoHardware = noHardware;
            _config.Write();
        }

        public static void SetNoPbx(bool noPbx) {
            _config.Read();
            _config.NoPbx = noPbx;
            _config.Write();
        }
         * */

        public static void SetDebug(bool debug) {
            _config.Read();
            _config.Debug = debug;
            _config.Write();
        }

        public static bool GetAutoStart() {
            _config.Read();
            return _config.AutoStart;
        }

        public static string GetLastParFile() {
            _config.Read();
            return _config.LastParFile;
        }

        public static string GetParFileFolderRelPath() {
            _config.Read();
            return _config.ParFileFolderRelPath;
        }

        public static string GetParFileCommand() {
            _config.Read();
            return _config.ParFileCommand;
        }

        public static string GetLogFolder() {
            _config.Read();
            return _config.LogFileFolder;
        }

        /*
        public static string GetDataFolder() {
            _config.Read();
            return _config.DataFileFolder;
        }
         * */

        public static PopCommands GetLastCommand() {
            _config.Read();
            return _config.LastCommand;
        }

        public static PopStatus GetCurrentStatus() {
            _config.Read();
            return _config.Status;
        }

        /*
        public static bool GetNoHardware() {
            _config.Read();
            return _config.NoHardware;
        }

        public static bool GetNoPbx() {
            _config.Read();
            return _config.NoPbx;
        }
        * */

        public static bool GetDebug() {
            _config.Read();
            return _config.Debug;
        }
 
    }

    public class PopNConfig {

        public string ParFileCommand;   // par file to use; set by user interface; read by worker waiting to Start
                                        //   also set by worker autostart
        public string LastParFile;      // last par file used; set by worker thread initStart; 
                                        //   read by mainForm ctor for default file in dropdown
                                        //   read by worker for autostart par file (2)
                                        //   
        public string ParFileFolderRelPath;     // path to par file folder relative to executable directory;
                                                //   used by user interface for initial display
        public bool AutoStart;
        public string LogFileFolder;
        //public string DataFileFolder;
        public PopCommands LastCommand;
        public PopStatus Status;
        //POPREV: removed debug options from state file rev 3.15
        //public bool NoHardware;
        //public bool NoPbx;
        public bool Debug;

        private string _fileName;

        public PopNConfig() {
            _fileName = "State";
            LastParFile = "";
            ParFileFolderRelPath = "";
            ParFileCommand = "";
            AutoStart = false;
            LogFileFolder = "";
            //DataFileFolder = "";
            LastCommand = PopCommands.None;
            Status = PopStatus.None;
            //NoHardware = false;
            //NoPbx = false;
            Debug = false;
        }

        public void Read() {
            object obj = DacSerializer.DeserializeAppObject(_fileName, typeof(PopNConfig), baseName: "POPN4");
            if (obj is PopNConfig) {
                PopNConfig oldConfig = (PopNConfig)obj;
                this.LastParFile = oldConfig.LastParFile;
                this.ParFileCommand = oldConfig.ParFileCommand;
                this.ParFileFolderRelPath = oldConfig.ParFileFolderRelPath;
                this.AutoStart = oldConfig.AutoStart;
                this.LogFileFolder = oldConfig.LogFileFolder;
                //this.DataFileFolder = oldConfig.DataFileFolder;
                this.LastCommand = oldConfig.LastCommand;
                this.Status = oldConfig.Status;
                //this.NoHardware = oldConfig.NoHardware;
                //this.NoPbx = oldConfig.NoPbx;
                this.Debug = oldConfig.Debug;
            }
        }

        public bool Write() {
            bool OK = DacSerializer.SerializeAppObject(_fileName, this, typeof(PopNConfig), baseName: "POPN4");
            return OK;
        }
    }
}
