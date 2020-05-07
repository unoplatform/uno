using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Devices.Midi.Internal
{
	internal static class MidiHelpers
	{
		/// <summary>
		/// Returns the MIDI channel from the message's first byte.
		/// </summary>
		/// <param name="firstMessageByte">First byte in a MIDI message.</param>
		/// <returns>Channel.</returns>
		internal static byte GetChannel(byte firstMessageByte)
		{
			return (byte)(firstMessageByte & 0b0000_1111);
		}
	}
}
