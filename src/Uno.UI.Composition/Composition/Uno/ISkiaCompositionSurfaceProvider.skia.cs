#nullable enable

using SkiaSharp;
using Windows.UI.Composition;

namespace Uno.UI.Composition
{
	internal interface ISkiaCompositionSurfaceProvider
	{
		SkiaCompositionSurface? SkiaCompositionSurface { get; }
	}
}
