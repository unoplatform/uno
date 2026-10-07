// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewAutomationPeer.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
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
			// Return the same rich cell peer used for tree navigation.
			var owningColumn = rowImpl.GetCellOwningColumn(cellElement);
			AutomationPeer cellPeer =
				new TableViewCellAutomationPeer(cellFE, rowElement, owningColumn, column);
			return ProviderFromPeer(cellPeer);
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
			return Array.Empty<IRawElementProviderSimple>();
		}

		int selectedIndex = impl.SelectedIndexInternal();
		if (selectedIndex < 0)
		{
			return Array.Empty<IRawElementProviderSimple>();
		}

		// Only a realized row has a provider. A selected row scrolled out of the realization window
		// reports as unrealized here rather than fabricating a peer for an element that does not
		// exist; AT clients reach it through ItemContainer.FindItemByProperty, which realizes it.
		if (impl.GetRowsRepeaterInternal() is { } repeater)
		{
			if (repeater.TryGetElement(selectedIndex) is { } rowElement)
			{
				if (FrameworkElementAutomationPeer.CreatePeerForElement(rowElement) is { } rowPeer)
				{
					// An empty selection is a valid answer; an array holding a null provider is not.
					if (ProviderFromPeer(rowPeer) is { } provider)
					{
						List<IRawElementProviderSimple> selection = new();
						selection.Add(provider);
						return selection.ToArray();
					}
				}
			}
		}

		return Array.Empty<IRawElementProviderSimple>();
	}

	/// <summary>
	/// Gets a value that indicates whether the UI Automation provider allows more than one child element to be selected concurrently.
	/// </summary>
	public bool CanSelectMultiple
	{
		get
		{
			// Single selection only in this release. Multiple must derive this from SelectionMode, and
			// GetSelection below must return every selected row rather than at most one.
			return false;
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

		// Key off visible logical Columns() so headers enumerate before templates realize.
		if (Owner is TableView tableView)
		{
			if (tableView.Columns is { } columns)
			{
				var count = columns.Count;
				headers.Capacity = count;
				liveCache.Capacity = count;
				for (int i = 0; i < count; i++)
				{
					var column = columns[i];
					if (!IsVisibleColumn(column))
					{
						continue;
					}

					var headerPeer = GetOrCreateColumnHeaderPeer(tableView, column);
					if (headerPeer is null)
					{
						continue;
					}

					// The peer is cached even when it currently has no provider: ProviderFromPeer only
					// yields one for a peer UIA has connected, so a transiently unconnected peer must
					// keep its identity for the next enumeration rather than being rebuilt.
					liveCache.Add(new ColumnHeaderPeerCacheEntry { column = new WeakReference<TableViewColumn>(column), peer = headerPeer });

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

	private AutomationPeer? GetOrCreateColumnHeaderPeer(
		TableView? tableView,
		TableViewColumn? column)
	{
		if (tableView is null || column is null)
		{
			return null;
		}

		// Looked up against the cache as it stood at the previous enumeration, so a column that
		// survives keeps the very same peer - and therefore the same provider identity - across calls.
		foreach (var entry in m_columnHeaderPeerCache)
		{
			if (entry.peer is not null &&
				entry.column is not null &&
				entry.column.TryGetTarget(out var cachedColumn) &&
				cachedColumn == column)
			{
				return entry.peer;
			}
		}

		// The TableView owns the peer so headers stay enumerable before their templates realize;
		// TableViewColumnHeaderAutomationPeer supplies its own per-column RuntimeId and AutomationId
		// to keep the headers distinguishable despite the shared owner.
		return new TableViewColumnHeaderAutomationPeer(tableView, column);
	}

	private static string ItemToName(object? item)
	{
		// Boxed WinRT primitives surface as IPropertyValue, not IStringable.
		// TODO Uno: IPropertyValue projection. try_as<IPropertyValue>() + Type() becomes ValueConversionHelpers.TryGetPropertyType.
		if (item is not null && ValueConversionHelpers.TryGetPropertyType(item, out var propertyType))
		{
			switch (propertyType)
			{
				case PropertyType.String: return (string)item;
				case PropertyType.Boolean: return (bool)item ? "True" : "False";
				case PropertyType.Int16: return ((int)(short)item).ToString(CultureInfo.InvariantCulture);
				case PropertyType.Int32: return ((int)item).ToString(CultureInfo.InvariantCulture);
				case PropertyType.Int64: return ((long)item).ToString(CultureInfo.InvariantCulture);
				case PropertyType.UInt8: return ((uint)(byte)item).ToString(CultureInfo.InvariantCulture);
				case PropertyType.UInt16: return ((uint)(ushort)item).ToString(CultureInfo.InvariantCulture);
				case PropertyType.UInt32: return ((uint)item).ToString(CultureInfo.InvariantCulture);
				case PropertyType.UInt64: return ((ulong)item).ToString(CultureInfo.InvariantCulture);
				case PropertyType.Single: return CppWinRTHelpers.ToHString((float)item);
				case PropertyType.Double: return CppWinRTHelpers.ToHString((double)item);
				default: break;
			}
		}

		if (SharedHelpers.IsStringable(item))
		{
			return SharedHelpers.StringableToString(item);
		}

		return string.Empty;
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

		// Resolve startAfter through its owning realized TableViewRow.
		int startIndex = -1;
		if (startAfter is not null)
		{
			if (PeerFromProvider(startAfter) is FrameworkElementAutomationPeer startPeer)
			{
				if (startPeer.Owner is TableViewRow ownerRow)
				{
					int rowIndex = repeater.GetElementIndex(ownerRow);
					if (rowIndex >= 0)
					{
						startIndex = rowIndex;
					}
					else
					{
						// Avoid restarting at item 0 after startAfter has been virtualized away.
						return null;
					}
				}
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
					string requested = StringPropertyValue(value);

					string candidate = string.Empty;
					if (repeater.TryGetElement(i) is { } rowElement)
					{
						if (FrameworkElementAutomationPeer.CreatePeerForElement(rowElement) is { } rowPeer)
						{
							candidate = rowPeer.GetName();
						}
					}
					if (string.IsNullOrEmpty(candidate))
					{
						candidate = ItemToName(item);
					}

					isMatch = (candidate == requested);
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
