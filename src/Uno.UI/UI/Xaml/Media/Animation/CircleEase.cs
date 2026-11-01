// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// MUX Reference EasingFunctions.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

using System;

namespace Microsoft.UI.Xaml.Media.Animation;

public partial class CircleEase : EasingFunctionBase
{
	private protected override float EaseInCore(float normalizedTime)
	{
		normalizedTime = Math.Clamp(normalizedTime, -1.0f, 1.0f);
		return -1.0f * (MathF.Sqrt(1.0f - (normalizedTime * normalizedTime)) - 1.0f);
	}
}
