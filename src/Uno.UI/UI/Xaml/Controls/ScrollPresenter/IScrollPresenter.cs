// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

internal interface IScrollPresenter
{
	Brush Background { get; set; }

	ScrollingScrollMode ComputedHorizontalScrollMode { get; }

	ScrollingScrollMode ComputedVerticalScrollMode { get; }

	UIElement Content { get; set; }

	ScrollingContentOrientation ContentOrientation { get; set; }

	CompositionPropertySet ExpressionAnimationSources { get; }

	double ExtentHeight { get; }

	double ExtentWidth { get; }

	double HorizontalAnchorRatio { get; set; }

	double HorizontalOffset { get; }

	ScrollingChainMode HorizontalScrollChainMode { get; set; }

	IScrollController HorizontalScrollController { get; set; }

	ScrollingScrollMode HorizontalScrollMode { get; set; }

	ScrollingRailMode HorizontalScrollRailMode { get; set; }

	IList<ScrollSnapPointBase> HorizontalSnapPoints { get; }

	ScrollingInputKinds IgnoredInputKinds { get; set; }

	double MaxZoomFactor { get; set; }

	double MinZoomFactor { get; set; }

	double ScrollableHeight { get; }

	double ScrollableWidth { get; }

	ScrollingInteractionState State { get; }

	double VerticalAnchorRatio { get; set; }

	double VerticalOffset { get; }

	ScrollingChainMode VerticalScrollChainMode { get; set; }

	IScrollController VerticalScrollController { get; set; }

	ScrollingScrollMode VerticalScrollMode { get; set; }

	ScrollingRailMode VerticalScrollRailMode { get; set; }

	IList<ScrollSnapPointBase> VerticalSnapPoints { get; }

	double ViewportHeight { get; }

	double ViewportWidth { get; }

	ScrollingChainMode ZoomChainMode { get; set; }

	float ZoomFactor { get; }

	ScrollingZoomMode ZoomMode { get; set; }

	IList<ZoomSnapPointBase> ZoomSnapPoints { get; }

	event TypedEventHandler<ScrollPresenter, ScrollingAnchorRequestedEventArgs> AnchorRequested;

	event TypedEventHandler<ScrollPresenter, ScrollingBringingIntoViewEventArgs> BringingIntoView;

	event TypedEventHandler<ScrollPresenter, object> ExtentChanged;

	event TypedEventHandler<ScrollPresenter, ScrollingScrollAnimationStartingEventArgs> ScrollAnimationStarting;

	event TypedEventHandler<ScrollPresenter, ScrollingScrollCompletedEventArgs> ScrollCompleted;

	event TypedEventHandler<ScrollPresenter, object> StateChanged;

	event TypedEventHandler<ScrollPresenter, object> ViewChanged;

	event TypedEventHandler<ScrollPresenter, ScrollingZoomAnimationStartingEventArgs> ZoomAnimationStarting;

	event TypedEventHandler<ScrollPresenter, ScrollingZoomCompletedEventArgs> ZoomCompleted;

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
