// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewSource.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls.Tabular.Primitives;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewSource
{
	// TODO Uno: Original C++ anonymous-namespace alias
	// namespace tabularPrimitives = winrt::Microsoft::UI::Xaml::Controls::Tabular::Primitives::implementation;
	// is covered by the `using Microsoft.UI.Xaml.Controls.Tabular.Primitives;` directive.

	// TODO Uno: Original C++: TableViewSource::~TableViewSource() = default;

	internal TableViewSource(object? items)
	{
		m_engine = new ShapedItemsSource(items);

		// Diagnostics raised by the engine are contractual for apps written against this type, so
		// keep naming it even though the engine itself must not know about it.
		m_engine.DiagnosticName("TableViewSource");
		// Handlers are installed before Start() so the very first projection is observed like any
		// later one -- the control never has to special-case its own construction.
		m_engine.SetProjectionRebuiltHandler(() => OnProjectionRebuilt());
		m_engine.SetShapingChangedHandler(reorderOnly => NotifyOwnerShapingChanged(reorderOnly));
		m_engine.Start();
	}

	/// <summary>
	/// Creates a <see cref="TableViewSource"/> over the specified collection. Accepts the same collection
	/// interfaces as <see cref="ItemsSourceView"/>; the projection is populated by the time this method returns.
	/// </summary>
	/// <param name="items">The collection to shape.</param>
	/// <returns>The new <see cref="TableViewSource"/>.</returns>
	/// <exception cref="ArgumentException"><paramref name="items"/> is null or is not a supported collection.</exception>
	public static TableViewSource From(object items)
	{
		if (items is null)
		{
			throw new ArgumentException("items cannot be null.");
		}

		return new TableViewSource(items);
	}

	/// <summary>
	/// Filters the source with the specified predicate. Each call replaces the previous predicate;
	/// use <see cref="ClearFilter"/> to remove it.
	/// </summary>
	/// <param name="predicate">Returns true for the items to keep.</param>
	/// <returns>This <see cref="TableViewSource"/>.</returns>
	public TableViewSource Filter(TableViewPredicate predicate)
	{
		if (predicate is null)
		{
			throw new ArgumentException("predicate cannot be null.");
		}
		m_engine!.SetFilter(item => predicate(item));
		return this;
	}

	/// <summary>
	/// Groups the source by the key the specified selector returns.
	/// </summary>
	/// <param name="key">Selects the group key of an item. Use <see cref="ClearGroupBy"/> to remove grouping.</param>
	/// <returns>This <see cref="TableViewSource"/>.</returns>
	public TableViewSource GroupBy(TableViewKeySelector key)
	{
		return GroupBy(key, null);
	}

	/// <summary>
	/// Groups the source by the key the specified selector returns, using a stable string identity for each group key.
	/// </summary>
	/// <param name="key">Selects the group key of an item.</param>
	/// <param name="groupIdentitySelector">Optional. Returns a stable string identity for a group key; null selects
	/// the built-in value-type group identity.</param>
	/// <returns>This <see cref="TableViewSource"/>.</returns>
	public TableViewSource GroupBy(TableViewKeySelector key, TableViewIdentitySelector? groupIdentitySelector)
	{
		if (key is null)
		{
			throw new ArgumentException("key cannot be null.");
		}

		RowIdentity.IdentitySelector? groupIdentity = null;
		if (groupIdentitySelector is not null)
		{
			groupIdentity = item => groupIdentitySelector(item);
		}

		m_engine!.SetGroup(item => key(item), groupIdentity);
		return this;
	}

	/// <summary>
	/// Removes the filter.
	/// </summary>
	/// <returns>This <see cref="TableViewSource"/>.</returns>
	public TableViewSource ClearFilter()
	{
		m_engine!.ClearFilter();
		return this;
	}

	/// <summary>
	/// Removes the grouping.
	/// </summary>
	/// <returns>This <see cref="TableViewSource"/>.</returns>
	public TableViewSource ClearGroupBy()
	{
		m_engine!.ClearGroup();
		return this;
	}

	/// <summary>
	/// Removes every sort axis.
	/// </summary>
	/// <returns>This <see cref="TableViewSource"/>.</returns>
	public TableViewSource ClearSort()
	{
		m_engine!.ClearSorts();
		return this;
	}

	internal TableViewSource ClearSort(string sortAxisToken)
	{
		m_engine!.ClearSort(sortAxisToken);
		return this;
	}

	internal TableViewSource ClearSortsExcept(string sortAxisToken)
	{
		m_engine!.ClearSortsExcept(sortAxisToken);
		return this;
	}

	internal List<ActiveSortAxisInfo> ActiveSortAxisInfos()
	{
		List<ActiveSortAxisInfo> infos = new();
		foreach (var axis in m_engine!.ActiveSortAxisInfos())
		{
			infos.Add(new ActiveSortAxisInfo
			{
				AxisToken = axis.AxisToken,
				SortMemberPath = axis.SortMemberPath,
				Direction = axis.Direction,
			});
		}
		return infos;
	}

	internal static string SortAxisTokenForPath(string sortMemberPath)
	{
		// Distinct from the control's "column:<address>" tokens, so a path-declared axis and a
		// column-declared axis are never mistaken for one another even when they sort the same
		// property. Tokenizing at all is what makes re-sorting the same path REPLACE its axis: an
		// untokenized axis is matched by delegate identity, and every call would synthesize a fresh
		// delegate, so Sort("Name", Asc) followed by Sort("Name", Desc) would stack two axes.
		return "path:" + sortMemberPath;
	}

	/// <summary>
	/// Sorts the source by the key the specified selector returns. The axis is anonymous: a bound
	/// <see cref="TableView"/> cannot attribute it to a column. Prefer <see cref="Sort(string, SortDirection)"/>
	/// whenever the key is a property.
	/// </summary>
	/// <param name="key">Selects the sort key of an item.</param>
	/// <param name="direction">The sort direction. <see cref="SortDirection.None"/> removes this key's sort axis.</param>
	/// <returns>This <see cref="TableViewSource"/>.</returns>
	public TableViewSource Sort(TableViewKeySelector key, SortDirection direction)
	{
		return SortCore("", "", key, "", direction);
	}

	/// <summary>
	/// Sorts the source by the property the specified path names, evaluated the same way a column evaluates
	/// its SortMemberPath. Re-sorting the same path replaces that axis in place.
	/// </summary>
	/// <param name="sortMemberPath">The property path to sort on. Must not be empty.</param>
	/// <param name="direction">The sort direction. <see cref="SortDirection.None"/> removes this axis.</param>
	/// <returns>This <see cref="TableViewSource"/>.</returns>
	public TableViewSource Sort(string sortMemberPath, SortDirection direction)
	{
		if (string.IsNullOrEmpty(sortMemberPath))
		{
			throw new ArgumentException("sortMemberPath cannot be empty.");
		}

		// One resolver per axis, captured by the selector and reused across every comparison: the
		// binding is built once per path, so steady-state cost is a DataContext write plus a property
		// read rather than a fresh binding per item.
		SortMemberPathResolver resolver = new(sortMemberPath);
		TableViewKeySelector key = item =>
		{
			return resolver.Resolve(item);
		};

		return SortCore("", SortAxisTokenForPath(sortMemberPath), key, sortMemberPath, direction);
	}

	internal TableViewSource SortReplacing(string previousSortAxisToken, string sortAxisToken, TableViewKeySelector key, string sortMemberPath, SortDirection direction)
	{
		return SortCore(previousSortAxisToken, sortAxisToken, key, sortMemberPath, direction);
	}

	private TableViewSource SortCore(string previousSortAxisToken, string sortAxisToken, TableViewKeySelector key, string sortMemberPath, SortDirection direction)
	{
		if (key is null)
		{
			throw new ArgumentException("key cannot be null.");
		}

		// A projected enum is just an int32 across the ABI, so an out-of-range value would otherwise
		// reach the comparator and be treated as Descending (anything that is not Ascending). None is
		// a defined value and removes this key's sort axis, per the IDL contract.
		if (direction != SortDirection.None &&
			direction != SortDirection.Ascending &&
			direction != SortDirection.Descending)
		{
			throw new ArgumentException("direction must be a defined SortDirection value.");
		}

		// The engine stores a std::function, which cannot be compared, so the delegate itself is
		// handed over as the axis identity. The wrapping lambda holds a strong ref to that same
		// delegate, so the identity stays valid for as long as the axis lives.
		// TODO Uno: Original C++ passes key.as<winrt::Windows::Foundation::IUnknown>(); the delegate object is
		// its own identity on .NET.
		m_engine!.SetSort(
			previousSortAxisToken,
			sortAxisToken,
			item => key(item),
			key,
			sortMemberPath,
			direction);
		return this;
	}

	internal bool IsGrouped()
	{
		return m_engine!.IsProjectedAsGrouped();
	}

	internal ItemsSourceView? GetItemsSourceView()
	{
		return m_itemsSourceView;
	}

	internal ITableViewRowMetadataProvider? GetRowMetadata()
	{
		return m_rowMetadata;
	}

	private void OnProjectionRebuilt()
	{
		// The control-level reading of the projection: what an ItemsRepeater consumes, and how a row
		// at an index is described. The engine reports which shape it produced; deciding what that
		// shape means for a TableView is this class's only remaining projection responsibility.
		switch (m_engine!.Kind())
		{
			case ShapedItemsSource.ProjectionKind.Grouped:
				{
					var adapter = m_engine!.GroupedAdapter();
					// No wrap: the grouped view IS an ItemsSourceView, so ItemsRepeater consumes it directly.
					m_itemsSourceView = adapter!.Entries();
					m_rowMetadata = RowMetadataProvider.CreateForGroupedRows(adapter, MakeIdentitySelector());
					break;
				}
			case ShapedItemsSource.ProjectionKind.Flat:
				{
					// tracker_ref, so set through .set(); read the local back into the metadata provider since
					// CreateForFlatRows takes the concrete ItemsSourceView, not the tracked slot.
					var flatView = new ItemsSourceView(m_engine!.Rows());
					m_itemsSourceView = flatView;
					m_rowMetadata = RowMetadataProvider.CreateForFlatRows(flatView, MakeIdentitySelector());
					break;
				}
			case ShapedItemsSource.ProjectionKind.Unshaped:
				// An unshaped mirror carries no shaped identity, so there is deliberately no row
				// metadata: consumers must not read sorted/identity semantics off a degraded projection.
				m_itemsSourceView = new ItemsSourceView(m_engine!.Rows());
				m_rowMetadata = null;
				break;
			case ShapedItemsSource.ProjectionKind.None:
				m_itemsSourceView = null;
				m_rowMetadata = null;
				break;
		}

		// Every rebuild mints a fresh row-metadata provider and can swap the view, so anything the
		// owner cached from the previous projection is stale from here - whether or not grouped-ness
		// changed.
		NotifyOwnerProjectionChanged();
	}

	private TableViewRowItemKeySelector? MakeIdentitySelector()
	{
		// Bridge the engine's Object-returning identity selector to the String-returning selector the
		// row-metadata provider consumes. The engine derives identity from each item's object identity
		// and already stringifies it, so this only unwraps the box.
		var keySelector = m_engine!.IdentitySelector();
		if (keySelector is null)
		{
			return null;
		}

		return item =>
		{
			object? key = null;
			try
			{
				key = keySelector(item);
			}
			catch (Exception)
			{
				return "";
			}
			// TODO Uno: IPropertyValue projection. Original C++:
			// if (auto propertyValue = key.try_as<winrt::IPropertyValue>();
			//     propertyValue && propertyValue.Type() == winrt::PropertyType::String)
			if (key is string propertyValue)
			{
				return propertyValue;
			}
			return "";
		};
	}

	internal void SetOwningTableView(object? owner)
	{
		// Sharing one shaped TableViewSource across two TableView controls has no coherent
		// semantics: shaping verbs (Filter/Sort/GroupBy) mutate a single projection, and the two
		// controls would compete for it (last-writer-wins) while both cache the ItemsSourceView /
		// row-metadata / grouped-ness the first bind produced. Since only a single owner slot
		// exists, silently overwriting m_owningTableView here left the previous owner rendering
		// against stale shape metadata and never receiving projection-shape notifications.
		//
		// Contract: fail-fast if a second live TableView tries to bind to a TableViewSource that
		// is already owned by a different TableView. In chk this asserts (same shape as the
		// null-queue and identity fail-fasts in this file); in fre we keep the existing owner
		// intact and refuse the new binding rather than corrupt the projection state, so a
		// shipping app degrades gracefully instead of crashing. Callers must Unbind the source
		// from the previous TableView (assign a different ItemsSource) before binding it here.
		if (owner is not null)
		{
			if (m_owningTableView is not null && m_owningTableView.TryGetTarget(out var existingOwner))
			{
				if (!ReferenceEquals(existingOwner, owner))
				{
					MUX_ASSERT(false,
						"TableViewSource: attempted to bind an already-owned source to a second TableView. " +
						"A TableViewSource has single-owner semantics; unbind it from the previous " +
						"TableView (assign a different ItemsSource) before binding to another.");
					return;
				}
				// Same TableView re-registering itself (e.g. re-entrant ItemsSource re-assignment
				// with the same value). Idempotent.
			}
		}

		m_owningTableView = owner is not null ? new WeakReference<object>(owner) : null;

		if (owner is null)
		{
			// Detaching: drop the owner's handlers with the owner itself, so a source that has been
			// swapped out of ItemsSource cannot drive the control it used to belong to.
			m_projectionChanged = null;
			m_shapingChanged = null;
		}
	}

	private void NotifyOwnerProjectionChanged()
	{
		if (m_projectionChanged is not null)
		{
			m_projectionChanged();
		}
	}

	private void NotifyOwnerShapingChanged(bool reorderOnly)
	{
		if (m_shapingChanged is not null)
		{
			m_shapingChanged(reorderOnly);
		}
	}
}
