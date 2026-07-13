#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Opaque, backend-created bitmap/image handle. Lifetime is owned by whatever produced it (an image
/// surface, a decode, a snapshot), so this handle is not itself disposable.
/// </summary>
internal interface IImage
{
	int PixelWidth { get; }
	int PixelHeight { get; }
}
