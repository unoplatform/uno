// MUX Reference TeachingTip.properties.cpp, commit de78834

#nullable enable

using System.Windows.Input;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

partial class TeachingTip
{
	/// <summary>
	/// Gets or sets the command to invoke when the action button is clicked.
	/// </summary>
	public ICommand ActionButtonCommand
	{
		get => (ICommand)GetValue(ActionButtonCommandProperty);
		set => SetValue(ActionButtonCommandProperty, value);
	}

	/// <summary>
	/// Identifies the ActionButtonCommand dependency property.
	/// </summary>
	public static DependencyProperty ActionButtonCommandProperty { get; } =
		DependencyProperty.Register(nameof(ActionButtonCommand), typeof(ICommand), typeof(TeachingTip), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the parameter to pass to the command for the action button.
	/// </summary>
	public object ActionButtonCommandParameter
	{
		get => (object)GetValue(ActionButtonCommandParameterProperty);
		set => SetValue(ActionButtonCommandParameterProperty, value);
	}

	/// <summary>
	/// Gets or sets the parameter to pass to the command for the action button.
	/// </summary>
	public static DependencyProperty ActionButtonCommandParameterProperty { get; } =
		DependencyProperty.Register(nameof(ActionButtonCommandParameter), typeof(object), typeof(TeachingTip), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the text of the teaching tip's action button.
	/// </summary>
	public object ActionButtonContent
	{
		get => (object)GetValue(ActionButtonContentProperty);
		set => SetValue(ActionButtonContentProperty, value);
	}

	/// <summary>
	/// Identifies the ActionButtonContent dependency property.
	/// </summary>
	public static DependencyProperty ActionButtonContentProperty { get; } =
		DependencyProperty.Register(nameof(ActionButtonContent), typeof(object), typeof(TeachingTip), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the Style to apply to the action button.
	/// </summary>
	public Style ActionButtonStyle
	{
		get => (Style)GetValue(ActionButtonStyleProperty);
		set => SetValue(ActionButtonStyleProperty, value);
	}

	/// <summary>
	/// Identifies the ActionButtonStyle dependency property.
	/// </summary>
	public static DependencyProperty ActionButtonStyleProperty { get; } =
		DependencyProperty.Register(nameof(ActionButtonStyle), typeof(Style), typeof(TeachingTip), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the command to invoke when the close button is clicked.
	/// </summary>
	public ICommand CloseButtonCommand
	{
		get => (ICommand)GetValue(CloseButtonCommandProperty);
		set => SetValue(CloseButtonCommandProperty, value);
	}

	/// <summary>
	/// Identifies the CloseButtonCommand dependency property.
	/// </summary>
	public static DependencyProperty CloseButtonCommandProperty { get; } =
		DependencyProperty.Register(nameof(CloseButtonCommand), typeof(ICommand), typeof(TeachingTip), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the parameter to pass to the command for the close button.
	/// </summary>
	public object CloseButtonCommandParameter
	{
		get => (object)GetValue(CloseButtonCommandParameterProperty);
		set => SetValue(CloseButtonCommandParameterProperty, value);
	}

	/// <summary>
	/// Identifies the CloseButtonCommandParameter dependency property.
	/// </summary>
	public static DependencyProperty CloseButtonCommandParameterProperty { get; } =
		DependencyProperty.Register(nameof(CloseButtonCommandParameter), typeof(object), typeof(TeachingTip), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the content of the teaching tip's close button.
	/// </summary>
	public object CloseButtonContent
	{
		get => (object)GetValue(CloseButtonContentProperty);
		set => SetValue(CloseButtonContentProperty, value);
	}

	/// <summary>
	/// Identifies the CloseButtonCommand dependency property.
	/// </summary>
	public static DependencyProperty CloseButtonContentProperty { get; } =
		DependencyProperty.Register(nameof(CloseButtonContent), typeof(object), typeof(TeachingTip), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the Style to apply to the teaching tip's close button.
	/// </summary>
	public Style CloseButtonStyle
	{
		get => (Style)GetValue(CloseButtonStyleProperty);
		set => SetValue(CloseButtonStyleProperty, value);
	}

	/// <summary>
	/// Identifies the CloseButtonStyle dependency property.
	/// </summary>
	public static DependencyProperty CloseButtonStyleProperty { get; } =
		DependencyProperty.Register(nameof(CloseButtonStyle), typeof(Style), typeof(TeachingTip), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Border-to-border graphic content displayed in the header or footer of the teaching tip. Will appear opposite of the tail in targeted teaching tips unless otherwise set.
	/// </summary>
	public UIElement HeroContent
	{
		get => (UIElement)GetValue(HeroContentProperty);
		set => SetValue(HeroContentProperty, value);
	}

	/// <summary>
	/// Identifies the HeroContentPlacement dependency property.
	/// </summary>
	public static DependencyProperty HeroContentProperty { get; } =
		DependencyProperty.Register(nameof(HeroContent), typeof(UIElement), typeof(TeachingTip), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Placement of the hero content within the teaching tip.
	/// </summary>
	public TeachingTipHeroContentPlacementMode HeroContentPlacement
	{
		get => (TeachingTipHeroContentPlacementMode)GetValue(HeroContentPlacementProperty);
		set => SetValue(HeroContentPlacementProperty, value);
	}

	/// <summary>
	/// Identifies the HeroContentPlacement dependency property.
	/// </summary>
	public static DependencyProperty HeroContentPlacementProperty { get; } =
		DependencyProperty.Register(nameof(HeroContentPlacement), typeof(TeachingTipHeroContentPlacementMode), typeof(TeachingTip), new FrameworkPropertyMetadata(TeachingTipHeroContentPlacementMode.Auto, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the graphic content to appear alongside the title and subtitle
	/// </summary>
	public IconSource IconSource
	{
		get => (IconSource)GetValue(IconSourceProperty);
		set => SetValue(IconSourceProperty, value);
	}

	/// <summary>
	/// Identifies the IconSource dependency property.
	/// </summary>
	public static DependencyProperty IconSourceProperty { get; } =
		DependencyProperty.Register(nameof(IconSource), typeof(IconSource), typeof(TeachingTip), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Enables light-dismiss functionality so that a teaching tip will dismiss when a user scrolls or interacts with other elements of the application.
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
		DependencyProperty.Register(nameof(IsLightDismissEnabled), typeof(bool), typeof(TeachingTip), new FrameworkPropertyMetadata(false, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates whether the teaching tip is open.
	/// </summary>
	public bool IsOpen
	{
		get => (bool)GetValue(IsOpenProperty);
		set => SetValue(IsOpenProperty, value);
	}

	/// <summary>
	/// Identifies the IsOpen dependency property.
	/// </summary>
	public static DependencyProperty IsOpenProperty { get; } =
		DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(TeachingTip), new FrameworkPropertyMetadata(false, OnPropertyChanged));

	/// <summary>
	/// Adds a margin between a targeted teaching tip and its target or between a non-targeted teaching tip and the xaml root.
	/// </summary>
	public Thickness PlacementMargin
	{
		get => (Thickness)GetValue(PlacementMarginProperty);
		set => SetValue(PlacementMarginProperty, value);
	}

	/// <summary>
	/// Identifies the PlacementMargin dependency property.
	/// </summary>
	public static DependencyProperty PlacementMarginProperty { get; } =
		DependencyProperty.Register(nameof(PlacementMargin), typeof(Thickness), typeof(TeachingTip), new FrameworkPropertyMetadata(default(Thickness), OnPropertyChanged));

	/// <summary>
	/// Preferred placement to be used for the teaching tip. If there is not enough space to show at the preferred placement, a new placement will be automatically chosen.
	/// Placement is relative to its target if Target is non-null or to the parent window of the teaching tip if Target is null.
	/// </summary>
	public TeachingTipPlacementMode PreferredPlacement
	{
		get => (TeachingTipPlacementMode)GetValue(PreferredPlacementProperty);
		set => SetValue(PreferredPlacementProperty, value);
	}

	/// <summary>
	/// Identifies the PreferredPlacement dependency property.
	/// </summary>
	public static DependencyProperty PreferredPlacementProperty { get; } =
		DependencyProperty.Register(nameof(PreferredPlacement), typeof(TeachingTipPlacementMode), typeof(TeachingTip), new FrameworkPropertyMetadata(TeachingTipPlacementMode.Auto, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates whether the teaching tip will constrain to the bounds of its xaml root.
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
		DependencyProperty.Register(nameof(ShouldConstrainToRootBounds), typeof(bool), typeof(TeachingTip), new FrameworkPropertyMetadata(true, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates whether the teaching tip will constrain to the bounds of its xaml root.
	/// </summary>
	public string Subtitle
	{
		get => (string)GetValue(SubtitleProperty);
		set => SetValue(SubtitleProperty, value);
	}

	/// <summary>
	/// Identifies the Subtitle dependency property.
	/// </summary>
	public static DependencyProperty SubtitleProperty { get; } =
		DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(TeachingTip), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Toggles collapse of a teaching tip's tail. Can be used to override auto behavior to make a tail visible on a non-targeted teaching tip and hidden on a targeted teaching tip.
	/// </summary>
	public TeachingTipTailVisibility TailVisibility
	{
		get => (TeachingTipTailVisibility)GetValue(TailVisibilityProperty);
		set => SetValue(TailVisibilityProperty, value);
	}

	/// <summary>
	/// Identifies the TailVisibility dependency property.
	/// </summary>
	public static DependencyProperty TailVisibilityProperty { get; } =
		DependencyProperty.Register(nameof(TailVisibility), typeof(TeachingTipTailVisibility), typeof(TeachingTip), new FrameworkPropertyMetadata(TeachingTipTailVisibility.Auto, OnPropertyChanged));

	/// <summary>
	/// Sets the target for a teaching tip to position itself relative to and point at with its tail.
	/// </summary>
	public FrameworkElement Target
	{
		get => (FrameworkElement)GetValue(TargetProperty);
		set => SetValue(TargetProperty, value);
	}

	/// <summary>
	/// Identifies the Target dependency property.
	/// </summary>
	public static DependencyProperty TargetProperty { get; } =
		DependencyProperty.Register(nameof(Target), typeof(FrameworkElement), typeof(TeachingTip), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Provides calculated values that can be referenced as TemplatedParent sources when defining templates for a TeachingTip. Not intended for general use.
	/// </summary>
	public TeachingTipTemplateSettings TemplateSettings
	{
		get => (TeachingTipTemplateSettings)GetValue(TemplateSettingsProperty);
		private set => SetValue(TemplateSettingsProperty, value);
	}

	/// <summary>
	/// Identifies the TemplateSettings dependency property.
	/// </summary>
	public static DependencyProperty TemplateSettingsProperty { get; } =
		DependencyProperty.Register(nameof(TemplateSettings), typeof(TeachingTipTemplateSettings), typeof(TeachingTip), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the title of the teaching tip.
	/// </summary>
	public string Title
	{
		get => (string)GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}

	/// <summary>
	/// Identifies the Title dependency property.
	/// </summary>
	public static DependencyProperty TitleProperty { get; } =
		DependencyProperty.Register(nameof(Title), typeof(string), typeof(TeachingTip), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	// WinUI has a separate property changed method per property, but they all have the same body.
	// Simplified this as a single method.

	private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (TeachingTip)sender;
		owner.OnPropertyChanged(args);
	}

	/// <summary>
	/// Occurs after the action button is clicked.
	/// </summary>
	public event TypedEventHandler<TeachingTip, object> ActionButtonClick;

	/// <summary>
	/// Occurs after the close button is clicked.
	/// </summary>
	public event TypedEventHandler<TeachingTip, object> CloseButtonClick;

	/// <summary>
	/// Occurs after the tip is closed.
	/// </summary>
	public event TypedEventHandler<TeachingTip, TeachingTipClosedEventArgs> Closed;

	/// <summary>
	/// Occurs just before the tip begins to close.
	/// </summary>
	public event TypedEventHandler<TeachingTip, TeachingTipClosingEventArgs> Closing;
}
