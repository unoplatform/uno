using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.AnimatedVisuals;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls.AnimatedVisuals;

[TestClass]
public class Given_AnimatedAcceptVisualSource
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TryCreateAnimatedVisual_Returns_NonNull()
	{
		var anchor = new Border { Width = 1, Height = 1 };
		await UITestHelper.Load(anchor);
		await TestServices.WindowHelper.WaitForIdle();
		var compositor = ElementCompositionPreview.GetElementVisual(anchor).Compositor;

		var source = new AnimatedAcceptVisualSource();
		var visual = source.TryCreateAnimatedVisual(compositor, out _);

		Assert.IsNotNull(visual, "AnimatedAcceptVisualSource should return a non-null IAnimatedVisual.");
		Assert.AreEqual(48f, visual.Size.X, "Width must be 48.");
		Assert.AreEqual(48f, visual.Size.Y, "Height must be 48.");
		Assert.IsTrue(visual.Duration.Ticks > 0, "Duration must be > 0.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Drawn_Checkmark_Stroke_Is_Not_Too_Thick()
	{
		// End-to-end stroke-width regression: AnimatedAcceptVisualSource builds a Lottie composition
		// where each CompositionSpriteShape carries an inner Scale(0.7) and a 4 DIP stroke. WinUI's
		// Composition pipeline scales the stroke through the shape transform (4 * 0.7 = 2.8 DIP at
		// 1× device scale). Uno used to pre-bake the shape transform into the geometry without
		// scaling the stroke, so the rendered checkmark was ~1.43× thicker than WinUI. This test
		// renders the fully-drawn checkmark and asserts the stroke pixels stay within a thickness
		// budget that the pre-fix Uno render would exceed.
		var source = new AnimatedAcceptVisualSource();
		var icon = new AnimatedIcon
		{
			Width = 96,
			Height = 96,
			Source = source,
			Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
		};
		// "NormalOn" lands on the steady "checkmark fully drawn" state once the
		// NormalOffToNormalOn segment finishes (~310 ms per the Lottie source).
		AnimatedIcon.SetState(icon, "NormalOff");

		var border = new Border
		{
			Width = 96,
			Height = 96,
			Background = new SolidColorBrush(Microsoft.UI.Colors.White),
			Child = icon,
		};

		await UITestHelper.Load(border);
		await TestServices.WindowHelper.WaitForIdle();

		AnimatedIcon.SetState(icon, "NormalOn");
		// 1.5 s is comfortably more than the marker segment + a couple of compositor ticks for
		// the deferred Completed callback to fire and pin Progress at the marker end.
		await Task.Delay(TimeSpan.FromMilliseconds(1500));
		await TestServices.WindowHelper.WaitForIdle();

		var screenshot = await UITestHelper.ScreenShot(border);

		// Locate the checkmark by its bounding box of red pixels.
		var bounds = ImageAssert.GetColorBounds(screenshot, Microsoft.UI.Colors.Red, tolerance: 200);
		Assert.IsFalse(bounds.IsEmpty, "Expected the red checkmark to render at NormalOn state.");
		Assert.IsTrue(bounds.Width >= 20 && bounds.Width <= 80,
			$"Checkmark bounds width {bounds.Width} is outside the expected range — glyph likely mis-sized.");
		Assert.IsTrue(bounds.Height >= 15 && bounds.Height <= 70,
			$"Checkmark bounds height {bounds.Height} is outside the expected range — glyph likely mis-sized.");

		// Find the longest contiguous run of red pixels along any horizontal scan line. The
		// checkmark's V-bottom is where the two ~45° legs converge, producing the widest scan-line
		// run in the icon. For a 96×96 AnimatedIcon (outer scale 96/48 = 2, inner shape Scale 0.7,
		// raw 4 DIP stroke):
		//   - After fix:  effective stroke = 4 × 2 × 0.7 = 5.6 DIP. V-bottom width ≈ 5.6 × 2 / sin(45°) ≈ 16 px (with AA, ~14–18 px).
		//   - Before fix: effective stroke = 4 × 2     = 8.0 DIP. V-bottom width ≈ 8.0 × 2 / sin(45°) ≈ 23 px (with AA, ~21–25 px).
		// A 19-pixel ceiling discriminates with margin on both sides.
		var maxRun = MaxHorizontalRun(screenshot, Microsoft.UI.Colors.Red, bounds, tolerance: 200);
		Assert.IsTrue(maxRun <= 19,
			$"Checkmark V-bottom horizontal scan width {maxRun} px exceeds 19 px ceiling " +
			$"(bounds: {bounds}). This indicates CompositionSpriteShape.Scale isn't scaling the " +
			"stroke thickness through the canvas — the rendered checkmark is too thick relative " +
			"to the WinUI baseline (~16 px).");
		Assert.IsTrue(maxRun >= 8,
			$"Checkmark V-bottom horizontal scan width {maxRun} px is suspiciously narrow " +
			$"(bounds: {bounds}). Stroke may have been thinned to invisibility.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Path_Stretched_Stroke_Stays_At_Declared_Thickness()
	{
		// Regression for the Shape.skia.cs Stretch refactor: WinUI Path/Rectangle keep
		// StrokeThickness constant regardless of Stretch. The CompositionShape transform refactor
		// re-routes Shape's stretch through the geometry-only transform channel so this contract
		// is preserved. A 4 DIP stroke on a 100-unit horizontal line stretched to 200 logical
		// pixels must still render as ~4 px tall.
		var path = new Path
		{
			Width = 200,
			Height = 200,
			Stretch = Stretch.Fill,
			Stroke = new SolidColorBrush(Microsoft.UI.Colors.Red),
			StrokeThickness = 4,
			Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), "M 0,50 L 100,50"),
		};
		var host = new Border
		{
			Width = 200,
			Height = 200,
			Background = new SolidColorBrush(Microsoft.UI.Colors.White),
			Child = path,
		};

		await UITestHelper.Load(host);
		await TestServices.WindowHelper.WaitForIdle();

		var screenshot = await UITestHelper.ScreenShot(host);

		var bounds = ImageAssert.GetColorBounds(screenshot, Microsoft.UI.Colors.Red, tolerance: 40);
		Assert.IsFalse(bounds.IsEmpty, "Stretched Path should render visible red stroke pixels.");

		// Stroke thickness is 4 DIP; stretch must not amplify it. Allow ±2 px for AA fringe and
		// platform-specific rasterization differences.
		Assert.IsTrue(bounds.Height >= 3 && bounds.Height <= 7,
			$"Stretched Path stroke height {bounds.Height} should stay near declared 4 px " +
			"regardless of Stretch=Fill scaling.");
	}

	private static int MaxHorizontalRun(RawBitmap bitmap, Windows.UI.Color color, Windows.Foundation.Rect bounds, int tolerance)
	{
		int max = 0;
		var xMin = (int)bounds.X;
		var yMin = (int)bounds.Y;
		var xMax = Math.Min((int)Math.Ceiling(bounds.X + bounds.Width), bitmap.Width);
		var yMax = Math.Min((int)Math.Ceiling(bounds.Y + bounds.Height), bitmap.Height);

		for (int y = yMin; y < yMax; y++)
		{
			int run = 0;
			for (int x = xMin; x < xMax; x++)
			{
				var p = bitmap.GetPixel(x, y);
				bool match = Math.Abs(p.R - color.R) <= tolerance
					&& Math.Abs(p.G - color.G) <= tolerance
					&& Math.Abs(p.B - color.B) <= tolerance;
				if (match)
				{
					run++;
					if (run > max) max = run;
				}
				else
				{
					run = 0;
				}
			}
		}
		return max;
	}
}
