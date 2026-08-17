#if __SKIA__
#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Extensibility;
using Uno.UI.Xaml.Media;
using Uno.UI.Xaml.Media.Imaging.Svg;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Imaging;

partial class SvgImageSource
{
	private Task<ImageData>? _currentOpenTask;

	private ISvgProvider? _svgProvider;

	internal event EventHandler? SourceLoaded;

	private void InitSvgProvider()
	{
		// A registered SVG renderer (opt-in, e.g. the managed engine) is the primary path; the Skia-based add-in,
		// when installed, still drives the vector SvgCanvas and serves as a rendering fallback.
		ApiExtensibility.CreateInstance(this, out _svgProvider);

		if (_svgProvider is not null)
		{
			_svgProvider.SourceLoaded += OnSourceLoaded;
		}
	}

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

		// Primary: a registered SVG renderer (opt-in via the host builder) parses the markup. Core names no managed
		// impl; when none is registered we fall straight through to the optional Skia-based add-in below.
		if (Uno.UI.Composition.Drawing.SvgRenderer.Current is { } renderer
			&& renderer.Parse(imageData.ByteArray, Uno.UI.Composition.Drawing.GeometryFactory.Current, Uno.UI.Composition.Drawing.DrawingFactory.Current) is { } document)
		{
			_svgDocument = document;
			SourceLoaded?.Invoke(this, EventArgs.Empty);
			return imageData;
		}

		// Fallback: the optional Skia-based add-in (drives the vector SvgCanvas and any unsupported markup).
		if (_svgProvider is not null && await _svgProvider.TryLoadSvgDataAsync(imageData.ByteArray))
		{
			return imageData;
		}

		return ImageData.Empty;
	}

	internal UIElement? GetCanvas() => _svgProvider?.GetCanvas();

	internal bool IsParsed => _svgDocument is not null || (_svgProvider?.IsParsed ?? false);

	internal Size SourceSize => _svgDocument?.SourceSize ?? _svgProvider?.SourceSize ?? default;

	private void OnSourceLoaded(object? sender, EventArgs e) => SourceLoaded?.Invoke(this, EventArgs.Empty);

	private void Unload() => _svgProvider?.Unload();

	private protected override void UnloadImageSourceData()
	{
		_currentOpenTask = null;
		Unload();
	}
}
#endif
