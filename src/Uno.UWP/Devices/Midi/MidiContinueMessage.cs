namespace Windows.Devices.Midi
{
	public partial class MidiContinueMessage : global::Windows.Devices.Midi.IMidiMessage
	{
		public MidiContinueMessage()
		{
		}

		public global::Windows.Storage.Streams.IBuffer RawData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer MidiContinueMessage.RawData is not implemented in Uno.");
			}
		}
		public global::System.TimeSpan Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MidiContinueMessage.Timestamp is not implemented in Uno.");
			}
		}
		public MidiMessageType Type => MidiMessageType.Continue;		
	}
}
