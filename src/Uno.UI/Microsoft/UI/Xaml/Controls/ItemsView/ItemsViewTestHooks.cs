// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Windows.Foundation;
#if !HAS_UNO_WINUI
using Windows.UI.Xaml.Controls;
#endif

namespace Microsoft.UI.Private.Controls;

partial class ItemsViewTestHooks
{
	private static ItemsViewTestHooks s_testHooks;

	public static ItemsViewTestHooks GetGlobalTestHooks()
	{
		return s_testHooks;
	}

	static ItemsViewTestHooks EnsureGlobalTestHooks()
	{
		s_testHooks ??= new();
		return s_testHooks;
	}

	internal static Point GetKeyboardNavigationReferenceOffset(ItemsView itemsView)
	{
		if (itemsView is not null)
		{
			return itemsView.GetKeyboardNavigationReferenceOffset();
		}
		else
		{
			return new Point(-1.0f, -1.0f);
		}
	}

	internal static void NotifyKeyboardNavigationReferenceOffsetChanged(ItemsView itemsView)
	{
		var hooks = EnsureGlobalTestHooks();
		KeyboardNavigationReferenceOffsetChanged?.Invoke(itemsView, null);
	}

	static event TypedEventHandler<ItemsView, object> KeyboardNavigationReferenceOffsetChanged;

	internal static ScrollView GetScrollViewPart(ItemsView itemsView)
	{
		if (itemsView is not null)
		{
			return itemsView.GetScrollViewPart();
		}

		return null;
	}

	internal static ItemsRepeater GetItemsRepeaterPart(ItemsView itemsView)
	{
		if (itemsView is not null)
		{
			return itemsView.GetItemsRepeaterPart();
		}

		return null;
	}

	internal static SelectionModel GetSelectionModel(ItemsView itemsView)
	{
		if (itemsView is not null)
		{
			return itemsView.GetSelectionModel();
		}

		return null;
	}

}
