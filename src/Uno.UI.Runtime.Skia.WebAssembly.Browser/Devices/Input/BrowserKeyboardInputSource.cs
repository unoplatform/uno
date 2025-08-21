using System;
using System.Runtime.InteropServices.JavaScript;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Uno.Foundation.Logging;
using Uno.UI.Xaml;
using Uno.UI.Dispatching;

namespace Uno.UI.Runtime.Skia;

internal partial class BrowserKeyboardInputSource : IUnoKeyboardInputSource
{
	private static readonly Logger _log = typeof(BrowserPointerInputSource).Log();

	public event TypedEventHandler<object, KeyEventArgs>? KeyDown;
	public event TypedEventHandler<object, KeyEventArgs>? KeyUp;

	[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserKeyboardInputSource.initialize")]
	private static partial void Initialize([JSMarshalAs<JSType.Any>] object inputSource);

	public BrowserKeyboardInputSource()
	{
		Initialize(this);
	}

	[JSExport]
	private static byte OnNativeKeyboardEvent(
		[JSMarshalAs<JSType.Any>] object inputSource,
		bool down,
		bool ctrl,
		bool shift,
		bool alt,
		bool meta,
		string code,
		string key)
	{
		if (_log.IsEnabled(LogLevel.Debug))
		{
			_log.Debug($"Native Keyboard Event: down={down}, ctrl={ctrl}, shift={shift}, meta={meta}, code={code}, key=${key}");
		}

		// Ensure that the async context is set properly, since we're raising
		// events from outside the dispatcher.
		using var syncContextScope = NativeDispatcher.Main.SynchronizationContext.Apply();

		var args = new KeyEventArgs(
			"keyboard",
			BrowserVirtualKeyHelper.FromCode(code),
			(shift ? VirtualKeyModifiers.Shift : VirtualKeyModifiers.None) | (ctrl ? VirtualKeyModifiers.Control : VirtualKeyModifiers.None) | (meta ? VirtualKeyModifiers.Windows : VirtualKeyModifiers.None) | (alt ? VirtualKeyModifiers.Menu : VirtualKeyModifiers.None),
			new CorePhysicalKeyStatus
			{
				ScanCode = 0, // not implemented
				RepeatCount = 1,
			},

			unicodeKey: GetUnicodeKey(key));

		if (down)
		{
			((BrowserKeyboardInputSource)inputSource).KeyDown?.Invoke(inputSource, args);
		}
		else
		{
			((BrowserKeyboardInputSource)inputSource).KeyUp?.Invoke(inputSource, args);
		}

		return (byte)(args.Handled ? HtmlEventDispatchResult.PreventDefault : HtmlEventDispatchResult.Ok);
	}

	private static char? GetUnicodeKey(string key)
	{
		// From https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/key
		// We heuristically decide that if we're given a length-1 key, that it's an actual unicode character and not a
		// description of the key.
		if (key.Length == 1)
		{
			return key[0];
		}

		// If the key is not a single character, we can try to convert
		switch (key.ToLowerInvariant())
		{
			case "enter":
				return '\r';
			default:
				return null;
		}
	}
}
