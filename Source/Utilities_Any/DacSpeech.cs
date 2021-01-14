using System;
using SpeechLib;
using System.Windows.Forms;

namespace DACarter.Utilities
{
	/// <summary>
	/// Summary description for DacSpeech.
	/// </summary>
	public class DacSpeech
	{
		// default settings
		private static int _speakingRate = -2;		// choose value from -10 to 10
		private static int _whichVoice = 0;			// choose voice index 
		private static int _volume = 100;			// choose volume (1-100?)

		public static int NumberOfVoices;

		private DacSpeech()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		static DacSpeech() {
			SpVoice theVoice = new SpVoice();
			NumberOfVoices = theVoice.GetVoices("", "").Count;
		}

		public static void Speak(string text) {

			try {
				SpeechVoiceSpeakFlags SpFlags = 0;	// 0 = synchronous fn call
				SpVoice Voice = new SpVoice();

				if (_whichVoice < 0) {
					_whichVoice = 0;
				}
				if (_whichVoice >= NumberOfVoices) {
					_whichVoice = NumberOfVoices - 1;
				}
				Voice.Voice = Voice.GetVoices("","").Item(_whichVoice);

				Voice.Rate = _speakingRate;

				Voice.Volume = _volume;
				if (text.Length > 0) {
					Voice.Speak(text, SpFlags);
				}
				else {
					Voice.Speak("I have nothing to say.",SpFlags);
				}
			}
			catch (Exception e) {
				MessageBoxEx.Show(text,"Cannot Speak" + _whichVoice + NumberOfVoices,
					MessageBoxButtons.OK, MessageBoxIcon.Exclamation, 2000);
			}
		}

		public static void Speak(string text, int voiceIndex) {
			_whichVoice = voiceIndex;
			try {
				Speak(text);
			}
			catch (Exception e) {
				MessageBoxEx.Show(text,"Cannot Speak",MessageBoxButtons.OK, MessageBoxIcon.Exclamation, 2000);
			}
		}

		public static void Speak(string text, int voiceIndex, int volume) {
			if ((volume <= 100) && (volume > 0)) {
				_volume = volume;
			}
			try {
				Speak(text, voiceIndex);
			}
			catch (Exception e) {
				MessageBoxEx.Show(text,"Cannot Speak",MessageBoxButtons.OK, MessageBoxIcon.Exclamation, 2000);
			}
		}

		public static void Speak(string text, int voiceIndex, int volume, int rate) {
			if ((rate <= 10) && (rate >= -10)) {
				_speakingRate = rate;
			}
			try {
				Speak(text, voiceIndex, volume);
			}
			catch (Exception e) {
				MessageBoxEx.Show(text,"Cannot Speak",MessageBoxButtons.OK, MessageBoxIcon.Exclamation, 2000);
			}
		}


	}
}
