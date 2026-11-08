// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\HierarchicalSourceAdapter\HierarchicalSourceAdapter.cpp, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Dispatching;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class HierarchicalSourceAdapter
{
	// Every node key the index mints starts with this. A bare prefix is not a key.
	private const string c_nodeKeyPrefix = "node:";

	// Above this run length, one Rebuild + Reset beats per-row splice notifications.
	private const int c_maxSpliceRows = 64;

	public HierarchicalSourceAdapter()
	{
		// __RP_Marker_ClassById(RuntimeProfiler.ProfId_HierarchicalSourceAdapter);

		var queue = DispatcherQueue.GetForCurrentThread();
		if (queue is null)
		{
			// TODO Uno: winrt::hresult_error(RPC_E_WRONG_THREAD, ...) maps to InvalidOperationException.
			throw new InvalidOperationException("HierarchicalSourceAdapter must be constructed on a UI thread.");
		}
		m_uiQueue = new WeakReference<DispatcherQueue>(queue);

		m_entriesView = new ItemsSourceView(m_entries);

		// Collapsed baseline so untouched nodes are not walked. Set before attaching the handler so it
		// raises no Change into a half-constructed adapter.
		m_expansion.SetAllExpanded(false);

		m_expansion.SetChangedHandler(
			(ShapingHelpers.RowExpansionModel.Change change) =>
			{
				if (m_suppressExpansionChanged)
				{
					m_expansionChangedWhileSuppressed = true;
					return;
				}
				OnExpansionChanged(change);
			});
	}

	private partial bool MutateExpansionQuietly(Action fn)
	{
		m_suppressExpansionChanged = true;
		m_expansionChangedWhileSuppressed = false;
		try
		{
			fn();
		}
		finally
		{
			m_suppressExpansionChanged = false;
		}
		var changed = m_expansionChangedWhileSuppressed;
		m_expansionChangedWhileSuppressed = false;
		return changed;
	}

	public partial void ProjectionChanged(Action? fn)
	{
		m_projectionChanged = fn;
	}

	private partial void RaiseProjectionChanged()
	{
		// Copied: the handler may reassign m_projectionChanged while running inside it.
		var handler = m_projectionChanged;
		if (handler is not null)
		{
			handler();
		}
	}

	public static partial bool IsNodeKey(string key)
	{
		return key.Length > c_nodeKeyPrefix.Length && key.StartsWith(c_nodeKeyPrefix, StringComparison.Ordinal);
	}

	// --- Index hand-off -------------------------------------------------------------------------------

	public partial void SetIndex(ShapingHelpers.ParentKeyIndex? index, List<int> rootSegments)
	{
		// Pin self: a consumer reacting to the publish may drop the last external owner.

		m_detached = false;
		m_index = index;
		m_rootSegments = rootSegments;

		// A collapse override only means something on a row that is still a context row.
		if (m_index is not null)
		{
			m_filterOverlayCollapsed.RemoveWhere(key => !m_index.ContextKeys.Contains(key));
		}
		else
		{
			m_filterOverlayCollapsed.Clear();
		}

		Rebuild();

		// Pruned after publish, against the UNFILTERED key set, so intent for filtered-out rows survives.
		if (m_index is not null && m_index.Structure is not null && !IsPrunedForStructure(m_index.Structure))
		{
			// Same structure means already pruned.
			HashSet<string> liveKeys = new(m_index.Structure.KeyByItem.Count, StringComparer.Ordinal);
			foreach (var key in m_index.Structure.KeyByItem.Values)
			{
				liveKeys.Add(key);
			}
			m_expansion.RetainOnly(liveKeys);
			m_prunedForStructure = new WeakReference<ShapingHelpers.ParentStructure>(m_index.Structure);
		}
	}

	// Uno specific: m_prunedForStructure.lock() != m_index->Structure.
	private bool IsPrunedForStructure(ShapingHelpers.ParentStructure structure)
	{
		return m_prunedForStructure is not null &&
			m_prunedForStructure.TryGetTarget(out var pruned) &&
			ReferenceEquals(pruned, structure);
	}

	public partial void ClearIndex()
	{
		// No Rebuild: the repeater may still be bound mid-teardown, so an empty Reset is a hazard.
		// May run inside our own notification; m_detached stops the still-publishing frame.
		m_detached = true;
		m_index = null;
		m_rootSegments = new();
		m_filterOverlayCollapsed.Clear();

		// Descriptors kept: stale rows may stay bound until the projection swap.
		m_indexByNodeKey.Clear();
		m_indexByNodeKeyValid = true;
	}

	public partial void ResetIntentQuietly()
	{
		// SetAllExpanded moves the baseline and also clears every explicit intent.
		MutateExpansionQuietly(() => { m_expansion.SetAllExpanded(false); });
		m_filterOverlayCollapsed.Clear();
	}

	public partial void ResetFilterOverlay()
	{
		m_filterOverlayCollapsed.Clear();
	}

	// --- The one responsibility: materialize the visible rows ----------------------------------------

	private partial bool EffectiveExpanded(string nodeKey, bool hasChildren, bool isContext)
	{
		if (!hasChildren)
		{
			return false;
		}

		// Context rows follow the overlay only, so toggling them never writes intent.
		return isContext ? !m_filterOverlayCollapsed.Contains(nodeKey) : m_expansion.IsExpanded(nodeKey);
	}

	private partial bool EffectiveExpandedForKey(string nodeKey)
	{
		if (m_index is null)
		{
			return m_expansion.IsExpanded(nodeKey);
		}

		var children = m_index.TryGetChildren(nodeKey);
		return EffectiveExpanded(nodeKey, children is not null && children.Count != 0, m_index.ContextKeys.Contains(nodeKey));
	}

	// TODO Uno: a class rather than a C++ local struct, so the back of the stack can be mutated through a reference.
	private sealed class Frame
	{
		public required List<object?> Siblings;
		public required int Begin;
		public required int End;
		public required int Next;
		public required string ParentKey;
		public required int Depth;
	}

	private partial void EmitRange(
		List<object?> siblings,
		int begin,
		int end,
		string parentKey,
		int depth,
		List<object?> built,
		List<NodeRow> descriptors)
	{
		if (m_index is null)
		{
			return;
		}

		List<Frame> stack = new();
		stack.Add(new Frame { Siblings = siblings, Begin = begin, End = end, Next = begin, ParentKey = parentKey, Depth = depth });

		while (stack.Count != 0)
		{
			var frame = stack[stack.Count - 1];
			if (frame.Next >= frame.End)
			{
				stack.RemoveAt(stack.Count - 1);
				continue;
			}

			var i = frame.Next++;
			var item = frame.Siblings[i];
			var key = m_index.TryGetKey(item);
			if (key is null)
			{
				// Every indexed item has a key; an unknown one cannot be addressed, so it is skipped.
				continue;
			}

			var children = m_index.TryGetChildren(key);

			NodeRow row = new();
			row.Item = item;
			row.NodeKey = key;
			row.ParentKey = frame.ParentKey;
			row.Depth = frame.Depth;
			row.ChildCount = children is not null ? children.Count : 0;
			row.SiblingIndex = i - frame.Begin + 1;
			row.SiblingCount = frame.End - frame.Begin;
			row.HasChildren = row.ChildCount > 0;
			row.IsContext = m_index.ContextKeys.Contains(key);
			row.IsExpanded = EffectiveExpanded(row.NodeKey, row.HasChildren, row.IsContext);

			// Captured before the push below, which may reallocate the stack and invalidate `frame`.
			var childDepth = frame.Depth + 1;
			var descend = row.IsExpanded;
			var nodeKey = row.NodeKey;

			built.Add(item);
			descriptors.Add(row);

			if (descend)
			{
				stack.Add(new Frame { Siblings = children!, Begin = 0, End = children!.Count, Next = 0, ParentKey = nodeKey, Depth = childDepth });
			}
		}
	}

	private partial void Rebuild()
	{
		AssertRebuildOnUiThread();

		if (m_detached)
		{
			// Retracted: publishing even an empty Reset would reach a repeater mid-teardown.
			return;
		}

		if (m_rebuildInFlight)
		{
			// Re-entered from a ReplaceAll notification: coalesce into one follow-up after unwind.
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
				List<object?> built = new();
				List<NodeRow> descriptors = new();

				if (m_index is not null)
				{
					var roots = m_index.Roots;
					if (m_rootSegments.Count == 0)
					{
						EmitRange(roots, 0, roots.Count, "", 0, built, descriptors);
					}
					else
					{
						// One segment per group bucket, so a root's sibling position is per bucket.
						MUX_ASSERT(m_rootSegments.Sum() == roots.Count);

						var segmentBegin = 0;
						foreach (var length in m_rootSegments)
						{
							var segmentEnd = Math.Min(segmentBegin + length, roots.Count);
							EmitRange(roots, segmentBegin, segmentEnd, "", 0, built, descriptors);
							segmentBegin = segmentEnd;
						}
					}
				}

				// Built into a local so a failure cannot publish a partial map.
				Dictionary<string, int> indexByNodeKey = new(descriptors.Count, StringComparer.Ordinal);
				for (var i = 0; i < descriptors.Count; ++i)
				{
					indexByNodeKey.TryAdd(descriptors[i].NodeKey, i);
				}

				// Publish metadata before the Reset so synchronous handlers see agreeing state. Moves; no-throw.
				var previousDescriptors = m_descriptors;
				m_descriptors = descriptors;
				var previousIndex = m_indexByNodeKey;
				m_indexByNodeKey = indexByNodeKey;
				var previousIndexValid = m_indexByNodeKeyValid;
				m_indexByNodeKeyValid = true;
				m_indexByItemValid = false;

				// Roll back metadata only if ReplaceAll's assignment threw (old rows still published, detected
				// by row count). A throw from a Reset handler happens after the new rows are live; keep them.
				var publishedCount = built.Count;
				try
				{
					m_entries.ReplaceAll(built);
				}
				catch
				{
					if (m_entries.Count != publishedCount)
					{
						m_descriptors = previousDescriptors;
						m_indexByNodeKey = previousIndex;
						m_indexByNodeKeyValid = previousIndexValid;
						m_indexByItemValid = false;
					}
					throw;
				}
			}
			finally
			{
				// resetGuard runs here, publishing into runPending whether a re-entrant request arrived.
				m_rebuildInFlight = false;
				runPending = m_pendingRebuild;
				m_pendingRebuild = false;
			}
		} while (runPending && !m_detached);

		if (m_detached)
		{
			// A handler of the Reset above retracted the projection; see ClearIndex.
			return;
		}

		// One edge for the whole rebuild.
		RaiseProjectionChanged();
	}

	// --- Expansion -----------------------------------------------------------------------------------

	public partial bool IsNodeExpanded(string nodeKey)
	{
		// Answer the presented state (incl. overlay), so toggling an overlay-expanded context row
		// collapses it rather than no-op expanding.
		if (TryGetIndexForNodeKey(nodeKey, out var index))
		{
			var row = TryGetNodeRow(index);
			if (row is not null)
			{
				return row.IsExpanded;
			}
		}

		// Not visible: context rows answer from the overlay, others from intent.
		if (m_index is not null && m_index.ContextKeys.Contains(nodeKey))
		{
			return EffectiveExpandedForKey(nodeKey);
		}
		return m_expansion.IsExpanded(nodeKey);
	}

	public partial void SetNodeExpanded(string nodeKey, bool isExpanded)
	{
		if (m_index is null)
		{
			return;
		}

		if (!TryGetIndexForNodeKey(nodeKey, out var index))
		{
			return;
		}

		var row = TryGetNodeRow(index);
		if (row is null || !row.HasChildren)
		{
			return;
		}

		var before = row.IsExpanded;

		if (row.IsContext)
		{
			// Overlay-only for context rows; discarded when the filter changes.
			if (isExpanded)
			{
				m_filterOverlayCollapsed.Remove(nodeKey);
			}
			else
			{
				m_filterOverlayCollapsed.Add(nodeKey);
			}

			var after = EffectiveExpanded(nodeKey, true, true);
			if (before != after)
			{
				ApplySingleToggle(nodeKey, after);
			}
			return;
		}

		if (m_expansion.IsExpanded(nodeKey) != isExpanded)
		{
			// Through the model, so its Changed notification drives ApplySingleToggle.
			m_expansion.SetExpanded(nodeKey, isExpanded);
		}
		else if (before != isExpanded)
		{
			// Intent agrees but presented state does not (earlier materialization never landed); reconcile.
			ApplySingleToggle(nodeKey, isExpanded);
		}
	}

	public partial void ExpandAll()
	{
		var hadOverlay = m_filterOverlayCollapsed.Count != 0;
		m_filterOverlayCollapsed.Clear();

		// Moves the baseline, so future nodes also arrive expanded.
		var changed = MutateExpansionQuietly(() => { m_expansion.SetAllExpanded(true); });
		if (changed || hadOverlay)
		{
			Rebuild();
		}
	}

	public partial void CollapseAll()
	{
		var changed = MutateExpansionQuietly(() => { m_expansion.SetAllExpanded(false); });

		// Context rows are overlay-expanded, so collapse-all must override each one.
		var overlayChanged = false;
		if (m_index is not null)
		{
			foreach (var key in m_index.ContextKeys)
			{
				overlayChanged |= m_filterOverlayCollapsed.Add(key);
			}
		}

		if (changed || overlayChanged)
		{
			Rebuild();
		}
	}

	public partial void ExpandSubtree(string nodeKey)
	{
		if (m_index is null || !IsNodeKey(nodeKey))
		{
			return;
		}

		var overlayChanged = false;
		var changed = MutateExpansionQuietly(() =>
		{
			List<string> pending = new() { nodeKey };
			while (pending.Count != 0)
			{
				var key = pending[pending.Count - 1];
				pending.RemoveAt(pending.Count - 1);

				var children = m_index.TryGetChildren(key);
				if (children is null || children.Count == 0)
				{
					continue;
				}

				if (m_index.ContextKeys.Contains(key))
				{
					// A context row is presented from the overlay alone; expanding it writes no intent.
					overlayChanged |= m_filterOverlayCollapsed.Remove(key);
				}
				else
				{
					m_expansion.SetExpanded(key, true);
				}

				foreach (var child in children)
				{
					var childKey = m_index.TryGetKey(child);
					if (childKey is not null)
					{
						pending.Add(childKey);
					}
				}
			}
		});

		if (changed || overlayChanged)
		{
			Rebuild();
		}
	}

	private partial void OnExpansionChanged(ShapingHelpers.RowExpansionModel.Change change)
	{
		AssertRebuildOnUiThread();

		// Only a single-node toggle can splice; baseline moves and batches rebuild.
		if (change.AffectsAllKeys || change.Keys.Count != 1)
		{
			Rebuild();
			return;
		}

		// Splice to the presented state; for context rows intent changes move nothing.
		ApplySingleToggle(change.Keys[0], EffectiveExpandedForKey(change.Keys[0]));
	}

	private partial void ApplySingleToggle(string nodeKey, bool isExpanded)
	{
		AssertRebuildOnUiThread();

		if (m_rebuildInFlight)
		{
			// Raised mid-publish; the outer operation runs one coalesced Rebuild afterward.
			m_pendingRebuild = true;
			return;
		}

		var runPending = false;
		var applied = false;
		{
			m_rebuildInFlight = true;
			m_pendingRebuild = false;
			try
			{
				applied = TryApplyExpansionSplice(nodeKey, isExpanded);
			}
			finally
			{
				m_rebuildInFlight = false;
				runPending = m_pendingRebuild;
				m_pendingRebuild = false;
			}
		}

		// Re-entrant or unresolved: one Rebuild after unwind (no-op once detached).
		if (runPending || !applied)
		{
			Rebuild();
		}
	}

	private partial bool TryApplyExpansionSplice(string nodeKey, bool expand)
	{
		// Structures must already agree or a ranged splice would corrupt the pairing.
		if (m_index is null || m_descriptors.Count != m_entries.Count)
		{
			return false;
		}

		if (!TryGetIndexForNodeKey(nodeKey, out var index) || index < 0 || index >= m_descriptors.Count)
		{
			// Not visible (behind a collapsed ancestor); Rebuild reconciles.
			return false;
		}

		var node = m_descriptors[index];
		if (node.IsExpanded == expand || !node.HasChildren)
		{
			return true;
		}

		var nodeDepth = node.Depth;

		if (!expand)
		{
			// Visible descendants = contiguous following rows with greater Depth.
			var runEnd = index + 1;
			while (runEnd < m_descriptors.Count && m_descriptors[runEnd].Depth > nodeDepth)
			{
				++runEnd;
			}

			if (runEnd - (index + 1) > c_maxSpliceRows)
			{
				// A large run costs one notification per row; one Reset is cheaper.
				return false;
			}

			// Descriptor first, so the row reads collapsed when RemoveRows notifies.
			m_descriptors[index].IsExpanded = false;
			RemoveRows(index + 1, runEnd - (index + 1));
			if (!m_detached)
			{
				RaiseProjectionChanged();
			}
			return true;
		}

		var children = m_index.TryGetChildren(nodeKey);
		if (children is null || children.Count == 0)
		{
			return false;
		}

		// Expand: emit the subtree from the index (honouring nested intent), then insert.
		List<object?> built = new();
		List<NodeRow> descriptors = new();
		EmitRange(children, 0, children.Count, nodeKey, nodeDepth + 1, built, descriptors);

		if (built.Count > c_maxSpliceRows)
		{
			return false;
		}

		// Parent first, for the same reason the collapse path does it.
		m_descriptors[index].IsExpanded = true;
		InsertRows(index + 1, built, descriptors);
		if (!m_detached)
		{
			RaiseProjectionChanged();
		}
		return true;
	}

	// --- The triple invariant ------------------------------------------------------------------------

	private partial void InsertRows(
		int index,
		List<object?> items,
		List<NodeRow> descriptors)
	{
		MUX_ASSERT(items.Count == descriptors.Count);
		if (items.Count == 0)
		{
			return;
		}

		// One row at a time, descriptor before its row: InsertAt notifies synchronously and handlers
		// read descriptors by index. The node-key map is invalidated and rebuilt lazily.
		var insertAt = index;
		for (var i = 0; i < items.Count && !m_detached; ++i)
		{
			m_descriptors.Insert(insertAt, descriptors[i]);
			m_indexByNodeKeyValid = false;
			m_indexByItemValid = false;
			m_entries.Insert(insertAt++, items[i]);
		}
	}

	private partial void RemoveRows(int index, int count)
	{
		if (count <= 0)
		{
			return;
		}

		// Mirror of InsertRows, removed from the end so remaining rows keep indices until removed.
		// Stops if a handler retracts the projection (see ClearIndex).
		for (var i = index + count - 1; i >= index && !m_detached; --i)
		{
			m_descriptors.RemoveAt(i);
			m_indexByNodeKeyValid = false;
			m_indexByItemValid = false;
			m_entries.RemoveAt(i);
		}
	}

	// --- Lookups --------------------------------------------------------------------------------------

	public partial NodeRow? TryGetNodeRow(int index)
	{
		if (index < 0 || index >= m_descriptors.Count)
		{
			return null;
		}

		return m_descriptors[index];
	}

	public partial NodeRow? TryGetNodeRowForItem(object? item)
	{
		if (item is null)
		{
			return null;
		}

		EnsureItemIndex();

		if (!m_indexByItem.TryGetValue(item, out var found) ||
			found < 0 ||
			found >= m_descriptors.Count)
		{
			return null;
		}

		return m_descriptors[found];
	}

	public partial bool IndexContainsItem(object? item)
	{
		if (item is null || m_index is null || m_index.Structure is null)
		{
			return false;
		}

		var structure = m_index.Structure;
		var key = m_index.TryGetKey(item);
		if (key is null)
		{
			return false;
		}

		if (!structure.IndexByKey.TryGetValue(key, out var position))
		{
			return false;
		}

		// The filter keeps a node in its parent's sibling list (or the roots), so look only there.
		var parent = structure.ParentIndex[position];
		var siblings = m_index.Roots;
		if (parent != ShapingHelpers.ParentStructure.Root)
		{
			var children = m_index.TryGetChildren(structure.NodeKeys[parent]);
			if (children is null)
			{
				return false;
			}
			siblings = children;
		}

		var target = structure.Rows[position];
		return siblings.Exists(sibling => ReferenceEquals(sibling, target));
	}

	private partial void EnsureItemIndex()
	{
		if (m_indexByItemValid)
		{
			return;
		}

		m_indexByItem.Clear();
		m_indexByItem.EnsureCapacity(m_descriptors.Count);

		for (var i = 0; i < m_descriptors.Count; ++i)
		{
			var descriptorItem = m_descriptors[i].Item;
			if (descriptorItem is not null)
			{
				m_indexByItem.TryAdd(descriptorItem, i);
			}
		}

		m_indexByItemValid = true;
	}

	private partial void EnsureNodeKeyIndex()
	{
		if (m_indexByNodeKeyValid)
		{
			return;
		}

		m_indexByNodeKey.Clear();
		m_indexByNodeKey.EnsureCapacity(m_descriptors.Count);

		for (var i = 0; i < m_descriptors.Count; ++i)
		{
			m_indexByNodeKey.TryAdd(m_descriptors[i].NodeKey, i);
		}

		m_indexByNodeKeyValid = true;
	}

	public partial bool TryGetIndexForNodeKey(string nodeKey, out int index)
	{
		index = -1;
		if (string.IsNullOrEmpty(nodeKey))
		{
			return false;
		}

		EnsureNodeKeyIndex();

		if (!m_indexByNodeKey.TryGetValue(nodeKey, out var found))
		{
			return false;
		}

		index = found;
		return true;
	}

	// --- Threading ------------------------------------------------------------------------------------

	private partial bool OnUiThread()
	{
		return m_uiQueue is not null && m_uiQueue.TryGetTarget(out var queue) && queue.HasThreadAccess;
	}

	private partial void AssertRebuildOnUiThread()
	{
		MUX_ASSERT(m_uiQueue is null || !m_uiQueue.TryGetTarget(out _) || OnUiThread());
	}
}
