// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// MUX Reference EasingFunctions.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

namespace Microsoft.UI.Xaml.Media.Animation;

public partial class QuarticEase : EasingFunctionBase
{
	private protected override float EaseInCore(float normalizedTime)
		=> normalizedTime * normalizedTime * normalizedTime * normalizedTime;
}
