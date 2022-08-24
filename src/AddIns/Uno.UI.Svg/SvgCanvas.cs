using System;
using System.IO;
using ShimSkiaSharp;
using SkiaSharp;
using SkiaSharp.Views.UWP;
using Svg.Skia;
using Uno.Disposables;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using SKMatrix = SkiaSharp.SKMatrix;

namespace Uno.UI.Svg;

internal partial class SvgCanvas :
#if __IOS__ || __MACOS__
	SKSwapChainPanel
#else
	SKXamlCanvas
#endif
{
	private readonly SvgImageSource _svgImageSource;
	private readonly SvgProvider _svgProvider;
	private readonly CompositeDisposable _disposables = new();
	private SKMatrix _currentScaleMatrix = default;

	public SvgCanvas(SvgImageSource svgImageSource, SvgProvider svgProvider)
	{
		_svgImageSource = svgImageSource;
		_svgProvider = svgProvider;

		SizeChanged += SvgCanvas_SizeChanged;

		_svgProvider.SourceLoaded += SvgProviderSourceOpened;
		_disposables.Add(() => _svgProvider.SourceLoaded -= SvgProviderSourceOpened);
		_disposables.Add(_svgImageSource.RegisterDisposablePropertyChangedCallback(SvgImageSource.UriSourceProperty, SourcePropertyChanged));
		_disposables.Add(_svgImageSource.RegisterDisposablePropertyChangedCallback(SvgImageSource.RasterizePixelHeightProperty, SourcePropertyChanged));
		_disposables.Add(_svgImageSource.RegisterDisposablePropertyChangedCallback(SvgImageSource.RasterizePixelWidthProperty, SourcePropertyChanged));

		Loaded += SvgCanvas_Loaded;
		Unloaded += SvgCanvas_Unloaded;
	}

	private void SvgCanvas_Loaded(object sender, RoutedEventArgs e)
	{
		Invalidate();

#if __IOS__ || __MACOS__
		// The SKGLTextureView is opaque by default, so we poke at the tree
		// to change the opacity of the first view of the SKSwapChainPanel
		// to make it transparent.
		if (Subviews.Length == 1 &&
			Subviews[0] is GLKit.GLKView texture)
		{
			texture.Opaque = false;
		}
#endif
	}

	private void SvgCanvas_Unloaded(object sender, RoutedEventArgs e) => _disposables.Dispose();

	private void SourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs dp) => Invalidate();

	private void SvgCanvas_SizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs args) => Invalidate();

	private void SvgProviderSourceOpened(object sender, EventArgs e)
	{
		InvalidateMeasure();
		Invalidate();
	}

	//protected override Size MeasureOverride(Size availableSize)
	//{
	//	if (_skSvg?.Picture != null)
	//	{
	//		// TODO:MZ: Handle case where SVG is rasterized
	//		var measuredSize = new Size(_skSvg.Picture.CullRect.Width, _skSvg.Picture.CullRect.Height);
	//		return measuredSize;
	//		//Size ret;

	//		//if (
	//		//	double.IsInfinity(availableSize.Width)
	//		//	&& double.IsInfinity(availableSize.Height)
	//		//)
	//		//{
	//		//	ret = measuredSize;
	//		//}
	//		//else
	//		//{
	//		//	ret = ImageSizeHelper.AdjustSize(availableSize, measuredSize);
	//		//}

	//		// Always making sure the ret size isn't bigger than the available size for an image with a fixed width or height
	//		//ret = new Size(
	//		//	!Double.IsNaN(Width) && (ret.Width > availableSize.Width) ? availableSize.Width : ret.Width,
	//		//	!Double.IsNaN(Height) && (ret.Height > availableSize.Height) ? availableSize.Height : ret.Height
	//		//);

	//		//if (this.Log().IsEnabled(LogLevel.Debug))
	//		//{
	//		//	this.Log().LogDebug($"Measure {this} availableSize:{availableSize} measuredSize:{_lastMeasuredSize} ret:{ret} Stretch: {Stretch} Width:{Width} Height:{Height}");
	//		//}
	//	}
	//	else
	//	{
	//		return default;
	//	}
	//}

	protected override Size ArrangeOverride(Size finalSize)
	{
		finalSize = base.ArrangeOverride(finalSize);

		SKMatrix scaleMatrix = default;
		if (_svgProvider.SkSvg?.Picture?.CullRect is { } rect)
		{
			scaleMatrix = SKMatrix.CreateScale((float)finalSize.Width / rect.Width, (float)finalSize.Height / rect.Height);
		}

		if (scaleMatrix != _currentScaleMatrix)
		{
			_currentScaleMatrix = scaleMatrix;
			Invalidate();
		}

		return finalSize;
	}


	protected override void OnPaintSurface(SKPaintGLSurfaceEventArgs e)
	{
		e.Surface.Canvas.Clear(SKColors.Transparent);
		if (_svgProvider.SkSvg?.Picture is { } picture)
		{
			e.Surface.Canvas.DrawPicture(picture, ref _currentScaleMatrix);
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
