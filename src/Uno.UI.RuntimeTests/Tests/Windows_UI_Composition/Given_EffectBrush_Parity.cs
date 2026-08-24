#if __SKIA__
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Graphics.Effects;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

// Skia≡WebGPU effect-graph parity. Runs under whichever backend the head selected (Skia default, or UNO_WEBGPU=1),
// asserting the spec-correct output — both backends must match. Colours are deterministic from the effect math.
[TestClass]
[RunsOnUIThread]
public class Given_EffectBrush_Parity
{
	private static async Task<RawBitmap> RenderEffectOverColor(IGraphicsEffect effect, Color source, int size = 64)
	{
		var compositor = TestServices.WindowHelper.XamlRoot.Compositor;
		var brush = compositor.CreateEffectFactory(effect).CreateBrush();
		brush.SetSourceParameter("source", compositor.CreateColorBrush(source));

		var sprite = compositor.CreateSpriteVisual();
		sprite.Brush = brush;
		sprite.Size = new Vector2(size, size);

		var host = new Border { Width = size, Height = size };
		ElementCompositionPreview.SetElementChildVisual(host, sprite);
		TestServices.WindowHelper.WindowContent = host;
		await TestServices.WindowHelper.WaitForLoaded(host);
		await TestServices.WindowHelper.WaitForIdle();
		return await UITestHelper.ScreenShot(host);
	}

	[TestMethod]
	public async Task When_Invert_Of_Red_Is_Cyan()
	{
		var effect = new InvertEffect { Source = new CompositionEffectSourceParameter("source") };
		var bmp = await RenderEffectOverColor(effect, Colors.Red);
		ImageAssert.HasColorAt(bmp, bmp.Width / 2, bmp.Height / 2, Color.FromArgb(255, 0, 255, 255), tolerance: 8);
	}

	[TestMethod]
	public async Task When_GaussianBlur_Preserves_Uniform_Interior()
	{
		// Blur of a uniform fill leaves the interior (far from the clamped edges) the same colour. This exercises the
		// evaluator's BlurEffectNode path (a no-op before this) and must match on both backends.
		var effect = new GaussianBlurEffect { Source = new CompositionEffectSourceParameter("source"), BlurAmount = 8f };
		var bmp = await RenderEffectOverColor(effect, Colors.Green);
		ImageAssert.HasColorAt(bmp, bmp.Width / 2, bmp.Height / 2, Colors.Green, tolerance: 16);
	}

	[TestMethod]
	public async Task When_Grayscale_Of_Red()
	{
		var effect = new GrayscaleEffect { Source = new CompositionEffectSourceParameter("source") };
		var bmp = await RenderEffectOverColor(effect, Colors.Red);
		// D2D grayscale luminance of pure red ≈ 0.2126*255 ≈ 54 (sRGB-linear weights); allow tolerance for the exact matrix.
		ImageAssert.HasColorAt(bmp, bmp.Width / 2, bmp.Height / 2, Color.FromArgb(255, 54, 54, 54), tolerance: 24);
	}
}
#endif
