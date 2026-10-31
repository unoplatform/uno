#nullable enable

namespace Uno.UI.Runtime.Wayland;

// Updated on the dispatch thread. Tracks keyboard focus and the latest input serial that
// wl_data_device.set_selection requires. Any serial value, including zero after wrapping, is valid once focus
// was entered.
internal sealed class WaylandSelectionSerial
{
	private volatile bool _hasFocus;

	// Read from any thread.
	internal bool HasFocus => _hasFocus;

	internal uint Serial { get; private set; }

	internal void Enter(uint serial)
	{
		_hasFocus = true;
		Serial = serial;
	}

	internal void Update(uint serial)
	{
		if (_hasFocus)
		{
			Serial = serial;
		}
	}

	internal void Leave() => _hasFocus = false;
}
