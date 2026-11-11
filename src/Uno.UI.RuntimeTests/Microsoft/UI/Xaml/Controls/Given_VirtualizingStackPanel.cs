using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_VirtualizingStackPanel
{
	[TestMethod]
	public void When_Default_Values()
	{
		var sut = new VirtualizingStackPanel();

		Assert.AreEqual(Orientation.Vertical, sut.Orientation);
	}

	[TestMethod]
	[DataRow(Orientation.Vertical)]
	[DataRow(Orientation.Horizontal)]
	public async Task When_ItemsPanel_Of_ItemsControl_Then_Items_Stacked(Orientation orientation)
	{
		const int count = 5;
		var sut = new ItemsControl
		{
			Width = 400,
			Height = 400,
			ItemsSource = Enumerable.Range(0, count).Select(i => $"Item {i}").ToArray(),
			ItemsPanel = (ItemsPanelTemplate)XamlReader.Load(
				$"<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><VirtualizingStackPanel Orientation='{orientation}' /></ItemsPanelTemplate>"),
		};

		try
		{
			await UITestHelper.Load(sut, x => x.IsLoaded);
			await WindowHelper.WaitForIdle();

			var containers = Enumerable.Range(0, count).Select(i => sut.ContainerFromIndex(i) as FrameworkElement).ToArray();
			Assert.IsTrue(containers.All(c => c is { ActualWidth: > 0, ActualHeight: > 0 }), "Expected every item to get a sized container.");

			for (var i = 1; i < count; i++)
			{
				var previous = containers[i - 1].TransformToVisual(sut.ItemsPanelRoot).TransformPoint(default);
				var current = containers[i].TransformToVisual(sut.ItemsPanelRoot).TransformPoint(default);
				if (orientation == Orientation.Vertical)
				{
					Assert.AreEqual(previous.Y + containers[i - 1].ActualHeight, current.Y, 1);
					Assert.AreEqual(previous.X, current.X, 1);
				}
				else
				{
					Assert.AreEqual(previous.X + containers[i - 1].ActualWidth, current.X, 1);
					Assert.AreEqual(previous.Y, current.Y, 1);
				}
			}

			// The ItemsControl adds the containers: the panel must not generate a second set.
			Assert.AreEqual(count, sut.ItemsPanelRoot.Children.Count);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_ItemsPanel_Of_ItemsControl_In_ScrollViewer_Then_Scrolls_Stacked()
	{
		const int count = 50;
		var sut = new ItemsControl
		{
			ItemsSource = Enumerable.Range(0, count).Select(i => $"Item {i}").ToArray(),
			ItemsPanel = (ItemsPanelTemplate)XamlReader.Load(
				"<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><VirtualizingStackPanel /></ItemsPanelTemplate>"),
		};
		var scrollViewer = new ScrollViewer { Width = 200, Height = 200, Content = sut };

		try
		{
			await UITestHelper.Load(scrollViewer, x => x.IsLoaded);
			await WindowHelper.WaitForIdle();

			var middle = scrollViewer.ScrollableHeight / 2;
			scrollViewer.ChangeView(null, middle, null, disableAnimation: true);
			await WindowHelper.WaitFor(() => Math.Abs(scrollViewer.VerticalOffset - middle) < 1, message: "Expected the ScrollViewer to scroll.");
			scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null, disableAnimation: true);
			await WindowHelper.WaitFor(() => Math.Abs(scrollViewer.VerticalOffset - scrollViewer.ScrollableHeight) < 1, message: "Expected the ScrollViewer to scroll to the end.");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(count, sut.ItemsPanelRoot.Children.Count);

			var last = (FrameworkElement)sut.ContainerFromIndex(count - 1);
			var lastTop = last.TransformToVisual(sut.ItemsPanelRoot).TransformPoint(default).Y;
			Assert.AreEqual(sut.ItemsPanelRoot.ActualHeight - last.ActualHeight, lastTop, 1);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
