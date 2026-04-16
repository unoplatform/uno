using System;

namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Represents an element that has a value within a specific range,
/// such as the ProgressBar, ScrollBar, and Slider controls.
/// </summary>
public partial class RangeBase : Control
{
	/// <summary>
	/// Provides base class initialization behavior for RangeBase-derived classes.
	/// </summary>
	protected RangeBase()
	{
		DefaultStyleKey = typeof(RangeBase);
	}

	// Uno specific: Coercion for properties to call RangeBase Core

	private static object CoerceValue(DependencyObject dependencyObject, object baseValue, DependencyPropertyValuePrecedences _)
	{
		return ((RangeBase)dependencyObject).SetRangeBaseValue(ValueProperty, baseValue);
	}

	private static object CoerceMinimum(DependencyObject dependencyObject, object baseValue, DependencyPropertyValuePrecedences _)
	{
		return ((RangeBase)dependencyObject).SetRangeBaseValue(MinimumProperty, baseValue);
	}

	private static object CoerceMaximum(DependencyObject dependencyObject, object baseValue, DependencyPropertyValuePrecedences _)
	{
		return ((RangeBase)dependencyObject).SetRangeBaseValue(MaximumProperty, baseValue);
	}

	// Uno specific: coercion workaround

	private static void OnRangeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is not RangeBase control) return;

		if (e.Property == MinimumProperty || e.Property == MaximumProperty)
		{
			var clampedValue = control.CoerceValueBetween(control.m_uncoercedValue, control.Minimum, control.Maximum);
			var currentValue = control.Value;

			if (clampedValue != currentValue)
			{
				control.CoerceValue(ValueProperty);
			}
		}
	}
}
