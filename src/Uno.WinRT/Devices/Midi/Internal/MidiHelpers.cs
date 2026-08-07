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

		internal static byte GetFrame(byte frameByte)
		{
			return (byte)(frameByte & 0b1111_0000);
		}

		internal static byte GetFrameValues(byte frameByte)
		{
			return (byte)(frameByte & 0b0000_1111);
		}

		/// <summary>
		/// Returns the MIDI bend value from two bytes in message.
		/// </summary>
		/// <param name="firstByte">First byte of the value in MIDI message order.</param>
		/// <param name="secondByte">Second byte of the value in MIDI message order.</param>
		/// <returns>Bend.</returns>
		internal static ushort GetBend(byte firstByte, byte secondByte) => GetUshort(firstByte, secondByte);

		/// <summary>
		/// Returns the MIDI beats value from two bytes in message.
		/// </summary>
		/// <param name="firstByte">First byte of the value in MIDI message order.</param>
		/// <param name="secondByte">Second byte of the value in MIDI message order.</param>
		/// <returns>Beats.</returns>
		internal static ushort GetBeats(byte firstByte, byte secondByte) => GetUshort(firstByte, secondByte);

		/// <summary>
		/// Reads an ushort value from two bytes (used for beats and bend).
		/// The parameter value is encoded as using 7 bits of each byte.
		/// </summary>
		/// <param name="firstByte">First byte.</param>
		/// <param name="secondByte">Second byte.</param>
		/// <returns>Ushort value</returns>
		private static ushort GetUshort(byte firstByte, byte secondByte) => (ushort)((secondByte << 7) | firstByte);
	}
}
