public class HapticFeedbackManager
{
    private DateTime? lastClickTime = null;

    public IReadOnlyList<SimpleHapticsControllerFeedback> SupportedFeedback { get; } = new SimpleHapticsControllerFeedback[]
    {
        new SimpleHapticsControllerFeedback(KnownSimpleHapticsControllerWaveforms.Click, TimeSpan.FromMilliseconds(100)),
        new SimpleHapticsControllerFeedback(KnownSimpleHapticsControllerWaveforms.Press, TimeSpan.FromMilliseconds(300)),
        new SimpleHapticsControllerFeedback(KnownSimpleHapticsControllerWaveforms.Click, TimeSpan.FromMilliseconds(50)), // Single Click
        new SimpleHapticsControllerFeedback(KnownSimpleHapticsControllerWaveforms.Click, TimeSpan.FromMilliseconds(150)), // Double Click
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

        if (feedback.Duration.TotalMilliseconds <= 100)
        {
            // Single Click
            impact.ImpactOccurred();
        }
        else if (feedback.Duration.TotalMilliseconds <= 200)
        {
            // Potential Double Click - Check time interval with last click
            if (lastClickTime.HasValue && (DateTime.Now - lastClickTime.Value).TotalMilliseconds < 200)
            {
                // Double Click
                impact.ImpactOccurred();
            }
            else
            {
                // Treat as a single click if not a double click
                lastClickTime = DateTime.Now;
                impact.ImpactOccurred();
            }
        }
        else
        {
            // Handle other feedback types as before
            impact.ImpactOccurred();
        }
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
