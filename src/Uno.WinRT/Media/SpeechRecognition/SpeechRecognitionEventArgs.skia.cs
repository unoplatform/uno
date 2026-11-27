#nullable enable

namespace Windows.Media.SpeechRecognition;

public partial class SpeechContinuousRecognitionResultGeneratedEventArgs
{
	internal SpeechContinuousRecognitionResultGeneratedEventArgs(SpeechRecognitionResult result) => Result = result;

	public SpeechRecognitionResult Result { get; }
}

public partial class SpeechContinuousRecognitionCompletedEventArgs
{
	internal SpeechContinuousRecognitionCompletedEventArgs(SpeechRecognitionResultStatus status) => Status = status;

	public SpeechRecognitionResultStatus Status { get; }
}

public partial class SpeechRecognitionQualityDegradingEventArgs
{
	internal SpeechRecognitionQualityDegradingEventArgs(SpeechRecognitionAudioProblem problem) => Problem = problem;

	public SpeechRecognitionAudioProblem Problem { get; }
}
