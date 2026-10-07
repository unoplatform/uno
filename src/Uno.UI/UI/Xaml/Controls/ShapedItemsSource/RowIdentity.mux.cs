// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\ShapedItemsSource\RowIdentity.cpp, tag winui3/main, commit dc28206ea35

#nullable enable

using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.Foundation.Collections;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class RowIdentity
{
	private static void LogUntrackableRow(uint index, string? reason)
	{
#if DEBUG
		TVDiag.DbgLogF(
			"[RowIdentity] Row %u could not be given an identity (%ls) after validation had " +
			"already passed; it is absent from the identity map.\n",
			index,
			reason ?? "unspecified reason");
#endif
	}

	private static bool TryCanonicalizeStableGroupKey(object? key, out string identity)
	{
		identity = "";
		if (key is null)
		{
			return false;
		}

		return ShapingHelpers.ValueKey.TryGetStablePropertyKey(key, out identity, true);
	}

	internal static partial ShapingHelpers.KeySelector MakeObjectIdentitySelector()
	{
		return (object? item) =>
		{
			if (item is null)
			{
				return null;
			}

			// COM identity: the IUnknown obtained by QI is the canonical per-object pointer, so
			// two references to the same object always stringify identically and two distinct
			// objects never collide -- including two boxed copies of the same value, which are
			// separate objects and therefore separate rows.
			// TODO Uno: there is no IUnknown address in .NET; ObjectIdentityHelper hands out a stable id per live object.
			// A string or value-type row is an IPropertyValue box, and under C#/WinRT every read through the ABI
			// mints a new box, so such a row never keeps its identity across reads and equal values never collide.
			// The fresh object below stands in for that new box; keying the .NET reference instead would merge
			// interned strings and reused boxes into one identity.
			// Original C++:
			// auto const unknown = item.as<winrt::Windows::Foundation::IUnknown>();
			// auto const address = reinterpret_cast<uintptr_t>(winrt::get_abi(unknown));
			var address = ValueConversionHelpers.TryGetPropertyType(item, out _)
				? ObjectIdentityHelper.GetId(new object())
				: ObjectIdentityHelper.GetId(item);

			// swprintf_s(buffer, L"0x%zx", static_cast<size_t>(address));
			return "0x" + address.ToString("x", CultureInfo.InvariantCulture);
		};
	}

	internal static partial bool TryGetRequiredRowIdentity(
		object? item,
		ShapingHelpers.KeySelector? keySelector,
		out string identity,
		ref string? reason)
	{
		identity = "";
		if (item is null)
		{
			reason = "null row item";
			return false;
		}

		if (keySelector is null)
		{
			reason = "missing row identity selector";
			return false;
		}

		object? key = null;
		try
		{
			key = keySelector(item);
		}
		catch
		{
			reason = "row identity selector threw";
			return false;
		}

		if (key is null)
		{
			reason = "null row identity";
			return false;
		}

		// TODO Uno: IPropertyValue projection
		if (ValueConversionHelpers.GetPropertyType(key.GetType()) == PropertyType.String)
		{
			identity = (string)key;
			if (!string.IsNullOrEmpty(identity))
			{
				return true;
			}

			reason = "empty row identity";
			return false;
		}

		reason = "row identity selector returned a non-string value";
		return false;
	}

	internal static partial bool ValidateRowIdentities(
		List<object?> rows,
		ShapingHelpers.KeySelector? keySelector,
		ref string? reason)
	{
		HashSet<string> identities = new(rows.Count);

		foreach (var item in rows)
		{
			if (!TryGetRequiredRowIdentity(item, keySelector, out var identity, ref reason))
			{
				return false;
			}

			if (!identities.Add(identity))
			{
				// Identity is the item's object address, so a repeat means one object is occupying
				// two rows -- not two items that merely look alike.
				reason = "the same item object appears on more than one row";
				return false;
			}
		}

		return true;
	}

	internal static partial void ClearFlatRowIdentityTracking(
		HashSet<string> identities,
		Dictionary<string, uint> identityToIndex)
	{
		identities.Clear();
		identityToIndex.Clear();
	}

	internal static partial void RebuildFlatRowIdentityTracking(
		List<object?> rows,
		bool identityRequired,
		ShapingHelpers.KeySelector? keySelector,
		HashSet<string> identities,
		Dictionary<string, uint> identityToIndex)
	{
		ClearFlatRowIdentityTracking(identities, identityToIndex);
		if (!identityRequired)
		{
			return;
		}

		identities.EnsureCapacity(rows.Count);
		identityToIndex.EnsureCapacity(rows.Count);
		for (var index = 0; index < rows.Count; ++index)
		{
			string? reason = null;
			if (TryGetRequiredRowIdentity(rows[index], keySelector, out var identity, ref reason))
			{
				identities.Add(identity);
				identityToIndex.TryAdd(identity, (uint)index);
			}
			else
			{
				// Unreachable by construction: identity is required here, and both callers have
				// already proven every row can produce one. The full-rebuild path runs
				// ValidateRowIdentities immediately before this and throws on the first failure,
				// and the sort-only in-place path re-projects rows it just proved element-identical
				// to the previously validated projection. Reaching this branch therefore means an
				// upstream invariant broke, not that the source is merely awkward.
				//
				// Skip rather than throw, because the map only accelerates lookup: a missing entry
				// costs TryGetTrackedFlatRowIndex a failed find and the caller a linear scan, which
				// is a far better outcome in fre than tearing down a projection that is otherwise
				// intact. The cost is that the map is NOT guaranteed to hold one entry per row, so
				// its size must never be used as a row count -- use the row vector's size instead.
				LogUntrackableRow((uint)index, reason);
				MUX_ASSERT(false);
			}
		}
	}

	internal static partial bool TryGetTrackedFlatRowIndex(
		string identity,
		IObservableVector<object?>? rows,
		ShapingHelpers.KeySelector? keySelector,
		Dictionary<string, uint> identityToIndex,
		ref uint index)
	{
		if (!identityToIndex.TryGetValue(identity, out var trackedIndex) || rows is null)
		{
			return false;
		}

		index = trackedIndex;
		if (index >= rows.Count)
		{
			return false;
		}

		string? reason = null;
		return TryGetRequiredRowIdentity(rows[(int)index], keySelector, out var projectedIdentity, ref reason) &&
			projectedIdentity == identity;
	}

	internal static partial void ShiftTrackedFlatRowIndicesForInsert(
		Dictionary<string, uint> identityToIndex,
		uint insertedIndex)
	{
		foreach (var key in identityToIndex.Keys)
		{
			ref var index = ref CollectionsMarshal.GetValueRefOrNullRef(identityToIndex, key);
			if (index >= insertedIndex)
			{
				++index;
			}
		}
	}

	internal static partial void ShiftTrackedFlatRowIndicesForRemove(
		Dictionary<string, uint> identityToIndex,
		uint removedIndex)
	{
		foreach (var key in identityToIndex.Keys)
		{
			ref var index = ref CollectionsMarshal.GetValueRefOrNullRef(identityToIndex, key);
			if (index > removedIndex)
			{
				--index;
			}
		}
	}

	internal static partial bool TryGetGroupIdentity(
		object? key,
		IdentitySelector? groupIdentitySelector,
		out string identity,
		ref string? reason)
	{
		identity = "";
		if (groupIdentitySelector is not null)
		{
			try
			{
				identity = groupIdentitySelector(key);
			}
			catch
			{
				reason = "group identity selector threw";
				return false;
			}

			if (!string.IsNullOrEmpty(identity))
			{
				return true;
			}

			reason = "empty group identity";
			return false;
		}

		if (TryCanonicalizeStableGroupKey(key, out identity))
		{
			return true;
		}

		reason = key is not null ? "reference group key without stable identity selector" : "null group key";
		return false;
	}

	internal static partial bool GroupKeysEqual(object? a, object? b)
	{
		if (ReferenceEquals(a, b))
		{
			return true;
		}

		if (a is null || b is null)
		{
			return false;
		}

		if (TryCanonicalizeStableGroupKey(a, out var aIdentity) &&
			TryCanonicalizeStableGroupKey(b, out var bIdentity))
		{
			return aIdentity == bIdentity;
		}

		// try_as<IUnknown> identity is reference identity in .NET, already handled by the ReferenceEquals
		// above, so this path is always false.
		// Original C++:
		// auto aUnknown = a.try_as<::IUnknown>();
		// auto bUnknown = b.try_as<::IUnknown>();
		// return aUnknown && bUnknown && aUnknown.get() == bUnknown.get();
		return false;
	}

	internal static partial string StringifyKey(object? key) => ShapingHelpers.ValueKey.ToString(key);
}
