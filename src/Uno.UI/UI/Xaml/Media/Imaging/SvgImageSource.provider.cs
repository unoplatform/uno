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
	private CancellationTokenSource? _loadCts;

	internal event EventHandler? SourceLoaded;

	private bool TryOpenSvgImageData(out Task<ImageData> asyncImage)
	{
		// The load is shared by every open of this source, so it can't be bound to the token of the open that started
		// it: an Image subscribing while the source's own open is in flight supersedes that open, but must reuse its
		// load. Like WinUI, only a change of the source itself (UnloadImageSourceData) aborts the load.
		if (_currentOpenTask is null)
		{
			_loadCts = new();
			_currentOpenTask = LoadSvgImageAsync(_loadCts.Token);
		}

		asyncImage = _currentOpenTask;
		return true;
	}

	private async Task<ImageData> LoadSvgImageAsync(CancellationToken ct)
	{
		// Re-opening replaces the retained document, so release the previous one before parsing again.
		Unload();

		var imageData = await GetSvgImageDataAsync(ct);
		if (ct.IsCancellationRequested)
		{
			// The source changed while reading; the load that replaced this one publishes the result.
			return ImageData.Empty;
		}

		if (imageData.Kind != ImageDataKind.ByteArray || imageData.ByteArray is null)
		{
			// An aborted load (the source changed mid-read) is not a load failure.
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
		_loadCts?.Cancel();
		_loadCts?.Dispose();
		_loadCts = null;
		_currentOpenTask = null;
		Unload();
	}
}
#endif
