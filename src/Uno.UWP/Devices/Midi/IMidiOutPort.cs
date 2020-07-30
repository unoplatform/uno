#if !NET461 && !__SKIA__ && !__NETSTD_REFERENCE__
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	/// <summary>
	/// Represents a single MIDI out port.
	/// </summary>
	public partial interface IMidiOutPort : global::System.IDisposable
	{
		/// <summary>
		/// Gets the ID of the device that contains the MIDI out port.
		/// </summary>
		string DeviceId { get; }

		/// <summary>
		/// Send the data in the specified MIDI message to the device associated with this MidiOutPort.
		/// </summary>
		/// <param name="midiMessage">The MIDI message to send to the device.</param>
		void SendMessage(IMidiMessage midiMessage);

		/// <summary>
		/// Sends the contents of the buffer through the MIDI out port.
		/// </summary>
		/// <param name="midiData">The data to send to the device.</param>
		void SendBuffer(IBuffer midiData);
	}
}
#endif
