using System;
using System.Collections.Generic;
using System.Text;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Uno.Media.Playback;

namespace Uno.Extensions.ExoMediaPlayer
{
	[Register("uno.extensions.exomediaplayer.VideoSurface")]
	public class VideoSurface : FrameLayout, IVideoSurface
	{
		private const float MAX_ASPECT_RATIO_DEFORMATION_FRACTION = 0.01f;

		private float _videoAspectRatio;

		public VideoSurface(Context context) : base(context)
		{
			var surface = new SurfaceView(Application.Context);
			surface.LayoutParameters = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
			AddView(surface, 0);
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
			_videoAspectRatio = widthHeightRatio;
			RequestLayout();
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

			if (_videoAspectRatio == 0)
			{
				// Aspect ratio not set.
				return;
			}

			int width = MeasuredWidth;
			int height = MeasuredHeight;
			float viewAspectRatio = (float)width / height;
			float aspectDeformation = _videoAspectRatio / viewAspectRatio - 1;
			if (Math.Abs(aspectDeformation) <= MAX_ASPECT_RATIO_DEFORMATION_FRACTION)
			{
				// We're within the allowed tolerance.
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
