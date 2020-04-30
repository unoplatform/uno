using System;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	public partial interface IMidiMessage
	{
		IBuffer RawData { get; }

		TimeSpan Timestamp { get; }

		MidiMessageType Type { get; }
	}
}
