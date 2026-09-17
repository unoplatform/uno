using System;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

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
	[GeneratedDependencyProperty(DefaultValue = false, ChangedCallback = true)]
	public partial bool AreScrollSnapPointsRegular { get; set; }

	/// <summary>
	/// Gets a value that indicates whether the vertical snap points
	/// for the StackPanel are equidistant from each other.
	/// </summary>
	public bool AreVerticalSnapPointsRegular => AreVerticalSnapPointsRegularImpl();

	/// <summary>
	/// Gets or sets a value that indicates how far the background
	/// extends in relation to this element's border.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = default(BackgroundSizing), ChangedCallback = true)]
	public partial BackgroundSizing BackgroundSizing { get; set; }

	/// <summary>
	/// Gets or sets a brush that describes the border fill of the panel.
	/// </summary>
	[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnBorderBrushPropertyChanged), Options = FrameworkPropertyMetadataOptions.ValueInheritsDataContext)]
	public partial Brush BorderBrush { get; set; }

	private static Brush GetBorderBrushDefaultValue() => SolidColorBrushHelper.Transparent;

	/// <summary>
	/// Gets or sets the border thickness of the panel.
	/// </summary>
	[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnBorderThicknessPropertyChanged), Options = FrameworkPropertyMetadataOptions.AffectsMeasure)]
	public partial Thickness BorderThickness { get; set; }

	private static Thickness GetBorderThicknessDefaultValue() => Thickness.Empty;

	/// <summary>
	/// Gets or sets the radius for the corners of the panel's border.
	/// </summary>
	[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnCornerRadiusPropertyChanged))]
	public partial CornerRadius CornerRadius { get; set; }

	private static CornerRadius GetCornerRadiusDefaultValue() => CornerRadius.None;

	/// <summary>
	/// Gets or sets the dimension by which child elements are stacked.
	/// </summary>
	[GeneratedDependencyProperty(Options = FrameworkPropertyMetadataOptions.AffectsMeasure)]
	public partial Orientation Orientation { get; set; }

	private static Orientation GetOrientationDefaultValue() => Orientation.Vertical;

	/// <summary>
	/// Gets or sets the distance between the border and its child object.
	/// </summary>
	[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnPaddingPropertyChanged), Options = FrameworkPropertyMetadataOptions.AffectsMeasure)]
	public partial Thickness Padding { get; set; }

	private static Thickness GetPaddingDefaultValue() => default;

	/// <summary>
	/// Gets or sets a uniform distance (in pixels) between stacked items.
	/// It is applied in the direction of the StackPanel's Orientation.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = 0.0, Options = FrameworkPropertyMetadataOptions.AffectsMeasure)]
	public partial double Spacing { get; set; }

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
