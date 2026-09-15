namespace Windows.Devices.Midi
{
	/// <summary>
	/// Provides data for the MessageReceived event.
	/// </summary>
	public partial class MidiMessageReceivedEventArgs
	{
		internal MidiMessageReceivedEventArgs(IMidiMessage midiMessage)
		{
			Message = midiMessage;
		}

		/// <summary>
		/// The MIDI message.
		/// </summary>
		public IMidiMessage Message { get; }
	}
}
