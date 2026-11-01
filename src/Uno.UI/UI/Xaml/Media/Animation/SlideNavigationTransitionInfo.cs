// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference ThemeTransitions.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

using System.Collections.Generic;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// Provides the animated transition behavior for a slide navigation between pages.
/// </summary>
public partial class SlideNavigationTransitionInfo : NavigationTransitionInfo
{
	#region Effect DependencyProperty

	/// <summary>
	/// Gets or sets a value that specifies the direction of the slide transition.
	/// </summary>
	public SlideNavigationTransitionEffect Effect
	{
		get => (SlideNavigationTransitionEffect)GetValue(EffectProperty);
		set => SetValue(EffectProperty, value);
	}

	/// <summary>
	/// Identifies the Effect dependency property.
	/// </summary>
	public static DependencyProperty EffectProperty { get; } =
		DependencyProperty.Register(
			nameof(Effect),
			typeof(SlideNavigationTransitionEffect),
			typeof(SlideNavigationTransitionInfo),
			new FrameworkPropertyMetadata(SlideNavigationTransitionEffect.FromBottom));

	#endregion

	public SlideNavigationTransitionInfo() : base() { }

	private protected override IList<Storyboard> CreateStoryboardsCore(UIElement element, NavigationTrigger trigger)
	{
		var spElement = element;
		Storyboard spStoryboard = new();
		var spTimelines = spStoryboard.Children;
		EasingFunctionBase spEasing;

		DependencyObject spAnimationTargetAsDO = spElement;

		// Our translate may be in the x or y directions so let the right code path initialize it.
		string propertyNameTranslate;

		// Create storyboard.
		DoubleAnimationUsingKeyFrames spTranslateAnimation = null;
		DoubleAnimationUsingKeyFrames spOpacityAnimation = null;
		var effect = Effect;
		if (effect == SlideNavigationTransitionEffect.FromLeft || effect == SlideNavigationTransitionEffect.FromRight)
		{
			const double translationExitOffset = 150;
			const double translationEntranceOffset = -200;
			Point inControlPoint1 = new(0.1f, 0.9f);
			Point inControlPoint2 = new(0.2f, 1.0f);
			Point outControlPoint1 = new(0.7f, 0.0f);
			Point outControlPoint2 = new(1.0f, .5f);
			const long outDuration = 150;
			const long inDuration = 300;
			double reverseTranslationFactor = effect == SlideNavigationTransitionEffect.FromLeft ? 1 : -1;

			propertyNameTranslate = nameof(TranslateTransform.X);
			switch (trigger)
			{
				case NavigationTrigger.NavigatingAway:
					spOpacityAnimation = new();
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, 1.0, 0);
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, 0.0, outDuration);

					spTranslateAnimation = new();
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spTranslateAnimation, 0.0, 0);
					NavigateTransitionHelper.RegisterSplineKeyFrame(spTranslateAnimation, translationExitOffset * reverseTranslationFactor, outDuration, outControlPoint1, outControlPoint2);
					break;
				case NavigationTrigger.NavigatingTo:
					spOpacityAnimation = new();
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, 0.0, 0);
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, 1.0, outDuration);

					spTranslateAnimation = new();
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spTranslateAnimation, translationEntranceOffset * reverseTranslationFactor, outDuration);
					NavigateTransitionHelper.RegisterSplineKeyFrame(spTranslateAnimation, 0.0, outDuration + inDuration, inControlPoint1, inControlPoint2);
					break;
				case NavigationTrigger.BackNavigatingAway:
					spOpacityAnimation = new();
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, 1.0, 0);
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, 0.0, outDuration);  // Hide it at end of translate animation

					spTranslateAnimation = new();
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spTranslateAnimation, 0.0, 0);
					NavigateTransitionHelper.RegisterSplineKeyFrame(spTranslateAnimation, translationEntranceOffset * reverseTranslationFactor, outDuration, outControlPoint1, outControlPoint2);
					break;
				case NavigationTrigger.BackNavigatingTo:
					spOpacityAnimation = new();
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, 0.0, 0);
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, 1.0, outDuration);

					spTranslateAnimation = new();
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spTranslateAnimation, translationExitOffset * reverseTranslationFactor, outDuration);
					NavigateTransitionHelper.RegisterSplineKeyFrame(spTranslateAnimation, 0.0, outDuration + inDuration, inControlPoint1, inControlPoint2);
					break;
			}
		}
		else
		{
			propertyNameTranslate = nameof(TranslateTransform.Y);
			switch (trigger)
			{
				case NavigationTrigger.NavigatingTo:
					spEasing = NavigateTransitionHelper.GetExponentialEase(NavigateTransitionHelper.SLIDE_EASE, EasingMode.EaseOut);

					spTranslateAnimation = new();
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spTranslateAnimation, NavigateTransitionHelper.SLIDE_OFFSET_IN, NavigateTransitionHelper.SLIDE_START_TIME);
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spTranslateAnimation, NavigateTransitionHelper.SLIDE_OFFSET_IN, NavigateTransitionHelper.SLIDE_MID_TIME);
					NavigateTransitionHelper.RegisterEasingKeyFrame(spTranslateAnimation, NavigateTransitionHelper.NO_OFFSET, NavigateTransitionHelper.SLIDE_END_TIME, spEasing);

					spOpacityAnimation = new();
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, NavigateTransitionHelper.NO_OPACITY, NavigateTransitionHelper.SLIDE_START_TIME);
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, NavigateTransitionHelper.NO_OPACITY, NavigateTransitionHelper.SLIDE_MID_TIME - 10);
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, NavigateTransitionHelper.FULL_OPACITY, NavigateTransitionHelper.SLIDE_MID_TIME);
					break;
				case NavigationTrigger.NavigatingAway:
					spOpacityAnimation = new();
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, NavigateTransitionHelper.FULL_OPACITY, NavigateTransitionHelper.SLIDE_START_TIME);
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, NavigateTransitionHelper.FULL_OPACITY, NavigateTransitionHelper.SLIDE_MID_TIME - 10);
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, NavigateTransitionHelper.NO_OPACITY, NavigateTransitionHelper.SLIDE_MID_TIME);
					break;
				case NavigationTrigger.BackNavigatingTo:
					spOpacityAnimation = new();
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, NavigateTransitionHelper.NO_OPACITY, NavigateTransitionHelper.SLIDE_START_TIME);
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, NavigateTransitionHelper.NO_OPACITY, NavigateTransitionHelper.SLIDE_MID_TIME - 10);
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, NavigateTransitionHelper.FULL_OPACITY, NavigateTransitionHelper.SLIDE_MID_TIME);
					break;
				case NavigationTrigger.BackNavigatingAway:
					spEasing = NavigateTransitionHelper.GetExponentialEase(NavigateTransitionHelper.SLIDE_EASE, EasingMode.EaseIn);

					spTranslateAnimation = new();
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spTranslateAnimation, NavigateTransitionHelper.NO_OFFSET, NavigateTransitionHelper.SLIDE_START_TIME);
					NavigateTransitionHelper.RegisterEasingKeyFrame(spTranslateAnimation, NavigateTransitionHelper.SLIDE_OFFSET_OUT, NavigateTransitionHelper.SLIDE_END_TIME, spEasing);

					spOpacityAnimation = new();
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, NavigateTransitionHelper.FULL_OPACITY, NavigateTransitionHelper.SLIDE_START_TIME);
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, NavigateTransitionHelper.FULL_OPACITY, NavigateTransitionHelper.SLIDE_MID_TIME - 10);
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, NavigateTransitionHelper.NO_OPACITY, NavigateTransitionHelper.SLIDE_MID_TIME);
					break;
			}
		}

		if (spTranslateAnimation != null)
		{
			// Target property was initialized to the appropriate value depending on which codepath we took.
			// UNO TODO: WinUI animates "(UIElement.TransitionTarget).(TransitionTarget.CompositeTransform)", which Uno lacks,
			// so the key frames target a TranslateTransform installed as the element's RenderTransform instead.
			TranslateTransform translateTransform = new();
			spElement.RenderTransform = translateTransform;
			Storyboard.SetTarget(spTranslateAnimation, translateTransform);
			Storyboard.SetTargetProperty(spTranslateAnimation, propertyNameTranslate);
			spTimelines.Add(spTranslateAnimation);
		}

		if (spOpacityAnimation != null)
		{
			// UNO TODO: WinUI animates "(UIElement.TransitionTarget).Opacity"; Uno animates UIElement.Opacity.
			var propertyNameOpacity = nameof(UIElement.Opacity);
			Storyboard.SetTarget(spOpacityAnimation, spAnimationTargetAsDO);
			Storyboard.SetTargetProperty(spOpacityAnimation, propertyNameOpacity);
			spTimelines.Add(spOpacityAnimation);
		}

		// Ensure sure that the animated pages are not hit test visible while animating.
		NavigateTransitionHelper.RemoveHitTestVisbility(spTimelines, spAnimationTargetAsDO);

		return new List<Storyboard> { spStoryboard };
	}
}
