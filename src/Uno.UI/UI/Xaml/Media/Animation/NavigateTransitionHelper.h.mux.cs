// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference NavigateTransitionHelper.h, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Media.Animation;

internal struct ObjectKeyFrameDescription
{
	public long m_time;
	public bool m_value;
	public EasingFunctionBase? m_easingFunction;

	public ObjectKeyFrameDescription(long time, bool value)
	{
		m_time = time;
		m_value = value;
		m_easingFunction = null;
	}

	public ObjectKeyFrameDescription(long time, bool value, EasingFunctionBase? easingFunction)
	{
		m_time = time;
		m_value = value;
		m_easingFunction = easingFunction;
	}
}

internal struct ObjectAnimationDescription
{
	public UIElement? m_element;
	public string? m_prop;
	public List<ObjectKeyFrameDescription>? m_pKeyFrameDescs;

	public ObjectAnimationDescription(UIElement element, string prop, List<ObjectKeyFrameDescription> keyFrames)
	{
		m_element = element;
		m_prop = prop;
		m_pKeyFrameDescs = keyFrames;
	}
}

internal struct DoubleKeyFrameDescription
{
	public long m_time;
	public double m_value;
	public EasingFunctionBase? m_easingFunction;

	public DoubleKeyFrameDescription(long time, double value)
	{
		m_time = time;
		m_value = value;
		m_easingFunction = null;
	}

	public DoubleKeyFrameDescription(long time, double value, EasingFunctionBase? easingFunction)
	{
		m_time = time;
		m_value = value;
		m_easingFunction = easingFunction;
	}
}

internal struct DoubleAnimationDescription
{
	public UIElement? m_element;
	public string? m_prop;
	public List<DoubleKeyFrameDescription>? m_pKeyFrameDescs;

	public DoubleAnimationDescription(UIElement element, string prop, List<DoubleKeyFrameDescription> keyFrames)
	{
		m_element = element;
		m_prop = prop;
		m_pKeyFrameDescs = keyFrames;
	}
}

internal static partial class NavigateTransitionHelper
{
	// General Values.

	public const int WARP_FACTOR = 1;
	public const int NO_OFFSET = 0;
	public const int TIME_ZERO = 0;

	// Turnstile Values.

	public const int TURNSTILE_EASE = 6;

	public const int TURNSTILE_OFFSET_IN = 80;
	public const int TURNSTILE_OFFSET_OUT = 50;
	public const double TURNSTILE_AXIS_X = -0.1;
	public const double TURNSTILE_AXIS_Z = -100.0;

	public const long TURNSTILE_START_TIME = 0;
	public const long TURNSTILE_MID_TIME = WARP_FACTOR * 128;
	public const long TURNSTILE_END_TIME = TURNSTILE_MID_TIME + WARP_FACTOR * 428;

	public const int TURNSTILE_FEATHER_OFFSET_IN = WARP_FACTOR * 33;
	public const int TURNSTILE_FEATHER_OFFSET_OUT = WARP_FACTOR * 33;

	public const double TURNSTILE_FEATHER_SCALE_IN = 0.5;
	public const double TURNSTILE_FEATHER_SCALE_OUT = 0.5;

	// Slide Values.

	public const int SLIDE_EASE = 6;

	public const int SLIDE_OFFSET_IN = 200;
	public const int SLIDE_OFFSET_OUT = 200;

	public const long SLIDE_START_TIME = 0;
	public const long SLIDE_MID_TIME = WARP_FACTOR * 250;
	public const long SLIDE_END_TIME = SLIDE_MID_TIME + WARP_FACTOR * 350;

	// Opacity Values.

	public const int NO_OPACITY = 0;
	public const int FULL_OPACITY = 1;

	// Scale Values.

	public const int NO_SCALE = 1;
}
