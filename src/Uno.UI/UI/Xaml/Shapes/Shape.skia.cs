#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.Extensions;
using Windows.UI.Composition;
using Uno.Disposables;
using System.IO.Compression;
using SkiaSharp;
using System.Numerics;

namespace Windows.UI.Xaml.Shapes
{
	partial class Shape
	{
		private readonly SerialDisposable _fillSubscription = new();
		private readonly SerialDisposable _strokeSubscription = new();
		private readonly CompositionSpriteShape _shape;
		private readonly CompositionPathGeometry _geometry;

		public Shape()
		{
			var visual = Visual;
			var compositor = visual.Compositor;

			_geometry = compositor.CreatePathGeometry();
			_shape = compositor.CreateSpriteShape(_geometry);
#if DEBUG
			_shape.Comment = "#path";
#endif

			visual.Shapes.Add(_shape);
		}

		private Rect GetPathBoundingBox(SkiaGeometrySource2D path)
			=> path.Geometry.TightBounds.ToRect();

		private protected void Render(Windows.UI.Composition.SkiaGeometrySource2D? path, double? scaleX = null, double? scaleY = null, double? renderOriginX = null, double? renderOriginY = null)
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
			_fillSubscription.Disposable = null;

			_shape.FillBrush = null;

			_fillSubscription.Disposable = Brush.AssignAndObserveBrush(Fill, Visual.Compositor, compositionBrush => _shape.FillBrush = compositionBrush);
		}

		private void UpdateStrokeThickness()
		{
			_shape.StrokeThickness = (float)ActualStrokeThickness;
		}

		private void UpdateStrokeDashArray()
		{
			var compositionStrokeDashArray = new CompositionStrokeDashArray();
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
			_strokeSubscription.Disposable = null;

			_shape.StrokeBrush = null;

			_strokeSubscription.Disposable = Brush.AssignAndObserveBrush(Stroke, Visual.Compositor, compositionBrush => _shape.StrokeBrush = compositionBrush);
		}
	}
}
