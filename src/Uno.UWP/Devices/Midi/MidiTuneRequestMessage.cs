using System;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	public partial class MidiTuneRequestMessage : IMidiMessage
	{
		public MidiTuneRequestMessage()
		{
		}

		public IBuffer RawData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer MidiTuneRequestMessage.RawData is not implemented in Uno.");
			}
		}

		public TimeSpan Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MidiTuneRequestMessage.Timestamp is not implemented in Uno.");
			}
		}

		public MidiMessageType Type => MidiMessageType.TuneRequest;
	}
}
