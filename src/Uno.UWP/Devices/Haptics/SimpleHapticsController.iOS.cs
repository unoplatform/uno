using System;
using System.Collections.Generic;
using UIKit;

namespace Windows.Devices.Haptics
{
	public partial class SimpleHapticsController
	{
		public IReadOnlyList<SimpleHapticsControllerFeedback> SupportedFeedback { get; } = new SimpleHapticsControllerFeedback[]
		{
			new SimpleHapticsControllerFeedback(KnownSimpleHapticsControllerWaveforms.Click, TimeSpan.Zero),
			new SimpleHapticsControllerFeedback(KnownSimpleHapticsControllerWaveforms.Press, TimeSpan.Zero)
		};

		public void SendHapticFeedback(SimpleHapticsControllerFeedback feedback)
		{
			var impactStyle = FeedbackToImpactStyle(feedback);
			using var impact = new UIImpactFeedbackGenerator(impactStyle);
			impact.Prepare();
			impact.ImpactOccurred();
		}

		private UIImpactFeedbackStyle FeedbackToImpactStyle(SimpleHapticsControllerFeedback feedback)
		{
			if (feedback.Waveform == KnownSimpleHapticsControllerWaveforms.Click)
			{
				return UIImpactFeedbackStyle.Light;
			}
			else if (feedback.Waveform == KnownSimpleHapticsControllerWaveforms.Press)
			{
				return UIImpactFeedbackStyle.Medium;
			}
			else
			{
				throw new NotSupportedException("Unsupported feedback waveform");
			}
		}
	}
}
