using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO && !HAS_UNO_WINUI
using Windows.UI.Xaml.Controls;
#endif

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
public partial class Given_PipsPager
{
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)]
	public async Task When_SelectedIndex_Beyond_MaxVisiblePips_All_Visible_Pips_Are_Realized()
	{
		// Repro for the trailing-pips-disappear bug: with NumberOfPages > MaxVisiblePips,
		// navigating past the initial visible window must scroll the trailing pips into
		// view and they must render. Previously the inner ItemsRepeater was given a
		// LayoutClip matching the viewport (60) even though its content was wider (96),
		// which clipped away every pip that scrolled in from the trailing side.

		// Give the pager an opaque, known background and force a known theme so
		// the trailing-pixel check can distinguish rendered pip ink from the
		// surrounding background regardless of the host's current theme.
		// (Pip glyph color comes from theme resources — Light theme produces dark
		// glyphs on the white background; the inverse holds for Dark theme.)
		var SUT = new PipsPager
		{
			NumberOfPages = 8,
			MaxVisiblePips = 5,
			RequestedTheme = ElementTheme.Light,
			Background = new SolidColorBrush(Microsoft.UI.Colors.White),
		};

		await UITestHelper.Load(SUT);

		var repeater = SUT.FindFirstDescendant<ItemsRepeater>("PipsPagerItemsRepeater");
		Assert.IsNotNull(repeater, "Inner ItemsRepeater not found");

		var scrollViewer = SUT.FindFirstDescendant<ScrollViewer>("PipsPagerScrollViewer");
		Assert.IsNotNull(scrollViewer, "Inner ScrollViewer not found");

		SUT.SelectedPageIndex = 5;
		await TestServices.WindowHelper.WaitForIdle();
		// Allow the BringIntoView animation (≈1s on Skia) to complete before sampling.
		await Task.Delay(1500);
		await TestServices.WindowHelper.WaitForIdle();

		var horizontalOffset = scrollViewer.HorizontalOffset;
		var viewportWidth = scrollViewer.ViewportWidth;
		var extentWidth = scrollViewer.ExtentWidth;

		// With pip 5 centered, the visible window covers indices 3..7 — all five must
		// be realized and laid out at distinct positions inside the pager.
		var positions = new System.Collections.Generic.Dictionary<int, Windows.Foundation.Point>();
		for (int i = 3; i <= 7; i++)
		{
			var pip = repeater.TryGetElement(i) as FrameworkElement;
			Assert.IsNotNull(pip, $"Pip {i} must be realized when SelectedPageIndex centers it in the viewport");
			Assert.IsTrue(pip.ActualWidth > 0, $"Pip {i} must have non-zero ActualWidth after navigation, was {pip.ActualWidth}");
			Assert.IsTrue(pip.ActualHeight > 0, $"Pip {i} must have non-zero ActualHeight after navigation, was {pip.ActualHeight}");
			positions[i] = pip.TransformToVisual(SUT).TransformPoint(new Windows.Foundation.Point(0, 0));
		}

		Assert.IsTrue(horizontalOffset > 0,
			$"ScrollViewer must have scrolled to bring the selected pip into view. " +
			$"HorizontalOffset={horizontalOffset}, ViewportWidth={viewportWidth}, ExtentWidth={extentWidth}");

		for (int i = 4; i <= 7; i++)
		{
			Assert.IsTrue(positions[i].X > positions[i - 1].X,
				$"Pip {i} ({positions[i].X}) must lay out to the right of pip {i - 1} ({positions[i - 1].X})");
		}

		for (int i = 3; i <= 7; i++)
		{
			Assert.IsTrue(positions[i].X >= 0 && positions[i].X < SUT.ActualWidth,
				$"Pip {i} X position {positions[i].X} is outside pager width {SUT.ActualWidth}. " +
				$"ScrollViewer offset={horizontalOffset}, viewport={viewportWidth}, extent={extentWidth}");
		}

#if HAS_RENDER_TARGET_BITMAP
		// Visual check — the previous assertions only verify layout state. This catches
		// the case where pips are at the right coordinates but never make it to the
		// rendered output (clip / opacity / composition issue). The bug clipped the
		// inner ItemsRepeater at viewport-width, so any rendered pixel that lands in
		// the trailing half of the pager proves the trailing pips render.
		// Use an opaque screenshot and a known white background so the only non-white
		// pixels are actual rendered pip ink (avoiding false positives from
		// fully-transparent RGB=0 pixels).
		var screenshot = await UITestHelper.ScreenShot(SUT, opaque: true);
		bool inkInTrailingHalf = false;
		int midY = screenshot.Height / 2;
		var trailingStart = screenshot.Width / 2;
		for (int x = trailingStart; x < screenshot.Width; x++)
		{
			var c = screenshot.GetPixel(x, midY);
			// Background is opaque white; anything materially darker is rendered ink.
			if (c.A == 255 && (c.R < 250 || c.G < 250 || c.B < 250))
			{
				inkInTrailingHalf = true;
				break;
			}
		}

		Assert.IsTrue(inkInTrailingHalf,
			$"Expected pip ink in the pager's trailing half (>= x={trailingStart}). " +
			$"Before the fix the inner ItemsRepeater was clipped at viewport-width, so the trailing pips never rendered. " +
			$"ScrollViewer offset={horizontalOffset}, viewport={viewportWidth}, extent={extentWidth}.");
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
#if __WASM__
	[Ignore("RenderTargetBitmap is not implemented on WASM.")]
#else
	[Ignore("Fails even on Windows, very flaky on Uno.")] // Flaky #9080
#endif
	public async Task When_MaxVisiblePips_GreaterThan_NumberOfPages_Horizontal()
	{
		var SUT = new PipsPager
		{
			NumberOfPages = 7,
			MaxVisiblePips = 5
		};

		await UITestHelper.Load(SUT);

		var initialScreenshot = await UITestHelper.ScreenShot(SUT);

		var color = initialScreenshot.GetPixel(initialScreenshot.Width - 5, initialScreenshot.Height / 2);

		SUT.SelectedPageIndex = 3;
		await TestServices.WindowHelper.WaitForIdle();

		var scrolledScreenshot = await UITestHelper.ScreenShot(SUT);
		ImageAssert.HasColorAt(scrolledScreenshot, scrolledScreenshot.Width - 5, scrolledScreenshot.Height / 2, color);
	}

	[TestMethod]
	[RunsOnUIThread]
#if __WASM__
	[Ignore("RenderTargetBitmap is not implemented on WASM.")]
#else
	[Ignore("Very flaky on Uno.")] // Flaky #9080
#endif
	public async Task When_MaxVisiblePips_GreaterThan_NumberOfPages_Vertical()
	{
		var SUT = new PipsPager
		{
			NumberOfPages = 7,
			MaxVisiblePips = 5
		};

		await UITestHelper.Load(SUT);

		var initialScreenshot = await UITestHelper.ScreenShot(SUT);

		var color = initialScreenshot.GetPixel(initialScreenshot.Width / 2, initialScreenshot.Height - 5);

		SUT.SelectedPageIndex = 3;
		await TestServices.WindowHelper.WaitForIdle();

		var scrolledScreenshot = await UITestHelper.ScreenShot(SUT);
		ImageAssert.HasColorAt(scrolledScreenshot, scrolledScreenshot.Width / 2, scrolledScreenshot.Height - 5, color);
	}
}
