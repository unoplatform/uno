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
	}
}
