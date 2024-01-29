using System;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Uno.UI;
using NotImplementedException = System.NotImplementedException;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	partial class FlyoutBase
	{
		internal bool UsePickerFlyoutTheme { get; set; }

		internal void PlaceFlyoutForDateTimePicker(Point point)
		{
			//wf::Size presenterSize = { };
			//ctl::ComPtr<PickerFlyoutThemeTransition> spTransitionAsPickerFlyoutThemeTransition;

			m_isPositionedForDateTimePicker = true;

			// **************************************************************************************
			// UNO-FIX: Ensure the flyout stays in visible bounds
			// **************************************************************************************
			var childRect = ((FrameworkElement)_popup.Child).GetAbsoluteBoundsRect();
			var rect = new Rect(point, childRect.Size);
			var visibleBounds = XamlRoot?.VisualTree.VisibleBounds ?? default;

			if (rect.Right > visibleBounds.Right)
			{
				rect.X = visibleBounds.Right - rect.Width;
			}

			if (rect.Bottom > visibleBounds.Bottom)
			{
				rect.Y = visibleBounds.Bottom - rect.Height;
			}

			if (rect.Top < visibleBounds.Top)
			{
				rect.Y = visibleBounds.Top;
			}

			if (rect.Left < visibleBounds.Left)
			{
				rect.X = visibleBounds.Left;
			}
			// **************************************************************************************

			// **************************************************************************************
			// UNO-FIX: Make the location relative to the Anchor
			// **************************************************************************************
			var target = Target;
			var targetTransform = target.TransformToVisual(default).Inverse;
			var relativeLocation = targetTransform.TransformPoint(rect.Location);
			// **************************************************************************************


			//_popup.CustomLayouter = new PickerLayouter(this);
			SetPopupPosition(target, relativeLocation);

			//IFC_RETURN(ForwardPopupFlowDirection());
			//SetTargetPosition(point);
			//IFC_RETURN(ApplyTargetPosition());
			//IFC_RETURN(GetPresenterSize(&presenterSize));
			//IFC_RETURN(PerformPlacement(presenterSize));

			//spTransitionAsPickerFlyoutThemeTransition = m_tpThemeTransition.AsOrNull<PickerFlyoutThemeTransition>();

			//if (spTransitionAsPickerFlyoutThemeTransition)
			//{
			//	DOUBLE popupOffset = 0.0;
			//	wf::Point centerOfTarget = { 0, 0 };

			//	IFC_RETURN(m_tpPopup->get_VerticalOffset(&popupOffset));

			//	if (m_tpPlacementTarget)
			//	{
			//		DOUBLE actualHeight = 0.0;
			//		ctl::ComPtr<xaml::Media::IGeneralTransform> spTransformFromTargetToWindow;

			//		m_tpPlacementTarget.Cast<FrameworkElement>()->TransformToVisual(nullptr, &spTransformFromTargetToWindow);
			//		IFC_RETURN(spTransformFromTargetToWindow->TransformPoint(centerOfTarget, &centerOfTarget));

			//		IFC_RETURN(m_tpPlacementTarget->get_ActualHeight(&actualHeight));
			//		centerOfTarget.Y += static_cast<float>(actualHeight / 2.0);
			//	}

			//	DOUBLE center = popupOffset + presenterSize.Height / 2;
			//	DOUBLE offsetFromCenter = centerOfTarget.Y - center;
			//	IFC_RETURN(spTransitionAsPickerFlyoutThemeTransition->put_OpenedLength(presenterSize.Height));
			//	IFC_RETURN(spTransitionAsPickerFlyoutThemeTransition->put_OffsetFromCenter(offsetFromCenter));
			//}

			//return S_OK;
		}

		internal void DisablePresenterResizing()
		{
		}
	}
}
