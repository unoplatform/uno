#if !__ANDROID__ && !__IOS__
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollViewerIRefreshInfoProviderAdapter.h, commit 838a0cc

#nullable enable

using Uno.Disposables;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using RefreshPullDirection = Microsoft.UI.Xaml.Controls.RefreshPullDirection;

namespace Microsoft.UI.Private.Controls;

internal partial class ScrollViewerIRefreshInfoProviderAdapter
{
	private RefreshInfoProviderImpl? m_infoProvider = null;
	private IAdapterAnimationHandler? m_animationHandler = null;
	private ScrollViewer? m_scrollViewer = null;
	private RefreshPullDirection m_refreshPullDirection = RefreshPullDirection.TopToBottom;
	private InteractionTracker? m_interactionTracker = null;
	private VisualInteractionSource? m_visualInteractionSource = null;
	private bool m_visualInteractionSourceIsAttached = false;

	private SerialDisposable m_scrollViewer_LoadedToken = new SerialDisposable();
	private SerialDisposable m_scrollViewer_DirectManipulationCompletedToken = new SerialDisposable();
	private SerialDisposable m_scrollViewer_ViewChangingToken = new SerialDisposable();
	private SerialDisposable m_infoProvider_RefreshStartedToken = new SerialDisposable();
	private SerialDisposable m_infoProvider_RefreshCompletedToken = new SerialDisposable();

	private PointerEventHandler m_boxedPointerPressedEventHandler;
}
#endif
