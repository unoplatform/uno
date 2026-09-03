#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Opaque, backend-owned handle to a realized effect (a translated <see cref="Windows.Graphics.Effects.IGraphicsEffect"/>
/// graph). The composition layer holds it without knowing how the backend realizes it, like <see cref="IShader"/>
/// and <see cref="IImage"/>.
/// </summary>
public interface IEffectFilter : IDisposable
{
}
