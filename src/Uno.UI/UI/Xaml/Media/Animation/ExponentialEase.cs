// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// MUX Reference EasingFunctions.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

using System;
using Uno.UI.Helpers.Boxes;

namespace Microsoft.UI.Xaml.Media.Animation;

public partial class ExponentialEase : EasingFunctionBase
{
	public double Exponent
	{
		get => (double)this.GetValue(ExponentProperty);
		set => this.SetValue(ExponentProperty, Boxer.Box(value));
	}

	public static DependencyProperty ExponentProperty { get; } =
		DependencyProperty.Register(
			nameof(Exponent),
			typeof(double),
			typeof(ExponentialEase),
			new FrameworkPropertyMetadata(defaultValue: 2.0)
		);

	private protected override float EaseInCore(float normalizedTime)
	{
		var exponent = (float)Exponent;
		if (exponent >= -FloatEpsilon && exponent <= FloatEpsilon)
		{
			return normalizedTime;
		}

		return (MathF.Pow(FloatE, exponent * normalizedTime) - 1.0f) / (MathF.Pow(FloatE, exponent) - 1.0f);
	}
}
