using System;
using System.IO;
using SkiaSharp;
using Svg.Skia;
using Uno.Disposables;
using Windows.Foundation;
using Windows.Graphics.Display;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using SKMatrix = SkiaSharp.SKMatrix;
using SKRect = SkiaSharp.SKRect;
using SkiaCanvas = global::Uno.WinUI.Graphics2DSK.SKCanvasElement;

namespace Uno.UI.Svg;

internal partial class SvgCanvas : SkiaCanvas
{
	private readonly SvgImageSource _svgImageSource;
	private readonly SvgProvider _svgProvider;
	private readonly CompositeDisposable _disposables = new();

	private Size _lastArrangeSize;

	public SvgCanvas(SvgImageSource svgImageSource, SvgProvider svgProvider)
	{
		_svgImageSource = svgImageSource;
		_svgProvider = svgProvider;

		SizeChanged += SvgCanvas_SizeChanged;

		_svgProvider.SourceUpdated += SvgProviderSourceOpened;
		_disposables.Add(() => _svgProvider.SourceUpdated -= SvgProviderSourceOpened); ;

		Loaded += SvgCanvas_Loaded;
		Unloaded += SvgCanvas_Unloaded;
	}

	private void SvgCanvas_Loaded(object? sender, RoutedEventArgs e) => Invalidate();

	private void SvgCanvas_Unloaded(object sender, RoutedEventArgs e) => _disposables.Dispose();

	private void SvgCanvas_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs args) => Invalidate();

	private void SvgProviderSourceOpened(object? sender, EventArgs e)
	{
		if (Dispatcher.HasThreadAccess)
		{
			InvalidateLayout();
		}
		else
		{
			_ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => InvalidateLayout());
		}
	}

	private void InvalidateLayout()
	{
		InvalidateMeasure();
		InvalidateArrange();
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		finalSize = base.ArrangeOverride(finalSize);
		_lastArrangeSize = finalSize;
		Invalidate();
		return finalSize;
	}

	protected override void RenderOverride(SKCanvas canvas, Size area)
	{
		Draw(canvas, (float)area.Width, (float)area.Height);
	}

	private void Draw(SKCanvas canvas, float width, float height)
	{
		if (_svgImageSource.UseRasterized && _svgProvider.SkBitmap is { } bitmap)
		{
			var sourceRect = new SKRect(0, 0, bitmap.Width, bitmap.Height);
			var destRect = new SKRect(0, 0, width, height);
			canvas.DrawBitmap(bitmap, sourceRect, destRect, SKSamplingOptions.Default, null);
		}
		else if (_svgProvider.SkSvg?.Picture is { } picture)
		{
			var svgScaleMatrix = CreateScaleMatrix();
			canvas.DrawPicture(picture, in svgScaleMatrix);
		}
	}
	private SKMatrix CreateScaleMatrix()
	{
		if (_lastArrangeSize == default)
		{
			return SKMatrix.Identity;
		}

		SKMatrix scaleMatrix = default;
		if (_svgProvider.SkSvg?.Picture?.CullRect is { } rect)
		{
			scaleMatrix = SKMatrix.CreateScale((float)_lastArrangeSize.Width / rect.Width, (float)_lastArrangeSize.Height / rect.Height);
		}

		return scaleMatrix;
	}
}
