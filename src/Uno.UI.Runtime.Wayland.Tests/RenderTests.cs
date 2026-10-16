#nullable enable

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Composition.Drawing;
using Uno.UI.Runtime.Wayland;
using Uno.UI.Runtime.Wayland.Protocols;
using Uno.UI.Runtime.Wayland.Rendering;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.Runtime.Wayland.Tests;

public sealed partial class DisplayTests
{
	[TestMethod]
	public void EglConfigPrefersNonblockingSwaps()
	{
		IntPtr[] configs = [new(1), new(2), new(3)];
		Assert.AreEqual(configs[2], WaylandEglConfig.Select(configs, config => config == configs[0] ? 1 : config == configs[1] ? null : 0, _ => 0, _ => 0));
	}

	[TestMethod]
	public void EglConfigRejectsClampedOrUnknownSwapIntervals()
	{
		IntPtr[] configs = [new(1), new(2)];
		Assert.ThrowsExactly<NotSupportedException>(() => WaylandEglConfig.Select(configs, config => config == configs[0] ? 1 : null, _ => 0, _ => 0));
		Assert.ThrowsExactly<NotSupportedException>(() => WaylandEglConfig.Select([], _ => 0, _ => 0, _ => 0));
	}

	[TestMethod]
	public void FrameSchedulerProducesFramesWhileFrameCallbacksAreWithheld()
	{
		using var peer = new ProtocolPeer();
		using var display = peer.Connect();
		using var compositor = (WlCompositor)display.Bind(WlCompositor.ProtocolName, WlCompositor.MaximumVersion);
		using var surface = compositor.CreateSurface();
		var owner = new BlockingCollection<Action>();
		var renders = 0;
		var stalls = 0;
		// The peer never answers wl_surface.frame, like a compositor hiding the surface.
		using var scheduler = new WaylandFrameScheduler(surface, () => ++renders > 0, () => stalls++, owner.Add);
		scheduler.Invalidate();
		scheduler.Invalidate();
		Assert.AreEqual(1, renders, "The second frame must wait for the frame callback.");
		Assert.IsTrue(owner.TryTake(out var tick, TimeSpan.FromSeconds(5)), "The stall timer did not fire.");
		tick();
		Assert.AreEqual(1, stalls);
		Assert.AreEqual(1, renders);
		Assert.IsFalse(owner.TryTake(out _, WaylandFrameScheduler.StallInterval * 3), "An idle UI must not keep producing frames.");
		scheduler.Invalidate();
		Assert.IsTrue(owner.TryTake(out tick, TimeSpan.FromSeconds(5)));
		tick();
		Assert.AreEqual(2, stalls);
	}

	[TestMethod]
	public void FrameSchedulerDoesNotStallWithoutPendingFrames()
	{
		using var peer = new ProtocolPeer();
		using var display = peer.Connect();
		using var compositor = (WlCompositor)display.Bind(WlCompositor.ProtocolName, WlCompositor.MaximumVersion);
		using var surface = compositor.CreateSurface();
		var owner = new BlockingCollection<Action>();
		var stalls = 0;
		using var scheduler = new WaylandFrameScheduler(surface, () => true, () => stalls++, owner.Add);
		scheduler.Invalidate();
		Assert.IsFalse(owner.TryTake(out _, WaylandFrameScheduler.StallInterval * 3));
		Assert.AreEqual(0, stalls);
	}

	[TestMethod]
	[DataRow(60)]
	[DataRow(165)]
	public void ConfigureFloodPresentsAtDisplayRate(int refreshRate)
	{
		using var peer = new ProtocolPeer();
		using var display = peer.Connect();
		using var compositor = (WlCompositor)display.Bind(WlCompositor.ProtocolName, WlCompositor.MaximumVersion);
		using var surface = compositor.CreateSurface();
		using var owner = new BlockingCollection<Action>();
		var ui = new Queue<Action>();
		var readiness = new WaylandFrameReadiness();
		var acknowledgements = new List<uint>();
		var presentations = new List<WaylandWindowConfiguration>();
		var draws = 0;
		long received = 0;
		WaylandFrameScheduler? frames = null;
		WaylandWindowConfiguration? applied = null;
		var configures = new WaylandConfigureScheduler(ui.Enqueue, configuration =>
		{
			applied = configuration;
			readiness.ConfigureUI(configuration.Generation, boundsChanged: true);
			readiness.ConfigureRender(configuration.Generation);
			frames!.Invalidate();
		});
		using var scheduler = frames = new WaylandFrameScheduler(surface, () =>
		{
			draws++;
			readiness.BeginFrame();
			var configuration = configures.Current!;
			var layout = configuration.Layout;
			var width = WaylandRenderScale.ToBufferPixels(layout.ContentWidth, configuration.Scale);
			var height = WaylandRenderScale.ToBufferPixels(layout.ContentHeight, configuration.Scale);
			if (!readiness.CanPresent(width, height, width, height))
			{
				return false;
			}
			Assert.AreEqual(applied, configuration, "The buffer must reflect the UI's applied configure.");
			Assert.IsTrue(configuration.SerialSequence <= received, "Only received serials may be acknowledged.");
			Assert.IsFalse(readiness.CanPresent(width - 2, height, width, height), "A stale-size buffer must never commit.");
			configures.Acknowledge(configuration, acknowledgements.Add);
			surface.Commit();
			presentations.Add(configuration);
			configures.Complete(configuration.Generation);
			return true;
		}, () => { }, owner.Add);
		display.StartDispatch();

		void Receive(long generation)
		{
			received = generation;
			configures.Enqueue(new(generation, new(800 + (int)generation, 600 + (int)generation, 32, 8, WaylandCaptionButtons.Close),
				1.25, 150, 1, WaylandToplevelStates.Resizing, "test", true, (uint)generation, generation));
		}
		void ApplyPending()
		{
			Assert.IsTrue(ui.Count <= 1, "Configure arrivals must coalesce into one dispatcher turn.");
			if (ui.TryDequeue(out var apply))
			{
				apply();
			}
		}
		void RecordAndPresent(int tick)
		{
			Assert.IsTrue(readiness.OnFrameRecorded(arrangedAtBounds: true));
			var before = presentations.Count;
			var drawsBefore = draws;
			scheduler.Invalidate();
			if (before != 0)
			{
				Assert.AreEqual(before, presentations.Count, "A pending callback must prevent a second draw.");
				Assert.AreEqual(drawsBefore, draws, "Invalidation must not draw while a callback is pending.");
				peer.CompleteFrame((uint)(tick / 33));
				var deadline = Stopwatch.StartNew();
				while (presentations.Count == before && deadline.Elapsed < TimeSpan.FromSeconds(5))
				{
					Assert.IsTrue(owner.TryTake(out var action, TimeSpan.FromSeconds(5)));
					action();
				}
			}
			Assert.AreEqual(before + 1, presentations.Count);
			Assert.AreEqual(drawsBefore + 1, draws, "Each display opportunity must draw exactly once.");
		}

		// Virtual time: 33,000 ticks/s represents both 200 configures/s and 60/165 display callbacks/s exactly.
		Receive(1);
		ApplyPending();
		RecordAndPresent(0);
		for (var tick = 1; tick <= 33000; tick++)
		{
			if (tick < 33000 && tick % 165 == 0)
			{
				Receive(1 + tick / 165);
			}
			ApplyPending();
			if (tick % (33000 / refreshRate) == 0)
			{
				RecordAndPresent(tick);
			}
		}
		Assert.AreEqual(refreshRate + 1, presentations.Count, "Resizing must keep presenting before the configure stream stops.");
		Assert.IsTrue(acknowledgements.Zip(acknowledgements.Skip(1), (previous, next) => next > previous).All(ordered => ordered));
		Assert.IsTrue(acknowledgements.All(serial => serial <= 200), "Only received serials may be acknowledged.");
		ApplyPending();
		RecordAndPresent(33000 + 33000 / refreshRate);
		Assert.AreEqual(200u, acknowledgements[^1], "The last pending geometry must eventually be presented.");
		Assert.AreEqual(0, ui.Count);
		display.StopDispatch();
	}

	[TestMethod]
	public void StateChangesApplyWhileResizeFrameCallbackIsWithheld()
	{
		using var peer = new ProtocolPeer();
		using var display = peer.Connect();
		using var compositor = (WlCompositor)display.Bind(WlCompositor.ProtocolName, WlCompositor.MaximumVersion);
		using var surface = compositor.CreateSurface();
		using var owner = new BlockingCollection<Action>();
		var ui = new Queue<Action>();
		var geometry = new List<long>();
		var states = new List<WaylandToplevelStates>();
		var acknowledgements = new List<uint>();
		var draws = 0;
		using var frames = new WaylandFrameScheduler(surface, () =>
		{
			draws++;
			surface.Commit();
			return true;
		}, () => { }, owner.Add);
		// The previously presented buffer has an outstanding callback that the peer withholds.
		frames.Invalidate();
		var configures = new WaylandConfigureScheduler(ui.Enqueue, configuration =>
		{
			geometry.Add(configuration.Generation);
			frames.Invalidate();
		}, configuration => states.Add(configuration.States));
		var resize = new WaylandWindowConfiguration(1, new(1280, 799, 32, 8, WaylandCaptionButtons.Close),
			1, 0, 1, WaylandToplevelStates.Resizing, "test", true, 42, 1);
		configures.Enqueue(resize);
		while (ui.TryDequeue(out var apply))
		{
			apply();
		}
		foreach (var state in new[] { WaylandToplevelStates.Activated, WaylandToplevelStates.Maximized, WaylandToplevelStates.Fullscreen, WaylandToplevelStates.None })
		{
			var configuration = resize with { Generation = 2, Serial = 43, SerialSequence = 2, States = state };
			configures.Enqueue(configuration);
			Assert.AreEqual(1, ui.Count);
			ui.Dequeue()();
			Assert.AreEqual(state, states[^1]);
			configures.AcknowledgeState(configuration, presentedGeneration: 0, acknowledgements.Add);
		}
		Assert.AreEqual(1, draws);
		CollectionAssert.AreEqual(new long[] { 1 }, geometry);
		Assert.AreEqual(0, acknowledgements.Count, "State notifications must not acknowledge past the blocked resize.");
		configures.Acknowledge(resize, acknowledgements.Add);
		configures.Complete(1, presented: true);
		ui.Dequeue()();
		configures.Acknowledge(configures.Current!, acknowledgements.Add);
		CollectionAssert.AreEqual(new uint[] { 42, 43 }, acknowledgements);
	}

	[TestMethod]
	[DataRow(60)]
	[DataRow(165)]
	public void RecordedConfigureFloodAppliesAndAcknowledgesAtDisplayRate(int refreshRate)
	{
		using var peer = new ProtocolPeer();
		using var display = peer.Connect();
		using var compositor = (WlCompositor)display.Bind(WlCompositor.ProtocolName, WlCompositor.MaximumVersion);
		using var surface = compositor.CreateSurface();
		using var owner = new BlockingCollection<Action>();
		var ui = new Queue<Action>();
		var readiness = new WaylandFrameReadiness();
		var applied = new List<long>();
		var presented = new List<uint>();
		var acknowledgements = new List<uint>();
		WaylandFrameScheduler? frames = null;
		var configures = new WaylandConfigureScheduler(ui.Enqueue, configuration =>
		{
			applied.Add(configuration.Generation);
			readiness.ConfigureUI(configuration.Generation, boundsChanged: true);
			// The host records the updated bounds before staging the render configure.
			Assert.IsTrue(readiness.OnFrameRecorded(arrangedAtBounds: true));
			readiness.ConfigureRender(configuration.Generation);
			frames!.Invalidate();
		});
		using var scheduler = frames = new WaylandFrameScheduler(surface, () =>
		{
			readiness.BeginFrame();
			var configuration = configures.Current!;
			var layout = configuration.Layout;
			var geometry = WaylandWindowGeometry.Create(layout.ContentWidth, layout.ContentHeight, layout.TitleBarHeight, configuration.Scale);
			Assert.IsTrue(readiness.CanPresent(geometry.BufferWidth, geometry.BufferHeight, geometry.BufferWidth, geometry.BufferHeight));
			Assert.IsFalse(readiness.CanPresent(geometry.BufferWidth, geometry.BufferHeight - 1, geometry.BufferWidth, geometry.BufferHeight));
			configures.HoldPresentation(configuration.Generation);
			configures.Acknowledge(configuration, acknowledgements.Add);
			surface.Commit();
			presented.Add(configuration.Serial!.Value);
			configures.Complete(configuration.Generation, presented: true);
			return true;
		}, () => { }, owner.Add, () => configures.Complete(readiness.RenderGeneration, presented: false));
		display.StartDispatch();

		void Receive(long generation) => configures.Enqueue(new(generation,
			new(1280, 800 - (int)(generation % 2), 32, 8, WaylandCaptionButtons.Close),
			1.5, 180, 1, WaylandToplevelStates.Resizing, "test", false, (uint)generation, generation));
		void ApplyPending()
		{
			Assert.IsTrue(ui.Count <= 1);
			if (ui.TryDequeue(out var apply))
			{
				apply();
			}
		}
		void DisplayFrame(uint timestamp)
		{
			var before = presented.Count;
			peer.CompleteFrame(timestamp);
			var deadline = Stopwatch.StartNew();
			while (presented.Count == before && deadline.Elapsed < TimeSpan.FromSeconds(5))
			{
				Assert.IsTrue(owner.TryTake(out var action, TimeSpan.FromSeconds(5)));
				action();
			}
			Assert.AreEqual(before + 1, presented.Count);
		}

		Receive(1);
		ApplyPending();
		Assert.AreEqual(1, presented.Count);
		// 23,100 ticks represents 525 configure arrivals/s and both display rates exactly.
		var appliedAtPreviousFrame = applied.Count;
		for (var tick = 1; tick <= 23100; tick++)
		{
			if (tick % 44 == 0)
			{
				Receive(1 + tick / 44);
			}
			ApplyPending();
			if (tick % (23100 / refreshRate) == 0)
			{
				Assert.AreEqual(appliedAtPreviousFrame + 1, applied.Count, "Exactly one configure must reach UI per display frame.");
				appliedAtPreviousFrame = applied.Count;
				DisplayFrame((uint)tick);
			}
		}
		Assert.AreEqual(refreshRate + 1, presented.Count);
		ApplyPending();
		DisplayFrame(23101);
		Assert.AreEqual(526u, acknowledgements[^1], "The final pending serial must be presented and acknowledged.");
		CollectionAssert.AreEqual(presented, acknowledgements, "Every presented configure needs its own ack.");
		Assert.IsTrue(acknowledgements.Zip(acknowledgements.Skip(1), (previous, next) => next > previous).All(ordered => ordered));
		Assert.AreEqual(0, ui.Count);
		display.StopDispatch();
	}

	[TestMethod]
	[DataRow(1602, 1202)]
	[DataRow(800, 600)]
	public void RenderTargetUsesAppliedConfigureSizeEvenWhenCanvasSizeIsStale(int canvasWidth, int canvasHeight)
	{
		var size = WaylandRenderTargetSize.GetSize(configured: true, logicalWidth: 801, logicalHeight: 601, scale: 2, canvasWidth: canvasWidth, canvasHeight: canvasHeight);
		Assert.AreEqual((1602, 1202), size);
	}

	[TestMethod]
	public void RenderTargetUsesCanvasSizeBeforeConfigure()
	{
		var size = WaylandRenderTargetSize.GetSize(configured: false, logicalWidth: 0, logicalHeight: 0, scale: 2, canvasWidth: 800, canvasHeight: 600);
		Assert.AreEqual((800, 600), size);
	}

	[TestMethod]
	public void IntegerScaleFollowsTheLargestEnteredOutput()
	{
		var low = WaylandOutputInfo.Unknown with { Name = "A", Scale = 1 };
		var high = WaylandOutputInfo.Unknown with { Name = "B", Scale = 2 };
		Assert.AreEqual(2, WaylandOutput.GetIntegerScale([low, high], 1));
		Assert.AreEqual(1, WaylandOutput.GetIntegerScale([low], 2));
		Assert.AreEqual(2, WaylandOutput.GetIntegerScale([], 2), "Without outputs the previous scale is kept.");
	}

	[TestMethod]
	public void OutputDiagonalNeedsPhysicalSize()
	{
		Assert.IsNull(WaylandOutputInfo.Unknown.DiagonalSizeInInches);
		var output = WaylandOutputInfo.Unknown with { PhysicalWidthMm = 300, PhysicalHeightMm = 400 };
		Assert.AreEqual(500 / 25.4, output.DiagonalSizeInInches!.Value, 1e-9);
	}

	[TestMethod]
	public void StartupFailsInsteadOfHangingOnAnUnresponsiveCompositor()
	{
		using var peer = new ProtocolPeer(answerSync: false);
		var clock = Stopwatch.StartNew();
		Assert.ThrowsExactly<IOException>(() => peer.Connect());
		Assert.IsTrue(clock.Elapsed < TimeSpan.FromSeconds(30), "Startup must give up after its timeout.");
	}

	[TestMethod]
	public void CompositorDisconnectStopsDispatchWithAnIOException()
	{
		var peer = new ProtocolPeer();
		using var display = peer.Connect();
		display.StartDispatch();
		peer.Dispose();
		Assert.IsTrue(SpinWait.SpinUntil(() =>
		{
			lock (display.SyncRoot)
			{
				return display.DispatchError is not null;
			}
		}, TimeSpan.FromSeconds(5)), "A closed connection must stop the dispatch loop.");
		Assert.IsInstanceOfType<IOException>(display.DispatchError);
		Assert.ThrowsExactly<IOException>(() => display.Sync());
	}

	[TestMethod]
	public void StaleGenerationSkipsReplayAndReleasesTheUiFrame()
	{
		using var peer = new ProtocolPeer();
		using var display = peer.Connect();
		using var compositor = (WlCompositor)display.Bind(WlCompositor.ProtocolName, WlCompositor.MaximumVersion);
		using var surface = compositor.CreateSurface();
		var owner = new BlockingCollection<Action>();
		var gate = new FakeFrameGate();
		var frames = new FakeFrameProducer();
		var context = new FakeSwapChain();
		using var renderer = new WaylandRenderer(frames, gate, new WaylandRenderScale(surface, null), context, new FakeDrawingFactory(), surface, owner.Add);

		// The UI is still laying out a newer configure: the frame must not be replayed or presented, but Uno's
		// next UI frame, scheduled from this native request, must still be released.
		gate.Ready = false;
		renderer.Configure(800, 600);
		Assert.AreEqual(1, gate.Begun);
		Assert.AreEqual(1, frames.Skipped);
		Assert.AreEqual(0, frames.Requested);
		Assert.AreEqual(0, context.Acquired);
		Assert.AreEqual(0, gate.Ended.Count, "A skipped frame is not consumed.");

		// The skipped frame stays pending, so the liveness fallback still fires without a frame callback.
		Assert.IsTrue(owner.TryTake(out var tick, TimeSpan.FromSeconds(5)), "The stall timer did not fire.");
		gate.Ready = true;
		tick();
		Assert.AreEqual(1, frames.Requested);
		CollectionAssert.AreEqual(new[] { false }, gate.Ended);

		// Once the layout is recorded and the host invalidates, the frame is replayed and presented.
		context.Committed = true;
		renderer.Invalidate();
		Assert.AreEqual(2, frames.Requested);
		Assert.AreEqual(1, frames.Skipped);
		CollectionAssert.AreEqual(new[] { false, true }, gate.Ended);
	}

	private sealed class FakeFrameGate : IWaylandFrameGate
	{
		public bool Ready { get; set; } = true;
		public int Begun { get; private set; }
		public List<bool> Ended { get; } = new();
		public void BeginFrame() => Begun++;
		public bool IsFrameReady => Ready;
		public bool CanPresent(int frameWidth, int frameHeight, int bufferWidth, int bufferHeight) => Ready;
		public void EndFrame(bool presented) => Ended.Add(presented);
		public void FrameOpportunity()
		{
		}
	}

	private sealed class FakeFrameProducer : IWaylandFrameProducer
	{
		public int Requested { get; private set; }
		public int Skipped { get; private set; }
		public bool TryRequestFrame(ISwapChain swapChain)
		{
			Requested++;
			return true;
		}
		public void SkipFrame() => Skipped++;
	}

	private sealed class FakeSwapChain : IWaylandGraphicsContext
	{
		public GraphicsContextKind Kind => GraphicsContextKind.Software;
		public bool Committed { get; set; }
		public int Acquired { get; private set; }
		public IRenderTarget AcquireRenderTarget(int width, int height)
		{
			Acquired++;
			throw new NotSupportedException();
		}
		public void Present()
		{
		}
		public void Dispose()
		{
		}
	}

	private sealed class FakeDrawingFactory : IDrawingFactory
	{
		public ITexture RenderOffscreen(int pixelWidth, int pixelHeight, Action<IDrawingSession> render) => throw new NotSupportedException();
		public Task<IImage> SnapshotAsync(ITexture texture) => throw new NotSupportedException();
		public ITexture CreateTexture(IImage image) => throw new NotSupportedException();
		public ITexture CreateTexture(int pixelWidth, int pixelHeight, ReadOnlySpan<byte> bgraPremul) => throw new NotSupportedException();
		public IShader CreateLinearGradientShader(Vector2 start, Vector2 end, Color[] colors, float[] colorPositions, GradientTileMode tileMode, Matrix3x2 localMatrix) => throw new NotSupportedException();
		public IShader CreateRadialGradientShader(Vector2 center, Vector2 gradientOrigin, float radiusX, float radiusY, Color[] colors, float[] colorPositions, GradientTileMode tileMode, Matrix3x2 localMatrix) => throw new NotSupportedException();
		public IColorFilter CreateTintColorFilter(Color color) => throw new NotSupportedException();
		public IColorFilter CreateColorMatrixColorFilter(float[] matrix) => throw new NotSupportedException();
		public IEffectFilter? CreateEffectFilter(EffectNode tree, Rect bounds) => throw new NotSupportedException();
		public IEffectFilter CreateDropShadowFilter(float dx, float dy, float sigmaX, float sigmaY, Color color) => throw new NotSupportedException();
		public ICommandRecorder CreateRecording() => throw new NotSupportedException();
	}
}
