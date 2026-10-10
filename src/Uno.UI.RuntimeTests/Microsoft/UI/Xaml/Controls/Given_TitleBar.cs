#if HAS_UNO_WINUI || WINDOWS
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_TitleBar
{
	[TestMethod]
	public async Task When_Loaded_Template_Parts_Are_Realized()
	{
		var content = new Button { Content = "Content" };
		var rightHeader = new Button { Content = "Right" };
		var sut = new TitleBar
		{
			Title = "My Title",
			Subtitle = "Preview",
			IsBackButtonVisible = true,
			IsPaneToggleButtonVisible = true,
			Content = content,
			RightHeader = rightHeader,
		};

		try
		{
			await UITestHelper.Load(sut);

			sut.FindFirstDescendant<Button>("PART_BackButton").Should().NotBeNull();
			sut.FindFirstDescendant<Button>("PART_PaneToggleButton").Should().NotBeNull();
			sut.FindFirstDescendant<TextBlock>(t => t.Text == "My Title").Should().NotBeNull();
			sut.FindFirstDescendant<TextBlock>(t => t.Text == "Preview").Should().NotBeNull();
			content.ActualWidth.Should().BeGreaterThan(0);
			rightHeader.ActualWidth.Should().BeGreaterThan(0);
		}
		finally
		{
			ClearPassthroughRegions(sut);
		}
	}

	[TestMethod]
	public async Task When_Buttons_Invoked_Requests_Are_Raised()
	{
		var sut = new TitleBar
		{
			Title = "My Title",
			IsBackButtonVisible = true,
			IsPaneToggleButtonVisible = true,
		};
		var backRequested = 0;
		var paneToggleRequested = 0;
		sut.BackRequested += (s, e) => backRequested++;
		sut.PaneToggleRequested += (s, e) => paneToggleRequested++;

		try
		{
			await UITestHelper.Load(sut);

			Invoke(sut.FindFirstDescendant<Button>("PART_BackButton")!);
			Invoke(sut.FindFirstDescendant<Button>("PART_PaneToggleButton")!);

			backRequested.Should().Be(1);
			paneToggleRequested.Should().Be(1);
		}
		finally
		{
			ClearPassthroughRegions(sut);
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Content_Set_Interactive_Elements_Are_Passthrough()
	{
		var content = new Button { Content = "Content" };
		var sut = new TitleBar { Title = "My Title", Content = content };

		try
		{
			await UITestHelper.Load(sut);

			var source = InputNonClientPointerSource.GetForWindowId(sut.XamlRoot!.ContentIslandEnvironment!.AppWindowId);
			await TestServices.WindowHelper.WaitFor(() => source.GetRegionRects(NonClientRegionKind.Passthrough).Length > 0);

			var bounds = content.TransformToVisual(null).TransformBounds(new(0, 0, content.ActualWidth, content.ActualHeight));
			var scale = sut.XamlRoot.RasterizationScale;
			var center = (X: (int)((bounds.X + bounds.Width / 2) * scale), Y: (int)((bounds.Y + bounds.Height / 2) * scale));

			source.GetRegionRects(NonClientRegionKind.Passthrough)
				.Should().Contain(r => r.X <= center.X && center.X < r.X + r.Width && r.Y <= center.Y && center.Y < r.Y + r.Height);
		}
		finally
		{
			ClearPassthroughRegions(sut);
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public async Task When_Extended_Into_TitleBar_RightHeader_Avoids_Caption_Buttons()
	{
		var rightHeader = new Button { Content = "Right" };
		var sut = new TitleBar { Title = "My Title", RightHeader = rightHeader };
		var window = new Window { Content = sut };
		window.ExtendsContentIntoTitleBar = true;
		window.SetTitleBar(sut);
		var activated = false;
		try
		{
			window.Activated += (s, e) => activated = true;
			window.Activate();
			await TestServices.WindowHelper.WaitFor(() => activated && rightHeader.ActualWidth > 0);

			var rightInset = window.AppWindow.TitleBar.RightInset;
			rightInset.Should().BeGreaterThan(0);

			// WinUI sizes the right padding column straight from RightInset, so the header must end before it.
			await TestServices.WindowHelper.WaitFor(() =>
				rightHeader.TransformToVisual(sut).TransformBounds(new(0, 0, rightHeader.ActualWidth, rightHeader.ActualHeight)).Right <= sut.ActualWidth - rightInset);
		}
		finally
		{
			await TestServices.WindowHelper.WaitForIdle();
			window.Close();
		}
	}

	private static void Invoke(Button button) =>
		((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();

	// TitleBar registers Passthrough rects on the shared test window; don't leak them into other tests.
	private static void ClearPassthroughRegions(TitleBar sut)
	{
		if (sut.XamlRoot?.ContentIslandEnvironment is { } environment)
		{
			InputNonClientPointerSource.GetForWindowId(environment.AppWindowId).ClearRegionRects(NonClientRegionKind.Passthrough);
		}

		TestServices.WindowHelper.WindowContent = null;
	}
}
#endif
