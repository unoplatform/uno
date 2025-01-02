#if __IOS__ || __MACOS__ || __SKIA__ || __ANDROID__
#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Media;
using Uno.UI.Xaml.Media.Imaging.Svg;
using Windows.Foundation;

namespace Windows.UI.Xaml.Media.Imaging;

partial class SvgImageSource
{
	private const string SvgPackageName =
#if HAS_UNO_WINUI
		"Uno.WinUI.Svg";
#else
		"Uno.UI.Svg";
#endif

	private Task<ImageData>? _currentOpenTask;

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

	private bool TryOpenSvgImageData(CancellationToken ct, out Task<ImageData> asyncImage)
	{
		_currentOpenTask ??= LoadSvgImageAsync(ct);
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
