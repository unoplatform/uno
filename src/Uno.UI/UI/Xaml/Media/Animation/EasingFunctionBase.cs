// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// MUX Reference EasingFunctions.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Media.Animation;

public partial class EasingFunctionBase : DependencyObject, IEasingFunction
{
	internal const float FloatPi = 3.1415926535897932384626433832795f;
	internal const float FloatE = 2.7182818284590452353602874713527f;
	internal const float FloatEpsilon = 0.0001f;

	private protected EasingFunctionBase()
	{
		InitializeBinder();
	}

	public EasingMode EasingMode
	{
		get => (EasingMode)this.GetValue(EasingModeProperty);
		set => this.SetValue(EasingModeProperty, value);
	}

	public static DependencyProperty EasingModeProperty { get; } =
		DependencyProperty.Register("EasingMode", typeof(EasingMode), typeof(EasingFunctionBase), new FrameworkPropertyMetadata(EasingMode.EaseOut));

	public double Ease(double normalizedTime) => EaseValue((float)normalizedTime);

	internal float EaseValue(float normalizedTime)
	{
		switch (EasingMode)
		{
			case EasingMode.EaseIn:
				return EaseInCore(normalizedTime);
			case EasingMode.EaseOut:
				return 1.0f - EaseInCore(1.0f - normalizedTime);
			case EasingMode.EaseInOut:
				if (normalizedTime < 0.5f)
				{
					return EaseInCore(normalizedTime * 2.0f) / 2.0f;
				}

				return (1.0f - EaseInCore(2.0f - normalizedTime * 2.0f)) / 2.0f + 0.5f;
			default:
				return 0;
		}
	}

	internal static float EaseValue(EasingFunctionBase? easing, float value)
		=> easing is null ? value : easing.EaseValue(value);

	private protected virtual float EaseInCore(float normalizedTime) => normalizedTime;

	double IEasingFunction.Ease(double currentTime, double startValue, double finalValue, double duration)
		=> startValue + Ease(currentTime / duration) * (finalValue - startValue);
}
