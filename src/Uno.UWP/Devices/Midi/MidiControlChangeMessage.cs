using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	public partial class MidiControlChangeMessage : IMidiMessage
	{
		public MidiControlChangeMessage(byte channel, byte controller, byte controlValue)
		{
			Channel = channel;
			Controller = controller;
			ControlValue = controlValue;
		}

		public byte Channel { get; }

		public byte ControlValue { get; }

		public byte Controller { get; }

		public IBuffer RawData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer MidiControlChangeMessage.RawData is not implemented in Uno.");
			}
		}

		public global::System.TimeSpan Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MidiControlChangeMessage.Timestamp is not implemented in Uno.");
			}
		}

		public MidiMessageType Type => MidiMessageType.ControlChange;
	}
}
