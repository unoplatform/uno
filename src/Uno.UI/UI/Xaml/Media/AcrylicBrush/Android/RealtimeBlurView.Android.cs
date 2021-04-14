/*

Implementation based on https://github.com/mmin18/RealtimeBlurView.
with some modifications and removal of unused features.

------------------------------------------------------------------------------

 https://github.com/mmin18/RealtimeBlurView
 Latest commit    82df352     on 24 May 2019

 Copyright 2016 Tu Yimin (http://github.com/mmin18)

 Licensed under the Apache License, Version 2.0 (the "License");
 you may not use this file except in compliance with the License.
 You may obtain a copy of the License at

 http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.

------------------------------------------------------------------------------
 Adapted to csharp by Jean-Marie Alfonsi
------------------------------------------------------------------------------
*/

using System;
using System.Diagnostics;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;

namespace Uno.UI.Xaml.Media
{
	internal partial class RealtimeBlurView : View
	{
		private float mDownsampleFactor = 4;
		private int mOverlayColor; // default #aaffffff
		private float mBlurRadius = 10; // (0 < r <= 25)

		private IBlurImpl mBlurImpl;
		private bool mDirty;
		private Bitmap mBitmapToBlur, mBlurredBitmap;
		private Canvas mBlurringCanvas;
		private bool mIsRendering;
		private Paint mPaint;
		private readonly Rect mRectSrc = new Rect();
		private readonly Rect mRectDst = new Rect();
		// mDecorView should be the root view of the activity (even if you are on a different window like a dialog)
		private View mDecorView;
		// If the view is on different root view (usually means we are on a PopupWindow),
		// we need to manually call invalidate() in onPreDraw(), otherwise we will not be able to see the changes
		private bool mDifferentRoot;
		private static int RENDERING_COUNT;

		private readonly PreDrawListener _preDrawListener;

		public RealtimeBlurView(Context context) : base(context)
		{
			_preDrawListener = new PreDrawListener(this);
			mBlurImpl = GetBlurImplementation(); // provide your own by override getBlurImpl()			
			mBlurRadius = TypedValue.ApplyDimension(ComplexUnitType.Dip, 10, context.Resources.DisplayMetrics);
			//mDownsampleFactor = a.getFloat(R.styleable.RealtimeBlurView_realtimeDownsampleFactor, 4);

			mPaint = new Paint();
		}

		public void SetOverlayColor(int color, bool invalidate = true)
		{
			if (mOverlayColor != color)
			{
				mOverlayColor = color;
				if (invalidate)
				{
					Invalidate();
				}
			}
		}

		protected IBlurImpl GetBlurImplementation() => new AndroidStockBlur();

		private class PreDrawListener : Java.Lang.Object, ViewTreeObserver.IOnPreDrawListener
		{
			private readonly JniWeakReference<RealtimeBlurView> _weakBlurView;

			public PreDrawListener(RealtimeBlurView blurView)
			{
				_weakBlurView = new JniWeakReference<RealtimeBlurView>(blurView);
			}

			public PreDrawListener(IntPtr handle, JniHandleOwnership transfer)
				: base(handle, transfer)
			{
			}

			public bool OnPreDraw()
			{
				if (!_weakBlurView.TryGetTarget(out var blurView))
				{
					return false;
				}

				int[] locations = new int[2];
				Bitmap oldBmp = blurView.mBlurredBitmap;
				View decor = blurView.mDecorView;
				if (decor != null && blurView.IsShown && blurView.Prepare())
				{
					bool redrawBitmap = blurView.mBlurredBitmap != oldBmp;
					oldBmp = null;
					decor.GetLocationOnScreen(locations);
					int x = -locations[0];
					int y = -locations[1];

					blurView.GetLocationOnScreen(locations);
					x += locations[0];
					y += locations[1];

					// just erase transparent
					blurView.mBitmapToBlur.EraseColor(blurView.mOverlayColor & 0xffffff);

					int rc = blurView.mBlurringCanvas.Save();
					blurView.mIsRendering = true;
					RENDERING_COUNT++;
					try
					{
						blurView.mBlurringCanvas.Scale(
							1f * blurView.mBitmapToBlur.Width / blurView.Width,
							1f * blurView.mBitmapToBlur.Height / blurView.Height);
						blurView.mBlurringCanvas.Translate(-x, -y);
						if (decor.Background != null)
						{
							decor.Background.Draw(blurView.mBlurringCanvas);
						}
						decor.Draw(blurView.mBlurringCanvas);
					}
					catch (StopException)
					{
					}
					finally
					{
						blurView.mIsRendering = false;
						RENDERING_COUNT--;
						blurView.mBlurringCanvas.RestoreToCount(rc);
					}

					blurView.Blur(blurView.mBitmapToBlur, blurView.mBlurredBitmap);

					if (redrawBitmap || blurView.mDifferentRoot)
					{
						blurView.Invalidate();
					}
				}

				return true;
			}
		}

		public void SetBlurRadius(float radius)
		{
			if (mBlurRadius != radius)
			{
				mBlurRadius = radius;
				mDirty = true;
				Invalidate();
			}
		}

		public void SetDownsampleFactor(float factor)
		{
			if (factor <= 0)
			{
				throw new ArgumentOutOfRangeException(
					"Downsample factor must be greater than 0.");
			}

			if (mDownsampleFactor != factor)
			{
				mDownsampleFactor = factor;
				mDirty = true; // may also change blur radius
				ReleaseBitmap();
				Invalidate();
			}
		}

		public void SetOverlayColor(int color)
		{
			if (mOverlayColor != color)
			{
				mOverlayColor = color;
				Invalidate();
			}
		}

		private void ReleaseBitmap()
		{
			if (mBitmapToBlur != null)
			{
				mBitmapToBlur.Recycle();
				mBitmapToBlur = null;
			}
			if (mBlurredBitmap != null)
			{
				mBlurredBitmap.Recycle();
				mBlurredBitmap = null;
			}
		}

		protected void Release()
		{
			ReleaseBitmap();
			mBlurImpl.Release();
		}

		protected bool Prepare()
		{
			if (mBlurRadius == 0)
			{
				Release();
				return false;
			}

			float downsampleFactor = mDownsampleFactor;
			float radius = mBlurRadius / downsampleFactor;
			if (radius > 25)
			{
				downsampleFactor = downsampleFactor * radius / 25;
				radius = 25;
			}

			int width = Width;
			int height = Height;

			int scaledWidth = System.Math.Max(1, (int)(width / downsampleFactor));
			int scaledHeight = System.Math.Max(1, (int)(height / downsampleFactor));

			bool dirty = mDirty;

			if (mBlurringCanvas == null || mBlurredBitmap == null
					|| mBlurredBitmap.Width != scaledWidth
					|| mBlurredBitmap.Height != scaledHeight)
			{
				dirty = true;
				ReleaseBitmap();

				bool r = false;
				try
				{
					mBitmapToBlur = Bitmap.CreateBitmap(scaledWidth, scaledHeight, Bitmap.Config.Argb8888);
					if (mBitmapToBlur == null)
					{
						return false;
					}
					mBlurringCanvas = new Canvas(mBitmapToBlur);

					mBlurredBitmap = Bitmap.CreateBitmap(scaledWidth, scaledHeight, Bitmap.Config.Argb8888);
					if (mBlurredBitmap == null)
					{
						return false;
					}

					r = true;
				}
				catch (Java.Lang.OutOfMemoryError)
				{
					// Bitmap.createBitmap() may cause OOM error
					// Simply ignore and fallback
				}
				finally
				{
					if (!r)
					{
						Release();
					}
				}
				if (!r)
				{
					return false;
				}
			}

			if (dirty)
			{
				if (mBlurImpl.Prepare(Context, mBitmapToBlur, radius))
				{
					mDirty = false;
				}
				else
				{
					return false;
				}
			}

			return true;
		}

		protected void Blur(Bitmap bitmapToBlur, Bitmap blurredBitmap)
		{
			mBlurImpl.Blur(bitmapToBlur, blurredBitmap);
		}

		protected View GetActivityDecorView()
		{
			Context ctx = Context;
			for (int i = 0; i < 4 && ctx != null && !(ctx is Android.App.Activity) && ctx is ContextWrapper; i++)
			{
				ctx = ((ContextWrapper)ctx).BaseContext;
			}
			if (ctx is Android.App.Activity)
			{
				return ((Android.App.Activity)ctx).Window.DecorView;
			}
			else
			{
				return null;
			}
		}

		protected override void OnAttachedToWindow()
		{
			base.OnAttachedToWindow();
			mDecorView = GetActivityDecorView();
			if (mDecorView != null)
			{
				mDecorView.ViewTreeObserver.AddOnPreDrawListener(_preDrawListener);
				mDifferentRoot = mDecorView.RootView != RootView;
				if (mDifferentRoot)
				{
					mDecorView.PostInvalidate();
				}
			}
			else
			{
				mDifferentRoot = false;
			}
		}

		//@Override
		protected override void OnDetachedFromWindow()
		{
			if (mDecorView != null)
			{
				mDecorView.ViewTreeObserver.RemoveOnPreDrawListener(_preDrawListener);
			}
			Release();
			base.OnDetachedFromWindow();
		}

		//@Override
		public override void Draw(Canvas canvas)
		{
			if (mIsRendering)
			{
				// Quit here, don't draw views above me
				return;
			}
			else if (RENDERING_COUNT > 0)
			{
				// Doesn't support blurview overlap on another blurview
			}
			else
			{
				base.Draw(canvas);
			}
		}

		protected override void OnDraw(Canvas canvas)
		{
			base.OnDraw(canvas);
			DrawBlurredBitmap(canvas, mBlurredBitmap, mOverlayColor);
		}

		///**
		// * Custom draw the blurred bitmap and color to define your own shape
		// *
		// * @param canvas
		// * @param blurredBitmap
		// * @param overlayColor
		// */
		protected void DrawBlurredBitmap(Canvas canvas, Bitmap blurredBitmap, int overlayColor)
		{
			if (blurredBitmap != null)
			{
				mRectSrc.Right = blurredBitmap.Width;
				mRectSrc.Bottom = blurredBitmap.Height;
				mRectDst.Right = Width;
				mRectDst.Bottom = Height;
				canvas.DrawBitmap(blurredBitmap, mRectSrc, mRectDst, null);
			}
			mPaint.Color = new Color(overlayColor);
			canvas.DrawRect(mRectDst, mPaint);
		}

		public void Destroy()
		{
			if (mDecorView != null)
			{
				mDecorView.ViewTreeObserver.RemoveOnPreDrawListener(_preDrawListener);
			}

			Release();
		}

		private class StopException : Exception
		{
		}
	}
}
