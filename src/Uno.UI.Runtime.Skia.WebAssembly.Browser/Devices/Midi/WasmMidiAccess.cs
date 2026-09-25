#nullable enable

using System.Threading.Tasks;

namespace Uno.Devices.Midi;

/// <summary>
/// Requests access to MIDI devices through the browser's Web MIDI API.
/// </summary>
public static class WasmMidiAccess
{
	/// <summary>
	/// Requests MIDI access up front, e.g. from a user gesture. Access is otherwise requested
	/// on first device enumeration or port use. Includes system-exclusive messages when
	/// <see cref="WinRTFeatureConfiguration.Midi.RequestSystemExclusiveAccess"/> is set.
	/// </summary>
	/// <returns><see langword="true"/> if the browser granted access.</returns>
	public static Task<bool> RequestAsync() => Internal.WasmMidiAccess.RequestAsync();
}
