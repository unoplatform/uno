// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference ThemeTransitions.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.Boxes;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// Specifies the animation to run when a user navigates forward in a logical hierarchy, like from a master list to a detail page.
/// </summary>
public partial class ContinuumNavigationTransitionInfo : NavigationTransitionInfo
{
	private static readonly List<WeakReference<DependencyObject>> s_spEntranceElements = new();
	private static readonly List<WeakReference<DependencyObject>> s_spExitElements = new();

	public ContinuumNavigationTransitionInfo() : base() { }

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs pArgs)
	{
	}

	#region ExitElement DependencyProperty

	/// <summary>
	/// Gets or sets the UIElement that is animated during the exit transition.
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
			new FrameworkPropertyMetadata(null, OnPropertyChanged));

	#endregion

	/// <inheritdoc />
	protected override string GetNavigationStateCore() => "0";

	/// <inheritdoc />
	protected override void SetNavigationStateCore(string navigationState)
	{
	}

	private protected override IList<Storyboard> CreateStoryboardsCore(UIElement element, NavigationTrigger trigger)
	{
		var spElement = element;
		UIElement spContinuumTarget;
		Storyboard spStoryboard = null;
		List<DoubleAnimationDescription> animations = new();

		// Strings used for property names.
		// UNO TODO: WinUI animates "(UIElement.TransitionTarget).(TransitionTarget.CompositeTransform)" and
		// "(UIElement.TransitionTarget).Opacity", which Uno lacks. The key frames target a CompositeTransform
		// installed as the element's RenderTransform, and UIElement.Opacity, instead.
		// UNO TODO: WinUI uses the "UIElement.IsHitTestVisible" path; Uno resolves the plain property name.
		var propertyNameIsHitTestVisible = nameof(UIElement.IsHitTestVisible);
		var propertyNameOpacity = nameof(UIElement.Opacity);
		// var propertyNameTranslateX = "(UIElement.RenderTransform).(CompositeTransform.TranslateX)";
		var propertyNameTranslateY = "(UIElement.RenderTransform).(CompositeTransform.TranslateY)";
		var propertyNameScaleX = "(UIElement.RenderTransform).(CompositeTransform.ScaleX)";
		var propertyNameScaleY = "(UIElement.RenderTransform).(CompositeTransform.ScaleY)";
		var propertyNameCenterOfRotationX = "(UIElement.Projection).(PlaneProjection.CenterOfRotationX)";
		var propertyNameCenterOfRotationY = "(UIElement.Projection).(PlaneProjection.CenterOfRotationY)";
		var propertyNameCenterOfRotationZ = "(UIElement.Projection).(PlaneProjection.CenterOfRotationZ)";
		var propertyNameRotationX = "(UIElement.Projection).(PlaneProjection.RotationX)";

		// Collections of key-frame descriptions used to define the animation.
		// The "target" refers to the element that flies around during continuum
		// while the background refers to the page. These are referred as the active
		// and inactive elements respectively in UIX.
		List<DoubleKeyFrameDescription> centerOfRotationXKeyFramesForTarget = new();
		List<DoubleKeyFrameDescription> centerOfRotationYKeyFramesForTarget = new();
		List<DoubleKeyFrameDescription> centerOfRotationZKeyFramesForTarget = new();
		List<DoubleKeyFrameDescription> rotationXKeyFramesForTarget = new();
		List<DoubleKeyFrameDescription> scaleXKeyFramesForBackground = new();
		List<DoubleKeyFrameDescription> scaleYKeyFramesForBackground = new();
		List<DoubleKeyFrameDescription> scaleXKeyFramesForTarget = new();
		List<DoubleKeyFrameDescription> scaleYKeyFramesForTarget = new();
		List<DoubleKeyFrameDescription> opacityKeyFramesForBackground = new();
		List<DoubleKeyFrameDescription> opacityKeyFramesForTarget = new();
		List<DoubleKeyFrameDescription> translateYKeyFramesForBackground = new();
		List<DoubleKeyFrameDescription> translateYKeyFramesForTarget = new();

		// Get easing functions.
		var spPowerEasingOut2 = NavigateTransitionHelper.GetPowerEase(2, EasingMode.EaseOut);
		var spCircleEasingOut = NavigateTransitionHelper.GetCircleEase(EasingMode.EaseOut);
		var spExponentialEasingIn2 = NavigateTransitionHelper.GetExponentialEase(2, EasingMode.EaseIn);
		var spExponentialEasingIn6 = NavigateTransitionHelper.GetExponentialEase(6, EasingMode.EaseIn);
		var spExponentialEasingOut4 = NavigateTransitionHelper.GetExponentialEase(4, EasingMode.EaseOut);
		var spExponentialEasingOut6 = NavigateTransitionHelper.GetExponentialEase(6, EasingMode.EaseOut);
		var spExponentialEasingOut8 = NavigateTransitionHelper.GetExponentialEase(8, EasingMode.EaseOut);
		var spExponentialEasingInOut = NavigateTransitionHelper.GetExponentialEase(0.75, EasingMode.EaseInOut);

		if (trigger == NavigationTrigger.NavigatingTo)
		{
			// Continuum animation forward in.
			const long speedFactor = 1;
			const long staggeringFactor = 267;
			const long startTime = (0 + staggeringFactor) * speedFactor;
			const long middleTime1 = (100 + staggeringFactor) * speedFactor;
			const long middleTime2 = (167 + staggeringFactor) * speedFactor;
			const long endTime = (350 + staggeringFactor) * speedFactor;
			const double scaleFactorForBackground = 0.9;
			const double centerOfRotationXForTarget = 0.0;
			const double centerOfRotationYForTarget = 1.0;
			const double centerOfRotationZForTarget = 0.0;
			const int xRotationOffsetForTarget = -50;
			const int yTranslationOffsetDownForTarget = 200;
			const int yTranslationOffsetUpForTarget = -40;
			const double scaleFactorForTarget = 1.5;
			Point transformOrigin = new(0.5f, 0.5f);

			// Define the key frames to scale the background along the X axis.
			scaleXKeyFramesForBackground.Add(new DoubleKeyFrameDescription(startTime, scaleFactorForBackground, spCircleEasingOut));
			scaleXKeyFramesForBackground.Add(new DoubleKeyFrameDescription(endTime, NavigateTransitionHelper.NO_SCALE, spCircleEasingOut));

			// Define the key frames to scale the background along the Y axis.
			scaleYKeyFramesForBackground.Add(new DoubleKeyFrameDescription(startTime, scaleFactorForBackground, spCircleEasingOut));
			scaleYKeyFramesForBackground.Add(new DoubleKeyFrameDescription(endTime, NavigateTransitionHelper.NO_SCALE, spCircleEasingOut));

			// Define the key frames to change the opacity of the background.
			opacityKeyFramesForBackground.Add(new DoubleKeyFrameDescription(NavigateTransitionHelper.TIME_ZERO, NavigateTransitionHelper.NO_OPACITY));
			opacityKeyFramesForBackground.Add(new DoubleKeyFrameDescription(startTime - 1, NavigateTransitionHelper.NO_OPACITY));
			opacityKeyFramesForBackground.Add(new DoubleKeyFrameDescription(startTime, NavigateTransitionHelper.FULL_OPACITY));

			// Define the animations.
			SetTransitionTargetTransform(spElement);
			animations.Add(new DoubleAnimationDescription(spElement, propertyNameScaleX, scaleXKeyFramesForBackground));
			animations.Add(new DoubleAnimationDescription(spElement, propertyNameScaleY, scaleYKeyFramesForBackground));
			animations.Add(new DoubleAnimationDescription(spElement, propertyNameOpacity, opacityKeyFramesForBackground));

			// Animation for target.
			spContinuumTarget = GetLogicalEntranceElement(element);

			if (spContinuumTarget is not null)
			{
				// Clobber this, as we have to clobber values anyway.
				PlaneProjection spPlaneProjection = new();
				spContinuumTarget.Projection = spPlaneProjection;
				SetTransitionTargetTransform(spContinuumTarget);

				// Define the key frame collections to hold the center of rotation
				// around the X, Y and Z axes.
				centerOfRotationXKeyFramesForTarget.Add(new DoubleKeyFrameDescription(startTime, centerOfRotationXForTarget));
				centerOfRotationYKeyFramesForTarget.Add(new DoubleKeyFrameDescription(startTime, centerOfRotationYForTarget));
				centerOfRotationZKeyFramesForTarget.Add(new DoubleKeyFrameDescription(startTime, centerOfRotationZForTarget));

				// Define the key frames to rotate the target around the X axis.
				rotationXKeyFramesForTarget.Add(new DoubleKeyFrameDescription(startTime, xRotationOffsetForTarget));
				rotationXKeyFramesForTarget.Add(new DoubleKeyFrameDescription(endTime, NavigateTransitionHelper.NO_OFFSET, spPowerEasingOut2));

				// Define the key frames to translate the target along the Y axis.
				translateYKeyFramesForTarget.Add(new DoubleKeyFrameDescription(startTime, yTranslationOffsetDownForTarget));
				translateYKeyFramesForTarget.Add(new DoubleKeyFrameDescription(middleTime1, yTranslationOffsetUpForTarget, spExponentialEasingOut8));
				translateYKeyFramesForTarget.Add(new DoubleKeyFrameDescription(endTime, NavigateTransitionHelper.NO_OFFSET, spExponentialEasingInOut));

				// Define the key frames to scale the target along the X axis.
				scaleXKeyFramesForTarget.Add(new DoubleKeyFrameDescription(startTime, scaleFactorForTarget));
				scaleXKeyFramesForTarget.Add(new DoubleKeyFrameDescription(endTime, NavigateTransitionHelper.NO_SCALE, spPowerEasingOut2));

				// Define the key frames to scale the target along the Y axis.
				scaleYKeyFramesForTarget.Add(new DoubleKeyFrameDescription(startTime, scaleFactorForTarget));
				scaleYKeyFramesForTarget.Add(new DoubleKeyFrameDescription(endTime, NavigateTransitionHelper.NO_SCALE, spPowerEasingOut2));

				// Define the key frames to change the opacity of the target.
				opacityKeyFramesForTarget.Add(new DoubleKeyFrameDescription(NavigateTransitionHelper.TIME_ZERO, NavigateTransitionHelper.NO_OPACITY));
				opacityKeyFramesForTarget.Add(new DoubleKeyFrameDescription(startTime, NavigateTransitionHelper.NO_OPACITY));
				opacityKeyFramesForTarget.Add(new DoubleKeyFrameDescription(middleTime2, NavigateTransitionHelper.FULL_OPACITY, spExponentialEasingOut6));

				// Define the animations.
				animations.Add(new DoubleAnimationDescription(spContinuumTarget, propertyNameCenterOfRotationX, centerOfRotationXKeyFramesForTarget));
				animations.Add(new DoubleAnimationDescription(spContinuumTarget, propertyNameCenterOfRotationY, centerOfRotationYKeyFramesForTarget));
				animations.Add(new DoubleAnimationDescription(spContinuumTarget, propertyNameCenterOfRotationZ, centerOfRotationZKeyFramesForTarget));
				animations.Add(new DoubleAnimationDescription(spContinuumTarget, propertyNameRotationX, rotationXKeyFramesForTarget));
				animations.Add(new DoubleAnimationDescription(spContinuumTarget, propertyNameTranslateY, translateYKeyFramesForTarget));
				animations.Add(new DoubleAnimationDescription(spContinuumTarget, propertyNameScaleX, scaleXKeyFramesForTarget));
				animations.Add(new DoubleAnimationDescription(spContinuumTarget, propertyNameScaleY, scaleYKeyFramesForTarget));
				animations.Add(new DoubleAnimationDescription(spContinuumTarget, propertyNameOpacity, opacityKeyFramesForTarget));
			}

			// Create the storyboard.
			spStoryboard = NavigateTransitionHelper.CreateStoryboardForNavigationAnimations(animations);

			// Set the transform origin by adding a point animation.
			NavigateTransitionHelper.SetTransformOrigin(spElement, transformOrigin, spStoryboard);
		}
		else if (trigger == NavigationTrigger.NavigatingAway)
		{
			// Continuum animation forward out.
			const long speedFactor = 1;
			const long staggeringFactor = 0;
			const long startTime = (0 + staggeringFactor) * speedFactor;
			const long middleTime1 = (100 + staggeringFactor) * speedFactor;
			const long middleTime2 = (120 + staggeringFactor) * speedFactor;
			const long endTime = (250 + staggeringFactor) * speedFactor;
			const double centerOfRotationXForTarget = 0.0;
			const double centerOfRotationYForTarget = 1.0;
			const double centerOfRotationZForTarget = 0.0;
			const int xRotationOffsetForTarget = 80;
			const int yTranslationOffsetForTarget = 200;
			const double scaleFactorForTarget = 1.5;
			Point transformOrigin = new(0.0f, 1.0f);

			// Define the key frames to change the opacity of the background.
			opacityKeyFramesForBackground.Add(new DoubleKeyFrameDescription(NavigateTransitionHelper.TIME_ZERO, NavigateTransitionHelper.FULL_OPACITY));
			opacityKeyFramesForBackground.Add(new DoubleKeyFrameDescription(startTime, NavigateTransitionHelper.FULL_OPACITY));
			// NOTE: This next key frame is not part of the original fade-out animation.
			// We are adding it in order to prevent the target from becoming invisible
			// halfway through the animation.
			opacityKeyFramesForBackground.Add(new DoubleKeyFrameDescription(middleTime2, NavigateTransitionHelper.FULL_OPACITY));
			opacityKeyFramesForBackground.Add(new DoubleKeyFrameDescription(endTime, NavigateTransitionHelper.NO_OPACITY, spCircleEasingOut));

			// Define the animations.
			animations.Add(new DoubleAnimationDescription(spElement, propertyNameOpacity, opacityKeyFramesForBackground));

			// Animation for target.
			spContinuumTarget = GetLogicalExitElement(element);

			if (spContinuumTarget is not null)
			{
				// Clobber this, as we have to clobber values anyway.
				PlaneProjection spElementPlaneProjection = new();
				spContinuumTarget.Projection = spElementPlaneProjection;
				SetTransitionTargetTransform(spContinuumTarget);

				// Define the key frame collections to hold the center of rotation
				// around the X, Y and Z axes.
				centerOfRotationXKeyFramesForTarget.Add(new DoubleKeyFrameDescription(startTime, centerOfRotationXForTarget));
				centerOfRotationYKeyFramesForTarget.Add(new DoubleKeyFrameDescription(startTime, centerOfRotationYForTarget));
				centerOfRotationZKeyFramesForTarget.Add(new DoubleKeyFrameDescription(startTime, centerOfRotationZForTarget));

				// Define the key frames to rotate the target around the X axis.
				rotationXKeyFramesForTarget.Add(new DoubleKeyFrameDescription(startTime, NavigateTransitionHelper.NO_OFFSET));
				rotationXKeyFramesForTarget.Add(new DoubleKeyFrameDescription(middleTime2, xRotationOffsetForTarget, spExponentialEasingIn2));

				// Define the key frames to translate the target along the Y axis.
				translateYKeyFramesForTarget.Add(new DoubleKeyFrameDescription(startTime, NavigateTransitionHelper.NO_OFFSET));
				translateYKeyFramesForTarget.Add(new DoubleKeyFrameDescription(middleTime2, yTranslationOffsetForTarget, spExponentialEasingIn6));

				// Define the key frames to scale the target along the X axis.
				scaleXKeyFramesForTarget.Add(new DoubleKeyFrameDescription(startTime, NavigateTransitionHelper.NO_SCALE));
				scaleXKeyFramesForTarget.Add(new DoubleKeyFrameDescription(middleTime2, scaleFactorForTarget, spExponentialEasingIn6));

				// Define the key frames to scale the target along the Y axis.
				scaleYKeyFramesForTarget.Add(new DoubleKeyFrameDescription(startTime, NavigateTransitionHelper.NO_SCALE));
				scaleYKeyFramesForTarget.Add(new DoubleKeyFrameDescription(middleTime2, scaleFactorForTarget, spExponentialEasingIn6));

				// Define the key frames to change the opacity of the target.
				opacityKeyFramesForTarget.Add(new DoubleKeyFrameDescription(NavigateTransitionHelper.TIME_ZERO, NavigateTransitionHelper.FULL_OPACITY));
				opacityKeyFramesForTarget.Add(new DoubleKeyFrameDescription(startTime, NavigateTransitionHelper.FULL_OPACITY));
				opacityKeyFramesForTarget.Add(new DoubleKeyFrameDescription(middleTime1, NavigateTransitionHelper.FULL_OPACITY));
				opacityKeyFramesForTarget.Add(new DoubleKeyFrameDescription(middleTime2, NavigateTransitionHelper.NO_OPACITY));

				// Define the animations.
				animations.Add(new DoubleAnimationDescription(spContinuumTarget, propertyNameCenterOfRotationX, centerOfRotationXKeyFramesForTarget));
				animations.Add(new DoubleAnimationDescription(spContinuumTarget, propertyNameCenterOfRotationY, centerOfRotationYKeyFramesForTarget));
				animations.Add(new DoubleAnimationDescription(spContinuumTarget, propertyNameCenterOfRotationZ, centerOfRotationZKeyFramesForTarget));
				animations.Add(new DoubleAnimationDescription(spContinuumTarget, propertyNameRotationX, rotationXKeyFramesForTarget));
				animations.Add(new DoubleAnimationDescription(spContinuumTarget, propertyNameTranslateY, translateYKeyFramesForTarget));
				animations.Add(new DoubleAnimationDescription(spContinuumTarget, propertyNameScaleX, scaleXKeyFramesForTarget));
				animations.Add(new DoubleAnimationDescription(spContinuumTarget, propertyNameScaleY, scaleYKeyFramesForTarget));
				animations.Add(new DoubleAnimationDescription(spContinuumTarget, propertyNameOpacity, opacityKeyFramesForTarget));
			}

			// Create the storyboard.
			spStoryboard = NavigateTransitionHelper.CreateStoryboardForNavigationAnimations(animations);

			if (spContinuumTarget is not null)
			{
				// Set the transform origin by adding a point animation.
				NavigateTransitionHelper.SetTransformOrigin(spContinuumTarget, transformOrigin, spStoryboard);
			}
		}
		else if (trigger == NavigationTrigger.BackNavigatingTo)
		{
			// Continuum animation backward in.
			const long speedFactor = 1;
			const long staggeringFactor = 267;
			const long startTime = (0 + staggeringFactor) * speedFactor;
			const long middleTime = (200 + staggeringFactor) * speedFactor;
			const long endTime = (350 + staggeringFactor) * speedFactor;
			const double centerOfRotationXForTarget = 0.0;
			const double centerOfRotationYForTarget = 0.0;
			const double centerOfRotationZForTarget = 0.0;
			const int xRotationOffsetForTarget = -90;
			const int yTranslationOffsetForTarget = -20;

			// Define the key frames to change the opacity of the background.
			opacityKeyFramesForBackground.Add(new DoubleKeyFrameDescription(NavigateTransitionHelper.TIME_ZERO, NavigateTransitionHelper.NO_OPACITY));
			opacityKeyFramesForBackground.Add(new DoubleKeyFrameDescription(startTime, NavigateTransitionHelper.NO_OPACITY, spCircleEasingOut));
			opacityKeyFramesForBackground.Add(new DoubleKeyFrameDescription(endTime, NavigateTransitionHelper.FULL_OPACITY, spCircleEasingOut));

			// Define the animations.
			animations.Add(new DoubleAnimationDescription(spElement, propertyNameOpacity, opacityKeyFramesForBackground));

			// Animation for target.
			// [TODO] - This call only works when NavigationCacheMode on the Page is
			// "Enabled" or "Required".  How we will handle this is currently unknown, as the cache and
			// state story in Jupiter is weak.
			spContinuumTarget = GetLogicalExitElement(element);

			if (spContinuumTarget is not null)
			{
				// Clobber this, as we have to clobber values anyway.
				PlaneProjection spElementPlaneProjection = new();
				spContinuumTarget.Projection = spElementPlaneProjection;
				SetTransitionTargetTransform(spContinuumTarget);

				// Define the key frame collections to hold the center of rotation
				// around the X, Y and Z axes.
				centerOfRotationXKeyFramesForTarget.Add(new DoubleKeyFrameDescription(startTime, centerOfRotationXForTarget));
				centerOfRotationYKeyFramesForTarget.Add(new DoubleKeyFrameDescription(startTime, centerOfRotationYForTarget));
				centerOfRotationZKeyFramesForTarget.Add(new DoubleKeyFrameDescription(startTime, centerOfRotationZForTarget));

				// Define the key frames to rotate the target around the X axis.
				rotationXKeyFramesForTarget.Add(new DoubleKeyFrameDescription(startTime, xRotationOffsetForTarget));
				rotationXKeyFramesForTarget.Add(new DoubleKeyFrameDescription(middleTime, NavigateTransitionHelper.NO_OFFSET, spExponentialEasingOut4));

				// Define the key frames to translate the target along the Y axis.
				translateYKeyFramesForTarget.Add(new DoubleKeyFrameDescription(startTime, yTranslationOffsetForTarget));
				translateYKeyFramesForTarget.Add(new DoubleKeyFrameDescription(middleTime, NavigateTransitionHelper.NO_OFFSET, spExponentialEasingOut4));

				// Define the key frames to change the opacity of the target.
				opacityKeyFramesForTarget.Add(new DoubleKeyFrameDescription(NavigateTransitionHelper.TIME_ZERO, NavigateTransitionHelper.NO_OPACITY));
				opacityKeyFramesForTarget.Add(new DoubleKeyFrameDescription(startTime - 1, NavigateTransitionHelper.NO_OPACITY));
				opacityKeyFramesForTarget.Add(new DoubleKeyFrameDescription(startTime, NavigateTransitionHelper.FULL_OPACITY));

				// Define the animations.
				animations.Add(new DoubleAnimationDescription(spContinuumTarget, propertyNameCenterOfRotationX, centerOfRotationXKeyFramesForTarget));
				animations.Add(new DoubleAnimationDescription(spContinuumTarget, propertyNameCenterOfRotationY, centerOfRotationYKeyFramesForTarget));
				animations.Add(new DoubleAnimationDescription(spContinuumTarget, propertyNameCenterOfRotationZ, centerOfRotationZKeyFramesForTarget));
				animations.Add(new DoubleAnimationDescription(spContinuumTarget, propertyNameRotationX, rotationXKeyFramesForTarget));
				animations.Add(new DoubleAnimationDescription(spContinuumTarget, propertyNameTranslateY, translateYKeyFramesForTarget));
				animations.Add(new DoubleAnimationDescription(spContinuumTarget, propertyNameOpacity, opacityKeyFramesForTarget));
			}

			// Create the storyboard.
			spStoryboard = NavigateTransitionHelper.CreateStoryboardForNavigationAnimations(animations);
		}
		else if (trigger == NavigationTrigger.BackNavigatingAway)
		{
			// Continuum animation backward out.
			const long speedFactor = 1;
			const long staggeringFactor = 0;
			const long startTime = (0 + staggeringFactor) * speedFactor;
			const long middleTime = (240 + staggeringFactor) * speedFactor;
			const long endTime = (250 + staggeringFactor) * speedFactor;
			const int yOffsetForBackground = 200;

			// Define the key frames to translate the background along the Y axis.
			translateYKeyFramesForBackground.Add(new DoubleKeyFrameDescription(startTime, NavigateTransitionHelper.NO_OFFSET));
			translateYKeyFramesForBackground.Add(new DoubleKeyFrameDescription(endTime, yOffsetForBackground, spExponentialEasingIn6));

			// Define the key frames to change the opacity of the background.
			opacityKeyFramesForBackground.Add(new DoubleKeyFrameDescription(NavigateTransitionHelper.TIME_ZERO, NavigateTransitionHelper.FULL_OPACITY));
			opacityKeyFramesForBackground.Add(new DoubleKeyFrameDescription(startTime, NavigateTransitionHelper.FULL_OPACITY));
			opacityKeyFramesForBackground.Add(new DoubleKeyFrameDescription(middleTime, NavigateTransitionHelper.FULL_OPACITY));
			opacityKeyFramesForBackground.Add(new DoubleKeyFrameDescription(endTime, NavigateTransitionHelper.NO_OPACITY));

			// Define the animations.
			SetTransitionTargetTransform(spElement);
			animations.Add(new DoubleAnimationDescription(spElement, propertyNameTranslateY, translateYKeyFramesForBackground));
			animations.Add(new DoubleAnimationDescription(spElement, propertyNameOpacity, opacityKeyFramesForBackground));

			// Create the storyboard.
			spStoryboard = NavigateTransitionHelper.CreateStoryboardForNavigationAnimations(animations);
		}

		// Ensure that the animated pages are not hit test visible.
		{
			List<ObjectKeyFrameDescription> keyFramesIsHitTestVisible = new();

			// Set isHitTestVisible key frames.
			keyFramesIsHitTestVisible.Add(new ObjectKeyFrameDescription(NavigateTransitionHelper.TIME_ZERO, false));

			// Add object animations to storyboard.
			var spAnimation = NavigateTransitionHelper.GetObjectAnimation(new ObjectAnimationDescription(spElement, propertyNameIsHitTestVisible, keyFramesIsHitTestVisible));

			NavigateTransitionHelper.AddTimelineToStoryboard(spAnimation, spStoryboard);
		}

		return new List<Storyboard> { spStoryboard };
	}

	// UNO TODO: Uno has no TransitionTarget, whose CompositeTransform WinUI animates; install one as the RenderTransform.
	private static void SetTransitionTargetTransform(UIElement element) => element.RenderTransform = new CompositeTransform();

	private UIElement GetLogicalExitElement(UIElement page)
	{
		// Check for explicit target.
		var spExitElement = ExitElement;
		return spExitElement;
	}

	private static UIElement GetLogicalEntranceElement(UIElement page)
	{
		UIElement spEntranceElementResult = null;

		var entranceElements = GetEntranceElements();

		foreach (var it in entranceElements)
		{
			it.TryGetTarget(out var target);
			var spEntranceElement = target as UIElement;

			if (spEntranceElement is not null)
			{
				// UNO TODO: WinUI fails when the page or the entrance element is not a FrameworkElement.
				if (page is FrameworkElement spPageAsFE
					&& spEntranceElement is FrameworkElement spEntranceElementAsFE
					&& NavigateTransitionHelper.IsAncestor(spPageAsFE, spEntranceElementAsFE))
				{
					spEntranceElementResult = spEntranceElement;
					break;
				}
			}
		}

		return spEntranceElementResult;
	}

	// ContinuumNavigationTransitionInfoFactory

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
		new FrameworkPropertyMetadata(BoolBoxes.False, OnPropertyChanged));

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
		new FrameworkPropertyMetadata(BoolBoxes.False, OnPropertyChanged));

	#endregion

	#region ExitElementContainer Attached Property

	/// <summary>
	/// Gets the value of the ExitElementContainer attached property for a specified ListViewBase.
	/// </summary>
	public static bool GetExitElementContainer(ListViewBase element)
	{
		return (bool)element.GetValue(ExitElementContainerProperty);
	}

	/// <summary>
	/// Sets the value of the ExitElementContainer attached property for a specified ListViewBase.
	/// </summary>
	public static void SetExitElementContainer(ListViewBase element, bool value)
	{
		element.SetValue(ExitElementContainerProperty, value);
	}

	/// <summary>
	/// Identifies the ExitElementContainer attached property.
	/// </summary>
	public static DependencyProperty ExitElementContainerProperty
	{
		[DynamicDependency(nameof(GetExitElementContainer))]
		[DynamicDependency(nameof(SetExitElementContainer))]
		get;
	} = DependencyProperty.RegisterAttached(
		"ExitElementContainer",
		typeof(bool),
		typeof(ContinuumNavigationTransitionInfo),
		new FrameworkPropertyMetadata(BoolBoxes.False, OnPropertyChanged));

	#endregion

	private static void OnPropertyChanged(DependencyObject pSender, DependencyPropertyChangedEventArgs pArgs)
	{
		var spDO = pSender;

		if (pArgs.Property == ExitElementProperty)
		{
			// This is just a DP: bubble up to DO.
			((ContinuumNavigationTransitionInfo)pSender).OnPropertyChanged(pArgs);
		}
		else if (pArgs.Property == IsEntranceElementProperty)
		{
			// This is an attached property, bubble up to page.
			var isEntranceElement = (bool)pArgs.NewValue;

			lock (s_spEntranceElements)
			{
				// Erase the value in the list.
				for (var it = 0; it < s_spEntranceElements.Count; ++it)
				{
					s_spEntranceElements[it].TryGetTarget(out var spItAsDO);

					if (spItAsDO == spDO)
					{
						s_spEntranceElements.RemoveAt(it);
						break;
					}
				}

				if (isEntranceElement)
				{
					s_spEntranceElements.Add(new WeakReference<DependencyObject>(spDO));
				}
			}
		}
		else if (pArgs.Property == IsExitElementProperty)
		{
			// This is an attached property, bubble up to page.
			var isExitElement = (bool)pArgs.NewValue;

			lock (s_spExitElements)
			{
				// Erase the value in the list.
				for (var it = 0; it < s_spExitElements.Count; ++it)
				{
					s_spExitElements[it].TryGetTarget(out var spItAsDO);

					if (spItAsDO == spDO)
					{
						s_spExitElements.RemoveAt(it);
						break;
					}
				}

				if (isExitElement)
				{
					s_spExitElements.Add(new WeakReference<DependencyObject>(spDO));
				}
			}
		}
	}

	internal static List<WeakReference<DependencyObject>> GetEntranceElements()
	{
		lock (s_spEntranceElements)
		{
			return new(s_spEntranceElements);
		}
	}

	internal static void ClearEntranceElements()
	{
		lock (s_spEntranceElements)
		{
			s_spEntranceElements.RemoveAll(it => !it.TryGetTarget(out _));
		}
	}

	internal static List<WeakReference<DependencyObject>> GetExitElements()
	{
		lock (s_spExitElements)
		{
			return new(s_spExitElements);
		}
	}

	internal static void ClearExitElements()
	{
		lock (s_spExitElements)
		{
			s_spExitElements.RemoveAll(it => !it.TryGetTarget(out _));
		}
	}
}
