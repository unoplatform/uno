#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Globalization;

namespace Windows.Media.SpeechRecognition;

/// <summary>
/// Platform speech engine used by the Skia <see cref="SpeechRecognizer"/>.
/// </summary>
/// <remarks>
/// The extension only reports raw engine events; WinUI semantics (states, timeouts, statuses, constraints)
/// are implemented by <see cref="SpeechRecognizer"/>. Methods must not raise events synchronously.
/// Each <see cref="BeginUtterance"/> is answered by exactly one <see cref="UtteranceCompleted"/>.
/// </remarks>
internal interface ISpeechRecognizerExtension : IDisposable
{
	event EventHandler<string>? HypothesisGenerated;

	event EventHandler? SpeechDetected;

	/// <summary>Raised when the engine stopped consuming audio and is computing the final result.</summary>
	event EventHandler? AudioCaptureEnded;

	event EventHandler<SpeechAudioLevel>? AudioLevelChanged;

	event EventHandler<SpeechUtteranceOutcome>? UtteranceCompleted;

	/// <returns><see langword="false"/> when the language is not supported.</returns>
	bool Initialize(Language language);

	Task<SpeechRecognizerAuthorization> RequestAuthorizationAsync();

	/// <summary>Starts recognizing a new utterance, starting audio capture if needed.</summary>
	void BeginUtterance(SpeechUtteranceOptions options);

	/// <summary>Stops feeding audio to the current utterance and requests its final result.</summary>
	void EndUtterance();

	/// <summary>Discards the current utterance, which completes as <see cref="SpeechUtteranceOutcomeKind.Canceled"/>.</summary>
	void CancelUtterance();

	void StopAudio();

	IReadOnlyList<string> GetSupportedLanguageTags();

	string? GetSystemLanguageTag();
}

internal enum SpeechRecognizerAuthorization
{
	Authorized,
	Denied,
	MissingUsageDescription,
}

internal enum SpeechTaskHint
{
	Unspecified = 0,
	Dictation = 1,
	Search = 2,
	Confirmation = 3,
}

internal sealed record SpeechUtteranceOptions(SpeechTaskHint TaskHint, IReadOnlyList<string> ContextualStrings);

internal readonly record struct SpeechAudioLevel(float RmsDecibels, float PeakDecibels);

/// <param name="Confidence">Between 0 and 1.</param>
/// <param name="Start">Offset from <see cref="SpeechUtteranceOutcome.AudioStartTime"/>.</param>
internal sealed record SpeechTranscription(string Text, double Confidence, TimeSpan Start, TimeSpan Duration);

internal enum SpeechUtteranceOutcomeKind
{
	Success,
	Error,
	Canceled,
}

/// <param name="Transcriptions">Most likely first. Only set for <see cref="SpeechUtteranceOutcomeKind.Success"/>.</param>
/// <param name="AudioStartTime">When the first audio sample of the utterance was captured, if known.</param>
/// <param name="ErrorStatus">The WinUI status matching the engine error.</param>
internal sealed record SpeechUtteranceOutcome(
	SpeechUtteranceOutcomeKind Kind,
	IReadOnlyList<SpeechTranscription> Transcriptions,
	DateTimeOffset? AudioStartTime = null,
	SpeechRecognitionResultStatus ErrorStatus = SpeechRecognitionResultStatus.Unknown,
	string? ErrorMessage = null);
