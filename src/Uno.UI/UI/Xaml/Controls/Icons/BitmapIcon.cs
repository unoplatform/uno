using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents an icon that uses a bitmap as its content.
/// </summary>
public partial class BitmapIcon : IconElement, IThemeChangeAware
{
	private readonly Image _image;

	private protected override bool SupportsDirectChild => true;

	/// <summary>
	/// Initializes a new instance of the BitmapIcon class.
	/// </summary>
	public BitmapIcon()
	{
		_image = new Image();
		AddIconChild(_image);

		UpdateImageMonochromeColor();
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the bitmap is shown in a single color.
	/// </summary>
	public bool ShowAsMonochrome
	{
		get => (bool)GetValue(ShowAsMonochromeProperty);
		set => SetValue(ShowAsMonochromeProperty, value);
	}

	/// <summary>
	/// Identifies the ShowAsMonochrome dependency property.
	/// </summary>
	public static DependencyProperty ShowAsMonochromeProperty { get; } =
		DependencyProperty.Register(
			nameof(ShowAsMonochrome), typeof(bool),
			typeof(BitmapIcon),
			new FrameworkPropertyMetadata(true, (s, e) => (s as BitmapIcon)?.OnShowAsMonochromeChanged((bool)e.NewValue)));

	/// <summary>
	/// Gets or sets the Uniform Resource Identifier (URI) of the graphics source file that generated this BitmapImage.
	/// </summary>
	public Uri UriSource
	{
		get => (Uri)GetValue(UriSourceProperty);
		set => SetValue(UriSourceProperty, value);
	}

	/// <summary>
	/// Identifies the UriSource dependency property.
	/// </summary>
	public static DependencyProperty UriSourceProperty { get; } =
		DependencyProperty.Register(
			nameof(UriSource),
			typeof(Uri),
			typeof(BitmapIcon),
			new FrameworkPropertyMetadata(
				default(Uri),
				propertyChangedCallback: (s, e) => ((BitmapIcon)s).OnUriSourceChanged((Uri)e.NewValue)));

	// Mirrors the Uri -> ImageSource conversion the binding engine applies through ImageSourceConverter.
	private void OnUriSourceChanged(Uri uriSource)
	{
		if (_image is not null)
		{
			_image.Source = (ImageSource)uriSource;
		}
	}

	private void OnShowAsMonochromeChanged(bool value) => UpdateImageMonochromeColor();

	private protected override void OnForegroundChanged(DependencyPropertyChangedEventArgs e)
	{
		// When ShowAsMonochrome is false, the foreground color is not used
		// for rendering the image, so there is no need to update.
		if (ShowAsMonochrome)
		{
			UpdateImageMonochromeColor();
		}
	}

	private void UpdateImageMonochromeColor()
	{
#if !IS_UNIT_TESTS
		if (_image is not null)
		{
			_image.MonochromeColor = ShowAsMonochrome ? (Foreground as SolidColorBrush)?.Color : null;
		}
#endif
	}

	// The way this works in WinUI is by the MarkInheritedPropertyDirty call in CFrameworkElement::NotifyThemeChangedForInheritedProperties
	// There is a special handling for Foreground specifically there.
	void IThemeChangeAware.OnThemeChanged() => UpdateImageMonochromeColor();
}
