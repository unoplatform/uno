// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference ThemeTransitions.cpp, tag winui3/release/1.7-stable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// Specifies the animation to run when content appears on a Page.
/// </summary>
public partial class EntranceNavigationTransitionInfo : NavigationTransitionInfo
{
	// Animation constants from WinUI
	private const double TranslationOffset = 140;
	private const long OutDuration = 150;
	private const long InDuration = 300;

	private static readonly List<WeakReference<UIElement>> s_targetElements = new();

	public EntranceNavigationTransitionInfo() : base() { }

	#region IsTargetElement Attached Property

	/// <summary>
	/// Gets the value of the IsTargetElement attached property for a specified element.
	/// </summary>
	public static bool GetIsTargetElement(UIElement element)
	{
		return (bool)element.GetValue(IsTargetElementProperty);
	}

	/// <summary>
	/// Sets the value of the IsTargetElement attached property for a specified element.
	/// </summary>
	public static void SetIsTargetElement(UIElement element, bool value)
	{
		element.SetValue(IsTargetElementProperty, value);
	}

	/// <summary>
	/// Identifies the IsTargetElement attached property.
	/// </summary>
	public static DependencyProperty IsTargetElementProperty
	{
		[DynamicDependency(nameof(GetIsTargetElement))]
		[DynamicDependency(nameof(SetIsTargetElement))]
		get;
	} = DependencyProperty.RegisterAttached(
		"IsTargetElement",
		typeof(bool),
		typeof(EntranceNavigationTransitionInfo),
		new FrameworkPropertyMetadata(false, OnIsTargetElementChanged)
	);

	private static void OnIsTargetElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not UIElement element)
		{
			return;
		}

		var isTargetElement = (bool)e.NewValue;

		// Remove existing entry for this element
		lock (s_targetElements)
		{
			s_targetElements.RemoveAll(wr => !wr.TryGetTarget(out var target) || target == element);

			// Add if the new value is true
			if (isTargetElement)
			{
				s_targetElements.Add(new WeakReference<UIElement>(element));
			}
		}
	}

	#endregion

	private protected override IList<Storyboard> CreateStoryboardsCore(UIElement element, NavigationTrigger trigger)
	{
		var storyboard = new Storyboard();

		// Find the target element (either a marked element or the page itself)
		var targetElement = GetLogicalTargetElement(element) ?? element;

		// Ensure the element has a TranslateTransform for animation
		var translateTransform = EnsureTranslateTransform(targetElement);

		var inControlPoint1 = new Point(0.1, 0.9);
		var inControlPoint2 = new Point(0.2, 1.0);
		var outControlPoint1 = new Point(0.7, 0.0);
		var outControlPoint2 = new Point(1.0, 0.5);

		switch (trigger)
		{
			case NavigationTrigger.NavigatingAway:
				// Fade out with spline
				AddSplineOpacityAnimation(storyboard, targetElement, 1.0, 0, 0.0, OutDuration, outControlPoint1, outControlPoint2);
				break;

			case NavigationTrigger.NavigatingTo:
				// Discrete opacity: 0 -> 1 at outDuration
				AddDiscreteOpacityAnimation(storyboard, targetElement, 0.0, 0, 1.0, OutDuration);

				// TranslateY: start at offset, stay there until outDuration, then animate to 0
				AddEntranceTranslateYAnimation(storyboard, translateTransform, TranslationOffset, OutDuration, 0.0, OutDuration + InDuration, inControlPoint1, inControlPoint2);
				break;

			case NavigationTrigger.BackNavigatingAway:
				// Discrete opacity: 1 -> 0 at outDuration
				AddDiscreteOpacityAnimation(storyboard, targetElement, 1.0, 0, 0.0, OutDuration);

				// TranslateY: 0 -> offset with spline
				AddSplineTranslateYAnimation(storyboard, translateTransform, 0.0, 0, TranslationOffset, OutDuration, outControlPoint1, outControlPoint2);
				break;

			case NavigationTrigger.BackNavigatingTo:
				// Opacity: 0 -> 0 at outDuration, then spline to 1
				AddDelayedSplineOpacityAnimation(storyboard, targetElement, 0.0, OutDuration, 1.0, OutDuration + InDuration, inControlPoint1, inControlPoint2);
				break;
		}

		return new List<Storyboard> { storyboard };
	}

	private static UIElement GetLogicalTargetElement(UIElement page)
	{
		lock (s_targetElements)
		{
			foreach (var weakRef in s_targetElements)
			{
				if (weakRef.TryGetTarget(out var targetElement))
				{
					// Check if target element is a descendant of the page
					if (IsAncestor(page, targetElement))
					{
						return targetElement;
					}
				}
			}
		}

		return null;
	}

	private static bool IsAncestor(UIElement potentialAncestor, UIElement element)
	{
		var current = element as DependencyObject;
		while (current != null)
		{
			if (current == potentialAncestor)
			{
				return true;
			}
			current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
		}
		return false;
	}

	private static TranslateTransform EnsureTranslateTransform(UIElement element)
	{
		var translateTransform = new TranslateTransform();
		element.RenderTransform = translateTransform;
		return translateTransform;
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

	private static void AddSplineOpacityAnimation(Storyboard storyboard, UIElement element, double startValue, long startTimeMs, double endValue, long endTimeMs, Point controlPoint1, Point controlPoint2)
	{
		var opacityAnimation = new DoubleAnimationUsingKeyFrames();
		opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(startValue, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(startTimeMs))));
		opacityAnimation.KeyFrames.Add(new SplineDoubleKeyFrame(endValue, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(endTimeMs)), new KeySpline(controlPoint1, controlPoint2)));
		Storyboard.SetTarget(opacityAnimation, element);
		Storyboard.SetTargetProperty(opacityAnimation, nameof(UIElement.Opacity));
		storyboard.Children.Add(opacityAnimation);
	}

	private static void AddDelayedSplineOpacityAnimation(Storyboard storyboard, UIElement element, double holdValue, long holdTimeMs, double endValue, long endTimeMs, Point controlPoint1, Point controlPoint2)
	{
		var opacityAnimation = new DoubleAnimationUsingKeyFrames();
		opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(holdValue, KeyTime.FromTimeSpan(TimeSpan.Zero)));
		opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(holdValue, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(holdTimeMs))));
		opacityAnimation.KeyFrames.Add(new SplineDoubleKeyFrame(endValue, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(endTimeMs)), new KeySpline(controlPoint1, controlPoint2)));
		Storyboard.SetTarget(opacityAnimation, element);
		Storyboard.SetTargetProperty(opacityAnimation, nameof(UIElement.Opacity));
		storyboard.Children.Add(opacityAnimation);
	}

	private static void AddEntranceTranslateYAnimation(Storyboard storyboard, TranslateTransform translateTransform, double holdValue, long holdTimeMs, double endValue, long endTimeMs, Point controlPoint1, Point controlPoint2)
	{
		var translateAnimation = new DoubleAnimationUsingKeyFrames();
		translateAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(holdValue, KeyTime.FromTimeSpan(TimeSpan.Zero)));
		translateAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(holdValue, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(holdTimeMs))));
		translateAnimation.KeyFrames.Add(new SplineDoubleKeyFrame(endValue, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(endTimeMs)), new KeySpline(controlPoint1, controlPoint2)));
		Storyboard.SetTarget(translateAnimation, translateTransform);
		Storyboard.SetTargetProperty(translateAnimation, nameof(TranslateTransform.Y));
		storyboard.Children.Add(translateAnimation);
	}

	private static void AddSplineTranslateYAnimation(Storyboard storyboard, TranslateTransform translateTransform, double startValue, long startTimeMs, double endValue, long endTimeMs, Point controlPoint1, Point controlPoint2)
	{
		var translateAnimation = new DoubleAnimationUsingKeyFrames();
		translateAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(startValue, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(startTimeMs))));
		translateAnimation.KeyFrames.Add(new SplineDoubleKeyFrame(endValue, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(endTimeMs)), new KeySpline(controlPoint1, controlPoint2)));
		Storyboard.SetTarget(translateAnimation, translateTransform);
		Storyboard.SetTargetProperty(translateAnimation, nameof(TranslateTransform.Y));
		storyboard.Children.Add(translateAnimation);
	}

	/// <summary>
	/// Clears expired weak references from the target elements list.
	/// Called internally during navigation.
	/// </summary>
	internal static void ClearTargetElements()
	{
		lock (s_targetElements)
		{
			s_targetElements.RemoveAll(wr => !wr.TryGetTarget(out _));
		}
	}
}
