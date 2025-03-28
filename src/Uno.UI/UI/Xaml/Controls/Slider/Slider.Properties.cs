using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml.Controls;

public partial class Slider
{
	/// <summary>
	/// Gets or sets the content for the control's header.
	/// </summary>
	public object Header
	{
		get => (object)GetValue(HeaderProperty);
		set => SetValue(HeaderProperty, value);
	}

	/// <summary>
	/// Identifies the Header dependency property.
	/// </summary>
	public static DependencyProperty HeaderProperty { get; } =
		DependencyProperty.Register(
			nameof(Header),
			typeof(object),
			typeof(Slider),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets the DataTemplate used to display the content of the control's header.
	/// </summary>
	public DataTemplate HeaderTemplate
	{
		get => (DataTemplate)GetValue(HeaderTemplateProperty);
		set => SetValue(HeaderTemplateProperty, value);
	}

	/// <summary>
	/// Identifies the HeaderTemplate dependency property.
	/// </summary>
	public static DependencyProperty HeaderTemplateProperty { get; } =
		DependencyProperty.Register(
			nameof(HeaderTemplate),
			typeof(DataTemplate),
			typeof(Slider),
			new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

	/// <summary>
	/// Gets or sets the value of the Slider while the user is interacting with it,
	/// before the value is snapped to either the tick or step value. The value
	/// the Slider snaps to is specified by the SnapsTo property.
	/// </summary>
	public double IntermediateValue
	{
		get => (double)GetValue(IntermediateValueProperty);
		set => SetValue(IntermediateValueProperty, value);
	}

	/// <summary>
	/// Identifies the IntermediateValue dependency property.
	/// </summary>
	public static DependencyProperty IntermediateValueProperty { get; } =
		DependencyProperty.Register(
			nameof(IntermediateValue),
			typeof(double),
			typeof(Slider),
			new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets or sets a value that indicates the direction of increasing value.
	/// </summary>
	public bool IsDirectionReversed
	{
		get => (bool)GetValue(IsDirectionReversedProperty);
		set => SetValue(IsDirectionReversedProperty, value);
	}

	/// <summary>
	/// Identifies the IsDirectionReversed dependency property.
	/// </summary>
	public static DependencyProperty IsDirectionReversedProperty { get; } =
		DependencyProperty.Register(
			nameof(IsDirectionReversed),
			typeof(bool),
			typeof(Slider),
			new FrameworkPropertyMetadata(false));

	/// <summary>
	/// Gets or sets a value that determines whether the slider value
	/// is shown in a tool tip for the Thumb component of the Slider.
	/// </summary>
	public bool IsThumbToolTipEnabled
	{
		get => (bool)GetValue(IsThumbToolTipEnabledProperty);
		set => SetValue(IsThumbToolTipEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the IsThumbToolTipEnabled dependency property.
	/// </summary>
	public static DependencyProperty IsThumbToolTipEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(IsThumbToolTipEnabled),
			typeof(bool),
			typeof(Slider),
			new FrameworkPropertyMetadata(true));

	/// <summary>
	/// Gets or sets the orientation of a Slider.
	/// </summary>
	/// <remarks>
	/// The default is Horizontal.
	/// </remarks>
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
			typeof(Slider),
			new FrameworkPropertyMetadata(Orientation.Horizontal));

	/// <summary>
	/// Gets or sets a value that indicates how the Slider
	/// conforms the thumb position to its steps or tick marks.
	/// </summary>
	public SliderSnapsTo SnapsTo
	{
		get => (SliderSnapsTo)GetValue(SnapsToProperty);
		set => SetValue(SnapsToProperty, value);
	}

	/// <summary>
	/// Identifies the SnapsTo dependency property.
	/// </summary>
	/// <remarks>
	/// Defaults to StepValues.
	/// </remarks>
	public static DependencyProperty SnapsToProperty { get; } =
		DependencyProperty.Register(
			nameof(SnapsTo),
			typeof(SliderSnapsTo),
			typeof(Slider),
			new FrameworkPropertyMetadata(SliderSnapsTo.StepValues));

	/// <summary>
	/// Gets or sets the value part of a value range that steps should be created for.
	/// </summary>
	public double StepFrequency
	{
		get => (double)GetValue(StepFrequencyProperty);
		set => SetValue(StepFrequencyProperty, value);
	}

	/// <summary>
	/// Identifies the StepFrequency dependency property.
	/// </summary>
	public static DependencyProperty StepFrequencyProperty { get; } =
		DependencyProperty.Register(
			nameof(StepFrequency),
			typeof(double),
			typeof(Slider),
			new FrameworkPropertyMetadata(1.0));

	/// <summary>
	/// Gets or sets the converter logic that converts the range value of the Slider into tool tip content.
	/// </summary>
	public IValueConverter ThumbToolTipValueConverter
	{
		get => (IValueConverter)GetValue(ThumbToolTipValueConverterProperty);
		set => SetValue(ThumbToolTipValueConverterProperty, value);
	}

	/// <summary>
	/// Identifies the ThumbToolTipValueConverter dependency property.
	/// </summary>
	public static DependencyProperty ThumbToolTipValueConverterProperty { get; } =
		DependencyProperty.Register(
			nameof(ThumbToolTipValueConverter),
			typeof(IValueConverter),
			typeof(Slider),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets the increment of the value range that ticks should be created for.
	/// </summary>
	/// <remarks>
	/// Default is 0.0.
	/// </remarks>
	public double TickFrequency
	{
		get => (double)GetValue(TickFrequencyProperty);
		set => SetValue(TickFrequencyProperty, value);
	}

	/// <summary>
	/// Identifies the TickFrequency dependency property.
	/// </summary>
	public static DependencyProperty TickFrequencyProperty { get; } =
		DependencyProperty.Register(
			nameof(TickFrequency),
			typeof(double),
			typeof(Slider),
			new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets or sets a value that indicates where to draw tick marks in relation to the track.
	/// </summary>
	/// <remarks>
	/// Default is Inline.
	/// </remarks>
	public TickPlacement TickPlacement
	{
		get => (TickPlacement)GetValue(TickPlacementProperty);
		set => SetValue(TickPlacementProperty, value);
	}

	/// <summary>
	/// Identifies the TickPlacement dependency property.
	/// </summary>
	public static DependencyProperty TickPlacementProperty { get; } =
		DependencyProperty.Register(
			nameof(TickPlacement),
			typeof(TickPlacement),
			typeof(Slider),
			new FrameworkPropertyMetadata(TickPlacement.Inline));
}
