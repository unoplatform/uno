#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Wayland.Rendering;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class ConfigureTests
{
	private static WaylandWindowConfiguration Configuration(long generation)
		=> new(generation, new(800 + (int)generation, 600 + (int)generation, 32, 8, WaylandCaptionButtons.Close),
			1.25, 150, 1, WaylandToplevelStates.Resizing, "captured title", true, (uint)generation, generation);

	[TestMethod]
	public async Task SecondaryWindowFirstMapAcknowledgesBeforeDecorationAndContentBuffers()
	{
		var primaryUI = new Queue<Action>();
		var secondaryUI = new Queue<Action>();
		var primary = new WaylandConfigureScheduler(primaryUI.Enqueue, _ => { });
		WaylandConfigureScheduler? secondary = null;
		secondary = new WaylandConfigureScheduler(secondaryUI.Enqueue, configuration =>
		{
			if (!configuration.CanPresent)
			{
				// Match ApplyConfigurationUI: provisional bounds have no frame to complete them.
				secondary!.Complete(configuration.Generation, presented: false);
			}
		});
		var requests = new List<string>();
		uint? primaryAck = null;
		uint? secondaryAck = null;
		var coordinator = new WaylandPresentationCoordinator(new object(), (command, _) => command(), () => { });
		using var primaryPresenting = new ManualResetEventSlim();
		using var releasePrimary = new ManualResetEventSlim();
		using var secondaryRequested = new ManualResetEventSlim();

		primary.Enqueue(Configuration(54));
		primaryUI.Dequeue()();
		secondary.Enqueue(Configuration(0) with { Configured = false, Serial = null, SerialSequence = 0 });
		secondaryUI.Dequeue()();

		var first = Task.Run(() => coordinator.Submit(() => primary.PreparePresentation(54, serial =>
		{
			primaryAck = serial;
			requests.Add($"primary ack {serial}");
		}, configuration =>
		{
			Assert.AreEqual(configuration.Serial, primaryAck);
			requests.Add("primary CSD attach/commit");
			primaryPresenting.Set();
			Assert.IsTrue(releasePrimary.Wait(TimeSpan.FromSeconds(5)));
			requests.Add("primary content attach/commit");
			return true;
		}, out _)));
		Task<bool>? second = null;
		try
		{
			Assert.IsTrue(primaryPresenting.Wait(TimeSpan.FromSeconds(5)));
			Assert.IsFalse(secondary.PreparePresentation(0, _ => Assert.Fail("Provisional serial was acknowledged."),
				_ => throw new AssertFailedException("Provisional buffers were attached."), out _));
			// The initial configure arrives while the other window is presenting. Receipt sequences are per-window.
			secondary.Enqueue(Configuration(1) with
			{
				Serial = 55,
				Layout = new(1024, 640, 32, 8, WaylandCaptionButtons.Close),
				Scale = 1,
				PreferredScale = 0
			});
			Assert.AreEqual(1, secondaryUI.Count, "Completing provisional bounds must schedule the initial configure.");
			secondaryUI.Dequeue()();
			Assert.IsFalse(secondary.PreparePresentation(0, _ => Assert.Fail("A stale generation was acknowledged."),
				_ => throw new AssertFailedException("Stale buffers were attached."), out _));
			second = Task.Run(() =>
			{
				secondaryRequested.Set();
				return coordinator.Submit(() => secondary.PreparePresentation(1, serial =>
				{
					secondaryAck = serial;
					requests.Add($"secondary ack {serial}");
				}, configuration =>
				{
					Assert.AreEqual(55u, secondaryAck, "The initial ack must precede the first CSD attachment.");
					requests.Add("secondary CSD attach/commit");
					Assert.AreEqual(configuration.Serial, secondaryAck, "Content must use this surface's ack.");
					requests.Add("secondary content attach/commit");
					return true;
				}, out _));
			});
			Assert.IsTrue(secondaryRequested.Wait(TimeSpan.FromSeconds(5)));
			Assert.IsNull(secondaryAck, "The other window still owns the presentation opportunity.");
		}
		finally
		{
			releasePrimary.Set();
			await Task.WhenAll(first, second ?? Task.FromResult(false)).WaitAsync(TimeSpan.FromSeconds(5));
		}
		Assert.IsTrue(await first);
		Assert.IsTrue(await second!);
		CollectionAssert.AreEqual(new[]
		{
			"primary ack 54", "primary CSD attach/commit", "primary content attach/commit",
			"secondary ack 55", "secondary CSD attach/commit", "secondary content attach/commit"
		}, requests);
	}

	[TestMethod]
	public void DecorationPreparationRetryDoesNotAcknowledgeInitialConfigureTwice()
	{
		var ui = new Queue<Action>();
		var scheduler = new WaylandConfigureScheduler(ui.Enqueue, _ => { });
		var acknowledgements = new List<uint>();
		scheduler.Enqueue(Configuration(1) with { Serial = 55 });
		ui.Dequeue()();
		Assert.IsFalse(scheduler.PreparePresentation(1, acknowledgements.Add, configuration =>
		{
			CollectionAssert.AreEqual(new uint[] { 55 }, acknowledgements, "Ack must precede even a deferred CSD draw.");
			scheduler.HoldPresentation(configuration.Generation);
			return false;
		}, out _));
		scheduler.Enqueue(Configuration(2) with { Serial = 56 });
		scheduler.Complete(1, presented: false);
		Assert.AreEqual(0, ui.Count);
		Assert.IsTrue(scheduler.PreparePresentation(1, acknowledgements.Add, _ => true, out _));
		CollectionAssert.AreEqual(new uint[] { 55 }, acknowledgements);
		scheduler.Complete(1);
		ui.Dequeue()();
		Assert.IsTrue(scheduler.PreparePresentation(2, acknowledgements.Add, _ => true, out _));
		CollectionAssert.AreEqual(new uint[] { 55, 56 }, acknowledgements);
	}

	[TestMethod]
	public void ProvisionalSnapshotCannotMapWhenInitialConfigureArrivesDuringApplication()
	{
		var ui = new Queue<Action>();
		var acknowledgements = new List<uint>();
		var presentable = new List<long>();
		WaylandConfigureScheduler? scheduler = null;
		scheduler = new(ui.Enqueue, configuration =>
		{
			if (!configuration.Configured)
			{
				// Dispatch receives the initial configure after provisional bounds were published on UI.
				scheduler!.Enqueue(Configuration(2));
			}
			if (configuration.CanPresent)
			{
				presentable.Add(configuration.Generation);
				scheduler!.Acknowledge(configuration, acknowledgements.Add);
				Assert.AreEqual(configuration.Serial, acknowledgements[^1], "Initial ack must precede attachment.");
			}
			else
			{
				scheduler!.Complete(configuration.Generation, presented: false);
			}
		});
		scheduler.Enqueue(Configuration(1) with { Configured = false, Serial = null, SerialSequence = 0 });
		ui.Dequeue()();
		Assert.AreEqual(0, presentable.Count);
		Assert.AreEqual(0, acknowledgements.Count);
		ui.Dequeue()();
		CollectionAssert.AreEqual(new long[] { 2 }, presentable);
		CollectionAssert.AreEqual(new uint[] { 2 }, acknowledgements);
		Assert.IsFalse((Configuration(3) with { Configured = false }).CanPresent);
		Assert.IsFalse((Configuration(3) with { Serial = null }).CanPresent);
	}

	[TestMethod]
	[DataRow((uint)WaylandToplevelStates.Activated)]
	[DataRow((uint)WaylandToplevelStates.Maximized)]
	[DataRow((uint)WaylandToplevelStates.Fullscreen)]
	[DataRow((uint)WaylandToplevelStates.None)]
	public void StateChangesBypassInFlightResizeWithoutAcknowledgingIt(uint stateFlags)
	{
		var states = (WaylandToplevelStates)stateFlags;
		var ui = new Queue<Action>();
		var geometry = new List<WaylandWindowConfiguration>();
		var notified = new List<WaylandWindowConfiguration>();
		var acknowledgements = new List<uint>();
		var scheduler = new WaylandConfigureScheduler(ui.Enqueue, geometry.Add, notified.Add);
		var resize = Configuration(1);
		scheduler.Enqueue(resize);
		while (ui.TryDequeue(out var action))
		{
			action();
		}
		var state = Configuration(2) with { Layout = resize.Layout, States = states };
		scheduler.Enqueue(state);
		Assert.AreEqual(1, ui.Count, "Only the independent state notification can run without a frame.");
		ui.Dequeue()();
		Assert.AreEqual(states, notified[^1].States);
		Assert.AreEqual(1, geometry.Count, "State notification must preserve the in-flight geometry.");
		scheduler.AcknowledgeState(state, presentedGeneration: 0, acknowledgements.Add);
		Assert.AreEqual(0, acknowledgements.Count, "State ack must not overtake the unpresented resize.");
		scheduler.Acknowledge(resize, acknowledgements.Add);
		scheduler.Complete(1);
		ui.Dequeue()();
		scheduler.Acknowledge(state, acknowledgements.Add);
		CollectionAssert.AreEqual(new uint[] { 1, 2 }, acknowledgements);
	}

	[TestMethod]
	public void DecorationSnapshotRetainsSerialGeometryAndContentAcrossFallback()
	{
		var ui = new Queue<Action>();
		var scheduler = new WaylandConfigureScheduler(ui.Enqueue, _ => { });
		var presentation = new WaylandPresentationState();
		var acknowledgements = new List<uint>();
		var previous = Configuration(1);
		Assert.IsTrue(presentation.Submit(previous.Generation, previous.Layout, previous.Scale, () => true));
		scheduler.Enqueue(Configuration(2));
		ui.Dequeue()();
		var decorationSnapshot = scheduler.Current!;
		scheduler.HoldPresentation(decorationSnapshot.Generation);
		scheduler.Enqueue(Configuration(3));
		scheduler.Complete(decorationSnapshot.Generation, presented: false);
		Assert.AreEqual(0, ui.Count, "Fallback must not replace a configure with a cached child buffer.");
		Assert.AreSame(decorationSnapshot, scheduler.Current);
		Assert.IsFalse(presentation.Matches(decorationSnapshot.Layout, decorationSnapshot.Scale), "New CSD cannot be flushed with previous content.");
		Assert.IsTrue(presentation.Matches(previous.Layout, previous.Scale), "A title-only redraw may advance with matching old content.");
		Assert.IsFalse(presentation.Matches(previous.Layout, previous.Scale * 2));
		Assert.IsFalse(presentation.Submit(decorationSnapshot.Generation, decorationSnapshot.Layout, decorationSnapshot.Scale, () => false));
		scheduler.Complete(decorationSnapshot.Generation, presented: false);
		Assert.AreSame(decorationSnapshot, scheduler.Current);
		Assert.IsTrue(presentation.Submit(decorationSnapshot.Generation, decorationSnapshot.Layout, decorationSnapshot.Scale, () =>
		{
			scheduler.Acknowledge(decorationSnapshot, acknowledgements.Add);
			return true;
		}));
		scheduler.Complete(decorationSnapshot.Generation, presented: true);
		ui.Dequeue()();
		Assert.AreEqual(3L, scheduler.Current!.Generation);
		CollectionAssert.AreEqual(new uint[] { 2 }, acknowledgements);
		Assert.AreEqual(decorationSnapshot.Layout, presentation.Layout);
	}

	[TestMethod]
	public void CachedDecorationAlsoPinsAnAlreadyQueuedSuccessor()
	{
		var ui = new Queue<Action>();
		var scheduler = new WaylandConfigureScheduler(ui.Enqueue, _ => { });
		scheduler.Enqueue(Configuration(1));
		ui.Dequeue()();
		scheduler.Enqueue(Configuration(2));
		scheduler.Complete(1);
		// A repeated draw stages a child before the posted successor has reached the UI thread.
		scheduler.HoldPresentation(1);
		ui.Dequeue()();
		Assert.AreEqual(1L, scheduler.Current!.Generation);
		scheduler.Complete(1, presented: false);
		Assert.AreEqual(0, ui.Count);
		scheduler.Complete(1, presented: true);
		ui.Dequeue()();
		Assert.AreEqual(2L, scheduler.Current!.Generation);
	}

	[TestMethod]
	public void StateAckCannotOvertakeAnAcknowledgedButUnsubmittedResize()
	{
		var ui = new Queue<Action>();
		var scheduler = new WaylandConfigureScheduler(ui.Enqueue, _ => { });
		var acknowledgements = new List<uint>();
		scheduler.Enqueue(Configuration(1));
		ui.Dequeue()();
		scheduler.Acknowledge(scheduler.Current!, acknowledgements.Add);
		scheduler.AcknowledgeState(Configuration(2), presentedGeneration: 0, acknowledgements.Add);
		CollectionAssert.AreEqual(new uint[] { 1 }, acknowledgements);
		scheduler.Complete(1, presented: true);
		scheduler.AcknowledgeState(Configuration(2), presentedGeneration: 1, acknowledgements.Add);
		CollectionAssert.AreEqual(new uint[] { 1, 2 }, acknowledgements);
	}

	[TestMethod]
	public void FrameOpportunityAdvancesGeometryWithoutWaitingForRecordedLayout()
	{
		var ui = new Queue<Action>();
		var readiness = new WaylandFrameReadiness();
		var applied = new List<long>();
		var scheduler = new WaylandConfigureScheduler(ui.Enqueue, configuration =>
		{
			applied.Add(configuration.Generation);
			readiness.ConfigureUI(configuration.Generation, boundsChanged: true);
			readiness.ConfigureRender(configuration.Generation);
		});
		scheduler.Enqueue(Configuration(1));
		ui.Dequeue()();
		for (var frame = 1; frame <= 165; frame++)
		{
			for (var arrival = 1; arrival <= 4; arrival++)
			{
				scheduler.Enqueue(Configuration(1 + 4 * frame + arrival));
			}
			readiness.BeginFrame();
			Assert.IsFalse(readiness.IsFrameReady);
			// A display/stall opportunity is independent of whether Uno recorded the resize yet.
			scheduler.Complete(readiness.RenderGeneration, presented: false);
			Assert.AreEqual(1, ui.Count);
			ui.Dequeue()();
			Assert.AreEqual(frame + 1, applied.Count);
			Assert.AreEqual(0, ui.Count, "The opportunity must not spin or apply every incoming configure.");
		}
	}

	[TestMethod]
	public void DecorationWaitingForBufferAlsoPinsItsConfigureAcrossFallback()
	{
		var ui = new Queue<Action>();
		var scheduler = new WaylandConfigureScheduler(ui.Enqueue, _ => { });
		var redraw = new WaylandDecorationRedrawState();
		scheduler.Enqueue(Configuration(1));
		ui.Dequeue()();
		scheduler.HoldPresentation(1);
		Assert.IsTrue(redraw.BeginRedraw());
		redraw.CompleteRedraw(committed: false);
		scheduler.Enqueue(Configuration(2));
		scheduler.Complete(1, presented: false);
		Assert.IsTrue(redraw.Dirty);
		Assert.AreEqual(0, ui.Count, "A buffer release must still draw the original configure's decorations.");
		Assert.AreEqual(1L, scheduler.Current!.Generation);
		Assert.IsTrue(redraw.BeginRedraw());
		redraw.CompleteRedraw(committed: true);
		scheduler.Complete(1, presented: false);
		Assert.AreEqual(0, ui.Count);
		redraw.ParentCommitted();
		scheduler.Complete(1, presented: true);
		ui.Dequeue()();
		Assert.AreEqual(2L, scheduler.Current!.Generation);
	}

	[TestMethod]
	public void HalfPixelContentDoesNotUseUncorrectedNearestEvenBounds()
	{
		var geometry = WaylandWindowGeometry.Create(803, 535, 32, 1.5);
		Assert.AreEqual(802, (int)Math.Round(535 * 1.5f), "Uncorrected Uno bounds lose one buffer pixel.");
		Assert.AreEqual(803, geometry.BufferHeight);
		Assert.AreEqual(803, (int)Math.Round(geometry.Height * geometry.Scale));
	}

	[TestMethod]
	[DataRow(1280, 799, 32, 120u)]
	[DataRow(803, 535, 32, 180u)]
	[DataRow(801, 639, 32, 150u)]
	[DataRow(1280, 799, 0, 140u)]
	[DataRow(1025, 641, 32, 240u)]
	[DataRow(1280, 800, 0, 120u)]
	[DataRow(1280, 799, 0, 120u)]
	[DataRow(1019, 639, 32, 140u)]
	public void ContentBoundsAndBufferUseTheSamePixelGeometry(int width, int height, int titleBarHeight, uint preferredScale)
	{
		var scale = preferredScale / 120d;
		var geometry = WaylandWindowGeometry.Create(width, height, titleBarHeight, scale);
		var buffer = WaylandRenderTargetSize.GetSize(true, width, height, scale, 0, 0);
		// This is Uno's float DPI and nearest-even conversion at swapchain acquisition.
		Assert.AreEqual(buffer.Width, (int)Math.Round(geometry.Width * geometry.Scale));
		Assert.AreEqual(buffer.Height, (int)Math.Round(geometry.Height * geometry.Scale));
		Assert.AreEqual(buffer.Width, geometry.BufferWidth);
		Assert.AreEqual(buffer.Height, geometry.BufferHeight);
		Assert.AreEqual(WaylandRenderScale.ToBufferPixels(height + titleBarHeight, scale), geometry.OuterHeight);
		var readiness = new WaylandFrameReadiness();
		readiness.ConfigureUI(1, boundsChanged: true);
		readiness.OnFrameRecorded(arrangedAtBounds: true);
		readiness.ConfigureRender(1);
		readiness.BeginFrame();
		Assert.IsTrue(readiness.CanPresent(geometry.BufferWidth, geometry.BufferHeight, buffer.Width, buffer.Height));
		Assert.IsFalse(readiness.CanPresent(geometry.BufferWidth, geometry.BufferHeight - 1, buffer.Width, buffer.Height));
	}
}
