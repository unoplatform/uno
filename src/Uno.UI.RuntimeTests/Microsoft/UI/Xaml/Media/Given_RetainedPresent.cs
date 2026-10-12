#if __SKIA__
using System.Diagnostics;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media;

/// <summary>
/// A frame that left the target as it was may skip its present, on a window that keeps showing the last one. Getting
/// that wrong leaves stale pixels on screen, so every way a frame can change the window must rule the skip out.
/// </summary>
[TestClass]
public class Given_RetainedPresent
{
	private static readonly Size Bounds = new(100, 100);
	private static readonly long Origin = 1000 * Stopwatch.Frequency;
	private static readonly long Frame = Stopwatch.Frequency / 60;

	[TestMethod]
	public void When_First_Frame_Then_Not_Skipped()
	{
		Assert.IsTrue(CompositionTarget.IsResized(Size.Empty, 1, Bounds, 1), "the first frame sizes the target");
		Assert.IsFalse(new RetainedPresentTracker().TrySkipUnchanged(Origin), "the window has shown nothing yet");
	}

	[TestMethod]
	public void When_Resized_Or_Rescaled_Then_Not_Unchanged()
	{
		Assert.IsTrue(CompositionTarget.IsResized(Bounds, 1, new Size(120, 100), 1));
		Assert.IsTrue(CompositionTarget.IsResized(Bounds, 1, Bounds, 2), "a scale change repaints at the new density");
		Assert.IsFalse(CompositionTarget.IsResized(Bounds, 2, Bounds, 2));

		Assert.IsFalse(CompositionTarget.IsTargetUnchanged(resized: true, damage: null, preservesContents: true, damageOverlay: false, forceFullRepaint: false));
	}

	[TestMethod]
	public void When_Nothing_Damaged_On_A_Retained_Target_Then_Unchanged()
	{
		Assert.IsTrue(CompositionTarget.IsTargetUnchanged(resized: false, damage: null, preservesContents: true, damageOverlay: false, forceFullRepaint: false));

		Assert.IsFalse(CompositionTarget.IsTargetUnchanged(false, [new Rect(0, 0, 1, 1)], true, false, false), "damage repaints");
		Assert.IsFalse(CompositionTarget.IsTargetUnchanged(false, null, preservesContents: false, false, false), "a target that loses its pixels is repainted whole");
		Assert.IsFalse(CompositionTarget.IsTargetUnchanged(false, null, true, damageOverlay: true, false));
		Assert.IsFalse(CompositionTarget.IsTargetUnchanged(false, null, true, false, forceFullRepaint: true));
	}

	[TestMethod]
	public void When_An_Overlay_Draws_Then_Not_Skipped()
	{
		Assert.IsTrue(CompositionTarget.CanSkipPresent(targetUnchanged: true, fpsOverlay: false, hostOverlay: false));
		Assert.IsFalse(CompositionTarget.CanSkipPresent(true, fpsOverlay: true, hostOverlay: false), "the frame counter draws every frame");
		Assert.IsFalse(CompositionTarget.CanSkipPresent(true, fpsOverlay: false, hostOverlay: true), "a host overlay draws every frame");
		Assert.IsFalse(CompositionTarget.CanSkipPresent(false, false, false));
	}

	[TestMethod]
	public void When_Last_Present_Reached_The_Window_Then_Skipped()
	{
		var tracker = Presented(3);

		Assert.IsTrue(tracker.TrySkipUnchanged(Origin + 3 * Frame));
		Assert.IsTrue(tracker.LastPresentSkipped);
		Assert.IsTrue(tracker.TrySkipUnchanged(Origin + 4 * Frame), "the window keeps showing it");
	}

	/// <summary>macOS reports the first presents after an idle second as never shown, so those are not relied on.</summary>
	[TestMethod]
	public void When_Presented_After_Idle_Then_The_Next_Frames_Present_Again()
	{
		var tracker = Presented(3);
		var later = Origin + 2 * Stopwatch.Frequency;

		tracker.OnPresented(succeeded: true, later);
		Assert.IsFalse(tracker.TrySkipUnchanged(later + Frame));
		tracker.OnPresented(succeeded: true, later + Frame);
		Assert.IsFalse(tracker.TrySkipUnchanged(later + 2 * Frame));
		tracker.OnPresented(succeeded: true, later + 2 * Frame);

		Assert.IsTrue(tracker.TrySkipUnchanged(later + 3 * Frame));
	}

	[TestMethod]
	public void When_Idle_Since_The_Last_Present_Then_Skipped()
	{
		var tracker = new RetainedPresentTracker();
		tracker.OnPresented(succeeded: true, Origin);

		Assert.IsTrue(tracker.TrySkipUnchanged(Origin + 2 * Stopwatch.Frequency), "the window had all the time it needs to show it");
	}

	[TestMethod]
	public void When_Last_Present_Failed_Then_Presented_Again()
	{
		var tracker = Presented(3);
		tracker.OnPresented(succeeded: false, Origin + 3 * Frame);

		Assert.IsFalse(tracker.TrySkipUnchanged(Origin + 4 * Frame), "the target holds a frame the window never got");
		Assert.IsFalse(tracker.LastPresentSkipped);
	}

	[TestMethod]
	public void When_Target_Replaced_Then_Presented_Again()
	{
		var tracker = Presented(3);
		tracker.OnTargetReplaced();

		Assert.IsFalse(tracker.TrySkipUnchanged(Origin + 3 * Frame));
	}

	private static RetainedPresentTracker Presented(int frames)
	{
		var tracker = new RetainedPresentTracker();
		for (var i = 0; i < frames; i++)
		{
			tracker.OnPresented(succeeded: true, Origin + i * Frame);
		}

		return tracker;
	}
}
#endif
