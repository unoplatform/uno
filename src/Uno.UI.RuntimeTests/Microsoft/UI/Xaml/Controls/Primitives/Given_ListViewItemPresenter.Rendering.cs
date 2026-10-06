#if HAS_UNO
#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_ListViewItemPresenter
{
	private const byte PixelTolerance = 10;

	[TestMethod]
	public async Task When_NonRounded_Inline_State_Fills_Under_Content()
	{
		using var chromeScope = ListViewChromeHelper.UseNonRoundedChrome();
		var (root, item, presenter) = await CreateRenderingItem();
		item.Background = new SolidColorBrush(Colors.Red);
		presenter.PointerOverBackground = new SolidColorBrush(Colors.Lime);
		presenter.SelectedBackground = new SolidColorBrush(Colors.Blue);
		await WindowHelper.WaitForIdle();

		var center = GetPoint(item, root, 0.5, 0.5);
		await AssertColor(root, center, Colors.Red, "Normal draws the item Background");

		VisualStateManager.GoToState(item, "PointerOver", false);
		await AssertColor(root, center, Colors.Lime, "PointerOver fill");

		VisualStateManager.GoToState(item, "Selected", false);
		await AssertColor(root, center, Colors.Blue, "Selected fill");

		VisualStateManager.GoToState(item, "Normal", false);
		await AssertColor(root, center, Colors.Red, "back to the item Background");
	}

	[TestMethod]
	public async Task When_NonRounded_Inline_State_Fill_Uses_BackgroundTransition()
	{
		using var chromeScope = ListViewChromeHelper.UseNonRoundedChrome();
		var (root, item, presenter) = await CreateRenderingItem();
		presenter.PointerOverBackground = new SolidColorBrush(Colors.Lime);
		presenter.BackgroundTransition = new BrushTransition { Duration = TimeSpan.FromSeconds(3) };
		await WindowHelper.WaitForIdle();

		var center = GetPoint(item, root, 0.5, 0.5);

		VisualStateManager.GoToState(item, "PointerOver", false);
		await WindowHelper.WaitForIdle();
		var during = await UITestHelper.ScreenShot(root);
		ImageAssert.DoesNotHaveColorAt(during, center, Colors.Lime, tolerance: PixelTolerance);

		await Task.Delay(TimeSpan.FromSeconds(3.5));
		await AssertColor(root, center, Colors.Lime, "the transition reaches the PointerOver brush");
	}

	[TestMethod]
	public async Task When_NonRounded_Control_Border_Drawn_Over_Content()
	{
		using var chromeScope = ListViewChromeHelper.UseNonRoundedChrome();
		var (root, item, presenter) = await CreateRenderingItem(contentBackground: "Yellow");
		item.BorderBrush = new SolidColorBrush(Colors.Magenta);
		item.BorderThickness = new Thickness(6);
		await WindowHelper.WaitForIdle();

		// The content fills the item, so the ring is only visible when it is drawn above it.
		await AssertColor(root, GetPoint(item, root, 0, 0.5, dx: 2), Colors.Magenta, "left border ring");
		await AssertColor(root, GetPoint(item, root, 1, 0.5, dx: -2), Colors.Magenta, "right border ring");
		await AssertColor(root, GetPoint(item, root, 0.5, 0, dy: 2), Colors.Magenta, "top border ring");
		await AssertColor(root, GetPoint(item, root, 0.5, 0.5), Colors.Yellow, "the ring is hollow");
	}

	[TestMethod]
	public async Task When_CornerRadius_Then_Post_Layer_Suppressed()
	{
		using var chromeScope = ListViewChromeHelper.UseNonRoundedChrome();
		var (root, item, presenter) = await CreateRenderingItem(contentBackground: "Yellow");
		item.BorderBrush = new SolidColorBrush(Colors.Magenta);
		item.BorderThickness = new Thickness(6);
		presenter.PlaceholderBackground = new SolidColorBrush(Colors.Orange);
		await WindowHelper.WaitForIdle();

		var left = GetPoint(item, root, 0, 0.5, dx: 2);
		await AssertColor(root, left, Colors.Magenta, "square corners draw the post layer");

		presenter.CornerRadius = new CornerRadius(8);
		await WindowHelper.WaitForIdle();

		var screenshot = await UITestHelper.ScreenShot(root);
		ImageAssert.DoesNotHaveColorAt(screenshot, left, Colors.Magenta, tolerance: PixelTolerance);
		Assert.AreEqual(0, GetOverContentShapeCount(presenter));

		VisualStateManager.GoToState(item, "DataPlaceholder", false);
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(0, GetOverContentShapeCount(presenter), "the placeholder belongs to the suppressed post layer too");
	}

	[TestMethod]
	public async Task When_DataPlaceholder_Forced_Then_Placeholder_Fill_Over_Content()
	{
		using var chromeScope = ListViewChromeHelper.UseRoundedChromeResource(true);
		var (root, item, presenter) = await CreateRenderingItem(contentBackground: "Yellow");
		presenter.PlaceholderBackground = new SolidColorBrush(Colors.Orange);
		await WindowHelper.WaitForIdle();

		var center = GetPoint(item, root, 0.5, 0.5);
		await AssertColor(root, center, Colors.Yellow, "DataAvailable shows the content");

		VisualStateManager.GoToState(item, "DataPlaceholder", false);
		await AssertColor(root, center, Colors.Orange, "the placeholder covers the content");

		VisualStateManager.GoToState(item, "DataAvailable", false);
		await AssertColor(root, center, Colors.Yellow, "DataAvailable removes the placeholder");
	}

	[TestMethod]
	public async Task When_GridView_Overlay_State_Ring_Not_Drawn_While_Keyboard_Focused()
	{
		using var chromeScope = ListViewChromeHelper.UseNonRoundedChrome();

		var style = CreateRenderingItemStyle("GridViewItem", "CheckMode=\"Overlay\" SelectedBackground=\"Blue\"", contentBackground: "Yellow");
		var grid = new GridView
		{
			ItemContainerStyle = style,
			SelectionMode = ListViewSelectionMode.Single,
			ItemsSource = new[] { "Item 0" },
		};
		var button = new Button { Content = "Other" };
		var root = new StackPanel { Background = new SolidColorBrush(Colors.White), Children = { grid, button } };

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(grid);
		await WindowHelper.WaitForIdle();

		var item = (GridViewItem)grid.ContainerFromIndex(0);
		item.FocusVisualPrimaryBrush = new SolidColorBrush(Colors.Transparent);
		item.FocusVisualSecondaryBrush = new SolidColorBrush(Colors.Transparent);
		var presenter = (ListViewItemPresenter)VisualTreeHelper.GetChild(item, 0);

		grid.SelectedIndex = 0;
		await WindowHelper.WaitForIdle();

		var ring = GetPoint(item, root, 0, 0.5, dx: 1);
		var inside = GetPoint(item, root, 0, 0.5, dx: 4);
		await AssertColor(root, ring, Colors.Blue, "unfocused selected item draws the 2px state ring");
		await AssertColor(root, inside, Colors.Yellow, "the ring is 2px wide");

		item.Focus(FocusState.Keyboard);
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(ListViewBaseItemPresenter.FocusStates.Focused, ChromeTestHelper.GetField<ListViewBaseItemPresenter.VisualStates>(presenter, "m_visualStates").focusState);

		var focused = await UITestHelper.ScreenShot(root);
		ImageAssert.DoesNotHaveColorAt(focused, ring, Colors.Blue, tolerance: PixelTolerance);
		Assert.AreEqual(0, GetOverContentShapeCount(presenter), "the focus visual draws the ring while keyboard-focused");

		button.Focus(FocusState.Keyboard);
		await WindowHelper.WaitForIdle();

		await AssertColor(root, ring, Colors.Blue, "the chrome draws the ring again once focus leaves");
	}

	[TestMethod]
	public async Task When_No_Background_Then_Presenter_Hit_Testable()
	{
		using var chromeScope = ListViewChromeHelper.UseRoundedChromeResource(true);
		var (root, item, presenter) = await CreateRenderingItem(contentBackground: "");
		item.Background = null;
		await WindowHelper.WaitForIdle();

		var center = GetPoint(item, root, 0.5, 0.5);
		Assert.IsTrue(presenter.HitTest(presenter.TransformToVisual(root).Inverse.TransformPoint(center)));
		Assert.AreSame(presenter, HitTest(root, center));
	}

	[TestMethod]
	public async Task When_Rounded_Corner_Then_Hit_Test_Misses_Corner()
	{
		var (root, item, presenter) = await CreateRenderingItem(contentBackground: "");
		presenter.CornerRadius = new CornerRadius(20);
		await WindowHelper.WaitForIdle();

		Assert.IsFalse(presenter.HitTest(new Point(1, 1)), "inside the top-left rounded corner");
		Assert.IsFalse(presenter.HitTest(new Point(presenter.ActualWidth - 1, presenter.ActualHeight - 1)), "inside the bottom-right rounded corner");
		Assert.IsTrue(presenter.HitTest(new Point(20, 1)), "top edge past the corner");
		Assert.IsTrue(presenter.HitTest(new Point(presenter.ActualWidth / 2, presenter.ActualHeight / 2)));

		Assert.AreNotSame(presenter, HitTest(root, GetPoint(item, root, 0, 0, dx: 1, dy: 1)), "rounded corner");
		Assert.AreSame(presenter, HitTest(root, GetPoint(item, root, 0.5, 0.5)), "center");
	}

	private static UIElement? HitTest(UIElement root, Point point)
		=> VisualTreeHelper.HitTest(root.TransformToVisual(null).TransformPoint(point), root.XamlRoot?.VisualTree.RootElement).element;

	private static Point GetPoint(FrameworkElement element, UIElement root, double relativeX, double relativeY, double dx = 0, double dy = 0)
		=> element.TransformToVisual(root).TransformPoint(new Point(element.ActualWidth * relativeX + dx, element.ActualHeight * relativeY + dy));

	private static async Task AssertColor(FrameworkElement root, Point point, Color expected, string message)
	{
		await WindowHelper.WaitForIdle();
		var screenshot = await UITestHelper.ScreenShot(root);
		try
		{
			ImageAssert.HasColorAt(screenshot, point, expected, tolerance: PixelTolerance);
		}
		catch (Exception e)
		{
			throw new AssertFailedException($"{message}: {e.Message}", e);
		}
	}

	private static int GetOverContentShapeCount(ListViewBaseItemPresenter presenter)
		=> ChromeTestHelper.GetField<ShapeVisual?>(presenter, "m_overContentLayerVisual") is { Parent: not null } layer ? layer.Shapes.Count : 0;

	private static async Task<(StackPanel root, ListViewItem item, ListViewItemPresenter presenter)> CreateRenderingItem(string? contentBackground = null)
	{
		var item = new ListViewItem
		{
			Style = CreateRenderingItemStyle("ListViewItem", "", contentBackground),
			Width = 200,
			Height = 60,
			MinWidth = 0,
			MinHeight = 0,
			Padding = new Thickness(0),
		};
		var root = new StackPanel { Background = new SolidColorBrush(Colors.White), Padding = new Thickness(10), Children = { item } };

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(item);
		await WindowHelper.WaitForIdle();

		return (root, item, (ListViewItemPresenter)VisualTreeHelper.GetChild(item, 0));
	}

	private static Style CreateRenderingItemStyle(string targetType, string presenterAttributes, string? contentBackground)
	{
		var content = contentBackground switch
		{
			null => "",
			"" => "<ListViewItemPresenter.Content><Border /></ListViewItemPresenter.Content>",
			_ => $"<ListViewItemPresenter.Content><Border Background=\"{contentBackground}\" /></ListViewItemPresenter.Content>",
		};

		return (Style)XamlReader.Load($"""
			<Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" TargetType="{targetType}">
				<Setter Property="UseSystemFocusVisuals" Value="True" />
				<Setter Property="Padding" Value="0" />
				<Setter Property="MinWidth" Value="0" />
				<Setter Property="MinHeight" Value="0" />
				<Setter Property="Width" Value="200" />
				<Setter Property="Height" Value="60" />
				<Setter Property="HorizontalContentAlignment" Value="Stretch" />
				<Setter Property="VerticalContentAlignment" Value="Stretch" />
				<Setter Property="Template">
					<Setter.Value>
						<ControlTemplate TargetType="{targetType}">
							<ListViewItemPresenter {presenterAttributes}
								HorizontalContentAlignment="Stretch"
								VerticalContentAlignment="Stretch"
								SelectionCheckMarkVisualEnabled="False"
								CornerRadius="0">{content}</ListViewItemPresenter>
						</ControlTemplate>
					</Setter.Value>
				</Setter>
			</Style>
			""");
	}
}
#endif
