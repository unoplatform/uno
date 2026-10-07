// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TabularShaping\ShapingDescriptions.cpp, tag winui3/main, commit dc28206ea35

#nullable enable

using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.UI.Xaml.Controls.Tabular;

internal static partial class ShapingHelpers
{
	// namespace {

	private static ShapingWork MaxWork(ShapingWork a, ShapingWork b) => (uint)a >= (uint)b ? a : b;

	// Two axes are "the same axis" only when both carry a property path and the paths match.
	// A missing path means the axis is a bare delegate, which cannot be compared, so the
	// caller must already have taken the not-diffable path before reaching here.
	private static bool SameSorts(List<SortDescription> a, List<SortDescription> b)
	{
		if (a.Count != b.Count)
		{
			return false;
		}
		for (int i = 0; i < a.Count; ++i)
		{
			if (a[i].PropertyName != b[i].PropertyName || a[i].Direction != b[i].Direction)
			{
				return false;
			}
		}
		return true;
	}

	private static bool SameGroups(List<GroupDescription> a, List<GroupDescription> b)
	{
		if (a.Count != b.Count)
		{
			return false;
		}
		for (int i = 0; i < a.Count; ++i)
		{
			if (a[i].PropertyName != b[i].PropertyName)
			{
				return false;
			}
		}
		return true;
	}

	private static bool SameFilters(List<FilterDescription> a, List<FilterDescription> b)
	{
		if (a.Count != b.Count)
		{
			return false;
		}
		for (int i = 0; i < a.Count; ++i)
		{
			// CriterionId, not just PropertyName: two filters on the same column with
			// different thresholds are different filters, and only the id can say so.
			if (a[i].PropertyName != b[i].PropertyName || a[i].CriterionId != b[i].CriterionId)
			{
				return false;
			}
		}
		return true;
	}

	// Composite group key for a multi-axis grouping, so N group axes collapse to one bucket
	// level. Each axis contributes its ToString form; the separator is a character that
	// cannot appear in a well-formed key prefix, and each segment is length-prefixed so
	// "a" + "b|c" and "a|b" + "c" cannot alias.
	//
	// Also yields the first axis's key, which is what a single-axis group header displays.
	// Returning it from here rather than re-evaluating guarantees the retained key is the one
	// that actually produced the identity — a re-evaluation could disagree under live shaping —
	// and halves the app-evaluator cost on the reshape hot path.
	private static string CompositeGroupIdentity(
		List<GroupDescription> groups,
		object? item,
		out object? firstAxisKey)
	{
		firstAxisKey = null;

		StringBuilder composite = new();
		bool isFirstAxis = true;
		foreach (var group in groups)
		{
			object? key = null;
			if (group.Evaluator is not null)
			{
				try
				{
					key = group.Evaluator(item);
				}
				catch
				{
					key = null;
				}
			}

			if (isFirstAxis)
			{
				firstAxisKey = key;
				isFirstAxis = false;
			}

			string segment;
			try
			{
				segment = ValueKey.ToString(key);
			}
			catch
			{
				// ToString swallows a throwing IStringable internally, but a QI against a
				// disconnected proxy can still fail. Keep this layer's identity policy total:
				// a sentinel segment groups such items together rather than aborting the
				// reshape mid-bucketize and leaving the state half-built.
				segment = "<unavailable>";
			}

			composite.Append(segment.Length.ToString(CultureInfo.InvariantCulture));
			composite.Append(':');
			composite.Append(segment);
			composite.Append('\x1f');
		}
		return composite.ToString();
	}

	private static void SortWithinRange(List<object?> items, List<SortDescription> sorts)
	{
		if (sorts.Count == 0 || items.Count < 2)
		{
			return;
		}

		StableSortByKeys(
			items,
			sorts.Count,
			(item, axisIndex) =>
			{
				var evaluator = sorts[axisIndex].Evaluator;
				if (evaluator is null)
				{
					return null;
				}
				try
				{
					return evaluator(item);
				}
				catch
				{
					// A throwing key selector sorts as the null class rather than aborting the
					// whole reshape, matching ApplyPredicateFilter's fail-safe posture.
					return null;
				}
			},
			axisIndex => sorts[axisIndex].Direction);
	}

	private static void FlattenBucketsInto(List<KeyedBucket> buckets, List<object?> items)
	{
		int total = 0;
		foreach (var bucket in buckets)
		{
			total += bucket.Items.Count;
		}

		items.Clear();
		if (items.Capacity < total)
		{
			items.Capacity = total;
		}
		foreach (var bucket in buckets)
		{
			items.AddRange(bucket.Items);
		}
	}

	private static void ApplyFilters(List<object?> items, List<FilterDescription> filters)
	{
		foreach (var filter in filters)
		{
			if (filter.Predicate is null)
			{
				continue;
			}
			ApplyPredicateFilter(items, filter.Predicate);
		}
	}

	private static void Bucketize(ShapingState state, ShapingSpec spec)
	{
		state.Buckets.Clear();
		state.IsGrouped = false;

		if (spec.Groups.Count == 0)
		{
			return;
		}

		// Layer 1's default identity policy: the composite ToString of the group keys. It is
		// total — every failure mode inside CompositeGroupIdentity degrades to a sentinel
		// segment rather than throwing — so bucketization here never falls back to flat.
		// Consumers wanting a fail-fast or app-supplied identity call BucketizeToGroups
		// directly. That helper resolves identity from the KEY, which cannot express a
		// composite over several axes, so multi-axis bucketization is done here against the
		// ITEM.
		//
		// KeyedBucket::Key carries only the FIRST axis's key; for a multi-axis spec the
		// authoritative discriminator is Identity, not Key.
		List<KeyedBucket> buckets = new();
		Dictionary<string, int> indexByIdentity = new(global::System.StringComparer.Ordinal);
		foreach (var item in state.Items)
		{
			var identity = CompositeGroupIdentity(spec.Groups, item, out var representativeKey);

			if (!indexByIdentity.TryGetValue(identity, out var bucketIndex))
			{
				indexByIdentity.Add(identity, buckets.Count);
				buckets.Add(new KeyedBucket { Key = representativeKey, Identity = identity, Items = new() { item } });
			}
			else
			{
				buckets[bucketIndex].Items.Add(item);
			}
		}

		state.Buckets = buckets;
		state.IsGrouped = true;
	}

	// } // namespace

	sealed partial class ShapingSpec
	{
		public partial bool IsDiffable()
		{
			foreach (var filter in Filters)
			{
				if (!filter.IsDiffable())
				{
					return false;
				}
			}
			foreach (var group in Groups)
			{
				if (!group.IsDiffable())
				{
					return false;
				}
			}
			foreach (var sort in Sorts)
			{
				if (!sort.IsDiffable())
				{
					return false;
				}
			}
			return true;
		}

		public partial ShapingDelta Diff(ShapingSpec next)
		{
			ShapingDelta delta = default;

			// A bare delegate destroys the metadata a diff needs. Rather than guess that two
			// std::functions are the same, say so and pay for a full reshape.
			if (!IsDiffable() || !next.IsDiffable())
			{
				delta.FilterChanged = true;
				delta.GroupingChanged = true;
				delta.SortChanged = true;
				delta.RequiredWork = ShapingWork.FullReshape;
				return delta;
			}

			delta.FilterChanged = !SameFilters(Filters, next.Filters);
			delta.GroupingChanged = !SameGroups(Groups, next.Groups);
			delta.SortChanged = !SameSorts(Sorts, next.Sorts);

			if (delta.FilterChanged)
			{
				delta.RequiredWork = ShapingWork.FullReshape;
			}
			if (delta.GroupingChanged)
			{
				delta.RequiredWork = MaxWork(delta.RequiredWork, ShapingWork.ReBucket);
			}
			if (delta.SortChanged)
			{
				delta.RequiredWork = MaxWork(delta.RequiredWork, ShapingWork.ReSortWithinBuckets);
			}
			return delta;
		}
	}

	internal static partial void Reshape(ShapingState state, ShapingSpec spec, ShapingDelta delta)
	{
		// The incremental paths read FilteredSource and Buckets as authoritative. Against a state
		// that has never been fully shaped they would produce an empty, apparently-valid
		// projection with no error signal, so establish membership first instead.
		var work = state.HasProjection ? delta.RequiredWork : ShapingWork.FullReshape;

		switch (work)
		{
			case ShapingWork.None:
				return;

			case ShapingWork.FullReshape:
				state.FilteredSource = new List<object?>(state.Source);
				ApplyFilters(state.FilteredSource, spec.Filters);
				state.Items = new List<object?>(state.FilteredSource);
				Bucketize(state, spec);
				break;

			case ShapingWork.ReBucket:
				// Membership is unchanged, but the order must come from FilteredSource, not from the
				// flattened Items: those are still clustered by the PREVIOUS grouping, which would
				// make both the new bucket order and the stable sort's tie order depend on it.
				state.Items = new List<object?>(state.FilteredSource);
				Bucketize(state, spec);
				break;

			case ShapingWork.ReSortWithinBuckets:
				if (!state.IsGrouped)
				{
					// Ungrouped, so there are no buckets to sort within and Items carries whatever
					// order the last reshape left. Re-seat it on source order so the stable sort
					// breaks ties consistently with a full reshape of the same spec.
					state.Items = new List<object?>(state.FilteredSource);
				}
				break;
		}

		if (state.IsGrouped)
		{
			foreach (var bucket in state.Buckets)
			{
				SortWithinRange(bucket.Items, spec.Sorts);
			}
			FlattenBucketsInto(state.Buckets, state.Items);
		}
		else
		{
			SortWithinRange(state.Items, spec.Sorts);
		}

		state.HasProjection = true;
	}
}
