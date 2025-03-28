#nullable enable

using Windows.Foundation;
using Windows.UI.Composition;
using System.Numerics;

namespace Windows.UI.Xaml.Shapes
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

		private protected virtual void Render(Windows.UI.Composition.SkiaGeometrySource2D? path, double? scaleX = null, double? scaleY = null, double? renderOriginX = null, double? renderOriginY = null)
		{
			if (path is null)
			{
				_geometry.Path = null;
				return;
			}

			_geometry.Path = new CompositionPath(path);
			_shape.Scale = scaleX != null && scaleY != null
				? new Vector2((float)scaleX.Value, (float)scaleY.Value)
				: Vector2.One;
			_shape.Offset = LayoutRound(new Vector2((float)(renderOriginX ?? 0), (float)(renderOriginY ?? 0)));

			UpdateRender();
		}

		private void UpdateRender()
		{
			OnFillBrushChanged();
			OnStrokeBrushChanged();
			UpdateStrokeThickness();
			UpdateStrokeDashArray();
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
	}
}
