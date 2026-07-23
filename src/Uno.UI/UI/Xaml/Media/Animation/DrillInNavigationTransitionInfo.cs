// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference ThemeTransitions.cpp, tag winui3/release/1.7-stable

using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// Specifies the animation to run when a user drills into a logical hierarchy.
/// </summary>
public partial class DrillInNavigationTransitionInfo : NavigationTransitionInfo
{
	// Animation duration constants from WinUI (in milliseconds)
	private const long NavigatingAwayScaleDuration = 100;
	private const long NavigatingAwayOpacityDuration = 100;
	private const long NavigatingToScaleDuration = 783;
	private const long NavigatingToOpacityDuration = 333;
	private const long BackNavigatingAwayScaleDuration = 100;
	private const long BackNavigatingAwayOpacityDuration = 100;
	private const long BackNavigatingToScaleDuration = 333;
	private const long BackNavigatingToOpacityDuration = 333;

	public DrillInNavigationTransitionInfo() : base() { }

	private protected override IList<Storyboard> CreateStoryboardsCore(UIElement element, NavigationTrigger trigger)
	{
		var storyboard = new Storyboard();
		var transformOrigin = new Point(0.5, 0.5);

		// Ensure the element has a ScaleTransform for animation
		var scaleTransform = EnsureScaleTransform(element, transformOrigin);

		switch (trigger)
		{
			case NavigationTrigger.NavigatingTo:
				CreateNavigatingToAnimations(storyboard, element, scaleTransform);
				break;

			case NavigationTrigger.NavigatingAway:
				CreateNavigatingAwayAnimations(storyboard, element, scaleTransform);
				break;

			case NavigationTrigger.BackNavigatingTo:
				CreateBackNavigatingToAnimations(storyboard, element, scaleTransform);
				break;

			case NavigationTrigger.BackNavigatingAway:
				CreateBackNavigatingAwayAnimations(storyboard, element, scaleTransform);
				break;
		}

		return new List<Storyboard> { storyboard };
	}

	private static ScaleTransform EnsureScaleTransform(UIElement element, Point transformOrigin)
	{
		var scaleTransform = new ScaleTransform
		{
			ScaleX = 1.0,
			ScaleY = 1.0
		};

		element.RenderTransform = scaleTransform;
		element.RenderTransformOrigin = transformOrigin;

		return scaleTransform;
	}

	private static void CreateNavigatingToAnimations(Storyboard storyboard, UIElement element, ScaleTransform scaleTransform)
	{
		// Scale: 0.94 -> 1.0 over 783ms
		const double scaleFactor = 0.94;
		var scaleKeySpline = new KeySpline(0.1, 0.9, 0.2, 1.0);

		AddScaleAnimation(storyboard, scaleTransform, scaleFactor, 1.0, NavigatingToScaleDuration, scaleKeySpline);

		// Opacity: 0 -> 1 over 333ms
		var opacityKeySpline = new KeySpline(0.17, 0.17, 0.0, 1.0);
		AddOpacityAnimation(storyboard, element, 0.0, 1.0, NavigatingToOpacityDuration, opacityKeySpline);
	}

	private static void CreateNavigatingAwayAnimations(Storyboard storyboard, UIElement element, ScaleTransform scaleTransform)
	{
		// Scale: 1.0 -> 1.04 over 100ms
		const double scaleFactor = 1.04;
		var scaleKeySpline = new KeySpline(0.1, 0.9, 0.2, 1.0);

		AddScaleAnimation(storyboard, scaleTransform, 1.0, scaleFactor, NavigatingAwayScaleDuration, scaleKeySpline);

		// Opacity: 1 -> 0 over 100ms
		var opacityKeySpline = new KeySpline(0.17, 0.17, 0.0, 1.0);
		AddOpacityAnimation(storyboard, element, 1.0, 0.0, NavigatingAwayOpacityDuration, opacityKeySpline);
	}

	private static void CreateBackNavigatingToAnimations(Storyboard storyboard, UIElement element, ScaleTransform scaleTransform)
	{
		// Scale: 1.06 -> 1.0 over 333ms
		const double scaleFactor = 1.06;
		var scaleKeySpline = new KeySpline(0.12, 0.0, 0.0, 1.0);

		AddScaleAnimation(storyboard, scaleTransform, scaleFactor, 1.0, BackNavigatingToScaleDuration, scaleKeySpline);

		// Opacity: 0 -> 1 over 333ms
		var opacityKeySpline = new KeySpline(0.17, 0.17, 0.0, 1.0);
		AddOpacityAnimation(storyboard, element, 0.0, 1.0, BackNavigatingToOpacityDuration, opacityKeySpline);
	}

	private static void CreateBackNavigatingAwayAnimations(Storyboard storyboard, UIElement element, ScaleTransform scaleTransform)
	{
		// Scale: 1.0 -> 0.96 over 100ms
		const double scaleFactor = 0.96;
		var scaleKeySpline = new KeySpline(0.1, 0.9, 0.2, 1.0);

		AddScaleAnimation(storyboard, scaleTransform, 1.0, scaleFactor, BackNavigatingAwayScaleDuration, scaleKeySpline);

		// Opacity: 1 -> 0 over 100ms
		var opacityKeySpline = new KeySpline(0.17, 0.17, 0.0, 1.0);
		AddOpacityAnimation(storyboard, element, 1.0, 0.0, BackNavigatingAwayOpacityDuration, opacityKeySpline);
	}

	private static void AddScaleAnimation(Storyboard storyboard, ScaleTransform scaleTransform, double from, double to, long durationMs, KeySpline keySpline)
	{
		// ScaleX animation
		var scaleXAnimation = new DoubleAnimationUsingKeyFrames();
		scaleXAnimation.KeyFrames.Add(new SplineDoubleKeyFrame(from, KeyTime.FromTimeSpan(TimeSpan.Zero), keySpline));
		scaleXAnimation.KeyFrames.Add(new SplineDoubleKeyFrame(to, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMs)), keySpline));
		Storyboard.SetTarget(scaleXAnimation, scaleTransform);
		Storyboard.SetTargetProperty(scaleXAnimation, nameof(ScaleTransform.ScaleX));
		storyboard.Children.Add(scaleXAnimation);

		// ScaleY animation
		var scaleYAnimation = new DoubleAnimationUsingKeyFrames();
		scaleYAnimation.KeyFrames.Add(new SplineDoubleKeyFrame(from, KeyTime.FromTimeSpan(TimeSpan.Zero), keySpline));
		scaleYAnimation.KeyFrames.Add(new SplineDoubleKeyFrame(to, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMs)), keySpline));
		Storyboard.SetTarget(scaleYAnimation, scaleTransform);
		Storyboard.SetTargetProperty(scaleYAnimation, nameof(ScaleTransform.ScaleY));
		storyboard.Children.Add(scaleYAnimation);
	}

	private static void AddOpacityAnimation(Storyboard storyboard, UIElement element, double from, double to, long durationMs, KeySpline keySpline)
	{
		var opacityAnimation = new DoubleAnimationUsingKeyFrames();
		opacityAnimation.KeyFrames.Add(new SplineDoubleKeyFrame(from, KeyTime.FromTimeSpan(TimeSpan.Zero), keySpline));
		opacityAnimation.KeyFrames.Add(new SplineDoubleKeyFrame(to, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMs)), keySpline));
		Storyboard.SetTarget(opacityAnimation, element);
		Storyboard.SetTargetProperty(opacityAnimation, nameof(UIElement.Opacity));
		storyboard.Children.Add(opacityAnimation);
	}
}
