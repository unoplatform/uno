using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Midi;

namespace Uno.Devices.Midi.Internal
{
	internal class MidiMessageParser
	{
		public IMidiMessage Parse(byte[] bytes, TimeSpan timestamp)
		{
			if (bytes is null)
			{
				throw new ArgumentNullException(nameof(bytes));
			}

			if (bytes.Length == 0)
			{
				throw new ArgumentException(nameof(bytes), "MIDI message cannot be empty");
			}

			// Parsing logic based on
		    // https://www.midi.org/specifications-old/item/table-1-summary-of-midi-message

			// Try to parse channel voice messages first
			// These start with a unique combination of four bits and the remaining
			// four represent the channel.

			var upperBits = (byte)(bytes[0] & 0b_1111_0000);
			switch (upperBits)
			{
				case (byte)MidiMessageType.NoteOff:
					return new MidiNoteOffMessage(bytes, timestamp);
				case (byte)MidiMessageType.NoteOn:
					return new MidiNoteOnMessage(bytes, timestamp);
				case (byte)MidiMessageType.PolyphonicKeyPressure:
					return new MidiPolyphonicKeyPressureMessage(bytes, timestamp);
				case (byte)MidiMessageType.ControlChange:
					return new MidiControlChangeMessage(bytes, timestamp);
				case (byte)MidiMessageType.ProgramChange:
					return new MidiProgramChangeMessage(bytes, timestamp);
				case (byte)MidiMessageType.ChannelPressure:
					return new MidiChannelPressureMessage(bytes, timestamp);
				case (byte)MidiMessageType.PitchBendChange:
					return new MidiPitchBendChangeMessage(bytes, timestamp);
			}

			// System common messages
			switch (bytes[0])
			{
				case (byte)MidiMessageType.MidiTimeCode:
					return new MidiTimeCodeMessage(bytes, timestamp);
				case (byte)MidiMessageType.SongPositionPointer:
					return new MidiSongPositionPointerMessage(bytes, timestamp);
				case (byte)MidiMessageType.SongSelect:
					return new MidiSongSelectMessage(bytes, timestamp);
				case (byte)MidiMessageType.TuneRequest:
					return new MidiTuneRequestMessage(bytes, timestamp);
				case (byte)MidiMessageType.TimingClock:
					return new MidiTimingClockMessage(bytes, timestamp);
				case (byte)MidiMessageType.Start:
					return new MidiStartMessage(bytes, timestamp);
				case (byte)MidiMessageType.Continue:
					return new MidiContinueMessage(bytes, timestamp);
				case (byte)MidiMessageType.Stop:
					return new MidiStopMessage(bytes, timestamp);
				case (byte)MidiMessageType.ActiveSensing:
					return new MidiActiveSensingMessage(bytes, timestamp);
				case (byte)MidiMessageType.SystemReset:
					return new MidiSystemResetMessage(bytes, timestamp);
				default:
					return new MidiSystemExclusiveMessage(bytes, timestamp);
			}
		}
	}
}
