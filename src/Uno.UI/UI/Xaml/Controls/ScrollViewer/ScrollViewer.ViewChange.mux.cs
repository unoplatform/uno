// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollViewer_Partial.cpp, commit 5f9e85113

#nullable disable

using System;
using DirectUI;

namespace Microsoft.UI.Xaml.Controls
{
	partial class ScrollViewer
	{
#if __SKIA__
#pragma warning disable IDE0051 // Private member is unused (placeholder for full impl)

		// #region Public events declared by the IDL surface but only currently implemented for Skia

		// Occurs when the view is about to change.
		public event EventHandler<ScrollViewerViewChangingEventArgs> ViewChanging;

		// Occurs when DirectManipulation starts.
		public event EventHandler<object> DirectManipulationStarted;

		// Occurs when DirectManipulation completes.
		public event EventHandler<object> DirectManipulationCompleted;

		// #endregion

		// #region View change notification batching ported from ScrollViewer_Partial.cpp

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

				// TODO Uno: ViewChanged event itself is declared on the cross-platform ScrollViewer.cs
				// partial; raise it via a helper that can reach into that field. Here we route through
				// a partial method so that file remains the source of truth for the event field.
				RaiseViewChangedEvent(isIntermediate);
			}

			if (!isIntermediate)
			{
				// Reset pending shifts.
				// Note: m_pendingViewportShiftX/Y live on the Anchoring partial.
				ResetPendingViewportShifts();
			}
		}

		// Bridge into the cross-platform ViewChanged event. The actual EventHandler field is on
		// `ScrollViewer.cs` (the cross-platform partial). The bridging method is declared as a
		// partial method here and implemented inline below to keep the public surface stable.
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

		// #endregion

		// #region IScrollOwner Notify offset/zoom-factor changing methods

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

		// #endregion

		// TODO Uno: Phase 4 — port `GetInnerManipulationDataProvider`. Returns the inner panel implementing
		// IManipulationDataProvider (e.g. virtualizing-panel). Until that contract is wired up, no inner
		// provider exists in the managed Skia path.
		private object GetInnerManipulationDataProvider() => null;

		// #region UpdateCanScroll / OnScrollBarVisibilityChanged

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

		// #endregion

#pragma warning restore IDE0051
#endif
	}
}
