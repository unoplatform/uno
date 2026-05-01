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

		// #region Zoom factor change pipeline ported from ScrollViewer_Partial.cpp

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

		// #endregion

#pragma warning restore IDE0051
#endif
	}
}
