namespace Windows.Devices.Midi
{
	public partial class MidiActiveSensingMessage : IMidiMessage
	{
		public MidiActiveSensingMessage()
		{
		}

		public global::Windows.Storage.Streams.IBuffer RawData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer MidiActiveSensingMessage.RawData is not implemented in Uno.");
			}
		}

		public global::System.TimeSpan Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MidiActiveSensingMessage.Timestamp is not implemented in Uno.");
			}
		}
		public MidiMessageType Type => MidiMessageType.ActiveSensing;
	}
}
