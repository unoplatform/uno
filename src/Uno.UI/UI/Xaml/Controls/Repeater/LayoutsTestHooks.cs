// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LayoutsTestHooks.h, commit 5f9e851133b3
// MUX Reference LayoutsTestHooks.cpp, commit 5f9e851133b3

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Private.Controls;

internal partial class LayoutsTestHooks
{
	private static LayoutsTestHooks s_testHooks;

	internal static LayoutsTestHooks GetGlobalTestHooks() => s_testHooks;

	private static void EnsureHooks()
	{
		if (s_testHooks == null)
		{
			s_testHooks = new LayoutsTestHooks();
		}
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
		if (relayout)
		{
			// TODO Uno: LinedFlowLayout.InvalidateLayout() is not yet ported (PR 9).
			// Original C++:
			// if (auto instance = layout.as<LinedFlowLayout>())
			// {
			//     instance->InvalidateLayout();
			//     return;
			// }
		}

		if (layout is Layout layoutInstance)
		{
			layoutInstance.InvalidateMeasure();
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
		// TODO Uno: LinedFlowLayout is not yet ported (PR 9).
		// Original C++:
		// if (auto instance = layout.as<LinedFlowLayout>())
		// {
		//     return instance->FirstRealizedItemIndexDbg();
		// }
		return -1;
	}

	internal static int GetLayoutLastRealizedItemIndex(object layout)
	{
		// TODO Uno: LinedFlowLayout is not yet ported (PR 9).
		return -1;
	}

	internal static int GetLinedFlowLayoutFirstFrozenItemIndex(object linedFlowLayout)
	{
		// TODO Uno: LinedFlowLayout is not yet ported (PR 9).
		return -1;
	}

	internal static int GetLinedFlowLayoutLastFrozenItemIndex(object linedFlowLayout)
	{
		// TODO Uno: LinedFlowLayout is not yet ported (PR 9).
		return -1;
	}

	internal static double GetLinedFlowLayoutAverageItemAspectRatio(object linedFlowLayout)
	{
		// TODO Uno: LinedFlowLayout is not yet ported (PR 9).
		return 0.0;
	}

	internal static double GetLinedFlowLayoutRawAverageItemsPerLine(object linedFlowLayout)
	{
		// TODO Uno: LinedFlowLayout is not yet ported (PR 9).
		return 0.0;
	}

	internal static double GetLinedFlowLayoutSnappedAverageItemsPerLine(object linedFlowLayout)
	{
		// TODO Uno: LinedFlowLayout is not yet ported (PR 9).
		return 0.0;
	}

	internal static double GetLinedFlowLayoutForcedAverageItemAspectRatio(object linedFlowLayout)
	{
		// TODO Uno: LinedFlowLayout is not yet ported (PR 9).
		return 0.0;
	}

	internal static void SetLinedFlowLayoutForcedAverageItemAspectRatio(object linedFlowLayout, double forcedAverageItemAspectRatio)
	{
		// TODO Uno: LinedFlowLayout is not yet ported (PR 9).
	}

	internal static double GetLinedFlowLayoutForcedAverageItemsPerLineDivider(object linedFlowLayout)
	{
		// TODO Uno: LinedFlowLayout is not yet ported (PR 9).
		return 0.0;
	}

	internal static void SetLinedFlowLayoutForcedAverageItemsPerLineDivider(object linedFlowLayout, double forcedAverageItemsPerLineDivider)
	{
		// TODO Uno: LinedFlowLayout is not yet ported (PR 9).
	}

	internal static double GetLinedFlowLayoutForcedWrapMultiplier(object linedFlowLayout)
	{
		// TODO Uno: LinedFlowLayout is not yet ported (PR 9).
		return 0.0;
	}

	internal static void SetLinedFlowLayoutForcedWrapMultiplier(object linedFlowLayout, double forcedWrapMultiplier)
	{
		// TODO Uno: LinedFlowLayout is not yet ported (PR 9).
	}

	internal static bool GetLinedFlowLayoutIsFastPathSupported(object linedFlowLayout)
	{
		// TODO Uno: LinedFlowLayout is not yet ported (PR 9).
		return false;
	}

	internal static void SetLinedFlowLayoutIsFastPathSupported(object linedFlowLayout, bool isFastPathSupported)
	{
		// TODO Uno: LinedFlowLayout is not yet ported (PR 9).
	}

	private event TypedEventHandler<object, object> m_layoutAnchorIndexChangedEventSource;

	internal static IDisposable LayoutAnchorIndexChanged(TypedEventHandler<object, object> value)
	{
		EnsureHooks();
		s_testHooks.m_layoutAnchorIndexChangedEventSource += value;
		return Uno.Disposables.Disposable.Create(() => s_testHooks.m_layoutAnchorIndexChangedEventSource -= value);
	}

	internal void NotifyLayoutAnchorIndexChanged(object layout)
	{
		m_layoutAnchorIndexChangedEventSource?.Invoke(layout, null);
	}

	private event TypedEventHandler<object, object> m_layoutAnchorOffsetChangedEventSource;

	internal static IDisposable LayoutAnchorOffsetChanged(TypedEventHandler<object, object> value)
	{
		EnsureHooks();
		s_testHooks.m_layoutAnchorOffsetChangedEventSource += value;
		return Uno.Disposables.Disposable.Create(() => s_testHooks.m_layoutAnchorOffsetChangedEventSource -= value);
	}

	internal void NotifyLayoutAnchorOffsetChanged(object layout)
	{
		m_layoutAnchorOffsetChangedEventSource?.Invoke(layout, null);
	}

	// TODO Uno: LinedFlowLayout is not yet ported (PR 9).
	// Original C++ events:
	// - LinedFlowLayoutSnappedAverageItemsPerLineChanged (TypedEventHandler<IInspectable, IInspectable>)
	// - LinedFlowLayoutInvalidated (TypedEventHandler<IInspectable, LayoutsTestHooksLinedFlowLayoutInvalidatedEventArgs>)
	// - LinedFlowLayoutItemLocked (TypedEventHandler<IInspectable, LayoutsTestHooksLinedFlowLayoutItemLockedEventArgs>)
	// These will be added in PR 9.

	internal static int GetLinedFlowLayoutLineIndex(object linedFlowLayout, int itemIndex)
	{
		// TODO Uno: LinedFlowLayout is not yet ported (PR 9).
		return -1;
	}

	internal static void ClearLinedFlowLayoutItemAspectRatios(object linedFlowLayout)
	{
		// TODO Uno: LinedFlowLayout is not yet ported (PR 9).
	}

	internal static void UnlockLinedFlowLayoutItems(object linedFlowLayout)
	{
		// TODO Uno: LinedFlowLayout is not yet ported (PR 9).
	}
}
