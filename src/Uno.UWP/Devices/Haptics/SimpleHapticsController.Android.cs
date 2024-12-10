#nullable enable
#program warning disable CS0618 // obsolete members

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
            new SimpleHapticsControllerFeedback(KnownSimpleHapticsControllerWaveforms.Press, TimeSpan.FromMilliseconds(300)),
            new SimpleHapticsControllerFeedback(KnownSimpleHapticsControllerWaveforms.Click, TimeSpan.FromMilliseconds(100)), // Single Click
            new SimpleHapticsControllerFeedback(KnownSimpleHapticsControllerWaveforms.Click, TimeSpan.FromMilliseconds(150))  // Double Click
        };

        public void SendHapticFeedback(SimpleHapticsControllerFeedback feedback)
        {
            if (feedback is null)
            {
                throw new ArgumentNullException(nameof(feedback));
            }

            Vibrator vibrator = (Vibrator)Android.App.Application.Context.GetSystemService(Android.Content.Context.VibratorService);

            if (feedback.Waveform == KnownSimpleHapticsControllerWaveforms.Press)
            {
                if (vibrator.HasVibrator)
                {
                    vibrator.Vibrate((long)feedback.Duration.TotalMilliseconds);
                }
                else
                {
                    throw new NotSupportedException("Device does not support vibration");
                }
            }
            else if (feedback.Waveform == KnownSimpleHapticsControllerWaveforms.Click)
            {
                if (feedback.Duration.TotalMilliseconds <= 100)
                {
                    // Single Click
                    if (vibrator.HasVibrator)
                    {
                        vibrator.Vibrate(VibrationEffect.CreateOneShot((long)feedback.Duration.TotalMilliseconds, VibrationEffect.DefaultAmplitude));
                    }
                    else
                    {
                        throw new NotSupportedException("Device does not support vibration");
                    }
                }
                else if (feedback.Duration.TotalMilliseconds <= 200)
                {
                    // Double Click
                    if (vibrator.HasVibrator)
                    {
                        long[] pattern = { 0, (long)feedback.Duration.TotalMilliseconds, 50, (long)feedback.Duration.TotalMilliseconds };
                        vibrator.Vibrate(VibrationEffect.CreateWaveform(pattern, -1));
                    }
                    else
                    {
                        throw new NotSupportedException("Device does not support vibration");
                    }
                }
                else
                {
                    throw new NotSupportedException("Unsupported feedback duration for Click waveform");
                }
            }
            else
            {
                throw new NotSupportedException("Unsupported feedback waveform");
            }
        }
    }
}
