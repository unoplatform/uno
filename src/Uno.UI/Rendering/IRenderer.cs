#nullable enable

using SkiaSharp;

namespace Uno.UI.Rendering;

internal interface IRenderer
{
	SKColor BackgroundColor { get; set; }
}
