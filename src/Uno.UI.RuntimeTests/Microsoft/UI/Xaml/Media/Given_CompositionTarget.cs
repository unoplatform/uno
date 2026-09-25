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
			Assert.IsInstanceOfType(entry.Data, typeof(SkiaSharp.SKPicture), "The Skia backend should expose the recorded frame as an SKPicture.");
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
