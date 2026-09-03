using System;
using Uno.Devices.Midi.Internal;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	/// <summary>
	/// Represents a MIDI message that specifies a song position pointer.
	/// </summary>
	public partial class MidiSongPositionPointerMessage : IMidiMessage
	{
		private readonly Storage.Streams.Buffer _buffer;

		/// <summary>
		/// Creates a new MidiSongPositionPointerMessage object.
		/// </summary>
		/// <param name="beats">The song position pointer encoded in a 14-bit value from 0-16383.</param>
		public MidiSongPositionPointerMessage(ushort beats)
		{
			MidiMessageValidators.VerifyRange(beats, MidiMessageParameter.Beats);

			_buffer = new Storage.Streams.Buffer(new byte[]
			{
				(byte)MidiMessageType.SongPositionPointer,
				(byte)(beats & 0b0111_1111),
				(byte)(beats >> 7)
			});
		}

		internal MidiSongPositionPointerMessage(byte[] rawData, TimeSpan timestamp)
		{
			MidiMessageValidators.VerifyMessageLength(rawData, 3, Type);
			MidiMessageValidators.VerifyMessageType(rawData[0], Type);
			MidiMessageValidators.VerifyRange(MidiHelpers.GetBeats(rawData[1], rawData[2]), MidiMessageParameter.Beats);

			_buffer = new Storage.Streams.Buffer(rawData);
			Timestamp = timestamp;
		}

		/// <summary>
		/// Gets the type of this MIDI message.
		/// </summary>
		public MidiMessageType Type => MidiMessageType.SongPositionPointer;

		/// <summary>
		/// Gets the song position pointer encoded in a 14-bit value from 0-16383.
		/// </summary>
		public ushort Beats => MidiHelpers.GetBeats(_buffer.GetByte(1), _buffer.GetByte(2));

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
