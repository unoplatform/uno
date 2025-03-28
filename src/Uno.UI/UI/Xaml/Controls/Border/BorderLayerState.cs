#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI;
using Windows.UI.Xaml.Media.Imaging;

namespace Uno.UI.Xaml.Controls;

// TODO: Remove Android-specific code when BorderLayerRenderer can handle brush changes on its own.

/// <summary>
/// Represents a state of border layer.
/// </summary>
/// <param name="ElementSize">Element size.</param>
/// <param name="Background">Background brush.</param>
/// <param name="BackgroundSizing">Background sizing.</param>
/// <param name="BorderBrush">Border brush.</param>
/// <param name="BorderThickness">Border thickness.</param>
/// <param name="CornerRadius">Corner radius.</param>
internal record struct BorderLayerState(
	Size ElementSize,
	Brush? Background,
	BackgroundSizing BackgroundSizing,
	Brush? BorderBrush,
	Thickness BorderThickness,
	CornerRadius CornerRadius
#if __ANDROID__
	,
	ImageSource? BackgroundImageSource,
	Uri? BackgroundImageSourceUri,
	Color? BackgroundColor,
	Color? BackgroundFallbackColor,
	Color? BorderBrushColor
#endif
	)
{
	internal BorderLayerState(Size elementSize, IBorderInfoProvider borderInfoProvider) : this(
		elementSize,
		borderInfoProvider.Background,
		borderInfoProvider.BackgroundSizing,
		borderInfoProvider.BorderBrush,
		borderInfoProvider.BorderThickness,
		borderInfoProvider.CornerRadius
#if __ANDROID__
		,
		GetBackgroundImageSource(borderInfoProvider.Background),
		GetBackgroundImageSourceUri(borderInfoProvider.Background),
		(borderInfoProvider.Background as SolidColorBrush)?.ColorWithOpacity,
		(borderInfoProvider.Background as XamlCompositionBrushBase)?.FallbackColorWithOpacity,
		(borderInfoProvider.BorderBrush as SolidColorBrush)?.ColorWithOpacity
#endif
		)
	{
	}

#if __ANDROID__
	private static ImageSource? GetBackgroundImageSource(Brush? background) => (background as ImageBrush)?.ImageSource;

	private static Uri? GetBackgroundImageSourceUri(Brush? background)
	{
		var source = GetBackgroundImageSource(background);
		return source switch
		{
			BitmapImage bitmapImage => bitmapImage.UriSource,
			SvgImageSource svgImageSource => svgImageSource.UriSource,
			_ => null
		};
	}
#endif
}
