using System;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	/// <summary>
	/// Represents a MIDI message which is implemented by all MIDI message classes.
	/// </summary>
	public partial interface IMidiMessage
	{
		/// <summary>
		/// Gets the array of bytes associated with the MIDI message, including status byte.
		/// </summary>
		IBuffer RawData { get; }

		/// <summary>
		/// Gets the duration from when the MidiInPort was created to the time the message was received.
		/// For messages being sent to a MidiOutPort, this value has no meaning.
		/// </summary>
		TimeSpan Timestamp { get; }

		/// <summary>
		/// Gets the type of this MIDI message.
		/// </summary>
		MidiMessageType Type { get; }
	}
}
