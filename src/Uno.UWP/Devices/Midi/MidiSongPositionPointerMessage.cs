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
		/// <summary>
		/// Creates a new MidiSongPositionPointerMessage object.
		/// </summary>
		/// <param name="beats">The song position pointer encoded in a 14-bit value from 0-16383.</param>
		public MidiSongPositionPointerMessage(ushort beats)
		{
			MidiMessageValidators.VerifyRange(beats, 16383, nameof(beats));

			Beats = beats;
			RawData = new InMemoryBuffer(new byte[]
			{
				(byte)Type,
				(byte)(beats & 0b0111_1111),
				(byte)(beats >> 7)
			});
		}

		/// <summary>
		/// Gets the type of this MIDI message.
		/// </summary>
		public MidiMessageType Type => MidiMessageType.SongPositionPointer;

		/// <summary>
		/// Gets the array of bytes associated with the MIDI message, including status byte.
		/// </summary>
		public IBuffer RawData { get; }

		/// <summary>
		/// Gets the duration from when the MidiInPort was created to the time the message was received.
		/// For messages being sent to a MidiOutPort, this value has no meaning.
		/// </summary>
		public TimeSpan Timestamp { get; } = TimeSpan.Zero;

		/// <summary>
		/// Gets the song position pointer encoded in a 14-bit value from 0-16383.
		/// </summary>
		public ushort Beats { get; }
	}
}
