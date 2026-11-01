// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference PointKeyFrame.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// Animates from the Point value of the previous key frame to its own Value using splined interpolation.
/// </summary>
public partial class SplinePointKeyFrame : PointKeyFrame
{
	public SplinePointKeyFrame()
	{
	}

	/// <summary>
	/// Gets or sets the two control points that define animation progress for this key frame.
	/// </summary>
	public KeySpline KeySpline
	{
		get => (KeySpline)GetValue(KeySplineProperty);
		set => SetValue(KeySplineProperty, value);
	}

	/// <summary>
	/// Identifies the KeySpline dependency property.
	/// </summary>
	public static DependencyProperty KeySplineProperty { get; } =
		DependencyProperty.Register(nameof(KeySpline), typeof(KeySpline), typeof(SplinePointKeyFrame), new FrameworkPropertyMetadata(null));

	// Computes effective progress of the current keyframe versus a prior time value.
	// The keyframe must be initalized with percentage for this to work.
	internal override float GetEffectiveProgress(float rCurrentProgress)
	{
		if (KeySpline is { } keySpline)
		{
			return keySpline.GetSplineProgress(rCurrentProgress);
		}
		else
		{
			return 0;
		}
	}
}
