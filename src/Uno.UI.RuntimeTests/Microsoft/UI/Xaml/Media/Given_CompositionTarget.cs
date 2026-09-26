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
}
#endif
