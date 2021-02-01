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
		private readonly Storage.Streams.Buffer _buffer;

		/// <summary>
		/// Creates a new MidiPitchBendChangeMessage object.
		/// </summary>
		/// <param name="channel">Channel.</param>
		/// <param name="bend">Bend.</param>
		public MidiPitchBendChangeMessage(byte channel, ushort bend)
		{
			MidiMessageValidators.VerifyRange(channel, MidiMessageParameter.Channel);
			MidiMessageValidators.VerifyRange(bend, MidiMessageParameter.Bend);

			_buffer = new Storage.Streams.Buffer(new byte[]
			{
				(byte)((byte)Type | channel),
				(byte)(bend & 0b0111_1111),
				(byte)(bend >> 7)
			});
		}

		internal MidiPitchBendChangeMessage(byte[] rawData, TimeSpan timestamp)
		{			
			MidiMessageValidators.VerifyMessageLength(rawData, 3, Type);
			MidiMessageValidators.VerifyMessageType(rawData[0], Type);
			MidiMessageValidators.VerifyRange(MidiHelpers.GetChannel(rawData[0]), MidiMessageParameter.Channel);			
			MidiMessageValidators.VerifyRange(MidiHelpers.GetBend(rawData[1], rawData[2]), MidiMessageParameter.Bend);

			_buffer = new Storage.Streams.Buffer(rawData);
			Timestamp = timestamp;
		}

		/// <summary>
		/// Gets the type of this MIDI message.
		/// </summary>
		public MidiMessageType Type => MidiMessageType.PitchBendChange;

		/// <summary>
		/// Gets the channel from 0-15 that this message applies to.
		/// </summary>
		public byte Channel => MidiHelpers.GetChannel(_buffer.GetByte(0));

		/// <summary>
		/// Gets the pitch bend value which is specified as a 14-bit value from 0-16383.
		/// </summary>
		public ushort Bend => MidiHelpers.GetBend(_buffer.GetByte(1), _buffer.GetByte(2));

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
