#nullable enable
#pragma warning disable CS0618 // obsolete members

using System;
using System.Collections.Generic;
using Android.App;
using Android.Views;
using Android.Provider;

using Uno.Extensions;
using Uno.UI;
using PhoneVibrationDevice = Windows.Phone.Devices.Notification.VibrationDevice;
using Uno.Foundation.Logging;

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
				bool hapticFeedbackEnabled = Settings.System.GetInt(activity.ContentResolver, Settings.System.HapticFeedbackEnabled, 0) != 0;
				if (hapticFeedbackEnabled)
				{
					var executed = activity.Window?.DecorView.PerformHapticFeedback(androidFeedback) ?? false;
					if (!executed && PhoneVibrationDevice.GetDefault() is { } vibrationDevice)
					{
						// Fall back to VibrationDevice
						vibrationDevice.Vibrate(feedback.Duration);
					}
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
