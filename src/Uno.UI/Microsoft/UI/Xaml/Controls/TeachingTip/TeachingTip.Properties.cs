// MUX Reference TeachingTip.properties.cpp, commit de78834

#nullable enable

using System.Windows.Input;
using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

public partial class TeachingTip
{
	public ICommand ActionButtonCommand
	{
		get => (ICommand)GetValue(ActionButtonCommandProperty);
		set => SetValue(ActionButtonCommandProperty, value);
	}

	public static DependencyProperty ActionButtonCommandProperty { get; } =
		DependencyProperty.Register(nameof(ActionButtonCommand), typeof(ICommand), typeof(TeachingTip), new PropertyMetadata(null, OnPropertyChanged));

	public object ActionButtonCommandParameter
	{
		get => (object)GetValue(ActionButtonCommandParameterProperty);
		set => SetValue(ActionButtonCommandParameterProperty, value);
	}

	public static DependencyProperty ActionButtonCommandParameterProperty { get; } =
		DependencyProperty.Register(nameof(ActionButtonCommandParameter), typeof(object), typeof(TeachingTip), new PropertyMetadata(null, OnPropertyChanged));

	public object ActionButtonContent
	{
		get => (object)GetValue(ActionButtonContentProperty);
		set => SetValue(ActionButtonContentProperty, value);
	}

	public static DependencyProperty ActionButtonContentProperty { get; } =
		DependencyProperty.Register(nameof(ActionButtonContent), typeof(object), typeof(TeachingTip), new PropertyMetadata(null, OnPropertyChanged));

	public Style ActionButtonStyle
	{
		get => (Style)GetValue(ActionButtonStyleProperty);
		set => SetValue(ActionButtonStyleProperty, value);
	}

	public static DependencyProperty ActionButtonStyleProperty { get; } =
		DependencyProperty.Register(nameof(ActionButtonStyle), typeof(Style), typeof(TeachingTip), new PropertyMetadata(null, OnPropertyChanged));

	public ICommand CloseButtonCommand
	{
		get => (ICommand)GetValue(CloseButtonCommandProperty);
		set => SetValue(CloseButtonCommandProperty, value);
	}

	public static DependencyProperty CloseButtonCommandProperty { get; } =
		DependencyProperty.Register(nameof(CloseButtonCommand), typeof(ICommand), typeof(TeachingTip), new PropertyMetadata(null, OnPropertyChanged));

	public object CloseButtonCommandParameter
	{
		get => (object)GetValue(CloseButtonCommandParameterProperty);
		set => SetValue(CloseButtonCommandParameterProperty, value);
	}

	public static DependencyProperty CloseButtonCommandParameterProperty { get; } =
		DependencyProperty.Register(nameof(CloseButtonCommandParameter), typeof(object), typeof(TeachingTip), new PropertyMetadata(null, OnPropertyChanged));

	public object CloseButtonContent
	{
		get => (object)GetValue(CloseButtonContentProperty);
		set => SetValue(CloseButtonContentProperty, value);
	}

	public static DependencyProperty CloseButtonContentProperty { get; } =
		DependencyProperty.Register(nameof(CloseButtonContent), typeof(object), typeof(TeachingTip), new PropertyMetadata(null, OnPropertyChanged));

	public Style CloseButtonStyle
	{
		get => (Style)GetValue(CloseButtonStyleProperty);
		set => SetValue(CloseButtonStyleProperty, value);
	}

	public static DependencyProperty CloseButtonStyleProperty { get; } =
		DependencyProperty.Register(nameof(CloseButtonStyle), typeof(Style), typeof(TeachingTip), new PropertyMetadata(null, OnPropertyChanged));

	public UIElement HeroContent
	{
		get => (UIElement)GetValue(HeroContentProperty);
		set => SetValue(HeroContentProperty, value);
	}

	public static DependencyProperty HeroContentProperty { get; } =
		DependencyProperty.Register(nameof(HeroContent), typeof(UIElement), typeof(TeachingTip), new PropertyMetadata(null, OnPropertyChanged));

	public TeachingTipHeroContentPlacementMode HeroContentPlacement
	{
		get => (TeachingTipHeroContentPlacementMode)GetValue(HeroContentPlacementProperty);
		set => SetValue(HeroContentPlacementProperty, value);
	}

	public static DependencyProperty HeroContentPlacementProperty { get; } =
		DependencyProperty.Register(nameof(HeroContentPlacement), typeof(TeachingTipHeroContentPlacementMode), typeof(TeachingTip), new PropertyMetadata(TeachingTipHeroContentPlacementMode.Auto, OnPropertyChanged));

	public IconSource IconSource
	{
		get => (IconSource)GetValue(IconSourceProperty);
		set => SetValue(IconSourceProperty, value);
	}

	public static DependencyProperty IconSourceProperty { get; } =
		DependencyProperty.Register(nameof(IconSource), typeof(IconSource), typeof(TeachingTip), new PropertyMetadata(null, OnPropertyChanged));

	public bool IsLightDismissEnabled
	{
		get => (bool)GetValue(IsLightDismissEnabledProperty);
		set => SetValue(IsLightDismissEnabledProperty, value);
	}

	public static DependencyProperty IsLightDismissEnabledProperty { get; } =
		DependencyProperty.Register(nameof(IsLightDismissEnabled), typeof(bool), typeof(TeachingTip), new PropertyMetadata(false, OnPropertyChanged));

	public bool IsOpen
	{
		get => (bool)GetValue(IsOpenProperty);
		set => SetValue(IsOpenProperty, value);
	}

	public static DependencyProperty IsOpenProperty { get; } =
		DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(TeachingTip), new PropertyMetadata(false, OnPropertyChanged));

	public Thickness PlacementMargin
	{
		get => (Thickness)GetValue(PlacementMarginProperty);
		set => SetValue(PlacementMarginProperty, value);
	}

	public static DependencyProperty PlacementMarginProperty { get; } =
		DependencyProperty.Register(nameof(PlacementMargin), typeof(Thickness), typeof(TeachingTip), new PropertyMetadata(default(Thickness), OnPropertyChanged));

	public TeachingTipPlacementMode PreferredPlacement
	{
		get => (TeachingTipPlacementMode)GetValue(PreferredPlacementProperty);
		set => SetValue(PreferredPlacementProperty, value);
	}

	public static DependencyProperty PreferredPlacementProperty { get; } =
		DependencyProperty.Register(nameof(PreferredPlacement), typeof(TeachingTipPlacementMode), typeof(TeachingTip), new PropertyMetadata(TeachingTipPlacementMode.Auto, OnPropertyChanged));

	public bool ShouldConstrainToRootBounds
	{
		get => (bool)GetValue(ShouldConstrainToRootBoundsProperty);
		set => SetValue(ShouldConstrainToRootBoundsProperty, value);
	}

	public static DependencyProperty ShouldConstrainToRootBoundsProperty { get; } =
		DependencyProperty.Register(nameof(ShouldConstrainToRootBounds), typeof(bool), typeof(TeachingTip), new PropertyMetadata(true, OnPropertyChanged));

	public string Subtitle
	{
		get => (string)GetValue(SubtitleProperty);
		set => SetValue(SubtitleProperty, value);
	}

	public static DependencyProperty SubtitleProperty { get; } =
		DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(TeachingTip), new PropertyMetadata(null, OnPropertyChanged));

	public TeachingTipTailVisibility TailVisibility
	{
		get => (TeachingTipTailVisibility)GetValue(TailVisibilityProperty);
		set => SetValue(TailVisibilityProperty, value);
	}

	public static DependencyProperty TailVisibilityProperty { get; } =
		DependencyProperty.Register(nameof(TailVisibility), typeof(TeachingTipTailVisibility), typeof(TeachingTip), new PropertyMetadata(TeachingTipTailVisibility.Auto, OnPropertyChanged));

	public FrameworkElement Target
	{
		get => (FrameworkElement)GetValue(TargetProperty);
		set => SetValue(TargetProperty, value);
	}

	public static DependencyProperty TargetProperty { get; } =
		DependencyProperty.Register(nameof(Target), typeof(FrameworkElement), typeof(TeachingTip), new PropertyMetadata(null, OnPropertyChanged));

	public TeachingTipTemplateSettings TemplateSettings
	{
		get => (TeachingTipTemplateSettings)GetValue(TemplateSettingsProperty);
		set => SetValue(TemplateSettingsProperty, value);
	}

	public static DependencyProperty TemplateSettingsProperty { get; } =
		DependencyProperty.Register(nameof(TemplateSettings), typeof(TeachingTipTemplateSettings), typeof(TeachingTip), new PropertyMetadata(null, OnPropertyChanged));

	public string Title
	{
		get => (string)GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}

	public static DependencyProperty TitleProperty { get; } =
		DependencyProperty.Register(nameof(Title), typeof(string), typeof(TeachingTip), new PropertyMetadata(null, OnPropertyChanged));

	// WinUI has a separate property changed method per property, but they all have the same body.
	// Simplified this as a single method.

	private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (TeachingTip)sender;
		owner.OnPropertyChanged(args);
	}
}
