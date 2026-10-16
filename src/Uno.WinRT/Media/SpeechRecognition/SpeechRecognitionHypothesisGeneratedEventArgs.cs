namespace Windows.Media.SpeechRecognition
{
	public partial class SpeechRecognitionHypothesisGeneratedEventArgs
	{
		internal SpeechRecognitionHypothesisGeneratedEventArgs()
		{
		}

		public SpeechRecognitionHypothesis Hypothesis { get; internal set; }
	}
}
