#if __ANDROID__ || __IOS__ || __WASM__
namespace Windows.Devices.Midi
{
	public partial class MidiNoteOffMessage : IMidiMessage
	{
		public MidiNoteOffMessage(byte channel, byte note, byte velocity)
		{
			Channel = channel;
			Note = note;
			Velocity = velocity;
		}

		public MidiMessageType Type => MidiMessageType.NoteOff;

		public byte Channel { get; }

		public byte Note { get; }

		public byte Velocity { get; }
	}
}
#endif
