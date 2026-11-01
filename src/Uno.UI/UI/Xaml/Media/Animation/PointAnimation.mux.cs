// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference PointAnimation.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class PointAnimation
{
	public PointAnimation()
	{
	}

	internal static XPOINTF ToXPointF(Point point) => new XPOINTF { x = (float)point.X, y = (float)point.Y };

	internal static Point FromXPointF(XPOINTF point) => new Point(point.x, point.y);

	// Generic interpolation method - this MUST be overridden by derived types
	// to provide a way to blend to values of the given animation type
	private protected override void InterpolateCurrentValue(float rPercentEnd)
		=> m_ptCurrentValue = InterpolateCurrentValue(m_ptFrom, m_ptTo, rPercentEnd);

	// TODO Uno specific: static so PointAnimationUsingKeyFrames (a CPointAnimation in WinUI) can share it.
	internal static XPOINTF InterpolateCurrentValue(XPOINTF ptFrom, XPOINTF ptTo, float rPercentEnd)
	{
		// Easing functions can give an unnormalized value outside of the range 0 to 1 so
		// don't validate that rPercentEnd is from 0 to 1 but handle the cases where is is.

		var rPercentStart = 1.0f - rPercentEnd;

		// Compute actual interpolation
		XPOINTF result;
		result.x = ptFrom.x * rPercentStart + rPercentEnd * ptTo.x;
		result.y = ptFrom.y * rPercentStart + rPercentEnd * ptTo.y;
		return result;
	}

	// Computes the m_pTo field from the m_pBy and m_pFrom fields using a type-specific addition;
	private protected override void ComputeToUsingFromAndBy()
	{
		// Compute type-specific add
		m_ptTo.x = m_ptFrom.x + m_ptBy.x;
		m_ptTo.y = m_ptFrom.y + m_ptBy.y;
	}

	private protected override void ValueAssign(AssignmentOperand destOperand, AssignmentOperand sourceOperand)
	{
		global::System.Diagnostics.Debug.Assert(sourceOperand != destOperand);

		GetInternalOperand(destOperand) = GetInternalOperand(sourceOperand);
	}

	private protected override object? ValueAssign(AssignmentOperand sourceOperand)
	{
		var sourceValue = GetInternalOperand(sourceOperand);

		// TODO Uno specific: Uno's point properties take a Point (WinUI sets an XPOINTF CValue).
		return FromXPointF(sourceValue);
	}

	private protected override void ValueAssign(AssignmentOperand destOperand, object? sourceOperand)
	{
		ref var destValue = ref GetInternalOperand(destOperand);

		if (sourceOperand is Point point)
		{
			destValue = ToXPointF(point);
		}
		else
		{
			destValue = default;
		}
	}

	// Initializes the base value of the animation from either the animation target or from a handoff.
	private protected override void GetAnimationBaseValue()
	{
		// we should have a reference, but if we don't have an actual target, keep current value
		global::System.Diagnostics.Debug.Assert(m_targetObjectWeakRef is not null);
		var targetObject = GetResolvedTargetObject();

		if (targetObject != null)
		{
			if (targetObject.GetValue(m_pTargetDependencyProperty!) is Point rawValue)
			{
				m_ptBaseValue = ToXPointF(rawValue);
			}
		}
	}

	// Returns reference to class member variable mapping to operand.
	private ref XPOINTF GetInternalOperand(AssignmentOperand operand)
	{
		switch (operand)
		{
			case AssignmentOperand.From:
				return ref m_ptFrom;

			case AssignmentOperand.To:
				return ref m_ptTo;

			case AssignmentOperand.By:
				return ref m_ptBy;

			case AssignmentOperand.BaseValue:
				return ref m_ptBaseValue;

			case AssignmentOperand.NonAnimatedBaseValue:
				return ref m_ptNonAnimatedBaseValue;

			case AssignmentOperand.CurrentValue:
				return ref m_ptCurrentValue;

			default:
				throw new ArgumentOutOfRangeException(nameof(operand));
		}
	}
}
