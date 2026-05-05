#nullable enable

using Windows.Foundation;
using Microsoft.UI.Composition;
using SkiaSharp;
using System.Numerics;

namespace Microsoft.UI.Xaml.Shapes
{
	partial class Shape
	{
		private readonly CompositionSpriteShape _shape;
		private readonly CompositionPathGeometry _geometry;

		private protected CompositionSpriteShape SpriteShape => _shape;

		public Shape()
		{
			var visual = Visual;
			var compositor = visual.Compositor;

			_geometry = compositor.CreatePathGeometry();
			_shape = compositor.CreateSpriteShape(_geometry);
#if DEBUG
			_shape.Comment = "#path";
#endif

			((ShapeVisual)visual).Shapes.Add(_shape);
		}

		private Rect GetPathBoundingBox(SkiaGeometrySource2D path)
			=> path.TightBounds.ToRect();

		private protected override ContainerVisual CreateElementVisual() => Compositor.GetSharedCompositor().CreateShapeVisual();

		private protected virtual void Render(Microsoft.UI.Composition.SkiaGeometrySource2D? path, double? scaleX = null, double? scaleY = null, double? renderOriginX = null, double? renderOriginY = null)
		{
			if (path is null)
			{
				_geometry.Path = null;
				return;
			}

			_geometry.Path = new CompositionPath(path);
			// Stretch goes through the geometry-only transform channel so it doesn't scale the
			// stroke (matching WinUI Path/Rectangle, where StrokeThickness stays constant
			// regardless of Stretch). The public CompositionShape.Scale stays at identity here
			// — it's reserved for true Composition-API transforms (used by AnimatedVisualSource
			// generated code), which DO scale strokes via the canvas.
			_shape.SetGeometryTransform(scaleX != null && scaleY != null
				? SKMatrix.CreateScale((float)scaleX.Value, (float)scaleY.Value)
				: SKMatrix.CreateIdentity());
			_shape.Offset = LayoutRound(new Vector2((float)(renderOriginX ?? 0), (float)(renderOriginY ?? 0)));

			UpdateRender();
		}

		private void UpdateRender()
		{
			OnFillBrushChanged();
			OnStrokeBrushChanged();
			UpdateStrokeThickness();
			UpdateStrokeDashArray();
			UpdateStrokeCaps();
			UpdateStrokeLineJoin();
			UpdateStrokeMiterLimit();
			UpdateStrokeDashOffset();
		}

		private void OnFillBrushChanged()
		{
			_shape.FillBrush = Fill?.GetOrCreateCompositionBrush(Visual.Compositor);
		}

		private void UpdateStrokeThickness()
		{
			_shape.StrokeThickness = (float)ActualStrokeThickness;
		}

		private void UpdateStrokeDashArray()
		{
			var compositionStrokeDashArray = new CompositionStrokeDashArray(_shape.Compositor);
			var strokeDashArray = StrokeDashArray;
			if (strokeDashArray is null)
			{
				_shape.StrokeDashArray = null;
				return;
			}

			for (int i = 0; i < strokeDashArray.Count; i++)
			{
				compositionStrokeDashArray.Add((float)strokeDashArray[i]);
			}

			_shape.StrokeDashArray = compositionStrokeDashArray;
		}

		private void OnStrokeBrushChanged()
		{
			_shape.StrokeBrush = Stroke?.GetOrCreateCompositionBrush(Visual.Compositor);
		}

		private void UpdateStrokeCaps()
		{
			_shape.StrokeStartCap = (CompositionStrokeCap)StrokeStartLineCap;
			_shape.StrokeEndCap = (CompositionStrokeCap)StrokeEndLineCap;
			_shape.StrokeDashCap = (CompositionStrokeCap)StrokeDashCap;
		}

		private void UpdateStrokeLineJoin()
		{
			_shape.StrokeLineJoin = (CompositionStrokeLineJoin)StrokeLineJoin;
		}

		private void UpdateStrokeMiterLimit()
		{
			_shape.StrokeMiterLimit = (float)StrokeMiterLimit;
		}

		private void UpdateStrokeDashOffset()
		{
			_shape.StrokeDashOffset = (float)StrokeDashOffset;
		}

		/// <summary>
		/// Returns a mask that represents the alpha channel of the shape as a CompositionBrush.
		/// This brush can be used with CompositionMaskBrush or DropShadow.Mask to create shaped effects.
		/// </summary>
		/// <returns>A CompositionBrush representing the shape as an alpha mask.</returns>
		public CompositionBrush GetAlphaMask()
		{
			var compositor = Compositor.GetSharedCompositor();
			var surface = new AlphaMaskSurface(compositor, Visual);
			var brush = compositor.CreateSurfaceBrush(surface);
			brush.Stretch = CompositionStretch.None;
			return brush;
		}
	}
}
