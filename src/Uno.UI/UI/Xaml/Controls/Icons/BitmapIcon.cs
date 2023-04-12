using System;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Represents an icon that uses a bitmap as its content.
/// </summary>
public partial class BitmapIcon : IconElement
{
	private Image _image;
	private Grid _grid;

	/// <summary>
	/// Initializes a new instance of the BitmapIcon class.
	/// </summary>
	public BitmapIcon()
	{
		SetValue(ForegroundProperty, SolidColorBrushHelper.Black, DependencyPropertyValuePrecedences.Inheritance);

		Loaded += (s, e) SynchronizeProperties();
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

	protected override Size MeasureOverride(Size availableSize)
	{
		if (_image == null)
		{
			_image = new Image
			{
				Stretch = Media.Stretch.Uniform
			};

			UpdateImageMonochromeColor();

			_image.SetBinding(
				dependencyProperty: Image.SourceProperty,
				binding: new Binding { Source = this, Path = nameof(UriSource) }
			);

			_grid = new Grid();
			_grid.Children.Add(_image);

			AddIconElementView(_grid);
		}


		return base.MeasureOverride(availableSize);
	}


	private void OnShowAsMonochromeChanged(bool value) => UpdateImageMonochromeColor();

	private protected override void OnForegroundChanged(DependencyPropertyChangedEventArgs e)
	{
		base.OnForegroundChanged(e);
		UpdateImageMonochromeColor();
	}

	private void UpdateImageMonochromeColor()
	{
#if !NET461
		if (_image != null)
		{
			_image.MonochromeColor = ShowAsMonochrome ? (Foreground as SolidColorBrush)?.Color : null;
		}
#endif
	}
}
