using System;
using Uno.Devices.Midi.Internal;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	/// <summary>
	/// Represents a MIDI message that specifies a MIDI note to turn on.
	/// </summary>
	public partial class MidiNoteOnMessage : IMidiMessage
	{
		/// <summary>
		/// Creates a new MidiNoteOnMessage object.
		/// </summary>
		/// <param name="channel">The channel from 0-15 that this message applies to.</param>
		/// <param name="note">The note which is specified as a value from 0-127.</param>
		/// <param name="velocity">The velocity which is specified as a value from 0-127.</param>
		public MidiNoteOnMessage(byte channel, byte note, byte velocity)
		{
			MidiMessageValidators.VerifyRange(channel, 15, nameof(channel));
			MidiMessageValidators.VerifyRange(note, 127, nameof(note));
			MidiMessageValidators.VerifyRange(velocity, 127, nameof(velocity));

			Channel = channel;
			Note = note;
			Velocity = velocity;
			RawData = new InMemoryBuffer(new byte[]
			{
				(byte)((byte)Type | Channel),
				Note,
				Velocity
			});
		}

		public MidiMessageType Type => MidiMessageType.NoteOn;

		public byte Channel { get; }

		public byte Note { get; }

		public byte Velocity { get; }

		public IBuffer RawData { get; }

		public TimeSpan Timestamp { get; } = TimeSpan.Zero;
	}
}
