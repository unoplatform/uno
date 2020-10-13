using System;
using Uno.Devices.Midi.Internal;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	/// <summary>
	/// Represents a MIDI message that specifies a program change.
	/// </summary>
	public partial class MidiProgramChangeMessage : IMidiMessage
	{
		private readonly Storage.Streams.Buffer _buffer;

		/// <summary>
		/// Creates a new MidiProgramChangeMessage object.
		/// </summary>
		/// <param name="channel">The channel from 0-15 that this message applies to.</param>
		/// <param name="program">The program to change from 0-127.</param>
		public MidiProgramChangeMessage(byte channel, byte program)			
		{
			MidiMessageValidators.VerifyRange(channel, MidiMessageParameter.Channel);
			MidiMessageValidators.VerifyRange(program, MidiMessageParameter.Program);

			_buffer = new Storage.Streams.Buffer(new byte[]
			{
				(byte)((byte)MidiMessageType.ProgramChange | channel),
				program,
			});
		}

		internal MidiProgramChangeMessage(byte[] rawData, TimeSpan timestamp)
		{
			MidiMessageValidators.VerifyMessageLength(rawData, 2, Type);
			MidiMessageValidators.VerifyMessageType(rawData[0], Type);
			MidiMessageValidators.VerifyRange(MidiHelpers.GetChannel(rawData[0]), MidiMessageParameter.Channel);
			MidiMessageValidators.VerifyRange(rawData[1], MidiMessageParameter.Program);

			_buffer = new Storage.Streams.Buffer(rawData);
			Timestamp = timestamp;
		}

		/// <summary>
		/// Gets the type of this MIDI message.
		/// </summary>
		public MidiMessageType Type => MidiMessageType.ProgramChange;

		/// <summary>
		/// Gets the channel from 0-15 that this message applies to.
		/// </summary>
		public byte Channel => MidiHelpers.GetChannel(_buffer.GetByte(0));

		/// <summary>
		/// Gets the program to change from 0-127.
		/// </summary>
		public byte Program => _buffer.GetByte(1);

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
