using System;
using System.Collections.Generic;
using System.Text;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Widget;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	internal partial class NativeImageView : ImageView
	{
		private bool _skipLayoutRequest;
		private bool _skipRecolor;
		private bool NeedsRecolor => !_skipRecolor && Owner.MonochromeColor != null;

		private Image Owner => Parent as Image;

		public NativeImageView() : base(ContextHelper.Current) { }

		public override void SetImageDrawable(Drawable drawable)
		{
			if (drawable != null)
			{
				Owner.UpdateSourceImageSize(new global::Windows.Foundation.Size(drawable.IntrinsicWidth, drawable.IntrinsicHeight));
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

			TryRecolorMonochrome();
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
				Owner.UpdateSourceImageSize(new global::Windows.Foundation.Size(Drawable.IntrinsicWidth, Drawable.IntrinsicHeight));
			}

			TryRecolorMonochrome();
		}

		public override void SetImageBitmap(Bitmap bm)
		{
			if (bm != null)
			{
				// A bitmap usually is not density aware (unlike resources in drawable-*dpi directories), and preserves it's original size in pixels.
				// To match Windows, we render an image that measures 200px by 200px to 200dp by 200dp.
				// Hence, we consider the physical size of the bitmap to be the logical size of the image.
				Owner.UpdateSourceImageSize(new global::Windows.Foundation.Size(bm.Width, bm.Height), isLogicalPixels: true);
			}

			try
			{
				_skipLayoutRequest = true;

				if (NeedsRecolor && bm is not null)
				{
					bm = RecolorBitmapMonochrome(bm, Owner.MonochromeColor);
				}

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

				if (Owner.ShouldDowngradeLayoutRequest())
				{
					base.ForceLayout();
					return;
				}
			}

			base.RequestLayout();
		}

		protected override bool SetFrame(int l, int t, int r, int b)
		{
			var frameSize = new global::Windows.Foundation.Size(r - l, b - t).PhysicalToLogicalPixels();
			Owner.UpdateMatrix(frameSize);

			return base.SetFrame(l, t, r, b);
		}

		private void TryRecolorMonochrome()
		{
			if (NeedsRecolor && Drawable is BitmapDrawable)
			{
				Bitmap source = ((BitmapDrawable)Drawable).Bitmap;
				if (source is not null)
				{
					Bitmap target = RecolorBitmapMonochrome(source, Owner.MonochromeColor);

					int actualWidth = Drawable.IntrinsicWidth, actualHeight = Drawable.IntrinsicHeight;
					Bitmap rescaled = Bitmap.CreateScaledBitmap(target, actualWidth, actualHeight, false);

					_skipRecolor = true;
					SetImageBitmap(rescaled);
					_skipRecolor = false;
				}
			}
		}

		private Bitmap RecolorBitmapMonochrome(Bitmap source, Color? color)
		{
			int width = source.Width, height = source.Height;

			Bitmap.Config pixelFormat = Bitmap.Config.Argb8888;

			Bitmap target = Bitmap.CreateBitmap(width, height, pixelFormat);

			int[] sourcePixels = new int[width * height];
			int[] targetPixels = new int[width * height];

			int targetR = color.Value.R;
			int targetG = color.Value.G;
			int targetB = color.Value.B;

			source.GetPixels(sourcePixels, 0, width, 0, 0, width, height);
			for (int i = 0; i < sourcePixels.Length; ++i)
			{
				int sourceColor = sourcePixels[i];
				int targetColor = sourceColor;
				if (sourceColor != 0)
				{
					int sourceAlpha = Android.Graphics.Color.GetAlphaComponent(sourceColor);
					targetColor = new Android.Graphics.Color(targetR, targetG, targetB, sourceAlpha);
				}
				targetPixels[i] = targetColor;
			}
			target.SetPixels(targetPixels, 0, width, 0, 0, width, height);

			return target;
		}
	}
}
