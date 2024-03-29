using System;
using System.Runtime.InteropServices.JavaScript;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Uno.Foundation.Logging;

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
	private static bool OnNativeKeyboardEvent(
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

		var virtualKey = BrowserVirtualKeyHelper.FromCode(code);
		if (down && !ctrl && !shift && !alt && !meta && virtualKey == VirtualKey.Tab && WebAssemblyWindowWrapper.Instance.IsAccessibilityEnabled)
		{
			// Let the browser manages tab navigation for accessibility.
			return false;
		}

		var args = new KeyEventArgs(
			"keyboard",
			virtualKey,
			(shift ? VirtualKeyModifiers.Shift : VirtualKeyModifiers.None) | (ctrl ? VirtualKeyModifiers.Control : VirtualKeyModifiers.None) | (meta ? VirtualKeyModifiers.Windows : VirtualKeyModifiers.None) | (alt ? VirtualKeyModifiers.Menu : VirtualKeyModifiers.None),
			new CorePhysicalKeyStatus
			{
				ScanCode = 0, // not implemented
				RepeatCount = 1,
			},
			// From https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/key
			// We heuristically decide that if we're given a length-1 key, that it's an actual unicode character and not a
			// description of the key.
			unicodeKey: key.Length == 1 ? key[0] : null);

		if (down)
		{
			((BrowserKeyboardInputSource)inputSource).KeyDown?.Invoke(inputSource, args);
		}
		else
		{
			((BrowserKeyboardInputSource)inputSource).KeyUp?.Invoke(inputSource, args);
		}

		return args.Handled;
	}
}
