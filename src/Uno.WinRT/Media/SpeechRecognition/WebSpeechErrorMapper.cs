namespace Windows.Media.SpeechRecognition;

// Maps W3C Web Speech API error codes to the UWP outcome contract. Kept platform-neutral so the
// mapping can be unit tested; only the WebAssembly recognizer consumes it.
internal static class WebSpeechErrorMapper
{
	internal static SpeechRecognitionResultStatus ToStatus(string error) =>
		error switch
		{
			"no-speech" => SpeechRecognitionResultStatus.TimeoutExceeded,
			"no-match" => SpeechRecognitionResultStatus.Success,
			"aborted" => SpeechRecognitionResultStatus.UserCanceled,
			"audio-capture" => SpeechRecognitionResultStatus.MicrophoneUnavailable,
			"not-allowed" => SpeechRecognitionResultStatus.MicrophoneUnavailable,
			"service-not-allowed" => SpeechRecognitionResultStatus.MicrophoneUnavailable,
			"network" => SpeechRecognitionResultStatus.NetworkFailure,
			"language-not-supported" => SpeechRecognitionResultStatus.TopicLanguageNotSupported,
			"bad-grammar" => SpeechRecognitionResultStatus.GrammarCompilationFailure,
			_ => SpeechRecognitionResultStatus.Unknown
		};
}
