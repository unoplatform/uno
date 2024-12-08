#pragma warning disable 67
using System;
using Windows.Foundation;

namespace Windows.UI.Xaml.Media.Imaging;

public partial class SvgImageSource : ImageSource
{
	/// <summary>
	/// Gets or sets the Uniform Resource Identifier (URI) of the SVG source file that generated this SvgImageSource.
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
			typeof(SvgImageSource),
			new FrameworkPropertyMetadata(default(Uri), (s, e) => (s as SvgImageSource)?.OnUriSourceChanged(e)));


	/// <summary>
	/// Gets or sets the height to use for SVG rasterization operations.
	/// </summary>
	/// <remarks>
	/// The height (in logical pixels) to use for SVG decoding operations. The default is NaN.
	/// </remarks>
	public double RasterizePixelHeight
	{
		get => (double)GetValue(RasterizePixelHeightProperty);
		set => SetValue(RasterizePixelHeightProperty, value);
	}

	/// <summary>
	/// Identifies the RasterizePixelHeight dependency property.
	/// </summary>
	public static DependencyProperty RasterizePixelHeightProperty { get; } =
		DependencyProperty.Register(
			nameof(RasterizePixelHeight),
			typeof(double),
			typeof(SvgImageSource),
			new FrameworkPropertyMetadata(double.NaN));

	/// <summary>
	/// Gets or sets the width to use for SVG rasterization operations.
	/// </summary>
	/// <remarks>
	/// The width (in logical pixels) to use for SVG decoding operations. The default is NaN.
	/// </remarks>
	public double RasterizePixelWidth
	{
		get => (double)GetValue(RasterizePixelWidthProperty);
		set => SetValue(RasterizePixelWidthProperty, value);
	}

	/// <summary>
	/// Identifies the RasterizePixelWidth dependency property.
	/// </summary>
	public static DependencyProperty RasterizePixelWidthProperty { get; } =
		DependencyProperty.Register(
			nameof(RasterizePixelWidth),
			typeof(double),
			typeof(SvgImageSource),
			new FrameworkPropertyMetadata(double.NaN));

	/// <summary>
	/// Occurs when the SVG source is downloaded and decoded with no failure.
	/// </summary>
	public event TypedEventHandler<SvgImageSource, SvgImageSourceOpenedEventArgs> Opened;

	/// <summary>
	/// Occurs when there is an error associated with SVG retrieval or format.
	/// </summary>
	public event TypedEventHandler<SvgImageSource, SvgImageSourceFailedEventArgs> OpenFailed;
}
