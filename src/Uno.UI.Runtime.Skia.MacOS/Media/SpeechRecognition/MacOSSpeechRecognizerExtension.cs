#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Windows.Globalization;
using Windows.Media.SpeechRecognition;

namespace Uno.UI.Runtime.Skia.MacOS;

internal sealed class MacOSSpeechRecognizerExtension : ISpeechRecognizerExtension
{
	private static readonly ConcurrentDictionary<nint, MacOSSpeechRecognizerExtension> _instances = new();

	private nint _nativeHandle;
	private TaskCompletionSource<(string text, IReadOnlyList<string> alternates)>? _pendingRecognition;
	private bool _disposed;

	public event Action<string>? HypothesisGenerated;
	public event Action<SpeechRecognizerState>? StateChanged;

	private MacOSSpeechRecognizerExtension(object owner)
	{
		// `owner` is the SpeechRecognizer instance; we don't need it directly — language is supplied via Initialize().
	}

	public static unsafe void Register()
	{
		NativeUno.uno_speech_set_callbacks(
			hypothesis: &OnHypothesis,
			state: &OnState,
			result: &OnResult,
			error: &OnError);

		ApiExtensibility.Register(typeof(ISpeechRecognizerExtension), o => new MacOSSpeechRecognizerExtension(o));
	}

	public void Initialize(Language language, SpeechRecognizerTimeouts timeouts)
	{
		if (_nativeHandle != 0)
		{
			NativeUno.uno_speech_recognizer_destroy(_nativeHandle);
			_instances.TryRemove(_nativeHandle, out _);
			_nativeHandle = 0;
		}

		// Apple expects locale identifiers like "en-US"; SpeechRecognizer ctor seeds CurrentLanguage from CultureInfo.CurrentCulture.Name
		// which is already in BCP-47 form, so passing LanguageTag directly is correct.
		var localeTag = language?.LanguageTag ?? CultureInfo.CurrentCulture.Name;
		_nativeHandle = NativeUno.uno_speech_recognizer_create(localeTag);
		if (_nativeHandle == 0)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Speech recognition is not available for locale '{localeTag}' on this macOS version.");
			}
			return;
		}

		_instances[_nativeHandle] = this;
	}

	public Task<(string text, IReadOnlyList<string> alternates)> RecognizeAsync()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(nameof(MacOSSpeechRecognizerExtension));
		}
		if (_nativeHandle == 0)
		{
			throw new InvalidOperationException("Speech recognition is not available on this macOS version.");
		}

		var tcs = new TaskCompletionSource<(string text, IReadOnlyList<string> alternates)>(TaskCreationOptions.RunContinuationsAsynchronously);

		// Replace any previous pending recognition with this one (last-writer-wins, matches iOS behavior of cancelling prior task).
		var previous = Interlocked.Exchange(ref _pendingRecognition, tcs);
		previous?.TrySetCanceled();

		if (!NativeUno.uno_speech_recognizer_start(_nativeHandle))
		{
			Interlocked.CompareExchange(ref _pendingRecognition, null, tcs);
			tcs.TrySetException(new InvalidOperationException("Failed to start speech recognition. See logs for details."));
		}

		return tcs.Task;
	}

	public Task StopAsync()
	{
		if (_nativeHandle != 0)
		{
			NativeUno.uno_speech_recognizer_stop(_nativeHandle);
		}
		return Task.CompletedTask;
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;

		var handle = Interlocked.Exchange(ref _nativeHandle, 0);
		if (handle != 0)
		{
			_instances.TryRemove(handle, out _);
			NativeUno.uno_speech_recognizer_destroy(handle);
		}

		var pending = Interlocked.Exchange(ref _pendingRecognition, null);
		pending?.TrySetCanceled();
	}

	private static MacOSSpeechRecognizerExtension? Resolve(nint handle)
		=> _instances.TryGetValue(handle, out var instance) ? instance : null;

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe void OnHypothesis(nint handle, byte* textUtf8)
	{
		var instance = Resolve(handle);
		if (instance is null)
		{
			return;
		}
		var text = Marshal.PtrToStringUTF8((nint)textUtf8) ?? string.Empty;
		try
		{
			instance.HypothesisGenerated?.Invoke(text);
		}
		catch (Exception ex)
		{
			if (instance.Log().IsEnabled(LogLevel.Error))
			{
				instance.Log().Error("HypothesisGenerated handler threw", ex);
			}
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnState(nint handle, int state)
	{
		var instance = Resolve(handle);
		if (instance is null)
		{
			return;
		}
		try
		{
			instance.StateChanged?.Invoke((SpeechRecognizerState)state);
		}
		catch (Exception ex)
		{
			if (instance.Log().IsEnabled(LogLevel.Error))
			{
				instance.Log().Error("StateChanged handler threw", ex);
			}
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe void OnResult(nint handle, byte* textUtf8, byte* alternatesUtf8)
	{
		var instance = Resolve(handle);
		if (instance is null)
		{
			return;
		}
		var text = Marshal.PtrToStringUTF8((nint)textUtf8) ?? string.Empty;
		IReadOnlyList<string> alternates;
		if (alternatesUtf8 != null)
		{
			var joined = Marshal.PtrToStringUTF8((nint)alternatesUtf8) ?? string.Empty;
			alternates = joined.Length == 0
				? Array.Empty<string>()
				: joined.Split('\x1F');
		}
		else
		{
			alternates = Array.Empty<string>();
		}

		var pending = Interlocked.Exchange(ref instance._pendingRecognition, null);
		pending?.TrySetResult((text, alternates));
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe void OnError(nint handle, byte* messageUtf8)
	{
		var instance = Resolve(handle);
		if (instance is null)
		{
			return;
		}
		var message = Marshal.PtrToStringUTF8((nint)messageUtf8) ?? "Unknown speech recognition error";

		var pending = Interlocked.Exchange(ref instance._pendingRecognition, null);
		pending?.TrySetException(new Exception($"Error during speech recognition: {message}"));
	}
}
