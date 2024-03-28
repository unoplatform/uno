using System;

namespace Windows.UI.Xaml.Controls.Primitives;

public partial class RangeBase
{
	/// <summary>
	/// Gets or sets a value to be added to or subtracted from the Value of a RangeBase control.
	/// </summary>
	public double LargeChange
	{
		get => (double)GetValue(LargeChangeProperty);
		set
		{
			EnsureValidDoubleValue(value);
			SetValue(LargeChangeProperty, value);
		}
	}

	/// <summary>
	/// Identifies the LargeChange dependency property.
	/// </summary>
	public static DependencyProperty LargeChangeProperty { get; } =
		DependencyProperty.Register(
			nameof(LargeChange),
			typeof(double),
			typeof(RangeBase),
			new FrameworkPropertyMetadata(1.0));

	/// <summary>
	/// Gets or sets the Maximum possible Value of the range element.
	/// </summary>
	public double Maximum
	{
		get => (double)GetValue(MaximumProperty);
		set
		{
			EnsureValidDoubleValue(value);
			SetValue(MaximumProperty, value);
		}
	}

	/// <summary>
	/// Identifies the Maximum dependency property.
	/// </summary>
	public static DependencyProperty MaximumProperty { get; } =
		DependencyProperty.Register(
			nameof(Maximum),
			typeof(double),
			typeof(RangeBase),
			new FrameworkPropertyMetadata(1.0, null, CoerceMaximum));

	/// <summary>
	/// Gets or sets the Minimum possible Value of the range element.
	/// </summary>
	public double Minimum
	{
		get => (double)GetValue(MinimumProperty);
		set
		{
			EnsureValidDoubleValue(value);
			SetValue(MinimumProperty, value);
		}
	}

	/// <summary>
	/// Identifies the Minimum dependency property.
	/// </summary>
	public static DependencyProperty MinimumProperty { get; } =
		DependencyProperty.Register(
			nameof(Minimum),
			typeof(double),
			typeof(RangeBase),
			new FrameworkPropertyMetadata(0.0, null, CoerceMinimum));

	/// <summary>
	/// Gets or sets a Value to be added to or subtracted from the Value of a RangeBase control.
	/// </summary>
	public double SmallChange
	{
		get => (double)GetValue(SmallChangeProperty);
		set
		{
			EnsureValidDoubleValue(value);
			SetValue(SmallChangeProperty, value);
		}
	}

	/// <summary>
	/// Identifies the SmallChange dependency property.
	/// </summary>
	public static DependencyProperty SmallChangeProperty { get; } =
		DependencyProperty.Register(
			nameof(SmallChange),
			typeof(double),
			typeof(RangeBase),
			new FrameworkPropertyMetadata(0.1));

	/// <summary>
	/// Gets or sets the current magnitude of the range control.
	/// </summary>
	public double Value
	{
		get => (double)GetValue(ValueProperty);
		set
		{
			EnsureValidDoubleValue(value);
			SetValue(ValueProperty, value);
		}
	}

	/// <summary>
	/// Identifies the Value dependency property.
	/// </summary>
	public static DependencyProperty ValueProperty { get; } =
		DependencyProperty.Register(
			nameof(Value),
			typeof(double),
			typeof(RangeBase),
			new FrameworkPropertyMetadata(0.0, null, CoerceValue));

	/// <summary>
	/// Occurs when the range value changes.
	/// </summary>
	public event RangeBaseValueChangedEventHandler ValueChanged;
}
