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
}
