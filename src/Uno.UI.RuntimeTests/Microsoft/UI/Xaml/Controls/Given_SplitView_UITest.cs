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

		using var _ = UITestHelper.ResetWindowContent();
		await UITestHelper.Load(root);

		// Sample 4px inside TargetRect's own right edge (which is inset by CompactPaneLength)
		// to stay clear of anti-aliasing at the content/pane boundary.
		var x = targetRect.ActualWidth - 4;
		var y = targetRect.ActualHeight / 2;

		var compactScreenshot = await UITestHelper.ScreenShot(root);
		ImageAssert.HasColorAtChild(compactScreenshot, targetRect, x, y, Microsoft.UI.Colors.Blue);

		split.IsPaneOpen = true;
		await WaitForPaneToSettle(split);

		var expandedScreenshot = await UITestHelper.ScreenShot(root);
		ImageAssert.HasColorAtChild(expandedScreenshot, targetRect, x, y, Microsoft.UI.Colors.Red);

		split.IsPaneOpen = false;
		await WaitForPaneToSettle(split);

		var compactAgainScreenshot = await UITestHelper.ScreenShot(root);
		ImageAssert.HasColorAtChild(compactAgainScreenshot, targetRect, x, y, Microsoft.UI.Colors.Blue);
	}

	// WinUI's compact overlay transitions slide the pane and its clip over 0.35s; an idle UI thread doesn't mean they're done.
	private static async Task WaitForPaneToSettle(SplitView split)
	{
		var paneRoot = (UIElement)FindDescendant(split, "PaneRoot");
		var paneTransform = (CompositeTransform)paneRoot.RenderTransform;
		var paneClipTransform = (CompositeTransform)((RectangleGeometry)paneRoot.Clip).Transform;

		var expectedClipTranslateX = split.IsPaneOpen ? 0 : split.TemplateSettings.OpenPaneLengthMinusCompactLength;

		await UITestHelper.WaitFor(() => paneTransform.TranslateX == 0 && paneClipTransform.TranslateX == expectedClipTranslateX);
		await UITestHelper.WaitForIdle();
	}

	private static DependencyObject FindDescendant(DependencyObject parent, string name)
	{
		for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
		{
			var child = VisualTreeHelper.GetChild(parent, i);
			if (child is FrameworkElement { Name: var childName } && childName == name)
			{
				return child;
			}

			if (FindDescendant(child, name) is { } match)
			{
				return match;
			}
		}

		return null;
	}
}
