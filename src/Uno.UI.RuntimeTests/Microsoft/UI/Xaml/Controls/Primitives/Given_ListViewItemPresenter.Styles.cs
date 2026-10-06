#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI;
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

#if !HAS_UNO
[TestClass]
[RunsOnUIThread]
#endif
public partial class Given_ListViewItemPresenter
{
	private const byte LookTolerance = 3;

	[TestMethod]
	public async Task When_Default_ListViewItem_Template_Root_Is_Rounded_Presenter()
	{
		var list = new ListView { Items = { "Item 0" } };
		var standalone = new ListViewItem { Content = "Standalone" };

		WindowHelper.WindowContent = new StackPanel { Children = { list, standalone } };
		await WindowHelper.WaitForLoaded(standalone);
		await WindowHelper.WaitForIdle();

		foreach (var item in new[] { (ListViewItem)list.ContainerFromIndex(0), standalone })
		{
			var root = VisualTreeHelper.GetChild(item, 0);
			Assert.IsInstanceOfType(root, typeof(ListViewItemPresenter));
			Assert.AreEqual(new CornerRadius(4), ((ListViewItemPresenter)root).CornerRadius);
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/17231")]
	[RequiresFullWindow]
	public async Task When_Default_ListView_Selected_Matches_WinUI_Look()
	{
		var list = new ListView { Width = 200, Items = { "Item 0", "Item 1", "Item 2" } };
		var root = new Grid
		{
			RequestedTheme = ElementTheme.Light,
			Background = new SolidColorBrush(Colors.White),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Children = { list },
		};

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(list);
		await WindowHelper.WaitForIdle();

		list.SelectedIndex = 1;
		await WaitForChromeAnimations();

		var selected = (ListViewItem)list.ContainerFromIndex(1);
		var unselected = (ListViewItem)list.ContainerFromIndex(0);
		var presenter = (ListViewItemPresenter)VisualTreeHelper.GetChild(selected, 0);
		var backplate = OverWhite(((SolidColorBrush)presenter.SelectedBackground!).Color);
		var indicator = ((SolidColorBrush)presenter.SelectionIndicatorBrush!).Color;

		var bitmap = await UITestHelper.ScreenShot(root);
		var width = selected.ActualWidth;
		var midY = selected.ActualHeight / 2;

		// Backplate: SubtleFill, inset {4,2,4,2}, CornerRadius 4.
		AssertLook(bitmap, root, selected, width / 2, midY, backplate, "backplate center");
		AssertLook(bitmap, root, selected, width / 2, 1, Colors.White, "top inset");
		AssertLook(bitmap, root, selected, width / 2, 3.5, backplate, "below the top inset");
		AssertLook(bitmap, root, selected, width / 2, selected.ActualHeight - 1, Colors.White, "bottom inset");
		AssertLook(bitmap, root, selected, 2, midY, Colors.White, "left inset");
		AssertLook(bitmap, root, selected, width - 2, midY, Colors.White, "right inset");
		AssertLook(bitmap, root, selected, width - 6, midY, backplate, "right of the backplate");
		AssertLook(bitmap, root, selected, 4.5, 2.5, Colors.White, "rounded top-left corner");

		// Selection indicator: 3x16 pill at x=4, vertically centered.
		AssertLook(bitmap, root, selected, 5.5, midY, indicator, "pill center");
		AssertLook(bitmap, root, selected, 5.5, midY - 5, indicator, "pill upper part");
		AssertLook(bitmap, root, selected, 5.5, midY + 5, indicator, "pill lower part");
		AssertLook(bitmap, root, selected, 5.5, midY - 11, backplate, "above the pill");
		AssertLook(bitmap, root, selected, 9.5, midY, backplate, "right of the pill");

		AssertLook(bitmap, root, unselected, width / 2, unselected.ActualHeight / 2, Colors.White, "unselected item has no backplate");
		AssertLook(bitmap, root, unselected, 5.5, unselected.ActualHeight / 2, Colors.White, "unselected item has no pill");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19707")]
	[RequiresFullWindow]
	public async Task When_DefaultListViewItemStyle_BasedOn_Override_Selection_Works()
	{
		var template = (ControlTemplate)XamlReader.Load(
			"""
			<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				TargetType="ListViewItem">
				<ListViewItemPresenter x:Name="Root"
					ContentMargin="{TemplateBinding Padding}"
					HorizontalContentAlignment="{TemplateBinding HorizontalContentAlignment}"
					VerticalContentAlignment="{TemplateBinding VerticalContentAlignment}"
					CornerRadius="2"
					SelectedBackground="Red"
					SelectedPointerOverBackground="Red"
					PointerOverBackground="Transparent"
					SelectionIndicatorBrush="Blue"
					SelectionIndicatorPointerOverBrush="Blue" />
			</ControlTemplate>
			""");

		var style = new Style(typeof(ListViewItem))
		{
			BasedOn = (Style)Application.Current.Resources["DefaultListViewItemStyle"],
			Setters =
			{
				new Setter(Control.PaddingProperty, new Thickness(24, 0, 0, 0)),
				new Setter(Control.TemplateProperty, template),
			},
		};

		var list = new ListView { Width = 200, ItemContainerStyle = style, Items = { "Item 0", "Item 1" } };
		var root = new Grid
		{
			Background = new SolidColorBrush(Colors.White),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Children = { list },
		};

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(list);
		await WindowHelper.WaitForIdle();

		var first = (ListViewItem)list.ContainerFromIndex(0);
		var second = (ListViewItem)list.ContainerFromIndex(1);
		var presenter = (ListViewItemPresenter)VisualTreeHelper.GetChild(first, 0);
		Assert.AreEqual(new Thickness(24, 0, 0, 0), presenter.ContentMargin, "TemplateBinding to the overridden Padding");
		Assert.AreEqual(new CornerRadius(2), presenter.CornerRadius);

		list.SelectedIndex = 0;
		await WaitForChromeAnimations();

		var bitmap = await UITestHelper.ScreenShot(root);
		AssertLook(bitmap, root, first, first.ActualWidth - 10, first.ActualHeight / 2, Colors.Red, "selected backplate");
		AssertLook(bitmap, root, first, 5.5, first.ActualHeight / 2, Colors.Blue, "selection indicator");
		AssertLook(bitmap, root, second, second.ActualWidth - 10, second.ActualHeight / 2, Colors.White, "unselected item");

		list.SelectedIndex = 1;
		await WaitForChromeAnimations();

		bitmap = await UITestHelper.ScreenShot(root);
		AssertLook(bitmap, root, first, first.ActualWidth - 10, first.ActualHeight / 2, Colors.White, "deselected item");
		AssertLook(bitmap, root, second, second.ActualWidth - 10, second.ActualHeight / 2, Colors.Red, "newly selected backplate");
		AssertLook(bitmap, root, second, 5.5, second.ActualHeight / 2, Colors.Blue, "newly selected indicator");
		Assert.IsTrue(second.IsSelected);
		Assert.IsFalse(first.IsSelected);
	}

	private static async Task WaitForChromeAnimations()
	{
		await WindowHelper.WaitForIdle();
		// Indicator select and deselect animations take up to 600 ms.
		await Task.Delay(800);
		await WindowHelper.WaitForIdle();
	}

	private static Color OverWhite(Color color)
	{
		byte Blend(byte channel) => (byte)Math.Round(channel * color.A / 255.0 + 255 * (1 - color.A / 255.0));
		return Color.FromArgb(255, Blend(color.R), Blend(color.G), Blend(color.B));
	}

	private static void AssertLook(RawBitmap bitmap, UIElement root, FrameworkElement element, double x, double y, Color expected, string message)
	{
		var point = element.TransformToVisual(root).TransformPoint(new Point(x, y));
		try
		{
			ImageAssert.HasColorAt(bitmap, point, expected, tolerance: LookTolerance);
		}
		catch (Exception e)
		{
			throw new AssertFailedException($"{message}: {e.Message}", e);
		}
	}

#if HAS_UNO
	[TestMethod]
	public async Task When_DefaultListViewItemStyle_Renders_Rounded_Presenter()
	{
		ListViewBaseItemPresenter.ClearIsRoundedListViewBaseItemChromeEnabledCache();
		try
		{
			var style = (Style)Application.Current.Resources["DefaultListViewItemStyle"];
			var item = new ListViewItem { Content = "Item", Style = style };

			WindowHelper.WindowContent = item;
			await WindowHelper.WaitForLoaded(item);
			await WindowHelper.WaitForIdle();

			var presenter = (ListViewItemPresenter)VisualTreeHelper.GetChild(item, 0);
			Assert.AreEqual(new CornerRadius(4), presenter.CornerRadius);
			Assert.IsNotNull(ChromeTestHelper.GetField<Border>(presenter, "m_backplateRectangle"), "Rounded chrome creates the backplate");
		}
		finally
		{
			ListViewBaseItemPresenter.ClearIsRoundedListViewBaseItemChromeEnabledCache();
		}
	}

	[TestMethod]
	public async Task When_DefaultGridViewItemStyle_Roots_On_ListViewItemPresenter()
	{
		var style = (Style)Application.Current.Resources["DefaultGridViewItemStyle"];
		var item = new GridViewItem { Content = "Item", Style = style };

		WindowHelper.WindowContent = item;
		await WindowHelper.WaitForLoaded(item);
		await WindowHelper.WaitForIdle();

		Assert.IsInstanceOfType(VisualTreeHelper.GetChild(item, 0), typeof(ListViewItemPresenter));
	}

	[TestMethod]
	public void When_Rounded_Chrome_Resource_And_New_Keys_Present()
	{
		var resources = Application.Current.Resources;

		Assert.AreEqual(true, resources["ListViewBaseItemRoundedChromeEnabled"]);
		Assert.AreEqual(new CornerRadius(4), resources["ListViewItemCornerRadius"]);
		Assert.AreEqual(new CornerRadius(1.5), resources["ListViewItemSelectionIndicatorCornerRadius"]);
		Assert.IsNotNull(resources["ListViewItemSelectionIndicatorBrush"]);
		Assert.IsNotNull(resources["ListViewItemCheckBoxSelectedBrush"]);
		Assert.IsNotNull(resources["ListViewItemBackgroundSelectedDisabled"]);
		Assert.IsInstanceOfType(resources["ListViewItemExpanded"], typeof(Style));
		Assert.IsInstanceOfType(resources["ListPickerFlyoutPresenterItemStyle"], typeof(Style));
	}
#endif
}
