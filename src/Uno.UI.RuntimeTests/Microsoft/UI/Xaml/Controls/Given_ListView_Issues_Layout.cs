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
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_ListView_Issues_Layout
{
	private const double Tolerance = 1.0;

	public sealed class Group : ObservableCollection<string>
	{
		public Group(string key, int count)
			: base(Enumerable.Range(0, count).Select(i => $"{key}.{i}"))
		{
			Key = key;
		}

		public string Key { get; }

		public override string ToString() => Key;
	}

	public sealed class Row
	{
		public Row(int index, double height)
		{
			Index = index;
			Height = height;
		}

		public int Index { get; }

		public double Height { get; }
	}

	private sealed class CountingListView : ListView
	{
		public int MeasureCount { get; private set; }

		public int ArrangeCount { get; private set; }

		protected override Size MeasureOverride(Size availableSize)
		{
			MeasureCount++;
			return base.MeasureOverride(availableSize);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			ArrangeCount++;
			return base.ArrangeOverride(finalSize);
		}
	}

	private CollectionViewSource? _cvs;

	[TestCleanup]
	public void Cleanup() => WindowHelper.WindowContent = null;

	private static DataTemplate Template(string xaml) => (DataTemplate)XamlReader.Load(
		$"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>{xaml}</DataTemplate>");

	private static ItemsPanelTemplate PanelTemplate(string panelXaml) => (ItemsPanelTemplate)XamlReader.Load(
		$"<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>{panelXaml}</ItemsPanelTemplate>");

	private static List<string> Strings(int count) => Enumerable.Range(0, count).Select(i => $"Item {i}").ToList();

	private static ListView CreateNoCacheList(object source) => new()
	{
		Width = 300,
		Height = 400,
		ItemsSource = source,
		ItemTemplate = Template("<Border Height='40'><TextBlock Text='{Binding}' /></Border>"),
		ItemsPanel = PanelTemplate("<ItemsStackPanel CacheLength='0' />"),
	};

	private static async Task<T> Load<T>(T element)
		where T : FrameworkElement
	{
		WindowHelper.WindowContent = element;
		await WindowHelper.WaitForLoaded(element);
		await WindowHelper.WaitForIdle();
		await WindowHelper.WaitForIdle();
		return element;
	}

	private static ScrollViewer GetScrollViewer(ListViewBase list) => (ScrollViewer)list.GetTemplateChild("ScrollViewer");

	private static IEnumerable<T> Descendants<T>(DependencyObject root)
		where T : DependencyObject
	{
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (child is T t)
			{
				yield return t;
			}

			foreach (var d in Descendants<T>(child))
			{
				yield return d;
			}
		}
	}

	private static Rect BoundsIn(FrameworkElement element, FrameworkElement relativeTo) =>
		element.TransformToVisual(relativeTo).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));

	private static async Task ChangeViewAndWait(ScrollViewer sv, double offset)
	{
		sv.ChangeView(null, offset, null, true);
		await WindowHelper.WaitFor(() => Math.Abs(sv.VerticalOffset - offset) < Tolerance, timeoutMS: 3000, message: $"Scroll to {offset}");
		await WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/166")]
	public async Task When_ItemsStackPanel_VerticalAlignment_Bottom_And_Few_Items_Then_Items_Hug_Bottom()
	{
		ListView list = new()
		{
			Width = 300,
			Height = 400,
			ItemsSource = Strings(3),
			ItemTemplate = Template("<Border Height='40'><TextBlock Text='{Binding}' /></Border>"),
			ItemsPanel = PanelTemplate("<ItemsStackPanel VerticalAlignment='Bottom' ItemsUpdatingScrollMode='KeepLastItemInView' />"),
		};
		await Load(list);

		var last = (FrameworkElement)list.ContainerFromIndex(2);
		var first = (FrameworkElement)list.ContainerFromIndex(0);
		var panel = (FrameworkElement)list.ItemsPanelRoot;
		var panelBounds = BoundsIn(panel, list);

		Assert.AreEqual(panelBounds.Bottom, BoundsIn(last, list).Bottom, Tolerance, "Last item sits at the bottom of the panel");
		Assert.IsGreaterThan(BoundsIn(first, list).Y, BoundsIn(last, list).Y);
		Assert.IsGreaterThan(list.Height / 2, BoundsIn(first, list).Y, "Items are pushed down, not top aligned");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/4257")]
	public async Task When_KeepLastItemInView_And_Item_Appended_At_End_Then_Last_Item_Is_In_View()
	{
		ObservableCollection<string> items = new(Strings(30));
		ListView list = new()
		{
			Width = 300,
			Height = 400,
			ItemsSource = items,
			ItemTemplate = Template("<Border Height='40'><TextBlock Text='{Binding}' /></Border>"),
			ItemsPanel = PanelTemplate("<ItemsStackPanel VerticalAlignment='Bottom' ItemsUpdatingScrollMode='KeepLastItemInView' />"),
		};
		await Load(list);
		var sv = GetScrollViewer(list);
		await ChangeViewAndWait(sv, sv.ScrollableHeight);

		for (int i = 0; i < 3; i++)
		{
			items.Add($"New {i}");
			await WindowHelper.WaitForIdle();
		}

		await WindowHelper.WaitFor(() => list.ContainerFromIndex(items.Count - 1) is not null && Math.Abs(sv.VerticalOffset - sv.ScrollableHeight) < Tolerance, timeoutMS: 3000, message: "Scrolled to the newest item");

		var last = (FrameworkElement)list.ContainerFromIndex(items.Count - 1);
		var lastBounds = BoundsIn(last, list);
		Assert.AreEqual("New 2", ((ListViewItem)last).Content);
		Assert.IsLessThanOrEqualTo(list.ActualHeight + Tolerance, lastBounds.Bottom);
		Assert.IsGreaterThanOrEqualTo(-Tolerance, lastBounds.Y);
	}
}
