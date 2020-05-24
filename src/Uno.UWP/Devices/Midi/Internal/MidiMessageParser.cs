using System;
using System.Collections.Generic;
using Windows.Devices.Midi;

namespace Uno.Devices.Midi.Internal
{
	internal class MidiMessageParser
	{
		/// <summary>
		/// Parses input byte array to MIDI messages.
		/// For Android, multiple messages can be received
		/// in a single batch.
		/// </summary>
		/// <param name="bytes">Bytes.</param>
		/// <param name="startingOffset">Starting offset.</param>
		/// <param name="length">Length.</param>
		/// <param name="timestamp">Timestamp.</param>
		/// <returns>Parsed MIDI messages.</returns>
		public IEnumerable<IMidiMessage> Parse(byte[] bytes, int startingOffset, int length, TimeSpan timestamp)
		{
			if (bytes is null)
			{
				throw new ArgumentNullException(nameof(bytes));
			}

			if (bytes.Length == 0)
			{
				throw new ArgumentException(nameof(bytes), "MIDI message cannot be empty");
			}

			var currentOffset = startingOffset;
			while (currentOffset - startingOffset < length)
			{
				var availableLength = length - (currentOffset - startingOffset);
				yield return ReadNextMessage(bytes, ref currentOffset, availableLength, timestamp);
			}
		}

		private IMidiMessage ReadNextMessage(byte[] bytes, ref int offset, int availableLength, TimeSpan timestamp)
		{
			// Parsing logic based on
			// https://www.midi.org/specifications-old/item/table-1-summary-of-midi-message

			// Try to parse channel voice messages first
			// These start with a unique combination of four bits and the remaining
			// four represent the channel.

			var upperBits = (byte)(bytes[offset] & 0b_1111_0000);
			switch (upperBits)
			{
				case (byte)MidiMessageType.NoteOff:
					var noteOffBytes = CopySubmessage(bytes, ref offset, 3, availableLength);
					return new MidiNoteOffMessage(noteOffBytes, timestamp);
				case (byte)MidiMessageType.NoteOn:
					var noteOnBytes = CopySubmessage(bytes, ref offset, 3, availableLength);
					return new MidiNoteOnMessage(noteOnBytes, timestamp);
				case (byte)MidiMessageType.PolyphonicKeyPressure:
					var polyphonicBytes = CopySubmessage(bytes, ref offset, 3, availableLength);
					return new MidiPolyphonicKeyPressureMessage(polyphonicBytes, timestamp);
				case (byte)MidiMessageType.ControlChange:
					var controlChangeBytes = CopySubmessage(bytes, ref offset, 3, availableLength);
					return new MidiControlChangeMessage(controlChangeBytes, timestamp);
				case (byte)MidiMessageType.ProgramChange:
					var programChangeBytes = CopySubmessage(bytes, ref offset, 2, availableLength);
					return new MidiProgramChangeMessage(programChangeBytes, timestamp);
				case (byte)MidiMessageType.ChannelPressure:
					var channelPressureBytes = CopySubmessage(bytes, ref offset, 2, availableLength);
					return new MidiChannelPressureMessage(channelPressureBytes, timestamp);
				case (byte)MidiMessageType.PitchBendChange:
					var pitchBendBytes = CopySubmessage(bytes, ref offset, 3, availableLength);
					return new MidiPitchBendChangeMessage(pitchBendBytes, timestamp);
			}

			// System common messages
			switch (bytes[offset])
			{
				case (byte)MidiMessageType.MidiTimeCode:
					var midiCodeBytes = CopySubmessage(bytes, ref offset, 2, availableLength);
					return new MidiTimeCodeMessage(midiCodeBytes, timestamp);
				case (byte)MidiMessageType.SongPositionPointer:
					var songPositionBytes = CopySubmessage(bytes, ref offset, 3, availableLength);
					return new MidiSongPositionPointerMessage(songPositionBytes, timestamp);
				case (byte)MidiMessageType.SongSelect:
					var songSelectBytes = CopySubmessage(bytes, ref offset, 2, availableLength);
					return new MidiSongSelectMessage(songSelectBytes, timestamp);
				case (byte)MidiMessageType.TuneRequest:
					var tuneRequestBytes = CopySubmessage(bytes, ref offset, 1, availableLength);
					return new MidiTuneRequestMessage(tuneRequestBytes, timestamp);
				case (byte)MidiMessageType.TimingClock:
					var timingClockBytes = CopySubmessage(bytes, ref offset, 1, availableLength);
					return new MidiTimingClockMessage(timingClockBytes, timestamp);
				case (byte)MidiMessageType.Start:
					var startBytes = CopySubmessage(bytes, ref offset, 1, availableLength);
					return new MidiStartMessage(startBytes, timestamp);
				case (byte)MidiMessageType.Continue:
					var continueBytes = CopySubmessage(bytes, ref offset, 1, availableLength);
					return new MidiContinueMessage(continueBytes, timestamp);
				case (byte)MidiMessageType.Stop:
					var stopBytes = CopySubmessage(bytes, ref offset, 1, availableLength);
					return new MidiStopMessage(stopBytes, timestamp);
				case (byte)MidiMessageType.ActiveSensing:
					var activeSensingBytes = CopySubmessage(bytes, ref offset, 1, availableLength);
					return new MidiActiveSensingMessage(activeSensingBytes, timestamp);
				case (byte)MidiMessageType.SystemReset:
					var systemResetBytes = CopySubmessage(bytes, ref offset, 1, availableLength);
					return new MidiSystemResetMessage(systemResetBytes, timestamp);
				default:
					// take all remaining bytes
					var remainingBytes = CopySubmessage(bytes, ref offset, availableLength, availableLength);
					return new MidiSystemExclusiveMessage(remainingBytes, timestamp);
			}
		}

		private byte[] CopySubmessage(byte[] source, ref int offset, int requestedLength, int availableLength)
		{
			if (requestedLength > availableLength)
			{
				throw new InvalidOperationException(
					"The MIDI message is longer than available buffer.");
			}
			byte[] result = new byte[requestedLength];
			Array.Copy(source, offset, result, 0, requestedLength);
			offset += requestedLength;
			return result;
		}
	}
}
