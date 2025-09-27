// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\SelectorBar\SelectorBarTestHooks.cpp, tag winui3/release/1.5.2, commit b91b3ce6f25c587a9e18c4e122f348f51331f18b

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

internal class SelectorBarTestHooks
{
	internal static ItemsView? GetItemsViewPart(SelectorBar selectorBar)
	{
		if (selectorBar is not null)
		{
			return selectorBar.GetItemsViewPart();
		}

		return null;
	}
}
