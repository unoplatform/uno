// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// MUX Reference EasingFunctions.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

using System;
using Uno.UI.Helpers.Boxes;

namespace Microsoft.UI.Xaml.Media.Animation;

public partial class BackEase : EasingFunctionBase
{
	public double Amplitude
	{
		get => (double)this.GetValue(AmplitudeProperty);
		set => this.SetValue(AmplitudeProperty, Boxer.Box(value));
	}

	public static DependencyProperty AmplitudeProperty { get; } =
		DependencyProperty.Register("Amplitude", typeof(double), typeof(BackEase), new FrameworkPropertyMetadata(DoubleBoxes.One));

	private protected override float EaseInCore(float normalizedTime)
	{
		if (normalizedTime < 0.0f)
		{
			normalizedTime = 0.0f;
		}

		return (normalizedTime * normalizedTime * normalizedTime) - normalizedTime * (float)Amplitude * MathF.Sin(normalizedTime * FloatPi);
	}
}
