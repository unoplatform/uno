// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TabularShaping\ParentKeyIndex.h, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.UI.Xaml.Controls.Tabular;

internal static partial class ShapingHelpers
{
	// Validated tree over the UNFILTERED source, in source order. Independent of filter/sort, so a
	// reshape can reuse it once no key or parent key moved. Immutable once built.
	internal sealed class ParentStructure
	{
		// TODO Uno: SIZE_MAX.
		public const int Root = int.MaxValue;

		// Source order. Keeps keyed rows alive so no KeyByItem address can be recycled.
		public List<object?> Rows = new();
		// Object (identity) keys, held for the structure's lifetime. Node keys persist as expansion
		// intent and selection anchors until the next structure prunes them; holding the objects
		// means no new key can take a still-referenced address in the meantime.
		public List<object?> KeyObjects = new();
		// ABI pointer -> nodeKey, for every row.
		// TODO Uno: the ABI pointer becomes reference identity; a null row is keyed by s_nullRow.
		public Dictionary<object, string> KeyByItem = new(ReferenceEqualityComparer.Instance);
		// nodeKey -> position in Rows.
		public Dictionary<string, int> IndexByKey = new(StringComparer.Ordinal);
		// Per row: key (into KeyByItem), parent position or Root, children in source order.
		public List<string> NodeKeys = new();
		public List<int> ParentIndex = new();
		public List<List<int>> ChildIndices = new();
		public List<int> RootIndices = new();
	}

	// Parent-key index: one filtered, sorted reading of a ParentStructure. Immutable once built.
	internal sealed class ParentKeyIndex
	{
		// Items are in sorted order within every sibling list.
		public List<object?> Roots = new();
		// parent nodeKey -> children.
		public Dictionary<string, List<object?>> Children = new(StringComparer.Ordinal);
		public HashSet<string> ContextKeys = new(StringComparer.Ordinal); // kept only because a descendant matched the filter
																		  // Nodes the filter keeps: the matches and the ancestors that give them context.
		public int KeptNodeCount;
		// Owns the keys all lookups resolve against; its key set is complete and unfiltered.
		public ParentStructure? Structure;

		public List<object?>? TryGetChildren(string nodeKey) => Children.TryGetValue(nodeKey, out var children) ? children : null;

		public string? TryGetKey(object? item)
		{
			if (Structure is null)
			{
				return null;
			}
			return Structure.KeyByItem.TryGetValue(item ?? s_nullRow, out var key) ? key : null;
		}
	}

	// Uno specific: stands in for a null row, which has no ABI-pointer-like identity of its own.
	private static readonly object s_nullRow = new();

	// std::function<bool(IInspectable const&)>
	internal delegate bool ParentKeyFilter(object? item);
	// Sorts one sibling list in place, stably. Called only for lists of two or more rows.
	internal delegate void SiblingSorter(List<object?> items);

	// Runs key/parent selectors once per row. False with `error` on duplicate/null key, self-parent
	// or cycle.
	// TODO Uno: the C++ out parameter is assigned only on success; C# returns it as null on failure.
	internal static partial bool BuildParentStructure(
		List<object?> rows,
		KeySelector keySelector,
		KeySelector parentKeySelector,
		[NotNullWhen(true)] out ParentStructure? @out,
		out string error);

	// True when re-running the selectors over `structure.Rows` yields the same keys and parents.
	// Object keys always fail: a freed key's address can be reused, so they cannot be proven unchanged.
	internal static partial bool ParentStructureStillMatches(
		ParentStructure structure,
		KeySelector keySelector,
		KeySelector parentKeySelector);

	// Runs no selector. Empty `filter`/`sort` = none. Filter keeps matches plus ancestors; sort orders
	// each sibling list (roots only if `sortRoots`), ties in source order.
	internal static partial void BuildParentKeyIndex(
		ParentStructure structure,
		ParentKeyFilter? filter,
		SiblingSorter? sort,
		bool sortRoots,
		out ParentKeyIndex @out);

	// Exposed for the adapter: "node:" + lookup key, or empty when `key` means "no key".
	internal static partial string MakeNodeKey(object? key);

	// True when MakeNodeKey keyed by object address (address may be recycled, so unprovable).
	internal static partial bool IsObjectNodeKey(string nodeKey);
}
