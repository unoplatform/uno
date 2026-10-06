#if HAS_UNO
#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;
using static Uno.UI.Extensions.ViewExtensions;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_ListViewItemPresenter
{
	[TestMethod]
	public async Task When_CommonStates2_Drive_Backplate()
	{
		using var chromeScope = ListViewChromeHelper.UseRoundedChromeResource(true);
		var (list, items) = await CreateWinUIListView(ListViewSelectionMode.Single);
		var (item0, presenter0) = items[0];
		var (item1, presenter1) = items[1];

		var backplate0 = ChromeTestHelper.GetField<Border>(presenter0, "m_backplateRectangle");
		Assert.AreSame(item0.Background, backplate0.Background, "Normal uses the item Background");

		list.SelectedIndex = 0;
		await WindowHelper.WaitForIdle();
		Assert.AreSame(presenter0.SelectedBackground, backplate0.Background);

		VisualStateManager.GoToState(item0, "PointerOverSelected", false);
		Assert.AreSame(presenter0.SelectedPointerOverBackground, backplate0.Background);

		VisualStateManager.GoToState(item0, "PressedSelected", false);
		Assert.AreSame(presenter0.SelectedPressedBackground, backplate0.Background);

		var backplate1 = ChromeTestHelper.GetField<Border>(presenter1, "m_backplateRectangle");
		VisualStateManager.GoToState(item1, "PointerOver", false);
		Assert.AreSame(presenter1.PointerOverBackground, backplate1.Background);

		VisualStateManager.GoToState(item1, "Pressed", false);
		Assert.AreSame(presenter1.PressedBackground, backplate1.Background);

		VisualStateManager.GoToState(item1, "Normal", false);
		Assert.AreSame(item1.Background, backplate1.Background);
	}

	[TestMethod]
	public async Task When_Selected_In_Single_Mode_Indicator_Shown_And_Hidden()
	{
		using var chromeScope = ListViewChromeHelper.UseRoundedChromeResource(true);
		var (list, items) = await CreateWinUIListView(ListViewSelectionMode.Single);
		var (_, presenter) = items[0];

		Assert.IsNull(ChromeTestHelper.GetField<Border?>(presenter, "m_selectionIndicatorRectangle"));

		list.SelectedIndex = 0;
		await WindowHelper.WaitForIdle();

		var indicator = ChromeTestHelper.GetField<Border>(presenter, "m_selectionIndicatorRectangle");
		Assert.AreSame(presenter, indicator.GetParent());
		Assert.AreSame(presenter.SelectionIndicatorBrush, indicator.Background);
		await ChromeTestHelper.WaitForNoRunningAnimation(presenter);
		Assert.AreEqual(1.0, indicator.TransitionTarget!.Opacity, 1e-5);

		list.SelectedIndex = -1;
		await ChromeTestHelper.WaitForNoRunningAnimation(presenter);

		Assert.IsNull(ChromeTestHelper.GetField<Border?>(presenter, "m_selectionIndicatorRectangle"));
		Assert.IsNull(indicator.GetParent());
	}

	[TestMethod]
	public async Task When_MultiSelect_Inline_CheckBox_And_Content_Offset()
	{
		using var chromeScope = ListViewChromeHelper.UseRoundedChromeResource(true);
		var (list, items) = await CreateWinUIListView(ListViewSelectionMode.None);
		var (_, presenter) = items[0];
		var content = presenter.GetTemplateChildIfExists()!;
		var contentLeftBefore = content.TransformToVisual(presenter).TransformPoint(default).X;

		list.SelectionMode = ListViewSelectionMode.Multiple;
		await ChromeTestHelper.WaitForNoRunningAnimation(presenter);
		await WindowHelper.WaitForIdle();

		var checkBox = ChromeTestHelper.GetField<Border>(presenter, "m_multiSelectCheckBoxRectangle");
		Assert.AreSame(presenter, checkBox.GetParent());
		Assert.AreEqual(14, checkBox.TransformToVisual(presenter).TransformPoint(default).X, 0.5, "Inline check box margin");
		Assert.AreEqual(contentLeftBefore + 28, content.TransformToVisual(presenter).TransformPoint(default).X, 0.5, "Inline content offset");

		list.SelectionMode = ListViewSelectionMode.None;
		await ChromeTestHelper.WaitForNoRunningAnimation(presenter);

		Assert.IsNull(ChromeTestHelper.GetField<Border?>(presenter, "m_multiSelectCheckBoxRectangle"));
		Assert.IsNull(checkBox.GetParent());
	}

	[TestMethod]
	public async Task When_MultiSelect_Overlay_CheckBox_Top_Right()
	{
		using var chromeScope = ListViewChromeHelper.UseRoundedChromeResource(true);
		var (list, items) = await CreateWinUIListView(ListViewSelectionMode.None);
		var (_, presenter) = items[0];
		presenter.CheckMode = ListViewItemPresenterCheckMode.Overlay;
		var content = presenter.GetTemplateChildIfExists()!;
		var contentLeftBefore = content.TransformToVisual(presenter).TransformPoint(default).X;

		list.SelectionMode = ListViewSelectionMode.Multiple;
		await ChromeTestHelper.WaitForNoRunningAnimation(presenter);
		await WindowHelper.WaitForIdle();

		var checkBox = ChromeTestHelper.GetField<Border>(presenter, "m_multiSelectCheckBoxRectangle");
		var checkBoxOrigin = checkBox.TransformToVisual(presenter).TransformPoint(default);
		// Rounded Overlay margin: inner selection border (1) + SelectedBorderThickness + 1 on the top and right.
		var selectedBorderThickness = presenter.GetSelectedBorderThickness();
		Assert.AreEqual(presenter.ActualWidth - checkBox.ActualWidth - (2 + selectedBorderThickness.Right), checkBoxOrigin.X, 0.5, "Overlay check box is top-right");
		Assert.AreEqual(2 + selectedBorderThickness.Top, checkBoxOrigin.Y, 0.5, "Overlay check box is top-right");
		Assert.AreEqual(contentLeftBefore, content.TransformToVisual(presenter).TransformPoint(default).X, 0.5, "Overlay does not offset content");
	}

	[TestMethod]
	public async Task When_Disabled_Rounded_Opacity_On_Template_Child()
	{
		using var chromeScope = ListViewChromeHelper.UseRoundedChromeResource(true);
		var (_, items) = await CreateWinUIListView(ListViewSelectionMode.Single);
		var (item, presenter) = items[0];
		var content = presenter.GetTemplateChildIfExists()!;

		item.IsEnabled = false;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(0.3, content.Opacity, 1e-6);
		Assert.AreEqual(1.0, item.Opacity);

		item.IsEnabled = true;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(1.0, content.Opacity);
	}

	[TestMethod]
	public async Task When_Disabled_Non_Rounded_Opacity_On_Item()
	{
		using var chromeScope = ListViewChromeHelper.UseNonRoundedChrome();
		var (_, items) = await CreateWinUIListView(ListViewSelectionMode.Single);
		var (item, presenter) = items[0];
		var content = presenter.GetTemplateChildIfExists()!;

		item.IsEnabled = false;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(0.3, item.Opacity, 1e-6);
		Assert.AreEqual(1.0, content.Opacity);

		item.IsEnabled = true;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(1.0, item.Opacity);
	}

	[TestMethod]
	public async Task When_Reveal_Root_Template_VisualStateGroups_Still_Applied()
	{
		using var chromeScope = ListViewChromeHelper.UseRoundedChromeResource(true);
		var style = new Style(typeof(ListViewItem));
		style.Setters.Add(new Setter(Control.TemplateProperty, XamlReader.Load(
			"""
			<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
							 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
							 TargetType="ListViewItem">
				<RevealListViewItemPresenter x:Name="Root"
											 Content="{TemplateBinding Content}"
											 SelectedBackground="Red">
					<VisualStateManager.VisualStateGroups>
						<VisualStateGroup x:Name="CommonStates">
							<VisualState x:Name="Normal" />
							<VisualState x:Name="Selected">
								<VisualState.Setters>
									<Setter Target="Root.Tag" Value="selected" />
								</VisualState.Setters>
							</VisualState>
						</VisualStateGroup>
					</VisualStateManager.VisualStateGroups>
				</RevealListViewItemPresenter>
			</ControlTemplate>
			""")));

		var (list, items) = await CreateListView(ListViewSelectionMode.Single, style);
		var (item, presenter) = items[0];

		Assert.IsInstanceOfType(presenter, typeof(RevealListViewItemPresenter));
		Assert.AreSame(item, ChromeTestHelper.GetParentItem(presenter));

		list.SelectedIndex = 0;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("selected", presenter.Tag, "the template's own VisualStateGroups still run");
		Assert.AreSame(presenter.SelectedBackground, ChromeTestHelper.GetField<Border>(presenter, "m_backplateRectangle").Background);
		Assert.IsNotNull(ChromeTestHelper.GetField<Border?>(presenter, "m_selectionIndicatorRectangle"));
	}

	[TestMethod]
	public async Task When_Unknown_State_Is_No_Op()
	{
		using var chromeScope = ListViewChromeHelper.UseRoundedChromeResource(true);
		var (list, items) = await CreateWinUIListView(ListViewSelectionMode.Single);
		var (item, presenter) = items[0];
		list.SelectedIndex = 0;
		await ChromeTestHelper.WaitForNoRunningAnimation(presenter);

		var before = ChromeTestHelper.GetField<ListViewBaseItemPresenter.VisualStates>(presenter, "m_visualStates");
		var children = presenter.GetChildren().ToArray();

		Assert.IsFalse(VisualStateManager.GoToState(item, "NotAChromeState", true));

		Assert.AreEqual(before, ChromeTestHelper.GetField<ListViewBaseItemPresenter.VisualStates>(presenter, "m_visualStates"));
		CollectionAssert.AreEqual(children, presenter.GetChildren().ToArray());
		Assert.AreEqual(0, ChromeTestHelper.GetField<IList>(presenter, "m_animationCommands").Count);
		ChromeTestHelper.AssertNoRunningAnimation(presenter);
	}

	[TestMethod]
	public async Task When_Known_State_Repeated_Is_No_Op()
	{
		using var chromeScope = ListViewChromeHelper.UseRoundedChromeResource(true);
		var (list, items) = await CreateWinUIListView(ListViewSelectionMode.Single);
		var (item, presenter) = items[0];
		list.SelectedIndex = 0;
		await ChromeTestHelper.WaitForNoRunningAnimation(presenter);

		var indicator = ChromeTestHelper.GetField<Border>(presenter, "m_selectionIndicatorRectangle");

		// Recognised but unchanged: no work, and the probe stops at CommonStates2.
		VisualStateManager.GoToState(item, "Selected", true);

		Assert.AreEqual(0, ChromeTestHelper.GetField<IList>(presenter, "m_animationCommands").Count);
		ChromeTestHelper.AssertNoRunningAnimation(presenter);
		Assert.AreSame(indicator, ChromeTestHelper.GetField<Border>(presenter, "m_selectionIndicatorRectangle"));
	}

	[TestMethod]
	public async Task When_Presenter_Subclass_Is_Template_Root_Links_And_Drives_Chrome()
	{
		using var chromeScope = ListViewChromeHelper.UseRoundedChromeResource(true);
		var style = new Style(typeof(ListViewItem));
		style.Setters.Add(new Setter(Control.TemplateProperty, new ControlTemplate(null, (_, _) => new SubclassedListViewItemPresenter())));

		var (list, items) = await CreateListView(ListViewSelectionMode.Single, style);
		var (item, presenter) = items[0];

		Assert.IsInstanceOfType(presenter, typeof(SubclassedListViewItemPresenter));
		Assert.AreSame(item, ChromeTestHelper.GetParentItem(presenter));
		Assert.AreSame(presenter, item.GetGridViewItemChromeNoRef());

		list.SelectedIndex = 0;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(ListViewBaseItemPresenter.CommonStates2.Selected, ChromeTestHelper.GetField<ListViewBaseItemPresenter.VisualStates>(presenter, "m_visualStates").commonState2);
		Assert.IsNotNull(ChromeTestHelper.GetField<Border?>(presenter, "m_selectionIndicatorRectangle"));
	}

	[TestMethod]
	public async Task When_Retemplated_Old_Chrome_Unlinked()
	{
		using var chromeScope = ListViewChromeHelper.UseRoundedChromeResource(true);
		var (_, items) = await CreateWinUIListView(ListViewSelectionMode.Single);
		var (item, oldPresenter) = items[0];

		item.Template = new ControlTemplate(null, (_, _) => new ListViewItemPresenter());
		await WindowHelper.WaitForIdle();

		var newPresenter = (ListViewItemPresenter)VisualTreeHelper.GetChild(item, 0);
		Assert.AreNotSame(oldPresenter, newPresenter);
		Assert.IsNull(ChromeTestHelper.GetParentItem(oldPresenter));
		Assert.AreSame(item, ChromeTestHelper.GetParentItem(newPresenter));
		Assert.AreSame(newPresenter, item.GetGridViewItemChromeNoRef());
	}

	[TestMethod]
	public async Task When_Recycled_Selected_Container_Has_No_Stale_Chrome()
	{
		using var chromeScope = ListViewChromeHelper.UseRoundedChromeResource(true);
		var source = Enumerable.Range(0, 200).Select(i => $"Item {i}").ToArray();
		var list = new ListView
		{
			Height = 300,
			SelectionMode = ListViewSelectionMode.Single,
			ItemContainerStyle = WinUIItemStyles.ListViewItemStyle,
			ItemsSource = source,
		};
		list.Resources.MergedDictionaries.Add(WinUIItemStyles.Resources);

		WindowHelper.WindowContent = list;
		await WindowHelper.WaitForLoaded(list);
		await WindowHelper.WaitForIdle();

		list.SelectedIndex = 0;
		await WindowHelper.WaitForIdle();

		var container = (ListViewItem)list.ContainerFromIndex(0);
		var presenter = (ListViewItemPresenter)VisualTreeHelper.GetChild(container, 0);
		Assert.IsNotNull(ChromeTestHelper.GetField<Border?>(presenter, "m_selectionIndicatorRectangle"));

		var scrollViewer = list.FindFirstDescendant<ScrollViewer>()!;
		for (var offset = 100.0; offset < 4000 && list.IndexFromContainer(container) is 0 or -1; offset += 100)
		{
			scrollViewer.ChangeView(null, offset, null, disableAnimation: true);
			await WindowHelper.WaitForIdle();
		}

		var reusedIndex = list.IndexFromContainer(container);
		Assert.IsTrue(reusedIndex > 0, "the selected container was not reused");
		Assert.IsFalse(container.IsSelected);

		Assert.IsNull(ChromeTestHelper.GetField<Border?>(presenter, "m_selectionIndicatorRectangle"), "stale selection indicator");
		Assert.IsNull(ChromeTestHelper.GetField<Border?>(presenter, "m_multiSelectCheckBoxRectangle"), "stale check box");
		Assert.AreSame(container.Background, ChromeTestHelper.GetField<Border>(presenter, "m_backplateRectangle").Background, "Normal backplate");
		Assert.AreEqual(ListViewBaseItemPresenter.CommonStates2.Normal, ChromeTestHelper.GetField<ListViewBaseItemPresenter.VisualStates>(presenter, "m_visualStates").commonState2);
		ChromeTestHelper.AssertNoRunningAnimation(presenter);

		scrollViewer.ChangeView(null, 0, null, disableAnimation: true);
		await WindowHelper.WaitForIdle();

		var container0 = (ListViewItem)list.ContainerFromIndex(0);
		var presenter0 = (ListViewItemPresenter)VisualTreeHelper.GetChild(container0, 0);
		Assert.IsTrue(container0.IsSelected);
		var indicator = ChromeTestHelper.GetField<Border?>(presenter0, "m_selectionIndicatorRectangle");
		Assert.IsNotNull(indicator, "indicator missing after scrolling back");
		Assert.AreSame(presenter0, indicator.GetParent());
		ChromeTestHelper.AssertNoRunningAnimation(presenter0);
	}

	[TestMethod]
	public async Task When_PrepareForRecycle_Flushes_Running_Chrome_Animations()
	{
		using var chromeScope = ListViewChromeHelper.UseRoundedChromeResource(true);
		var (list, items) = await CreateWinUIListView(ListViewSelectionMode.Single);
		var (item, presenter) = items[0];

		list.SelectionMode = ListViewSelectionMode.Multiple;
		list.SelectedIndex = 0;

		Assert.IsNotNull(ChromeTestHelper.GetRunningStoryboard(presenter), "precondition: a chrome animation is running");

		item.PrepareForRecycle();

		ChromeTestHelper.AssertNoRunningAnimation(presenter);
		Assert.AreEqual(int.MaxValue, ChromeTestHelper.GetField<int>(presenter, "m_currentHighestCommandPriority"));
	}

	private static Task<(ListView list, List<(ListViewItem item, ListViewItemPresenter presenter)> items)> CreateWinUIListView(ListViewSelectionMode selectionMode)
		=> CreateListView(selectionMode, WinUIItemStyles.ListViewItemStyle);

	private static async Task<(ListView list, List<(ListViewItem item, ListViewItemPresenter presenter)> items)> CreateListView(ListViewSelectionMode selectionMode, Style itemContainerStyle)
	{
		var list = new ListView
		{
			SelectionMode = selectionMode,
			ItemContainerStyle = itemContainerStyle,
			ItemsSource = new[] { "Item 0", "Item 1", "Item 2" },
		};
		list.Resources.MergedDictionaries.Add(WinUIItemStyles.Resources);

		WindowHelper.WindowContent = list;
		await WindowHelper.WaitForLoaded(list);
		await WindowHelper.WaitForIdle();

		var items = new List<(ListViewItem, ListViewItemPresenter)>();
		for (var i = 0; i < 3; i++)
		{
			var item = (ListViewItem)list.ContainerFromIndex(i);
			items.Add((item, (ListViewItemPresenter)VisualTreeHelper.GetChild(item, 0)));
		}

		return (list, items);
	}

	public partial class SubclassedListViewItemPresenter : ListViewItemPresenter
	{
	}
}

internal static class ChromeTestHelper
{
	private static readonly string[] AnimationStates =
	[
		"m_pointerPressedAnimation",
		"m_reorderHintAnimation",
		"m_dragDropAnimation",
		"m_multiSelectAnimation",
		"m_indicatorSelectAnimation",
		"m_selectionIndicatorAnimation",
	];

	public static T GetField<T>(ListViewBaseItemPresenter presenter, string name)
		=> (T)typeof(ListViewBaseItemPresenter)
			.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!
			.GetValue(presenter)!;

	public static ContentControl? GetParentItem(ListViewBaseItemPresenter presenter)
		=> GetField<ContentControl?>(presenter, "m_pParentListViewBaseItemNoRef");

	public static Storyboard? GetRunningStoryboard(ListViewBaseItemPresenter presenter)
	{
		foreach (var name in AnimationStates)
		{
			var state = GetField<object>(presenter, name);
			if (state.GetType().GetField("tpStoryboard", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(state) is Storyboard storyboard)
			{
				return storyboard;
			}
		}

		return null;
	}

	public static void AssertNoRunningAnimation(ListViewBaseItemPresenter presenter)
		=> Assert.IsNull(GetRunningStoryboard(presenter), "a chrome storyboard is still running");

	public static async Task WaitForNoRunningAnimation(ListViewBaseItemPresenter presenter)
	{
		await WindowHelper.WaitForIdle();
		await WindowHelper.WaitFor(() => GetRunningStoryboard(presenter) is null, message: "chrome animations did not complete");
		await WindowHelper.WaitForIdle();
	}
}

// WinUI 2.5.1 DefaultListViewItemStyle / DefaultGridViewItemStyle (controls/dev/CommonStyles), verbatim.
// The resources above them are stand-ins for keys Uno does not define until the styles resync.
internal static class WinUIItemStyles
{
	private static ResourceDictionary? _resources;

	public static ResourceDictionary Resources => _resources ??= (ResourceDictionary)XamlReader.Load(Xaml);

	public static Style ListViewItemStyle => (Style)Resources["DefaultListViewItemStyle"];

	public static Style GridViewItemStyle => (Style)Resources["DefaultGridViewItemStyle"];

	private const string Xaml =
		"""
		<ResourceDictionary xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
		  <x:Double x:Key="ListViewItemDisabledThemeOpacity">0.3</x:Double>
		  <x:Boolean x:Key="ListViewItemSelectionIndicatorVisualEnabled">True</x:Boolean>
		  <CornerRadius x:Key="ListViewItemCornerRadius">4</CornerRadius>
		  <CornerRadius x:Key="ListViewItemCheckBoxCornerRadius">3</CornerRadius>
		  <CornerRadius x:Key="ListViewItemSelectionIndicatorCornerRadius">1.5</CornerRadius>
		  <SolidColorBrush x:Key="ListViewItemCheckPressedBrush" Color="#FF010101" />
		  <SolidColorBrush x:Key="ListViewItemCheckDisabledBrush" Color="#FF020202" />
		  <SolidColorBrush x:Key="ListViewItemCheckBoxPointerOverBrush" Color="#FF030303" />
		  <SolidColorBrush x:Key="ListViewItemCheckBoxPressedBrush" Color="#FF040404" />
		  <SolidColorBrush x:Key="ListViewItemCheckBoxDisabledBrush" Color="#FF050505" />
		  <SolidColorBrush x:Key="ListViewItemCheckBoxSelectedBrush" Color="#FF060606" />
		  <SolidColorBrush x:Key="ListViewItemCheckBoxSelectedPointerOverBrush" Color="#FF070707" />
		  <SolidColorBrush x:Key="ListViewItemCheckBoxSelectedPressedBrush" Color="#FF080808" />
		  <SolidColorBrush x:Key="ListViewItemCheckBoxSelectedDisabledBrush" Color="#FF090909" />
		  <SolidColorBrush x:Key="ListViewItemCheckBoxBorderBrush" Color="#FF0A0A0A" />
		  <SolidColorBrush x:Key="ListViewItemCheckBoxPointerOverBorderBrush" Color="#FF0B0B0B" />
		  <SolidColorBrush x:Key="ListViewItemCheckBoxPressedBorderBrush" Color="#FF0C0C0C" />
		  <SolidColorBrush x:Key="ListViewItemCheckBoxDisabledBorderBrush" Color="#FF0D0D0D" />
		  <SolidColorBrush x:Key="ListViewItemBackgroundSelectedDisabled" Color="#FF0E0E0E" />
		  <SolidColorBrush x:Key="ListViewItemSelectionIndicatorBrush" Color="#FF0F0F0F" />
		  <SolidColorBrush x:Key="ListViewItemSelectionIndicatorPointerOverBrush" Color="#FF101010" />
		  <SolidColorBrush x:Key="ListViewItemSelectionIndicatorPressedBrush" Color="#FF111111" />
		  <SolidColorBrush x:Key="ListViewItemSelectionIndicatorDisabledBrush" Color="#FF121212" />
		  <Style x:Key="DefaultListViewItemStyle" TargetType="ListViewItem">
		    <Setter Property="FontFamily" Value="{ThemeResource ContentControlThemeFontFamily}" />
		    <Setter Property="FontSize" Value="{ThemeResource ControlContentThemeFontSize}" />
		    <Setter Property="Background" Value="{ThemeResource ListViewItemBackground}" />
		    <Setter Property="Foreground" Value="{ThemeResource ListViewItemForeground}" />
		    <Setter Property="TabNavigation" Value="Local" />
		    <Setter Property="IsHoldingEnabled" Value="True" />
		    <Setter Property="Padding" Value="16,0,12,0" />
		    <Setter Property="HorizontalContentAlignment" Value="Stretch" />
		    <Setter Property="VerticalContentAlignment" Value="Center" />
		    <Setter Property="MinWidth" Value="{ThemeResource ListViewItemMinWidth}" />
		    <Setter Property="MinHeight" Value="{ThemeResource ListViewItemMinHeight}" />
		    <Setter Property="AllowDrop" Value="False" />
		    <Setter Property="UseSystemFocusVisuals" Value="True" />
		    <Setter Property="FocusVisualMargin" Value="1" />
		    <Setter Property="FocusVisualPrimaryBrush" Value="{ThemeResource ListViewItemFocusVisualPrimaryBrush}" />
		    <Setter Property="FocusVisualPrimaryThickness" Value="2" />
		    <Setter Property="FocusVisualSecondaryBrush" Value="{ThemeResource ListViewItemFocusVisualSecondaryBrush}" />
		    <Setter Property="FocusVisualSecondaryThickness" Value="1" />
		    <Setter Property="Template">
		      <Setter.Value>
		        <ControlTemplate TargetType="ListViewItem">
		          <ListViewItemPresenter x:Name="Root" ContentTransitions="{TemplateBinding ContentTransitions}" Control.IsTemplateFocusTarget="True" FocusVisualMargin="{TemplateBinding FocusVisualMargin}" FocusVisualPrimaryBrush="{TemplateBinding FocusVisualPrimaryBrush}" FocusVisualPrimaryThickness="{TemplateBinding FocusVisualPrimaryThickness}" FocusVisualSecondaryBrush="{TemplateBinding FocusVisualSecondaryBrush}" FocusVisualSecondaryThickness="{TemplateBinding FocusVisualSecondaryThickness}" SelectionCheckMarkVisualEnabled="{ThemeResource ListViewItemSelectionCheckMarkVisualEnabled}" CheckBrush="{ThemeResource ListViewItemCheckBrush}" CheckBoxBrush="{ThemeResource ListViewItemCheckBoxBrush}" DragBackground="{ThemeResource ListViewItemDragBackground}" DragForeground="{ThemeResource ListViewItemDragForeground}" FocusBorderBrush="{ThemeResource ListViewItemFocusBorderBrush}" FocusSecondaryBorderBrush="{ThemeResource ListViewItemFocusSecondaryBorderBrush}" PlaceholderBackground="{ThemeResource ListViewItemPlaceholderBackground}" PointerOverBackground="{ThemeResource ListViewItemBackgroundPointerOver}" PointerOverForeground="{ThemeResource ListViewItemForegroundPointerOver}" SelectedBackground="{ThemeResource ListViewItemBackgroundSelected}" SelectedForeground="{ThemeResource ListViewItemForegroundSelected}" SelectedPointerOverBackground="{ThemeResource ListViewItemBackgroundSelectedPointerOver}" PressedBackground="{ThemeResource ListViewItemBackgroundPressed}" SelectedPressedBackground="{ThemeResource ListViewItemBackgroundSelectedPressed}" DisabledOpacity="{ThemeResource ListViewItemDisabledThemeOpacity}" DragOpacity="{ThemeResource ListViewItemDragThemeOpacity}" ReorderHintOffset="{ThemeResource ListViewItemReorderHintThemeOffset}" HorizontalContentAlignment="{TemplateBinding HorizontalContentAlignment}" VerticalContentAlignment="{TemplateBinding VerticalContentAlignment}" ContentMargin="{TemplateBinding Padding}" CheckMode="{ThemeResource ListViewItemCheckMode}" CornerRadius="{ThemeResource ListViewItemCornerRadius}" CheckPressedBrush="{ThemeResource ListViewItemCheckPressedBrush}" CheckDisabledBrush="{ThemeResource ListViewItemCheckDisabledBrush}" CheckBoxPointerOverBrush="{ThemeResource ListViewItemCheckBoxPointerOverBrush}" CheckBoxPressedBrush="{ThemeResource ListViewItemCheckBoxPressedBrush}" CheckBoxDisabledBrush="{ThemeResource ListViewItemCheckBoxDisabledBrush}" CheckBoxSelectedBrush="{ThemeResource ListViewItemCheckBoxSelectedBrush}" CheckBoxSelectedPointerOverBrush="{ThemeResource ListViewItemCheckBoxSelectedPointerOverBrush}" CheckBoxSelectedPressedBrush="{ThemeResource ListViewItemCheckBoxSelectedPressedBrush}" CheckBoxSelectedDisabledBrush="{ThemeResource ListViewItemCheckBoxSelectedDisabledBrush}" CheckBoxBorderBrush="{ThemeResource ListViewItemCheckBoxBorderBrush}" CheckBoxPointerOverBorderBrush="{ThemeResource ListViewItemCheckBoxPointerOverBorderBrush}" CheckBoxPressedBorderBrush="{ThemeResource ListViewItemCheckBoxPressedBorderBrush}" CheckBoxDisabledBorderBrush="{ThemeResource ListViewItemCheckBoxDisabledBorderBrush}" CheckBoxCornerRadius="{ThemeResource ListViewItemCheckBoxCornerRadius}" SelectionIndicatorCornerRadius="{ThemeResource ListViewItemSelectionIndicatorCornerRadius}" SelectionIndicatorVisualEnabled="{ThemeResource ListViewItemSelectionIndicatorVisualEnabled}" SelectionIndicatorBrush="{ThemeResource ListViewItemSelectionIndicatorBrush}" SelectionIndicatorPointerOverBrush="{ThemeResource ListViewItemSelectionIndicatorPointerOverBrush}" SelectionIndicatorPressedBrush="{ThemeResource ListViewItemSelectionIndicatorPressedBrush}" SelectionIndicatorDisabledBrush="{ThemeResource ListViewItemSelectionIndicatorDisabledBrush}" SelectedDisabledBackground="{ThemeResource ListViewItemBackgroundSelectedDisabled}" />
		        </ControlTemplate>
		      </Setter.Value>
		    </Setter>
		  </Style>
		  <Style x:Key="DefaultGridViewItemStyle" TargetType="GridViewItem">
		    <Setter Property="FontFamily" Value="{ThemeResource ContentControlThemeFontFamily}" />
		    <Setter Property="FontSize" Value="{ThemeResource ControlContentThemeFontSize}" />
		    <Setter Property="Background" Value="{ThemeResource GridViewItemBackground}" />
		    <Setter Property="Foreground" Value="{ThemeResource GridViewItemForeground}" />
		    <Setter Property="TabNavigation" Value="Local" />
		    <Setter Property="IsHoldingEnabled" Value="True" />
		    <Setter Property="HorizontalContentAlignment" Value="Center" />
		    <Setter Property="VerticalContentAlignment" Value="Center" />
		    <Setter Property="Margin" Value="0,0,4,4" />
		    <Setter Property="MinWidth" Value="{ThemeResource GridViewItemMinWidth}" />
		    <Setter Property="MinHeight" Value="{ThemeResource GridViewItemMinHeight}" />
		    <Setter Property="AllowDrop" Value="True" />
		    <Setter Property="UseSystemFocusVisuals" Value="{StaticResource UseSystemFocusVisuals}" />
		    <Setter Property="FocusVisualMargin" Value="-3" />
		    <Setter Property="FocusVisualPrimaryBrush" Value="{ThemeResource GridViewItemFocusVisualPrimaryBrush}" />
		    <Setter Property="FocusVisualPrimaryThickness" Value="2" />
		    <Setter Property="FocusVisualSecondaryBrush" Value="{ThemeResource GridViewItemFocusVisualSecondaryBrush}" />
		    <Setter Property="FocusVisualSecondaryThickness" Value="1" />
		    <Setter Property="Template">
		      <Setter.Value>
		        <ControlTemplate TargetType="GridViewItem">
		          <ListViewItemPresenter x:Name="Root" ContentTransitions="{TemplateBinding ContentTransitions}" Control.IsTemplateFocusTarget="True" FocusVisualMargin="{TemplateBinding FocusVisualMargin}" FocusVisualPrimaryBrush="{TemplateBinding FocusVisualPrimaryBrush}" FocusVisualPrimaryThickness="{TemplateBinding FocusVisualPrimaryThickness}" FocusVisualSecondaryBrush="{TemplateBinding FocusVisualSecondaryBrush}" FocusVisualSecondaryThickness="{TemplateBinding FocusVisualSecondaryThickness}" SelectionCheckMarkVisualEnabled="{ThemeResource GridViewItemSelectionCheckMarkVisualEnabled}" CheckBrush="{ThemeResource GridViewItemCheckBrush}" CheckBoxBrush="{ThemeResource GridViewItemCheckBoxBrush}" DragBackground="{ThemeResource GridViewItemDragBackground}" DragForeground="{ThemeResource GridViewItemDragForeground}" FocusBorderBrush="{ThemeResource GridViewItemFocusBorderBrush}" PlaceholderBackground="{ThemeResource GridViewItemPlaceholderBackground}" PointerOverBackground="{ThemeResource GridViewItemBackgroundPointerOver}" PointerOverForeground="{ThemeResource GridViewItemForegroundPointerOver}" SelectedBackground="{ThemeResource GridViewItemBackgroundSelected}" SelectedForeground="{ThemeResource GridViewItemForegroundSelected}" SelectedPointerOverBackground="{ThemeResource GridViewItemBackgroundSelectedPointerOver}" PressedBackground="{ThemeResource GridViewItemBackgroundPressed}" SelectedPressedBackground="{ThemeResource GridViewItemBackgroundSelectedPressed}" DisabledOpacity="{ThemeResource ListViewItemDisabledThemeOpacity}" DragOpacity="{ThemeResource ListViewItemDragThemeOpacity}" ReorderHintOffset="{ThemeResource GridViewItemReorderHintThemeOffset}" HorizontalContentAlignment="{TemplateBinding HorizontalContentAlignment}" VerticalContentAlignment="{TemplateBinding VerticalContentAlignment}" ContentMargin="{TemplateBinding Padding}" CheckMode="{ThemeResource GridViewItemCheckMode}" SelectedBorderThickness="{ThemeResource GridViewItemSelectedBorderThickness}" SelectedPointerOverBorderBrush="{ThemeResource GridViewItemSelectedPointerOverBorderBrush}" CornerRadius="{ThemeResource GridViewItemCornerRadius}" CheckBoxCornerRadius="{ThemeResource GridViewItemCheckBoxCornerRadius}" CheckPressedBrush="{ThemeResource GridViewItemCheckPressedBrush}" CheckDisabledBrush="{ThemeResource GridViewItemCheckDisabledBrush}" CheckBoxPointerOverBrush="{ThemeResource GridViewItemCheckBoxPointerOverBrush}" CheckBoxPressedBrush="{ThemeResource GridViewItemCheckBoxPressedBrush}" CheckBoxDisabledBrush="{ThemeResource GridViewItemCheckBoxDisabledBrush}" CheckBoxSelectedBrush="{ThemeResource GridViewItemCheckBoxSelectedBrush}" CheckBoxSelectedPointerOverBrush="{ThemeResource GridViewItemCheckBoxSelectedPointerOverBrush}" CheckBoxSelectedPressedBrush="{ThemeResource GridViewItemCheckBoxSelectedPressedBrush}" CheckBoxSelectedDisabledBrush="{ThemeResource GridViewItemCheckBoxSelectedDisabledBrush}" CheckBoxBorderBrush="{ThemeResource GridViewItemCheckBoxBorderBrush}" CheckBoxPointerOverBorderBrush="{ThemeResource GridViewItemCheckBoxPointerOverBorderBrush}" CheckBoxPressedBorderBrush="{ThemeResource GridViewItemCheckBoxPressedBorderBrush}" CheckBoxDisabledBorderBrush="{ThemeResource GridViewItemCheckBoxDisabledBorderBrush}" PointerOverBorderBrush="{ThemeResource GridViewItemPointerOverBorderBrush}" SelectedDisabledBackground="{ThemeResource GridViewItemBackgroundSelectedDisabled}" SelectedBorderBrush="{ThemeResource GridViewItemSelectedBorderBrush}" SelectedPressedBorderBrush="{ThemeResource GridViewItemSelectedPressedBorderBrush}" SelectedDisabledBorderBrush="{ThemeResource GridViewItemSelectedDisabledBorderBrush}" SelectedInnerBorderBrush="{ThemeResource GridViewItemSelectedInnerBorderBrush}" />
		        </ControlTemplate>
		      </Setter.Value>
		    </Setter>
		  </Style>
		</ResourceDictionary>
		""";
}
#endif
