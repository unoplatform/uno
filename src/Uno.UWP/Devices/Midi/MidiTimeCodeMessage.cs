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
		/// <summary>
		/// Creates a new MidiTimeCodeMessage object.
		/// </summary>
		/// <param name="frameType">The frame type from 0-7.</param>
		/// <param name="values">The time code from 0-15.</param>
		public MidiTimeCodeMessage(byte frameType, byte values)
		{
			MidiMessageValidators.VerifyRange(frameType, 7, nameof(frameType));
			MidiMessageValidators.VerifyRange(values, 15, nameof(values));

			FrameType = frameType;
			Values = values;
			RawData = new InMemoryBuffer(new byte[]
			{
				(byte)Type,
				(byte)(frameType << 4 | values)
			});
		}

		/// <summary>
		/// Gets the type of this MIDI message.
		/// </summary>
		public MidiMessageType Type => MidiMessageType.MidiTimeCode;

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
		/// Gets the value of the frame type from 0-7.
		/// </summary>
		public byte FrameType { get; }

		/// <summary>
		/// Gets the time code value from 0-15.
		/// </summary>
		public byte Values { get; }
	}
}
