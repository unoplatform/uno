using System;
using Uno.Devices.Midi.Internal;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	/// <summary>
	/// Represents a MIDI message that specifies the polyphonic key pressure.
	/// </summary>
	public partial class MidiPolyphonicKeyPressureMessage : IMidiMessage
	{
		/// <summary>
		/// Creates a new MidiPolyphonicKeyPressureMessage object.
		/// </summary>
		/// <param name="channel">The channel from 0-15 that this message applies to.</param>
		/// <param name="note">The note which is specified as a value from 0-127.</param>
		/// <param name="pressure">The polyphonic key pressure which is specified as a value from 0-127.</param>
		public MidiPolyphonicKeyPressureMessage(byte channel, byte note, byte pressure)
		{
			MidiMessageValidators.VerifyRange(channel, 15, nameof(channel));
			MidiMessageValidators.VerifyRange(note, 127, nameof(note));
			MidiMessageValidators.VerifyRange(pressure, 127, nameof(pressure));

			Channel = channel;
			Note = note;
			Pressure = pressure;
			RawData = new InMemoryBuffer(new byte[]
			{
				(byte)((byte)Type | Channel),
				Note,
				Pressure
			});
		}

		/// <summary>
		/// Gets the type of this MIDI message.
		/// </summary>
		public MidiMessageType Type => MidiMessageType.PolyphonicKeyPressure;

		/// <summary>
		/// Gets the array of bytes associated with the MIDI message, including status byte.
		/// </summary>
		public IBuffer RawData { get; }

		/// <summary>
		/// Gets the duration from when the MidiInPort was created to the time the message was received.
		/// For messages being sent to a MidiOutPort, this value has no meaning.
		/// </summary>
		public TimeSpan Timestamp { get; }

		/// <summary>
		/// Gets the channel from 0-15 that this message applies to.
		/// </summary>
		public byte Channel { get; }

		/// <summary>
		/// Gets the note which is specified as a value from 0-127.
		/// </summary>
		public byte Note { get; }

		/// <summary>
		/// Gets the polyphonic key pressure which is specified as a value from 0-127.
		/// </summary>
		public byte Pressure { get; }		

	}
}
