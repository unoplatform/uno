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
            new SimpleHapticsControllerFeedback(KnownSimpleHapticsControllerWaveforms.Click, TimeSpan.FromMilliseconds(100)), // Single Click
            new SimpleHapticsControllerFeedback(KnownSimpleHapticsControllerWaveforms.Click, TimeSpan.FromMilliseconds(150)), // Double Click
            new SimpleHapticsControllerFeedback(KnownSimpleHapticsControllerWaveforms.Press, TimeSpan.FromMilliseconds(300))
        };

        public void SendHapticFeedback(SimpleHapticsControllerFeedback feedback)
        {
            if (feedback is null)
            {
                throw new ArgumentNullException(nameof(feedback));
            }

            if (feedback.Waveform == KnownSimpleHapticsControllerWaveforms.Click)
            {
                // Determine if it's a single click or double click based on the duration
                if (feedback.Duration.TotalMilliseconds <= 100)
                {
                    // Single Click
                    NSHapticFeedbackManager.DefaultPerformer.PerformFeedback(NSHapticFeedbackPattern.Generic, NSHapticFeedbackPerformanceTime.Default);
                }
                else if (feedback.Duration.TotalMilliseconds <= 200)
                {
                    // Double Click
                    NSHapticFeedbackManager.DefaultPerformer.PerformFeedback(NSHapticFeedbackPattern.Generic, NSHapticFeedbackPerformanceTime.Default);
                    NSHapticFeedbackManager.DefaultPerformer.PerformFeedback(NSHapticFeedbackPattern.Generic, NSHapticFeedbackPerformanceTime.Default);
                }
                else
                {
                    throw new NotSupportedException("Unsupported feedback duration for Click waveform");
                }
            }
            else if (feedback.Waveform == KnownSimpleHapticsControllerWaveforms.Press)
            {
                NSHapticFeedbackManager.DefaultPerformer.PerformFeedback(NSHapticFeedbackPattern.Generic, NSHapticFeedbackPerformanceTime.Default);
            }
            else
            {
                throw new NotSupportedException("Unsupported feedback waveform");
            }
        }
    }
}
