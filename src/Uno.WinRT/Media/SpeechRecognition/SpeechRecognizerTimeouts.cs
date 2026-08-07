using System;

namespace Windows.Media.SpeechRecognition
{
	public partial class SpeechRecognizerTimeouts
	{
		public TimeSpan InitialSilenceTimeout { get; set; }

		public TimeSpan EndSilenceTimeout { get; set; }

		public TimeSpan BabbleTimeout { get; set; }
	}
}
