// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LayoutsTestHooks.h, commit b8cfb8490

#nullable enable

using Windows.Foundation;

namespace Microsoft.UI.Private.Controls;

partial class LayoutsTestHooks
{
	internal static LayoutsTestHooks? GetGlobalTestHooks() => s_testHooks;

	private static LayoutsTestHooks? s_testHooks;

	private event TypedEventHandler<object, object>? m_layoutAnchorIndexChangedEventSource;
	private event TypedEventHandler<object, object>? m_layoutAnchorOffsetChangedEventSource;
	private event TypedEventHandler<object, object>? m_linedFlowLayoutSnappedAverageItemsPerLineChangedEventSource;
	private event TypedEventHandler<object, LayoutsTestHooksLinedFlowLayoutInvalidatedEventArgs>? m_linedFlowLayoutInvalidatedEventSource;
	private event TypedEventHandler<object, LayoutsTestHooksLinedFlowLayoutItemLockedEventArgs>? m_linedFlowLayoutItemLockedEventSource;
}
