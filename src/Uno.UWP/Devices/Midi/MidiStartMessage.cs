using System;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	public  partial class MidiStartMessage : IMidiMessage
	{
		public MidiStartMessage()
		{			
		}

		public IBuffer RawData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer MidiStartMessage.RawData is not implemented in Uno.");
			}
		}
		public TimeSpan Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MidiStartMessage.Timestamp is not implemented in Uno.");
			}
		}

		public MidiMessageType Type => MidiMessageType.Start;
	}
}
