// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference StickyHeaderWrapper.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

//  Abstract:
//      A helper class for managing various data and lifetimes associated with sticky headers

#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace DirectUI;

internal sealed partial class StickyHeaderWrapper : IDisposable
{
#if !HAS_UNO // TODO Uno: no DManip secondary content relationships.
	internal ISecondaryContentRelationship GetRelationship() => m_spRelationship;
#endif

	internal double GetGroupBottom() => m_groupBottom;
	internal double GetPanelOffset() => m_panelOffset;
	internal double GetPannableExtent() => m_pannableExtent;
	internal Rect GetDesiredBounds() => m_desiredBounds;

	// we call this method to apply render transform to parent Header to align it with the right position on the screen.
	// note that during direct manipulation parent's position will be off and TransformToVisual API will return wrong data.
	// See UpdateHeaderPosition.

	// Compensate for overpan compression.
	//
	// When overpan compression is in effect, the default DManip overpan behavior is turned off.
	// Secondary content curves are used to create the overpan compression effect.
	// The sticky header curves are not aware of the secondary content curves for overpan compression.
	// Thus, if we do not compensate for them here, the Sticky Header curves have no knowledge of
	// content boundaries, and will continue to produce a speedbump effect even when we are panning
	// beyond the pannable range.
	//
	// We don't want this - we want to make sure the sticky header curves are only in effect for the pannable range.
	// Thus, we must clamp the sticky header curve coordinates at the pannable range.
	// See GetEffectiveDimensions.

#if !HAS_UNO // TODO Uno: no layout transition element, DManip relationship or storyboard.
	private UIElement m_spLayoutTransitionElement;
	private CUIElement m_pLTEParentNoRef;

	private CompositeTransform m_spTransform;
	private ISecondaryContentRelationship m_spRelationship;

	private StoryboardCompletedEventCallback m_epStoryboardCompleted;
	private Storyboard m_spStoryboard;
#endif

	private WeakReference<UIElement>? m_wrTarget;
	private WeakReference<Panel>? m_wrTargetHost;
	private Rect m_desiredBounds;
	private double m_groupBottom;
	private double m_panelOffset;
	private double m_pannableExtent;

#if HAS_UNO
	// The header is rendered directly, so its own transform carries the sticky offset that the DManip relationship would drive.
	private CompositeTransform? m_spHeaderTransform;
	private WeakReference<ScrollViewer>? m_wrScrollViewer;
#endif
}
