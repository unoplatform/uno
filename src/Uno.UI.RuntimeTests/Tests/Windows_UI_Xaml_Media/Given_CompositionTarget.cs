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
	/// A driver on a target whose host is gone can never tick again — the frame that would bring its next
	/// tick will not come. It has to be dropped, or the compositor keeps counting an animation in flight
	/// for the rest of the process.
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
