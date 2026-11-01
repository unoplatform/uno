// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ColorAnimation.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class ColorAnimation
{
	private static uint ExtractChannel(uint Color, int ChannelOffset) => (Color >> ChannelOffset) & 0xFF;

	private static byte ComputeColorMultiply(int FromColor, int ToColor, int MultiplicationFactor)
		=> ByteSaturate(FromColor + (((ToColor - FromColor) * MultiplicationFactor) / 256));

	private static uint ComputeChannelColor(int FromColor, int ToColor, int MultiplicationFactor, int ChannelOffset)
		=> ((uint)ComputeColorMultiply(FromColor, ToColor, MultiplicationFactor)) << ChannelOffset;

	public ColorAnimation()
	{
	}

	// Generic interpolation method - this MUST be overridden by derived types
	// to provide a way to blend to values of the given animation type
	private protected override void InterpolateCurrentValue(float rPercentEnd)
		=> m_vCurrentValue = InterpolateCurrentValue(m_vFrom, m_vTo, rPercentEnd);

	// TODO Uno specific: static so ColorAnimationUsingKeyFrames (a CColorAnimation in WinUI) can share it.
	internal static uint InterpolateCurrentValue(uint m_vFrom, uint m_vTo, float rPercentEnd)
	{
		// Easing functions can give an unnormalized value outside of the range 0 to 1 so
		// don't validate that rPercentEnd is from 0 to 1 but handle the cases where is is.
		if (rPercentEnd < 0.0f || rPercentEnd > 1.0f)
		{
			// Get the to and from values, if the percentage is negative swap the to and from values.
			var FromColor = (rPercentEnd >= 0.0f) ? m_vFrom : m_vTo;
			var ToColor = (rPercentEnd >= 0.0f) ? m_vTo : m_vFrom;

			// Handle if this is a negative percentage.
			if (rPercentEnd < 0.0f)
			{
				// The to and from have already been flipped so change the percentage to match.
				rPercentEnd = 1.0f - rPercentEnd;
			}

			// Compute interpolation
			var uBlend = (int)XcpRound(rPercentEnd * 256.0f);

			// 32bit - ARGB
			const int BlueShift = 0x00;
			const int GreenShift = 0x08;
			const int RedShift = 0x10;
			const int AlphaShift = 0x18;

			// Extra color channels from source
			var AlphaFrom = (int)ExtractChannel(FromColor, AlphaShift);
			var RedFrom = (int)ExtractChannel(FromColor, RedShift);
			var GreenFrom = (int)ExtractChannel(FromColor, GreenShift);
			var BlueFrom = (int)ExtractChannel(FromColor, BlueShift);

			var AlphaTo = (int)ExtractChannel(ToColor, AlphaShift);
			var RedTo = (int)ExtractChannel(ToColor, RedShift);
			var GreenTo = (int)ExtractChannel(ToColor, GreenShift);
			var BlueTo = (int)ExtractChannel(ToColor, BlueShift);

			var FinalAlpha = ComputeChannelColor(AlphaFrom, AlphaTo, uBlend, AlphaShift);
			var FinalRed = ComputeChannelColor(RedFrom, RedTo, uBlend, RedShift);
			var FinalGreen = ComputeChannelColor(GreenFrom, GreenTo, uBlend, GreenShift);
			var FinalBlue = ComputeChannelColor(BlueFrom, BlueTo, uBlend, BlueShift);

			// Compute color
			var rgbResult = (FinalAlpha | FinalRed | FinalGreen | FinalBlue);

			// Set value into our Dependency object
			return rgbResult;
		}
		else
		{
			var FromColor = m_vFrom;
			var ToColor = m_vTo;

			// Compute interpolation
			var uBlend = (uint)XcpRound(rPercentEnd * 256.0f);
			var uInvBlend = 256 - uBlend;

			// Convert to our fast blending format
			var uColorFrom00aa00gg = ((FromColor) >> 8) & 0x00ff00ff;
			var uColorFrom00rr00bb = (FromColor) & 0x00ff00ff;
			var uColorTo00aa00gg = ((ToColor) >> 8) & 0x00ff00ff;
			var uColorTo00rr00bb = (ToColor) & 0x00ff00ff;

			// Blend
			var uBlendedToColoraa00gg00 = unchecked((uColorTo00aa00gg * uBlend) & 0xff00ff00);
			var uBlendedToColor00rr00bb = unchecked(((uColorTo00rr00bb * uBlend) >> 8) & 0x00ff00ff);
			var uBlendedFromColoraa00gg00 = unchecked((uColorFrom00aa00gg * uInvBlend) & 0xff00ff00);
			var uBlendedFromColor00rr00bb = unchecked(((uColorFrom00rr00bb * uInvBlend) >> 8) & 0x00ff00ff);

			// Compute color
			var rgbResult = unchecked((uBlendedFromColoraa00gg00 + uBlendedToColoraa00gg00)
				| (uBlendedFromColor00rr00bb + uBlendedToColor00rr00bb));

			// Set value into our Dependency object
			return rgbResult;
		}
	}

	// Computes the m_pTo field from the m_pBy and m_pFrom fields using a type-specific addition
	private protected override void ComputeToUsingFromAndBy() => m_vTo = ComputeToUsingFromAndBy(m_vFrom, m_vBy);

	internal static uint ComputeToUsingFromAndBy(uint m_vFrom, uint m_vBy)
	{
		// Compute type-specific add
		// For Color do a clamped component-wise add

		// Split "From" value into channels
		var uAFrom = (m_vFrom >> 24) & 0x000000ff;
		var uRFrom = (m_vFrom >> 16) & 0x000000ff;
		var uGFrom = (m_vFrom >> 8) & 0x000000ff;
		var uBFrom = (m_vFrom >> 0) & 0x000000ff;

		// Add "By" channel values
		uAFrom += (m_vBy >> 24) & 0x000000ff;
		uRFrom += (m_vBy >> 16) & 0x000000ff;
		uGFrom += (m_vBy >> 8) & 0x000000ff;
		uBFrom += (m_vBy >> 0) & 0x000000ff;

		// Clamp channel values at 0xff
		uAFrom = Math.Min(uAFrom, 0x000000ff);
		uRFrom = Math.Min(uRFrom, 0x000000ff);
		uGFrom = Math.Min(uGFrom, 0x000000ff);
		uBFrom = Math.Min(uBFrom, 0x000000ff);

		// Reconstruct Color
		return (uAFrom << 24) + (uRFrom << 16) + (uGFrom << 8) + uBFrom;
	}

	// Initializes the base value of the animation from either the animation target or from a handoff.
	private protected override void GetAnimationBaseValue()
	{
		// we should have a reference, but if we don't have an actual target, keep current value
		var targetObject = GetResolvedTargetObject();

		if (targetObject != null)
		{
			if (targetObject.GetValue(m_pTargetDependencyProperty!) is Color rawValue)
			{
				m_vBaseValue = ToArgb(rawValue);
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

	// TODO Uno specific: static so ColorAnimationUsingKeyFrames (a CColorAnimation in WinUI) can share it.
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
		// NOTE: Independent color animations are not supported for TextBox/PasswordBox/RichEditBox.
		//           They are supported for TextBlock, RichTextBlock, Glyphs, and other UIElements (Shape, Border, Panel, etc).
		// TODO Uno: TextBoxView is not a UIElement in Uno, so the TextBoxView exclusion does not apply.
		if (pTargetObject is UIElement)
		{
			var pElement = (UIElement)pTargetObject;

			//
			// Approved list of property targets for independent animation
			//
			// Note: This doesn't distinguish between stroke & fill or between border & background.
			// If one of them is animated, then the other will be cloned as well. The clone just won't
			// be animated on the render thread. If this is a problem, we can split them up.
			//

			// Brush properties.
			if (targetDependencyProperty == SolidColorBrush.ColorProperty)
			{
				// Brush color animations are independent.
				global::System.Diagnostics.Debug.Assert(pSender is SolidColorBrush);
				isIndependentAnimation = true;
			}
			else
			{
				// Not independent.
				global::System.Diagnostics.Debug.Assert(!isIndependentAnimation);
			}

			if (isIndependentAnimation)
			{
				GetForegroundBrush(pElement, out var hasForegroundProperty, out var pForegroundBrush);

				// The Foreground property is inherited. If the animating brush is set as the Foreground, the animation
				// needs to be inherited as well. It also might end up being dependent if a descendent is cached.
				if (hasForegroundProperty && pForegroundBrush == pSender)
				{
					// FindInheritedIATargets will add pElement as a target as well.
					FindInheritedIndependentAnimationTargets(
						pElement,
						pForegroundBrush!,
						pIATargets);
				}
				// If this ColorAnimation was on a property other than the Foreground, just add pElement to the targets.
				else if (pIATargets != null)
				{
					pIATargets.Add(pElement);
				}
			}
		}
		// If we haven't reached a UIElement, keep walking up.
		else if (pTargetObject is SolidColorBrush)
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

	// Assumes the animated brush is originally set as an inherited property (Foreground).
	// Walks through the subgraph to verify no element inheriting this property is cached,
	// and that all all elements inherited the property are marked as animation targets as
	// well.
	private static void FindInheritedIndependentAnimationTargets(
		UIElement pElement,
		Brush pAnimatedBrush,
		List<DependencyObject>? pIATargets)
	{
		var walkChildren = true;

		// Check if this element supports the Foreground property.
		GetForegroundBrush(pElement, out var hasForegroundProperty, out var pForegroundBrush);

		if (hasForegroundProperty)
		{
			// If the Foreground is the animated brush, then the animation is inherited here.
			if (pForegroundBrush == pAnimatedBrush)
			{
				// Mark the element as animated.
				pIATargets?.Add(pElement);
			}
			// Otherwise, the element is not inheriting the Foreground animation since it has a different Foreground
			// brush set. The breadth walk should stop, since the animation will not inherit any further in this branch.
			else
			{
				walkChildren = false;
			}
		}

		// Walk through the children looking for more elements that might inherit this animation.
		if (walkChildren)
		{
			foreach (var pChild in GetChildren(pElement))
			{
				FindInheritedIndependentAnimationTargets(pChild, pAnimatedBrush, pIATargets);
			}
		}
	}
}
