// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference ThemeTransitions.cpp, tag winui3/release/1.7-stable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// Specifies the common page navigation transition behavior that applies to all
/// NavigationThemeTransition page navigations.
/// </summary>
/// <remarks>
/// Note: The WinUI turnstile animation uses PlaneProjection for 3D rotation effects.
/// Since PlaneProjection is not implemented in Uno, this uses a 2D fallback with
/// scale and opacity animations to approximate the visual effect.
/// </remarks>
public partial class CommonNavigationTransitionInfo : NavigationTransitionInfo
{
	// Animation constants from WinUI (simplified for 2D fallback)
	private const long OutDuration = 150;
	private const long InDuration = 300;
	private const long StaggerDelay = 83; // Delay between stagger elements
	private const double ExitScaleFactor = 0.9;

	private static readonly List<WeakReference<UIElement>> s_staggerElements = new();

	public CommonNavigationTransitionInfo() : base() { }

	#region IsStaggeringEnabled

	/// <summary>
	/// Gets or sets a value that determines whether staggering is enabled for the transition.
	/// </summary>
	public bool IsStaggeringEnabled
	{
		get => (bool)GetValue(IsStaggeringEnabledProperty);
		set => SetValue(IsStaggeringEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the IsStaggeringEnabled dependency property.
	/// </summary>
	public static DependencyProperty IsStaggeringEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(IsStaggeringEnabled),
			typeof(bool),
			typeof(CommonNavigationTransitionInfo),
			new FrameworkPropertyMetadata(false)
		);

	#endregion

	#region IsStaggerElement Attached Property

	/// <summary>
	/// Gets the value of the IsStaggerElement attached property for a specified element.
	/// </summary>
	public static bool GetIsStaggerElement(UIElement element)
	{
		return (bool)element.GetValue(IsStaggerElementProperty);
	}

	/// <summary>
	/// Sets the value of the IsStaggerElement attached property for a specified element.
	/// </summary>
	public static void SetIsStaggerElement(UIElement element, bool value)
	{
		element.SetValue(IsStaggerElementProperty, value);
	}

	/// <summary>
	/// Identifies the IsStaggerElement attached property.
	/// </summary>
	public static DependencyProperty IsStaggerElementProperty
	{
		[DynamicDependency(nameof(GetIsStaggerElement))]
		[DynamicDependency(nameof(SetIsStaggerElement))]
		get;
	} = DependencyProperty.RegisterAttached(
		"IsStaggerElement",
		typeof(bool),
		typeof(CommonNavigationTransitionInfo),
		new FrameworkPropertyMetadata(false, OnIsStaggerElementChanged)
	);

	private static void OnIsStaggerElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not UIElement element)
		{
			return;
		}

		var isStaggerElement = (bool)e.NewValue;

		lock (s_staggerElements)
		{
			// Remove existing entry for this element
			s_staggerElements.RemoveAll(wr => !wr.TryGetTarget(out var target) || target == element);

			// Add if the new value is true
			if (isStaggerElement)
			{
				s_staggerElements.Add(new WeakReference<UIElement>(element));
			}
		}
	}

	#endregion

	private protected override IList<Storyboard> CreateStoryboardsCore(UIElement element, NavigationTrigger trigger)
	{
		var storyboards = new List<Storyboard>();

		// Create the main page animation
		var mainStoryboard = CreateMainPageAnimation(element, trigger);
		storyboards.Add(mainStoryboard);

		// If staggering is enabled, create stagger animations for marked elements
		if (IsStaggeringEnabled)
		{
			var staggerStoryboards = CreateStaggerAnimations(element, trigger);
			storyboards.AddRange(staggerStoryboards);
		}

		return storyboards;
	}

	private static Storyboard CreateMainPageAnimation(UIElement element, NavigationTrigger trigger)
	{
		var storyboard = new Storyboard();
		var transformOrigin = new Point(0.5, 0.5);

		// Use scale transform for 2D turnstile-like effect
		var scaleTransform = new ScaleTransform { ScaleX = 1.0, ScaleY = 1.0 };
		element.RenderTransform = scaleTransform;
		element.RenderTransformOrigin = transformOrigin;

		var inControlPoint1 = new Point(0.1, 0.9);
		var inControlPoint2 = new Point(0.2, 1.0);
		var outControlPoint1 = new Point(0.7, 0.0);
		var outControlPoint2 = new Point(1.0, 0.5);

		switch (trigger)
		{
			case NavigationTrigger.NavigatingAway:
				// Exit: scale down and fade out
				AddScaleAnimation(storyboard, scaleTransform, 1.0, ExitScaleFactor, OutDuration, outControlPoint1, outControlPoint2);
				AddOpacityAnimation(storyboard, element, 1.0, 0.0, OutDuration, outControlPoint1, outControlPoint2);
				break;

			case NavigationTrigger.NavigatingTo:
				// Enter: fade in and scale up
				AddDelayedScaleAnimation(storyboard, scaleTransform, ExitScaleFactor, OutDuration, 1.0, OutDuration + InDuration, inControlPoint1, inControlPoint2);
				AddDelayedOpacityAnimation(storyboard, element, 0.0, OutDuration, 1.0, OutDuration + InDuration, inControlPoint1, inControlPoint2);
				break;

			case NavigationTrigger.BackNavigatingAway:
				// Back exit: scale down and fade out
				AddScaleAnimation(storyboard, scaleTransform, 1.0, ExitScaleFactor, OutDuration, outControlPoint1, outControlPoint2);
				AddOpacityAnimation(storyboard, element, 1.0, 0.0, OutDuration, outControlPoint1, outControlPoint2);
				break;

			case NavigationTrigger.BackNavigatingTo:
				// Back enter: fade in
				AddDelayedOpacityAnimation(storyboard, element, 0.0, OutDuration, 1.0, OutDuration + InDuration, inControlPoint1, inControlPoint2);
				break;
		}

		return storyboard;
	}

	private List<Storyboard> CreateStaggerAnimations(UIElement page, NavigationTrigger trigger)
	{
		var storyboards = new List<Storyboard>();
		var staggerElements = GetStaggerElementsForPage(page);

		for (int i = 0; i < staggerElements.Count; i++)
		{
			var staggerElement = staggerElements[i];
			var delay = i * StaggerDelay;
			var storyboard = CreateStaggerElementAnimation(staggerElement, trigger, delay);
			storyboards.Add(storyboard);
		}

		return storyboards;
	}

	private static Storyboard CreateStaggerElementAnimation(UIElement element, NavigationTrigger trigger, long delayMs)
	{
		var storyboard = new Storyboard();
		var transformOrigin = new Point(0.5, 0.5);

		var scaleTransform = new ScaleTransform { ScaleX = 1.0, ScaleY = 1.0 };
		element.RenderTransform = scaleTransform;
		element.RenderTransformOrigin = transformOrigin;

		var inControlPoint1 = new Point(0.1, 0.9);
		var inControlPoint2 = new Point(0.2, 1.0);

		// Stagger elements only animate on entrance
		if (trigger == NavigationTrigger.NavigatingTo || trigger == NavigationTrigger.BackNavigatingTo)
		{
			var startTime = OutDuration + delayMs;
			var endTime = startTime + InDuration;

			// Scale from smaller to normal
			AddDelayedScaleAnimation(storyboard, scaleTransform, ExitScaleFactor, startTime, 1.0, endTime, inControlPoint1, inControlPoint2);

			// Fade in
			AddDelayedOpacityAnimation(storyboard, element, 0.0, startTime, 1.0, endTime, inControlPoint1, inControlPoint2);
		}

		return storyboard;
	}

	private static List<UIElement> GetStaggerElementsForPage(UIElement page)
	{
		var result = new List<UIElement>();

		lock (s_staggerElements)
		{
			foreach (var weakRef in s_staggerElements)
			{
				if (weakRef.TryGetTarget(out var element))
				{
					if (IsAncestor(page, element))
					{
						result.Add(element);
					}
				}
			}
		}

		return result;
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
			current = VisualTreeHelper.GetParent(current);
		}
		return false;
	}

	private static void AddScaleAnimation(Storyboard storyboard, ScaleTransform scaleTransform, double from, double to, long durationMs, Point controlPoint1, Point controlPoint2)
	{
		var keySpline = new KeySpline(controlPoint1, controlPoint2);

		// ScaleX
		var scaleXAnimation = new DoubleAnimationUsingKeyFrames();
		scaleXAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(from, KeyTime.FromTimeSpan(TimeSpan.Zero)));
		scaleXAnimation.KeyFrames.Add(new SplineDoubleKeyFrame(to, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMs)), keySpline));
		Storyboard.SetTarget(scaleXAnimation, scaleTransform);
		Storyboard.SetTargetProperty(scaleXAnimation, nameof(ScaleTransform.ScaleX));
		storyboard.Children.Add(scaleXAnimation);

		// ScaleY
		var scaleYAnimation = new DoubleAnimationUsingKeyFrames();
		scaleYAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(from, KeyTime.FromTimeSpan(TimeSpan.Zero)));
		scaleYAnimation.KeyFrames.Add(new SplineDoubleKeyFrame(to, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMs)), keySpline));
		Storyboard.SetTarget(scaleYAnimation, scaleTransform);
		Storyboard.SetTargetProperty(scaleYAnimation, nameof(ScaleTransform.ScaleY));
		storyboard.Children.Add(scaleYAnimation);
	}

	private static void AddDelayedScaleAnimation(Storyboard storyboard, ScaleTransform scaleTransform, double holdValue, long holdTimeMs, double endValue, long endTimeMs, Point controlPoint1, Point controlPoint2)
	{
		var keySpline = new KeySpline(controlPoint1, controlPoint2);

		// ScaleX
		var scaleXAnimation = new DoubleAnimationUsingKeyFrames();
		scaleXAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(holdValue, KeyTime.FromTimeSpan(TimeSpan.Zero)));
		scaleXAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(holdValue, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(holdTimeMs))));
		scaleXAnimation.KeyFrames.Add(new SplineDoubleKeyFrame(endValue, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(endTimeMs)), keySpline));
		Storyboard.SetTarget(scaleXAnimation, scaleTransform);
		Storyboard.SetTargetProperty(scaleXAnimation, nameof(ScaleTransform.ScaleX));
		storyboard.Children.Add(scaleXAnimation);

		// ScaleY
		var scaleYAnimation = new DoubleAnimationUsingKeyFrames();
		scaleYAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(holdValue, KeyTime.FromTimeSpan(TimeSpan.Zero)));
		scaleYAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(holdValue, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(holdTimeMs))));
		scaleYAnimation.KeyFrames.Add(new SplineDoubleKeyFrame(endValue, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(endTimeMs)), keySpline));
		Storyboard.SetTarget(scaleYAnimation, scaleTransform);
		Storyboard.SetTargetProperty(scaleYAnimation, nameof(ScaleTransform.ScaleY));
		storyboard.Children.Add(scaleYAnimation);
	}

	private static void AddOpacityAnimation(Storyboard storyboard, UIElement element, double from, double to, long durationMs, Point controlPoint1, Point controlPoint2)
	{
		var keySpline = new KeySpline(controlPoint1, controlPoint2);
		var opacityAnimation = new DoubleAnimationUsingKeyFrames();
		opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(from, KeyTime.FromTimeSpan(TimeSpan.Zero)));
		opacityAnimation.KeyFrames.Add(new SplineDoubleKeyFrame(to, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMs)), keySpline));
		Storyboard.SetTarget(opacityAnimation, element);
		Storyboard.SetTargetProperty(opacityAnimation, nameof(UIElement.Opacity));
		storyboard.Children.Add(opacityAnimation);
	}

	private static void AddDelayedOpacityAnimation(Storyboard storyboard, UIElement element, double holdValue, long holdTimeMs, double endValue, long endTimeMs, Point controlPoint1, Point controlPoint2)
	{
		var keySpline = new KeySpline(controlPoint1, controlPoint2);
		var opacityAnimation = new DoubleAnimationUsingKeyFrames();
		opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(holdValue, KeyTime.FromTimeSpan(TimeSpan.Zero)));
		opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(holdValue, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(holdTimeMs))));
		opacityAnimation.KeyFrames.Add(new SplineDoubleKeyFrame(endValue, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(endTimeMs)), keySpline));
		Storyboard.SetTarget(opacityAnimation, element);
		Storyboard.SetTargetProperty(opacityAnimation, nameof(UIElement.Opacity));
		storyboard.Children.Add(opacityAnimation);
	}

	/// <summary>
	/// Clears expired weak references from the stagger elements list.
	/// </summary>
	internal static void ClearStaggerElements()
	{
		lock (s_staggerElements)
		{
			s_staggerElements.RemoveAll(wr => !wr.TryGetTarget(out _));
		}
	}
}
