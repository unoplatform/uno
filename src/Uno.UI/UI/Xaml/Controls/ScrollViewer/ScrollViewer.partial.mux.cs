// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollViewer_Partial.cpp, commit 5f9e85113

#nullable disable

using System;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.DataBinding;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls
{
#if __SKIA__
	// MUX Reference ScrollViewer_Partial.h:90 — `class ScrollViewer : public IScrollOwner`.
	// Adding the interface as a separate partial keeps the cross-platform
	// declaration in ScrollViewer.cs intact while making SV serve as the
	// IScrollOwner for the new SCP port on Skia.
	partial class ScrollViewer : IScrollOwner
	{
	}
#endif

	partial class ScrollViewer
	{
#if __SKIA__
#pragma warning disable IDE0051 // Private member is unused (placeholder for full impl)

		// These keycodes are undefined in the VirtualKey enum, so define them here.
		// - (on number row)
		private const VirtualKey SCROLLVIEWER_KEYCODE_MINUS = (VirtualKey)189;
		// = (on number row)
		private const VirtualKey SCROLLVIEWER_KEYCODE_EQUALS = (VirtualKey)187;

		// Note: IsAnimationEnabled() is provided by the cross-platform ScrollViewer.cs partial.

		// IsEnabled property changed handler.
		// (C++ source line 509)
		internal void OnIsEnabledChangedCore()
		{
			// The new IsEnabled status most likely changes the result of get_CanManipulateElements.
			// TODO Uno: Phase 4 — port OnManipulatabilityAffectingPropertyChanged.
			// OnManipulatabilityAffectingPropertyChanged(
			//     pIsInLiveTree: null,
			//     isCachedPropertyChanged: false,
			//     isContentChanged: false,
			//     isAffectingConfigurations: !IsInDirectManipulation,
			//     isAffectingTouchConfiguration: false);

			if (!IsEnabled)
			{
				// TODO Uno: Phase 4 — port SetConstantVelocities. The constant-velocity scroll path
				// is not yet wired up on Skia.
				// SetConstantVelocities(0, 0);
			}

			// TODO Uno: Phase 4 — call UpdateVisualState() once the new ChangeVisualState is the source of truth.
			// UpdateVisualState();
		}

		// Called when the parent of this ScrollViewer changed. Makes sure the manipulation handler knows
		// about the possible change in the get_CanManipulateElements return value.
		// (C++ source line 549)
		internal void OnTreeParentUpdatedCore(object pNewParent, bool isParentAlive)
		{
			// TODO Uno: Phase 4 — DM-container registration. The C++ source toggles this when the SV
			// enters/leaves the live tree. On Skia the InputManager.PointerManager handles this implicitly.
			// if (m_hManipulationHandler is null)
			// {
			//     put_IsDirectManipulationContainer(isParentAlive || pNewParent is not null);
			// }
			// else
			// {
			//     OnManipulatabilityAffectingPropertyChanged(
			//         pIsInLiveTree: null,
			//         isCachedPropertyChanged: false,
			//         isContentChanged: false,
			//         isAffectingConfigurations: false,
			//         isAffectingTouchConfiguration: false);
			// }
		}

		// Initializes a new instance of the ScrollViewer class.
		// Note: this is the field-init logic from the C++ constructor. The actual public C# constructor
		// lives on the cross-platform partial (ScrollViewer.cs) and calls InitializePartial(), which we
		// hook into via InitializePartial below.
		partial void InitializePartial()
		{
			m_inMeasure = false;
			m_inChildInvalidateMeasure = false;
			m_ignoreSemanticZoomNavigationInput = false;
			m_handleScrollInfoWheelEvent = true;
			m_scrollVisibilityX = Visibility.Visible;
			m_scrollVisibilityY = Visibility.Visible;
			m_xOffset = 0.0;
			m_yOffset = 0.0;
			m_xMinOffset = 0.0;
			m_yMinOffset = 0.0;
			m_xPixelOffset = 0.0;
			m_yPixelOffset = 0.0;
			m_unboundHorizontalOffset = 0.0;
			m_unboundVerticalOffset = 0.0;
			m_xViewport = 0.0;
			m_yViewport = 0.0;
			m_xPixelViewport = 0.0;
			m_yPixelViewport = 0.0;
			m_xExtent = 0.0;
			m_yExtent = 0.0;
			m_xPixelExtent = 0.0;
			m_yPixelExtent = 0.0;
			m_contentWidthRequested = -1;
			m_contentHeightRequested = -1;
			m_xPixelOffsetRequested = -1;
			m_yPixelOffsetRequested = -1;
			m_cLevelsFromRootCallForZoom = 0;
			m_initialMaxZoomFactor = 0.0f;
			m_initialZoomFactor = 0.0f;
			m_requestedMaxZoomFactor = 0.0f;
			m_requestedZoomFactor = 0.0f;
			m_preDirectManipulationOffsetX = 0.0f;
			m_preDirectManipulationOffsetY = 0.0f;
			m_preDirectManipulationZoomFactor = 0.0f;
			m_preDirectManipulationNonVirtualizedTranslationCorrection = 0.0f;
			m_currentHorizontalScrollMode = ScrollMode.Disabled;
			m_currentVerticalScrollMode = ScrollMode.Disabled;
			m_currentZoomMode = ZoomMode.Disabled;
			m_currentHorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
			m_currentVerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
			m_currentHorizontalAlignment = DMAlignment.None;
			m_currentVerticalAlignment = DMAlignment.None;
			m_currentIsHorizontalRailEnabled = false;
			m_currentIsVerticalRailEnabled = false;
			m_currentIsScrollInertiaEnabled = false;
			m_currentIsZoomInertiaEnabled = false;
			m_pDMStateChangeHandler = null;
			m_hManipulationHandler = null;
			m_canManipulateElementsByTouch = false;
			m_canManipulateElementsNonTouch = false;
			m_canManipulateElementsWithBringIntoViewport = false;
			m_canManipulateElementsWithAsyncBringIntoViewport = false;
			m_touchConfiguration = DMConfigurations.None;
			m_nonTouchConfiguration = DMConfigurations.None;
			m_activeHorizontalAlignment = DMAlignment.None;
			m_activeVerticalAlignment = DMAlignment.None;
			m_overridingMinZoomFactor = 0.0f;
			m_overridingMaxZoomFactor = 0.0f;
			m_isInDirectManipulation = false;
			m_isDirectManipulationStopped = false;
			m_isInDirectManipulationCompletion = false;
			m_isInDirectManipulationZoom = false;
			m_isInDirectManipulationSync = false;
			m_isInChangeViewBringIntoViewport = false;
			m_isInZoomFactorSync = false;
			m_isManipulationHandlerInterestedInNotifications = false;
			m_isDirectManipulationZoomFactorChange = false;
			m_isDirectManipulationZoomFactorChangeIgnored = false;
			m_isOffsetChangeIgnored = false;
			m_areViewportConfigurationsInvalid = false;
			m_isCanManipulateElementsInvalid = false;
			m_isScrollBarIgnoringUserInputInvalid = false;
			m_shouldFocusOnRightTapUnhandled = false;
			m_showingMouseIndicators = false;
			m_preferMouseIndicators = false;
			m_isTargetHorizontalOffsetValid = false;
			m_isTargetVerticalOffsetValid = false;
			m_isTargetZoomFactorValid = false;
			m_targetHorizontalOffset = 0.0;
			m_targetVerticalOffset = 0.0;
			m_targetZoomFactor = 1.0f;
			m_targetChangeViewHorizontalOffset = -1.0;
			m_targetChangeViewVerticalOffset = -1.0;
			m_targetChangeViewZoomFactor = -1.0f;
			m_inertiaEndHorizontalOffset = 0.0f;
			m_inertiaEndVerticalOffset = 0.0f;
			m_inertiaEndZoomFactor = 1.0f;
			m_isInertiaEndTransformValid = false;
			m_isInertial = false;
			m_isViewChangingDelayed = false;
			m_isViewChangedDelayed = false;
			m_isDelayedViewChangedIntermediate = false;
			m_isInIntermediateViewChangedMode = false;
			m_isViewChangedRaisedInIntermediateMode = false;
			m_iViewChangingDelay = 0;
			m_iViewChangedDelay = 0;
			m_inSemanticZoomAnimation = false;
			m_keepIndicatorsShowing = false;
			m_isPointerOverVerticalScrollbar = false;
			m_isPointerOverHorizontalScrollbar = false;
			m_isLoaded = false;
			m_blockIndicators = false;
			m_isDraggingThumb = false;
			m_horizontalOffsetCached = -1.0;
			m_verticalOffsetCached = -1.0;
			m_isTopLeftHeaderAssociated = false;
			m_isTopHeaderAssociated = false;
			m_isLeftHeaderAssociated = false;
			m_isNearVerticalAlignmentForced = false;
			m_isHorizontalStretchAlignmentTreatedAsNear = false;
			m_isVerticalStretchAlignmentTreatedAsNear = false;
			m_cForcePanXConfiguration = 0;
			m_cForcePanYConfiguration = 0;
			m_layoutSize = new global::Windows.Foundation.Size(0, 0);

			m_horizontalOverpanMode = DMOverpanMode.Default;
			m_verticalOverpanMode = DMOverpanMode.Default;
		}

#if false
		// TODO Uno: Original C++ destructor cleanup. Uno does not support cleanup via finalizers.
		// Move this logic into Loaded/Unloaded event handlers or other lifecycle methods to avoid leaks.

		// Original destructor logic (not executed):
		// ~ScrollViewer()
		// {
		//     // Releasing and unhooking of template parts and events
		//     IGNOREHR(UnhookTemplate());
		//
		//     m_trManipulatableElement.Clear();
		//
		//     if (m_spZoomSnapPoints)
		//     {
		//         IGNOREHR(m_spZoomSnapPoints->remove_VectorChanged(m_ZoomSnapPointsVectorChangedToken));
		//         ZeroMemory(&m_ZoomSnapPointsVectorChangedToken, sizeof(m_ZoomSnapPointsVectorChangedToken));
		//     }
		//
		//     if (m_coreInputViewOcclusionsChangedToken.value != 0)
		//     {
		//         ... // CoreInputView occlusion handler removal — N/A on Uno managed targets.
		//     }
		//
		//     IGNOREHR(UnhookScrollSnapPointsInfoEvents(TRUE /*isForHorizontalSnapPoints*/));
		//     IGNOREHR(UnhookScrollSnapPointsInfoEvents(FALSE /*isForHorizontalSnapPoints*/));
		//
		//     if (auto dxamlCore = DXamlCore::GetCurrent())
		//     {
		//         dxamlCore->UnregisterFromDynamicScrollbarsSettingChanged(this);
		//     }
		// }
#endif

		// (Public events declared by the IDL surface — currently implemented for Skia only.)

		// Occurs when the view is about to change.
		public event EventHandler<ScrollViewerViewChangingEventArgs> ViewChanging;

		// Occurs when DirectManipulation starts.
		public event EventHandler<object> DirectManipulationStarted;

		// Occurs when DirectManipulation completes.
		public event EventHandler<object> DirectManipulationCompleted;

		// Gets reference to the IScrollInfo implementation.
		internal IScrollInfo GetScrollInfo() => m_wrScrollInfo?.Target as IScrollInfo;

		// Sets reference to the IScrollInfo implementation.
		internal void PutScrollInfo(IScrollInfo value)
		{
			if (m_wrScrollInfo is not null)
			{
				WeakReferencePool.ReturnWeakReference(this, m_wrScrollInfo);
				m_wrScrollInfo = null;
			}

			if (value is not null)
			{
				m_wrScrollInfo = WeakReferencePool.RentWeakReference(this, value);
				UpdateCanScroll(value);
			}
		}

		// Hook the WinUI-port template-part state on top of the cross-platform
		// OnApplyTemplate already in ScrollViewer.cs. Caches the SCP, the
		// horizontal/vertical ScrollBars and wires the Scroll / DragStarted /
		// DragCompleted / PointerEntered / PointerExited handlers as the
		// C++ OnApplyTemplate at line 1232 does. The remainder of the C++
		// OnApplyTemplate (visual-state storyboards, transition cleanup,
		// IsRootScrollViewer special case) is Phase-4 work.
		private void OnApplyTemplate_MuxPartial()
		{
			m_keepIndicatorsShowing = false;

			UnhookTemplate();

			// no longer dragging a thumb
			m_isDraggingThumb = false;

			m_hasNoIndicatorStateStoryboardCompletedHandler = false;

			if (!IsRootScrollViewer() || IsRootScrollViewerAllowImplicitStyle())
			{
				// Get the parts. Uno keeps the cross-platform _presenter cached on
				// ScrollViewer.cs; reuse that here so we don't double-fetch the
				// template child. The two scroll bars are still resolved by name
				// because the cross-platform path doesn't cache them as fields
				// the new port can read.
				m_trElementScrollContentPresenter = _presenter as ScrollContentPresenter;
				m_trElementHorizontalScrollBar = GetTemplateChild("HorizontalScrollBar") as ScrollBar;
				m_trElementVerticalScrollBar = GetTemplateChild("VerticalScrollBar") as ScrollBar;
				m_tpElementScrollBarSeparator = GetTemplateChild("ScrollBarSeparator") as UIElement;

				// Re-run the SCP's HookupScrollingComponents from here. SCP.OnApplyTemplate
				// runs earlier in the layout pipeline before the cross-platform SV sets
				// `presenter.ScrollOwner = this`, so the SCP-side hookup typically can't
				// resolve its owning SV yet. Calling it again from here, after SCP.ScrollOwner
				// has been set by the cross-platform SV.OnApplyTemplate (ScrollViewer.cs:997),
				// wires SV.PutScrollInfo and SCP.PutScrollOwner so the new IScrollInfo /
				// IScrollOwner pipeline becomes live for programmatic scroll + bring-into-view.
				if (m_trElementScrollContentPresenter is not null)
				{
					m_trElementScrollContentPresenter.HookupScrollingComponents();
				}
			}

			if (m_trElementHorizontalScrollBar is { } hScrollBar)
			{
				if (m_trElementScrollContentPresenter is { } scp && scp.IsChildActualWidthUsedAsExtent())
				{
					hScrollBar.StartUseOfActualSizeAsExtent();
				}

				ScrollEventHandler scrollHandler = OnHorizontalScrollBarScroll;
				hScrollBar.Scroll += scrollHandler;
				m_HorizontalScrollToken.Disposable = global::Uno.Disposables.Disposable.Create(() => hScrollBar.Scroll -= scrollHandler);

				DragStartedEventHandler dragStartedHandler = OnScrollBarThumbDragStarted;
				hScrollBar.ThumbDragStarted += dragStartedHandler;
				m_horizontalThumbDragStartedToken.Disposable = global::Uno.Disposables.Disposable.Create(() => hScrollBar.ThumbDragStarted -= dragStartedHandler);

				DragCompletedEventHandler dragCompletedHandler = OnScrollBarThumbDragCompleted;
				hScrollBar.ThumbDragCompleted += dragCompletedHandler;
				m_horizontalThumbDragCompletedToken.Disposable = global::Uno.Disposables.Disposable.Create(() => hScrollBar.ThumbDragCompleted -= dragCompletedHandler);

				Microsoft.UI.Xaml.Input.PointerEventHandler pointerEnteredHandler = OnHorizontalScrollbarPointerEntered;
				hScrollBar.PointerEntered += pointerEnteredHandler;
				m_horizontalScrollbarPointerEnteredToken.Disposable = global::Uno.Disposables.Disposable.Create(() => hScrollBar.PointerEntered -= pointerEnteredHandler);

				Microsoft.UI.Xaml.Input.PointerEventHandler pointerExitedHandler = OnHorizontalScrollbarPointerExited;
				hScrollBar.PointerExited += pointerExitedHandler;
				m_horizontalScrollbarPointerExitedToken.Disposable = global::Uno.Disposables.Disposable.Create(() => hScrollBar.PointerExited -= pointerExitedHandler);

				RefreshScrollBarIsIgnoringUserInput(true /*isForHorizontalOrientation*/);
			}

			if (m_trElementVerticalScrollBar is { } vScrollBar)
			{
				if (m_trElementScrollContentPresenter is { } scp && scp.IsChildActualHeightUsedAsExtent())
				{
					vScrollBar.StartUseOfActualSizeAsExtent();
				}

				ScrollEventHandler scrollHandler = OnVerticalScrollBarScroll;
				vScrollBar.Scroll += scrollHandler;
				m_VerticalScrollToken.Disposable = global::Uno.Disposables.Disposable.Create(() => vScrollBar.Scroll -= scrollHandler);

				DragStartedEventHandler dragStartedHandler = OnScrollBarThumbDragStarted;
				vScrollBar.ThumbDragStarted += dragStartedHandler;
				m_verticalThumbDragStartedToken.Disposable = global::Uno.Disposables.Disposable.Create(() => vScrollBar.ThumbDragStarted -= dragStartedHandler);

				DragCompletedEventHandler dragCompletedHandler = OnScrollBarThumbDragCompleted;
				vScrollBar.ThumbDragCompleted += dragCompletedHandler;
				m_verticalThumbDragCompletedToken.Disposable = global::Uno.Disposables.Disposable.Create(() => vScrollBar.ThumbDragCompleted -= dragCompletedHandler);

				Microsoft.UI.Xaml.Input.PointerEventHandler pointerEnteredHandler = OnVerticalScrollbarPointerEntered;
				vScrollBar.PointerEntered += pointerEnteredHandler;
				m_verticalScrollbarPointerEnteredToken.Disposable = global::Uno.Disposables.Disposable.Create(() => vScrollBar.PointerEntered -= pointerEnteredHandler);

				Microsoft.UI.Xaml.Input.PointerEventHandler pointerExitedHandler = OnVerticalScrollbarPointerExited;
				vScrollBar.PointerExited += pointerExitedHandler;
				m_verticalScrollbarPointerExitedToken.Disposable = global::Uno.Disposables.Disposable.Create(() => vScrollBar.PointerExited -= pointerExitedHandler);

				RefreshScrollBarIsIgnoringUserInput(false /*isForHorizontalOrientation*/);
			}
		}

		// Releases and unhooks template parts and their events.
		internal void UnhookTemplate()
		{
			// Cleanup any existing template parts
			if (m_trElementHorizontalScrollBar is { } hScrollBar)
			{
				// TODO Uno: Phase 4 — call StopUseOfActualSizeAsExtent on the ScrollBar once that helper lands.
				// hScrollBar.StopUseOfActualSizeAsExtent();
				m_HorizontalScrollToken.Disposable = null;
				m_horizontalThumbDragStartedToken.Disposable = null;
				m_horizontalThumbDragCompletedToken.Disposable = null;
				m_horizontalScrollbarPointerEnteredToken.Disposable = null;
				m_horizontalScrollbarPointerExitedToken.Disposable = null;
			}
			if (m_trElementVerticalScrollBar is { } vScrollBar)
			{
				// TODO Uno: Phase 4 — call StopUseOfActualSizeAsExtent on the ScrollBar once that helper lands.
				// vScrollBar.StopUseOfActualSizeAsExtent();
				m_VerticalScrollToken.Disposable = null;
				m_verticalThumbDragStartedToken.Disposable = null;
				m_verticalThumbDragCompletedToken.Disposable = null;
				m_verticalScrollbarPointerEnteredToken.Disposable = null;
				m_verticalScrollbarPointerExitedToken.Disposable = null;
			}
			m_trElementRoot = null;
			m_trElementScrollContentPresenter = null;
			m_trElementHorizontalScrollBar = null;
			m_trElementVerticalScrollBar = null;
			m_tpElementScrollBarSeparator = null;
			m_trLayoutAdjustmentsForOcclusionsStoryboard = null;
		}

		// Scrolls the view in the specified direction.
		internal void ScrollInDirection(
			VirtualKey key,
			bool animate = false)
		{
			if (animate)
			{
				// Let DManip animate the scroll within a ListViewBase header or footer.

				// No special processing is required here for right-to-left scenarios,
				// CInputServices::ProcessInputMessageWithDirectManipulation does it instead.
				// For PageUp/Down, Home and End keys though, that method must ignore the RightToLeft
				// flow direction, otherwise a move to the opposite direction is performed.
				// TODO Uno: ProcessInputMessage forwards the keystroke to the DM service. Until the
				// DM adapter exists, fall through to non-animated scrolling.
				// ProcessInputMessage(key == VirtualKey.PageUp || key == VirtualKey.PageDown ||
				//                     key == VirtualKey.Home  || key == VirtualKey.End,
				//                     out var isHandled);
			}

			{
				var direction = FlowDirection;
				var invert = direction == FlowDirection.RightToLeft;

				switch (key)
				{
					case VirtualKey.Up:
						LineUp();
						break;
					case VirtualKey.Down:
						LineDown();
						break;
					case VirtualKey.Left:
						if (invert)
						{
							LineRight();
						}
						else
						{
							LineLeft();
						}
						break;
					case VirtualKey.Right:
						if (invert)
						{
							LineLeft();
						}
						else
						{
							LineRight();
						}
						break;
					case VirtualKey.PageUp:
						PageUp();
						break;
					case VirtualKey.PageDown:
						PageDown();
						break;
					case VirtualKey.Home:
						PageHome();
						break;
					case VirtualKey.End:
						PageEnd();
						break;
					default:
						// Do nothing
						break;
				}
			}
		}

		// Scrolls the content within the ScrollViewer to the specified
		// horizontal offset position. The public ScrollToHorizontalOffset is
		// declared in the cross-platform ScrollViewer.cs and routes through
		// ChangeView; this Internal variant is the C++ entry-point used by
		// the new port for internal flows that should not touch ChangeView.
		internal void ScrollToHorizontalOffsetInternal(double offset)
			=> HandleHorizontalScroll(ScrollEventType.ThumbPosition, offset);

		// Scrolls the content within the ScrollViewer to the specified vertical
		// offset position. See ScrollToHorizontalOffsetInternal for the public
		// API split rationale.
		internal void ScrollToVerticalOffsetInternal(double offset)
			=> HandleVerticalScroll(ScrollEventType.ThumbPosition, offset);

		// (C++ source line 14325)
		internal void GetTargetView(
			out double pTargetHorizontalOffset,
			out double pTargetVerticalOffset,
			out float pTargetZoomFactor)
		{
			global::System.Diagnostics.Debug.Assert(IsInManipulation);

			// Return the target view of the latest ChangeView request if set.
			if (m_targetChangeViewHorizontalOffset != -1.0)
			{
				// No ViewChanging event was raised since the last ChangeView request.
				global::System.Diagnostics.Debug.Assert(m_targetChangeViewVerticalOffset != -1.0);
				global::System.Diagnostics.Debug.Assert(m_targetChangeViewZoomFactor != -1.0f);
				pTargetHorizontalOffset = m_targetChangeViewHorizontalOffset;
				pTargetVerticalOffset = m_targetChangeViewVerticalOffset;
				pTargetZoomFactor = m_targetChangeViewZoomFactor;
			}
			// Else return the end-of-inertia view if set.
			else if (m_isInertial && m_isInertiaEndTransformValid)
			{
				pTargetHorizontalOffset = m_inertiaEndHorizontalOffset;
				pTargetVerticalOffset = m_inertiaEndVerticalOffset;
				pTargetZoomFactor = m_inertiaEndZoomFactor;
			}
			// Else no target view is available.
			else
			{
				pTargetHorizontalOffset = -1.0;
				pTargetVerticalOffset = -1.0;
				pTargetZoomFactor = -1.0f;
			}
		}

		// (C++ source line 1148)
		internal DependencyObject GetPointedElement()
			=> m_wrPointedElement?.Target as DependencyObject;

		// Sets the m_wrPointedElement field which represents
		// the dependency object touched at the beginning of a
		// touch-based manipulation.
		// (C++ source line 1167)
		internal void PutPointedElement(DependencyObject value)
		{
			if (m_wrPointedElement is not null)
			{
				WeakReferencePool.ReturnWeakReference(this, m_wrPointedElement);
				m_wrPointedElement = null;
			}

			if (value is not null)
			{
				m_wrPointedElement = WeakReferencePool.RentWeakReference(this, value);
			}
		}

		// (C++ source line 3316)
		internal bool ChangeViewImpl(
			double? pHorizontalOffset,
			double? pVerticalOffset,
			float? pZoomFactor)
		{
			return ChangeViewInternal(
				pHorizontalOffset,
				pVerticalOffset,
				pZoomFactor,
				null /*pOldZoomFactor*/,
				false /*forceChangeToCurrentView*/,
				true /*adjustWithMandatorySnapPoints*/,
				true /*skipDuringTouchContact*/,
				true /*skipAnimationWhileRunning*/,
				false /*disableAnimation*/,
				true /*applyAsManip*/,
				false /*transformIsInertiaEnd*/,
				false /*isForMakeVisible*/);
		}

		// Combines the abilities of ScrollToHorizontalOffset, ScrollToVerticalOffset
		// and ZoomToFactor, with the option of animating to the target view and snapping
		// to mandatory snap points.
		// (C++ source line 3355)
		internal bool ChangeViewWithOptionalAnimationImpl(
			double? pHorizontalOffset,
			double? pVerticalOffset,
			float? pZoomFactor,
			bool disableAnimation)
		{
			return ChangeViewInternal(
				pHorizontalOffset,
				pVerticalOffset,
				pZoomFactor,
				null /*pOldZoomFactor*/,
				false /*forceChangeToCurrentView*/,
				true /*adjustWithMandatorySnapPoints*/,
				true /*skipDuringTouchContact*/,
				true /*skipAnimationWhileRunning*/,
				disableAnimation,
				true /*applyAsManip*/,
				false /*transformIsInertiaEnd*/,
				false /*isForMakeVisible*/);
		}

		// Combines the abilities of ScrollToHorizontalOffset, ScrollToVerticalOffset
		// and ZoomToFactor with optional animation, snap-point adjustment, and DM
		// BringIntoViewport coordination.
		// (C++ source line 3385)
		// NOTE: this is the WinUI ChangeViewInternal port. The DM-aware branches
		// (m_canManipulateElementsWithBringIntoViewport / BringIntoViewportInternal /
		// GetDManipView path) are preserved verbatim inside `#if false` blocks per
		// the don't-simplify rule and become live when the Phase-4 DM adapter
		// lands. The non-DM SetOffsetsWithExtents / SetHorizontalOffset /
		// SetVerticalOffset / ZoomToFactorInternal path is the active one on Skia.
		internal bool ChangeViewInternal(
			double? pHorizontalOffset,
			double? pVerticalOffset,
			float? pZoomFactor,
			float? pOldZoomFactor,
			bool forceChangeToCurrentView,
			bool adjustWithMandatorySnapPoints,
			bool skipDuringTouchContact,
			bool skipAnimationWhileRunning,
			bool disableAnimation,
			bool applyAsManip,
			bool transformIsInertiaEnd,
			bool isForMakeVisible)
		{
			bool isHandled = false;
			bool isBringIntoViewportCallAllowed = true;
			bool isBringIntoViewportCalled = false;
			bool isScrollContentPresenterScrollClient = false;
			bool isViewChangingDelayed = false;
			bool isViewChangedDelayed = false;
			bool canManipulateElementsByTouch = false;
			bool canManipulateElementsNonTouch = false;
			bool canManipulateElementsWithBringIntoViewport = false;
			bool canHorizontallyScroll = false;
			bool canVerticallyScroll = false;
			bool clearInChangeViewBringIntoViewport = false;
			double currentUnzoomedPixelExtentWidth = -1.0;
			double currentUnzoomedPixelExtentHeight = -1.0;
			double targetExtentWidth = -1.0;
			double targetExtentHeight = -1.0;
			double viewportWidth = 0.0;
			double viewportHeight = 0.0;
			double viewportPixelWidth = 0.0;
			double viewportPixelHeight = 0.0;
			double currentHorizontalOffset = 0.0;
			double currentVerticalOffset = 0.0;
			double currentTargetHorizontalOffset = 0.0;
			double currentTargetVerticalOffset = 0.0;
			double oldTargetChangeViewHorizontalOffset = -1.0;
			double oldTargetChangeViewVerticalOffset = -1.0;
			double minHorizontalOffset = -1.0;
			double minVerticalOffset = -1.0;
			double maxHorizontalOffset = -1.0;
			double maxVerticalOffset = -1.0;
			double targetHorizontalOffset = 0.0;
			double targetVerticalOffset = 0.0;
			double targetPixelHorizontalOffset = 0.0;
			double targetPixelVerticalOffset = 0.0;
			double adjustedTargetHorizontalOffset = 0.0;
			double adjustedTargetVerticalOffset = 0.0;
			float adjustedTargetZoomFactor = 1.0f;
			float targetZoomFactor = 1.0f;
			float currentZoomFactor = 1.0f;
			float currentTargetZoomFactor = 1.0f;
			float oldTargetChangeViewZoomFactor = -1.0f;
			float minZoomFactor = 0.0f;
			float maxZoomFactor = 0.0f;
			float targetTranslateX = 0.0f;
			float targetTranslateY = 0.0f;
			global::Windows.Foundation.Rect bounds = default;
			DMAlignment alignment = DMAlignment.None;
			UIElement spContentUIElement = null;
			UIElement spScrollInfoAsElement = null;
			object spProvider = null;
			IScrollInfo spScrollInfo = null;
			Orientation orientation = Orientation.Horizontal;
			global::Windows.Foundation.Size sizeFirstVisibleItem = default;

			global::System.Diagnostics.Debug.Assert(!transformIsInertiaEnd || (forceChangeToCurrentView && disableAnimation));

			// The provided pOldZoomFactor value is only used when m_isInZoomFactorSync is True.
			// i.e. when a ZoomToFactor call causes us to push a new content transform to DManip.
			// The value is expected to be nullptr in all other scenarios.
			global::System.Diagnostics.Debug.Assert((pOldZoomFactor == null && !m_isInZoomFactorSync) || (pOldZoomFactor != null && pOldZoomFactor.Value > 0.0f && m_isInZoomFactorSync));

			bool returnValue = false;

			if (!pHorizontalOffset.HasValue && !pVerticalOffset.HasValue && !pZoomFactor.HasValue)
			{
				if (forceChangeToCurrentView)
				{
					// XAML is pushing new transform to DManip.
					m_isInDirectManipulationSync = true;
				}
				else
				{
					// Not a single view characteristic was provided. Do no attempt any view change.
					// Unless forceChangeToCurrentView is True, in which case invoke a BringIntoViewport
					// for the current view to synchronize XAML and DManip.
					goto Cleanup;
				}
			}

			spScrollInfo = GetScrollInfo();
			if (spScrollInfo is null)
			{
				// No IScrollInfo to interact with. Return False.
				goto Cleanup;
			}

			currentHorizontalOffset = HorizontalOffset;
			currentVerticalOffset = VerticalOffset;
			currentZoomFactor = ZoomFactor;

			if (transformIsInertiaEnd && m_targetChangeViewHorizontalOffset != -1.0)
			{
				// This ChangeViewInternal call follows a cancellation of inertia in CInputServices::StopInertialViewport.
				// Store the previous ChangeView target (pixel-based offsets) so it can be used if the newly requested
				// target is slightly different because of roundings required for ZoomToRect.
				oldTargetChangeViewHorizontalOffset = m_targetChangeViewHorizontalOffset;
				oldTargetChangeViewVerticalOffset = m_targetChangeViewVerticalOffset;
				oldTargetChangeViewZoomFactor = m_targetChangeViewZoomFactor;

				// Since inertia was cancelled, the previous ChangeView target is no longer valid.
				// These values can no longer be used for setting the 3 currentTarget*** variables below.
				m_targetChangeViewHorizontalOffset = -1.0;
				m_targetChangeViewVerticalOffset = -1.0;
				m_targetChangeViewZoomFactor = -1.0f;
			}

			// Take into account latest requests for offset and zoom factor changes, if any.
			if (m_isTargetHorizontalOffsetValid)
			{
				currentTargetHorizontalOffset = m_targetHorizontalOffset;
			}
			else if (m_targetChangeViewHorizontalOffset >= 0.0)
			{
				currentTargetHorizontalOffset = m_targetChangeViewHorizontalOffset;
			}
			else
			{
				currentTargetHorizontalOffset = currentHorizontalOffset;
			}

			if (m_isTargetVerticalOffsetValid)
			{
				currentTargetVerticalOffset = m_targetVerticalOffset;
			}
			else if (m_targetChangeViewVerticalOffset >= 0.0)
			{
				currentTargetVerticalOffset = m_targetChangeViewVerticalOffset;
			}
			else
			{
				currentTargetVerticalOffset = currentVerticalOffset;
			}

			if (m_isTargetZoomFactorValid)
			{
				currentTargetZoomFactor = m_targetZoomFactor;
			}
			else if (m_targetChangeViewZoomFactor > 0.0f)
			{
				currentTargetZoomFactor = m_targetChangeViewZoomFactor;
			}
			else
			{
				currentTargetZoomFactor = currentZoomFactor;
			}

			if (!m_canManipulateElementsWithAsyncBringIntoViewport)
			{
				// When a projection is set for instance, and DManip is partially turned off, only perform non-animated view changes.
				disableAnimation = true;
			}

			if (!disableAnimation)
			{
				// Do not attempt to animate when OS Settings have turned off animations.
				disableAnimation = !IsAnimationEnabled;
			}

			spProvider = GetInnerManipulationDataProvider();
			if (spProvider is FrameworkElement spProviderFE)
			{
				// TODO Uno: Phase 5 — IManipulationDataProvider port. PhysicalOrientation
				// is read from the provider; defaults to Horizontal until that lands.

				// When operating with a IManipulationDataProvider implementation, we do not support animations. Pretend the flag was set to False.
				disableAnimation = true;

				if ((orientation == Orientation.Horizontal && pHorizontalOffset.HasValue) ||
					(orientation == Orientation.Vertical && pVerticalOffset.HasValue))
				{
					// Also IManipulationDataProvider does not currently provide a means to compute a pixel-based
					// offset given a logical offset. So DManip ZoomToRect which are pixel-based are skipped in favor
					// of ScrollToHorizontalOffset/ScrollToVerticalOffset/ZoomToFactor calls.
					isBringIntoViewportCallAllowed = false;
				}
				_ = spProviderFE;
			}

			ComputePixelViewportWidth((spProvider is not null && orientation == Orientation.Horizontal) ? spProvider : null, true /*isProviderSet*/, out viewportPixelWidth);
			ComputePixelViewportHeight((spProvider is not null && orientation == Orientation.Vertical) ? spProvider : null, true /*isProviderSet*/, out viewportPixelHeight);

			minHorizontalOffset = spScrollInfo.GetMinHorizontalOffset();
			minVerticalOffset = spScrollInfo.GetMinVerticalOffset();
			minZoomFactor = MinZoomFactor;
			maxZoomFactor = MaxZoomFactor;

			if (pZoomFactor.HasValue)
			{
				targetZoomFactor = pZoomFactor.Value;

				if (float.IsNaN(targetZoomFactor) || float.IsInfinity(targetZoomFactor))
				{
					// Use standard error string "The value cannot be infinite or Not a Number (NaN)."
					throw new global::System.ArgumentException("The value cannot be infinite or Not a Number (NaN).");
				}

				// Clamp the provided value based on MinZoomFactor and MaxZoomFactor
				if (targetZoomFactor < minZoomFactor)
				{
					targetZoomFactor = minZoomFactor;
				}
				else if (targetZoomFactor > maxZoomFactor)
				{
					targetZoomFactor = maxZoomFactor;
				}
			}
			else
			{
				// Use current zoom factor, or latest requested zoom factor, since no target was specified.
				targetZoomFactor = currentTargetZoomFactor;

				// No need to clamp this value based on MinZoomFactor and MaxZoomFactor
				// since the current dependency property is already clamped.
			}

			if (disableAnimation && adjustWithMandatorySnapPoints)
			{
				AdjustZoomFactorWithMandatorySnapPoints(minZoomFactor, maxZoomFactor, ref targetZoomFactor);
			}

			canHorizontallyScroll = spScrollInfo.GetCanHorizontallyScroll();
			// Even when canHorizontallyScroll is False, the current horizontal offset may be greater than minHorizontalOffset.
			// This is because IScrollInfo implementations like ItemsPresenter, VirtualizingStackPanel and WrapGrip allow
			// their SetHorizontalOffset to set the offset to values other than minHorizontalOffset, via the ScrollToHorizontalOffset
			// method. The ScrollContentPresenter however forces the offset to be minHorizontalOffset when it is the IScrollInfo implementer.
			// When forceChangeToCurrentView is True (i.e. ScrollViewer::NotifyBringIntoViewportNeeded is being processed to sync up the XAML
			// and DManip transforms) and the ScrollContentPresenter is not the IScrollInfo implementer (i.e. spProvider is set), the fact that
			// get_CanHorizontallyScroll returned false must be overwritten such that a DManip ZoomToRect call with the current horizontal
			// offset is made.
			canHorizontallyScroll |= forceChangeToCurrentView && spProvider != null;
			if (canHorizontallyScroll)
			{
				viewportWidth = spScrollInfo.GetViewportWidth();
			}
			if (canHorizontallyScroll && !double.IsPositiveInfinity(viewportWidth))
			{
				if (pHorizontalOffset.HasValue)
				{
					targetHorizontalOffset = pHorizontalOffset.Value;
					if (double.IsNaN(targetHorizontalOffset) || double.IsInfinity(targetHorizontalOffset))
					{
						// Use standard error string "The value cannot be infinite or Not a Number (NaN)."
						throw new global::System.ArgumentException("The value cannot be infinite or Not a Number (NaN).");
					}
					if (targetHorizontalOffset < minHorizontalOffset)
					{
						targetHorizontalOffset = minHorizontalOffset;
					}
				}
				else
				{
					// Use current horizontal offset, or latest requested offset, since no target was specified.
					targetHorizontalOffset = currentTargetHorizontalOffset;
				}
				if (spProvider == null || orientation == Orientation.Vertical)
				{
					// ScrollViewer operates with pixel-based horizontal offsets.
					if (disableAnimation || double.IsInfinity((float)(targetHorizontalOffset / targetZoomFactor)))
					{
						// Clamp the pixel-based horizontal offset so it does not exceed the maximum value.
						AdjustTargetHorizontalOffset(
							disableAnimation,
							adjustWithMandatorySnapPoints,
							targetZoomFactor,
							minHorizontalOffset,
							currentHorizontalOffset,
							viewportPixelWidth,
							ref targetHorizontalOffset,
							out currentUnzoomedPixelExtentWidth,
							out maxHorizontalOffset,
							out targetExtentWidth);
					}
					targetPixelHorizontalOffset = targetHorizontalOffset;
				}
				else
				{
					// ScrollViewer operates with logical-based horizontal offsets.
					global::System.Diagnostics.Debug.Assert(spProvider != null && orientation == Orientation.Horizontal);
					global::System.Diagnostics.Debug.Assert(disableAnimation);

					if (isBringIntoViewportCallAllowed)
					{
						// The horizontal offset cannot be altered since the panel operates in logical-based units.
						targetPixelHorizontalOffset = (m_xPixelOffsetRequested == -1) ? m_xPixelOffset : m_xPixelOffsetRequested;

						if (m_isInZoomFactorSync)
						{
							// For horizontal virtualizing panels that use logical-based offsets, the pixel-based target horizontal
							// offset needs to be adjusted based on the zoom factor change. The logical offset is unchanged though.
							targetPixelHorizontalOffset *= targetZoomFactor / pOldZoomFactor.Value;
						}
					}

					if (adjustWithMandatorySnapPoints)
					{
						// Make sure the logical offset snaps to an integer if near mandatory scroll snap points are effective.
						AdjustLogicalOffsetWithMandatorySnapPoints(
							true /*isForHorizontalOffset*/,
							ref targetHorizontalOffset);
					}
				}
			}
			else
			{
				// canHorizontallyScroll == False or viewportWidth == DoubleUtil::PositiveInfinity. No matter the requested horizontal offset, it is assumed to be minHorizontalOffset.
				maxHorizontalOffset = minHorizontalOffset;
				targetHorizontalOffset = minHorizontalOffset;
				if (spProvider == null || orientation == Orientation.Vertical)
				{
					// ScrollViewer operates with pixel-based horizontal offsets.
					targetPixelHorizontalOffset = minHorizontalOffset;
				}
			}

			canVerticallyScroll = spScrollInfo.GetCanVerticallyScroll();
			canVerticallyScroll |= forceChangeToCurrentView && spProvider != null;
			if (canVerticallyScroll)
			{
				viewportHeight = spScrollInfo.GetViewportHeight();
			}
			if (canVerticallyScroll && !double.IsPositiveInfinity(viewportHeight))
			{
				if (pVerticalOffset.HasValue)
				{
					targetVerticalOffset = pVerticalOffset.Value;
					if (double.IsNaN(targetVerticalOffset) || double.IsInfinity(targetVerticalOffset))
					{
						throw new global::System.ArgumentException("The value cannot be infinite or Not a Number (NaN).");
					}
					if (targetVerticalOffset < minVerticalOffset)
					{
						targetVerticalOffset = minVerticalOffset;
					}
				}
				else
				{
					// Use current horizontal offset, or latest requested offset, since no target was specified.
					targetVerticalOffset = currentTargetVerticalOffset;
				}
				if (spProvider == null || orientation == Orientation.Horizontal)
				{
					// Clamp the pixel-based vertical offset so it does not exceed the maximum value.
					AdjustTargetVerticalOffset(
						disableAnimation,
						adjustWithMandatorySnapPoints,
						targetZoomFactor,
						minVerticalOffset,
						currentVerticalOffset,
						viewportPixelHeight,
						ref targetVerticalOffset,
						out currentUnzoomedPixelExtentHeight,
						out maxVerticalOffset,
						out targetExtentHeight);
					targetPixelVerticalOffset = targetVerticalOffset;
				}
				else
				{
					// ScrollViewer operates with logical-based vertical offsets.
					global::System.Diagnostics.Debug.Assert(spProvider != null && orientation == Orientation.Vertical);
					global::System.Diagnostics.Debug.Assert(disableAnimation);

					if (isBringIntoViewportCallAllowed)
					{
						// The vertical offset cannot be altered since the panel operates in logical-based units.
						targetPixelVerticalOffset = (m_yPixelOffsetRequested == -1) ? m_yPixelOffset : m_yPixelOffsetRequested;

						if (m_isInZoomFactorSync)
						{
							// For vertical virtualizing panels that use logical-based offsets, the pixel-based target vertical
							// offset needs to be adjusted based on the zoom factor change. The logical offset is unchanged though.
							targetPixelVerticalOffset *= targetZoomFactor / pOldZoomFactor.Value;
						}
					}

					if (adjustWithMandatorySnapPoints)
					{
						// Make sure the logical offset snaps to an integer if near mandatory scroll snap points are effective.
						AdjustLogicalOffsetWithMandatorySnapPoints(
							false /*isForHorizontalOffset*/,
							ref targetVerticalOffset);
					}
				}
			}
			else
			{
				// canVerticallyScroll == False or viewportHeight == DoubleUtil::PositiveInfinity. No matter the requested horizontal offset, it is assumed to be minVerticalOffset.
				maxVerticalOffset = minVerticalOffset;
				targetVerticalOffset = minVerticalOffset;
				if (spProvider == null || orientation == Orientation.Horizontal)
				{
					// ScrollViewer operates with pixel-based vertical offsets.
					targetPixelVerticalOffset = minVerticalOffset;
				}
			}

			if (transformIsInertiaEnd &&
				Math.Abs(oldTargetChangeViewHorizontalOffset - targetPixelHorizontalOffset) < ScrollViewerScrollRoundingToleranceForBringIntoViewport &&
				Math.Abs(oldTargetChangeViewVerticalOffset - targetPixelVerticalOffset) < ScrollViewerScrollRoundingToleranceForBringIntoViewport &&
				Math.Abs(oldTargetChangeViewZoomFactor - targetZoomFactor) < ScrollViewerZoomRoundingToleranceForBringIntoViewport)
			{
				// Because of roundings required for calling the DManip ZoomToRect method, the end-of-inertia transform returned by DManip might not exactly match
				// the target of the previous ChangeViewInternal call. Use the original target in order to land exactly where the original ChangeViewInternal caller
				// intended to. This is feasible since disableAnimation is True and DManip's SetContentTransformValues is now invoked instead of ZoomToRect.
				targetPixelHorizontalOffset = targetHorizontalOffset = oldTargetChangeViewHorizontalOffset;
				targetPixelVerticalOffset = targetVerticalOffset = oldTargetChangeViewVerticalOffset;
				targetZoomFactor = oldTargetChangeViewZoomFactor;
			}

			if (DoubleUtil.AreClose(currentTargetHorizontalOffset, targetHorizontalOffset) &&
				DoubleUtil.AreClose(currentTargetVerticalOffset, targetVerticalOffset) &&
				DoubleUtil.AreClose(currentTargetZoomFactor, targetZoomFactor) &&
				!forceChangeToCurrentView)
			{
				// Target view is the current or imminent view
				if (!disableAnimation)
				{
					global::System.Diagnostics.Debug.Assert(spProvider == null);

					// Check if the target view is illegal from a mandatory snap points respect.
					adjustedTargetHorizontalOffset = targetHorizontalOffset;
					adjustedTargetVerticalOffset = targetVerticalOffset;
					adjustedTargetZoomFactor = targetZoomFactor;

					AdjustViewWithMandatorySnapPoints(
						minHorizontalOffset,
						maxHorizontalOffset,
						currentHorizontalOffset,
						minVerticalOffset,
						maxVerticalOffset,
						currentVerticalOffset,
						minZoomFactor,
						maxZoomFactor,
						viewportPixelWidth,
						viewportPixelHeight,
						ref currentUnzoomedPixelExtentWidth,
						ref currentUnzoomedPixelExtentHeight,
						ref adjustedTargetHorizontalOffset,
						ref adjustedTargetVerticalOffset,
						ref adjustedTargetZoomFactor);

					if (DoubleUtil.AreClose(currentTargetHorizontalOffset, adjustedTargetHorizontalOffset) &&
						DoubleUtil.AreClose(currentTargetVerticalOffset, adjustedTargetVerticalOffset) &&
						DoubleUtil.AreClose(currentTargetZoomFactor, adjustedTargetZoomFactor))
					{
						// Current or imminent view would be unaffected by mandatory snap points.
						if (currentTargetZoomFactor != adjustedTargetZoomFactor)
						{
							// Since the current view is so close to the target view, move directly to it instead of animating.
							disableAnimation = true;
							// Use the adjusted view based on the mandatory snap points.
							targetHorizontalOffset = adjustedTargetHorizontalOffset;
							targetVerticalOffset = adjustedTargetVerticalOffset;
							targetZoomFactor = adjustedTargetZoomFactor;
						}
						else
						{
							// The zoom factor is not changing at all and offsets are close. No need to perform a view change.
							goto Cleanup;
						}
					}
				}
				else if (currentTargetZoomFactor == targetZoomFactor)
				{
					// The zoom factor is not changing at all and offsets are close. No need to perform a view change.
					goto Cleanup;
				}
			}

			if (disableAnimation && currentTargetZoomFactor == targetZoomFactor && !forceChangeToCurrentView)
			{
				// When only offset jumps are requested, do not use DManip's ZoomToRect
				// to avoid flickers when the developer changes both an extent and offset.
				// Instead use SetOffsetsWithExtents, ScrollToHorizontalOffset or
				// ScrollToVerticalOffset which results in DManip's SetContentRect and no
				// visual flicker.
				isBringIntoViewportCallAllowed = false;
			}

#if false // TODO Uno: Phase 4 — DM adapter. The C++ block at lines 3953-4259 invokes
		  // get_CanManipulateElements + BringIntoViewportInternal to drive an animated
		  // DManip view change. The non-DM fallback below is the active path on Skia.
			if (isBringIntoViewportCallAllowed)
			{
				get_CanManipulateElements(
					out canManipulateElementsByTouch,
					out canManipulateElementsNonTouch,
					out canManipulateElementsWithBringIntoViewport);

				if (canManipulateElementsWithBringIntoViewport && (!applyAsManip || m_canManipulateElementsWithAsyncBringIntoViewport))
				{
					// ... (DM-aware path: BringIntoViewportInternal call, DManip view snapshotting,
					// targetTranslate computation via ComputeTranslation[X/Y]Correction, etc.) ...
				}
			}
#endif

			if (!isBringIntoViewportCalled || !isHandled)
			{
				// This ScrollViewer or its Content might not be in the tree.
				global::System.Diagnostics.Debug.Assert(spScrollInfo is not null);
				spContentUIElement = GetContentUIElement();
				if (spContentUIElement is not null)
				{
					// Do not attempt a non-animated move to the requested view if there is no Content present.
					// This is to minimize the discrepancy between ZoomToFactor and ScrollToHorizontalOffset/
					// ScrollToVerticalOffset: ZoomToFactor is effective even when there is no Content, while
					// ScrollToHorizontalOffset/ScrollToVerticalOffset are not.

					// Batch up any potential ViewChanging and ViewChanged notifications resulting from calls to ZoomToFactor,
					// SetOffsetsWithExtents, SetHorizontalOffset and SetVerticalOffset into a single notification.
					if (!isViewChangingDelayed)
					{
						DelayViewChanging();
						isViewChangingDelayed = true;
					}
					if (!isViewChangedDelayed)
					{
						DelayViewChanged();
						isViewChangedDelayed = true;
					}

					spScrollInfoAsElement = GetScrollInfoAsElement();
					isScrollContentPresenterScrollClient = IsScrollContentPresenterScrollClient();

					if (!disableAnimation && adjustWithMandatorySnapPoints)
					{
						AdjustViewWithMandatorySnapPoints(
							minHorizontalOffset,
							maxHorizontalOffset,
							currentHorizontalOffset,
							minVerticalOffset,
							maxVerticalOffset,
							currentVerticalOffset,
							minZoomFactor,
							maxZoomFactor,
							viewportPixelWidth,
							viewportPixelHeight,
							ref currentUnzoomedPixelExtentWidth,
							ref currentUnzoomedPixelExtentHeight,
							ref targetHorizontalOffset,
							ref targetVerticalOffset,
							ref targetZoomFactor);
					}

					bool zoomChanged = false;

					// First take care of the potential zoom factor change so offsets can be adjusted accordingly.
					ZoomToFactorInternal(targetZoomFactor, true /*delayAndFlushViewChanged*/, out zoomChanged);

					if (isScrollContentPresenterScrollClient)
					{
						if (m_trElementScrollContentPresenter is not null)
						{
							// Jump to the target offsets
							m_trElementScrollContentPresenter.SetOffsetsWithExtents(targetHorizontalOffset, targetVerticalOffset, currentUnzoomedPixelExtentWidth * targetZoomFactor, currentUnzoomedPixelExtentHeight * targetZoomFactor);
						}
					}
					else
					{
						// Make sure the latest zoom factor gets applied so the SetHorizontalOffset/SetVerticalOffset
						// can operate with the accurate zoom factor.
						// We can skip this if the zoom factor didn't actually change.
						if (spScrollInfoAsElement is not null && zoomChanged)
						{
							spScrollInfoAsElement.UpdateLayout();
						}

						// Jump to the target offsets
						spScrollInfo.SetHorizontalOffset(targetHorizontalOffset);
						spScrollInfo.SetVerticalOffset(targetVerticalOffset);
					}

					global::System.Diagnostics.Debug.Assert(isViewChangingDelayed);
					isViewChangingDelayed = false;
					FlushViewChanging();

					global::System.Diagnostics.Debug.Assert(isViewChangedDelayed);
					isViewChangedDelayed = false;
					FlushViewChanged();

					if (IsInManipulation && spScrollInfoAsElement is not null)
					{
						// Make sure the offset change request gets applied through a ScrollViewer.InvalidateScrollInfo call
						// whenever there is an active manipulation, so the next DManip feedback does not override the changes
						// requested. This situation occurs for instance when DManip's ZoomToRect was skipped because of a touch contact.
						// A non-animated move was accomplished instead.
						global::System.Diagnostics.Debug.Assert(!isHandled);
						spScrollInfoAsElement.UpdateLayout();
					}
					isHandled = true;
				}
			}

			returnValue = isHandled;

		Cleanup:
			m_isInDirectManipulationSync = false;
			if (isViewChangingDelayed)
			{
				FlushViewChanging();
			}
			if (isViewChangedDelayed)
			{
				FlushViewChanged();
			}
			if (clearInChangeViewBringIntoViewport)
			{
				global::System.Diagnostics.Debug.Assert(m_isInChangeViewBringIntoViewport);
				m_isInChangeViewBringIntoViewport = false;
			}

			// Suppress unused-variable warnings for stub-only locals (used only by the
			// `#if false` DM block above).
			_ = canManipulateElementsByTouch;
			_ = canManipulateElementsNonTouch;
			_ = canManipulateElementsWithBringIntoViewport;
			_ = clearInChangeViewBringIntoViewport;
			_ = bounds;
			_ = alignment;
			_ = targetTranslateX;
			_ = targetTranslateY;
			_ = targetExtentWidth;
			_ = targetExtentHeight;
			_ = sizeFirstVisibleItem;
			_ = adjustedTargetZoomFactor;
			_ = isBringIntoViewportCalled;
			_ = skipAnimationWhileRunning;
			_ = applyAsManip;
			_ = isForMakeVisible;
			_ = skipDuringTouchContact;

			return returnValue;
		}

		// Scroll content by one page to the left.
		internal void PageLeft() => HandleHorizontalScroll(ScrollEventType.LargeDecrement);

		// Scroll content by one line to the left.
		internal void LineLeft() => HandleHorizontalScroll(ScrollEventType.SmallDecrement);

		// Scroll content by one line to the right.
		internal void LineRight() => HandleHorizontalScroll(ScrollEventType.SmallIncrement);

		// Scroll content by one page to the right.
		internal void PageRight() => HandleHorizontalScroll(ScrollEventType.LargeIncrement);

		// Scroll content by one page to the top.
		internal void PageUp() => HandleVerticalScroll(ScrollEventType.LargeDecrement);

		// Scroll content by one line to the top.
		internal void LineUp() => HandleVerticalScroll(ScrollEventType.SmallDecrement);

		// Scroll content by one line to the bottom.
		internal void LineDown() => HandleVerticalScroll(ScrollEventType.SmallIncrement);

		// Scroll content by one page to the bottom.
		internal void PageDown() => HandleVerticalScroll(ScrollEventType.LargeIncrement);

		// Scroll content to the beginning.
		internal void PageHome() => HandleHorizontalScroll(ScrollEventType.First);

		// Scroll content to the end.
		internal void PageEnd() => HandleHorizontalScroll(ScrollEventType.Last);

		// PointerEntered event handler.
		protected override void OnPointerEntered(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs pArgs)
		{
			// Don't process the pointer input when IHM is hidden on the root SV
			if (IsRootScrollViewer() && !IsInputPaneShow())
			{
				return;
			}

			base.OnPointerEntered(pArgs);

			var spPointer = pArgs.Pointer;
			var pointerDeviceType = spPointer.PointerDeviceType;

			// Mouse input dominates. If we are showing panning indicators and then mouse comes into play, mouse indicators win.
			if (Microsoft.UI.Input.PointerDeviceType.Touch != pointerDeviceType)
			{
				m_preferMouseIndicators = true;
				ShowIndicators();
			}
		}

		// PointerMoved event handler.
		protected override void OnPointerMoved(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs pArgs)
		{
			// Don't process the pointer input on the root SV
			if (IsRootScrollViewer())
			{
				return;
			}

			base.OnPointerMoved(pArgs);

			// TODO Uno: PointerRoutedEventArgs.IsGenerated isn't a public API on Uno; the WinUI source skips
			// generated replays. The cross-platform path will route generated pointer events normally — likely
			// fine since indicator behaviour is purely cosmetic here.

			var spPointer = pArgs.Pointer;
			var pointerDeviceType = spPointer.PointerDeviceType;

			// Mouse input dominates. If we are showing panning indicators and then mouse comes into play, mouse indicators win.
			if (Microsoft.UI.Input.PointerDeviceType.Touch != pointerDeviceType)
			{
				m_preferMouseIndicators = true;
				ShowIndicators();
			}
		}

		// PointerExited event handler.
		protected override void OnPointerExited(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs pArgs)
		{
			// Don't process the pointer input on the root SV
			if (IsRootScrollViewer())
			{
				return;
			}

			base.OnPointerExited(pArgs);

			var spPointer = pArgs.Pointer;
			var pointerDeviceType = spPointer.PointerDeviceType;

			// Mouse input dominates. If we are showing panning indicators and then mouse comes into play, mouse indicators win.
			if (Microsoft.UI.Input.PointerDeviceType.Touch != pointerDeviceType)
			{
				m_isPointerOverVerticalScrollbar = false;
				m_isPointerOverHorizontalScrollbar = false;
				m_preferMouseIndicators = true;
				ShowIndicators();
			}
		}

		// Gets the horizontal extent in pixels even for logical scrolling scenarios.
		// (C++ source line 5607)
		internal void ComputePixelExtentWidth(
			bool ignoreZoomFactor,
			object pProvider,
			out double pValue)
		{
			pValue = 0.0;

			if (pProvider is not null)
			{
				// TODO Uno: Phase 5 — IManipulationDataProvider integration for
				// virtualizing panels. pProvider->ComputePixelExtent(...) goes here.
				pValue = ExtentWidth;
			}
			else
			{
				pValue = ExtentWidth;
				if (ignoreZoomFactor && m_trElementScrollContentPresenter is not null)
				{
					pValue /= m_trElementScrollContentPresenter.GetLastZoomFactorApplied();
				}
			}
		}

		// Gets the horizontal extent in pixels even for logical scrolling scenarios.
		// Overload that attempts to determine the potential inner horizontal IManipulationDataProvider.
		// (C++ source line 5635)
		internal void ComputePixelExtentWidth(out double pValue)
		{
			var spProvider = GetInnerManipulationDataProvider(true /*isForHorizontalOrientation*/);
			ComputePixelExtentWidth(false /*ignoreZoomFactor*/, spProvider, out pValue);
		}

		// Gets the vertical extent in pixels even for logical scrolling scenarios.
		// (C++ source line 5652)
		internal void ComputePixelExtentHeight(
			bool ignoreZoomFactor,
			object pProvider,
			out double pValue)
		{
			pValue = 0.0;

			if (pProvider is not null)
			{
				// TODO Uno: Phase 5 — IManipulationDataProvider integration.
				pValue = ExtentHeight;
			}
			else
			{
				pValue = ExtentHeight;
				if (ignoreZoomFactor && m_trElementScrollContentPresenter is not null)
				{
					pValue /= m_trElementScrollContentPresenter.GetLastZoomFactorApplied();
				}
			}
		}

		// Gets the vertical extent in pixels even for logical scrolling scenarios.
		// Overload that attempts to determine the potential inner vertical IManipulationDataProvider.
		// (C++ source line 5680)
		internal void ComputePixelExtentHeight(out double pValue)
		{
			var spProvider = GetInnerManipulationDataProvider(false /*isForHorizontalOrientation*/);
			ComputePixelExtentHeight(false /*ignoreZoomFactor*/, spProvider, out pValue);
		}

		// Gets the viewport width in pixels even for logical scrolling scenarios.
		// When isProviderSet is True, the provided pProvider param is valid even when NULL.
		// (C++ source line 5698)
		internal void ComputePixelViewportWidth(
			object pProvider,
			bool isProviderSet,
			out double pValue)
		{
			pValue = 0.0;
			object spProviderLocal = pProvider;
			double viewportWidth = 0.0;

			if (spProviderLocal is null && !isProviderSet)
			{
				spProviderLocal = GetInnerManipulationDataProvider(true /*isForHorizontalOrientation*/);
			}

			if (spProviderLocal is FrameworkElement spFE)
			{
				viewportWidth = spFE.ActualWidth;
			}
			else
			{
				viewportWidth = ViewportWidth;
				if (double.IsPositiveInfinity(viewportWidth) && m_trElementScrollContentPresenter is not null)
				{
					// Since IScrollInfo reports an infinite horizontal viewport, fallback to using the
					// m_trElementScrollContentPresenter's width
					viewportWidth = m_trElementScrollContentPresenter.ActualWidth;
				}
			}

			pValue = viewportWidth;
		}

		// Gets the viewport height in pixels even for logical scrolling scenarios.
		// When isProviderSet is True, the provided pProvider param is valid even when NULL.
		// (C++ source line 5741)
		internal void ComputePixelViewportHeight(
			object pProvider,
			bool isProviderSet,
			out double pValue)
		{
			pValue = 0.0;
			double viewportHeight = 0.0;
			object spProviderLocal = pProvider;

			if (spProviderLocal is null && !isProviderSet)
			{
				spProviderLocal = GetInnerManipulationDataProvider(false /*isForHorizontalOrientation*/);
			}

			if (spProviderLocal is FrameworkElement spFE)
			{
				viewportHeight = spFE.ActualHeight;
			}
			else
			{
				viewportHeight = ViewportHeight;
				if (double.IsPositiveInfinity(viewportHeight) && m_trElementScrollContentPresenter is not null)
				{
					// Since IScrollInfo reports an infinite vertical viewport, fallback to using the
					// m_trElementScrollContentPresenter's height
					viewportHeight = m_trElementScrollContentPresenter.ActualHeight;
				}
			}

			pValue = viewportHeight;
		}

		// Gets the value of the scrollable width of the content in pixels even for logical scrolling scenarios.
		// (C++ source line 5783)
		internal void ComputePixelScrollableWidth(
			object pProvider,
			out double pixelScrollableWidth)
		{
			pixelScrollableWidth = 0.0;

			ComputePixelExtentWidth(false /*ignoreZoomFactor*/, pProvider, out var extent);
			ComputePixelViewportWidth(pProvider, true /*isProviderSet*/, out var viewport);

			pixelScrollableWidth = Math.Max(0.0, extent - viewport);
		}

		// Gets the value of the scrollable height of the content in pixels even for logical scrolling scenarios.
		// (C++ source line 5805)
		internal void ComputePixelScrollableHeight(
			object pProvider,
			out double pixelScrollableHeight)
		{
			pixelScrollableHeight = 0.0;

			ComputePixelExtentHeight(false /*ignoreZoomFactor*/, pProvider, out var extent);
			ComputePixelViewportHeight(pProvider, true /*isProviderSet*/, out var viewport);

			pixelScrollableHeight = Math.Max(0.0, extent - viewport);
		}

		// Ensures the target horizontal offset for a ChangeView request does
		// not exceed the maximum offset given the target zoom factor, viewport
		// width, extent width and optionally the mandatory scroll snap points.
		// (C++ source line 12164)
		internal void AdjustTargetHorizontalOffset(
			bool disableAnimation,
			bool adjustWithMandatorySnapPoints,
			float targetZoomFactor,
			double minHorizontalOffset,
			double currentHorizontalOffset,
			double viewportPixelWidth,
			ref double pTargetHorizontalOffset,
			out double pCurrentUnzoomedPixelExtentWidth,
			out double pMaxHorizontalOffset,
			out double pTargetExtentWidth)
		{
			double targetHorizontalOffset = pTargetHorizontalOffset;
			double currentUnzoomedPixelExtentWidth = 0.0;
			double maxHorizontalOffset = 0.0;
			double targetExtentWidth = 0.0;

			pCurrentUnzoomedPixelExtentWidth = 0.0;
			pMaxHorizontalOffset = 0.0;
			pTargetExtentWidth = 0.0;

			// Compute current extent width
			ComputePixelExtentWidth(true /*ignoreZoomFactor*/, null /*pProvider*/, out currentUnzoomedPixelExtentWidth);

			// Compute expected extent width
			targetExtentWidth = currentUnzoomedPixelExtentWidth * targetZoomFactor;

			// Compute expected max offset
			maxHorizontalOffset = Math.Max(minHorizontalOffset, targetExtentWidth - viewportPixelWidth);

			// Clamp target offset with expected max offset
			if (targetHorizontalOffset > maxHorizontalOffset)
			{
				targetHorizontalOffset = maxHorizontalOffset;
			}

			// Adjust offset with mandatory scroll snap points
			if (disableAnimation && adjustWithMandatorySnapPoints)
			{
				// TODO Uno: Phase 5 — RefreshScrollSnapPointsInfo + AdjustOffsetWithMandatorySnapPoints
				// chain port. The C++ does:
				// if (!m_trScrollSnapPointsInfo) { IFC_RETURN(RefreshScrollSnapPointsInfo()); }
				// IFC_RETURN(AdjustOffsetWithMandatorySnapPoints(...));
				// Our existing SnapPoints.cs has an AdjustOffsetWithMandatorySnapPoints helper
				// but with a different signature; reconcile in Phase 5.
			}

			pTargetHorizontalOffset = targetHorizontalOffset;
			pCurrentUnzoomedPixelExtentWidth = currentUnzoomedPixelExtentWidth;
			pMaxHorizontalOffset = maxHorizontalOffset;
			pTargetExtentWidth = targetExtentWidth;
		}

		// Ensures the target vertical offset for a ChangeView request does
		// not exceed the maximum offset given the target zoom factor, viewport
		// height, extent height and optionally the mandatory scroll snap points.
		// (C++ source line 12230)
		internal void AdjustTargetVerticalOffset(
			bool disableAnimation,
			bool adjustWithMandatorySnapPoints,
			float targetZoomFactor,
			double minVerticalOffset,
			double currentVerticalOffset,
			double viewportPixelHeight,
			ref double pTargetVerticalOffset,
			out double pCurrentUnzoomedPixelExtentHeight,
			out double pMaxVerticalOffset,
			out double pTargetExtentHeight)
		{
			double targetVerticalOffset = pTargetVerticalOffset;
			double currentUnzoomedPixelExtentHeight = 0.0;
			double maxVerticalOffset = 0.0;
			double targetExtentHeight = 0.0;

			pCurrentUnzoomedPixelExtentHeight = 0.0;
			pMaxVerticalOffset = 0.0;
			pTargetExtentHeight = 0.0;

			// Compute current extent height
			ComputePixelExtentHeight(true /*ignoreZoomFactor*/, null /*pProvider*/, out currentUnzoomedPixelExtentHeight);

			// Compute expected extent height
			targetExtentHeight = currentUnzoomedPixelExtentHeight * targetZoomFactor;

			// Compute expected max offset
			maxVerticalOffset = Math.Max(minVerticalOffset, targetExtentHeight - viewportPixelHeight);

			// Clamp target offset with expected max offset
			if (targetVerticalOffset > maxVerticalOffset)
			{
				targetVerticalOffset = maxVerticalOffset;
			}

			// Adjust offset with mandatory scroll snap points
			if (disableAnimation && adjustWithMandatorySnapPoints)
			{
				// TODO Uno: Phase 5 — see AdjustTargetHorizontalOffset.
			}

			pTargetVerticalOffset = targetVerticalOffset;
			pCurrentUnzoomedPixelExtentHeight = currentUnzoomedPixelExtentHeight;
			pMaxVerticalOffset = maxVerticalOffset;
			pTargetExtentHeight = targetExtentHeight;
		}

		// Returns the value of ZoomSnapPoints which can be null if uninitialized.
		// Mirrors the C++ m_spZoomSnapPoints field accessor pattern. Note that
		// ZoomSnapPoints is currently NotImplemented on Skia at the public-API
		// level (the DP is registered, but reads return null until snap-points
		// land on Skia), so the AdjustZoomFactorWithMandatorySnapPoints loop
		// below stays a no-op until that lands.
		private global::System.Collections.Generic.IList<float> GetZoomSnapPoints()
			=> GetValue(ZoomSnapPointsProperty) as global::System.Collections.Generic.IList<float>;

		// Adjusts the provided target zoom factor based on potential mandatory
		// zoom snap points.
		// (C++ source line 12550)
		internal void AdjustZoomFactorWithMandatorySnapPoints(
			float minZoomFactor,
			float maxZoomFactor,
			ref float pTargetZoomFactor)
		{
			float adjustedZoomFactor = pTargetZoomFactor;
			double smallestDistance = double.MaxValue;
			SnapPointsType snapPointsType;
			var zoomSnapPoints = GetZoomSnapPoints();

			if (zoomSnapPoints is not null)
			{
				snapPointsType = ZoomSnapPointsType;

				if (snapPointsType == SnapPointsType.MandatorySingle ||
					snapPointsType == SnapPointsType.Mandatory)
				{
					// Pick the mandatory zoom snap point closest to *pTargetZoomFactor
					if (zoomSnapPoints.Count > 0)
					{
						foreach (var zoomSnapPoint in zoomSnapPoints)
						{
							if (zoomSnapPoint >= minZoomFactor && zoomSnapPoint <= maxZoomFactor &&
								Math.Abs(zoomSnapPoint - pTargetZoomFactor) < smallestDistance)
							{
								smallestDistance = Math.Abs(zoomSnapPoint - pTargetZoomFactor);
								adjustedZoomFactor = zoomSnapPoint;
							}
						}
						pTargetZoomFactor = adjustedZoomFactor;
					}
				}
			}
		}

		// Adjusts the provided target HorizontalOffset, target VerticalOffset and target
		// ZoomFactor based on potential mandatory scroll and zoom snap points.
		// The provided min & max offsets and factor guarantee that the adjustment remains
		// within the allowed boundaries.
		// (C++ source line 12623)
		internal void AdjustViewWithMandatorySnapPoints(
			double minHorizontalOffset,
			double maxHorizontalOffset,
			double currentHorizontalOffset,
			double minVerticalOffset,
			double maxVerticalOffset,
			double currentVerticalOffset,
			float minZoomFactor,
			float maxZoomFactor,
			double viewportPixelWidth,
			double viewportPixelHeight,
			ref double pCurrentUnzoomedPixelExtentWidth,
			ref double pCurrentUnzoomedPixelExtentHeight,
			ref double pTargetHorizontalOffset,
			ref double pTargetVerticalOffset,
			ref float pTargetZoomFactor)
		{
			double targetExtentWidth = 0.0;
			double targetExtentHeight = 0.0;

			global::System.Diagnostics.Debug.Assert(viewportPixelWidth > 0);
			global::System.Diagnostics.Debug.Assert(viewportPixelHeight > 0);

			// Adjust the target zoom factor based on potential mandatory zoom snap points
			AdjustZoomFactorWithMandatorySnapPoints(minZoomFactor, maxZoomFactor, ref pTargetZoomFactor);

			// Compute expected extent width
			if (pCurrentUnzoomedPixelExtentWidth == -1.0)
			{
				ComputePixelExtentWidth(true /*ignoreZoomFactor*/, null /*pProvider*/, out pCurrentUnzoomedPixelExtentWidth);
			}
			global::System.Diagnostics.Debug.Assert(pCurrentUnzoomedPixelExtentWidth != -1.0);
			targetExtentWidth = pCurrentUnzoomedPixelExtentWidth * pTargetZoomFactor;

			// ...and expected extent height
			if (pCurrentUnzoomedPixelExtentHeight == -1.0)
			{
				ComputePixelExtentHeight(true /*ignoreZoomFactor*/, null /*pProvider*/, out pCurrentUnzoomedPixelExtentHeight);
			}
			global::System.Diagnostics.Debug.Assert(pCurrentUnzoomedPixelExtentHeight != -1.0);
			targetExtentHeight = pCurrentUnzoomedPixelExtentHeight * pTargetZoomFactor;

			// Compute expected max horizontal offset
			if (maxHorizontalOffset == -1.0)
			{
				maxHorizontalOffset = Math.Max(minHorizontalOffset, targetExtentWidth - viewportPixelWidth);
			}

			// ... and expected max vertical offset
			if (maxVerticalOffset == -1.0)
			{
				maxVerticalOffset = Math.Max(minVerticalOffset, targetExtentHeight - viewportPixelHeight);
			}

			global::System.Diagnostics.Debug.Assert(minHorizontalOffset >= 0.0);
			global::System.Diagnostics.Debug.Assert(minVerticalOffset >= 0.0);
			global::System.Diagnostics.Debug.Assert(maxHorizontalOffset >= 0.0);
			global::System.Diagnostics.Debug.Assert(maxVerticalOffset >= 0.0);
			global::System.Diagnostics.Debug.Assert(targetExtentWidth >= 0.0);
			global::System.Diagnostics.Debug.Assert(targetExtentHeight >= 0.0);

			// Adjust the target offsets based on potential mandatory scroll snap points
			AdjustOffsetWithMandatorySnapPoints(
				true /*isForHorizontalOffset*/,
				minHorizontalOffset,
				maxHorizontalOffset,
				currentHorizontalOffset,
				targetExtentWidth /*targetExtentDim*/,
				viewportPixelWidth,
				pTargetZoomFactor,
				ref pTargetHorizontalOffset);

			AdjustOffsetWithMandatorySnapPoints(
				false /*isForHorizontalOffset*/,
				minVerticalOffset,
				maxVerticalOffset,
				currentVerticalOffset,
				targetExtentHeight /*targetExtentDim*/,
				viewportPixelHeight,
				pTargetZoomFactor,
				ref pTargetVerticalOffset);
		}

		// (C++ source line 9044)
		internal void CanScrollForFocusNavigation(VirtualKey key,
			Microsoft.UI.Xaml.Input.FocusNavigationDirection direction,
			out bool canScroll)
		{
			// check if we can scroll
			// Check if current key should trigger chaining
			bool continueRouting = ShouldContinueRoutingKeyDownEvent(key);

			canScroll = !continueRouting;
			if (canScroll)
			{
				ScrollMode scrollMode = ScrollMode.Disabled;

				switch (direction)
				{
					case Microsoft.UI.Xaml.Input.FocusNavigationDirection.Up:
					case Microsoft.UI.Xaml.Input.FocusNavigationDirection.Down:
						scrollMode = VerticalScrollMode;
						break;

					case Microsoft.UI.Xaml.Input.FocusNavigationDirection.Left:
					case Microsoft.UI.Xaml.Input.FocusNavigationDirection.Right:
						scrollMode = HorizontalScrollMode;
						break;
				}

				// if scroll mode is disabled, we cannot scroll
				canScroll = (scrollMode != ScrollMode.Disabled);
			}
		}

		// Determines if a change in the content's extents, if any, is expected
		// based on DirectManipulation feedback.
		// (C++ source line 10716)
		internal void AreExtentChangesExpected(out bool areExtentChangesExpected)
		{
			areExtentChangesExpected = false;

			IsExtentXChangeExpected(out var isExtentChangeExpected);
			if (!isExtentChangeExpected)
			{
				return;
			}

			IsExtentYChangeExpected(out isExtentChangeExpected);
			if (!isExtentChangeExpected)
			{
				return;
			}

			areExtentChangesExpected = true;
		}

		// Determines if a change in the content's width is expected based on
		// DirectManipulation feedback.
		// (C++ source line 10751)
		internal void IsExtentXChangeExpected(out bool isExtentXChangeExpected)
		{
			isExtentXChangeExpected = false;

			if (!IsInDirectManipulation || m_contentWidthRequested == -1)
			{
				return;
			}

			var spProvider = GetInnerManipulationDataProvider(true /*isForHorizontalOrientation*/);

			ComputePixelExtentWidth(false /*ignoreZoomFactor*/, spProvider, out var extent);
			if (Math.Abs(m_contentWidthRequested - extent) / extent > ScrollViewerZoomExtentRoundingTolerance)
			{
				return;
			}

			isExtentXChangeExpected = true;
		}

		// Determines if a change in the content's height is expected based on
		// DirectManipulation feedback.
		// (C++ source line 10806)
		internal void IsExtentYChangeExpected(out bool isExtentYChangeExpected)
		{
			isExtentYChangeExpected = false;

			if (!IsInDirectManipulation || m_contentHeightRequested == -1)
			{
				return;
			}

			var spProvider = GetInnerManipulationDataProvider(false /*isForHorizontalOrientation*/);

			ComputePixelExtentHeight(false /*ignoreZoomFactor*/, spProvider, out var extent);
			if (Math.Abs(m_contentHeightRequested - extent) / extent > ScrollViewerZoomExtentRoundingTolerance)
			{
				return;
			}

			isExtentYChangeExpected = true;
		}

		// Computes the effect of the left margin and horizontal alignment when
		// the content is smaller than the viewport.
		// The extent parameter provided is valid when positive, and needs to be
		// computed when strictly negative.
		// (C++ source line 7318)
		internal void ComputeTranslationXCorrection(
			bool inManipulation,
			bool forInitialTransformationAdjustment,
			bool adjustDimensions,
			object pProvider,
			double leftMargin,
			double extent,
			float zoomFactor,
			out float pTranslationX)
		{
			double viewport = 0;
			double translationX = 0;
			DMAlignment alignment;

			global::System.Diagnostics.Debug.Assert(zoomFactor > 0.0f);

			pTranslationX = 0.0f;

			alignment = ComputeHorizontalAlignment(true /*canUseCachedProperties*/);

			if (alignment == DMAlignment.Center || alignment == DMAlignment.Far)
			{
				if (extent < 0)
				{
					ComputePixelExtentWidth(false /*ignoreZoomFactor*/, pProvider, out extent);
				}
				ComputePixelViewportWidth(pProvider, true /*isProviderSet*/, out viewport);

				if (!forInitialTransformationAdjustment && extent < viewport)
				{
					if (!inManipulation && extent / zoomFactor < viewport)
					{
						translationX = (1.0 / (double)zoomFactor - 1.0) * extent;
					}
					else
					{
						translationX = viewport - extent;
					}

					if (alignment == DMAlignment.Center)
					{
						translationX /= 2.0;
					}
				}
				else if (extent / zoomFactor < viewport && !inManipulation)
				{
					translationX = extent / zoomFactor - viewport;

					if (alignment == DMAlignment.Center)
					{
						translationX /= 2.0;
					}
				}

				if (adjustDimensions && extent < viewport)
				{
					// Take into account the fact that DManip consumes integers instead of decimal numbers for content and viewport dimensions.

					// Evaluate exact offset used by XAML layout engine
					double offsetX = extent / zoomFactor - viewport;
					if (alignment == DMAlignment.Center)
					{
						offsetX /= 2.0;
					}

					// Evaluate approximated offset used by DManip based on adjusted content extent and viewport width
					double adjustedOffsetX = AdjustPixelContentDim(extent / zoomFactor) - AdjustPixelViewportDim(viewport);
					if (alignment == DMAlignment.Center)
					{
						adjustedOffsetX /= 2.0;
					}

					// Adjust the returned translation based on the difference between the two XAML/DManip evaluations.
					// translationX is computed differently above depending on forInitialTransformationAdjustment (a variation of
					// viewport-extent vs. extent-viewport), so the sign of the adjustment depends on forInitialTransformationAdjustment too.
					if (forInitialTransformationAdjustment)
					{
						translationX += adjustedOffsetX - offsetX;
					}
					else
					{
						translationX += offsetX - adjustedOffsetX;
					}
				}
			}

			// Skip the margin's contribution when the initial adjustment is computed - in that case
			// GetManipulationPrimaryContentTransform is also called with forMargins==True.
			if (!forInitialTransformationAdjustment)
			{
				translationX += ((double)zoomFactor - 1.0) * leftMargin;
			}

			pTranslationX = (float)translationX;
		}

		// Computes the effect of the top margin and vertical alignment when
		// the content is smaller than the viewport.
		// The extent parameter provided is valid when positive, and needs to be
		// computed when strictly negative.
		// (C++ source line 7430)
		internal void ComputeTranslationYCorrection(
			bool inManipulation,
			bool forInitialTransformationAdjustment,
			bool adjustDimensions,
			object pProvider,
			double topMargin,
			double extent,
			float zoomFactor,
			out float pTranslationY)
		{
			double viewport = 0;
			double translationY = 0;
			DMAlignment alignment;

			pTranslationY = 0.0f;

			alignment = ComputeVerticalAlignment(true /*canUseCachedProperties*/);

			if (alignment == DMAlignment.Center || alignment == DMAlignment.Far)
			{
				if (extent < 0)
				{
					ComputePixelExtentHeight(false /*ignoreZoomFactor*/, pProvider, out extent);
				}
				ComputePixelViewportHeight(pProvider, true /*isProviderSet*/, out viewport);

				if (!forInitialTransformationAdjustment && extent < viewport)
				{
					if (!inManipulation && extent / zoomFactor < viewport)
					{
						translationY = (1.0 / (double)zoomFactor - 1.0) * extent;
					}
					else
					{
						translationY = viewport - extent;
					}

					if (alignment == DMAlignment.Center)
					{
						translationY /= 2.0;
					}
				}
				else if (extent / zoomFactor < viewport && !inManipulation)
				{
					translationY = extent / zoomFactor - viewport;

					if (alignment == DMAlignment.Center)
					{
						translationY /= 2.0;
					}
				}

				if (adjustDimensions && extent < viewport)
				{
					// Take into account the fact that DManip consumes integers instead of decimal numbers for content and viewport dimensions.

					// Evaluate exact offset used by XAML layout engine
					double offsetY = extent / zoomFactor - viewport;
					if (alignment == DMAlignment.Center)
					{
						offsetY /= 2.0;
					}

					// Evaluate approximated offset used by DManip based on adjusted content extent and viewport height
					double adjustedOffsetY = AdjustPixelContentDim(extent / zoomFactor) - AdjustPixelViewportDim(viewport);
					if (alignment == DMAlignment.Center)
					{
						adjustedOffsetY /= 2.0;
					}

					// Adjust the returned translation based on the difference between the two XAML/DManip evaluations.
					// translationY is computed differently above depending on forInitialTransformationAdjustment (a variation of
					// viewport-extent vs. extent-viewport), so the sign of the adjustment depends on forInitialTransformationAdjustment too.
					if (forInitialTransformationAdjustment)
					{
						translationY += adjustedOffsetY - offsetY;
					}
					else
					{
						translationY += offsetY - adjustedOffsetY;
					}
				}
			}

			// Skip the margin's contribution when the initial adjustment is computed - in that case
			// GetManipulationPrimaryContentTransform is also called with forMargins==True.
			if (!forInitialTransformationAdjustment)
			{
				translationY += ((double)zoomFactor - 1.0) * topMargin;
			}

			pTranslationY = (float)translationY;
		}

		// Adjusts the provided logical targetOffset based on potential mandatory near-aligned scroll snap points.
		// (C++ source line 12301)
		internal void AdjustLogicalOffsetWithMandatorySnapPoints(
			bool isForHorizontalOffset,
			ref double pTargetOffset)
		{
			SnapPointsType snapPointsType = SnapPointsType.None;
			Microsoft.UI.Xaml.Controls.Primitives.SnapPointsAlignment snapPointsAlignment = Microsoft.UI.Xaml.Controls.Primitives.SnapPointsAlignment.Near;

			if (_snapPointsInfo is null)
			{
				// No scroll snap point implementation to operate against.
				return;
			}

			if (isForHorizontalOffset)
			{
				snapPointsType = HorizontalSnapPointsType;
				snapPointsAlignment = HorizontalSnapPointsAlignment;
			}
			else
			{
				snapPointsType = VerticalSnapPointsType;
				snapPointsAlignment = VerticalSnapPointsAlignment;
			}

			if (snapPointsType != SnapPointsType.MandatorySingle &&
				snapPointsType != SnapPointsType.Mandatory)
			{
				// Scroll snap points are not mandatory.
				return;
			}

			if (snapPointsAlignment != Microsoft.UI.Xaml.Controls.Primitives.SnapPointsAlignment.Near)
			{
				// Scroll snap points are not near-aligned. We don't know how to alter the logical offset.
				return;
			}

			// Truncate targetOffset to the lower integer - this corresponds to the snap point
			// separating two consecutive items.

			// That truncation is not performed though when the target offset is very slightly
			// smaller than an integer because of a float rounding.

			// Tolerated rounding delta in logical unit for skipping the adjustment to the lower integer.
			const double ScrollViewerScrollSnapPointRoundingToleranceForProvider = 0.00001;
			double adjustedTargetOffset = Math.Floor(pTargetOffset);

			if (pTargetOffset - adjustedTargetOffset < 1.0 - ScrollViewerScrollSnapPointRoundingToleranceForProvider)
			{
				pTargetOffset = adjustedTargetOffset;
			}
		}

		// Note: OnPointerPressed and OnPointerReleased are already implemented on
		// ScrollViewer.MuxInternal.cs (an older WinUI-derived partial). They mirror
		// C++ ScrollViewer_Partial.cpp:2466 and :2502 closely; the only deviation
		// is that GestureFollowing reads as None (`PointerRoutedEventArgs.GestureFollowing`
		// is currently NotImplemented), so the m_shouldFocusOnRightTapUnhandled
		// branch never fires. Once GestureFollowing lands, uncomment the
		// commented-out branch in MuxInternal to wire the right-tap focus path.

		// (C++ source line 2323)
		protected override void OnGotFocus(RoutedEventArgs args)
		{
			base.OnGotFocus(args);

			if (IsRootScrollViewer())
			{
				return;
			}

			var lastInputDeviceType = XamlRoot?.VisualTree.ContentRoot.InputManager.LastInputDeviceType
				?? global::Uno.UI.Xaml.Input.InputDeviceType.None;

			m_preferMouseIndicators =
				lastInputDeviceType == global::Uno.UI.Xaml.Input.InputDeviceType.Mouse ||
				lastInputDeviceType == global::Uno.UI.Xaml.Input.InputDeviceType.Pen;

			ShowIndicators();

			// If we are here because a text-editable descendant got focus,
			// we might have to reflow the ScrollViewer around any existing occlusions.
			bool reduceViewportForCoreInputViewOcclusions = ReduceViewportForCoreInputViewOcclusions;

			if (reduceViewportForCoreInputViewOcclusions)
			{
				ReflowAroundCoreInputViewOcclusions();
			}
		}

		// (C++ source line 2856)
		internal void MakeVisible(
			// Child element to bring into view
			UIElement element,
			// Target rectangle dimensions. If empty, bring the child element's
			// RenderSize dimensions into view.
			global::Windows.Foundation.Rect targetRect,
			// Pass on forceIntoView from sender to ancestor ScrollViewer
			bool forceIntoView,
			// When set to True, the DManip ZoomToRect method is invoked.
			bool useAnimation,
			// Forwarded to the BringIntoView method to indicate whether its own
			// MakeVisible calls should be skipped during an ongoing manipulation or not.
			bool skipDuringManipulation,
			double horizontalAlignmentRatio,
			double verticalAlignmentRatio,
			double offsetX,
			double offsetY,
			// Uno-specific out parameters used by OnBringIntoViewRequested for parent
			// SV propagation. The C++ ScrollViewer::MakeVisible (line 2856) calls
			// UIElement::BringIntoView at the end to originate a fresh RequestBringIntoView
			// event up the visual tree; Uno hasn't ported that internal API yet, so the
			// caller (OnBringIntoViewRequested) updates the args directly using these
			// out params instead.
			out global::Windows.Foundation.Rect desiredViewOut,
			out double remainingOffsetX,
			out double remainingOffsetY)
		{
			global::Windows.Foundation.Rect visibleBounds = default;
			global::Windows.Foundation.Rect desiredView = default;
			global::Windows.Foundation.Point visiblePoint = default;
			global::Windows.Foundation.Point transformedPoint = default;
			desiredViewOut = default;
			remainingOffsetX = offsetX;
			remainingOffsetY = offsetY;

			if (element is not null && m_trElementScrollContentPresenter is not null)
			{
				bool isAncestorOfChild = m_trElementScrollContentPresenter.IsAncestorOf(element);
				bool isAncestorOfPresenter = this.IsAncestorOf(m_trElementScrollContentPresenter);

				if (isAncestorOfChild &&
					isAncestorOfPresenter &&
					IsInLiveTree)
				{
					global::Windows.Foundation.Rect target = default;

					bool empty = targetRect.IsEmpty || (targetRect.Width == 0 && targetRect.Height == 0);

					if (empty)
					{
						var renderSize = element.RenderSize;
						target.X = 0;
						target.Y = 0;
						target.Width = renderSize.Width;
						target.Height = renderSize.Height;
					}
					else
					{
						target = targetRect;
					}

					// Get the rectangle for the scroll content present after bringing
					// the containing child element into view. The new rectangle is the
					// parameter for bringing the ScrollViewer's content into view by a
					// ScrollViewer ancestor.
					var spScrollInfo = GetScrollInfo();
					if (spScrollInfo is not null)
					{
						double appliedOffsetX = 0.0;
						double appliedOffsetY = 0.0;

						visibleBounds = spScrollInfo.MakeVisible(
							element,
							target,
							useAnimation,
							horizontalAlignmentRatio,
							verticalAlignmentRatio,
							offsetX,
							offsetY,
							out appliedOffsetX,
							out appliedOffsetY);

						// Compute the remaining offsets to apply by potential parent contributors. The amount
						// applied by the last contributor, spScrollInfo, must not be applied more than once.
						if (appliedOffsetX != 0.0)
						{
							remainingOffsetX -= appliedOffsetX;
						}
						if (appliedOffsetY != 0.0)
						{
							remainingOffsetY -= appliedOffsetY;
						}
					}

					empty = visibleBounds.IsEmpty || (visibleBounds.Width == 0 && visibleBounds.Height == 0);
					if (!empty)
					{
						var spTransform = m_trElementScrollContentPresenter.TransformToVisual(this);
						visiblePoint.X = visibleBounds.X;
						visiblePoint.Y = visibleBounds.Y;
						transformedPoint = spTransform.TransformPoint(visiblePoint);
						desiredView.X = transformedPoint.X;
						desiredView.Y = transformedPoint.Y;
						desiredView.Width = visibleBounds.Width;
						desiredView.Height = visibleBounds.Height;
					}
					else
					{
						desiredView = visibleBounds;
					}

					desiredViewOut = desiredView;

					// TODO Uno: Phase 4 — port UIElement::BringIntoView(rect, forceIntoView,
					// useAnimation, skipDuringManipulation, horizontalAlignmentRatio,
					// verticalAlignmentRatio, offsetX, offsetY) which originates a
					// RequestBringIntoView event up the visual tree so a parent ScrollViewer
					// can complete the request. Uno's OnBringIntoViewRequested uses the
					// `desiredView` + `remaining*Offset` out params to update the routed-event
					// args directly so the parent SV picks up the bubbled event with the
					// adjusted target — equivalent to BringIntoView re-originating, except
					// without spawning a new event.
					_ = forceIntoView;
					_ = skipDuringManipulation;
				}
			}
		}

		// OnBringIntoViewRequested is called from the event handler ScrollViewer
		// registers for the event.  The default implementation checks to make sure the
		// visual is a child of the scroll viewer, and then delegates to a method there.
		// (C++ source line 2992)
		protected override void OnBringIntoViewRequested(BringIntoViewRequestedEventArgs args)
		{
			// In certain circumstances (currently only the Pivot ScrollViewer), we never want
			// the ScrollViewer to handle RequestBringIntoView, ever.  The reason in the case
			// of the Pivot ScrollViewer is that it is solely intended to hold the Pivot items
			// and provide a way to shift between them - we never want the Pivot ScrollViewer
			// to move in order to bring something inside it into view.
			// In such cases, we'll just ignore RequestBringIntoView events unconditionally.
			if (m_isRequestBringIntoViewIgnored)
			{
				return;
			}

			var spTargetObject = args.TargetElement;
			UIElement elementNoRef = spTargetObject;

			if (elementNoRef is not null && this != elementNoRef)
			{
				bool isAncestor = this.IsAncestorOf(elementNoRef);
				if (isAncestor)
				{
					// TODO Uno: BringIntoViewRequestedEventArgs.ForceIntoView is not yet
					// surfaced in Uno; treat it as false until exposed.
					bool forceIntoView = false;
					bool useAnimation = args.AnimationDesired;
					// TODO Uno: BringIntoViewRequestedEventArgs.InterruptDuringManipulation
					// is not yet surfaced in Uno; default to true (the WinUI default).
					bool skipDuringManipulation = true;
					double horizontalAlignmentRatio = args.HorizontalAlignmentRatio;
					double verticalAlignmentRatio = args.VerticalAlignmentRatio;
					double offsetX = args.HorizontalOffset;
					double offsetY = args.VerticalOffset;
					bool bringIntoView = BringIntoViewOnFocusChange;

					// Don't bring into view if ScrollViewer.BringIntoViewOnFocusChange = FALSE,
					// unless forceIntoView is set. An app sets ScrollViewer.BringIntoViewOnFocusChange to FALSE
					// when it wants to handle BringIntoView.
					// To prevent incorrect scroll offsets, don't auto scroll into view when ScrollViewer
					// is being manipulated by user. For example, don't scroll into view during zoomin/out
					// in SemanticZoom using the keyboard.

					// TODO Uno: Phase 4 — once IsInManipulation is wired up to the DM adapter,
					// honour skipDuringManipulation. For now treat the SV as never being in
					// manipulation so the bring-into-view request always fires.
					if (forceIntoView || (bringIntoView && (!skipDuringManipulation || !IsInManipulation)))
					{
						var rect = args.TargetRect;
						MakeVisible(
							elementNoRef,
							rect,
							forceIntoView,
							useAnimation,
							skipDuringManipulation,
							horizontalAlignmentRatio,
							verticalAlignmentRatio,
							offsetX,
							offsetY,
							out var desiredView,
							out var remainingOffsetX,
							out var remainingOffsetY);

						// Parent SV propagation (Uno-specific): the C++ MakeVisible originates
						// a fresh RequestBringIntoView event via UIElement::BringIntoView so
						// any ancestor ScrollViewer can complete the request. Until that internal
						// API is ported, update the routed-event args so the same event continues
						// bubbling with the adjusted target rect / element / offsets — the parent
						// SV's OnBringIntoViewRequested then picks up the request with a target
						// inside this SV.
						bool desiredViewEmpty = desiredView.IsEmpty || (desiredView.Width == 0 && desiredView.Height == 0);
						if (!desiredViewEmpty)
						{
							// Compute the SV's own viewport rectangle in its coordinate space.
							var viewportRect = new global::Windows.Foundation.Rect(0, 0, ActualWidth, ActualHeight);

							if (Uno.UI.Helpers.WinUI.SharedHelpers.DoRectsIntersect(desiredView, viewportRect))
							{
								// A portion of the rect is still in this SV's viewport — let the
								// event bubble so a parent SV can scroll the rest into its own view.
								args.TargetRect = desiredView;
								args.TargetElement = this;
								args.HorizontalOffset = remainingOffsetX;
								args.VerticalOffset = remainingOffsetY;
							}
							else
							{
								// The rect is entirely outside this SV's viewport — no further
								// parent scrolling can help; mark handled.
								args.Handled = true;
							}
						}
						else
						{
							// MakeVisible bailed out (this SV isn't a real ancestor of the target,
							// or no IScrollInfo). Mark handled so the event doesn't keep firing.
							args.Handled = true;
						}
					}
					else
					{
						// SV is configured not to bring into view (BringIntoViewOnFocusChange=false
						// without forceIntoView, or skipping during manipulation). Mark handled to
						// prevent duplicate work by ancestors.
						args.Handled = true;
					}
				}
			}
		}

		// Handles the vertical ScrollBar.Scroll event and updates the UI.
		internal void HandleVerticalScroll(ScrollEventType scrollEventType, double offset = 0.0)
		{
			// If style changes and Content cannot be found - just exit.
			var spScrollInfo = GetScrollInfo();
			if (spScrollInfo is null)
			{
				return;
			}

			var oldOffset = spScrollInfo.GetVerticalOffset();
			var newOffset = oldOffset;

			switch (scrollEventType)
			{
				case ScrollEventType.EndScroll:
					LeaveIntermediateViewChangedMode(raiseFinalViewChanged: true);
					break;
				case ScrollEventType.ThumbPosition:
				case ScrollEventType.ThumbTrack:
					if (scrollEventType == ScrollEventType.ThumbTrack)
					{
						EnterIntermediateViewChangedMode();
					}
					newOffset = offset;
					break;
				case ScrollEventType.LargeDecrement:
					spScrollInfo.PageUp();
					break;
				case ScrollEventType.LargeIncrement:
					spScrollInfo.PageDown();
					break;
				case ScrollEventType.SmallDecrement:
					spScrollInfo.LineUp();
					break;
				case ScrollEventType.SmallIncrement:
					spScrollInfo.LineDown();
					break;
				case ScrollEventType.First:
					newOffset = double.MinValue;
					break;
				case ScrollEventType.Last:
					newOffset = double.MaxValue;
					break;
			}

			var isCarouselPanel = IsPanelACarouselPanel(isForHorizontalOrientation: false);
			// Do not clamp offset for carousel-ing panel.
			if (!isCarouselPanel)
			{
				newOffset = Math.Max(newOffset, 0.0);
				// Clamp the new offset at this stage to prevent unnecessary layout.
				var scrollable = ScrollableHeight;
				newOffset = Math.Min(scrollable, newOffset);
			}

			// If newOffset does not match oldOffset, apply it
			if (!DoubleUtil.AreClose(oldOffset, newOffset))
			{
				// potentially block updating the scrollinfo while thumb dragging is occurring
				var isDeferring = false;
				if (m_isDraggingThumb)
				{
					isDeferring = IsDeferredScrollingEnabled;
					if (isDeferring)
					{
						m_verticalOffsetCached = newOffset;
					}
				}

				if (!isDeferring)
				{
					spScrollInfo.SetVerticalOffset(newOffset);
				}
			}
		}

		// Handles the horizontal ScrollBar.Scroll event and updates the UI.
		internal void HandleHorizontalScroll(ScrollEventType scrollEventType, double offset = 0.0)
		{
			// If style changes and Content cannot be found - just exit.
			var spScrollInfo = GetScrollInfo();
			if (spScrollInfo is null)
			{
				return;
			}

			var oldOffset = spScrollInfo.GetHorizontalOffset();
			var newOffset = oldOffset;

			switch (scrollEventType)
			{
				case ScrollEventType.EndScroll:
					LeaveIntermediateViewChangedMode(raiseFinalViewChanged: true);
					break;
				case ScrollEventType.ThumbPosition:
				case ScrollEventType.ThumbTrack:
					if (scrollEventType == ScrollEventType.ThumbTrack)
					{
						EnterIntermediateViewChangedMode();
					}
					newOffset = offset;
					break;
				case ScrollEventType.LargeDecrement:
					spScrollInfo.PageLeft();
					break;
				case ScrollEventType.LargeIncrement:
					spScrollInfo.PageRight();
					break;
				case ScrollEventType.SmallDecrement:
					spScrollInfo.LineLeft();
					break;
				case ScrollEventType.SmallIncrement:
					spScrollInfo.LineRight();
					break;
				case ScrollEventType.First:
					newOffset = double.MinValue;
					break;
				case ScrollEventType.Last:
					newOffset = double.MaxValue;
					break;
			}

			newOffset = Math.Max(newOffset, 0.0);

			// Clamp the new offset at this stage to prevent unnecessary layout.
			var scrollable = ScrollableWidth;
			newOffset = Math.Min(scrollable, newOffset);

			// If newOffset does not match oldOffset, apply it
			if (!DoubleUtil.AreClose(oldOffset, newOffset))
			{
				// potentially block updating the scrollinfo while thumb dragging is occurring
				var isDeferring = false;
				if (m_isDraggingThumb)
				{
					isDeferring = IsDeferredScrollingEnabled;
					if (isDeferring)
					{
						m_horizontalOffsetCached = newOffset;
					}
				}

				if (!isDeferring)
				{
					spScrollInfo.SetHorizontalOffset(newOffset);
				}
			}
		}

		// Member of the IScrollOwner internal contract. Allows the interface
		// consumer to notify this ScrollViewer of an attempt to change the
		// horizontal offset. Used for the ViewChanging notifications.
		internal void NotifyHorizontalOffsetChanging(
			double targetHorizontalOffset,
			double targetVerticalOffset)
		{
			NotifyOffsetChanging(targetHorizontalOffset, targetVerticalOffset);
		}

		// Member of the IScrollOwner internal contract. Allows the interface
		// consumer to notify this ScrollViewer of an attempt to change the
		// vertical offset. Used for the ViewChanging notifications.
		internal void NotifyVerticalOffsetChanging(
			double targetHorizontalOffset,
			double targetVerticalOffset)
		{
			NotifyOffsetChanging(targetHorizontalOffset, targetVerticalOffset);
		}

		// Common method called by NotifyHorizontalOffsetChanging and NotifyVerticalOffsetChanging
		// in order to invoke RaiseViewChanging with the correct zoom factor.
		private void NotifyOffsetChanging(
			double targetHorizontalOffset,
			double targetVerticalOffset)
		{
			float targetZoomFactor;

			// Use the targeted zoom factor if it's changing, otherwise the current value.
			if (m_isTargetZoomFactorValid)
			{
				targetZoomFactor = m_targetZoomFactor;
			}
			else
			{
				targetZoomFactor = ZoomFactor;
			}

			RaiseViewChanging(targetHorizontalOffset, targetVerticalOffset, targetZoomFactor);
		}

		// Called internally when a zoom factor change is processed in order to invoke RaiseViewChanging
		// with the correct offsets.
		internal void NotifyZoomFactorChanging(float targetZoomFactor)
		{
			double targetHorizontalOffset;
			double targetVerticalOffset;

			if (m_isTargetHorizontalOffsetValid)
			{
				targetHorizontalOffset = m_targetHorizontalOffset;
			}
			else
			{
				targetHorizontalOffset = HorizontalOffset;
			}

			if (m_isTargetVerticalOffsetValid)
			{
				targetVerticalOffset = m_targetVerticalOffset;
			}
			else
			{
				targetVerticalOffset = VerticalOffset;
			}

			RaiseViewChanging(targetHorizontalOffset, targetVerticalOffset, targetZoomFactor);
		}

		// Called when the HorizontalScrollBarVisibility or
		// VerticalScrollBarVisibility property has changed.
		internal void OnScrollBarVisibilityChanged()
		{
			var scrollInfo = GetScrollInfo();
			if (scrollInfo is not null)
			{
				UpdateCanScroll(scrollInfo);
			}

			// ScrollBar visibilities affect the DirectManipulation viewport configurations.
			// TODO Uno: Phase 4 — call OnViewportConfigurationsAffectingPropertyChanged once DM adapter exists.
			// OnViewportConfigurationsAffectingPropertyChanged();

			InvalidateMeasure();
		}

		// Updates the provided IScrollInfo's CanHorizontallyScroll & CanVerticallyScroll characteristics based
		// on the scrollbars visibility, and resets the offset(s) when scrolling in not enabled.
		private void UpdateCanScroll(IScrollInfo pScrollInfo)
		{
			global::System.Diagnostics.Debug.Assert(pScrollInfo is not null);

			var horizontal = HorizontalScrollBarVisibility;
			var vertical = VerticalScrollBarVisibility;

			if (horizontal == ScrollBarVisibility.Disabled)
			{
				// When the horizontal scrollbar becomes disabled, the horizontal offset needs to be reset to 0.
				pScrollInfo.SetHorizontalOffset(0.0);
			}
			pScrollInfo.PutCanHorizontallyScroll(horizontal != ScrollBarVisibility.Disabled);

			if (vertical == ScrollBarVisibility.Disabled)
			{
				// When the vertical scrollbar becomes disabled, the vertical offset needs to be reset to 0.
				pScrollInfo.SetVerticalOffset(0.0);
			}
			pScrollInfo.PutCanVerticallyScroll(vertical != ScrollBarVisibility.Disabled);
		}

		// Handle the horizontal ScrollBar's Scroll event.
		internal void OnHorizontalScrollBarScroll(object pSender, ScrollEventArgs pArgs)
		{
			// Do not process this request when the effective HorizontalScrollMode is Disabled.
			var scrollMode = GetEffectiveHorizontalScrollMode(canUseCachedProperty: true);
			if (scrollMode == ScrollMode.Disabled)
			{
				return;
			}

			var scrollEventType = pArgs.ScrollEventType;
			var newOffset = pArgs.NewValue;

			HandleHorizontalScroll(scrollEventType, newOffset);
		}

		// Handle the vertical ScrollBar's Scroll event.
		internal void OnVerticalScrollBarScroll(object pSender, ScrollEventArgs pArgs)
		{
			// Do not process this request when the effective VerticalScrollMode is Disabled.
			var scrollMode = GetEffectiveVerticalScrollMode(canUseCachedProperty: true);
			if (scrollMode == ScrollMode.Disabled)
			{
				return;
			}

			var scrollEventType = pArgs.ScrollEventType;
			var newOffset = pArgs.NewValue;

			HandleVerticalScroll(scrollEventType, newOffset);
		}

		// Handle DragStarted on the horizontal ScrollBar's Thumb.
		internal void OnScrollBarThumbDragStarted(object pSender, DragStartedEventArgs pArgs)
		{
			// Suppress the scrollbars from fading out while we are dragging.
			m_keepIndicatorsShowing = true;

			// Suppress scrolling when dragging a thumb
			m_isDraggingThumb = true;

			ShowIndicators();
		}

		// Handle DragCompleted on the horizontal ScrollBar's Thumb.
		internal void OnScrollBarThumbDragCompleted(object pSender, DragCompletedEventArgs pArgs)
		{
			// Make the scrollbars fade out, after the normal delay.
			m_keepIndicatorsShowing = false;

			// Suppress scrolling when dragging a thumb
			m_isDraggingThumb = false;

			// and synchronize
			SynchronizeScrollOffsetsAfterThumbDeferring();

			// TODO Uno: Phase 4 — call UpdateVisualState() once it's wired up.
			// UpdateVisualState();
		}

		// Handle PointerEntered on the vertical scrollbar
		internal void OnVerticalScrollbarPointerEntered(object pSender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs pArgs)
		{
			m_isPointerOverVerticalScrollbar = true;
		}

		// Handle PointerExited on the vertical scrollbar
		internal void OnVerticalScrollbarPointerExited(object pSender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs pArgs)
		{
			m_isPointerOverVerticalScrollbar = false;
		}

		// Handle PointerEntered on the horizontal scrollbar
		internal void OnHorizontalScrollbarPointerEntered(object pSender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs pArgs)
		{
			m_isPointerOverHorizontalScrollbar = true;
		}

		// Handle PointerExited on the horizontal scrollbar
		internal void OnHorizontalScrollbarPointerExited(object pSender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs pArgs)
		{
			m_isPointerOverHorizontalScrollbar = false;
		}

		// Returns a rounded down dimension for the viewport since DManip only accepts integer viewport values.
		// A rounded down value is used to avoid bugs with unreachable mandatory scroll snap points.
		internal static double AdjustPixelViewportDim(double pixelViewportDim)
		{
			// Round to the closest lower integer.
			// +3.000 --> +3.0
			// +8.731 --> +8.0
			// +5.999 --> +5.0
			global::System.Diagnostics.Debug.Assert(pixelViewportDim >= 0.0f);
			return (double)(long)pixelViewportDim;
		}

		// Returns a rounded up dimension for the content since DManip only accepts integer content values.
		// A rounded up value is used to avoid bugs with unreachable mandatory scroll snap points.
		internal static double AdjustPixelContentDim(double pixelContentDim)
		{
			// Round to the closest upper integer.
			// +3.000 --> +3.0
			// +8.731 --> +9.0
			// +5.001 --> +6.0
			global::System.Diagnostics.Debug.Assert(pixelContentDim >= 0.0f);

			var roundedDim = (long)pixelContentDim;
			if (pixelContentDim == (double)roundedDim)
			{
				return (double)roundedDim;
			}
			else
			{
				return (double)(roundedDim + 1);
			}
		}

		// Returns the IScrollInfo implementation as a UIElement.
		internal UIElement GetScrollInfoAsElement() => GetScrollInfo() as UIElement;

		// Returns the potential inner IManipulationDataProvider regardless of orientation.
		// TODO Uno: Phase 4 — IManipulationDataProvider isn't implemented in the managed Skia path yet.
		// Always returns null (the SCP is the IScrollInfo implementer).
		private object GetInnerManipulationDataProvider() => null;

		// Returns the potential inner IManipulationDataProvider if it's oriented according
		// to the provided orientation.
		private object GetInnerManipulationDataProvider(bool isForHorizontalOrientation) => null;

		// Updates the ScrollBar's IsIgnoringUserInput flag based on the scroll mode setting.
		// Delays the update when there is an ongoing manipulation.
		// The horizontal or vertical ScrollBar is affected depending on the param.
		internal void RefreshScrollBarIsIgnoringUserInput(bool isForHorizontalOrientation)
		{
			if (IsInDirectManipulation)
			{
				// Refresh the ScrollBar after the current manipulation
				m_isScrollBarIgnoringUserInputInvalid = true;
				return;
			}

			if (isForHorizontalOrientation)
			{
				if (m_trElementHorizontalScrollBar is { } hScrollBar)
				{
					var scrollMode = HorizontalScrollMode;
					hScrollBar.IsIgnoringUserInput = scrollMode == ScrollMode.Disabled;
				}
			}
			else
			{
				if (m_trElementVerticalScrollBar is { } vScrollBar)
				{
					var scrollMode = VerticalScrollMode;
					vScrollBar.IsIgnoringUserInput = scrollMode == ScrollMode.Disabled;
				}
			}
		}

		// Updates the zoom factor value. Equivalent of ScrollToHorizontalOffset
		// and ScrollToVerticalOffset for the ZoomFactor dependency property.
		public void ZoomToFactor(float value)
		{
			ZoomToFactorInternal(value, delayAndFlushViewChanged: true, out _);
		}

		// Called internally to update the zoom factor property without batching the ViewChanged notifications.
		internal void ZoomToFactorInternal(
			float value,
			bool delayAndFlushViewChanged,
			out bool zoomChanged)
		{
			bool result = false;
			zoomChanged = false;

			if (delayAndFlushViewChanged)
			{
				DelayViewChanged();
			}

			try
			{
				var zoomFactor = ZoomFactor;

				if (value != zoomFactor)
				{
					var minZoomFactor = MinZoomFactor;
					var maxZoomFactor = MaxZoomFactor;

					// Value gets put into the [MinZoomFactor, MaxZoomFactor] range,
					// in accordance to what ScrollToHorizontalOffset does for instance.
					if (value < minZoomFactor)
					{
						value = minZoomFactor;
					}
					else if (value > maxZoomFactor)
					{
						value = maxZoomFactor;
					}

					if (value != zoomFactor)
					{
						if (m_isDirectManipulationZoomFactorChange || !IsInDirectManipulation || m_currentZoomMode != ZoomMode.Disabled)
						{
							// Zoom factor change is triggered by DManip feedback,
							// or occurs outside a DManip operation,
							// or is a programmatic zoom factor change request that needs to cancel DManip,
							PutZoomFactorCore(value);
							zoomChanged = true;
						}
						else
						{
							// Zoom factor change is programmatic, during a DManip operation
							// TODO Uno: Phase 4 — route through ChangeViewInternal once it lands. For now, just
							// programmatically apply the value.
							PutZoomFactorCore(value);
							zoomChanged = true;
							if (!m_isTargetZoomFactorValid || m_targetZoomFactor != value)
							{
								// Raise the ViewChanging event with the new target zoom factor. This will set m_isTargetZoomFactorValid to True and
								// m_targetZoomFactor to value, allowing m_targetZoomFactor to be used in any potential imminent call to ChangeViewInternal.
								NotifyZoomFactorChanging(value);
							}
							_ = result; // suppress unused warning until ChangeViewInternal lands
						}
					}
				}
			}
			finally
			{
				if (delayAndFlushViewChanged)
				{
					FlushViewChanged();
				}
			}
		}

		// Bridge to the ZoomFactor DP setter (which is `private set` on the cross-platform partial).
		// Mirrors ScrollViewerGenerated::put_ZoomFactor in WinUI.
		private void PutZoomFactorCore(float value) => SetValue(ZoomFactorProperty, value);

		// Validates the MinZoomFactor property when its value is changed.
		internal void OnMinZoomFactorPropertyChanged(object pOldValue, object pNewValue)
		{
			DelayViewChanged();

			try
			{
				bool bIsValidFloatValue;
				bool bRestoreOldValue = false;
				float oldValue = 0;

				// Ensure it's a valid value
				IsValidFloatValue(pNewValue, out var newValue, out bIsValidFloatValue);
				if (!bIsValidFloatValue)
				{
					bRestoreOldValue = true;
					// Using ERROR_INVALID_DOUBLE_VALUE even though property is a float, because the error string is "The value cannot be infinite or Not a Number (NaN)."
					throw new ArgumentException("The value cannot be infinite or Not a Number (NaN).");
				}

				if (newValue < ScrollViewerMinimumZoomFactor)
				{
					bRestoreOldValue = true;
					// "The MinZoomFactor property cannot be set to a value smaller than 0.1."
					throw new ArgumentException("The MinZoomFactor property cannot be set to a value smaller than 0.1.");
				}

				try
				{
					// Note: this section is a workaround, containing the
					// logic to hold all calls to the property changed
					// methods until after all coercion has completed
					// Logic was copied from RangeBase control, for its Minimum, Value, Maximum properties.
					// ----------
					if (m_cLevelsFromRootCallForZoom == 0)
					{
						m_initialMaxZoomFactor = MaxZoomFactor;
						m_initialZoomFactor = ZoomFactor;
					}
					m_cLevelsFromRootCallForZoom++;
					// ----------

					CoerceMaxZoomFactor();
					CoerceZoomFactor();

					// Note: this section completes the workaround to call
					// the property changed logic if all coercion has completed
					// ----------
					m_cLevelsFromRootCallForZoom--;
					if (m_cLevelsFromRootCallForZoom == 0)
					{
						GetFloatValue(pOldValue, out oldValue);

						OnZoomFactorBoundaryChanged(isForLowerBound: true, oldValue, newValue);

						var maxZoomFactor = MaxZoomFactor;

						if (m_initialMaxZoomFactor != maxZoomFactor)
						{
							OnZoomFactorBoundaryChanged(isForLowerBound: false, m_initialMaxZoomFactor, maxZoomFactor);
						}

						var zoomFactor = ZoomFactor;

						if (m_initialZoomFactor != zoomFactor)
						{
							OnZoomFactorChanged(m_initialZoomFactor, zoomFactor);
						}
					}
					// ----------
				}
				catch
				{
					if (bRestoreOldValue)
					{
						IsValidFloatValue(pOldValue, out oldValue, out bIsValidFloatValue);
						if (bIsValidFloatValue)
						{
							// Restore the old MinZoomFactor value if it was valid.
							MinZoomFactor = oldValue;
						}
					}
					throw;
				}
			}
			finally
			{
				FlushViewChanged();
			}
		}

		// Validates the MaxZoomFactor property when its value is changed.
		internal void OnMaxZoomFactorPropertyChanged(object pOldValue, object pNewValue)
		{
			DelayViewChanged();

			try
			{
				bool bIsValidFloatValue;
				bool bRestoreOldValue = false;
				float oldValue = 0;

				IsValidFloatValue(pNewValue, out var newValue, out bIsValidFloatValue);

				if (!bIsValidFloatValue)
				{
					bRestoreOldValue = true;
					// Using ERROR_INVALID_DOUBLE_VALUE even though property is a float, because the error string is "The value cannot be infinite or Not a Number (NaN)."
					throw new ArgumentException("The value cannot be infinite or Not a Number (NaN).");
				}

				if (newValue < ScrollViewerMinimumZoomFactor)
				{
					bRestoreOldValue = true;
					// "The MaxZoomFactor property cannot be set to a value smaller than 0.1."
					throw new ArgumentException("The MaxZoomFactor property cannot be set to a value smaller than 0.1.");
				}

				GetFloatValue(pOldValue, out oldValue);

				try
				{
					// Note: this section is a workaround, containing the
					// logic to hold all calls to the property changed
					// methods until after all coercion has completed
					// ----------
					if (m_cLevelsFromRootCallForZoom == 0)
					{
						m_requestedMaxZoomFactor = newValue;
						m_initialMaxZoomFactor = oldValue;
						m_initialZoomFactor = ZoomFactor;
					}
					m_cLevelsFromRootCallForZoom++;
					// ----------

					CoerceMaxZoomFactor();
					CoerceZoomFactor();

					// Note: this section completes the workaround to call
					// the property changed logic if all coercion has completed
					// ----------
					m_cLevelsFromRootCallForZoom--;
					if (m_cLevelsFromRootCallForZoom == 0)
					{
						var maxZoomFactor = MaxZoomFactor;

						if (m_initialMaxZoomFactor != maxZoomFactor)
						{
							OnZoomFactorBoundaryChanged(isForLowerBound: false, m_initialMaxZoomFactor, maxZoomFactor);
						}

						var zoomFactor = ZoomFactor;

						if (m_initialZoomFactor != zoomFactor)
						{
							OnZoomFactorChanged(m_initialZoomFactor, zoomFactor);
						}
					}
					// ----------
				}
				catch
				{
					if (bRestoreOldValue)
					{
						IsValidFloatValue(pOldValue, out oldValue, out bIsValidFloatValue);
						if (bIsValidFloatValue)
						{
							// Restore the old MaxZoomFactor value if it was valid.
							MaxZoomFactor = oldValue;
						}
					}
					throw;
				}
			}
			finally
			{
				FlushViewChanged();
			}
		}

		// Called when the HorizontalOffset dependency property changed.
		internal void OnHorizontalOffsetPropertyChanged(object pOldValue, object pNewValue)
		{
			m_isTargetHorizontalOffsetValid = false;
			RaiseViewChanged(m_isInIntermediateViewChangedMode /*isIntermediate*/);
		}

		// Called when the VerticalOffset dependency property changed.
		internal void OnVerticalOffsetPropertyChanged(object pOldValue, object pNewValue)
		{
			m_isTargetVerticalOffsetValid = false;
			RaiseViewChanged(m_isInIntermediateViewChangedMode /*isIntermediate*/);
		}

		// Validates the ZoomFactor property when its value is changed.
		internal void OnZoomFactorPropertyChanged(object pOldValue, object pNewValue)
		{
			bool bIsValidFloatValue;
			bool bRestoreOldValue = false;
			float oldValue = 0;

			IsValidFloatValue(pNewValue, out var newValue, out bIsValidFloatValue);

			if (!bIsValidFloatValue)
			{
				bRestoreOldValue = true;
				// Using ERROR_INVALID_DOUBLE_VALUE even though property is a float, because the error string is "The value cannot be infinite or Not a Number (NaN)."
				throw new ArgumentException("The value cannot be infinite or Not a Number (NaN).");
			}

			global::System.Diagnostics.Debug.Assert(newValue >= ScrollViewerMinimumZoomFactor);

			GetFloatValue(pOldValue, out oldValue);

			try
			{
				// Note: this section is a workaround, containing the
				// logic to hold all calls to the property changed
				// methods until after all coercion has completed
				// ----------
				if (m_cLevelsFromRootCallForZoom == 0)
				{
					m_requestedZoomFactor = newValue;
					m_initialZoomFactor = oldValue;
				}
				m_cLevelsFromRootCallForZoom++;
				// ----------

				CoerceZoomFactor();

				// Note: this section completes the workaround to call
				// the property changed logic if all coercion has completed
				// ----------
				m_cLevelsFromRootCallForZoom--;
				if (m_cLevelsFromRootCallForZoom == 0)
				{
					var zoomFactor = ZoomFactor;

					if (m_initialZoomFactor != zoomFactor)
					{
						OnZoomFactorChanged(m_initialZoomFactor, zoomFactor);
					}
				}
				// ----------
			}
			catch
			{
				if (bRestoreOldValue)
				{
					IsValidFloatValue(pOldValue, out oldValue, out bIsValidFloatValue);
					if (bIsValidFloatValue)
					{
						// Restore the old ZoomFactor value if it was valid.
						PutZoomFactorCore(oldValue);
					}
				}
				throw;
			}
		}

		// Ensures the MaxZoomFactor is greater than or equal to the MinZoomFactor.
		private void CoerceMaxZoomFactor()
		{
			var minZoomFactor = MinZoomFactor;
			var maxZoomFactor = MaxZoomFactor;

			if (m_requestedMaxZoomFactor != maxZoomFactor && m_requestedMaxZoomFactor >= minZoomFactor)
			{
				MaxZoomFactor = m_requestedMaxZoomFactor;
			}
			else if (maxZoomFactor < minZoomFactor)
			{
				MaxZoomFactor = minZoomFactor;
			}
		}

		// Ensures the ZoomFactor falls between the MinZoomFactor and MaxZoomFactor values.
		// This function assumes that (MaxZoomFactor >= MinZoomFactor)
		private void CoerceZoomFactor()
		{
			var minZoomFactor = MinZoomFactor;
			var maxZoomFactor = MaxZoomFactor;
			var zoomFactor = ZoomFactor;

			if (m_requestedZoomFactor != zoomFactor && m_requestedZoomFactor >= minZoomFactor && m_requestedZoomFactor <= maxZoomFactor)
			{
				NotifyZoomFactorChanging(m_requestedZoomFactor /*targetZoomFactor*/);
				PutZoomFactorCore(m_requestedZoomFactor);
			}
			else
			{
				if (zoomFactor < minZoomFactor)
				{
					NotifyZoomFactorChanging(minZoomFactor /*targetZoomFactor*/);
					PutZoomFactorCore(minZoomFactor);
				}
				if (zoomFactor > maxZoomFactor)
				{
					NotifyZoomFactorChanging(maxZoomFactor /*targetZoomFactor*/);
					PutZoomFactorCore(maxZoomFactor);
				}
			}
		}

		// Returns a float if the IPropertyValue contains a FLOAT or DOUBLE.
		private static void GetFloatValue(object pValue, out float floatValue)
		{
			if (pValue is null)
			{
				floatValue = 0.0f;
				return;
			}

			switch (pValue)
			{
				case float f:
					floatValue = f;
					return;
				case double d:
					floatValue = (float)d;
					return;
				default:
					floatValue = Convert.ToSingle(pValue, global::System.Globalization.CultureInfo.InvariantCulture);
					return;
			}
		}

		// Extracts a float from a IPropertyValue and checks if it's valid.
		private static void IsValidFloatValue(object pValue, out float floatValue, out bool pbIsValidFloatValue)
		{
			pbIsValidFloatValue = true;
			GetFloatValue(pValue, out floatValue);

			// Using the same code as xcp\win\dll\slm\math.cpp
			// WinNT & CE only use DOUBLE for _isnan and _finite.
			if (double.IsNaN(floatValue) || double.IsInfinity(floatValue))
			{
				pbIsValidFloatValue = false;
			}
		}

		// Called when the MinZoomFactor or MaxZoomFactor value changed.
		internal void OnZoomFactorBoundaryChanged(
			bool isForLowerBound,
			float oldZoomFactorBoundary,
			float newZoomFactorBoundary)
		{
			// If a DirectManipulation manip is active, push the new boundary to DM.
			// TODO Uno: Phase 4 — port OnPrimaryContentAffectingPropertyChanged. For now, no-op since
			// the new managed Skia path doesn't have a separate DM service to push to.
			// OnPrimaryContentAffectingPropertyChanged(
			//     boundsChanged: false,
			//     horizontalAlignmentChanged: false,
			//     verticalAlignmentChanged: false,
			//     zoomFactorBoundaryChanged: true);
		}

		// Called when the ZoomFactor value changed.
		internal void OnZoomFactorChanged(float oldZoomFactor, float newZoomFactor)
		{
			// TODO Uno: Phase 4 — wire SetZoomFactor on the inner ScrollContentPresenter once
			// HookupScrollingComponents lands.
			// if (m_trElementScrollContentPresenter is not null)
			// {
			//     m_trElementScrollContentPresenter.SetZoomFactor(newZoomFactor);
			// }

			// TODO Uno: Phase 5 — snap-points reaction (m_trScrollSnapPointsInfo handling, GetEffectiveZoomMode,
			// horizontal/vertical snap-point alignment checks, OnSnapPointsAffectingPropertyChanged calls).

			if (IsInDirectManipulation)
			{
				if (!m_isDirectManipulationZoomFactorChange)
				{
					if (m_currentZoomMode != ZoomMode.Disabled)
					{
						// Zoom factor was altered during a manipulation. This manipulation is going to be canceled.
						// Ignore the final zoom factor in HandleManipulationDelta, in order not to override the 'value' set here.
						m_isDirectManipulationZoomFactorChangeIgnored = true;
					}

					// Let the InputManager know about the zoom factor change request.
					// A programmatic zoom factor change during a DM manipulation:
					//  - cancels that manipulation if ZoomMode is Enabled.
					// TODO Uno: Phase 4 — port OnPrimaryContentTransformChanged.
					// OnPrimaryContentTransformChanged(translationXChanged: false, translationYChanged: false, zoomFactorChanged: true);
				}
			}
			else if (!m_isInDirectManipulationSync)
			{
				global::System.Diagnostics.Debug.Assert(!m_isDirectManipulationZoomFactorChange);
				bool clearInZoomFactorSync = true;
				m_isInZoomFactorSync = true;
				try
				{
					// Zoom factor change has to be pushed to DManip as the XAML and DManip transforms are kept in sync.
					ChangeViewInternal(
						pHorizontalOffset: null,
						pVerticalOffset: null,
						pZoomFactor: null,
						pOldZoomFactor: oldZoomFactor,
						forceChangeToCurrentView: true,
						adjustWithMandatorySnapPoints: false,
						skipDuringTouchContact: false,
						skipAnimationWhileRunning: false,
						disableAnimation: true,
						applyAsManip: false,
						transformIsInertiaEnd: false,
						isForMakeVisible: false);
					// result returned is FALSE when the BringIntoViewport operation is delayed during a manipulation completion.
					clearInZoomFactorSync = false;
					m_isInZoomFactorSync = false;
				}
				finally
				{
					if (clearInZoomFactorSync)
					{
						m_isInZoomFactorSync = false;
					}
				}
			}

			m_isTargetZoomFactorValid = false;
			RaiseViewChanged(m_isInIntermediateViewChangedMode /*isIntermediate*/);
		}

		// Retrieves the UIElement for the ScrollViewer content if any.
		internal UIElement GetContentUIElement()
		{
			// TODO Uno: Phase 4 wiring — read from m_trElementScrollContentPresenter once OnApplyTemplate
			// populates it. For now route through the cross-platform _presenter field.
			if (IsLoaded && _presenter is ScrollContentPresenter scp)
			{
				return scp.Content as UIElement;
			}
			return null;
		}

		// Retrieves the effective IsHorizontalRailEnabled value: m_currentIsHorizontalRailEnabled or
		// get_IsHorizontalRailEnabled depending on whether there is an active manip or not.
		internal bool GetEffectiveIsHorizontalRailEnabled(bool canUseCachedProperty)
		{
			if (IsInDirectManipulation && canUseCachedProperty)
			{
				return m_currentIsHorizontalRailEnabled;
			}
			else
			{
				return IsHorizontalRailEnabled;
			}
		}

		// Retrieves the effective IsVerticalRailEnabled value:
		// m_currentIsVerticalRailEnabled or get_IsVerticalRailEnabled
		// depending on whether there is an active manip or not.
		internal bool GetEffectiveIsVerticalRailEnabled(bool canUseCachedProperty)
		{
			if (IsInDirectManipulation && canUseCachedProperty)
			{
				return m_currentIsVerticalRailEnabled;
			}
			else
			{
				return IsVerticalRailEnabled;
			}
		}

		// Retrieves the effective IsScrollInertiaEnabled value:
		// m_currentIsScrollInertiaEnabled or get_IsScrollInertiaEnabled
		// depending on whether there is an active manip or not.
		internal bool GetEffectiveIsScrollInertiaEnabled(bool canUseCachedProperty)
		{
			if (IsInDirectManipulation && canUseCachedProperty)
			{
				return m_currentIsScrollInertiaEnabled;
			}
			else
			{
				return IsScrollInertiaEnabled;
			}
		}

		// Retrieves the effective IsZoomInertiaEnabled value:
		// m_currentIsZoomInertiaEnabled or get_IsZoomInertiaEnabled depending
		// on whether there is an active manip or not.
		internal bool GetEffectiveIsZoomInertiaEnabled(bool canUseCachedProperty)
		{
			if (IsInDirectManipulation && canUseCachedProperty)
			{
				return m_currentIsZoomInertiaEnabled;
			}
			else
			{
				return IsZoomInertiaEnabled;
			}
		}

		// Retrieves the effective horizontal scrollbar visibility:
		// m_currentHorizontalScrollBarVisibility or get_HorizontalScrollBarVisibility
		// depending on whether there is an active manip or not.
		internal ScrollBarVisibility GetEffectiveHorizontalScrollBarVisibility(bool canUseCachedProperty)
		{
			if (IsInDirectManipulation && canUseCachedProperty)
			{
				return m_currentHorizontalScrollBarVisibility;
			}
			else
			{
				return HorizontalScrollBarVisibility;
			}
		}

		// Retrieves the effective vertical scrollbar visibility:
		// m_currentVerticalScrollBarVisibility or get_VerticalScrollBarVisibility
		// depending on whether there is an active manip or not.
		internal ScrollBarVisibility GetEffectiveVerticalScrollBarVisibility(bool canUseCachedProperty)
		{
			if (IsInDirectManipulation && canUseCachedProperty)
			{
				return m_currentVerticalScrollBarVisibility;
			}
			else
			{
				return VerticalScrollBarVisibility;
			}
		}

		// Retrieves the horizontal alignment at the beginning of the current
		// manipulation if any when canUseCachedProperty is True, or the result
		// of ComputeHorizontalAlignment otherwise.
		internal DMAlignment GetEffectiveHorizontalAlignment(bool canUseCachedProperty)
		{
			if (IsInDirectManipulation && canUseCachedProperty)
			{
				return m_currentHorizontalAlignment;
			}
			else
			{
				return ComputeHorizontalAlignment(canUseCachedProperties: false);
			}
		}

		// Retrieves the vertical alignment at the beginning of the current
		// manipulation if any when canUseCachedProperty is True, or the result
		// of ComputeHorizontalAlignment otherwise.
		// (Note: name "ComputeHorizontalAlignment" in C++ source comment is a typo for "ComputeVerticalAlignment".)
		internal DMAlignment GetEffectiveVerticalAlignment(bool canUseCachedProperty)
		{
			if (IsInDirectManipulation && canUseCachedProperty)
			{
				return m_currentVerticalAlignment;
			}
			else
			{
				return ComputeVerticalAlignment(canUseCachedProperties: false);
			}
		}

		// Computes the required horizontal alignment to provide to DirectManipulation.
		internal DMAlignment ComputeHorizontalAlignment(bool canUseCachedProperties)
		{
			bool resetHorizontalStretchAlignmentTreatedAsNear = true;
			DMAlignment alignment = DMAlignment.Center;
			DMAlignment contentAlignment = DMAlignment.Center;

			DMAlignment horizontalAlignment = DMAlignment.None;

			// First access the ScrollViewer's HorizontalContentAlignment
			var horizontalContentAlignment = HorizontalContentAlignment;
			switch (horizontalContentAlignment)
			{
				case HorizontalAlignment.Left:
					contentAlignment = DMAlignment.Near;
					break;
				case HorizontalAlignment.Center:
					contentAlignment = DMAlignment.Center;
					break;
				case HorizontalAlignment.Right:
					contentAlignment = DMAlignment.Far;
					break;
				case HorizontalAlignment.Stretch:
					contentAlignment = DMAlignment.Center;
					break;
			}

			// Determine whether the ScrollContentPresenter is the IScrollInfo implementer or not
			var isScrollContentPresenterScrollClient = IsScrollContentPresenterScrollClient();

			if (isScrollContentPresenterScrollClient)
			{
				// When the ScrollContentPresenter is the IScrollInfo implementer,
				// use the horizontal alignment of the manipulated element by default.
				var spContentUIElement = GetContentUIElement();
				if (spContentUIElement is FrameworkElement spContentFE)
				{
					var horizontalContentFEAlignment = spContentFE.HorizontalAlignment;
					switch (horizontalContentFEAlignment)
					{
						case HorizontalAlignment.Left:
							alignment = DMAlignment.Near;
							break;
						case HorizontalAlignment.Right:
							alignment = DMAlignment.Far;
							break;
						case HorizontalAlignment.Stretch:
							resetHorizontalStretchAlignmentTreatedAsNear = false;
							if (m_isHorizontalStretchAlignmentTreatedAsNear)
							{
								alignment = DMAlignment.Near;
							}
							break;
					}
				}

				var scrollMode = GetEffectiveHorizontalScrollMode(canUseCachedProperties);
				if (scrollMode == ScrollMode.Disabled)
				{
					// When horizontal panning is turned off, use None when the content is
					// larger than the viewport in order to keep the zoom center between the fingers.
					var scrollable = ScrollableWidth;
					if (scrollable > 0)
					{
						alignment = DMAlignment.None;
					}
				}

				var hsbv = GetEffectiveHorizontalScrollBarVisibility(canUseCachedProperty: false);
				if (hsbv == ScrollBarVisibility.Disabled)
				{
					// When horizontal scrollbar visibility is disabled, the content is cut off if it's
					// wider than the viewport, and the HorizontalOffset can only be 0.0f. If the zoom factor
					// is larger than 1.0f, and the content is horizontally scrollable, pick a Left alignment
					var scrollable = ScrollableWidth;
					if (scrollable > 0)
					{
						alignment = DMAlignment.Near;
					}
				}
			}
			else
			{
				alignment = contentAlignment;
			}

			horizontalAlignment = alignment;

			// TODO Uno: Phase 6 — header child Unlock-center logic. Headers aren't ported yet.
			// if (horizontalAlignment == DMAlignment.Near && m_trElementScrollContentPresenter is { } pScrollContentPresenter)
			// {
			//     if (pScrollContentPresenter.IsTopLeftHeaderChild_New || pScrollContentPresenter.IsTopHeaderChild_New || pScrollContentPresenter.IsLeftHeaderChild_New)
			//     {
			//         horizontalAlignment = (DMAlignment)((int)alignment + (int)DMAlignment.Unlocked);
			//     }
			// }

			if (resetHorizontalStretchAlignmentTreatedAsNear)
			{
				m_isHorizontalStretchAlignmentTreatedAsNear = false;
			}

			return horizontalAlignment;
		}

		// Computes the required vertical alignment to provide to DirectManipulation.
		internal DMAlignment ComputeVerticalAlignment(bool canUseCachedProperties)
		{
			bool resetVerticalStretchAlignmentTreatedAsNear = true;
			DMAlignment alignment = DMAlignment.Center;
			DMAlignment contentAlignment = DMAlignment.Center;

			// Work around for ScrollViewer / ScrollContentPresenter bug Windows Blue 358691
			// Only forced by Hub on phone
			if (m_isNearVerticalAlignmentForced)
			{
				return DMAlignment.Near;
			}

			DMAlignment verticalAlignment = DMAlignment.None;

			// First access the ScrollViewer's VerticalContentAlignment
			var verticalContentAlignment = VerticalContentAlignment;
			switch (verticalContentAlignment)
			{
				case VerticalAlignment.Top:
					contentAlignment = DMAlignment.Near;
					break;
				case VerticalAlignment.Center:
					contentAlignment = DMAlignment.Center;
					break;
				case VerticalAlignment.Bottom:
					contentAlignment = DMAlignment.Far;
					break;
				case VerticalAlignment.Stretch:
					contentAlignment = DMAlignment.Center;
					break;
			}

			// Determine whether the ScrollContentPresenter is the IScrollInfo implementer or not
			var isScrollContentPresenterScrollClient = IsScrollContentPresenterScrollClient();

			if (isScrollContentPresenterScrollClient)
			{
				// When the ScrollContentPresenter is the IScrollInfo implementer,
				// use the vertical alignment of the manipulated element by default.
				var spContentUIElement = GetContentUIElement();
				if (spContentUIElement is FrameworkElement spContentFE)
				{
					var verticalContentFEAlignment = spContentFE.VerticalAlignment;
					switch (verticalContentFEAlignment)
					{
						case VerticalAlignment.Top:
							alignment = DMAlignment.Near;
							break;
						case VerticalAlignment.Bottom:
							alignment = DMAlignment.Far;
							break;
						case VerticalAlignment.Stretch:
							resetVerticalStretchAlignmentTreatedAsNear = false;
							if (m_isVerticalStretchAlignmentTreatedAsNear)
							{
								alignment = DMAlignment.Near;
							}
							break;
					}
				}

				var scrollMode = GetEffectiveVerticalScrollMode(canUseCachedProperties);
				if (scrollMode == ScrollMode.Disabled)
				{
					// When vertical panning is turned off, use None when the content is
					// larger than the viewport in order to keep the zoom center between the fingers.
					var scrollable = ScrollableHeight;
					if (scrollable > 0)
					{
						alignment = DMAlignment.None;
					}
				}

				var vsbv = GetEffectiveVerticalScrollBarVisibility(canUseCachedProperty: false);
				if (vsbv == ScrollBarVisibility.Disabled)
				{
					// When vertical scrollbar visibility is disabled, the content is cut off if it's
					// taller than the viewport, and the VerticalOffset can only be 0.0f. If the zoom factor
					// is larger than 1.0f, and the content is vertically scrollable, pick a Top alignment
					var scrollable = ScrollableHeight;
					if (scrollable > 0)
					{
						alignment = DMAlignment.Near;
					}
				}
			}
			else
			{
				alignment = contentAlignment;
			}

			verticalAlignment = alignment;

			// TODO Uno: Phase 6 — header child Unlock-center logic. Headers aren't ported yet.

			if (resetVerticalStretchAlignmentTreatedAsNear)
			{
				m_isVerticalStretchAlignmentTreatedAsNear = false;
			}

			return verticalAlignment;
		}

		// Gets a value indicating whether the current ScrollContentPresenter is the IScrollInfo implementer.
		// Uses the ScrollContentPresenter::IsScrollClient(...) method.
		internal bool IsScrollContentPresenterScrollClient()
		{
			if (m_trElementScrollContentPresenter is { } pScp)
			{
				return pScp.IsScrollClient();
			}
			return false;
		}

		// Retrieves the effective horizontal scroll mode: m_currentHorizontalScrollMode or
		// get_HorizontalScrollMode depending on whether there is an active manip or not.
		internal ScrollMode GetEffectiveHorizontalScrollMode(bool canUseCachedProperty)
		{
			if (IsInDirectManipulation && canUseCachedProperty)
			{
				return m_currentHorizontalScrollMode;
			}
			else
			{
				return HorizontalScrollMode;
			}
		}

		// Retrieves the effective vertical scroll mode: m_currentVerticalScrollMode or
		// get_VerticalScrollMode depending on whether there is an active manip or not.
		internal ScrollMode GetEffectiveVerticalScrollMode(bool canUseCachedProperty)
		{
			if (IsInDirectManipulation && canUseCachedProperty)
			{
				return m_currentVerticalScrollMode;
			}
			else
			{
				return VerticalScrollMode;
			}
		}

		// Retrieves the effective zoom mode: m_currentZoomMode or get_ZoomMode
		// depending on whether there is an active manip or not.
		internal ZoomMode GetEffectiveZoomMode(bool canUseCachedProperty)
		{
			if (IsInDirectManipulation && canUseCachedProperty)
			{
				return m_currentZoomMode;
			}
			else
			{
				return ZoomMode;
			}
		}

		// Retrieves the left and top margins of the provided element.
		internal static void GetTopLeftMargins(UIElement pElement, out double topMargin, out double leftMargin)
		{
			topMargin = 0;
			leftMargin = 0;

			if (pElement is FrameworkElement spFrameworkElement)
			{
				var margins = spFrameworkElement.Margin;
				topMargin = margins.Top;
				leftMargin = margins.Left;
			}
		}

		// Called by internal controls to apply a pseudo-LayoutTransform
		// to the ScrollViewer.Content element.
		internal void SetLayoutSize(global::Windows.Foundation.Size layoutSize)
		{
			if (layoutSize.Width != m_layoutSize.Width || layoutSize.Height != m_layoutSize.Height)
			{
				m_layoutSize = layoutSize;

				// TODO Uno: Phase 4 — invalidate the inner SCP and re-measure with the computed pixel viewport
				// once m_trElementScrollContentPresenter is wired up.
				// if (m_trElementScrollContentPresenter is not null)
				// {
				//     m_trElementScrollContentPresenter.InvalidateMeasure();
				//     ComputePixelViewportWidth(null, false, out var widthViewport);
				//     ComputePixelViewportHeight(null, false, out var heightViewport);
				//     m_trElementScrollContentPresenter.Measure(new Size((float)widthViewport, (float)heightViewport));
				// }
			}
		}

		// Called at the end of a DM manipulation when the first layout occurs
		// after receiving the DMManipulationCompleted notification.
		internal void PostDirectManipulationLayoutRefreshed()
		{
			m_isInDirectManipulationCompletion = false;
			NotifyLayoutRefreshed();
		}

		// Member of the IScrollOwner internal contract. Allows the interface consumer to notify this ScrollViewer
		// that an ArrangeOverride occurred after the consumer gets an IManipulationDataProvider::UpdateInManipulation(...)
		// call with isInLiveTree=True. Also called by PostDirectManipulationLayoutRefreshed during the first
		// ScrollContentPresenter layout after a DManip completes.
		internal void NotifyLayoutRefreshed()
		{
			// TODO Uno: Phase 4 — port OnPrimaryContentChanged. The DM-side syncing path is not wired up yet.
			// OnPrimaryContentChanged(
			//     layoutRefreshed: true,
			//     boundsChanged: false,
			//     horizontalAlignmentChanged: false,
			//     verticalAlignmentChanged: false,
			//     zoomFactorBoundaryChanged: false);
		}

		// Register this instance as being under control of a semantic zoom.
		internal void RegisterAsSemanticZoomHost()
		{
			m_ignoreSemanticZoomNavigationInput = true;

			// TODO Uno: Phase 4 — once OnApplyTemplate populates m_trElementScrollContentPresenter, route to
			// SCP.RegisterAsSemanticZoomPresenter().
			// if (m_trElementScrollContentPresenter is not null)
			// {
			//     m_trElementScrollContentPresenter.RegisterAsSemanticZoomPresenter();
			// }
		}

		// Indicates whether we're at our highest zoom factor (as defined by MaxZoomFactor).
		internal bool IsAtMaxZoom()
		{
			var maxZoomFactor = MaxZoomFactor;
			var currentZoomFactor = ZoomFactor;
			return DoubleUtil.GreaterThanOrClose(currentZoomFactor, maxZoomFactor);
		}

		// Indicates whether we're at our lowest zoom factor (as defined by MinZoomFactor).
		internal bool IsAtMinZoom()
		{
			var minZoomFactor = MinZoomFactor;
			var currentZoomFactor = ZoomFactor;
			return DoubleUtil.LessThanOrClose(currentZoomFactor, minZoomFactor);
		}

		// Called by HorizontalSnapPointsChangedHandler and
		// VerticalSnapPointsChangedHandler when snap points changed
		// or by OnZoomSnapPointsCollectionChanged when the ZoomSnapPoints observable collection changed,
		// or by OnSnapPointsAffectingPropertyChanged when a property affecting snap points changed.
		internal void OnSnapPointsChanged(DMMotionTypes motionType)
		{
			// TODO Uno: Phase 5 — push snap-point notification to the manipulation handler once the DM
			// adapter exists. The native WinUI implementation calls
			// CoreImports::ManipulationHandler_NotifySnapPointsChanged here.
		}

		// Called when the Content property changed.
		// The current content is the new one at this point.
		internal void OnContentPropertyChanged()
		{
			m_isHorizontalStretchAlignmentTreatedAsNear = false;
			m_isVerticalStretchAlignmentTreatedAsNear = false;

			// TODO Uno: Phase 4 — port OnManipulatabilityAffectingPropertyChanged.
			// OnManipulatabilityAffectingPropertyChanged(
			//     pIsInLiveTree: null,
			//     isCachedPropertyChanged: false,
			//     isContentChanged: true,
			//     isAffectingConfigurations: false,
			//     isAffectingTouchConfiguration: false);
		}

		// Called when a characteristic changes that affects the DM viewport configurations.
		internal void OnViewportConfigurationsAffectingPropertyChanged()
		{
			if (m_hManipulationHandler is not null)
			{
				if (IsInDirectManipulation)
				{
					// Viewport configurations need to be refreshed after the current manipulation completes.
					m_areViewportConfigurationsInvalid = true;
				}
				else if (m_canManipulateElementsByTouch || m_canManipulateElementsNonTouch || m_canManipulateElementsWithBringIntoViewport)
				{
					m_areViewportConfigurationsInvalid = false;
					// TODO Uno: Phase 4 — port GetManipulationConfigurations + OnViewportAffectingPropertyChanged.
					// The DM viewport-configuration push is part of the DM adapter (Phase 4) and isn't wired up
					// yet on Skia. Until then, this branch is a no-op (the m_areViewportConfigurationsInvalid
					// gate above already handles the in-manipulation case).
				}
			}
		}

		// Called when the input manager may need to be told that a manipulatable element has changed.
		internal void NotifyManipulatableElementChanged()
		{
			if (m_hManipulationHandler is not null)
			{
				var pManipulatableElementNoRef = GetContentUIElement();

				if ((m_trManipulatableElement as object) != (pManipulatableElementNoRef as object))
				{
					// Refresh the m_trScrollSnapPointsInfo member based on the new content.
					// The snap point event handlers are unhooked. The NotifyManipulatableElementChanged
					// call below will cause the manipulation handler to stop listening to notifications.
					// i.e. m_isManipulationHandlerInterestedInNotifications will be reset to FALSE.
					// Any subsequent manipulation will cause the potentially new snap points to be pushed
					// to DirectManipulation.
					// TODO Uno: Phase 5 — port RefreshScrollSnapPointsInfo.
					// RefreshScrollSnapPointsInfo();

					if (m_trManipulatableElement is null && m_trElementScrollContentPresenter is { } pScp)
					{
						// The old manipulatable element was null. Unparent the potential headers so
						// they get added to the CDMViewport that is about to be created.
						// TODO Uno: Phase 6 — header unparenting + InvalidateMeasure.
					}

					// TODO Uno: Phase 4 — let the ManipulationHandler know about this manipulatable element change.
					// CoreImports::ManipulationHandler_NotifyManipulatableElementChanged(...) — DM-only.

					m_trManipulatableElement = pManipulatableElementNoRef;
				}
			}
		}

		// Used to tell the container if the manipulation handler wants to be aware of manipulation
		// characteristic changes even though no manipulation is in progress.
		internal void SetManipulationHandlerWantsNotifications(UIElement pManipulatedElement, bool wantsNotifications)
		{
			global::System.Diagnostics.Debug.Assert(pManipulatedElement is not null);

			m_isManipulationHandlerInterestedInNotifications = wantsNotifications;
			if (!wantsNotifications && !IsInDirectManipulation)
			{
				// TODO Uno: Phase 5 — port UnhookScrollSnapPointsInfoEvents.
				// UnhookScrollSnapPointsInfoEvents(isForHorizontalSnapPoints: true);
				// UnhookScrollSnapPointsInfoEvents(isForHorizontalSnapPoints: false);
			}
		}

		// Called when a property that changes responsiveness to occlusions changes.
		internal void OnReduceViewportForCoreInputViewOcclusionsChanged()
		{
			// TODO Uno: Phase 5 — CoreInputView occlusion subscription. The Win32 CoreInputView API isn't
			// available on Skia; soft-keyboard reflow on Skia happens via a different InputManager path.
			// For now this is a no-op so the property change doesn't fire any unsupported native call.
		}

		// Called when the ScrollViewer.CanContentRenderOutsideBounds property changed.
		internal void OnCanContentRenderOutsideBoundsChanged(object newValue)
		{
			if (m_trElementScrollContentPresenter is { } scp)
			{
				bool canContentRenderOutsideBounds = newValue switch
				{
					bool b => b,
					null => false,
					_ => Convert.ToBoolean(newValue, global::System.Globalization.CultureInfo.InvariantCulture),
				};
				// TODO Uno: Phase 4 — once SCP CanContentRenderOutsideBoundsProperty is implemented on Skia, push it through:
				// scp.CanContentRenderOutsideBounds = canContentRenderOutsideBounds;
				_ = canContentRenderOutsideBounds; // suppress unused warning until DP is wired.
			}
		}

		// Changes the bottom margin of the ScrollViewer's child to reflow the
		// content around docked CoreInputView occlusions such as the software
		// keyboard.
		internal void ReflowAroundCoreInputViewOcclusions()
		{
			// TODO Uno: Phase 5 — CoreInputView occlusion-driven reflow. The Win32 CoreInputView API isn't
			// available on Skia; the equivalent path is in InputManager.PointerManager / OnScreenKeyboard.
			// Stubbed for now so the property-changed handler can call it without a side effect.
		}

		// Called to determine if our manipulation data is stale and we need to bring into view.
		internal bool IsBringIntoViewportNeeded()
		{
			// We might not have a manipulation handler yet.  If not, assume we need a bring into view.
			if (m_hManipulationHandler is null)
			{
				return true;
			}

			// Get the manipulated element
			var pManipulatedElementNoRef = GetContentUIElement();

			if (pManipulatedElementNoRef is null)
			{
				// No content element so content can't exceed viewport
				return false;
			}

			// TODO Uno: Phase 4 — full IsBringIntoViewportNeeded port. The C++ source compares the current
			// DM transform with the XAML offsets/zoom and returns true when they have diverged. Until DM
			// adapter exists, return false (no sync needed).
			return false;
		}

		// Synchonizes the ScrollData's m_ComputedOffset and m_Offset fields.
		internal void SynchronizeScrollOffsets()
		{
			var spScrollInfo = GetScrollInfo();
			if (spScrollInfo is null)
			{
				return;
			}

			// Synchonize the ScrollData's m_ComputedOffset.X and m_Offset.X fields
			var offset = spScrollInfo.GetHorizontalOffset();
			spScrollInfo.SetHorizontalOffset(offset);

			// Synchonize the ScrollData's m_ComputedOffset.Y and m_Offset.Y fields
			offset = spScrollInfo.GetVerticalOffset();
			spScrollInfo.SetVerticalOffset(offset);
		}

		// after the thumb drag completes, we need to push the cached values to the scrollinfo
		internal void SynchronizeScrollOffsetsAfterThumbDeferring()
		{
			var spScrollInfo = GetScrollInfo();
			if (spScrollInfo is not null)
			{
				// we have completed a drag, so synchronize
				if (m_horizontalOffsetCached != -1.0)
				{
					spScrollInfo.SetHorizontalOffset(m_horizontalOffsetCached);
				}
				if (m_verticalOffsetCached != -1.0)
				{
					spScrollInfo.SetVerticalOffset(m_verticalOffsetCached);
				}
			}

			m_horizontalOffsetCached = m_verticalOffsetCached = -1.0;
		}

		// Obtains the zoom action (if any) DM will attempt if given the provided key combination.
		internal static ZoomDirection GetKeyboardMessageZoomAction(VirtualKeyModifiers keyModifiers, VirtualKey key)
		{
			var messageZoomDirection = ZoomDirection.None;

			// Filter out the shift key, we are not sensitive to it.
			// This is the design for now, will be reviewed for RC.
			keyModifiers &= ~VirtualKeyModifiers.Shift;

			if (keyModifiers == VirtualKeyModifiers.Control)
			{
				if (key == VirtualKey.Subtract
					|| key == SCROLLVIEWER_KEYCODE_MINUS)
				{
					messageZoomDirection = ZoomDirection.Out;
				}
				else if (key == VirtualKey.Add
					|| key == SCROLLVIEWER_KEYCODE_EQUALS)
				{
					messageZoomDirection = ZoomDirection.In;
				}
			}

			return messageZoomDirection;
		}

		// Called by a control interested in knowning DirectManipulation state changes.
		// Only one listener can declare itself at once.
		internal void SetDirectManipulationStateChangeHandlerCore(IDirectManipulationStateChangeHandler pDMStateChangeHandler)
		{
			global::System.Diagnostics.Debug.Assert(pDMStateChangeHandler is null || m_pDMStateChangeHandler is null);
			m_pDMStateChangeHandler = pDMStateChangeHandler;
		}

		// allows SeZo to remove the indicators from view temporarily during view change.
		internal void BlockIndicatorsFromShowing()
		{
			if (!m_blockIndicators && IsConscious())
			{
				m_blockIndicators = true;

				VisualStateManager.GoToState(this, "NoIndicator", false);
				if (!m_hasNoIndicatorStateStoryboardCompletedHandler)
				{
					m_showingMouseIndicators = false;
				}

				// TODO Uno: Phase 4 — call ScrollBar.BlockIndicatorFromShowing() once that ScrollBar method
				// is available on the managed Skia path.
				// m_trElementHorizontalScrollBar?.BlockIndicatorFromShowing();
				// m_trElementVerticalScrollBar?.BlockIndicatorFromShowing();
				m_keepIndicatorsShowing = false;
			}
		}

		internal void ResetBlockIndicatorsFromShowing()
		{
			m_blockIndicators = false;
			// TODO Uno: Phase 4 — call ScrollBar.ResetBlockIndicatorFromShowing() on the two scrollbars.
			// m_trElementHorizontalScrollBar?.ResetBlockIndicatorFromShowing();
			// m_trElementVerticalScrollBar?.ResetBlockIndicatorFromShowing();
		}

		// Change to the correct visual state for the ScrollViewer.
		private protected override void ChangeVisualState(
			// true to use transitions when updating the visual state, false
			// to snap directly to the new visual state.
			bool bUseTransitions)
		{
			if (!IsConscious())
			{
				ShowIndicators();
			}
			else if (!m_keepIndicatorsShowing)
			{
				VisualStateManager.GoToState(this, "NoIndicator", bUseTransitions);
				VisualStateManager.GoToState(this, "ScrollBarSeparatorCollapsed", bUseTransitions);
				if (!m_hasNoIndicatorStateStoryboardCompletedHandler)
				{
					m_showingMouseIndicators = false;
				}
			}
		}

		// Show the appropriate scrolling indicators.
		internal void ShowIndicators()
		{
			var showScrollBarSeparator = !IsConscious();

			if ((!m_blockIndicators || !IsConscious())
				&& !AreBothScrollBarsCollapsed())
			{
				// Mouse indicators dominate if they are already showing or if we have set the flag to prefer them.
				if (m_preferMouseIndicators || m_showingMouseIndicators || !IsConscious())
				{
					if (AreBothScrollBarsVisible() &&
						(m_isPointerOverVerticalScrollbar || m_isPointerOverHorizontalScrollbar))
					{
						VisualStateManager.GoToState(this, "MouseIndicatorFull", true);
						showScrollBarSeparator = true;
					}
					else
					{
						VisualStateManager.GoToState(this, "MouseIndicator", true);
					}

					m_showingMouseIndicators = true;
				}
				else
				{
					VisualStateManager.GoToState(this, "TouchIndicator", true);
				}
			}

			var isEnabled = IsEnabled;

			// Select the proper state for the ScrollBar separator square within the ScrollBarSeparatorStates group:
			if (Uno.UI.Helpers.WinUI.SharedHelpers.IsAnimationsEnabled())
			{
				// When OS animations are turned on, show the square when a ScrollBar is shown unless the ScrollViewer is disabled, using an animation.
				VisualStateManager.GoToState(this, (showScrollBarSeparator && isEnabled) ? "ScrollBarSeparatorExpanded" : "ScrollBarSeparatorCollapsed", true /*useTransitions*/);
			}
			else
			{
				// OS animations are turned off. Show or hide the square depending on the presence of a ScrollBar, without an animation.
				// When the ScrollViewer is disabled, hide the square in sync with the ScrollBar(s).
				if (showScrollBarSeparator)
				{
					VisualStateManager.GoToState(this, isEnabled ? "ScrollBarSeparatorExpandedWithoutAnimation" : "ScrollBarSeparatorCollapsed", true /*useTransitions*/);
				}
				else
				{
					VisualStateManager.GoToState(this, isEnabled ? "ScrollBarSeparatorCollapsedWithoutAnimation" : "ScrollBarSeparatorCollapsed", true /*useTransitions*/);
				}
			}
		}

		// Returns true iff both horizontal and vertical scrollbars are collapsed. Used to skip scroll bar animations.
		internal bool AreBothScrollBarsCollapsed()
			=> m_scrollVisibilityX == Visibility.Collapsed
				&& m_scrollVisibilityY == Visibility.Collapsed;

		// Returns true iff both horizontal and vertical scrollbars are visible.
		internal bool AreBothScrollBarsVisible()
			=> m_scrollVisibilityX == Visibility.Visible
				&& m_scrollVisibilityY == Visibility.Visible;

		// Handler for when the TouchIndicator or MouseIndicator state's storyboard completes animating.
		internal void IndicatorStateStoryboardCompleted(object pUnused1, object pUnused2)
		{
			// If the cursor is currently directly over either scrollbar then don't automatically hide the indicators
			if (!m_keepIndicatorsShowing &&
				!(m_isPointerOverVerticalScrollbar || m_isPointerOverHorizontalScrollbar))
			{
				// Go to the NoIndicator state using transitions.  There should be a delay before the NoIndicator state actually shows.
				// TODO Uno: Phase 4 — call UpdateVisualState() once it's wired up; for now use VisualStateManager.GoToState.
				VisualStateManager.GoToState(this, "NoIndicator", true /*useTransitions*/);
			}
		}

		// Handler for when the NoIndicator state's storyboard completes animating.
		internal void NoIndicatorStateStoryboardCompleted(object pUnused1, object pUnused2)
		{
			global::System.Diagnostics.Debug.Assert(m_hasNoIndicatorStateStoryboardCompletedHandler);
			m_showingMouseIndicators = false;
		}

		// Raises or delays the ViewChanging event with the provided target transform and IsInertial flag.
		// The event is delayed when m_iViewChangingDelay is strictly positive. In that case the event is
		// raised later when m_iViewChangingDelay reaches 0.
		internal void RaiseViewChanging(
			double targetHorizontalOffset,
			double targetVerticalOffset,
			float targetZoomFactor)
		{
			m_isTargetHorizontalOffsetValid = m_isTargetVerticalOffsetValid = m_isTargetZoomFactorValid = true;

			m_targetHorizontalOffset = targetHorizontalOffset;
			m_targetVerticalOffset = targetVerticalOffset;
			m_targetZoomFactor = targetZoomFactor;

			// Now that the view is about to change, clear the potential latest view change request.
			m_targetChangeViewHorizontalOffset = -1.0;
			m_targetChangeViewVerticalOffset = -1.0;
			m_targetChangeViewZoomFactor = -1.0f;

			if (m_iViewChangingDelay > 0)
			{
				m_isViewChangingDelayed = true;
			}
			else
			{
				m_isViewChangingDelayed = false;

				if (ViewChanging is { } pEventSource)
				{
					var spArgs = new ScrollViewerViewChangingEventArgs
					{
						IsInertial = m_isInertial
					};

					var spNextView = new ScrollViewerView(
						m_targetHorizontalOffset,
						m_targetVerticalOffset,
						m_targetZoomFactor);
					spArgs.NextView = spNextView;

					var spProvider = GetInnerManipulationDataProvider();
					if (m_isInertial && m_isInertiaEndTransformValid && spProvider is null)
					{
						var spFinalView = new ScrollViewerView(
							m_inertiaEndHorizontalOffset,
							m_inertiaEndVerticalOffset,
							m_inertiaEndZoomFactor);
						spArgs.FinalView = spFinalView;
					}
					else
					{
						// When the ScrollViewer is not in inertial mode, or the ScrollViewer operates with a
						// logical-based IManipulationDataProvider, the FinalView component of the event args
						// is built with the NextView content.
						spArgs.FinalView = spNextView;
					}
					pEventSource(this, spArgs);
				}
			}
		}

		// Raises the ViewChanged event with the provided IsIntermediate value
		// unless m_iViewChangedDelay is strictly positive. In that case
		// the event is delayed and raised when m_iViewChangedDelay reaches 0 again.
		internal void RaiseViewChanged(bool isIntermediate)
		{
			if (m_iViewChangedDelay > 0)
			{
				m_isViewChangedDelayed = true;
				// The batched up & delayed ViewChanged event will use the isIntermediate value
				// of the latest request.
				m_isDelayedViewChangedIntermediate = isIntermediate;
			}
			else
			{
				if (m_isInIntermediateViewChangedMode)
				{
					// ViewChanged is raised during an 'intermediate mode'
					// This means that ViewChanged with IsIntermediate==False needs to be raised
					// at the end of this 'intermediate mode'.
					m_isViewChangedRaisedInIntermediateMode = true;
				}

				m_isViewChangedDelayed = false;

				// Note: ViewChanged event itself is declared on the cross-platform ScrollViewer.cs
				// partial; raise it via a helper that can reach into that field.
				RaiseViewChangedEvent(isIntermediate);
			}

			if (!isIntermediate)
			{
				// Reset pending shifts.
				// Note: m_pendingViewportShiftX/Y live on the Anchoring partial.
				ResetPendingViewportShifts();
			}
		}

		// Bridge into the cross-platform ViewChanged event field.
		private void RaiseViewChangedEvent(bool isIntermediate)
		{
			var handlers = ViewChanged;
			if (handlers is not null)
			{
				var args = new ScrollViewerViewChangedEventArgs { IsIntermediate = isIntermediate };
				handlers.Invoke(this, args);
			}
		}

		// Pending viewport shifts live on the Anchoring partial; reset them.
		private void ResetPendingViewportShifts()
		{
			m_pendingViewportShiftX = 0.0;
			m_pendingViewportShiftY = 0.0;
		}

		// Increments m_iViewChangingDelay to delay any
		// potential attempt at raising the ViewChanging event.
		internal void DelayViewChanging() => m_iViewChangingDelay++;

		// Increments m_iViewChangedDelay to delay any
		// potential attempt at raising the ViewChanged event.
		internal void DelayViewChanged() => m_iViewChangedDelay++;

		// Decrements m_iViewChangingDelay and checks if a
		// ViewChanging notification was delayed and can
		// now be raised. DelayViewChanging and FlushViewChanging
		// need to go in pairs.
		internal void FlushViewChanging()
		{
			global::System.Diagnostics.Debug.Assert(m_iViewChangingDelay > 0);

			m_iViewChangingDelay--;

			if (m_iViewChangingDelay == 0 && m_isViewChangingDelayed)
			{
				RaiseViewChanging(m_targetHorizontalOffset, m_targetVerticalOffset, m_targetZoomFactor);
			}
		}

		// Decrements m_iViewChangedDelay and checks if a
		// ViewChanged notification was delayed and can
		// now be raised. DelayViewChanged and FlushViewChanged
		// need to go in pairs.
		internal void FlushViewChanged()
		{
			global::System.Diagnostics.Debug.Assert(m_iViewChangedDelay > 0);

			m_iViewChangedDelay--;

			if (m_iViewChangedDelay == 0 && m_isViewChangedDelayed)
			{
				RaiseViewChanged(m_isDelayedViewChangedIntermediate /*isIntermediate*/);
			}
		}

		// Called at the beginning of an operation that may cause several ViewChanged events, like a DM manip.
		internal void EnterIntermediateViewChangedMode()
		{
			if (!m_isInIntermediateViewChangedMode)
			{
				m_isInIntermediateViewChangedMode = true;

				// This flag is set to True the first time ViewChanged is raised during this multi-notification operation.
				m_isViewChangedRaisedInIntermediateMode = false;
			}
		}

		// Called at the end of an operation that may have caused several ViewChanged events, like a DM manip.
		internal void LeaveIntermediateViewChangedMode(bool raiseFinalViewChanged)
		{
			if (m_isInIntermediateViewChangedMode)
			{
				m_isInIntermediateViewChangedMode = false;

				if (m_isViewChangedRaisedInIntermediateMode)
				{
					m_isViewChangedRaisedInIntermediateMode = false;

					if (raiseFinalViewChanged)
					{
						// Mark the end of a multi-notification operation
						RaiseViewChanged(false /*isIntermediate*/);
					}
				}
			}
		}

		internal void RaiseDirectManipulationStarted()
		{
			DirectManipulationStarted?.Invoke(this, EventArgs.Empty);
		}

		internal void RaiseDirectManipulationCompleted()
		{
			DirectManipulationCompleted?.Invoke(this, EventArgs.Empty);
		}

		internal bool IsThumbDragging()
		{
			var result = false;

			if (m_trElementHorizontalScrollBar is { } hScrollBar)
			{
				result |= hScrollBar.IsDragging;
			}
			if (m_trElementVerticalScrollBar is { } vScrollBar)
			{
				result |= vScrollBar.IsDragging;
			}

			return result;
		}

		// Set explicit DMOverpanMode flags to configure overpan modes for horizontal and vertical directions.
		internal void SetOverpanModes(DMOverpanMode horizontalOverpanMode, DMOverpanMode verticalOverpanMode)
		{
			var horizontalOverpanModeChanged = horizontalOverpanMode != m_horizontalOverpanMode;
			var verticalOverpanModeChanged = verticalOverpanMode != m_verticalOverpanMode;

			if (horizontalOverpanModeChanged || verticalOverpanModeChanged)
			{
				m_horizontalOverpanMode = horizontalOverpanMode;
				m_verticalOverpanMode = verticalOverpanMode;
				// TODO Uno: Phase 4 — port OnViewportAffectingPropertyChanged.
				// OnViewportAffectingPropertyChanged(
				//     boundsChanged: false,
				//     touchConfigurationChanged: false,
				//     nonTouchConfigurationChanged: false,
				//     configurationsChanged: false,
				//     chainedMotionTypesChanged: false,
				//     horizontalOverpanModeChanged,
				//     verticalOverpanModeChanged,
				//     out _);
			}
		}

		// Prevents overpan effect so that panning will hard-stop at the boundaries of the scrollable region.
		internal void DisableOverpanImpl()
		{
			SetOverpanModes(DMOverpanMode.None, DMOverpanMode.None);
		}

		// Reenables overpan.
		internal void EnableOverpanImpl()
		{
			SetOverpanModes(DMOverpanMode.Default, DMOverpanMode.Default);
		}

		// Enters the mode where the child's actual size is used for
		// the extent exposed through IScrollInfo.
		// (C++ source line 16083)
		internal void StartUseOfActualSizeAsExtent(bool isHorizontal)
		{
			if (isHorizontal && m_trElementHorizontalScrollBar is { } hScrollBar)
			{
				hScrollBar.StartUseOfActualSizeAsExtent();
			}
			else if (!isHorizontal && m_trElementVerticalScrollBar is { } vScrollBar)
			{
				vScrollBar.StartUseOfActualSizeAsExtent();
			}
		}

		// Leaves the mode where the child's actual size is used for
		// the extent exposed through IScrollInfo.
		// (C++ source line 16097)
		internal void StopUseOfActualSizeAsExtent(bool isHorizontal)
		{
			if (isHorizontal && m_trElementHorizontalScrollBar is { } hScrollBar)
			{
				hScrollBar.StopUseOfActualSizeAsExtent();
			}
			else if (!isHorizontal && m_trElementVerticalScrollBar is { } vScrollBar)
			{
				vScrollBar.StopUseOfActualSizeAsExtent();
			}
		}

		// Loaded event handler.
		// (C++ source line 15035)
		internal void OnLoadedCore()
		{
			m_isLoaded = true;

			// DManip needs to be aware of the content transform immediately via a ZoomToRect call.
			// Prior attempts at synchronizing the XAML and DManip transforms may have failed because a viewport size, in pixels, was still 0.
			// TODO Uno: Phase 4 — port OnPrimaryContentTransformChanged.
			// OnPrimaryContentTransformChanged(translationXChanged: true, translationYChanged: true, zoomFactorChanged: true);
		}

		// Unloaded event handler.
		// (C++ source line 15050)
		internal void OnUnloadedCore()
		{
			m_isLoaded = false;

			m_showingMouseIndicators = false;
			m_keepIndicatorsShowing = false;
		}

		// Determine if content can be scrolled.
		// It can, if for either dimension scrolling is enabled AND content size is greater than size of viewport.
		internal void IsContentScrollable(
			bool ignoreScrollMode,
			bool ignoreScrollBarVisibility,
			out bool isContentHorizontallyScrollable,
			out bool isContentVerticallyScrollable)
		{
			isContentHorizontallyScrollable = false;
			isContentVerticallyScrollable = false;

			if (IsEnabled)
			{
				var spScrollInfo = GetScrollInfo();
				double minOffset = 0.0;
				double viewportSize = 0.0;
				double contentExtentSize = 0.0;
				ScrollMode scrollMode;
				ScrollBarVisibility horizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
				ScrollBarVisibility verticalScrollBarVisibility = ScrollBarVisibility.Disabled;

				scrollMode = GetEffectiveVerticalScrollMode(canUseCachedProperty: true);

				if (spScrollInfo is not null)
				{
					minOffset = spScrollInfo.GetMinVerticalOffset();
					viewportSize = spScrollInfo.GetViewportHeight();
					contentExtentSize = spScrollInfo.GetExtentHeight();
				}
				else
				{
					viewportSize = ViewportHeight;
					contentExtentSize = ExtentHeight;
				}

				if (!ignoreScrollBarVisibility)
				{
					horizontalScrollBarVisibility = HorizontalScrollBarVisibility;
					verticalScrollBarVisibility = VerticalScrollBarVisibility;
				}

				if ((ignoreScrollMode || scrollMode != ScrollMode.Disabled) &&
					(ignoreScrollBarVisibility || verticalScrollBarVisibility != ScrollBarVisibility.Disabled) &&
					(contentExtentSize - minOffset) > viewportSize)
				{
					isContentVerticallyScrollable = true;
				}

				scrollMode = GetEffectiveHorizontalScrollMode(canUseCachedProperty: true);

				if (spScrollInfo is not null)
				{
					minOffset = spScrollInfo.GetMinHorizontalOffset();
					viewportSize = spScrollInfo.GetViewportWidth();
					contentExtentSize = spScrollInfo.GetExtentWidth();
				}
				else
				{
					viewportSize = ViewportWidth;
					contentExtentSize = ExtentWidth;
				}

				if ((ignoreScrollMode || scrollMode != ScrollMode.Disabled) &&
					(ignoreScrollBarVisibility || horizontalScrollBarVisibility != ScrollBarVisibility.Disabled) &&
					(contentExtentSize - minOffset) > viewportSize)
				{
					isContentHorizontallyScrollable = true;
				}
			}
		}

		// Returns the combined size of the headers:
		// - horizontal size is max(TopLeftHeader's width, LeftHeader's width)
		// - vertical size is max(TopLeftHeader's height, TopHeader's height)
		internal void GetHeadersSize(out global::Windows.Foundation.Size pSize)
		{
			pSize = default;

			if (m_trElementScrollContentPresenter is { } pScrollContentPresenter)
			{
				double sizeX = 0.0;
				double sizeY = 0.0;

				// TODO Uno: Phase 6 — port get_TopLeftHeader / get_TopHeader / get_LeftHeader on SCP and the
				// header-margin computation. Headers are not yet wired up on the managed Skia path; this
				// returns (0, 0).

				pSize = new global::Windows.Foundation.Size((float)sizeX, (float)sizeY);
			}
		}

		// Returns the horizontal and vertical ratios between the ScrollViewer effective viewport
		// and its actual size. That viewport is potentially reduced by the presence of headers.
		// Ratios returned depend on the quadrant owning the provided child.
		// If child is not provided, we assume the child is in the content.
		internal void GetViewportRatios(DependencyObject pChild, out global::Windows.Foundation.Size pRatios)
		{
			pRatios = new global::Windows.Foundation.Size(1.0f, 1.0f);

			if (m_trElementScrollContentPresenter is not null)
			{
				GetHeadersSize(out var sizeHeaders);

				if (sizeHeaders.Width > 0.0f || sizeHeaders.Height > 0.0f)
				{
					// TODO Uno: Phase 6 — header ownership lookup + ComputePixelViewportWidth/Height
					// once those land. Until headers are wired up, the ratios stay (1, 1).
				}
			}
		}

		// TODO Uno: Phase 5 — port `IsPanelACarouselPanel`. CarouselPanel is a virtualizing panel used
		// by ComboBox; until it's hooked up here, return false (which is the safe default for
		// non-carousel scenarios — the offset gets clamped, which is correct for normal Scroll panels).
		private bool IsPanelACarouselPanel(bool isForHorizontalOrientation) => false;

		internal float GetZoomedHorizontalOffsetWithPendingShifts()
		{
			var zoomedHorizontalOffset = HorizontalOffset;
			zoomedHorizontalOffset += m_pendingViewportShiftX;
			return (float)zoomedHorizontalOffset;
		}

		internal float GetZoomedVerticalOffsetWithPendingShifts()
		{
			var zoomedVerticalOffset = VerticalOffset;
			zoomedVerticalOffset += m_pendingViewportShiftY;
			return (float)zoomedVerticalOffset;
		}

		// Determines if key press should be forwarded or processed by DM.
		// The determination is made based on whether scrolling and chaining are
		// enabled and viewport is near an edge within the current item.
		// (C++ source line 15074)
		internal bool ShouldContinueRoutingKeyDownEvent(VirtualKey key)
		{
			double offset = 0.0;
			double edge = 0.0;
			bool chainingEnabled = false;
			ScrollMode scrollMode = ScrollMode.Disabled;
			FlowDirection direction = FlowDirection;

			switch (key)
			{
				case VirtualKey.GamepadLeftTrigger:
				case VirtualKey.Up:
					offset = VerticalOffset;
					chainingEnabled = IsVerticalScrollChainingEnabled;
					scrollMode = VerticalScrollMode;
					break;

				case VirtualKey.GamepadRightTrigger:
				case VirtualKey.Down:
					offset = VerticalOffset;
					edge = ScrollableHeight;
					chainingEnabled = IsVerticalScrollChainingEnabled;
					scrollMode = VerticalScrollMode;
					break;

				case VirtualKey.GamepadLeftShoulder:
				case VirtualKey.Left:
					offset = HorizontalOffset;
					if (direction == FlowDirection.RightToLeft)
					{
						edge = ScrollableWidth;
					}
					chainingEnabled = IsHorizontalScrollChainingEnabled;
					scrollMode = HorizontalScrollMode;
					break;

				case VirtualKey.GamepadRightShoulder:
				case VirtualKey.Right:
					offset = HorizontalOffset;
					if (direction == FlowDirection.LeftToRight)
					{
						edge = ScrollableWidth;
					}
					chainingEnabled = IsHorizontalScrollChainingEnabled;
					scrollMode = HorizontalScrollMode;
					break;
			}

			// Methods get_xxxOffset() do not return fractional parts and get_ScrollableXxx() do,
			// define 'near' as within radius of 1 unit.

			if (scrollMode != ScrollMode.Disabled &&
				chainingEnabled &&
				DoubleUtil.LessThanOrClose(Math.Abs(edge - offset), 1.0))
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		// RightTappedUnhandled event handler.
		// Private event.
		// (C++ source line 2589)
		private protected override void OnRightTappedUnhandled(Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs args)
		{
			base.OnRightTappedUnhandled(args);

			if (!args.Handled && m_shouldFocusOnRightTapUnhandled)
			{
				// Set focus to the ScrollViewer to capture key input for scrolling
				bool focused = Focus(FocusState.Pointer);
				args.Handled = focused;
			}

			m_shouldFocusOnRightTapUnhandled = false;
		}

		// Determines if the ScrollViewer should clear focus on pointer released or right tapped.
		// This determination is made based on whether or not it is part of a template for a
		// text control - if it is not, then we should clear focus.
		// (C++ source line 15157)
		internal bool GetShouldClearFocus()
		{
			DependencyObject spCurrent = this;

			while (spCurrent is not null)
			{
				if (spCurrent is TextBox or PasswordBox or RichEditBox)
				{
					return false;
				}

				spCurrent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(spCurrent);
			}

			return true;
		}

		// #region IScrollOwner contract — explicit interface implementation.
		// Each member is a thin adapter onto an existing port method or a
		// scoped Phase-stub. The full WinUI behavior (in particular the 600-line
		// InvalidateScrollInfoImpl and 400-line InvalidateScrollInfo_TryUpdateValues)
		// will land in Phase 4 once the DM adapter is wired up. For now the
		// adapter is functional enough that an SCP that has been told to use
		// the new IScrollOwner pipeline can call back without crashing.

		// Member of the IScrollOwner internal contract.
		// Pushes the latest IScrollInfo state to the scroll bars and the
		// HorizontalOffset / VerticalOffset DPs. The full DM-aware port at
		// C++ source line 4392 will replace this once the adapter lands.
		void IScrollOwner.InvalidateScrollInfoImpl()
		{
			// Batch up any potential ViewChanged events into a single notification
			DelayViewChanged();

			var spScrollInfo = GetScrollInfo();
			if (spScrollInfo is null)
			{
				FlushViewChanged();
				return;
			}

			var horizontalOffset = spScrollInfo.GetHorizontalOffset();
			var verticalOffset = spScrollInfo.GetVerticalOffset();
			var minHorizontalOffset = spScrollInfo.GetMinHorizontalOffset();
			var minVerticalOffset = spScrollInfo.GetMinVerticalOffset();
			var extentWidth = spScrollInfo.GetExtentWidth();
			var extentHeight = spScrollInfo.GetExtentHeight();
			var viewportWidth = spScrollInfo.GetViewportWidth();
			var viewportHeight = spScrollInfo.GetViewportHeight();

			if (m_trElementHorizontalScrollBar is not null)
			{
				if (!m_trElementHorizontalScrollBar.IsDragging)
				{
					m_trElementHorizontalScrollBar.Minimum = minHorizontalOffset;
				}
				m_trElementHorizontalScrollBar.Maximum = Math.Max(minHorizontalOffset, extentWidth - viewportWidth);
				m_trElementHorizontalScrollBar.ViewportSize = viewportWidth;
				if (!m_trElementHorizontalScrollBar.IsDragging)
				{
					m_trElementHorizontalScrollBar.Value = horizontalOffset;
				}
			}

			if (m_trElementVerticalScrollBar is not null)
			{
				if (!m_trElementVerticalScrollBar.IsDragging)
				{
					m_trElementVerticalScrollBar.Minimum = minVerticalOffset;
				}
				m_trElementVerticalScrollBar.Maximum = Math.Max(minVerticalOffset, extentHeight - viewportHeight);
				m_trElementVerticalScrollBar.ViewportSize = viewportHeight;
				if (!m_trElementVerticalScrollBar.IsDragging)
				{
					m_trElementVerticalScrollBar.Value = verticalOffset;
				}
			}

			// Push the offsets to the public DPs directly via SetValue so we don't
			// recurse back through ScrollToHorizontal/VerticalOffsetInternal (which
			// is what may have triggered this InvalidateScrollInfoImpl in the first
			// place). DP-changed handlers will fire ViewChanging/ViewChanged via
			// NotifyHorizontal/VerticalOffsetChanging.
			if (HorizontalOffset != horizontalOffset)
			{
				SetValue(HorizontalOffsetProperty, horizontalOffset);
			}
			if (VerticalOffset != verticalOffset)
			{
				SetValue(VerticalOffsetProperty, verticalOffset);
			}

			FlushViewChanged();
		}

		// Already ported as an instance method; forward through the explicit
		// interface so SCP can call them through the IScrollOwner pipeline.
		void IScrollOwner.NotifyLayoutRefreshed() => NotifyLayoutRefreshed();

		void IScrollOwner.NotifyHorizontalOffsetChanging(double targetHorizontalOffset, double targetVerticalOffset)
			=> NotifyHorizontalOffsetChanging(targetHorizontalOffset, targetVerticalOffset);

		void IScrollOwner.NotifyVerticalOffsetChanging(double targetHorizontalOffset, double targetVerticalOffset)
			=> NotifyVerticalOffsetChanging(targetHorizontalOffset, targetVerticalOffset);

		void IScrollOwner.ScrollToHorizontalOffsetImpl(double offset) => ScrollToHorizontalOffsetInternal(offset);

		void IScrollOwner.ScrollToVerticalOffsetImpl(double offset) => ScrollToVerticalOffsetInternal(offset);

		void IScrollOwner.SetScrollInfo(IScrollInfo value) => PutScrollInfo(value);

		IScrollInfo IScrollOwner.GetScrollInfo() => GetScrollInfo();

		float IScrollOwner.GetZoomFactor() => ZoomFactor;

		// TODO Uno: Phase 4 DM wiring — forwards a pure-inertia keyboard zoom
		// (Ctrl+Plus / Ctrl+Minus) request to DirectManipulation. Stubbed for
		// now; the keyboard zoom path is not exercised on Skia until the DM
		// adapter lands.
		void IScrollOwner.ProcessPureInertiaInputMessage(ZoomDirection zoomDirection)
		{
			_ = zoomDirection;
		}

		// Returns true while DM is in a zoom manipulation. Reflected through
		// the existing MuxInternal m_isInDirectManipulationZoom field so we
		// stay consistent with the rest of the SV state machine. The field
		// stays false until the DM adapter lands but we still surface the
		// real getter for parity with C++.
		bool IScrollOwner.IsInDirectManipulationZoom() => m_isInDirectManipulationZoom;

		// Tracks whether the SV is itself triggering an InvalidateMeasure on
		// its inner panel (bug 261102 / 342668 workaround). Skia's pure-managed
		// scroll path does not have that race, so this is permanently false
		// for now.
		bool IScrollOwner.IsInChildInvalidateMeasure() => false;

		// #endregion

#pragma warning restore IDE0051
#endif
	}
}
