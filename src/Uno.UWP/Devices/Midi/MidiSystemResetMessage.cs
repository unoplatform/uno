using System;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	public partial class MidiSystemResetMessage : global::Windows.Devices.Midi.IMidiMessage
	{
		public MidiSystemResetMessage()
		{
		}

		public IBuffer RawData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer MidiSystemResetMessage.RawData is not implemented in Uno.");
			}
		}

		public TimeSpan Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MidiSystemResetMessage.Timestamp is not implemented in Uno.");
			}
		}
		public MidiMessageType Type => MidiMessageType.SystemReset;
	}
}
