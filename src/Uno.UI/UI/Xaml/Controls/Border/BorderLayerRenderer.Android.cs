#nullable enable
#pragma warning disable 0618 // Used for compatibility with SetBackgroundDrawable and previous API Levels

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Graphics.Drawables.Shapes;
using Android.Views;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Controls;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Rect = Windows.Foundation.Rect;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls
{
	partial class BorderLayerRenderer
	{
		private const double __opaqueAlpha = 255;

		private readonly SerialDisposable _layerDisposable = new SerialDisposable();
		private static readonly float[] _outerRadiiStore = new float[8];
		private static readonly float[] _innerRadiiStore = new float[8];
		private static Paint? _strokePaint;
		private static Paint? _fillPaint;
		private Action? _backgroundChanged;
		private Action? _borderChanged;

		private static ImageSource? GetBackgroundImageSource(BorderLayerState? state)
			=> (state?.Background as ImageBrush)?.ImageSource;

		partial void UpdatePlatform()
		{
			var drawArea = new Rect(default, _owner.LayoutSlotWithMarginsAndAlignments.Size.LogicalToPhysicalPixels());
			var newState = new BorderLayerState(drawArea.Size, _borderInfoProvider);
			var previousLayoutState = _currentState;

			if (newState.Equals(previousLayoutState))
			{
				return;
			}

			var newStateBackgroundImageSource = GetBackgroundImageSource(newState);
			var oldStateBackgroundImageSource = GetBackgroundImageSource(previousLayoutState);

			var imageHasChanged = newStateBackgroundImageSource != oldStateBackgroundImageSource;
			var shouldDisposeEagerly = imageHasChanged || newStateBackgroundImageSource == null;
			if (shouldDisposeEagerly)
			{
				// Clear previous value anyway in order to make sure the previous values are unset before the new ones.
				// This prevents the case where a second update would set a new background and then set the background to null when disposing the previous.
				_layerDisposable.Disposable = null;
			}

			Action? onImageSet = null;
			var disposable = InnerCreateLayers(_owner, drawArea, newState.Background, newState.BackgroundSizing, newState.BorderThickness, newState.BorderBrush, newState.CornerRadius, () => onImageSet?.Invoke());

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

			//if (willUpdateMeasures)
			//{
			//	view.RequestLayout();
			//}
			//else
			//{
			//	view.Invalidate();
			//}

			_currentState = newState;
		}

		/// <summary>
		/// Removes the added layers during a call to <see cref="UpdateLayer" />.
		/// </summary>
		partial void ClearPlatform()
		{
			_layerDisposable.Disposable = null;
		}

		private IDisposable InnerCreateLayers(
			BindableView view,
			Rect drawArea,
			Brush? background,
			BackgroundSizing backgroundSizing,
			Thickness borderThickness,
			Brush? borderBrush,
			CornerRadius cornerRadius,
			Action onImageSet)
		{
			// In case the element has no size, skip everything!
			if (drawArea.Width == 0 && drawArea.Height == 0)
			{
				return Disposable.Empty;
			}

			var disposables = new CompositeDisposable();

			var physicalBorderThickness = borderThickness.LogicalToPhysicalPixels();
			var isInnerBorderSizing = backgroundSizing == BackgroundSizing.InnerBorderEdge;
			var adjustedArea = isInnerBorderSizing
				? drawArea.DeflateBy(physicalBorderThickness)
				: drawArea;

			var fullCornerRadius = cornerRadius.GetRadii(drawArea.Size, borderThickness);

			if (!fullCornerRadius.IsEmpty)
			{
				if ((view as UIElement)?.FrameRoundingAdjustment is { } fra)
				{
					drawArea.Height += fra.Height;
					drawArea.Width += fra.Width;
				}

				// This needs to be adjusted if multiple UI threads are used in the future for multi-window
				fullCornerRadius.Outer.GetRadii(_outerRadiiStore);
				fullCornerRadius.Inner.GetRadii(_innerRadiiStore);

				using (var backgroundPath = new Path())
				{
					backgroundPath.AddRoundRect(adjustedArea.ToRectF(), _innerRadiiStore, Path.Direction.Cw!);
					//We only need to set a background if the drawArea is non-zero
					if (!drawArea.HasZeroArea())
					{
						if (background is ImageBrush imageBrushBackground)
						{
							//Copy the path because it will be disposed when we exit the using block
							var pathCopy = new Path(backgroundPath);
							var setBackground = DispatchSetImageBrushAsBackground(view, imageBrushBackground, drawArea, onImageSet, pathCopy);
							disposables.Add(setBackground);
						}
						else if (background is AcrylicBrush acrylicBrush)
						{
							var apply = acrylicBrush.Subscribe(view, drawArea, backgroundPath);
							disposables.Add(apply);
						}
						else
						{
							_fillPaint ??= new();
							if (background is not null)
							{
								background.ApplyToFillPaint(drawArea, _fillPaint);
							}
							else
							{
								_fillPaint.Reset();
								_fillPaint.Color = Android.Graphics.Color.Transparent;
							}
							ExecuteWithNoRelayout(view, v => v.SetBackgroundDrawable(Brush.GetBackgroundDrawable(background, drawArea, _fillPaint, backgroundPath)));
						}
						disposables.Add(() => ExecuteWithNoRelayout(view, v => v.SetBackgroundDrawable(null)));
					}

					if (borderThickness != Thickness.Empty && borderBrush != null && !(borderBrush is ImageBrush))
					{
						//TODO: Handle case when BorderBrush is an ImageBrush
						// Related Issue: https://github.com/unoplatform/uno/issues/6893
						_strokePaint ??= new();
						borderBrush.ApplyToStrokePaint(drawArea, _strokePaint);

						//Create the path for the outer and inner rectangles that will become our border shape							
						using var borderPath = new Path();

						borderPath.AddRoundRect(drawArea, _outerRadiiStore, Path.Direction.Cw!);
						borderPath.AddRoundRect(adjustedArea, _innerRadiiStore, Path.Direction.Cw!);

						var overlay = GetOverlayDrawable(
							_strokePaint,
							physicalBorderThickness,
							new global::System.Drawing.Size((int)drawArea.Width, (int)drawArea.Height),
							borderPath);

						if (overlay != null)
						{
							overlay.SetBounds(0, 0, view.Width, view.Height);
							SetOverlay(view, disposables, overlay);
						}
					}
				}
			}
			else // No corner radius
			{
				//We only need to set a background if the drawArea is non-zero and Thickness is empty
				//We need to set a background and adjust the draw area if the drawArea is non-zero and Thickness isn't empty
				if (!drawArea.HasZeroArea())
				{
					var finalDrawArea = borderThickness != Thickness.Empty ? adjustedArea : drawArea;

					var backgroundPath = finalDrawArea.ToPath();

					if (background is ImageBrush imageBrushBackground)
					{
						var setBackground = DispatchSetImageBrushAsBackground(view, imageBrushBackground, drawArea, onImageSet);
						disposables.Add(setBackground);
					}
					else if (background is AcrylicBrush acrylicBrush)
					{
						var apply = acrylicBrush.Subscribe(view, drawArea, backgroundPath);
						disposables.Add(apply);
					}
					else
					{
						_fillPaint ??= new();
						if (background is not null)
						{
							background.ApplyToFillPaint(drawArea, _fillPaint);
						}
						else
						{
							_fillPaint.Reset();
							_fillPaint.Color = Android.Graphics.Color.Transparent;
						}
						ExecuteWithNoRelayout(view, v => v.SetBackgroundDrawable(Brush.GetBackgroundDrawable(background, drawArea, _fillPaint, backgroundPath, antiAlias: false)));
					}
					disposables.Add(() => ExecuteWithNoRelayout(view, v => v.SetBackgroundDrawable(null)));
				}

				if (borderThickness != Thickness.Empty && !(borderBrush is ImageBrush))
				{
					//TODO: Handle case when BorderBrush is an ImageBrush
					// Related Issue: https://github.com/unoplatform/uno/issues/6893
					_strokePaint ??= new();
					if (borderBrush is not null)
					{
						borderBrush.ApplyToStrokePaint(drawArea, _strokePaint);
					}
					else
					{
						_strokePaint.Reset();
						_strokePaint.Color = Android.Graphics.Color.Transparent;
					}

					var overlay = GetOverlayDrawable(_strokePaint, physicalBorderThickness, new global::System.Drawing.Size(view.Width, view.Height));

					if (overlay != null)
					{
						overlay.SetBounds(0, 0, view.Width, view.Height);
						SetOverlay(view, disposables, overlay);
					}
				}
			}

			// Subscribe to brush changes. The changes will trigger Update(), which will work,
			// because even though the brush instance is the same, there are additional properties
			// that BorderLayerState tracks on Android. This is not ideal and we should avoid it by refactoring
			// this file to handle brush changes on the same brush instance on its own instead.
			Brush.SetupBrushChanged(_currentState.Background, background, ref _backgroundChanged, () => Update(), false);
			Brush.SetupBrushChanged(_currentState.BorderBrush, borderBrush, ref _borderChanged, () => Update(), false);

			return disposables;
		}

		private static void SetDrawableAlpha(Drawable drawable, int alpha)
		{
			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Kitkat)
			{
				drawable.Alpha = alpha;
			}
			else
			{
				// Do nothing, not supported by this API Level
			}
		}

		private static void SetOverlay(BindableView view, CompositeDisposable disposables, Drawable overlay)
		{
			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Kitkat)
			{
				ExecuteWithNoRelayout(view, v => v.Overlay?.Add(overlay));
				disposables.Add(() => ExecuteWithNoRelayout(view, v => v.Overlay?.Remove(overlay)));
			}
			else
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

		private static IDisposable DispatchSetImageBrushAsBackground(BindableView view, ImageBrush background, Windows.Foundation.Rect drawArea, Action onImageSet, Path? maskingPath = null)
		{
			var disposable = new CompositeDisposable();
			Dispatch(
				view.Dispatcher,
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
		private static async Task<IDisposable> SetImageBrushAsBackground(CancellationToken ct, BindableView view, ImageBrush background, Windows.Foundation.Rect drawArea, Path? maskingPath, Action onImageSet)
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

		private static Drawable? GetOverlayDrawable(
			Paint strokePaint,
			Thickness physicalBorderThickness,
			global::System.Drawing.Size viewSize,
			Path? borderPath = null)
		{
			if (strokePaint != null)
			{
				if (borderPath != null)
				{
					borderPath.SetFillType(Path.FillType.EvenOdd!);

					var drawable = new PaintDrawable();

					BorderLayerRendererNative.BuildBorderCornerRadius(drawable, borderPath, strokePaint, viewSize.Width, viewSize.Height);

					return drawable;
				}
				else if (!viewSize.IsEmpty)
				{
					// Alias the stroke to reduce interop
					var paintStyleStroke = Paint.Style.Stroke;
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
							if (lineDrawable.Paint is { } paint)
							{
								paint.AntiAlias = false;
								paint.Color = strokePaint.Color;
								paint.SetShader(strokePaint.Shader);
								paint.StrokeWidth = (float)physicalBorderThickness.Top;
								paint.SetStyle(paintStyleStroke);
								paint.Alpha = strokePaint.Alpha;
							}
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
							if (lineDrawable.Paint is { } paint)
							{
								paint.AntiAlias = false;
								paint.Color = strokePaint.Color;
								paint.SetShader(strokePaint.Shader);
								paint.StrokeWidth = (float)physicalBorderThickness.Right;
								paint.SetStyle(paintStyleStroke);
								paint.Alpha = strokePaint.Alpha;
							}
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
							if (lineDrawable.Paint is { } paint)
							{
								paint.AntiAlias = false;
								paint.Color = strokePaint.Color;
								paint.SetShader(strokePaint.Shader);
								paint.StrokeWidth = (float)physicalBorderThickness.Bottom;
								paint.SetStyle(paintStyleStroke);
								paint.Alpha = strokePaint.Alpha;
							}
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
							if (lineDrawable.Paint is { } paint)
							{
								paint.AntiAlias = false;
								paint.Color = strokePaint.Color;
								paint.SetShader(strokePaint.Shader);
								paint.StrokeWidth = (float)physicalBorderThickness.Left;
								paint.SetStyle(paintStyleStroke);
								paint.Alpha = strokePaint.Alpha;
							}
							drawables.Add(lineDrawable);
						}
					}

					return new LayerDrawable(drawables.ToArray());
				}
			}

			return null;
		}

		private static IDisposable Dispatch(CoreDispatcher? dispatcher, Func<CancellationToken, Task> handler)
		{
			if (dispatcher is null)
			{
				return Disposable.Empty;
			}

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
	}
}
