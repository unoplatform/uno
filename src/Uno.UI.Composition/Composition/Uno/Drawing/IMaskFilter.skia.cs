#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Opaque, backend-created mask filter (e.g. a blur applied to a paint's coverage mask). Referenced by
/// <see cref="PaintParams.MaskFilter"/> and produced by <see cref="IDrawingBackend"/>. Disposable because
/// backends typically build one per use (e.g. a per-frame shadow blur).
/// </summary>
internal interface IMaskFilter : IDisposable
{
}
