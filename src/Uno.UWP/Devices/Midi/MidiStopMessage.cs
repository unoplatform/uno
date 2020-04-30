using System;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	public partial class MidiStopMessage : IMidiMessage
	{
		public MidiStopMessage()
		{
		}


		public IBuffer RawData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer MidiStopMessage.RawData is not implemented in Uno.");
			}
		}

		public TimeSpan Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MidiStopMessage.Timestamp is not implemented in Uno.");
			}
		}

		public MidiMessageType Type => MidiMessageType.Stop;
	}
}
