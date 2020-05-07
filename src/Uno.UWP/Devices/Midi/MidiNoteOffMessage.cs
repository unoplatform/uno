using System;
using Uno.Devices.Midi.Internal;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	/// <summary>
	/// Represents a MIDI messages that specifies a MIDI note to turn off.
	/// </summary>
	public partial class MidiNoteOffMessage : IMidiMessage
	{
		private readonly InMemoryBuffer _buffer;

		/// <summary>
		/// Creates a new MidiNoteOffMessage object.
		/// </summary>
		/// <param name="channel">The channel from 0-15 that this message applies to.</param>
		/// <param name="note">The note which is specified as a value from 0-127.</param>
		/// <param name="velocity">The velocity which is specified as a value from 0-127.</param>
		public MidiNoteOffMessage(byte channel, byte note, byte velocity)
			: this(new byte[] {
				(byte)((byte)MidiMessageType.NoteOff| channel),
				note,
				velocity
			})
		{
		}

		internal MidiNoteOffMessage(byte[] rawData)
		{
			if ((rawData[0] & (byte)MidiMessageType.NoteOff) == (byte)MidiMessageType.NoteOff)
			{

			}
				MidiMessageValidators.VerifyRange(channel, 15, nameof(channel));
			MidiMessageValidators.VerifyRange(note, 127, nameof(note));
			MidiMessageValidators.VerifyRange(velocity, 127, nameof(velocity));

			_buffer = new InMemoryBuffer(new byte[]
			{
				(byte)((byte)Type | Channel),
				Note,
				Velocity
			};
		}

		/// <summary>
		/// Gets the type of this MIDI message.
		/// </summary>
		public MidiMessageType Type => MidiMessageType.NoteOff;

		/// <summary>
		/// Gets the channel from 0-15 that this message applies to.
		/// </summary>
		public byte Channel => MidiHelpers.GetChannel(_buffer.Data[0]);

		/// <summary>
		/// Gets the note to turn off which is specified as a value from 0-127.
		/// </summary>
		public byte Note => _buffer.Data[1];

		/// <summary>
		/// Gets the value of the velocity from 0-127.
		/// </summary>
		public byte Velocity { get; }

		/// <summary>
		/// Gets the array of bytes associated with the MIDI message, including status byte.
		/// </summary>
		public IBuffer RawData => _buffer;

		/// <summary>
		/// Gets the duration from when the MidiInPort was created to the time the message was received.
		/// For messages being sent to a MidiOutPort, this value has no meaning.
		/// </summary>
		public TimeSpan Timestamp { get; internal set; } = TimeSpan.Zero;
	}
}
