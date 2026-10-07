// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewRowAutomationPeer.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System.Collections.Generic;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewRowAutomationPeer
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TableViewRowAutomationPeer"/> class.
	/// </summary>
	/// <param name="owner">The <see cref="TableViewRow"/> to create a peer for.</param>
	public TableViewRowAutomationPeer(TableViewRow owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => typeof(TableViewRow).FullName!;

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		// Data rows are DataItems.
		return AutomationControlType.DataItem;
	}

	protected override object? GetPatternCore(PatternInterface patternInterface)
	{
		// SelectionItem is advertised only while the owner can actually select. A recycled row has no
		// owner, so it correctly advertises nothing.
		if (patternInterface == PatternInterface.SelectionItem)
		{
			if (GetOwningTableView() is { } tableView)
			{
				if (tableView.CanSelectRows())
				{
					return this;
				}
			}
		}

		return base.GetPatternCore(patternInterface);
	}

	private TableView? GetOwningTableView()
	{
		if (Owner is TableViewRow row)
		{
			return row.GetOwningTableView();
		}

		return null;
	}

	private int GetRowIndex()
	{
		var row = Owner as TableViewRow;
		var tableView = GetOwningTableView();
		if (row is null || tableView is null)
		{
			return -1;
		}

		if (tableView.GetRowsRepeaterInternal() is { } repeater)
		{
			return repeater.GetElementIndex(row);
		}

		return -1;
	}

	// ----- ISelectionItemProvider -----

	/// <summary>
	/// Gets a value that indicates whether the row is selected.
	/// </summary>
	public bool IsSelected
	{
		get
		{
			if (Owner is TableViewRow row)
			{
				return row.IsSelected;
			}

			return false;
		}
	}

	/// <summary>
	/// Gets the UI Automation provider of the owning <see cref="TableView"/>.
	/// </summary>
	public IRawElementProviderSimple? SelectionContainer
	{
		get
		{
			if (GetOwningTableView() is { } tableView)
			{
				if (FrameworkElementAutomationPeer.CreatePeerForElement(tableView) is { } peer)
				{
					return ProviderFromPeer(peer);
				}
			}

			return null;
		}
	}

	/// <summary>
	/// Adds the row to the selection. The container is single-select, so this makes the row the selection.
	/// </summary>
	public void AddToSelection()
	{
		// The container is single-select, so "add" can only mean "make this the selection". XAML's own
		// single-select peers (ListViewItemAutomationPeer) behave the same way rather than failing.
		// Multiple must add to the selection instead of replacing it.
		Select();
	}

	/// <summary>
	/// Removes the row from the selection.
	/// </summary>
	public void RemoveFromSelection()
	{
		if (GetOwningTableView() is { } tableView)
		{
			int index = GetRowIndex();
			if (index >= 0)
			{
				// Deselect only clears when THIS row is the selection, so a stale UIA call
				// cannot wipe out a selection the user has since moved elsewhere.
				tableView.Deselect(index);
			}
		}
	}

	/// <summary>
	/// Makes the row the current selection.
	/// </summary>
	public void Select()
	{
		if (GetOwningTableView() is { } tableView)
		{
			int index = GetRowIndex();
			if (index >= 0)
			{
				// toggle=false: UIA Select() means "make this the selection", never "clear it".
				tableView.SelectRowIndexFromInteraction(index, false);
			}
		}
	}

	// See header comment for rationale.
	protected override IList<AutomationPeer> GetChildrenCore()
	{
		List<AutomationPeer> children = new();

		var row = Owner as TableViewRow;
		if (row is null)
		{
			return children;
		}

		var rowImpl = row;
		if (rowImpl is null)
		{
			return children;
		}

		var cellsHost = rowImpl.GetCellsHostPanelInternal();
		if (cellsHost is null)
		{
			// Mid-realize fallback: expose row chrome rather than an empty vector.
			return base.GetChildrenCore();
		}

		var cellChildren = cellsHost.Children;
		var count = cellChildren.Count;
		int visibleColumnIndex = 0;
		for (int i = 0; i < count; ++i)
		{
			if (cellChildren[i] is UIElement cellElement)
			{
				var column = rowImpl.GetCellOwningColumn(cellElement);
				if (column is null || column.Visibility != Visibility.Visible)
				{
					continue;
				}

				if (cellElement is FrameworkElement cellFE)
				{
					// Dedicated cell peers provide names, coordinates, and header references.
					AutomationPeer cellPeer =
						new TableViewCellAutomationPeer(cellFE, row, column, visibleColumnIndex);
					children.Add(cellPeer);
				}
				++visibleColumnIndex;
			}
		}

		return children;
	}
}
