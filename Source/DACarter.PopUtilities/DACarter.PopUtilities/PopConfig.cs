using DACarter.Utilities;

namespace DACarter.PopUtilities {

	public static class PopStateFile {

		private static PopConfig _config;

		static PopStateFile() {
			_config = new PopConfig();
		}

		public static void SetAutoStart(bool auto) {
			_config.Read();
			_config.AutoStart = auto;
			_config.Write();
		}

		public static void SetParFile(string fileName) {
			_config.Read();
			_config.LastParFile = fileName;
			_config.Write();
		}

		public static void SetLogFolder(string folder) {
			_config.Read();
			_config.LogFileFolder = folder;
			_config.Write();
		}

		public static bool GetAutoStart() {
			_config.Read();
			return _config.AutoStart;
		}

		public static string GetParFile() {
			_config.Read();
			return _config.LastParFile;
		}

		public static string GetLogFolder() {
			_config.Read();
			return _config.LogFileFolder;
		}
	}

	public class PopConfig {

		public string LastParFile;
		public bool AutoStart;
		public string LogFileFolder;

		private string _fileName;

		public PopConfig() {
			_fileName = "State";
			LastParFile = "";
			AutoStart = false;
			LogFileFolder = "";
		}

		public void Read() {
			object obj = DacSerializer.DeserializeAppObject(_fileName, typeof(PopConfig));
			if (obj is PopConfig) {
				PopConfig oldConfig = (PopConfig)obj;
				this.LastParFile = oldConfig.LastParFile;
				this.AutoStart = oldConfig.AutoStart;
				this.LogFileFolder = oldConfig.LogFileFolder;
			}
		}

		public bool Write() {
			bool OK = DacSerializer.SerializeAppObject(_fileName, this, typeof(PopConfig));
			return OK;
		}
	}
}
