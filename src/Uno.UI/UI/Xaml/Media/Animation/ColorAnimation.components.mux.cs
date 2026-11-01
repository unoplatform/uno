// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference components/animation/ColorAnimation.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;
using Windows.UI;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class ColorAnimation
{
#if !HAS_UNO
	// TODO Uno: NOT PORTED - AttachDCompAnimations, ApplyDCompAnimationInstanceToTarget, AttachDCompAnimationInstancesToTarget,
	// DetachDCompAnimationInstancesFromTarget, ResolvePendingDCompAnimationOperations, SeekDCompAnimationInstances,
	// PauseDCompAnimationsOnSuspend, ResumeDCompAnimationsOnResume and ReleaseDCompResources: composition animations.
#endif

	private protected override void ValueAssign(AssignmentOperand destOperand, AssignmentOperand sourceOperand)
	{
		global::System.Diagnostics.Debug.Assert(sourceOperand != destOperand);

		GetInternalOperand(destOperand) = GetInternalOperand(sourceOperand);
	}

	private protected override object? ValueAssign(AssignmentOperand sourceOperand)
		=> FromArgb(GetInternalOperand(sourceOperand));

	private protected override void ValueAssign(AssignmentOperand destOperand, object? sourceOperand)
	{
		global::System.Diagnostics.Debug.Assert(sourceOperand is Color);

		GetInternalOperand(destOperand) = sourceOperand is Color color ? ToArgb(color) : 0;
	}

	private ref uint GetInternalOperand(AssignmentOperand operand)
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

#if !HAS_UNO
	// TODO Uno: NOT PORTED - MakeCompositionKeyFrameAnimationVirtual: composition animations.
#endif
}
