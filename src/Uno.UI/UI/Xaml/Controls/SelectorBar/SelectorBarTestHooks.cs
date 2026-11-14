// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\SelectorBar\SelectorBarTestHooks.cpp

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
