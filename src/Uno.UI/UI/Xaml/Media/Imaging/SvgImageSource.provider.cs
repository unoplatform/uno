#if __IOS__ || __MACOS__ || __SKIA__ || __ANDROID__
#nullable enable

using System;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Media.Imaging.Svg;
using Windows.Foundation;

namespace Windows.UI.Xaml.Media.Imaging;

partial class SvgImageSource
{
	private ISvgProvider? _svgProvider;

	private void InitSvgProvider()
	{
		if (!ApiExtensibility.CreateInstance(this, out _svgProvider))
		{			
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().LogError("To use SVG on this platform, make sure to install the Uno.UI.Svg package.");
			}
		}

		if (_svgProvider is not null)
		{
			_svgProvider.SourceLoaded += OnSourceLoaded;			
		}
	}

	internal event EventHandler SourceLoaded;

	internal UIElement? GetCanvas() => _svgProvider?.GetCanvas();

	internal bool IsParsed => _svgProvider?.IsParsed ?? false;

	internal Size SourceSize => _svgProvider?.SourceSize ?? default;

	private void OnSourceLoaded(object? sender, EventArgs e) => SourceLoaded?.Invoke(this, EventArgs.Empty);
}
#endif
