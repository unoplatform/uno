#nullable enable

using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IShader"/> wrapping an <see cref="SKShader"/>.</summary>
internal sealed class SkiaShader : DrawingResource, IShader
{
	public SkiaShader(SKShader shader) => Shader = shader;

	public SKShader Shader { get; }

	protected override void Free() => Shader.Dispose();
}
