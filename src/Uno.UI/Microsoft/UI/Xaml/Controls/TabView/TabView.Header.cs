// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TabView.cpp, commit 367bb0d512cd

using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

public partial class TabView
{
	internal const double c_tabShadowDepth = 16.0;
	internal const string c_tabViewShadowDepthName = "TabViewShadowDepth";

	internal UIElement GetShadowReceiver() => m_shadowReceiver;

	internal string GetTabCloseButtonTooltipText() => m_tabCloseButtonTooltipText;

	private bool m_updateTabWidthOnPointerLeave = false;
	private bool m_pointerInTabstrip = false;

	private ColumnDefinition m_leftContentColumn;
	private ColumnDefinition m_tabColumn;
	private ColumnDefinition m_addButtonColumn;
	private ColumnDefinition m_rightContentColumn;

	private ListView m_listView;
	private ContentPresenter m_tabContentPresenter;
	private ContentPresenter m_rightContentPresenter;
	private Grid m_tabContainerGrid;
	private ScrollViewer m_scrollViewer;
	private RepeatButton m_scrollDecreaseButton;
	private RepeatButton m_scrollIncreaseButton;
	private Button m_addButton;
	private ItemsPresenter m_itemsPresenter;

	private Grid m_shadowReceiver;

	private readonly SerialDisposable m_listViewLoadedRevoker = new();
	private readonly SerialDisposable m_tabStripPointerExitedRevoker = new();
	private readonly SerialDisposable m_tabStripPointerEnteredRevoker = new();
	private readonly SerialDisposable m_listViewSelectionChangedRevoker = new();
	private readonly SerialDisposable m_listViewGettingFocusRevoker = new();

	private readonly SerialDisposable m_listViewCanReorderItemsPropertyChangedRevoker = new();
	private readonly SerialDisposable m_listViewAllowDropPropertyChangedRevoker = new();

	private readonly SerialDisposable m_listViewDragItemsStartingRevoker = new();
	private readonly SerialDisposable m_listViewDragItemsCompletedRevoker = new();
	private readonly SerialDisposable m_listViewDragOverRevoker = new();
	private readonly SerialDisposable m_listViewDropRevoker = new();

	private readonly SerialDisposable m_scrollViewerLoadedRevoker = new();
	private readonly SerialDisposable m_scrollViewerViewChangedRevoker = new();

	private readonly SerialDisposable m_addButtonClickRevoker = new();

	private readonly SerialDisposable m_scrollDecreaseClickRevoker = new();
	private readonly SerialDisposable m_scrollIncreaseClickRevoker = new();

	private readonly SerialDisposable m_addButtonKeyDownRevoker = new();

	private readonly SerialDisposable m_itemsPresenterSizeChangedRevoker = new();

	private DispatcherHelper m_dispatcherHelper;

	private string m_tabCloseButtonTooltipText;

	private Size m_previousAvailableSize;

	private bool m_isDragging;

#if HAS_UNO
	//TODO Uno specific: Watches scrollable width to update visibility of scroll buttons.
	private readonly SerialDisposable m_ScrollViewerScrollableWidthPropertyChangedRevoker = new();
#endif
}
