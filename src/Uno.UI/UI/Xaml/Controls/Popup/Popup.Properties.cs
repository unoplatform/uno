using System;
using Uno.UI.Helpers.Boxes;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class Popup
{
	// IsOpen, Child and IsLightDismissEnabled come from DependencyPropertyMixinGenerator.

	/// <summary>
	/// Occurs when the IsOpen property is set to false.
	/// </summary>
	public event EventHandler<object> Closed;

	/// <summary>
	/// Occurs when the IsOpen property is set to true.
	/// </summary>
	public event EventHandler<object> Opened;

	/// <summary>
	/// Raised when the ActualPlacement property changes.
	/// </summary>
	public event EventHandler<object> ActualPlacementChanged;

	/// <summary>
	/// Gets or sets a value that specifies whether the area outside of a light-dismiss UI is darkened.
	/// </summary>
	public LightDismissOverlayMode LightDismissOverlayMode
	{
		get => (LightDismissOverlayMode)GetValue(LightDismissOverlayModeProperty);
		set => SetValue(LightDismissOverlayModeProperty, value);
	}

	/// <summary>
	/// Identifies the LightDismissOverlayMode dependency property.
	/// </summary>
	public static DependencyProperty LightDismissOverlayModeProperty { get; } =
		DependencyProperty.Register(
			nameof(LightDismissOverlayMode),
			typeof(LightDismissOverlayMode),
			typeof(Popup),
			new FrameworkPropertyMetadata(LightDismissOverlayMode.Off));

	/// <summary>
	/// Gets or sets the element to use as the popup's placement target.
	/// </summary>
	public FrameworkElement PlacementTarget
	{
		get => (FrameworkElement)GetValue(PlacementTargetProperty);
		set => SetValue(PlacementTargetProperty, value);
	}

	/// <summary>
	/// Identifies the PlacementTarget dependency property.
	/// </summary>
	public static DependencyProperty PlacementTargetProperty { get; } =
		DependencyProperty.Register(
			nameof(PlacementTarget),
			typeof(FrameworkElement),
			typeof(Popup),
			new FrameworkPropertyMetadata(
				default(FrameworkElement),
				FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext
			));

	/// <summary>
	/// Gets or sets the preferred placement to be used for the popup, in relation to its placement target.
	/// </summary>
	public PopupPlacementMode DesiredPlacement
	{
		get => (PopupPlacementMode)GetValue(DesiredPlacementProperty);
		set => SetValue(DesiredPlacementProperty, value);
	}

	/// <summary>
	/// Identifies the DesiredPlacement dependency property.
	/// </summary>
	public static DependencyProperty DesiredPlacementProperty { get; } =
		DependencyProperty.Register(
			nameof(DesiredPlacement),
			typeof(PopupPlacementMode),
			typeof(Popup),
			new FrameworkPropertyMetadata(PopupPlacementMode.Auto));

	/// <summary>
	/// Gets a value that indicates whether the popup is constrained to the bounds of its XAML root.
	/// </summary>
	public bool IsConstrainedToRootBounds => GetIsConstrainedToRootBounds();

	/// <summary>
	/// Gets or sets a value that indicates whether the popup should be shown within the bounds of the XAML root.
	/// </summary>
	public bool ShouldConstrainToRootBounds
	{
		get => (bool)GetValue(ShouldConstrainToRootBoundsProperty);
		set => SetValue(ShouldConstrainToRootBoundsProperty, Boxer.Box(value));
	}

	/// <summary>
	/// Identifies the ShouldConstrainToRootBounds dependency property.
	/// </summary>
	public static DependencyProperty ShouldConstrainToRootBoundsProperty { get; } =
		DependencyProperty.Register(
			nameof(ShouldConstrainToRootBounds),
			typeof(bool),
			typeof(Popup),
			new FrameworkPropertyMetadata(BoolBoxes.True));

	internal DependencyObject OverlayInputPassThroughElement
	{
		get => (DependencyObject)GetValue(OverlayInputPassThroughElementProperty);
		set => SetValue(OverlayInputPassThroughElementProperty, value);
	}

	internal static DependencyProperty OverlayInputPassThroughElementProperty { get; } =
		DependencyProperty.Register(nameof(OverlayInputPassThroughElement), typeof(DependencyObject), typeof(Popup), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

	/// <summary>
	/// Gets the actual placement of the popup, in relation to its placement target.
	/// </summary>
	public PopupPlacementMode ActualPlacement
	{
		get => m_actualPlacement;
		internal set
		{
			if (m_actualPlacement != value)
			{
				m_actualPlacement = value;

				ActualPlacementChanged?.Invoke(this, value);
			}
		}
	}

	/// <summary>
	/// Gets or sets the distance between the left side of the application window and the left side of the popup.
	/// </summary>
	public double HorizontalOffset
	{
		get => (double)GetValue(HorizontalOffsetProperty);
		set => SetValue(HorizontalOffsetProperty, Boxer.Box(value));
	}

	/// <summary>
	/// Gets the identifier for the HorizontalOffset dependency property.
	/// </summary>
	public static DependencyProperty HorizontalOffsetProperty { get; } =
		DependencyProperty.Register(
			nameof(HorizontalOffset),
			typeof(double),
			typeof(Popup),
			new FrameworkPropertyMetadata(
				DoubleBoxes.Zero,
				FrameworkPropertyMetadataOptions.AffectsMeasure,
				propertyChangedCallback: (s, e) => ((Popup)s)?.OnOffsetChanged()
			)
		);

	/// <summary>
	/// Gets or sets the distance between the top of the application window and the top of the popup.
	/// </summary>
	public double VerticalOffset
	{
		get => (double)GetValue(VerticalOffsetProperty);
		set => SetValue(VerticalOffsetProperty, Boxer.Box(value));
	}

	/// <summary>
	/// Gets the identifier for the VerticalOffset dependency property.
	/// </summary>
	public static DependencyProperty VerticalOffsetProperty { get; } =
		DependencyProperty.Register(
			nameof(VerticalOffset),
			typeof(double),
			typeof(Popup),
			new FrameworkPropertyMetadata(
				DoubleBoxes.Zero,
				FrameworkPropertyMetadataOptions.AffectsMeasure,
				propertyChangedCallback: (s, e) => ((Popup)s)?.OnOffsetChanged()
			)
		);
}
