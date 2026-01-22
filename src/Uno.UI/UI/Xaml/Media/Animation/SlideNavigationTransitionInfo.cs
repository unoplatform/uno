// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference ThemeTransitions.cpp, tag winui3/release/1.7-stable

using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// Provides the animated transition behavior for a slide navigation between pages.
/// </summary>
public partial class SlideNavigationTransitionInfo : NavigationTransitionInfo
{
	// Animation constants from WinUI for FromLeft/FromRight
	private const double HorizontalTranslationExitOffset = 150;
	private const double HorizontalTranslationEntranceOffset = -200;
	private const long HorizontalOutDuration = 150;
	private const long HorizontalInDuration = 300;

	// Animation constants from WinUI for FromBottom
	private const double VerticalSlideOffsetIn = 200;
	private const double VerticalSlideOffsetOut = 100;
	private const long VerticalSlideStartTime = 0;
	private const long VerticalSlideMidTime = 167;
	private const long VerticalSlideEndTime = 500;
	private const double SlideEaseExponent = 6.0;

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
		var storyboard = new Storyboard();

		// Ensure the element has a TranslateTransform for animation
		var translateTransform = EnsureTranslateTransform(element);

		if (Effect == SlideNavigationTransitionEffect.FromLeft || Effect == SlideNavigationTransitionEffect.FromRight)
		{
			CreateHorizontalSlideAnimations(storyboard, element, translateTransform, trigger);
		}
		else // FromBottom
		{
			CreateVerticalSlideAnimations(storyboard, element, translateTransform, trigger);
		}

		return new List<Storyboard> { storyboard };
	}

	private static TranslateTransform EnsureTranslateTransform(UIElement element)
	{
		var translateTransform = new TranslateTransform();
		element.RenderTransform = translateTransform;
		return translateTransform;
	}

	private void CreateHorizontalSlideAnimations(Storyboard storyboard, UIElement element, TranslateTransform translateTransform, NavigationTrigger trigger)
	{
		// Direction factor: FromLeft = 1, FromRight = -1
		double directionFactor = Effect == SlideNavigationTransitionEffect.FromLeft ? 1.0 : -1.0;

		var inControlPoint1 = new Point(0.1, 0.9);
		var inControlPoint2 = new Point(0.2, 1.0);
		var outControlPoint1 = new Point(0.7, 0.0);
		var outControlPoint2 = new Point(1.0, 0.5);

		switch (trigger)
		{
			case NavigationTrigger.NavigatingAway:
				// Exit: opacity 1->0 at outDuration, translate 0->exitOffset
				AddDiscreteOpacityAnimation(storyboard, element, 1.0, 0, 0.0, HorizontalOutDuration);
				AddSplineTranslateXAnimation(storyboard, translateTransform,
					0.0, 0,
					HorizontalTranslationExitOffset * directionFactor, HorizontalOutDuration,
					outControlPoint1, outControlPoint2);
				break;

			case NavigationTrigger.NavigatingTo:
				// Enter: opacity 0->1 at outDuration, translate entranceOffset->0
				AddDiscreteOpacityAnimation(storyboard, element, 0.0, 0, 1.0, HorizontalOutDuration);
				AddSplineTranslateXAnimation(storyboard, translateTransform,
					HorizontalTranslationEntranceOffset * directionFactor, HorizontalOutDuration,
					0.0, HorizontalOutDuration + HorizontalInDuration,
					inControlPoint1, inControlPoint2);
				break;

			case NavigationTrigger.BackNavigatingAway:
				// Back exit: opacity 1->0 at outDuration, translate 0->entranceOffset
				AddDiscreteOpacityAnimation(storyboard, element, 1.0, 0, 0.0, HorizontalOutDuration);
				AddSplineTranslateXAnimation(storyboard, translateTransform,
					0.0, 0,
					HorizontalTranslationEntranceOffset * directionFactor, HorizontalOutDuration,
					outControlPoint1, outControlPoint2);
				break;

			case NavigationTrigger.BackNavigatingTo:
				// Back enter: opacity 0->1 at outDuration, translate exitOffset->0
				AddDiscreteOpacityAnimation(storyboard, element, 0.0, 0, 1.0, HorizontalOutDuration);
				AddSplineTranslateXAnimation(storyboard, translateTransform,
					HorizontalTranslationExitOffset * directionFactor, HorizontalOutDuration,
					0.0, HorizontalOutDuration + HorizontalInDuration,
					inControlPoint1, inControlPoint2);
				break;
		}
	}

	private static void CreateVerticalSlideAnimations(Storyboard storyboard, UIElement element, TranslateTransform translateTransform, NavigationTrigger trigger)
	{
		var easeIn = new ExponentialEase { Exponent = SlideEaseExponent, EasingMode = EasingMode.EaseIn };
		var easeOut = new ExponentialEase { Exponent = SlideEaseExponent, EasingMode = EasingMode.EaseOut };

		switch (trigger)
		{
			case NavigationTrigger.NavigatingTo:
				// Enter from bottom with slide up and fade in
				AddVerticalEnterAnimation(storyboard, element, translateTransform, easeOut);
				break;

			case NavigationTrigger.NavigatingAway:
				// Exit by fading out (no translate)
				AddVerticalExitAnimation(storyboard, element);
				break;

			case NavigationTrigger.BackNavigatingTo:
				// Back enter: just fade in (no translate)
				AddVerticalBackEnterAnimation(storyboard, element);
				break;

			case NavigationTrigger.BackNavigatingAway:
				// Back exit: slide down and fade out
				AddVerticalBackExitAnimation(storyboard, element, translateTransform, easeIn);
				break;
		}
	}

	private static void AddVerticalEnterAnimation(Storyboard storyboard, UIElement element, TranslateTransform translateTransform, EasingFunctionBase easing)
	{
		// TranslateY: start at SLIDE_OFFSET_IN, stay there until MID_TIME, then animate to 0
		var translateAnimation = new DoubleAnimationUsingKeyFrames();
		translateAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(VerticalSlideOffsetIn, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(VerticalSlideStartTime))));
		translateAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(VerticalSlideOffsetIn, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(VerticalSlideMidTime))));
		translateAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(VerticalSlideEndTime)), easing));
		Storyboard.SetTarget(translateAnimation, translateTransform);
		Storyboard.SetTargetProperty(translateAnimation, nameof(TranslateTransform.Y));
		storyboard.Children.Add(translateAnimation);

		// Opacity: 0 until just before MID_TIME, then 1
		var opacityAnimation = new DoubleAnimationUsingKeyFrames();
		opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(VerticalSlideStartTime))));
		opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(VerticalSlideMidTime - 10))));
		opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(VerticalSlideMidTime))));
		Storyboard.SetTarget(opacityAnimation, element);
		Storyboard.SetTargetProperty(opacityAnimation, nameof(UIElement.Opacity));
		storyboard.Children.Add(opacityAnimation);
	}

	private static void AddVerticalExitAnimation(Storyboard storyboard, UIElement element)
	{
		// Just fade out
		var opacityAnimation = new DoubleAnimationUsingKeyFrames();
		opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(VerticalSlideStartTime))));
		opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(VerticalSlideMidTime - 10))));
		opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(VerticalSlideMidTime))));
		Storyboard.SetTarget(opacityAnimation, element);
		Storyboard.SetTargetProperty(opacityAnimation, nameof(UIElement.Opacity));
		storyboard.Children.Add(opacityAnimation);
	}

	private static void AddVerticalBackEnterAnimation(Storyboard storyboard, UIElement element)
	{
		// Just fade in
		var opacityAnimation = new DoubleAnimationUsingKeyFrames();
		opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(VerticalSlideStartTime))));
		opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(VerticalSlideMidTime - 10))));
		opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(VerticalSlideMidTime))));
		Storyboard.SetTarget(opacityAnimation, element);
		Storyboard.SetTargetProperty(opacityAnimation, nameof(UIElement.Opacity));
		storyboard.Children.Add(opacityAnimation);
	}

	private static void AddVerticalBackExitAnimation(Storyboard storyboard, UIElement element, TranslateTransform translateTransform, EasingFunctionBase easing)
	{
		// TranslateY: 0 -> SLIDE_OFFSET_OUT
		var translateAnimation = new DoubleAnimationUsingKeyFrames();
		translateAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(VerticalSlideStartTime))));
		translateAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(VerticalSlideOffsetOut, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(VerticalSlideEndTime)), easing));
		Storyboard.SetTarget(translateAnimation, translateTransform);
		Storyboard.SetTargetProperty(translateAnimation, nameof(TranslateTransform.Y));
		storyboard.Children.Add(translateAnimation);

		// Opacity: fade out
		var opacityAnimation = new DoubleAnimationUsingKeyFrames();
		opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(VerticalSlideStartTime))));
		opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(VerticalSlideMidTime - 10))));
		opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(VerticalSlideMidTime))));
		Storyboard.SetTarget(opacityAnimation, element);
		Storyboard.SetTargetProperty(opacityAnimation, nameof(UIElement.Opacity));
		storyboard.Children.Add(opacityAnimation);
	}

	private static void AddDiscreteOpacityAnimation(Storyboard storyboard, UIElement element, double startValue, long startTimeMs, double endValue, long endTimeMs)
	{
		var opacityAnimation = new DoubleAnimationUsingKeyFrames();
		opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(startValue, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(startTimeMs))));
		opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(endValue, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(endTimeMs))));
		Storyboard.SetTarget(opacityAnimation, element);
		Storyboard.SetTargetProperty(opacityAnimation, nameof(UIElement.Opacity));
		storyboard.Children.Add(opacityAnimation);
	}

	private static void AddSplineTranslateXAnimation(Storyboard storyboard, TranslateTransform translateTransform,
		double startValue, long startTimeMs, double endValue, long endTimeMs, Point controlPoint1, Point controlPoint2)
	{
		var translateAnimation = new DoubleAnimationUsingKeyFrames();
		translateAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(startValue, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(startTimeMs))));
		translateAnimation.KeyFrames.Add(new SplineDoubleKeyFrame(endValue, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(endTimeMs)),
			new KeySpline(controlPoint1, controlPoint2)));
		Storyboard.SetTarget(translateAnimation, translateTransform);
		Storyboard.SetTargetProperty(translateAnimation, nameof(TranslateTransform.X));
		storyboard.Children.Add(translateAnimation);
	}
}
