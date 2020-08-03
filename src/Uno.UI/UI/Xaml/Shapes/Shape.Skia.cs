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
using Windows.Devices.Usb;
using System.Numerics;

namespace Windows.UI.Xaml.Shapes
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

		private protected void Render(Windows.UI.Composition.SkiaGeometrySource2D path, double? scaleX = null, double? scaleY = null, double? renderOriginX = null, double? renderOriginY = null)
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

				var scbFill = Fill as SolidColorBrush;
				var lgbFill = Fill as LinearGradientBrush;
				if (scbFill != null)
				{
					_fillSubscription.Disposable =
						Brush.AssignAndObserveBrush(scbFill, c => _pathSpriteShape.FillBrush = Visual.Compositor.CreateColorBrush(scbFill.Color));
				}
				else if (lgbFill != null)
				{
				}
			}
		}

		private void UpdateStrokeThickness()
		{
			if (_pathSpriteShape != null)
			{
				_pathSpriteShape.StrokeThickness = (float)StrokeThickness;
			}
		}

		private void UpdateStroke()
		{
			var brush = Stroke as SolidColorBrush ?? SolidColorBrushHelper.Transparent;

			if (_pathSpriteShape != null)
			{
				_strokeSubscription.Disposable =
					Brush.AssignAndObserveBrush(brush, c => _pathSpriteShape.StrokeBrush = Visual.Compositor.CreateColorBrush(brush.Color));
			}
		}
	}
}
