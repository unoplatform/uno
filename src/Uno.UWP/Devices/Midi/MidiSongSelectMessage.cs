using System;
using Uno.Devices.Midi.Internal;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	/// <summary>
	/// Represents a MIDI message that specifies the selected song.
	/// </summary>
	public partial class MidiSongSelectMessage : IMidiMessage
	{
		/// <summary>
		/// Creates a new MidiSongSelectMessage object.
		/// </summary>
		/// <param name="song">The song to select from 0-127.</param>
		public MidiSongSelectMessage(byte song)
		{			
			MidiMessageValidators.VerifyRange(song, 127, nameof(song));

			Song = song;
			RawData = new InMemoryBuffer(new byte[]
			{
				(byte)Type,
				song
			});
		}

		/// <summary>
		/// Gets the type of this MIDI message.
		/// </summary>
		public MidiMessageType Type => MidiMessageType.SongSelect;

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
		/// Gets the song to select from 0-127.
		/// </summary>
		public byte Song { get; }
	}
}
