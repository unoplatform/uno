using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Uno.UI.DevTools.Input;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_ListBox
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1362")]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_Touch_Hold_Item_Then_Selected()
	{
		var items = new[] { new ListBoxItem { Content = "0" }, new ListBoxItem { Content = "1" }, new ListBoxItem { Content = "2" } };
		var list = new ListBox
		{
			Width = 200,
			Height = 200,
			ItemsPanel = (ItemsPanelTemplate)XamlReader.Load(
				"<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><StackPanel /></ItemsPanelTemplate>"),
		};
		foreach (var item in items)
		{
			list.Items.Add(item);
		}

		try
		{
			await UITestHelper.Load(list);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			// Unlike a ListViewItem, WinUI's ListBoxItem selects on the unhandled RightTapped which follows a touch hold.
			await finger.Hold(items[1].GetAbsoluteCenter());
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, list.SelectedIndex);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1362")]
	public async Task When_ItemsSource_Set_Then_Containers_Realized()
	{
		var sut = new ListBox
		{
			Width = 200,
			Height = 200,
			ItemsSource = new[] { "Apple", "Banana", "Cherry", "Date" },
		};

		try
		{
			await UITestHelper.Load(sut, x => x.IsLoaded);
			await WindowHelper.WaitForIdle();

			Assert.IsNotNull(sut.ItemsPanelRoot);
			Assert.AreEqual(4, sut.ItemsPanelRoot.Children.OfType<ListBoxItem>().Count());
			Assert.IsTrue(sut.ContainerFromIndex(3) is ListBoxItem { ActualHeight: > 0 });
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1362")]
	public async Task When_ItemsSource_Set_After_Load_Then_Containers_Realized()
	{
		var sut = new ListBox
		{
			Width = 200,
			Height = 200,
		};

		try
		{
			await UITestHelper.Load(sut, x => x.IsLoaded);
			await WindowHelper.WaitForIdle();

			sut.ItemsSource = new[] { "Item 1", "Item 2", "Item 3" };
			await WindowHelper.WaitFor(() => sut.ContainerFromIndex(2) is not null, message: "Expected the containers to be realized.");

			Assert.IsNotNull(sut.ItemsPanelRoot);
			Assert.AreEqual(3, sut.ItemsPanelRoot.Children.OfType<ListBoxItem>().Count());
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1362")]
	public async Task When_Many_Items_Then_Virtualized()
	{
		var source = Enumerable.Range(0, 1000).Select(i => $"Item {i}").ToArray();
		var sut = new ListBox
		{
			Width = 200,
			Height = 200,
			ItemsSource = source,
		};

		try
		{
			await UITestHelper.Load(sut, x => x.IsLoaded);
			await WindowHelper.WaitForIdle();

			var realized = sut.ItemsPanelRoot.Children.OfType<ListBoxItem>().Count();
			Assert.IsTrue(realized is > 0 and < 100, $"Expected a virtualized panel, got {realized} containers for {source.Length} items.");
			Assert.IsNull(sut.ContainerFromIndex(900));

			sut.ScrollIntoView(source[900]);
			await WindowHelper.WaitFor(() => sut.ContainerFromIndex(900) is ListBoxItem, message: "Expected ScrollIntoView to realize the item.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1362")]
	public async Task When_SelectedIndex_Set_Then_SelectedItems_Updated()
	{
		var sut = new ListBox { ItemsSource = new[] { "a", "b", "c" } };

		try
		{
			await UITestHelper.Load(sut);

			sut.SelectedIndex = 1;
			await WindowHelper.WaitForIdle();

			CollectionAssert.AreEqual(new[] { "b" }, sut.SelectedItems.ToArray());
			Assert.IsTrue(((ListBoxItem)sut.ContainerFromIndex(1)).IsSelected);

			sut.SelectedIndex = -1;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, sut.SelectedItems.Count);
			Assert.IsFalse(((ListBoxItem)sut.ContainerFromIndex(1)).IsSelected);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1362")]
	public async Task When_SelectedItems_Changed_Then_SelectionChanged_Raised()
	{
		var sut = new ListBox { ItemsSource = new[] { "a", "b", "c" }, SelectionMode = SelectionMode.Multiple };
		var events = new List<SelectionChangedEventArgs>();
		sut.SelectionChanged += (_, e) => events.Add(e);

		try
		{
			await UITestHelper.Load(sut);

			sut.SelectedItems.Add("b");
			sut.SelectedItems.Add("c");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(2, events.Count);
			CollectionAssert.AreEqual(new[] { "b" }, events[0].AddedItems.ToArray());
			CollectionAssert.AreEqual(new[] { "c" }, events[1].AddedItems.ToArray());
			Assert.AreEqual(1, sut.SelectedIndex);
			Assert.AreEqual("b", sut.SelectedItem);
			Assert.IsTrue(((ListBoxItem)sut.ContainerFromIndex(1)).IsSelected);
			Assert.IsTrue(((ListBoxItem)sut.ContainerFromIndex(2)).IsSelected);

			events.Clear();
			sut.SelectedItems.Remove("b");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, events.Count);
			CollectionAssert.AreEqual(new[] { "b" }, events[0].RemovedItems.ToArray());
			Assert.AreEqual(2, sut.SelectedIndex);
			Assert.IsFalse(((ListBoxItem)sut.ContainerFromIndex(1)).IsSelected);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1362")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Items_Changed_Then_Multi_Selection_Kept(bool useItemsSource)
	{
		var source = new ObservableCollection<string> { "a", "b", "c", "d" };
		var sut = new ListBox { SelectionMode = SelectionMode.Multiple };
		if (useItemsSource)
		{
			sut.ItemsSource = source;
		}
		else
		{
			foreach (var item in source)
			{
				sut.Items.Add(item);
			}
		}

		void Insert(int index, string item)
		{
			if (useItemsSource)
			{
				source.Insert(index, item);
			}
			else
			{
				sut.Items.Insert(index, item);
			}
		}

		void RemoveAt(int index)
		{
			if (useItemsSource)
			{
				source.RemoveAt(index);
			}
			else
			{
				sut.Items.RemoveAt(index);
			}
		}

		var events = new List<SelectionChangedEventArgs>();
		sut.SelectionChanged += (_, e) => events.Add(e);

		try
		{
			await UITestHelper.Load(sut);

			sut.SelectedItems.Add("b");
			sut.SelectedItems.Add("d");
			await WindowHelper.WaitForIdle();
			events.Clear();

			// [z, a, b, c, d]: an insertion before the selection only moves it.
			Insert(0, "z");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("z", ((ListBoxItem)sut.ContainerFromIndex(0)).Content);
			AssertSelected(sut, 2, 4);
			Assert.AreEqual(0, events.Count);
			Assert.AreEqual(2, sut.SelectedIndex);
			Assert.AreEqual("b", sut.SelectedItem);

			// [z, a, c, d]: removing a selected item deselects only that item.
			RemoveAt(2);
			await WindowHelper.WaitForIdle();
			AssertSelected(sut, 3);
			Assert.AreEqual(1, events.Count);
			CollectionAssert.AreEqual(new[] { "b" }, events[0].RemovedItems.ToArray());
			Assert.AreEqual(0, events[0].AddedItems.Count);
			Assert.AreEqual(3, sut.SelectedIndex);
			Assert.AreEqual("d", sut.SelectedItem);
			events.Clear();

			// [z, c, d]: removing an unselected item only moves the selection.
			RemoveAt(1);
			await WindowHelper.WaitForIdle();
			AssertSelected(sut, 2);
			Assert.AreEqual(0, events.Count);
			Assert.AreEqual(2, sut.SelectedIndex);

			if (!useItemsSource)
			{
				// ItemCollection's indexer doesn't raise VectorChanged, so a replacement in Items isn't observable.
				return;
			}

			// [z, c, e]: replacing a selected item deselects it.
			source[2] = "e";
			await WindowHelper.WaitForIdle();
			AssertSelected(sut);
			Assert.AreEqual(1, events.Count);
			CollectionAssert.AreEqual(new[] { "d" }, events[0].RemovedItems.ToArray());
			Assert.AreEqual(-1, sut.SelectedIndex);
			Assert.IsNull(sut.SelectedItem);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1362")]
	[DataRow(SelectionMode.Multiple)]
	[DataRow(SelectionMode.Extended)]
	public async Task When_Multi_Selection_And_SelectedIndex_Or_SelectedItem_Set(SelectionMode mode)
	{
		var sut = new ListBox { ItemsSource = new[] { "a", "b", "c", "d" }, SelectionMode = mode };
		var events = new List<SelectionChangedEventArgs>();
		sut.SelectionChanged += (_, e) => events.Add(e);

		try
		{
			await UITestHelper.Load(sut);

			sut.SelectedItems.Add("a");
			sut.SelectedItems.Add("c");
			await WindowHelper.WaitForIdle();
			events.Clear();

			// A value which isn't in the items is rejected, and the selection is left as is.
			sut.SelectedItem = "zzz";
			await WindowHelper.WaitForIdle();
			AssertSelected(sut, 0, 2);
			Assert.AreEqual(0, events.Count);
			Assert.AreEqual("a", sut.SelectedItem);
			Assert.AreEqual(0, sut.SelectedIndex);

			// SelectedIndex selects just that item.
			sut.SelectedIndex = 1;
			await WindowHelper.WaitForIdle();
			AssertSelected(sut, 1);
			Assert.AreEqual(1, events.Count);
			CollectionAssert.AreEquivalent(new[] { "a", "c" }, events[0].RemovedItems.ToArray());
			CollectionAssert.AreEqual(new[] { "b" }, events[0].AddedItems.ToArray());
			Assert.AreEqual("b", sut.SelectedItem);
			events.Clear();

			// So does SelectedItem.
			sut.SelectedItems.Add("a");
			await WindowHelper.WaitForIdle();
			events.Clear();
			sut.SelectedItem = "d";
			await WindowHelper.WaitForIdle();
			AssertSelected(sut, 3);
			Assert.AreEqual(1, events.Count);
			CollectionAssert.AreEquivalent(new[] { "a", "b" }, events[0].RemovedItems.ToArray());
			CollectionAssert.AreEqual(new[] { "d" }, events[0].AddedItems.ToArray());
			Assert.AreEqual(3, sut.SelectedIndex);
			events.Clear();

			sut.SelectedIndex = -1;
			await WindowHelper.WaitForIdle();
			AssertSelected(sut);
			Assert.AreEqual(1, events.Count);
			CollectionAssert.AreEqual(new[] { "d" }, events[0].RemovedItems.ToArray());
			Assert.IsNull(sut.SelectedItem);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1362")]
	public async Task When_Many_Items_SelectAll_Then_Deselected()
	{
		const int count = 5000;
		var sut = new ListBox { Width = 200, Height = 200, ItemsSource = Enumerable.Range(0, count).Select(i => $"Item {i}").ToArray(), SelectionMode = SelectionMode.Multiple };
		var events = new List<SelectionChangedEventArgs>();
		sut.SelectionChanged += (_, e) => events.Add(e);

		try
		{
			await UITestHelper.Load(sut, x => x.IsLoaded);
			await WindowHelper.WaitForIdle();

			sut.SelectAll();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(count, sut.SelectedItems.Count);
			Assert.AreEqual(1, events.Count);
			Assert.AreEqual(count, events[0].AddedItems.Count);
			Assert.IsTrue(sut.ItemsPanelRoot.Children.OfType<ListBoxItem>().All(c => c.IsSelected));

			sut.SelectedItems.Remove("Item 1");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(count - 1, sut.SelectedItems.Count);
			Assert.IsFalse(((ListBoxItem)sut.ContainerFromIndex(1)).IsSelected);
			Assert.IsTrue(((ListBoxItem)sut.ContainerFromIndex(2)).IsSelected);

			sut.SelectedIndex = -1;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, sut.SelectedItems.Count);
			Assert.IsFalse(sut.ItemsPanelRoot.Children.OfType<ListBoxItem>().Any(c => c.IsSelected));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1362")]
	public async Task When_SelectedItems_Observed_Then_Unselectable_Additions_Ignored()
	{
		var sut = new ListBox { ItemsSource = new[] { "a", "b", "c" }, SelectionMode = SelectionMode.Multiple };
		var events = new List<SelectionChangedEventArgs>();
		sut.SelectionChanged += (_, e) => events.Add(e);

		// A second handler: ObservableCollection then rejects any change made from inside its CollectionChanged.
		var notifications = 0;
		if (sut.SelectedItems is INotifyCollectionChanged collection)
		{
			collection.CollectionChanged += (_, _) => notifications++;
		}
		else
		{
			((IObservableVector<object>)sut.SelectedItems).VectorChanged += (_, _) => notifications++;
		}

		try
		{
			await UITestHelper.Load(sut);

			// An item which isn't in the items selects nothing. (WinUI keeps it in SelectedItems until the next change.)
			sut.SelectedItems.Add("zzz");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, events.Count);
			Assert.AreEqual(-1, sut.SelectedIndex);
			AssertSelectedContainers(sut);

			sut.SelectedItems.Add("a");
			sut.SelectedItems.Add("b");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(2, events.Count);
			AssertSelectedContainers(sut, 0, 1);

			// Neither does an item which is already selected.
			sut.SelectedItems.Add("b");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(2, events.Count);
			AssertSelectedContainers(sut, 0, 1);

			sut.SelectedItems.Add("c");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(3, events.Count);
			AssertSelected(sut, 0, 1, 2);
			Assert.AreEqual(0, sut.SelectedIndex);
			Assert.IsTrue(notifications > 0);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1362")]
	public async Task When_Single_Selection_And_SelectedItems_Changed_Then_Throws()
	{
		var sut = new ListBox { ItemsSource = new[] { "a", "b", "c" } };
		var events = new List<SelectionChangedEventArgs>();

		try
		{
			await UITestHelper.Load(sut);

			sut.SelectedIndex = 1;
			await WindowHelper.WaitForIdle();
			sut.SelectionChanged += (_, e) => events.Add(e);

			// WinUI fails these with E_UNEXPECTED.
			AssertThrows(() => sut.SelectedItems.Add("a"));
			AssertThrows(() => sut.SelectedItems.Remove("b"));
			AssertThrows(() => sut.SelectedItems.Clear());
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, events.Count);
			Assert.AreEqual(1, sut.SelectedIndex);
			Assert.AreEqual("b", sut.SelectedItem);
			AssertSelectedContainers(sut, 1);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}

		static void AssertThrows(Action action)
		{
			try
			{
				action();
			}
			catch (Exception)
			{
				return;
			}

			Assert.Fail("Expected the change to SelectedItems to throw.");
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1362")]
	[DataRow(SelectionMode.Multiple, SelectionMode.Single, "-[c]; -[a]")]
	[DataRow(SelectionMode.Multiple, SelectionMode.Extended, "-[a,c]")]
	[DataRow(SelectionMode.Extended, SelectionMode.Multiple, "-[a,c]")]
	[DataRow(SelectionMode.Single, SelectionMode.Multiple, "-[c]")]
	[DataRow(SelectionMode.Single, SelectionMode.Extended, "-[c]")]
	public async Task When_SelectionMode_Changed_Then_Selection_Cleared(SelectionMode oldMode, SelectionMode newMode, string expectedEvents)
	{
		var sut = new ListBox { ItemsSource = new[] { "a", "b", "c" }, SelectionMode = oldMode };
		var events = new List<SelectionChangedEventArgs>();
		sut.SelectionChanged += (_, e) => events.Add(e);

		try
		{
			await UITestHelper.Load(sut);

			if (oldMode == SelectionMode.Single)
			{
				sut.SelectedIndex = 2;
			}
			else
			{
				sut.SelectedItems.Add("a");
				sut.SelectedItems.Add("c");
			}
			await WindowHelper.WaitForIdle();
			events.Clear();

			sut.SelectionMode = newMode;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, sut.SelectedItems.Count, $"SelectedItems: [{string.Join(",", sut.SelectedItems)}]");
			Assert.AreEqual(-1, sut.SelectedIndex);
			Assert.IsNull(sut.SelectedItem);
			Assert.AreEqual(expectedEvents, FormatEvents(events));
			AssertSelected(sut);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1362")]
	[DataRow(SelectionMode.Single)]
	[DataRow(SelectionMode.Multiple)]
	[DataRow(SelectionMode.Extended)]
	public async Task When_ItemsSource_Replaced_Then_Selection_Cleared(SelectionMode mode)
	{
		var sut = new ListBox { ItemsSource = new[] { "a", "b", "c" }, SelectionMode = mode };
		var events = new List<SelectionChangedEventArgs>();
		sut.SelectionChanged += (_, e) => events.Add(e);

		try
		{
			await UITestHelper.Load(sut);

			if (mode == SelectionMode.Single)
			{
				sut.SelectedIndex = 2;
			}
			else
			{
				sut.SelectedItems.Add("a");
				sut.SelectedItems.Add("c");
			}
			await WindowHelper.WaitForIdle();
			events.Clear();

			// The new source shares an item with the old selection: the selection is still cleared.
			sut.ItemsSource = new[] { "c", "d" };
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, sut.SelectedItems.Count, $"SelectedItems: [{string.Join(",", sut.SelectedItems)}]");
			Assert.AreEqual(-1, sut.SelectedIndex);
			Assert.IsNull(sut.SelectedItem);
			Assert.AreEqual(mode == SelectionMode.Single ? "-[c]" : "-[a,c]", FormatEvents(events));
			AssertSelected(sut);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1362")]
	public async Task When_SelectAll()
	{
		var sut = new ListBox { ItemsSource = new[] { "a", "b", "c" } };

		try
		{
			await UITestHelper.Load(sut);

			// WinUI fails it with E_NOT_SUPPORTED, which surfaces as a COMException there.
#if HAS_UNO
			Assert.ThrowsExactly<NotSupportedException>(() => sut.SelectAll());
#else
			Assert.Throws<Exception>(() => sut.SelectAll());
#endif

			sut.SelectionMode = SelectionMode.Multiple;
			sut.SelectAll();
			await WindowHelper.WaitForIdle();

			CollectionAssert.AreEquivalent(new[] { "a", "b", "c" }, sut.SelectedItems.ToArray());
			Assert.IsTrue(Enumerable.Range(0, 3).All(i => ((ListBoxItem)sut.ContainerFromIndex(i)).IsSelected));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1362")]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // The mouse helper can't hold modifier keys on WinUI.
	[DataRow(SelectionMode.Single)]
	[DataRow(SelectionMode.Multiple)]
	[DataRow(SelectionMode.Extended)]
	public async Task When_Item_Clicked(SelectionMode mode)
	{
		var sut = new ListBox { Width = 200, ItemsSource = Enumerable.Range(0, 8).Select(i => $"Item {i}").ToArray(), SelectionMode = mode };

		try
		{
			await UITestHelper.Load(sut);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			async Task Click(int index, VirtualKeyModifiers modifiers = VirtualKeyModifiers.None)
			{
#if HAS_UNO
				mouse.Press(((ListBoxItem)sut.ContainerFromIndex(index)).GetAbsoluteCenter(), modifiers);
				mouse.Release(modifiers);
				await WindowHelper.WaitForIdle();
#else
				await Task.CompletedTask;
				throw new NotSupportedException("The mouse helper can't hold modifier keys on WinUI.");
#endif
			}

			await Click(1);
			AssertSelected(sut, 1);

			switch (mode)
			{
				case SelectionMode.Single:
					await Click(3);
					AssertSelected(sut, 3);
					Assert.AreEqual(3, sut.SelectedIndex);

					await Click(3, VirtualKeyModifiers.Control);
					AssertSelected(sut);
					Assert.AreEqual(-1, sut.SelectedIndex);
					break;

				case SelectionMode.Multiple:
					await Click(3);
					AssertSelected(sut, 1, 3);

					await Click(1);
					AssertSelected(sut, 3);
					break;

				case SelectionMode.Extended:
					await Click(3, VirtualKeyModifiers.Shift);
					AssertSelected(sut, 1, 2, 3);

					await Click(5, VirtualKeyModifiers.Control);
					AssertSelected(sut, 1, 2, 3, 5);

					// The toggle moved the anchor to 5, and a plain shift-click replaces the selection.
					await Click(4, VirtualKeyModifiers.Shift);
					AssertSelected(sut, 4, 5);

					// Ctrl+Shift adds the range from the anchor without clearing.
					await Click(7, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);
					AssertSelected(sut, 4, 5, 6, 7);

					await Click(0);
					AssertSelected(sut, 0);
					break;
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1362")]
#if WINAPPSDK
	[Ignore("KeyboardHelper.PressKeySequence raises the key events on Uno only.")]
#endif
	public async Task When_Extended_Keyboard()
	{
		var sut = new ListBox { Width = 200, ItemsSource = Enumerable.Range(0, 8).Select(i => $"Item {i}").ToArray(), SelectionMode = SelectionMode.Extended };

		try
		{
			await UITestHelper.Load(sut);

			sut.SelectedIndex = 0;
			var result = await FocusManager.TryFocusAsync(sut.ContainerFromIndex(0), FocusState.Keyboard);
			Assert.IsTrue(result.Succeeded);
			await WindowHelper.WaitForIdle();

			// A plain move selects the focused item and makes it the anchor.
			await KeyboardHelper.Down();
			AssertSelected(sut, 1);

			await KeyboardHelper.PressKeySequence("$d$_shift#$d$_down#$u$_down#$d$_down#$u$_down#$u$_shift");
			AssertSelected(sut, 1, 2, 3);

			await KeyboardHelper.Down();
			AssertSelected(sut, 4);

			// Ctrl moves the focus only, Ctrl+Space toggles the focused item.
			// (A sequence raises its keys on the element focused when it starts, so each key that needs the new focus gets its own.)
			await KeyboardHelper.PressKeySequence("$d$_ctrl#$d$_down#$u$_down#$u$_ctrl");
			await KeyboardHelper.PressKeySequence("$d$_ctrl#$d$_down#$u$_down#$u$_ctrl");
			AssertSelected(sut, 4);
			Assert.AreEqual(sut.ContainerFromIndex(6), FocusManager.GetFocusedElement(sut.XamlRoot));

			await KeyboardHelper.PressKeySequence("$d$_ctrl#$d$_space#$u$_space#$u$_ctrl");
			AssertSelected(sut, 4, 6);

			await KeyboardHelper.PressKeySequence("$d$_ctrl#$d$_a#$u$_a#$u$_ctrl");
			AssertSelected(sut, 0, 1, 2, 3, 4, 5, 6, 7);

			await KeyboardHelper.PressKeySequence("$d$_ctrl#$d$_a#$u$_a#$u$_ctrl");
			AssertSelected(sut);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1362")]
#if WINAPPSDK
	[Ignore("KeyboardHelper.PressKeySequence raises the key events on Uno only.")]
#endif
	public async Task When_Duplicate_Items_And_Ctrl_A_Then_Toggles()
	{
		// Equal items share one SelectedItems entry, so SelectedItems has fewer entries than Items once all are selected.
		var sut = new ListBox { Width = 200, ItemsSource = new[] { "a", "a", "b" }, SelectionMode = SelectionMode.Multiple };

		try
		{
			await UITestHelper.Load(sut);

			var result = await FocusManager.TryFocusAsync(sut.ContainerFromIndex(2), FocusState.Keyboard);
			Assert.IsTrue(result.Succeeded);
			await WindowHelper.WaitForIdle();

			await KeyboardHelper.PressKeySequence("$d$_ctrl#$d$_a#$u$_a#$u$_ctrl");
			CollectionAssert.AreEquivalent(new[] { "a", "b" }, sut.SelectedItems.ToArray());

			await KeyboardHelper.PressKeySequence("$d$_ctrl#$d$_a#$u$_a#$u$_ctrl");
			Assert.AreEqual(0, sut.SelectedItems.Count, $"SelectedItems: [{string.Join(",", sut.SelectedItems)}]");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1362")]
	public void When_Default_Values()
	{
		var sut = new ListBox();

		Assert.AreEqual(SelectionMode.Single, sut.SelectionMode);
		Assert.IsTrue(sut.SingleSelectionFollowsFocus);
		Assert.AreEqual(0, sut.SelectedItems.Count);
		Assert.AreEqual(-1, sut.SelectedIndex);
		Assert.IsNull(sut.SelectedItem);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1362")]
	public async Task When_ListBoxItem_IsSelected_Set_In_Multiple_Mode()
	{
		var sut = new ListBox { ItemsSource = new[] { "a", "b", "c" }, SelectionMode = SelectionMode.Multiple };
		var events = new List<SelectionChangedEventArgs>();
		sut.SelectionChanged += (_, e) => events.Add(e);

		try
		{
			await UITestHelper.Load(sut);

			sut.SelectedItems.Add("a");
			await WindowHelper.WaitForIdle();
			events.Clear();

			// Selecting a container adds its item to the selection.
			((ListBoxItem)sut.ContainerFromIndex(2)).IsSelected = true;
			await WindowHelper.WaitForIdle();
			AssertSelected(sut, 0, 2);
			Assert.AreEqual("+[c]", FormatEvents(events));
			Assert.AreEqual(0, sut.SelectedIndex);
			events.Clear();

			// Deselecting one removes only its item.
			((ListBoxItem)sut.ContainerFromIndex(0)).IsSelected = false;
			await WindowHelper.WaitForIdle();
			AssertSelected(sut, 2);
			Assert.AreEqual("-[a]", FormatEvents(events));
			Assert.AreEqual(2, sut.SelectedIndex);
			Assert.AreEqual("c", sut.SelectedItem);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1362")]
#if WINAPPSDK
	[Ignore("KeyboardHelper raises the key events on Uno only.")]
#endif
	public async Task When_SingleSelectionFollowsFocus_False()
	{
		var sut = new ListBox { Width = 200, ItemsSource = new[] { "a", "b", "c" }, SingleSelectionFollowsFocus = false };

		try
		{
			await UITestHelper.Load(sut);

			sut.SelectedIndex = 0;
			var result = await FocusManager.TryFocusAsync(sut.ContainerFromIndex(0), FocusState.Keyboard);
			Assert.IsTrue(result.Succeeded);
			await WindowHelper.WaitForIdle();

			// The focus moves, the selection doesn't.
			await KeyboardHelper.Down();
			Assert.AreEqual(sut.ContainerFromIndex(1), FocusManager.GetFocusedElement(sut.XamlRoot));
			AssertSelected(sut, 0);

			// Space selects the focused item.
			await KeyboardHelper.Space();
			AssertSelected(sut, 1);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1362")]
	[DataRow(SelectionMode.Single)]
	[DataRow(SelectionMode.Multiple)]
	public async Task When_SelectedIndex_Set_Before_ItemsSource(SelectionMode mode)
	{
		// As in markup, where SelectedIndex can come before ItemsSource: WinUI keeps the index until items are available.
		var sut = new ListBox { Width = 200, Height = 200, SelectionMode = mode };
		sut.SelectedIndex = 1;

		try
		{
			await UITestHelper.Load(sut, x => x.IsLoaded);

			sut.ItemsSource = new[] { "a", "b", "c" };
			await WindowHelper.WaitFor(() => sut.ContainerFromIndex(2) is ListBoxItem { ActualHeight: > 0 });
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, sut.SelectedIndex);
			Assert.AreEqual("b", sut.SelectedItem);
			AssertSelected(sut, 1);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1362")]
	public async Task When_SelectedItems_Changed_In_SelectionChanged()
	{
		var sut = new ListBox { ItemsSource = new[] { "a", "b", "c" }, SelectionMode = SelectionMode.Multiple };
		var events = new List<SelectionChangedEventArgs>();
		sut.SelectionChanged += (_, e) =>
		{
			events.Add(e);

			// Selecting "a" also selects "b".
			if (e.AddedItems.Contains("a"))
			{
				sut.SelectedItems.Add("b");
			}
		};

		// A second handler: ObservableCollection then rejects any change made from inside its CollectionChanged.
		var notifications = 0;
		if (sut.SelectedItems is INotifyCollectionChanged collection)
		{
			collection.CollectionChanged += (_, _) => notifications++;
		}
		else
		{
			((IObservableVector<object>)sut.SelectedItems).VectorChanged += (_, _) => notifications++;
		}

		try
		{
			await UITestHelper.Load(sut);

			sut.SelectedItems.Add("a");
			await WindowHelper.WaitForIdle();

			AssertSelected(sut, 0, 1);
			Assert.AreEqual("+[a]; +[b]", FormatEvents(events));
			Assert.IsTrue(notifications > 0);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/1362")]
	public async Task When_ItemsPanel_Swapped()
	{
		const int count = 100;
		var sut = new ListBox
		{
			Width = 200,
			Height = 200,
			ItemsSource = Enumerable.Range(0, count).Select(i => $"Item {i}").ToArray(),
			SelectionMode = SelectionMode.Multiple,
		};

		try
		{
			await UITestHelper.Load(sut, x => x.IsLoaded);
			sut.SelectedItems.Add("Item 1");
			await WindowHelper.WaitForIdle();
			Assert.IsInstanceOfType<VirtualizingStackPanel>(sut.ItemsPanelRoot);
			Assert.IsTrue(sut.ItemsPanelRoot.Children.Count < count, "The default panel should virtualize.");

			// A StackPanel realizes every item.
			sut.ItemsPanel = (ItemsPanelTemplate)XamlReader.Load(
				"<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><StackPanel /></ItemsPanelTemplate>");
			await WindowHelper.WaitFor(() => sut.ItemsPanelRoot is StackPanel { IsLoaded: true });
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(count, sut.ItemsPanelRoot.Children.Count);
			AssertContainer(0);
			AssertSelected(sut, 1);

			// Back to a virtualizing panel.
			sut.ItemsPanel = (ItemsPanelTemplate)XamlReader.Load(
				"<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><VirtualizingStackPanel /></ItemsPanelTemplate>");
			await WindowHelper.WaitFor(() => sut.ItemsPanelRoot is VirtualizingStackPanel { IsLoaded: true });
			await WindowHelper.WaitFor(() => sut.ContainerFromIndex(0) is ListBoxItem { ActualHeight: > 0 });
#if HAS_UNO
			// (WinUI realized every item after this swap.)
			Assert.IsTrue(sut.ItemsPanelRoot.Children.Count < count, "The virtualizing panel should virtualize.");
#endif
			AssertContainer(0);
			Assert.IsTrue(((ListBoxItem)sut.ContainerFromIndex(1)).IsSelected);
			CollectionAssert.AreEqual(new[] { "Item 1" }, sut.SelectedItems.ToArray());
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}

		void AssertContainer(int index)
			=> Assert.AreEqual($"Item {index}", (sut.ContainerFromIndex(index) as ListBoxItem)?.Content);
	}

	// One entry per SelectionChanged event: "+[added]" and/or "-[removed]".
	private static string FormatEvents(IEnumerable<SelectionChangedEventArgs> events)
		=> string.Join("; ", events.Select(e => string.Join(" ", new[]
		{
			e.AddedItems.Count > 0 ? $"+[{string.Join(",", e.AddedItems)}]" : null,
			e.RemovedItems.Count > 0 ? $"-[{string.Join(",", e.RemovedItems)}]" : null,
		}.Where(part => part is not null))));

	private static void AssertSelectedContainers(ListBox sut, params int[] expected)
	{
		var containers = Enumerable.Range(0, sut.Items.Count).Where(i => sut.ContainerFromIndex(i) is ListBoxItem { IsSelected: true }).ToArray();
		CollectionAssert.AreEqual(expected, containers, $"Selected containers: [{string.Join(",", containers)}]");
	}

	private static void AssertSelected(ListBox sut, params int[] expected)
	{
		AssertSelectedContainers(sut, expected);

		var items = sut.SelectedItems.Select(item => sut.Items.IndexOf(item)).OrderBy(i => i).ToArray();
		CollectionAssert.AreEqual(expected, items, $"SelectedItems: [{string.Join(",", items)}]");
	}
}
