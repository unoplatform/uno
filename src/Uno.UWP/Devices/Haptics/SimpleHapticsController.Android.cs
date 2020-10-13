#nullable enable

using System;
using System.Collections.Generic;
using Android.App;
using Android.Views;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI;

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

			if (ContextHelper.Current == null)
			{
				throw new InvalidOperationException($"Context must be initialized before {nameof(SendHapticFeedback)} is called.");
			}
			try
			{
				var activity = (Activity)ContextHelper.Current;
				var androidFeedback = FeedbackToAndroidFeedback(feedback);
				activity.Window?.DecorView.PerformHapticFeedback(androidFeedback);
			}
			catch (Exception ex)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError($"Could not send haptic feedback: {ex}");
				}
			}
		}

		private static FeedbackConstants FeedbackToAndroidFeedback(SimpleHapticsControllerFeedback feedback)
		{
			if (feedback.Waveform == KnownSimpleHapticsControllerWaveforms.Click)
			{
				return FeedbackConstants.ContextClick;
			}
			else if (feedback.Waveform == KnownSimpleHapticsControllerWaveforms.Press)
			{
				return FeedbackConstants.LongPress;
			}
			else
			{
				throw new NotSupportedException("Unsupported feedback waveform");
			}
		}
	}
}
