#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Tizen.System;
using Uno.Extensions;
using Windows.Devices.Haptics;

namespace Uno.UI.Runtime.Skia.Tizen.Devices.Haptics
{
	public class TizenSimpleHapticsControllerExtension : ISimpleHapticsControllerExtension
	{
		private const string TapPattern = "Tap";
		private const string HoldPattern = "Hold";

		public TizenSimpleHapticsControllerExtension(object owner)
		{
		}

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

			try
			{
				var tizenFeedback = new Feedback();
				var pattern = FeedbackToPattern(feedback);
				if (tizenFeedback.IsSupportedPattern(FeedbackType.Vibration, pattern))
				{
					tizenFeedback.Play(FeedbackType.Vibration, pattern);
				}
			}
			catch (Exception ex)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError($"Could not send haptic feedback: {ex}");
				}
			}
		}

		static string FeedbackToPattern(SimpleHapticsControllerFeedback feedback)
		{
			if (feedback.Waveform == KnownSimpleHapticsControllerWaveforms.Click)
			{
				return TapPattern;
			}
			else if (feedback.Waveform == KnownSimpleHapticsControllerWaveforms.Press)
			{
				return HoldPattern;
			}
			else
			{
				throw new NotSupportedException("Unsupported feedback waveform");
			}
		}
	}
}
