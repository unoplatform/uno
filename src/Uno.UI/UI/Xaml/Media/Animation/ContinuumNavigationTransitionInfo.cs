// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference ThemeTransitions.cpp, tag winui3/release/1.7-stable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// Specifies the continuum animation behavior for page navigation transitions.
/// </summary>
/// <remarks>
/// Note: The WinUI continuum animation provides connected animation effects where an element
/// appears to move from the source page to the destination page. This implementation provides
/// the API surface but uses simplified scale + opacity animations as a fallback since full
/// connected animation support requires additional infrastructure.
/// </remarks>
public partial class ContinuumNavigationTransitionInfo : NavigationTransitionInfo
{
	// Animation constants
	private const long OutDuration = 150;
	private const long InDuration = 300;
	private const double ExitScaleFactor = 0.9;
	private const double EntranceScaleFactor = 1.1;

	private static readonly List<WeakReference<UIElement>> s_entranceElements = new();
	private static readonly List<WeakReference<UIElement>> s_exitElements = new();

	public ContinuumNavigationTransitionInfo() : base() { }

	#region ExitElement Property

	/// <summary>
	/// Gets or sets the element that will be animated during a page exit transition.
	/// </summary>
	public UIElement ExitElement
	{
		get => (UIElement)GetValue(ExitElementProperty);
		set => SetValue(ExitElementProperty, value);
	}

	/// <summary>
	/// Identifies the ExitElement dependency property.
	/// </summary>
	public static DependencyProperty ExitElementProperty { get; } =
		DependencyProperty.Register(
			nameof(ExitElement),
			typeof(UIElement),
			typeof(ContinuumNavigationTransitionInfo),
			new FrameworkPropertyMetadata(null));

	#endregion

	#region IsEntranceElement Attached Property

	/// <summary>
	/// Gets the value of the IsEntranceElement attached property for a specified element.
	/// </summary>
	public static bool GetIsEntranceElement(UIElement element)
	{
		return (bool)element.GetValue(IsEntranceElementProperty);
	}

	/// <summary>
	/// Sets the value of the IsEntranceElement attached property for a specified element.
	/// </summary>
	public static void SetIsEntranceElement(UIElement element, bool value)
	{
		element.SetValue(IsEntranceElementProperty, value);
	}

	/// <summary>
	/// Identifies the IsEntranceElement attached property.
	/// </summary>
	public static DependencyProperty IsEntranceElementProperty
	{
		[DynamicDependency(nameof(GetIsEntranceElement))]
		[DynamicDependency(nameof(SetIsEntranceElement))]
		get;
	} = DependencyProperty.RegisterAttached(
		"IsEntranceElement",
		typeof(bool),
		typeof(ContinuumNavigationTransitionInfo),
		new FrameworkPropertyMetadata(false, OnIsEntranceElementChanged));

	private static void OnIsEntranceElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not UIElement element)
		{
			return;
		}

		var isEntranceElement = (bool)e.NewValue;

		lock (s_entranceElements)
		{
			s_entranceElements.RemoveAll(wr => !wr.TryGetTarget(out var target) || target == element);

			if (isEntranceElement)
			{
				s_entranceElements.Add(new WeakReference<UIElement>(element));
			}
		}
	}

	#endregion

	#region IsExitElement Attached Property

	/// <summary>
	/// Gets the value of the IsExitElement attached property for a specified element.
	/// </summary>
	public static bool GetIsExitElement(UIElement element)
	{
		return (bool)element.GetValue(IsExitElementProperty);
	}

	/// <summary>
	/// Sets the value of the IsExitElement attached property for a specified element.
	/// </summary>
	public static void SetIsExitElement(UIElement element, bool value)
	{
		element.SetValue(IsExitElementProperty, value);
	}

	/// <summary>
	/// Identifies the IsExitElement attached property.
	/// </summary>
	public static DependencyProperty IsExitElementProperty
	{
		[DynamicDependency(nameof(GetIsExitElement))]
		[DynamicDependency(nameof(SetIsExitElement))]
		get;
	} = DependencyProperty.RegisterAttached(
		"IsExitElement",
		typeof(bool),
		typeof(ContinuumNavigationTransitionInfo),
		new FrameworkPropertyMetadata(false, OnIsExitElementChanged));

	private static void OnIsExitElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not UIElement element)
		{
			return;
		}

		var isExitElement = (bool)e.NewValue;

		lock (s_exitElements)
		{
			s_exitElements.RemoveAll(wr => !wr.TryGetTarget(out var target) || target == element);

			if (isExitElement)
			{
				s_exitElements.Add(new WeakReference<UIElement>(element));
			}
		}
	}

	#endregion

	#region ExitElementContainer Attached Property

	/// <summary>
	/// Gets the value of the ExitElementContainer attached property for a ListViewBase.
	/// </summary>
	public static bool GetExitElementContainer(ListViewBase element)
	{
		return (bool)element.GetValue(ExitElementContainerProperty);
	}

	/// <summary>
	/// Sets the value of the ExitElementContainer attached property for a ListViewBase.
	/// </summary>
	public static void SetExitElementContainer(ListViewBase element, bool value)
	{
		element.SetValue(ExitElementContainerProperty, value);
	}

	/// <summary>
	/// Identifies the ExitElementContainer attached property.
	/// </summary>
	public static DependencyProperty ExitElementContainerProperty { get; } =
		DependencyProperty.RegisterAttached(
			"ExitElementContainer",
			typeof(bool),
			typeof(ContinuumNavigationTransitionInfo),
			new FrameworkPropertyMetadata(false));

	#endregion

	protected override IList<Storyboard> CreateStoryboardsCore(UIElement element, NavigationTrigger trigger)
	{
		var storyboards = new List<Storyboard>();

		// Create the main page animation
		var mainStoryboard = CreateMainPageAnimation(element, trigger);
		storyboards.Add(mainStoryboard);

		// Create animations for marked entrance/exit elements
		switch (trigger)
		{
			case NavigationTrigger.NavigatingTo:
			case NavigationTrigger.BackNavigatingTo:
				var entranceStoryboards = CreateEntranceElementAnimations(element, trigger);
				storyboards.AddRange(entranceStoryboards);
				break;

			case NavigationTrigger.NavigatingAway:
			case NavigationTrigger.BackNavigatingAway:
				var exitStoryboards = CreateExitElementAnimations(element, trigger);
				storyboards.AddRange(exitStoryboards);
				break;
		}

		return storyboards;
	}

	private static Storyboard CreateMainPageAnimation(UIElement element, NavigationTrigger trigger)
	{
		var storyboard = new Storyboard();

		var inControlPoint1 = new Point(0.1, 0.9);
		var inControlPoint2 = new Point(0.2, 1.0);
		var outControlPoint1 = new Point(0.7, 0.0);
		var outControlPoint2 = new Point(1.0, 0.5);

		switch (trigger)
		{
			case NavigationTrigger.NavigatingAway:
			case NavigationTrigger.BackNavigatingAway:
				// Fade out the page
				AddOpacityAnimation(storyboard, element, 1.0, 0.0, OutDuration, outControlPoint1, outControlPoint2);
				break;

			case NavigationTrigger.NavigatingTo:
			case NavigationTrigger.BackNavigatingTo:
				// Fade in the page
				AddDelayedOpacityAnimation(storyboard, element, 0.0, OutDuration, 1.0, OutDuration + InDuration, inControlPoint1, inControlPoint2);
				break;
		}

		return storyboard;
	}

	private List<Storyboard> CreateEntranceElementAnimations(UIElement page, NavigationTrigger trigger)
	{
		var storyboards = new List<Storyboard>();
		var entranceElements = GetEntranceElementsForPage(page);

		foreach (var entranceElement in entranceElements)
		{
			var storyboard = CreateContinuumEntranceAnimation(entranceElement);
			storyboards.Add(storyboard);
		}

		return storyboards;
	}

	private List<Storyboard> CreateExitElementAnimations(UIElement page, NavigationTrigger trigger)
	{
		var storyboards = new List<Storyboard>();

		// First check the ExitElement property
		if (ExitElement != null && IsAncestor(page, ExitElement))
		{
			var storyboard = CreateContinuumExitAnimation(ExitElement);
			storyboards.Add(storyboard);
		}

		// Then check for elements marked with IsExitElement
		var exitElements = GetExitElementsForPage(page);
		foreach (var exitElement in exitElements)
		{
			var storyboard = CreateContinuumExitAnimation(exitElement);
			storyboards.Add(storyboard);
		}

		return storyboards;
	}

	private static Storyboard CreateContinuumEntranceAnimation(UIElement element)
	{
		var storyboard = new Storyboard();
		var transformOrigin = new Point(0.5, 0.5);

		var scaleTransform = new ScaleTransform { ScaleX = EntranceScaleFactor, ScaleY = EntranceScaleFactor };
		element.RenderTransform = scaleTransform;
		element.RenderTransformOrigin = transformOrigin;

		var inControlPoint1 = new Point(0.1, 0.9);
		var inControlPoint2 = new Point(0.2, 1.0);

		// Scale from larger to normal
		AddDelayedScaleAnimation(storyboard, scaleTransform, EntranceScaleFactor, OutDuration, 1.0, OutDuration + InDuration, inControlPoint1, inControlPoint2);

		// Fade in
		AddDelayedOpacityAnimation(storyboard, element, 0.0, OutDuration, 1.0, OutDuration + InDuration, inControlPoint1, inControlPoint2);

		return storyboard;
	}

	private static Storyboard CreateContinuumExitAnimation(UIElement element)
	{
		var storyboard = new Storyboard();
		var transformOrigin = new Point(0.5, 0.5);

		var scaleTransform = new ScaleTransform { ScaleX = 1.0, ScaleY = 1.0 };
		element.RenderTransform = scaleTransform;
		element.RenderTransformOrigin = transformOrigin;

		var outControlPoint1 = new Point(0.7, 0.0);
		var outControlPoint2 = new Point(1.0, 0.5);

		// Scale down
		AddScaleAnimation(storyboard, scaleTransform, 1.0, ExitScaleFactor, OutDuration, outControlPoint1, outControlPoint2);

		// Fade out
		AddOpacityAnimation(storyboard, element, 1.0, 0.0, OutDuration, outControlPoint1, outControlPoint2);

		return storyboard;
	}

	private static List<UIElement> GetEntranceElementsForPage(UIElement page)
	{
		var result = new List<UIElement>();

		lock (s_entranceElements)
		{
			foreach (var weakRef in s_entranceElements)
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

	private static List<UIElement> GetExitElementsForPage(UIElement page)
	{
		var result = new List<UIElement>();

		lock (s_exitElements)
		{
			foreach (var weakRef in s_exitElements)
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

		var scaleXAnimation = new DoubleAnimationUsingKeyFrames();
		scaleXAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(from, KeyTime.FromTimeSpan(TimeSpan.Zero)));
		scaleXAnimation.KeyFrames.Add(new SplineDoubleKeyFrame(to, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMs)), keySpline));
		Storyboard.SetTarget(scaleXAnimation, scaleTransform);
		Storyboard.SetTargetProperty(scaleXAnimation, nameof(ScaleTransform.ScaleX));
		storyboard.Children.Add(scaleXAnimation);

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

		var scaleXAnimation = new DoubleAnimationUsingKeyFrames();
		scaleXAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(holdValue, KeyTime.FromTimeSpan(TimeSpan.Zero)));
		scaleXAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(holdValue, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(holdTimeMs))));
		scaleXAnimation.KeyFrames.Add(new SplineDoubleKeyFrame(endValue, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(endTimeMs)), keySpline));
		Storyboard.SetTarget(scaleXAnimation, scaleTransform);
		Storyboard.SetTargetProperty(scaleXAnimation, nameof(ScaleTransform.ScaleX));
		storyboard.Children.Add(scaleXAnimation);

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
	/// Clears expired weak references from the element tracking lists.
	/// </summary>
	internal static void ClearTrackedElements()
	{
		lock (s_entranceElements)
		{
			s_entranceElements.RemoveAll(wr => !wr.TryGetTarget(out _));
		}

		lock (s_exitElements)
		{
			s_exitElements.RemoveAll(wr => !wr.TryGetTarget(out _));
		}
	}
}
