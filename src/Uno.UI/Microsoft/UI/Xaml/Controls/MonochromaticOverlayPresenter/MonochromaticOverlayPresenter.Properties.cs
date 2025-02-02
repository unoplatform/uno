using Windows.UI;
using Microsoft.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives;

public partial class MonochromaticOverlayPresenter
{
	/// <summary>
	/// Gets or sets the base color used to generate the monochromatic overlay.
	/// </summary>
	public Color ReplacementColor
	{
		get => (Color)GetValue(ReplacementColorProperty);
		set => SetValue(ReplacementColorProperty, value);
	}

	/// <summary>
	/// Identifies the ReplacementColor dependency property.
	/// </summary>
	public static DependencyProperty ReplacementColorProperty { get; } =
		DependencyProperty.Register(
			nameof(ReplacementColor),
			typeof(Color),
			typeof(MonochromaticOverlayPresenter),
			new FrameworkPropertyMetadata(default(Color), OnPropertyChanged));

	/// <summary>
	/// Gets or sets the source element on which to overlay the monochromatic color scheme or hue.
	/// </summary>
	public UIElement SourceElement
	{
		get => (UIElement)GetValue(SourceElementProperty);
		set => SetValue(SourceElementProperty, value);
	}

	/// <summary>
	/// Identifies the SourceElement dependency property.
	/// </summary>
	public static DependencyProperty SourceElementProperty { get; } =
		DependencyProperty.Register(
			nameof(SourceElement),
			typeof(UIElement),
			typeof(MonochromaticOverlayPresenter),
			new FrameworkPropertyMetadata(null, OnPropertyChanged));

	private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var presenter = sender as MonochromaticOverlayPresenter;
		presenter?.OnPropertyChanged(args);
	}
}
