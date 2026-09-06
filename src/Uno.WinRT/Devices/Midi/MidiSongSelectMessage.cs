using System;
using Uno.Devices.Midi.Internal;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	/// <summary>
	/// Represents a MIDI message that specifies the selected song.
	/// </summary>
	public partial class MidiSongSelectMessage : IMidiMessage
	{
		private readonly Storage.Streams.Buffer _buffer;

		/// <summary>
		/// Creates a new MidiSongSelectMessage object.
		/// </summary>
		/// <param name="song">The song to select from 0-127.</param>
		public MidiSongSelectMessage(byte song)
		{
			MidiMessageValidators.VerifyRange(song, MidiMessageParameter.Song);

			_buffer = new Storage.Streams.Buffer(new byte[]
			{
				(byte)MidiMessageType.SongSelect,
				song
			});
		}

		internal MidiSongSelectMessage(byte[] rawData, TimeSpan timestamp)
		{
			MidiMessageValidators.VerifyMessageLength(rawData, 2, Type);
			MidiMessageValidators.VerifyMessageType(rawData[0], Type);
			MidiMessageValidators.VerifyRange(rawData[1], MidiMessageParameter.Song);

			_buffer = new Storage.Streams.Buffer(rawData);
			Timestamp = timestamp;
		}

		/// <summary>
		/// Gets the type of this MIDI message.
		/// </summary>
		public MidiMessageType Type => MidiMessageType.SongSelect;

		/// <summary>
		/// Gets the array of bytes associated with the MIDI message, including status byte.
		/// </summary>
		public IBuffer RawData => _buffer;

		/// <summary>
		/// Gets the song to select from 0-127.
		/// </summary>
		public byte Song => _buffer.GetByte(1);

		/// <summary>
		/// Gets the duration from when the MidiInPort was created to the time the message was received.
		/// For messages being sent to a MidiOutPort, this value has no meaning.
		/// </summary>
		public TimeSpan Timestamp { get; } = TimeSpan.Zero;
	}
}
