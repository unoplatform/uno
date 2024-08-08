using System;
using System.Numerics;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using SkiaSharp;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// A <see cref="FrameworkElement"/> that exposes the ability to draw directly on the window using SkiaSharp.
/// </summary>
/// <remarks>This is only available on skia-based targets.</remarks>
public abstract class SKCanvasElement : FrameworkElement
{
	private class SKCanvasVisual(SKCanvasElement owner, Compositor compositor) : Visual(compositor)
	{
		internal override void Paint(in PaintingSession session)
		{
			session.Canvas.Save();
			// clipping here guards against a naked canvas.Clear() call which would wipe out the entire window.
			session.Canvas.ClipRect(new SKRect(0, 0, Size.X, Size.Y));
			owner.RenderOverride(session.Canvas, Size.ToSize());
			session.Canvas.Restore();
		}

		public void Invalidate() => Compositor.InvalidateRender(this);
	}

	private readonly SKCanvasVisual _skiaVisual;

	protected SKCanvasElement()
	{
		_skiaVisual = new SKCanvasVisual(this, ElementCompositionPreview.GetElementVisual(this).Compositor);
		Visual.Children.InsertAtTop(_skiaVisual);

		SizeChanged += OnSizeChanged;
	}

	/// <summary>
	/// Queue a rendering cycle that will call <see cref="RenderOverride"/>.
	/// </summary>
	public void Invalidate() => _skiaVisual.Invalidate();

	/// <summary>
	/// The SkiaSharp drawing logic goes here.
	/// </summary>
	/// <param name="canvas">The SKCanvas that should be drawn on.</param>
	/// <param name="area">The dimensions of the clipping area.</param>
	/// <remarks>
	/// When called, the <paramref name="canvas"/> is already set up such that the origin (0,0) is at the top-left of the clipping area.
	/// Drawing outside this area (i.e. outside the (0, 0, area.Width, area.Height rectangle) will be clipped out.
	/// </remarks>
	protected abstract void RenderOverride(SKCanvas canvas, Size area);

	/// <summary>
	/// By default, SKCanvasElement uses all the <see cref="availableSize"/> given. Subclasses of <see cref="SKCanvasElement"/>
	/// should override this method if they need something different.
	/// </summary>
	/// <remarks>An exception will be thrown if availableSize is infinite (e.g. if inside a StackPanel).</remarks>
	protected override Size MeasureOverride(Size availableSize)
	{
		if (availableSize.Width == Double.PositiveInfinity ||
			availableSize.Height == Double.PositiveInfinity ||
			double.IsNaN(availableSize.Width) ||
			double.IsNaN(availableSize.Height))
		{
			throw new ArgumentException($"{nameof(SKCanvasElement)} cannot be measured with infinite or NaN values, but received availableSize={availableSize}.");
		}

		return availableSize;
	}

	/// <summary>
	/// By default, SKCanvasElement uses all the <see cref="finalSize"/> given. Subclasses of <see cref="SKCanvasElement"/>
	/// should override this method if they need something different.
	/// </summary>
	/// <remarks>An exception will be thrown if <see cref="finalSize"/> is infinite (e.g. if inside a StackPanel).</remarks>
	protected override Size ArrangeOverride(Size finalSize)
	{
		if (finalSize.Width == double.PositiveInfinity ||
			finalSize.Height == double.PositiveInfinity ||
			double.IsNaN(finalSize.Width) ||
			double.IsNaN(finalSize.Height))
		{
			throw new ArgumentException($"{nameof(SKCanvasElement)} cannot be arranged with infinite or NaN values, but received finalSize={finalSize}.");
		}
		return finalSize;
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs args) => _skiaVisual.Size = args.NewSize.ToVector2();
}
