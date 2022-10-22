#nullable enable

using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;

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

	void Unload();
}
