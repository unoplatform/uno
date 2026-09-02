#if __SKIA__
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls.AnimatedVisuals;

// End-to-end validation that unmodified LottieGen output (WinUIVersion 3.0) compiles against Uno and
// renders through AnimatedVisualPlayer's WinUI composition flow. These sources exercise the Composition
// gaps that were filled: ColorKeyFrameAnimation, animated gradient brushes, PathKeyFrameAnimation,
// TrimStart/End/Offset, and the shared-AnimationController progress-scrub model.
[TestClass]
[RunsOnUIThread]
public partial class Given_Generated_Lottie
{
	private static IAnimatedVisualSource CreateSource(string name) => name switch
	{
		"Watermelon" => new global::AnimatedVisuals.Watermelon(),          // color keyframes + path morph + 2 controllers
		"Gradient_shapes" => new global::AnimatedVisuals.Gradient_shapes(), // linear/radial gradient brushes
		"LottieLogo2" => new global::AnimatedVisuals.LottieLogo2(),         // 165 trim animations + shared controller
		_ => throw new ArgumentException(name),
	};

	[TestMethod]
	[DataRow("Watermelon")]
	[DataRow("Gradient_shapes")]
	[DataRow("LottieLogo2")]
	public async Task When_Generated_Source_Loads_And_Renders(string name)
	{
		var player = new AnimatedVisualPlayer
		{
			Source = CreateSource(name),
			Width = 200,
			Height = 200,
			AutoPlay = false,
		};

		await UITestHelper.Load(player);

		// The WinUI composition flow only reports loaded when TryCreateAnimatedVisual returned a real
		// IAnimatedVisual and its RootVisual was hosted — i.e. the whole pipeline compiled and built.
		Assert.IsTrue(player.IsAnimatedVisualLoaded, $"{name}: animated visual did not load");
		Assert.IsTrue(player.Duration > TimeSpan.Zero, $"{name}: duration should be positive");

		// Scrubbing must not throw (drives every keyframe animation through the shared controller).
		player.SetProgress(0.25);
		await WindowHelper.WaitForIdle();
		player.SetProgress(0.75);
		await WindowHelper.WaitForIdle();

		var bitmap = await UITestHelper.ScreenShot(player);
		await bitmap.Populate();

		// Prove something actually rendered: at least some pixels differ from the transparent corner.
		var background = bitmap.GetPixel(0, 0);
		var rendered = 0;
		for (var x = 0; x < bitmap.Width; x += 8)
		{
			for (var y = 0; y < bitmap.Height; y += 8)
			{
				if (bitmap.GetPixel(x, y) != background)
				{
					rendered++;
				}
			}
		}

		Assert.IsTrue(rendered > 5, $"{name}: expected rendered content, only {rendered} non-background samples");
	}
}
#endif
