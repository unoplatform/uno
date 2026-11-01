// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ObjectAnimationUsingKeyFrames.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class ObjectAnimationUsingKeyFrames
{
	public ObjectAnimationUsingKeyFrames()
	{
		// Create internal members
		KeyFrames = new ObjectKeyFrameCollection(owner: this, isAutoPropertyInheritanceEnabled: false);

		// Set Defaults
		m_vFrom = null;
		m_vTo = null;
		m_vBaseValue = null;
		m_vNonAnimatedBaseValue = null;
		m_vCurrentValue = null;
	}

	private protected override void ValueAssign(AssignmentOperand destOperand, AssignmentOperand sourceOperand)
	{
		global::System.Diagnostics.Debug.Assert(sourceOperand != destOperand);

		GetInternalOperand(destOperand) = CopyConverted(GetInternalOperand(sourceOperand));
	}

	private protected override object? ValueAssign(AssignmentOperand sourceOperand)
		=> CopyConverted(GetInternalOperand(sourceOperand));

	private protected override void ValueAssign(AssignmentOperand destOperand, object? sourceOperand)
		=> GetInternalOperand(destOperand) = CopyConverted(sourceOperand);

	// Initializes the base value of the animation from either the
	// animation target or from a handoff.
	private protected override void GetAnimationBaseValue()
	{
		// we should have a reference and an object, but if we don't have an actual target, keep current value
		var targetObject = GetResolvedTargetObject();

		if (targetObject != null)
		{
			var value = targetObject.GetValue(m_pTargetDependencyProperty!);

			// Copy new base value.
			ValueAssign(AssignmentOperand.BaseValue, value);
		}
	}

	// Generic interpolation method - this MUST be overridden by derived types
	// to provide a way to blend to values of the given animation type
	private protected override void InterpolateCurrentValue(float rPercentEnd)
	{
		// Internal validation
		global::System.Diagnostics.Debug.Assert(rPercentEnd <= 1.0f);
		global::System.Diagnostics.Debug.Assert(rPercentEnd >= 0.0f);

		// Compute actual interpolation
		// We only deal with discrete keyframe values so the percent should be either 1.0 or 0.0
		if (rPercentEnd == 0.0f)
		{
			// Clone From
			ValueAssign(AssignmentOperand.CurrentValue, AssignmentOperand.From);
		}
		else if (rPercentEnd == 1.0f)
		{
			// Clone To
			ValueAssign(AssignmentOperand.CurrentValue, AssignmentOperand.To);
		}
		else
		{
			throw new InvalidOperationException("Object key frames can only be discrete.");
		}
	}

	private protected override void PostInterpolateValues()
	{
		// We can't have the object associated with the keyframe and the target:
		// for multiple associated objects that's not an issue, but for the rest
		// we must break that original association.
		// This is safe since we are only holding the value in the keyframe, never
		// actually using it for anything in the visual tree.
		// TODO Uno: Uno doesn't track associations, a value can be shared by the key frame and the target.
	}

	private ref object? GetInternalOperand(AssignmentOperand operand)
	{
		switch (operand)
		{
			case AssignmentOperand.From:
				return ref m_vFrom;

			case AssignmentOperand.To:
				return ref m_vTo;

			case AssignmentOperand.By:
				return ref m_vBy;

			case AssignmentOperand.BaseValue:
				return ref m_vBaseValue;

			case AssignmentOperand.NonAnimatedBaseValue:
				return ref m_vNonAnimatedBaseValue;

			case AssignmentOperand.CurrentValue:
				return ref m_vCurrentValue;

			default:
				throw new ArgumentOutOfRangeException(nameof(operand));
		}
	}
}
