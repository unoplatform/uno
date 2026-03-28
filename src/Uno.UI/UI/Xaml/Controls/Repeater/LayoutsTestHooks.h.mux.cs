// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LayoutsTestHooks.h, commit 5f9e851133b3

using System;
using Windows.Foundation;

namespace Microsoft.UI.Private.Controls;

partial class LayoutsTestHooks
{
	private static LayoutsTestHooks s_testHooks;

	// Inline method from C++ header.
	internal static LayoutsTestHooks GetGlobalTestHooks() => s_testHooks;

	private event TypedEventHandler<object, object> m_layoutAnchorIndexChangedEventSource;
	private event TypedEventHandler<object, object> m_layoutAnchorOffsetChangedEventSource;

	// TODO Uno: LinedFlowLayout is not yet ported (PR 9).
	// Original C++ fields:
	// winrt::event<TypedEventHandler<IInspectable, IInspectable>> m_linedFlowLayoutSnappedAverageItemsPerLineChangedEventSource;
	// winrt::event<TypedEventHandler<IInspectable, LayoutsTestHooksLinedFlowLayoutInvalidatedEventArgs>> m_linedFlowLayoutInvalidatedEventSource;
	// winrt::event<TypedEventHandler<IInspectable, LayoutsTestHooksLinedFlowLayoutItemLockedEventArgs>> m_linedFlowLayoutItemLockedEventSource;
	// These will be added in PR 9.
}
