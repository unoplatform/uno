// MUX Reference LayoutTransition_partial.cpp

using Windows.Foundation;
using Windows.UI.Xaml;

namespace DirectUI;

public class PickerFlyoutThemeTransition
{
	private bool ParticipatesInTransition(
		UIElement element,
		TransitionTrigger transitionTrigger)
	{
		return
			transitionTrigger == TransitionTrigger.Load ||
			transitionTrigger == TransitionTrigger.Unload;
	}

	private TransitionParent CreateStoryboardImpl(
		UIElement element,
		Rect start,
		Rect destination,
		TransitionTrigger transitionTrigger,
		_In_ Vector<xaml_animation.Storyboard*>* storyboards,
		_Out_ xaml.TransitionParent* parentForTransition)
	{

		const wf::Point nullPoint = { 0, 0 };
		const wf::Point splitOrigin = { 0, 0.5 };
		DOUBLE openedLength = 0.0;
		DOUBLE offsetFromCenter = 0.0;
		TimingFunctionDescription easing = TimingFunctionDescription();
		easing.cp3.X = 0.0f; // Cubic-bezier (0, 0, 0, 1). Default TimingFunctionDescription() constructor creates a Linear curve (0,0,0,0,1,1,1,1).

		ctl::ComPtr<Storyboard> spStoryboard;
		ctl::ComPtr<wfc::IVector<xaml_animation::Timeline*>> spTimelines;

		IFC_RETURN(ctl::make(&spStoryboard));
		IFC_RETURN(CoreImports::Storyboard_SetTarget(static_cast<CTimeline*>(spStoryboard.Get()->GetHandle()), static_cast<UIElement*>(element)->GetHandle()));
		IFC_RETURN(spStoryboard->get_Children(&spTimelines));

		IFC_RETURN(get_OpenedLength(&openedLength));
		IFC_RETURN(get_OffsetFromCenter(&offsetFromCenter));

		if (openedLength == 0)
		{
			return S_OK;
		}

		double initialClipScaleY = 0.0;
		double finalClipScaleY = 0.0;

		if (transitionTrigger == xaml.TransitionTrigger_Load)
		{
			const double closedRatio = 0.50;
			double clipLength = openedLength * closedRatio;
			double maxOffset = openedLength * (1 - closedRatio) / 2.0;   // Max offset possible before the clip is partially off the element.

			if (DoubleUtil.Abs(offsetFromCenter) > maxOffset)
			{
				double pixelsOff = (clipLength / 2.0) - (openedLength / 2.0 - DoubleUtil.Abs(offsetFromCenter));
				initialClipScaleY = pixelsOff / openedLength * 2.0 + closedRatio;
			}
			else
			{
				initialClipScaleY = closedRatio;
			}

			finalClipScaleY = (0.5 + DoubleUtil.Abs(offsetFromCenter / openedLength)) * 2;

			ThemeGeneratorHelper themeSupplier(nullPoint, nullPoint, nullptr, nullptr, FALSE, spTimelines.Get());
			IFC_RETURN(themeSupplier.Initialize());

			IFC_RETURN(themeSupplier.SetClipOriginValues(splitOrigin));   // to get the same speed going up and down, we always use 0.5 for this animation

			IFC_RETURN(themeSupplier.RegisterKeyFrame(themeSupplier.GetClipScaleYPropertyName(), initialClipScaleY, 0, 0, &easing));
			IFC_RETURN(themeSupplier.RegisterKeyFrame(themeSupplier.GetClipScaleYPropertyName(), finalClipScaleY, 0, PickerFlyoutThemeTransition.s_OpenDuration, &easing));
			IFC_RETURN(themeSupplier.RegisterKeyFrame(themeSupplier.GetClipTranslateYPropertyName(), offsetFromCenter, 0, 0, &easing));  // immediately go there

			IFC_RETURN(themeSupplier.RegisterKeyFrame(themeSupplier.GetOpacityPropertyName(), 1.0, 0, 0, &easing));    // be fully opaque
		}
		else if (transitionTrigger == xaml::TransitionTrigger_Unload)
		{
			const DOUBLE closedRatio = 0.15;
			DOUBLE clipLength = openedLength * closedRatio;
			DOUBLE maxOffset = openedLength * (1 - closedRatio) / 2.0;   // Max offset possible before the clip is partially off the element.
			TimingFunctionDescription linear = TimingFunctionDescription();

			initialClipScaleY = (0.5 + DoubleUtil::Abs(offsetFromCenter / openedLength)) * 2;

			if (DoubleUtil::Abs(offsetFromCenter) > maxOffset)
			{
				DOUBLE pixelsOff = (clipLength / 2.0) - (openedLength / 2.0 - DoubleUtil::Abs(offsetFromCenter));
				finalClipScaleY = pixelsOff / openedLength * 2.0 + closedRatio;
			}
			else
			{
				finalClipScaleY = closedRatio;
			}

			ThemeGeneratorHelper themeSupplier(nullPoint, nullPoint, nullptr, nullptr, FALSE, spTimelines.Get());
			IFC_RETURN(themeSupplier.Initialize());

			IFC_RETURN(themeSupplier.SetClipOriginValues(splitOrigin));

			IFC_RETURN(themeSupplier.RegisterKeyFrame(themeSupplier.GetClipScaleYPropertyName(), initialClipScaleY, 0, 0, &easing));
			IFC_RETURN(themeSupplier.RegisterKeyFrame(themeSupplier.GetClipScaleYPropertyName(), finalClipScaleY, 0, PickerFlyoutThemeTransition::s_CloseDuration, &easing));
			IFC_RETURN(themeSupplier.RegisterKeyFrame(themeSupplier.GetClipTranslateYPropertyName(), offsetFromCenter, 0, 0, &easing));  // immediately go there

			IFC_RETURN(themeSupplier.RegisterKeyFrame(themeSupplier.GetOpacityPropertyName(), 1.0, 0, 0, &linear));
			IFC_RETURN(themeSupplier.RegisterKeyFrame(themeSupplier.GetOpacityPropertyName(), 1.0, 0, PickerFlyoutThemeTransition::s_OpacityChangeBeginTime, &linear));
			IFC_RETURN(themeSupplier.RegisterKeyFrame(themeSupplier.GetOpacityPropertyName(), 0.0, PickerFlyoutThemeTransition::s_OpacityChangeBeginTime, PickerFlyoutThemeTransition::s_OpacityChangeDuration, &linear));
		}

		IFC_RETURN(storyboards->Append(spStoryboard.Get()));
	}
}
