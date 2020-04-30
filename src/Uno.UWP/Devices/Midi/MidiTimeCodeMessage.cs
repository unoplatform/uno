using System;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	public partial class MidiTimeCodeMessage : global::Windows.Devices.Midi.IMidiMessage
	{
		public MidiTimeCodeMessage(byte frameType, byte values)
		{
			FrameType = frameType;
			Values = values;
		}

		public IBuffer RawData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer MidiTimeCodeMessage.RawData is not implemented in Uno.");
			}
		}

		public TimeSpan Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MidiTimeCodeMessage.Timestamp is not implemented in Uno.");
			}
		}

		public byte FrameType { get; }

		public byte Values { get; }

		public MidiMessageType Type => MidiMessageType.MidiTimeCode;
	}
}
