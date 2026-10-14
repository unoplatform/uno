#nullable enable

using Microsoft.UI.Composition;

namespace Uno.WinUI.Graphics3DWebGpu;

/// <summary>
/// The composition visual backing <see cref="WebGpuCanvasElement"/>. A <see cref="BorderVisual"/> because the element
/// is a <c>Grid</c>, whose background and border it still draws; the element's texture goes on top.
/// </summary>
internal sealed class WebGpuCanvasVisual(WebGpuCanvasElement owner, Compositor compositor) : BorderVisual(compositor)
{
	internal override bool CanPaint() => true;

	internal override void Paint(in PaintingSession session)
	{
		base.Paint(in session);
		owner.Paint(session.Session, Size);
	}

	public void Invalidate() => Compositor.InvalidateRender(this);
}
