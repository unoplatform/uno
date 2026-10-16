#if __APPLE_UIKIT__ || __SKIA__ || __ANDROID__
#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Media;
using Uno.UI.Xaml.Media.Imaging.Svg;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Imaging;

partial class SvgImageSource
{
	private const string SvgPackageName = "Uno.WinUI.Svg";

	private Task<ImageData>? _currentOpenTask;
	private CancellationTokenSource? _loadCts;

	private ISvgProvider? _svgProvider;

	internal event EventHandler? SourceLoaded;

	private void InitSvgProvider()
	{
		if (!ApiExtensibility.CreateInstance(this, out _svgProvider))
		{
			LogSvgPackageError();
		}

		if (_svgProvider is not null)
		{
			_svgProvider.SourceLoaded += OnSourceLoaded;
		}
	}

	private bool TryOpenSvgImageData(out Task<ImageData> asyncImage)
	{
		// The load is shared by every open of this source, so it can't be bound to the token of the open that started
		// it: SetSourceAsync and a subscribing brush both re-open the source while it loads, cancelling that token.
		// Like WinUI, only a change of the source itself (UnloadImageSourceData) aborts the load.
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
		if (_svgProvider is null)
		{
			LogSvgPackageError();
			return ImageData.Empty;
		}

		var imageData = await GetSvgImageDataAsync(ct);
		if (ct.IsCancellationRequested)
		{
			// The source changed while reading; the load that replaced this one publishes the result.
			return ImageData.Empty;
		}

		if (imageData.Kind == ImageDataKind.ByteArray &&
			imageData.ByteArray is not null &&
			await _svgProvider.TryLoadSvgDataAsync(imageData.ByteArray))
		{
			return imageData;
		}

		return ImageData.Empty;
	}

	internal UIElement? GetCanvas() => _svgProvider?.GetCanvas();

	internal bool IsParsed => _svgProvider?.IsParsed ?? false;

	internal Size SourceSize => _svgProvider?.SourceSize ?? default;

	private void OnSourceLoaded(object? sender, EventArgs e) => SourceLoaded?.Invoke(this, EventArgs.Empty);

	private void Unload() => _svgProvider?.Unload();

	private protected override void UnloadImageSourceData()
	{
		_loadCts?.Cancel();
		_loadCts?.Dispose();
		_loadCts = null;
		_currentOpenTask = null;
		Unload();
	}

	private void LogSvgPackageError()
	{
		if (this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().LogError($"To use SVG on this platform, make sure to install the {SvgPackageName} package.");
		}
	}
}
#endif
