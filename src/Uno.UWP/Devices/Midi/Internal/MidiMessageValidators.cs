using System;
using Windows.Devices.Midi;

namespace Uno.Devices.Midi.Internal
{
	internal static class MidiMessageValidators
	{
		internal static void VerifyMessageType(byte firstMessageByte, MidiMessageType type)
		{
			var typeByte = (byte)type;
			if ((firstMessageByte & typeByte) != typeByte)
			{
				throw new ArgumentException(
					$"The message does not match expected type of {type}");
			}
		}

		internal static void VerifyRange(int value, int max, string paramName)
		{
			if (value > max)
			{
				throw new ArgumentException(
					$"{nameof(paramName)} must be a number in the range 0 - {max}",
					paramName);
			}
		}
	}
}
