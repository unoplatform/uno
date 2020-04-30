using System;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	public  partial class MidiSystemExclusiveMessage : IMidiMessage
	{
		public MidiSystemExclusiveMessage(IBuffer rawData)
		{
			awData = rawData;
		}

		public IBuffer RawData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer MidiSystemExclusiveMessage.RawData is not implemented in Uno.");
			}
		}

		public TimeSpan Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MidiSystemExclusiveMessage.Timestamp is not implemented in Uno.");
			}
		}

		public MidiMessageType Type => MidiMessageType.SystemExclusive		
	}
}
