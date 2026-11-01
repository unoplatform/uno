// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference components/animation/DoubleAnimation.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;
using System.Globalization;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class DoubleAnimation
{
#if !HAS_UNO
	// TODO Uno: NOT PORTED - the destructor, AttachDCompAnimations, ApplyDCompAnimationInstanceToTarget,
	// AttachDCompAnimationInstancesToTarget, DetachDCompAnimationInstancesFromTarget, ResolvePendingDCompAnimationOperations,
	// SeekDCompAnimationInstances, PauseDCompAnimationsOnSuspend, ResumeDCompAnimationsOnResume, ReleaseDCompResources and
	// MakeCompositionKeyFrameAnimationVirtual: composition animations.
#endif

	private protected override void ValueAssign(AssignmentOperand destOperand, AssignmentOperand sourceOperand)
	{
		global::System.Diagnostics.Debug.Assert(sourceOperand != destOperand);

		GetInternalOperand(destOperand) = GetInternalOperand(sourceOperand);
	}

	private protected override object? ValueAssign(AssignmentOperand sourceOperand)
	{
		var sourceValue = GetInternalOperand(sourceOperand);

		// TODO Uno specific: Uno's double properties take a double (WinUI sets a float CValue and converts it).
		return (double)sourceValue;
	}

	private protected override void ValueAssign(AssignmentOperand destOperand, object? sourceOperand)
		=> GetInternalOperand(destOperand) = ToFloat(sourceOperand);

	internal static float ToFloat(object? sourceOperand) => sourceOperand switch
	{
		float floatValue => floatValue,
		double doubleValue => (float)doubleValue,
		null => 0f,
		_ => Convert.ToSingle(sourceOperand, CultureInfo.InvariantCulture),
	};

	private ref float GetInternalOperand(AssignmentOperand operand)
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
