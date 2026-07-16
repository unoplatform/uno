#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Opaque, backend-created shader handle (gradients, image/tile shaders, …). Referenced by
/// passed to <see cref="IDrawingSession"/> draw verbs and produced by <see cref="IDrawingBackend"/> factory methods.
/// </summary>
/// <remarks>
/// Shaders are expensive to build and are cached by the producing brush across frames, so — like
/// <see cref="IGeometry"/> — they cross the boundary as handles rather than by value.
/// </remarks>
internal interface IShader
{
}
