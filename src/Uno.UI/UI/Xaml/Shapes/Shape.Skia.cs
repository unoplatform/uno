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

		internal virtual SkiaGeometrySource2D GetGeometry(Size finalSize)
		{
			throw new NotSupportedException($"Path.GetGeometry must be implemented");
		}

		private void InitCommonShapeProperties() { }

		protected override Size MeasureOverride(Size availableSize)
		{
			var geometrySource = GetGeometry(availableSize);

			return new Size(geometrySource.Geometry.Bounds.Width, geometrySource.Geometry.Bounds.Height);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var area = new Rect(0, 0, finalSize.Width, finalSize.Height);

			switch (Stretch)
			{
				default:
				case Stretch.None:
					break;
				case Stretch.Fill:
					area = new Rect(0, 0, finalSize.Width, finalSize.Height);
					break;
				case Stretch.Uniform:
					area = (area.Height > area.Width)
						? (new Rect((float)area.X, (float)area.Y, (float)area.Width, (float)area.Width))
						: (new Rect((float)area.X, (float)area.Y, (float)area.Height, (float)area.Height));
					break;
				case Stretch.UniformToFill:
					area = (area.Height > area.Width)
						? (new Rect((float)area.X, (float)area.Y, (float)area.Height, (float)area.Height))
						: (new Rect((float)area.X, (float)area.Y, (float)area.Width, (float)area.Width));
					break;
			}

			var shrinkValue = -ActualStrokeThickness / 2;
			if (area != Rect.Empty)
			{
				area.Inflate(shrinkValue, shrinkValue);
			}

			var geometrySource = GetGeometry(finalSize);
			var compositionPath = new CompositionPath(geometrySource);
			var pathGeometry = Visual.Compositor.CreatePathGeometry();
			pathGeometry.Path = compositionPath;

			_pathSpriteShape = Visual.Compositor.CreateSpriteShape(pathGeometry);

			_rectangleVisual.Shapes.Clear();
			_rectangleVisual.Shapes.Add(_pathSpriteShape);

			UpdateFill();
			UpdateStroke();
			UpdateStrokeThickness();
			return finalSize; // geometrySource.Geometry.Bounds.Size.ToSize();
		}

		partial void OnStrokeUpdatedPartial()
		{
			UpdateStroke();

			InvalidateMeasure();
		}


		partial void OnFillUpdatedPartial()
		{
			UpdateFill();

			InvalidateMeasure();
		}

		partial void OnStrokeThicknessUpdatedPartial()
		{
			if (StrokeThickness > 0 && StrokeThickness < 1)
			{
				StrokeThickness = 1;
				return;
			}

			UpdateStrokeThickness();

			InvalidateMeasure();
		}

		partial void OnStrokeDashArrayUpdatedPartial()
		{
			// _rectangleLayer.LineDashPattern = newValue.Safe().Select(d => new NSNumber(d)).ToArray();

			InvalidateMeasure();
		}

		partial void OnStretchUpdatedPartial()
		{
			InvalidateMeasure();
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
