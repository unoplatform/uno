using System;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls;
//TODO:MZ: Use generated DPs
partial class StackPanel
{
	/// <summary>
	/// Gets a value that indicates whether the horizontal snap points
	/// for the StackPanel are equidistant from each other.
	/// </summary>
	public bool AreHorizontalSnapPointsRegular => AreHorizontalSnapPointsRegularImpl();

	/// <summary>
	/// Gets or sets a value that indicates whether the generated snap points
	/// used for panning in the StackPanel are equidistant from each other.
	/// </summary>
	public bool AreScrollSnapPointsRegular
	{
		get => (bool)GetValue(AreScrollSnapPointsRegularProperty);
		set => SetValue(AreScrollSnapPointsRegularProperty, value);
	}

	/// <summary>
	/// Identifies the AreScrollSnapPointsRegular dependency property.
	/// </summary>
	public static DependencyProperty AreScrollSnapPointsRegularProperty { get; } =
		DependencyProperty.Register(
			nameof(AreScrollSnapPointsRegular),
			typeof(bool),
			typeof(StackPanel),
			new FrameworkPropertyMetadata(false));

	/// <summary>
	/// Gets a value that indicates whether the vertical snap points
	/// for the StackPanel are equidistant from each other.
	/// </summary>
	public bool AreVerticalSnapPointsRegular => AreVerticalSnapPointsRegularImpl();

	/// <summary>
	/// Gets or sets a value that indicates how far the background
	/// extends in relation to this element's border.
	/// </summary>
	public BackgroundSizing BackgroundSizing
	{
		get => (BackgroundSizing)GetValue(BackgroundSizingProperty);
		set => SetValue(BackgroundSizingProperty, value);
	}

	/// <summary>
	/// Identifies the BackgroundSizing dependency property.
	/// </summary>
	public static DependencyProperty BackgroundSizingProperty { get; } =
		DependencyProperty.Register(
			nameof(BackgroundSizing),
			typeof(BackgroundSizing),
			typeof(StackPanel),
			new FrameworkPropertyMetadata(default(BackgroundSizing)));

	/// <summary>
	/// Gets or sets a brush that describes the border fill of the panel.
	/// </summary>
	public Brush BorderBrush
	{
		get => (Brush)GetValue(BorderBrushProperty);
		set => SetValue(BorderBrushProperty, value);
	}

	/// <summary>
	/// Identifies the BorderBrush dependency property.
	/// </summary>
	public static DependencyProperty BorderBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(BorderBrush),
			typeof(Brush),
			typeof(StackPanel),
			new FrameworkPropertyMetadata(default(Brush)));

	/// <summary>
	/// Gets or sets the border thickness of the panel.
	/// </summary>
	public Thickness BorderThickness
	{
		get => (Thickness)GetValue(BorderThicknessProperty);
		set => SetValue(BorderThicknessProperty, value);
	}

	/// <summary>
	/// Identifies the BorderThickness dependency property.
	/// </summary>
	public static DependencyProperty BorderThicknessProperty { get; } =
		DependencyProperty.Register(
			nameof(BorderThickness),
			typeof(Thickness),
			typeof(StackPanel),
			new FrameworkPropertyMetadata(default(Thickness)));

	/// <summary>
	/// Gets or sets the radius for the corners of the panel's border.
	/// </summary>
	public CornerRadius CornerRadius
	{
		get => (CornerRadius)GetValue(CornerRadiusProperty);
		set => SetValue(CornerRadiusProperty, value);
	}

	/// <summary>
	/// Identifies the CornerRadius dependency property.
	/// </summary>
	public static DependencyProperty CornerRadiusProperty { get; } =
		DependencyProperty.Register(
			nameof(CornerRadius),
			typeof(CornerRadius),
			typeof(StackPanel),
			new FrameworkPropertyMetadata(default(CornerRadius)));

	/// <summary>
	/// Gets or sets the dimension by which child elements are stacked.
	/// </summary>
	public Orientation Orientation
	{
		get => (Orientation)GetValue(OrientationProperty);
		set => SetValue(OrientationProperty, value);
	}

	/// <summary>
	/// Identifies the Orientation  dependency property.
	/// </summary>
	public static DependencyProperty OrientationProperty { get; } =
		DependencyProperty.Register(
			nameof(Orientation),
			typeof(Orientation),
			typeof(StackPanel),
			new FrameworkPropertyMetadata(
				default(Orientation),
				FrameworkPropertyMetadataOptions.AffectsMeasure));

	/// <summary>
	/// Gets or sets the distance between the border and its child object.
	/// </summary>
	public Thickness Padding
	{
		get => (Thickness)GetValue(PaddingProperty);
		set => SetValue(PaddingProperty, value);
	}

	/// <summary>
	/// Identifies the Padding dependency property.
	/// </summary>
	public static DependencyProperty PaddingProperty { get; } =
		DependencyProperty.Register(
			nameof(Padding),
			typeof(Thickness),
			typeof(StackPanel),
			new FrameworkPropertyMetadata(default(Thickness)));

	/// <summary>
	/// Gets or sets a uniform distance (in pixels) between stacked items.
	/// It is applied in the direction of the StackPanel's Orientation.
	/// </summary>
	public double Spacing
	{
		get => (double)GetValue(SpacingProperty);
		set => SetValue(SpacingProperty, value);
	}

	/// <summary>
	/// Identifies the Spacing dependency property.
	/// </summary>
	public static DependencyProperty SpacingProperty { get; } =
		DependencyProperty.Register(
			nameof(Spacing),
			typeof(double),
			typeof(StackPanel),
			new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

	private EventHandler<object> _horizontalSnapPointsChanged;
	
	/// <summary>
	/// Occurs when the measurements for horizontal snap points change.
	/// </summary>
	public event EventHandler<object> HorizontalSnapPointsChanged
	{
		add => AddHorizontalSnapPointsChanged(value);
		remove => RemoveHorizontalSnapPointsChanged(value);
	}
	
	private EventHandler<object> _verticalSnapPointsChanged;

	/// <summary>
	/// Occurs when the measurements for vertical snap points change.
	/// </summary>
	public event EventHandler<object> VerticalSnapPointsChanged
	{
		add => AddVerticalSnapPointsChanged(value);
		remove => RemoveVerticalSnapPointsChanged(value);
	}
}
