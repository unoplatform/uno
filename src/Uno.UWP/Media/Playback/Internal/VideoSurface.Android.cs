using System;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Uno.Media.Playback
{
	[Register("uno.media.playback.VideoSurface")]
	public class VideoSurface : SurfaceView, IVideoSurface
	{
		private const float MAX_ASPECT_RATIO_DEFORMATION_FRACTION = 0.01f;

		private float _videoAspectRatio;

		public VideoSurface(Context context) : base(context)
		{
		}

		public VideoSurface(Context context, IAttributeSet attrs) : base(context, attrs)
		{
		}

		public VideoSurface(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
		{
		}

		public VideoSurface(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
		{
		}

		protected VideoSurface(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{
		}

		public void SetAspectRatio(float widthHeightRatio)
		{
			//if (_videoAspectRatio != widthHeightRatio)
			//{
			_videoAspectRatio = widthHeightRatio;
			RequestLayout();
			//}
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

			if (_videoAspectRatio == 0)
			{
				return;
			}

			int width = MeasuredWidth;
			int height = MeasuredHeight;
			float viewAspectRatio = (float)width / height;
			float aspectDeformation = _videoAspectRatio / viewAspectRatio - 1;
			if (Math.Abs(aspectDeformation) <= MAX_ASPECT_RATIO_DEFORMATION_FRACTION)
			{
				return;
			}

			if (aspectDeformation > 0)
			{
				height = (int)(width / _videoAspectRatio);
			}
			else
			{
				width = (int)(height * _videoAspectRatio);
			}

			base.OnMeasure(MeasureSpec.MakeMeasureSpec(width, MeasureSpecMode.Exactly), MeasureSpec.MakeMeasureSpec(height, MeasureSpecMode.Exactly));
		}
	}
}
