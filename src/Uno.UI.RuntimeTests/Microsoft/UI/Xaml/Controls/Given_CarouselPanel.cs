#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_CarouselPanel
{
	private static string[] CreateItems(int count) => Enumerable.Range(0, count).Select(x => $"Item {x}").ToArray();

	private static async Task<(ComboBox ComboBox, CarouselPanel Panel, ScrollViewer ScrollViewer)> Open(ComboBox comboBox)
	{
		comboBox.IsDropDownOpen = true;
		await WindowHelper.WaitForIdle();

		var panel = (CarouselPanel)comboBox.ItemsPanelRoot;
		return (comboBox, panel, panel.FindFirstAncestorOrThrow<ScrollViewer>()!);
	}

	private static bool IsInViewport(ComboBox comboBox, ScrollViewer scrollViewer, int index, double tolerance = 0.5)
	{
		if (comboBox.ContainerFromIndex(index) is not FrameworkElement container)
		{
			return false;
		}

		var top = container.TransformToVisual(scrollViewer).TransformPoint(default).Y;
		return top >= -tolerance && top + container.ActualHeight <= scrollViewer.ViewportHeight + tolerance;
	}

	[TestMethod]
	public async Task When_Default_Template_Then_Hosts_Items_On_Modern_Panel()
	{
		ComboBox sut = new() { ItemsSource = CreateItems(5) };
		await UITestHelper.Load(sut);

		try
		{
			var (_, panel, _) = await Open(sut);

			Assert.IsInstanceOfType<ModernCollectionBasePanel>(panel);
			Assert.AreEqual(Orientation.Vertical, panel.PhysicalOrientation);
			Assert.HasCount(5, panel.Children.OfType<ComboBoxItem>().ToArray());
		}
		finally
		{
			sut.IsDropDownOpen = false;
		}
	}

	[TestMethod]
	public async Task When_Queried_As_Snap_Points_Info_Then_Does_Not_Throw()
	{
		ComboBox sut = new() { ItemsSource = CreateItems(5) };
		await UITestHelper.Load(sut);

		try
		{
			var (_, panel, _) = await Open(sut);
			IScrollSnapPointsInfo info = panel;

			Assert.AreEqual(info.AreVerticalSnapPointsRegular, panel.AreVerticalSnapPointsRegular);
			Assert.AreEqual(info.AreHorizontalSnapPointsRegular, panel.AreHorizontalSnapPointsRegular);

			EventHandler<object> handler = (_, _) => { };
			panel.VerticalSnapPointsChanged += handler;
			panel.VerticalSnapPointsChanged -= handler;
		}
		finally
		{
			sut.IsDropDownOpen = false;
		}
	}

	[TestMethod]
	public async Task When_Few_Items_Then_DropDown_Fits_Them()
	{
		ComboBox sut = new() { ItemsSource = CreateItems(3) };
		await UITestHelper.Load(sut);

		try
		{
			var (_, panel, scrollViewer) = await Open(sut);

			var containers = panel.Children.OfType<ComboBoxItem>().ToArray();
			Assert.HasCount(3, containers);

			var itemsPresenterMargin = ((FrameworkElement)VisualTreeHelper.GetParent(panel)).Margin;
			var itemsHeight = containers.Sum(c => c.ActualHeight) + itemsPresenterMargin.Top + itemsPresenterMargin.Bottom;
			Assert.IsGreaterThan(0, itemsHeight, "Containers should be measured");
			Assert.AreEqual(itemsHeight, scrollViewer.ExtentHeight, 1.0, "Extent should fit the items");
			Assert.AreEqual(itemsHeight, scrollViewer.ViewportHeight, 1.0, "Viewport should shrink to the items");
			Assert.AreEqual(0, scrollViewer.ScrollableHeight, 1.0, "Should not scroll");
			Assert.IsTrue(containers.All(c => c.ActualWidth >= sut.ActualWidth - 1 - panel.Margin.Left - panel.Margin.Right), "Containers should stretch to the dropdown");
		}
		finally
		{
			sut.IsDropDownOpen = false;
		}
	}

	[TestMethod]
	public async Task When_Many_Items_Then_DropDown_Is_Limited_And_Virtualized()
	{
		ComboBox sut = new() { ItemsSource = CreateItems(1000) };
		await UITestHelper.Load(sut);

		try
		{
			var (_, panel, scrollViewer) = await Open(sut);

			Assert.IsLessThanOrEqualTo(sut.MaxDropDownHeight, scrollViewer.ViewportHeight);
			Assert.IsGreaterThan(scrollViewer.ViewportHeight, scrollViewer.ExtentHeight, "Should be scrollable");

			// CacheLength 0.5: half a viewport around it, which a thousand items must not defeat.
			Assert.IsLessThan(50, panel.Children.OfType<ComboBoxItem>().Count());
		}
		finally
		{
			sut.IsDropDownOpen = false;
		}
	}

	[TestMethod]
	public async Task When_Scrolled_Then_Containers_Are_Recycled()
	{
		ComboBox sut = new() { ItemsSource = CreateItems(1000) };
		await UITestHelper.Load(sut);

		try
		{
			var (_, panel, scrollViewer) = await Open(sut);
			var initial = panel.Children.OfType<ComboBoxItem>().Count();

			scrollViewer.ChangeView(null, scrollViewer.ExtentHeight / 2, null, disableAnimation: true);
			await WindowHelper.WaitForIdle();

			Assert.IsLessThanOrEqualTo(initial + 5, panel.Children.OfType<ComboBoxItem>().Count());
			Assert.IsNull(sut.ContainerFromIndex(40), "Containers far behind the viewport should be recycled");
			Assert.IsNotNull(sut.ContainerFromIndex(500), "Containers in the viewport should be realized");
		}
		finally
		{
			sut.IsDropDownOpen = false;
		}
	}

	[TestMethod]
	public async Task When_SelectedIndex_Set_Before_First_Open_Then_Selected_Item_Is_Shown()
	{
		ComboBox sut = new() { ItemsSource = CreateItems(200), SelectedIndex = 100 };
		await UITestHelper.Load(sut);

		try
		{
			Assert.AreEqual("Item 100", sut.SelectedItem);

			var (_, _, scrollViewer) = await Open(sut);

			Assert.AreEqual(100, sut.SelectedIndex);
			Assert.IsTrue(IsInViewport(sut, scrollViewer, 100), $"Selected container should be in the viewport, offset={scrollViewer.VerticalOffset} viewport={scrollViewer.ViewportHeight}");

			var container = (ComboBoxItem)sut.ContainerFromIndex(100);
			Assert.IsTrue(container.IsSelected);

			var centerOffset = container.TransformToVisual(scrollViewer).TransformPoint(default).Y + container.ActualHeight / 2 - scrollViewer.ViewportHeight / 2;
			Assert.AreEqual(0, centerOffset, container.ActualHeight, "Selected container should be centered");
			Assert.AreEqual(container, FocusManager.GetFocusedElement(sut.XamlRoot!));
		}
		finally
		{
			sut.IsDropDownOpen = false;
		}
	}

	[TestMethod]
	public async Task When_Selection_Changed_While_Closed_Then_Reopen_Shows_It()
	{
		ComboBox sut = new() { ItemsSource = CreateItems(200), SelectedIndex = 5 };
		await UITestHelper.Load(sut);

		try
		{
			var (_, _, scrollViewer) = await Open(sut);
			Assert.IsTrue(IsInViewport(sut, scrollViewer, 5));

			sut.IsDropDownOpen = false;
			await WindowHelper.WaitForIdle();

			sut.SelectedIndex = 150;
			await WindowHelper.WaitForIdle();

			(_, _, scrollViewer) = await Open(sut);
			Assert.IsTrue(IsInViewport(sut, scrollViewer, 150), $"offset={scrollViewer.VerticalOffset}");
			Assert.IsTrue(((ComboBoxItem)sut.ContainerFromIndex(150)).IsSelected);
		}
		finally
		{
			sut.IsDropDownOpen = false;
		}
	}

	[TestMethod]
	public async Task When_Selected_Is_Last_Then_Viewport_Is_Clamped_To_The_End()
	{
		ComboBox sut = new() { ItemsSource = CreateItems(100), SelectedIndex = 99 };
		await UITestHelper.Load(sut);

		try
		{
			var (_, _, scrollViewer) = await Open(sut);

			Assert.IsTrue(IsInViewport(sut, scrollViewer, 99), $"offset={scrollViewer.VerticalOffset}");
			Assert.AreEqual(scrollViewer.ScrollableHeight, scrollViewer.VerticalOffset, 1.0);
		}
		finally
		{
			sut.IsDropDownOpen = false;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Open_Arrow_Keys_Move_Past_The_Viewport()
	{
		ComboBox sut = new() { ItemsSource = CreateItems(100), SelectedIndex = 0 };
		await UITestHelper.Load(sut);

		try
		{
			sut.Focus(FocusState.Programmatic);
			var (_, _, scrollViewer) = await Open(sut);

			const int steps = 30;
			for (var i = 0; i < steps; i++)
			{
				await KeyboardHelper.Down();
			}
			await WindowHelper.WaitForIdle();

			var focused = FocusManager.GetFocusedElement(sut.XamlRoot!) as ComboBoxItem;
			Assert.IsNotNull(focused);
			Assert.AreEqual(steps, sut.IndexFromContainer(focused));
			Assert.IsTrue(IsInViewport(sut, scrollViewer, steps), $"offset={scrollViewer.VerticalOffset}");

			for (var i = 0; i < steps; i++)
			{
				await KeyboardHelper.Up();
			}
			await WindowHelper.WaitForIdle();

			focused = FocusManager.GetFocusedElement(sut.XamlRoot!) as ComboBoxItem;
			Assert.IsNotNull(focused);
			Assert.AreEqual(0, sut.IndexFromContainer(focused));
			Assert.IsTrue(IsInViewport(sut, scrollViewer, 0), $"offset={scrollViewer.VerticalOffset}");
		}
		finally
		{
			sut.IsDropDownOpen = false;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Closed_Arrow_Keys_Change_Selection_Without_Opening()
	{
		ComboBox sut = new() { ItemsSource = CreateItems(100), SelectedIndex = 10 };
		await UITestHelper.Load(sut);

		sut.Focus(FocusState.Programmatic);
		await WindowHelper.WaitForIdle();

		await KeyboardHelper.Down();
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(11, sut.SelectedIndex);
		Assert.IsFalse(sut.IsDropDownOpen);

		await KeyboardHelper.Up();
		await KeyboardHelper.Up();
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(9, sut.SelectedIndex);
		Assert.IsFalse(sut.IsDropDownOpen);
		Assert.AreEqual(sut, FocusManager.GetFocusedElement(sut.XamlRoot!));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Closed_By_Enter_Then_Focus_Returns_And_Selection_Commits()
	{
		ComboBox sut = new() { ItemsSource = CreateItems(100), SelectedIndex = 40 };
		await UITestHelper.Load(sut);

		sut.Focus(FocusState.Programmatic);
		await Open(sut);

		await KeyboardHelper.Down();
		await WindowHelper.WaitForIdle();
		await KeyboardHelper.Enter();
		await WindowHelper.WaitForIdle();

		Assert.IsFalse(sut.IsDropDownOpen);
		Assert.AreEqual(41, sut.SelectedIndex);
		Assert.AreEqual("Item 41", sut.SelectedItem);
		Assert.AreEqual(sut, FocusManager.GetFocusedElement(sut.XamlRoot!));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Closed_By_Escape_Then_Focus_Returns_And_Selection_Restores()
	{
		ComboBox sut = new()
		{
			ItemsSource = CreateItems(100),
			SelectedIndex = 40,
			SelectionChangedTrigger = ComboBoxSelectionChangedTrigger.Always,
		};
		await UITestHelper.Load(sut);

		sut.Focus(FocusState.Programmatic);
		await Open(sut);

		await KeyboardHelper.Down();
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(41, sut.SelectedIndex);

		await KeyboardHelper.Escape();
		await WindowHelper.WaitForIdle();

		Assert.IsFalse(sut.IsDropDownOpen);
		Assert.AreEqual(40, sut.SelectedIndex);
		Assert.AreEqual(sut, FocusManager.GetFocusedElement(sut.XamlRoot!));
	}

	[TestMethod]
	public async Task When_Items_Change_While_Open_Then_Panel_Updates_In_Place()
	{
		ObservableCollection<string> source = new(CreateItems(3));
		ComboBox sut = new() { ItemsSource = source };
		await UITestHelper.Load(sut);

		try
		{
			var (_, panel, _) = await Open(sut);
			var first = sut.ContainerFromIndex(0);

			source.Add("Added");
			await WindowHelper.WaitForIdle();
			Assert.IsNotNull(sut.ContainerFromItem("Added"));
			Assert.AreSame(first, sut.ContainerFromIndex(0), "An insert should not reset the realized containers");
			Assert.HasCount(4, panel.Children.OfType<ComboBoxItem>().Where(c => c.Visibility == Visibility.Visible && sut.IndexFromContainer(c) >= 0).ToArray());

			source.RemoveAt(0);
			await WindowHelper.WaitForIdle();
			Assert.IsNull(sut.ContainerFromItem("Item 0"));
			Assert.AreEqual(3, sut.Items.Count);
		}
		finally
		{
			sut.IsDropDownOpen = false;
		}
	}

	[TestMethod]
	public async Task When_ItemTemplate_Changes_Then_Containers_Are_Refreshed()
	{
		ComboBox sut = new() { ItemsSource = CreateItems(3) };
		await UITestHelper.Load(sut);

		try
		{
			await Open(sut);

			sut.DisplayMemberPath = "Length";
			await WindowHelper.WaitForIdle();

			var container = (ComboBoxItem)sut.ContainerFromIndex(0);
			Assert.IsNotNull(container);
			Assert.IsTrue(container.FindFirstDescendant<TextBlock>()?.Text == "6", "DisplayMemberPath should apply to realized containers");
		}
		finally
		{
			sut.IsDropDownOpen = false;
		}
	}

	[TestMethod]
	public async Task When_Source_Is_Grouped_Then_Items_Are_Flattened()
	{
		CollectionViewSource source = new()
		{
			Source = Enumerable.Range(0, 30).GroupBy(x => x / 10).ToArray(),
			IsSourceGrouped = true,
		};
		ComboBox sut = new() { ItemsSource = source.View };
		await UITestHelper.Load(sut);

		try
		{
			var (_, panel, _) = await Open(sut);

			Assert.AreEqual(30, sut.Items.Count);
			Assert.IsNotNull(sut.ContainerFromIndex(0));
			Assert.IsGreaterThan(0, panel.Children.OfType<ComboBoxItem>().Count());
		}
		finally
		{
			sut.IsDropDownOpen = false;
		}
	}

	[TestMethod]
	public async Task When_Editable_Then_Selected_Item_Is_Shown_And_TextBox_Keeps_Focus()
	{
		ComboBox sut = new() { ItemsSource = CreateItems(100), SelectedIndex = 80, IsEditable = true };
		await UITestHelper.Load(sut);

		try
		{
			sut.Focus(FocusState.Programmatic);
			var (_, _, scrollViewer) = await Open(sut);

			Assert.IsTrue(IsInViewport(sut, scrollViewer, 80), $"offset={scrollViewer.VerticalOffset}");
			Assert.IsInstanceOfType<TextBox>(FocusManager.GetFocusedElement(sut.XamlRoot!));
		}
		finally
		{
			sut.IsDropDownOpen = false;
		}
	}

	[TestMethod]
	public async Task When_Custom_ItemsPanel_Then_ComboBox_Manages_Children()
	{
		ComboBox sut = new()
		{
			ItemsSource = CreateItems(5),
			SelectedIndex = 2,
			ItemsPanel = (ItemsPanelTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
				"""
				<ItemsPanelTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
					<StackPanel />
				</ItemsPanelTemplate>
				"""),
		};
		await UITestHelper.Load(sut);

		try
		{
			sut.IsDropDownOpen = true;
			await WindowHelper.WaitForIdle();

			Assert.IsInstanceOfType<StackPanel>(sut.ItemsPanelRoot);
			Assert.HasCount(5, sut.ItemsPanelRoot.Children);
			Assert.AreEqual(sut.ContainerFromIndex(2), FocusManager.GetFocusedElement(sut.XamlRoot!));
		}
		finally
		{
			sut.IsDropDownOpen = false;
		}
	}
}
