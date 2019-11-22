using Android.Graphics;
using Uno.Media;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Windows.Foundation;
using Uno.Disposables;
using Android.Graphics.Drawables;
using Android.Graphics.Drawables.Shapes;
using Android.Views;
using Uno.Extensions;
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

		public ArbitraryShapeBase()
		{

		}

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

		protected abstract Android.Graphics.Path GetPath();

		private IDisposable BuildDrawableLayer()
		{
			if (_controlHeight == 0 || _controlWidth == 0)
			{
				return Disposable.Empty;
			}

			var drawables = new List<Drawable>();

			var path = GetPath();
			if (path == null)
			{
				return Disposable.Empty;
			}

			// Scale the path using its Stretch
			Android.Graphics.Matrix matrix = new Android.Graphics.Matrix();
			switch (this.Stretch)
			{
				case Media.Stretch.Fill:
				case Media.Stretch.None:
					matrix.SetScale((float)_scaleX, (float)_scaleY);
					break;
				case Media.Stretch.Uniform:
					var scale = Math.Min(_scaleX, _scaleY);
					matrix.SetScale((float)scale, (float)scale);
					break;
				case Media.Stretch.UniformToFill:
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

			if (Stretch == Stretch.None)
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

			var imageBrushFill = Fill as ImageBrush;
			if (imageBrushFill != null)
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

				this.SetStrokeDashEffect(lineDrawable.Paint);

				drawables.Add(lineDrawable);
			}

			// Draw the contour
			if (Stroke != null)
			{
				using (var strokeBrush = new Paint(Stroke.GetStrokePaint(drawArea)))
				{
					var lineDrawable = new PaintDrawable();
					lineDrawable.Shape = new PathShape(path, (float)_controlWidth, (float)_controlHeight);
					lineDrawable.Paint.Color = strokeBrush.Color;
					lineDrawable.Paint.SetShader(strokeBrush.Shader);
					lineDrawable.Paint.StrokeWidth = (float)PhysicalStrokeThickness;
					lineDrawable.Paint.SetStyle(Paint.Style.Stroke);
					lineDrawable.Paint.Alpha = strokeBrush.Alpha;

					this.SetStrokeDashEffect(lineDrawable.Paint);

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

		protected override Size MeasureOverride(Size size)
		{
			var path = GetPath();
			if (path == null)
			{
				return default(Size);
			}
			var physicalBounds = new RectF();
			if (path != null)
			{
				path.ComputeBounds(physicalBounds, false);
			}

			var bounds = ViewHelper.PhysicalToLogicalPixels(physicalBounds);

			var pathWidth = bounds.Width();
			var pathHeight = bounds.Height();

			if (ShouldPreserveOrigin)
			{
				pathWidth += bounds.Left;
				pathHeight += bounds.Top;
			}

			var availableWidth = size.Width;
			var availableHeight = size.Height;

			var userWidth = this.Width;
			var userHeight = this.Height;

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
			switch (this.Stretch)
			{
				// If the Stretch is None, the drawing is not the same size as the control
				case Stretch.None:
					_scaleX = 1;
					_scaleY = 1;
					calculatedWidth = (double)pathWidth;
					calculatedHeight = (double)pathHeight;
					break;
				case Stretch.Fill:
					calculatedWidth = (double)pathWidth * (double)_scaleX;
					calculatedHeight = (double)pathHeight * (double)_scaleY;

					break;
				// Override the _calculated dimensions if the stretch is Uniform or UniformToFill
				case Stretch.Uniform:
					double scale = (double)Math.Min(_scaleX, _scaleY);
					calculatedWidth = (double)pathWidth * scale;
					calculatedHeight = (double)pathHeight * scale;
					break;
				case Stretch.UniformToFill:
					scale = (double)Math.Max(_scaleX, _scaleY);
					calculatedWidth = (double)pathWidth * scale;
					calculatedHeight = (double)pathHeight * scale;
					break;
			}

			calculatedWidth += strokeThickness;
			calculatedHeight += strokeThickness;

			return new Size(calculatedWidth, calculatedHeight);
		}
	}
}
