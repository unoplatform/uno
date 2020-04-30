using System;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	public partial class MidiProgramChangeMessage : global::Windows.Devices.Midi.IMidiMessage
	{
		public MidiProgramChangeMessage(byte channel, byte program)
		{
			Channel = channel;
			Program = program;
		}

		public IBuffer RawData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer MidiProgramChangeMessage.RawData is not implemented in Uno.");
			}
		}

		public TimeSpan Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MidiProgramChangeMessage.Timestamp is not implemented in Uno.");
			}
		}


		public byte Channel { get; }
		
		public byte Program { get; }

		public MidiMessageType Type => MidiMessageType.ProgramChange;
	}
}
