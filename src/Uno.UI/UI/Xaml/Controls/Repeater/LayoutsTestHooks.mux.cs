// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LayoutsTestHooks.cpp, commit b8cfb8490

#nullable enable

using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Microsoft.UI.Private.Controls;

partial class LayoutsTestHooks
{
	private static void EnsureHooks()
	{
		s_testHooks ??= new LayoutsTestHooks();
	}

	internal static IndexBasedLayoutOrientation GetLayoutForcedIndexBasedLayoutOrientation(object layout)
	{
		if (layout is Layout instance)
		{
			return instance.GetForcedIndexBasedLayoutOrientationDbg();
		}

		return IndexBasedLayoutOrientation.None;
	}

	internal static void SetLayoutForcedIndexBasedLayoutOrientation(object layout, IndexBasedLayoutOrientation forcedIndexBasedLayoutOrientation)
	{
		if (layout is Layout instance)
		{
			instance.SetForcedIndexBasedLayoutOrientationDbg(forcedIndexBasedLayoutOrientation);
		}
	}

	internal static void ResetLayoutForcedIndexBasedLayoutOrientation(object layout)
	{
		if (layout is Layout instance)
		{
			instance.ResetForcedIndexBasedLayoutOrientationDbg();
		}
	}

	internal static void LayoutInvalidateMeasure(object layout, bool relayout)
	{
		if (relayout && layout is LinedFlowLayout linedFlowLayout)
		{
			linedFlowLayout.InvalidateLayout();
			return;
		}

		if (layout is Layout instance)
		{
			instance.InvalidateMeasure();
		}
	}

	internal static int GetLayoutLogItemIndex(object layout)
	{
		if (layout is Layout instance)
		{
			return instance.LogItemIndexDbg();
		}

		return -1;
	}

	internal static void SetLayoutLogItemIndex(object layout, int logItemIndex)
	{
		if (layout is Layout instance)
		{
			instance.LogItemIndexDbg(logItemIndex);
		}
	}

	internal static int GetLayoutAnchorIndex(object layout)
	{
		if (layout is Layout instance)
		{
			return instance.LayoutAnchorIndexDbg();
		}

		return -1;
	}

	internal static double GetLayoutAnchorOffset(object layout)
	{
		if (layout is Layout instance)
		{
			return instance.LayoutAnchorOffsetDbg();
		}

		return -1.0;
	}

	internal static int GetLayoutFirstRealizedItemIndex(object layout)
	{
		if (layout is LinedFlowLayout instance)
		{
			return instance.FirstRealizedItemIndexDbg();
		}

		return -1;
	}

	internal static int GetLayoutLastRealizedItemIndex(object layout)
	{
		if (layout is LinedFlowLayout instance)
		{
			return instance.LastRealizedItemIndexDbg();
		}

		return -1;
	}

	internal static int GetLinedFlowLayoutFirstFrozenItemIndex(object linedFlowLayout)
	{
		if (linedFlowLayout is LinedFlowLayout instance)
		{
			return instance.FirstFrozenItemIndexDbg();
		}

		return -1;
	}

	internal static int GetLinedFlowLayoutLastFrozenItemIndex(object linedFlowLayout)
	{
		if (linedFlowLayout is LinedFlowLayout instance)
		{
			return instance.LastFrozenItemIndexDbg();
		}

		return -1;
	}

	internal static double GetLinedFlowLayoutAverageItemAspectRatio(object linedFlowLayout)
	{
		if (linedFlowLayout is LinedFlowLayout instance)
		{
			return instance.AverageItemAspectRatioDbg();
		}

		return 0.0;
	}

	internal static double GetLinedFlowLayoutRawAverageItemsPerLine(object linedFlowLayout)
	{
		if (linedFlowLayout is LinedFlowLayout instance)
		{
			return instance.RawAverageItemsPerLineDbg();
		}

		return 0.0;
	}

	internal static double GetLinedFlowLayoutSnappedAverageItemsPerLine(object linedFlowLayout)
	{
		if (linedFlowLayout is LinedFlowLayout instance)
		{
			return instance.SnappedAverageItemsPerLineDbg();
		}

		return 0.0;
	}

	internal static double GetLinedFlowLayoutForcedAverageItemAspectRatio(object linedFlowLayout)
	{
		if (linedFlowLayout is LinedFlowLayout instance)
		{
			return instance.ForcedAverageItemAspectRatioDbg();
		}

		return 0.0;
	}

	internal static void SetLinedFlowLayoutForcedAverageItemAspectRatio(object linedFlowLayout, double forcedAverageItemAspectRatio)
	{
		if (linedFlowLayout is LinedFlowLayout instance)
		{
			instance.ForcedAverageItemAspectRatioDbg(forcedAverageItemAspectRatio);
		}
	}

	internal static double GetLinedFlowLayoutForcedAverageItemsPerLineDivider(object linedFlowLayout)
	{
		if (linedFlowLayout is LinedFlowLayout instance)
		{
			return instance.ForcedAverageItemsPerLineDividerDbg();
		}

		return 0.0;
	}

	internal static void SetLinedFlowLayoutForcedAverageItemsPerLineDivider(object linedFlowLayout, double forcedAverageItemsPerLineDivider)
	{
		if (linedFlowLayout is LinedFlowLayout instance)
		{
			instance.ForcedAverageItemsPerLineDividerDbg(forcedAverageItemsPerLineDivider);
		}
	}

	internal static double GetLinedFlowLayoutForcedWrapMultiplier(object linedFlowLayout)
	{
		if (linedFlowLayout is LinedFlowLayout instance)
		{
			return instance.ForcedWrapMultiplierDbg();
		}

		return 0.0;
	}

	internal static void SetLinedFlowLayoutForcedWrapMultiplier(object linedFlowLayout, double forcedWrapMultiplier)
	{
		if (linedFlowLayout is LinedFlowLayout instance)
		{
			instance.ForcedWrapMultiplierDbg(forcedWrapMultiplier);
		}
	}

	internal static bool GetLinedFlowLayoutIsFastPathSupported(object linedFlowLayout)
	{
		if (linedFlowLayout is LinedFlowLayout instance)
		{
			return instance.IsFastPathSupportedDbg();
		}

		return false;
	}

	internal static void SetLinedFlowLayoutIsFastPathSupported(object linedFlowLayout, bool isFastPathSupported)
	{
		if (linedFlowLayout is LinedFlowLayout instance)
		{
			instance.IsFastPathSupportedDbg(isFastPathSupported);
		}
	}

	internal static int GetLinedFlowLayoutLineIndex(object linedFlowLayout, int itemIndex)
	{
		if (linedFlowLayout is LinedFlowLayout instance)
		{
			return instance.GetLineIndexDbg(itemIndex);
		}

		return -1;
	}

	internal static void ClearLinedFlowLayoutItemAspectRatios(object linedFlowLayout)
	{
		if (linedFlowLayout is LinedFlowLayout instance)
		{
			instance.ClearItemAspectRatios();
		}
	}

	internal static void UnlockLinedFlowLayoutItems(object linedFlowLayout)
	{
		if (linedFlowLayout is LinedFlowLayout instance)
		{
			instance.UnlockItems();
		}
	}

	internal static event TypedEventHandler<object, object> LayoutAnchorIndexChanged
	{
		add
		{
			EnsureHooks();
			s_testHooks!.m_layoutAnchorIndexChangedEventSource += value;
		}
		remove
		{
			EnsureHooks();
			s_testHooks!.m_layoutAnchorIndexChangedEventSource -= value;
		}
	}

	internal void NotifyLayoutAnchorIndexChanged(object layout) =>
		m_layoutAnchorIndexChangedEventSource?.Invoke(layout, null!);

	internal static event TypedEventHandler<object, object> LayoutAnchorOffsetChanged
	{
		add
		{
			EnsureHooks();
			s_testHooks!.m_layoutAnchorOffsetChangedEventSource += value;
		}
		remove
		{
			EnsureHooks();
			s_testHooks!.m_layoutAnchorOffsetChangedEventSource -= value;
		}
	}

	internal void NotifyLayoutAnchorOffsetChanged(object layout) =>
		m_layoutAnchorOffsetChangedEventSource?.Invoke(layout, null!);

	internal static event TypedEventHandler<object, object> LinedFlowLayoutSnappedAverageItemsPerLineChanged
	{
		add
		{
			EnsureHooks();
			s_testHooks!.m_linedFlowLayoutSnappedAverageItemsPerLineChangedEventSource += value;
		}
		remove
		{
			EnsureHooks();
			s_testHooks!.m_linedFlowLayoutSnappedAverageItemsPerLineChangedEventSource -= value;
		}
	}

	internal void NotifyLinedFlowLayoutSnappedAverageItemsPerLineChanged(object linedFlowLayout) =>
		m_linedFlowLayoutSnappedAverageItemsPerLineChangedEventSource?.Invoke(linedFlowLayout, null!);

	internal static event TypedEventHandler<object, LayoutsTestHooksLinedFlowLayoutInvalidatedEventArgs> LinedFlowLayoutInvalidated
	{
		add
		{
			EnsureHooks();
			s_testHooks!.m_linedFlowLayoutInvalidatedEventSource += value;
		}
		remove
		{
			EnsureHooks();
			s_testHooks!.m_linedFlowLayoutInvalidatedEventSource -= value;
		}
	}

	internal void NotifyLinedFlowLayoutInvalidated(object linedFlowLayout, LinedFlowLayoutInvalidationTrigger invalidationTrigger) =>
		m_linedFlowLayoutInvalidatedEventSource?.Invoke(
			linedFlowLayout,
			new LayoutsTestHooksLinedFlowLayoutInvalidatedEventArgs(invalidationTrigger));

	internal static event TypedEventHandler<object, LayoutsTestHooksLinedFlowLayoutItemLockedEventArgs> LinedFlowLayoutItemLocked
	{
		add
		{
			EnsureHooks();
			s_testHooks!.m_linedFlowLayoutItemLockedEventSource += value;
		}
		remove
		{
			EnsureHooks();
			s_testHooks!.m_linedFlowLayoutItemLockedEventSource -= value;
		}
	}

	internal void NotifyLinedFlowLayoutItemLocked(object linedFlowLayout, int itemIndex, int lineIndex) =>
		m_linedFlowLayoutItemLockedEventSource?.Invoke(
			linedFlowLayout,
			new LayoutsTestHooksLinedFlowLayoutItemLockedEventArgs(itemIndex, lineIndex));
}
