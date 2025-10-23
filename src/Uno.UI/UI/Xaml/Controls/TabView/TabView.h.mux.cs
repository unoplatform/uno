// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\controls\dev\TabView\TabView.h, commit 65718e2813

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.UI.Content;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Windows.Foundation;
using Windows.Graphics;
using Microsoft.UI;

namespace Microsoft.UI.Xaml.Controls;

internal enum TabTearOutDraggingState
{
	Idle,
	DraggingTabWithinTabView,
	DraggingTornOutTab,
};

public partial class TabView
{
	internal string GetTabCloseButtonTooltipText() => m_tabCloseButtonTooltipText;

	private static List<WeakReference<TabView>> s_tabViewWithTearOutList = new();
	private static readonly object _tabViewListLock = new object();

	//private static Mutex s_tabWithTearOutListMutex;

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

	private readonly SerialDisposable m_listViewLoadedRevoker = new();
	private readonly SerialDisposable m_tabStripPointerExitedRevoker = new();
	private readonly SerialDisposable m_tabStripPointerEnteredRevoker = new();
	private readonly SerialDisposable m_listViewSelectionChangedRevoker = new();
	private readonly SerialDisposable m_listViewGettingFocusRevoker = new();
	private readonly SerialDisposable m_listViewSizeChangedRevoker = new();

	private readonly SerialDisposable m_listViewCanReorderItemsPropertyChangedRevoker = new();
	private readonly SerialDisposable m_listViewAllowDropPropertyChangedRevoker = new();

	private readonly SerialDisposable m_listViewDragItemsStartingRevoker = new();
	private readonly SerialDisposable m_listViewDragItemsCompletedRevoker = new();
	private readonly SerialDisposable m_listViewDragOverRevoker = new();
	private readonly SerialDisposable m_listViewDropRevoker = new();
	private readonly SerialDisposable m_listViewDragEnterRevoker = new();
	private readonly SerialDisposable m_listViewDragLeaveRevoker = new();

	private readonly SerialDisposable m_scrollViewerLoadedRevoker = new();
	private readonly SerialDisposable m_scrollViewerViewChangedRevoker = new();

	private readonly SerialDisposable m_addButtonClickRevoker = new();

	private readonly SerialDisposable m_scrollDecreaseClickRevoker = new();
	private readonly SerialDisposable m_scrollIncreaseClickRevoker = new();

	private readonly SerialDisposable m_addButtonKeyDownRevoker = new();

	private readonly SerialDisposable m_itemsPresenterSizeChangedRevoker = new();

	private string m_tabCloseButtonTooltipText;

	private new Size m_previousAvailableSize;

	private bool m_isItemBeingDragged;
	private bool m_isItemDraggedOver;
	private double? m_expandedWidthForDragOver;

	private SerialDisposable m_enteringMoveSizeToken = new();
	private SerialDisposable m_enteredMoveSizeToken = new();
	private SerialDisposable m_windowRectChangingToken = new();
	private SerialDisposable m_exitedMoveSizeToken = new();

	private bool m_isInTabTearOutLoop;
	private AppWindow m_tabTearOutNewAppWindow = null;
	private object m_dataItemBeingDragged;
	private TabViewItem m_tabBeingDragged;
	private TabView m_tabViewContainingTabBeingDragged;
	private TabView m_tabViewInNewAppWindow;
	private Point m_originalTabBeingDraggedPoint;
	private Point m_dragPositionOffset;
	private TabTearOutDraggingState m_tabTearOutDraggingState = TabTearOutDraggingState.Idle;
	private PointInt32 m_tabTearOutInitialPosition;

	private RectInt32 m_nonClientRegion;
	private bool m_nonClientRegionSet;

	private List<(RectInt32, TabView)> m_tabViewBoundsTuples = new();

	private Microsoft.UI.WindowId m_lastAppWindowId;
	private InputNonClientPointerSource m_inputNonClientPointerSource;
	private ContentCoordinateConverter m_appWindowCoordinateConverter;

#if HAS_UNO
	//TODO Uno specific: Watches scrollable width to update visibility of scroll buttons.
	private readonly SerialDisposable m_ScrollViewerScrollableWidthPropertyChangedRevoker = new();
#endif
}
