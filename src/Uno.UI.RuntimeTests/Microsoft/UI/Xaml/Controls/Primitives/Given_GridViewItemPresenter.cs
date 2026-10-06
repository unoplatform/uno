#if HAS_UNO
#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_GridViewItemPresenter
{
	[TestInitialize]
	public void Init() => ListViewBaseItemPresenter.ClearIsRoundedListViewBaseItemChromeEnabledCache();

	[TestCleanup]
	public void Cleanup() => ListViewBaseItemPresenter.ClearIsRoundedListViewBaseItemChromeEnabledCache();

	[TestMethod]
	public async Task When_Default_GridViewItem_PointerOver_Shows_Outer_Border()
	{
		using var chromeScope = ListViewChromeHelper.UseRoundedChromeResource(true);
		var (_, item, presenter) = await CreateGridView(WinUIItemStyles.GridViewItemStyle);

		Assert.IsInstanceOfType(presenter, typeof(ListViewItemPresenter), "WinUI's DefaultGridViewItemStyle roots on ListViewItemPresenter");
		Assert.IsNull(ChromeTestHelper.GetField<Border?>(presenter, "m_outerBorder"));

		VisualStateManager.GoToState(item, "PointerOver", false);

		var outer = ChromeTestHelper.GetField<Border>(presenter, "m_outerBorder");
		Assert.AreSame(((ListViewItemPresenter)presenter).PointerOverBorderBrush, outer.BorderBrush);
		Assert.IsNull(ChromeTestHelper.GetField<Border?>(presenter, "m_innerSelectionBorder"));

		VisualStateManager.GoToState(item, "Normal", false);

		Assert.IsNull(ChromeTestHelper.GetField<Border?>(presenter, "m_outerBorder"));
		Assert.IsNull(outer.GetParent());
	}

	[TestMethod]
	public async Task When_Default_GridViewItem_Selected_Shows_Outer_And_Inner_Borders()
	{
		using var chromeScope = ListViewChromeHelper.UseRoundedChromeResource(true);
		var (grid, _, presenter) = await CreateGridView(WinUIItemStyles.GridViewItemStyle);
		var lvip = (ListViewItemPresenter)presenter;

		grid.SelectedIndex = 0;
		await WindowHelper.WaitForIdle();

		var outer = ChromeTestHelper.GetField<Border>(presenter, "m_outerBorder");
		var inner = ChromeTestHelper.GetField<Border>(presenter, "m_innerSelectionBorder");
		Assert.AreSame(lvip.SelectedBorderBrush, outer.BorderBrush);
		Assert.AreSame(lvip.SelectedInnerBorderBrush, inner.BorderBrush);
		Assert.AreSame(lvip.SelectedBackground, ChromeTestHelper.GetField<Border>(presenter, "m_backplateRectangle").Background);

		grid.SelectedIndex = -1;
		await WindowHelper.WaitForIdle();

		Assert.IsNull(ChromeTestHelper.GetField<Border?>(presenter, "m_innerSelectionBorder"));
		Assert.IsNull(ChromeTestHelper.GetField<Border?>(presenter, "m_outerBorder"));
	}

	[TestMethod]
	public async Task When_Disabled_While_Selected_Keeps_Selected_Border()
	{
		using var chromeScope = ListViewChromeHelper.UseRoundedChromeResource(true);
		var (grid, item, presenter) = await CreateGridView(WinUIItemStyles.GridViewItemStyle);
		var lvip = (ListViewItemPresenter)presenter;

		grid.SelectedIndex = 0;
		await WindowHelper.WaitForIdle();

		item.IsEnabled = false;
		await WindowHelper.WaitForIdle();

		Assert.AreSame(lvip.SelectedDisabledBorderBrush, ChromeTestHelper.GetField<Border>(presenter, "m_outerBorder").BorderBrush);
		Assert.AreEqual(0.3, presenter.GetTemplateChildIfExists()!.Opacity, 1e-6);
		Assert.AreSame(lvip.SelectedDisabledBackground, ChromeTestHelper.GetField<Border>(presenter, "m_backplateRectangle").Background);
	}

	[TestMethod]
	public async Task When_Default_GridViewItem_MultiSelect_Overlay_CheckBox()
	{
		using var chromeScope = ListViewChromeHelper.UseRoundedChromeResource(true);
		var (grid, _, presenter) = await CreateGridView(WinUIItemStyles.GridViewItemStyle);
		Assert.AreEqual(ListViewItemPresenterCheckMode.Overlay, ((ListViewItemPresenter)presenter).CheckMode);

		var content = presenter.GetTemplateChildIfExists()!;
		var contentLeftBefore = content.TransformToVisual(presenter).TransformPoint(default).X;

		grid.SelectionMode = ListViewSelectionMode.Multiple;
		await ChromeTestHelper.WaitForNoRunningAnimation(presenter);
		await WindowHelper.WaitForIdle();

		var checkBox = ChromeTestHelper.GetField<Border>(presenter, "m_multiSelectCheckBoxRectangle");
		Assert.AreSame(presenter, checkBox.GetParent());
		var checkBoxOrigin = checkBox.TransformToVisual(presenter).TransformPoint(default);
		// Rounded Overlay margin: inner selection border (1) + SelectedBorderThickness + 1 on the top and right.
		var selectedBorderThickness = presenter.GetSelectedBorderThickness();
		Assert.AreEqual(presenter.ActualWidth - checkBox.ActualWidth - (2 + selectedBorderThickness.Right), checkBoxOrigin.X, 0.5, "Overlay check box is top-right");
		Assert.AreEqual(2 + selectedBorderThickness.Top, checkBoxOrigin.Y, 0.5, "Overlay check box is top-right");
		Assert.AreEqual(contentLeftBefore, content.TransformToVisual(presenter).TransformPoint(default).X, 0.5, "Overlay does not offset content");

		grid.SelectionMode = ListViewSelectionMode.None;
		await ChromeTestHelper.WaitForNoRunningAnimation(presenter);

		Assert.IsNull(ChromeTestHelper.GetField<Border?>(presenter, "m_multiSelectCheckBoxRectangle"));
	}

	[TestMethod]
	public async Task When_GridViewItemPresenter_Root_Drives_Chrome()
	{
		using var chromeScope = ListViewChromeHelper.UseRoundedChromeResource(true);
		var style = new Style(typeof(GridViewItem));
		style.Setters.Add(new Setter(Control.TemplateProperty, new ControlTemplate(null, (_, _) => new GridViewItemPresenter
		{
			Width = 100,
			Height = 100,
			SelectedBackground = new SolidColorBrush(Microsoft.UI.Colors.Red),
			PointerOverBackground = new SolidColorBrush(Microsoft.UI.Colors.Green),
		})));

		var (grid, item, presenter) = await CreateGridView(style);
		var gvip = (GridViewItemPresenter)presenter;

		Assert.AreSame(item, ChromeTestHelper.GetParentItem(presenter));
		Assert.AreSame(presenter, item.GetGridViewItemChromeNoRef());

		var backplate = ChromeTestHelper.GetField<Border>(presenter, "m_backplateRectangle");

		VisualStateManager.GoToState(item, "PointerOver", false);
		Assert.AreSame(gvip.PointerOverBackground, backplate.Background);

		grid.SelectedIndex = 0;
		await WindowHelper.WaitForIdle();
		Assert.AreSame(gvip.SelectedBackground, backplate.Background);
		Assert.AreEqual(ListViewBaseItemPresenter.CommonStates2.Selected, ChromeTestHelper.GetField<ListViewBaseItemPresenter.VisualStates>(presenter, "m_visualStates").commonState2);
	}

	private static async Task<(GridView grid, GridViewItem item, ListViewBaseItemPresenter presenter)> CreateGridView(Style itemContainerStyle)
	{
		var grid = new GridView
		{
			Width = 400,
			Height = 300,
			SelectionMode = ListViewSelectionMode.Single,
			ItemContainerStyle = itemContainerStyle,
			ItemsSource = new[] { "Item 0", "Item 1", "Item 2" },
		};
		grid.Resources.MergedDictionaries.Add(WinUIItemStyles.Resources);

		WindowHelper.WindowContent = grid;
		await WindowHelper.WaitForLoaded(grid);
		await WindowHelper.WaitForIdle();

		var item = (GridViewItem)grid.ContainerFromIndex(0);
		return (grid, item, (ListViewBaseItemPresenter)VisualTreeHelper.GetChild(item, 0));
	}
}
#endif
