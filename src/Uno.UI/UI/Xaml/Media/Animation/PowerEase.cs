// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// MUX Reference EasingFunctions.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

using System;
using Uno.UI.Helpers.Boxes;

namespace Microsoft.UI.Xaml.Media.Animation;

public partial class PowerEase : EasingFunctionBase
{
	public double Power
	{
		get => (double)this.GetValue(PowerProperty);
		set => this.SetValue(PowerProperty, Boxer.Box(value));
	}

	public static DependencyProperty PowerProperty { get; } =
		DependencyProperty.Register(nameof(Power), typeof(double), typeof(PowerEase), new FrameworkPropertyMetadata(2.0));

	private protected override float EaseInCore(float normalizedTime)
	{
		var effectivePower = MathF.Max((float)Power, 0.0f);
		return MathF.Pow(normalizedTime, effectivePower);
	}
}
