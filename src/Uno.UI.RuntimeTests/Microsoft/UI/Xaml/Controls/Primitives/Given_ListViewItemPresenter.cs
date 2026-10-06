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

[TestClass]
[RunsOnUIThread]
public class Given_ListViewItemPresenter
{
	private const double Tolerance = 1e-6;

	[TestInitialize]
	public void Init() => ListViewBaseItemPresenter.ClearIsRoundedListViewBaseItemChromeEnabledCache();

	[TestCleanup]
	public void Cleanup() => ListViewBaseItemPresenter.ClearIsRoundedListViewBaseItemChromeEnabledCache();

	[TestMethod]
	public void When_ListViewItemPresenter_Typed_Getters()
	{
		var presenter = new ListViewItemPresenter
		{
			DisabledOpacity = 0.4,
			DragOpacity = 0.6,
			ReorderHintOffset = 5,
			SelectedBorderThickness = new Thickness(3),
			SelectionCheckMarkVisualEnabled = false,
			RevealBorderThickness = new Thickness(2),
			RevealBackgroundShowsAboveContent = true,
			RevealBackground = new SolidColorBrush(Microsoft.UI.Colors.Red),
			RevealBorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Blue),
			CheckBoxCornerRadius = new CornerRadius(5),
			SelectionIndicatorCornerRadius = new CornerRadius(2),
			SelectionIndicatorMode = ListViewItemPresenterSelectionIndicatorMode.Inline,
		};

		Assert.AreEqual(0.4f, presenter.GetDisabledOpacity(), Tolerance);
		Assert.AreEqual(0.6f, presenter.GetDragOpacity(), Tolerance);
		Assert.AreEqual(5f, presenter.GetReorderHintOffset(), Tolerance);
		Assert.AreEqual(new Thickness(3), presenter.GetSelectedBorderThickness());
		Assert.IsFalse(presenter.GetSelectionCheckMarkVisualEnabled());
		Assert.AreEqual(new Thickness(2), presenter.GetRevealBorderThickness());
		Assert.IsTrue(presenter.GetRevealBackgroundShowsAboveContent());
		Assert.AreSame(presenter.RevealBackground, presenter.GetRevealBackgroundBrushNoRef());
		Assert.AreSame(presenter.RevealBorderBrush, presenter.GetRevealBorderBrushNoRef());
		Assert.AreEqual(new CornerRadius(5), presenter.GetCheckBoxCornerRadius());
		Assert.AreEqual(new CornerRadius(2), presenter.GetSelectionIndicatorCornerRadius());
		Assert.AreEqual(ListViewItemPresenterSelectionIndicatorMode.Inline, presenter.GetSelectionIndicatorMode());
	}

	[TestMethod]
	public void When_GridViewItemPresenter_Typed_Getters()
	{
		var presenter = new GridViewItemPresenter
		{
			DisabledOpacity = 0.25,
			DragOpacity = 0.5,
			ReorderHintOffset = 7,
			SelectedBorderThickness = new Thickness(4),
			SelectionCheckMarkVisualEnabled = false,
		};

		Assert.AreEqual(0.25f, presenter.GetDisabledOpacity(), Tolerance);
		Assert.AreEqual(0.5f, presenter.GetDragOpacity(), Tolerance);
		Assert.AreEqual(7f, presenter.GetReorderHintOffset(), Tolerance);
		Assert.AreEqual(new Thickness(4), presenter.GetSelectedBorderThickness());
		Assert.IsFalse(presenter.GetSelectionCheckMarkVisualEnabled());

		// LVIP-only properties read as their WinUI fallback on a GVIP.
		Assert.IsNull(presenter.GetRevealBackgroundBrushNoRef());
		Assert.IsNull(presenter.GetRevealBorderBrushNoRef());
		Assert.IsFalse(presenter.GetRevealBackgroundShowsAboveContent());
		Assert.AreEqual(default(CornerRadius), presenter.GetCheckBoxCornerRadius());
		Assert.AreEqual(default(CornerRadius), presenter.GetSelectionIndicatorCornerRadius());
	}

	[TestMethod]
	public void When_GridViewItemPresenter_Getters_Ignore_ListViewItemPresenter_Defaults()
	{
		var listViewItemPresenter = new ListViewItemPresenter();
		var gridViewItemPresenter = new GridViewItemPresenter();

		Assert.AreEqual(10f, listViewItemPresenter.GetReorderHintOffset(), Tolerance);
		Assert.AreEqual(16f, gridViewItemPresenter.GetReorderHintOffset(), Tolerance);
	}

	[TestMethod]
	public void When_Rounded_Forced_And_Radii_Zero_Then_Defaults()
	{
		var presenter = new ListViewItemPresenter
		{
			CornerRadius = default,
			CheckBoxCornerRadius = default,
			SelectionIndicatorCornerRadius = default,
		};

		Assert.AreEqual(default(CornerRadius), presenter.GetGeneralCornerRadius());

		using (ListViewBaseItemChromeRuntimeFeatures.Override(forceRounded: true))
		{
			Assert.AreEqual(new CornerRadius(4), presenter.GetGeneralCornerRadius());
			Assert.AreEqual(new CornerRadius(3), presenter.GetCheckBoxCornerRadius());
			Assert.AreEqual(new CornerRadius(1.5), presenter.GetSelectionIndicatorCornerRadius());

			presenter.CornerRadius = new CornerRadius(6);
			Assert.AreEqual(new CornerRadius(6), presenter.GetGeneralCornerRadius());
		}
	}

	[TestMethod]
	public void When_Selection_Indicator_Mode_Forced()
	{
		var presenter = new ListViewItemPresenter { SelectionIndicatorMode = ListViewItemPresenterSelectionIndicatorMode.Overlay };

		using (ListViewBaseItemChromeRuntimeFeatures.Override(forceSelectionIndicatorModeInline: true))
		{
			Assert.AreEqual(ListViewItemPresenterSelectionIndicatorMode.Inline, presenter.GetSelectionIndicatorMode());
		}

		Assert.AreEqual(ListViewItemPresenterSelectionIndicatorMode.Overlay, presenter.GetSelectionIndicatorMode());
	}

	[TestMethod]
	public void When_SetChromedListViewBaseItem_Link_And_Unlink()
	{
		var presenter = new ListViewItemPresenter();

		presenter.SetChromedListViewBaseItem(new ListViewItem());
		Assert.IsTrue(presenter.IsChromeForListViewItem());
		Assert.IsFalse(presenter.IsChromeForGridViewItem());

		// The item type decides, not the presenter type: WinUI's GridViewItem style roots on a ListViewItemPresenter.
		presenter.SetChromedListViewBaseItem(new GridViewItem());
		Assert.IsFalse(presenter.IsChromeForListViewItem());
		Assert.IsTrue(presenter.IsChromeForGridViewItem());

		presenter.SetChromedListViewBaseItem(null);
		Assert.IsFalse(presenter.IsChromeForListViewItem());
		Assert.IsFalse(presenter.IsChromeForGridViewItem());

		Assert.ThrowsExactly<InvalidOperationException>(() => presenter.SetChromedListViewBaseItem(new Border()));
	}

	[TestMethod]
	public void When_Unlinked_ListViewItemPresenter_Measured_Then_Throws()
	{
		var presenter = new ListViewItemPresenter();

		var ex = Assert.ThrowsExactly<InvalidOperationException>(() => presenter.Measure(new Size(100, 100)));

		Assert.AreEqual("ListViewItemPresenter can only be used as the first child in the template for a ListViewItem.", ex.Message);
	}

	[TestMethod]
	public void When_Unlinked_GridViewItemPresenter_Measured_Then_Throws()
	{
		var presenter = new GridViewItemPresenter();

		var ex = Assert.ThrowsExactly<InvalidOperationException>(() => presenter.Measure(new Size(100, 100)));

		Assert.AreEqual("GridViewItemPresenter can only be used as the first child in the template for a GridViewItem.", ex.Message);
	}

	[TestMethod]
	public void When_Linked_Then_Measure_Does_Not_Throw()
	{
		var presenter = new ListViewItemPresenter();
		presenter.SetChromedListViewBaseItem(new ListViewItem());

		presenter.Measure(new Size(100, 100));

		presenter.SetChromedListViewBaseItem(null);
		Assert.ThrowsExactly<InvalidOperationException>(() =>
		{
			presenter.InvalidateMeasure();
			presenter.Measure(new Size(101, 101));
		});
	}

	[TestMethod]
	public async Task When_Template_Child_Added_Then_Inserted_Above_Backplate()
	{
		var presenter = new ListViewItemPresenter();
		presenter.SetChromedListViewBaseItem(new ListViewItem());

		var backplate = new Border();
		SetBackplate(presenter, backplate);
		presenter.AddChild(backplate, 0);

		var first = new Grid { Width = 10, Height = 10 };
		presenter.Content = first;

		WindowHelper.WindowContent = presenter;
		await WindowHelper.WaitForLoaded(presenter);

		var children = presenter.GetChildren();
		Assert.AreEqual(2, children.Count);
		Assert.AreSame(backplate, children[0]);
		Assert.AreSame(first, children[1]);
		Assert.AreSame(first, presenter.GetTemplateChildIfExists());

		var second = new Grid { Width = 20, Height = 20 };
		presenter.Content = second;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(2, children.Count);
		Assert.AreSame(backplate, children[0]);
		Assert.AreSame(second, children[1]);
		Assert.IsNull(first.Parent);
		Assert.AreSame(second, presenter.GetTemplateChildIfExists());
	}

	[TestMethod]
	public async Task When_Only_Chrome_Children_Then_No_Template_Child()
	{
		var presenter = new ListViewItemPresenter();
		presenter.SetChromedListViewBaseItem(new ListViewItem());

		var backplate = new Border();
		SetBackplate(presenter, backplate);
		presenter.AddChild(backplate, 0);

		WindowHelper.WindowContent = presenter;
		await WindowHelper.WaitForLoaded(presenter, p => p.IsLoaded);

		Assert.IsNull(presenter.GetTemplateChildIfExists());
	}

	[TestMethod]
	public async Task When_Template_Root_Then_Item_Links_Chrome()
	{
		var item = new ListViewItem { Content = "Item", Template = CreatePresenterTemplate() };
		var list = new ListView { Items = { item } };

		WindowHelper.WindowContent = list;
		await WindowHelper.WaitForLoaded(item);

		var presenter = (ListViewItemPresenter)VisualTreeHelper.GetChild(item, 0);
		Assert.IsTrue(presenter.IsChromeForListViewItem());
		Assert.AreSame(presenter, item.GetGridViewItemChromeNoRef());

		item.Template = CreatePresenterTemplate();
		await WindowHelper.WaitForIdle();

		var newPresenter = (ListViewItemPresenter)VisualTreeHelper.GetChild(item, 0);
		Assert.AreNotSame(presenter, newPresenter);
		Assert.IsFalse(presenter.IsChromeForListViewItem(), "Old chrome should be unlinked on retemplate");
		Assert.IsTrue(newPresenter.IsChromeForListViewItem());
		Assert.AreSame(newPresenter, item.GetGridViewItemChromeNoRef());
	}

	[TestMethod]
	public async Task When_Selection_Indicator_Forced_Then_Follows_Parent_SelectionMode()
	{
		var item = new ListViewItem { Content = "Item", Template = CreatePresenterTemplate() };
		var list = new ListView { SelectionMode = ListViewSelectionMode.Single, Items = { item } };

		WindowHelper.WindowContent = list;
		await WindowHelper.WaitForLoaded(item);

		var presenter = (ListViewItemPresenter)VisualTreeHelper.GetChild(item, 0);

		using (ListViewBaseItemChromeRuntimeFeatures.Override(forceSelectionIndicatorVisual: true))
		{
			Assert.IsTrue(presenter.IsSelectionIndicatorVisualEnabled());
			Assert.IsTrue(presenter.IsInSelectionIndicatorMode());

			list.SelectionMode = ListViewSelectionMode.Multiple;
			Assert.IsFalse(presenter.IsInSelectionIndicatorMode());

			list.SelectionMode = ListViewSelectionMode.Extended;
			Assert.IsTrue(presenter.IsInSelectionIndicatorMode());

			using (ListViewBaseItemChromeRuntimeFeatures.Override(denySelectionIndicatorVisual: true))
			{
				Assert.IsFalse(presenter.IsSelectionIndicatorVisualEnabled());
			}
		}
	}

	private static ControlTemplate CreatePresenterTemplate()
		=> (ControlTemplate)XamlReader.Load(
			"""
			<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" TargetType="ListViewItem">
				<ListViewItemPresenter Content="{TemplateBinding Content}" />
			</ControlTemplate>
			""");

	// The backplate is created by the rounded chrome; set it directly to pin the template child index rule.
	private static void SetBackplate(ListViewBaseItemPresenter presenter, Border backplate)
		=> typeof(ListViewBaseItemPresenter)
			.GetField("m_backplateRectangle", BindingFlags.NonPublic | BindingFlags.Instance)!
			.SetValue(presenter, backplate);
}
#endif
