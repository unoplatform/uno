using System;
using System.Linq;
using Android.Graphics;
using Uno.Foundation.Logging;
using Uno.UI;
using Microsoft.UI.Xaml.Media;
using Canvas = Android.Graphics.Canvas;

namespace Microsoft.UI.Xaml.Shapes
{
	public partial class Shape
	{
		private APath _path;
		private global::Windows.Foundation.Rect _drawArea;
		protected global::Windows.Foundation.Rect _logicalRenderingArea;
		private static Paint _strokePaint;
		private static Paint _fillPaint;

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
			APath path,
			global::Windows.Foundation.Size? size = null,
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

			var matrix = new AMatrix();

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
				_fillPaint ??= new();
				fill.ApplyToFillPaint(_drawArea, _fillPaint);
				canvas.DrawPath(_path, _fillPaint);
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

			_strokePaint ??= new Paint();
			stroke.ApplyToStrokePaint(_drawArea, _strokePaint);
			SetStrokeDashEffect(_strokePaint);

			if (_drawArea.HasZeroArea())
			{
				//Draw the stroke as a fill because the shape has no area
				_strokePaint.SetStyle(Paint.Style.Fill);
				canvas.DrawCircle((float)(strokeThickness / 2), (float)(strokeThickness / 2), (float)(strokeThickness / 2), _strokePaint);
			}
			else
			{
				_strokePaint.StrokeWidth = (float)strokeThickness;
				canvas.DrawPath(_path, _strokePaint);
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

		private global::Windows.Foundation.Rect GetPathBoundingBox(APath path)
		{
			// This method should return the bounding box, *not including* control points.
			// On Android, there doesn't seem to be an easy built-in way to do that, since ComputeBounds will include control points.
			// There is currently no "ComputeTightBounds" method.
			// There are two ways to go:
			// 1. Compute the tight bounds ourselves.
			// 2. Use the native Approximate method to approximate the path into straight lines.
			// We go with 2 for now, but this can be revisited later if we found that 1 has more benefits.

			// From documentation (https://developer.android.com/reference/android/graphics/Path#approximate(float)):
			// Approximate the Path with a series of line segments. This returns float[] with the array containing point components. There are three components for each point, in order:
			// - Fraction along the length of the path that the point resides
			// - The x coordinate of the point
			// - The y coordinate of the point

			// So each point has three elements in the array.
			var approximatedPoints = path.Approximate(0.5f);
			if (approximatedPoints.Length < 6)
			{
				// Less than two points is quite meaningless and probably shouldn't happen unless the path is empty.
				// Fallback to ComputeBounds.
				var pathBounds = new RectF();
				path.ComputeBounds(pathBounds, true);
				return pathBounds;
			}

			float minX = float.MaxValue;
			float minY = float.MaxValue;
			float maxX = float.MinValue;
			float maxY = float.MinValue;

			for (int i = 0; i < approximatedPoints.Length; i += 3)
			{
				var currentX = approximatedPoints[i + 1];
				var currentY = approximatedPoints[i + 2];
				minX = Math.Min(minX, currentX);
				maxX = Math.Max(maxX, currentX);
				minY = Math.Min(minY, currentY);
				maxY = Math.Max(maxY, currentY);
			}

			return new global::Windows.Foundation.Rect(
				x: minX,
				y: minY,
				width: maxX - minX,
				height: maxY - minY);
		}

		protected APath GetOrCreatePath()
		{
			_path?.Reset();
			return _path ?? new APath();
		}

		protected global::Windows.Foundation.Rect TransformToLogical(global::Windows.Foundation.Rect renderingArea)
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

		protected global::Windows.Foundation.Size BasicArrangeOverride(global::Windows.Foundation.Size finalSize, Action<APath> action)
		{
			var (shapeSize, renderingArea) = ArrangeRelativeShape(finalSize);

			if (renderingArea.Width > 0 && renderingArea.Height > 0)
			{
				var logicalRenderingArea = TransformToLogical(renderingArea);

				if (!_logicalRenderingArea.Equals(logicalRenderingArea))
				{
					_logicalRenderingArea = logicalRenderingArea;
					APath path = GetOrCreatePath();
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

		private void OnFillBrushChanged() => InvalidateCommon();
		private void OnStrokeBrushChanged() => InvalidateCommon();

		private void InvalidateCommon()
		{
			// The try-catch here is primarily for the benefit of Android. This callback is raised when (say) the brush color changes,
			// which may happen when the system theme changes from light to dark. For app-level resources, a large number of views may
			// be subscribed to changes on the brush, including potentially some that have been removed from the visual tree, collected
			// on the native side, but not yet collected on the managed side (for Xamarin targets).

			// On Android, in practice this could result in ObjectDisposedExceptions when calling RequestLayout(). The try/catch is to
			// ensure that callbacks are correctly raised for remaining views referencing the brush which *are* still live in the visual tree.
			try
			{
				Invalidate();
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"Failed to invalidate for brush changed: {e}");
				}
			}
		}
	}
}
