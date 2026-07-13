#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Opaque, backend-defined retained state produced by a recording and replayed via
/// <see cref="IDrawingSession.Draw"/>. It is <em>not</em> necessarily a display list — the SkiaSharp
/// backend stores an <c>SKPicture</c>, another backend may store a texture, a command buffer, or any
/// metadata it maintains for the recorded content. Composition holds it opaquely and never inspects it.
/// </summary>
internal interface IRenderData : IDisposable
{
}
