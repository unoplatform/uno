// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference ThemeTransitions.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Uno.UI.Helpers.Boxes;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// Specifies the animation to run when content appears on a Page.
/// </summary>
public partial class EntranceNavigationTransitionInfo : NavigationTransitionInfo
{
	private static readonly List<WeakReference<DependencyObject>> s_spTargetElements = new();

	public EntranceNavigationTransitionInfo() : base() { }

	/// <inheritdoc />
	protected override string GetNavigationStateCore() => "0";

	/// <inheritdoc />
	protected override void SetNavigationStateCore(string navigationState)
	{
	}

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
		// UNO TODO: WinUI registers this property as "IsEntranceElement". Uno resolves attached properties
		// by their registered name, so it keeps the public name.
		"IsTargetElement",
		typeof(bool),
		typeof(EntranceNavigationTransitionInfo),
		new FrameworkPropertyMetadata(BoolBoxes.False, OnPropertyChanged)
	);

	private static void OnPropertyChanged(DependencyObject pSender, DependencyPropertyChangedEventArgs pArgs)
	{
		var spDO = pSender;

		if (pArgs.Property == IsTargetElementProperty)
		{
			// This is an attached property, bubble up to page.
			var isTargetElement = (bool)pArgs.NewValue;

			lock (s_spTargetElements)
			{
				// Erase the value in the list.
				for (var it = 0; it < s_spTargetElements.Count; ++it)
				{
					s_spTargetElements[it].TryGetTarget(out var spItAsDO);

					if (spItAsDO == spDO)
					{
						s_spTargetElements.RemoveAt(it);
						break;
					}
				}

				if (isTargetElement)
				{
					s_spTargetElements.Add(new WeakReference<DependencyObject>(spDO));
				}
			}
		}
	}

	private static List<WeakReference<DependencyObject>> GetTargetElements()
	{
		lock (s_spTargetElements)
		{
			return new(s_spTargetElements);
		}
	}

	internal static void ClearTargetElements()
	{
		lock (s_spTargetElements)
		{
			s_spTargetElements.RemoveAll(it => !it.TryGetTarget(out _));
		}
	}

	#endregion

	private protected override IList<Storyboard> CreateStoryboardsCore(UIElement element, NavigationTrigger trigger)
	{
		var spElement = element;
		DependencyObject spAnimationTargetAsDO;
		Storyboard spStoryboard = new();

		// UNO TODO: WinUI animates "(UIElement.TransitionTarget).(TransitionTarget.CompositeTransform).TranslateY" and
		// "(UIElement.TransitionTarget).Opacity"; Uno animates a TranslateTransform set as RenderTransform, and UIElement.Opacity.
		var propertyNameTranslateY = nameof(TranslateTransform.Y);
		var propertyNameOpacity = nameof(UIElement.Opacity);

		var spTarget = GetLogicalTargetElement(element);

		if (spTarget is not null)
		{
			spAnimationTargetAsDO = spTarget;
		}
		else
		{
			spAnimationTargetAsDO = spElement;
		}

		var spTimelines = spStoryboard.Children;

		{
			const double translationOffset = 140;
			Point inControlPoint1 = new(0.1f, 0.9f);
			Point inControlPoint2 = new(0.2f, 1.0f);
			Point outControlPoint1 = new(0.7f, 0.0f);
			Point outControlPoint2 = new(1.0f, .5f);
			const long outDuration = 150;
			const long inDuration = 300;

			DoubleAnimationUsingKeyFrames spTranslateYAnimation = null;
			DoubleAnimationUsingKeyFrames spOpacityAnimation = null;

			switch (trigger)
			{
				case NavigationTrigger.NavigatingAway:
					spOpacityAnimation = new();
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, 1.0, 0);
					NavigateTransitionHelper.RegisterSplineKeyFrame(spOpacityAnimation, 0.0, outDuration, outControlPoint1, outControlPoint2);
					break;
				case NavigationTrigger.NavigatingTo:
					spOpacityAnimation = new();
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, 0.0, 0);
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, 1.0, outDuration);

					spTranslateYAnimation = new();
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spTranslateYAnimation, translationOffset, 0);
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spTranslateYAnimation, translationOffset, outDuration);
					NavigateTransitionHelper.RegisterSplineKeyFrame(spTranslateYAnimation, 0.0, outDuration + inDuration, inControlPoint1, inControlPoint2);
					break;
				case NavigationTrigger.BackNavigatingAway:
					spOpacityAnimation = new();
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, 1.0, 0);
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, 0.0, outDuration);  // Hide it at end of translate animation

					spTranslateYAnimation = new();
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spTranslateYAnimation, 0.0, 0);
					NavigateTransitionHelper.RegisterSplineKeyFrame(spTranslateYAnimation, translationOffset, outDuration, outControlPoint1, outControlPoint2);
					break;
				case NavigationTrigger.BackNavigatingTo:
					spOpacityAnimation = new();
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, 0.0, 0);
					NavigateTransitionHelper.RegisterDiscreteKeyFrame(spOpacityAnimation, 0.0, outDuration);
					NavigateTransitionHelper.RegisterSplineKeyFrame(spOpacityAnimation, 1.0, outDuration + inDuration, inControlPoint1, inControlPoint2);
					break;
			}

			if (spTranslateYAnimation != null)
			{
				TranslateTransform translateTransform = new();
				((UIElement)spAnimationTargetAsDO).RenderTransform = translateTransform;
				Storyboard.SetTarget(spTranslateYAnimation, translateTransform);
				Storyboard.SetTargetProperty(spTranslateYAnimation, propertyNameTranslateY);
				spTimelines.Add(spTranslateYAnimation);
			}

			if (spOpacityAnimation != null)
			{
				Storyboard.SetTarget(spOpacityAnimation, spAnimationTargetAsDO);
				Storyboard.SetTargetProperty(spOpacityAnimation, propertyNameOpacity);
				spTimelines.Add(spOpacityAnimation);
			}
		}

		return new List<Storyboard> { spStoryboard };
	}

	private static UIElement GetLogicalTargetElement(UIElement page)
	{
		UIElement spTargetElement = null;

		var targetElements = GetTargetElements();

		foreach (var it in targetElements)
		{
			// A dead reference resets the result, as resolving a WinUI weak reference does.
			spTargetElement = it.TryGetTarget(out var target) ? target as UIElement : null;

			if (spTargetElement is not null)
			{
				// UNO TODO: WinUI fails when the page or the target is not a FrameworkElement.
				if (page is FrameworkElement spPageAsFE
					&& spTargetElement is FrameworkElement spTargetElementAsFE
					&& NavigateTransitionHelper.IsAncestor(spPageAsFE, spTargetElementAsFE))
				{
					// Valid target element found.
					break;
				}
			}
		}

		return spTargetElement;
	}
}
