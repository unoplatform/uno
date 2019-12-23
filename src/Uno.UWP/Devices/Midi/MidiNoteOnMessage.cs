#if __ANDROID__ || __IOS__ || __WASM__
namespace Windows.Devices.Midi
{
	public partial class MidiNoteOnMessage : IMidiMessage
	{
		public MidiNoteOnMessage(byte channel, byte note, byte velocity)
		{
			Channel = channel;
			Note = note;
			Velocity = velocity;
		}

		public MidiMessageType Type => MidiMessageType.NoteOn;

		public byte Channel { get; }

		public byte Note { get; }

		public byte Velocity { get; }
	}
}
#endif
