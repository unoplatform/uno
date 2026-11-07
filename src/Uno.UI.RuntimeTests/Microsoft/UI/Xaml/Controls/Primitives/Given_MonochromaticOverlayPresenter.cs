using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Tests.Common;
using MUXControlsTestApp.Utilities;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls_Primitives;

[TestClass]
[RunsOnUIThread]
// WinUI's RenderTargetBitmap doesn't capture the SetElementChildVisual content the presenter renders through.
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
public class Given_MonochromaticOverlayPresenter
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/14353")]
	public async Task When_Source_Has_Content()
	{
		var (root, _, _) = CreateSut();

		await UITestHelper.Load(root);
		await WaitForBrushUpdate();

		var screenshot = await UITestHelper.ScreenShot(root);

		// Covered source content takes the ReplacementColor, the uncovered part keeps its own color.
		ImageAssert.HasColorAt(screenshot, 50, 25, Colors.Red, tolerance: 5);
		ImageAssert.HasColorAt(screenshot, 50, 75, Colors.Blue, tolerance: 5);
		// Transparent source pixels stay transparent under the presenter (alpha is preserved).
		ImageAssert.HasColorAt(screenshot, 150, 25, Colors.White, tolerance: 5);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/14353")]
	public async Task When_Source_Content_Moves()
	{
		var (root, _, content) = CreateSut();

		await UITestHelper.Load(root);
		await WaitForBrushUpdate();

		// Like a scrolling LoopingSelector: the source's content changes while the presenter itself does not.
		content.RenderTransform = new TranslateTransform { X = 100 };
		await WaitForBrushUpdate();

		var screenshot = await UITestHelper.ScreenShot(root);

		ImageAssert.HasColorAt(screenshot, 50, 25, Colors.White, tolerance: 5);
		ImageAssert.HasColorAt(screenshot, 150, 25, Colors.Red, tolerance: 5);
		ImageAssert.HasColorAt(screenshot, 150, 75, Colors.Blue, tolerance: 5);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/14353")]
	public async Task When_ReplacementColor_Changes()
	{
		var (root, presenter, _) = CreateSut();

		await UITestHelper.Load(root);
		await WaitForBrushUpdate();

		presenter.ReplacementColor = Colors.Green;
		await WaitForBrushUpdate();

		var screenshot = await UITestHelper.ScreenShot(root);

		ImageAssert.HasColorAt(screenshot, 50, 25, Colors.Green, tolerance: 5);
		ImageAssert.HasColorAt(screenshot, 50, 75, Colors.Blue, tolerance: 5);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/14353")]
	public async Task When_ReplacementColor_Transparent()
	{
		var (root, presenter, _) = CreateSut();
		presenter.ReplacementColor = Colors.Transparent;

		await UITestHelper.Load(root);
		await WaitForBrushUpdate();

		var screenshot = await UITestHelper.ScreenShot(root);

		// A transparent ReplacementColor shows the source as-is (identity color matrix).
		ImageAssert.HasColorAt(screenshot, 50, 25, Colors.Blue, tolerance: 5);
	}

	// Text recolored to its own color over an opaque background must look exactly like the text itself: any
	// resampling of the overlay's source (a texture rasterized at another density, or off the pixel grid) blurs it.
	[TestMethod]
	[RequiresScaling(1f)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/14353")]
	public Task When_Text_Recolored_Is_Sharp_At_1x() => AssertRecoloredTextIsSharp();

	[TestMethod]
	[RequiresScaling(1.5f)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/14353")]
	public Task When_Text_Recolored_Is_Sharp_At_150() => AssertRecoloredTextIsSharp();

	[TestMethod]
	[RequiresScaling(2.625f)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/14353")]
	public Task When_Text_Recolored_Is_Sharp_At_2625() => AssertRecoloredTextIsSharp();

	private static async Task AssertRecoloredTextIsSharp()
	{
		var text = new TextBlock { Text = "Sharp overlay text", FontSize = 20, Foreground = new SolidColorBrush(Colors.Black) };
		var source = new Grid { Padding = new Thickness(3.3, 2.7, 0, 0) };
		source.Children.Add(text);
		var presenter = new MonochromaticOverlayPresenter
		{
			ReplacementColor = Colors.Black,
			Background = new SolidColorBrush(Colors.White),
			SourceElement = source,
			IsHitTestVisible = false,
		};
		var root = new Grid
		{
			Width = 220,
			Height = 40,
			Background = new SolidColorBrush(Colors.White),
		};
		root.Children.Add(source);
		root.Children.Add(presenter);

		await UITestHelper.Load(root);
		await WaitForBrushUpdate();
		var overlaid = await UITestHelper.ScreenShot(root);

		presenter.Visibility = Visibility.Collapsed;
		await WaitForBrushUpdate();
		var plain = await UITestHelper.ScreenShot(root);

		var (maxDiff, at) = MaxDifference(overlaid, plain);
		Assert.IsTrue(maxDiff <= 24, $"The recolored text differs from the text itself by {maxDiff} at {at}: the overlay is not pixel-sharp.");
	}

	private static (int MaxDiff, string At) MaxDifference(RawBitmap a, RawBitmap b)
	{
		var pa = a.GetPixels();
		var pb = b.GetPixels();
		Assert.AreEqual(pa.Length, pb.Length);
		var width = a.Bitmap.PixelWidth;
		int max = 0, at = 0;
		for (var i = 0; i < pa.Length; i++)
		{
			var d = System.Math.Abs(pa[i] - pb[i]);
			if (d > max)
			{
				max = d;
				at = i / 4;
			}
		}

		return (max, $"({at % width}, {at / width}) device px");
	}

	// Asserts that the selected value of a Date/TimePicker flyout shows through its highlight band, recolored by the
	// MonochromaticOverlayPresenter, and that the band follows the value when the selector is scrolled.
	internal static async Task AssertHighlightTracksSelection(FrameworkElement flyoutPresenter, LoopingSelector selector)
	{
		var highlight = (FrameworkElement)flyoutPresenter.FindVisualChildByName("HighlightRect");
		Assert.IsNotNull(highlight);

		var overlays = VisualTreeUtils.FindVisualChildrenByType<MonochromaticOverlayPresenter>(flyoutPresenter).ToArray();
		Assert.IsTrue(overlays.Length > 0);
		foreach (var overlay in overlays)
		{
			Assert.AreEqual(1, overlay.Opacity);
			Assert.IsNotNull(overlay.SourceElement);
		}

		var replacementColor = overlays[0].ReplacementColor;
		replacementColor.A = 255;

		var bounds = highlight.TransformToVisual(flyoutPresenter).TransformBounds(new(0, 0, highlight.ActualWidth, highlight.ActualHeight));
		const int inset = 4;
		var band = new System.Drawing.Rectangle(
			(int)bounds.X + inset,
			(int)bounds.Y + inset,
			(int)bounds.Width - 2 * inset,
			(int)bounds.Height - 2 * inset);

		var before = await UITestHelper.ScreenShot(flyoutPresenter);
		// Without the presenter, the opaque highlight hides the selected value under a solid fill.
		ImageAssert.HasColorInRectangle(before, band, replacementColor, tolerance: 30);

		await LoopingSelectorHelper.DoLoopingSelectorSelectionChange(selector);
		await UITestHelper.WaitForIdle();

		var after = await UITestHelper.ScreenShot(flyoutPresenter);
		ImageAssert.HasColorInRectangle(after, band, replacementColor, tolerance: 30);
		Assert.IsTrue(HasDifferentPixel(before, after, band), "The highlight band did not update when the selector scrolled.");
	}

	private static bool HasDifferentPixel(RawBitmap a, RawBitmap b, System.Drawing.Rectangle rect)
	{
		for (var x = rect.Left; x < rect.Right; x++)
		{
			for (var y = rect.Top; y < rect.Bottom; y++)
			{
				if (a.GetPixel(x, y) != b.GetPixel(x, y))
				{
					return true;
				}
			}
		}

		return false;
	}

	// Like WinUI, the presenter rebuilds its brush on the next CompositionTarget.Rendering tick.
	private static async Task WaitForBrushUpdate()
	{
		await UITestHelper.WaitForIdle();
		await UITestHelper.WaitForRender(frameCount: 2);
	}

	// A 200x100 white root with a source whose left half (100x100) is blue, and a presenter over the top half.
	private static (Grid Root, MonochromaticOverlayPresenter Presenter, Border Content) CreateSut()
	{
		var content = new Border
		{
			Width = 100,
			Height = 100,
			HorizontalAlignment = HorizontalAlignment.Left,
			Background = new SolidColorBrush(Colors.Blue),
		};

		var source = new Grid { Width = 200, Height = 100 };
		source.Children.Add(content);

		var presenter = new MonochromaticOverlayPresenter
		{
			Width = 200,
			Height = 50,
			VerticalAlignment = VerticalAlignment.Top,
			ReplacementColor = Colors.Red,
			SourceElement = source,
			IsHitTestVisible = false,
		};

		var root = new Grid
		{
			Width = 200,
			Height = 100,
			Background = new SolidColorBrush(Colors.White),
		};
		root.Children.Add(source);
		root.Children.Add(presenter);

		return (root, presenter, content);
	}
}
