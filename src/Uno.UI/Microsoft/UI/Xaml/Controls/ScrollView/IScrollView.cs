// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

internal interface IScrollView
{
	Visibility ComputedHorizontalScrollBarVisibility { get; }

	ScrollingScrollMode ComputedHorizontalScrollMode { get; }

	Visibility ComputedVerticalScrollBarVisibility { get; }

	ScrollingScrollMode ComputedVerticalScrollMode { get; }

	UIElement Content { get; set; }

	ScrollingContentOrientation ContentOrientation { get; set; }

	UIElement CurrentAnchor { get; }

	CompositionPropertySet ExpressionAnimationSources { get; }

	double ExtentHeight { get; }

	double ExtentWidth { get; }

	double HorizontalAnchorRatio { get; set; }

	double HorizontalOffset { get; }

	ScrollingScrollBarVisibility HorizontalScrollBarVisibility { get; set; }

	ScrollingChainMode HorizontalScrollChainMode { get; set; }

	ScrollingScrollMode HorizontalScrollMode { get; set; }

	ScrollingRailMode HorizontalScrollRailMode { get; set; }

	ScrollingInputKinds IgnoredInputKinds { get; set; }

	double MaxZoomFactor { get; set; }

	double MinZoomFactor { get; set; }

	ScrollPresenter ScrollPresenter { get; }

	double ScrollableHeight { get; }

	double ScrollableWidth { get; }

	ScrollingInteractionState State { get; }

	double VerticalAnchorRatio { get; set; }

	double VerticalOffset { get; }

	ScrollingScrollBarVisibility VerticalScrollBarVisibility { get; set; }

	ScrollingChainMode VerticalScrollChainMode { get; set; }

	ScrollingScrollMode VerticalScrollMode { get; set; }

	ScrollingRailMode VerticalScrollRailMode { get; set; }

	double ViewportHeight { get; }

	double ViewportWidth { get; }

	ScrollingChainMode ZoomChainMode { get; set; }

	float ZoomFactor { get; }

	ScrollingZoomMode ZoomMode { get; set; }

	event TypedEventHandler<ScrollView, ScrollingAnchorRequestedEventArgs> AnchorRequested;

	event TypedEventHandler<ScrollView, ScrollingBringingIntoViewEventArgs> BringingIntoView;

	event TypedEventHandler<ScrollView, object> ExtentChanged;

	event TypedEventHandler<ScrollView, ScrollingScrollAnimationStartingEventArgs> ScrollAnimationStarting;

	event TypedEventHandler<ScrollView, ScrollingScrollCompletedEventArgs> ScrollCompleted;

	event TypedEventHandler<ScrollView, object> StateChanged;

	event TypedEventHandler<ScrollView, object> ViewChanged;

	event TypedEventHandler<ScrollView, ScrollingZoomAnimationStartingEventArgs> ZoomAnimationStarting;

	event TypedEventHandler<ScrollView, ScrollingZoomCompletedEventArgs> ZoomCompleted;

	void RegisterAnchorCandidate(UIElement element);

	void UnregisterAnchorCandidate(UIElement element);

	int ScrollTo(double horizontalOffset, double verticalOffset);

	int ScrollTo(double horizontalOffset, double verticalOffset, ScrollingScrollOptions options);

	int ScrollBy(double horizontalOffsetDelta, double verticalOffsetDelta);

	int ScrollBy(double horizontalOffsetDelta, double verticalOffsetDelta, ScrollingScrollOptions options);

	int AddScrollVelocity(Vector2 offsetsVelocity, Vector2? inertiaDecayRate);

	int ZoomTo(float zoomFactor, Vector2? centerPoint);

	int ZoomTo(float zoomFactor, Vector2? centerPoint, ScrollingZoomOptions options);

	int ZoomBy(float zoomFactorDelta, Vector2? centerPoint);

	int ZoomBy(float zoomFactorDelta, Vector2? centerPoint, ScrollingZoomOptions options);

	int AddZoomVelocity(float zoomFactorVelocity, Vector2? centerPoint, float? inertiaDecayRate);
}
