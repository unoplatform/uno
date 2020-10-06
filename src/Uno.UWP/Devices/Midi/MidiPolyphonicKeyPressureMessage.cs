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
		private readonly Storage.Streams.Buffer _buffer;

		/// <summary>
		/// Creates a new MidiPolyphonicKeyPressureMessage object.
		/// </summary>
		/// <param name="channel">The channel from 0-15 that this message applies to.</param>
		/// <param name="note">The note which is specified as a value from 0-127.</param>
		/// <param name="pressure">The polyphonic key pressure which is specified as a value from 0-127.</param>
		public MidiPolyphonicKeyPressureMessage(byte channel, byte note, byte pressure)			
		{
			MidiMessageValidators.VerifyRange(channel, MidiMessageParameter.Channel);
			MidiMessageValidators.VerifyRange(note, MidiMessageParameter.Note);
			MidiMessageValidators.VerifyRange(pressure, MidiMessageParameter.Pressure);

			_buffer = new Storage.Streams.Buffer(new byte[]
			{
				(byte)((byte)Type | channel),
				note,
				pressure
			});
		}

		internal MidiPolyphonicKeyPressureMessage(byte[] rawData, TimeSpan timestamp)
		{
			MidiMessageValidators.VerifyMessageLength(rawData, 3, Type);
			MidiMessageValidators.VerifyMessageType(rawData[0], Type);
			MidiMessageValidators.VerifyRange(MidiHelpers.GetChannel(rawData[0]), MidiMessageParameter.Channel);
			MidiMessageValidators.VerifyRange(rawData[1], MidiMessageParameter.Note);
			MidiMessageValidators.VerifyRange(rawData[2], MidiMessageParameter.Pressure);

			_buffer = new Storage.Streams.Buffer(rawData);
			Timestamp = timestamp;
		}

		/// <summary>
		/// Gets the type of this MIDI message.
		/// </summary>
		public MidiMessageType Type => MidiMessageType.PolyphonicKeyPressure;

		/// <summary>
		/// Gets the channel from 0-15 that this message applies to.
		/// </summary>
		public byte Channel => MidiHelpers.GetChannel(_buffer.GetByte(0));

		/// <summary>
		/// Gets the note which is specified as a value from 0-127.
		/// </summary>
		public byte Note => _buffer.GetByte(1);

		/// <summary>
		/// Gets the polyphonic key pressure which is specified as a value from 0-127.
		/// </summary>
		public byte Pressure => _buffer.GetByte(2);

		/// <summary>
		/// Gets the array of bytes associated with the MIDI message, including status byte.
		/// </summary>
		public IBuffer RawData => _buffer;

		/// <summary>
		/// Gets the duration from when the MidiInPort was created to the time the message was received.
		/// For messages being sent to a MidiOutPort, this value has no meaning.
		/// </summary>
		public TimeSpan Timestamp { get; } = TimeSpan.Zero;
	}
}
