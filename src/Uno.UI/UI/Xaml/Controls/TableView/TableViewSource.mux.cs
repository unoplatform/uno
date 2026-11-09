// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewSource.cpp, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls.Tabular.Primitives;
using Uno.UI.Helpers.WinUI;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewSource
{
	// TODO Uno: C++ nests this as `struct TableViewSource::ShapingTip` with a destructor; C# uses IDisposable under `using`.
	private sealed class ShapingTip : IDisposable
	{
		public ShapingTip(TableViewSource source, TableViewTip.Operation kind, bool meaningful = true)
		{
			this.source = source;
			this.kind = kind;
			handlers = source.m_shapingTipHandlers;
			previous = source.m_shapingTip;
			source.m_shapingTip = this;
			ownerGeneration = source.m_ownerGeneration;
			observation.unproven = previous is not null || source.m_notificationDepth != 0;
			if (previous is not null) { previous.observation.unproven = true; }
			if (handlers is not null)
			{
				generation = handlers.begin!(kind, meaningful);
			}
		}

		public void Dispose()
		{
			source.m_shapingTip = previous;
			if (handlers is not null)
			{
				var stale = ownerGeneration != source.m_ownerGeneration;
				handlers.complete!(kind, generation, !stale && observation.Failed(),
					stale ? TableViewTip.IgnoreReason.Stale : observation.Ignored(),
					observation.error, observation.appFaultKind, observation.contained);
			}
		}

		public void Apply(Action callback)
		{
			// The verb counts its own contained delegate faults, however it ends. A tally already running belongs to the verb
			// a delegate started this one in, and goes back to it; with no such verb it is live shaping's or a source
			// change's, not this verb's.
			var enclosing = source.m_engine.ExchangeSwallowedFaults(new());
			try
			{
				try
				{
					callback();
					observation.completed = true;
				}
				catch (Exception error)
				{
					// The app's collection, its broken contract, or a callout arrive tagged as the app's; TableView's own
					// invalid arguments stay its own.
					var fault = TableViewTip.CurrentFault(error);
					if (observation.error is null) { observation.error = fault.error; }
					if (observation.appFaultKind == TableViewTip.AppFaultKind.None) { observation.appFaultKind = fault.appFault; }
					throw;
				}
			}
			finally
			{
				var own = source.m_engine.ExchangeSwallowedFaults(previous is not null ? enclosing : new());
				observation.contained = new TableViewTip.ContainedFaults { count = own.count, firstError = own.firstError };
			}
		}

		private readonly TableViewSource source;
		private readonly TableViewTip.Operation kind;
		private readonly ShapingTipHandlers? handlers;
		private readonly ShapingTip? previous;
		private readonly ulong ownerGeneration;
		private readonly ulong generation;
		public readonly TableViewTip.ShapingObservation observation = new();
	}

	internal void SetShapingTipHandlers(
		object? owner,
		Func<TableViewTip.Operation, bool, ulong> begin,
		ShapingTipCompleted complete)
	{
		if (!ReferenceEquals(GetOwningTableView(), owner)) { return; }
		try
		{
			m_shapingTipHandlers = new ShapingTipHandlers { begin = begin, complete = complete };
		}
		catch (Exception) { global::System.Diagnostics.Debug.WriteLine("TableView TiP: source observation unavailable."); }
	}

	// TODO Uno: Original C++: TableViewSource::~TableViewSource() = default;

	internal TableViewSource(object? items)
	{
		m_threadAffinity = ReferenceTrackerThreadAffinity.ForCurrentThread();
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
	/// use <see cref="ClearFilter"/> to remove it rather than passing a pass-everything one.
	/// </summary>
	/// <param name="predicate">Returns true for the items to keep.</param>
	/// <returns>This <see cref="TableViewSource"/>.</returns>
	public TableViewSource Filter(TableViewPredicate predicate)
	{
		m_threadAffinity.CheckThread();

		if (predicate is null)
		{
			throw new ArgumentException("predicate cannot be null.");
		}
		using ShapingTip tip = new(this, TableViewTip.Operation.Filter);
		tip.Apply(() =>
		{
			m_engine.SetFilter(item => predicate(item));
		});
		return this;
	}

	/// <summary>
	/// Groups the source by the key the specified selector returns.
	/// </summary>
	/// <param name="key">Selects the group key of an item. Use <see cref="ClearGroupBy"/> to remove grouping.</param>
	/// <returns>This <see cref="TableViewSource"/>.</returns>
	/// <remarks>
	/// Grouping fails fast. When a group's identity cannot be resolved, the call that builds the projection throws
	/// <see cref="ArgumentException"/>; the projection is never silently flattened. An identity is unresolvable when the
	/// key is null, or is a reference type and no identity selector was supplied; when the key is the empty string, or a
	/// supplied identity selector returns it; when a supplied identity selector throws; or when two logically-different
	/// keys resolve to the same identity string and no identity selector was supplied to declare that collapse intentional.
	/// </remarks>
	public TableViewSource GroupBy(TableViewKeySelector key)
	{
		m_threadAffinity.CheckThread();

		return GroupBy(key, null);
	}

	/// <summary>
	/// Groups the source by the key the specified selector returns, using a stable string identity for each group key.
	/// </summary>
	/// <param name="key">Selects the group key of an item.</param>
	/// <param name="groupIdentitySelector">Optional. Returns a stable string identity for a group key; null selects
	/// the built-in value-type group identity (string, int, long, Guid, bool or enum). A reference-type group key with no
	/// selector fails fast at projection time, as does an empty identity from either source.</param>
	/// <returns>This <see cref="TableViewSource"/>.</returns>
	public TableViewSource GroupBy(TableViewKeySelector key, TableViewIdentitySelector? groupIdentitySelector)
	{
		m_threadAffinity.CheckThread();

		if (key is null)
		{
			throw new ArgumentException("key cannot be null.");
		}

		using ShapingTip tip = new(this, TableViewTip.Operation.GroupConfiguration);
		RowIdentity.IdentitySelector? groupIdentity = null;
		if (groupIdentitySelector is not null)
		{
			groupIdentity = item => groupIdentitySelector(item);
		}

		tip.Apply(() =>
		{
			m_engine.SetGroup(item => key(item), groupIdentity);
		});
		return this;
	}

	/// <summary>
	/// Makes the source a tree: each item is a node identified by <paramref name="keySelector"/>, nested under the
	/// node whose key <paramref name="parentKeySelector"/> returns. Calling it again replaces the relation and resets expansion.
	/// </summary>
	/// <param name="keySelector">Selects the key of an item. Required; use <see cref="ClearParentBy"/> to remove the tree.</param>
	/// <param name="parentKeySelector">Selects the key of an item's parent. A null or unknown key makes the item a root. Required.</param>
	/// <returns>This <see cref="TableViewSource"/>.</returns>
	/// <exception cref="ArgumentException">A selector is null, or the data has a null or duplicate key, an item that is its own parent, or a cycle.</exception>
	public TableViewSource ParentBy(TableViewKeySelector keySelector, TableViewKeySelector parentKeySelector)
	{
		m_threadAffinity.CheckThread();

		if (keySelector is null || parentKeySelector is null)
		{
			throw new ArgumentException("ParentBy: keySelector and parentKeySelector are required.");
		}

		m_engine.SetParent(
			item => keySelector(item),
			item => parentKeySelector(item));
		return this;
	}

	// A clear is measured only when the engine holds something to clear. The engine keeps a declaration
	// even when re-projecting it throws, so it, not a flag set around the call, is the answer.
	/// <summary>
	/// Removes the filter.
	/// </summary>
	/// <returns>This <see cref="TableViewSource"/>.</returns>
	public TableViewSource ClearFilter()
	{
		m_threadAffinity.CheckThread();

		using ShapingTip tip = new(this, TableViewTip.Operation.Filter, m_engine.HasFilter());
		tip.Apply(() => { m_engine.ClearFilter(); });
		return this;
	}

	/// <summary>
	/// Removes the grouping.
	/// </summary>
	/// <returns>This <see cref="TableViewSource"/>.</returns>
	public TableViewSource ClearGroupBy()
	{
		m_threadAffinity.CheckThread();

		using ShapingTip tip = new(this, TableViewTip.Operation.GroupConfiguration, m_engine.HasGroup());
		tip.Apply(() => { m_engine.ClearGroup(); });
		return this;
	}

	/// <summary>
	/// Removes the tree and shows the source flat. Resets expansion.
	/// </summary>
	/// <returns>This <see cref="TableViewSource"/>.</returns>
	public TableViewSource ClearParentBy()
	{
		m_threadAffinity.CheckThread();

		m_engine.ClearParentBy();
		return this;
	}

	/// <summary>
	/// Removes every sort axis.
	/// </summary>
	/// <returns>This <see cref="TableViewSource"/>.</returns>
	public TableViewSource ClearSort()
	{
		m_threadAffinity.CheckThread();

		using ShapingTip tip = new(this, TableViewTip.Operation.Sort, m_engine.HasSortAxis(""));
		tip.Apply(() => { m_engine.ClearSorts(); });
		return this;
	}

	internal TableViewSource ClearSort(string sortAxisToken)
	{
		using ShapingTip tip = new(this, TableViewTip.Operation.Sort, m_engine.HasSortAxis(sortAxisToken));
		tip.Apply(() => { m_engine.ClearSort(sortAxisToken); });
		return this;
	}

	internal TableViewSource ClearSortsExcept(string sortAxisToken)
	{
		using ShapingTip tip = new(this, TableViewTip.Operation.Sort, m_engine.HasSortOtherThan(sortAxisToken));
		tip.Apply(() => { m_engine.ClearSortsExcept(sortAxisToken); });
		return this;
	}

	/// <summary>
	/// Gets or sets whether the shaping verbs follow changes to the items' properties. When true, an item that raises
	/// <c>PropertyChanged</c> is re-filtered, re-sorted and re-grouped, and under <see cref="ParentBy"/> re-parented, in place.
	/// Defaults to false.
	/// </summary>
	public bool IsLiveShaping
	{
		get
		{
			m_threadAffinity.CheckThread();

			return m_engine.IsLiveShapingEnabled();
		}
		set
		{
			m_threadAffinity.CheckThread();

			m_engine.SetLiveShaping(value);
		}
	}

	internal List<ActiveSortAxisInfo> ActiveSortAxisInfos()
	{
		List<ActiveSortAxisInfo> infos = new();
		foreach (var axis in m_engine.ActiveSortAxisInfos())
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
	/// <remarks>
	/// Both Sort overloads are last writer wins against <see cref="TableView"/>'s own sort. When several axes are active
	/// the first one declared is the primary sort and each later axis breaks ties within the previous.
	/// </remarks>
	public TableViewSource Sort(TableViewKeySelector key, SortDirection direction)
	{
		m_threadAffinity.CheckThread();

		return SortCore("", "", key, "", direction);
	}

	/// <summary>
	/// Sorts the source by the property the specified path names, evaluated the same way a column evaluates
	/// its SortMemberPath. Re-sorting the same path replaces that axis in place.
	/// </summary>
	/// <param name="sortMemberPath">The property path to sort on. Must not be empty.</param>
	/// <param name="direction">The sort direction. <see cref="SortDirection.None"/> removes this axis.</param>
	/// <returns>This <see cref="TableViewSource"/>.</returns>
	/// <exception cref="ArgumentException"><paramref name="sortMemberPath"/> is empty.</exception>
	/// <remarks>
	/// The path is evaluated by the same binding-based evaluator a column uses for its SortMemberPath, so a path that
	/// displays also sorts, and a bound <see cref="TableView"/> can match it to a column and light that column's sort
	/// indicator. This is the preferred form.
	/// </remarks>
	public TableViewSource Sort(string sortMemberPath, SortDirection direction)
	{
		m_threadAffinity.CheckThread();

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
		// key.as<IUnknown>(): the delegate object is its own identity on .NET.
		var keyIdentity = key;
		// Removing an axis that is not there is a no-op, measured no more than a clear with nothing to clear.
		bool meaningful = direction != SortDirection.None ||
			m_engine.HasMatchingSort(sortAxisToken, keyIdentity) ||
			(previousSortAxisToken.Length != 0 && previousSortAxisToken != sortAxisToken && m_engine.HasSortAxis(previousSortAxisToken));
		using ShapingTip tip = new(this, TableViewTip.Operation.Sort, meaningful);
		tip.Apply(() =>
		{
			m_engine.SetSort(
				previousSortAxisToken,
				sortAxisToken,
				item => key(item),
				keyIdentity,
				sortMemberPath,
				direction);
		});
		return this;
	}

	internal bool IsGrouped()
	{
		return m_engine.IsProjectedAsGrouped();
	}

	internal bool HasFilter() => m_engine.HasFilter();

	internal bool HasGroupBy() => m_engine.HasGroup();

	internal bool HasParentBy() => m_engine.HasParentBy();

	internal uint SourceRowCount() => m_engine.SourceRowCount();

	internal uint DataRowCount()
	{
		// Rows() is only a snapshot of a tree's visible rows; the published index holds every node the filter keeps.
		var adapter = m_engine.HierarchicalAdapter();
		if (adapter is not null && adapter.HasIndex())
		{
			return adapter.KeptNodeCount();
		}
		// Rows() is the shaped data without headers and ignores group expansion.
		var rows = m_engine.Rows();
		return rows is not null ? (uint)rows.Count : 0;
	}

	internal bool ContainsShapedItem(object? item)
	{
		if (item is null)
		{
			return false;
		}

		// Decided by the published index, not Kind(): a rebuild publishes the index (or releases it)
		// before raising the Reset that closes the edit, while Kind() is only updated afterwards.
		var adapter = m_engine.HierarchicalAdapter();
		if (adapter is not null && adapter.HasIndex())
		{
			// Rows() is only a snapshot of the visible rows here; the index holds every filtered node.
			return adapter.IndexContainsItem(item);
		}

		// Rows() is the shaped data without headers and ignores group expansion.
		var rows = m_engine.Rows();
		if (rows is null)
		{
			return false;
		}
		foreach (var row in rows)
		{
			if (row is not null && ReferenceEquals(row, item))
			{
				return true;
			}
		}
		return false;
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
		var previousRowMetadata = m_rowMetadata;
		switch (m_engine.Kind())
		{
			case ShapedItemsSource.ProjectionKind.Grouped:
				{
					var adapter = m_engine.GroupedAdapter();
					if (adapter is null)
					{
						// A projection kind without its adapter is never published coherently; keep the
						// previous projection until the engine publishes again.
						MUX_ASSERT(false, "TableViewSource: Grouped projection published without a grouped adapter.");
						return;
					}
					// No wrap: the grouped view IS an ItemsSourceView, so ItemsRepeater consumes it directly.
					m_itemsSourceView = adapter.Entries();
					m_rowMetadata = RowMetadataProvider.CreateForGroupedRows(adapter, MakeIdentitySelector());
					break;
				}
			case ShapedItemsSource.ProjectionKind.Hierarchical:
				{
					var adapter = m_engine.HierarchicalAdapter();
					if (adapter is null)
					{
						MUX_ASSERT(false, "TableViewSource: Hierarchical projection published without a hierarchical adapter.");
						return;
					}
					// Same as grouped: consumed directly.
					m_itemsSourceView = adapter.Entries();
					m_rowMetadata = RowMetadataProvider.CreateForHierarchicalRows(adapter, MakeIdentitySelector());
					break;
				}
			case ShapedItemsSource.ProjectionKind.GroupedHierarchical:
				{
					// Grouped adapter supplies the presented axis (header rows); hierarchy adapter supplies
					// level and expansion.
					var groupedAdapter = m_engine.GroupedAdapter();
					var hierarchicalAdapter = m_engine.HierarchicalAdapter();
					if (groupedAdapter is null || hierarchicalAdapter is null)
					{
						MUX_ASSERT(false, "TableViewSource: GroupedHierarchical projection published without both adapters.");
						return;
					}
					m_itemsSourceView = groupedAdapter.Entries();
					m_rowMetadata = RowMetadataProvider.CreateForGroupedHierarchicalRows(
						groupedAdapter,
						hierarchicalAdapter,
						MakeIdentitySelector());
					break;
				}
			case ShapedItemsSource.ProjectionKind.Flat:
				{
					// tracker_ref, so set through .set(); read the local back into the metadata provider since
					// CreateForFlatRows takes the concrete ItemsSourceView, not the tracked slot.
					var flatView = new ItemsSourceView(m_engine.Rows());
					m_itemsSourceView = flatView;
					m_rowMetadata = RowMetadataProvider.CreateForFlatRows(flatView, MakeIdentitySelector());
					break;
				}
			case ShapedItemsSource.ProjectionKind.Unshaped:
				// An unshaped mirror carries no shaped identity, so there is deliberately no row
				// metadata: consumers must not read sorted/identity semantics off a degraded projection.
				m_itemsSourceView = new ItemsSourceView(m_engine.Rows());
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
		if (m_shapingTip is not null) { m_shapingTip.observation.published = true; }
		NotifyOwnerProjectionChanged();

		// TODO Uno: shared_ptr release of the replaced provider. The owner swaps its copy during the
		// notify above, so this was the last reference.
		if (!ReferenceEquals(previousRowMetadata, m_rowMetadata))
		{
			previousRowMetadata?.Dispose();
		}
	}

	private TableViewRowItemKeySelector? MakeIdentitySelector()
	{
		// Bridge the engine's Object-returning identity selector to the String-returning selector the
		// row-metadata provider consumes. The engine derives identity from each item's object identity
		// and already stringifies it, so this only unwraps the box.
		var keySelector = m_engine.IdentitySelector();
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

	internal bool SetOwningTableView(object? owner)
	{
		// Sharing one shaped TableViewSource across two TableView controls has no coherent
		// semantics: shaping verbs (Filter/Sort/GroupBy) mutate a single projection, and the two
		// controls would compete for it (last-writer-wins) while both cache the ItemsSourceView /
		// row-metadata / grouped-ness the first bind produced. Since only a single owner slot
		// exists, silently overwriting m_owningTableView here left the previous owner rendering
		// against stale shape metadata and never receiving projection-shape notifications.
		//
		// Contract: a second TableView cannot bind a source another TableView already owns; it is
		// refused and renders empty until the owner releases it (assigns a different ItemsSource or
		// is unloaded). Not an assert: a navigation loads the new page before it unloads the old one,
		// so a refusal is a normal transient state.
		var existingOwner = GetOwningTableView();
		if (existingOwner is not null)
		{
			if (!ReferenceEquals(existingOwner, owner))
			{
				return false;
			}
			// Same TableView re-registering itself (e.g. reclaiming on Loaded). Idempotent.
		}

		if (!ReferenceEquals(GetOwningTableView(), owner)) { ++m_ownerGeneration; }
		m_owningTableView = owner is not null ? new WeakReference<object>(owner) : null;
		RemoveOwnerReleasedHandler(owner);
		return true;
	}

	internal void AddOwnerReleasedHandler(object? waiter, Action handler)
	{
		RemoveOwnerReleasedHandler(waiter);
		m_ownerReleasedWaiters.Add(new OwnerReleasedWaiter { Waiter = new WeakReference<object>(waiter!), Handler = handler });
	}

	internal void RemoveOwnerReleasedHandler(object? waiter)
	{
		// Also prunes waiters that have been collected.
		m_ownerReleasedWaiters.RemoveAll(entry =>
		{
			return !entry.Waiter.TryGetTarget(out var entryWaiter) || ReferenceEquals(entryWaiter, waiter);
		});
	}

	internal void ReleaseOwningTableView(object? owner)
	{
		// Only the owner can release; a table whose bind was refused must not detach the real owner.
		var existingOwner = GetOwningTableView();
		if (existingOwner is not null && !ReferenceEquals(existingOwner, owner))
		{
			return;
		}

		if (GetOwningTableView() is not null) { ++m_ownerGeneration; }
		m_owningTableView = null;

		// Drop the owner's handlers with the owner itself, so a source that has been swapped out of
		// ItemsSource cannot drive the control it used to belong to.
		m_projectionChanged = null;
		m_shapingChanged = null;
		m_shapingTipHandlers = null;
		// A restore held for the former owner's editor must not wait on an edit it will no longer
		// hear the end of.
		m_engine.SetLiveShapingHold(null);
		m_engine.SetLiveItemChangedHook(null);
		m_engine.SetRowsReleasedHook(null);
		m_engine.SetLiveItemReshapeHandler(null);
		m_engine.ResumeHeldLiveShaping();

		// Last, after the former owner's handlers are dropped. Each waiter is popped before it runs:
		// it re-enters SetOwningTableView, and one that does not claim must not be retried.
		while (GetOwningTableView() is null && m_ownerReleasedWaiters.Count != 0)
		{
			var waiter = m_ownerReleasedWaiters[0];
			m_ownerReleasedWaiters.RemoveAt(0);
			if (waiter.Waiter.TryGetTarget(out _) && waiter.Handler is not null)
			{
				waiter.Handler();
			}
		}
	}

	internal void SetLiveShapingHold(Func<bool>? isHeld)
	{
		m_engine.SetLiveShapingHold(isHeld);
	}

	internal void SetLiveItemChangedHook(Func<object?, bool, bool>? hook)
	{
		m_engine.SetLiveItemChangedHook(hook);
	}

	internal void SetLiveItemReshapeHandler(Action? handler)
	{
		m_engine.SetLiveItemReshapeHandler(handler);
	}

	internal void SetRowsReleasedHook(Action<object?, List<object?>>? hook)
	{
		m_engine.SetRowsReleasedHook(hook);
	}

	internal void ResumeHeldLiveShaping()
	{
		m_engine.ResumeHeldLiveShaping();
	}

	internal bool CanApplySort()
	{
		return m_engine.CanProjectWithRowIdentity();
	}

	private void NotifyOwnerProjectionChanged()
	{
		if (m_projectionChanged is not null)
		{
			// TODO Uno: the C++ get_strong() keeps this source alive while the owner runs app code; the GC does that here.
			++m_notificationDepth;
			try
			{
				m_projectionChanged();
			}
			finally
			{
				--m_notificationDepth;
			}
		}
	}

	private void NotifyOwnerShapingChanged(bool reorderOnly)
	{
		// Only the in-place reorder notification proves computation; Refresh can merely defer.
		if (reorderOnly && m_shapingTip is not null) { m_shapingTip.observation.published = true; }
		if (m_shapingChanged is not null)
		{
			++m_notificationDepth;
			try
			{
				m_shapingChanged(reorderOnly);
			}
			finally
			{
				--m_notificationDepth;
			}
		}
	}
}
