#if HAS_UNO
#nullable enable

using System;
using System.Linq;
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
public partial class Given_ListViewItemPresenter
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

		var outerBorder = new Border();
		SetOuterBorder(presenter, outerBorder);
		presenter.AddChild(outerBorder);

		var first = new Grid { Width = 10, Height = 10 };
		presenter.Content = first;

		WindowHelper.WindowContent = presenter;
		await WindowHelper.WaitForLoaded(presenter, p => p.IsLoaded);

		AssertChildren(presenter, backplate, first, outerBorder);
		Assert.AreSame(first, presenter.GetTemplateChildIfExists());

		var second = new Grid { Width = 20, Height = 20 };
		presenter.Content = second;
		await WindowHelper.WaitForIdle();

		AssertChildren(presenter, backplate, second, outerBorder);
		Assert.IsNull(first.Parent);
		Assert.AreSame(second, presenter.GetTemplateChildIfExists());
	}

	[TestMethod]
	public async Task When_Template_Child_Added_Without_Backplate_Then_Inserted_First()
	{
		using var chromeScope = ListViewChromeHelper.UseNonRoundedChrome();
		var presenter = new ListViewItemPresenter();
		presenter.SetChromedListViewBaseItem(new ListViewItem());

		var outerBorder = new Border();
		SetOuterBorder(presenter, outerBorder);
		presenter.AddChild(outerBorder);

		var first = new Grid { Width = 10, Height = 10 };
		presenter.Content = first;

		WindowHelper.WindowContent = presenter;
		await WindowHelper.WaitForLoaded(presenter, p => p.IsLoaded);

		AssertChildren(presenter, first, outerBorder);
		Assert.AreSame(first, presenter.GetTemplateChildIfExists());

		var second = new Grid { Width = 20, Height = 20 };
		presenter.Content = second;
		await WindowHelper.WaitForIdle();

		AssertChildren(presenter, second, outerBorder);
		Assert.IsNull(first.Parent);
		Assert.AreSame(second, presenter.GetTemplateChildIfExists());
	}

	[TestMethod]
	public void When_RadialGradientBrush_Then_Not_Null_Composition_Brush()
	{
		var radialGradientBrush = new RadialGradientBrush
		{
			GradientStops = { new GradientStop { Color = Microsoft.UI.Colors.Red, Offset = 0 } },
		};

		Assert.IsFalse(ListViewBaseItemPresenter.IsNullCompositionBrush(radialGradientBrush));
		Assert.IsTrue(ListViewBaseItemPresenter.IsNullCompositionBrush(new TestCompositionBrush()));
		Assert.IsFalse(ListViewBaseItemPresenter.IsNullCompositionBrush(new SolidColorBrush(Microsoft.UI.Colors.Red)));
		Assert.IsFalse(ListViewBaseItemPresenter.IsNullCompositionBrush(null));
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

	[TestMethod]
	public void When_Rounded_ListView_Backplate()
	{
		using var _ = ListViewChromeHelper.UseRoundedChromeResource(true);

		var itemBackground = new SolidColorBrush(Microsoft.UI.Colors.Red);
		var selectedBackground = new SolidColorBrush(Microsoft.UI.Colors.Green);
		var presenter = new ListViewItemPresenter { CornerRadius = new CornerRadius(4), SelectedBackground = selectedBackground };
		presenter.SetChromedListViewBaseItem(new ListViewItem { Background = itemBackground });

		presenter.EnsureBackplate();

		var backplate = GetChromeField<Border>(presenter, "m_backplateRectangle");
		Assert.AreSame(backplate, presenter.GetChildren()[0]);
		Assert.AreEqual(new Thickness(4, 2, 4, 2), backplate.Margin);
		Assert.AreEqual(new CornerRadius(4), backplate.CornerRadius);
		Assert.AreEqual(default(Thickness), backplate.BorderThickness);
		Assert.IsNull(backplate.BorderBrush);
		Assert.IsFalse(backplate.IsHitTestVisible);
		Assert.AreEqual(HorizontalAlignment.Stretch, backplate.HorizontalAlignment);
		Assert.AreEqual(VerticalAlignment.Stretch, backplate.VerticalAlignment);
		Assert.AreSame(itemBackground, backplate.Background, "Normal backplate uses the item's Background");

		SetCommonState(presenter, ListViewBaseItemPresenter.CommonStates2.Selected);
		presenter.SetBackplateBackground();
		Assert.AreSame(selectedBackground, backplate.Background);

		var newSelectedBackground = new SolidColorBrush(Microsoft.UI.Colors.Blue);
		presenter.SelectedBackground = newSelectedBackground;
		Assert.AreSame(newSelectedBackground, backplate.Background, "SelectedBackground change refreshes the backplate");

		SetDisabledState(presenter, ListViewBaseItemPresenter.DisabledStates.Disabled);
		SetCommonState(presenter, ListViewBaseItemPresenter.CommonStates2.Normal);
		presenter.SetBackplateBackground();
		Assert.IsNull(backplate.Background, "Disabled and unselected has no backplate brush");
	}

	[TestMethod]
	public void When_Rounded_GridView_Opaque_Outer_Border_Nests_Inner_Border()
	{
		using var _ = ListViewChromeHelper.UseRoundedChromeResource(true);

		var opaqueBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
		var innerBrush = new SolidColorBrush(Microsoft.UI.Colors.White);
		var presenter = new ListViewItemPresenter
		{
			CornerRadius = new CornerRadius(4),
			SelectedBorderBrush = opaqueBrush,
			SelectedInnerBorderBrush = innerBrush,
			SelectedBorderThickness = new Thickness(2),
		};
		presenter.SetChromedListViewBaseItem(new GridViewItem());
		SetCommonState(presenter, ListViewBaseItemPresenter.CommonStates2.Selected);

		presenter.EnsureBackplate();
		var backplate = GetChromeField<Border>(presenter, "m_backplateRectangle");
		Assert.AreEqual(default(Thickness), backplate.Margin, "No outer border yet");

		presenter.EnsureOuterBorder();
		presenter.EnsureInnerSelectionBorder();

		var outer = GetChromeField<Border>(presenter, "m_outerBorder");
		var inner = GetChromeField<Border>(presenter, "m_innerSelectionBorder");

		Assert.AreSame(opaqueBrush, outer.BorderBrush);
		Assert.AreEqual(new Thickness(2), outer.BorderThickness);
		Assert.AreEqual(new CornerRadius(4), outer.CornerRadius);
		Assert.IsNull(outer.Background);
		Assert.IsFalse(outer.IsHitTestVisible);
		Assert.AreEqual(new Thickness(1), backplate.Margin, "Opaque outer border gives the backplate a 1px margin");

		Assert.AreSame(inner, outer.Child);
		Assert.AreSame(innerBrush, inner.BorderBrush);
		Assert.AreEqual(new Thickness(2), inner.BorderThickness);
		Assert.AreEqual(new Thickness(-1), inner.Margin);
		Assert.AreEqual(new CornerRadius(3), inner.CornerRadius, "max(3, 4 - 2)");
		Assert.IsFalse(inner.IsHitTestVisible);
		CollectionAssert.AreEqual(new UIElement[] { backplate, outer }, presenter.GetChildren().ToArray());

		// A semi-transparent outer brush moves the inner border onto the presenter.
		presenter.SelectedBorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(0x80, 0xFF, 0, 0));

		Assert.IsNull(outer.Child);
		CollectionAssert.AreEqual(new UIElement[] { backplate, outer, inner }, presenter.GetChildren().ToArray());
		Assert.AreEqual(new Thickness(3), inner.BorderThickness, "1 + SelectedBorderThickness");
		Assert.AreEqual(new CornerRadius(4), inner.CornerRadius);
		Assert.AreEqual(default(Thickness), backplate.Margin);

		presenter.SelectedBorderBrush = opaqueBrush;
		Assert.AreSame(inner, outer.Child);
		CollectionAssert.AreEqual(new UIElement[] { backplate, outer }, presenter.GetChildren().ToArray());
	}

	[TestMethod]
	public void When_Rounded_GridView_PointerOver_Outer_Border()
	{
		using var _ = ListViewChromeHelper.UseRoundedChromeResource(true);

		var pointerOverBorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray);
		var presenter = new ListViewItemPresenter { PointerOverBorderBrush = pointerOverBorderBrush };
		presenter.SetChromedListViewBaseItem(new GridViewItem());
		SetCommonState(presenter, ListViewBaseItemPresenter.CommonStates2.PointerOver);

		presenter.EnsureOuterBorder();

		var outer = GetChromeField<Border>(presenter, "m_outerBorder");
		Assert.AreSame(pointerOverBorderBrush, outer.BorderBrush);
		Assert.AreEqual(new Thickness(1), outer.BorderThickness, "Unselected outer border thickness");

		presenter.EnsureBackplate();
		var backplate = GetChromeField<Border>(presenter, "m_backplateRectangle");
		Assert.AreEqual(new Thickness(1), backplate.Margin);

		presenter.RemoveOuterBorder();
		Assert.IsNull(GetChromeField<Border?>(presenter, "m_outerBorder"));
		Assert.IsNull(outer.Parent);
		Assert.AreEqual(default(Thickness), backplate.Margin, "Removing the outer border resets the backplate margin");
	}

	[TestMethod]
	public void When_Rounded_ListView_CheckBox()
	{
		using var _ = ListViewChromeHelper.UseRoundedChromeResource(true);

		var checkBoxBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
		var checkBoxBorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Green);
		var checkBoxSelectedBrush = new SolidColorBrush(Microsoft.UI.Colors.Blue);
		var checkBrush = new SolidColorBrush(Microsoft.UI.Colors.White);
		var presenter = new ListViewItemPresenter
		{
			CheckBoxCornerRadius = new CornerRadius(3),
			CheckBoxBrush = checkBoxBrush,
			CheckBoxBorderBrush = checkBoxBorderBrush,
			CheckBoxSelectedBrush = checkBoxSelectedBrush,
			CheckBrush = checkBrush,
		};
		presenter.SetChromedListViewBaseItem(new ListViewItem());

		presenter.EnsureMultiSelectCheckBox();

		var checkBox = GetChromeField<Border>(presenter, "m_multiSelectCheckBoxRectangle");
		var glyph = GetChromeField<FontIcon>(presenter, "m_multiSelectCheckGlyph");

		Assert.IsTrue(presenter.GetChildren().Contains(checkBox));
		Assert.IsFalse(checkBox.IsHitTestVisible);
		Assert.AreEqual(20, checkBox.MinWidth);
		Assert.AreEqual(20, checkBox.Height);
		Assert.IsNotNull(checkBox.TransitionTarget);
		Assert.AreEqual(new Thickness(14, 0, 0, 0), checkBox.Margin);
		Assert.AreEqual(HorizontalAlignment.Left, checkBox.HorizontalAlignment);
		Assert.AreEqual(VerticalAlignment.Center, checkBox.VerticalAlignment);
		Assert.AreEqual(new Rect(0, 0, 20, 20), ((RectangleGeometry)checkBox.Clip).Rect);
		Assert.AreEqual(new CornerRadius(3), checkBox.CornerRadius);
		Assert.AreSame(checkBoxBrush, checkBox.Background);
		Assert.AreEqual(new Thickness(1), checkBox.BorderThickness);
		Assert.AreSame(checkBoxBorderBrush, checkBox.BorderBrush);

		Assert.AreSame(glyph, checkBox.Child);
		Assert.AreEqual("", glyph.Glyph);
		Assert.AreEqual(16, glyph.FontSize);
		Assert.AreEqual(0, glyph.Opacity, "Unselected glyph starts hidden");
		Assert.IsFalse(glyph.IsHitTestVisible);
		Assert.AreSame(checkBrush, glyph.Foreground);

		SetCommonState(presenter, ListViewBaseItemPresenter.CommonStates2.Selected);
		presenter.SetMultiSelectCheckBoxProperties();
		Assert.AreSame(checkBoxSelectedBrush, checkBox.Background);
		Assert.AreEqual(default(Thickness), checkBox.BorderThickness);
		Assert.IsNull(checkBox.BorderBrush);

		var newSelectedBrush = new SolidColorBrush(Microsoft.UI.Colors.Yellow);
		presenter.CheckBoxSelectedBrush = newSelectedBrush;
		Assert.AreSame(newSelectedBrush, checkBox.Background);

		presenter.RemoveMultiSelectCheckBox();
		Assert.IsFalse(presenter.GetChildren().Contains(checkBox));
		Assert.IsNull(GetChromeField<Border?>(presenter, "m_multiSelectCheckBoxRectangle"));
		Assert.IsNull(GetChromeField<FontIcon?>(presenter, "m_multiSelectCheckGlyph"));
	}

	[TestMethod]
	public void When_NonRounded_ListView_CheckBox()
	{
		using var _ = ListViewChromeHelper.UseNonRoundedChrome();

		var checkBoxBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
		var checkBrush = new SolidColorBrush(Microsoft.UI.Colors.White);
		var presenter = new ListViewItemPresenter { CheckBoxBrush = checkBoxBrush, CheckBrush = checkBrush };
		presenter.SetChromedListViewBaseItem(new ListViewItem());

		presenter.EnsureMultiSelectCheckBox();

		var checkBox = GetChromeField<Border>(presenter, "m_multiSelectCheckBoxRectangle");
		var glyph = GetChromeField<FontIcon>(presenter, "m_multiSelectCheckGlyph");

		Assert.AreEqual(new Thickness(12, 0, 0, 0), checkBox.Margin);
		Assert.AreEqual(default(CornerRadius), checkBox.CornerRadius);
		Assert.AreEqual(new Thickness(2), checkBox.BorderThickness);
		Assert.AreSame(checkBoxBrush, checkBox.BorderBrush);
		Assert.IsNull(checkBox.Background);
		Assert.AreSame(checkBrush, glyph.Foreground);
	}

	[TestMethod]
	public void When_Overlay_CheckBox()
	{
		var selectedBackground = new SolidColorBrush(Microsoft.UI.Colors.Blue);
		var checkBoxBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);

		using (ListViewChromeHelper.UseNonRoundedChrome())
		{
			var presenter = new ListViewItemPresenter { CheckMode = ListViewItemPresenterCheckMode.Overlay, SelectedBackground = selectedBackground, CheckBoxBrush = checkBoxBrush };
			presenter.SetChromedListViewBaseItem(new GridViewItem());

			presenter.EnsureMultiSelectCheckBox();

			var checkBox = GetChromeField<Border>(presenter, "m_multiSelectCheckBoxRectangle");
			Assert.AreEqual(new Thickness(0, 2, 2, 0), checkBox.Margin);
			Assert.AreEqual(HorizontalAlignment.Right, checkBox.HorizontalAlignment);
			Assert.AreEqual(VerticalAlignment.Top, checkBox.VerticalAlignment);
			Assert.IsNull(checkBox.Clip);
			Assert.AreEqual(default(Thickness), checkBox.BorderThickness);
			Assert.AreSame(checkBoxBrush, checkBox.Background);

			SetCommonState(presenter, ListViewBaseItemPresenter.CommonStates2.Selected);
			presenter.SetMultiSelectCheckBoxBackground();
			Assert.AreSame(selectedBackground, checkBox.Background);
		}

		using (ListViewChromeHelper.UseRoundedChromeResource(true))
		{
			var presenter = new ListViewItemPresenter { CheckMode = ListViewItemPresenterCheckMode.Overlay, SelectedBorderThickness = new Thickness(2) };
			presenter.SetChromedListViewBaseItem(new GridViewItem());

			presenter.EnsureMultiSelectCheckBox();

			var checkBox = GetChromeField<Border>(presenter, "m_multiSelectCheckBoxRectangle");
			Assert.AreEqual(new Thickness(0, 4, 4, 0), checkBox.Margin, "inner selection border 1 + selected border 2 + 1");
		}
	}

	[TestMethod]
	public async Task When_Rounded_ListView_Selection_Indicator()
	{
		using var _ = ListViewChromeHelper.UseRoundedChromeResource(true);

		var indicatorBrush = new SolidColorBrush(Microsoft.UI.Colors.Blue);
		var item = new ListViewItem { Content = "Item", Template = CreatePresenterTemplate() };
		var list = new ListView { SelectionMode = ListViewSelectionMode.Single, Items = { item } };

		WindowHelper.WindowContent = list;
		await WindowHelper.WaitForLoaded(item);

		var presenter = (ListViewItemPresenter)VisualTreeHelper.GetChild(item, 0);
		presenter.SelectionIndicatorBrush = indicatorBrush;
		presenter.SelectionIndicatorCornerRadius = new CornerRadius(1.5);
		Assert.IsTrue(presenter.IsInSelectionIndicatorMode());
		SetCommonState(presenter, ListViewBaseItemPresenter.CommonStates2.Selected);

		presenter.EnsureSelectionIndicator();

		var indicator = GetChromeField<Border>(presenter, "m_selectionIndicatorRectangle");
		Assert.IsTrue(presenter.GetChildren().Contains(indicator));
		Assert.IsFalse(indicator.IsHitTestVisible);
		Assert.IsNotNull(indicator.TransitionTarget);
		// Size stores floats, so the rounded width is compared with a float tolerance.
		Assert.AreEqual(presenter.LayoutRound(3), indicator.Width, 1e-5);
		Assert.AreEqual(presenter.LayoutRound(4), indicator.Margin.Left, 1e-5);
		Assert.AreEqual(0, indicator.Margin.Top);
		Assert.AreEqual(0, indicator.Margin.Right);
		Assert.AreEqual(0, indicator.Margin.Bottom);
		Assert.AreEqual(HorizontalAlignment.Left, indicator.HorizontalAlignment);
		Assert.AreEqual(VerticalAlignment.Stretch, indicator.VerticalAlignment);
		Assert.AreEqual(default(Thickness), indicator.BorderThickness);
		Assert.AreSame(indicatorBrush, indicator.Background);
		Assert.AreEqual(new CornerRadius(1.5), indicator.CornerRadius);

		var newBrush = new SolidColorBrush(Microsoft.UI.Colors.Green);
		presenter.SelectionIndicatorBrush = newBrush;
		Assert.AreSame(newBrush, indicator.Background);

		presenter.SelectionIndicatorCornerRadius = new CornerRadius(2);
		Assert.AreEqual(new CornerRadius(2), indicator.CornerRadius);

		presenter.RemoveSelectionIndicator();
		Assert.IsFalse(presenter.GetChildren().Contains(indicator));
	}

	[TestMethod]
	public async Task When_Backplate_Ensured_Then_Below_Template_Child()
	{
		using var _ = ListViewChromeHelper.UseRoundedChromeResource(true);

		var presenter = new ListViewItemPresenter();
		presenter.SetChromedListViewBaseItem(new ListViewItem());

		var content = new Grid { Width = 10, Height = 10 };
		presenter.Content = content;

		WindowHelper.WindowContent = presenter;
		await WindowHelper.WaitForLoaded(presenter, p => p.IsLoaded);

		// The rounded chrome creates the backplate on its first measure.
		var backplate = GetChromeField<Border>(presenter, "m_backplateRectangle");
		AssertChildren(presenter, backplate, content);

		presenter.EnsureBackplate();
		Assert.AreSame(backplate, GetChromeField<Border>(presenter, "m_backplateRectangle"));
		AssertChildren(presenter, backplate, content);
		Assert.AreSame(content, presenter.GetTemplateChildIfExists());

		presenter.EnsureMultiSelectCheckBox();
		presenter.AddSecondaryChrome();
		var checkBox = GetChromeField<Border>(presenter, "m_multiSelectCheckBoxRectangle");
		var secondaryChrome = GetChromeField<ListViewBaseItemSecondaryChrome>(presenter, "m_pSecondaryChrome");
		AssertChildren(presenter, backplate, content, checkBox, secondaryChrome);
		Assert.AreSame(presenter, secondaryChrome.m_pPrimaryChromeNoRef);

		var newContent = new Grid { Width = 20, Height = 20 };
		presenter.Content = newContent;
		await WindowHelper.WaitForIdle();

		AssertChildren(presenter, backplate, newContent, checkBox, secondaryChrome);
		Assert.AreSame(newContent, presenter.GetTemplateChildIfExists());
	}

	[TestMethod]
	public void When_SelectedForeground_Then_Animated_Value_Over_Local()
	{
		var localForeground = new SolidColorBrush(Microsoft.UI.Colors.Blue);
		var selectedForeground = new SolidColorBrush(Microsoft.UI.Colors.White);
		var presenter = new ListViewItemPresenter { Foreground = localForeground };
		presenter.SetChromedListViewBaseItem(new ListViewItem());

		SetCommonState(presenter, ListViewBaseItemPresenter.CommonStates2.Selected);
		presenter.SelectedForeground = selectedForeground;

		Assert.AreSame(selectedForeground, presenter.Foreground, "SelectedForeground change applies while selected");
		Assert.AreEqual(DependencyPropertyValuePrecedences.Animations, presenter.GetCurrentHighestValuePrecedence(ContentPresenter.ForegroundProperty));

		SetCommonState(presenter, ListViewBaseItemPresenter.CommonStates2.Normal);
		presenter.SetForegroundBrush();

		Assert.AreSame(localForeground, presenter.Foreground);
		Assert.AreEqual(DependencyPropertyValuePrecedences.Local, presenter.GetCurrentHighestValuePrecedence(ContentPresenter.ForegroundProperty));
	}

	[TestMethod]
	public void When_PointerOverForeground_Explicit_Null_Then_Applied()
	{
		var localForeground = new SolidColorBrush(Microsoft.UI.Colors.Blue);
		var presenter = new ListViewItemPresenter { Foreground = localForeground };
		presenter.SetChromedListViewBaseItem(new ListViewItem());
		SetCommonState(presenter, ListViewBaseItemPresenter.CommonStates2.PointerOver);

		presenter.SetForegroundBrush();
		Assert.AreSame(localForeground, presenter.Foreground, "Default PointerOverForeground leaves Foreground alone");

		// Same-value set raises no change callback; the explicit value still counts.
		presenter.PointerOverForeground = null;
		presenter.SetForegroundBrush();
		Assert.IsNull(presenter.Foreground, "An explicit null PointerOverForeground is applied");
		Assert.AreEqual(DependencyPropertyValuePrecedences.Animations, presenter.GetCurrentHighestValuePrecedence(ContentPresenter.ForegroundProperty));
	}

	[TestMethod]
	public void When_NonRounded_Inline_SelectedForeground_Then_CheckBox_Follows()
	{
		using var _ = ListViewChromeHelper.UseNonRoundedChrome();

		var checkBoxBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
		var checkBrush = new SolidColorBrush(Microsoft.UI.Colors.Green);
		var selectedForeground = new SolidColorBrush(Microsoft.UI.Colors.White);
		var presenter = new ListViewItemPresenter { CheckBoxBrush = checkBoxBrush, CheckBrush = checkBrush, SelectedForeground = selectedForeground };
		presenter.SetChromedListViewBaseItem(new ListViewItem());
		SetCommonState(presenter, ListViewBaseItemPresenter.CommonStates2.Selected);

		presenter.EnsureMultiSelectCheckBox();

		var checkBox = GetChromeField<Border>(presenter, "m_multiSelectCheckBoxRectangle");
		var glyph = GetChromeField<FontIcon>(presenter, "m_multiSelectCheckGlyph");
		Assert.AreSame(selectedForeground, presenter.Foreground);
		Assert.AreSame(selectedForeground, checkBox.BorderBrush);
		Assert.AreSame(selectedForeground, glyph.Foreground);

		SetCommonState(presenter, ListViewBaseItemPresenter.CommonStates2.Normal);
		presenter.SetForegroundBrush();
		Assert.AreSame(checkBoxBrush, checkBox.BorderBrush);
		Assert.AreSame(checkBrush, glyph.Foreground);
	}

	[TestMethod]
	public void When_CornerRadius_Changed_Then_Chrome_Radii_Updated()
	{
		using var _ = ListViewChromeHelper.UseRoundedChromeResource(true);

		var presenter = new ListViewItemPresenter
		{
			CornerRadius = new CornerRadius(4),
			SelectedBorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Red),
			SelectedBorderThickness = new Thickness(2),
		};
		presenter.SetChromedListViewBaseItem(new GridViewItem());
		SetCommonState(presenter, ListViewBaseItemPresenter.CommonStates2.Selected);

		presenter.EnsureBackplate();
		presenter.EnsureOuterBorder();
		presenter.EnsureInnerSelectionBorder();

		var backplate = GetChromeField<Border>(presenter, "m_backplateRectangle");
		var outer = GetChromeField<Border>(presenter, "m_outerBorder");
		var inner = GetChromeField<Border>(presenter, "m_innerSelectionBorder");

		presenter.CornerRadius = new CornerRadius(8);

		Assert.AreEqual(new CornerRadius(8), backplate.CornerRadius);
		Assert.AreEqual(new CornerRadius(8), outer.CornerRadius);
		Assert.AreEqual(new CornerRadius(6), inner.CornerRadius, "Nested inner radius shrinks by the selected border thickness");
	}

	[TestMethod]
	public void When_NonRounded_CornerRadius_Changed_Then_Backplate_Untouched()
	{
		Border backplate;
		ListViewItemPresenter presenter;

		using (ListViewChromeHelper.UseRoundedChromeResource(true))
		{
			presenter = new ListViewItemPresenter { CornerRadius = new CornerRadius(4) };
			presenter.SetChromedListViewBaseItem(new ListViewItem());
			presenter.EnsureBackplate();
			backplate = GetChromeField<Border>(presenter, "m_backplateRectangle");
		}

		using (ListViewChromeHelper.UseNonRoundedChrome())
		{
			presenter.CornerRadius = new CornerRadius(8);
			Assert.AreEqual(new CornerRadius(4), backplate.CornerRadius);
		}
	}

	[TestMethod]
	public void When_Drag_Overlay_Visible_Then_CheckBox_Hosts_Count()
	{
		var presenter = new ListViewItemPresenter { CheckMode = ListViewItemPresenterCheckMode.Overlay };
		presenter.SetChromedListViewBaseItem(new GridViewItem());

		presenter.SetDragItemsCount(3);
		var textBlock = GetChromeField<TextBlock>(presenter, "m_pDragItemsCountTextBlock");
		Assert.AreEqual("3", textBlock.Text);
		Assert.AreEqual(Visibility.Collapsed, textBlock.Visibility);
		Assert.IsFalse(textBlock.IsHitTestVisible);
		Assert.AreEqual(Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw, Microsoft.UI.Xaml.Automation.AutomationProperties.GetAccessibilityView(textBlock));
		Assert.AreEqual(HorizontalAlignment.Center, textBlock.HorizontalAlignment);
		Assert.AreEqual(VerticalAlignment.Center, textBlock.VerticalAlignment);

		presenter.SetDragOverlayTextBlockVisible(true);

		var checkBox = GetChromeField<Border>(presenter, "m_multiSelectCheckBoxRectangle");
		Assert.AreSame(textBlock, checkBox.Child);
		Assert.IsNull(checkBox.Clip);
		Assert.AreEqual(Visibility.Visible, textBlock.Visibility);
		Assert.AreEqual(Visibility.Visible, checkBox.Visibility);
		Assert.AreEqual(new Thickness(2), checkBox.BorderThickness);
		Assert.AreEqual(HorizontalAlignment.Center, checkBox.HorizontalAlignment);
		Assert.AreEqual(VerticalAlignment.Center, checkBox.VerticalAlignment);

		presenter.SetDragOverlayTextBlockVisible(false);

		// WinUI quirk: the check box keeps the text block as its child.
		Assert.AreEqual(Visibility.Collapsed, textBlock.Visibility);
		Assert.AreSame(textBlock, checkBox.Child);
	}

	[TestMethod]
	public void When_Quirk_Property_Changes_Then_Chrome_Not_Refreshed()
	{
		using var _ = ListViewChromeHelper.UseRoundedChromeResource(true);

		var listViewItemPresenter = new ListViewItemPresenter { CheckBoxPointerOverBorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Red) };
		listViewItemPresenter.SetChromedListViewBaseItem(new ListViewItem());
		SetCommonState(listViewItemPresenter, ListViewBaseItemPresenter.CommonStates2.PointerOver);
		listViewItemPresenter.EnsureMultiSelectCheckBox();
		var checkBox = GetChromeField<Border>(listViewItemPresenter, "m_multiSelectCheckBoxRectangle");
		var original = checkBox.BorderBrush;

		listViewItemPresenter.CheckBoxPointerOverBorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Green);
		Assert.AreSame(original, checkBox.BorderBrush, "WinUI quirk: CheckBoxPointerOverBorderBrush has no case");

		var gridViewItemPresenter = new GridViewItemPresenter { CheckBrush = new SolidColorBrush(Microsoft.UI.Colors.Red) };
		gridViewItemPresenter.SetChromedListViewBaseItem(new GridViewItem());
		gridViewItemPresenter.EnsureMultiSelectCheckBox();
		var glyph = GetChromeField<FontIcon>(gridViewItemPresenter, "m_multiSelectCheckGlyph");
		var originalGlyphBrush = glyph.Foreground;

		gridViewItemPresenter.CheckBrush = new SolidColorBrush(Microsoft.UI.Colors.Green);
		Assert.AreSame(originalGlyphBrush, glyph.Foreground, "WinUI quirk: GVIP CheckBrush has no case");
	}

	private static void SetCommonState(ListViewBaseItemPresenter presenter, ListViewBaseItemPresenter.CommonStates2 state)
		=> UpdateVisualStates(presenter, (ref ListViewBaseItemPresenter.VisualStates states) => states.commonState2 = state);

	private static void SetDisabledState(ListViewBaseItemPresenter presenter, ListViewBaseItemPresenter.DisabledStates state)
		=> UpdateVisualStates(presenter, (ref ListViewBaseItemPresenter.VisualStates states) => states.disabledState = state);

	private delegate void VisualStatesUpdater(ref ListViewBaseItemPresenter.VisualStates states);

	// Drive the chrome state directly, without GoToChromedState side effects.
	private static void UpdateVisualStates(ListViewBaseItemPresenter presenter, VisualStatesUpdater update)
	{
		var field = typeof(ListViewBaseItemPresenter).GetField("m_visualStates", BindingFlags.NonPublic | BindingFlags.Instance)!;
		var states = (ListViewBaseItemPresenter.VisualStates)field.GetValue(presenter)!;
		update(ref states);
		field.SetValue(presenter, states);
	}

	private static T GetChromeField<T>(ListViewBaseItemPresenter presenter, string name)
		=> (T)typeof(ListViewBaseItemPresenter)
			.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!
			.GetValue(presenter)!;

	private static ControlTemplate CreatePresenterTemplate()
		=> (ControlTemplate)XamlReader.Load(
			"""
			<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" TargetType="ListViewItem">
				<ListViewItemPresenter Content="{TemplateBinding Content}" />
			</ControlTemplate>
			""");

	// The backplate and outer border are created by the chrome rendering; set them directly to pin the template child index rule.
	private static void SetBackplate(ListViewBaseItemPresenter presenter, Border backplate)
		=> SetChromeField(presenter, "m_backplateRectangle", backplate);

	private static void SetOuterBorder(ListViewBaseItemPresenter presenter, Border outerBorder)
		=> SetChromeField(presenter, "m_outerBorder", outerBorder);

	private static void SetChromeField(ListViewBaseItemPresenter presenter, string name, Border value)
		=> typeof(ListViewBaseItemPresenter)
			.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!
			.SetValue(presenter, value);

	private static void AssertChildren(ListViewBaseItemPresenter presenter, params UIElement[] expected)
	{
		CollectionAssert.AreEqual(expected, presenter.GetChildren().ToArray());
		CollectionAssert.AreEqual(expected.Select(e => e.Visual).ToArray(), presenter.Visual.Children.ToArray());
	}

	// Unconnected app-defined XamlCompositionBrushBase: no CompositionBrush yet.
	private sealed class TestCompositionBrush : XamlCompositionBrushBase
	{
	}
}
#endif
