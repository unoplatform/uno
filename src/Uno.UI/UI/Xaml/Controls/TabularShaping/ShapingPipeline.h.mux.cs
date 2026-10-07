// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TabularShaping\ShapingPipeline.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Layer 2 of the Tabular shaping stack: the engine that owns the *current* set of shaping verbs
// and applies them to a materialized row vector.
//
// Layer 1 (ShapingDescriptions) says what a shape is and how to diff two of them. This layer
// owns the mutable, ordered accumulation of verbs a fluent caller builds up over time -- which
// sort axes are active, in what order they were declared relative to each other and to the group
// verb, and which filter predicates are in force -- and turns that into the two operations
// every projection needs: filter a vector, and sort a vector (or a slice of the axes).
//
// It holds no collection, no dispatcher, no notification state and no tabular vocabulary. The
// owner keeps the projection; this keeps the recipe. That split is what lets the projection layer
// above be about incremental change and marshaling rather than about shaping.
internal static partial class ShapingHelpers
{
	// One active sort axis. Storage order is declaration order; PRECEDENCE is by Order, earliest
	// declared first -- see ActiveSortAxes.
	// TODO Uno: a value type as in C++; mutating an element of List<SortAxis> needs a read-modify-write.
	internal struct SortAxis
	{
		public SortAxis()
		{
		}

		// Identifies the axis for replace/clear (e.g. a column token). Empty is legal and means
		// the axis is identified only by its evaluator.
		public string AxisToken = "";
		public KeySelector? Key;
		// Stable identity of the evaluator, used to match axes when AxisToken is empty.
		// std::function is not equality-comparable, so a caller that wants un-tokenized replace
		// semantics supplies the underlying delegate here as an IUnknown. Comparing the
		// interface (rather than a raw address) keeps the original delegate-equality semantics,
		// which match two different interface pointers on the same COM object.
		// TODO Uno: IUnknown identity becomes object reference identity.
		public object? KeyIdentity;
		// The property path this axis sorts on, when the caller named one. Empty means the key is
		// a delegate that no path expresses (computed, multi-field, identity, or a ranked custom
		// comparer). Purely descriptive - the pipeline never evaluates it - but it is what lets a
		// consumer read an axis back and say which column it is about.
		public string SortMemberPath = "";
		public SortDirection Direction;
		// Position in the single verb sequence shared with the group verb. Sorts declared before
		// GroupBy order the groups; sorts declared after it order rows within a group.
		public int Order;
		// Stable diff identity of this axis, minted whenever the axis is declared or re-declared.
		// A layer-1 SortDescription is compared by PropertyName, and the fluent surface offers no
		// property path to compare, so the pipeline supplies one. It is deliberately NOT the
		// AxisToken: the same token can be re-declared at the same direction carrying a different
		// key delegate, which naming by token would diff as "unchanged". Re-minting per
		// declaration makes the diff conservative in the safe direction — it can report a change
		// that turned out to be identical, never the reverse.
		public string DescriptionId = "";
	}

	// One active filter axis. All axes are conjunctive and all of them run before any grouping
	// or sorting, so unlike SortAxis there is no verb Order here: declaration order is only a
	// tie-break for evaluation cost, never for meaning.
	internal struct FilterAxis
	{
		public FilterAxis()
		{
		}

		// Identifies the axis for replace/clear (e.g. a column token). Empty is legal and is the
		// single-filter shorthand's axis, so a caller that never names its filters keeps exactly
		// the old one-predicate behaviour.
		public string AxisToken = "";
		public Predicate? Predicate;
		// Stable diff identity of this axis, re-minted on every declaration. A predicate's
		// parameters (operator, threshold, selected values) live inside the delegate and are
		// invisible to a diff, so without this the commonest mutation -- same column, different
		// threshold -- would diff as "no change" and silently keep the old membership.
		public string CriterionId = "";
	}

	internal sealed partial class ShapingPipeline
	{
		// -- filter ---------------------------------------------------------------------------
		// Filters are conjunctive and always run before sort/group shaping. Axes are identified
		// by token so a caller with several independent filter sources (a column filter and a
		// search box, say) can declare and retract them independently.
		//
		// The untokenized overload is the single-filter shorthand: it declares the empty-token
		// axis, so calling it repeatedly replaces one predicate exactly as it always did.
		public partial void SetFilter(Predicate? predicate);
		// A null predicate removes the axis rather than declaring an always-false one.
		public partial void SetFilter(string axisToken, Predicate? predicate);
		public partial void ClearFilter();
		public partial void ClearFilter(string axisToken);
		public bool HasFilter() => m_filters.Count != 0;
		// A predicate that throws excludes the item (fail-safe). Returns true when no filter is
		// set, so callers can gate unconditionally.
		public partial bool PassesFilter(object? item);
		public partial void ApplyFilter(List<object?> rows);

		// -- sort -----------------------------------------------------------------------------
		// Declares or replaces a sort axis. When previousAxisToken is non-empty and differs from
		// axisToken, axes carrying it are dropped first (a column re-keying itself). A direction
		// of None removes the axis instead of adding it. sortMemberPath is descriptive only and
		// may be empty; see SortAxis::SortMemberPath.
		public partial void SetSort(
			string previousAxisToken,
			string axisToken,
			KeySelector? key,
			object? keyIdentity,
			string sortMemberPath,
			SortDirection direction);
		public void ClearSorts() => m_sorts.Clear();
		public partial void ClearSort(string axisToken);
		// Drops every axis EXCEPT the one carrying axisToken. An untokenized axis (empty
		// AxisToken) can only be cleared this way: ClearSort("") means clear-all, so a caller
		// holding one token cannot address the axes it does not own by token alone.
		public partial void ClearSortsExcept(string axisToken);
		public partial bool HasActiveSort();

		// Axes with a live key and direction whose Order falls strictly between the bounds. A
		// negative bound is unbounded, so ActiveSortAxes(-1, -1) is every active axis. Returned in
		// PRECEDENCE order -- earliest declared first -- so index 0 is the primary sort.
		public partial List<SortAxis> ActiveSortAxes(int afterOrder, int beforeOrder);
		public partial void ApplySort(List<object?> rows, int afterOrder = -1, int beforeOrder = -1);

		// Where a newly-arrived row belongs, plus whether the sort alone actually decides that.
		internal struct SortedInsertPlacement
		{
			public uint Index;
			// True when a row already in the projection compares equal on EVERY active axis, so
			// the sort keys alone do not determine where the new row goes: the tie has to be
			// broken by source order, which the projection does not carry. A caller that cannot
			// establish the source order of the tie group must rebuild rather than guess.
			public bool TiedWithExistingRow;
		}

		public partial SortedInsertPlacement SortedInsertPlacementFor(
			object? item,
			uint count,
			Func<uint, object?> getRow);

		// Compares `item` against `row` on the active axes alone, applying each axis's direction.
		// 0 means the two are indistinguishable to the sort, i.e. their relative order is decided
		// by the stable sort's source-order tiebreak rather than by any key.
		public partial int CompareItemToRow(object? item, object? row);

		// -- verb ordering --------------------------------------------------------------------
		public int NextVerbOrder() => m_nextVerbOrder++;
		// Places the group verb at the end of the current verb sequence and returns its order.
		// The key is optional and is carried only so the spec can describe the grouping axis;
		// this class never evaluates it.
		public partial int MarkGroupVerb(KeySelector? key = null);
		public partial void ClearGroupVerb();
		public int GroupOrder() => m_groupOrder;

		// -- spec -----------------------------------------------------------------------------
		// The layer-1 description of the verbs currently held: what shape is wanted, expressed as
		// values rather than as accumulated calls. Sort axes appear in the same order ApplySort
		// applies them, so a spec and this pipeline always describe the same projection.
		//
		// Every description carries a minted PropertyName rather than a real property path. The
		// fluent surface takes delegates, not paths, so there is nothing else to name an axis by;
		// a minted id keeps the spec diffable (an empty name would force a full reshape for any
		// change, which is exactly the outcome the diff exists to avoid) and stays honest because
		// the id changes whenever the underlying delegate is re-declared.
		public partial ShapingSpec BuildSpec();

		// Diffs the current verbs against the spec adopted by the previous call, adopts the new
		// one, and returns the work the change requires. The first call always reports a full
		// reshape, since there is no prior shape to have moved from.
		public partial ShapingDelta CommitSpec();

		public ShapingSpec CommittedSpec() => m_committedSpec;

		// Mints a process-unique-per-pipeline description id, e.g. "s3" for the fourth axis
		// declared. Only ever compared for equality, never parsed.
		private partial string MintDescriptionId(char prefix);


		// Compares an item's precomputed axis keys against a row whose keys are evaluated on the
		// fly. Lets the binary search hoist axis filtering and item-key evaluation out of the loop.
		private static partial int CompareKeysToRow(
			List<SortAxis> axes,
			List<object?> itemKeys,
			object? row);

		// Declaration-ordered filter axes, all conjunctive.
		private List<FilterAxis> m_filters = new();
		private List<SortAxis> m_sorts = new();
		private int m_nextVerbOrder;
		private int m_groupOrder = -1;
		// Diff identity and evaluator of the group verb, held only so BuildSpec can describe it.
		private string m_groupDescriptionId = "";
		private KeySelector? m_groupKey;
		private ShapingSpec m_committedSpec = new();
		// False until the first CommitSpec. Distinguishes "committed an empty spec" (a source
		// with no verbs, where a later verb is a real change) from "never committed" (where the
		// caller has no projection yet and must do the full reshape regardless).
		private bool m_hasCommittedSpec;
		private uint m_nextDescriptionId;
	}
}
