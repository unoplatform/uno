using Android.Graphics;
using Uno.Foundation.Logging;
using Uno.UI;
using System.Linq;
using Microsoft.UI.Xaml.Media;
using Canvas = Android.Graphics.Canvas;
using System;

namespace Microsoft.UI.Xaml.Shapes
{
	public partial class Shape
	{
		private Android.Graphics.Path _path;
		private Windows.Foundation.Rect _drawArea;
		protected Windows.Foundation.Rect _logicalRenderingArea;

		protected bool HasStroke
		{
			get { return Stroke != null && ActualStrokeThickness > 0; }
		}

		internal double PhysicalStrokeThickness => ViewHelper.LogicalToPhysicalPixels((double)ActualStrokeThickness);

		public Shape()
		{
			SetWillNotDraw(false);
		}

		protected override void OnDraw(Canvas canvas)
		{
			base.OnDraw(canvas);
			if (_path == null)
			{
				return;
			}

			//Drawing paths on the canvas does not respect the canvas' ClipBounds
			if (ClippedFrame is { } clippedFrame)
			{
				clippedFrame = clippedFrame.LogicalToPhysicalPixels();
				if (FrameRoundingAdjustment is { } fra)
				{
					clippedFrame.Width += fra.Width;
					clippedFrame.Height += fra.Height;
				}

				canvas.ClipRect(clippedFrame.ToRectF());
			}

			DrawFill(canvas);
			DrawStroke(canvas);
		}

		private protected void Render(
			Android.Graphics.Path path,
			Windows.Foundation.Size? size = null,
			double scaleX = 1d,
			double scaleY = 1d,
			double renderOriginX = 0d,
			double renderOriginY = 0d)
		{
			if (path == null)
			{
				if (_path != null)
				{
					_path = null;
					_drawArea = new RectF();
					Invalidate();
				}
				return;
			}
			_path = path;

			var matrix = new Android.Graphics.Matrix();

			matrix.SetScale((float)scaleX * (float)ViewHelper.Scale, (float)scaleY * (float)ViewHelper.Scale);
			matrix.PostTranslate(ViewHelper.LogicalToPhysicalPixels(renderOriginX), ViewHelper.LogicalToPhysicalPixels(renderOriginY));

			_path.Transform(matrix);

			_drawArea = GetPathBoundingBox(_path);

			_drawArea.Width = size?.Width ?? _drawArea.Width;
			_drawArea.Height = size?.Height ?? _drawArea.Height;

			Invalidate();
		}

		private void DrawFill(Canvas canvas)
		{
			if (_drawArea.HasZeroArea())
			{
				return;
			}

			if (Fill is ImageBrush imageBrushFill)
			{
				imageBrushFill.ScheduleRefreshIfNeeded(_drawArea, Invalidate);
				imageBrushFill.DrawBackground(canvas, _drawArea, _path);
			}
			else
			{
				var fill = Fill ?? SolidColorBrushHelper.Transparent;
				var fillPaint = fill.GetFillPaint(_drawArea);
				canvas.DrawPath(_path, fillPaint);
			}
		}

		private void DrawStroke(Canvas canvas)
		{
			var stroke = Stroke;
			if (!HasStroke || stroke is ImageBrush)
			{
				return;
			}

			var strokeThickness = PhysicalStrokeThickness;

			using (var strokePaint = new Paint(stroke.GetStrokePaint(_drawArea)))
			{
				SetStrokeDashEffect(strokePaint);

				if (_drawArea.HasZeroArea())
				{
					//Draw the stroke as a fill because the shape has no area
					strokePaint.SetStyle(Paint.Style.Fill);
					canvas.DrawCircle((float)(strokeThickness / 2), (float)(strokeThickness / 2), (float)(strokeThickness / 2), strokePaint);
				}
				else
				{
					strokePaint.StrokeWidth = (float)strokeThickness;
					canvas.DrawPath(_path, strokePaint);
				}
			}
		}

		private void SetStrokeDashEffect(Paint strokePaint)
		{
			var strokeDashArray = StrokeDashArray;

			if (strokeDashArray != null && strokeDashArray.Count > 0)
			{
				// If only value specified in the dash array, copy and add it
				if (strokeDashArray.Count == 1)
				{
					strokeDashArray.Add(strokeDashArray[0]);
				}

				// Make sure the dash array has a positive number of items, Android cannot have an odd number
				// of items in the array (in such a case we skip the dash effect and log the error)
				//		https://developer.android.com/reference/android/graphics/DashPathEffect.html
				//		**  The intervals array must contain an even number of entries (>=2), with
				//			the even indices specifying the "on" intervals, and the odd indices
				//			specifying the "off" intervals.  **
				if (strokeDashArray.Count % 2 == 0)
				{
					var pattern = strokeDashArray.Select(d => (float)d).ToArray();
					strokePaint.SetPathEffect(new DashPathEffect(pattern, 0));
				}
				else
				{
					this.Log().Error("StrokeDashArray containing an odd number of values is not supported on Android.");
				}
			}
		}

		private Windows.Foundation.Rect GetPathBoundingBox(Android.Graphics.Path path)
		{
			//There is currently a bug here, Android's ComputeBounds includes the control points of a Bezier path
			//which can result in improper positioning when aligning paths with Bezier segments.
			var pathBounds = new RectF();
			path.ComputeBounds(pathBounds, true);
			return pathBounds;
		}

		protected Android.Graphics.Path GetOrCreatePath()
		{
			_path?.Reset();
			return _path ?? new Android.Graphics.Path();
		}

		protected Windows.Foundation.Rect TransformToLogical(Windows.Foundation.Rect renderingArea)
		{
			//Android's path rendering logic rounds values down to the nearest int, make sure we round up here instead using the ViewHelper scaling logic
			var physicalRenderingArea = renderingArea.LogicalToPhysicalPixels();
			if (FrameRoundingAdjustment is { } fra)
			{
				physicalRenderingArea.Height += fra.Height;
				physicalRenderingArea.Width += fra.Width;
			}

			var logicalRenderingArea = physicalRenderingArea.PhysicalToLogicalPixels();
			logicalRenderingArea.X = renderingArea.X;
			logicalRenderingArea.Y = renderingArea.Y;

			return logicalRenderingArea;
		}

		protected Windows.Foundation.Size BasicArrangeOverride(Windows.Foundation.Size finalSize, Action<Android.Graphics.Path> action)
		{
			var (shapeSize, renderingArea) = ArrangeRelativeShape(finalSize);

			if (renderingArea.Width > 0 && renderingArea.Height > 0)
			{
				var logicalRenderingArea = TransformToLogical(renderingArea);

				if (!_logicalRenderingArea.Equals(logicalRenderingArea))
				{
					_logicalRenderingArea = logicalRenderingArea;
					Android.Graphics.Path path = GetOrCreatePath();
					action(path);
					Render(path);
				}
			}
			else if (_path != null)
			{
				Render(null);
			}

			return shapeSize;
		}
	}
}
