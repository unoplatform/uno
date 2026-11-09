// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TabularShaping\ParentKeyIndex.cpp, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Tabular;

internal static partial class ShapingHelpers
{
	// namespace {

	private const string c_nodePrefix = "node:";
	private const int c_nodePrefixLength = 5;
	private const string c_valueKeyPrefix = "value:";
	private const string c_objectKeyPrefix = "object:";

	private static bool IsNoKey(object? key)
	{
		if (key is null) { return true; }
		// TODO Uno: IPropertyValue projection. A boxed .NET value is never PropertyType.Empty; only null stands for it.
		if (IsPropertyValue(key))
		{
			if (GetPropertyValueType(key) == PropertyType.Empty) { return true; }
			if (GetPropertyValueType(key) == PropertyType.String && ((string)key).Length == 0) { return true; }
		}
		return false;
	}

	private static object? SafeSelect(KeySelector selector, object? item)
	{
		try { return selector(item); } catch { return null; }
	}

	// Value boxes the shared formatter misses (e.g. WinRT enums as OtherType), keyed by
	// (type, value) so a fresh box per call keeps the key. Empty when not handled.
	private static string TryFormatExtraValueKey(object key)
	{
		if (!IsPropertyValue(key))
		{
			return "";
		}

		switch (GetPropertyValueType(key))
		{
			case PropertyType.OtherType:
				{
					string value;
					// TODO Uno: IPropertyValue::GetInt64() / GetUInt64() succeed on an enum box only, and the enum's
					// underlying type decides which of the two does.
					if (key.GetType().IsEnum)
					{
						var underlying = Enum.GetUnderlyingType(key.GetType());
						if (underlying == typeof(byte) || underlying == typeof(ushort) || underlying == typeof(uint) || underlying == typeof(ulong))
						{
							value = "u" + Convert.ToUInt64(key, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
						}
						else
						{
							value = Convert.ToInt64(key, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
						}
					}
					else
					{
						return "";
					}

					string typeName = "";
					// TODO Uno: winrt::get_class_name(key) is the type's full name.
					try { typeName = key.GetType().FullName ?? ""; } catch { }
					return c_valueKeyPrefix + "e:" + typeName + ":" + value;
				}
			case PropertyType.Point:
				{
					var p = (Point)key;
					return c_valueKeyPrefix + "pt:" + FloatBits(p.X) + "," + FloatBits(p.Y);
				}
			case PropertyType.Size:
				{
					var s = (Size)key;
					return c_valueKeyPrefix + "sz:" + FloatBits(s.Width) + "," + FloatBits(s.Height);
				}
			case PropertyType.Rect:
				{
					var r = (Rect)key;
					return c_valueKeyPrefix + "rc:" + FloatBits(r.X) + "," + FloatBits(r.Y) + "," + FloatBits(r.Width) + "," + FloatBits(r.Height);
				}
			default:
				return "";
		}
	}

	// Original C++: swprintf_s(buffer, L"%08X", CanonicalFloatBits(value))
	private static string FloatBits(double value) =>
		CanonicalFloatBits((float)value).ToString("X8", CultureInfo.InvariantCulture);

	private static bool SafeFilter(ParentKeyFilter filter, object? item)
	{
		try { return filter(item); } catch { return false; }
	}

	// User-facing node key for error text: raw value, or a placeholder for an object key.
	private static string DescribeNodeKey(string nodeKey)
	{
		string view = nodeKey;
		if (view.StartsWith(c_nodePrefix, StringComparison.Ordinal))
		{
			view = view.Substring(c_nodePrefixLength);
		}

		if (view.StartsWith(c_objectKeyPrefix, StringComparison.Ordinal))
		{
			return "(object key)";
		}

		if (view.StartsWith(c_valueKeyPrefix, StringComparison.Ordinal))
		{
			view = view.Substring(c_valueKeyPrefix.Length);
			bool isEnum = view.StartsWith("e:", StringComparison.Ordinal);
			int tagEnd = view.IndexOf(':');
			if (tagEnd >= 0)
			{
				view = view.Substring(tagEnd + 1);
			}

			// An enum key is "<type>:<value>"; the value alone is what the app would recognise.
			if (isEnum)
			{
				int valueStart = view.LastIndexOf(':');
				if (valueStart >= 0)
				{
					view = view.Substring(valueStart + 1);
				}
			}
		}

		return view;
	}

	// } // namespace

	internal static partial string MakeNodeKey(object? key)
	{
		if (IsNoKey(key)) { return ""; }
		var extra = TryFormatExtraValueKey(key!);
		if (extra.Length != 0)
		{
			return c_nodePrefix + extra;
		}
		return c_nodePrefix + ValueKey.ToObjectLookupKey(key, false);
	}

	internal static partial bool IsObjectNodeKey(string nodeKey)
	{
		const string c_objectNodeKeyPrefix = "node:object:";
		return nodeKey.StartsWith(c_objectNodeKeyPrefix, StringComparison.Ordinal);
	}

	internal static partial bool BuildParentStructure(
		List<object?> rows,
		KeySelector keySelector,
		KeySelector parentKeySelector,
		out ParentStructure? @out,
		out string error)
	{
		const int c_root = ParentStructure.Root;
		int n = rows.Count;
		@out = null;
		error = "";

		ParentStructure result = new();
		result.Rows = new(rows);
		result.KeyByItem.EnsureCapacity(n);
		result.IndexByKey.EnsureCapacity(n);
		result.NodeKeys = new(n);
		result.ParentIndex = new(n);
		result.ChildIndices = new(n);
		for (int i = 0; i < n; ++i)
		{
			result.NodeKeys.Add("");
			result.ParentIndex.Add(c_root);
			result.ChildIndices.Add(new());
		}

		var indexByKey = result.IndexByKey;

		// Hold every key for the whole build: an object key is its address, which a released
		// temporary could hand to the next one.
		List<object?> keepAlive = new(n * 2);

		// Pass 1: keys, duplicates, null keys.
		for (int i = 0; i < n; ++i)
		{
			var key = SafeSelect(keySelector, rows[i]);
			var nodeKey = MakeNodeKey(key);
			if (IsObjectNodeKey(nodeKey))
			{
				// Outlives the build: the address is also expansion intent and selection identity.
				result.KeyObjects.Add(key);
			}
			keepAlive.Add(key);
			if (nodeKey.Length == 0)
			{
				// Name the item's type rather than a position: callers may pass rows in any order.
				string typeName = "";
				if (rows[i] is not null)
				{
					try { typeName = rows[i]!.GetType().FullName ?? ""; } catch { }
				}
				error = "ParentBy: key selector returned null or empty for an item"
					+ (typeName.Length == 0 ? "" : " of type '" + typeName + "'")
					+ ".";
				return false;
			}
			// The same object on two rows yields the same key, so it is reported as a duplicate below.
			bool inserted = result.KeyByItem.TryAdd(rows[i] ?? s_nullRow, nodeKey);
			string stored = nodeKey;
			if (!inserted || !indexByKey.TryAdd(stored, i))
			{
				error = "ParentBy: duplicate key '" + DescribeNodeKey(stored) + "'.";
				return false;
			}
			result.NodeKeys[i] = stored;
		}

		// Pass 2: resolve parents; self-parent is an error, unknown parent = orphan root.
		for (int i = 0; i < n; ++i)
		{
			var parentKeyValue = SafeSelect(parentKeySelector, rows[i]);
			var parentKey = MakeNodeKey(parentKeyValue);
			keepAlive.Add(parentKeyValue);
			if (parentKey.Length == 0) { continue; }
			if (parentKey == result.NodeKeys[i])
			{
				error = "ParentBy: item with key '" + DescribeNodeKey(result.NodeKeys[i]) + "' is its own parent.";
				return false;
			}
			if (indexByKey.TryGetValue(parentKey, out var parentPosition))
			{
				result.ParentIndex[i] = parentPosition;
			}
		}

		// Child lists in source order.
		for (int i = 0; i < n; ++i)
		{
			(result.ParentIndex[i] == c_root ? result.RootIndices : result.ChildIndices[result.ParentIndex[i]]).Add(i);
		}

		// Cycle check: everything must be reachable from a root.
		int reached = 0;
		{
			List<int> stack = new(result.RootIndices);
			stack.Reverse();
			while (stack.Count != 0)
			{
				int i = stack[^1]; stack.RemoveAt(stack.Count - 1);
				++reached;
				var children = result.ChildIndices[i];
				for (int c = children.Count - 1; c >= 0; --c)
				{
					stack.Add(children[c]);
				}
			}
		}
		if (reached != n)
		{
			var seen = new bool[n];
			List<int> stack = new(result.RootIndices);
			while (stack.Count != 0) { int i = stack[^1]; stack.RemoveAt(stack.Count - 1); seen[i] = true; stack.AddRange(result.ChildIndices[i]); }
			for (int i = 0; i < n; ++i)
			{
				if (!seen[i])
				{
					// Unreached nodes hang below a cycle; the first revisited node is on the cycle.
					var onPath = new bool[n];
					int p = i;
					while (!onPath[p])
					{
						onPath[p] = true;
						p = result.ParentIndex[p];
					}
					error = "ParentBy: cycle detected involving key '" + DescribeNodeKey(result.NodeKeys[p]) + "'.";
					return false;
				}
			}
		}

		GC.KeepAlive(keepAlive);
		@out = result;
		return true;
	}

	internal static partial bool ParentStructureStillMatches(
		ParentStructure structure,
		KeySelector keySelector,
		KeySelector parentKeySelector)
	{
		const int c_root = ParentStructure.Root;
		var s = structure;

		for (int i = 0; i < s.Rows.Count; ++i)
		{
			var nodeKey = MakeNodeKey(SafeSelect(keySelector, s.Rows[i]));
			if (nodeKey.Length == 0 || IsObjectNodeKey(nodeKey) || nodeKey != s.NodeKeys[i])
			{
				return false;
			}

			// All keys so far match (any later mismatch fails anyway), so resolving against the
			// retained key table answers as a rebuild would.
			var parentKey = MakeNodeKey(SafeSelect(parentKeySelector, s.Rows[i]));
			int parent = c_root;
			if (parentKey.Length != 0)
			{
				if (IsObjectNodeKey(parentKey))
				{
					return false;
				}
				if (s.IndexByKey.TryGetValue(parentKey, out var position))
				{
					parent = position;
				}
			}

			if (parent != s.ParentIndex[i])
			{
				return false;
			}
		}
		return true;
	}

	internal static partial void BuildParentKeyIndex(
		ParentStructure structure,
		ParentKeyFilter? filter,
		SiblingSorter? sort,
		bool sortRoots,
		out ParentKeyIndex @out)
	{
		const int c_root = ParentStructure.Root;
		var s = structure;
		int n = s.Rows.Count;

		// Filter: 0 = out, 1 = match, 2 = context (ancestor of a match).
		var state = new byte[n];
		Array.Fill(state, (byte)1);
		if (filter is not null)
		{
			for (int i = 0; i < n; ++i) { state[i] = SafeFilter(filter, s.Rows[i]) ? (byte)1 : (byte)0; }
			for (int i = 0; i < n; ++i)
			{
				if (state[i] != 1) { continue; }
				for (int p = s.ParentIndex[i]; p != c_root && state[p] == 0; p = s.ParentIndex[p]) { state[p] = 2; }
			}
		}

		// Per-sibling-list stable sort over source order breaks ties like one global sort.
		ParentKeyIndex result = new();
		foreach (int i in s.RootIndices) { if (state[i] != 0) { result.Roots.Add(s.Rows[i]); } }
		if (sort is not null && sortRoots && result.Roots.Count > 1) { sort(result.Roots); }

		for (int i = 0; i < n; ++i)
		{
			if (state[i] == 0) { continue; }
			++result.KeptNodeCount;
			if (state[i] == 2) { result.ContextKeys.Add(s.NodeKeys[i]); }
			List<object?> kids = new();
			foreach (int c in s.ChildIndices[i]) { if (state[c] != 0) { kids.Add(s.Rows[c]); } }
			if (kids.Count == 0) { continue; }
			if (sort is not null && kids.Count > 1) { sort(kids); }
			result.Children.TryAdd(s.NodeKeys[i], kids);
		}
		result.Structure = structure;
		@out = result;
	}
}
