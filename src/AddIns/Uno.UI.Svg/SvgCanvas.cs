#if !__NETSTD_REFERENCE__
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
#if HAS_UNO_WINUI
using SkiaSharp.Views.Windows;

#if __SKIA__
using SkiaCanvas = global::Uno.WinUI.Graphics2DSK.SKCanvasElement;
#elif __MACCATALYST__ || !(__APPLE_UIKIT__ || __ANDROID__)
using SkiaCanvas = SkiaSharp.Views.Windows.SKXamlCanvas;
using SkiaPaintEventArgs = SkiaSharp.Views.Windows.SKPaintSurfaceEventArgs;
#else
using SkiaCanvas = SkiaSharp.Views.Windows.SKSwapChainPanel;
using SkiaPaintEventArgs = SkiaSharp.Views.Windows.SKPaintGLSurfaceEventArgs;
#endif
#else
using SkiaSharp.Views.UWP;
#if __MACCATALYST__ || !(__APPLE_UIKIT__ || __ANDROID__)
using SkiaCanvas = SkiaSharp.Views.UWP.SKXamlCanvas;
using SkiaPaintEventArgs = SkiaSharp.Views.UWP.SKPaintSurfaceEventArgs;
#else
using SkiaCanvas = SkiaSharp.Views.UWP.SKSwapChainPanel;
using SkiaPaintEventArgs = SkiaSharp.Views.UWP.SKPaintGLSurfaceEventArgs;
#endif
#endif

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

	private void SvgCanvas_Loaded(object? sender, RoutedEventArgs e)
	{
		Invalidate();

#if __MACCATALYST__
		this.Opaque = false;
#elif __APPLE_UIKIT__
		// The SKGLTextureView is opaque by default, so we poke at the tree
		// to change the opacity of the first view of the SKSwapChainPanel
		// to make it transparent.
		if (Subviews.Length == 1 &&
			Subviews[0] is GLKit.GLKView texture)
		{
			texture.Opaque = false;
		}
#elif __ANDROID__
		// The SKGLTextureView is opaque by default, so we poke at the tree
		// to change the opacity of the first view of the SKSwapChainPanel
		// to make it transparent.
		if (ChildCount == 1 &&
			GetChildAt(0) is Android.Views.TextureView texture)
		{
			texture.SetOpaque(false);
		}
#endif
	}

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

#if __SKIA__
	protected override void RenderOverride(SKCanvas canvas, Size area)
	{
		Draw(canvas, (float)area.Width, (float)area.Height);
	}
#else
	protected override void OnPaintSurface(SkiaPaintEventArgs e)
	{
		var canvas = e.Surface.Canvas;
		var scale = (float)GetScaleFactorForLayoutRounding();
		canvas.SetMatrix(SKMatrix.CreateScale(scale, scale));
		canvas.Clear(SKColors.Transparent);
		Draw(canvas, (float)_lastArrangeSize.Width, (float)_lastArrangeSize.Height);
	}
#endif

	private void Draw(SKCanvas canvas, float width, float height)
	{
		if (_svgImageSource.UseRasterized && _svgProvider.SkBitmap is { } bitmap)
		{
			var sourceRect = new SKRect(0, 0, bitmap.Width, bitmap.Height);
			var destRect = new SKRect(0, 0, width, height);
			canvas.DrawBitmap(bitmap, sourceRect, destRect);
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
#endif
