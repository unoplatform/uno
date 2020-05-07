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

			// Try to parse channel voice messages first
			// These start with a unique combination of four bits and the remaining
			// four represent the channel.

			var upperBits = (byte)(bytes[0] & 0b_1111_0000);
			var lowerBits = (byte)(bytes[0] & 0b_0000_1111);
			if (upperBits == (byte)MidiMessageType.NoteOff)
			{
				return new MidiNoteOffMessage(lowerBits, bytes[1], bytes[2]);
			}

		}

		private static bool MatchesMessageType(byte firstByte, MidiMessageType messageType)
		{
			var encodedMessageType = (byte)messageType;
			return (firstByte & encodedMessageType) == encodedMessageType;
		}
	}
}
