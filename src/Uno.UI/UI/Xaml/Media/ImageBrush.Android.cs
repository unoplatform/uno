using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.Graphics;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Controls;
using Microsoft.UI.Xaml.Media;
using Android.Graphics.Drawables;
using Android.Views.Animations;
using Windows.Foundation;
using Uno.Disposables;
using Windows.UI.Core;
using Uno.UI;

using Rect = Windows.Foundation.Rect;

namespace Microsoft.UI.Xaml.Media
{
	public partial class ImageBrush
	{
		private Action _onImageLoaded;
		private bool _imageSourceChanged;
		private Rect _lastDrawRect;
		private readonly SerialDisposable _refreshPaint = new SerialDisposable();

		partial void OnSourceChangedPartial(ImageSource newValue, ImageSource oldValue)
		{
			oldValue?.Dispose();
			_imageSourceChanged = true;
			_onImageLoaded?.Invoke();
		}

		private protected override void ApplyToPaintInner(Rect drawRect, Paint paint)
		{
			throw new NotSupportedException($"{nameof(ApplyToPaintInner)} is not supported for ImageBrush.");
		}

		internal void ScheduleRefreshIfNeeded(Rect drawRect, Action onImageLoaded)
		{
			_onImageLoaded = onImageLoaded;

			//If ImageSource or draw size has changed, refresh the Paint
			//TODO: should also check if Stretch has changed
			if (_imageSourceChanged || !drawRect.Equals(_lastDrawRect))
			{
				_imageSourceChanged = false;
				_lastDrawRect = drawRect;

				if (ImageSource != null)
				{
					RefreshImageAsync(drawRect);
				}
				else
				{
					_refreshPaint.Disposable = null;
				}
			}
		}

		private async void RefreshImageAsync(Rect drawRect)
		{
			CoreDispatcher.CheckThreadAccess();

			var cd = new CancellationDisposable();

			_refreshPaint.Disposable = cd;

			await RefreshImage(cd.Token, drawRect);
		}

		private async Task RefreshImage(CancellationToken ct, Rect drawRect)
		{
			if (ImageSource is ImageSource imageSource && (_imageSourceChanged || !imageSource.IsOpened) && !drawRect.HasZeroArea())
			{
				try
				{
					var image = await imageSource.Open(ct,
						targetImage: null,
						targetWidth: (int)drawRect.Width,
						targetHeight: (int)drawRect.Height);

					if (image.Bitmap != null || imageSource.IsImageLoadedToUiDirectly)
					{
						OnImageOpened();
					}
					else
					{
						OnImageFailed();
					}
				}
				catch (Exception ex)
				{
					this.Log().Error("RefreshImage failed", ex);
					OnImageFailed();
				}
			}
			_onImageLoaded?.Invoke();
		}

		/// <summary>
		/// Loads the ImageBrush's source bitmap and transforms it based on target bounds and shape, Stretch mode, and RelativeTransform.
		/// </summary>
		/// <param name="drawRect">The destination bounds</param>
		/// <param name="maskingPath">An optional path to clip the bitmap by (eg an ellipse)</param>
		/// <returns>A bitmap transformed based on target bounds and shape, Stretch mode, and RelativeTransform</returns>
		internal async Task<Bitmap> GetBitmap(CancellationToken ct, Rect drawRect, Path maskingPath = null)
		{
			await RefreshImage(ct, drawRect);

			if (ct.IsCancellationRequested)
			{
				return null;
			}
			return TryGetTransformedBitmap(drawRect, maskingPath);
		}

		/// <summary>
		/// Transforms ImageBrush's bitmap based on target bounds and shape, Stretch mode, and RelativeTransform, and draws it to the supplied canvas.
		/// </summary>
		/// <param name="destinationCanvas">The canvas to draw the final image on</param>
		/// <param name="drawRect">The destination bounds</param>
		/// <param name="maskingPath">An optional path to clip the bitmap by (eg an ellipse)</param>
		internal void DrawBackground(Canvas destinationCanvas, Rect drawRect, Path maskingPath = null)
		{
			//Create a temporary bitmap
			var output = TryGetTransformedBitmap(drawRect, maskingPath);
			if (output == null)
			{
				return;
			}

			var paint = new Paint();

			//Draw the output bitmap to the screen
			var rect = new ARect(0, 0, (int)drawRect.Width, (int)drawRect.Height);
			destinationCanvas.DrawBitmap(output, rect, rect, paint);
		}

		/// <summary>
		/// Synchronously tries to return a bitmap for this ImageBrush. If the backing image is not available,
		/// a fetch will be scheduled and the onImageLoaded callback will be called once the backing image is ready.
		/// </summary>
		/// <param name="drawRect">The destination bounds</param>
		/// <param name="onImageLoaded">A callback that will be called when the backing image changes (eg, to redraw your view)</param>
		/// <param name="maskingPath">An optional path to clip the bitmap by (eg an ellipse)</param>
		/// <returns>A bitmap transformed based on target bounds and shape, Stretch mode, and RelativeTransform</returns>
		internal Bitmap TryGetBitmap(Rect drawRect, Action onImageLoaded, Path maskingPath = null)
		{
			ScheduleRefreshIfNeeded(drawRect, onImageLoaded);

			return TryGetTransformedBitmap(drawRect, maskingPath);
		}

		/// <summary>
		/// Transforms ImageBrush's source bitmap based on target bounds and shape, Stretch mode, and RelativeTransform.
		/// </summary>
		/// <param name="drawRect">The destination bounds</param>
		/// <param name="maskingPath">An optional path to clip the bitmap by (eg an ellipse)</param>
		/// <returns></returns>
		private Bitmap TryGetTransformedBitmap(Rect drawRect, Path maskingPath = null)
		{
			var imgSrc = ImageSource;
			if (imgSrc == null || !imgSrc.TryOpenSync(out var sourceBitmap))
			{
				return null;
			}

			if (sourceBitmap.Handle == IntPtr.Zero || sourceBitmap.IsRecycled)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
				{
					this.Log().Error("Attempted to use a collected or recycled bitmap.");
				}
				return null;
			}

			var outputBitmap = Bitmap.CreateBitmap((int)drawRect.Width, (int)drawRect.Height, ABitmap.Config.Argb8888);

			//and a temporary canvas that will draw into the bitmap
			using (var bitmapCanvas = new Canvas(outputBitmap))
			{
				var matrix = GenerateMatrix(drawRect, sourceBitmap);
				var paint = new Paint()
				{
					//Smoothes edges of painted area
					AntiAlias = true,
					//Applies bilinear sampling (smoothing) to interior of painted area
					FilterBitmap = true,
					//The color is not important, we just want an alpha of 100%
					Color = Colors.White
				};

				//Draw ImageBrush's bitmap to temporary canvas
				bitmapCanvas.DrawBitmap(sourceBitmap, matrix, paint);

				//If a path was supplied, mask the area outside of it
				if (maskingPath != null)
				{
					//This will draw an alpha of 0 everywhere *outside* the supplied path, leaving the bitmap within the path http://developer.android.com/reference/android/graphics/PorterDuff.Mode.html
					paint.SetXfermode(new PorterDuffXfermode(PorterDuff.Mode.DstOut));
					maskingPath.ToggleInverseFillType();
					bitmapCanvas.DrawPath(maskingPath, paint);
					maskingPath.ToggleInverseFillType();
				}
				return outputBitmap;
			}
		}

		/// <summary>
		/// Create matrix to transform image based on relative dimensions of bitmap and drawRect, Stretch mode, and RelativeTransform
		/// </summary>
		/// <param name="drawRect"></param>
		/// <param name="bitmap"></param>
		/// <returns></returns>
		private AMatrix GenerateMatrix(Rect drawRect, Bitmap bitmap)
		{
			var matrix = new AMatrix();

			// Note that bitmap.Width and bitmap.Height (in physical pixels) are automatically scaled up when loaded from local resources, but aren't when acquired externally.
			// This means that bitmaps acquired externally might not render the same way on devices with different densities when using Stretch.None.

			var sourceRect = new Rect(0, 0, bitmap.Width, bitmap.Height);
			var destinationRect = GetArrangedImageRect(sourceRect.Size, drawRect);

			matrix.SetRectToRect(sourceRect.ToRectF(), destinationRect.ToRectF(), AMatrix.ScaleToFit.Fill);

			RelativeTransform?.ToNativeMatrix(matrix, size: new Size(sourceRect.Width, sourceRect.Height));
			return matrix;
		}

		protected override void OnRelativeTransformChanged(Transform oldValue, Transform newValue)
		{
			base.OnRelativeTransformChanged(oldValue, newValue);

			_onImageLoaded?.Invoke();
		}
	}
}
