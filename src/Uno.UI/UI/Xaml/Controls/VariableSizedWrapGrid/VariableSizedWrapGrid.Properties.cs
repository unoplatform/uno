using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class VariableSizedWrapGrid
{
	/// <summary>
	/// Identifies the VariableSizedWrapGrid.ColumnSpan XAML attached property.
	/// </summary>
	public static DependencyProperty ColumnSpanProperty
	{
		[DynamicDependency(nameof(GetColumnSpan))]
		[DynamicDependency(nameof(SetColumnSpan))]
		get;
	} = DependencyProperty.RegisterAttached(
			"ColumnSpan",
			typeof(int),
			typeof(VariableSizedWrapGrid),
			new FrameworkPropertyMetadata(1));

	/// <summary>
	/// Gets the value of the VariableSizedWrapGrid.ColumnSpan XAML attached property from a target element.
	/// </summary>
	/// <param name="element">The target element.</param>
	/// <returns>The obtained value.</returns>
	public static int GetColumnSpan(UIElement element) => (int)element.GetValue(ColumnSpanProperty);

	/// <summary>
	/// Sets the value of the VariableSizedWrapGrid.ColumnSpan XAML attached property on a target element.
	/// </summary>
	/// <param name="element">The target element.</param>
	/// <param name="value">The value to set.</param>
	public static void SetColumnSpan(UIElement element, int value) => element.SetValue(ColumnSpanProperty, value);

	/// <summary>
	/// Gets or sets the alignment rules by which child elements are arranged for the horizontal dimension.
	/// </summary>
	public HorizontalAlignment HorizontalChildrenAlignment
	{
		get => (HorizontalAlignment)GetValue(HorizontalChildrenAlignmentProperty);
		set => SetValue(HorizontalChildrenAlignmentProperty, value);
	}

	/// <summary>
	/// Identifies the HorizontalChildrenAlignment dependency property.
	/// </summary>
	public static DependencyProperty HorizontalChildrenAlignmentProperty { get; } =
		DependencyProperty.Register(
			nameof(HorizontalChildrenAlignment),
			typeof(HorizontalAlignment),
			typeof(VariableSizedWrapGrid),
			new FrameworkPropertyMetadata(default(HorizontalAlignment)));

	/// <summary>
	/// Gets or sets the height of the layout area for each item that is contained in a VariableSizedWrapGrid.
	/// </summary>
	public double ItemHeight
	{
		get => (double)GetValue(ItemHeightProperty);
		set => SetValue(ItemHeightProperty, value);
	}

	/// <summary>
	/// Identifies the ItemHeight dependency property.
	/// </summary>
	public static DependencyProperty ItemHeightProperty { get; } =
		DependencyProperty.Register(
			nameof(ItemHeight),
			typeof(double),
			typeof(VariableSizedWrapGrid),
			new FrameworkPropertyMetadata(double.NaN));

	/// <summary>
	/// Gets or sets the width of the layout area for each item that is contained in a VariableSizedWrapGrid.
	/// </summary>
	public double ItemWidth
	{
		get => (double)GetValue(ItemWidthProperty);
		set => SetValue(ItemWidthProperty, value);
	}

	/// <summary>
	/// Identifies the ItemWidth dependency property.
	/// </summary>
	public static DependencyProperty ItemWidthProperty { get; } =
		DependencyProperty.Register(
			nameof(ItemWidth),
			typeof(double),
			typeof(VariableSizedWrapGrid),
			new FrameworkPropertyMetadata(double.NaN));

	/// <summary>
	/// Gets or sets a value that influences the wrap point, also accounting for Orientation.
	/// </summary>
	public int MaximumRowsOrColumns
	{
		get => (int)GetValue(MaximumRowsOrColumnsProperty);
		set => SetValue(MaximumRowsOrColumnsProperty, value);
	}

	/// <summary>
	/// Identifies the MaximumRowsOrColumns dependency property.
	/// </summary>
	public static DependencyProperty MaximumRowsOrColumnsProperty { get; } =
		DependencyProperty.Register(
			nameof(MaximumRowsOrColumns),
			typeof(int),
			typeof(VariableSizedWrapGrid),
			new FrameworkPropertyMetadata(-1));

	internal override Orientation? PhysicalOrientation => Orientation;

	/// <summary>
	/// Gets or sets the direction in which child elements are arranged.
	/// </summary>
	public Orientation Orientation
	{
		get => (Orientation)GetValue(OrientationProperty);
		set => SetValue(OrientationProperty, value);
	}

	/// <summary>
	/// Identifies the Orientation dependency property.
	/// </summary>
	public static DependencyProperty OrientationProperty { get; } =
		DependencyProperty.Register(
			nameof(Orientation),
			typeof(Orientation),
			typeof(VariableSizedWrapGrid),
			new FrameworkPropertyMetadata(default(Orientation)));

	/// <summary>
	/// Identifies the VariableSizedWrapGrid.RowSpan XAML attached property.
	/// </summary>
	public static DependencyProperty RowSpanProperty
	{
		[DynamicDependency(nameof(GetRowSpan))]
		[DynamicDependency(nameof(SetRowSpan))]
		get;
	} = DependencyProperty.RegisterAttached(
			"RowSpan",
			typeof(int),
			typeof(VariableSizedWrapGrid),
			new FrameworkPropertyMetadata(1));

	/// <summary>
	/// Gets the value of the VariableSizedWrapGrid.RowSpan XAML attached property from a target element.
	/// </summary>
	/// <param name="element">The target element.</param>
	/// <returns>The obtained value.</returns>
	public static int GetRowSpan(UIElement element) => (int)element.GetValue(RowSpanProperty);

	/// <summary>
	/// Sets the value of the VariableSizedWrapGrid.RowSpan XAML attached property on a target element.
	/// </summary>
	/// <param name="element">The target element.</param>
	/// <param name="value">The value to set.</param>
	public static void SetRowSpan(UIElement element, int value) => element.SetValue(RowSpanProperty, value);

	/// <summary>
	/// Gets or sets the alignment rules by which child elements are arranged for the vertical dimension.
	/// </summary>
	public VerticalAlignment VerticalChildrenAlignment
	{
		get => (VerticalAlignment)GetValue(VerticalChildrenAlignmentProperty);
		set => SetValue(VerticalChildrenAlignmentProperty, value);
	}

	/// <summary>
	/// Identifies the VerticalChildrenAlignment dependency property.
	/// </summary>
	public static DependencyProperty VerticalChildrenAlignmentProperty { get; } =
		DependencyProperty.Register(
			nameof(VerticalChildrenAlignment),
			typeof(VerticalAlignment),
			typeof(VariableSizedWrapGrid),
			new FrameworkPropertyMetadata(default(VerticalAlignment)));
}
