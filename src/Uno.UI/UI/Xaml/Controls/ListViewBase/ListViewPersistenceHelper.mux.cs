// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ListViewPersistenceHelper_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d


#nullable enable

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class ListViewPersistenceHelper
{
	public static string GetRelativeScrollPosition(
		ListViewBase listViewBase,
		ListViewItemToKeyHandler itemToKeyHandler)
	{
		string returnValue;

		returnValue = listViewBase.GetRelativeScrollPosition(
			itemToKeyHandler);

		return returnValue;
	}

	public static IAsyncAction SetRelativeScrollPositionAsync(
		ListViewBase listViewBase,
		string relativeScrollPosition,
		ListViewKeyToItemHandler keyToItemHandler)
	{
		IAsyncAction returnValue;

		returnValue = listViewBase.SetRelativeScrollPositionAsync(
			relativeScrollPosition,
			keyToItemHandler);

		return returnValue;
	}
}

