// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ColorAnimation.h, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;
using Windows.UI;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class ColorAnimation
{
	internal override bool IsAnimation() => true;

#if HAS_UNO
	// TODO Uno specific: WinUI writes From/To/By straight into the operands (the property storage), and tracks unset
	// values through IsPropertyDefault. Uno's properties are nullable: null is the default.
	private void OnAnimationValuePropertyChanged(AssignmentOperand operand, object? newValue)
	{
		if (newValue is Color value)
		{
			GetInternalOperand(operand) = ToArgb(value);
		}
	}

	private protected override bool IsFromDefault => From is null;

	private protected override bool IsToDefault => To is null;

	private protected override bool IsByDefault => By is null;

	private protected override EasingFunctionBase? m_pEasingFunction => EasingFunction;

	private protected override bool m_enableDependentAnimation => EnableDependentAnimation;

	private protected override Type? AnimatedPropertyType => typeof(Color);
#endif

	internal uint m_vFrom = 0;
	internal uint m_vTo = 0;
	internal uint m_vBy = 0;
	internal uint m_vBaseValue = 0;
	internal uint m_vNonAnimatedBaseValue = 0;
	internal uint m_vCurrentValue = 0;
}
