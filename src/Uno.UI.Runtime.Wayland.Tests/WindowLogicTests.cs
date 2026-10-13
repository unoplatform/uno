#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Wayland;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class WindowLogicTests
{
	private const uint Top = 1, Bottom = 2, Left = 4, Right = 8;

	private static byte[] Values(params uint[] values)
	{
		var bytes = new byte[values.Length * 4];
		for (var i = 0; i < values.Length; i++)
		{
			BitConverter.TryWriteBytes(bytes.AsSpan(i * 4), values[i]);
		}
		return bytes;
	}

	[TestMethod]
	public void InitialActivationRaisesCodeActivatedBeforePointerActivated()
	{
		CollectionAssert.AreEqual(
			new[] { CoreWindowActivationState.CodeActivated, CoreWindowActivationState.PointerActivated },
			WaylandToplevelStateParser.GetActivationSequence(WaylandToplevelStates.Activated, initialActivation: true).ToArray());
		CollectionAssert.AreEqual(
			new[] { CoreWindowActivationState.CodeActivated },
			WaylandToplevelStateParser.GetActivationSequence(WaylandToplevelStates.None, initialActivation: true).ToArray());
	}

	[TestMethod]
	public void LaterConfiguresOnlyFollowCompositorFocus()
	{
		CollectionAssert.AreEqual(
			new[] { CoreWindowActivationState.PointerActivated },
			WaylandToplevelStateParser.GetActivationSequence(WaylandToplevelStates.Activated | WaylandToplevelStates.Maximized, initialActivation: false).ToArray());
		CollectionAssert.AreEqual(
			new[] { CoreWindowActivationState.Deactivated },
			WaylandToplevelStateParser.GetActivationSequence(WaylandToplevelStates.None, initialActivation: false).ToArray());
	}

	[TestMethod]
	public void CompositorFullscreenEntryAndExitChangePresenter()
	{
		Assert.IsTrue(WaylandToplevelStateParser.HasPresenterChanged(WaylandToplevelStates.None, WaylandToplevelStates.Fullscreen, false, false));
		Assert.IsTrue(WaylandToplevelStateParser.HasPresenterChanged(WaylandToplevelStates.Fullscreen, WaylandToplevelStates.None, false, false));
		Assert.IsFalse(WaylandToplevelStateParser.HasPresenterChanged(WaylandToplevelStates.Fullscreen, WaylandToplevelStates.Fullscreen | WaylandToplevelStates.Activated, false, false));
	}

	[TestMethod]
	public void PresenterChangesIncludeSameSizeAndMinimizedTransitions()
	{
		Assert.IsTrue(WaylandToplevelStateParser.HasPresenterChanged(WaylandToplevelStates.None, WaylandToplevelStates.Maximized, false, false));
		Assert.IsTrue(WaylandToplevelStateParser.HasPresenterChanged(WaylandToplevelStates.Maximized, WaylandToplevelStates.Fullscreen, false, false));
		Assert.IsTrue(WaylandToplevelStateParser.HasPresenterChanged(WaylandToplevelStates.None, WaylandToplevelStates.Activated, true, false));
		Assert.IsTrue(WaylandToplevelStateParser.HasPresenterChanged(WaylandToplevelStates.None, WaylandToplevelStates.Maximized, true, true));
		Assert.IsFalse(WaylandToplevelStateParser.HasPresenterChanged(WaylandToplevelStates.None, WaylandToplevelStates.Activated | WaylandToplevelStates.Resizing, false, false));
	}

	[TestMethod]
	public void ToplevelStatesParseKnownValuesOnly()
	{
		Assert.AreEqual(WaylandToplevelStates.None, WaylandToplevelStateParser.ParseStates([]));
		Assert.AreEqual(
			WaylandToplevelStates.Maximized | WaylandToplevelStates.Activated | WaylandToplevelStates.TiledLeft,
			WaylandToplevelStateParser.ParseStates(Values(1, 4, 5, 99, 13)));
		// A truncated trailing value is ignored.
		Assert.AreEqual(WaylandToplevelStates.Fullscreen, WaylandToplevelStateParser.ParseStates(Values(2).Concat(new byte[] { 4, 0 }).ToArray()));
	}

	[TestMethod]
	public void WindowCapabilitiesParse()
	{
		Assert.AreEqual(
			WaylandWindowCapabilities.WindowMenu | WaylandWindowCapabilities.Minimize,
			WaylandToplevelStateParser.ParseCapabilities(Values(1, 4, 0, 7)));
	}

	[TestMethod]
	public void DecorationSurfaceSurroundsGeometry()
	{
		var layout = new WaylandDecorationLayout(800, 600, 32, 8, WaylandCaptionButtons.Minimize | WaylandCaptionButtons.Maximize | WaylandCaptionButtons.Close);
		Assert.AreEqual((-8, -40, 816, 648), (layout.SurfaceX, layout.SurfaceY, layout.SurfaceWidth, layout.SurfaceHeight));
		Assert.AreEqual((-32, 632), (layout.GeometryY, layout.GeometryHeight));
		Assert.IsTrue(layout.TryGetButton(WaylandDecorationPart.Close, out var x, out var y, out var width, out var height));
		Assert.AreEqual((762, 8, 46, 32), (x, y, width, height));
		Assert.IsTrue(layout.TryGetButton(WaylandDecorationPart.Minimize, out x, out _, out _, out _));
		Assert.AreEqual(670, x);
		Assert.AreEqual(670, layout.ButtonsLeft);
		Assert.IsFalse(default(WaylandDecorationLayout).IsVisible);
	}

	[TestMethod]
	public void DecorationHitTest()
	{
		var layout = new WaylandDecorationLayout(800, 600, 32, 8, WaylandCaptionButtons.Maximize | WaylandCaptionButtons.Close);
		Assert.AreEqual((WaylandDecorationPart.TitleBar, 0u), layout.HitTest(100, 20));
		Assert.AreEqual((WaylandDecorationPart.Close, 0u), layout.HitTest(780, 20));
		Assert.AreEqual((WaylandDecorationPart.Maximize, 0u), layout.HitTest(740, 20));
		// No minimize button: its slot belongs to the title bar.
		Assert.AreEqual((WaylandDecorationPart.TitleBar, 0u), layout.HitTest(690, 20));
		Assert.AreEqual((WaylandDecorationPart.None, 0u), layout.HitTest(100, 300));
		Assert.AreEqual((WaylandDecorationPart.ResizeEdge, Top), layout.HitTest(400, 2));
		Assert.AreEqual((WaylandDecorationPart.ResizeEdge, Left), layout.HitTest(2, 300));
		Assert.AreEqual((WaylandDecorationPart.ResizeEdge, Top | Left), layout.HitTest(2, 2));
		// Corner zones extend along the edges.
		Assert.AreEqual((WaylandDecorationPart.ResizeEdge, Top | Left), layout.HitTest(20, 2));
		Assert.AreEqual((WaylandDecorationPart.ResizeEdge, Bottom | Right), layout.HitTest(815, 647));
		Assert.AreEqual((WaylandDecorationPart.ResizeEdge, Bottom | Right), layout.HitTest(812, 630));
		Assert.AreEqual((WaylandDecorationPart.None, 0u), layout.HitTest(-1, 300));
	}

	[TestMethod]
	public void ResizeWaitsForFrameRecordedAfterLayout()
	{
		var readiness = new WaylandFrameReadiness();
		Assert.IsFalse(readiness.ConfigureUI(1, boundsChanged: false));
		readiness.ConfigureRender(1);
		readiness.BeginFrame();
		Assert.IsTrue(readiness.CanPresent(800, 600, 800, 600));

		Assert.IsTrue(readiness.ConfigureUI(2, boundsChanged: true));
		readiness.ConfigureRender(2);
		// The canvas already has the new size, but the recorded picture still has the old layout.
		readiness.BeginFrame();
		Assert.IsFalse(readiness.CanPresent(900, 700, 900, 700));
		Assert.IsFalse(readiness.OnFrameRecorded(arrangedAtBounds: false));
		readiness.BeginFrame();
		Assert.IsFalse(readiness.CanPresent(900, 700, 900, 700));
		Assert.IsTrue(readiness.OnFrameRecorded(arrangedAtBounds: true));
		readiness.BeginFrame();
		Assert.IsTrue(readiness.CanPresent(900, 700, 900, 700));
		Assert.IsFalse(readiness.OnFrameRecorded(arrangedAtBounds: true));
	}

	[TestMethod]
	public void PictureTakenBeforeLayoutWasRecordedIsNotPresented()
	{
		var readiness = new WaylandFrameReadiness();
		readiness.ConfigureUI(1, boundsChanged: true);
		readiness.ConfigureRender(1);
		readiness.BeginFrame();
		// The UI records the new layout while the render thread still draws the older picture.
		Assert.IsTrue(readiness.OnFrameRecorded(arrangedAtBounds: true));
		Assert.IsFalse(readiness.CanPresent(800, 600, 800, 600));
		readiness.BeginFrame();
		Assert.IsTrue(readiness.CanPresent(800, 600, 800, 600));
	}

	[TestMethod]
	public void ConfigureDuringLayoutWaitKeepsWaiting()
	{
		var readiness = new WaylandFrameReadiness();
		Assert.IsTrue(readiness.ConfigureUI(1, boundsChanged: true));
		// A state-only configure must not release the resize whose layout was not recorded yet.
		Assert.IsTrue(readiness.ConfigureUI(2, boundsChanged: false));
		readiness.ConfigureRender(2);
		readiness.BeginFrame();
		Assert.IsFalse(readiness.CanPresent(800, 600, 800, 600));
		Assert.IsTrue(readiness.OnFrameRecorded(arrangedAtBounds: true));
		readiness.BeginFrame();
		Assert.IsTrue(readiness.CanPresent(800, 600, 800, 600));
	}

	[TestMethod]
	public void SupersededGenerationNeverPresents()
	{
		var readiness = new WaylandFrameReadiness();
		readiness.ConfigureUI(2, boundsChanged: false);
		readiness.ConfigureRender(1);
		for (var i = 0; i < 2 * WaylandFrameReadiness.MaxStaleFrames; i++)
		{
			readiness.BeginFrame();
			Assert.IsFalse(readiness.CanPresent(800, 600, 800, 600));
		}
		readiness.ConfigureRender(2);
		readiness.BeginFrame();
		Assert.IsTrue(readiness.CanPresent(800, 600, 800, 600));
	}

	[TestMethod]
	public void LayoutWaitIsBoundedButMismatchedBuffersNeverPresent()
	{
		var readiness = new WaylandFrameReadiness();
		readiness.ConfigureUI(1, boundsChanged: true);
		for (var i = 0; i < WaylandFrameReadiness.MaxStaleFrames; i++)
		{
			Assert.IsFalse(readiness.OnFrameRecorded(arrangedAtBounds: false));
		}
		Assert.IsTrue(readiness.OnFrameRecorded(arrangedAtBounds: false));

		readiness.ConfigureRender(1);
		readiness.BeginFrame();
		Assert.IsTrue(readiness.CanPresent(101, 101, 101, 101));
		Assert.IsFalse(readiness.CanPresent(100, 100, 101, 101));
		for (var i = 0; i < 2 * WaylandFrameReadiness.MaxStaleFrames; i++)
		{
			readiness.BeginFrame();
			Assert.IsFalse(readiness.CanPresent(80, 80, 101, 101));
		}
		readiness.BeginFrame();
		Assert.IsTrue(readiness.CanPresent(101, 101, 101, 101));
	}

	[TestMethod]
	public void StaleGenerationIsDetectedBeforeDrawing()
	{
		var readiness = new WaylandFrameReadiness();
		Assert.IsFalse(readiness.ConfigureUI(1, boundsChanged: false));
		readiness.ConfigureRender(1);
		readiness.BeginFrame();
		Assert.IsTrue(readiness.IsFrameReady);

		// The UI begins a newer configure: the render thread must not replay the old frame for it.
		Assert.IsTrue(readiness.ConfigureUI(2, boundsChanged: true));
		readiness.ConfigureRender(2);
		readiness.BeginFrame();
		Assert.IsFalse(readiness.IsFrameReady, "The layout is not recorded yet.");
		Assert.IsTrue(readiness.OnFrameRecorded(arrangedAtBounds: true));
		readiness.BeginFrame();
		Assert.IsTrue(readiness.IsFrameReady);
		Assert.IsFalse(readiness.CanPresent(900, 700, 800, 600), "A current generation still needs matching frame and buffer sizes.");
		Assert.IsTrue(readiness.CanPresent(900, 700, 900, 700));
	}

	private static WaylandWindowConfiguration Configuration(long generation, uint? serial = null, long? serialSequence = null)
		=> new(generation, new(800 + (int)generation, 600 + (int)generation, 32, 8, WaylandCaptionButtons.Close),
			1, 0, 1, WaylandToplevelStates.Resizing, "test", true, serial ?? (uint)generation, serialSequence ?? generation);

	[TestMethod]
	public void ConfigureBurstDoesNotReplaceLayoutInFlight()
	{
		var ui = new Queue<Action>();
		var applied = new List<WaylandWindowConfiguration>();
		var scheduler = new WaylandConfigureScheduler(ui.Enqueue, applied.Add);
		scheduler.Enqueue(Configuration(1));
		scheduler.Enqueue(Configuration(2));
		Assert.AreEqual(1, ui.Count);
		ui.Dequeue()();
		Assert.AreEqual(2L, scheduler.Current!.Generation);
		for (var generation = 3; generation <= 200; generation++)
		{
			scheduler.Enqueue(Configuration(generation));
		}
		Assert.AreEqual(0, ui.Count, "New arrivals must not restart an in-flight layout.");
		Assert.AreEqual(2L, scheduler.Current!.Generation);
		scheduler.Complete(1);
		Assert.AreEqual(0, ui.Count, "An older frame must not release the applied configure.");
		scheduler.Complete(2);
		scheduler.Complete(2);
		Assert.AreEqual(1, ui.Count, "Completion must schedule exactly one dispatcher turn.");
		ui.Dequeue()();
		CollectionAssert.AreEqual(new long[] { 2, 200 }, applied.Select(configuration => configuration.Generation).ToArray());
		scheduler.Complete(200);
		Assert.AreEqual(0, ui.Count, "An idle configure scheduler must not spin.");
	}

	[TestMethod]
	public void CompletedLayoutAcknowledgesItsReceivedSerialDespiteNewerPendingConfigure()
	{
		var ui = new Queue<Action>();
		var scheduler = new WaylandConfigureScheduler(ui.Enqueue, _ => { });
		var acknowledgements = new List<uint>();
		scheduler.Enqueue(Configuration(1, 42));
		ui.Dequeue()();
		var inFlight = scheduler.Current!;
		scheduler.Enqueue(Configuration(2, 43));
		scheduler.Acknowledge(inFlight, acknowledgements.Add);
		scheduler.Acknowledge(inFlight, acknowledgements.Add);
		CollectionAssert.AreEqual(new uint[] { 42 }, acknowledgements);
		scheduler.Complete(1);
		ui.Dequeue()();
		scheduler.Acknowledge(scheduler.Current!, acknowledgements.Add);
		scheduler.Acknowledge(inFlight, acknowledgements.Add);
		scheduler.Complete(2);
		// A local scale change retains the received serial, which must not be acknowledged twice.
		scheduler.Enqueue(Configuration(3, 43, 2));
		ui.Dequeue()();
		scheduler.Acknowledge(scheduler.Current!, acknowledgements.Add);
		CollectionAssert.AreEqual(new uint[] { 42, 43 }, acknowledgements);
	}

	[TestMethod]
	public void ConfigureAcknowledgementsFollowReceiptOrderAcrossSerialWraparound()
	{
		var ui = new Queue<Action>();
		var scheduler = new WaylandConfigureScheduler(ui.Enqueue, _ => { });
		var acknowledgements = new List<uint>();
		scheduler.Enqueue(Configuration(1, uint.MaxValue));
		ui.Dequeue()();
		scheduler.Acknowledge(scheduler.Current!, acknowledgements.Add);
		scheduler.Complete(1);
		scheduler.Enqueue(Configuration(2, 0));
		ui.Dequeue()();
		scheduler.Acknowledge(scheduler.Current!, acknowledgements.Add);
		CollectionAssert.AreEqual(new uint[] { uint.MaxValue, 0 }, acknowledgements);
	}

	[TestMethod]
	public void ConfigureFloodCannotResetLayoutFallback()
	{
		var ui = new Queue<Action>();
		var readiness = new WaylandFrameReadiness();
		var scheduler = new WaylandConfigureScheduler(ui.Enqueue, configuration =>
		{
			readiness.ConfigureUI(configuration.Generation, boundsChanged: true);
			readiness.ConfigureRender(configuration.Generation);
		});
		scheduler.Enqueue(Configuration(1));
		ui.Dequeue()();
		for (var frame = 0; frame <= WaylandFrameReadiness.MaxStaleFrames; frame++)
		{
			scheduler.Enqueue(Configuration(2 + frame));
			Assert.AreEqual(frame == WaylandFrameReadiness.MaxStaleFrames, readiness.OnFrameRecorded(arrangedAtBounds: false));
		}
		readiness.BeginFrame();
		Assert.IsTrue(readiness.IsFrameReady);
		Assert.AreEqual(1L, scheduler.Current!.Generation);
		// The hidden-surface fallback consumes this ready frame without committing it.
		scheduler.Complete(readiness.RenderGeneration);
		Assert.AreEqual(1, ui.Count);
		ui.Dequeue()();
		Assert.AreEqual(2L + WaylandFrameReadiness.MaxStaleFrames, scheduler.Current!.Generation);
	}

	[TestMethod]
	public void CoalescedLocalChangesRetainActivationNotificationAndDoNotInventSerials()
	{
		var ui = new Queue<Action>();
		var scheduler = new WaylandConfigureScheduler(ui.Enqueue, _ => { });
		scheduler.Enqueue(Configuration(1) with { Serial = null, SerialSequence = 0 });
		scheduler.Enqueue(Configuration(2) with { NotifyActivation = false, Serial = null, SerialSequence = 0 });
		ui.Dequeue()();
		Assert.IsTrue(scheduler.Current!.NotifyActivation);
		scheduler.Acknowledge(scheduler.Current!, _ => Assert.Fail("No configure serial was received."));
	}

	[TestMethod]
	public void TinyDecorationEdgesAreValid()
	{
		uint[] validEdges = [Top, Bottom, Left, Right, Top | Left, Top | Right, Bottom | Left, Bottom | Right];
		for (var width = 1; width <= 33; width++)
		{
			for (var height = 1; height <= 33; height++)
			{
				var layout = new WaylandDecorationLayout(width, height, 0, 8, WaylandCaptionButtons.None);
				for (var x = 0; x < layout.SurfaceWidth; x++)
				{
					for (var y = 0; y < layout.SurfaceHeight; y++)
					{
						var hit = layout.HitTest(x, y);
						if (hit.Part == WaylandDecorationPart.ResizeEdge)
						{
							CollectionAssert.Contains(validEdges, hit.Edges);
						}
					}
				}
			}
		}
		Assert.AreEqual((WaylandDecorationPart.ResizeEdge, Top | Right), new WaylandDecorationLayout(20, 20, 0, 8, WaylandCaptionButtons.None).HitTest(18, 2));
	}

	[TestMethod]
	public void TitleBarFitsConstrainedGeometry()
	{
		for (var height = 1; height <= 64; height++)
		{
			var titleBar = WaylandDecorationLayout.FitTitleBar(height, 32);
			var layout = new WaylandDecorationLayout(20, height - titleBar, titleBar, 0, WaylandCaptionButtons.Close);
			Assert.AreEqual(height, layout.GeometryHeight);
			Assert.IsTrue(layout.ContentHeight >= 1);
		}
	}

	[TestMethod]
	public void MaximizedLayoutHasNoResizeBorder()
	{
		var layout = new WaylandDecorationLayout(800, 600, 32, 0, WaylandCaptionButtons.Close);
		Assert.AreEqual((0, -32, 800, 632), (layout.SurfaceX, layout.SurfaceY, layout.SurfaceWidth, layout.SurfaceHeight));
		Assert.AreEqual((WaylandDecorationPart.TitleBar, 0u), layout.HitTest(0, 0));
		Assert.AreEqual((WaylandDecorationPart.Close, 0u), layout.HitTest(799, 31));
		Assert.AreEqual((WaylandDecorationPart.None, 0u), layout.HitTest(400, 40));
	}
}
