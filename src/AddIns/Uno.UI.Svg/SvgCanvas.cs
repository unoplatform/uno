using System;
using System.IO;
using SkiaSharp;
using SkiaSharp.Views.UWP;
using Svg.Skia;
using Uno.Disposables;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Uno.UI.Svg;

internal partial class SvgCanvas : SKXamlCanvas
{
	private readonly SvgImageSource _svgImageSource;
	private SKSvg? _skSvg;
	private readonly CompositeDisposable _disposables = new();

	public SvgCanvas(SvgImageSource svgImageSource)
	{
		_svgImageSource = svgImageSource;
		SizeChanged += SvgCanvas_SizeChanged;

		_disposables.Add(_svgImageSource.Subscribe(OnSourceOpened));
		_disposables.Add(_svgImageSource.RegisterDisposablePropertyChangedCallback(SvgImageSource.UriSourceProperty, SourcePropertyChanged));
		_disposables.Add(_svgImageSource.RegisterDisposablePropertyChangedCallback(SvgImageSource.RasterizePixelHeightProperty, SourcePropertyChanged));
		_disposables.Add(_svgImageSource.RegisterDisposablePropertyChangedCallback(SvgImageSource.RasterizePixelWidthProperty, SourcePropertyChanged));

		Loaded += SvgCanvas_Loaded;
		Unloaded += SvgCanvas_Unloaded;
	}

	private void SvgCanvas_Loaded(object sender, RoutedEventArgs e) => Invalidate();

	private void SvgCanvas_Unloaded(object sender, RoutedEventArgs e) => _disposables.Dispose();

	private void SourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs dp) => Invalidate();

	private void SvgCanvas_SizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs args) => Invalidate();

	protected override Size MeasureOverride(Size availableSize)
	{
		if (_skSvg?.Picture != null)
		{
			// TODO:MZ: Handle case where SVG is rasterized
			var measuredSize = new Size(_skSvg.Picture.CullRect.Width, _skSvg.Picture.CullRect.Height);
			return measuredSize;
			//Size ret;

			//if (
			//	double.IsInfinity(availableSize.Width)
			//	&& double.IsInfinity(availableSize.Height)
			//)
			//{
			//	ret = measuredSize;
			//}
			//else
			//{
			//	ret = ImageSizeHelper.AdjustSize(availableSize, measuredSize);
			//}

			// Always making sure the ret size isn't bigger than the available size for an image with a fixed width or height
			//ret = new Size(
			//	!Double.IsNaN(Width) && (ret.Width > availableSize.Width) ? availableSize.Width : ret.Width,
			//	!Double.IsNaN(Height) && (ret.Height > availableSize.Height) ? availableSize.Height : ret.Height
			//);

			//if (this.Log().IsEnabled(LogLevel.Debug))
			//{
			//	this.Log().LogDebug($"Measure {this} availableSize:{availableSize} measuredSize:{_lastMeasuredSize} ret:{ret} Stretch: {Stretch} Width:{Width} Height:{Height}");
			//}
		}
		else
		{
			return default;
		}
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		return base.ArrangeOverride(finalSize);
	}

	private void OnSourceOpened(ImageData obj)
	{
		try
		{
			_skSvg = new SKSvg();
			using var memoryStream = new MemoryStream(obj.Data);
			_skSvg.Load(memoryStream);
			InvalidateMeasure();
			Invalidate();
		}
		catch (Exception)
		{
			_svgImageSource.RaiseImageFailed(SvgImageSourceLoadStatus.InvalidFormat);
		}
	}

	protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
	{
		if (_skSvg is not null)
		{
			var scale = SKMatrix.CreateScale(0.5f, 0.5f);
			var image = Parent as Image;
			var width = ActualWidth;
			var height = ActualHeight;
			var fit = _skSvg.Picture!.CullRect.AspectFit(new SKSize((float)width, (float)height));
			//scale = SKMatrix.CreateScale((float)fit.Width / (float)_skSvg.Picture!.CullRect.Width, (float)fit.Height / (float)_skSvg.Picture!.CullRect.Height);
			e.Surface.Canvas.DrawPicture(_skSvg.Picture, ref scale);
		}
		if (double.IsNaN(_svgImageSource.RasterizePixelHeight) && double.IsNaN(_svgImageSource.RasterizePixelWidth))
		{
			// Draw as actual vectors
		}
		else
		{
			// Draw as bitmap
		}
	}
}
