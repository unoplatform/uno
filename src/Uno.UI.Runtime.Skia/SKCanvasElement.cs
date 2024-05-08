using System.Numerics;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using SkiaSharp;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// A wrapper around <see cref="SkiaVisual"/> that takes care of sizing, layouting and DPI.
/// </summary>
public abstract class SKCanvasElement : FrameworkElement
{
	private class SKCanvasVisual(SKCanvasElement owner, Compositor compositor) : SkiaVisual(compositor)
	{
		protected override void RenderOverride(SKCanvas canvas) => owner.RenderOverride(canvas, Size.ToSize());
	}

	private readonly SkiaVisual _skiaVisual;

	protected SKCanvasElement()
	{
		_skiaVisual = new SKCanvasVisual(this, ElementCompositionPreview.GetElementVisual(this).Compositor);
		Visual.Children.InsertAtTop(_skiaVisual);
	}

	/// <summary>
	/// The SkiaSharp drawing logic goes here.
	/// </summary>
	/// <param name="canvas">The SKCanvas that should be drawn on. The drawing will directly appear in the clipping area.</param>
	/// <param name="area">The dimensions of the clipping area.</param>
	protected abstract void RenderOverride(SKCanvas canvas, Size area);

	/// <summary>
	/// By default, SKCanvasElement uses all the <see cref="availableSize"/> given. Subclasses of SKCanvasElement
	/// should override this method if they need something different.
	/// </summary>
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

	protected override Size ArrangeOverride(Size finalSize)
	{
		_skiaVisual.Size = new Vector2((float)finalSize.Width, (float)finalSize.Height);
		// clipping is necessary in case a user does a canvas clear without any clipping defined.
		// In that case. the entire window will be cleared.
		_skiaVisual.Clip = _skiaVisual.Compositor.CreateRectangleClip(0, 0, (float)finalSize.Width, (float)finalSize.Height);

		if (finalSize.Width == Double.PositiveInfinity ||
			finalSize.Height == Double.PositiveInfinity ||
			double.IsNaN(finalSize.Width) ||
			double.IsNaN(finalSize.Height))
		{
			throw new ArgumentException($"{nameof(SKCanvasElement)} cannot be arranged with infinite or NaN values, but received finalSize={finalSize}.");
		}
		return finalSize;
	}
}
