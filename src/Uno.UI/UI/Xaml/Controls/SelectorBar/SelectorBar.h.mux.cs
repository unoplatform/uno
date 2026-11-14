// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\SelectorBar\SelectorBar.h

#nullable enable

using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls;

partial class SelectorBar
{
	private ItemsView? _itemsView;

	// private readonly SerialDisposable _loadedRevoker = new();

	private readonly SerialDisposable _itemsViewSelectedItemPropertyChangedRevoker = new();

	private const string s_itemsViewPartName = "PART_ItemsView";
}
