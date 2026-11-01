// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference PointKeyFrame.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// Animates from the Point value of the previous key frame to its own Value using an easing function.
/// </summary>
public partial class EasingPointKeyFrame : PointKeyFrame
{
	public EasingPointKeyFrame()
	{
	}

	/// <summary>
	/// Gets or sets the easing function applied to the key frame.
	/// </summary>
	public EasingFunctionBase EasingFunction
	{
		get => (EasingFunctionBase)GetValue(EasingFunctionProperty);
		set => SetValue(EasingFunctionProperty, value);
	}

	/// <summary>
	/// Identifies the EasingFunction dependency property.
	/// </summary>
	public static DependencyProperty EasingFunctionProperty { get; } =
		DependencyProperty.Register(nameof(EasingFunction), typeof(EasingFunctionBase), typeof(EasingPointKeyFrame), new FrameworkPropertyMetadata(null));

	// Computes effective progress of the current keyframe versus a prior time value.
	internal override float GetEffectiveProgress(float rCurrentProgress)
		=> EasingFunctionBase.EaseValue(EasingFunction, rCurrentProgress);
}
