using Windows.Foundation;

namespace Windows.UI.Xaml.Controls.Primitives
{
	partial class FlyoutBase
	{
#if !DEBUG
#error TODO
#endif
		internal bool UsePickerFlyoutTheme { get; set; }

		internal void PlaceFlyoutForDateTimePicker(Point point)
		{
			//wf::Size presenterSize = { };
			//ctl::ComPtr<PickerFlyoutThemeTransition> spTransitionAsPickerFlyoutThemeTransition;

			//m_isPositionedForDateTimePicker = true;

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
	}
}
