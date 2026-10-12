#nullable enable

using System.Diagnostics;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// For a swapchain whose window keeps showing the last frame it presented: whether a frame that left the target
/// unchanged (<see cref="ISwapChain.PresentUnchanged"/>) can skip its present.
/// </summary>
internal sealed class RetainedPresentTracker
{
	// macOS reports the first presents after about a second of idle as never shown (presentedTime 0). The frames right
	// after one present again rather than trust the window, as they did before presents could be skipped.
	private const int PresentsAfterIdle = 3;

	private static readonly long IdleGap = Stopwatch.Frequency / 2;

	private long _lastPresent;
	private int _presentsSinceIdle;

	/// <summary>Whether the window shows the target's current pixels.</summary>
	public bool LastPresentSucceeded { get; private set; }

	/// <summary>Whether the last frame needed no present, the window already showing it.</summary>
	public bool LastPresentSkipped { get; private set; }

	/// <param name="timestamp">The <see cref="Stopwatch.GetTimestamp"/> time of the present.</param>
	public void OnPresented(bool succeeded, long timestamp)
	{
		_presentsSinceIdle = timestamp - _lastPresent > IdleGap ? 1 : _presentsSinceIdle + 1;
		_lastPresent = timestamp;
		LastPresentSkipped = false;
		LastPresentSucceeded = succeeded;
	}

	/// <summary>The target was reallocated, so the window has never shown its pixels.</summary>
	public void OnTargetReplaced() => LastPresentSucceeded = false;

	/// <summary>Records an unchanged frame at <paramref name="timestamp"/> as skipped, when the window already shows it.</summary>
	public bool TrySkipUnchanged(long timestamp)
		=> LastPresentSkipped = LastPresentSucceeded
			&& (_presentsSinceIdle >= PresentsAfterIdle || timestamp - _lastPresent > IdleGap);
}
