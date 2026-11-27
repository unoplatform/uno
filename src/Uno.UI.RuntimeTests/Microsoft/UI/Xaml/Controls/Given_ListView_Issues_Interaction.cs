#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
#if HAS_UNO
using Uno.UI.DevTools.Input;
using Windows.UI.Input.Preview.Injection;
#endif
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_ListView_Issues_Interaction
{
	[TestCleanup]
	public void Cleanup() => WindowHelper.WindowContent = null;

	private static DataTemplate TextTemplate() => (DataTemplate)XamlReader.Load(
		"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding}' Height='30' /></DataTemplate>");

	private static ScrollViewer GetScrollViewer(ListViewBase list) => (ScrollViewer)list.GetTemplateChild("ScrollViewer");

	private static IEnumerable<T> Descendants<T>(DependencyObject root)
		where T : DependencyObject
	{
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(root, i);
			if (child is T match)
			{
				yield return match;
			}

			foreach (T nested in Descendants<T>(child))
			{
				yield return nested;
			}
		}
	}

	public sealed class Item
	{
		public Item(string name) => Name = name;

		public string Name { get; }
	}

	// https://github.com/unoplatform/uno/issues/4293
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/4293")]
	public async Task When_Grouped_Source_Is_List_Of_Lists()
	{
		List<List<Item>> groups = new()
		{
			new() { new("1:1"), new("1:2"), new("1:3") },
			new() { new("2:1"), new("2:2"), new("2:3") },
			new() { new("3:1"), new("3:2") },
		};

		CollectionViewSource cvs = new() { IsSourceGrouped = true, Source = groups };
		ListView list = new()
		{
			Width = 300,
			Height = 500,
			ItemTemplate = (DataTemplate)XamlReader.Load(
				"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding Name}' Height='30' /></DataTemplate>"),
			ItemsSource = cvs.View,
		};
		list.GroupStyle.Add(new GroupStyle
		{
			HeaderTemplate = (DataTemplate)XamlReader.Load(
				"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding Count}' Height='30' /></DataTemplate>"),
		});

		try
		{
			WindowHelper.WindowContent = list;
			await WindowHelper.WaitForLoaded(list);
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitFor(() => list.ContainerFromIndex(7) is not null, message: "last item container realized");

			int total = groups.Sum(g => g.Count);
			Assert.AreEqual(8, total);

			for (int i = 0; i < total; i++)
			{
				var container = list.ContainerFromIndex(i) as ListViewItem;
				Assert.IsNotNull(container, $"container {i}");
				Item? expected = list.Items[i] as Item;
				Assert.IsNotNull(expected, $"Items[{i}] should be the Item, not a group");
				Assert.AreSame(expected, container!.DataContext, $"DataContext of container {i}");
				Assert.AreEqual(expected!.Name, Descendants<TextBlock>(container).First().Text, $"text of container {i}");
			}

			var headers = Descendants<ListViewHeaderItem>(list).ToList();
			Assert.HasCount(3, headers, "group headers realized");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	// https://github.com/unoplatform/uno/issues/9392
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/9392")]
	public async Task When_Arrow_Down_Past_Viewport_Keeps_Navigating()
	{
		ListView list = new()
		{
			Width = 300,
			Height = 400,
			ItemTemplate = TextTemplate(),
			ItemsSource = Enumerable.Range(0, 500).Select(i => $"Item {i}").ToList(),
		};

		WindowHelper.WindowContent = list;
		await WindowHelper.WaitForLoaded(list);
		await WindowHelper.WaitForIdle();

		var first = (ListViewItem)list.ContainerFromIndex(0);
		Assert.IsTrue(first.Focus(FocusState.Keyboard));

		for (int i = 0; i < 200; i++)
		{
			await KeyboardHelper.Down();
			await WindowHelper.WaitForIdle();
		}

		var focused = FocusManager.GetFocusedElement(list.XamlRoot!) as ListViewItem;
		Assert.IsNotNull(focused, "a ListViewItem should keep the focus");
		Assert.AreEqual(200, list.IndexFromContainer(focused!), "focused index after 200 Down presses");

		Rect bounds = focused!.TransformToVisual(list).TransformBounds(new Rect(0, 0, focused.ActualWidth, focused.ActualHeight));
		Assert.IsGreaterThanOrEqualTo(-1.0, bounds.Top, "focused item scrolled into view (top)");
		Assert.IsLessThanOrEqualTo(list.ActualHeight + 1.0, bounds.Bottom, "focused item scrolled into view (bottom)");
	}

	// https://github.com/unoplatform/uno/issues/24919
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24919")]
	[DataRow("GamepadRightShoulder", true)]
	[DataRow("GamepadRightTrigger", false)]
	public async Task When_Gamepad_Paging_Key_Pressed_Focus_Moves_Forward(string key, bool horizontal)
	{
		// WinUI: triggers page vertical lists, shoulders page horizontal lists.
		ListView list = new()
		{
			Width = horizontal ? 400 : 300,
			Height = horizontal ? 100 : 400,
			ItemTemplate = horizontal
				? (DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding}' Width='100' /></DataTemplate>")
				: TextTemplate(),
			ItemsSource = Enumerable.Range(0, 100).Select(i => $"Item {i}").ToList(),
		};
		if (horizontal)
		{
			list.ItemsPanel = (ItemsPanelTemplate)XamlReader.Load(
				"<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><ItemsStackPanel Orientation='Horizontal' /></ItemsPanelTemplate>");
			ScrollViewer.SetHorizontalScrollMode(list, ScrollMode.Enabled);
			ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Auto);
			ScrollViewer.SetVerticalScrollMode(list, ScrollMode.Disabled);
			ScrollViewer.SetVerticalScrollBarVisibility(list, ScrollBarVisibility.Disabled);
		}

		WindowHelper.WindowContent = list;
		await WindowHelper.WaitForLoaded(list);
		await WindowHelper.WaitForIdle();

		var first = (ListViewItem)list.ContainerFromIndex(1);
		list.SelectedIndex = 1;
		Assert.IsTrue(first.Focus(FocusState.Keyboard));
		await WindowHelper.WaitForIdle();

		await KeyboardHelper.PressKeySequence($"$d$_{key}#$u$_{key}");
		await WindowHelper.WaitForIdle();

		var focused = FocusManager.GetFocusedElement(list.XamlRoot!) as ListViewItem;
		Assert.IsNotNull(focused);
		int index = list.IndexFromContainer(focused!);
		Assert.IsGreaterThan(1, index, $"{key} should page focus forward");
	}

	private sealed class IncrementalSource : ObservableCollection<string>, ISupportIncrementalLoading
	{
		private readonly DispatcherQueue _queue = DispatcherQueue.GetForCurrentThread();

		public IncrementalSource(int initial, int total)
		{
			Total = total;
			for (int i = 0; i < initial; i++)
			{
				Add($"Item {i}");
			}
		}

		public int Total { get; }

		public bool HasMoreItems => Count < Total;

		public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
		{
			TaskCompletionSource<LoadMoreItemsResult> tcs = new();
			_queue.TryEnqueue(() =>
			{
				int toAdd = Math.Min((int)count, Total - Count);
				for (int i = 0; i < toAdd; i++)
				{
					Add($"Item {Count}");
				}

				tcs.SetResult(new LoadMoreItemsResult { Count = (uint)toAdd });
			});
			return AsyncInfo.Run(_ => tcs.Task);
		}
	}

	// https://github.com/unoplatform/uno/issues/20784
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/20784")]
	public async Task When_GridView_Scrolled_To_End_Loads_More_Items()
	{
		IncrementalSource source = new(12, 1000);
		GridView grid = new()
		{
			Width = 400,
			Height = 300,
			ItemTemplate = (DataTemplate)XamlReader.Load(
				"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Border Width='128' Height='128'><TextBlock Text='{Binding}' /></Border></DataTemplate>"),
			ItemsSource = source,
		};

		WindowHelper.WindowContent = grid;
		await WindowHelper.WaitForLoaded(grid);
		await WindowHelper.WaitForIdle();

		int last = -1;
		int stable = 0;
		for (int i = 0; i < 100 && stable < 5; i++)
		{
			await WindowHelper.WaitForIdle();
			stable = source.Count == last ? stable + 1 : 0;
			last = source.Count;
		}

		int before = source.Count;
		ScrollViewer sv = GetScrollViewer(grid);
		sv.ChangeView(null, sv.ScrollableHeight, null, true);
		await WindowHelper.WaitForIdle();
		await WindowHelper.WaitFor(() => source.Count > before, timeoutMS: 3000, message: "more items loaded after scrolling to the end");
	}

	private static Rect BoundsInWindow(FrameworkElement element)
		=> element.TransformToVisual(null).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));

	private static Point CenterInWindow(FrameworkElement element)
	{
		Rect r = BoundsInWindow(element);
		return new Point(r.X + r.Width / 2, r.Y + r.Height / 2);
	}

	// https://github.com/unoplatform/uno/issues/20966
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/20966")]
	public async Task When_Header_Reset_To_Null_Layout_Updates()
	{
		ListView list = new()
		{
			Width = 300,
			Height = 400,
			ItemTemplate = TextTemplate(),
			ItemsSource = Enumerable.Range(0, 10).Select(i => $"Item {i}").ToList(),
			Header = new Border { Height = 100 },
		};

		WindowHelper.WindowContent = list;
		await WindowHelper.WaitForLoaded(list);
		await WindowHelper.WaitForIdle();

		double FirstItemY() => ((FrameworkElement)list.ContainerFromIndex(0)).TransformToVisual(list).TransformPoint(new Point(0, 0)).Y;

		double withHeader = FirstItemY();

		list.Header = null;
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(withHeader - 100, FirstItemY(), 1.0, "first item moves up by the header height");

		list.Header = new Border { Height = 100 };
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(withHeader, FirstItemY(), 1.0, "first item returns when header is restored");
	}

#if HAS_UNO
	// https://github.com/unoplatform/uno/issues/11601
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/11601")]
	public async Task When_GridView_CanReorderItems_Mouse_Drag_Reorders()
	{
		ObservableCollection<string> source = new(Enumerable.Range(0, 8).Select(i => i.ToString()));
		GridView grid = new()
		{
			Width = 440,
			Height = 300,
			AllowDrop = true,
			CanDragItems = true,
			CanReorderItems = true,
			ItemTemplate = (DataTemplate)XamlReader.Load(
				"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Border Width='100' Height='100'><TextBlock Text='{Binding}' /></Border></DataTemplate>"),
			ItemsSource = source,
		};

		await UITestHelper.Load(grid, x => x.IsLoaded && grid.ContainerFromIndex(3) is not null);
		await WindowHelper.WaitForIdle();

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();

		await Task.Delay(800); // let the entrance transitions settle
		await WindowHelper.WaitForIdle();

		Point from = CenterInWindow((FrameworkElement)grid.ContainerFromIndex(0));
		Point to = CenterInWindow((FrameworkElement)grid.ContainerFromIndex(3));

		mouse.MoveTo(from);
		await WindowHelper.WaitForIdle();
		mouse.Press();
		await WindowHelper.WaitForIdle();
		mouse.MoveTo(new Point(from.X + (to.X - from.X) / 2, from.Y));
		await WindowHelper.WaitForIdle();
		mouse.MoveTo(to);
		await WindowHelper.WaitForIdle();
		mouse.Release();
		await WindowHelper.WaitForIdle();

		Assert.AreNotEqual(0, source.IndexOf("0"), "dragged item moved away from the front");
		Assert.AreEqual("1", source[0]);
	}

	// https://github.com/unoplatform/uno/issues/25282
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25282")]
	public async Task When_Reorder_Item_Content_AllowDrop_DropResult_Is_Move()
	{
		ObservableCollection<string> source = new(Enumerable.Range(0, 3).Select(i => i.ToString()));
		ListView list = new()
		{
			Width = 300,
			Height = 400,
			AllowDrop = true,
			CanDragItems = true,
			CanReorderItems = true,
			ItemTemplate = (DataTemplate)XamlReader.Load(
				"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding}' Height='100' AllowDrop='True' /></DataTemplate>"),
			ItemsSource = source,
		};

		DataPackageOperation? result = null;
		list.DragItemsCompleted += (_, e) => result = e.DropResult;

		await UITestHelper.Load(list, x => x.IsLoaded && list.ContainerFromIndex(2) is not null);
		await WindowHelper.WaitForIdle();

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();

		await Task.Delay(800); // let the entrance transitions settle
		await WindowHelper.WaitForIdle();

		Point from = CenterInWindow((FrameworkElement)list.ContainerFromIndex(0));
		Point to = CenterInWindow((FrameworkElement)list.ContainerFromIndex(2));

		mouse.MoveTo(from);
		await WindowHelper.WaitForIdle();
		mouse.Press();
		await WindowHelper.WaitForIdle();
		mouse.MoveTo(new Point(from.X, from.Y + 100));
		await WindowHelper.WaitForIdle();
		mouse.MoveTo(to);
		await WindowHelper.WaitForIdle();
		mouse.Release();
		await WindowHelper.WaitForIdle();
		await WindowHelper.WaitFor(() => result is not null, timeoutMS: 3000, message: "DragItemsCompleted raised");

		Assert.AreEqual(DataPackageOperation.Move, result, "DropResult");
		Assert.AreNotEqual("0", source[0], "order changed");
	}
#endif

#if HAS_UNO
	private sealed class FactoryTemplateSelector : DataTemplateSelector
	{
		protected override DataTemplate SelectTemplateCore(object item)
		{
			string text = (string)item;
			UIElement view = new TextBlock { Text = text, Height = 30 };
			return new DataTemplate(null, (_, _) => view);
		}
	}

	// https://github.com/unoplatform/uno/issues/17461
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/17461")]
	public async Task When_DataTemplateSelector_Returns_Factory_Template_Rows_Keep_Their_Data()
	{
		ListView list = new()
		{
			Width = 300,
			Height = 300,
			ItemTemplateSelector = new FactoryTemplateSelector(),
			ItemsSource = Enumerable.Range(0, 200).Select(i => $"Item {i}").ToList(),
		};

		WindowHelper.WindowContent = list;
		await WindowHelper.WaitForLoaded(list);
		await WindowHelper.WaitForIdle();

		var sv = GetScrollViewer(list);
		foreach (double offset in new[] { 1500.0, 3000.0, 600.0, 0.0, 4500.0, 1200.0 })
		{
			sv.ChangeView(null, offset, null, true);
			await WindowHelper.WaitFor(() => Math.Abs(sv.VerticalOffset - offset) < 1.0, timeoutMS: 3000, message: $"scrolled to {offset}");
			await WindowHelper.WaitForIdle();

			int checkedCount = 0;
			foreach (ListViewItem container in Descendants<ListViewItem>(list))
			{
				int index = list.IndexFromContainer(container);
				if (index < 0)
				{
					continue;
				}

				TextBlock? tb = Descendants<TextBlock>(container).FirstOrDefault();
				Assert.IsNotNull(tb, $"container {index} has content");
				Assert.AreEqual($"Item {index}", tb!.Text, $"container {index} at offset {offset}");
				checkedCount++;
			}

			Assert.IsGreaterThan(0, checkedCount, $"containers realized at offset {offset}");
		}
	}
#endif
}
