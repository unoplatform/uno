#if __SKIA__
#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.Xaml.Media;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Imaging;

partial class SvgImageSource
{
	private Task<ImageData>? _currentOpenTask;

	internal event EventHandler? SourceLoaded;

	private bool TryOpenSvgImageData(CancellationToken ct, out Task<ImageData> asyncImage)
	{
		_currentOpenTask ??= LoadSvgImageAsync(ct);
		asyncImage = _currentOpenTask;
		return true;
	}

	private async Task<ImageData> LoadSvgImageAsync(CancellationToken ct)
	{
		var imageData = await GetSvgImageDataAsync(ct);
		if (imageData.Kind != ImageDataKind.ByteArray || imageData.ByteArray is null)
		{
			return ImageData.Empty;
		}

		// The single registered ISvgRenderer (Skia by default, or the managed engine / an app-supplied one when
		// registered via the host builder) parses the markup into a retained vector document. When none is registered
		// or the markup can't be parsed, there is nothing to draw.
		if (Uno.UI.Composition.Drawing.SvgRenderer.Current is { } renderer
			&& renderer.Parse(imageData.ByteArray, Uno.UI.Composition.Drawing.GeometryFactory.Current, Uno.UI.Composition.Drawing.DrawingFactory.Current) is { } document)
		{
			_svgDocument = document;
			SourceLoaded?.Invoke(this, EventArgs.Empty);
			return imageData;
		}

		return ImageData.Empty;
	}

	internal bool IsParsed => _svgDocument is not null;

	internal Size SourceSize => _svgDocument?.SourceSize ?? default;

	private void Unload() => _svgDocument = null;

	private protected override void UnloadImageSourceData()
	{
		_currentOpenTask = null;
		Unload();
	}
}
#endif
