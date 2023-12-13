using System;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls.Primitives;

//TODO:MZ: Verify default values, change callbacks, affects measure/arrange
partial class FlyoutBase
{
	/// <summary>
	/// Gets or sets a value that indicates whether
	/// the element automatically gets focus when the user interacts with it.
	/// </summary>
	public bool AllowFocusOnInteraction
	{
		get => (bool)GetValue(AllowFocusOnInteractionProperty);
		set => SetValue(AllowFocusOnInteractionProperty, value);
	}

	/// <summary>
	/// Identifies the AllowFocusOnInteraction dependency property.
	/// </summary>
	public static DependencyProperty AllowFocusOnInteractionProperty { get; } =
		DependencyProperty.Register(
			nameof(AllowFocusOnInteraction),
			typeof(bool),
			typeof(FlyoutBase),
			new FrameworkPropertyMetadata(true));

	/// <summary>
	/// Gets or sets a value that specifies whether the control
	/// can receive focus when it's disabled.
	/// </summary>
	public bool AllowFocusWhenDisabled
	{
		get => (bool)GetValue(AllowFocusWhenDisabledProperty);
		set => SetValue(AllowFocusWhenDisabledProperty, value);
	}

	/// <summary>
	/// Identifies the AllowFocusWhenDisabled dependency property.
	/// </summary>
	public static DependencyProperty AllowFocusWhenDisabledProperty { get; } =
		DependencyProperty.Register(
			nameof(AllowFocusWhenDisabled),
			typeof(bool),
			typeof(FlyoutBase),
			new FrameworkPropertyMetadata(false));

	/// <summary>
	/// Gets or sets a value that indicates whether animations
	/// are played when the flyout is opened or closed.
	/// </summary>
	public bool AreOpenCloseAnimationsEnabled
	{
		get => (bool)GetValue(AreOpenCloseAnimationsEnabledProperty);
		set => SetValue(AreOpenCloseAnimationsEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the AreOpenCloseAnimationsEnabled dependency property.
	/// </summary>
	public static DependencyProperty AreOpenCloseAnimationsEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(AreOpenCloseAnimationsEnabled),
			typeof(bool),
			typeof(FlyoutBase),
			new FrameworkPropertyMetadata(true));

	/// <summary>
	/// Gets the flyout associated with the specified element.
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	public static FlyoutBase GetAttachedFlyout(FrameworkElement element) =>
		(FlyoutBase)element.GetValue(AttachedFlyoutProperty);

	/// <summary>
	/// Associates the specified flyout with the specified FrameworkElement.
	/// </summary>
	/// <param name="element"></param>
	/// <param name="value"></param>
	public static void SetAttachedFlyout(FrameworkElement element, FlyoutBase value) =>
		element.SetValue(AttachedFlyoutProperty, value);

	/// <summary>
	/// Identifies the FlyoutBase.AttachedFlyout XAML attached property.
	/// </summary>
	public static DependencyProperty AttachedFlyoutProperty { get; } =
		DependencyProperty.RegisterAttached(
			"AttachedFlyout",
			typeof(FlyoutBase),
			typeof(FlyoutBase),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets a value that specifies the control's preference
	/// for whether it plays sounds.
	/// </summary>
	public ElementSoundMode ElementSoundMode
	{
		get => (ElementSoundMode)GetValue(ElementSoundModeProperty);
		set => SetValue(ElementSoundModeProperty, value);
	}

	/// <summary>
	/// Identifies the ElementSoundMode dependency property.
	/// </summary>
	public static DependencyProperty ElementSoundModeProperty { get; } =
		DependencyProperty.Register(nameof(ElementSoundMode), typeof(ElementSoundMode), typeof(FlyoutBase), new FrameworkPropertyMetadata(ElementSoundMode.Default));

	/// <summary>
	/// Gets a value that indicates whether the input device used
	/// to open the flyout does not easily open the secondary commands.
	/// </summary>
	public bool InputDevicePrefersPrimaryCommands => (bool)GetValue(InputDevicePrefersPrimaryCommandsProperty);
	
	/// <summary>
	/// Identifies the InputDevicePrefersPrimaryCommands dependency property.
	/// </summary>
	public static DependencyProperty InputDevicePrefersPrimaryCommandsProperty { get; } =
		DependencyProperty.Register(nameof(InputDevicePrefersPrimaryCommands), typeof(bool), typeof(FlyoutBase), new FrameworkPropertyMetadata(false));

	/// <summary>
	/// Gets a value that indicates whether the flyout is open.
	/// </summary>
	public bool IsOpen => IsOpenImpl(); //TODO:MZ: Sync with IsOpenProperty somehow

	/// <summary>
	/// Identifies the IsOpen dependency property.
	/// </summary>
	public static DependencyProperty IsOpenProperty { get; } =
		DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(FlyoutBase), new FrameworkPropertyMetadata(false));

	/// <summary>
	/// Gets or sets a value that specifies whether
	/// the area outside of a light-dismiss UI is darkened.
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
			typeof(FlyoutBase),
			new FrameworkPropertyMetadata(LightDismissOverlayMode.Auto));

	/// <summary>
	/// Gets or sets an element that should receive pointer input events
	/// even when underneath the flyout's overlay.
	/// </summary>
	public DependencyObject OverlayInputPassThroughElement
	{
		get => (DependencyObject)GetValue(OverlayInputPassThroughElementProperty);
		set => SetValue(OverlayInputPassThroughElementProperty, value);
	}

	/// <summary>
	/// Identifies the OverlayInputPassThroughElement dependency property.
	/// </summary>
	public static DependencyProperty OverlayInputPassThroughElementProperty { get; } =
		DependencyProperty.Register(
			nameof(OverlayInputPassThroughElement),
			typeof(DependencyObject),
			typeof(FlyoutBase),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets the default placement to be used for the flyout, in relation to its placement target.
	/// </summary>
	public FlyoutPlacementMode Placement
	{
		get => (FlyoutPlacementMode)GetValue(PlacementProperty);
		set => SetValue(PlacementProperty, value);
	}

	/// <summary>
	/// Identifies the Placement dependency property
	/// </summary>
	public static DependencyProperty PlacementProperty { get; } =
		DependencyProperty.Register(
			nameof(Placement),
			typeof(FlyoutPlacementMode),
			typeof(FlyoutBase),
			new FrameworkPropertyMetadata(FlyoutPlacementMode.Auto));

	/// <summary>
	/// Gets or sets a value that indicates whether the flyout
	/// should be shown within the bounds of the XAML root.
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
			typeof(FlyoutBase),
			new FrameworkPropertyMetadata(false));

	/// <summary>
	/// Gets or sets a value that indicates how a flyout behaves when shown.
	/// </summary>
	public FlyoutShowMode ShowMode
	{
		get => (FlyoutShowMode)GetValue(ShowModeProperty);
		set => SetValue(ShowModeProperty, value);
	}

	/// <summary>
	/// Identifies the ShowMode dependency property.
	/// </summary>
	public static DependencyProperty ShowModeProperty { get; } =
		DependencyProperty.Register(
			nameof(ShowMode),
			typeof(FlyoutShowMode),
			typeof(FlyoutBase),
			new FrameworkPropertyMetadata(FlyoutShowMode.Auto));

	/// <summary>
	/// Gets the element to use as the flyout's placement target.
	/// </summary>
	public FrameworkElement Target
	{
		get => (FrameworkElement)GetValue(TargetProperty);
		private set => SetValue(TargetProperty, value);
	}

	/// <summary>
	/// Identifies the Target dependency property.
	/// </summary>
	public static DependencyProperty TargetProperty { get; } =
		DependencyProperty.Register(
			nameof(Target),
			typeof(FrameworkElement),
			typeof(FlyoutBase),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Occurs when the flyout is hidden.
	/// </summary>
	public event EventHandler<object> Closed;

	/// <summary>
	/// Occurs when the flyout starts to be hidden.
	/// </summary>
	public event TypedEventHandler<FlyoutBase, FlyoutBaseClosingEventArgs> Closing;

	/// <summary>
	/// Occurs when the flyout is shown.
	/// </summary>
	public event EventHandler<object> Opened;

	/// <summary>
	/// Occurs before the flyout is shown.
	/// </summary>
	public event EventHandler<object> Opening;
}
