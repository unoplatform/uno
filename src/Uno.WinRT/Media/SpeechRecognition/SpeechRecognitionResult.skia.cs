#nullable enable

using System;

namespace Windows.Media.SpeechRecognition;

public partial class SpeechRecognitionResult
{
	public SpeechRecognitionResultStatus Status { get; internal set; }

	public SpeechRecognitionConfidence Confidence { get; internal set; } = SpeechRecognitionConfidence.Rejected;

	public DateTimeOffset PhraseStartTime { get; internal set; }

	public TimeSpan PhraseDuration { get; internal set; }

	// WinRT doesn't define the RawConfidence ranges of each level.
	internal static SpeechRecognitionConfidence GetConfidence(double rawConfidence)
		=> rawConfidence switch
		{
			>= 0.8 => SpeechRecognitionConfidence.High,
			>= 0.5 => SpeechRecognitionConfidence.Medium,
			_ => SpeechRecognitionConfidence.Low,
		};
}
