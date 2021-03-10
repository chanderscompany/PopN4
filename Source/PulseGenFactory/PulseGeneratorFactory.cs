using System;

using DACarter.PopUtilities;

namespace DACarter.NOAA.Hardware {

    public static class PulseGeneratorFactory {

        private static PulseGeneratorDevice _pulseGenDevice;

        static PulseGeneratorFactory() {
            _pulseGenDevice = null;
        }

        /// <summary>
        /// If there is a PulseBlaster Card present,
        ///     create Pulse Blaster object;
        /// else
        ///     create PbxControllerCard object.
        /// Unless argument is true, then only create PbxControllerCard
        /// </summary>
        /// <returns></returns>
        public static PulseGeneratorDevice GetNewPulseGenDevice(bool getPBXonly = false) {

            _pulseGenDevice = null;
            if (!getPBXonly) {
                try {
                    _pulseGenDevice = new PulseBlaster();
                }
                catch (Exception e) {
                    _pulseGenDevice = null;
                }
            }
            if ((_pulseGenDevice == null) || !_pulseGenDevice.Exists()) {
                _pulseGenDevice = new PbxControllerCard();
            }
            return _pulseGenDevice;
        }

        public static PulseGeneratorDevice GetNewPulseGenDevice(PopParameters parameters, int parSetIndex) {

            _pulseGenDevice = new PulseBlaster(parameters, parSetIndex);
            if ((_pulseGenDevice == null) || !_pulseGenDevice.Exists()) {
                _pulseGenDevice = new PbxControllerCard(parameters, parSetIndex);
            }
            return _pulseGenDevice;
        }

    }
}
