#nullable enable

using System;
using System.Collections.Generic;
using UIKit;

#nullable enable

namespace Windows.Devices.Haptics
{
	public partial class SimpleHapticsController
	{
		public IReadOnlyList<SimpleHapticsControllerFeedback> SupportedFeedback { get; } = new SimpleHapticsControllerFeedback[]
		{
			new SimpleHapticsControllerFeedback(KnownSimpleHapticsControllerWaveforms.Click, TimeSpan.FromMilliseconds(100)),
			new SimpleHapticsControllerFeedback(KnownSimpleHapticsControllerWaveforms.Press, TimeSpan.FromMilliseconds(300))
		};

		public void SendHapticFeedback(SimpleHapticsControllerFeedback feedback)
		{
			if (feedback is null)
			{
				throw new ArgumentNullException(nameof(feedback));
			}

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
