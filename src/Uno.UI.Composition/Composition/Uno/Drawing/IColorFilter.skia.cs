#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Opaque, backend-created color-filter handle (e.g. an opacity/alpha modulation applied to a paint).
/// Referenced by <see cref="PaintParams.ColorFilter"/> and produced by <see cref="IDrawingBackend"/>.
/// </summary>
internal interface IColorFilter
{
}
