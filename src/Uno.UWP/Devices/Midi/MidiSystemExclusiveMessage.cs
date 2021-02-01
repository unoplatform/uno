using System;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	/// <summary>
	/// Represents a MIDI message that specifies a system exclusive message.
	/// </summary>
	public partial class MidiSystemExclusiveMessage : IMidiMessage
	{
		/// <summary>
		/// Creates a new MidiSystemExclusiveMessage object.
		/// </summary>
		/// <param name="rawData">The system exclusive data.</param>
		public MidiSystemExclusiveMessage(IBuffer rawData)
		{
			if (rawData == null)
			{
				throw new ArgumentNullException(nameof(rawData));
			}

			if (rawData.Length == 0)
			{
				throw new ArgumentException("Buffer must not be empty", nameof(rawData));
			}

			if (rawData is Storage.Streams.Buffer inMemory &&
				inMemory.GetByte(0) == (byte)MidiMessageType.EndSystemExclusive)
			{
				Type = MidiMessageType.EndSystemExclusive;
			}

			RawData = rawData;
		}

		internal MidiSystemExclusiveMessage(byte[] rawData, TimeSpan timestamp)
		{
			if (rawData == null)
			{
				throw new ArgumentNullException(nameof(rawData));
			}

			if (rawData.Length == 0)
			{
				throw new ArgumentException("Buffer must not be empty", nameof(rawData));
			}

			if (rawData[0] == (byte)MidiMessageType.EndSystemExclusive)
			{
				Type = MidiMessageType.EndSystemExclusive;
			}

			RawData = new Storage.Streams.Buffer(rawData);
			Timestamp = timestamp;
		}

		/// <summary>
		/// Gets the type of this MIDI message.
		/// </summary>
		public MidiMessageType Type { get; } = MidiMessageType.SystemExclusive;

		/// <summary>
		/// Gets the array of bytes associated with the MIDI message, including status byte.
		/// </summary>
		public IBuffer RawData { get; }

		/// <summary>
		/// Gets the duration from when the MidiInPort was created to the time the message was received.
		/// For messages being sent to a MidiOutPort, this value has no meaning.
		/// </summary>
		public TimeSpan Timestamp { get; } = TimeSpan.Zero;
	}
}
