// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// MUX Reference EasingFunctions.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

using System;
using Uno.UI.Helpers.Boxes;

namespace Microsoft.UI.Xaml.Media.Animation;

public partial class ElasticEase : EasingFunctionBase
{
	public double Springiness
	{
		get => (double)this.GetValue(SpringinessProperty);
		set => this.SetValue(SpringinessProperty, Boxer.Box(value));
	}

	public int Oscillations
	{
		get => (int)this.GetValue(OscillationsProperty);
		set => this.SetValue(OscillationsProperty, Boxer.Box(value));
	}

	public static DependencyProperty OscillationsProperty { get; } =
		DependencyProperty.Register(
			nameof(Oscillations),
			typeof(int),
			typeof(ElasticEase),
			new FrameworkPropertyMetadata(3));

	public static DependencyProperty SpringinessProperty { get; } =
		DependencyProperty.Register(
			nameof(Springiness),
			typeof(double),
			typeof(ElasticEase),
			new FrameworkPropertyMetadata(3.0));

	private protected override float EaseInCore(float normalizedTime)
	{
		float oscillations = Math.Max(Oscillations, 0);
		float springiness = (float)Springiness;

		float exponentialModifier;
		if (springiness >= -FloatEpsilon && springiness <= FloatEpsilon)
		{
			exponentialModifier = normalizedTime;
		}
		else
		{
			exponentialModifier = (MathF.Pow(FloatE, springiness * normalizedTime) - 1.0f) / (MathF.Pow(FloatE, springiness) - 1.0f);
		}

		return exponentialModifier * MathF.Sin(normalizedTime * ((2.0f * FloatPi * oscillations) + (FloatPi / 2.0f)));
	}
}
