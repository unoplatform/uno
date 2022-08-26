#pragma warning disable 67

using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls;

partial class Image
{
	/// <summary>
	/// Gets or sets the source for the image.
	/// </summary>
	public ImageSource Source
	{
		get => (ImageSource)GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}

	/// <summary>
	/// Identifies the Source  dependency property.
	/// </summary>
	public static DependencyProperty SourceProperty { get; } =
		DependencyProperty.Register(
			nameof(Source),
			typeof(ImageSource),
			typeof(Image),
			new FrameworkPropertyMetadata(
				null,
				(s, e) => ((Image)s).OnSourceChanged((ImageSource)e.NewValue)));

	/// <summary>
	/// Gets or sets a value that describes how an Image should be stretched to fill the destination rectangle.
	/// </summary>
	public Stretch Stretch
	{
		get => (Stretch)GetValue(StretchProperty);
		set => SetValue(StretchProperty, value);
	}

	/// <summary>
	/// Identifies the Stretch  dependency property.
	/// </summary>
	public static DependencyProperty StretchProperty { get; } =
		DependencyProperty.Register(
			nameof(Stretch),
			typeof(Stretch),
			typeof(Image),
			new FrameworkPropertyMetadata(
				Stretch.Uniform,
				(s, e) => ((Image)s).OnStretchChanged((Stretch)e.NewValue, (Stretch)e.OldValue)));

#if !__WASM__
	/// <summary>
	/// Occurs when there is an error associated with image retrieval or format.
	/// </summary>
	public event ExceptionRoutedEventHandler ImageFailed;

	/// <summary>
	/// Occurs when the image source is downloaded and decoded with no failure. You can use this event to determine the natural size of the image source.
	/// </summary>
	public event RoutedEventHandler ImageOpened;
#endif
}
