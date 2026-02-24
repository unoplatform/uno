#nullable enable

using System.Threading.Tasks;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// Provides helpers for controlling browser input behavior in WebAssembly Skia apps.
/// </summary>
public static partial class BrowserInputHelper
{
	private static bool _isBrowserZoomEnabled = true;

	/// <summary>
	/// Gets or sets a value indicating whether browser zoom via Ctrl+wheel is enabled.
	/// When set to <c>false</c>, the Skia pointer handler will call <c>preventDefault()</c>
	/// on Ctrl+wheel events, blocking the browser's built-in zoom while still delivering
	/// the event to the Uno pointer pipeline.
	/// Default is <c>true</c>.
	/// </summary>
	public static bool IsBrowserZoomEnabled
	{
		get => _isBrowserZoomEnabled;
		set
		{
			if (_isBrowserZoomEnabled == value)
			{
				return;
			}

			_isBrowserZoomEnabled = value;
			NativeMethods.SetBrowserZoomEnabled(value);
		}
	}

	/// <summary>
	/// Gets a value indicating whether the browser supports the Keyboard Lock API
	/// (<c>navigator.keyboard</c>). Returns <c>true</c> on Chromium-based browsers
	/// over HTTPS, <c>false</c> otherwise.
	/// </summary>
	public static bool IsKeyboardLockSupported => NativeMethods.IsKeyboardLockSupported();

	/// <summary>
	/// Locks the specified keyboard keys using the browser Keyboard Lock API.
	/// When locked, these keys are delivered to the app instead of being
	/// intercepted by the browser or OS.
	/// Requires HTTPS. Not supported in all browsers (Chrome/Edge).
	/// </summary>
	/// <param name="keyCodes">
	/// Browser key codes in <c>KeyboardEvent.code</c> format
	/// (e.g. <c>"KeyW"</c>, <c>"F5"</c>, <c>"Escape"</c>).
	/// If empty, all keys are locked.
	/// </param>
	/// <returns>A task that completes when the keys are locked.</returns>
	public static Task LockKeysAsync(params string[] keyCodes)
		=> NativeMethods.LockKeys(keyCodes);

	/// <summary>
	/// Unlocks all previously locked keys, restoring default browser key handling.
	/// </summary>
	public static void UnlockKeys()
		=> NativeMethods.UnlockKeys();
}
