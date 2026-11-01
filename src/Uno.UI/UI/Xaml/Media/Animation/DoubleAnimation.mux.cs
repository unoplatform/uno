// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference DoubleAnimation.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class DoubleAnimation
{
	public DoubleAnimation()
	{
	}

	// Generic interpolation method - this MUST be overridden by derived types
	// to provide a way to blend to values of the given animation type
	private protected override void InterpolateCurrentValue(float rPercentEnd)
	{
		// Now that we have EasingFunctions we can go above 1.0 and below 0.0.
		// Easing functions can give an unnormalized value outside of the range 0 to 1 so
		// don't validate that rPercentEnd is from 0 to 1 but handle the cases where is is.
		var rPercentStart = 1.0f - rPercentEnd;
		var rResult = m_vFrom * rPercentStart + rPercentEnd * m_vTo;
		m_vCurrentValue = rResult;
	}

	// Computes the m_pTo field from the m_pBy and m_pFrom fields using a type-specific addition;
	private protected override void ComputeToUsingFromAndBy()
	{
		// Compute type-specific To value
		m_vTo = m_vFrom + m_vBy;
	}

	// Initializes the base value of the animation from either the animation target or from a handoff.
	private protected override void GetAnimationBaseValue()
	{
		// we should have a reference, but if we don't have an actual target, keep current value
		global::System.Diagnostics.Debug.Assert(m_targetObjectWeakRef is not null);
		var targetObject = GetResolvedTargetObject();

		if (targetObject != null)
		{
			var rawValue = targetObject.GetValue(m_pTargetDependencyProperty!);

			if (rawValue is float floatValue)
			{
				m_vBaseValue = floatValue;
			}

			if (rawValue is double doubleValue)
			{
				m_vBaseValue = (float)doubleValue;
			}
		}
	}

	// Find the element(s) whose properties are targeted by this animation.
	// Returns whether or not the animation is independent.
	private protected override bool FindIndependentAnimationTargetsRecursive(
		DependencyObject pTargetObject,
		DependencyObject? pSender,
		List<DependencyObject>? pIATargets)
		=> FindIndependentAnimationTargetsRecursive(this, m_pTargetDependencyProperty!, pTargetObject, pSender, pIATargets);

	// TODO Uno specific: static so DoubleAnimationUsingKeyFrames (a CDoubleAnimation in WinUI) can share it.
	internal static bool FindIndependentAnimationTargetsRecursive(
		Timeline animation,
		DependencyProperty targetDependencyProperty,
		DependencyObject pTargetObject,
		DependencyObject? pSender,
		List<DependencyObject>? pIATargets)
	{
		// Assume the animation is not independent until we find a target UIElement.
		var isIndependentAnimation = false;

		// If we reached a target UIElement, notify it of the animation.
		var isTargetUIElement = pTargetObject is UIElement;
		if (isTargetUIElement)
		{
			var pElement = (UIElement)pTargetObject;

			//
			// Approved list of property targets for independent animation
			//
			var property = targetDependencyProperty;

			// UIElement properties.
			if (property == Canvas.LeftProperty || property == Canvas.TopProperty)
			{
				global::System.Diagnostics.Debug.Assert(pSender == null);
				var pParent = pElement.GetParent();

				// Animate Canvas.Left/Top only if the parent is a canvas.
				if (pParent is Canvas)
				{
					isIndependentAnimation = true;
				}
			}
			else if (property == UIElement.OpacityProperty)
			{
				global::System.Diagnostics.Debug.Assert(pSender == null);
				isIndependentAnimation = true;
			}
			// Transform properties.
			else if (property == RotateTransform.CenterXProperty
				|| property == RotateTransform.CenterYProperty
				|| property == RotateTransform.AngleProperty
				|| property == ScaleTransform.CenterYProperty
				|| property == ScaleTransform.ScaleXProperty
				|| property == ScaleTransform.ScaleYProperty
				|| property == ScaleTransform.CenterXProperty
				|| property == SkewTransform.CenterXProperty
				|| property == SkewTransform.CenterYProperty
				|| property == SkewTransform.AngleXProperty
				|| property == SkewTransform.AngleYProperty
				|| property == TranslateTransform.XProperty
				|| property == TranslateTransform.YProperty
				|| property == CompositeTransform.CenterXProperty
				|| property == CompositeTransform.CenterYProperty
				|| property == CompositeTransform.ScaleXProperty
				|| property == CompositeTransform.ScaleYProperty
				|| property == CompositeTransform.SkewXProperty
				|| property == CompositeTransform.SkewYProperty
				|| property == CompositeTransform.RotationProperty
				|| property == CompositeTransform.TranslateXProperty
				|| property == CompositeTransform.TranslateYProperty)
			{
				// Render transform animations are independent.
				if (pSender is Transform)
				{
					isIndependentAnimation = true;
				}
				// Clip transform animations are independent.
				else if (pSender is RectangleGeometry)
				{
					isIndependentAnimation = true;
				}

				// TODO Uno: TransitionTarget is not ported.
			}
			// PlaneProjection properties
			else if (property == PlaneProjection.RotationYProperty
				|| property == PlaneProjection.RotationZProperty
				|| property == PlaneProjection.RotationXProperty
				|| property == PlaneProjection.CenterOfRotationXProperty
				|| property == PlaneProjection.CenterOfRotationYProperty
				|| property == PlaneProjection.CenterOfRotationZProperty
				|| property == PlaneProjection.LocalOffsetXProperty
				|| property == PlaneProjection.LocalOffsetYProperty
				|| property == PlaneProjection.LocalOffsetZProperty
				|| property == PlaneProjection.GlobalOffsetXProperty
				|| property == PlaneProjection.GlobalOffsetYProperty
				|| property == PlaneProjection.GlobalOffsetZProperty)
			{
				global::System.Diagnostics.Debug.Assert(pSender is PlaneProjection);
				isIndependentAnimation = true;
			}
			// TODO Uno: CompositeTransform3D and TransitionTarget properties: those types are not implemented.
			else
			{
				// Not independent.
				global::System.Diagnostics.Debug.Assert(!isIndependentAnimation);
			}

			// If this UIElement is a valid IA target, add it to the list for this animation.
			if (isIndependentAnimation && pIATargets != null)
			{
				pIATargets.Add(pElement);
			}
		}
		// If we haven't reached a UIElement, keep walking up.
		// TODO Uno: TransformCollection is not a DependencyObject in Uno; a transform in a TransformGroup has the group as parent.
		else if (pTargetObject is Transform
			|| pTargetObject is Projection
			|| pTargetObject is RectangleGeometry)
		{
			animation.CheckElementUsingAnimatedTarget(
				pTargetObject,
				pIATargets,
				ref isIndependentAnimation);
		}
		// Since a shareable resource being animated might be defined in a resource dictionary,
		// do not disable the animation because of it.  The animation will only be independent
		// if it's hooked up to a non-HW cached UIElement elsewhere, however.
		else if (pTargetObject is ResourceDictionary)
		{
			isIndependentAnimation = true;
		}

		return isIndependentAnimation;
	}
}
