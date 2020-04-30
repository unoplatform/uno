using System;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	public partial class MidiSongSelectMessage : IMidiMessage
	{
		public MidiSongSelectMessage(byte song)
		{
			Song = song;
		}

		public IBuffer RawData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer MidiSongSelectMessage.RawData is not implemented in Uno.");
			}
		}

		public TimeSpan Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MidiSongSelectMessage.Timestamp is not implemented in Uno.");
			}
		}

		public byte Song { get; }

		public MidiMessageType Type => MidiMessageType.SongSelect;
	}
}
