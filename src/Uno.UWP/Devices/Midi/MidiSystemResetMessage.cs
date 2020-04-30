using System;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	/// <summary>
	/// Represents a MIDI message that specifies a system reset.
	/// </summary>
	public partial class MidiSystemResetMessage : IMidiMessage
	{
		/// <summary>
		/// Creates a new MidiSystemResetMessage object.
		/// </summary>
		public MidiSystemResetMessage()
		{
			RawData = new InMemoryBuffer(new byte[]
			{
				(byte)Type
			});
		}

		/// <summary>
		/// Gets the type of this MIDI message.
		/// </summary>
		public MidiMessageType Type => MidiMessageType.SystemReset;

		/// <summary>
		/// Gets the array of bytes associated with the MIDI message, including status byte.
		/// </summary>
		public IBuffer RawData { get; }

		/// <summary>
		/// Gets the duration from when the MidiInPort was created to the time the message was received.
		/// For messages being sent to a MidiOutPort, this value has no meaning.
		/// </summary>
		public TimeSpan Timestamp { get; }
	}
}
