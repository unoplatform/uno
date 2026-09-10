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

	[TestMethod]
	[RequiresFullWindow]
	public async Task When_No_AppBars_Then_Content_Fills_Page()
	{
		var (page, content) = await SetupPageAsync();

		try
		{
			VerifyContentBounds(page, content, topInset: 0, totalInset: 0);
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

			VerifyContentBounds(page, content, topInset: CompactHeight, totalInset: CompactHeight);

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
			VerifyContentBounds(page, content, topInset: CompactHeight, totalInset: 2 * CompactHeight);
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

			VerifyContentBounds(page, content, topInset: MinimalHeight, totalInset: MinimalHeight + CompactHeight);
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
			VerifyContentBounds(page, content, topInset: 0, totalInset: 0);
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
			VerifyContentBounds(page, content, topInset: 0, totalInset: 0);
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

			VerifyContentBounds(page, content, topInset: CompactHeight, totalInset: CompactHeight);
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
			VerifyContentBounds(page, content, topInset: CompactHeight, totalInset: CompactHeight);
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

			VerifyContentBounds(page, content, topInset: CompactHeight, totalInset: CompactHeight);

			appBar.ClosedDisplayMode = AppBarClosedDisplayMode.Minimal;
			await TestServices.WindowHelper.WaitForIdle();

			VerifyContentBounds(page, content, topInset: MinimalHeight, totalInset: MinimalHeight);

			appBar.Visibility = Visibility.Collapsed;
			await TestServices.WindowHelper.WaitForIdle();

			VerifyContentBounds(page, content, topInset: 0, totalInset: 0);

			appBar.Visibility = Visibility.Visible;
			await TestServices.WindowHelper.WaitForIdle();

			VerifyContentBounds(page, content, topInset: MinimalHeight, totalInset: MinimalHeight);

			page.TopAppBar = null;
			await TestServices.WindowHelper.WaitForIdle();

			VerifyContentBounds(page, content, topInset: 0, totalInset: 0);
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

			VerifyContentBounds(page, content, topInset: 0, totalInset: 0);
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
	/// Asserts the content rect the page arranged, measured relative to the page itself so an
	/// embedded test root or a title bar cannot shift the expectation.
	/// </summary>
	private static void VerifyContentBounds(Page page, FrameworkElement content, double topInset, double totalInset)
	{
		var origin = content.TransformToVisual(page).TransformPoint(new Point(0, 0));

		Assert.AreEqual(0d, origin.X, Tolerance, "Content X");
		Assert.AreEqual(topInset, origin.Y, Tolerance, "Content Y");
		Assert.AreEqual(page.ActualWidth, content.ActualWidth, Tolerance, "Content width");
		Assert.AreEqual(page.ActualHeight - totalInset, content.ActualHeight, Tolerance, "Content height");
	}
}
