#pragma warning disable 0618 // Used for compatibility with SetBackgroundDrawable and previous API Levels

using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Graphics.Drawables.Shapes;
using Android.Views;
using Uno.Extensions;
using Uno.UI.Controls;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI;
using Android.Support.V4.View;
using System.Diagnostics;

namespace Windows.UI.Xaml.Controls
{
	internal class BorderLayerRenderer
	{
		private const double __opaqueAlpha = 255;

		private LayoutState _currentState;

		private readonly SerialDisposable _layerDisposable = new SerialDisposable();

		/// <summary>
		/// Updates or creates a sublayer to render a border-like shape.
		/// </summary>
		/// <param name="view">The view to which we should add the layers</param>
		/// <param name="background">The background brush of the border</param>
		/// <param name="borderThickness">The border thickness</param>
		/// <param name="borderBrush">The border brush</param>
		/// <param name="cornerRadius">The corner radius</param>
		/// <param name="padding">The padding to apply on the content</param>
		public void UpdateLayers(
			FrameworkElement view,
			Brush background,
			Thickness borderThickness,
			Brush borderBrush,
			CornerRadius cornerRadius,
			Thickness padding,
			bool willUpdateMeasures = false
			)
		{
			// This is required because android Height and Width are hidden by Control.
			var baseView = view as View;

			var drawArea = view.LayoutSlot.LogicalToPhysicalPixels();
			var newState = new LayoutState(drawArea, background, borderThickness, borderBrush, cornerRadius, padding);
			var previousLayoutState = _currentState;

			if (!newState.Equals(previousLayoutState))
			{
				bool imageHasChanged = newState.BackgroundImageSource != previousLayoutState?.BackgroundImageSource;
				bool shouldDisposeEagerly = imageHasChanged || newState.BackgroundImageSource == null;
				if (shouldDisposeEagerly)
				{

					// Clear previous value anyway in order to make sure the previous values are unset before the new ones.
					// This prevents the case where a second update would set a new background and then set the background to null when disposing the previous.
					_layerDisposable.Disposable = null; 
				}

				Action onImageSet = null;
				var disposable = InnerCreateLayers(view, drawArea, background, borderThickness, borderBrush, cornerRadius, () => onImageSet?.Invoke());
				
				// Most of the time we immediately dispose the previous layer. In the case where we're using an ImageBrush,
				// and the backing image hasn't changed, we dispose the previous layer at the moment the new background is applied,
				// to prevent a visible flicker.
				if (shouldDisposeEagerly)
				{
					_layerDisposable.Disposable = disposable;
				}
				else
				{
					onImageSet = () => _layerDisposable.Disposable = disposable;
				}

				if (willUpdateMeasures)
				{
					view.RequestLayout();
				}
				else
				{
					view.Invalidate();
				}

				_currentState = newState;
			}
		}

		/// <summary>
		/// Removes the added layers during a call to <see cref="UpdateLayers" />.
		/// </summary>
		internal void Clear()
		{
			_layerDisposable.Disposable = null;
			_currentState = null;
		}

		private static IDisposable InnerCreateLayers(BindableView view,
			Windows.Foundation.Rect drawArea,
			Brush background,
			Thickness borderThickness,
			Brush borderBrush,
			CornerRadius cornerRadius,
			Action onImageSet
		)
		{
			var disposables = new CompositeDisposable();

			var physicalBorderThickness = borderThickness.LogicalToPhysicalPixels();

			if (cornerRadius != 0)
			{
				using (var path = cornerRadius.GetOutlinePath(drawArea.ToRectF()))
				{
					path.SetFillType(Path.FillType.EvenOdd);

					//We only need to set a background if the drawArea is non-zero
					if (!drawArea.HasZeroArea())
					{
						var imageBrushBackground = background as ImageBrush;
						if (imageBrushBackground != null)
						{
							//Copy the path because it will be disposed when we exit the using block
							var pathCopy = new Path(path);
							var setBackground = DispatchSetImageBrushAsBackground(view, imageBrushBackground, drawArea, onImageSet, pathCopy);
							disposables.Add(setBackground);
						}
						else
						{
							var fillPaint = background?.GetFillPaint(drawArea) ?? new Paint() { Color = Android.Graphics.Color.Transparent };
							ExecuteWithNoRelayout(view, v => v.SetBackgroundDrawable(GetBackgroundDrawable(background, drawArea, fillPaint, path)));
						}
						disposables.Add(() => ExecuteWithNoRelayout(view, v => v.SetBackgroundDrawable(null)));
					}

					if (borderThickness != Thickness.Empty && borderBrush != null && !(borderBrush is ImageBrush))
					{
						using (var strokePaint = new Paint(borderBrush.GetStrokePaint(drawArea)))
						{
							var overlay = GetOverlayDrawable(strokePaint, physicalBorderThickness, new global::System.Drawing.Size((int)drawArea.Width, (int)drawArea.Height), path);

							if (overlay != null)
							{
								overlay.SetBounds(0, 0, view.Width, view.Height);
								SetOverlay(view, disposables, overlay);
							}
						}
					}
				}
			}
			else // No corner radius
			{
				//We only need to set a background if the drawArea is non-zero
				if (!drawArea.HasZeroArea())
				{
					var imageBrushBackground = background as ImageBrush;
					if (imageBrushBackground != null)
					{
						var setBackground = DispatchSetImageBrushAsBackground(view, imageBrushBackground, drawArea, onImageSet);
						disposables.Add(setBackground);
					}
					else
					{
						var fillPaint = background?.GetFillPaint(drawArea) ?? new Paint() { Color = Android.Graphics.Color.Transparent };
						ExecuteWithNoRelayout(view, v => v.SetBackgroundDrawable(GetBackgroundDrawable(background, drawArea, fillPaint)));
					}
					disposables.Add(() => ExecuteWithNoRelayout(view, v => v.SetBackgroundDrawable(null)));
				}

				if (borderBrush != null && !(borderBrush is ImageBrush))
				{
					//TODO: Handle case that BorderBrush is an ImageBrush
					using (var strokePaint = borderBrush.GetStrokePaint(drawArea))
					{
						var overlay = GetOverlayDrawable(strokePaint, physicalBorderThickness, new global::System.Drawing.Size(view.Width, view.Height));

						if (overlay != null)
						{
							overlay.SetBounds(0, 0, view.Width, view.Height);
							SetOverlay(view, disposables, overlay);
						}
					}
				}
			}

			return disposables;
		}

		private static Drawable GetBackgroundDrawable(Brush background, Windows.Foundation.Rect drawArea, Paint fillPaint, Path maskingPath = null)
		{
			if (background is ImageBrush)
			{
				throw new InvalidOperationException($"This method should not be called for ImageBrush, use {nameof(DispatchSetImageBrushAsBackground)} instead");
			}

			if (maskingPath == null)
			{
				var solidBrush = background as SolidColorBrush;

				if (solidBrush != null)
				{
					return new ColorDrawable(solidBrush.ColorWithOpacity);
				}

				if (fillPaint != null)
				{
					var linearDrawable = new PaintDrawable();
					var drawablePaint = linearDrawable.Paint;
					drawablePaint.Color = fillPaint.Color;
					drawablePaint.SetShader(fillPaint.Shader);

					return linearDrawable;
				}

				return null;
			}

			var drawable = new PaintDrawable();
			drawable.Shape = new PathShape(maskingPath, (float)drawArea.Width, (float)drawArea.Height);
			var paint = drawable.Paint;
			paint.Color = fillPaint.Color;
			paint.SetShader(fillPaint.Shader);
			return drawable;
		}

		private static void SetDrawableAlpha(Drawable drawable, int alpha)
		{
#if __ANDROID_18__
			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Kitkat)
			{
				drawable.Alpha = alpha;
			}
			else
#endif
			{
				// Do nothing, not supported by this API Level
			}
		}

		private static void SetOverlay(BindableView view, CompositeDisposable disposables, Drawable overlay)
		{
#if __ANDROID_18__
			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Kitkat)
			{
				ExecuteWithNoRelayout(view, v => v.Overlay.Add(overlay));
				disposables.Add(() => ExecuteWithNoRelayout(view, v => v.Overlay.Remove(overlay)));
			}
			else
#endif
			{
				// Set overlay is not supported by this platform, set the background instead
				// and merge with the existing background.
				// It'll break some scenarios, like having borders on top of the content.

				var list = new List<Drawable>();

				var currentBackground = view.Background;
				if (currentBackground != null)
				{
					list.Add(currentBackground);
				}

				list.Add(overlay);

				view.SetBackgroundDrawable(new LayerDrawable(list.ToArray()));
				disposables.Add(() => view.SetBackgroundDrawable(null));
			}
		}

		private static IDisposable DispatchSetImageBrushAsBackground(BindableView view, ImageBrush background, Windows.Foundation.Rect drawArea, Action onImageSet, Path maskingPath = null)
		{
			var disposable = new CompositeDisposable();
			Dispatch(
				view?.Dispatcher,
				async ct =>
					{
						var bitmapDisposable = await SetImageBrushAsBackground(ct, view, background, drawArea, maskingPath, onImageSet);
						disposable.Add(bitmapDisposable);
					}
			)
			.DisposeWith(disposable);

			return disposable;
		}

		//Load bitmap from ImageBrush and set it as a bitmapDrawable background on target view
		private static async Task<IDisposable> SetImageBrushAsBackground(CancellationToken ct, BindableView view, ImageBrush background, Windows.Foundation.Rect drawArea, Path maskingPath, Action onImageSet)
		{
			var bitmap = await background.GetBitmap(ct, drawArea, maskingPath);

			onImageSet();

			if (ct.IsCancellationRequested || bitmap == null)
			{
				bitmap?.Recycle();
				bitmap?.Dispose();
				return Disposable.Empty;
			}

			var bitmapDrawable = new BitmapDrawable(bitmap);
			SetDrawableAlpha(bitmapDrawable, (int)(background.Opacity * __opaqueAlpha));
			ExecuteWithNoRelayout(view, v => v.SetBackgroundDrawable(bitmapDrawable));

			return Disposable.Create(() =>
			{
				bitmapDrawable?.Bitmap?.Recycle();
				bitmapDrawable?.Dispose();
			});
		}

		private static void ExecuteWithNoRelayout(BindableView target, Action<BindableView> action)
		{
			if (target == null)
			{
				throw new ArgumentNullException(nameof(target));
			}

			using (target.PreventRequestLayout())
			{
				action(target);
			}
		}

		private static Drawable GetOverlayDrawable(Paint strokePaint, Thickness physicalBorderThickness, global::System.Drawing.Size viewSize, Path path = null)
		{
			if (strokePaint != null)
			{
				// Alias the stroke to reduce interop
				var paintStyleStroke = Paint.Style.Stroke;

				if (path != null)
				{
					var drawable = new PaintDrawable();
					drawable.Shape = new PathShape(path, viewSize.Width, viewSize.Height);
					var paint = drawable.Paint;
					paint.Color = strokePaint.Color;
					paint.SetShader(strokePaint.Shader);
					paint.StrokeWidth = (float)physicalBorderThickness.Top;
					paint.SetStyle(paintStyleStroke);
					paint.Alpha = strokePaint.Alpha;
					return drawable;
				}
				else if (viewSize != null && !viewSize.IsEmpty)
				{
					var drawables = new List<Drawable>();

					if (physicalBorderThickness.Top != 0)
					{
						var adjustY = (float)physicalBorderThickness.Top / 2;						

						using (var line = new Path())
						{						
							line.MoveTo((float)physicalBorderThickness.Left, (float)adjustY); 
							line.LineTo(viewSize.Width - (float)physicalBorderThickness.Right, (float)adjustY); 
							line.Close();

							var lineDrawable = new PaintDrawable();
							lineDrawable.Shape = new PathShape(line, viewSize.Width, viewSize.Height);
							var paint = lineDrawable.Paint;
							paint.Color = strokePaint.Color;
							paint.SetShader(strokePaint.Shader);
							paint.StrokeWidth = (float)physicalBorderThickness.Top;
							paint.SetStyle(paintStyleStroke);
							paint.Alpha = strokePaint.Alpha;
							drawables.Add(lineDrawable);
						}
					}

					if (physicalBorderThickness.Right != 0)
					{
						var adjustX = physicalBorderThickness.Right / 2;

						using (var line = new Path())
						{
							line.MoveTo((float)(viewSize.Width - adjustX), 0);
							line.LineTo((float)(viewSize.Width - adjustX), viewSize.Height);
							line.Close();

							var lineDrawable = new PaintDrawable();
							lineDrawable.Shape = new PathShape(line, viewSize.Width, viewSize.Height);
							var paint = lineDrawable.Paint;
							paint.Color = strokePaint.Color;
							paint.SetShader(strokePaint.Shader);
							paint.StrokeWidth = (float)physicalBorderThickness.Right;
							paint.SetStyle(paintStyleStroke);
							paint.Alpha = strokePaint.Alpha;
							drawables.Add(lineDrawable);
						}
					}

					if (physicalBorderThickness.Bottom != 0)
					{
						var adjustY = physicalBorderThickness.Bottom / 2;
						
						using (var line = new Path())
						{
							line.MoveTo((float)physicalBorderThickness.Left, (float)(viewSize.Height - adjustY));
							line.LineTo(viewSize.Width - (float)physicalBorderThickness.Right, (float)(viewSize.Height - adjustY));
							line.Close();

							var lineDrawable = new PaintDrawable();
							lineDrawable.Shape = new PathShape(line, viewSize.Width, viewSize.Height);
							var paint = lineDrawable.Paint;
							paint.Color = strokePaint.Color;
							paint.SetShader(strokePaint.Shader);
							paint.StrokeWidth = (float)physicalBorderThickness.Bottom;
							paint.SetStyle(paintStyleStroke);
							paint.Alpha = strokePaint.Alpha;
							drawables.Add(lineDrawable);
						}
					}

					if (physicalBorderThickness.Left != 0)
					{
						var adjustX = physicalBorderThickness.Left / 2;

						using (var line = new Path())
						{
							line.MoveTo((float)adjustX, 0);
							line.LineTo((float)adjustX, viewSize.Height);
							line.Close();

							var lineDrawable = new PaintDrawable();
							lineDrawable.Shape = new PathShape(line, viewSize.Width, viewSize.Height);
							var paint = lineDrawable.Paint;
							paint.Color = strokePaint.Color;
							paint.SetShader(strokePaint.Shader);
							paint.StrokeWidth = (float)physicalBorderThickness.Left;
							paint.SetStyle(paintStyleStroke);
							paint.Alpha = strokePaint.Alpha;
							drawables.Add(lineDrawable);
						}
					}

					return new LayerDrawable(drawables.ToArray());
				}
			}

			return null;
		}

		private static IDisposable Dispatch(CoreDispatcher dispatcher, Func<CancellationToken, Task> handler)
		{
			var cd = new CancellationDisposable();

			// Execute the non-async part of the loading on the current thread.
			// Dispatch the rest if required.
			var t = handler(cd.Token);

			dispatcher.RunAsync(
				CoreDispatcherPriority.Normal,
				async () => await t
			).AsTask(cd.Token);

			return cd;
		}

		private class LayoutState : IEquatable<LayoutState>
		{
			public readonly Windows.Foundation.Rect Area;
			public readonly Brush Background;
			public readonly ImageSource BackgroundImageSource;
			public readonly Color? BackgroundColor;
			public readonly Brush BorderBrush;
			public readonly Thickness BorderThickness;
			public readonly CornerRadius CornerRadius;
			public readonly Thickness Padding;

			public LayoutState(Windows.Foundation.Rect area, Brush background, Thickness borderThickness, Brush borderBrush, CornerRadius cornerRadius, Thickness padding)
			{
				Area = area;
				Background = background;
				BorderBrush = borderBrush;
				CornerRadius = cornerRadius;
				BorderThickness = borderThickness;
				Padding = padding;

				var imageBrushBackground = Background as ImageBrush;
				BackgroundImageSource = imageBrushBackground?.ImageSource;

				BackgroundColor = (Background as SolidColorBrush)?.Color;
			}

			public bool Equals(LayoutState other)
			{
				return other != null
					&& other.Area == Area
					&& other.Background == Background
					&& other.BackgroundImageSource == BackgroundImageSource
					&& other.BackgroundColor == BackgroundColor
					&& other.BorderBrush == BorderBrush
					&& other.BorderThickness == BorderThickness
					&& other.CornerRadius == CornerRadius
					&& other.Padding == Padding;
			}
		}
	}
}
