// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference PointAnimation.h, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class PointAnimation
{
	// WinUI animates points as XPOINTF (float x/y).
	internal struct XPOINTF
	{
		public float x;
		public float y;
	}

	internal override bool IsAnimation() => true;

#if HAS_UNO
	// TODO Uno specific: WinUI writes From/To/By straight into the operands (m_ptFrom/m_ptTo/m_ptBy are the property storage),
	// and tracks unset values through IsPropertyDefault. Uno's properties are nullable: null is the default.
	private void OnAnimationValuePropertyChanged(AssignmentOperand operand, object? newValue)
	{
		if (newValue is Point value)
		{
			GetInternalOperand(operand) = ToXPointF(value);
		}
	}

	private protected override bool IsFromDefault => From is null;

	private protected override bool IsToDefault => To is null;

	private protected override bool IsByDefault => By is null;

	private protected override EasingFunctionBase? m_pEasingFunction => EasingFunction;

	private protected override bool m_enableDependentAnimation => EnableDependentAnimation;

	private protected override Type? AnimatedPropertyType => typeof(Point);
#endif

	internal XPOINTF m_ptFrom;
	internal XPOINTF m_ptTo;
	internal XPOINTF m_ptBy;
	internal XPOINTF m_ptBaseValue;
	internal XPOINTF m_ptNonAnimatedBaseValue;
	internal XPOINTF m_ptCurrentValue;
}
