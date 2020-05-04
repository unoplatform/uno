#if __ANDROID__ || __IOS__ || __WASM__
using Uno.Devices.Enumeration.Internal;

namespace Windows.Devices.Midi
{
	/// <summary>
	/// Represents a port used to receive MIDI messages from a MIDI device.
	/// </summary>
	public sealed partial  class MidiInPort : global::System.IDisposable
	{
		private readonly static string MidiInAqsFilter =
			"System.Devices.InterfaceClassGuid:=\"{" + DeviceClassGuids.MidiIn + "}\" AND System.Devices.InterfaceEnabled:=System.StructuredQueryType.Boolean#True";

		/// <summary>
		/// Gets a query string that can be used to enumerate all MidiInPort objects on the system.
		/// </summary>
		/// <returns>The query string used to enumerate the MidiInPort objects on the system.</returns>
		public static string GetDeviceSelector() => MidiInAqsFilter;
	}
}
#endif
