// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\ShapedItemsSource\ShapedGroup.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Data;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class ShapedGroup
{
	public ShapedGroup(object? key, string groupKey)
	{
		m_key = key;
		m_groupKey = groupKey;
		m_items = new ObservableVector<object?>();
	}

	public partial object? Group => m_key;

	public partial IObservableVector<object?> GroupItems => m_items;

	public partial IEnumerator<object?> GetEnumerator() => m_items.GetEnumerator();

	public override partial string ToString()
	{
		var text = SharedHelpers.TryGetStringRepresentationFromObject(m_key);
		return string.IsNullOrEmpty(text) ? "<null>" : text;
	}

	// TODO Uno: TypeName { Kind = TypeKind::Custom, Name = L"ShapedGroup" } has no System.Type equivalent;
	// the CLR type of this class stands in for the custom type name.
	// Original C++:
	// winrt::TypeName typeName;
	// typeName.Kind = winrt::TypeKind::Custom;
	// typeName.Name = L"ShapedGroup";
	// return typeName;
	public partial Type Type => typeof(ShapedGroup);

	public partial ICustomProperty? GetCustomProperty(string name)
	{
		// Deliberately property-less. The two properties this used to expose were magic-string
		// back-channels for the group's identity and key; both are now read through
		// ICollectionViewGroup and GroupKey(). Re-adding one would re-introduce the layering
		// violation those interfaces exist to remove.
		return null;
	}

	public partial ICustomProperty? GetIndexedProperty(string name, Type type) => null;

	public partial string GetStringRepresentation() => ToString();

	public partial string GroupKey() => m_groupKey;

	public partial string StableGroupIdentity() => m_groupKey;

	public partial void GroupKey(string value) => m_groupKey = value;

	public partial void Key(object? value) => m_key = value;

	public partial void SetItems(List<object?> items) => m_items.ReplaceAll(items);
}
