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

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
		}
	}
}
