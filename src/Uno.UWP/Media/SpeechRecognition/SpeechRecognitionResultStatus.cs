namespace Windows.Media.SpeechRecognition
{
	public enum SpeechRecognitionResultStatus 
	{
		Success,
		TopicLanguageNotSupported,
		GrammarLanguageMismatch,
		GrammarCompilationFailure,
		AudioQualityFailure,
		UserCanceled,
		Unknown,
		TimeoutExceeded,
		PauseLimitExceeded,
		NetworkFailure,
		MicrophoneUnavailable
	}
}
