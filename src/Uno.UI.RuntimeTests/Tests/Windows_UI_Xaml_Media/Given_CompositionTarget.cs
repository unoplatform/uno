#if __SKIA__
using System;
using System.Threading.Tasks;
using Microsoft.UI;
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
