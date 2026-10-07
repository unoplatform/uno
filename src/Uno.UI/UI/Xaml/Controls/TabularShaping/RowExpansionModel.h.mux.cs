// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TabularShaping\RowExpansionModel.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Layer 1 of the Tabular shaping stack: expand/collapse INTENT, separated from the structure the
// intent applies to. Shared by both flattening axes — grouping keys it by a group's stable
// identity, hierarchy keys it by a node's stable identity — which is why it lives in layer 1
// rather than beside either adapter.
//
// A group's (or node's) expansion state looks like it belongs on the group, but it does not.
// Groups are derived: the shaping layer above re-mints them on every reshape, so state stored on
// a group dies with each sort, filter or regroup — which is exactly why "collapse a group, then
// re-sort" used to lose the collapse. Expansion is a property of the USER's intent about a KEY,
// and it outlives every group object that key ever had.
//
// So this type stores intent keyed by a caller-supplied string, holds no reference to any group,
// and knows nothing about rows, runs or a projection. What it owns is the one non-obvious rule:
// intent is stored only where it DIFFERS from the default. That is what makes "expand all" O(1)
// rather than O(groups), keeps the store from growing without bound across many toggles, and
// makes `SetAllExpanded` behave correctly for groups that do not exist yet -- a group that
// arrives later inherits the default rather than an intent nobody expressed about it.
//
// Deliberately free of WinRT collection types, XAML and any tabular vocabulary: a TreeView, a
// grouped ItemsRepeater or an app-authored hierarchy can use it directly, and it is testable
// with no dispatcher and no host.
internal static partial class ShapingHelpers
{
	internal sealed partial class RowExpansionModel
	{
		// Keys whose resolved state changed, and what they changed TO. Empty `Keys` with
		// `AffectsAllKeys` true means the default moved, so every key with no explicit intent
		// changed at once -- the case a consumer must answer with a full rebuild rather than a
		// per-key splice.
		internal struct Change
		{
			public Change()
			{
			}

			public List<string> Keys = new();
			public bool AffectsAllKeys;
			public bool IsExpanded = true;
		}

		internal delegate void ChangedHandler(Change change);

		// Raised after intent changes, never during. A handler may re-enter and read state.
		public void SetChangedHandler(ChangedHandler? handler) => m_changed = handler;

		// What a key with no explicit intent resolves to. Setting it CLEARS every explicit
		// intent: a caller changing the default is declaring a new baseline, and keeping the
		// old exceptions would resolve keys against a baseline nobody asked for.
		public bool DefaultExpanded() => m_defaultExpanded;
		public partial void SetDefaultExpanded(bool expanded);

		public partial bool IsExpanded(string key);
		public partial void SetExpanded(string key, bool isExpanded);
		public void Toggle(string key) => SetExpanded(key, !IsExpanded(key));

		// Moves the baseline and drops every exception, so keys that do not exist yet also
		// resolve to `isExpanded`. This is "expand all" as an intent, not as a loop over the
		// groups that happen to be live.
		public partial void SetAllExpanded(bool isExpanded);

		// Drops intent for keys that no longer exist. Without this, an intent for a group that
		// vanished (a filter removed its last row, the source was reassigned) lingers forever and
		// the store grows unbounded across changing datasets. Silent: pruning a dead key changes
		// no live key's resolved state.
		public partial void RetainOnly(HashSet<string> liveKeys);

		public partial void Clear();

		private partial void RaiseChanged(Change change);

		private bool m_defaultExpanded = true;
		// Exceptions only. A key present here resolves to the OPPOSITE of m_defaultState, which
		// is why moving the default has to clear the set rather than reinterpret it.
		private HashSet<string> m_nonDefault = new();
		private ChangedHandler? m_changed;
	}
}
