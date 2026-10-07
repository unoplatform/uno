// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Samples\TableViewSampleApp\SelectionPage.xaml.cs, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable
#pragma warning disable CS8305 // TableView is [Experimental]

using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp;

// Single row selection: pointer and keyboard gestures, the SelectedItem/SelectedIndex surface,
// and the two reshape behaviours that are easy to get wrong (selection follows the ITEM
// across an insert; a removed selected item CLEARS rather than sliding onto its neighbour).
[Sample("TableView", Name = "TableView_Selection")]
public sealed partial class SelectionPage : Page
{
	private readonly ObservableCollection<Item> _items = new(Data.Make(40));
	private bool _ready;   // guards the combo SelectionChanged that fires during XAML load

	// A running count, never reset by the button handlers. Several operations are specified NOT to
	// raise SelectionChanged - inserting above the selected row being the important one - and the
	// only way to see that from the UI is to watch this number stay put across the click.
	private int _selectionChangedCount;
	private string _lastDelta = "-";

	public SelectionPage()
	{
		this.InitializeComponent();

		Table.Columns.Add(SampleColumns.Text("Name", nameof(Item.Name), SampleColumns.Auto()));
		Table.Columns.Add(SampleColumns.Text("Role", nameof(Item.Role), SampleColumns.Auto()));
		Table.Columns.Add(SampleColumns.Text("City", nameof(Item.City), SampleColumns.Auto()));
		Table.Columns.Add(SampleColumns.Text("Score", nameof(Item.Score), SampleColumns.Auto()));

		Table.ItemsSource = _items;
		_ready = true;

		UpdateStatus();
	}

	private void Mode_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (!_ready)
		{
			return;
		}

		Table.SelectionMode = ModeCombo.SelectedIndex == 1
			? TableViewSelectionMode.None
			: TableViewSelectionMode.Single;

		UpdateStatus();
	}

	private void Table_SelectionChanged(TableView sender, SelectionChangedEventArgs args)
	{
		// One event carries the whole delta: replacing a selection reports both vectors.
		var removed = args.RemovedItems.Count == 0 ? "-" : ((Item)args.RemovedItems[0]).Name;
		var added = args.AddedItems.Count == 0 ? "-" : ((Item)args.AddedItems[0]).Name;

		_selectionChangedCount++;
		_lastDelta = $"removed: {removed}   added: {added}";

		UpdateStatus();
	}

	private void SelectFirst_Click(object sender, RoutedEventArgs e)
	{
		Table.Select(0);
		UpdateStatus();
	}

	private void SelectLast_Click(object sender, RoutedEventArgs e)
	{
		Table.Select(_items.Count - 1);
		UpdateStatus();
	}

	private void DeselectAll_Click(object sender, RoutedEventArgs e)
	{
		Table.DeselectAll();
		UpdateStatus();
	}

	// Selection follows the ITEM: the same row stays selected and only SelectedIndex shifts, so
	// this raises no SelectionChanged.
	private void InsertAbove_Click(object sender, RoutedEventArgs e)
	{
		var index = Table.SelectedIndex;
		if (index < 0)
		{
			UpdateStatus("nothing selected");
			return;
		}

		_items.Insert(index, new Item("INSERTED", "-", "-", 0, "", System.DateTimeOffset.Now, "", null));
		UpdateStatus();
	}

	// Removing the selected item CLEARS the selection rather than handing the app whichever row
	// slid into that slot.
	private void RemoveSelected_Click(object sender, RoutedEventArgs e)
	{
		if (Table.SelectedItem is not Item item)
		{
			UpdateStatus("nothing selected");
			return;
		}

		_items.Remove(item);
		UpdateStatus();
	}

	private void UpdateStatus(string? note = null)
	{
		if (StatusText is null)
		{
			return;
		}

		var selected = Table.SelectedItem as Item;
		var isSelected = Table.SelectedIndex >= 0 && Table.IsSelected(Table.SelectedIndex);

		StatusText.Text =
			$"SelectionMode={Table.SelectionMode}  SelectedIndex={Table.SelectedIndex}  " +
			$"SelectedItem={selected?.Name ?? "null"}  " +
			$"IsSelected={isSelected}  rows={_items.Count}\n" +
			$"SelectionChanged fired {_selectionChangedCount}x   last -> {_lastDelta}" +
			(note is null ? "" : $"\n{note}");
	}
}
