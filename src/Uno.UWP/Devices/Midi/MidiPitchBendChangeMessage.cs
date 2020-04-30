using System;
using Uno.Devices.Midi.Internal;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	/// <summary>
	/// Represents a MIDI message that specifies a pitch bend change.
	/// </summary>
	public partial class MidiPitchBendChangeMessage : IMidiMessage
	{
		/// <summary>
		/// Creates a new MidiPitchBendChangeMessage object.
		/// </summary>
		/// <param name="channel">Channel.</param>
		/// <param name="bend">Bend.</param>
		public MidiPitchBendChangeMessage(byte channel, ushort bend)
		{
			MidiMessageValidators.VerifyRange(channel, 15, nameof(channel));
			MidiMessageValidators.VerifyRange(bend, 16383, nameof(bend));

			Channel = channel;
			Bend = bend;
			RawData = new InMemoryBuffer(new byte[]
			{
				(byte)((byte)Type | Channel),
				(byte)(bend & 0b0111_1111),
				(byte)(bend >> 7)
			});
		}

		/// <summary>
		/// Gets the type of this MIDI message.
		/// </summary>
		public MidiMessageType Type => MidiMessageType.PitchBendChange;

		/// <summary>
		/// Gets the array of bytes associated with the MIDI message, including status byte.
		/// </summary>
		public IBuffer RawData { get; }

		/// <summary>
		/// Gets the duration from when the MidiInPort was created to the time the message was received.
		/// For messages being sent to a MidiOutPort, this value has no meaning.
		/// </summary>
		public TimeSpan Timestamp { get; } = TimeSpan.Zero;

		/// <summary>
		/// Gets the channel from 0-15 that this message applies to.
		/// </summary>
		public byte Channel { get; }

		/// <summary>
		/// Gets the pitch bend value which is specified as a 14-bit value from 0-16383.
		/// </summary>
		public ushort Bend { get; }
	}
}
