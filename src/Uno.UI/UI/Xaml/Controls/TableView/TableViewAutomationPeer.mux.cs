// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewAutomationPeer.cpp, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;

using static Microsoft.UI.Xaml.Controls.Tabular.TableViewAutomationHelpers;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewAutomationPeer
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TableViewAutomationPeer"/> class.
	/// </summary>
	/// <param name="owner">The <see cref="TableView"/> to create a peer for.</param>
	public TableViewAutomationPeer(TableView owner) : base(owner)
	{
	}

	// A realized row's name as composed from its text cells, read from the item without realizing the row.
	private static string ComposeTextColumnValues(TableView table, object? item)
	{
		global::System.Text.StringBuilder composed = new();
		var separator = LocalizedOrFallbackForTableViewAutomation(ResourceAccessor.SR_TableViewCellTextSeparator, ", ");
		TextBlock? probe = null;
		foreach (var column in table.Columns)
		{
			var textColumn = IsVisibleColumn(column) ? column as TableViewTextColumn : null;
			var binding = textColumn?.Binding;
			// A detached probe cannot resolve element-relative sources.
			if (binding is null || !string.IsNullOrEmpty(binding.ElementName) || binding.RelativeSource is not null)
			{
				continue;
			}
			if (probe is null)
			{
				probe = new TextBlock();
				// Converters and StringFormat format for the target's language, which a cell inherits.
				probe.Language = table.Language;
			}
			string text = string.Empty;
			try
			{
				BindingOperations.SetBinding(probe, TextBlock.TextProperty, binding);
				probe.DataContext = item;
				text = probe.Text;
				probe.ClearValue(TextBlock.TextProperty);
				probe.DataContext = null;
			}
			catch (Exception)
			{
				// An app converter that throws only costs that column's text.
			}
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

	// A container-free data-item peer for a selected, unrealized row. Reads never realize it; once
	// realized it forwards to the row's peer but keeps its own RuntimeId.
	private sealed class TableViewRowItemAutomationPeer :
		AutomationPeer,
		ISelectionItemProvider,
		IVirtualizedItemProvider,
		IScrollItemProvider
	{
		public TableViewRowItemAutomationPeer(TableView table, object? item, int index, bool byIndex)
		{
			m_table = new WeakReference<TableView>(table);
			// Boxed values have no stable identity: such a peer tracks its index while that row stays selected.
			m_byIndex = byIndex;
			m_lastIndex = index;
			m_item.Track(item);
		}

		// Rebinding to the selected index picks the right occurrence of a duplicated item.
		public bool TryRebind(object? item, int index)
		{
			if (m_byIndex ? m_lastIndex != index : !m_item.SameIdentityAs(item))
			{
				return false;
			}
			m_lastIndex = index;
			return true;
		}

		protected override object? GetPatternCore(PatternInterface patternInterface)
		{
			int index = ItemIndex();
			var rowPeer = RealizedRowPeer(index);
			if (patternInterface == PatternInterface.SelectionItem ||
				patternInterface == PatternInterface.VirtualizedItem)
			{
				bool offered = patternInterface == PatternInterface.SelectionItem
					? index >= 0 && m_table.Get()!.CanSelectRows()
					: index >= 0 && rowPeer is null;
				if (offered)
				{
					return this;
				}
				return null;
			}
			// The row peer's ScrollItem is a framework default that GetPattern does not return.
			if (patternInterface == PatternInterface.ScrollItem)
			{
				if (rowPeer is not null)
				{
					return this;
				}
				return null;
			}
			return rowPeer is not null ? rowPeer.GetPattern(patternInterface) : base.GetPatternCore(patternInterface);
		}

		protected override object? NavigateCore(AutomationNavigationDirection direction)
		{
			if (direction == AutomationNavigationDirection.Parent)
			{
				return RefreshParent();
			}
			return base.NavigateCore(direction);
		}

		// Parented where realized rows are; re-resolved on use, as the template may have been reapplied.
		public AutomationPeer? RefreshParent()
		{
			var table = m_table.Get();
			if (table is null)
			{
				return null;
			}
			AutomationPeer? parent = FrameworkElementAutomationPeer.CreatePeerForElement(table);
			if (table.GetRowsRepeaterInternal() is { } repeater && VisualTreeHelper.GetChildrenCount(repeater) > 0)
			{
				if (VisualTreeHelper.GetChild(repeater, 0) is UIElement rowElement &&
					FrameworkElementAutomationPeer.CreatePeerForElement(rowElement) is { } rowPeer)
				{
					if (rowPeer.Navigate(AutomationNavigationDirection.Parent) is AutomationPeer rowParent)
					{
						parent = rowParent;
					}
				}
			}
			SetParent(parent);
			return parent;
		}

		protected override string GetClassNameCore() => typeof(TableViewRow).FullName!;
		protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataItem;
		protected override bool IsContentElementCore() => true;
		protected override bool IsControlElementCore() => true;

		protected override string GetNameCore()
		{
			int index = RequireIndex();
			// A realized row composes its name from its cells, so it can differ from ItemToName.
			if (RealizedRowPeer(index) is { } rowPeer)
			{
				return rowPeer.GetName();
			}
			// Read from the source: m_item may be a weak reference to a box nothing else keeps alive.
			var tableImpl = m_table.Get()!;
			var item = tableImpl.UnwrapEditingDataItem(tableImpl.GetRowsRepeaterInternal()!.ItemsSourceView.GetAt(index));
			var composed = ComposeTextColumnValues(tableImpl, item);
			if (!string.IsNullOrEmpty(composed))
			{
				return composed;
			}
			return ItemToName(item);
		}

		protected override bool IsEnabledCore()
		{
			if (RealizedRowPeer(ItemIndex()) is { } p)
			{
				return p.IsEnabled();
			}
			var table = m_table.Get();
			return table is null || table.IsEnabled;
		}

		protected override int GetCultureCore()
		{
			AutomationPeer? p = RealizedRowPeer(ItemIndex());
			if (p is null)
			{
				if (m_table.Get() is { } table)
				{
					p = FrameworkElementAutomationPeer.CreatePeerForElement(table);
				}
			}
			return p is not null ? p.GetCulture() : base.GetCultureCore();
		}

		protected override bool IsOffscreenCore() { var p = RealizedRowPeer(ItemIndex()); return p is null || p.IsOffscreen(); }
		protected override Rect GetBoundingRectangleCore() { var p = RealizedRowPeer(ItemIndex()); return p is not null ? p.GetBoundingRectangle() : default; }
		// Not forwarded: the framework re-parents returned children, which would steal the row's cell peers.
		protected override IList<AutomationPeer>? GetChildrenCore() => null;
		protected override Point GetClickablePointCore() { var p = RealizedRowPeer(ItemIndex()); return p is not null ? p.GetClickablePoint() : default; }
		protected override bool HasKeyboardFocusCore() { var p = RealizedRowPeer(ItemIndex()); return p is not null && p.HasKeyboardFocus(); }
		protected override bool IsKeyboardFocusableCore() { var p = RealizedRowPeer(ItemIndex()); return p is not null && p.IsKeyboardFocusable(); }
		protected override void SetFocusCore() { if (RealizedRowPeer(ItemIndex()) is { } p) { p.SetFocus(); } }

		public bool IsSelected
		{
			get
			{
				// Resolved first: it throws when the table is gone, before the table is dereferenced.
				int index = RequireIndex();
				return m_table.Get()!.SelectedIndexInternal() == index;
			}
		}

		public IRawElementProviderSimple? SelectionContainer
		{
			get
			{
				RequireIndex();
				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(m_table.Get()!);
				return peer is not null ? ProviderFromPeer(peer) : null;
			}
		}

		public void AddToSelection() { Select(); }

		public void RemoveFromSelection()
		{
			int index = RequireIndex();
			m_table.Get()!.Deselect(index);
		}

		public void Select()
		{
			int index = RequireIndex();
			m_table.Get()!.SelectRowIndexFromInteraction(index, false);
		}

		public void Realize()
		{
			// An ElementPrepared handler can rebind this peer to another occurrence while the row is created.
			int index = RequireIndex();
			TableViewRowAutomationPeer.RealizeRowAt(m_table.Get(), index,
				row => IsItemRow(row) && ItemIndex() == index);
		}

		public void ScrollIntoView()
		{
			var row = RealizedRow(RequireIndex());
			if (row is null)
			{
				ThrowElementNotAvailable();
			}
			BringIntoViewOptions options = new();
			options.AnimationDesired = false;
			row.StartBringIntoView(options);
		}

		// -1 once the item left the source (or a boxed value's row was deselected); the table is alive when >= 0.
		private int ItemIndex()
		{
			var tableImpl = m_table.Get();
			var repeater = tableImpl?.GetRowsRepeaterInternal();
			var view = repeater?.ItemsSourceView;
			if (tableImpl is null || view is null)
			{
				return -1;
			}

			int count = view.Count;
			int selectedIndex = tableImpl.SelectedIndexInternal();
			if (m_byIndex)
			{
				return m_lastIndex < count && m_lastIndex == selectedIndex ? m_lastIndex : -1;
			}

			bool matches(int index)
			{
				return index >= 0 && index < count &&
					m_item.SameIdentityAs(tableImpl.UnwrapEditingDataItem(view.GetAt(index)));
			}
			// The selected index first: after an insert, m_lastIndex can hold another occurrence of the item.
			foreach (int hint in stackalloc int[] { selectedIndex, m_lastIndex })
			{
				if (matches(hint))
				{
					return m_lastIndex = hint;
				}
			}
			for (int index = 0; index < count; ++index)
			{
				if (matches(index))
				{
					return m_lastIndex = index;
				}
			}
			return -1;
		}

		private int RequireIndex()
		{
			int index = ItemIndex();
			if (index < 0)
			{
				ThrowElementNotAvailable();
			}
			return index;
		}

		private bool IsItemRow(TableViewRow row)
		{
			return m_byIndex ||
				m_item.SameIdentityAs(m_table.Get()!.UnwrapEditingDataItem(row.DataContext));
		}

		private TableViewRow? RealizedRow(int index)
		{
			if (index < 0)
			{
				return null;
			}
			var row = m_table.Get()!.GetRowsRepeaterInternal()!.TryGetElement(index) as TableViewRow;
			return row is not null && IsItemRow(row) ? row : null;
		}

		private AutomationPeer? RealizedRowPeer(int index)
		{
			var row = RealizedRow(index);
			return row is not null ? FrameworkElementAutomationPeer.CreatePeerForElement(row) : null;
		}

		private readonly WeakReference<TableView> m_table;
		private readonly TableViewTrackedItemIdentity m_item = new();
		private readonly bool m_byIndex;
		private int m_lastIndex = -1;
	}

	private static int VisibleColumnToChildIndex(IList<UIElement> children, int visibleColumnIndex)
	{
		int currentVisibleIndex = 0;
		var count = children.Count;
		for (int i = 0; i < count; ++i)
		{
			var cellElement = children[i] as FrameworkElement;
			TableViewColumn? column = null;
			if (cellElement is not null)
			{
				column = cellElement.Tag as TableViewColumn;
			}
			if (!IsVisibleColumn(column))
			{
				continue;
			}

			if (currentVisibleIndex == visibleColumnIndex)
			{
				return i;
			}
			++currentVisibleIndex;
		}
		return -1;
	}

	protected override object? GetPatternCore(PatternInterface patternInterface)
	{
		if (Owner is TableView)
		{
			// Grid + Table are always advertised — they describe shape, not state.
			if (patternInterface == PatternInterface.Grid ||
				patternInterface == PatternInterface.Table)
			{
				return this;
			}

			// ItemContainer lets AT clients enumerate beyond realized rows.
			if (patternInterface == PatternInterface.ItemContainer)
			{
				return this;
			}

			// Selection is advertised only when the control can actually select. Advertising it while
			// SelectionMode is None would tell an AT client the grid is selectable when every Select()
			// it issues would be refused.
			if (patternInterface == PatternInterface.Selection)
			{
				if (GetImpl() is { } impl && impl.CanSelectRows())
				{
					return this;
				}
			}

			// Forward Scroll to the body ScrollViewer's peer.
			if (patternInterface == PatternInterface.Scroll)
			{
				if (GetImpl() is { } impl)
				{
					if (impl.GetBodyScrollerInternal() is { } bodyScroller)
					{
						if (FrameworkElementAutomationPeer.CreatePeerForElement(bodyScroller) is { } svPeer)
						{
							if (svPeer.GetPattern(PatternInterface.Scroll) is { } provider)
							{
								return provider;
							}
						}
					}
				}
			}
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore() => typeof(TableView).FullName!;

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataGrid;

	// The default bounds include cache rows the scroller clips; best-effort clip to the layout box.
	protected override Rect GetBoundingRectangleCore()
	{
		var bounds = base.GetBoundingRectangleCore();
		if (bounds.Width <= 0.0 || bounds.Height <= 0.0)
		{
			return bounds;
		}

		var owner = Owner as FrameworkElement;
		if (owner is null || owner.XamlRoot is null)
		{
			return bounds;
		}

		Rect localBounds = new(
			0.0,
			0.0,
			owner.ActualWidth,
			owner.ActualHeight);
		if (localBounds.Width <= 0.0 || localBounds.Height <= 0.0)
		{
			return bounds;
		}

		Rect layoutBounds = default;
		try
		{
			layoutBounds = SharedHelpers.ConvertDipsToPhysical(
				owner, owner.TransformToVisual(null).TransformBounds(localBounds));
		}
		catch (Exception)
		{
			// TransformToVisual throws while the owner is leaving the tree (teardown, window close).
			return bounds;
		}

		// Intersect reports no overlap as RectHelper::Empty() (infinite extents); never return that.
		var clipped = RectHelper.Intersect(bounds, layoutBounds);
		if (!(clipped.Width > 0.0) || !(clipped.Height > 0.0) ||
			!double.IsFinite(clipped.X) || !double.IsFinite(clipped.Y) ||
			!double.IsFinite(clipped.Width) || !double.IsFinite(clipped.Height))
		{
			return bounds;
		}

		return clipped;
	}

	protected override void ShowContextMenuCore()
	{
		// The base raises ContextRequested on the TableView itself, which shows its own
		// ContextFlyout before the template-root suppression hook can see it.
		if (GetImpl() is { } impl && impl.AreContextMenusSuppressed())
		{
			return;
		}

		base.ShowContextMenuCore();
	}

	internal void RaiseStructureChangedForSortChange()
	{
		// Same children, new order.
		RaiseStructureChanged(AutomationStructureChangeType.ChildrenReordered);
	}

	internal void RaiseStructureChangedForVirtualizationReset()
	{
		// Membership or bucketing changed, so any cached child could be gone.
		RaiseStructureChanged(AutomationStructureChangeType.ChildrenInvalidated);
	}

	internal void RaiseStructureChangedForGroupExpansion()
	{
		RaiseStructureChanged(AutomationStructureChangeType.ChildrenInvalidated);
	}

	internal void RaiseStructureChangedForColumnsChange()
	{
		// A column isn't a child of this peer, so ChildAdded/ChildRemoved don't apply.
		RaiseStructureChanged(AutomationStructureChangeType.ChildrenInvalidated);
	}

	private void RaiseStructureChanged(AutomationStructureChangeType structureChangeType)
	{
		RaiseStructureChangedEvent(structureChangeType, null);
	}

	private TableView? GetImpl()
	{
		TableView? impl = null;

		if (Owner is TableView tableView)
		{
			impl = tableView;
		}

		return impl;
	}

	/// <summary>
	/// Gets the total number of rows in the grid.
	/// </summary>
	public int RowCount
	{
		get
		{
			if (GetImpl() is { } impl)
			{
				return impl.GetRowCountInternal();
			}
			return 0;
		}
	}

	/// <summary>
	/// Gets the total number of visible columns in the grid.
	/// </summary>
	public int ColumnCount
	{
		get
		{
			if (Owner is TableView tableView)
			{
				if (tableView.Columns is { } cols)
				{
					return CountVisibleColumns(cols);
				}
			}
			return 0;
		}
	}

	/// <summary>
	/// Retrieves the UI Automation provider for the specified cell.
	/// </summary>
	/// <param name="row">The ordinal number of the row of interest.</param>
	/// <param name="column">The ordinal number of the visible column of interest.</param>
	/// <returns>The UI Automation provider for the specified cell, or null.</returns>
	public IRawElementProviderSimple? GetItem(int row, int column)
	{
		if (row < 0 || column < 0)
		{
			return null;
		}

		var impl = GetImpl();
		if (impl is null)
		{
			return null;
		}

		if (row >= impl.GetRowCountInternal() || column >= ColumnCount)
		{
			return null;
		}

		var repeater = impl.GetRowsRepeaterInternal();
		if (repeater is null)
		{
			return null;
		}

		UIElement? element = null;
		try
		{
			element = repeater.TryGetElement(row);
		}
		catch (Exception)
		{
			return null;
		}

		if (element is null)
		{
			// Bounded realization — realize ONLY the single requested row (not a scan)
			// so IGridProvider.GetItem honors the UIA grid contract (Narrator cell addressing)
			// without force-realizing the whole dataset.
			// Full sweep-virtualization would require exposing VirtualizedItemPattern.
			try
			{
				element = repeater.GetOrCreateElement(row);
			}
			catch (Exception)
			{
				return null;
			}
		}

		if (element is null)
		{
			return null;
		}

		if (element is TableViewGroupHeader header)
		{
			if (FrameworkElementAutomationPeer.CreatePeerForElement(header) is { } headerPeer)
			{
				return ProviderFromPeer(headerPeer);
			}
			return null;
		}

		var rowElement = element as TableViewRow;
		if (rowElement is null)
		{
			return null;
		}

		var rowImpl = rowElement;
		if (rowImpl is null)
		{
			return null;
		}

		var cellsHost = rowImpl.GetCellsHostPanelInternal();
		if (cellsHost is null)
		{
			return null;
		}

		var cellChildren = cellsHost.Children;
		int childIndex = VisibleColumnToChildIndex(cellChildren, column);
		if (childIndex < 0)
		{
			return null;
		}

		var cellElement = cellChildren[childIndex] as UIElement;
		if (cellElement is null)
		{
			return null;
		}

		if (cellElement is FrameworkElement cellFE)
		{
			// Route through the row peer so Grid.GetItem and tree navigation share provider identity.
			if (FrameworkElementAutomationPeer.CreatePeerForElement(rowElement) is TableViewRowAutomationPeer rowPeer)
			{
				if (rowPeer.GetOrCreateCellPeer(cellFE) is { } cellPeer)
				{
					return ProviderFromPeer(cellPeer);
				}
			}

			// A fresh peer would not match editing/tree navigation; custom row peers may expose no item.
		}
		return null;
	}

	/// <summary>
	/// Gets the primary direction of traversal for the table.
	/// </summary>
	public Microsoft.UI.Xaml.Automation.RowOrColumnMajor RowOrColumnMajor
	{
		get
		{
			// Tabular data flows row-by-row across columns — narrators reading
			// sequentially consume rows in order, so RowMajor matches the visual model.
			return Microsoft.UI.Xaml.Automation.RowOrColumnMajor.RowMajor;
		}
	}

	/// <summary>
	/// Gets a collection of UI Automation providers that represents all the row headers in the table.
	/// </summary>
	/// <returns>An empty array, as TableView has no row headers.</returns>
	public IRawElementProviderSimple[] GetRowHeaders()
	{
		// TableView has no row-header element; data rows are reached through Grid.GetItem.
		return Array.Empty<IRawElementProviderSimple>();
	}

	// ----- ISelectionProvider -----

	/// <summary>
	/// Retrieves a UI Automation provider for each child element that is selected.
	/// </summary>
	/// <returns>An array of UI Automation providers.</returns>
	public IRawElementProviderSimple[] GetSelection()
	{
		var impl = GetImpl();
		if (impl is null || !impl.CanSelectRows())
		{
			m_selectedItemPeerCache.Clear();
			return Array.Empty<IRawElementProviderSimple>();
		}

		int selectedIndex = impl.SelectedIndexInternal();
		if (selectedIndex < 0)
		{
			m_selectedItemPeerCache.Clear();
			return Array.Empty<IRawElementProviderSimple>();
		}

		// A UIA read must not realize the row: it would become the repeater's anchor and move the viewport.
		List<IRawElementProviderSimple> selection = new();
		List<AutomationPeer> liveCache = new();
		if (impl.GetRowsRepeaterInternal() is { } repeater)
		{
			AutomationPeer? peer = null;
			object? item = null;
			if (repeater.ItemsSourceView is { } view && selectedIndex < view.Count)
			{
				item = impl.UnwrapEditingDataItem(view.GetAt(selectedIndex));
			}
			var rowElement = repeater.TryGetElement(selectedIndex);
			if (rowElement is not null)
			{
				peer = FrameworkElementAutomationPeer.CreatePeerForElement(rowElement);
			}

			// Kept while its row is realized too, so a held item keeps one RuntimeId as it scrolls in and out.
			AutomationPeer? itemPeer = null;
			if (item is not null)
			{
				itemPeer = GetOrCreateSelectedItemPeer(impl, item, selectedIndex, rowElement is null /* create */);
			}
			if (itemPeer is not null)
			{
				liveCache.Add(itemPeer);
				if (peer is null)
				{
					peer = itemPeer;
				}
			}

			// An empty selection is a valid answer; an array holding a null provider is not.
			if (peer is not null)
			{
				if (ProviderFromPeer(peer) is { } provider)
				{
					selection.Add(provider);
				}
			}
		}

		m_selectedItemPeerCache.Clear();
		m_selectedItemPeerCache.AddRange(liveCache);
		return selection.ToArray();
	}

	private AutomationPeer? GetOrCreateSelectedItemPeer(TableView table, object? item, int index, bool create)
	{
		AutomationPeer? peer = null;
		foreach (var cached in m_selectedItemPeerCache)
		{
			// Only TableViewRowItemAutomationPeer instances are cached.
			if (cached is TableViewRowItemAutomationPeer itemPeer && itemPeer.TryRebind(item, index))
			{
				peer = cached;
				break;
			}
		}

		if (peer is null)
		{
			if (!create)
			{
				return null;
			}
			var repeater = table.GetRowsRepeaterInternal();
			bool byIndex = !TableView.SameInspectableIdentity(
				item, table.UnwrapEditingDataItem(repeater!.ItemsSourceView.GetAt(index)));
			peer = new TableViewRowItemAutomationPeer(table, item, index, byIndex);
		}

		((TableViewRowItemAutomationPeer)peer).RefreshParent();
		return peer;
	}

	/// <summary>
	/// Gets a value that indicates whether the UI Automation provider allows more than one child element to be selected concurrently.
	/// </summary>
	public bool CanSelectMultiple
	{
		get
		{
			// GetSelection below must return every selected row once this can be true.
			return GetImpl() is { } impl && impl.CanSelectMultipleRows();
		}
	}

	/// <summary>
	/// Gets a value that indicates whether the UI Automation provider requires at least one child element to be selected.
	/// </summary>
	public bool IsSelectionRequired
	{
		get
		{
			// Selection can always be empty: TableView never forces a row to stay selected.
			return false;
		}
	}

	/// <summary>
	/// Gets a collection of UI Automation providers that represents all the column headers in the table.
	/// </summary>
	/// <returns>An array of UI Automation providers, one per visible column.</returns>
	public IRawElementProviderSimple[] GetColumnHeaders()
	{
		List<IRawElementProviderSimple> headers = new();
		List<ColumnHeaderPeerCacheEntry> liveCache = new();

		// Key off visible logical Columns() so headers enumerate before templates realize. Hidden column
		// headers report none, matching each cell's GetColumnHeaderItems.
		if (Owner is TableView tableView && tableView.AreColumnHeadersShownInternal())
		{
			if (tableView.Columns is { } columns)
			{
				var count = columns.Count;
				List<TableViewColumn> seenColumns = new();
				headers.Capacity = count;
				liveCache.Capacity = count;
				seenColumns.Capacity = count;
				for (int i = 0; i < count; i++)
				{
					var column = columns[i];
					if (!IsVisibleColumn(column))
					{
						continue;
					}
					if (seenColumns.Contains(column))
					{
						continue;
					}
					seenColumns.Add(column);

					var headerPeer = GetOrCreateColumnHeaderPeer(tableView, column);
					if (headerPeer is null)
					{
						continue;
					}

					if (headerPeer is TableViewColumnHeaderAutomationPeer headerAutomationPeer &&
						headerAutomationPeer.IsTableViewOwned())
					{
						liveCache.Add(new ColumnHeaderPeerCacheEntry(column, headerPeer));
					}

					// A provider array must not contain nulls - UIA marshals every element.
					if (ProviderFromPeer(headerPeer) is { } provider)
					{
						headers.Add(provider);
					}
				}
			}
		}

		// Replacing the cache wholesale drops peers for columns that are gone or no longer visible.
		m_columnHeaderPeerCache = liveCache;

		return headers.ToArray();
	}

	internal AutomationPeer? GetOrCreateColumnHeaderPeer(
		TableView? tableView,
		TableViewColumn? column)
	{
		if (tableView is null || column is null ||
			column.GetOwningTableView() != tableView)
		{
			return null;
		}

		if (tableView.GetHeaderHostInternal() is { } host)
		{
			if (TableViewCellsPanel.CellForColumn(host, column) is { } header)
			{
				m_columnHeaderPeerCache.RemoveAll(
					entry =>
					{
						var entryColumn = entry.column.Get();
						return entry.peer is null || entryColumn is null || entryColumn == column;
					});

				return FrameworkElementAutomationPeer.CreatePeerForElement(header);
			}
		}

		foreach (var entry in m_columnHeaderPeerCache)
		{
			if (entry.peer is not null && entry.column.Get() == column)
			{
				return entry.peer;
			}
		}

		AutomationPeer peer = new TableViewColumnHeaderAutomationPeer(tableView, column);
		peer.SetParent(this);

		m_columnHeaderPeerCache.RemoveAll(entry => entry.peer is null || entry.column.Get() is null);

		m_columnHeaderPeerCache.Add(new ColumnHeaderPeerCacheEntry(column, peer));
		return peer;
	}

	private static string StringPropertyValue(object? value)
	{
		// TODO Uno: IPropertyValue projection.
		if (value is not null)
		{
			if (ValueConversionHelpers.GetPropertyType(value.GetType()) == PropertyType.String)
			{
				return (string)value;
			}
		}

		return string.Empty;
	}

	private static bool TryGetControlTypePropertyValue(object? value, ref AutomationControlType controlType)
	{
		// TODO Uno: IPropertyValue projection.
		if (value is not null)
		{
			if (ValueConversionHelpers.GetPropertyType(value.GetType()) == PropertyType.Int32)
			{
				controlType = (AutomationControlType)(int)value;
				return true;
			}
		}

		return false;
	}

	private static bool MatchesItemName(
		TableView table,
		object? item,
		UIElement? element,
		string requested)
	{
		if (element is not null)
		{
			if (FrameworkElementAutomationPeer.CreatePeerForElement(element) is { } peer)
			{
				return peer.GetName() == requested;
			}
			return false;
		}
		var group = GroupedEntry.TryGetGroupedEntry(item);
		var name = group is not null ? table.GetGroupHeaderNameCandidate(group) : ItemToName(item);
		return !string.IsNullOrEmpty(name) && name == requested;
	}

	/// <summary>
	/// Retrieves an element by the specified property value.
	/// </summary>
	/// <param name="startAfter">The item in the container after which to begin the search.</param>
	/// <param name="automationProperty">The property that contains the value to retrieve.</param>
	/// <param name="value">The value to retrieve.</param>
	/// <returns>The first item that matches the search criterion, or null.</returns>
	public IRawElementProviderSimple? FindItemByProperty(
		IRawElementProviderSimple? startAfter,
		AutomationProperty? automationProperty,
		object? value)
	{
		var property = automationProperty;

		// Name can match virtualized rows from item data; AutomationId/ClassName/ControlType
		// only inspect realized row peers. Full support needs VirtualizedItemPattern.
		if (property == TogglePatternIdentifiers.ToggleStateProperty)
		{
			return null;
		}

		// Walk the source so AT clients can navigate past realized rows.
		var impl = GetImpl();
		if (impl is null)
		{
			return null;
		}

		var repeater = impl.GetRowsRepeaterInternal();
		if (repeater is null)
		{
			return null;
		}

		var sourceView = repeater.ItemsSourceView;
		if (sourceView is null)
		{
			return null;
		}

		int count = sourceView.Count;
		if (count <= 0)
		{
			return null;
		}

		int startIndex = -1;
		if (startAfter is not null)
		{
			// Any repeater child is a valid anchor. Matching only TableViewRow restarted grouping
			// enumeration at item 0, so clients never advanced past the first group header.
			bool resolved = false;
			if (PeerFromProvider(startAfter) is FrameworkElementAutomationPeer startPeer)
			{
				if (startPeer.Owner is UIElement ownerElement)
				{
					int index = repeater.GetElementIndex(ownerElement);
					if (index >= 0)
					{
						startIndex = index;
						resolved = true;
					}
				}
			}

			// An unresolvable startAfter - virtualized away, or not one of ours - must not degrade to
			// "start from the beginning": that turns a wrong answer into an enumeration that never ends.
			if (!resolved)
			{
				return null;
			}
		}

		// Null property means "return the next item" per IItemContainerProvider.
		bool matchAny = property is null;

		for (int i = startIndex + 1; i < count; ++i)
		{
			object? item = null;
			try
			{
				item = sourceView.GetAt(i);
			}
			catch (Exception)
			{
				// Source can throw on out-of-bounds during a concurrent edit; bail.
				continue;
			}

			bool isMatch = matchAny;
			if (!matchAny)
			{
				// Realized row peer names override item text for custom AutomationProperties.Name.
				if (property == AutomationElementIdentifiers.NameProperty)
				{
					isMatch = MatchesItemName(impl, item, repeater.TryGetElement(i), StringPropertyValue(value));
				}
				// ValueValue — match by cell value (text content).
				else if (property == ValuePatternIdentifiers.ValueProperty)
				{
					string requested = StringPropertyValue(value);
					string candidate = ItemToName(item);
					isMatch = (candidate == requested);
				}
				// Match only explicit row AutomationId values; defaults are empty.
				else if (property == AutomationElementIdentifiers.AutomationIdProperty)
				{
					string requested = StringPropertyValue(value);

					string candidate = string.Empty;
					if (repeater.TryGetElement(i) is { } rowElement)
					{
						if (FrameworkElementAutomationPeer.CreatePeerForElement(rowElement) is { } rowPeer)
						{
							candidate = rowPeer.GetAutomationId();
						}
					}
					isMatch = (candidate == requested);
				}
				// ClassName — match by the row peer class name.
				else if (property == AutomationElementIdentifiers.ClassNameProperty)
				{
					string requested = StringPropertyValue(value);
					string candidate = typeof(TableViewRow).FullName!;
					if (repeater.TryGetElement(i) is { } rowElement)
					{
						if (FrameworkElementAutomationPeer.CreatePeerForElement(rowElement) is { } rowPeer)
						{
							candidate = rowPeer.GetClassName();
						}
					}
					isMatch = (candidate == requested);
				}
				// ControlType — match by the row peer control type.
				else if (property == AutomationElementIdentifiers.ControlTypeProperty)
				{
					AutomationControlType requested = default;
					if (TryGetControlTypePropertyValue(value, ref requested))
					{
						var candidate = AutomationControlType.DataItem;
						if (repeater.TryGetElement(i) is { } rowElement)
						{
							if (FrameworkElementAutomationPeer.CreatePeerForElement(rowElement) is { } rowPeer)
							{
								candidate = rowPeer.GetAutomationControlType();
							}
						}
						isMatch = (candidate == requested);
					}
				}
				// ItemType — match by item type string (e.g., "DataRow").
				else if (property == AutomationElementIdentifiers.ItemTypeProperty)
				{
					string requested = string.Empty;
					// TODO Uno: IPropertyValue projection.
					if (value is not null)
					{
						if (ValueConversionHelpers.GetPropertyType(value.GetType()) == PropertyType.String)
						{
							requested = (string)value;
						}
					}

					string candidate = "DataRow";
					isMatch = (candidate == requested);
				}
				// Unsupported properties stop the scan rather than looping indefinitely.
				else
				{
					return null;
				}
			}

			if (!isMatch)
			{
				continue;
			}

			var matchedRowElement = repeater.TryGetElement(i);
			if (matchedRowElement is null)
			{
				try
				{
					matchedRowElement = repeater.GetOrCreateElement(i);
				}
				catch (Exception)
				{
					return null;
				}
			}

			if (matchedRowElement is not null)
			{
				if (property == AutomationElementIdentifiers.NameProperty &&
					!MatchesItemName(impl, item, matchedRowElement, StringPropertyValue(value)))
				{
					continue;
				}

				if (FrameworkElementAutomationPeer.CreatePeerForElement(matchedRowElement) is { } rowPeer)
				{
					return ProviderFromPeer(rowPeer);
				}
			}
			return null;
		}

		return null;
	}
}
