using System;
using Android.Content;
using Android.Graphics;
using Android.Opengl;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Javax.Microedition.Khronos.Egl;

namespace Uno.Media.Playback
{
	[Register("uno.media.playback.VideoSurface")]
	public class VideoSurface : SurfaceView, IVideoSurface
	{
		public VideoSurface(Context context) : base(context)
		{
		}

		/// <summary>
		/// This method will clear the surface view from its last rendered pixels.
		/// This is used to avoid seeing the previous video rendering when setting
		/// a video source to null.
		/// </summary>
		internal void Clear()
		{
#pragma warning disable 618
			// The solution is as described here:
			// https://stackoverflow.com/questions/25660994/clear-video-frame-from-surfaceview-on-video-complete
			if (Holder?.Surface == null)
			{
				return;
			}

			var egl = Javax.Microedition.Khronos.Egl.EGLContext.EGL.JavaCast<IEGL10>();
			var display = egl.EglGetDisplay(EGL10.EglDefaultDisplay);
			egl.EglInitialize(display, null);

			int[] attribList =
			{
				EGL10.EglRedSize, 8,
				EGL10.EglGreenSize, 8,
				EGL10.EglBlueSize, 8,
				EGL10.EglAlphaSize, 8,
				EGL10.EglRenderableType, EGL10.EglWindowBit,
				EGL10.EglNone, 0, // placeholder for recordable [@-3]
				EGL10.EglNone
			};

			var configs = new Javax.Microedition.Khronos.Egl.EGLConfig[1];
			var numConfigs = new int[1];

			egl.EglChooseConfig(display, attribList, configs, configs.Length, numConfigs);
			var config = configs[0];
			var context = egl.EglCreateContext(display, config, EGL10.EglNoContext, new int[] { 12440, 2, EGL10.EglNone });

			var eglSurface = egl.EglCreateWindowSurface(display, config, Holder.Surface, new int[]{ EGL10.EglNone });

			egl.EglMakeCurrent(display, eglSurface, eglSurface, context);
			GLES20.GlClearColor(0, 0, 0, 1);
			GLES20.GlClear(GLES20.GlColorBufferBit);
			egl.EglSwapBuffers(display, eglSurface);
			egl.EglDestroySurface(display, eglSurface);
			egl.EglMakeCurrent(display, EGL10.EglNoSurface, EGL10.EglNoSurface, EGL10.EglNoContext);
			egl.EglDestroyContext(display, context);
			egl.EglTerminate(display);
#pragma warning restore 618
		}
	}
}
