#if CROSSRUNTIME
using Microsoft.UI.Xaml;
using Silk.NET.OpenGL;
using Uno.Foundation.Extensibility;
using Uno.Graphics;
using Uno.WinUI.Graphics3DGL;

[assembly: ApiExtension(typeof(IGLIsland), typeof(GLIsland), typeof(IGLIslandRenderer))]

namespace Uno.WinUI.Graphics3DGL;

/// <summary>
/// The <see cref="IGLIsland"/> extension: a <see cref="GLCanvasElement"/> whose drawing is delegated to an
/// <see cref="IGLIslandRenderer"/>. Lets other add-ins (e.g. Graphics2DSK's GL fallback) render through GL
/// without referencing this assembly.
/// </summary>
public sealed class GLIsland(IGLIslandRenderer renderer) : GLCanvasElement(null), IGLIsland
{
	public FrameworkElement Element => this;

	protected override void Init(GL gl) => renderer.Init();

	protected override void OnDestroy(GL gl) => renderer.Destroy();

	protected override void OnGLUnavailable() => renderer.OnUnavailable();

	protected override void RenderOverride(GL gl)
	{
		var width = (int)RenderSize.Width;
		var height = (int)RenderSize.Height;
		if (width > 0 && height > 0)
		{
			// The base has already bound our offscreen FBO.
			renderer.Render((uint)gl.GetInteger(GLEnum.FramebufferBinding), width, height);
		}
	}
}
#endif
