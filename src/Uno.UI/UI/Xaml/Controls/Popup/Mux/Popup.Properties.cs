using System;
using Windows.UI.Xaml.Media.Animation;

namespace Windows.UI.Xaml.Controls.Primitives;

public partial class Popup
{
	/// <summary>
	/// Gets the actual placement of the popup, in relation to its placement target.
	/// </summary>
	public PopupPlacementMode ActualPlacement { get; set; }

	/// <summary>
	/// Gets or sets the content to be hosted in the popup.
	/// </summary>
	public UIElement Child
	{
		get => (UIElement)GetValue(ChildProperty);
		set => SetValue(ChildProperty, value);
	}

	/// <summary>
	/// Gets the identifier for the Child dependency property.
	/// </summary>
	public static DependencyProperty ChildProperty { get; } =
	   DependencyProperty.Register(
		   nameof(Child),
		   typeof(UIElement),
		   typeof(Popup),
		   new FrameworkPropertyMetadata(
			   null,
			   FrameworkPropertyMetadataOptions.ValueInheritsDataContext));

	/// <summary>
	/// Gets or sets the collection of Transition style elements
	/// that apply to child content of a Popup.
	/// </summary>
	public TransitionCollection ChildTransitions
	{
		get => (TransitionCollection)GetValue(ChildTransitionsProperty);
		set => SetValue(ChildTransitionsProperty, value);
	}

	/// <summary>
	/// Identifies the ChildTransitions dependency property.
	/// </summary>
	public static DependencyProperty ChildTransitionsProperty { get; } =
		DependencyProperty.Register(
			nameof(ChildTransitions),
			typeof(TransitionCollection),
			typeof(Popup),
			new FrameworkPropertyMetadata(default(TransitionCollection)));

	/// <summary>
	/// Gets or sets the preferred placement to be used for the popup,
	/// in relation to its placement target.
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
			new FrameworkPropertyMetadata(default(PopupPlacementMode)));

	/// <summary>
	/// Gets or sets the distance between the left side of the application
	/// window and the left side of the popup. 
	/// </summary>
	public double HorizontalOffset
	{
		get => (double)GetValue(HorizontalOffsetProperty);
		set => SetValue(HorizontalOffsetProperty, value);
	}

	/// <summary>
	/// Gets the identifier for the HorizontalOffset dependency property.
	/// </summary>
	public static DependencyProperty HorizontalOffsetProperty { get; } =
		DependencyProperty.Register(
			nameof(HorizontalOffset),
			typeof(double),
			typeof(Popup),
			new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets a value that indicates whether the popup is shown within
	/// the bounds of the XAML root.
	/// </summary>
	public bool IsConstrainedToRootBounds { get; private set; }

	/// <summary>
	/// Gets or sets a value that determines how the Popup can be dismissed.
	/// </summary>
	public bool IsLightDismissEnabled
	{
		get => (bool)GetValue(IsLightDismissEnabledProperty);
		set => SetValue(IsLightDismissEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the IsLightDismissEnabled dependency property.
	/// </summary>
	public static DependencyProperty IsLightDismissEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(IsLightDismissEnabled),
			typeof(bool),
			typeof(Popup),
			new FrameworkPropertyMetadata(false));

	/// <summary>
	/// Gets or sets whether the popup is currently displayed on the screen.
	/// </summary>
	public bool IsOpen
	{
		get => (bool)GetValue(IsOpenProperty);
		set => SetValue(IsOpenProperty, value);
	}

	/// <summary>
	/// Gets the identifier for the IsOpen dependency property.
	/// </summary>
	public static DependencyProperty IsOpenProperty { get; } =
		DependencyProperty.Register(
			nameof(IsOpen),
			typeof(bool),
			typeof(Popup),
			new FrameworkPropertyMetadata(false));

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
		new FrameworkPropertyMetadata(default(LightDismissOverlayMode)));

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
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets a value that indicates whether the popup should
	/// be shown within the bounds of the XAML root.
	/// </summary>
	public bool ShouldConstrainToRootBounds
	{
		get => (bool)GetValue(ShouldConstrainToRootBoundsProperty);
		set => SetValue(ShouldConstrainToRootBoundsProperty, value);
	}

	/// <summary>
	/// Identifies the ShouldConstrainToRootBounds dependency property.
	/// </summary>
	public static DependencyProperty ShouldConstrainToRootBoundsProperty { get; } =
		DependencyProperty.Register(
			nameof(ShouldConstrainToRootBounds),
			typeof(bool),
			typeof(Popup),
			new FrameworkPropertyMetadata(default(bool)));

	/// <summary>
	/// Gets or sets the distance between the top of the application window
	/// and the top of the popup.
	/// </summary>
	public double VerticalOffset
	{
		get => (double)GetValue(VerticalOffsetProperty);
		set => SetValue(VerticalOffsetProperty, value);
	}

	/// <summary>
	/// Gets the identifier for the VerticalOffset dependency property.
	/// </summary>
	public static DependencyProperty VerticalOffsetProperty { get; } =
		DependencyProperty.Register(
			nameof(VerticalOffset),
			typeof(double),
			typeof(Popup),
			new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Occurs when the actual placement mode of the popup changes.
	/// </summary>
	public event EventHandler<object> ActualPlacementChanged;

	/// <summary>
	/// Fires when the IsOpen property is set to false.
	/// </summary>
	public event EventHandler<object> Closed;

	/// <summary>
	/// Fires when the IsOpen property is set to true.
	/// </summary>
	public event EventHandler<object> Opened;
}
