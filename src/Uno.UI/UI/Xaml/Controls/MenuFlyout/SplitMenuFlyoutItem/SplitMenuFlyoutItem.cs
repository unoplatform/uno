// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\winrtgeneratedclasses\SplitMenuFlyoutItem.g.h, commit 5f9e85113

using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// A menu flyout item that provides both a primary action and additional options through
/// a split button interface.
/// </summary>
[ContentProperty(Name = nameof(Items))]
public partial class SplitMenuFlyoutItem : MenuFlyoutItem, ISubMenuOwner
{
	/// <summary>
	/// Initializes a new instance of the SplitMenuFlyoutItem class.
	/// </summary>
	public SplitMenuFlyoutItem()
	{
		DefaultStyleKey = typeof(SplitMenuFlyoutItem);

		PrepareState();
	}
}
