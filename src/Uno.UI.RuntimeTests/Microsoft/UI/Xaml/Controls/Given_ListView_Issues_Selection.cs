#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
#if HAS_UNO
using Uno.UI.DevTools.Input;
#endif
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class DataContextProbe : Microsoft.UI.Xaml.Controls.Border
{
	public static List<object?> Log { get; } = new();

	public DataContextProbe()
	{
		DataContextChanged += (s, e) => Log.Add(e.NewValue);
	}
}

[TestClass]
public class Given_ListView_Issues_Selection
{
	private static ObservableCollection<string> MakeItems(int count)
		=> new(Enumerable.Range(0, count).Select(i => $"I{i}"));

	private static string Format(SelectionChangedEventArgs e)
		=> $"+[{string.Join(",", e.AddedItems)}] -[{string.Join(",", e.RemovedItems)}]";

	private static async Task<T> Host<T>(T control) where T : FrameworkElement
	{
		WindowHelper.WindowContent = control;
		await WindowHelper.WaitForLoaded(control);
		await WindowHelper.WaitForIdle();
		return control;
	}

	private static ItemsControl CreateItemsControlWithItemsStackPanel(Orientation orientation, IEnumerable<string> items, double size = 300)
		=> new()
		{
			Width = size,
			Height = size,
			ItemsSource = items.ToList(),
			ItemsPanel = (ItemsPanelTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
				$"<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><ItemsStackPanel Orientation='{orientation}'/></ItemsPanelTemplate>"),
		};

	private static IEnumerable<Func<ObservableCollection<string>, ListViewBase>> ListViewBaseFactories(ListViewSelectionMode mode)
	{
		yield return items => new ListView { Width = 200, Height = 400, SelectionMode = mode, ItemsSource = items };
		yield return items => new GridView { Width = 400, Height = 400, SelectionMode = mode, ItemsSource = items };
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/20540")]
	public async Task When_MultiSelect_Reselect_First_Keeps_Second_Selected()
	{
		try
		{
			var items = MakeItems(4);
			var lv = await Host(new ListView { Width = 200, Height = 400, SelectionMode = ListViewSelectionMode.Multiple, ItemsSource = items });

			foreach (var item in items)
			{
				lv.SelectedItems.Add(item);
			}
			await WindowHelper.WaitForIdle();

			lv.SelectedItems.Remove(items[0]);
			await WindowHelper.WaitForIdle();
			lv.SelectedItems.Add(items[0]);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(4, lv.SelectedItems.Count);
			for (var i = 0; i < 4; i++)
			{
				var container = (ListViewItem)lv.ContainerFromIndex(i);
				Assert.IsTrue(container.IsSelected, $"container {i} should be selected");
			}

			Assert.AreEqual(1, lv.SelectedRanges.Count);
			Assert.AreEqual(0, lv.SelectedRanges[0].FirstIndex);
			Assert.AreEqual(3, (int)lv.SelectedRanges[0].LastIndex);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/20540")]
	public async Task When_MultiSelect_Range_Deselect_Then_Reselect_First()
	{
		try
		{
			var items = MakeItems(4);
			var lv = await Host(new ListView { Width = 200, Height = 400, SelectionMode = ListViewSelectionMode.Multiple, ItemsSource = items });

			lv.SelectRange(new ItemIndexRange(0, 4));
			await WindowHelper.WaitForIdle();
			lv.DeselectRange(new ItemIndexRange(0, 1));
			await WindowHelper.WaitForIdle();
			lv.SelectRange(new ItemIndexRange(0, 1));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(4, lv.SelectedItems.Count);
			Assert.AreEqual(1, lv.SelectedRanges.Count);
			Assert.AreEqual(0, lv.SelectedRanges[0].FirstIndex);
			Assert.AreEqual(3, (int)lv.SelectedRanges[0].LastIndex);
			for (var i = 0; i < 4; i++)
			{
				Assert.IsTrue(((ListViewItem)lv.ContainerFromIndex(i)).IsSelected, $"container {i}");
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26382")]
	public async Task When_Multiple_SelectedIndex_Raises_Single_SelectionChanged()
	{
		try
		{
			foreach (var mode in new[] { ListViewSelectionMode.Multiple, ListViewSelectionMode.Extended })
			{
				foreach (var factory in ListViewBaseFactories(mode))
				{
					var items = MakeItems(10);
					var lv = await Host(factory(items));
					List<string> log = new();
					lv.SelectionChanged += (s, e) => log.Add(Format(e));

					lv.SelectedIndex = 4;
					await WindowHelper.WaitForIdle();

					CollectionAssert.AreEqual(new[] { "+[I4] -[]" }, log, $"{lv.GetType().Name} {mode}");
				}
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26382")]
	public async Task When_Extended_SelectedIndex_Replaces_Selection_With_Single_Event()
	{
		try
		{
			foreach (var factory in ListViewBaseFactories(ListViewSelectionMode.Extended))
			{
				var items = MakeItems(10);
				var lv = await Host(factory(items));
				lv.SelectedItems.Add(items[2]);
				lv.SelectedItems.Add(items[4]);
				await WindowHelper.WaitForIdle();

				List<string> log = new();
				lv.SelectionChanged += (s, e) => log.Add(Format(e));
				lv.SelectedIndex = 7;
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(1, log.Count, string.Join(" | ", log));
				Assert.AreEqual("+[I7] -[I2,I4]", log[0]);
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26382")]
	public async Task When_Multiple_SelectedItem_Set_Updates_SelectedIndex()
	{
		try
		{
			foreach (var factory in ListViewBaseFactories(ListViewSelectionMode.Multiple))
			{
				var items = MakeItems(10);
				var lv = await Host(factory(items));

				lv.SelectedIndex = 6;
				await WindowHelper.WaitForIdle();
				lv.SelectedItem = items[8];
				await WindowHelper.WaitForIdle();

				Assert.AreEqual("I8", lv.SelectedItem);
				Assert.AreEqual(8, lv.SelectedIndex, lv.GetType().Name);
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26382")]
	public async Task When_Multiple_Selected_Item_Removed_Only_That_Item_Is_Deselected()
	{
		try
		{
			foreach (var factory in ListViewBaseFactories(ListViewSelectionMode.Multiple))
			{
				var items = MakeItems(10);
				var lv = await Host(factory(items));
				lv.SelectedItems.Add(items[2]);
				lv.SelectedItems.Add(items[5]);
				await WindowHelper.WaitForIdle();

				List<string> log = new();
				lv.SelectionChanged += (s, e) => log.Add(Format(e));
				items.RemoveAt(2);
				await WindowHelper.WaitForIdle();

				CollectionAssert.AreEqual(new[] { "+[] -[I2]" }, log, lv.GetType().Name);
				Assert.AreEqual(4, lv.SelectedIndex);
				Assert.AreEqual("I5", lv.SelectedItem);
				CollectionAssert.AreEqual(new object[] { "I5" }, lv.SelectedItems.ToArray());
				Assert.IsTrue(((SelectorItem)lv.ContainerFromIndex(4)).IsSelected);
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26382")]
	public async Task When_Multiple_Removed_Item_Does_Not_Stay_In_SelectedItems()
	{
		try
		{
			foreach (var factory in ListViewBaseFactories(ListViewSelectionMode.Multiple))
			{
				var items = MakeItems(10);
				var lv = await Host(factory(items));
				lv.SelectedItems.Add(items[2]);
				lv.SelectedItems.Add(items[3]);
				lv.SelectedItems.Add(items[6]);
				await WindowHelper.WaitForIdle();

				List<string> log = new();
				lv.SelectionChanged += (s, e) => log.Add(Format(e));
				items.RemoveAt(3);
				await WindowHelper.WaitForIdle();

				CollectionAssert.AreEqual(new[] { "+[] -[I3]" }, log, lv.GetType().Name);
				CollectionAssert.AreEquivalent(new object[] { "I2", "I6" }, lv.SelectedItems.ToArray());
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static IEnumerable<(string Name, Func<ObservableCollection<string>, Selector> Create, int Index)> SelectorFactories()
	{
		yield return ("ListView", items => new ListView { Width = 200, Height = 400, ItemsSource = items }, 3);
		yield return ("GridView", items => new GridView { Width = 400, Height = 400, ItemsSource = items }, 3);
		yield return ("ComboBox", items => new ComboBox { Width = 200, ItemsSource = items }, 3);
		yield return ("ListBox", items => new ListBox { Width = 200, Height = 400, ItemsSource = items }, 3);
		yield return ("FlipView", items => new FlipView { Width = 200, Height = 200, ItemsSource = items }, 2);
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26383")]
	public async Task When_Item_Inserted_Or_Removed_Before_Selection_No_SelectionChanged()
	{
		try
		{
			foreach (var (name, create, index) in SelectorFactories())
			{
				var items = MakeItems(6);
				var selector = await Host(create(items));
				selector.SelectedIndex = index;
				await WindowHelper.WaitForIdle();

				List<string> log = new();
				selector.SelectionChanged += (s, e) => log.Add(Format(e));

				items.Insert(0, "I90");
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(0, log.Count, $"{name} insert: {string.Join(" | ", log)}");
				Assert.AreEqual(index + 1, selector.SelectedIndex, $"{name} insert index");

				items.RemoveAt(0);
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(0, log.Count, $"{name} remove: {string.Join(" | ", log)}");
				Assert.AreEqual(index, selector.SelectedIndex, $"{name} remove index");
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

#if HAS_UNO
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/10115")]
	public async Task When_Touch_Long_Press_Item_Does_Not_Select_Or_Click()
	{
		try
		{
			var items = MakeItems(4);
			var lv = await Host(new ListView { Width = 200, Height = 400, IsItemClickEnabled = true, ItemsSource = items });
			var itemClicks = 0;
			var holdings = 0;
			lv.ItemClick += (s, e) => itemClicks++;
			lv.Holding += (s, e) => holdings++;

			var injector = Windows.UI.Input.Preview.Injection.InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			var target = ((FrameworkElement)lv.ContainerFromIndex(1)).GetAbsoluteCenter();
			finger.Press(target);
			await WindowHelper.WaitFor(() => holdings > 0, timeoutMS: 5000);
			finger.Release();
			await WindowHelper.WaitForIdle();

			Assert.IsNull(lv.SelectedItem);
			Assert.AreEqual(-1, lv.SelectedIndex);
			Assert.AreEqual(0, itemClicks);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
#endif

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25514")]
	public async Task When_ChoosingItemContainer_Container_Is_Used()
	{
		try
		{
			var items = Enumerable.Range(0, 10).ToList();
			Dictionary<int, GridViewItem> supplied = new();
			List<object?> chosenItems = new();
			GridView gv = new() { Width = 400, Height = 400, ItemsSource = items };
			gv.ChoosingItemContainer += (s, e) =>
			{
				chosenItems.Add(e.Item);
				if (e.ItemContainer == null)
				{
					GridViewItem container = new();
					supplied[e.ItemIndex] = container;
					e.ItemContainer = container;
				}
			};
			await Host(gv);

			Assert.IsTrue(supplied.Count > 0, "ChoosingItemContainer should be raised");
			Assert.IsTrue(chosenItems.Contains(items[0]));
			foreach (var (index, container) in supplied)
			{
				Assert.AreSame(container, gv.ContainerFromIndex(index), $"index {index}");
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/10786")]
	public async Task When_ItemContainerStyle_Set_In_BasedOn_Style_After_Template()
	{
		try
		{
			var root = (StackPanel)Microsoft.UI.Xaml.Markup.XamlReader.Load(
				"""
				<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
					<StackPanel.Resources>
						<Style x:Key="ItemStyle" TargetType="ListBoxItem">
							<Setter Property="Background" Value="Blue" />
							<Setter Property="BorderThickness" Value="5" />
							<Setter Property="Template">
								<Setter.Value>
									<ControlTemplate TargetType="ListBoxItem">
										<Grid Background="{TemplateBinding Background}"><ContentPresenter Content="{TemplateBinding Content}" /></Grid>
									</ControlTemplate>
								</Setter.Value>
							</Setter>
						</Style>
						<Style x:Key="BaseStyle" TargetType="ListBox">
							<Setter Property="Template">
								<Setter.Value>
									<ControlTemplate TargetType="ListBox"><Grid><ItemsPresenter /></Grid></ControlTemplate>
								</Setter.Value>
							</Setter>
						</Style>
						<Style x:Key="DerivedStyle" TargetType="ListBox" BasedOn="{StaticResource BaseStyle}">
							<Setter Property="ItemContainerStyle" Value="{StaticResource ItemStyle}" />
						</Style>
					</StackPanel.Resources>
					<ListBox x:Name="Lb" Style="{StaticResource DerivedStyle}">
						<ListBoxItem Content="One" />
						<ListBoxItem Content="Two" />
					</ListBox>
				</StackPanel>
				""");
			await Host(root);
			var lb = (ListBox)root.FindName("Lb");

			for (var i = 0; i < 2; i++)
			{
				var item = (ListBoxItem)lb.ContainerFromIndex(i);
				var brush = item.Background as SolidColorBrush;
				Assert.IsNotNull(brush, $"item {i} background");
				Assert.AreEqual(Microsoft.UI.Colors.Blue, brush!.Color, $"item {i}");
				Assert.AreEqual(5d, item.BorderThickness.Left, $"item {i}");
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	public class TitleViewModel
	{
		public string Title { get; set; } = "Hello from the VM";
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/16531")]
	public async Task When_ItemsStackPanel_In_ItemsControl_Horizontal_Items_Are_Laid_Out_Left_To_Right()
	{
		try
		{
			var ic = await Host(CreateItemsControlWithItemsStackPanel(Orientation.Horizontal, new[] { "Item 1", "Item 2", "Item 3" }));

			var panel = (ItemsStackPanel)ic.ItemsPanelRoot;
			Assert.AreEqual(3, panel.Children.Count);
			double previousRight = -1;
			foreach (FrameworkElement child in panel.Children)
			{
				Assert.IsTrue(child.ActualWidth > 0, $"width {child.ActualWidth}");
				Assert.IsTrue(child.ActualHeight > 0, $"height {child.ActualHeight}");
				var x = child.TransformToVisual(panel).TransformPoint(default).X;
				Assert.IsTrue(x >= previousRight - 0.5, $"child at {x} overlaps previous ending at {previousRight}");
				previousRight = x + child.ActualWidth;
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25458")]
	public async Task When_ItemsStackPanel_In_ItemsControl_Creates_One_Container_Per_Item()
	{
		try
		{
			foreach (var orientation in new[] { Orientation.Horizontal, Orientation.Vertical })
			{
				var ic = await Host(CreateItemsControlWithItemsStackPanel(orientation, new[] { "A", "B", "C", "D", "E", "F" }));

				var panel = (ItemsStackPanel)ic.ItemsPanelRoot;
				Assert.AreEqual(6, panel.Children.Count, orientation.ToString());

				double previous = -1;
				foreach (FrameworkElement child in panel.Children)
				{
					var origin = child.TransformToVisual(panel).TransformPoint(default);
					var offset = orientation == Orientation.Horizontal ? origin.X : origin.Y;
					Assert.IsTrue(offset > previous, $"{orientation}: offset {offset} should advance past {previous}");
					previous = offset;
				}
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/4597")]
	public async Task When_ItemsControl_Item_Content_DataContext_Is_Set_Only_To_Item()
	{
		try
		{
			DataContextProbe.Log.Clear();
			var ic = (ItemsControl)Microsoft.UI.Xaml.Markup.XamlReader.Load(
				"""
				<ItemsControl xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:l="using:Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls">
					<ItemsControl.ItemTemplate>
						<DataTemplate>
							<l:DataContextProbe><TextBlock Text="{Binding}" /></l:DataContextProbe>
						</DataTemplate>
					</ItemsControl.ItemTemplate>
				</ItemsControl>
				""");
			ic.DataContext = "parent";
			ic.ItemsSource = new[] { "item" };
			await Host(ic);

			CollectionAssert.AreEqual(new[] { "item" }, DataContextProbe.Log.Where(v => v != null).ToArray(), string.Join(" > ", DataContextProbe.Log.Select(v => v ?? "<null>")));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26342")]
	public async Task When_Custom_ListViewItem_Style_BasedOn_Default_Selection_Is_Visible()
	{
		try
		{
			var root = (StackPanel)Microsoft.UI.Xaml.Markup.XamlReader.Load(
				"""
				<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
					<StackPanel.Resources>
						<Style x:Key="CompactListItemStyle" TargetType="ListViewItem" BasedOn="{StaticResource DefaultListViewItemStyle}">
							<Setter Property="MinHeight" Value="26" />
						</Style>
					</StackPanel.Resources>
					<ListView x:Name="Plain" Width="300" SelectionMode="Single">
						<ListViewItem Content="Item 1" />
						<ListViewItem Content="Item 2" />
						<ListViewItem Content="Item 3" />
					</ListView>
					<ListView x:Name="Lv" Width="300" SelectionMode="Single" ItemContainerStyle="{StaticResource CompactListItemStyle}">
						<ListViewItem Content="Item 1" />
						<ListViewItem Content="Item 2" />
						<ListViewItem Content="Item 3" />
					</ListView>
				</StackPanel>
				""");
			await Host(root);
			var lv = (ListView)root.FindName("Lv");

			var plain = (ListView)root.FindName("Plain");
			plain.SelectedIndex = 1;
			lv.SelectedIndex = 1;
			await WindowHelper.WaitForIdle();
			await UITestHelper.WaitForIdle(waitForCompositionAnimations: true);

			Assert.IsTrue(await SelectionIsPainted(plain, 1, 2), "control: default style shows selection");
			Assert.IsTrue(HasMinHeight(lv), "MinHeight should be applied");
			Assert.IsTrue(await SelectionIsPainted(lv, 1, 2), "Selected item should have a different background than an unselected one");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static bool HasMinHeight(ListView lv) => ((FrameworkElement)lv.ContainerFromIndex(1)).ActualHeight >= 26;

	private static async Task<bool> SelectionIsPainted(ListView lv, int selectedIndex, int unselectedIndex)
	{
		for (var attempt = 0; attempt < 40; attempt++)
		{
			var selected = await UITestHelper.ScreenShot((FrameworkElement)lv.ContainerFromIndex(selectedIndex), opaque: true);
			var unselected = await UITestHelper.ScreenShot((FrameworkElement)lv.ContainerFromIndex(unselectedIndex), opaque: true);
			if (DiffersInBackground(selected, unselected))
			{
				return true;
			}

			await Task.Delay(50);
		}

		return false;
	}

	private static bool DiffersInBackground(RawBitmap a, RawBitmap b)
	{
		for (var y = 0; y < Math.Min(a.Height, b.Height); y++)
		{
			for (var x = 0; x < a.Width; x++)
			{
				var textArea = x > 20 && x < 100;
				if (!textArea && a.GetPixel(x, y) != b.GetPixel(x, y))
				{
					return true;
				}
			}
		}

		return false;
	}

	private static async Task<ListView> HostListViewWithFocusableChildren()
	{
		var template = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			"""
			<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
				<StackPanel Orientation="Horizontal">
					<Button Content="Click me" Width="100" Height="32" />
					<TextBox Width="100" Height="32" />
				</StackPanel>
			</DataTemplate>
			""");
		return await Host(new ListView { Width = 400, Height = 300, ItemTemplate = template, ItemsSource = MakeItems(3) });
	}

	private static T FindDescendant<T>(DependencyObject root) where T : DependencyObject
	{
		var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < count; i++)
		{
			var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);
			if (child is T match)
			{
				return match;
			}

			var nested = FindDescendantOrDefault<T>(child);
			if (nested != null)
			{
				return nested;
			}
		}

		throw new InvalidOperationException($"{typeof(T).Name} not found");
	}

	private static T? FindDescendantOrDefault<T>(DependencyObject root) where T : DependencyObject
	{
		var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < count; i++)
		{
			var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);
			if (child is T match)
			{
				return match;
			}

			var nested = FindDescendantOrDefault<T>(child);
			if (nested != null)
			{
				return nested;
			}
		}

		return null;
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19134")]
	public async Task When_Child_Of_ListViewItem_Focused_Programmatically_Keeps_Focus()
	{
		try
		{
			var lv = await HostListViewWithFocusableChildren();
			var container = (ListViewItem)lv.ContainerFromIndex(0);
			var button = FindDescendant<Button>(container);
			var textBox = FindDescendant<TextBox>(container);

			Assert.IsTrue(button.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			Assert.AreSame(button, FocusManager.GetFocusedElement(lv.XamlRoot!));

			Assert.IsTrue(textBox.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			Assert.AreSame(textBox, FocusManager.GetFocusedElement(lv.XamlRoot!));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

#if HAS_UNO
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19134")]
	public async Task When_Child_Of_ListViewItem_Pressed_With_Pointer_Child_Gets_Focus()
	{
		try
		{
			var lv = await HostListViewWithFocusableChildren();
			var container = (ListViewItem)lv.ContainerFromIndex(1);
			var textBox = FindDescendant<TextBox>(container);

			var injector = Windows.UI.Input.Preview.Injection.InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();
			mouse.Press(textBox.GetAbsoluteCenter());
			mouse.Release();
			await WindowHelper.WaitForIdle();

			Assert.AreSame(textBox, FocusManager.GetFocusedElement(lv.XamlRoot!));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
#endif

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25461")]
	public async Task When_GridView_Multiple_Default_Style_Uses_Overlay_Check_Mode()
	{
		try
		{
			var gv = await Host(new GridView
			{
				Width = 400,
				Height = 300,
				SelectionMode = ListViewSelectionMode.Multiple,
				ItemsSource = MakeItems(12),
			});
			gv.SelectedItems.Add(gv.Items[0]);
			gv.SelectedItems.Add(gv.Items[3]);
			await WindowHelper.WaitForIdle();

			var container = (GridViewItem)gv.ContainerFromIndex(0);
			var listPresenter = FindDescendantOrDefault<ListViewItemPresenter>(container);
			Assert.IsNotNull(listPresenter, "default GridViewItem template should use a ListViewItemPresenter");
			Assert.AreEqual(ListViewItemPresenterCheckMode.Overlay, listPresenter!.CheckMode);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26424")]
	public async Task When_Item_Inserted_Or_Moved_Panel_Children_Follow_Item_Order()
	{
		try
		{
			var items = MakeItems(10);
			var lv = await Host(new ListView { Width = 300, Height = 600, ItemsSource = items });

			string Order() => string.Join(",", lv.ItemsPanelRoot.Children.Select(c => lv.IndexFromContainer(c)));

			items.Insert(3, "new");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("0,1,2,3,4,5,6,7,8,9,10", Order(), "after Insert(3)");

			items.Move(1, 8);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("0,1,2,3,4,5,6,7,8,9,10", Order(), "after Move(1, 8)");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
