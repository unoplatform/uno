using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions;
using Microsoft.UI.Composition;
using Uno.Disposables;
using System.IO.Compression;
using SkiaSharp;
using System.Numerics;

namespace Microsoft.UI.Xaml.Shapes
{
	[Markup.ContentProperty(Name = "SvgChildren")]
	partial class Shape
	{
		private ShapeVisual _rectangleVisual;
		private SerialDisposable _fillSubscription = new SerialDisposable();
		private SerialDisposable _strokeSubscription = new SerialDisposable();
		private CompositionSpriteShape _pathSpriteShape;

		public Shape()
		{
			_rectangleVisual = Visual.Compositor.CreateShapeVisual();
			Visual.Children.InsertAtBottom(_rectangleVisual);

			InitCommonShapeProperties();
		}

		private Rect GetPathBoundingBox(SkiaGeometrySource2D path)
			=> path.Geometry.Bounds.ToRect();

		private bool IsFinite(double value) => !double.IsInfinity(value);

		private void InitCommonShapeProperties() { }

		private protected void Render(Microsoft.UI.Composition.SkiaGeometrySource2D path, double? scaleX = null, double? scaleY = null, double? renderOriginX = null, double? renderOriginY = null)
		{
			var compositionPath = new CompositionPath(path);
			var pathGeometry = Visual.Compositor.CreatePathGeometry();
			pathGeometry.Path = compositionPath;

			_pathSpriteShape = Visual.Compositor.CreateSpriteShape(pathGeometry);

			if (scaleX != null && scaleY != null)
			{
				_pathSpriteShape.Scale = new Vector2((float)scaleX.Value, (float)scaleY.Value);
			}
			else
			{
				_pathSpriteShape.Scale = new Vector2(1, 1);
			}

			_pathSpriteShape.Offset = LayoutRound(new Vector2((float)(renderOriginX ?? 0), (float)(renderOriginY ?? 0)));

			_rectangleVisual.Shapes.Clear();
			_rectangleVisual.Shapes.Add(_pathSpriteShape);

			UpdateRender();
		}

		private void UpdateRender()
		{
			UpdateFill();
			UpdateStroke();
			UpdateStrokeThickness();
		}

		private void UpdateFill()
		{
			if (_pathSpriteShape != null)
			{
				_fillSubscription.Disposable = null;

				_pathSpriteShape.FillBrush = null;

				_fillSubscription.Disposable =
							Brush.AssignAndObserveBrush(Fill, Visual.Compositor, compositionBrush => _pathSpriteShape.FillBrush = compositionBrush);
			}
		}

		private void UpdateStrokeThickness()
		{
			if (_pathSpriteShape != null)
			{
				_pathSpriteShape.StrokeThickness = (float)ActualStrokeThickness;
			}
		}

		private void UpdateStroke()
		{
			if (_pathSpriteShape != null)
			{
				_strokeSubscription.Disposable = null;

				_pathSpriteShape.StrokeBrush = null;

				_strokeSubscription.Disposable =
							Brush.AssignAndObserveBrush(Stroke, Visual.Compositor, compositionBrush => _pathSpriteShape.StrokeBrush = compositionBrush);
			}
		}
	}
}
