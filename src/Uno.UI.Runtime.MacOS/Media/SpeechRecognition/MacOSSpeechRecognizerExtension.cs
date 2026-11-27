#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Windows.Globalization;
using Windows.Media.SpeechRecognition;

namespace Uno.UI.Runtime.MacOS;

internal sealed class MacOSSpeechRecognizerExtension : ISpeechRecognizerExtension
{
	// Must match UNOSpeechSignal, UNOSpeechUtteranceOutcome and UNOSpeechErrorCode in UNOSpeechRecognizer.h.
	private const int SignalSpeechDetected = 0;
	private const int SignalFinishedReadingAudio = 1;
	private const int OutcomeSuccess = 0;
	private const int OutcomeError = 1;
	private const string UnoSpeechErrorDomain = "UnoSpeech";
	private const long ErrorNoInputDevice = 1;
	private const long ErrorAudioEngineFailed = 2;
	private const long ErrorRecognizerUnavailable = 3;

	private const int MissingUsageDescription = -1;
	private const int SpeechAuthorized = 3; // SFSpeechRecognizerAuthorizationStatusAuthorized
	private const int MicrophoneAuthorized = 3; // AVAuthorizationStatusAuthorized

	private const char ListSeparator = '\x1F';

	private const string SFSpeechErrorDomain = "SFSpeechErrorDomain";
	private const long SFSpeechErrorAudioReadFailed = 2;
	private const long SFSpeechErrorTimeout = 12;

	// The speech service also reports errors from these undocumented domains.
	private const string AssistantErrorDomain = "kAFAssistantErrorDomain";
	private const string LocalSpeechErrorDomain = "kLSRErrorDomain";
	private const long AssistantErrorNoSpeechDetected = 1110;
	private const long AssistantErrorCanceled = 216;
	private const long RecognitionRequestCanceled = 301;

	private static readonly ConcurrentDictionary<nint, MacOSSpeechRecognizerExtension> _instances = new();
	private static readonly ConcurrentDictionary<long, TaskCompletionSource<SpeechRecognizerAuthorization>> _authorizationRequests = new();
	private static long _nextAuthorizationToken;

	private nint _handle;

	public event EventHandler<string>? HypothesisGenerated;
	public event EventHandler? SpeechDetected;
	public event EventHandler? AudioCaptureEnded;
	public event EventHandler<SpeechAudioLevel>? AudioLevelChanged;
	public event EventHandler<SpeechUtteranceOutcome>? UtteranceCompleted;

	public static unsafe void Register()
	{
		NativeUno.uno_speech_set_callbacks(&OnHypothesis, &OnSignal, &OnAudioLevel, &OnUtteranceCompleted, &OnAuthorization);
		ApiExtensibility.Register(typeof(ISpeechRecognizerExtension), _ => new MacOSSpeechRecognizerExtension());
	}

	public bool Initialize(Language language)
	{
		DestroyHandle();

		var languageTag = language?.LanguageTag ?? CultureInfo.CurrentCulture.Name;
		_handle = NativeUno.uno_speech_recognizer_create(languageTag);
		if (_handle == 0)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Speech recognition is not supported for language '{languageTag}' on this Mac.");
			}

			return false;
		}

		_instances[_handle] = this;
		return true;
	}

	public Task<SpeechRecognizerAuthorization> RequestAuthorizationAsync()
	{
		var token = Interlocked.Increment(ref _nextAuthorizationToken);
		var completion = new TaskCompletionSource<SpeechRecognizerAuthorization>(TaskCreationOptions.RunContinuationsAsynchronously);
		_authorizationRequests[token] = completion;
		NativeUno.uno_speech_request_authorization(token);
		return completion.Task;
	}

	public void BeginUtterance(SpeechUtteranceOptions options)
	{
		var contextualStrings = options.ContextualStrings.Count > 0 ? string.Join(ListSeparator, options.ContextualStrings) : null;
		NativeUno.uno_speech_recognizer_begin_utterance(GetHandle(), (int)options.TaskHint, contextualStrings);
	}

	public void EndUtterance() => NativeUno.uno_speech_recognizer_end_utterance(GetHandle());

	public void CancelUtterance() => NativeUno.uno_speech_recognizer_cancel_utterance(GetHandle());

	public void StopAudio()
	{
		if (_handle != 0)
		{
			NativeUno.uno_speech_recognizer_stop_audio(_handle);
		}
	}

	public IReadOnlyList<string> GetSupportedLanguageTags()
		=> TakeNativeString(NativeUno.uno_speech_copy_supported_locales())?.Split(ListSeparator, StringSplitOptions.RemoveEmptyEntries)
			?? Array.Empty<string>();

	public string? GetSystemLanguageTag() => TakeNativeString(NativeUno.uno_speech_copy_default_locale());

	public void Dispose() => DestroyHandle();

	private nint GetHandle() => _handle != 0 ? _handle : throw new InvalidOperationException("The speech recognizer language is not supported.");

	private void DestroyHandle()
	{
		var handle = Interlocked.Exchange(ref _handle, 0);
		if (handle != 0)
		{
			_instances.TryRemove(handle, out _);
			NativeUno.uno_speech_recognizer_destroy(handle);
		}
	}

	private static unsafe string? TakeNativeString(nint value)
	{
		if (value == 0)
		{
			return null;
		}

		try
		{
			return Marshal.PtrToStringUTF8(value);
		}
		finally
		{
			NativeMemory.Free((void*)value);
		}
	}

	internal static (SpeechUtteranceOutcomeKind Kind, SpeechRecognitionResultStatus Status) MapError(string? domain, long code)
		=> (domain, code) switch
		{
			(UnoSpeechErrorDomain, ErrorNoInputDevice or ErrorAudioEngineFailed) => (SpeechUtteranceOutcomeKind.Error, SpeechRecognitionResultStatus.MicrophoneUnavailable),
			(UnoSpeechErrorDomain, ErrorRecognizerUnavailable) => (SpeechUtteranceOutcomeKind.Error, SpeechRecognitionResultStatus.NetworkFailure),
			(SFSpeechErrorDomain, SFSpeechErrorAudioReadFailed) => (SpeechUtteranceOutcomeKind.Error, SpeechRecognitionResultStatus.AudioQualityFailure),
			(SFSpeechErrorDomain, SFSpeechErrorTimeout) => (SpeechUtteranceOutcomeKind.Error, SpeechRecognitionResultStatus.TimeoutExceeded),
			(AssistantErrorDomain, AssistantErrorNoSpeechDetected) => (SpeechUtteranceOutcomeKind.Error, SpeechRecognitionResultStatus.TimeoutExceeded),
			(AssistantErrorDomain, AssistantErrorCanceled or RecognitionRequestCanceled) or (LocalSpeechErrorDomain, RecognitionRequestCanceled)
				=> (SpeechUtteranceOutcomeKind.Canceled, SpeechRecognitionResultStatus.UserCanceled),
			("NSURLErrorDomain", _) => (SpeechUtteranceOutcomeKind.Error, SpeechRecognitionResultStatus.NetworkFailure),
			_ => (SpeechUtteranceOutcomeKind.Error, SpeechRecognitionResultStatus.Unknown),
		};

	internal static SpeechUtteranceOutcome ParseResult(string json)
	{
		using var document = JsonDocument.Parse(json);
		var root = document.RootElement;

		var transcriptions = new List<SpeechTranscription>();
		foreach (var transcription in root.GetProperty("transcriptions").EnumerateArray())
		{
			transcriptions.Add(new SpeechTranscription(
				transcription.GetProperty("text").GetString() ?? string.Empty,
				transcription.GetProperty("confidence").GetDouble(),
				TimeSpan.FromSeconds(transcription.GetProperty("start").GetDouble()),
				TimeSpan.FromSeconds(transcription.GetProperty("duration").GetDouble())));
		}

		var audioStart = root.GetProperty("audioStart").GetDouble();
		return new SpeechUtteranceOutcome(
			SpeechUtteranceOutcomeKind.Success,
			transcriptions,
			audioStart > 0 ? DateTimeOffset.FromUnixTimeMilliseconds((long)(audioStart * 1000)) : null);
	}

	private static SpeechUtteranceOutcome CreateOutcome(int outcome, string? json, string? errorDomain, long errorCode, string? errorMessage)
	{
		if (outcome == OutcomeSuccess && json is not null)
		{
			try
			{
				return ParseResult(json);
			}
			catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
			{
				return new SpeechUtteranceOutcome(SpeechUtteranceOutcomeKind.Error, Array.Empty<SpeechTranscription>(), ErrorMessage: ex.Message);
			}
		}

		if (outcome == OutcomeError)
		{
			var (kind, status) = MapError(errorDomain, errorCode);
			return new SpeechUtteranceOutcome(kind, Array.Empty<SpeechTranscription>(), ErrorStatus: status, ErrorMessage: errorMessage);
		}

		return new SpeechUtteranceOutcome(SpeechUtteranceOutcomeKind.Canceled, Array.Empty<SpeechTranscription>());
	}

	private static unsafe string? ToManagedString(byte* value) => value is null ? null : Marshal.PtrToStringUTF8((nint)value);

	private static MacOSSpeechRecognizerExtension? Resolve(nint handle)
		=> _instances.TryGetValue(handle, out var instance) ? instance : null;

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe void OnHypothesis(nint handle, byte* text)
	{
		if (Resolve(handle) is { } instance)
		{
			instance.HypothesisGenerated?.Invoke(instance, ToManagedString(text) ?? string.Empty);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnSignal(nint handle, int signal)
	{
		if (Resolve(handle) is { } instance)
		{
			switch (signal)
			{
				case SignalSpeechDetected:
					instance.SpeechDetected?.Invoke(instance, EventArgs.Empty);
					break;

				case SignalFinishedReadingAudio:
					instance.AudioCaptureEnded?.Invoke(instance, EventArgs.Empty);
					break;
			}
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnAudioLevel(nint handle, float rmsDecibels, float peakDecibels)
	{
		if (Resolve(handle) is { } instance)
		{
			instance.AudioLevelChanged?.Invoke(instance, new SpeechAudioLevel(rmsDecibels, peakDecibels));
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe void OnUtteranceCompleted(nint handle, int outcome, byte* resultJson, byte* errorDomain, long errorCode, byte* errorMessage)
	{
		if (Resolve(handle) is not { } instance)
		{
			return;
		}

		var result = CreateOutcome(outcome, ToManagedString(resultJson), ToManagedString(errorDomain), errorCode, ToManagedString(errorMessage));
		if (result.Kind == SpeechUtteranceOutcomeKind.Error && instance.Log().IsEnabled(LogLevel.Debug))
		{
			instance.Log().Debug($"Speech recognition failed ({ToManagedString(errorDomain)} {errorCode}): {result.ErrorMessage}");
		}

		instance.UtteranceCompleted?.Invoke(instance, result);
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnAuthorization(long token, int speechStatus, int microphoneStatus)
	{
		if (!_authorizationRequests.TryRemove(token, out var completion))
		{
			return;
		}

		completion.TrySetResult(
			speechStatus == MissingUsageDescription || microphoneStatus == MissingUsageDescription ? SpeechRecognizerAuthorization.MissingUsageDescription
			: speechStatus == SpeechAuthorized && microphoneStatus == MicrophoneAuthorized ? SpeechRecognizerAuthorization.Authorized
			: SpeechRecognizerAuthorization.Denied);
	}
}
