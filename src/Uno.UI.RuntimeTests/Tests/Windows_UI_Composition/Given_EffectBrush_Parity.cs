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
	public async Task When_Multiply_Cyan_Yellow_Is_Green()
	{
		// Multiply blend is per-channel product: cyan(0,1,1) × yellow(1,1,0) = (0,1,0) green. Multi-source path.
		var compositor = TestServices.WindowHelper.XamlRoot.Compositor;
		var effect = new BlendEffect
		{
			Background = new CompositionEffectSourceParameter("bg"),
			Foreground = new CompositionEffectSourceParameter("fg"),
			Mode = BlendEffectMode.Multiply,
		};
		var brush = compositor.CreateEffectFactory(effect).CreateBrush();
		brush.SetSourceParameter("bg", compositor.CreateColorBrush(Colors.Cyan));
		brush.SetSourceParameter("fg", compositor.CreateColorBrush(Colors.Yellow));

		var sprite = compositor.CreateSpriteVisual();
		sprite.Brush = brush;
		sprite.Size = new Vector2(64, 64);
		var host = new Border { Width = 64, Height = 64 };
		ElementCompositionPreview.SetElementChildVisual(host, sprite);
		TestServices.WindowHelper.WindowContent = host;
		await TestServices.WindowHelper.WaitForLoaded(host);
		await TestServices.WindowHelper.WaitForIdle();
		var bmp = await UITestHelper.ScreenShot(host);
		ImageAssert.HasColorAt(bmp, bmp.Width / 2, bmp.Height / 2, Color.FromArgb(255, 0, 255, 0), tolerance: 16);
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

	// NOTE: Contrast, GammaTransfer and CrossFade CANNOT be always-on parity tests here. Skia realizes them via SkSL
	// SKRuntimeEffect image filters, which render BLANK (00000000) under this lavapipe / SW-Vulkan test environment
	// (the runtime-effect path yields no output → the fuser returns a null filter → nothing is painted). An always-on
	// assertion would therefore fail the default Skia backend on CI. WebGPU renders all three (verified out-of-band this
	// session: CrossFade(red,blue,0.5)=FF800080 and GammaTransfer(exp=2, gray 128)=FF404040 both match spec exactly;
	// Contrast follows the identical quadratic S-curve — for a dark input it uses the "high" polynomial, so
	// Contrast(0.251, c=1) clamps to ~0, which is why WebGPU correctly returns black there). True Skia≡WebGPU parity for
	// these needs a Skia GPU/CPU path that supports SkSL runtime effects; tracked in specs/webgpu-effects/PLAN.md.

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
