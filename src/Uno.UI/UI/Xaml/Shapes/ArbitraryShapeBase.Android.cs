using Android.Graphics;
using System;
using System.Collections.Generic;
using Uno.UI;
using Windows.Foundation;
using Uno.Disposables;
using Android.Graphics.Drawables;
using Android.Graphics.Drawables.Shapes;
using Android.Views;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Shapes
{
	public abstract partial class ArbitraryShapeBase : Shape
	{
		// Drawing scale
		private double _scaleX;
		private double _scaleY;

		// Drawing container size
		private double _controlWidth;
		private double _controlHeight;
		private Size _lastAvailableSize;

		private Size GetActualSize() => new Size(_controlWidth, _controlHeight);

		protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
		{
			// Don't call base, we need to keep UIView.BackgroundColor set to transparent
			RefreshShape();
		}

		protected override void OnLoaded()
		{
			base.OnLoaded();

			RefreshShape();
		}

		protected abstract Android.Graphics.Path GetPath(Size availableSize);

		private IDisposable BuildDrawableLayer()
		{
			if (_controlHeight == 0 || _controlWidth == 0)
			{
				return Disposable.Empty;
			}

			var fill = Fill;
			var stroke = Stroke;

			if(fill == null && stroke == null)
			{
				// Nothing to draw!
				return Disposable.Empty;
			}

			// predict list capacity to reduce memory handling
			var drawablesCapacity = fill != null && stroke != null ? 2 : 1;
			var drawables = new List<Drawable>(drawablesCapacity);

			var path = GetPath(_lastAvailableSize);
			if (path == null)
			{
				return Disposable.Empty;
			}

			// Scale the path using its Stretch
			var matrix = new Android.Graphics.Matrix();
			var stretchMode = Stretch;

			switch (stretchMode)
			{
				case Stretch.Fill:
				case Stretch.None:
					matrix.SetScale((float)_scaleX, (float)_scaleY);
					break;
				case Stretch.Uniform:
					var scale = Math.Min(_scaleX, _scaleY);
					matrix.SetScale((float)scale, (float)scale);
					break;
				case Stretch.UniformToFill:
					scale = Math.Max(_scaleX, _scaleY);
					matrix.SetScale((float)scale, (float)scale);
					break;
			}
			path.Transform(matrix);

			// Move the path using its alignements
			var translation = new Android.Graphics.Matrix();

			var pathBounds = new RectF();

			// Compute the bounds. This is needed for stretched shapes and stroke thickness translation calculations.
			path.ComputeBounds(pathBounds, true);

			if (stretchMode == Stretch.None)
			{
				// Since we are not stretching, ensure we are using (0, 0) as origin.
				pathBounds.Left = 0;
				pathBounds.Top = 0;
			}

			if (!ShouldPreserveOrigin)
			{
				//We need to translate the shape to take in account the stroke thickness
				translation.SetTranslate((float)(-pathBounds.Left + PhysicalStrokeThickness * 0.5f), (float)(-pathBounds.Top + PhysicalStrokeThickness * 0.5f));
			}

			path.Transform(translation);

			// Draw the fill
			var drawArea = new Windows.Foundation.Rect(0, 0, _controlWidth, _controlHeight);

			if (fill is ImageBrush fillImageBrush)
			{
				var bitmapDrawable = new BitmapDrawable(Context.Resources, fillImageBrush.TryGetBitmap(drawArea, () => RefreshShape(forceRefresh: true), path));
				drawables.Add(bitmapDrawable);
			}
			else if(fill != null)
			{
				var fillPaint = fill.GetFillPaint(drawArea);

				var lineDrawable = new PaintDrawable
				{
					Shape = new PathShape(path, (float)_controlWidth, (float)_controlHeight)
				};
				lineDrawable.Paint.Color = fillPaint.Color;
				lineDrawable.Paint.SetShader(fillPaint.Shader);
				lineDrawable.Paint.SetStyle(Paint.Style.Fill);
				lineDrawable.Paint.Alpha = fillPaint.Alpha;

				drawables.Add(lineDrawable);
			}

			// Draw the contour
			if (stroke != null)
			{
				using (var strokeBrush = new Paint(stroke.GetStrokePaint(drawArea)))
				{
					var lineDrawable = new PaintDrawable
					{
						Shape = new PathShape(path, (float)_controlWidth, (float)_controlHeight)
					};
					lineDrawable.Paint.Color = strokeBrush.Color;
					lineDrawable.Paint.SetShader(strokeBrush.Shader);
					lineDrawable.Paint.StrokeWidth = (float)PhysicalStrokeThickness;
					lineDrawable.Paint.SetStyle(Paint.Style.Stroke);
					lineDrawable.Paint.Alpha = strokeBrush.Alpha;

					SetStrokeDashEffect(lineDrawable.Paint);

					drawables.Add(lineDrawable);
				}
			}

			var layerDrawable = new LayerDrawable(drawables.ToArray());

			// Set bounds must always be called, otherwise the android layout engine can't determine
			// the rendering size. See Drawable documentation for details.
			layerDrawable.SetBounds(0, 0, (int)_controlWidth, (int)_controlHeight);

			return SetOverlay(this, layerDrawable);
		}

		private IDisposable SetOverlay(View view, Drawable drawable)
		{
#if __ANDROID_18__
			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Kitkat)
			{
				using (PreventRequestLayout())
				{
					Overlay.Add(drawable);
				}

				return Disposable.Create(
					() =>
					{
						using (PreventRequestLayout())
						{
							Overlay.Remove(drawable);
						}
					}
				);
			}
			else
#endif
			{
#pragma warning disable 0618 // Used for compatibility with SetBackgroundDrawable and previous API Levels

				// Set overlay is not supported by this platform, set use the background instead.
				// It'll break some scenarios, like having borders on top of the content.
				view.SetBackgroundDrawable(drawable);
				return Disposable.Create(() => view.SetBackgroundDrawable(null));

#pragma warning restore 0618
			}
		}

		protected override void OnLayoutCore(bool changed, int left, int top, int right, int bottom)
		{
			_controlWidth = right - left;
			_controlHeight = bottom - top;

			RefreshShape();
			base.OnLayoutCore(changed, left, top, right, bottom);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			if (availableSize == default)
			{
				return default;
			}

			var strokeThickness = ActualStrokeThickness;
			var strokeSize = new Size(strokeThickness, strokeThickness);

			var availableSizeMinusStroke = availableSize.Subtract(strokeSize);

			var availableSizeWithAppliedConstrains =
				this.ApplySizeConstraints(availableSizeMinusStroke, strokeSize);

			_lastAvailableSize = availableSizeWithAppliedConstrains;

			var path = GetPath(availableSizeWithAppliedConstrains);
			if (path == null)
			{
				return default;
			}

			var physicalBounds = new RectF();
			path.ComputeBounds(physicalBounds, false);

			var bounds = physicalBounds.PhysicalToLogicalPixels();

			var stretch = Stretch;
			var preserveOrigin = ShouldPreserveOrigin || stretch == Stretch.None;
			var pathWidth = preserveOrigin ? bounds.Right : bounds.Width();
			var pathHeight = preserveOrigin ? bounds.Bottom : bounds.Height();

			_scaleX = availableSizeWithAppliedConstrains.Width / pathWidth;
			_scaleY = availableSizeWithAppliedConstrains.Height / pathHeight;

			//Make sure that we have a valid scale if both of them are not set
			if (double.IsInfinity(_scaleX) || double.IsNaN(_scaleX))
			{
				_scaleX = 1;
			}
			if (double.IsInfinity(_scaleY) || double.IsNaN(_scaleY))
			{
				_scaleY = 1;
			}

			var calculatedWidth = 0d;
			var calculatedHeight = 0d;

			// Calculate size following stretch mode
			switch (stretch)
			{
				// If the Stretch is None, the drawing is not the same size as the control
				case Stretch.None:
					_scaleX = 1;
					_scaleY = 1;
					calculatedWidth = pathWidth;
					calculatedHeight = pathHeight;
					break;
				case Stretch.Fill:
					calculatedWidth = pathWidth * _scaleX;
					calculatedHeight = pathHeight * _scaleY;

					break;
				// Override the _calculated dimensions if the stretch is Uniform or UniformToFill
				case Stretch.Uniform:
				{
					var scale = Math.Min(_scaleX, _scaleY);
					calculatedWidth = pathWidth * scale;
					calculatedHeight = pathHeight * scale;
					break;
				}
				case Stretch.UniformToFill:
				{
					var scale = Math.Max(_scaleX, _scaleY);
					calculatedWidth = pathWidth * scale;
					calculatedHeight = pathHeight * scale;
					break;
				}
			}

			var calculatedSize =
				new Size(
					calculatedWidth,
					calculatedHeight)
					.Add(strokeSize);

			return calculatedSize;
		}
	}
}
