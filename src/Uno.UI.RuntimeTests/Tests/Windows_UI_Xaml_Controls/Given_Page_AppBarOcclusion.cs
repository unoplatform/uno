using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

/// <summary>
/// Covers Page's docked app bar occlusion: WinUI's Page shrinks and offsets the rect it measures
/// and arranges its Content into so a docked Top/BottomAppBar does not cover page content
/// (Page_Partial.cpp CalculateAppBarOcclusionDimensions / CalculateUpdatedBounds).
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_Page_AppBarOcclusion
{
	/// <summary>
	/// AppBarThemeCompactHeight in the Fluent theme - the height a Compact bar reports whether it is
	/// open or closed (AppBar_Partial.cpp MeasureOverride). Same constant WinUI's own ValidateFootprint uses.
	/// </summary>
	private const double CompactHeight = 48;

	/// <summary>AppBarThemeMinimalHeight in the Fluent theme.</summary>
	private const double MinimalHeight = 24;

	private const double Tolerance = 0.5;

	/// <summary>How many consecutive idle rounds must report the same bar heights before we believe them.</summary>
	private const int RequiredStableRounds = 3;

	private const int MaxSettleRounds = 100;

	[TestMethod]
	[RequiresFullWindow]
	public async Task When_No_AppBars_Then_Content_Fills_Page()
	{
		var (page, content) = await SetupPageAsync();

		try
		{
			await VerifyContentBoundsAsync(page, content, topInset: 0, totalInset: 0);
		}
		finally
		{
			await CleanupAsync();
		}
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task When_TopAppBar_Compact_Then_Content_Is_Pushed_Down()
	{
		var (page, content) = await SetupPageAsync();

		try
		{
			var pageHeight = page.ActualHeight;

			page.TopAppBar = CreateAppBar(AppBarClosedDisplayMode.Compact);
			await TestServices.WindowHelper.WaitForIdle();

			await VerifyContentBoundsAsync(page, content, topInset: CompactHeight, totalInset: CompactHeight);

			// Page::ArrangeOverride returns the full arrangeSize - only the content is inset.
			Assert.AreEqual(pageHeight, page.ActualHeight, Tolerance, "The page itself must not shrink.");
		}
		finally
		{
			await CleanupAsync();
		}
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task When_Top_And_Bottom_AppBars_Then_Content_Is_Inset_By_Both()
	{
		var (page, content) = await SetupPageAsync();

		try
		{
			page.TopAppBar = CreateAppBar(AppBarClosedDisplayMode.Compact);
			page.BottomAppBar = CreateAppBar(AppBarClosedDisplayMode.Compact);
			await TestServices.WindowHelper.WaitForIdle();

			// The offset is the TOP bar's height alone, while the consumed height is the sum of both -
			// Page_Partial.cpp sets Rect.Y to topAppBarHeight and Rect.Height to top + bottom.
			await VerifyContentBoundsAsync(page, content, topInset: CompactHeight, totalInset: 2 * CompactHeight);
		}
		finally
		{
			await CleanupAsync();
		}
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task When_Top_Minimal_And_Bottom_Compact_Then_Content_Uses_Closed_Heights()
	{
		var (page, content) = await SetupPageAsync();

		try
		{
			page.TopAppBar = CreateAppBar(AppBarClosedDisplayMode.Minimal);
			page.BottomAppBar = CreateAppBar(AppBarClosedDisplayMode.Compact);
			await TestServices.WindowHelper.WaitForIdle();

			await VerifyContentBoundsAsync(page, content, topInset: MinimalHeight, totalInset: MinimalHeight + CompactHeight);
		}
		finally
		{
			await CleanupAsync();
		}
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task When_AppBar_Hidden_Then_No_Space_Is_Reserved()
	{
		var (page, content) = await SetupPageAsync();

		try
		{
			page.TopAppBar = CreateAppBar(AppBarClosedDisplayMode.Hidden);
			await TestServices.WindowHelper.WaitForIdle();

			// AppBar::MeasureOverride pins the Hidden desired height to 0.
			await VerifyContentBoundsAsync(page, content, topInset: 0, totalInset: 0);
		}
		finally
		{
			await CleanupAsync();
		}
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task When_AppBar_Collapsed_Then_No_Space_Is_Reserved()
	{
		var (page, content) = await SetupPageAsync();

		try
		{
			var appBar = CreateAppBar(AppBarClosedDisplayMode.Compact);
			appBar.Visibility = Visibility.Collapsed;
			page.TopAppBar = appBar;
			await TestServices.WindowHelper.WaitForIdle();

			// Collapsed does not zero ActualHeight, so Page::GetAppBarClosedHeight has to report 0 itself.
			await VerifyContentBoundsAsync(page, content, topInset: 0, totalInset: 0);
		}
		finally
		{
			await CleanupAsync();
		}
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task When_AppBar_IsOpen_Then_Reserved_Space_Is_The_Closed_Height()
	{
		var (page, content) = await SetupPageAsync();

		try
		{
			var appBar = CreateAppBar(AppBarClosedDisplayMode.Compact);

			// Content far taller than the compact height: an open docked bar is supposed to overhang
			// the page content, so the reservation must stay at the closed-display-mode height.
			appBar.Content = new Border { Height = 200 };
			appBar.IsOpen = true;
			page.TopAppBar = appBar;
			await TestServices.WindowHelper.WaitForIdle();

			await VerifyContentBoundsAsync(page, content, topInset: CompactHeight, totalInset: CompactHeight);
		}
		finally
		{
			await CleanupAsync();
		}
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task When_AppBar_IsSticky_Then_Reserved_Space_Is_Unchanged()
	{
		var (page, content) = await SetupPageAsync();

		try
		{
			var appBar = CreateAppBar(AppBarClosedDisplayMode.Compact);
			appBar.IsSticky = true;
			page.TopAppBar = appBar;
			await TestServices.WindowHelper.WaitForIdle();

			// IsSticky only drives the light-dismiss shield; nothing in Page reads it.
			await VerifyContentBoundsAsync(page, content, topInset: CompactHeight, totalInset: CompactHeight);
		}
		finally
		{
			await CleanupAsync();
		}
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task When_AppBar_Changes_Then_Content_Reflows()
	{
		var (page, content) = await SetupPageAsync();

		try
		{
			var appBar = CreateAppBar(AppBarClosedDisplayMode.Compact);
			page.TopAppBar = appBar;
			await TestServices.WindowHelper.WaitForIdle();

			await VerifyContentBoundsAsync(page, content, topInset: CompactHeight, totalInset: CompactHeight);

			appBar.ClosedDisplayMode = AppBarClosedDisplayMode.Minimal;
			await TestServices.WindowHelper.WaitForIdle();

			await VerifyContentBoundsAsync(page, content, topInset: MinimalHeight, totalInset: MinimalHeight);

			appBar.Visibility = Visibility.Collapsed;
			await TestServices.WindowHelper.WaitForIdle();

			await VerifyContentBoundsAsync(page, content, topInset: 0, totalInset: 0);

			appBar.Visibility = Visibility.Visible;
			await TestServices.WindowHelper.WaitForIdle();

			await VerifyContentBoundsAsync(page, content, topInset: MinimalHeight, totalInset: MinimalHeight);

			page.TopAppBar = null;
			await TestServices.WindowHelper.WaitForIdle();

			await VerifyContentBoundsAsync(page, content, topInset: 0, totalInset: 0);
		}
		finally
		{
			await CleanupAsync();
		}
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task When_Inline_AppBar_Then_No_Space_Is_Reserved()
	{
		var (page, content) = await SetupPageAsync();

		try
		{
			// An inline bar is laid out by its own parent; only Page.TopAppBar/BottomAppBar occlude.
			content.Child = new AppBar
			{
				ClosedDisplayMode = AppBarClosedDisplayMode.Compact,
				VerticalAlignment = VerticalAlignment.Top,
			};
			await TestServices.WindowHelper.WaitForIdle();

			await VerifyContentBoundsAsync(page, content, topInset: 0, totalInset: 0);
		}
		finally
		{
			await CleanupAsync();
		}
	}

	private static async Task<(Page page, Border content)> SetupPageAsync()
	{
		var page = TestServices.WindowHelper.SetupSimulatedAppPage();

		var content = new Border
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Background = new SolidColorBrush(Microsoft.UI.Colors.CornflowerBlue),
		};

		page.Content = content;

		await TestServices.WindowHelper.WaitForLoaded(content);
		await TestServices.WindowHelper.WaitForIdle();

		return (page, content);
	}

	private static async Task CleanupAsync()
	{
		TestServices.WindowHelper.WindowContent = null;
		await TestServices.WindowHelper.WaitForIdle();
	}

	private static AppBar CreateAppBar(AppBarClosedDisplayMode closedDisplayMode)
		=> new AppBar { ClosedDisplayMode = closedDisplayMode };

	/// <summary>
	/// A docked bar is created, templated and measured across several layout passes, and a single
	/// WaitForIdle can return while it still reports 0x0 - which reads exactly like "no bar" and
	/// would let the zero-inset cases pass vacuously. Pump idle until both bars' heights hold still.
	/// </summary>
	private static async Task WaitForAppBarsSettledAsync(Page page)
	{
		var previousTop = double.NegativeInfinity;
		var previousBottom = double.NegativeInfinity;
		var stableRounds = 0;

		for (var attempt = 0; attempt < MaxSettleRounds && stableRounds < RequiredStableRounds; attempt++)
		{
			await TestServices.WindowHelper.WaitForIdle();

			var top = ClosedHeightOf(page.TopAppBar);
			var bottom = ClosedHeightOf(page.BottomAppBar);

			stableRounds = top == previousTop && bottom == previousBottom ? stableRounds + 1 : 0;
			previousTop = top;
			previousBottom = bottom;
		}

		// -1 while a bar exists but has not been loaded yet, so an unmeasured bar can never be
		// mistaken for the settled zero that "no bar at all" produces.
		static double ClosedHeightOf(AppBar bar)
			=> bar is null ? 0 : bar.IsLoaded ? bar.ActualHeight : -1;
	}

	/// <summary>
	/// Asserts the content rect the page arranged, measured relative to the page itself so an
	/// embedded test root or a title bar cannot shift the expectation.
	/// </summary>
	private static async Task VerifyContentBoundsAsync(Page page, FrameworkElement content, double topInset, double totalInset)
	{
		await WaitForAppBarsSettledAsync(page);

		var origin = content.TransformToVisual(page).TransformPoint(new Point(0, 0));

		Assert.AreEqual(0d, origin.X, Tolerance, "Content X");
		Assert.AreEqual(topInset, origin.Y, Tolerance, "Content Y");
		Assert.AreEqual(page.ActualWidth, content.ActualWidth, Tolerance, "Content width");
		Assert.AreEqual(page.ActualHeight - totalInset, content.ActualHeight, Tolerance, "Content height");
	}
}
