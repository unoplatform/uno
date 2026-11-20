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
/// Specifies the common page navigation transition behavior that applies to all
/// NavigationThemeTransition page navigations.
/// </summary>
public partial class CommonNavigationTransitionInfo : NavigationTransitionInfo
{
	private static readonly List<WeakReference<DependencyObject>> s_spStaggerElements = new();

	public CommonNavigationTransitionInfo() : base() { }

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs pArgs)
	{
	}

	/// <summary>
	/// Gets or sets a value that determines whether staggering is enabled for the transition.
	/// </summary>
	public bool IsStaggeringEnabled
	{
		get => (bool)GetValue(IsStaggeringEnabledProperty);
		set => SetValue(IsStaggeringEnabledProperty, value);
	}

	/// <inheritdoc />
	protected override string GetNavigationStateCore()
	{
		// The navigation state system saved in strings.
		// This navigation state for TNTI only needs to save
		// the StaggeringEnabled property; as such, we are
		// saving "1" for TRUE and "0" for FALSE.

		var navState = "0";

		var isStaggeringEnabled = IsStaggeringEnabled;

		if (isStaggeringEnabled)
		{
			navState = "1";
		}

		return navState;
	}

	/// <inheritdoc />
	protected override void SetNavigationStateCore(string navigationState)
	{
		const string navStateEnabled = "1";
		var isStaggeringEnabled = false;

		var ordinal = string.CompareOrdinal(navStateEnabled, navigationState);

		if (ordinal == 0)
		{
			isStaggeringEnabled = true;
		}

		IsStaggeringEnabled = isStaggeringEnabled;
	}

	private protected override IList<Storyboard> CreateStoryboardsCore(UIElement element, NavigationTrigger trigger)
	{
		var isStaggeringEnabled = false;

		UIElement spElement;
		Storyboard spStoryboard;
		List<UIElement> visibleTargets = new();
		EasingFunctionBase spEasing = null;
		List<DoubleAnimationDescription> animations = new();

		// Create storyboard.
		spElement = element;

		// Instantiate property string.
		var propertyNameRotation = "(UIElement.Projection).(PlaneProjection.RotationY)";
		var propertyNameCenterX = "(UIElement.Projection).(PlaneProjection.CenterOfRotationX)";
		var propertyNameCenterZ = "(UIElement.Projection).(PlaneProjection.CenterOfRotationZ)";
		// UNO TODO: WinUI animates "(UIElement.TransitionTarget).Opacity"; Uno animates UIElement.Opacity.
		var propertyNameOpacity = nameof(UIElement.Opacity);
		// UNO TODO: WinUI uses the "UIElement.IsHitTestVisible" path; Uno resolves the plain property name.
		var propertyNameIsHitTestVisible = nameof(UIElement.IsHitTestVisible);

		isStaggeringEnabled = IsStaggeringEnabled;

		// Do the work for getting visible targets, as we don't
		// want to unnecessarily repeat the code.
		// UNO TODO: WinUI fails when the page is not a FrameworkElement; Uno does not stagger it.
		if (isStaggeringEnabled && spElement is FrameworkElement spElementAsFrameworkElement)
		{
			// Get the Page's Rect.
			double pageWidth = 0;
			double pageHeight = 0;
			Rect pageRect = default;

			pageWidth = spElementAsFrameworkElement.ActualWidth;
			pageHeight = spElementAsFrameworkElement.ActualHeight;

			pageRect.X = pageRect.Y = 0;
			pageRect.Width = (float)pageWidth;
			pageRect.Height = (float)pageHeight;

			// Get the stagger targets.
			var targets = GetStaggerElements();

			// Iterate through stagger targets.
			foreach (var it in targets)
			{
				it.TryGetTarget(out var spTargetAsDO);
				var spTarget = spTargetAsDO as UIElement;

				// UNO TODO: WinUI fails when a target is not a FrameworkElement; Uno skips it.
				if (spTarget is FrameworkElement spTargetAsFrameworkElement)
				{
					var pageIsAncestor = false;

					// Get the target's Rect.
					double targetWidth = 0;
					double targetHeight = 0;
					Rect targetRect = default;

					Rect targetTransformRect = default;

					targetWidth = spTargetAsFrameworkElement.ActualWidth;
					targetHeight = spTargetAsFrameworkElement.ActualHeight;

					targetRect.X = targetRect.Y = 0;
					targetRect.Width = (float)targetWidth;
					targetRect.Height = (float)targetHeight;

					// Transform the target to the Page and transform its Rect.
					var spTargetTransform = spTarget.TransformToVisual(spElement);
					targetTransformRect = spTargetTransform.TransformBounds(targetRect);

					// Check for intersection.
					var doIntersect =
						!(pageRect.Width < 0 || targetTransformRect.Width < 0) &&
						(targetTransformRect.X <= pageRect.X + pageRect.Width) &&
						(targetTransformRect.X + targetTransformRect.Width >= pageRect.X) &&
						(targetTransformRect.Y <= pageRect.Y + pageRect.Height) &&
						(targetTransformRect.Y + targetTransformRect.Height >= pageRect.Y);

					pageIsAncestor = NavigateTransitionHelper.IsAncestor(spElementAsFrameworkElement, spTargetAsFrameworkElement);

					if (doIntersect && pageIsAncestor)
					{
						// Add to visible targets.
						visibleTargets.Add(spTarget);
					}
				}
			}
		}

		// size() never throws.
		if (visibleTargets.Count <= 0)
		{
			// If there are no targets, then this page should turnstile.
			isStaggeringEnabled = false;
		}

		if (trigger == NavigationTrigger.NavigatingTo)
		{
			if (isStaggeringEnabled)
			{
				// First add opacity change for page.
				List<DoubleKeyFrameDescription> keyFramesOpacityPage = new();

				keyFramesOpacityPage.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.NO_OPACITY));
				keyFramesOpacityPage.Add(new(NavigateTransitionHelper.TURNSTILE_MID_TIME, NavigateTransitionHelper.NO_OPACITY));
				keyFramesOpacityPage.Add(new(NavigateTransitionHelper.TURNSTILE_MID_TIME + 1, NavigateTransitionHelper.FULL_OPACITY));

				animations.Add(new(spElement, propertyNameOpacity, keyFramesOpacityPage));

				// Iterate through visible targets and create animations.
				var numVisibleTargets = 0;

				// size() never throws.
				numVisibleTargets = visibleTargets.Count;

				for (var k = 0; k < numVisibleTargets; k++)
				{
					var spEasingInner = NavigateTransitionHelper.GetExponentialEase(NavigateTransitionHelper.TURNSTILE_EASE, EasingMode.EaseOut);

					List<DoubleKeyFrameDescription> keyFramesRotation = new();
					List<DoubleKeyFrameDescription> keyFramesCenterX = new();
					List<DoubleKeyFrameDescription> keyFramesCenterZ = new();
					List<DoubleKeyFrameDescription> keyFramesOpacity = new();

					var offset = (double)k * NavigateTransitionHelper.TURNSTILE_FEATHER_OFFSET_IN;

					// Set rotation key frames.
					keyFramesRotation.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, -1 * NavigateTransitionHelper.TURNSTILE_OFFSET_IN));
					keyFramesRotation.Add(new((long)(NavigateTransitionHelper.TURNSTILE_FEATHER_SCALE_IN * NavigateTransitionHelper.TURNSTILE_MID_TIME) + (long)offset, -1 * NavigateTransitionHelper.TURNSTILE_OFFSET_IN));
					keyFramesRotation.Add(new((long)(NavigateTransitionHelper.TURNSTILE_FEATHER_SCALE_IN * NavigateTransitionHelper.TURNSTILE_END_TIME) + (long)offset, NavigateTransitionHelper.NO_OFFSET, spEasingInner));

					// Set center key frames.
					keyFramesCenterX.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.TURNSTILE_AXIS_X));
					keyFramesCenterZ.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.TURNSTILE_AXIS_Z));

					// Set opacity key frames.
					keyFramesOpacity.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.NO_OPACITY));
					keyFramesOpacity.Add(new((long)(NavigateTransitionHelper.TURNSTILE_FEATHER_SCALE_IN * NavigateTransitionHelper.TURNSTILE_MID_TIME) + (long)offset, NavigateTransitionHelper.NO_OPACITY));
					keyFramesOpacity.Add(new((long)(NavigateTransitionHelper.TURNSTILE_FEATHER_SCALE_IN * NavigateTransitionHelper.TURNSTILE_MID_TIME) + (long)offset + 1, NavigateTransitionHelper.FULL_OPACITY));

					// Clobber this, as we have to clobber values anyway.
					PlaneProjection spElementPlaneProjection = new();
					visibleTargets[k].Projection = spElementPlaneProjection;

					// Add animations
					animations.Add(new(visibleTargets[k], propertyNameRotation, keyFramesRotation));
					animations.Add(new(visibleTargets[k], propertyNameCenterX, keyFramesCenterX));
					animations.Add(new(visibleTargets[k], propertyNameCenterZ, keyFramesCenterZ));
					animations.Add(new(visibleTargets[k], propertyNameOpacity, keyFramesOpacity));
				}
			}
			else
			{
				spEasing = NavigateTransitionHelper.GetExponentialEase(NavigateTransitionHelper.TURNSTILE_EASE, EasingMode.EaseOut);

				List<DoubleKeyFrameDescription> keyFramesRotation = new();
				List<DoubleKeyFrameDescription> keyFramesCenterX = new();
				List<DoubleKeyFrameDescription> keyFramesCenterZ = new();
				List<DoubleKeyFrameDescription> keyFramesOpacity = new();

				// Set rotation key frames.
				keyFramesRotation.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, -1 * NavigateTransitionHelper.TURNSTILE_OFFSET_IN));
				keyFramesRotation.Add(new(NavigateTransitionHelper.TURNSTILE_MID_TIME, -1 * NavigateTransitionHelper.TURNSTILE_OFFSET_IN));
				keyFramesRotation.Add(new(NavigateTransitionHelper.TURNSTILE_END_TIME, NavigateTransitionHelper.NO_OFFSET, spEasing));

				// Set center key frames.
				keyFramesCenterX.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.TURNSTILE_AXIS_X));
				keyFramesCenterZ.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.TURNSTILE_AXIS_Z));

				// Set opacity key frames.
				keyFramesOpacity.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.NO_OPACITY));
				keyFramesOpacity.Add(new(NavigateTransitionHelper.TURNSTILE_MID_TIME, NavigateTransitionHelper.NO_OPACITY));
				keyFramesOpacity.Add(new(NavigateTransitionHelper.TURNSTILE_MID_TIME + 1, NavigateTransitionHelper.FULL_OPACITY));

				// Add animations
				animations.Add(new(spElement, propertyNameRotation, keyFramesRotation));
				animations.Add(new(spElement, propertyNameCenterX, keyFramesCenterX));
				animations.Add(new(spElement, propertyNameCenterZ, keyFramesCenterZ));
				animations.Add(new(spElement, propertyNameOpacity, keyFramesOpacity));
			}
		}
		else if (trigger == NavigationTrigger.NavigatingAway)
		{
			if (isStaggeringEnabled)
			{
				// First add opacity change for page.
				List<DoubleKeyFrameDescription> keyFramesOpacityPage = new();

				keyFramesOpacityPage.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.FULL_OPACITY));
				keyFramesOpacityPage.Add(new(NavigateTransitionHelper.TURNSTILE_MID_TIME, NavigateTransitionHelper.FULL_OPACITY));
				keyFramesOpacityPage.Add(new(NavigateTransitionHelper.TURNSTILE_MID_TIME + 1, NavigateTransitionHelper.NO_OPACITY));

				animations.Add(new(spElement, propertyNameOpacity, keyFramesOpacityPage));

				// Iterate through visible targets and create animations.
				var numVisibleTargets = 0;

				// size() never throws.
				numVisibleTargets = visibleTargets.Count;

				for (var k = 0; k < numVisibleTargets; k++)
				{
					var spEasingInner = NavigateTransitionHelper.GetExponentialEase(NavigateTransitionHelper.TURNSTILE_EASE, EasingMode.EaseIn);

					List<DoubleKeyFrameDescription> keyFramesRotation = new();
					List<DoubleKeyFrameDescription> keyFramesCenterX = new();
					List<DoubleKeyFrameDescription> keyFramesCenterZ = new();
					List<DoubleKeyFrameDescription> keyFramesOpacity = new();

					var offset = (double)k * NavigateTransitionHelper.TURNSTILE_FEATHER_OFFSET_OUT;

					// Set rotation key frames.
					keyFramesRotation.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME + (long)offset, NavigateTransitionHelper.NO_OFFSET));
					// WinUI passes the unassigned spEasing (not spEasingInner), so this key frame is linear.
					keyFramesRotation.Add(new((long)(NavigateTransitionHelper.TURNSTILE_FEATHER_SCALE_OUT * NavigateTransitionHelper.TURNSTILE_MID_TIME) + (long)offset, NavigateTransitionHelper.TURNSTILE_OFFSET_OUT, spEasing));

					// Set center key frames.
					keyFramesCenterX.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.TURNSTILE_AXIS_X));
					keyFramesCenterZ.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.TURNSTILE_AXIS_Z));

					// Set opacity key frames.
					keyFramesOpacity.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME + (long)offset, NavigateTransitionHelper.FULL_OPACITY));
					keyFramesOpacity.Add(new((long)(NavigateTransitionHelper.TURNSTILE_FEATHER_SCALE_OUT * NavigateTransitionHelper.TURNSTILE_MID_TIME) + (long)offset, NavigateTransitionHelper.FULL_OPACITY));
					keyFramesOpacity.Add(new((long)(NavigateTransitionHelper.TURNSTILE_FEATHER_SCALE_OUT * NavigateTransitionHelper.TURNSTILE_MID_TIME) + (long)offset + 1, NavigateTransitionHelper.NO_OPACITY));

					// Clobber this, as we have to clobber values anyway.
					PlaneProjection spElementPlaneProjection = new();
					visibleTargets[k].Projection = spElementPlaneProjection;

					// Add animations
					animations.Add(new(visibleTargets[k], propertyNameRotation, keyFramesRotation));
					animations.Add(new(visibleTargets[k], propertyNameCenterX, keyFramesCenterX));
					animations.Add(new(visibleTargets[k], propertyNameCenterZ, keyFramesCenterZ));
					animations.Add(new(visibleTargets[k], propertyNameOpacity, keyFramesOpacity));
				}
			}
			else
			{
				spEasing = NavigateTransitionHelper.GetExponentialEase(NavigateTransitionHelper.TURNSTILE_EASE, EasingMode.EaseIn);

				List<DoubleKeyFrameDescription> keyFramesRotation = new();
				List<DoubleKeyFrameDescription> keyFramesCenterX = new();
				List<DoubleKeyFrameDescription> keyFramesCenterZ = new();
				List<DoubleKeyFrameDescription> keyFramesOpacity = new();

				// Set rotation key frames.
				keyFramesRotation.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.NO_OFFSET));
				keyFramesRotation.Add(new(NavigateTransitionHelper.TURNSTILE_MID_TIME, NavigateTransitionHelper.TURNSTILE_OFFSET_OUT, spEasing));

				// Set center key frames.
				keyFramesCenterX.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.TURNSTILE_AXIS_X));
				keyFramesCenterZ.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.TURNSTILE_AXIS_Z));

				// Set opacity key frames.
				keyFramesOpacity.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.FULL_OPACITY));
				keyFramesOpacity.Add(new(NavigateTransitionHelper.TURNSTILE_MID_TIME, NavigateTransitionHelper.FULL_OPACITY));
				keyFramesOpacity.Add(new(NavigateTransitionHelper.TURNSTILE_MID_TIME + 1, NavigateTransitionHelper.NO_OPACITY));

				// Add animations
				animations.Add(new(spElement, propertyNameRotation, keyFramesRotation));
				animations.Add(new(spElement, propertyNameCenterX, keyFramesCenterX));
				animations.Add(new(spElement, propertyNameCenterZ, keyFramesCenterZ));
				animations.Add(new(spElement, propertyNameOpacity, keyFramesOpacity));
			}
		}
		else if (trigger == NavigationTrigger.BackNavigatingTo)
		{
			if (isStaggeringEnabled)
			{
				// First add opacity change for page.
				List<DoubleKeyFrameDescription> keyFramesOpacityPage = new();

				keyFramesOpacityPage.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.NO_OPACITY));
				keyFramesOpacityPage.Add(new(NavigateTransitionHelper.TURNSTILE_MID_TIME, NavigateTransitionHelper.NO_OPACITY));
				keyFramesOpacityPage.Add(new(NavigateTransitionHelper.TURNSTILE_MID_TIME + 1, NavigateTransitionHelper.FULL_OPACITY));

				animations.Add(new(spElement, propertyNameOpacity, keyFramesOpacityPage));

				// Iterate through visible targets and create animations.
				var numVisibleTargets = 0;

				// size() never throws.
				numVisibleTargets = visibleTargets.Count;

				for (var k = 0; k < numVisibleTargets; k++)
				{
					var spEasingInner = NavigateTransitionHelper.GetExponentialEase(NavigateTransitionHelper.TURNSTILE_EASE, EasingMode.EaseOut);

					List<DoubleKeyFrameDescription> keyFramesRotation = new();
					List<DoubleKeyFrameDescription> keyFramesCenterX = new();
					List<DoubleKeyFrameDescription> keyFramesCenterZ = new();
					List<DoubleKeyFrameDescription> keyFramesOpacity = new();

					var offset = (double)k * NavigateTransitionHelper.TURNSTILE_FEATHER_OFFSET_IN;

					// Set rotation key frames.
					keyFramesRotation.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.TURNSTILE_OFFSET_OUT));
					keyFramesRotation.Add(new((long)(NavigateTransitionHelper.TURNSTILE_FEATHER_SCALE_IN * NavigateTransitionHelper.TURNSTILE_MID_TIME) + (long)offset, NavigateTransitionHelper.TURNSTILE_OFFSET_OUT));
					// WinUI passes the unassigned spEasing (not spEasingInner), so this key frame is linear.
					keyFramesRotation.Add(new((long)(NavigateTransitionHelper.TURNSTILE_FEATHER_SCALE_IN * NavigateTransitionHelper.TURNSTILE_END_TIME) + (long)offset, NavigateTransitionHelper.NO_OFFSET, spEasing));

					// Set center key frames.
					keyFramesCenterX.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.TURNSTILE_AXIS_X));
					keyFramesCenterZ.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.TURNSTILE_AXIS_Z));

					// Set opacity key frames.
					keyFramesOpacity.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.NO_OPACITY));
					keyFramesOpacity.Add(new((long)(NavigateTransitionHelper.TURNSTILE_FEATHER_SCALE_IN * NavigateTransitionHelper.TURNSTILE_MID_TIME) + (long)offset, NavigateTransitionHelper.NO_OPACITY));
					keyFramesOpacity.Add(new((long)(NavigateTransitionHelper.TURNSTILE_FEATHER_SCALE_IN * NavigateTransitionHelper.TURNSTILE_MID_TIME) + (long)offset + 1, NavigateTransitionHelper.FULL_OPACITY));

					// Clobber this, as we have to clobber values anyway.
					PlaneProjection spElementPlaneProjection = new();
					visibleTargets[k].Projection = spElementPlaneProjection;

					// Add animations
					animations.Add(new(visibleTargets[k], propertyNameRotation, keyFramesRotation));
					animations.Add(new(visibleTargets[k], propertyNameCenterX, keyFramesCenterX));
					animations.Add(new(visibleTargets[k], propertyNameCenterZ, keyFramesCenterZ));
					animations.Add(new(visibleTargets[k], propertyNameOpacity, keyFramesOpacity));
				}
			}
			else
			{
				spEasing = NavigateTransitionHelper.GetExponentialEase(NavigateTransitionHelper.TURNSTILE_EASE, EasingMode.EaseOut);

				List<DoubleKeyFrameDescription> keyFramesRotation = new();
				List<DoubleKeyFrameDescription> keyFramesCenterX = new();
				List<DoubleKeyFrameDescription> keyFramesCenterZ = new();
				List<DoubleKeyFrameDescription> keyFramesOpacity = new();

				// Set rotation key frames.
				keyFramesRotation.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.TURNSTILE_OFFSET_OUT));
				keyFramesRotation.Add(new(NavigateTransitionHelper.TURNSTILE_MID_TIME, NavigateTransitionHelper.TURNSTILE_OFFSET_OUT));
				keyFramesRotation.Add(new(NavigateTransitionHelper.TURNSTILE_END_TIME, NavigateTransitionHelper.NO_OFFSET, spEasing));

				// Set center key frames.
				keyFramesCenterX.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.TURNSTILE_AXIS_X));
				keyFramesCenterZ.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.TURNSTILE_AXIS_Z));

				// Set opacity key frames.
				keyFramesOpacity.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.NO_OPACITY));
				keyFramesOpacity.Add(new(NavigateTransitionHelper.TURNSTILE_MID_TIME, NavigateTransitionHelper.NO_OPACITY));
				keyFramesOpacity.Add(new(NavigateTransitionHelper.TURNSTILE_MID_TIME + 1, NavigateTransitionHelper.FULL_OPACITY));

				// Add animations
				animations.Add(new(spElement, propertyNameRotation, keyFramesRotation));
				animations.Add(new(spElement, propertyNameCenterX, keyFramesCenterX));
				animations.Add(new(spElement, propertyNameCenterZ, keyFramesCenterZ));
				animations.Add(new(spElement, propertyNameOpacity, keyFramesOpacity));
			}
		}
		else if (trigger == NavigationTrigger.BackNavigatingAway)
		{
			if (isStaggeringEnabled)
			{
				// First add opacity change for page.
				List<DoubleKeyFrameDescription> keyFramesOpacityPage = new();

				keyFramesOpacityPage.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.FULL_OPACITY));
				keyFramesOpacityPage.Add(new(NavigateTransitionHelper.TURNSTILE_MID_TIME, NavigateTransitionHelper.FULL_OPACITY));
				keyFramesOpacityPage.Add(new(NavigateTransitionHelper.TURNSTILE_MID_TIME + 1, NavigateTransitionHelper.NO_OPACITY));

				animations.Add(new(spElement, propertyNameOpacity, keyFramesOpacityPage));

				// Iterate through visible targets and create animations.
				var numVisibleTargets = 0;

				// size() never throws.
				numVisibleTargets = visibleTargets.Count;

				for (var k = 0; k < numVisibleTargets; k++)
				{
					var spEasingInner = NavigateTransitionHelper.GetExponentialEase(NavigateTransitionHelper.TURNSTILE_EASE, EasingMode.EaseIn);

					List<DoubleKeyFrameDescription> keyFramesRotation = new();
					List<DoubleKeyFrameDescription> keyFramesCenterX = new();
					List<DoubleKeyFrameDescription> keyFramesCenterZ = new();
					List<DoubleKeyFrameDescription> keyFramesOpacity = new();

					var offset = (double)k * NavigateTransitionHelper.TURNSTILE_FEATHER_OFFSET_OUT;

					// Set rotation key frames.
					keyFramesRotation.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME + (long)offset, NavigateTransitionHelper.NO_OFFSET));
					keyFramesRotation.Add(new((long)(NavigateTransitionHelper.TURNSTILE_FEATHER_SCALE_OUT * NavigateTransitionHelper.TURNSTILE_MID_TIME) + (long)offset, -1 * NavigateTransitionHelper.TURNSTILE_OFFSET_IN, spEasingInner));

					// Set center key frames.
					keyFramesCenterX.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.TURNSTILE_AXIS_X));
					keyFramesCenterZ.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.TURNSTILE_AXIS_Z));

					// Set opacity key frames.
					keyFramesOpacity.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME + (long)offset, NavigateTransitionHelper.FULL_OPACITY));
					keyFramesOpacity.Add(new((long)(NavigateTransitionHelper.TURNSTILE_FEATHER_SCALE_OUT * NavigateTransitionHelper.TURNSTILE_MID_TIME) + (long)offset, NavigateTransitionHelper.FULL_OPACITY));
					keyFramesOpacity.Add(new((long)(NavigateTransitionHelper.TURNSTILE_FEATHER_SCALE_OUT * NavigateTransitionHelper.TURNSTILE_MID_TIME) + (long)offset + 1, NavigateTransitionHelper.NO_OPACITY));

					// Clobber this, as we have to clobber values anyway.
					PlaneProjection spElementPlaneProjection = new();
					visibleTargets[k].Projection = spElementPlaneProjection;

					// Add animations
					animations.Add(new(visibleTargets[k], propertyNameRotation, keyFramesRotation));
					animations.Add(new(visibleTargets[k], propertyNameCenterX, keyFramesCenterX));
					animations.Add(new(visibleTargets[k], propertyNameCenterZ, keyFramesCenterZ));
					animations.Add(new(visibleTargets[k], propertyNameOpacity, keyFramesOpacity));
				}
			}
			else
			{
				spEasing = NavigateTransitionHelper.GetExponentialEase(NavigateTransitionHelper.TURNSTILE_EASE, EasingMode.EaseIn);

				List<DoubleKeyFrameDescription> keyFramesRotation = new();
				List<DoubleKeyFrameDescription> keyFramesCenterX = new();
				List<DoubleKeyFrameDescription> keyFramesCenterZ = new();
				List<DoubleKeyFrameDescription> keyFramesOpacity = new();

				// Set rotation key frames.
				keyFramesRotation.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.NO_OFFSET));
				keyFramesRotation.Add(new(NavigateTransitionHelper.TURNSTILE_MID_TIME, -1 * NavigateTransitionHelper.TURNSTILE_OFFSET_IN, spEasing));

				// Set center key frames.
				keyFramesCenterX.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.TURNSTILE_AXIS_X));
				keyFramesCenterZ.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.TURNSTILE_AXIS_Z));

				// Set opacity key frames.
				keyFramesOpacity.Add(new(NavigateTransitionHelper.TURNSTILE_START_TIME, NavigateTransitionHelper.FULL_OPACITY));
				keyFramesOpacity.Add(new(NavigateTransitionHelper.TURNSTILE_MID_TIME, NavigateTransitionHelper.FULL_OPACITY));
				keyFramesOpacity.Add(new(NavigateTransitionHelper.TURNSTILE_MID_TIME + 1, NavigateTransitionHelper.NO_OPACITY));

				// Add animations
				animations.Add(new(spElement, propertyNameRotation, keyFramesRotation));
				animations.Add(new(spElement, propertyNameCenterX, keyFramesCenterX));
				animations.Add(new(spElement, propertyNameCenterZ, keyFramesCenterZ));
				animations.Add(new(spElement, propertyNameOpacity, keyFramesOpacity));
			}
		}

		if (!isStaggeringEnabled)
		{
			// UNO TODO: WinUI attaches a PlaneProjection to the page's LayoutTransitionElement and redirects the
			// projection animations to it. Uno has no LayoutTransitionElement, so the page gets its own PlaneProjection,
			// replacing any projection the app set on it.
			spElement.Projection = new PlaneProjection();
		}

		// Create storyboard.
		spStoryboard = NavigateTransitionHelper.CreateStoryboardForNavigationAnimations(animations);

		// Ensure sure that the animated pages are not hit test visible.
		{
			List<ObjectKeyFrameDescription> keyFramesIsHitTestVisible = new();

			// Set isHitTestVisible key frames.
			keyFramesIsHitTestVisible.Add(new(NavigateTransitionHelper.TIME_ZERO, false));

			// Add object animations to storyboard.
			var spAnimation = NavigateTransitionHelper.GetObjectAnimation(new(spElement, propertyNameIsHitTestVisible, keyFramesIsHitTestVisible));

			NavigateTransitionHelper.AddTimelineToStoryboard(spAnimation, spStoryboard);
		}

		return new List<Storyboard> { spStoryboard };
	}

	/// <summary>
	/// Identifies the IsStaggeringEnabled dependency property.
	/// </summary>
	public static DependencyProperty IsStaggeringEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(IsStaggeringEnabled),
			typeof(bool),
			typeof(CommonNavigationTransitionInfo),
			new FrameworkPropertyMetadata(BoolBoxes.False, OnPropertyChanged));

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
		new FrameworkPropertyMetadata(BoolBoxes.False, OnPropertyChanged));

	private static void OnPropertyChanged(DependencyObject pSender, DependencyPropertyChangedEventArgs pArgs)
	{
		var spDO = pSender;
		var spProperty = pArgs.Property;

		if (spProperty == IsStaggeringEnabledProperty)
		{
			// This is just a DP: bubble up to DO.
			((CommonNavigationTransitionInfo)pSender).OnPropertyChanged(pArgs);
		}
		else if (spProperty == IsStaggerElementProperty)
		{
			// This is an attached property, bubble up to page.
			var isStaggerElement = (bool)pArgs.NewValue;

			lock (s_spStaggerElements)
			{
				// Erase the value in the vector.
				for (var it = 0; it < s_spStaggerElements.Count; ++it)
				{
					s_spStaggerElements[it].TryGetTarget(out var spItAsDO);

					if (spItAsDO == spDO)
					{
						s_spStaggerElements.RemoveAt(it);
						break;
					}
				}

				if (isStaggerElement)
				{
					s_spStaggerElements.Add(new WeakReference<DependencyObject>(spDO));
				}
			}
		}
	}

	/// <summary>
	/// Gets the value of the IsStaggerElement attached property for a specified element.
	/// </summary>
	public static bool GetIsStaggerElement(UIElement element) => (bool)element.GetValue(IsStaggerElementProperty);

	/// <summary>
	/// Sets the value of the IsStaggerElement attached property for a specified element.
	/// </summary>
	public static void SetIsStaggerElement(UIElement element, bool value) => element.SetValue(IsStaggerElementProperty, value);

	private static List<WeakReference<DependencyObject>> GetStaggerElements()
	{
		lock (s_spStaggerElements)
		{
			return new(s_spStaggerElements);
		}
	}

	internal static void ClearStaggerElements()
	{
		lock (s_spStaggerElements)
		{
			s_spStaggerElements.RemoveAll(it => !it.TryGetTarget(out _));
		}
	}
}
