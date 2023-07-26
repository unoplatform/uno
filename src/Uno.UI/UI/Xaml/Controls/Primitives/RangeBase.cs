using System;

namespace Windows.UI.Xaml.Controls.Primitives;

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
}
