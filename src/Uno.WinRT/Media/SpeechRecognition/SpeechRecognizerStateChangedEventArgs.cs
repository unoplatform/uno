namespace Windows.Media.SpeechRecognition
{
	public partial class SpeechRecognizerStateChangedEventArgs
	{
		internal SpeechRecognizerStateChangedEventArgs()
		{
		}

		public SpeechRecognizerState State { get; internal set; }
	}
}
