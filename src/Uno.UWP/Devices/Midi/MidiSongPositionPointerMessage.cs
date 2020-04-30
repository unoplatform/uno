using System;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	public partial class MidiSongPositionPointerMessage : IMidiMessage
	{
		public MidiSongPositionPointerMessage(ushort beats)
		{
			Beats = beats;
		}

		public IBuffer RawData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer MidiSongPositionPointerMessage.RawData is not implemented in Uno.");
			}
		}

		public TimeSpan Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MidiSongPositionPointerMessage.Timestamp is not implemented in Uno.");
			}
		}

		public ushort Beats { get; }

		public MidiMessageType Type => MidiMessageType.SongPositionPointer;
		
	}
}
