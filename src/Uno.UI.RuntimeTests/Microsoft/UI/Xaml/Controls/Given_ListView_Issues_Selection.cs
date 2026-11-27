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
using Microsoft.UI.Xaml.Media;
#if HAS_UNO
using Uno.UI.DevTools.Input;
#endif
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
}
