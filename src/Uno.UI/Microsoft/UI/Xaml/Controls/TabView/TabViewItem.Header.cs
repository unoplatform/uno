// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: TabViewItem.h, commit 27052f7

using Uno.Disposables;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial class TabViewItem
	{
		private Button m_closeButton = null;
		private ToolTip m_toolTip = null;
		private ContentPresenter m_headerContentPresenter = null;
		private TabViewWidthMode m_tabViewWidthMode = TabViewWidthMode.Equal;
		private TabViewCloseButtonOverlayMode m_closeButtonOverlayMode = TabViewCloseButtonOverlayMode.Auto;

		private bool m_firstTimeSettingToolTip = true;

		//private readonly SerialDisposable m_closeButtonClickRevoker = new SerialDisposable();
		//private readonly SerialDisposable m_tabDragStartingRevoker = new SerialDisposable();
		//private readonly SerialDisposable m_tabDragCompletedRevoker = new SerialDisposable();

		private bool m_hasPointerCapture = false;
		private bool m_isMiddlePointerButtonPressed = false;
		private bool m_isDragging = false;
		private bool m_isPointerOver = false;

		private object m_shadow = null;

		private TabView m_parentTabView = null;
	}
}
