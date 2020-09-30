using System;
using Uno.Devices.Midi.Internal;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	/// <summary>
	/// Represents a MIDI message that specifies a time code.
	/// </summary>
	public partial class MidiTimeCodeMessage : IMidiMessage
	{
		private readonly Storage.Streams.Buffer _buffer;

		/// <summary>
		/// Creates a new MidiTimeCodeMessage object.
		/// </summary>
		/// <param name="frameType">The frame type from 0-7.</param>
		/// <param name="values">The time code from 0-15.</param>
		public MidiTimeCodeMessage(byte frameType, byte values)			
		{
			MidiMessageValidators.VerifyRange(frameType, MidiMessageParameter.Frame);
			MidiMessageValidators.VerifyRange(values, MidiMessageParameter.FrameValues);

			_buffer = new Storage.Streams.Buffer(new byte[]
			{
				(byte)Type,
				(byte)(frameType << 4 | values)
			});
		}

		internal MidiTimeCodeMessage(byte[] rawData, TimeSpan timestamp)
		{
			MidiMessageValidators.VerifyMessageLength(rawData, 2, Type);
			MidiMessageValidators.VerifyMessageType(rawData[0], Type);
			MidiMessageValidators.VerifyRange(MidiHelpers.GetFrame(rawData[1]), MidiMessageParameter.Frame);
			MidiMessageValidators.VerifyRange(MidiHelpers.GetFrameValues(rawData[1]), MidiMessageParameter.FrameValues);

			_buffer = new Storage.Streams.Buffer(rawData);
			Timestamp = timestamp;
		}

		/// <summary>
		/// Gets the type of this MIDI message.
		/// </summary>
		public MidiMessageType Type => MidiMessageType.MidiTimeCode;

		/// <summary>
		/// Gets the value of the frame type from 0-7.
		/// </summary>
		public byte FrameType => MidiHelpers.GetFrame(_buffer.GetByte(1));

		/// <summary>
		/// Gets the time code value from 0-15.
		/// </summary>
		public byte Values => MidiHelpers.GetFrameValues(_buffer.GetByte(1));

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
