using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
namespace Microsoft.UI.Xaml.Controls;

public partial class SplitView
{
	public double CompactPaneLength
	{
		get => (double)this.GetValue(CompactPaneLengthProperty);
		set => this.SetValue(CompactPaneLengthProperty, value);
	}

	public static DependencyProperty CompactPaneLengthProperty { get; } =
		DependencyProperty.Register(
			"CompactPaneLength",
			typeof(double), typeof(SplitView),
			new FrameworkPropertyMetadata(
				defaultValue: (double)48,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure,
				(s, e) => ((SplitView)s)?.OnPropertyChanged(e)
			)
		);

	public UIElement Content
	{
		get => (UIElement)this.GetValue(ContentProperty);
		set => this.SetValue(ContentProperty, value);
	}

	public static DependencyProperty ContentProperty { get; } =
		DependencyProperty.Register(
			"Content",
			typeof(UIElement),
			typeof(SplitView),
			new FrameworkPropertyMetadata(
				defaultValue: null,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure,
				(s, e) => ((SplitView)s)?.OnContentChanged(e)
			)
		);

	public UIElement Pane
	{
		get => (UIElement)this.GetValue(PaneProperty);
		set => this.SetValue(PaneProperty, value);
	}

	public static DependencyProperty PaneProperty { get; } =
		DependencyProperty.Register(
			"Pane",
			typeof(UIElement),
			typeof(SplitView),
			new FrameworkPropertyMetadata(
				defaultValue: null,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure
			)
		);

	public SplitViewDisplayMode DisplayMode
	{
		get => (SplitViewDisplayMode)this.GetValue(DisplayModeProperty);
		set => this.SetValue(DisplayModeProperty, value);
	}

	public static DependencyProperty DisplayModeProperty { get; } =
		DependencyProperty.Register(
			"DisplayMode",
			typeof(SplitViewDisplayMode),
			typeof(SplitView),
			new FrameworkPropertyMetadata(
				defaultValue: SplitViewDisplayMode.Overlay,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure,
				propertyChangedCallback: (s, e) => ((SplitView)s)?.OnDisplayModeChanged()
			)
		);

	public bool IsPaneOpen
	{
		get => (bool)this.GetValue(IsPaneOpenProperty);
		set => this.SetValue(IsPaneOpenProperty, value);
	}

	//There is an error in the MSDN docs saying that the default value for IsPaneOpen is true, it is actually false
	public static DependencyProperty IsPaneOpenProperty { get; } =
		DependencyProperty.Register(
			"IsPaneOpen",
			typeof(bool),
			typeof(SplitView),
			new FrameworkPropertyMetadata(
				false,
				(s, e) => ((SplitView)s)?.OnIsPaneOpenChanged((bool)e.NewValue)
			)
		);

	public double OpenPaneLength
	{
		get => (double)this.GetValue(OpenPaneLengthProperty);
		set => this.SetValue(OpenPaneLengthProperty, value);
	}

	public static DependencyProperty OpenPaneLengthProperty { get; } =
		DependencyProperty.Register(
			"OpenPaneLength",
			typeof(double),
			typeof(SplitView),
			new FrameworkPropertyMetadata(
				defaultValue: (double)320,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure,
				(s, e) => ((SplitView)s)?.OnPropertyChanged(e)
			)
		);

	public Brush PaneBackground
	{
		get => (Brush)this.GetValue(PaneBackgroundProperty);
		set => this.SetValue(PaneBackgroundProperty, value);
	}

	public static DependencyProperty PaneBackgroundProperty { get; } =
		DependencyProperty.Register(
			"PaneBackground",
			typeof(Brush),
			typeof(SplitView),
			new FrameworkPropertyMetadata(
				SolidColorBrushHelper.Transparent,
				(s, e) => ((SplitView)s)?.OnPropertyChanged(e)
			)
		);

	public SplitViewPanePlacement PanePlacement
	{
		get => (SplitViewPanePlacement)this.GetValue(PanePlacementProperty);
		set => this.SetValue(PanePlacementProperty, value);
	}

	public static DependencyProperty PanePlacementProperty { get; } =
		DependencyProperty.Register(
			"PanePlacement",
			typeof(SplitViewPanePlacement),
			typeof(SplitView),
			new FrameworkPropertyMetadata(
				SplitViewPanePlacement.Left,
				(s, e) => ((SplitView)s)?.OnPropertyChanged(e)
			)
		);

	public SplitViewTemplateSettings TemplateSettings
	{
		get => (SplitViewTemplateSettings)this.GetValue(TemplateSettingsProperty);
		private set => this.SetValue(TemplateSettingsProperty, value);
	}

	public static DependencyProperty TemplateSettingsProperty { get; } =
		DependencyProperty.Register(
			"TemplateSettings",
			typeof(SplitViewTemplateSettings),
			typeof(SplitView),
			new FrameworkPropertyMetadata(
				null,
				(s, e) => ((SplitView)s)?.OnPropertyChanged(e)
			)
		);

	public LightDismissOverlayMode LightDismissOverlayMode
	{
		get => (LightDismissOverlayMode)this.GetValue(LightDismissOverlayModeProperty);
		set => this.SetValue(LightDismissOverlayModeProperty, value);
	}

	public static DependencyProperty LightDismissOverlayModeProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(LightDismissOverlayMode), typeof(LightDismissOverlayMode),
			typeof(SplitView),
			new FrameworkPropertyMetadata(LightDismissOverlayMode.Off));
}
