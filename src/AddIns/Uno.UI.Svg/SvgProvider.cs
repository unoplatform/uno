using System;
using System.Data.SqlTypes;
using System.IO;
using System.Threading.Tasks;
using ShimSkiaSharp;
using Svg.Skia;
using Uno.UI.Xaml.Media;
using Uno.UI.Xaml.Media.Imaging.Svg;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Uno.Foundation.Logging;
using Uno.Disposables;
using SkiaSharp;
using SKCanvas = SkiaSharp.SKCanvas;
using SKMatrix = SkiaSharp.SKMatrix;
using Windows.Graphics.Display;
using System.Diagnostics.CodeAnalysis;

namespace Uno.UI.Svg;

public partial class SvgProvider : ISvgProvider
{
	private readonly SvgImageSource _owner;
	private readonly CompositeDisposable _disposables = new();
	
	private SKSvg? _skSvg;
	private SKBitmap? _skBitmap;

	public SvgProvider(object owner)
	{
		if (owner is not SvgImageSource svgImageSource)
		{
			throw new InvalidOperationException("Owner must be a SvgImageSource instance.");
		}

		_owner = svgImageSource;

		_disposables.Add(_owner.RegisterDisposablePropertyChangedCallback(SvgImageSource.RasterizePixelHeightProperty, SourcePropertyChanged));
		_disposables.Add(_owner.RegisterDisposablePropertyChangedCallback(SvgImageSource.RasterizePixelWidthProperty, SourcePropertyChanged));
#if __SKIA__
		_owner.Subscribe(imageData =>
		{
			if (imageData.Kind == ImageDataKind.Empty)
			{
				// Empty image data is ignored.
				CleanupSvg();
				SourceUpdated?.Invoke(this, EventArgs.Empty);
				return;
			}
			else if (imageData.Kind != ImageDataKind.ByteArray || imageData.ByteArray is null)
			{
				throw new InvalidOperationException("SVG image data are not available.");
			}
			
			OnSourceOpened(imageData.ByteArray);
		});
#endif
	}

	private void SourcePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		if (UpdateBitmap())
		{
			SourceUpdated?.Invoke(this, EventArgs.Empty);
		}
	}

	public event EventHandler? SourceLoaded;
	
	internal event EventHandler? SourceUpdated;

	internal SKSvg? SkSvg => _skSvg;

	internal SKBitmap? SkBitmap => _skBitmap;

	public bool IsParsed => _skSvg?.Picture is not null;

	public Size SourceSize
	{
		get
		{
			if (_skSvg?.Picture?.CullRect is { } rect)
			{
				return new Size(rect.Width, rect.Height);
			}

			return default;
		}
	}

	public UIElement GetCanvas() => new SvgCanvas(_owner, this);

	private async void OnSourceOpened(byte[] svgBytes)
	{
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
			}
			else
			{
				CleanupSvg();
				_owner.RaiseImageFailed(SvgImageSourceLoadStatus.InvalidFormat);
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
		}
	}

	private void CleanupSvg()
	{
		_skSvg?.Dispose();
		_skBitmap?.Dispose();
		_skSvg = null;
		_skBitmap = null;
	}

	private Task<SKSvg?> LoadSvgAsync(byte[] svgBytes) =>
		Task.Run(() =>
		{
			var skSvg = new SKSvg();
			try
			{
				using (var memoryStream = new MemoryStream(svgBytes))
				{
					skSvg.Load(memoryStream);
				}
			}
			catch (Exception)
			{
				skSvg?.Dispose();
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
			canvas.DrawPicture(_skSvg.Picture, ref scaleMatrix);
			_skBitmap = bitmap;
			changed = true;
		} else if (
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

	// TODO: This is used by iOS/macOS/Android while Skia uses subscription. This behavior
	// should be aligned in the future so only one of the approaches is applied.
	public void NotifySourceOpened(byte[] svgBytes) => OnSourceOpened(svgBytes);
}
