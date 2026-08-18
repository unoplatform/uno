#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Opaque, backend-created shader handle (gradients, image/tile shaders, …). Passed to <see cref="IDrawingSession"/>
/// draw verbs and produced by <see cref="IDrawingFactory"/>. Expensive to build and cached by the producing brush
/// across frames, so — like <see cref="IGeometry"/> — it crosses the boundary as a handle rather than by value.
/// </summary>
public interface IShader
{
}
