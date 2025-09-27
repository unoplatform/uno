// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: TabViewItem.h, commit 65718e2813

#nullable enable

using Uno.Disposables;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class TabViewItem
{
	private static readonly string s_dragDropVisualVisibleStateName = "DragDropVisualVisible";
	private static readonly string s_dragDropVisualNotVisibleStateName = "DragDropVisualNotVisible";
	private static readonly string s_closeButtonCollapsedStateName = "CloseButtonCollapsed";
	private static readonly string s_closeButtonVisibleStateName = "CloseButtonVisible";
	private static readonly string s_compactStateName = "Compact";
	private static readonly string s_standardWidthStateName = "StandardWidth";
	private static readonly string s_iconStateName = "Icon";
	private static readonly string s_noIconStateName = "NoIcon";
	private static readonly string s_foregroundSetStateName = "ForegroundNotSet";
	private static readonly string s_foregroundNotSetStateName = "ForegroundSet";
	private static readonly string s_selectedBackgroundPathName = "SelectedBackgroundPath";
	private static readonly string s_contentPresenterName = "ContentPresenter";
	private static readonly string s_closeButtonName = "CloseButton";
	//private static readonly string s_tabDragVisualContainer = "TabDragVisualContainer";

	internal Button? GetCloseButton() => m_closeButton;

	private bool m_hasPointerCapture = false;
	private bool m_isMiddlePointerButtonPressed = false;
	private bool m_isBeingDragged = false;
	private bool m_isPointerOver = false;
	private bool m_isCheckingforDrag = false;
	private bool m_firstTimeSettingToolTip = true;

	private Path? m_selectedBackgroundPath;
	private Button? m_closeButton;
	private ToolTip? m_toolTip;
	private ContentPresenter? m_headerContentPresenter;
	private TabViewWidthMode m_tabViewWidthMode = TabViewWidthMode.Equal;
	private TabViewCloseButtonOverlayMode m_closeButtonOverlayMode = TabViewCloseButtonOverlayMode.Auto;

	private readonly SerialDisposable m_selectedBackgroundPathSizeChangedRevoker = new();
	private readonly SerialDisposable m_closeButtonClickRevoker = new();
	private readonly SerialDisposable m_tabDragStartingRevoker = new();
	private readonly SerialDisposable m_tabDragCompletedRevoker = new();
	private Point m_lastPointerPressedPosition = new();

	private TabView? m_parentTabView;

	private DispatcherHelper m_dispatcherHelper;

	private uint m_dragPointerId = 0;
}
