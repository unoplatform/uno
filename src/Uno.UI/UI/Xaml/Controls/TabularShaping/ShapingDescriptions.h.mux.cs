// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TabularShaping\ShapingDescriptions.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Layer 1 of the Tabular shaping stack: the declarative description of *what* shape is wanted,
// and the pure reshape that produces it. No XAML, no dispatcher, no collection types, no
// tabular vocabulary — so every type here is directly headless-testable and reusable by any
// consumer of the shaping substrate.
//
// Shaping state is declarative WITH a compiled delegate alongside, not one or the other. The
// PropertyName is what makes the design work: live shaping needs it to filter
// INotifyPropertyChanged callbacks, column glyphs and persisted layout need it to read state
// back, and the engine needs it to diff two specs. The Evaluator keeps the per-comparison cost
// at delegate speed with no reflection on the hot path.
//
// Where an app supplies a computed key that no property path can express, PropertyName is empty
// and the description is flagged not-diffable, which forces a full reshape. That is the honest,
// contained version of the delegate-only behaviour rather than the default path.
internal static partial class ShapingHelpers
{
	// Extracts the value a shaping axis operates on. Returning nullptr is legal and sorts/groups
	// as the null class; throwing is the caller's concern, not this layer's.
	internal delegate object? KeySelector(object? item);

	// Retains an item when it returns true. A predicate that throws is treated as "exclude",
	// matching the fail-safe semantics of ApplyPredicateFilter.
	internal delegate bool Predicate(object? item);

	internal struct SortDescription
	{
		public SortDescription()
		{
		}

		// Empty means the evaluator is not expressible as a property path. Such a description is
		// not diffable and forces a full reshape.
		public string PropertyName = "";
		public SortDirection Direction = SortDirection.Ascending;
		public KeySelector? Evaluator;

		public readonly bool IsDiffable() => !string.IsNullOrEmpty(PropertyName);
	}

	internal struct GroupDescription
	{
		public GroupDescription()
		{
		}

		public string PropertyName = "";
		public KeySelector? Evaluator;

		public readonly bool IsDiffable() => !string.IsNullOrEmpty(PropertyName);
	}

	internal struct FilterDescription
	{
		public FilterDescription()
		{
		}

		public string PropertyName = "";
		// Identifies the *criterion*, not just the column. A filter's parameters (operator,
		// threshold, selected values) live inside the Predicate and are invisible to a diff, so
		// without this the overwhelmingly common mutation — same column, different criterion —
		// would diff as "no change" and silently keep the old membership. Callers that rebuild a
		// predicate must mint a new CriterionId; leaving it empty is legal and means "not
		// diffable", which forces a full reshape.
		public string CriterionId = "";
		public Predicate? Predicate;

		public readonly bool IsDiffable() => !string.IsNullOrEmpty(PropertyName) && !string.IsNullOrEmpty(CriterionId);
	}

	// The minimum work a spec change requires. Ordered by cost so a caller can take the maximum
	// of several deltas.
	internal enum ShapingWork : uint
	{
		None = 0,
		// Membership and bucketing are unchanged; only the order within each bucket moved.
		ReSortWithinBuckets = 1,
		// Membership is unchanged; items must be re-bucketed, then re-sorted.
		ReBucket = 2,
		// Membership itself changed: filter, then bucket, then sort.
		FullReshape = 3,
	}

	internal struct ShapingDelta
	{
		public bool FilterChanged;
		public bool GroupingChanged;
		public bool SortChanged;
		public ShapingWork RequiredWork;

		public readonly bool IsNoOp() => RequiredWork == ShapingWork.None;
	}

	// An immutable description of the wanted shape. Filters apply first, then grouping, then
	// sorting within each group — the order the reshape below implements.
	// TODO Uno: a class rather than a struct; the C++ value is only ever moved or read through a
	// const reference, so sharing the instance is equivalent and avoids aliasing the lists on copy.
	internal sealed partial class ShapingSpec
	{
		public List<FilterDescription> Filters = new();
		public List<GroupDescription> Groups = new();
		public List<SortDescription> Sorts = new();

		// False when any description carries a delegate with no property path. Diffing such a
		// spec cannot distinguish "same delegate" from "different delegate", so the engine must
		// assume everything changed.
		public partial bool IsDiffable();

		// Returns the work required to move from *this to `next`. Conservative by construction:
		// when either side is not diffable the result is a full reshape with all flags set.
		public partial ShapingDelta Diff(ShapingSpec next);
	}

	// The mutable projection state a reshape operates on. Kept separate from the spec so the
	// spec stays an immutable value and the state can be spliced incrementally.
	// TODO Uno: a class; it is only ever passed by reference (ShapingState&). Assigning one list
	// field from another (state.Items = state.FilteredSource) is a copy in C++ and must copy here too.
	internal sealed class ShapingState
	{
		// The unshaped input, in source order. Reshape never mutates this.
		public List<object?> Source = new();
		// Source after the filter step, in source order. Retained so the cheaper reshape paths can
		// re-derive membership order without re-running the predicates; re-bucketing from the
		// flattened Items would otherwise make the result depend on the PREVIOUS grouping, and
		// make a stable sort break ties in stale-group order.
		public List<object?> FilteredSource = new();
		// The shaped output, flattened in group order when grouped.
		public List<object?> Items = new();
		// Populated only when the spec groups. Bucket items are the same instances as in Items.
		public List<KeyedBucket> Buckets = new();
		// False whenever the spec carries no group axes. This layer's identity policy is total, so
		// it never degrades a requested grouping to flat — false means "not requested", not
		// "attempted and failed". Consumers needing fail-fast identity use BucketizeToGroups.
		public bool IsGrouped;
		// False until a full reshape has established membership. The incremental paths read
		// FilteredSource and Buckets as authoritative, so applying one to a never-shaped state
		// would produce a silently empty projection; Reshape promotes to a full reshape instead.
		public bool HasProjection;
	}

	// Pure reshape. Applies exactly the work `delta` calls for:
	//
	//   FullReshape         filter -> bucket -> sort within buckets
	//   ReBucket            re-bucket from FilteredSource -> sort, membership untouched
	//   ReSortWithinBuckets sort only; membership and bucketing untouched
	//   None                no-op
	//
	// A state with no prior projection (HasProjection false) is promoted to FullReshape whatever
	// the delta says, since the incremental paths have nothing to build on.
	//
	// Group identity is the layer-1 default: the length-framed composite of ValueKey::ToString
	// over every group axis, which is value-based for property values and IStringable references and
	// instance-based otherwise. It is total and never degrades to flat. Consumers needing their own
	// identity or collision policy bucketize with BucketizeToGroups directly instead.
	internal static partial void Reshape(ShapingState state, ShapingSpec spec, ShapingDelta delta);

	// Convenience for the full-rebuild path: discards any prior projection and reshapes from
	// Source.
	internal static partial void Reshape(ShapingState state, ShapingSpec spec);
}
