// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\GroupedSourceAdapter\GroupedSourceAdapter.cpp, tag winui3/main, commit dc28206ea35

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Interop;
using Windows.Foundation.Collections;
using static Microsoft.UI.Xaml.Controls._Tracing;
using INotifyCollectionChanged = System.Collections.Specialized.INotifyCollectionChanged;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class GroupedSourceAdapter
{
	public GroupedSourceAdapter()
	{
		// __RP_Marker_ClassById(RuntimeProfiler.ProfId_GroupedSourceAdapter);

		var queue = DispatcherQueue.GetForCurrentThread();
		if (queue is null)
		{
			// TODO Uno: winrt::hresult_error(RPC_E_WRONG_THREAD, ...) maps to InvalidOperationException.
			throw new InvalidOperationException("GroupedSourceAdapter must be constructed on a UI thread.");
		}
		m_uiQueue = new WeakReference<DispatcherQueue>(queue);

		// One view over the stable entries vector, handed to every consumer.
		m_entriesView = new ItemsSourceView(m_entries);

		// Expansion intent lives in the model; the projection reacts to it in one place so a per-group
		// toggle and a programmatic ExpandAll reach the projection by exactly one path.
		m_expansion.SetChangedHandler(
			(ShapingHelpers.RowExpansionModel.Change change) =>
			{
				OnExpansionChanged(change);
			});
	}

#if HAS_UNO
	// TODO Uno: Original C++ destructor cleanup. Uno does not support cleanup via finalizers.
	// Move this logic into Loaded/Unloaded event handlers or other lifecycle methods to avoid leaks.
	// TODO Uno: Investigate potential leak: the outer source and every group's items collection keep
	// their handlers (each holds only a WeakReference to this adapter) until DetachSourceQuietly or a
	// Source change runs DetachFromSource.

	// Original destructor logic (not executed):
	// GroupedSourceAdapter::~GroupedSourceAdapter()
	// {
	//     DetachFromSource();
	// }
#endif

	public partial void Source(object? value)
	{
		// TODO Uno: IInspectable operator== is COM identity, i.e. reference equality.
		if (ReferenceEquals(m_source, value))
		{
			return;
		}

		DetachFromSource();
		m_source = value;
		AttachToSource();
		Rebuild();
	}

	public partial void DetachSourceQuietly()
	{
		DetachFromSource();
		m_source = null;
	}

	// --- The one responsibility: materialize the flat list -----------------------------------------

	private partial void Rebuild()
	{
		// Projection mutations are UI-thread-affine. Source notifications are required to arrive on the
		// owning UI thread and reach here synchronously; off-thread delivery is app misuse.
		AssertRebuildOnUiThread();

		if (m_rebuildInFlight)
		{
			// A change notification raised synchronously inside ReplaceAll re-entered Rebuild. Don't
			// drop it (that would leave m_entries stale) and don't nest it — remember it and run one
			// coalesced follow-up after the outer rebuild unwinds.
			m_pendingRebuild = true;
			return;
		}

		var runPending = false;
		do
		{
			m_rebuildInFlight = true;
			m_pendingRebuild = false;
			try
			{
				// Drop inner-group subscriptions before rebuilding; re-attach as we walk the (possibly new)
				// set of groups.
				UnsubscribeFromAllGroups();

				// Build into a local vector, then bulk-replace via a single Reset rather than firing N+1
				// VectorChanged events. ItemsRepeater treats one Reset as a wholesale rebuild; per-item Add
				// notifications during a known full rebuild force one layout invalidation per item.
				List<object?> built = new();
				HashSet<string> liveGroupKeys = new();
				m_liveGroupsByIdentityKey.Clear();
				m_ambiguousLiveGroupIdentityKeys.Clear();

				var source = Source();
				if (source is not null)
				{
					// Source must be iterable; each element is itself an iterable group.
					var groups = ShapingHelpers.EnumerateInspectableItems(source);
					foreach (var group in groups)
					{
						if (group is null)
						{
							continue;
						}

						// Compute the group's stable identity ONCE. A value-typed key (string department,
						// int year) yields a stable string that survives the key object being re-minted by
						// the next shaping pass; a non-value key has none.
						var identity = ShapingHelpers.GetGroupKeyIdentity(group);

						// The expansion-intent key: prefixed identity when the group has one, else a fall
						// back to the group object's own stable lookup key.
						var intentKey = !string.IsNullOrEmpty(identity)
							? "identity:" + identity
							: ShapingHelpers.ValueKey.ToObjectLookupKey(group, true);
						if (!string.IsNullOrEmpty(intentKey))
						{
							liveGroupKeys.Add(intentKey);
						}

						// Track the live group by its declared identity so a control holding only a key can
						// resolve it. A key that resolves to two different objects is marked ambiguous and
						// resolves to neither.
						if (!string.IsNullOrEmpty(identity) && !m_ambiguousLiveGroupIdentityKeys.Contains(identity))
						{
							var inserted = m_liveGroupsByIdentityKey.TryAdd(identity, group);
							if (!inserted && !ReferenceEquals(m_liveGroupsByIdentityKey[identity], group))
							{
								m_liveGroupsByIdentityKey.Remove(identity);
								m_ambiguousLiveGroupIdentityKeys.Add(identity);
							}
						}

						// Materialize the group's items. Per-group sorting is applied upstream by
						// ShapedItemsSource::RebuildGrouped; the adapter reads the group as-is.
						//
						// An empty group is NOT dropped: group existence is source-owned (a bucket exists
						// only if the app authored it — a Kanban column, an "Uncategorized" bucket, an empty
						// drag target), so it must stay targetable.
						var items = ShapingHelpers.EnumerateInspectableItems(ShapingHelpers.GetGroupItemsObject(group));
						var groupItemCount = items.Count;
						var isExpanded = m_expansion.IsExpanded(intentKey);

						// Collapsed groups still present a header (chrome stays targetable) but contribute no
						// data rows. The header carries the resolved count and expansion state so chevron
						// chrome and the automation peer read them without re-resolving against the adapter.
						built.Add(new GroupedEntry(group, groupItemCount, isExpanded));

						if (isExpanded)
						{
							// Data rows ARE the app items — no wrapper. This is the memory win the current
							// stack already has.
							foreach (var item in items)
							{
								built.Add(item);
							}
						}

						// Subscribe so a change inside the group triggers a rebuild.
						SubscribeToGroup(group);
					}
				}

				// Drop intent for groups that no longer exist, or the store grows unbounded across changing
				// datasets. Pruning a dead key changes no live key's resolved state, so it is silent.
				m_expansion.RetainOnly(liveGroupKeys);

				// One Reset for the whole projection.
				m_entries.ReplaceAll(built);

				// resetGuard runs here, at the end of the loop body, publishing into runPending whether a
				// re-entrant request arrived while the guard was held.
			}
			finally
			{
				m_rebuildInFlight = false;
				runPending = m_pendingRebuild;
				m_pendingRebuild = false;
			}
		} while (runPending);
	}

	private partial bool OnUiThread()
	{
		DispatcherQueue? queue = null;
		m_uiQueue?.TryGetTarget(out queue);
		return queue is not null && queue.HasThreadAccess;
	}

	private partial void AssertRebuildOnUiThread()
	{
		// A source bound off the UI thread captures no queue, so affinity can't be proven either way
		// and the assert stands down rather than firing on something it cannot judge.
		DispatcherQueue? queue = null;
		m_uiQueue?.TryGetTarget(out queue);
		MUX_ASSERT(queue is null || OnUiThread());
	}

	// --- Expansion ---------------------------------------------------------------------------------

	public partial bool IsGroupExpanded(object? group) => m_expansion.IsExpanded(GetGroupIntentKey(group));

	public partial void SetGroupExpanded(object? group, bool isExpanded)
	{
		if (group is null)
		{
			return;
		}

		var key = GetGroupIntentKey(group);
		if (m_expansion.IsExpanded(key) == isExpanded)
		{
			return;  // No projection change.
		}

		// Through the model, not around it, so a programmatic ExpandAll and a per-group toggle share a
		// path. The model raises Changed -> OnExpansionChanged -> Rebuild.
		m_expansion.SetExpanded(key, isExpanded);
	}

	public partial void ExpandAll()
	{
		// Moves the BASELINE, so groups that do not exist yet also arrive expanded — "expand all" is an
		// intent, not a loop over the groups that happen to be live now.
		m_expansion.SetAllExpanded(true);
	}

	public partial void CollapseAll() => m_expansion.SetAllExpanded(false);

	private partial void OnExpansionChanged(ShapingHelpers.RowExpansionModel.Change change)
	{
		AssertRebuildOnUiThread();

		if (m_rebuildInFlight)
		{
			// A change raised synchronously while a Rebuild/splice is unwinding. m_expansion already
			// reflects the new intent, so remember it and let the outer operation run one coalesced
			// Rebuild afterward rather than nesting.
			m_pendingRebuild = true;
			return;
		}

		// Only a single-group toggle can be a ranged splice. A baseline move (ExpandAll/CollapseAll)
		// or a multi-key batch shifts the whole visible-row set, for which a full Rebuild ending in one
		// Reset is both correct and cheapest.
		if (change.AffectsAllKeys || change.Keys.Count != 1)
		{
			Rebuild();
			return;
		}

		var runPending = false;
		var applied = false;
		{
			m_rebuildInFlight = true;
			m_pendingRebuild = false;
			try
			{
				applied = TryApplyExpansionSplice(change.Keys[0], change.IsExpanded);
			}
			finally
			{
				m_rebuildInFlight = false;
				runPending = m_pendingRebuild;
				m_pendingRebuild = false;
			}
		}

		// A re-entrant request, or an incremental splice that couldn't resolve the group, resolves to a
		// single authoritative Rebuild after the guard unwinds.
		if (runPending || !applied)
		{
			Rebuild();
		}
	}

	private partial bool TryApplyExpansionSplice(string intentKey, bool expand)
	{
		var count = (uint)m_entries.Count;
		for (uint i = 0; i < count; ++i)
		{
			var entry = GroupedEntry.TryGetGroupedEntry(m_entries[(int)i]);
			if (entry is null)
			{
				continue;
			}

			var group = entry.Group();
			if (GetGroupIntentKey(group) != intentKey)
			{
				continue;
			}

			// Header found. If its stored state already matches the target, the projection is already
			// coherent for this group; nothing to splice.
			if (entry.IsExpanded() == expand)
			{
				return true;
			}

			var groupItemCount = entry.GroupItemCount();

			// Re-mint the header with the new expansion flag (GroupedEntry is immutable). One Replace at
			// i refreshes the chevron/automation without touching any other row.
			m_entries[(int)i] = new GroupedEntry(group, groupItemCount, expand);

			if (expand)
			{
				// Materialize this group's data rows and insert them immediately after the header. The
				// stored count must agree with the live enumeration; a mismatch means the group content
				// changed without a notification we processed, so defer to Rebuild for coherence.
				var items = ShapingHelpers.EnumerateInspectableItems(ShapingHelpers.GetGroupItemsObject(group));
				if (items.Count != groupItemCount)
				{
					return false;
				}

				var insertAt = i + 1;
				foreach (var item in items)
				{
					m_entries.Insert((int)insertAt++, item);
				}
			}
			else
			{
				// Collapse: remove exactly this group's data rows, which occupy the slots right after
				// the header up to the next header (or the end). If a slot isn't a data row where one is
				// expected, the projection isn't shaped the way this fast path assumes -> Rebuild.
				var firstData = i + 1;
				for (var removed = 0; removed < groupItemCount; ++removed)
				{
					if (firstData >= m_entries.Count || GroupedEntry.TryGetGroupedEntry(m_entries[(int)firstData]) is not null)
					{
						return false;
					}
					m_entries.RemoveAt((int)firstData);
				}
			}

			return true;
		}

		// The header for this key isn't materialized in the projection (mid-reshape); let Rebuild
		// reconcile from source.
		return false;
	}

	public partial object? ResolveLiveGroupByIdentity(string groupKey)
	{
		if (string.IsNullOrEmpty(groupKey) || m_ambiguousLiveGroupIdentityKeys.Contains(groupKey))
		{
			return null;
		}

		return m_liveGroupsByIdentityKey.TryGetValue(groupKey, out var group) ? group : null;
	}

	private static partial string GetGroupIntentKey(object? group)
	{
		// A value-typed key (a string department, an int year) yields a stable string that survives the
		// key object being re-minted by the next shaping pass.
		var identity = ShapingHelpers.GetGroupKeyIdentity(group);
		if (!string.IsNullOrEmpty(identity))
		{
			return "identity:" + identity;
		}

		// A group whose key is not a value type has no stable string form; fall back to the group
		// object's own identity, stable across rebuilds because the producer reuses one instance per bucket.
		return ShapingHelpers.ValueKey.ToObjectLookupKey(group, true);
	}

	// --- Source / group subscription ---------------------------------------------------------------

	private partial void AttachToSource()
	{
		var src = Source();
		if (src is null)
		{
			return;
		}

		m_attachedSourceForRevocation = src;

		WeakReference<GroupedSourceAdapter> weakThis = new(this);

		// Notifications are required on the owning UI thread and applied synchronously as a full
		// Rebuild + Reset. INotifyCollectionChanged first — typed CLR ObservableCollection<T> won't
		// surface as IObservableVector.
		if (src is INotifyCollectionChanged incc)
		{
			m_outerCollectionChangedToken = (s, e) =>
			{
				if (weakThis.TryGetTarget(out var strongThis)) { strongThis.Rebuild(); }
			};
			incc.CollectionChanged += m_outerCollectionChangedToken;
		}
		else if (src is IObservableVector<object?> obs)
		{
			m_outerVectorChangedToken = (s, e) =>
			{
				if (weakThis.TryGetTarget(out var strongThis)) { strongThis.Rebuild(); }
			};
			obs.VectorChanged += m_outerVectorChangedToken;
		}
		else if (src is IBindableObservableVector bobs)
		{
			m_outerBindableVectorChangedToken = (s, e) =>
			{
				if (weakThis.TryGetTarget(out var strongThis)) { strongThis.Rebuild(); }
			};
			bobs.VectorChanged += m_outerBindableVectorChangedToken;
		}
	}

	private partial void DetachFromSource()
	{
		var attached = m_attachedSourceForRevocation;
		if (attached is not null)
		{
			if (m_outerCollectionChangedToken is not null)
			{
				if (attached is INotifyCollectionChanged incc)
				{
					var token = m_outerCollectionChangedToken;
					ShapingHelpers.SafeRevokeWith(() => { incc.CollectionChanged -= token; });
				}
			}
			if (m_outerVectorChangedToken is not null)
			{
				if (attached is IObservableVector<object?> obs)
				{
					var token = m_outerVectorChangedToken;
					ShapingHelpers.SafeRevokeWith(() => { obs.VectorChanged -= token; });
				}
			}
			if (m_outerBindableVectorChangedToken is not null)
			{
				if (attached is IBindableObservableVector bobs)
				{
					var token = m_outerBindableVectorChangedToken;
					ShapingHelpers.SafeRevokeWith(() => { bobs.VectorChanged -= token; });
				}
			}
		}
		m_outerCollectionChangedToken = null;
		m_outerVectorChangedToken = null;
		m_outerBindableVectorChangedToken = null;
		m_attachedSourceForRevocation = null;

		UnsubscribeFromAllGroups();
	}

	private partial void SubscribeToGroup(object? group)
	{
		if (group is null)
		{
			return;
		}

		InnerGroupSubscription sub = new();

		// Observe the collection that actually holds the items, not the group object. For a group that
		// implements ICollectionViewGroup without being a collection itself.
		var items = ShapingHelpers.GetGroupItemsObject(group);
		sub.ItemsForRevocation = items;

		WeakReference<GroupedSourceAdapter> weakThis = new(this);

		// Any change inside a group is a full Rebuild — no fast path, so no weak_ref to the group is
		// needed inside the callback (it captures only weak_from_this, never a strong back-ref).
		if (items is INotifyCollectionChanged incc)
		{
			sub.CollectionToken = (s, e) =>
			{
				if (weakThis.TryGetTarget(out var strongThis)) { strongThis.Rebuild(); }
			};
			incc.CollectionChanged += sub.CollectionToken;
		}
		else if (items is IObservableVector<object?> obs)
		{
			sub.Token = (s, e) =>
			{
				if (weakThis.TryGetTarget(out var strongThis)) { strongThis.Rebuild(); }
			};
			obs.VectorChanged += sub.Token;
		}
		else if (items is IBindableObservableVector bobs)
		{
			sub.BindableToken = (s, e) =>
			{
				if (weakThis.TryGetTarget(out var strongThis)) { strongThis.Rebuild(); }
			};
			bobs.VectorChanged += sub.BindableToken;
		}

		if (sub.CollectionToken is not null || sub.Token is not null || sub.BindableToken is not null)
		{
			m_innerSubscriptions.Add(sub);
		}
	}

	private partial void UnsubscribeFromAllGroups()
	{
		// Teardown is UI-thread-guaranteed by the owning ReferenceTrackers (TableViewSource / TableView),
		// so revoking here never crosses threads. SafeRevokeWith + the weak_from_this callbacks remain
		// for re-entrancy / GC-safety.
		foreach (var sub in m_innerSubscriptions)
		{
			var strong = sub.ItemsForRevocation;
			if (strong is null)
			{
				continue;
			}
			if (sub.CollectionToken is not null)
			{
				if (strong is INotifyCollectionChanged incc)
				{
					ShapingHelpers.SafeRevokeWith(() => { incc.CollectionChanged -= sub.CollectionToken; });
				}
			}
			if (sub.Token is not null)
			{
				if (strong is IObservableVector<object?> obs)
				{
					ShapingHelpers.SafeRevokeWith(() => { obs.VectorChanged -= sub.Token; });
				}
			}
			if (sub.BindableToken is not null)
			{
				if (strong is IBindableObservableVector bobs)
				{
					ShapingHelpers.SafeRevokeWith(() => { bobs.VectorChanged -= sub.BindableToken; });
				}
			}
		}
		m_innerSubscriptions.Clear();
	}
}
