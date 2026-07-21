#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Opaque, backend-created color-filter handle (e.g. an opacity/alpha modulation applied to a paint).
/// Passed to <see cref="IDrawingSession"/> draw verbs and produced by <see cref="IDrawingBackend"/>.
/// </summary>
public interface IColorFilter
{
}
