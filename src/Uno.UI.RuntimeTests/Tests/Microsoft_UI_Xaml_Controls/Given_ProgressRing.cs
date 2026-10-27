using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass, RunsOnUIThread]
public class Given_ProgressRing
{
	[TestMethod]
#if !(WINAPPSDK || __SKIA__ || __WASM__)
	[Ignore("IAnimatedVisualSource is not implemented")]
#endif
	public async Task When_NoProgress_IsIndeterminate_Toggle()
	{
		var SUT = new ProgressRing { Width = 20, Height = 20 };
		try
		{
			(SUT.Minimum, SUT.Value, SUT.Maximum) = (0, 0, 100);
			SUT.IsActive = true;
			SUT.IsIndeterminate = false;

			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForLoaded(SUT);
			await TestServices.WindowHelper.WaitForIdle();

			var player = SUT.FindFirstDescendant<AnimatedVisualPlayer>(x => x.Name == "LottiePlayer");
			if (player is null)
			{
				// The test may be invalid due to template change.
				Assert.IsNotNull(player, "Failed to find template part: AnimatedVisualPlayer#LottiePlayer");
			}

			SUT.IsIndeterminate = true;
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsTrue(player.IsPlaying, "LottiePlayer should be playing.");

			SUT.IsIndeterminate = false;
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsFalse(player.IsPlaying, "LottiePlayer should have stopped playing.");
		}
		finally
		{
			SUT.IsActive = false;
		}
	}

	[TestMethod]
#if !(WINAPPSDK || __SKIA__ || __WASM__)
	[Ignore("IAnimatedVisualSource is not implemented")]
#endif
	public async Task When_HalfProgress_IsIndeterminate_Toggle()
	{
		var SUT = new ProgressRing { Width = 20, Height = 20 };
		try
		{
			(SUT.Minimum, SUT.Value, SUT.Maximum) = (0, 50, 100);
			SUT.IsActive = true;
			SUT.IsIndeterminate = false;

			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForLoaded(SUT);
			await TestServices.WindowHelper.WaitForIdle();

			var player = SUT.FindFirstDescendant<AnimatedVisualPlayer>(x => x.Name == "LottiePlayer");
			if (player is null)
			{
				// The test may be invalid due to template change.
				Assert.IsNotNull(player, "Failed to find template part: AnimatedVisualPlayer#LottiePlayer");
			}

			SUT.IsIndeterminate = true;
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsTrue(player.IsPlaying, "LottiePlayer should be playing.");

			// note: unlike in When_NoProgress_IsIndeterminate_Toggle, the progress was (and still is) at 50%.
			// so upon entering "Determinate" again, it (should animate 0->50%)? before coming to a stop.
			SUT.IsIndeterminate = false;
			await TestServices.WindowHelper.WaitForIdle();
			//Assert.IsTrue(player.IsPlaying, "LottiePlayer should be animating briefly from 0% to 50%."); // not the case for windows, but this is animated on uno
			await TestServices.WindowHelper.WaitFor(() => player.IsPlaying == false, timeoutMS: 2000, "LottiePlayer should be eventually stop playing.");
		}
		finally
		{
			SUT.IsActive = false;
		}
	}

	[TestMethod]
#if !__SKIA__
	[Ignore("The test is unreliable when DPI scaling is not 1")]
#endif
	public async Task When_Stretch_Fill()
	{
		if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
		{
			Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
		}

		var pr1 = new ProgressRing { Width = 100, Height = 100, IsIndeterminate = false, Value = 50 };
		var pr2 = new ProgressRing { Width = 50, Height = 50, IsIndeterminate = false, Value = 50 };

		await UITestHelper.Load(new StackPanel { Children = { pr1, pr2 } });
		await Task.Delay(TimeSpan.FromSeconds(2)); // wait for the animation to end

		var screenshot1 = await UITestHelper.ScreenShot(pr1);
		var screenshot2 = await UITestHelper.ScreenShot(pr2);
		var pixels1 = screenshot1.GetPixels();
		var pixels2 = screenshot2.GetPixels();

		var different = false;
		for (int i = 0; i < screenshot2.Bitmap.PixelHeight; i++)
		{
			different = 0 != pixels1.AsSpan(i * screenshot2.Bitmap.PixelWidth, screenshot2.Bitmap.PixelWidth).SequenceCompareTo(pixels2.AsSpan(i * screenshot2.Bitmap.PixelWidth, screenshot2.Bitmap.PixelWidth));
			if (different)
			{
				break;
			}
		}
		Assert.IsTrue(different);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24555")]
#if !__SKIA__
	[Ignore("Reading the rendered ring back requires RenderTargetBitmap over the Skia composition pipeline")]
#endif
	public async Task When_Foreground_Brush_Colour_Mutated()
	{
		var foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
		var SUT = CreateDeterminateRing(foreground, new SolidColorBrush(Microsoft.UI.Colors.Transparent));

		try
		{
			await LoadSettledRing(SUT);
			var before = await CountRedAndLimePixels(SUT);
			Assert.IsTrue(before.Red > 0, $"The ring should paint the red foreground brush before it is mutated, but {before}.");

			foreground.Color = Microsoft.UI.Colors.Lime;
			await TestServices.WindowHelper.WaitForIdle();

			var after = await CountRedAndLimePixels(SUT);
			Assert.IsTrue(after.Lime > 0, $"The ring should repaint in the brush's new colour, but {after}.");
			Assert.AreEqual(0, after.Red, $"The ring should keep no pixel of the brush's old colour, but {after}.");
		}
		finally
		{
			SUT.IsActive = false;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24555")]
#if !__SKIA__
	[Ignore("Reading the rendered ring back requires RenderTargetBitmap over the Skia composition pipeline")]
#endif
	public async Task When_Background_Brush_Colour_Mutated()
	{
		var background = new SolidColorBrush(Microsoft.UI.Colors.Red);
		var SUT = CreateDeterminateRing(new SolidColorBrush(Microsoft.UI.Colors.Transparent), background);

		try
		{
			await LoadSettledRing(SUT);
			var before = await CountRedAndLimePixels(SUT);
			Assert.IsTrue(before.Red > 0, $"The ring's track should paint the red background brush before it is mutated, but {before}.");

			background.Color = Microsoft.UI.Colors.Lime;
			await TestServices.WindowHelper.WaitForIdle();

			var after = await CountRedAndLimePixels(SUT);
			Assert.IsTrue(after.Lime > 0, $"The ring's track should repaint in the brush's new colour, but {after}.");
			Assert.AreEqual(0, after.Red, $"The ring's track should keep no pixel of the brush's old colour, but {after}.");
		}
		finally
		{
			SUT.IsActive = false;
		}
	}

	private static ProgressRing CreateDeterminateRing(Brush foreground, Brush background) => new ProgressRing
	{
		// Large enough that the stroked ring survives anti-aliasing as a solid band of its own colour.
		Width = 160,
		Height = 160,
		IsIndeterminate = false,
		Minimum = 0,
		Maximum = 100,
		// A three-quarter arc: enough foreground to sample, and unambiguous at the trim's ends.
		Value = 75,
		IsActive = true,
		Foreground = foreground,
		Background = background,
	};

	private static async Task LoadSettledRing(ProgressRing ring)
	{
		await UITestHelper.Load(ring);

		var player = ring.FindFirstDescendant<AnimatedVisualPlayer>(x => x.Name == "LottiePlayer");
		Assert.IsNotNull(player, "Failed to find template part: AnimatedVisualPlayer#LottiePlayer");

		await TestServices.WindowHelper.WaitFor(
			() => player.IsAnimatedVisualLoaded,
			timeoutMS: 5000,
			"The determinate animated visual should load.");
		await TestServices.WindowHelper.WaitFor(
			() => !player.IsPlaying,
			timeoutMS: 5000,
			"The determinate progress animation should settle on its final value.");
	}

	// Both counts come from one screenshot so the assertions describe the same frame, and the whole
	// tally is returned rather than a verdict so a failure names what was actually on screen.
	private static async Task<RingPixels> CountRedAndLimePixels(FrameworkElement element)
	{
		var bitmap = await UITestHelper.ScreenShot(element);
		await bitmap.Populate();

		var red = 0;
		var lime = 0;
		for (var x = 0; x < bitmap.Width; x++)
		{
			for (var y = 0; y < bitmap.Height; y++)
			{
				var pixel = bitmap.GetPixel(x, y);
				if (IsRed(pixel))
				{
					red++;
				}
				else if (IsLime(pixel))
				{
					lime++;
				}
			}
		}

		return new RingPixels(red, lime);
	}

	private record struct RingPixels(int Red, int Lime)
	{
		public override string ToString() => $"red={Red}px, lime={Lime}px";
	}

	// Anti-aliased edges blend the ring into the page, so a match demands an opaque, clearly
	// dominant channel rather than an exact colour.
	private static bool IsRed(Color pixel) => pixel.A > 200 && pixel.R > 120 && pixel.R > pixel.G + 60 && pixel.R > pixel.B + 60;

	private static bool IsLime(Color pixel) => pixel.A > 200 && pixel.G > 120 && pixel.G > pixel.R + 60 && pixel.G > pixel.B + 60;
}
