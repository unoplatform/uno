using System;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	public partial class MidiPitchBendChangeMessage : IMidiMessage
	{
		public MidiPitchBendChangeMessage(byte channel, ushort bend)
		{
			Channel = channel;
			Bend = bend;
		}

		public IBuffer RawData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer MidiPitchBendChangeMessage.RawData is not implemented in Uno.");
			}
		}

		public TimeSpan Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MidiPitchBendChangeMessage.Timestamp is not implemented in Uno.");
			}
		}

		public ushort Bend { get; }

		public byte Channel { get; }

		public MidiMessageType Type => MidiMessageType.PitchBendChange;
	}
}
