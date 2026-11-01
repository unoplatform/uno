// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// MUX Reference EasingFunctions.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

using System;
using Uno.UI.Helpers.Boxes;

namespace Microsoft.UI.Xaml.Media.Animation;

public partial class BounceEase : EasingFunctionBase
{
	public double Bounciness
	{
		get => (double)this.GetValue(BouncinessProperty);
		set => this.SetValue(BouncinessProperty, Boxer.Box(value));
	}

	public int Bounces
	{
		get => (int)this.GetValue(BouncesProperty);
		set => this.SetValue(BouncesProperty, Boxer.Box(value));
	}

	public static DependencyProperty BouncesProperty { get; } =
		DependencyProperty.Register(
			nameof(Bounces),
			typeof(int),
			typeof(BounceEase),
			new FrameworkPropertyMetadata(3));

	public static DependencyProperty BouncinessProperty { get; } =
		DependencyProperty.Register(
			nameof(Bounciness),
			typeof(double),
			typeof(BounceEase),
			new FrameworkPropertyMetadata(2.0));

	private float GetClampedBounciness()
	{
		var bounciness = (float)Bounciness;
		return bounciness <= 1.0f + FloatEpsilon ? 1.01f : bounciness;
	}

	private protected override float EaseInCore(float normalizedTime)
	{
		var iBounces = Bounces;
		float bounces = iBounces >= 0 ? iBounces : 0.0f;

		// Clamp the bounciness so we don't hit a divide by zero
		float bounciness = GetClampedBounciness();

		float pow = MathF.Pow(bounciness, bounces);
		float oneMinusBounciness = 1.0f - bounciness;

		// 'unit' space calculations, geometric series with only half the last sum
		float sumOfUnits = (1.0f - pow) / oneMinusBounciness + pow / 2.0f;
		float unitAtT = normalizedTime * sumOfUnits;

		// 'bounce' space calculations
		float bounceAtT = (float)(Math.Log((double)(-unitAtT * (1.0f - bounciness) + 1.0f)) / Math.Log((double)bounciness));
		float start = MathF.Floor(bounceAtT);
		float end = start + 1.0f;

		// 'time' space calculations
		float startTime = (1.0f - MathF.Pow(bounciness, start)) / (oneMinusBounciness * sumOfUnits);
		float endTime = (1.0f - MathF.Pow(bounciness, end)) / (oneMinusBounciness * sumOfUnits);

		// Curve fitting for bounce.
		float midTime = (startTime + endTime) / 2.0f;
		float timeRelativeToPeak = normalizedTime - midTime;
		float radius = midTime - startTime;
		float amplitude = MathF.Pow(1.0f / bounciness, bounces - start);

		// Evaluate a quadratic that hits (startTime, 0), (endTime, 0), and peaks at amplitude
		return (-amplitude / (radius * radius)) * (timeRelativeToPeak - radius) * (timeRelativeToPeak + radius);
	}
}
