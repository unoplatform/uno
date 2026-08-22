// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RefreshInfoProviderImpl.h, commit d883cf3

#nullable enable

using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using RefreshPullDirection = Microsoft.UI.Xaml.Controls.RefreshPullDirection;

namespace Microsoft.UI.Private.Controls;

internal partial class RefreshInfoProviderImpl
{
	private const double DEFAULT_EXECUTION_RATIO = 0.8;

	private RefreshPullDirection m_refreshPullDirection = RefreshPullDirection.TopToBottom;
	private Size m_refreshVisualizerSize = new Size(1.0f, 1.0f);
	private bool m_isInteractingForRefresh = false;
	private int m_interactionRatioChangedCount = 0;
	private CompositionPropertySet? m_compositionProperties = null;
	private string m_interactionRatioCompositionProperty = "InteractionRatio";
	private double m_executionRatio = DEFAULT_EXECUTION_RATIO;
	// The last interactionRatio for which InteractionRatioChanged actually fired (i.e. wasn't throttled).
	// Tracked so the throttle in RaiseInteractionRatioChanged can detect crossings of executionRatio
	// and force a notification when the pointer sequence jumps across the threshold without landing in
	// the ±ALWAYS_RAISE_INTERACTION_RATIO_TOLERANCE band — otherwise the visualizer's Pending state
	// is never reached, breaking pull-to-refresh.
	private double m_lastFiredInteractionRatio = 0.0;
	private bool m_peeking = false;

	public event TypedEventHandler<IRefreshInfoProvider, object> IsInteractingForRefreshChanged;
#pragma warning disable CS0067 // The event 'RefreshInfoProviderImpl.InteractionRatioChanged' is never used
	public event TypedEventHandler<IRefreshInfoProvider, RefreshInteractionRatioChangedEventArgs> InteractionRatioChanged;
#pragma warning restore CS0067 // The event 'RefreshInfoProviderImpl.InteractionRatioChanged' is never used
	public event TypedEventHandler<IRefreshInfoProvider, object> RefreshStarted;
	public event TypedEventHandler<IRefreshInfoProvider, object> RefreshCompleted;
#if HAS_UNO
	internal event TypedEventHandler<IRefreshInfoProvider, InteractionTracker> IdleEntered;
#endif
}
