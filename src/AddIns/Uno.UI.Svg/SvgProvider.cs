#nullable enable

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

namespace Uno.UI.Svg;

[Preserve]
public partial class SvgProvider : ISvgProvider
{
	private readonly SvgImageSource _owner;

	private SKSvg? _skSvg;

	public SvgProvider(object owner)
	{
		if (owner is not SvgImageSource svgImageSource)
		{
			throw new InvalidOperationException("Owner must be a SvgImageSource instance.");
		}

		_owner = svgImageSource;
#if __SKIA__
		_owner.Subscribe(imageData =>
		{
			if (imageData.Kind != ImageDataKind.ByteArray || imageData.ByteArray is null)
			{
				throw new InvalidOperationException("SVG image data are not available.");
			}
			OnSourceOpened(imageData.ByteArray);
		});
#endif
	}

	public event EventHandler? SourceLoaded;

	internal SKSvg? SkSvg => _skSvg;

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
			var skSvg = await LoadSvgAsync(svgBytes);
			if (skSvg is not null)
			{
				_skSvg = skSvg;
				_owner.RaiseImageOpened();
				SourceLoaded?.Invoke(this, EventArgs.Empty);
			}
			else
			{
				_skSvg?.Dispose();
				_skSvg = null;
				_owner.RaiseImageFailed(SvgImageSourceLoadStatus.InvalidFormat);
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().LogError("Failed to load SVG image.", ex);
			}
		}
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

	// TODO: This is used by iOS/macOS/Android while Skia uses subscription. This behavior
	// should be aligned in the future so only one of the approaches is applied.
	public void NotifySourceOpened(byte[] svgBytes) => OnSourceOpened(svgBytes);
}
