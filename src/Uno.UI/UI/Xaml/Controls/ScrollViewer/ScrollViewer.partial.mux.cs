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

		// Public and deprecated version of ScrollToHorizontalOffsetInternal.
		public void ScrollToHorizontalOffset(double offset) => ScrollToHorizontalOffsetInternal(offset);

		// Scrolls the content within the ScrollViewer to the specified
		// horizontal offset position.
		internal void ScrollToHorizontalOffsetInternal(double offset)
			=> HandleHorizontalScroll(ScrollEventType.ThumbPosition, offset);

		// Public and deprecated version of ScrollToVerticalOffsetInternal.
		public void ScrollToVerticalOffset(double offset) => ScrollToVerticalOffsetInternal(offset);

		// Scrolls the content within the ScrollViewer to the specified vertical
		// offset position.
		internal void ScrollToVerticalOffsetInternal(double offset)
			=> HandleVerticalScroll(ScrollEventType.ThumbPosition, offset);

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

		// Handles the vertical ScrollBar.Scroll event and updates the UI.
		internal void HandleVerticalScroll(ScrollEventType scrollEventType, double offset = 0.0)
		{
			// If style changes and Content cannot be found - just exit.
			var spScrollInfo = GetScrollInfo();
			if (spScrollInfo is null)
			{
				return;
			}

			var oldOffset = spScrollInfo.VerticalOffset;
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

			var oldOffset = spScrollInfo.HorizontalOffset;
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
			pScrollInfo.CanHorizontallyScroll = horizontal != ScrollBarVisibility.Disabled;

			if (vertical == ScrollBarVisibility.Disabled)
			{
				// When the vertical scrollbar becomes disabled, the vertical offset needs to be reset to 0.
				pScrollInfo.SetVerticalOffset(0.0);
			}
			pScrollInfo.CanVerticallyScroll = vertical != ScrollBarVisibility.Disabled;
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
				// Zoom factor change has to be pushed to DManip as the XAML and DManip transforms are kept in sync.
				// TODO Uno: Phase 4 — port ChangeViewInternal. For now, the zoom factor change is applied directly
				// without DM-side syncing — acceptable on the managed Skia path.
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

		// Synchonizes the ScrollData's m_ComputedOffset and m_Offset fields.
		internal void SynchronizeScrollOffsets()
		{
			var spScrollInfo = GetScrollInfo();
			if (spScrollInfo is null)
			{
				return;
			}

			// Synchonize the ScrollData's m_ComputedOffset.X and m_Offset.X fields
			var offset = spScrollInfo.HorizontalOffset;
			spScrollInfo.SetHorizontalOffset(offset);

			// Synchonize the ScrollData's m_ComputedOffset.Y and m_Offset.Y fields
			offset = spScrollInfo.VerticalOffset;
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

		// Returns true iff both horizontal and vertical scrollbars are collapsed. Used to skip scroll bar animations.
		internal bool AreBothScrollBarsCollapsed()
			=> m_scrollVisibilityX == Visibility.Collapsed
				&& m_scrollVisibilityY == Visibility.Collapsed;

		// Returns true iff both horizontal and vertical scrollbars are visible.
		internal bool AreBothScrollBarsVisible()
			=> m_scrollVisibilityX == Visibility.Visible
				&& m_scrollVisibilityY == Visibility.Visible;

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

		// Pending viewport shifts live on the Anchoring partial; until that's wired up, this is a stub.
		private void ResetPendingViewportShifts()
		{
			// TODO Uno: Phase 5 — clear m_pendingViewportShiftX/Y on the Anchoring partial.
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

				// TODO Uno: Phase 4 — wire GetEffectiveVerticalScrollMode (which honours the manipulation-time cache).
				// For now use the live property.
				scrollMode = VerticalScrollMode;

				if (spScrollInfo is not null)
				{
					minOffset = spScrollInfo.MinVerticalOffset;
					viewportSize = spScrollInfo.ViewportHeight;
					contentExtentSize = spScrollInfo.ExtentHeight;
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

				// TODO Uno: Phase 4 — wire GetEffectiveHorizontalScrollMode (which honours the manipulation-time cache).
				scrollMode = HorizontalScrollMode;

				if (spScrollInfo is not null)
				{
					minOffset = spScrollInfo.MinHorizontalOffset;
					viewportSize = spScrollInfo.ViewportWidth;
					contentExtentSize = spScrollInfo.ExtentWidth;
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

		// TODO Uno: Phase 5 — port `IsPanelACarouselPanel`. CarouselPanel is a virtualizing panel used
		// by ComboBox; until it's hooked up here, return false (which is the safe default for
		// non-carousel scenarios — the offset gets clamped, which is correct for normal Scroll panels).
		private bool IsPanelACarouselPanel(bool isForHorizontalOrientation) => false;

#pragma warning restore IDE0051
#endif
	}
}
