#nullable enable

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

#if !WINAPPSDK

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Uno.UI.DevTools.Input;
using Windows.System;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

public partial class Given_TableView
{
	[TestMethod]
	public async Task When_Keyboard_Down_Up_Home_End_PageDown()
	{
		var items = People(30);
		var table = CreateTable(items);
		await LoadAsync(table);

		Assert.IsTrue(GetRow(table, 0)!.Focus(FocusState.Keyboard));
		await WindowHelper.WaitForIdle();

		await PressAsync("down");
		Assert.AreEqual(1, GetFocusedRowIndex(table), "Down moves focus");
		Assert.AreEqual(1, table.SelectedIndex, "selection follows focus");
		Assert.AreSame(items[1], table.SelectedItem);

		await PressAsync("down");
		Assert.AreEqual(2, table.SelectedIndex);

		await PressAsync("up");
		Assert.AreEqual(1, GetFocusedRowIndex(table));
		Assert.AreEqual(1, table.SelectedIndex);

		await PressAsync("end");
		Assert.AreEqual(items.Count - 1, GetFocusedRowIndex(table), "End focuses the last row");
		Assert.AreEqual(items.Count - 1, table.SelectedIndex);

		await PressAsync("home");
		Assert.AreEqual(0, GetFocusedRowIndex(table), "Home focuses the first row");
		Assert.AreEqual(0, table.SelectedIndex);

		// GetEstimatedRowsPerPage: viewport height over a realized data row's height.
		var rowsPerPage = Math.Max(1, (int)(GetBodyScroller(table).ViewportHeight / GetRow(table, 0)!.ActualHeight));
		await PressAsync("pagedown");
		Assert.AreEqual(Math.Min(items.Count - 1, rowsPerPage), table.SelectedIndex, "PageDown moves by the estimated rows per page");
		Assert.AreEqual(table.SelectedIndex, GetFocusedRowIndex(table));

		// Up on the first row does not move the selection; unlike Home/End/PageUp/PageDown the key is
		// left unhandled at the boundary, so focus may leave the table.
		await PressAsync("home");
		await PressAsync("up");
		Assert.AreEqual(0, table.SelectedIndex);
	}

	[TestMethod]
	public async Task When_Keyboard_Entry_From_Header()
	{
		// TableView_Keyboard.cpp: with focus inside the table but off the rows, Up/Down resume from the
		// SELECTED row ("otherwise Down after clicking away yanks the selection to the top"), PageDown
		// enters at rowsPerPage - 1 and End at the last row.
		var items = People(30);
		var table = CreateTable(items);
		await LoadAsync(table);

		var rowsPerPage = Math.Max(1, (int)(GetBodyScroller(table).ViewportHeight / GetRow(table, 0)!.ActualHeight));

		table.Select(5);
		await FocusHeaderAsync();
		await PressAsync("down");
		Assert.AreEqual(6, table.SelectedIndex, "Down resumes from the selected row");
		Assert.AreEqual(6, GetFocusedRowIndex(table));

		table.DeselectAll();
		await FocusHeaderAsync();
		await PressAsync("pagedown");
		var expectedPageEntry = Math.Clamp(rowsPerPage - 1, 0, items.Count - 1);
		Assert.AreEqual(expectedPageEntry, table.SelectedIndex, "the first PageDown lands on rowsPerPage - 1");
		Assert.AreEqual(expectedPageEntry, GetFocusedRowIndex(table));

		await FocusHeaderAsync();
		await PressAsync("end");
		Assert.AreEqual(items.Count - 1, table.SelectedIndex, "End enters at the last row");
		Assert.AreEqual(items.Count - 1, GetFocusedRowIndex(table));

		async Task FocusHeaderAsync()
		{
			Assert.IsTrue(GetHeaderCells(table)[0].Focus(FocusState.Keyboard), "the resizable header cell did not take focus");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(-1, GetFocusedRowIndex(table));
		}
	}

	[TestMethod]
	public async Task When_Keyboard_Handled_By_Cell_Control()
	{
		// A key a focused descendant already handled (a ComboBox in a template cell) is only acted on
		// when focus is on one of our rows or group headers.
		var table = CreateTable(People(10));
		var actionColumn = new TableViewTemplateColumn
		{
			Header = "Action",
			CellTemplate = (DataTemplate)XamlReader.Load(
				"""
				<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
					<Button Content="Act" />
				</DataTemplate>
				"""),
		};
		table.Columns.Add(actionColumn);
		await LoadAsync(table, width: 700);

		table.Select(1);

		var button = Descendants(GetCell(GetRow(table, 1)!, actionColumn)).OfType<Button>().Single();
		button.KeyDown += (_, e) =>
		{
			if (e.Key == VirtualKey.Down)
			{
				e.Handled = true;
			}
		};
		Assert.IsTrue(button.Focus(FocusState.Keyboard));
		await WindowHelper.WaitForIdle();

		await PressAsync("down");

		Assert.AreEqual(1, table.SelectedIndex, "a Down the cell control handled does not navigate");
	}

	[TestMethod]
	public async Task When_Ctrl_Navigation()
	{
		var table = CreateTable(People(10));
		await LoadAsync(table);

		table.Select(0);
		Assert.IsTrue(GetRow(table, 0)!.Focus(FocusState.Keyboard));
		await WindowHelper.WaitForIdle();

		var raised = 0;
		table.SelectionChanged += (_, _) => raised++;

		try
		{
			// Ctrl+Arrow moves the focus cursor without selecting (ListViewBase parity).
			await KeyboardHelper.PressKeySequence("$d$_ctrl#$d$_down#$u$_down#$d$_down#$u$_down#$u$_ctrl");
			await WindowHelper.WaitForIdle();
		}
		finally
		{
			await ReleaseCtrlAsync();
		}

		Assert.AreEqual(2, GetFocusedRowIndex(table));
		Assert.AreEqual(0, table.SelectedIndex);
		Assert.AreEqual(0, raised);
	}

	[TestMethod]
	public async Task When_Space_On_Focused_Row()
	{
		var items = People(10);
		var table = CreateTable(items);
		await LoadAsync(table);

		// Programmatic focus does not select.
		Assert.IsTrue(GetRow(table, 2)!.Focus(FocusState.Keyboard));
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(-1, table.SelectedIndex);

		await PressAsync("space");
		Assert.AreEqual(2, table.SelectedIndex, "Space selects the focused row");
		Assert.AreEqual(2, GetFocusedRowIndex(table), "without moving it");

		try
		{
			// Ctrl toggles, matching SingleSelector::OnInteractedAction.
			await KeyboardHelper.PressKeySequence("$d$_ctrl#$d$_space#$u$_space#$u$_ctrl");
			await WindowHelper.WaitForIdle();
		}
		finally
		{
			await ReleaseCtrlAsync();
		}

		Assert.AreEqual(-1, table.SelectedIndex, "Ctrl+Space toggles the selection off");
		Assert.IsNull(table.SelectedItem);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Pointer_Select_On_Release_Mouse()
	{
		var table = CreateTable(People(5));
		await LoadAsync(table);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();

		var point = Center(GetRow(table, 3)!);
		mouse.Press(point);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(-1, table.SelectedIndex, "a press alone does not select");
		Assert.AreEqual(3, GetFocusedRowIndex(table), "the press focuses the row");

		mouse.Release();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(3, table.SelectedIndex, "the release commits the selection");

		// A right-click never selects.
		mouse.PressRight(Center(GetRow(table, 1)!));
		mouse.ReleaseRight();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(3, table.SelectedIndex);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Pointer_Select_On_Release_Touch()
	{
		var table = CreateTable(People(5));
		await LoadAsync(table);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		finger.Press(Center(GetRow(table, 2)!));
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(-1, table.SelectedIndex, "a touch press alone does not select");

		finger.Release();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(2, table.SelectedIndex);
	}

	[TestMethod]
	public async Task When_Select_Without_Source_Or_ModeNone()
	{
		var items = People(5);
		var table = CreateTable(null);
		await LoadAsync(table);

		// Not queued: a request before the source exists is simply dropped.
		table.Select(1);
		Assert.AreEqual(-1, table.SelectedIndex);

		table.ItemsSource = items;
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(-1, table.SelectedIndex);

		table.SelectionMode = TableViewSelectionMode.None;
		table.Select(1);
		Assert.AreEqual(-1, table.SelectedIndex);

		table.SelectionMode = TableViewSelectionMode.Single;
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(-1, table.SelectedIndex, "a request made while selection was off is not replayed");

		table.Select(1);
		Assert.AreEqual(1, table.SelectedIndex);
		Assert.AreSame(items[1], table.SelectedItem);

		// Out of range is rejected, not coerced into a clear.
		table.Select(999);
		Assert.AreEqual(1, table.SelectedIndex);

		// Select(-1) is an explicit "select nothing".
		table.Select(-1);
		Assert.AreEqual(-1, table.SelectedIndex);
		Assert.IsNull(table.SelectedItem);
	}

	[TestMethod]
	public async Task When_Deselect_Only_Current()
	{
		var table = CreateTable(People(5));
		await LoadAsync(table);

		table.Select(2);
		Assert.IsTrue(table.IsSelected(2));
		Assert.IsFalse(table.IsSelected(1));
		Assert.IsFalse(table.IsSelected(-1));
		Assert.IsFalse(table.IsSelected(99));

		// A stale index cannot clobber the current selection.
		table.Deselect(1);
		Assert.AreEqual(2, table.SelectedIndex);

		table.Deselect(2);
		Assert.AreEqual(-1, table.SelectedIndex);
		Assert.IsFalse(table.IsSelected(2));
	}

	[TestMethod]
	public async Task When_DeselectAll()
	{
		var items = People(5);
		var table = CreateTable(items);
		await LoadAsync(table);

		table.Select(3);

		var args = new List<SelectionChangedEventArgs>();
		table.SelectionChanged += (_, e) => args.Add(e);

		table.DeselectAll();

		Assert.AreEqual(-1, table.SelectedIndex);
		Assert.IsNull(table.SelectedItem);
		Assert.AreEqual(1, args.Count);
		Assert.AreSame(items[3], args[0].RemovedItems.Single());
	}

	[TestMethod]
	public async Task When_DeselectAll_While_Unloaded_Drops_Pending_Selection()
	{
		var items = People(6);
		var table = CreateTable(items);
		var host = await LoadAsync(table);

		table.Select(3);

		host.Children.Remove(table);
		await WindowHelper.WaitFor(() => !table.IsLoaded);

		// DeselectAll also clears the selection held for the reload (ClearPendingSelection), so the
		// round trip cannot resurrect it.
		table.DeselectAll();

		host.Children.Add(table);
		await WindowHelper.WaitForLoaded(table);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(-1, table.SelectedIndex);
		Assert.IsNull(table.SelectedItem);
		Assert.IsTrue(GetRealizedRows(table).All(r => !r.IsSelected));
	}

	[TestMethod]
	public async Task When_SelectionMode_None_Clears()
	{
		var items = People(5);
		var table = CreateTable(items);
		await LoadAsync(table);

		table.Select(3);

		var args = new List<SelectionChangedEventArgs>();
		table.SelectionChanged += (_, e) => args.Add(e);

		table.SelectionMode = TableViewSelectionMode.None;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(-1, table.SelectedIndex);
		Assert.IsNull(table.SelectedItem);
		Assert.AreEqual(1, args.Count);
		Assert.AreSame(items[3], args[0].RemovedItems.Single());
		Assert.AreEqual(0, args[0].AddedItems.Count);
		Assert.IsFalse(GetRow(table, 3)!.IsSelected);
	}

	[TestMethod]
	public async Task When_Insert_Above_Selected()
	{
		var items = new ObservableCollection<Person>(People(6));
		var table = CreateTable(items);
		await LoadAsync(table);

		table.Select(2);
		var selected = items[2];

		var raised = 0;
		table.SelectionChanged += (_, _) => raised++;

		items.Insert(0, new Person("Inserted", 1, "Oslo"));
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(3, table.SelectedIndex, "the index follows the item");
		Assert.AreSame(selected, table.SelectedItem);
		Assert.AreEqual(0, raised, "a shifted index is not a selection change");
		Assert.IsTrue(GetRow(table, 3)!.IsSelected);
		Assert.IsFalse(GetRow(table, 2)!.IsSelected, "the row that slid into the old slot is not selected");
	}

	[TestMethod]
	public async Task When_Remove_Selected()
	{
		var items = new ObservableCollection<Person>(People(6));
		var table = CreateTable(items);
		await LoadAsync(table);

		table.Select(2);
		var selected = items[2];

		var args = new List<SelectionChangedEventArgs>();
		table.SelectionChanged += (_, e) => args.Add(e);

		items.Remove(selected);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(-1, table.SelectedIndex, "the selection clears rather than sliding onto the neighbour");
		Assert.IsNull(table.SelectedItem);
		Assert.AreEqual(1, args.Count);
		Assert.AreSame(selected, args[0].RemovedItems.Single());
		Assert.IsTrue(GetRealizedRows(table).All(r => !r.IsSelected));
	}

	[TestMethod]
	public async Task When_Replace_ItemsSource()
	{
		var table = CreateTable(People(6));
		await LoadAsync(table);

		table.Select(2);
		var selected = table.SelectedItem;

		var args = new List<SelectionChangedEventArgs>();
		table.SelectionChanged += (_, e) => args.Add(e);

		// A different data set does not contain the selected item.
		table.ItemsSource = People(6);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(-1, table.SelectedIndex);
		Assert.IsNull(table.SelectedItem);
		Assert.IsTrue(GetRealizedRows(table).All(r => !r.IsSelected));

		// ResolveSelectionAfterSourceChange publishes once: no transient null-then-reselect pair.
		Assert.AreEqual(1, args.Count);
		Assert.AreSame(selected, args[0].RemovedItems.Single());
		Assert.AreEqual(0, args[0].AddedItems.Count);
	}

	[TestMethod]
	public async Task When_TableViewSource_Filter_Keeps_Selection()
	{
		// OnSelectionSourceReset is subscribed ahead of SelectionModel so an in-place Reset that keeps
		// the selected item restores it silently instead of raising a clear-then-restore pair.
		var items = People(6);
		var source = TableViewSource.From(items);
		var table = CreateTable(source);
		await LoadAsync(table);

		table.Select(3);
		var selected = items[3];

		var args = new List<SelectionChangedEventArgs>();
		table.SelectionChanged += (_, e) => args.Add(e);

		source.Filter(new TableViewPredicate(item => !ReferenceEquals(item, items[1])));
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(0, args.Count, "the item is still selected, so nothing is reported");
		Assert.AreSame(selected, table.SelectedItem);
		Assert.AreEqual(2, table.SelectedIndex, "the index follows the item");
		Assert.IsTrue(GetRow(table, 2)!.IsSelected);

		source.Filter(new TableViewPredicate(item => !ReferenceEquals(item, selected)));
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(1, args.Count, "dropping the selected item raises exactly one clear");
		Assert.AreSame(selected, args[0].RemovedItems.Single());
		Assert.AreEqual(0, args[0].AddedItems.Count);
		Assert.AreEqual(-1, table.SelectedIndex);
		Assert.IsNull(table.SelectedItem);
		Assert.IsTrue(GetRealizedRows(table).All(r => !r.IsSelected));
	}

	[TestMethod]
	public async Task When_Nested_Select_Wins()
	{
		// OnSelectionModelSelectionChanged's m_selectionVersion guard: an observer that selects something
		// else from inside PushSelectionProperties publishes and raises the newer selection, and the
		// outer pass must not overwrite it or raise a stale delta afterwards.
		var items = People(5);
		var table = CreateTable(items);
		await LoadAsync(table);

		table.Select(0);

		var args = new List<SelectionChangedEventArgs>();
		table.SelectionChanged += (_, e) => args.Add(e);

		var redirected = false;
		var token = table.RegisterPropertyChangedCallback(TableView.SelectedItemProperty, (_, _) =>
		{
			if (!redirected && ReferenceEquals(table.SelectedItem, items[1]))
			{
				redirected = true;
				table.Select(2);
			}
		});

		try
		{
			table.Select(1);
		}
		finally
		{
			table.UnregisterPropertyChangedCallback(TableView.SelectedItemProperty, token);
		}

		Assert.IsTrue(redirected);
		Assert.AreEqual(2, table.SelectedIndex);
		Assert.AreSame(items[2], table.SelectedItem);
		Assert.IsTrue(GetRow(table, 2)!.IsSelected);
		Assert.IsFalse(GetRow(table, 1)!.IsSelected);
		Assert.IsFalse(GetRow(table, 0)!.IsSelected);

		Assert.IsTrue(args.Count > 0);
		Assert.AreSame(items[2], args[^1].AddedItems.Single(), "the last reported selection is the nested one");
		Assert.IsFalse(args.Any(a => a.AddedItems.Contains(items[1])), "no stale event for the superseded selection");
	}

	[TestMethod]
	public async Task When_Replace_ItemsSource_Containing_Selected_Item()
	{
		// TableView_Selection.cpp ResolveSelectionAfterSourceChange seeds the restore from the sticky
		// anchor, so a selection that survives the swap by identity is re-selected at its new index.
		// (The API spec says a replacement always clears; the shipped code does not, and the code wins.)
		var items = People(6);
		var table = CreateTable(items);
		await LoadAsync(table);

		table.Select(2);
		var selected = items[2];

		var raised = 0;
		table.SelectionChanged += (_, _) => raised++;

		var replacement = new List<Person> { new("New", 1, "Lima"), selected };
		table.ItemsSource = replacement;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(1, table.SelectedIndex);
		Assert.AreSame(selected, table.SelectedItem);
		Assert.AreEqual(0, raised, "the same item stays selected, so nothing is reported");
	}

	[TestMethod]
	public async Task When_SelectionChanged_Args()
	{
		var items = People(5);
		var table = CreateTable(items);
		await LoadAsync(table);

		table.Select(0);

		var events = new List<(SelectionChangedEventArgs Args, int Index, object? Item, bool RowIsSelected)>();
		table.SelectionChanged += (sender, e) =>
		{
			Assert.AreSame(table, sender);
			events.Add((e, table.SelectedIndex, table.SelectedItem, GetRow(table, 1)?.IsSelected ?? false));
		};

		table.Select(1);

		Assert.AreEqual(1, events.Count, "one event carries the whole delta");
		var (args, index, item, rowIsSelected) = events[0];
		Assert.AreSame(items[0], args.RemovedItems.Single());
		Assert.AreSame(items[1], args.AddedItems.Single());
		Assert.AreEqual(1, index, "SelectedIndex is already the new value inside the handler");
		Assert.AreSame(items[1], item, "SelectedItem is already the new value inside the handler");
		Assert.IsTrue(rowIsSelected, "row chrome is restamped before the event");

		// Re-selecting the same row is not a change.
		table.Select(1);
		Assert.AreEqual(1, events.Count);
	}

	[TestMethod]
	public async Task When_Unload_Reload_Keeps_Selection()
	{
		var items = People(6);
		var table = CreateTable(items);
		var host = await LoadAsync(table);

		table.Select(3);

		var raised = 0;
		table.SelectionChanged += (_, _) => raised++;

		host.Children.Remove(table);
		await WindowHelper.WaitFor(() => !table.IsLoaded);

		host.Children.Add(table);
		await WindowHelper.WaitForLoaded(table);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(3, table.SelectedIndex);
		Assert.AreSame(items[3], table.SelectedItem);
		Assert.AreEqual(0, raised, "the round trip restores the same selection silently");
		Assert.IsTrue(GetRow(table, 3)!.IsSelected);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Row_VisualStates()
	{
		var table = CreateTable(People(5));
		await LoadAsync(table);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();

		// Park the pointer away from the rows.
		mouse.MoveTo(Center(GetHeaderCells(table)[0]));
		await WindowHelper.WaitForIdle();

		table.Select(1);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("Selected", GetCommonState(GetRow(table, 1)!));
		Assert.AreEqual("Normal", GetCommonState(GetRow(table, 0)!));

		mouse.MoveTo(Center(GetRow(table, 1)!));
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("SelectedPointerOver", GetCommonState(GetRow(table, 1)!));

		mouse.MoveTo(Center(GetRow(table, 0)!));
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("PointerOver", GetCommonState(GetRow(table, 0)!));
		Assert.AreEqual("Selected", GetCommonState(GetRow(table, 1)!));

		// IsSelected is read-only to apps; the control is its only writer.
		Assert.IsTrue(GetRow(table, 1)!.IsSelected);
		Assert.IsFalse(GetRow(table, 0)!.IsSelected);

		// Pressed / SelectedPressed hold until the release.
		mouse.Press(Center(GetRow(table, 1)!));
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("SelectedPressed", GetCommonState(GetRow(table, 1)!));
		mouse.Release();
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("SelectedPointerOver", GetCommonState(GetRow(table, 1)!));

		mouse.MoveTo(Center(GetRow(table, 0)!));
		mouse.Press(Center(GetRow(table, 0)!));
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("Pressed", GetCommonState(GetRow(table, 0)!));
		mouse.Release();
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("SelectedPointerOver", GetCommonState(GetRow(table, 0)!), "the release selected row 0");

		mouse.MoveTo(Center(GetHeaderCells(table)[0]));
		await WindowHelper.WaitForIdle();

		// Disabled wins over every other state (ListViewItem parity).
		table.IsEnabled = false;
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("SelectedDisabled", GetCommonState(GetRow(table, 0)!));
		Assert.AreEqual("Disabled", GetCommonState(GetRow(table, 1)!));

		table.IsEnabled = true;
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("Selected", GetCommonState(GetRow(table, 0)!));
		Assert.AreEqual("Normal", GetCommonState(GetRow(table, 1)!));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_GroupHeader_Pointer()
	{
		var items = new ObservableCollection<Person>
		{
			new("Ada", 30, "Oslo"),
			new("Bob", 31, "Kyoto"),
			new("Cy", 32, "Oslo"),
		};
		var source = TableViewSource.From(items).GroupBy(new TableViewKeySelector(item => ((Person)item!).City));
		var table = CreateTable(source);
		await LoadAsync(table, height: 600);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();

		mouse.MoveTo(Center(GetHeaderCells(table)[0]));
		await WindowHelper.WaitForIdle();

		var header = GetOsloHeader();
		Assert.IsTrue(header.IsExpandable);
		Assert.IsTrue(header.IsExpanded);
		Assert.AreEqual("Normal", GetVisualState(header, "CommonStates"));
		Assert.AreEqual("Expanded", GetVisualState(header, "ExpansionStates"));
		Assert.AreEqual("Expandable", GetVisualState(header, "ExpandabilityStates"));

		// A left click toggles on release inside: PointerOver -> Pressed -> PointerOver.
		var point = Center(header);
		mouse.MoveTo(point);
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("PointerOver", GetVisualState(header, "CommonStates"));

		mouse.Press(point);
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("Pressed", GetVisualState(header, "CommonStates"));
		Assert.IsTrue(GetOsloHeader().IsExpanded, "the toggle waits for the release");

		mouse.Release();
		await WindowHelper.WaitForIdle();
		header = GetOsloHeader();
		Assert.IsFalse(header.IsExpanded);
		Assert.AreEqual("Collapsed", GetVisualState(header, "ExpansionStates"));
		Assert.AreEqual(1, GetRealizedRows(table).Count, "only the Kyoto row remains");

		// A right click neither toggles nor reaches row selection.
		var selectedBefore = table.SelectedIndex;
		point = Center(header);
		mouse.PressRight(point);
		mouse.ReleaseRight();
		await WindowHelper.WaitForIdle();
		Assert.IsFalse(GetOsloHeader().IsExpanded);
		Assert.AreEqual(selectedBefore, table.SelectedIndex);

		// A press that drags off the band does not activate.
		header = GetOsloHeader();
		point = Center(header);
		mouse.MoveTo(point);
		mouse.Press(point);
		mouse.MoveTo(Center(GetHeaderCells(table)[0]), 4);
		mouse.Release();
		await WindowHelper.WaitForIdle();
		Assert.IsFalse(GetOsloHeader().IsExpanded);

		TableViewGroupHeader GetOsloHeader()
			=> GetRealizedGroupHeaders(table).Single(h => (string?)((TableViewGroupInfo)h.Content).Key == "Oslo");
	}

	private static string? GetCommonState(TableViewRow row)
		=> GetVisualState(row, "CommonStates");

	private static string? GetVisualState(Control control, string groupName)
	{
		var templateRoot = (FrameworkElement)VisualTreeHelper.GetChild(control, 0);
		return VisualStateManager.GetVisualStateGroups(templateRoot)
			.Single(g => g.Name == groupName)
			.CurrentState?.Name;
	}

	private static async Task PressAsync(string key)
	{
		await KeyboardHelper.PressKeySequence($"$d$_{key}#$u$_{key}");
		await WindowHelper.WaitForIdle();
	}

	// KeyboardStateTracker is process-wide; never let a failed test leave Ctrl down.
	private static async Task ReleaseCtrlAsync()
	{
		if (FocusManager.GetFocusedElement(WindowHelper.XamlRoot) is UIElement focused)
		{
			await KeyboardHelper.PressKeySequence("$u$_ctrl", focused);
		}
	}
}

#endif
