// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: TabViewItem.h, commit 27052f7

#nullable enable

using Uno.Disposables;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class TabViewItem
{
	internal Button? GetCloseButton() => m_closeButton;

	private bool m_hasPointerCapture = false;
	private bool m_isMiddlePointerButtonPressed = false;
	private bool m_isDragging = false;
	private bool m_isPointerOver = false;
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

	private object? m_shadow;

	private TabView? m_parentTabView;

	private DispatcherHelper m_dispatcherHelper;
}
