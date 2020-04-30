using System;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	public partial class MidiTimingClockMessage : IMidiMessage
	{
		public MidiTimingClockMessage()
		{
		}

		public IBuffer RawData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer MidiTimingClockMessage.RawData is not implemented in Uno.");
			}
		}

		public TimeSpan Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MidiTimingClockMessage.Timestamp is not implemented in Uno.");
			}
		}

		public MidiMessageType Type => MidiMessageType.TimingClock;
	}
}
