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
		// Re-opening replaces the retained document, so release the previous one before parsing again.
		Unload();

		var imageData = await GetSvgImageDataAsync(ct);
		if (imageData.Kind != ImageDataKind.ByteArray || imageData.ByteArray is null)
		{
			// A superseded open cancels its token mid-read, which is not a load failure.
			if (imageData.Kind == ImageDataKind.Error && imageData.Error is not OperationCanceledException)
			{
				RaiseImageFailed(SvgImageSourceLoadStatus.Other);
				return imageData;
			}

			return ImageData.Empty;
		}

		// The single registered ISvgRenderer (Skia by default, or the managed engine / an app-supplied one when
		// registered via the host builder) parses the markup into a retained vector document. When none is registered
		// there is nothing to draw at all; when the markup can't be parsed, the source failed to open.
		if (Uno.UI.Composition.Drawing.SvgRenderer.Current is not { } renderer)
		{
			return ImageData.Empty;
		}

		if (renderer.Parse(imageData.ByteArray, Uno.UI.Composition.Drawing.GeometryFactory.Current, Uno.UI.Composition.Drawing.DrawingFactory.Current) is { } document)
		{
			_svgDocument = document;
			_svgSurface = new(document);
			RaiseImageOpened();
			SourceLoaded?.Invoke(this, EventArgs.Empty);
			return imageData;
		}

		RaiseImageFailed(SvgImageSourceLoadStatus.InvalidFormat);

		// Reported as an error (not just "no data") so consumers raise their own failure, e.g. Image.ImageFailed.
		return ImageData.FromError(new InvalidOperationException("Failed to load Svg source"));
	}

	internal bool IsParsed => _svgDocument is not null;

	internal Size SourceSize => _svgDocument?.SourceSize ?? default;

	private void Unload()
	{
		// The surface owns the parsed document (see CompositionSvgSurface), so disposing it releases both.
		_svgSurface?.Dispose();
		_svgSurface = null;
		_svgDocument = null;
	}

	private protected override void UnloadImageSourceData()
	{
		_currentOpenTask = null;
		Unload();
	}
}
#endif
