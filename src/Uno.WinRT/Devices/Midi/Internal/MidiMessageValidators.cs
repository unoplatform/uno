using System;
using Windows.Devices.Midi;

namespace Uno.Devices.Midi.Internal
{
	internal static class MidiMessageValidators
	{
		internal static void VerifyMessageLength(byte[] messageData, int expectedLength, MidiMessageType type)
		{
			if (messageData is null)
			{
				throw new ArgumentNullException(nameof(messageData));
			}
			if (messageData.Length != expectedLength)
			{
				throw new ArgumentException(
					$"MIDI message of type {type} must have length of {expectedLength} bytes",
					nameof(messageData));
			}
		}

		internal static void VerifyMessageType(byte firstByte, MidiMessageType type)
		{
			var typeByte = (byte)type;
			if ((firstByte & typeByte) != typeByte)
			{
				throw new ArgumentException(
					$"The message does not match expected type of {type}");
			}
		}

		internal static void VerifyRange(int value, MidiMessageParameter parameter)
		{
			if (value > (int)parameter)
			{
				throw new ArgumentException(
					$"{parameter} must be a number in the range 0 - {(int)parameter}");
			}
		}
	}
}
