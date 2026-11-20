using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Automation.Peers;
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
	// SkiaWasm excluded: render-loop-driven BringIntoView scroll stalls under the headless xvfb browser (flaky). #23524
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23524")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.SkiaWasm)]
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
		await UITestHelper.WaitForIdle(waitForCompositionAnimations: true);
		// Wait for the BringIntoView scroll to actually bring the trailing pip (index 7) into the
		// viewport — the post-condition asserted below — instead of a fixed delay, which on slower
		// runtimes (e.g. WASM) elapsed before the scroll settled and left the offset short, flaking CI.
		await UITestHelper.WaitFor(
			() =>
			{
				if (repeater.TryGetElement(7) is not FrameworkElement trailingPip)
				{
					return false;
				}

				var x = trailingPip.TransformToVisual(SUT).TransformPoint(new Windows.Foundation.Point(0, 0)).X;
				return scrollViewer.HorizontalOffset > 0 && x >= 0 && x < SUT.ActualWidth;
			},
			timeoutMS: 5000,
			message: "Trailing pip did not scroll into the viewport after navigating to SelectedPageIndex 5");
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
	}

	[TestMethod]
	[RunsOnUIThread]
	// Not verified on NativeWinUI. SkiaWasm: animated BringIntoView scroll
	// stalls under the headless browser (#23524).
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/16513")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_NumberOfPages_GreaterThan_MaxVisiblePips_Horizontal()
	{
		// Asserts the trailing slot still renders a pip once the scroll has settled.
		var SUT = new PipsPager
		{
			NumberOfPages = 7,
			MaxVisiblePips = 5,
			RequestedTheme = ElementTheme.Light,
			Background = new SolidColorBrush(Microsoft.UI.Colors.White),
		};

		await UITestHelper.Load(SUT);

		var repeater = SUT.FindFirstDescendant<ItemsRepeater>("PipsPagerItemsRepeater");
		Assert.IsNotNull(repeater, "Inner ItemsRepeater not found");

		var scrollViewer = SUT.FindFirstDescendant<ScrollViewer>("PipsPagerScrollViewer");
		Assert.IsNotNull(scrollViewer, "Inner ScrollViewer not found");

		SUT.SelectedPageIndex = 3;
		await UITestHelper.WaitForIdle(waitForCompositionAnimations: true);
		// Wait for the animated BringIntoView scroll to settle with the selected pip
		// centred in the viewport before asserting or screenshotting.
		await UITestHelper.WaitFor(
			() =>
			{
				if (repeater.TryGetElement(3) is not FrameworkElement selectedPip)
				{
					return false;
				}

				var x = selectedPip.TransformToVisual(SUT).TransformPoint(new Windows.Foundation.Point(0, 0)).X;
				var centre = x + selectedPip.ActualWidth / 2;
				return scrollViewer.HorizontalOffset > 0 && System.Math.Abs(centre - SUT.ActualWidth / 2) <= 1;
			},
			timeoutMS: 5000,
			message: "Selected pip did not scroll to the centre of the viewport after SelectedPageIndex = 3");
		await TestServices.WindowHelper.WaitForIdle();

		var horizontalOffset = scrollViewer.HorizontalOffset;

		Assert.IsTrue(horizontalOffset > 0,
			$"ScrollViewer must have scrolled to bring the selected pip into view. HorizontalOffset={horizontalOffset}");

		// With pip 3 centred in a window of 5 pips, the visible window covers indices
		// 1..5, so the trailing slot (index 5) must still hold a realized, laid-out pip.
		var trailingPip = repeater.TryGetElement(5) as FrameworkElement;
		Assert.IsNotNull(trailingPip, "Pip 5 must be realized in the trailing slot when pip 3 is centred");
		Assert.IsTrue(trailingPip.ActualWidth > 0 && trailingPip.ActualHeight > 0,
			$"Pip 5 must be laid out after navigation, was {trailingPip.ActualWidth}x{trailingPip.ActualHeight}");
		var trailingX = trailingPip.TransformToVisual(SUT).TransformPoint(new Windows.Foundation.Point(0, 0)).X;
		Assert.IsTrue(trailingX >= 0 && trailingX < SUT.ActualWidth,
			$"Pip 5 X position {trailingX} is outside pager width {SUT.ActualWidth}. ScrollViewer offset={horizontalOffset}");

		// With an opaque white background, any rendered pip ink near the trailing
		// edge proves the trailing slot still renders after the scroll.
		var screenshot = await UITestHelper.ScreenShot(SUT, opaque: true);
		bool inkInTrailingSlot = false;
		int midY = screenshot.Height / 2;
		var slotStart = (int)(screenshot.Width * 0.75);
		for (int x = slotStart; x < screenshot.Width; x++)
		{
			var c = screenshot.GetPixel(x, midY);
			if (c.A == 255 && (c.R < 250 || c.G < 250 || c.B < 250))
			{
				inkInTrailingSlot = true;
				break;
			}
		}

		Assert.IsTrue(inkInTrailingSlot,
			$"Expected pip ink in the trailing slot (>= x={slotStart}) after scrolling to SelectedPageIndex 3. ScrollViewer offset={horizontalOffset}.");
	}

	[TestMethod]
	[RunsOnUIThread]
	[Ignore("Very flaky on Uno.")] // Flaky #9080
	public async Task When_NumberOfPages_GreaterThan_MaxVisiblePips_Vertical()
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

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // IsVirtualizationEnabled is MUX_PREVIEW, not public on WinAppSDK
	public async Task When_WrapMode_Changes_Virtualization_Follows()
	{
#if HAS_UNO
		var SUT = new PipsPager { NumberOfPages = 10 };

		await UITestHelper.Load(SUT);

		if (SUT.FindFirstDescendant<ItemsRepeater>("PipsPagerItemsRepeater")?.Layout is not StackLayout stackLayout)
		{
			Assert.Fail("Inner StackLayout not found");
			return;
		}
		Assert.IsTrue(stackLayout.IsVirtualizationEnabled);

		SUT.WrapMode = PipsPagerWrapMode.Wrap;
		Assert.IsFalse(stackLayout.IsVirtualizationEnabled);

		SUT.WrapMode = PipsPagerWrapMode.None;
		Assert.IsTrue(stackLayout.IsVirtualizationEnabled);
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Pip_Is_Recycled_Click_Selects_Once()
	{
		var SUT = new PipsPager { NumberOfPages = 5 };

		await UITestHelper.Load(SUT);

		if (SUT.FindFirstDescendant<ItemsRepeater>("PipsPagerItemsRepeater") is not { } repeater)
		{
			Assert.Fail("Inner ItemsRepeater not found");
			return;
		}

		var originalPips = Enumerable.Range(0, 5).Select(repeater.TryGetElement).ToArray();

		// Shrinking and growing again recycles the trailing pips and prepares them anew.
		SUT.NumberOfPages = 2;
		await TestServices.WindowHelper.WaitForIdle();
		SUT.NumberOfPages = 5;
		await TestServices.WindowHelper.WaitForIdle();

		if (repeater.TryGetElement(2) is not Button pip)
		{
			Assert.Fail("Pip 2 was not realized");
			return;
		}
		CollectionAssert.Contains(originalPips, pip, "The pip must be recycled for this test to exercise re-preparation");

		// Resetting from the handler makes every click handler still attached to the pip select it again.
		var selections = 0;
		SUT.SelectedIndexChanged += (s, e) =>
		{
			if (SUT.SelectedPageIndex == 2)
			{
				selections++;
				SUT.SelectedPageIndex = 0;
			}
		};

		new ButtonAutomationPeer(pip).Invoke();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(1, selections);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_WrapMode_Changes_On_First_Page_Previous_Button_Shows()
	{
		var SUT = new PipsPager
		{
			NumberOfPages = 5,
			PreviousButtonVisibility = PipsPagerButtonVisibility.Visible,
		};

		await UITestHelper.Load(SUT);

		var previousButton = SUT.FindFirstDescendant<Button>("PreviousPageButton");
		Assert.IsNotNull(previousButton, "PreviousPageButton not found");
		Assert.AreEqual(0, previousButton.Opacity, "Previous button is hidden on the first page");

		SUT.WrapMode = PipsPagerWrapMode.Wrap;
		await UITestHelper.WaitForIdle();

		Assert.AreEqual(1, previousButton.Opacity, "Wrapping makes the first page's previous button usable");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_NumberOfPages_Decreases_Items_Are_Removed_From_End()
	{
		var SUT = new PipsPager { NumberOfPages = 5 };

		await UITestHelper.Load(SUT);

		SUT.NumberOfPages = 3;

		CollectionAssert.AreEqual(new[] { 1, 2, 3 }, SUT.TemplateSettings.PipsPagerItems.ToArray());
	}
}
