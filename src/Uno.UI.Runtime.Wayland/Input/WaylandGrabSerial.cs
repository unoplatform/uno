#nullable enable

using System.Threading;

namespace Uno.UI.Runtime.Wayland.Input;

// Dispatch thread. The serial of the implicit grab in progress on one window: the latest pointer button press
// or touch down, forgotten once that button or touch point is released, the pointer leaves, or the device goes
// away. wl_data_device.start_drag needs it, and the compositor ignores a serial whose grab already ended.
internal sealed class WaylandGrabSerial
{
	private (uint Serial, uint Button)? _button;
	private (uint Serial, int Id)? _touch;
	// Published for the UI thread, which only needs to know whether a grab is in progress; -1 when none.
	private long _current = -1;

	internal uint? Serial => Volatile.Read(ref _current) is >= 0 and var serial ? (uint)serial : null;

	internal void Press(uint serial, uint button)
	{
		_button = (serial, button);
		Publish();
	}

	internal void Release(uint button)
	{
		if (_button is { } press && press.Button == button)
		{
			_button = null;
			Publish();
		}
	}

	internal void TouchDown(uint serial, int id)
	{
		_touch = (serial, id);
		Publish();
	}

	internal void TouchUp(int id)
	{
		if (_touch is { } touch && touch.Id == id)
		{
			_touch = null;
			Publish();
		}
	}

	internal void ClearPointer()
	{
		_button = null;
		Publish();
	}

	internal void ClearTouch()
	{
		_touch = null;
		Publish();
	}

	private void Publish() => Volatile.Write(ref _current, _button?.Serial ?? _touch?.Serial ?? -1L);
}
