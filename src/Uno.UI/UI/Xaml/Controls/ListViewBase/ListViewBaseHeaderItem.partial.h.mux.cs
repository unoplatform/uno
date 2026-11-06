// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseHeaderItem_Partial.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System;
using DirectUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class ListViewBaseHeaderItem : ILayoutTransitionStoryboardNotification, ITransitionContextProvider
{
	private WeakReference<ListViewBase>? m_wrParentListViewBase;
	private Rect m_rectGroupBoundsCache;
	private bool m_invalidateGroupBoundsCache = true;

	private bool m_IsPressed;
	private bool m_IsPointerOver;
}
