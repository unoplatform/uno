#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Windows.Globalization;
using Windows.Media.SpeechRecognition;
using static Uno.UI.Runtime.Win32.WinRTInterop;

namespace Uno.UI.Runtime.Win32;

// Backs SpeechRecognizer with the OS Windows.Media.SpeechRecognition engine, the one WinUI apps use.
// The WinRT recognizer is agile, so its interface pointers are used from whichever thread calls in.
internal sealed class Win32SpeechRecognizerExtension : ISpeechRecognizerExtension
{
	private const string SpeechRecognizerClassId = "Windows.Media.SpeechRecognition.SpeechRecognizer";
	private const string LanguageClassId = "Windows.Globalization.Language";
	private const uint MaxAlternates = 5;

	private static readonly Guid IID_ILanguageFactory = new("9b0252ac-0c27-44f8-b792-9793fb66c63e");
	private static readonly Guid IID_ISpeechRecognizerFactory = new("60c488dd-7fb8-4033-ac70-d046f64818e1");
	private static readonly Guid IID_ISpeechRecognizer2 = new("63c9baf1-91e3-4ea4-86a1-7c3867d084a6");
	private static readonly Guid IID_AsyncActionCompletedHandler = new("a4ed5c81-76c9-40bd-8be6-b1d90fb20ae7");

	// Parameterized IIDs, derived from these WinRT signatures:
	// pinterface({fcdcf02c-e5d8-4478-915a-4d90b74b83a5};rc(Windows.Media.SpeechRecognition.SpeechRecognitionResult;{4e303157-034e-4652-857e-d0454cc4beec}))
	private static readonly Guid IID_RecognitionCompletedHandler = new("c2195c7d-dcc2-5c6d-9162-c8df66528762");
	// pinterface({fcdcf02c-e5d8-4478-915a-4d90b74b83a5};rc(Windows.Media.SpeechRecognition.SpeechRecognitionCompilationResult;{407e6c5d-6ac7-4da4-9cc1-2fce32cf7489}))
	private static readonly Guid IID_CompilationCompletedHandler = new("78c859bd-14d4-5c40-abff-490616d5e92d");
	// pinterface({9de1c534-6ae1-11e0-84e1-18a905bcc53f};rc(Windows.Media.SpeechRecognition.SpeechRecognizer;{0bc3c9cb-c26a-40f2-aeb5-8096b2e48073});rc(Windows.Media.SpeechRecognition.SpeechRecognizerStateChangedEventArgs;{563d4f09-ba03-4bad-ad81-ddc6c4dab0c3}))
	private static readonly Guid IID_StateChangedHandler = new("d1185e92-5c30-5561-b3e2-e82ddbd872c3");
	// pinterface({9de1c534-6ae1-11e0-84e1-18a905bcc53f};rc(Windows.Media.SpeechRecognition.SpeechRecognizer;{0bc3c9cb-c26a-40f2-aeb5-8096b2e48073});rc(Windows.Media.SpeechRecognition.SpeechRecognitionHypothesisGeneratedEventArgs;{55161a7a-8023-5866-411d-1213bb271476}))
	private static readonly Guid IID_HypothesisGeneratedHandler = new("4cb45aba-7573-545a-b29a-e9be35bd4682");

	// Vtable slots; 0-5 are IUnknown and IInspectable.
	private const int Slot_ILanguageFactory_CreateLanguage = 6;
	private const int Slot_ISpeechRecognizerFactory_Create = 6;
	private const int Slot_ISpeechRecognizer_get_Timeouts = 8;
	private const int Slot_ISpeechRecognizer_CompileConstraintsAsync = 10;
	private const int Slot_ISpeechRecognizer_RecognizeAsync = 11;
	private const int Slot_ISpeechRecognizer_add_StateChanged = 15;
	private const int Slot_ISpeechRecognizer_remove_StateChanged = 16;
	private const int Slot_ISpeechRecognizer2_StopRecognitionAsync = 8;
	private const int Slot_ISpeechRecognizer2_add_HypothesisGenerated = 9;
	private const int Slot_ISpeechRecognizer2_remove_HypothesisGenerated = 10;
	private const int Slot_ISpeechRecognizerTimeouts_put_InitialSilenceTimeout = 7;
	private const int Slot_ISpeechRecognizerTimeouts_put_EndSilenceTimeout = 9;
	private const int Slot_ISpeechRecognizerTimeouts_put_BabbleTimeout = 11;
	private const int Slot_ISpeechRecognitionCompilationResult_get_Status = 6;
	private const int Slot_ISpeechRecognitionResult_get_Status = 6;
	private const int Slot_ISpeechRecognitionResult_get_Text = 7;
	private const int Slot_ISpeechRecognitionResult_GetAlternates = 10;
	private const int Slot_IVectorView_GetAt = 6;
	private const int Slot_IVectorView_get_Size = 7;
	private const int Slot_ISpeechRecognizerStateChangedEventArgs_get_State = 6;
	private const int Slot_ISpeechRecognitionHypothesisGeneratedEventArgs_get_Hypothesis = 6;
	private const int Slot_ISpeechRecognitionHypothesis_get_Text = 6;

	private readonly object _gate = new();
	private nint _recognizer; // ISpeechRecognizer
	private nint _recognizer2; // ISpeechRecognizer2
	private long _stateChangedToken;
	private long _hypothesisGeneratedToken;
	private Exception? _initializationError;
	private SpeechRecognizerTimeouts? _timeouts;
	private bool _constraintsCompiled;
	private bool _disposed;

	public event EventHandler<string>? HypothesisGenerated;

	public event EventHandler<SpeechRecognizerState>? StateChanged;

	public void Initialize(Language language, SpeechRecognizerTimeouts timeouts)
	{
		ReleaseRecognizer();

		_timeouts = timeouts;
		_initializationError = null;
		_constraintsCompiled = false;

		var languageTag = language?.LanguageTag ?? CultureInfo.CurrentCulture.Name;
		try
		{
			_recognizer = CreateRecognizer(languageTag);
			_recognizer2 = QueryInterface(_recognizer, IID_ISpeechRecognizer2);
			_stateChangedToken = Subscribe(_recognizer, Slot_ISpeechRecognizer_add_StateChanged, WinRTDelegate.CreateEventHandler(IID_StateChangedHandler, OnNativeStateChanged));
			_hypothesisGeneratedToken = Subscribe(_recognizer2, Slot_ISpeechRecognizer2_add_HypothesisGenerated, WinRTDelegate.CreateEventHandler(IID_HypothesisGeneratedHandler, OnNativeHypothesisGenerated));
		}
		catch (Exception e)
		{
			ReleaseRecognizer();
			_initializationError = e;

			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Speech recognition is not available for '{languageTag}' (0x{e.HResult:X8}): {e.Message}");
			}
		}
	}

	public async Task<(string text, IReadOnlyList<string> alternates)> RecognizeAsync()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		// Own a reference for the whole operation so a concurrent Dispose cannot free the recognizer under us.
		var recognizer = AcquireReference(ref _recognizer);
		if (recognizer == 0)
		{
			throw new InvalidOperationException("Speech recognition is not available on this system.", _initializationError);
		}

		try
		{
			ApplyTimeouts(recognizer);

			if (!_constraintsCompiled)
			{
				var status = await CompileConstraintsAsync(recognizer);
				if (status != SpeechRecognitionResultStatus.Success)
				{
					throw new InvalidOperationException($"Speech recognition constraints could not be compiled ({status}).");
				}

				_constraintsCompiled = true;
			}

			var operation = GetPointer(recognizer, Slot_ISpeechRecognizer_RecognizeAsync);
			nint result;
			try
			{
				result = await AwaitAsync(operation, IID_RecognitionCompletedHandler, hasResults: true);
			}
			catch (OperationCanceledException) when (!_disposed)
			{
				return (string.Empty, Array.Empty<string>());
			}
			finally
			{
				Release(operation);
			}

			try
			{
				return ReadResult(result);
			}
			finally
			{
				Release(result);
			}
		}
		finally
		{
			Release(recognizer);
		}
	}

	public async Task StopAsync()
	{
		var recognizer2 = AcquireReference(ref _recognizer2);
		if (recognizer2 == 0)
		{
			return;
		}

		try
		{
			var action = GetPointer(recognizer2, Slot_ISpeechRecognizer2_StopRecognitionAsync);
			try
			{
				await AwaitAsync(action, IID_AsyncActionCompletedHandler, hasResults: false);
			}
			finally
			{
				Release(action);
			}
		}
		catch (Exception e)
		{
			// WinRT rejects a stop while no session is active; the other platforms treat that as a no-op.
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Stopping speech recognition had no effect: {e.Message}");
			}
		}
		finally
		{
			Release(recognizer2);
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		ReleaseRecognizer();
	}

	private static nint CreateRecognizer(string languageTag)
	{
		nint languageFactory = 0, language = 0, recognizerFactory = 0;
		try
		{
			languageFactory = GetActivationFactory(LanguageClassId, IID_ILanguageFactory);

			var tag = CreateHString(languageTag);
			try
			{
				language = GetPointer(languageFactory, Slot_ILanguageFactory_CreateLanguage, tag);
			}
			finally
			{
				DeleteHString(tag);
			}

			recognizerFactory = GetActivationFactory(SpeechRecognizerClassId, IID_ISpeechRecognizerFactory);
			return GetPointer(recognizerFactory, Slot_ISpeechRecognizerFactory_Create, language);
		}
		finally
		{
			Release(recognizerFactory);
			Release(language);
			Release(languageFactory);
		}
	}

	private static long Subscribe(nint source, int addSlot, nint handler)
	{
		try
		{
			return AddEventHandler(source, addSlot, handler);
		}
		finally
		{
			// The event source holds its own reference until the handler is removed.
			Release(handler);
		}
	}

	private void ApplyTimeouts(nint recognizer)
	{
		if (_timeouts is not { } timeouts)
		{
			return;
		}

		var nativeTimeouts = GetPointer(recognizer, Slot_ISpeechRecognizer_get_Timeouts);
		try
		{
			// Uno's timeouts default to zero; forward only values the app set so the engine keeps its own defaults.
			SetIfPositive(Slot_ISpeechRecognizerTimeouts_put_InitialSilenceTimeout, timeouts.InitialSilenceTimeout);
			SetIfPositive(Slot_ISpeechRecognizerTimeouts_put_EndSilenceTimeout, timeouts.EndSilenceTimeout);
			SetIfPositive(Slot_ISpeechRecognizerTimeouts_put_BabbleTimeout, timeouts.BabbleTimeout);
		}
		finally
		{
			Release(nativeTimeouts);
		}

		void SetIfPositive(int slot, TimeSpan value)
		{
			if (value > TimeSpan.Zero)
			{
				SetInt64(nativeTimeouts, slot, value.Ticks);
			}
		}
	}

	private static async Task<SpeechRecognitionResultStatus> CompileConstraintsAsync(nint recognizer)
	{
		var operation = GetPointer(recognizer, Slot_ISpeechRecognizer_CompileConstraintsAsync);
		nint result;
		try
		{
			result = await AwaitAsync(operation, IID_CompilationCompletedHandler, hasResults: true);
		}
		finally
		{
			Release(operation);
		}

		try
		{
			return (SpeechRecognitionResultStatus)GetInt32(result, Slot_ISpeechRecognitionCompilationResult_get_Status);
		}
		finally
		{
			Release(result);
		}
	}

	private static (string text, IReadOnlyList<string> alternates) ReadResult(nint result)
	{
		var status = (SpeechRecognitionResultStatus)GetInt32(result, Slot_ISpeechRecognitionResult_get_Status);
		switch (status)
		{
			case SpeechRecognitionResultStatus.Success:
				return (GetString(result, Slot_ISpeechRecognitionResult_get_Text), ReadAlternates(result));

			// Outcomes WinUI reports through Status rather than an exception: no speech, or stopped by the app.
			case SpeechRecognitionResultStatus.TimeoutExceeded:
			case SpeechRecognitionResultStatus.UserCanceled:
			case SpeechRecognitionResultStatus.PauseLimitExceeded:
				return (GetString(result, Slot_ISpeechRecognitionResult_get_Text), Array.Empty<string>());

			default:
				throw new InvalidOperationException($"Speech recognition failed ({status}).");
		}
	}

	private static IReadOnlyList<string> ReadAlternates(nint result)
	{
		var alternates = GetPointer(result, Slot_ISpeechRecognitionResult_GetAlternates, MaxAlternates);
		if (alternates == 0)
		{
			return Array.Empty<string>();
		}

		try
		{
			var count = (uint)GetInt32(alternates, Slot_IVectorView_get_Size);
			var texts = new string[count];
			for (uint i = 0; i < count; i++)
			{
				var alternate = GetPointer(alternates, Slot_IVectorView_GetAt, i);
				try
				{
					texts[i] = GetString(alternate, Slot_ISpeechRecognitionResult_get_Text);
				}
				finally
				{
					Release(alternate);
				}
			}

			return texts;
		}
		finally
		{
			Release(alternates);
		}
	}

	private void OnNativeStateChanged(nint sender, nint args)
	{
		var state = (SpeechRecognizerState)GetInt32(args, Slot_ISpeechRecognizerStateChangedEventArgs_get_State);
		StateChanged?.Invoke(this, state);
	}

	private void OnNativeHypothesisGenerated(nint sender, nint args)
	{
		var hypothesis = GetPointer(args, Slot_ISpeechRecognitionHypothesisGeneratedEventArgs_get_Hypothesis);
		try
		{
			HypothesisGenerated?.Invoke(this, GetString(hypothesis, Slot_ISpeechRecognitionHypothesis_get_Text));
		}
		finally
		{
			Release(hypothesis);
		}
	}

	private nint AcquireReference(ref nint field)
	{
		lock (_gate)
		{
			var pointer = field;
			if (pointer != 0)
			{
				AddRef(pointer);
			}

			return pointer;
		}
	}

	private void ReleaseRecognizer()
	{
		nint recognizer, recognizer2;
		lock (_gate)
		{
			recognizer = _recognizer;
			recognizer2 = _recognizer2;
			_recognizer = 0;
			_recognizer2 = 0;
		}

		if (recognizer2 != 0)
		{
			TryRun(() => RemoveEventHandler(recognizer2, Slot_ISpeechRecognizer2_remove_HypothesisGenerated, _hypothesisGeneratedToken));
			Release(recognizer2);
		}

		if (recognizer != 0)
		{
			TryRun(() => RemoveEventHandler(recognizer, Slot_ISpeechRecognizer_remove_StateChanged, _stateChangedToken));

			// Close ends any in-flight recognition, which then completes as canceled.
			TryRun(() => Close(recognizer));
			Release(recognizer);
		}

		_stateChangedToken = 0;
		_hypothesisGeneratedToken = 0;

		void TryRun(Action action)
		{
			try
			{
				action();
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Releasing the speech recognizer: {e.Message}");
				}
			}
		}
	}
}
