#nullable enable

using System;
using System.Collections.Generic;
using PhoneVibrationDevice = Windows.Phone.Devices.Notification.VibrationDevice;

namespace Windows.Devices.Haptics
{
	public partial class SimpleHapticsController
	{
		private readonly PhoneVibrationDevice? _vibrationDevice = PhoneVibrationDevice.GetDefault();

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

			if (_vibrationDevice == null)
			{
				throw new InvalidOperationException("Vibration is not supported");
			}

			_vibrationDevice.Vibrate(feedback.Duration);
		}
	}
}
