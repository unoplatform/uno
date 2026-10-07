// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewRowAutomationPeer.cpp, tag winui3/main, commit dc28206ea35

#nullable enable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewRowAutomationPeer
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TableViewRowAutomationPeer"/> class.
	/// </summary>
	/// <param name="owner">The <see cref="TableViewRow"/> to create a peer for.</param>
	public TableViewRowAutomationPeer(TableViewRow owner) : base(owner)
	{
		if (owner is not null)
		{
			if (owner.GetOwningTableView() is { } tableView)
			{
				TrackRowItem(owner, tableView);
			}
		}
		GetRowIndex();
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

		if (patternInterface == PatternInterface.VirtualizedItem && IsVirtualized())
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	private TableView? GetOwningTableView()
	{
		if (Owner is TableViewRow row)
		{
			var tableView = row.GetOwningTableView();
			if (tableView is not null && IsTrackedRow(row, tableView))
			{
				m_lastOwningTable = new(tableView);
				return tableView;
			}
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
			if (repeater.GetElementIndex(row) is var rowIndex && rowIndex >= 0)
			{
				m_lastOwningTable = new(tableView);
				m_lastKnownRowIndex = rowIndex;
				return rowIndex;
			}
		}

		return -1;
	}

	private bool IsVirtualized()
	{
		if (GetRowIndex() >= 0)
		{
			return false;
		}

		TableView? tableView = null;
		if (m_lastOwningTable is null || !m_lastOwningTable.TryGetTarget(out tableView) || m_lastKnownRowIndex < 0)
		{
			return false;
		}

		var tableImpl = tableView;
		var rowIndex = GetTrackedItemIndex(tableView);
		if (rowIndex < 0 || rowIndex >= tableImpl.GetRowCountInternal())
		{
			return false;
		}

		var repeater = tableImpl.GetRowsRepeaterInternal();
		bool isVirtualized = repeater is not null && repeater.TryGetElement(rowIndex) is null;
		if (isVirtualized)
		{
			m_lastKnownRowIndex = rowIndex;
		}
		return isVirtualized;
	}

	/// <summary>
	/// Makes the virtualized row fully accessible as a UI Automation element.
	/// </summary>
	void IVirtualizedItemProvider.Realize()
	{
		if (!IsVirtualized())
		{
			return;
		}

		// IVirtualizedItemProvider::Realize is synchronous: on return the client immediately re-queries
		// this provider and expects the element realized, with VirtualizedItem no longer supported.
		// Deferring to the dispatcher hands the client a still-virtualized provider.
		RealizeCore();
	}

	private void RealizeCore()
	{
		TableView? tableView = null;
		m_lastOwningTable?.TryGetTarget(out tableView);
		var rowIndex = tableView is not null ? GetTrackedItemIndex(tableView) : -1;
		if (tableView is null || rowIndex < 0)
		{
			ThrowElementNotAvailable();
		}

		var tableImpl = tableView;
		if (rowIndex >= tableImpl.GetRowCountInternal())
		{
			ThrowElementNotAvailable();
		}

		var repeater = tableImpl.GetRowsRepeaterInternal();
		if (repeater is null)
		{
			ThrowElementNotAvailable();
		}

		UIElement? element = null;
		try
		{
			element = repeater.TryGetElement(rowIndex);
			if (element is null)
			{
				element = repeater.GetOrCreateElement(rowIndex);
			}
		}
		catch
		{
			ThrowElementNotAvailable();
		}

		if (element is TableViewRow row)
		{
			if (!IsTrackedRow(row, tableView))
			{
				ThrowElementNotAvailable();
			}
		}
		else
		{
			ThrowElementNotAvailable();
		}

		if (element is FrameworkElement frameworkElement)
		{
			frameworkElement.StartBringIntoView();
		}
	}

#pragma warning disable IDE0051 // Unused upstream as well, kept for 1:1 parity
	private object? GetTrackedItem() => m_item.Resolve();
#pragma warning restore IDE0051

	private int GetTrackedItemIndex(TableView? tableView)
	{
		if (tableView is null || !m_item.IsTracking())
		{
			return -1;
		}

		var tableImpl = tableView;
		var repeater = tableImpl.GetRowsRepeaterInternal();
		var view = repeater?.ItemsSourceView;
		if (view is null)
		{
			return -1;
		}

		int count = view.Count;
		if (m_lastKnownRowIndex >= 0 && m_lastKnownRowIndex < count)
		{
			if (tableImpl.UnwrapEditingDataItem(view.GetAt(m_lastKnownRowIndex)) is var candidate &&
				m_item.SameIdentityAs(candidate))
			{
				return m_lastKnownRowIndex;
			}
		}

		int occurrence = 0;
		for (int index = 0; index < count; ++index)
		{
			if (tableImpl.UnwrapEditingDataItem(view.GetAt(index)) is var candidate &&
				m_item.SameIdentityAs(candidate))
			{
				if (m_trackedItemOccurrence < 0 || occurrence == m_trackedItemOccurrence)
				{
					return index;
				}
				++occurrence;
			}
		}

		return -1;
	}

	private int GetItemOccurrenceAtIndex(
		TableView? tableView,
		object? item,
		int targetIndex)
	{
		if (tableView is null || item is null || targetIndex < 0)
		{
			return -1;
		}

		var tableImpl = tableView;
		var repeater = tableImpl.GetRowsRepeaterInternal();
		var view = repeater?.ItemsSourceView;
		if (view is null || targetIndex >= view.Count)
		{
			return -1;
		}

		int occurrence = 0;
		for (int index = 0; index <= targetIndex; ++index)
		{
			if (tableImpl.UnwrapEditingDataItem(view.GetAt(index)) is { } candidate &&
				TableView.SameInspectableIdentity(candidate, item))
			{
				if (index == targetIndex)
				{
					return occurrence;
				}
				++occurrence;
			}
		}

		return -1;
	}

	private bool IsTrackedRow(TableViewRow? row, TableView? tableView)
	{
		if (row is null || tableView is null)
		{
			return false;
		}

		var rowItem = tableView.UnwrapEditingDataItem(row.DataContext);
		if (rowItem is null)
		{
			return false;
		}

		if (!m_item.IsTracking())
		{
			TrackRowItem(row, tableView);
			return true;
		}

		if (!m_item.SameIdentityAs(rowItem))
		{
			return false;
		}

		if (tableView.GetRowsRepeaterInternal() is { } repeater)
		{
			if (repeater.GetElementIndex(row) is var rowIndex && rowIndex >= 0)
			{
				var occurrence = GetItemOccurrenceAtIndex(tableView, rowItem, rowIndex);
				if (m_trackedItemOccurrence >= 0 && occurrence != m_trackedItemOccurrence)
				{
					return false;
				}

				TrackRowItem(row, tableView);
			}
		}

		return true;
	}

	private void TrackRowItem(TableViewRow? row, TableView? tableView)
	{
		if (row is not null && tableView is not null)
		{
			if (tableView.UnwrapEditingDataItem(row.DataContext) is { } item)
			{
				m_item.Track(item);
				m_trackedItemOccurrence = -1;
				if (tableView.GetRowsRepeaterInternal() is { } repeater)
				{
					if (repeater.GetElementIndex(row) is var rowIndex && rowIndex >= 0)
					{
						m_lastKnownRowIndex = rowIndex;
						m_trackedItemOccurrence = GetItemOccurrenceAtIndex(tableView, item, rowIndex);
					}
				}
			}
		}
	}

	internal bool CanReuseForRowItem(
		TableViewRow? row,
		TableView? tableView)
	{
		if (row is null || tableView is null)
		{
			return false;
		}

		if (!m_item.IsTracking())
		{
			return true;
		}

		var item = tableView.UnwrapEditingDataItem(row.DataContext);
		return m_item.SameIdentityAs(item);
	}

	internal void TrackCurrentRowItem(TableViewRow? row, TableView? tableView) => TrackRowItem(row, tableView);

	internal void DropCellPeerCache() => m_cellPeerCache.Clear();

	// UIA_E_ELEMENTNOTAVAILABLE projects to ElementNotAvailableException.
	[DoesNotReturn]
	private static void ThrowElementNotAvailable() => throw new ElementNotAvailableException();

	protected override string GetNameCore()
	{
		var row = Owner as TableViewRow;
		if (row is null)
		{
			return string.Empty;
		}
		if (GetOwningTableView() is null)
		{
			ThrowElementNotAvailable();
		}

		// Preserve explicit app metadata, not a base peer's Content/DataContext stringification.
		if (AutomationProperties.GetName(Owner) is { Length: > 0 } name)
		{
			return name;
		}
		if (GetLabeledBy() is { } label)
		{
			if (label.GetName() is { Length: > 0 } labelName)
			{
				return labelName;
			}
		}

		var rowImpl = row;
		var cellsHost = rowImpl?.GetCellsHostPanelInternal();
		if (rowImpl is null || cellsHost is null)
		{
			return string.Empty;
		}

		// First collect cheap visible-cell text without creating peers; this runs on every UIA name query.
		string composed = ComposeCellTexts(rowImpl, cellsHost, false /* allowPeerCreation */);
		if (composed.Length == 0)
		{
			// Only nameless rows fall back to bounded peer creation for visible template content.
			composed = ComposeCellTexts(rowImpl, cellsHost, true /* allowPeerCreation */);
		}

		if (composed.Length == 0)
		{
			// Template content with no name of its own either. The data item is the last thing left
			// that can distinguish this row from its neighbours.
			return TableViewAutomationHelpers.ItemToName(row.DataContext);
		}

		return composed;
	}

	private static string ComposeCellTexts(
		TableViewRow rowImpl,
		Panel cellsHost,
		bool allowPeerCreation)
	{
		StringBuilder composed = new();
		var separator = TableViewAutomationHelpers.LocalizedOrFallbackForTableViewAutomation(ResourceAccessor.SR_TableViewCellTextSeparator, ", ");
		var cellChildren = cellsHost.Children;
		var count = cellChildren.Count;
		for (int i = 0; i < count; ++i)
		{
			var cellElement = cellChildren[i] as UIElement;
			if (cellElement is null || !TableViewAutomationHelpers.IsVisibleColumn(rowImpl.GetCellOwningColumn(cellElement)))
			{
				continue;
			}

			var text = TableViewAutomationHelpers.GetCellDisplayText(cellElement as FrameworkElement, allowPeerCreation);
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}

			if (composed.Length > 0)
			{
				composed.Append(separator);
			}
			composed.Append(text);
		}

		return composed.ToString();
	}

	protected override int GetPositionInSetCore()
	{
		// An app-set AutomationProperties value wins, as in every dxaml peer that computes this.
		if (base.GetPositionInSetCore() is var provided && provided > 0)
		{
			return provided;
		}

		var index = GetRowIndex();
		if (index < 0)
		{
			// 0 is UIA's "not specified"; -1 would reach the client verbatim.
			return 0;
		}

		if (GetOwningTableView() is { } tableView)
		{
			return tableView.GetDataRowPositionInSetInternal(index);
		}

		return 0;
	}

	protected override int GetSizeOfSetCore()
	{
		if (base.GetSizeOfSetCore() is var provided && provided > 0)
		{
			return provided;
		}

		if (GetOwningTableView() is { } tableView)
		{
			if (tableView.GetDataRowSizeOfSetInternal() is var count && count > 0)
			{
				return count;
			}
		}

		return 0;
	}

	internal AutomationPeer? GetOrCreateCellPeer(
		FrameworkElement? cell)
	{
		if (cell is null)
		{
			return null;
		}

		foreach (var entry in m_cellPeerCache)
		{
			if (entry.peer is not null && entry.cell is not null && entry.cell.TryGetTarget(out var cachedCell) && cachedCell == cell)
			{
				return entry.peer;
			}
		}

		var row = Owner as TableViewRow;
		if (row is null || GetOwningTableView() is null)
		{
			return null;
		}

		// Pruned here as well as on the GetChildrenCore rebuild: a client that only addresses cells
		// through IGridProvider::GetItem never walks children, so this is its only prune point.
		m_cellPeerCache.RemoveAll(
			entry => entry.peer is null || entry.cell is null || !entry.cell.TryGetTarget(out _));

		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(cell);
		if (peer is null)
		{
			return null;
		}
		// GetChildren normally establishes this relationship, but Grid.GetItem can be
		// the first and only acquisition route. Connect that same peer before publishing it.
		peer.SetParent(this);
		m_cellPeerCache.Add(new(cell, peer));
		return peer;
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
				if (GetOwningTableView() is null)
				{
					return false;
				}
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
		if (GetOwningTableView() is null)
		{
			ThrowElementNotAvailable();
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

		// Rebuilt wholesale: peers for cells dropped by a rebuild or recycle are released, while a
		// surviving cell keeps the same peer and therefore the same provider identity.
		List<CellPeerCacheEntry> liveCache = new(count);

		for (int i = 0; i < count; ++i)
		{
			if (cellChildren[i] is UIElement cellElement)
			{
				var column = rowImpl.GetCellOwningColumn(cellElement);
				if (!TableViewAutomationHelpers.IsVisibleColumn(column))
				{
					continue;
				}

				if (cellElement is FrameworkElement cellFE)
				{
					// Dedicated cell peers provide names, coordinates, and header references.
					if (GetOrCreateCellPeer(cellFE) is { } cellPeer)
					{
						liveCache.Add(new(cellFE, cellPeer));
						children.Add(cellPeer);
					}
				}
			}
		}

		m_cellPeerCache = liveCache;

		return children;
	}
}
