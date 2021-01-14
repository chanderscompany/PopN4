using DACarter.PopUtilities;

namespace DACarter.NOAA.Hardware {
	/// <summary>
	/// Base class for all hardware that is used to generate pulses.
	/// </summary>
	public abstract class PulseGeneratorDevice {


		public abstract bool Exists();
		public abstract void Setup(PopParameters parameters, int parSetIndex);
		public abstract void StartPulses();
		public abstract void StopPulses();
        public abstract void Reset();
        public abstract int ReadStatus();
        public abstract bool IsBusy();
        public abstract void Close();

		protected PopParameters _parameters;
		protected int _parSetIndex;

		public PulseGeneratorDevice() {
			_parameters = null;
			_parSetIndex = -1;
		}

		public PulseGeneratorDevice(PopParameters parameters, int parSetIndex) {
			Init(parameters, parSetIndex);
		}

		public void Init(PopParameters parameters, int parSetIndex) {
			_parameters = parameters;
			_parSetIndex = parSetIndex;
		}
	}
}
