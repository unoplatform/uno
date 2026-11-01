// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference animation.h, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Media.Animation;

// TODO Uno specific: WinUI's CAnimation sits between CTimeline and the concrete animations. The public animations derive
// from Timeline directly (a public class can't derive from an internal one), so CAnimation is folded into Timeline and
// its members only apply when IsAnimation() is true. The CAnimation overrides of CTimeline virtuals are named
// Animation<Name> and dispatched from the CTimeline virtuals.
partial class Timeline
{
	private protected const float NULL_DURATION_DEFAULT = 1.0f;

	internal enum AnimationValueOperation
	{
		AnimationValueSet,
		AnimationValueClear,
	}

#if !HAS_UNO
	// TODO Uno: IsAnimationValueNull: Uno's From/To/By are nullable properties, a null value means "not set".
#endif

#if HAS_UNO
	[ThreadStatic]
	private static List<DependencyObject>? t_iaTargets;
#endif

	private bool IsIndependentAnimation(DependencyObject pTargetObject)
		=> FindIndependentAnimationTargets(pTargetObject, null);

	private protected enum AssignmentOperand
	{
		From,
		To,
		By,
		BaseValue,
		NonAnimatedBaseValue,
		CurrentValue,
	}

	private protected virtual void InterpolateCurrentValue(float rPercentEnd)
	{
	}

	private protected virtual void PostInterpolateValues()
	{
	}

	private protected virtual void ComputeToUsingFromAndBy()
	{
	}

	private protected virtual void ValueAssign(AssignmentOperand destOperand, AssignmentOperand sourceOperand)
	{
	}

	// ValueAssign(_Out_ CValue& destOperand, AssignmentOperand sourceOperand)
	private protected virtual object? ValueAssign(AssignmentOperand sourceOperand) => null;

	private protected virtual void ValueAssign(AssignmentOperand destOperand, object? sourceOperand)
	{
	}

	private protected virtual void GetAnimationBaseValue()
	{
	}

#if HAS_UNO
	// TODO Uno specific: These replace m_pKeyFrames, m_pEasingFunction, m_pDPTo/m_pDPFrom/m_pDPBy (and their
	// IsPropertyDefault state), m_enableDependentAnimation and m_fUsesKeyFrames, which the concrete animations provide.

	private protected virtual IEnumerable<IKeyFrame>? KeyFrames_Core => null;

	private protected virtual EasingFunctionBase? m_pEasingFunction => null;

	private protected virtual bool IsFromDefault => true;

	private protected virtual bool IsToDefault => true;

	private protected virtual bool IsByDefault => true;

	private protected virtual bool m_enableDependentAnimation => false;

	private protected virtual bool m_fUsesKeyFrames => false;

	/// <summary>The type of property an animation of this type can target, or null when any type goes.</summary>
	private protected virtual global::System.Type? AnimatedPropertyType => null;

	private KeyFrameCollection? m_pKeyFrames;
#endif

	private bool m_fFiredDependentAnimationWarning;
	private bool m_hasControlOfTarget = true;    // Whether ticking this animation will affect the value of the object+target
	private bool m_hasHandoff;
	private protected bool m_isToAnimation;
	private bool m_isZeroDuration;
}
