#nullable enable

using Microsoft.UI.Composition;

namespace Uno.UI.Composition
{
	internal interface ICompositionImageSurfaceProvider
	{
		CompositionImageSurface? ImageSurface { get; }
	}
}
