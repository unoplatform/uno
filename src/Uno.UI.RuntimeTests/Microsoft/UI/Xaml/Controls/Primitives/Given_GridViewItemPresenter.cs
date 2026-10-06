#if HAS_UNO
#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;
using static Private.Infrastructure.TestServices;

#if HAS_INPUT_INJECTOR
using Uno.UI.DevTools.Input;
using Windows.UI.Input.Preview.Injection;
#endif

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
	public async Task When_Default_GridViewItem_Template_Root_Is_Rounded_Presenter()
	{
		var (_, item, presenter) = await CreateGridView();

		Assert.IsInstanceOfType(presenter, typeof(ListViewItemPresenter), "The default GridViewItem style roots on ListViewItemPresenter");
		Assert.AreEqual(new CornerRadius(4), presenter.CornerRadius);
		Assert.AreEqual(ListViewItemPresenterCheckMode.Overlay, ((ListViewItemPresenter)presenter).CheckMode);
		Assert.IsTrue(item.AllowDrop, "WinUI's DefaultGridViewItemStyle sets AllowDrop");
		Assert.AreSame(presenter, item.GetGridViewItemChromeNoRef());
	}

	[TestMethod]
	public async Task When_Default_GridViewItem_PointerOver_Shows_Outer_Border()
	{
		var (_, item, presenter) = await CreateGridView();

		Assert.IsNull(ChromeTestHelper.GetField<Border?>(presenter, "m_outerBorder"));

		VisualStateManager.GoToState(item, "PointerOver", false);

		var outer = ChromeTestHelper.GetField<Border>(presenter, "m_outerBorder");
		Assert.AreSame(((ListViewItemPresenter)presenter).PointerOverBorderBrush, outer.BorderBrush);
		Assert.AreEqual(1, outer.BorderThickness.Left, 0.5, "Hover draws a 1 px outer border");
		Assert.AreEqual(new CornerRadius(4), outer.CornerRadius);
		Assert.IsNull(ChromeTestHelper.GetField<Border?>(presenter, "m_innerSelectionBorder"));

		VisualStateManager.GoToState(item, "Normal", false);

		Assert.IsNull(ChromeTestHelper.GetField<Border?>(presenter, "m_outerBorder"));
		Assert.IsNull(outer.GetParent());
	}

	[TestMethod]
	public async Task When_Default_GridViewItem_Selected_Shows_Outer_And_Inner_Borders()
	{
		var (grid, _, presenter) = await CreateGridView();
		var lvip = (ListViewItemPresenter)presenter;

		grid.SelectedIndex = 0;
		await WindowHelper.WaitForIdle();

		var outer = ChromeTestHelper.GetField<Border>(presenter, "m_outerBorder");
		var inner = ChromeTestHelper.GetField<Border>(presenter, "m_innerSelectionBorder");
		var backplate = ChromeTestHelper.GetField<Border>(presenter, "m_backplateRectangle");
		Assert.AreSame(lvip.SelectedBorderBrush, outer.BorderBrush);
		Assert.AreSame(lvip.SelectedInnerBorderBrush, inner.BorderBrush);
		Assert.AreSame(lvip.SelectedBackground, backplate.Background);
		Assert.AreEqual(2, outer.BorderThickness.Left, 0.5, "Selected draws a 2 px outer border");

		// The accent border brush is opaque: the inner border nests in the outer one and the backplate insets by 1 px.
		Assert.AreSame(outer, inner.GetParent());
		Assert.AreEqual(1, backplate.Margin.Left, 0.5);
		Assert.AreEqual(1, backplate.Margin.Top, 0.5);
		Assert.AreEqual(new CornerRadius(4), outer.CornerRadius);
		Assert.AreEqual(new CornerRadius(3), inner.CornerRadius);

		grid.SelectedIndex = -1;
		await WindowHelper.WaitForIdle();

		Assert.IsNull(ChromeTestHelper.GetField<Border?>(presenter, "m_innerSelectionBorder"));
		Assert.IsNull(ChromeTestHelper.GetField<Border?>(presenter, "m_outerBorder"));
		Assert.AreEqual(default(Thickness), backplate.Margin);
	}

	[TestMethod]
	public async Task When_Selected_Border_Translucent_Inner_Border_Not_Nested()
	{
		var (grid, _, presenter) = await CreateGridView();
		((ListViewItemPresenter)presenter).SelectedBorderBrush = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0x00, 0x00));

		grid.SelectedIndex = 0;
		await WindowHelper.WaitForIdle();

		var outer = ChromeTestHelper.GetField<Border>(presenter, "m_outerBorder");
		var inner = ChromeTestHelper.GetField<Border>(presenter, "m_innerSelectionBorder");
		Assert.AreSame(presenter, inner.GetParent());
		Assert.AreSame(presenter, outer.GetParent());
		Assert.AreEqual(default(Thickness), ChromeTestHelper.GetField<Border>(presenter, "m_backplateRectangle").Margin);
		Assert.AreEqual(new CornerRadius(4), inner.CornerRadius, "A non-nested inner border keeps the general radius");
	}

	[TestMethod]
	public async Task When_CornerRadius_Changes_On_Selected_Item_Borders_Follow()
	{
		var (grid, _, presenter) = await CreateGridView();

		grid.SelectedIndex = 0;
		await WindowHelper.WaitForIdle();

		var outer = ChromeTestHelper.GetField<Border>(presenter, "m_outerBorder");
		var inner = ChromeTestHelper.GetField<Border>(presenter, "m_innerSelectionBorder");
		var backplate = ChromeTestHelper.GetField<Border>(presenter, "m_backplateRectangle");

		presenter.CornerRadius = new CornerRadius(8);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(new CornerRadius(8), outer.CornerRadius);
		Assert.AreEqual(new CornerRadius(6), inner.CornerRadius, "Nested inner radius = outer radius - selected border thickness");
		Assert.AreEqual(new CornerRadius(8), backplate.CornerRadius);

		presenter.CornerRadius = new CornerRadius(1);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(new CornerRadius(1), outer.CornerRadius);
		Assert.AreEqual(new CornerRadius(3), inner.CornerRadius, "Nested inner radius never drops below 3");
	}

	[TestMethod]
	public async Task When_Disabled_While_Selected_Keeps_Selected_Border()
	{
		var (grid, item, presenter) = await CreateGridView();
		var lvip = (ListViewItemPresenter)presenter;

		grid.SelectedIndex = 0;
		await WindowHelper.WaitForIdle();

		item.IsEnabled = false;
		await WindowHelper.WaitForIdle();

		Assert.AreSame(lvip.SelectedDisabledBorderBrush, ChromeTestHelper.GetField<Border>(presenter, "m_outerBorder").BorderBrush);
		Assert.AreEqual(lvip.DisabledOpacity, presenter.GetTemplateChildIfExists()!.Opacity, 1e-6);
		Assert.AreSame(lvip.SelectedDisabledBackground, ChromeTestHelper.GetField<Border>(presenter, "m_backplateRectangle").Background);
	}

	[TestMethod]
	public async Task When_Default_GridViewItem_MultiSelect_Overlay_CheckBox()
	{
		var (grid, _, presenter) = await CreateGridView();

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

	[TestMethod]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_Default_GridView_Item_Dragged_Over_Uses_Horizontal_Zones()
	{
#if HAS_INPUT_INJECTOR
		var grid = new GridView
		{
			Width = 400,
			Height = 200,
			AllowDrop = true,
			CanDragItems = true,
			CanReorderItems = true,
			ItemsSource = new ObservableCollection<string> { "0", "1", "2" },
			ItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
				"""
				<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
					<Border Width="100" Height="100"><TextBlock Text="{Binding}" /></Border>
				</DataTemplate>
				"""),
		};

		await UITestHelper.Load(grid, x => x.IsLoaded && grid.ContainerFromIndex(2) is { });
		await WindowHelper.WaitForIdle();

		var dragged = (GridViewItem)grid.ContainerFromIndex(0);
		var target = (GridViewItem)grid.ContainerFromIndex(2);
		Assert.IsTrue(target.AllowDrop, "The default GridViewItem style sets AllowDrop");

		var dropCount = 0;
		DataPackageOperation? dropResult = null;
		grid.AddHandler(UIElement.DropEvent, new DragEventHandler((_, _) => dropCount++), handledEventsToo: true);
		grid.DragItemsCompleted += (_, e) => dropResult = e.DropResult;

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();

		// The panel flows horizontally, so the 30/40/30 GridViewItem zones split the item's width.
		var bounds = target.GetAbsoluteBoundsRect();
		var centerNearTop = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height * 0.1);
		var rightEdge = new Point(bounds.X + bounds.Width * 0.9, bounds.Y + bounds.Height / 2);

		mouse.Press(Center(dragged));
		await WindowHelper.WaitForIdle();
		try
		{
			mouse.MoveTo(centerNearTop, 16);
			await WindowHelper.WaitForIdle();
			// Moves made while the drag operation starts are not routed as drag events.
			await Task.Delay(100);
			mouse.MoveTo(new Point(centerNearTop.X + 1, centerNearTop.Y), 2);
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(grid.IsDragOverItem(target), "The horizontal center of the item is the drop-into zone");

			mouse.MoveTo(rightEdge, 2);
			await WindowHelper.WaitForIdle();
			Assert.IsFalse(grid.IsDragOverItem(target), "The horizontal edge of the item is a reorder zone");
		}
		finally
		{
			mouse.Release();
			await WindowHelper.WaitForIdle();
		}

		await UITestHelper.WaitFor(() => dropResult is not null, timeoutMS: 2000);
		Assert.AreEqual(1, dropCount, "The GridView still accepts the drop next to an AllowDrop item");
		Assert.AreEqual(DataPackageOperation.Move, dropResult);

		mouse.MoveTo(new Point(rightEdge.X, rightEdge.Y + 1000));
		await WindowHelper.WaitForIdle();

		static Point Center(FrameworkElement element)
		{
			var b = element.GetAbsoluteBoundsRect();
			return new Point(b.X + b.Width / 2, b.Y + b.Height / 2);
		}
#else
		await Task.CompletedTask;
#endif
	}

	private static async Task<(GridView grid, GridViewItem item, ListViewBaseItemPresenter presenter)> CreateGridView(Style? itemContainerStyle = null)
	{
		var grid = new GridView
		{
			Width = 400,
			Height = 300,
			SelectionMode = ListViewSelectionMode.Single,
			ItemsSource = new[] { "Item 0", "Item 1", "Item 2" },
		};
		if (itemContainerStyle is not null)
		{
			grid.ItemContainerStyle = itemContainerStyle;
		}

		WindowHelper.WindowContent = grid;
		await WindowHelper.WaitForLoaded(grid);
		await WindowHelper.WaitForIdle();

		var item = (GridViewItem)grid.ContainerFromIndex(0);
		return (grid, item, (ListViewBaseItemPresenter)VisualTreeHelper.GetChild(item, 0));
	}
}
#endif
