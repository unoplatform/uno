using System;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	public partial class MidiPolyphonicKeyPressureMessage : global::Windows.Devices.Midi.IMidiMessage
	{
		public MidiPolyphonicKeyPressureMessage(byte channel, byte note, byte pressure)
		{
			Channel = channel;
			Note = note;
			Pressure = pressure;
		}

		public IBuffer RawData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer MidiPolyphonicKeyPressureMessage.RawData is not implemented in Uno.");
			}
		}

		public TimeSpan Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MidiPolyphonicKeyPressureMessage.Timestamp is not implemented in Uno.");
			}
		}

		public byte Channel { get; }

		public byte Note { get; }		

		public byte Pressure { get; }		

		public MidiMessageType Type => MidiMessageType.PolyphonicKeyPressure;
	}
}
