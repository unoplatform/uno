// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TabularShaping\ShapingPipeline.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.UI.Xaml.Controls.Tabular;

internal static partial class ShapingHelpers
{
	internal sealed partial class ShapingPipeline
	{
		private partial string MintDescriptionId(char prefix) =>
			prefix + (m_nextDescriptionId++).ToString(CultureInfo.InvariantCulture);

		public partial void SetFilter(Predicate? predicate) => SetFilter("", predicate);

		public partial void SetFilter(string axisToken, Predicate? predicate)
		{
			if (predicate is null)
			{
				ClearFilter(axisToken);
				return;
			}

			var existing = m_filters.FindIndex(axis => axis.AxisToken == axisToken);

			// Re-minted even when replacing in place: the delegate cannot reveal that its threshold
			// moved, so a re-declaration must always read as a change.
			var criterionId = MintDescriptionId('f');

			if (existing != -1)
			{
				var axis = m_filters[existing];
				axis.Predicate = predicate;
				axis.CriterionId = criterionId;
				m_filters[existing] = axis;
				return;
			}

			m_filters.Add(new FilterAxis { AxisToken = axisToken, Predicate = predicate, CriterionId = criterionId });
		}

		public partial void ClearFilter() => m_filters.Clear();

		public partial void ClearFilter(string axisToken) => m_filters.RemoveAll(axis => axis.AxisToken == axisToken);

		public partial int MarkGroupVerb(KeySelector? key)
		{
			m_groupKey = key;
			m_groupDescriptionId = MintDescriptionId('g');
			return m_groupOrder = NextVerbOrder();
		}

		public partial void ClearGroupVerb()
		{
			m_groupOrder = -1;
			m_groupKey = null;
			m_groupDescriptionId = "";
		}

		public partial ShapingSpec BuildSpec()
		{
			ShapingSpec spec = new();

			foreach (var axis in m_filters)
			{
				// The untokenized shorthand has no column notion to name, so it keeps the synthetic
				// path it always had; a named axis is described by its own token, which is what lets
				// two filter axes diff independently. Either way CriterionId, not the path, is what
				// distinguishes two criteria on the same column.
				spec.Filters.Add(new FilterDescription
				{
					PropertyName = string.IsNullOrEmpty(axis.AxisToken) ? "(predicate)" : axis.AxisToken,
					CriterionId = axis.CriterionId,
					Predicate = axis.Predicate,
				});
			}

			if (m_groupOrder >= 0)
			{
				spec.Groups.Add(new GroupDescription { PropertyName = m_groupDescriptionId, Evaluator = m_groupKey });
			}

			// ActiveSortAxes, not m_sorts: it applies exactly the filtering (live key, live direction)
			// and yields exactly the order that ApplySort uses, so the spec cannot describe a
			// different projection than the pipeline produces.
			foreach (var axis in ActiveSortAxes(-1, -1))
			{
				// Named by the minted id, NOT by the AxisToken. A column can re-declare the same
				// token at the same direction while supplying a DIFFERENT key delegate — a custom
				// comparer whose ranks were just repopulated does exactly that — and naming the
				// description by the token would diff that as "unchanged" and silently skip the
				// re-sort the caller asked for. The id is re-minted by every declaration, so a
				// re-declaration always reads as a change: conservative in the safe direction.
				spec.Sorts.Add(new SortDescription { PropertyName = axis.DescriptionId, Direction = axis.Direction, Evaluator = axis.Key });
			}

			return spec;
		}

		public partial ShapingDelta CommitSpec()
		{
			var next = BuildSpec();

			ShapingDelta delta = default;
			if (m_hasCommittedSpec)
			{
				delta = m_committedSpec.Diff(next);
			}
			else
			{
				delta.FilterChanged = true;
				delta.GroupingChanged = true;
				delta.SortChanged = true;
				delta.RequiredWork = ShapingWork.FullReshape;
				m_hasCommittedSpec = true;
			}

			m_committedSpec = next;
			return delta;
		}

		public partial bool PassesFilter(object? item)
		{
			// Conjunctive, and short-circuiting: an item excluded by the first axis is never handed
			// to the rest, so an expensive predicate declared later costs nothing on rows already out.
			foreach (var axis in m_filters)
			{
				try
				{
					if (!axis.Predicate!(item))
					{
						return false;
					}
				}
				catch
				{
					return false;
				}
			}

			return true;
		}

		public partial void ApplyFilter(List<object?> rows)
		{
			if (m_filters.Count == 0)
			{
				return;
			}

			// One pass over the rows regardless of axis count: the axes are conjunctive, so they
			// compose into a single predicate rather than into N successive erase passes.
			ApplyPredicateFilter(
				rows,
				item =>
				{
					foreach (var axis in m_filters)
					{
						if (!axis.Predicate!(item))
						{
							return false;
						}
					}

					return true;
				});
		}

		public partial void SetSort(
			string previousAxisToken,
			string axisToken,
			KeySelector? key,
			object? keyIdentity,
			string sortMemberPath,
			SortDirection direction)
		{
			if (!string.IsNullOrEmpty(previousAxisToken) && previousAxisToken != axisToken)
			{
				ClearSort(previousAxisToken);
			}

			for (int i = 0; i < m_sorts.Count; ++i)
			{
				var it = m_sorts[i];
				// TODO Uno: IUnknown equality becomes reference identity.
				bool sameAxis = string.IsNullOrEmpty(axisToken)
					? (string.IsNullOrEmpty(it.AxisToken) && it.KeyIdentity is not null && ReferenceEquals(it.KeyIdentity, keyIdentity))
					: (it.AxisToken == axisToken);
				if (!sameAxis)
				{
					continue;
				}

				if (direction == SortDirection.None)
				{
					m_sorts.RemoveAt(i);
				}
				else
				{
					it.Key = key;
					it.KeyIdentity = keyIdentity;
					it.SortMemberPath = sortMemberPath;
					it.Direction = direction;
					// Match WPF DataGrid.DefaultSort: re-sorting an already-sorted axis replaces it
					// in place and keeps its existing precedence slot (Order is left untouched), rather
					// than moving the re-touched column. Since precedence is earliest-first, keeping
					// Order keeps a primary axis primary.
					// The evaluator was replaced wholesale, so the previous description no longer
					// describes this axis.
					it.DescriptionId = MintDescriptionId('s');
					m_sorts[i] = it;
				}
				return;
			}

			if (direction != SortDirection.None)
			{
				// A brand-new axis takes the next (largest) Order, which under earliest-first
				// precedence appends it as the least significant tie-break -- the analog of WPF's
				// SortDescriptions.Add.
				m_sorts.Add(new SortAxis
				{
					AxisToken = axisToken,
					Key = key,
					KeyIdentity = keyIdentity,
					SortMemberPath = sortMemberPath,
					Direction = direction,
					Order = NextVerbOrder(),
					DescriptionId = MintDescriptionId('s'),
				});
			}
		}

		public partial void ClearSort(string axisToken)
		{
			if (string.IsNullOrEmpty(axisToken))
			{
				ClearSorts();
				return;
			}

			m_sorts.RemoveAll(axis => axis.AxisToken == axisToken);
		}

		public partial void ClearSortsExcept(string axisToken)
		{
			if (string.IsNullOrEmpty(axisToken))
			{
				// No axis to preserve: an empty token identifies no single axis.
				ClearSorts();
				return;
			}

			m_sorts.RemoveAll(axis => axis.AxisToken != axisToken);
		}

		public partial bool HasActiveSort()
		{
			foreach (var axis in m_sorts)
			{
				if (axis.Key is not null && axis.Direction != SortDirection.None)
				{
					return true;
				}
			}
			return false;
		}

		public partial List<SortAxis> ActiveSortAxes(int afterOrder, int beforeOrder)
		{
			List<SortAxis> active = new();
			foreach (var axis in m_sorts)
			{
				bool afterMatches = afterOrder < 0 || axis.Order > afterOrder;
				bool beforeMatches = beforeOrder < 0 || axis.Order < beforeOrder;
				if (axis.Key is not null && axis.Direction != SortDirection.None && afterMatches && beforeMatches)
				{
					active.Add(axis);
				}
			}

			// Precedence is by declaration ORDER, earliest first -- matching WPF DataGrid, whose
			// SortDescriptions collection makes index 0 (the first column sorted) the primary sort and
			// each later column a tie-break. The axis with the SMALLEST Order is primary; a NEWLY
			// declared axis gets the largest Order (SetSort push_back) and is therefore appended as the
			// least significant tie-break, exactly like SortDescriptions.Add. Re-sorting an EXISTING
			// axis leaves its Order untouched (see SetSort), so its precedence slot is preserved --
			// matching WPF's in-place SortDescriptions replace. Consequence, also shared with WPF: when
			// the primary axis has unique keys, later axes never get to break a tie, so they have no
			// visible effect until the primary produces a tie.
			// TODO Uno: std::stable_sort is ShapingHelpers.StableSort.
			StableSort(
				active,
				(a, b) => a.Order < b.Order);

			return active;
		}

		public partial void ApplySort(List<object?> rows, int afterOrder, int beforeOrder)
		{
			var active = ActiveSortAxes(afterOrder, beforeOrder);
			if (active.Count == 0)
			{
				return;
			}

			StableSortByKeys(
				rows,
				active.Count,
				(item, axisIndex) =>
				{
					try
					{
						return active[axisIndex].Key!(item);
					}
					catch
					{
						return null;
					}
				},
				axisIndex => active[axisIndex].Direction);
		}

		private static partial int CompareKeysToRow(
			List<SortAxis> axes,
			List<object?> itemKeys,
			object? row)
		{
			for (int i = 0; i < axes.Count; ++i)
			{
				var axis = axes[i];
				object? rowKey = null;
				try
				{
					rowKey = axis.Key!(row);
				}
				catch
				{
					rowKey = null;
				}
				int cmp = ValueComparer.Compare(itemKeys[i], rowKey);
				if (cmp != 0)
				{
					return axis.Direction == SortDirection.Ascending ? cmp : -cmp;
				}
			}
			return 0;
		}

		public partial int CompareItemToRow(object? item, object? row)
		{
			var active = ActiveSortAxes(-1, -1);
			List<object?> itemKeys = new(active.Count);
			foreach (var axis in active)
			{
				object? key = null;
				try
				{
					key = axis.Key!(item);
				}
				catch
				{
					key = null;
				}
				itemKeys.Add(key);
			}

			return CompareKeysToRow(active, itemKeys, row);
		}

		public partial SortedInsertPlacement SortedInsertPlacementFor(
			object? item,
			uint count,
			Func<uint, object?> getRow)
		{
			// Hoist the active axes and the incoming item's keys out of the binary-search loop:
			// otherwise every step re-filters m_sorts (allocating a vector) and re-evaluates the
			// item's key selectors. The row's keys still must be evaluated per step.
			var active = ActiveSortAxes(-1, -1);
			List<object?> itemKeys = new(active.Count);
			foreach (var axis in active)
			{
				object? key = null;
				try
				{
					key = axis.Key!(item);
				}
				catch
				{
					key = null;
				}
				itemKeys.Add(key);
			}

			SortedInsertPlacement placement = default;
			placement.Index = UpperBoundInsertIndex(
				count,
				mid => CompareKeysToRow(active, itemKeys, getRow(mid)));

			// Equal-key rows are contiguous in a sorted projection and the upper bound lands just past
			// them, so the row immediately before the insertion point is the only one that has to be
			// probed to know whether the item joined a tie group.
			placement.TiedWithExistingRow =
				placement.Index > 0 &&
				CompareKeysToRow(active, itemKeys, getRow(placement.Index - 1)) == 0;

			return placement;
		}
	}
}
