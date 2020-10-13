#nullable enable

using System;
using System.Collections.Generic;
using AppKit;

namespace Windows.Devices.Haptics
{
	public partial class SimpleHapticsController
	{
		public IReadOnlyList<SimpleHapticsControllerFeedback> SupportedFeedback { get; } = new SimpleHapticsControllerFeedback[]
		{
			new SimpleHapticsControllerFeedback(KnownSimpleHapticsControllerWaveforms.Press, TimeSpan.FromMilliseconds(300))
		};

		public void SendHapticFeedback(SimpleHapticsControllerFeedback feedback)
		{
			if (feedback is null)
			{
				throw new ArgumentNullException(nameof(feedback));
			}

			if (feedback.Waveform != KnownSimpleHapticsControllerWaveforms.Press)
			{
				throw new NotSupportedException("Unsupported feedback waveform");
			}

			NSHapticFeedbackManager.DefaultPerformer.PerformFeedback(
				NSHapticFeedbackPattern.Generic,
				NSHapticFeedbackPerformanceTime.Default);
		}
	}
}
