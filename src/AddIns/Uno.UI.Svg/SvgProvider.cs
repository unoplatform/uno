using System;
using System.IO;
using System.Threading.Tasks;
using Uno.UI.Xaml.Media.Imaging.Svg;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Uno.Foundation.Logging;
using Uno.Disposables;
using Windows.Graphics.Display;


#if !__NETSTD_REFERENCE__
using ShimSkiaSharp;
using Svg.Skia;
using Uno.UI.Xaml.Media;
using Uno.UI.Composition.Drawing;
using SkiaSharp;
using SKCanvas = SkiaSharp.SKCanvas;
using SKMatrix = SkiaSharp.SKMatrix;
#else
#pragma warning disable CS0067
#endif

namespace Uno.UI.Svg;

public partial class SvgProvider : ISvgProvider
{
#if !__NETSTD_REFERENCE__
	// 16 MP (≈64 MB BGRA) ceiling on a single rasterization, to bound untrusted SVG dimensions.
	private const long MaxRasterizePixelCount = 16L * 1024 * 1024;

	private readonly SvgImageSource _owner;
	private readonly CompositeDisposable _disposables = new();

	private SKSvg? _skSvg;
	private SKBitmap? _skBitmap;
#endif

	public SvgProvider(object owner)
	{
#if __NETSTD_REFERENCE__
		throw new PlatformNotSupportedException();
#else

		if (owner is not SvgImageSource svgImageSource)
		{
			throw new InvalidOperationException("Owner must be a SvgImageSource instance.");
		}

		_owner = svgImageSource;

		_disposables.Add(_owner.RegisterDisposablePropertyChangedCallback(SvgImageSource.RasterizePixelHeightProperty, SourcePropertyChanged));
		_disposables.Add(_owner.RegisterDisposablePropertyChangedCallback(SvgImageSource.RasterizePixelWidthProperty, SourcePropertyChanged));
#endif // __NETSTD_REFERENCE__
	}

	public event EventHandler? SourceLoaded;

#if !__NETSTD_REFERENCE__
	internal event EventHandler? SourceUpdated;

	internal SKSvg? SkSvg => _skSvg;

	internal SKBitmap? SkBitmap => _skBitmap;
#endif

	public bool IsParsed
#if __NETSTD_REFERENCE__
		=> throw new PlatformNotSupportedException();
#else
		=> _skSvg?.Picture is not null;
#endif

	public Size SourceSize
	{
		get
		{
#if __NETSTD_REFERENCE__
			throw new PlatformNotSupportedException();
#else
			if (_skSvg?.Picture?.CullRect is { } rect)
			{
				return new Size(rect.Width, rect.Height);
			}

			return default;
#endif
		}
	}

	public UIElement GetCanvas()
#if __NETSTD_REFERENCE__
		=> throw new PlatformNotSupportedException();
#else
		=> new SvgCanvas(_owner, this);
#endif

	public unsafe object? RenderToImage(int pixelWidth, int pixelHeight)
#if !__NETSTD_REFERENCE__
	{
		if (_skSvg?.Picture is not { } picture || pixelWidth <= 0 || pixelHeight <= 0)
		{
			return null;
		}

		// The dimensions can derive from an untrusted SVG (a remote source's viewBox), so bound the
		// allocation: (long) math avoids int overflow, and the cap avoids an OOM from a hostile size.
		if ((long)pixelWidth * pixelHeight > MaxRasterizePixelCount)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"SVG rasterization size {pixelWidth}x{pixelHeight} exceeds the maximum; skipping.");
			}
			return null;
		}

		var info = new SKImageInfo(pixelWidth, pixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
		using var surface = SKSurface.Create(info);
		if (surface is null)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Failed to create a {pixelWidth}x{pixelHeight} surface for SVG rasterization.");
			}
			return null;
		}
		var canvas = surface.Canvas;
		canvas.Clear(SkiaSharp.SKColors.Transparent);
		var cull = picture.CullRect;
		if (cull.Width > 0 && cull.Height > 0)
		{
			canvas.Scale(pixelWidth / cull.Width, pixelHeight / cull.Height);
		}
		canvas.DrawPicture(picture);
		canvas.Flush();

		var bgra = new byte[pixelWidth * pixelHeight * 4];
		fixed (byte* p = bgra)
		{
			if (!surface.ReadPixels(info, (nint)p, info.RowBytes, 0, 0))
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"SVG rasterization ReadPixels failed for {pixelWidth}x{pixelHeight}.");
				}
				return null;
			}
		}

		return DrawingBackend.Current.CreateImageFrame(pixelWidth, pixelHeight, bgra).Frames[0];
	}
#else
		=> null;
#endif

	public
#if !__NETSTD_REFERENCE__
	async
#endif
	Task<bool> TryLoadSvgDataAsync(byte[] svgBytes)
	{
#if __NETSTD_REFERENCE__
		return Task.FromResult(false);
#else
		var succeeded = false;
		try
		{
			CleanupSvg();
			var skSvg = await LoadSvgAsync(svgBytes);
			if (skSvg is not null)
			{
				_skSvg = skSvg;
				_owner.RaiseImageOpened();
				_skBitmap = null;
				UpdateBitmap();
				SourceLoaded?.Invoke(this, EventArgs.Empty);
				succeeded = true;
			}
			else
			{
				CleanupSvg();
				_owner.RaiseImageFailed(SvgImageSourceLoadStatus.InvalidFormat);
				succeeded = false;
			}
			SourceUpdated?.Invoke(this, EventArgs.Empty);
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().LogError("Failed to load SVG image.", ex);
			}
			CleanupSvg();
			succeeded = false;
		}

		return succeeded;
#endif
	}

	private void CleanupSvg()
	{
#if !__NETSTD_REFERENCE__
		_skSvg?.Dispose();
		_skBitmap?.Dispose();
		_skSvg = null;
		_skBitmap = null;
#endif
	}

#if !__NETSTD_REFERENCE__
	private Task<SKSvg?> LoadSvgAsync(byte[] svgBytes) =>
		Task.Run(() =>
		{
			var skSvg = new SKSvg();
			try
			{
				using var memoryStream = new MemoryStream(svgBytes);
				skSvg.Load(memoryStream);
			}
			catch (Exception ex)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError("Failed to load SVG image.", ex);
				}
				skSvg.Dispose();
				skSvg = null;
			}

			return skSvg;
		});

	private bool UpdateBitmap()
	{
		var scale = DisplayInformation.GetForCurrentView().LogicalDpi / DisplayInformation.BaseDpi;
		var desiredPhysicalWidth = (int)(scale * _owner.RasterizePixelHeight);
		var desiredPhysicalHeight = (int)(scale * _owner.RasterizePixelWidth);
		var changed = false;
		if (!double.IsNaN(_owner.RasterizePixelHeight) &&
			!double.IsNaN(_owner.RasterizePixelWidth) &&
			_skSvg is not null &&
			(_skBitmap is null || _skBitmap.Width != desiredPhysicalWidth || _skBitmap.Height != desiredPhysicalHeight))
		{
			var bitmap = new SKBitmap(desiredPhysicalWidth, desiredPhysicalHeight);
			using SKCanvas canvas = new SKCanvas(bitmap);

			SKMatrix scaleMatrix = default;
			if (_skSvg.Picture?.CullRect is { } rect)
			{
				scaleMatrix = SKMatrix.CreateScale(bitmap.Width / rect.Width, bitmap.Height / rect.Height);
			}

			canvas.Clear(SKColors.Transparent);
			canvas.DrawPicture(_skSvg.Picture, in scaleMatrix);
			_skBitmap = bitmap;
			changed = true;
		}
		else if (
			double.IsNaN(_owner.RasterizePixelHeight) &&
			double.IsNaN(_owner.RasterizePixelWidth) &&
			_skBitmap is not null)
		{
			_skBitmap?.Dispose();
			_skBitmap = null;
			changed = true;
		}
		return changed;
	}

	private void SourcePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		if (UpdateBitmap())
		{
			SourceUpdated?.Invoke(this, EventArgs.Empty);
		}
	}
#endif

	public void Unload() => CleanupSvg();
}
