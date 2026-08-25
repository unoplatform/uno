#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Uno.UI.RuntimeTests.Helpers;
using CommunityToolkit.WinUI.Lottie;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

// Renderer-agnostic: passes on Skottie (default) and on the SkiaSharp-free managed engine (UNO_MANAGED_LOTTIE=1).
// LightBulb.json is a shape-only Lottie (groups, bezier paths, fills, strokes, animated transforms) — the managed
// engine's v1 scope — so a pass under UNO_MANAGED_LOTTIE=1 proves the managed engine renders and animates end-to-end.
[TestClass]
[RunsOnUIThread]
public class Given_ManagedLottie
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	public async Task When_Lottie_Renders_And_Animates()
	{
		var source = new LottieVisualSource();
		var player = new AnimatedVisualPlayer
		{
			Width = 100,
			Height = 100,
			AutoPlay = false,
			Source = source,
		};
		var host = new Border
		{
			Width = 120,
			Height = 120,
			Background = new SolidColorBrush(Colors.White),
			Child = player,
		};

		try
		{
			await UITestHelper.Load(host);
			await source.SetSourceAsync(new Uri("ms-appx:///Lottie/LightBulb.json"));
			await TestServices.WindowHelper.WaitFor(() => player.IsAnimatedVisualLoaded, timeoutMS: 5000, "LightBulb.json should load.");
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(player.Duration > TimeSpan.Zero, "The loaded animation should report a duration.");

			player.SetProgress(0.15);
			await TestServices.WindowHelper.WaitForIdle();
			var frameA = await UITestHelper.ScreenShot(host);
			await frameA.Populate();

			player.SetProgress(0.65);
			await TestServices.WindowHelper.WaitForIdle();
			var frameB = await UITestHelper.ScreenShot(host);
			await frameB.Populate();

			var w = (int)host.ActualWidth;
			var h = (int)host.ActualHeight;

			// Drew something: at least one frame differs from the plain white host background.
			Assert.IsTrue(NonBackgroundPixels(frameA, w, h, Colors.White) > 50, "The animation should render visible content, not a blank frame.");
			// Animated: the two progress points are not identical.
			Assert.IsTrue(DifferentPixels(frameA, frameB, w, h) > 50, "Different progress values should render different frames.");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	public async Task When_Trim_Animation_Renders_And_Animates()
	{
		// 4930-checkbox-animation.json exercises trim paths (tm) + ellipses + fills/strokes — the managed engine's
		// trim support. Renderer-agnostic: passes on Skottie and on the managed engine (UNO_MANAGED_LOTTIE=1).
		var source = new LottieVisualSource();
		var player = new AnimatedVisualPlayer { Width = 100, Height = 100, AutoPlay = false, Source = source };
		var host = new Border { Width = 120, Height = 120, Background = new SolidColorBrush(Colors.White), Child = player };

		try
		{
			await UITestHelper.Load(host);
			await source.SetSourceAsync(new Uri("ms-appx:///Lottie/4930-checkbox-animation.json"));
			await TestServices.WindowHelper.WaitFor(() => player.IsAnimatedVisualLoaded, timeoutMS: 5000, "checkbox animation should load.");
			await TestServices.WindowHelper.WaitForIdle();

			player.SetProgress(0.3);
			await TestServices.WindowHelper.WaitForIdle();
			var frameA = await UITestHelper.ScreenShot(host);
			await frameA.Populate();

			player.SetProgress(0.9);
			await TestServices.WindowHelper.WaitForIdle();
			var frameB = await UITestHelper.ScreenShot(host);
			await frameB.Populate();

			var w = (int)host.ActualWidth;
			var h = (int)host.ActualHeight;
			Assert.IsTrue(NonBackgroundPixels(frameA, w, h, Colors.White) > 20, "The trim-path animation should render visible content.");
			Assert.IsTrue(DifferentPixels(frameA, frameB, w, h) > 20, "The trim-path animation should animate across progress (trim start/end changing).");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	private static int NonBackgroundPixels(RawBitmap bmp, int w, int h, Color background)
	{
		var count = 0;
		for (var y = 0; y < h; y++)
		{
			for (var x = 0; x < w; x++)
			{
				var p = bmp.GetPixel(x, y);
				if (Math.Abs(p.R - background.R) + Math.Abs(p.G - background.G) + Math.Abs(p.B - background.B) > 24)
				{
					count++;
				}
			}
		}
		return count;
	}

	private static int DifferentPixels(RawBitmap a, RawBitmap b, int w, int h)
	{
		var count = 0;
		for (var y = 0; y < h; y++)
		{
			for (var x = 0; x < w; x++)
			{
				var pa = a.GetPixel(x, y);
				var pb = b.GetPixel(x, y);
				if (Math.Abs(pa.R - pb.R) + Math.Abs(pa.G - pb.G) + Math.Abs(pa.B - pb.B) > 24)
				{
					count++;
				}
			}
		}
		return count;
	}
}
