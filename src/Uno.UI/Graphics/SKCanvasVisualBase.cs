using System;
using Windows.Foundation;
using Microsoft.UI.Composition;

namespace Uno.UI.Graphics;

internal abstract class SKCanvasVisualBase : ContainerVisual
{
	/// <param name="renderCallback">The first parameter of the action must be an SkiaSharp.SKCanvas instance.</param>
	protected SKCanvasVisualBase(Action<object, Size> renderCallback, Compositor compositor) : base(compositor)
	{
		RenderCallback = renderCallback;
	}

	protected Action<object, Size> RenderCallback { get; }
	public abstract void Invalidate();
}
