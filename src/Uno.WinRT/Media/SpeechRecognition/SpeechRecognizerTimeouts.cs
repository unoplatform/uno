using System;

namespace Windows.Media.SpeechRecognition
{
	public partial class SpeechRecognizerTimeouts
	{
		internal SpeechRecognizerTimeouts()
		{
#if __SKIA__
			// WinRT doesn't document its defaults; these are the documented Windows Phone 8 values.
			InitialSilenceTimeout = TimeSpan.FromSeconds(5);
			EndSilenceTimeout = TimeSpan.FromMilliseconds(150);
#endif
		}

		public TimeSpan InitialSilenceTimeout { get; set; }

		public TimeSpan EndSilenceTimeout { get; set; }

		public TimeSpan BabbleTimeout { get; set; }
	}
}
