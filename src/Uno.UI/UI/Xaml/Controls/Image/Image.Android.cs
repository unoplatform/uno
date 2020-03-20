using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using Uno;
using Uno.Extensions;
using Uno.Logging;
using Uno.Diagnostics.Eventing;
using Uno.UI.DataBinding;
using Uno.UI.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.Foundation;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Android.Runtime;
using System.Threading;
using Uno.UI;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace Windows.UI.Xaml.Controls
{
	public partial class Image : ImageView, IImage
	{
		private bool _skipLayoutRequest;
		private bool _isInLayout;
		private double _sourceImageScale = 1;
		private Windows.Foundation.Size _sourceImageSize;
		private Windows.Foundation.Size SourceImageSize
		{
			get { return _sourceImageSize; }
		}

		/// <summary>
		/// Updates the size of the image source (drawable, bitmap, etc.)
		/// </summary>
		/// <param name="size">size of the image source (in physical pixels)</param>
		/// <param name="isLogicalPixels">indicates that the size of the image source is in logical pixels (this is the case when the source is an URI)</param>
		private void UpdateSourceImageSize(Windows.Foundation.Size size, bool isLogicalPixels = false)
		{
			if (_sourceImageSize == size)
			{
				return;
			}

			_sourceImageSize = isLogicalPixels
				? size // is logical size already, no conversion needed
				: size.PhysicalToLogicalPixels();

			_sourceImageScale = isLogicalPixels
				? ViewHelper.Scale // the size of the image source (usually a bitmap from UniversalImageLoader) is in logical pixels, we need to scale it when Stretch.None
				: 1; // the size of the image source (usually a drawable/resource) is already physical pixels, no need to scale it

			UpdateMatrix(_lastLayoutSize);

			if (Source is BitmapSource bitmapSource)
			{
				bitmapSource.PixelWidth = (int)_sourceImageSize.Width;
				bitmapSource.PixelHeight = (int)_sourceImageSize.Height;
			}
		}

		private Windows.Foundation.Size _lastLayoutSize;

		/// <summary>
		/// Internal use.
		/// </summary>
		/// <remarks>This constructor is *REQUIRED* for the Mono/Java 
		/// binding layer to function properly, in case java objects need to call methods 
		/// on a collected .NET instance.
		/// </remarks>
		internal Image(IntPtr ptr, Android.Runtime.JniHandleOwnership ownership)
			: base(ptr, ownership)
		{
		}

		public Image()
			: base(IntPtr.Zero, JniHandleOwnership.DoNotTransfer)
		{
			NativeInstanceHelper.CreateNativeInstance(base.GetType(), this, ContextHelper.Current, base.SetHandle);

			Initialize();
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

			var availableSize = ViewHelper.LogicalSizeFromSpec(widthMeasureSpec, heightMeasureSpec);

			var measuredSize = _layouter.Measure(availableSize);

			if (
				!double.IsInfinity(availableSize.Width)
				&& !double.IsInfinity(availableSize.Height)
				)
			{
				measuredSize = this.AdjustSize(availableSize, measuredSize);
			}

			var physicalMeasuredSize = measuredSize
				.LogicalToPhysicalPixels();

			// Report our final dimensions.
			var width = (int)physicalMeasuredSize.Width;
			var height = (int)physicalMeasuredSize.Height;

			SetMeasuredDimension(width, height);

			IFrameworkElementHelper
				.SizeThatFits(this, new Size(width, height).PhysicalToLogicalPixels());
		}

		partial void OnLayoutPartial(bool changed, int left, int top, int right, int bottom)
		{
			try
			{
				_isInLayout = true;
				var newSize = new Windows.Foundation.Size(right - left, bottom - top).PhysicalToLogicalPixels();

				if (
					// If the layout has changed, but the final size has not, this is just a translation.
					// So unless there was a layout requested, we can skip arranging the children.
					(changed && _lastLayoutSize != newSize)

					// Even if nothing changed, but a layout was requested, arrange the children.
					|| IsLayoutRequested
				)
				{
					_lastLayoutSize = newSize;

					_layouter.Arrange(new Windows.Foundation.Rect(0, 0, newSize.Width, newSize.Height));
				}

				// Try opening the image in the case where UseTargetSize has been set, as now
				// we have both _targetWidth and _targetWidth that have been set.
				TryOpenImage();
			}
			finally
			{
				_isInLayout = false;
			}
		}

		private int? _targetWidth;
		private int? _targetHeight;

		private Size _previousArrangeSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

		partial void SetTargetImageSize(Size targetSize)
		{
			// Ignore changes coming from MeasureOverride when a dimension is unconstrained. This will be picked up
			// by ArrangeOverride.
			if (!double.IsInfinity(targetSize.Width) && !double.IsInfinity(targetSize.Height))
			{
				var physicalSize = targetSize.LogicalToPhysicalPixels();
				_targetWidth = physicalSize.Width.SelectOrDefault(w => w != 0 ? (int?)w : null);
				_targetHeight = physicalSize.Height.SelectOrDefault(h => h != 0 ? (int?)h : null);


				TryOpenImage();
			}
		}

		partial void UpdateArrangeSize(Size arrangeSize)
		{
			// Here we validate that the target size is useable, and that it's not being reset to zero.
			// The scenario is as follows:
			//   - Image gets its source set, using UseTargetSize=true
			//   - Image gets measured with a non-infinite size (e.g. the parent is constrained), calls SetTargetImageSize with the requested available size
			//   - Measure returns [0;0] if there are no constraints on the image itself
			//   - SetTargetImageSize determines if the size is usable, loads the image with the tentative available size as the decode size.
			//   - Arrange is invoked with a [0;0] size as requsted by the measure (the image size is unknown)
			//   - Arrange invokes SetTargetImageSize with [0;0] which is ignored as there is already a tentative decode size.
			//   - The image gets loaded and the InvalidateMeasure is called
			//   - The measure is invoked again, returns the natural size of the image. If the SetTargetImageSize is called with
			//     a larger size, the image gets reloaded with a different decode size.
			//   - The arrange is invoked again with the proper final size. If the SetTargetImageSize is called with
			//     a larger size, the image gets reloaded again with a different decode size.
			//
			// The above scenario may be at disadvantage if the image does not have a Width/Height, or if the
			// image gets a different size in measure and arrange. A warning message gets displayed in such cases.
			//
			if (
				(Source?.UseTargetSize ?? false)
				&& (arrangeSize.Width > _previousArrangeSize.Width || arrangeSize.Height > _previousArrangeSize.Height)
				&& _openedImage != null
			)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
				{
					this.Log().Debug(
						this.ToString() +
						$" Image is being reloaded because the target size has changed ({_previousArrangeSize.Width}x{_previousArrangeSize.Height} to {arrangeSize.Width}x{arrangeSize.Height})");
				}

				_openedImage = null;
			}

			_previousArrangeSize = arrangeSize;
		}

		public override void SetImageDrawable(Drawable drawable)
		{
			if (drawable != null)
			{
				UpdateSourceImageSize(new Windows.Foundation.Size(drawable.IntrinsicWidth, drawable.IntrinsicHeight));
			}

			try
			{
				_skipLayoutRequest = true;
				base.SetImageDrawable(drawable);
			}
			finally
			{
				_skipLayoutRequest = false;
			}
		}

		public override void SetImageResource(int resId)
		{
			try
			{
				_skipLayoutRequest = true;

				base.SetImageResource(resId);
			}
			finally
			{
				_skipLayoutRequest = false;
			}

			if (Drawable != null)
			{
				UpdateSourceImageSize(new Windows.Foundation.Size(Drawable.IntrinsicWidth, Drawable.IntrinsicHeight));
			}
		}

		public override void SetImageBitmap(Bitmap bm)
		{
			if (bm != null)
			{
				// A bitmap usually is not density aware (unlike resources in drawable-*dpi directories), and preserves it's original size in pixels.
				// To match Windows, we render an image that measures 200px by 200px to 200dp by 200dp.
				// Hence, we consider the physical size of the bitmap to be the logical size of the image.
				UpdateSourceImageSize(new Windows.Foundation.Size(bm.Width, bm.Height), isLogicalPixels: true);
			}

			try
			{
				_skipLayoutRequest = true;

				base.SetImageBitmap(bm);
			}
			finally
			{
				_skipLayoutRequest = false;
			}
		}

		public override void RequestLayout()
		{
			if (_skipLayoutRequest)
			{
				// This is an optimization of the layout system to avoid having the image
				// request a layout of its parent after the image has been set, only based on the condition
				// that the size of the new drawable is different from the previous one.
				// See: http://grepcode.com/file/repository.grepcode.com/java/ext/com.google.android/android/4.4.4_r1/android/widget/ImageView.java#413

				// When the size of the image does not affect its parent size, we can skip 
				// the layout request and convert it to a ForceLayout, which does not invalidate the parent's layout.
				// This optimization is particularly important in ListView templates, where a layout phase
				// is very expensive.

				if (ShouldDowngradeLayoutRequest())
				{
					base.ForceLayout();
					return;
				}
			}

			if (_trace.IsEnabled && !IsLayoutRequested)
			{
				_trace.WriteEvent(
					FrameworkElement.TraceProvider.FrameworkElement_InvalidateMeasure,
					EventOpcode.Send,
					new[] {
						GetType().ToString(),
						this.GetDependencyObjectId().ToString()
					}
				);
			}

			base.RequestLayout();
		}

		private void TryOpenImage()
		{
			var imageSource = Source;

			if (_openedImage == imageSource)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug(this.ToString() + " TryOpenImage - cancelling because Source has not changed");
				}
				return;
			}

			if (imageSource != null && imageSource.UseTargetSize)
			{
				// If the ImageSource has the UseTargetSize set, the image 
				// must not be loaded until the first layout has been done.
				if (!IsLoaded)
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().Debug(this.ToString() + " TryOpenImage - cancelling because view is not loaded");
					}
					return;
				}
			}

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug(this.ToString() + " TryOpenImage - proceeding");
			}

			_openedImage = null;
			_successfullyOpenedImage = null;

			using (
				_imageTrace.WriteEventActivity(
					TraceProvider.Image_SetSourceStart,
					TraceProvider.Image_SetSourceStop,
					new object[] { this.GetDependencyObjectId() }
				)
			)
			{
				try
				{
					if (imageSource is WriteableBitmap wb)
					{
						SetFromWriteableBitmap(wb);
					}
					// We want to reset the image when there is no source provided.
					else if (!imageSource?.HasSource() ?? true)
					{
						ResetSource();
					}
					else if (imageSource.ImageData != null)
					{
						SetSourceBitmap(imageSource);
					}
					else if (imageSource.BitmapDrawable != null)
					{
						SetSourceDrawable(imageSource);
					}
					else if (imageSource.ResourceId.HasValue)
					{
						var dummy = SetSourceResource(imageSource);
					}
					else if (imageSource.FilePath.HasValue() || imageSource.WebUri != null || imageSource.Stream != null)
					{
						// We can't open the image until we have the proper target size, which is 
						// being computed after the layout has been completed.
						// However, if we have already determined that the Image has non-finite bounds (eg because it 
						// has no set Width/Height and is inside control that permits infinite space), then we will never
						// be able to set a targetSize and must load the image without one.
						if (imageSource.UseTargetSize && _targetWidth == null && _targetHeight == null && (_hasFiniteBounds ?? true) && !MustOpenImageToMeasure())
						{
							if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
							{
								this.Log().Debug(this.ToString() + " TryOpenImage - cancelling because view needs to be measured");
							}

							return;
						}

						var dummy = SetSourceUriOrStream(imageSource);
					}
					else
					{
						throw new NotSupportedException("The provided ImageSource is not supported.");
					}

					_openedImage = imageSource;
				}
				catch (OperationCanceledException)
				{
					// We may not have opened the image correctly, try next time.
					Source = null;
				}
				catch (Exception e)
				{
					this.Log().Error("Could not change image source", e);
					OnImageFailed(imageSource);
				}
			}
		}

		private bool MustOpenImageToMeasure()
		{
			// If an image doesn't have fixed sizes and is Uniform or None, we must use its aspect ratio to measure it.
			return (Stretch == Stretch.Uniform || Stretch == Stretch.None) && (double.IsNaN(Width) || double.IsNaN(Height));
		}

		private async Task SetSourceUriOrStream(ImageSource newImageSource)
		{
			// The Jupiter behavior is to reset the visual right away, displaying nothing
			// then show the new image. We're rescheduling the work below, so there is going
			// to be a visual blank displayed.
			ResetSource();

			try
			{
				var disposable = new CancellationDisposable();

				_imageFetchDisposable.Disposable = disposable;

				var bitmap = await newImageSource.Open(disposable.Token, this, _targetWidth, _targetHeight);

				if (newImageSource.IsImageLoadedToUiDirectly)
				{
					// The image may have already been set by the 
					// external loader (Universal Image Loader, most of the time)
					OnImageOpened(newImageSource);
				}
				else
				{
					SetImageBitmap(bitmap);

					if (bitmap != null)
					{
						OnImageOpened(newImageSource);
					}
					else
					{
						OnImageFailed(newImageSource);
					}
				}

				//If a remote image is fetched a second time, it may be set synchronously (eg if the image is cached) within a layout pass (ie from OnLayoutPartial). In this case, we must dispatch RequestLayout for the image control to be measured correctly.
				if (MustDispatchSetSource())
				{
					Dispatch(async ct => RequestLayout());
				}
			}
			catch (Exception ex)
			{
				this.Log().Warn("Image failed to open.", ex);

				OnImageFailed(newImageSource);
			}
		}

		private async Task SetSourceResource(ImageSource newImageSource)
		{
			// The Jupiter behavior is to reset the visual right away, displaying nothing
			// then show the new image. We're rescheduling the work below, so there is going
			// to be a visual blank displayed.
			ResetSource();

			BitmapFactory.Options o = new BitmapFactory.Options();
			o.InJustDecodeBounds = true;
			Bitmap bmp = BitmapFactory.DecodeResource(Resources, newImageSource.ResourceId.Value, o);
			int imageWidth = o.OutWidth;
			int imageHeight = o.OutHeight;

			Func<CancellationToken, Task> setResource = async (ct) =>
			{
				SetImageResource(newImageSource.ResourceId.Value);
				OnImageOpened(newImageSource);
			};

			if (
					// Large images are better off being decoded later.
					(imageWidth > 512 && imageHeight > 512)
					|| MustDispatchSetSource()
				)
			{
				Dispatch(setResource);
			}
			else
			{
				var unused = setResource(CancellationToken.None);
			}
		}

		private void SetSourceDrawable(ImageSource newImageSource)
		{
			if (MustDispatchSetSource())
			{
				Dispatch(ct => SetSourceDrawableAsync(ct, newImageSource));
			}
			else
			{
				var unused = SetSourceDrawableAsync(CancellationToken.None, newImageSource);
			}
		}
		private async Task SetSourceDrawableAsync(CancellationToken ct, ImageSource newImageSource)
		{
			SetImageDrawable(newImageSource.BitmapDrawable);
			OnImageOpened(newImageSource);
		}

		private void SetSourceBitmap(ImageSource newImageSource)
		{
			if (MustDispatchSetSource())
			{
				Dispatch(ct => SetSourceBitmapAsync(ct, newImageSource));
			}
			else
			{
				var unused = SetSourceBitmapAsync(CancellationToken.None, newImageSource);
			}
		}

		private void SetFromWriteableBitmap(WriteableBitmap bitmap)
		{
			var drawableBuffer = new int[bitmap.PixelWidth * bitmap.PixelHeight];
			var sourceBuffer = (bitmap.PixelBuffer as InMemoryBuffer).Data;

			// WriteableBitmap PixelBuffer is using BGRA format, Android's bitmap input buffer
			// requires Argb8888, so we swap bytes to conform to this format.

			for (int i = 0; i < drawableBuffer.Length; i++)
			{
				var a = sourceBuffer[i * 4 + 3];
				var r = sourceBuffer[i * 4 + 2];
				var g = sourceBuffer[i * 4 + 1];
				var b = sourceBuffer[i * 4 + 0];

				drawableBuffer[i] = (a << 24) | (r << 16) | (g << 8) | b;
			}

			var bm = Bitmap.CreateBitmap(drawableBuffer, bitmap.PixelWidth, bitmap.PixelHeight, Bitmap.Config.Argb8888);
			var drawable = new BitmapDrawable(Context.Resources, bm);

			SetImageDrawable(drawable);
			UpdateSourceImageSize(new Windows.Foundation.Size(bm.Width, bm.Height), isLogicalPixels: true);
			OnImageOpened(bitmap);
		}

		private async Task SetSourceBitmapAsync(CancellationToken ct, ImageSource newImageSource)
		{
			SetImageBitmap(newImageSource.ImageData);
			OnImageOpened(newImageSource);
		}

		/// <summary>
		/// If the image control does not have finite bounds, or if finite bounds have not yet been computed, then it must go through
		/// another measure/arrange pass after the image is set. 
		/// This is not guaranteed to happen if RequestLayout is called from within a layout pass, so we must set the image on the dispatcher 
		/// even if we wouldn't otherwise.
		/// </summary>
		/// <returns></returns>
		private bool MustDispatchSetSource()
		{
			return (!_hasFiniteBounds ?? true) && _isInLayout;
		}

		private void ResetSource()
		{
			// Internally, SetImageBitmap calls SetImageDrawable but creates a new BitmapDrawable. 
			// So it's better to use SetImageDrawable.
			SetImageDrawable(null);
		}

		partial void OnStretchChanged(Stretch newValue, Stretch oldValue)
		{
			UpdateMatrix(_lastLayoutSize);
		}

		protected override bool SetFrame(int l, int t, int r, int b)
		{
			var frameSize = new Windows.Foundation.Size(r - l, b - t).PhysicalToLogicalPixels();
			UpdateMatrix(frameSize);

			return base.SetFrame(l, t, r, b);
		}

		/// <summary>
		/// Sets the value of ImageView.ImageMatrix based on Stretch.
		/// </summary>
		/// <param name="frameSize">In logical pixels</param>
		private void UpdateMatrix(Windows.Foundation.Size frameSize)
		{
			SetScaleType(ScaleType.Matrix);

			if (SourceImageSize.Width == 0 || SourceImageSize.Height == 0 || frameSize.Width == 0 || frameSize.Height == 0)
			{
				return;
			}

			// Calculate the resulting space required on screen for the image
			var containerSize = this.MeasureSource(frameSize, SourceImageSize);

			// Calculate the position of the image to follow stretch and alignment requirements
			var sourceRect = this.ArrangeSource(frameSize, containerSize);

			var scaleX = (sourceRect.Width / SourceImageSize.Width) * _sourceImageScale;
			var scaleY = (sourceRect.Height / SourceImageSize.Height) * _sourceImageScale;
			var translateX = ViewHelper.LogicalToPhysicalPixels(sourceRect.X);
			var translateY = ViewHelper.LogicalToPhysicalPixels(sourceRect.Y);

			var matrix = new Android.Graphics.Matrix();
			matrix.PostScale((float)scaleX, (float)scaleY);
			matrix.PostTranslate(translateX, translateY);
			ImageMatrix = matrix;
		}

		partial void HitCheckOverridePartial(ref bool hitCheck)
		{
			hitCheck = Source?.HasSource() ?? false;
		}

		private partial class ImageLayouter
		{
			protected override void MeasureChild(View view, int widthSpec, int heightSpec)
			{
				// No children
			}
		}
	}
}
