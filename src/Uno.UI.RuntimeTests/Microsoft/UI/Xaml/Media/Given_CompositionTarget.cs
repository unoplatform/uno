#if __SKIA__
using System;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media;

[TestClass]
public class Given_CompositionTarget
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_WebGpu_Requested_Then_It_Renders()
	{
		// A lane that asks for WebGPU must not quietly test the Skia fallback, nor a WebGPU that draws nothing.
		if (Environment.GetEnvironmentVariable("UNO_WEBGPU") is not ("1" or "true" or "neutral" or "swapchain"))
		{
			Assert.Inconclusive("WebGPU was not requested (UNO_WEBGPU).");
		}

		var border = new Border { Width = 50, Height = 50, Background = new SolidColorBrush(Colors.Red) };
		await UITestHelper.Load(border);

		Assert.AreEqual(RuntimeTestBackends.WebGpu, RuntimeTestsBackendHelper.CurrentBackend, "UNO_WEBGPU is set but another backend won negotiation.");
		var screenshot = await UITestHelper.ScreenShot(border);
		ImageAssert.HasColorAt(screenshot, 25, 25, Colors.Red, tolerance: 5);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_SkipVisualTreePainting()
	{
		var border = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Red) };

		Assert.IsFalse(FeatureConfiguration.Rendering.SkipVisualTreePainting);
		FeatureConfiguration.Rendering.SkipVisualTreePainting = true;
		try
		{
			await UITestHelper.Load(border);

			var target = (CompositionTarget)border.Visual.CompositionTarget!;
			var frameRendered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			Action onFrameRendered = () => frameRendered.TrySetResult();
			target.FrameRendered += onFrameRendered;
			try
			{
				// A property change must still schedule and produce a (blank) frame.
				border.Background = new SolidColorBrush(Colors.Blue);
				await Task.WhenAny(frameRendered.Task, Task.Delay(2000));
				Assert.IsTrue(frameRendered.Task.IsCompleted, "The rendering pipeline should keep producing frames while painting is skipped.");
			}
			finally
			{
				target.FrameRendered -= onFrameRendered;
			}

			// RenderTargetBitmap-based screenshots don't go through the frame pipeline and must still capture actual content.
			var screenshot = await UITestHelper.ScreenShot(border);
			ImageAssert.HasColorAt(screenshot, 50, 50, Colors.Blue, tolerance: 5);
		}
		finally
		{
			FeatureConfiguration.Rendering.SkipVisualTreePainting = false;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Rendering_Carries_FrameData()
	{
		var border = new Border { Width = 50, Height = 50, Background = new SolidColorBrush(Colors.Green) };
		await UITestHelper.Load(border);

		var raised = new TaskCompletionSource<RenderingEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
		EventHandler<object> onRendering = (_, args) =>
		{
			if (args is RenderingEventArgs e && e.FrameData is { Count: > 0 })
			{
				raised.TrySetResult(e);
			}
		};

		CompositionTarget.Rendering += onRendering;
		try
		{
			border.Background = new SolidColorBrush(Colors.Blue);
			await Task.WhenAny(raised.Task, Task.Delay(5000));
			Assert.IsTrue(raised.Task.IsCompleted, "Rendering should be raised with frame data while a subscriber is attached.");

			var frameData = raised.Task.Result.FrameData!;
			var entry = frameData[0];
			Assert.IsNotNull(entry.Window, "Each entry should name the window the frame belongs to.");
			// Skia hands out its own SKPicture; a backend with nothing to expose hands out null.
			if (RuntimeTestsBackendHelper.CurrentBackend == RuntimeTestBackends.Skia)
			{
				Assert.IsInstanceOfType(entry.Data, typeof(SkiaSharp.SKPicture), "The Skia backend should expose the recorded frame as an SKPicture.");
			}
			else
			{
				Assert.IsNull(entry.Data, "A backend with no native recording type should expose null.");
			}
		}
		finally
		{
			CompositionTarget.Rendering -= onRendering;
		}
	}

	/// <summary>
	/// What a Rendering handler writes must be in the frame recorded right after it, from the same tick, rather
	/// than in the one after: that frame of latency also shows as uneven motion for per-frame animation.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Rendering_Handler_Writes_Then_Recorded_In_Same_Tick()
	{
		var border = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Red) };
		await UITestHelper.Load(border);
		var target = (CompositionTarget)border.Visual.CompositionTarget!;
		var dispatcherQueue = border.DispatcherQueue;

		var sameTick = 0;
		var laterTick = 0;
		var pending = false;
		EventHandler<object> onRendering = (_, _) =>
		{
			border.Visual.Opacity = border.Visual.Opacity > 0.5f ? 0.4f : 0.6f;
			pending = true;

			// Can only run once the current dispatcher item is done, so it sees the write still unrecorded
			// unless the record happened within the same tick.
			dispatcherQueue.TryEnqueue(() =>
			{
				if (pending)
				{
					pending = false;
					laterTick++;
				}
			});
		};
		Action onFrameRendered = () =>
		{
			if (pending)
			{
				pending = false;
				sameTick++;
			}
		};

		CompositionTarget.Rendering += onRendering;
		target.FrameRendered += onFrameRendered;
		try
		{
			await Task.Delay(1000);
		}
		finally
		{
			target.FrameRendered -= onFrameRendered;
			CompositionTarget.Rendering -= onRendering;
		}

		Assert.IsTrue(sameTick >= 5, $"a Rendering write should be recorded by the same tick, got {sameTick} same-tick and {laterTick} later records");
		Assert.IsTrue(laterTick <= 2, $"a Rendering write should not wait for the next tick, got {sameTick} same-tick and {laterTick} later records");
	}

	/// <summary>
	/// A frame has one timestamp: the frame drivers and Rendering handlers of one frame must agree on it, or
	/// two motions in the same frame would move by different amounts.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Frame_Driver_And_Rendering_Then_Same_Frame_Time()
	{
		var border = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Red) };
		await UITestHelper.Load(border);
		var target = (CompositionTarget)border.Visual.CompositionTarget!;

		long? driverTimestamp = null;
		var pairs = new System.Collections.Generic.List<(long Driver, TimeSpan Rendering)>();
		EventHandler<long> driver = (_, timestamp) => driverTimestamp = timestamp;
		EventHandler<object> onRendering = (_, args) =>
		{
			if (driverTimestamp is { } timestamp)
			{
				pairs.Add((timestamp, ((RenderingEventArgs)args).RenderingTime));
				driverTimestamp = null;
			}
		};

		target.FrameStarting += driver;
		CompositionTarget.Rendering += onRendering;
		try
		{
			await Task.Delay(1000);
		}
		finally
		{
			CompositionTarget.Rendering -= onRendering;
			target.FrameStarting -= driver;
		}

		Assert.IsTrue(pairs.Count >= 5, $"expected the driver and Rendering to be raised together, got {pairs.Count} frames");
		for (var i = 1; i < pairs.Count; i++)
		{
			var driverStep = pairs[i].Driver - pairs[i - 1].Driver;
			var renderingStep = (pairs[i].Rendering - pairs[i - 1].Rendering).Ticks;
			Assert.AreEqual(driverStep, renderingStep, $"frame {i}: the driver stepped {driverStep} ticks but RenderingTime {renderingStep}");
		}
	}

	/// <summary>
	/// A frame driver is motion: it must be evaluated once per presented frame, on the frame's cadence.
	/// Evaluating it on every dispatcher pump samples raw wall-clock jitter into the motion, runs a layout
	/// pass per pump and collapses the frame clock's interval so its grid never engages.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Frame_Driver_Writes_Then_Ticked_Once_Per_Frame()
	{
		var border = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Red) };
		await UITestHelper.Load(border);
		var target = (CompositionTarget)border.Visual.CompositionTarget!;

		var (ticks, frames) = await CountDriverTicks(target, (_, _) => border.Visual.Opacity = border.Visual.Opacity > 0.5f ? 0.4f : 0.6f);

		Assert.IsTrue(frames >= 5, $"the pipeline should keep producing frames while a driver is subscribed, got {frames}");
		Assert.IsTrue(ticks <= frames + 2, $"a driver must tick once per frame, got {ticks} ticks for {frames} frames");
	}

	/// <summary>
	/// The tick that evaluates drivers is kept alive by the frame chain itself, not by the drivers writing
	/// something: a driver that skips a frame (or is about to stop) must still get its next tick.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Frame_Driver_Writes_Nothing_Then_Still_Ticked_Every_Frame()
	{
		var border = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Red) };
		await UITestHelper.Load(border);
		var target = (CompositionTarget)border.Visual.CompositionTarget!;

		var (ticks, frames) = await CountDriverTicks(target, (_, _) => { });

		Assert.IsTrue(frames >= 5, $"a silent driver must keep the frame chain alive, got {frames} frames");
		Assert.IsTrue(ticks >= frames - 2 && ticks <= frames + 2, $"a driver must tick once per frame, got {ticks} ticks for {frames} frames");
	}

	/// <summary>
	/// A driver on a target whose host is gone can never tick again: the frame that would bring its next tick
	/// will not come. It has to be dropped, or the compositor keeps counting an animation in flight for the
	/// rest of the process.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaMacOS | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_Window_Closed_Then_Frame_Drivers_Dropped()
	{
		var secondary = new Window();
		var content = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Red) };
		secondary.Content = content;

		var activated = false;
		secondary.Activated += (_, _) => activated = true;
		secondary.Activate();
		await TestServices.WindowHelper.WaitFor(() => activated, message: "the secondary window should activate");
		await TestServices.WindowHelper.WaitForLoaded(content);

		var target = (CompositionTarget)content.Visual.CompositionTarget!;
		var compositor = content.Visual.Compositor;
		EventHandler<long> driver = (_, _) => { };

		target.FrameStarting += driver;
		try
		{
			Assert.IsTrue(compositor.IsAnimating, "a subscribed frame driver must count as an animation in flight");

			secondary.Close();

			await TestServices.WindowHelper.WaitFor(() => !compositor.IsAnimating, message: "closing a window must drop the frame drivers of its target");
		}
		finally
		{
			// A no-op once the target has dropped them, and keeps the count balanced if it hasn't.
			target.FrameStarting -= driver;
		}
	}

	/// <summary>
	/// A driver subscribed once the host is gone would never tick either, so it must not count as motion: a
	/// component reacting to the close (or a driver replacing itself) would otherwise leave it counted forever.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaMacOS | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_Window_Closed_Then_Later_Frame_Driver_Not_Counted()
	{
		var (secondary, content) = await OpenSecondaryWindow();
		var target = (CompositionTarget)content.Visual.CompositionTarget!;
		var compositor = content.Visual.Compositor;
		var xamlRoot = content.XamlRoot!;

		var closed = false;
		secondary.Closed += (_, _) => closed = true;
		secondary.Close();
		await TestServices.WindowHelper.WaitFor(() => closed, message: "the secondary window should close");

		// X11 unregisters after Closed, from a task continuation, and the drivers are then dropped on the UI thread.
		await TestServices.WindowHelper.WaitFor(() => Uno.UI.Hosting.XamlRootMap.GetHostForRoot(xamlRoot) is null, message: "the closed window should unregister");
		await TestServices.WindowHelper.WaitForIdle();
		await TestServices.WindowHelper.WaitFor(() => !compositor.IsAnimating, message: "nothing should be animating once the window is closed");

		EventHandler<long> driver = (_, _) => { };
		target.FrameStarting += driver;
		try
		{
			Assert.IsFalse(compositor.IsAnimating, "a frame driver on a closed window's target must not count as an animation in flight");
		}
		finally
		{
			target.FrameStarting -= driver;
		}
	}

	/// <summary>
	/// The layout tick can record ahead of the next vsync. After an idle gap, an animation started then must not
	/// date its start from the last frame before the gap, or its next frame plays the whole gap out at once.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Animation_Starts_After_Idle_Then_It_Starts_From_Now()
	{
		var border = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Red) };
		await UITestHelper.Load(border);
		await UITestHelper.WaitForIdle();
		await Task.Delay(1000);

		var visual = border.Visual;
		var compositor = visual.Compositor;
		var animation = compositor.CreateScalarKeyFrameAnimation();
		animation.InsertKeyFrame(0f, 0f, compositor.CreateLinearEasingFunction());
		animation.InsertKeyFrame(1f, 1000f, compositor.CreateLinearEasingFunction());
		animation.Duration = TimeSpan.FromSeconds(1000);

		visual.StartAnimation(nameof(Microsoft.UI.Composition.Visual.RotationAngleInDegrees), animation);
		try
		{
			// What the layout tick does when it runs ahead of the next vsync.
			((CompositionTarget)visual.CompositionTarget!).OnRenderFrameOpportunity();
			await Task.Delay(100);

			Assert.IsTrue(
				visual.RotationAngleInDegrees < 0.5f,
				$"the animation should have run for about 0.1s, but shows {visual.RotationAngleInDegrees:F2}s of progress");
		}
		finally
		{
			visual.StopAnimation(nameof(Microsoft.UI.Composition.Visual.RotationAngleInDegrees));
		}
	}

	/// <summary>
	/// As in WinUI, Rendering is raised after the tick's layout pass: a handler sees the layout of what changed
	/// earlier in the same tick, not the previous frame's.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Rendering_Then_Layout_Of_The_Tick_Is_Current()
	{
		var border = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Red) };
		var host = new StackPanel { Children = { border } };
		await UITestHelper.Load(host);
		var target = (CompositionTarget)border.Visual.CompositionTarget!;

		var frames = 0;
		var stale = 0;
		double? written = null;
		EventHandler<long> driver = (_, _) => written = border.Width = border.Width > 100 ? 100 : 120;
		EventHandler<object> onRendering = (_, _) =>
		{
			if (written is { } width)
			{
				frames++;
				if (border.ActualWidth != width)
				{
					stale++;
				}

				written = null;
			}
		};

		target.FrameStarting += driver;
		CompositionTarget.Rendering += onRendering;
		try
		{
			await Task.Delay(1000);
		}
		finally
		{
			CompositionTarget.Rendering -= onRendering;
			target.FrameStarting -= driver;
		}

		Assert.IsTrue(frames >= 5, $"expected the driver and Rendering to be raised together, got {frames} frames");
		Assert.AreEqual(0, stale, $"Rendering saw the previous layout in {stale} of {frames} frames");
	}

	/// <summary>
	/// The frame tick belongs to every window that presents, not only the first one: with the main window
	/// minimized, another window's Rendering handlers and frame drivers must keep ticking.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public async Task When_Main_Window_Minimized_Then_Other_Window_Still_Ticks()
	{
		var mainPresenter = (Microsoft.UI.Windowing.OverlappedPresenter)TestServices.WindowHelper.CurrentTestWindow.AppWindow.Presenter;
		var (secondary, content) = await OpenSecondaryWindow();
		var target = (CompositionTarget)content.Visual.CompositionTarget!;

		var ticks = 0;
		var raises = 0;
		EventHandler<long> driver = (_, _) => ticks++;
		EventHandler<object> onRendering = (_, _) => raises++;

		mainPresenter.Minimize();
		try
		{
			await Task.Delay(500);

			target.FrameStarting += driver;
			CompositionTarget.Rendering += onRendering;
			await Task.Delay(1000);
		}
		finally
		{
			CompositionTarget.Rendering -= onRendering;
			target.FrameStarting -= driver;
			mainPresenter.Restore();
			secondary.Close();
		}

		Assert.IsTrue(ticks >= 5, $"the driver of the visible window should keep ticking, got {ticks} ticks");
		Assert.IsTrue(raises >= 5, $"Rendering should keep being raised for the visible window, got {raises} raises");
	}

	private static async Task<(Window Window, Border Content)> OpenSecondaryWindow()
	{
		var secondary = new Window();
		var content = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Red) };
		secondary.Content = content;

		var activated = false;
		secondary.Activated += (_, _) => activated = true;
		secondary.Activate();
		await TestServices.WindowHelper.WaitFor(() => activated, message: "the secondary window should activate");
		await TestServices.WindowHelper.WaitForLoaded(content);

		return (secondary, content);
	}

	/// <summary>
	/// A driver starting after a pause must get the current frame's timestamp, not the last one before the pause:
	/// motion dates its start from its first tick, so a stale one plays the whole pause out in the next frame.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Frame_Driver_Starts_After_A_Pause_Then_First_Step_Is_One_Frame()
	{
		var border = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Red) };
		await UITestHelper.Load(border);
		var target = (CompositionTarget)border.Visual.CompositionTarget!;

		// A frame nothing ticks from, then a pause.
		border.Background = new SolidColorBrush(Colors.Blue);
		await UITestHelper.WaitForIdle();
		await Task.Delay(300);

		var timestamps = new System.Collections.Generic.List<long>();
		EventHandler<long> driver = (_, timestamp) => timestamps.Add(timestamp);
		target.FrameStarting += driver;
		try
		{
			await TestServices.WindowHelper.WaitFor(() => timestamps.Count >= 3, message: "the driver should keep ticking");
		}
		finally
		{
			target.FrameStarting -= driver;
		}

		// An absolute bound, well short of the pause: hosts that are not vsync-paced (the framebuffer) measure
		// intervals far below a display's, so a bound in frame intervals would fail on any ordinary frame there.
		var firstStep = timestamps[1] - timestamps[0];
		Assert.IsTrue(
			firstStep < 100 * TimeSpan.TicksPerMillisecond,
			$"the first step spanned {firstStep / (double)TimeSpan.TicksPerMillisecond:F1}ms, so it played out the pause");
	}

	/// <summary>
	/// Swapping one driver for another between frames, as the InteractionTracker does on every wheel notch, must
	/// not tick the new one a second time within the frame: the extra tick advances the motion off the frame grid.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Frame_Driver_Swapped_Between_Frames_Then_Ticked_Once_Per_Frame()
	{
		var border = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Red) };
		await UITestHelper.Load(border);
		var target = (CompositionTarget)border.Visual.CompositionTarget!;
		var dispatcherQueue = border.DispatcherQueue;

		var ticks = 0;
		var frames = 0;
		EventHandler<long> current = null;
		EventHandler<long> MakeDriver() => (_, _) => ticks++;
		void Swap()
		{
			if (current is null)
			{
				return;
			}

			target.FrameStarting -= current;
			current = MakeDriver();
			target.FrameStarting += current;
		}

		Action onFrameRendered = () =>
		{
			frames++;
			dispatcherQueue.TryEnqueue(Swap);
		};

		current = MakeDriver();
		target.FrameStarting += current;
		target.FrameRendered += onFrameRendered;
		try
		{
			await Task.Delay(1000);
		}
		finally
		{
			target.FrameRendered -= onFrameRendered;
			target.FrameStarting -= current;
			current = null;
		}

		Assert.IsTrue(frames >= 5, $"the pipeline should keep producing frames, got {frames}");
		Assert.IsTrue(ticks <= frames + 2, $"a swapped driver must tick once per frame, got {ticks} ticks for {frames} frames");
	}

	private static async Task<(int Ticks, int Frames)> CountDriverTicks(CompositionTarget target, EventHandler<long> driver)
	{
		var ticks = 0;
		var frames = 0;
		EventHandler<long> countingDriver = (s, e) =>
		{
			ticks++;
			driver(s, e);
		};
		Action onFrameRendered = () => frames++;

		target.FrameStarting += countingDriver;
		target.FrameRendered += onFrameRendered;
		try
		{
			await Task.Delay(1000);
		}
		finally
		{
			target.FrameRendered -= onFrameRendered;
			target.FrameStarting -= countingDriver;
		}

		return (ticks, frames);
	}
}
#endif
