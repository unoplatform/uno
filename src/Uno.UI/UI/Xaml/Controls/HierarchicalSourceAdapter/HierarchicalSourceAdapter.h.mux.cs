// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\HierarchicalSourceAdapter\HierarchicalSourceAdapter.h, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Dispatching;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Projects a validated, immutable ParentKeyIndex into the currently visible rows (raw app items; no
// wrappers). Pure projector: observes nothing, calls no app code, cannot fail part-way. Per-row
// metadata lives in a parallel POD vector. UI-thread-affine; publishing entry points pin themselves
// with shared_from_this because a notification handler may drop the last owner.
// TODO Uno: std::enable_shared_from_this becomes a plain class; an executing method's `this` is already reachable, so the keepAlive pins are not needed.
internal sealed partial class HierarchicalSourceAdapter
{
	// Must be constructed on a UI thread (throws RPC_E_WRONG_THREAD otherwise). Expansion baseline
	// is COLLAPSED so the first Rebuild does not realize the whole tree.
	// HierarchicalSourceAdapter(); (HierarchicalSourceAdapter.mux.cs)

	// Created once over m_entries, whose identity is stable for the adapter's lifetime.
	public ItemsSourceView? Entries() => m_entriesView;

	// Publishes a projection over `index`, then prunes intent to keys in the UNFILTERED source.
	// `rootSegments` splits index->Roots into per-group-bucket runs; empty means one segment.
	public partial void SetIndex(ShapingHelpers.ParentKeyIndex? index, List<int> rootSegments);

	// Drops the index without publishing; keeps intent. Detaches the adapter so an unwinding
	// publication stops at the next safe point. Only SetIndex re-attaches.
	public partial void ClearIndex();

	// Clears intent and filter overlay without rebuilding (new relation = new tree). Caller then
	// calls SetIndex / ClearIndex.
	public partial void ResetIntentQuietly();

	// Drops collapse overrides on filter context rows. Called when the filter changes.
	public partial void ResetFilterOverlay();

	// Raised once per coherent publish. Not Entries().CollectionChanged, which fires mid-mutation
	// and once per spliced row.
	public partial void ProjectionChanged(Action? fn);

	// Addressed by node key ("node:" + app key lookup form). SetNodeExpanded on a context row
	// moves only the filter overlay, never intent.
	public partial bool IsNodeExpanded(string nodeKey);
	public partial void SetNodeExpanded(string nodeKey, bool isExpanded);

	// Move the model's baseline, so not-yet-existing nodes inherit it. One Reset, no splice.
	public partial void ExpandAll();
	public partial void CollapseAll();

	// Expands `nodeKey` and all descendants (context rows via overlay), then publishes once.
	public partial void ExpandSubtree(string nodeKey);

	// Row metadata, index-aligned 1:1 with the visible rows.
	// TODO Uno: a class rather than a struct; C++ hands out NodeRow const* and mutates vector elements in place.
	internal sealed class NodeRow
	{
		public object? Item;
		public string NodeKey = "";
		// Node key of the parent row; empty for roots.
		public string ParentKey = "";

		// 0-based (roots render flush); TableViewRowInfo::Level is Depth + 1.
		public int Depth;

		public int ChildCount;

		// 1-based position in the shaped sibling set and its size. Under grouping, a root's
		// sibling set is its group bucket.
		public int SiblingIndex;
		public int SiblingCount;

		public bool HasChildren;
		public bool IsExpanded;

		// Kept only because a descendant matched the active filter.
		public bool IsContext;
	}

	public partial NodeRow? TryGetNodeRow(int index);
	public partial bool TryGetIndexForNodeKey(string nodeKey, out int index);

	// Lookup by item, for grouped hierarchies where presented indices include header rows. Sound
	// because an item occupies at most one row. Null if not visible.
	public partial NodeRow? TryGetNodeRowForItem(object? item);

	// True when `item` is a node of the filtered tree, whether or not its ancestors are expanded.
	public partial bool IndexContainsItem(object? item);
	public bool HasIndex() => m_index is not null;
	// Every node of the tree, before filtering; 0 with no index.
	public uint NodeCount()
	{
		return m_index is not null && m_index.Structure is not null ? (uint)m_index.Structure.Rows.Count : 0;
	}
	// The nodes the filter keeps, whether or not their ancestors are expanded; 0 with no index.
	public uint KeptNodeCount()
	{
		return m_index is not null ? (uint)m_index.KeptNodeCount : 0;
	}

	// Positive prefix test, so a stale or foreign key cannot become node intent.
	public static partial bool IsNodeKey(string key);

	private partial void Rebuild();
	private partial bool OnUiThread();
	private partial void AssertRebuildOnUiThread();
	private partial void OnExpansionChanged(ShapingHelpers.RowExpansionModel.Change change);

	// Splice when possible, Rebuild otherwise. Coalesces with an in-flight rebuild.
	private partial void ApplySingleToggle(string nodeKey, bool isExpanded);

	// Context rows follow the filter overlay; all others follow persistent intent.
	private partial bool EffectiveExpanded(string nodeKey, bool hasChildren, bool isContext);

	private partial bool EffectiveExpandedForKey(string nodeKey);

	// Explicit stack, not recursion, so depth is unbounded.
	private partial void EmitRange(
		List<object?> siblings,
		int begin,
		int end,
		string parentKey,
		int depth,
		List<object?> built,
		List<NodeRow> descriptors);

	// Splices one node's visible subtree (the contiguous run of deeper rows after it) in or out.
	// Returns false, so the caller rebuilds, if the key does not resolve, descriptor/entry counts
	// disagree, or the run exceeds c_maxSpliceRows.
	private partial bool TryApplyExpansionSplice(string nodeKey, bool expand);

	// Entries, descriptors and index maps are only correct as a set; mutate only through these.
	private partial void InsertRows(int index, List<object?> items, List<NodeRow> descriptors);
	private partial void RemoveRows(int index, int count);

	private partial void RaiseProjectionChanged();

	private partial void EnsureItemIndex();
	private partial void EnsureNodeKeyIndex();

	// Mutates expansion without Changed driving a splice/rebuild; returns whether anything changed.
	private partial bool MutateExpansionQuietly(Action fn);

	private ShapingHelpers.ParentKeyIndex? m_index;
	// Structure intent was last pruned against (see SetIndex).
	private WeakReference<ShapingHelpers.ParentStructure>? m_prunedForStructure;
	private List<int> m_rootSegments = new();

	// Context rows collapsed under the current filter; cleared on filter change, never intent.
	private readonly HashSet<string> m_filterOverlayCollapsed = new(StringComparer.Ordinal);

	private Action? m_projectionChanged;

	// Visible rows, as raw app items.
	// TODO Uno: typed as the concrete ObservableVector<T> (single_threaded_observable_vector) so ReplaceAll raises one Reset.
	private readonly ObservableVector<object?> m_entries = new();

	private ItemsSourceView? m_entriesView;

	// Index-aligned 1:1 with m_entries.
	private List<NodeRow> m_descriptors = new();

	// Lazy caches over m_descriptors: invalidated by mutations, rebuilt in one O(V) pass on lookup.
	private Dictionary<string, int> m_indexByNodeKey = new(StringComparer.Ordinal);
	private bool m_indexByNodeKeyValid = true;

	// Item ABI pointer -> index; the item is the only handle shared with the grouped axis.
	// TODO Uno: the ABI pointer becomes the item's reference identity.
	private readonly Dictionary<object, int> m_indexByItem = new(ReferenceEqualityComparer.Instance);
	private bool m_indexByItemValid;

	private readonly ShapingHelpers.RowExpansionModel m_expansion = new();

	private bool m_suppressExpansionChanged;
	private bool m_expansionChangedWhileSuppressed;

	// Affinity assertion only (chk).
	private WeakReference<DispatcherQueue>? m_uiQueue;

	// Re-entrancy guard: requests during a publish are coalesced and run once after unwind.
	private bool m_rebuildInFlight;
	private bool m_pendingRebuild;

	// Set by ClearIndex; notifying loops check it after each mutation (owner may retract mid-notify).
	private bool m_detached;
}
