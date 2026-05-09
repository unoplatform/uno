using System;
using System.Threading.Tasks;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Abstraction for X11 input method backends (IBus D-Bus, Fcitx D-Bus, XIM fallback).
/// Consumed by <see cref="X11KeyboardInputSource"/> and <see cref="X11ImeTextBoxExtension"/>.
/// </summary>
internal interface IX11InputMethod : IDisposable
{
	/// <summary>
	/// Whether this input method is connected and ready to process key events.
	/// </summary>
	bool IsEnabled { get; }

	/// <summary>
	/// Forward a key event to the IME for processing.
	/// Returns true if the IME consumed the event (do not dispatch KeyDown/KeyUp).
	/// </summary>
	Task<bool> HandleKeyEventAsync(uint keyVal, uint keyCode, uint state, bool isRelease);

	/// <summary>
	/// Set the cursor location for the IME candidate window (absolute screen pixels).
	/// </summary>
	void SetCursorLocation(int x, int y, int w, int h);

	/// <summary>
	/// Notify the IME of focus changes.
	/// </summary>
	void SetFocus(bool active);

	/// <summary>
	/// Reset the IME state (e.g., clear preedit).
	/// </summary>
	void Reset();

	/// <summary>
	/// Fired when the IME commits text (e.g., Chinese character after Pinyin composition).
	/// </summary>
	event Action<string> Commit;

	/// <summary>
	/// Fired when the IME forwards a key event back to the application.
	/// Parameters: keyval, keycode, state.
	/// </summary>
	event Action<uint, uint, uint> ForwardKey;

	/// <summary>
	/// Fired when the preedit (composition) text changes.
	/// Parameters: preedit text (null if cleared), cursor position.
	/// </summary>
	event Action<string?, int> PreeditChanged;
}
