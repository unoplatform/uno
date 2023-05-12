using System;
using Windows.Foundation;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Represents an icon that uses a bitmap as its content.
/// </summary>
public partial class BitmapIcon : IconElement
{
	private readonly Image _image;

	/// <summary>
	/// Initializes a new instance of the BitmapIcon class.
	/// </summary>
	public BitmapIcon()
	{
		_image = new Image();
		AddIconChild(_image);

		_image.SetBinding(
			dependencyProperty: Image.SourceProperty,
			binding: new Binding { Source = this, Path = nameof(UriSource) }
		);

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
			new FrameworkPropertyMetadata(default(Uri)));

	private void OnShowAsMonochromeChanged(bool value) => UpdateImageMonochromeColor();

	private protected override void OnForegroundChanged(DependencyPropertyChangedEventArgs e) => UpdateImageMonochromeColor();

	private void UpdateImageMonochromeColor()
	{
#if !IS_UNIT_TESTS
		if (_image is not null)
		{
			_image.MonochromeColor = ShowAsMonochrome ? (Foreground as SolidColorBrush)?.Color : null;
		}
#endif
	}
}
