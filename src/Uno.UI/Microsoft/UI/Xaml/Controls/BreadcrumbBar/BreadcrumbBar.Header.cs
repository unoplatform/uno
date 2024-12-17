// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BreadcrumbBar.h, tag winui3/release/1.5.3, commit 2a60e27

#nullable enable

using Uno.Disposables;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class BreadcrumbBar : Control
{
	private readonly SerialDisposable m_itemsRepeaterLoadedRevoker = new SerialDisposable();
	private readonly SerialDisposable m_itemsRepeaterElementPreparedRevoker = new SerialDisposable();
	private readonly SerialDisposable m_itemsRepeaterElementIndexChangedRevoker = new SerialDisposable();
	private readonly SerialDisposable m_itemsRepeaterElementClearingRevoker = new SerialDisposable();
	private readonly SerialDisposable m_itemsSourceChanged = new SerialDisposable();
	private readonly SerialDisposable m_breadcrumbKeyDownHandlerRevoker = new SerialDisposable();

	private readonly SerialDisposable m_itemsSourceAsObservableVectorChanged = new SerialDisposable();

	// This collection is only composed of the consumer defined objects, it doesn't
	// include the extra ellipsis/null element. This variable is only used to capture
	// changes in the ItemsSource
	private ItemsSourceView? m_breadcrumbItemsSourceView = null;

	// This is the "element collection" provided to the underlying ItemsRepeater, so it
	// includes the extra ellipsis/null element in the position 0.
	private BreadcrumbIterable? m_itemsIterable = null;

	private ItemsRepeater? m_itemsRepeater = null;
	private BreadcrumbElementFactory? m_itemsRepeaterElementFactory = null;
	private BreadcrumbLayout? m_itemsRepeaterLayout = null;

	// Pointers to first and last items to update visual states
	private BreadcrumbBarItem? m_ellipsisBreadcrumbBarItem = null;
	private BreadcrumbBarItem? m_lastBreadcrumbBarItem = null;

	// Index of the last focused item when breadcrumb lost focus
	private int m_focusedIndex = 1;

	// Template Parts
	private const string s_itemsRepeaterPartName = "PART_ItemsRepeater";
}
