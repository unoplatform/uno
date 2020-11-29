using System;

namespace Windows.Devices.Haptics
{
	public partial class SimpleHapticsControllerFeedback
	{
		internal SimpleHapticsControllerFeedback(ushort waveform, TimeSpan duration)
		{
			Waveform = waveform;
			Duration = duration;
		}

		public TimeSpan Duration { get; }

		public ushort Waveform { get; }
	}
}
