#if !__ANDROID__ && !__IOS__
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollViewerIRefreshInfoProviderDefaultAnimationHandler.h, commit d883cf3

#nullable enable

using Uno.Disposables;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Xaml;
using RefreshPullDirection = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RefreshPullDirection;

namespace Microsoft.UI.Private.Controls;

internal partial class ScrollViewerIRefreshInfoProviderDefaultAnimationHandler
{
	private readonly RefreshPullDirection m_refreshPullDirection;

	private UIElement? m_refreshVisualizer = null;
	private UIElement? m_infoProvider = null;
	private Visual? m_refreshVisualizerVisual = null;
	private Visual? m_infoProviderVisual = null;
	private InteractionTracker? m_interactionTracker = null;
	private Compositor? m_compositor = null;

	private bool m_interactionAnimationNeedsUpdating = true;
	private bool m_refreshRequestedAnimationNeedsUpdating = true;
	private bool m_refreshCompletedAnimationNeedsUpdating = true;

	private ExpressionAnimation? m_refreshVisualizerVisualOffsetAnimation = null;
	private ExpressionAnimation? m_infoProviderOffsetAnimation = null;

	private ExpressionAnimation? m_refreshVisualizerRefreshRequestedAnimation = null; // UNO TODO: Composition key frame animations are not yet supported.
	private ExpressionAnimation? m_infoProviderRefreshRequestedAnimation = null; // UNO TODO: Composition key frame animations are not yet supported.

	private ExpressionAnimation? m_refreshVisualizerRefreshCompletedAnimation = null; // UNO TODO: Composition key frame animations are not yet supported.
	private ExpressionAnimation? m_infoProviderRefreshCompletedAnimation = null; // UNO TODO: Composition key frame animations are not yet supported.
	private CompositionScopedBatch? m_refreshCompletedScopedBatch = null;

	private readonly SerialDisposable m_compositionScopedBatchCompletedEventToken = new SerialDisposable();
}
#endif
