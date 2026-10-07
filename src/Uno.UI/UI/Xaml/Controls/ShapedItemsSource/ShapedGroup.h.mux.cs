// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\ShapedItemsSource\ShapedGroup.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// A bucket produced by shaping: a group key, a stable string identity for that key, and the items
// that fell into it.
//
// It presents itself through ICollectionViewGroup — Group() is the key, GroupItems() the items —
// because that is the DECLARED contract layer 3 and the grouped adapter consume. The previous
// design published the same two facts as ICustomPropertyProvider properties named
// "__TableViewSourceGroupIdentity" and "__TableViewSourceGroupKey", which meant every consumer
// below the control had to know a TableView-specific magic string to read a group at all — the
// layering violation that made the grouped adapter un-reusable. Reading a group is now a QI, not a
// string probe, so an app-authored ICollectionViewGroup works with the same code path.
//
// ICustomPropertyProvider is still implemented, but only for GetStringRepresentation: XAML asks
// for it when a header template displays the group directly. It exposes no properties.
// TODO Uno: IIterable<IInspectable> projects as IEnumerable<object?> and IStringable as the ToString override.
// The constructor ShapedGroup(object? key, string groupKey) is in ShapedGroup.mux.cs; the members declared
// here are partial members implemented there.
internal sealed partial class ShapedGroup :
	ICollectionViewGroup,
	IEnumerable<object?>,
	ICustomPropertyProvider,
	ShapingHelpers.IGroupIdentity
{
	// ShapedGroup(object? key, string groupKey); (ShapedGroup.mux.cs)

	// ICollectionViewGroup — the contract consumers below layer 4 are allowed to know about.
	public partial object? Group { get; }
	public partial IObservableVector<object?> GroupItems { get; }

	// IIterable — a group is also directly enumerable, which is what the generic
	// "each element of the source is itself a collection" path expects.
	// TODO Uno: IIterable::First() becomes IEnumerable<T>.GetEnumerator().
	public partial IEnumerator<object?> GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	// IStringable / ICustomPropertyProvider
	public override partial string ToString();
	public partial Type Type { get; }
	public partial ICustomProperty? GetCustomProperty(string name);
	public partial ICustomProperty? GetIndexedProperty(string name, Type type);
	public partial string GetStringRepresentation();

	// The stable string identity of this group, unique across the projection. Distinct from the
	// key object: two runs of a key selector can produce equal-but-not-identical key objects, and
	// expansion state has to survive that.
	public partial string GroupKey();

	// IGroupIdentity — the layer-1 contract through which layer 3 reads the identity above
	// without depending on this type.
	public partial string StableGroupIdentity();

	public partial void GroupKey(string value);
	public partial void Key(object? value);
	public partial void SetItems(List<object?> items);

	private object? m_key;
	private string m_groupKey = "";
	// TODO Uno: typed as the concrete ObservableVector<T> (single_threaded_observable_vector) so ReplaceAll raises one Reset.
	private readonly ObservableVector<object?> m_items;
}
