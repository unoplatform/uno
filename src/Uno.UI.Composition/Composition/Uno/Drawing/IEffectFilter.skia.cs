#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Opaque, backend-owned handle to a realized effect (an <see cref="Windows.Graphics.Effects.IGraphicsEffect"/>
/// graph translated by the backend). The composition layer holds it without knowing how the backend
/// realizes it (e.g. an SKImageFilter DAG, possibly SkSL-based, for the Skia backend), mirroring the way
/// <see cref="IShader"/> and <see cref="IImage"/> hide their backing resources.
/// </summary>
internal interface IEffectFilter : IDisposable
{
}
