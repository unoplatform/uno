#nullable enable

using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Media.Imaging.Svg;

/// <summary>
/// This interface is used internally by Uno Platform
/// to allow the installation of SVG Addin.
/// Avoid using this interface directly, as its signature
/// may change.
/// </summary>
public interface ISvgProvider
{
	UIElement GetCanvas();

	bool IsParsed { get; }

	Size SourceSize { get; }

	event EventHandler? SourceLoaded;

	Task<bool> TryLoadSvgDataAsync(byte[] imageData);

	/// <summary>Rasterizes the loaded SVG to a neutral image (an <c>IImage</c>) at the given pixel size, or null if unsupported. Typed as object to keep this cross-flavor interface free of the Skia-only Drawing types.</summary>
	object? RenderToImage(int pixelWidth, int pixelHeight) => default;

	void Unload();
}
