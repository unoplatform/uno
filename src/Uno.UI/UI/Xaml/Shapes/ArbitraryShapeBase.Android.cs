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

			var drawables = new List<Drawable>();

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
			var drawArea = new Foundation.Rect(0, 0, _controlWidth, _controlHeight);

			if (Fill is ImageBrush imageBrushFill)
			{
				var bitmapDrawable = new BitmapDrawable(Context.Resources, imageBrushFill.TryGetBitmap(drawArea, () => RefreshShape(forceRefresh: true), path));
				drawables.Add(bitmapDrawable);
			}
			else
			{
				var fill = Fill ?? SolidColorBrushHelper.Transparent;
				var fillPaint = fill.GetFillPaint(drawArea);

				var lineDrawable = new PaintDrawable();
				lineDrawable.Shape = new PathShape(path, (float)_controlWidth, (float)_controlHeight);
				lineDrawable.Paint.Color = fillPaint.Color;
				lineDrawable.Paint.SetShader(fillPaint.Shader);
				lineDrawable.Paint.SetStyle(Paint.Style.Fill);
				lineDrawable.Paint.Alpha = fillPaint.Alpha;

				SetStrokeDashEffect(lineDrawable.Paint);

				drawables.Add(lineDrawable);
			}

			// Draw the contour
			var stroke = Stroke;
			if (stroke != null)
			{
				using (var strokeBrush = new Paint(stroke.GetStrokePaint(drawArea)))
				{
					var lineDrawable = new PaintDrawable();
					lineDrawable.Shape = new PathShape(path, (float)_controlWidth, (float)_controlHeight);
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
			_lastAvailableSize = availableSize;
			var path = GetPath(availableSize);
			if (path == null)
			{
				return default;
			}

			var physicalBounds = new RectF();
			path.ComputeBounds(physicalBounds, false);

			var bounds = ViewHelper.PhysicalToLogicalPixels(physicalBounds);

			var pathWidth = bounds.Width();
			var pathHeight = bounds.Height();

			if (ShouldPreserveOrigin)
			{
				pathWidth += bounds.Left;
				pathHeight += bounds.Top;
			}

			var availableWidth = availableSize.Width;
			var availableHeight = availableSize.Height;

			var userWidth = Width;
			var userHeight = Height;

			var controlWidth = availableWidth <= 0 ? userWidth : availableWidth;
			var controlHeight = availableHeight <= 0 ? userHeight : availableHeight;

			// Default values
			var calculatedWidth = LimitWithUserSize(controlWidth, userWidth, pathWidth);
			var calculatedHeight = LimitWithUserSize(controlHeight, userHeight, pathHeight);

			var strokeThickness = ActualStrokeThickness;

			_scaleX = (calculatedWidth - strokeThickness) / pathWidth;
			_scaleY = (calculatedHeight - strokeThickness) / pathHeight;

			//Make sure that we have a valid scale if both of them are not set
			if (double.IsInfinity(_scaleX) || double.IsNaN(_scaleX))
			{
				_scaleX = 1;
			}
			if (double.IsInfinity(_scaleY) || double.IsNaN(_scaleY))
			{
				_scaleY = 1;
			}

			// Here we will override some of the default values
			switch (Stretch)
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

			calculatedWidth += strokeThickness;
			calculatedHeight += strokeThickness;

			return new Size(calculatedWidth, calculatedHeight);
		}
	}
}
