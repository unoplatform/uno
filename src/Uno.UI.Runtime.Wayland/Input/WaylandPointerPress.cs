#nullable enable

using System;

namespace Uno.UI.Runtime.Wayland.Input;

// Pairs a wl_pointer.button serial with the XAML press it produced. The serial is queued to the UI thread
// just before the press event, so each press reads its own serial even when several are pending.
internal sealed class WaylandPointerPress
{
	private readonly Action<Action> _post;

	// UI thread.
	private (uint Serial, uint Button)? _press;

	internal WaylandPointerPress(Action<Action> post) => _post = post;

	// Dispatch thread.
	internal void Queue(uint serial, uint button) => _post(() => _press = (serial, button));

	// UI thread, while routing the press queued right after the serial.
	internal uint? Take(uint button)
	{
		if (_press is not { } press || press.Button != button)
		{
			return null;
		}
		_press = null;
		return press.Serial;
	}
}
