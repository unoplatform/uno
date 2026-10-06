#if HAS_UNO
#nullable enable

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_ListViewItemPresenter
{
	private const double LayoutTolerance = 1e-3;

	[TestMethod]
	[RequiresScaling(1f)]
	public async Task When_MultiSelect_Inline_Rounded_Then_Content_Prefix_28()
	{
		using var _ = ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true);

		var (presenter, content) = await CreateLayoutItem(multiSelect: true);

		Assert.AreEqual(128, presenter.DesiredSize.Width, LayoutTolerance);
		Assert.AreEqual(28, GetOffset(content, presenter).X, LayoutTolerance);
	}

	[TestMethod]
	[RequiresScaling(1f)]
	public async Task When_MultiSelect_Inline_Non_Rounded_Then_Content_Prefix_32()
	{
		using var _ = ListViewChromeHelper.UseNonRoundedChrome();

		var (presenter, content) = await CreateLayoutItem(multiSelect: true);

		Assert.AreEqual(132, presenter.DesiredSize.Width, LayoutTolerance);
		Assert.AreEqual(32, GetOffset(content, presenter).X, LayoutTolerance);
	}

	[TestMethod]
	[RequiresScaling(1f)]
	public async Task When_MultiSelect_Overlay_Or_Disabled_Then_No_Content_Prefix()
	{
		using var _ = ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true);

		var (presenter, content) = await CreateLayoutItem(multiSelect: true, configure: p => p.CheckMode = ListViewItemPresenterCheckMode.Overlay);

		Assert.AreEqual(100, presenter.DesiredSize.Width, LayoutTolerance);
		Assert.AreEqual(0, GetOffset(content, presenter).X, LayoutTolerance);

		presenter.CheckMode = ListViewItemPresenterCheckMode.Inline;
		presenter.SelectionCheckMarkVisualEnabled = false;
		await Relayout(presenter);

		Assert.AreEqual(100, presenter.DesiredSize.Width, LayoutTolerance);
		Assert.AreEqual(0, GetOffset(content, presenter).X, LayoutTolerance);
	}

	[TestMethod]
	[RequiresScaling(1f)]
	public async Task When_Selection_Indicator_Inline_Then_Content_Prefix_7()
	{
		using var _ = ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true, forceSelectionIndicatorVisual: true, forceSelectionIndicatorModeInline: true);

		var (presenter, content) = await CreateLayoutItem(inListView: true);

		Assert.AreEqual(107, presenter.DesiredSize.Width, LayoutTolerance);
		Assert.AreEqual(7, GetOffset(content, presenter).X, LayoutTolerance);

		// The larger multi-select prefix wins.
		SetMultiSelectState(presenter, ListViewBaseItemPresenter.MultiSelectStates.MultiSelectEnabled);
		await Relayout(presenter);

		Assert.AreEqual(128, presenter.DesiredSize.Width, LayoutTolerance);
		Assert.AreEqual(28, GetOffset(content, presenter).X, LayoutTolerance);
	}

	[TestMethod]
	[RequiresScaling(1f)]
	public async Task When_Selection_Indicator_Overlay_Then_No_Content_Prefix()
	{
		using var _ = ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true, forceSelectionIndicatorVisual: true, forceSelectionIndicatorModeOverlay: true);

		var (presenter, content) = await CreateLayoutItem(inListView: true);

		Assert.AreEqual(100, presenter.DesiredSize.Width, LayoutTolerance);
		Assert.AreEqual(0, GetOffset(content, presenter).X, LayoutTolerance);
	}

	[TestMethod]
	[RequiresScaling(1f)]
	[DataRow(40.0, 16.0)] // max(16, 40 - 20 - 20)
	[DataRow(100.0, 60.0)] // 100 - 20 - 20
	[DataRow(10.0, 10.0)] // available height <= 16
	public async Task When_Selection_Indicator_Height_Rule(double itemHeight, double expectedHeight)
	{
		using var _ = ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true, forceSelectionIndicatorVisual: true, forceSelectionIndicatorModeInline: true);

		var (presenter, _) = await CreateLayoutItem(inListView: true, contentHeight: itemHeight);
		presenter.EnsureSelectionIndicator();
		await Relayout(presenter);

		var indicator = GetChromeField<Border>(presenter, "m_selectionIndicatorRectangle");

		Assert.AreEqual(expectedHeight, indicator.ActualHeight, LayoutTolerance);
		Assert.AreEqual((itemHeight - expectedHeight) / 2, GetOffset(indicator, presenter).Y, LayoutTolerance);
	}

	[TestMethod]
	[RequiresScaling(1f)]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Selection_Indicator_Pressed_Then_Shrinks_By_6(bool selected)
	{
		using var _ = ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true, forceSelectionIndicatorVisual: true, forceSelectionIndicatorModeInline: true);

		var (presenter, _) = await CreateLayoutItem(inListView: true, contentHeight: 100);
		presenter.EnsureSelectionIndicator();
		SetCommonState(presenter, selected ? ListViewBaseItemPresenter.CommonStates2.PressedSelected : ListViewBaseItemPresenter.CommonStates2.Pressed);
		await Relayout(presenter);

		var indicator = GetChromeField<Border>(presenter, "m_selectionIndicatorRectangle");

		Assert.AreEqual(54, indicator.ActualHeight, LayoutTolerance);
		Assert.AreEqual(23, GetOffset(indicator, presenter).Y, LayoutTolerance);
	}

	[TestMethod]
	[RequiresScaling(1f)]
	public async Task When_Item_MinWidth_MinHeight_Then_Minimum_Desired_Size()
	{
		using var _ = ListViewChromeHelper.UseNonRoundedChrome();

		var (presenter, _) = await CreateLayoutItem(configureItem: item =>
		{
			item.MinWidth = 150;
			item.MinHeight = 70;
		});

		Assert.AreEqual(150, presenter.DesiredSize.Width, LayoutTolerance);
		Assert.AreEqual(70, presenter.DesiredSize.Height, LayoutTolerance);
	}

	[TestMethod]
	[RequiresScaling(1f)]
	public async Task When_Item_BorderThickness_And_ContentMargin_Then_Content_Inset()
	{
		using var _ = ListViewChromeHelper.UseNonRoundedChrome();

		var (presenter, content) = await CreateLayoutItem(
			configure: p => p.ContentMargin = new Thickness(1, 2, 3, 4),
			configureItem: item => item.BorderThickness = new Thickness(5, 6, 7, 8));

		Assert.AreEqual(100 + 1 + 3 + 5 + 7, presenter.DesiredSize.Width, LayoutTolerance);
		Assert.AreEqual(40 + 2 + 4 + 6 + 8, presenter.DesiredSize.Height, LayoutTolerance);
		Assert.AreEqual(new Point(1 + 5, 2 + 6), GetOffset(content, presenter));
	}

	[TestMethod]
	[RequiresScaling(1f)]
	public void When_Arranged_Smaller_Than_MultiSelect_Square_Then_Grows()
	{
		using var _ = ListViewChromeHelper.UseNonRoundedChrome();

		var presenter = new ListViewItemPresenter();
		presenter.SetChromedListViewBaseItem(new ListViewItem());
		SetMultiSelectState(presenter, ListViewBaseItemPresenter.MultiSelectStates.MultiSelectEnabled);

		presenter.Measure(new Size(10, 10));
		presenter.Arrange(new Rect(0, 0, 10, 10));

		Assert.AreEqual(new Size(20, 20), presenter.RenderSize);
	}

	[TestMethod]
	[RequiresScaling(1f)]
	public async Task When_Arranged_Smaller_Than_Selection_Indicator_Then_Grows()
	{
		using var _ = ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true, forceSelectionIndicatorVisual: true, forceSelectionIndicatorModeInline: true);

		var (presenter, _) = await CreateLayoutItem(inListView: true);

		presenter.Measure(new Size(2, 2));
		presenter.Arrange(new Rect(0, 0, 2, 2));

		// Width: 4 + 3 + 0, height: shrinkage 6 + 1.
		Assert.AreEqual(new Size(7, 7), presenter.RenderSize);
	}

	[TestMethod]
	[RequiresScaling(1f)]
	public async Task When_Layout_Rounding_At_100_Percent_Then_Chrome_Does_Not_Round()
	{
		using var _ = ListViewChromeHelper.UseNonRoundedChrome();

		var (presenter, _) = await CreateLayoutItem(configureItem: item => item.BorderThickness = new Thickness(1.1));

		// 100 + 2.2, rounded once as a whole by the framework.
		Assert.AreEqual(102, presenter.DesiredSize.Width, LayoutTolerance);
	}

	[TestMethod]
	[RequiresScaling(1.5f)]
	public async Task When_Layout_Rounding_At_150_Percent_Then_Chrome_Rounds_Each_Thickness()
	{
		using var _ = ListViewChromeHelper.UseNonRoundedChrome();

		var (presenter, _) = await CreateLayoutItem(configureItem: item => item.BorderThickness = new Thickness(1.1));

		// Each 1.1 side rounds to 2px / 1.5 = 1.333; rounding 102.2 as a whole would give 102.
		Assert.AreEqual(100 + 2 * (2 / 1.5), presenter.DesiredSize.Width, LayoutTolerance);
	}

	[TestMethod]
	public void When_Rounded_First_Measure_Then_Backplate_Created_At_Index_0()
	{
		using var _ = ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true);

		var presenter = new ListViewItemPresenter();
		presenter.SetChromedListViewBaseItem(new ListViewItem());

		Assert.IsNull(GetChromeField<Border?>(presenter, "m_backplateRectangle"));

		presenter.Measure(new Size(100, 100));

		var backplate = GetChromeField<Border?>(presenter, "m_backplateRectangle");
		Assert.IsNotNull(backplate);
		Assert.AreSame(backplate, presenter.GetChildren()[0]);
	}

	[TestMethod]
	public void When_Non_Rounded_Measure_Then_No_Backplate()
	{
		using var _ = ListViewChromeHelper.UseNonRoundedChrome();

		var presenter = new ListViewItemPresenter();
		presenter.SetChromedListViewBaseItem(new ListViewItem());

		presenter.Measure(new Size(100, 100));

		Assert.IsNull(GetChromeField<Border?>(presenter, "m_backplateRectangle"));
	}

	[TestMethod]
	[RequiresScaling(1f)]
	public async Task When_Secondary_Chrome_Then_Arranged_At_Primary_Bounds()
	{
		using var _ = ListViewChromeHelper.UseNonRoundedChrome();

		var (presenter, _) = await CreateLayoutItem(configureItem: item => item.MinWidth = 150);
		presenter.AddSecondaryChrome();
		await Relayout(presenter);

		var secondary = GetChromeField<FrameworkElement>(presenter, "m_pSecondaryChrome");

		Assert.AreEqual(new Rect(0, 0, presenter.ActualWidth, presenter.ActualHeight), LayoutInformation.GetLayoutSlot(secondary));
		Assert.AreEqual(presenter.ActualWidth, secondary.ActualWidth, LayoutTolerance);
		Assert.AreEqual(presenter.ActualHeight, secondary.ActualHeight, LayoutTolerance);
	}

	private static async Task<(ListViewItemPresenter presenter, FrameworkElement content)> CreateLayoutItem(
		bool multiSelect = false,
		bool inListView = false,
		double contentHeight = 40,
		Action<ListViewItemPresenter>? configure = null,
		Action<ListViewItem>? configureItem = null)
	{
		var content = new Border { Width = 100, Height = contentHeight };
		var item = new ListViewItem
		{
			Content = content,
			Template = CreateLayoutPresenterTemplate(),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			BorderThickness = default,
			MinWidth = 0,
			MinHeight = 0,
		};
		configureItem?.Invoke(item);

		FrameworkElement root = inListView
			? new ListView { SelectionMode = ListViewSelectionMode.Single, Items = { item } }
			: new StackPanel { HorizontalAlignment = HorizontalAlignment.Left, Children = { item } };

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(item);

		var presenter = (ListViewItemPresenter)VisualTreeHelper.GetChild(item, 0);
		configure?.Invoke(presenter);

		if (multiSelect)
		{
			SetMultiSelectState(presenter, ListViewBaseItemPresenter.MultiSelectStates.MultiSelectEnabled);
		}

		await Relayout(presenter);

		return (presenter, content);
	}

	private static async Task Relayout(ListViewItemPresenter presenter)
	{
		presenter.InvalidateMeasure();
		presenter.InvalidateArrange();
		await WindowHelper.WaitForIdle();
	}

	private static Point GetOffset(UIElement element, UIElement relativeTo)
		=> element.TransformToVisual(relativeTo).TransformPoint(default);

	private static void SetMultiSelectState(ListViewBaseItemPresenter presenter, ListViewBaseItemPresenter.MultiSelectStates state)
		=> UpdateVisualStates(presenter, (ref ListViewBaseItemPresenter.VisualStates states) => states.multiSelectState = state);

	private static ControlTemplate CreateLayoutPresenterTemplate()
		=> (ControlTemplate)XamlReader.Load(
			"""
			<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" TargetType="ListViewItem">
				<ListViewItemPresenter Content="{TemplateBinding Content}" HorizontalContentAlignment="Left" VerticalContentAlignment="Top" />
			</ControlTemplate>
			""");
}
#endif
