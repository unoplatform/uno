using System;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	public partial class MidiChannelPressureMessage : global::Windows.Devices.Midi.IMidiMessage
	{
		public MidiChannelPressureMessage(byte channel, byte pressure)
		{
			Channel = channel;
			Pressure = pressure;
		}

		public byte Channel { get; }

		public byte Pressure { get; }

		public IBuffer RawData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer MidiChannelPressureMessage.RawData is not implemented in Uno.");
			}
		}

		public TimeSpan Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MidiChannelPressureMessage.Timestamp is not implemented in Uno.");
			}
		}

		public MidiMessageType Type => MidiMessageType.ChannelPressure;
	}
}
