using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_SplitView_UITest
{
	// Mirrors UITests.Windows_UI_Xaml_Controls.SplitView.SplitViewClip: a right-placed, compact-overlay
	// SplitView whose compact pane must occupy exactly CompactPaneLength, leaving the content area to its left unclipped.
	[TestMethod]
	[RunsOnUIThread]
#if !HAS_RENDER_TARGET_BITMAP
	[Ignore("Cannot take screenshot on this platform.")]
#endif
	public async Task When_RightPane_Clipped()
	{
		var targetRect = new Border
		{
			Margin = new Thickness(0, 0, 48, 0),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
		};

		var split = new SplitView
		{
			Background = new SolidColorBrush(Microsoft.UI.Colors.Blue),
			CompactPaneLength = 48,
			DisplayMode = SplitViewDisplayMode.CompactOverlay,
			IsPaneOpen = false,
			OpenPaneLength = 200,
			PaneBackground = new SolidColorBrush(Microsoft.UI.Colors.Red),
			PanePlacement = SplitViewPanePlacement.Right,
			Content = new Button { Content = "Toggle" },
			Pane = new TextBlock
			{
				Margin = new Thickness(20),
				FontSize = 15,
				Text = "This test is very long and should be clipped!",
				TextWrapping = TextWrapping.Wrap,
			},
		};

		var root = new Grid
		{
			Width = 400,
			Height = 300,
			Children = { targetRect, split },
		};

		try
		{
			await UITestHelper.Load(root);

			// Sample 4px inside TargetRect's own right edge (which is inset by CompactPaneLength)
			// to stay clear of anti-aliasing at the content/pane boundary.
			var x = targetRect.ActualWidth - 4;
			var y = targetRect.ActualHeight / 2;

			var compactScreenshot = await UITestHelper.ScreenShot(root);
			ImageAssert.HasColorAtChild(compactScreenshot, targetRect, x, y, Microsoft.UI.Colors.Blue);

			split.IsPaneOpen = true;
			await UITestHelper.WaitForIdle();

			var expandedScreenshot = await UITestHelper.ScreenShot(root);
			ImageAssert.HasColorAtChild(expandedScreenshot, targetRect, x, y, Microsoft.UI.Colors.Red);

			split.IsPaneOpen = false;
			await UITestHelper.WaitForIdle();

			var compactAgainScreenshot = await UITestHelper.ScreenShot(root);
			ImageAssert.HasColorAtChild(compactAgainScreenshot, targetRect, x, y, Microsoft.UI.Colors.Blue);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
