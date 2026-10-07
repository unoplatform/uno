// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\GroupedSourceAdapter\GroupContract.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using Microsoft.UI.Xaml.Data;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// How a grouped structure is READ, expressed once.
//
// A group used to have to answer ICustomPropertyProvider probes for properties named
// "__TableViewSourceGroupIdentity" and "__TableViewSourceGroupKey" before anything could read its
// key — a control-specific magic string baked into every consumer, which is precisely what made
// the grouped stack unusable outside that one control. The contract is now
// ICollectionViewGroup: declared, implementable by an app, and checkable by the compiler.
//
// Layer 3 owns this because layer 3 is the layer that CONSUMES the contract. Layer 2 implements it
// without depending on this header, which keeps the two siblings rather than a stack.
internal static partial class ShapingHelpers
{
	// The group's key. A group that declares no key is its own key, which keeps a plain
	// collection-of-collections source working unchanged.
	internal static object? GetGroupKeyObject(object? group)
	{
		if (group is ICollectionViewGroup collectionViewGroup)
		{
			if (collectionViewGroup.Group is { } key)
			{
				return key;
			}
		}

		return group;
	}

	// The collection holding the group's items.
	internal static object? GetGroupItemsObject(object? group)
	{
		if (group is ICollectionViewGroup collectionViewGroup)
		{
			if (collectionViewGroup.GroupItems is { } items)
			{
				return items;
			}
		}

		return group;
	}

	// A stable string form of the group's key, or empty when the key has no value representation.
	//
	// Empty is meaningful: it says "this key cannot be compared by value, so compare the group
	// objects instead". Callers must not treat it as a valid key, or two unrelated groups with
	// non-value keys would collapse onto one another.
	internal static string GetGroupKeyIdentity(object? group)
	{
		// A group that minted its own identity wins. Re-deriving one from the key object would
		// discard the app's group identity selector, and would produce nothing at all for a
		// reference-typed key.
		if (group is not null)
		{
			if (group is IGroupIdentity identityProvider)
			{
				if (identityProvider.StableGroupIdentity() is { Length: > 0 } identity)
				{
					return identity;
				}
			}
		}

		var key = ValueKey.ToObjectLookupKey(GetGroupKeyObject(group), true);

		const string valuePrefix = "value:";
		var keyView = key.AsSpan();
		if (keyView.Length > valuePrefix.Length && keyView.Slice(0, valuePrefix.Length).SequenceEqual(valuePrefix.AsSpan()))
		{
			return keyView.Slice(valuePrefix.Length).ToString();
		}

		return "";
	}
}
