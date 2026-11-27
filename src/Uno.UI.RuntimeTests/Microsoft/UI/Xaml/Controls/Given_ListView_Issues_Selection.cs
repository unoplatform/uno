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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

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
}
