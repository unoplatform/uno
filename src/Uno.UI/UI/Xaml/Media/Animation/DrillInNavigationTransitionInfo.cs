// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference ThemeTransitions.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

using System.Collections.Generic;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// Specifies the animation to run when a user drills into a logical hierarchy.
/// </summary>
public partial class DrillInNavigationTransitionInfo : NavigationTransitionInfo
{
	private const long s_NavigatingAwayScaleDuration = 100;
	private const long s_NavigatingAwayOpacityDuration = 100;
	private const long s_NavigatingToScaleDuration = 783;
	private const long s_NavigatingToOpacityDuration = 333;
	private const long s_BackNavigatingAwayScaleDuration = 100;
	private const long s_BackNavigatingAwayOpacityDuration = 100;
	private const long s_BackNavigatingToScaleDuration = 333;
	private const long s_BackNavigatingToOpacityDuration = 333;

	public DrillInNavigationTransitionInfo() : base() { }

	/// <inheritdoc />
	protected override string GetNavigationStateCore() => "0";

	/// <inheritdoc />
	protected override void SetNavigationStateCore(string navigationState)
	{
	}

	private protected override IList<Storyboard> CreateStoryboardsCore(UIElement element, NavigationTrigger trigger)
	{
		Point transformOrigin = new(0.5, 0.5);
		var spElement = element;
		DependencyObject spElementAsDO = spElement;
		Storyboard spStoryboard = new();

		// UNO TODO: WinUI animates "(UIElement.TransitionTarget).(TransitionTarget.CompositeTransform).ScaleX/ScaleY" and
		// "(UIElement.TransitionTarget).Opacity"; Uno animates a ScaleTransform set as RenderTransform, and UIElement.Opacity.
		var propertyNameScaleX = nameof(ScaleTransform.ScaleX);
		var propertyNameScaleY = nameof(ScaleTransform.ScaleY);
		var propertyNameOpacity = nameof(UIElement.Opacity);

		ScaleTransform scaleTransform = new();
		spElement.RenderTransform = scaleTransform;

		var spTimelines = spStoryboard.Children;

		if (trigger == NavigationTrigger.NavigatingTo)
		{
			const double scaleFactor = 0.94;
			Point scaleCurveControlPoint1 = new(0.1f, 0.9f);
			Point scaleCurveControlPoint2 = new(0.2f, 1.0f);
			Point opacityCurveControlPoint1 = new(0.17f, 0.17f);
			Point opacityCurveControlPoint2 = new(0.0f, 1.0f);
			DoubleAnimationUsingKeyFrames spScaleXAnimation = new();
			DoubleAnimationUsingKeyFrames spScaleYAnimation = new();
			DoubleAnimationUsingKeyFrames spOpacityAnimation = new();

			NavigateTransitionHelper.SetTransformOrigin(spElement, transformOrigin, spStoryboard);

			Storyboard.SetTarget(spScaleXAnimation, scaleTransform);
			Storyboard.SetTargetProperty(spScaleXAnimation, propertyNameScaleX);
			NavigateTransitionHelper.RegisterSplineKeyFrame(spScaleXAnimation, scaleFactor, 0, scaleCurveControlPoint1, scaleCurveControlPoint2);
			NavigateTransitionHelper.RegisterSplineKeyFrame(spScaleXAnimation, 1.0, s_NavigatingToScaleDuration, scaleCurveControlPoint1, scaleCurveControlPoint2);

			Storyboard.SetTarget(spScaleYAnimation, scaleTransform);
			Storyboard.SetTargetProperty(spScaleYAnimation, propertyNameScaleY);
			NavigateTransitionHelper.RegisterSplineKeyFrame(spScaleYAnimation, scaleFactor, 0, scaleCurveControlPoint1, scaleCurveControlPoint2);
			NavigateTransitionHelper.RegisterSplineKeyFrame(spScaleYAnimation, 1.0, s_NavigatingToScaleDuration, scaleCurveControlPoint1, scaleCurveControlPoint2);

			Storyboard.SetTarget(spOpacityAnimation, spElementAsDO);
			Storyboard.SetTargetProperty(spOpacityAnimation, propertyNameOpacity);
			NavigateTransitionHelper.RegisterSplineKeyFrame(spOpacityAnimation, 0.0, 0, opacityCurveControlPoint1, opacityCurveControlPoint2);
			NavigateTransitionHelper.RegisterSplineKeyFrame(spOpacityAnimation, 1.0, s_NavigatingToOpacityDuration, opacityCurveControlPoint1, opacityCurveControlPoint2);

			spTimelines.Add(spScaleXAnimation);
			spTimelines.Add(spScaleYAnimation);
			spTimelines.Add(spOpacityAnimation);
		}
		else if (trigger == NavigationTrigger.NavigatingAway)
		{
			const double scaleFactor = 1.04;
			Point scaleCurveControlPoint1 = new(0.1f, 0.9f);
			Point scaleCurveControlPoint2 = new(0.2f, 1.0f);
			Point opacityCurveControlPoint1 = new(0.17f, 0.17f);
			Point opacityCurveControlPoint2 = new(0.0f, 1.0f);
			DoubleAnimationUsingKeyFrames spScaleXAnimation = new();
			DoubleAnimationUsingKeyFrames spScaleYAnimation = new();
			DoubleAnimationUsingKeyFrames spOpacityAnimation = new();

			NavigateTransitionHelper.SetTransformOrigin(spElement, transformOrigin, spStoryboard);

			Storyboard.SetTarget(spScaleXAnimation, scaleTransform);
			Storyboard.SetTargetProperty(spScaleXAnimation, propertyNameScaleX);
			NavigateTransitionHelper.RegisterSplineKeyFrame(spScaleXAnimation, 1.0, 0, scaleCurveControlPoint1, scaleCurveControlPoint2);
			NavigateTransitionHelper.RegisterSplineKeyFrame(spScaleXAnimation, scaleFactor, s_NavigatingAwayScaleDuration, scaleCurveControlPoint1, scaleCurveControlPoint2);

			Storyboard.SetTarget(spScaleYAnimation, scaleTransform);
			Storyboard.SetTargetProperty(spScaleYAnimation, propertyNameScaleY);
			NavigateTransitionHelper.RegisterSplineKeyFrame(spScaleYAnimation, 1.0, 0, scaleCurveControlPoint1, scaleCurveControlPoint2);
			NavigateTransitionHelper.RegisterSplineKeyFrame(spScaleYAnimation, scaleFactor, s_NavigatingAwayScaleDuration, scaleCurveControlPoint1, scaleCurveControlPoint2);

			Storyboard.SetTarget(spOpacityAnimation, spElementAsDO);
			Storyboard.SetTargetProperty(spOpacityAnimation, propertyNameOpacity);
			NavigateTransitionHelper.RegisterSplineKeyFrame(spOpacityAnimation, 1.0, 0, opacityCurveControlPoint1, opacityCurveControlPoint2);
			NavigateTransitionHelper.RegisterSplineKeyFrame(spOpacityAnimation, 0.0, s_NavigatingAwayOpacityDuration, opacityCurveControlPoint1, opacityCurveControlPoint2);

			spTimelines.Add(spScaleXAnimation);
			spTimelines.Add(spScaleYAnimation);
			spTimelines.Add(spOpacityAnimation);
		}
		else if (trigger == NavigationTrigger.BackNavigatingTo)
		{
			const double scaleFactor = 1.06;
			Point scaleCurveControlPoint1 = new(0.12f, 0.0f);
			Point scaleCurveControlPoint2 = new(0.0f, 1.0f);
			Point opacityCurveControlPoint1 = new(0.17f, 0.17f);
			Point opacityCurveControlPoint2 = new(0.0f, 1.0f);
			DoubleAnimationUsingKeyFrames spScaleXAnimation = new();
			DoubleAnimationUsingKeyFrames spScaleYAnimation = new();
			DoubleAnimationUsingKeyFrames spOpacityAnimation = new();

			NavigateTransitionHelper.SetTransformOrigin(spElement, transformOrigin, spStoryboard);

			Storyboard.SetTarget(spScaleXAnimation, scaleTransform);
			Storyboard.SetTargetProperty(spScaleXAnimation, propertyNameScaleX);
			NavigateTransitionHelper.RegisterSplineKeyFrame(spScaleXAnimation, scaleFactor, 0, scaleCurveControlPoint1, scaleCurveControlPoint2);
			NavigateTransitionHelper.RegisterSplineKeyFrame(spScaleXAnimation, 1.0, s_BackNavigatingToScaleDuration, scaleCurveControlPoint1, scaleCurveControlPoint2);

			Storyboard.SetTarget(spScaleYAnimation, scaleTransform);
			Storyboard.SetTargetProperty(spScaleYAnimation, propertyNameScaleY);
			NavigateTransitionHelper.RegisterSplineKeyFrame(spScaleYAnimation, scaleFactor, 0, scaleCurveControlPoint1, scaleCurveControlPoint2);
			NavigateTransitionHelper.RegisterSplineKeyFrame(spScaleYAnimation, 1.0, s_BackNavigatingToScaleDuration, scaleCurveControlPoint1, scaleCurveControlPoint2);

			Storyboard.SetTarget(spOpacityAnimation, spElementAsDO);
			Storyboard.SetTargetProperty(spOpacityAnimation, propertyNameOpacity);
			NavigateTransitionHelper.RegisterSplineKeyFrame(spOpacityAnimation, 0.0, 0, opacityCurveControlPoint1, opacityCurveControlPoint2);
			NavigateTransitionHelper.RegisterSplineKeyFrame(spOpacityAnimation, 1.0, s_BackNavigatingToOpacityDuration, opacityCurveControlPoint1, opacityCurveControlPoint2);

			spTimelines.Add(spScaleXAnimation);
			spTimelines.Add(spScaleYAnimation);
			spTimelines.Add(spOpacityAnimation);
		}
		else if (trigger == NavigationTrigger.BackNavigatingAway)
		{
			const double scaleFactor = 0.96;
			Point scaleCurveControlPoint1 = new(0.1f, 0.9f);
			Point scaleCurveControlPoint2 = new(0.2f, 1.0f);
			Point opacityCurveControlPoint1 = new(0.17f, 0.17f);
			Point opacityCurveControlPoint2 = new(0.0f, 1.0f);
			DoubleAnimationUsingKeyFrames spScaleXAnimation = new();
			DoubleAnimationUsingKeyFrames spScaleYAnimation = new();
			DoubleAnimationUsingKeyFrames spOpacityAnimation = new();

			NavigateTransitionHelper.SetTransformOrigin(spElement, transformOrigin, spStoryboard);

			Storyboard.SetTarget(spScaleXAnimation, scaleTransform);
			Storyboard.SetTargetProperty(spScaleXAnimation, propertyNameScaleX);
			NavigateTransitionHelper.RegisterSplineKeyFrame(spScaleXAnimation, 1.0, 0, scaleCurveControlPoint1, scaleCurveControlPoint2);
			NavigateTransitionHelper.RegisterSplineKeyFrame(spScaleXAnimation, scaleFactor, s_BackNavigatingAwayScaleDuration, scaleCurveControlPoint1, scaleCurveControlPoint2);

			Storyboard.SetTarget(spScaleYAnimation, scaleTransform);
			Storyboard.SetTargetProperty(spScaleYAnimation, propertyNameScaleY);
			NavigateTransitionHelper.RegisterSplineKeyFrame(spScaleYAnimation, 1.0, 0, scaleCurveControlPoint1, scaleCurveControlPoint2);
			NavigateTransitionHelper.RegisterSplineKeyFrame(spScaleYAnimation, scaleFactor, s_BackNavigatingAwayScaleDuration, scaleCurveControlPoint1, scaleCurveControlPoint2);

			Storyboard.SetTarget(spOpacityAnimation, spElementAsDO);
			Storyboard.SetTargetProperty(spOpacityAnimation, propertyNameOpacity);
			NavigateTransitionHelper.RegisterSplineKeyFrame(spOpacityAnimation, 1.0, 0, opacityCurveControlPoint1, opacityCurveControlPoint2);
			NavigateTransitionHelper.RegisterSplineKeyFrame(spOpacityAnimation, 0.0, s_BackNavigatingAwayOpacityDuration, opacityCurveControlPoint1, opacityCurveControlPoint2);

			spTimelines.Add(spScaleXAnimation);
			spTimelines.Add(spScaleYAnimation);
			spTimelines.Add(spOpacityAnimation);
		}

		return new List<Storyboard> { spStoryboard };
	}
}
