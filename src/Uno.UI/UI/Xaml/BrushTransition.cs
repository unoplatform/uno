// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BrushTransition_Partial.cpp + XamlOM Motion.cs line 14-27

using System;
using Uno.UI.Helpers;

namespace Microsoft.UI.Xaml;

public partial class BrushTransition : DependencyObject
{
	// WinUI default: 1500000 100-ns ticks = 150 ms (Microsoft.UI.Xaml.Motion.cs line 20).
	private TimeSpan _duration = TimeSpan.FromTicks(1500000);

	public TimeSpan Duration
	{
		get => (TimeSpan)GetValue(DurationProperty);
		set => SetValue(DurationProperty, value);
	}

	public static DependencyProperty DurationProperty { get; } =
		DependencyProperty.Register(
			nameof(Duration),
			typeof(TimeSpan),
			typeof(BrushTransition),
			new FrameworkPropertyMetadata(TimeSpan.FromTicks(1500000))
			{
				PropMethodCall = DurationPropMethod,
			});

#nullable enable
	private static object? DurationPropMethod(DependencyObject instance, bool isGet, object? valueToSet)
	{
		var transition = (BrushTransition)instance;
		if (isGet)
		{
			return transition._duration;
		}

		var newValue = (TimeSpan)valueToSet!;
		if (transition._duration != newValue)
		{
			transition._duration = newValue;
			return true;
		}

		return false;
	}
#nullable restore
}
