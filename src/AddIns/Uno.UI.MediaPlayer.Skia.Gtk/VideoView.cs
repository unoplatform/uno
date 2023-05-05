#nullable enable

using System;
using System.Runtime.InteropServices;
using Gdk;
using Gtk;
using LibVLCSharp.Shared;

namespace LibVLCSharp.GTK
{
	/// <summary>
	/// GTK VideoView for Windows, Linux and Mac.
	/// Mac is currently unsupported (see https://github.com/mono/gtk-sharp/issues/257)
	/// </summary>
	public class VideoView : DrawingArea, IVideoView
	{
		struct Native
		{
			[DllImport("libgdk-3-0.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr gdk_win32_window_get_handle(IntPtr window);

			/// <summary>
			/// Gets the window's HWND
			/// </summary>
			/// <remarks>Window only</remarks>
			/// <param name="gdkWindow">The pointer to the GdkWindow object</param>
			/// <returns>The window's HWND</returns>
			[DllImport("libgdk-3-0.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr gdk_win32_drawable_get_handle(IntPtr gdkWindow);

			/// <summary>
			/// Gets the window's XID
			/// </summary>
			/// <remarks>Linux X11 only</remarks>
			/// <param name="gdkWindow">The pointer to the GdkWindow object</param>
			/// <returns>The window's XID</returns>
			[DllImport("libgdk-x11-2.0.so.0", CallingConvention = CallingConvention.Cdecl)]
			internal static extern uint gdk_x11_drawable_get_xid(IntPtr gdkWindow);

			/// <summary>
			/// Gets the nsview's handle
			/// </summary>
			/// <remarks>Mac only</remarks>
			/// <param name="gdkWindow">The pointer to the GdkWindow object</param>
			/// <returns>The nsview's handle</returns>
			[DllImport("libgdk-quartz-2.0.0.dylib")]
			internal static extern IntPtr gdk_quartz_window_get_nsview(IntPtr gdkWindow);
		}

		private MediaPlayer? _mediaPlayer;

		/// <summary>
		/// GTK VideoView constructor
		/// </summary>
		public VideoView()
		{
			Core.Initialize();

			//            Color black = Color.Zero;
			//#pragma warning disable CS0612 // Type or member is obsolete
			//            Color.Parse("black", ref black);
			//            ModifyBg(StateType.Normal, black);
			//#pragma warning restore CS0612 // Type or member is obsolete

			Realized += (s, e) => Attach();
		}

		/// <summary>
		/// The MediaPlayer property for that GTK VideoView
		/// </summary>
		public MediaPlayer? MediaPlayer
		{
			get
			{
				return _mediaPlayer;
			}
			set
			{
				if (ReferenceEquals(_mediaPlayer, value))
				{
					return;
				}

				Detach();
				_mediaPlayer = value;
				Attach();
			}
		}

		void Attach()
		{
			if (!IsRealized || _mediaPlayer == null)
			{
				return;
			}

			if (PlatformHelper.IsWindows)
			{
				Console.WriteLine($"Player attached ({this.Window.Handle})");
#pragma warning disable CS0618 // Type or member is obsolete
				_mediaPlayer.Hwnd = Native.gdk_win32_window_get_handle(this.Window.Handle);
#pragma warning restore CS0618 // Type or member is obsolete
			}
			else if (PlatformHelper.IsLinux)
			{
				_mediaPlayer.XWindow = Native.gdk_x11_drawable_get_xid(this.Window.Handle);
			}
			else if (PlatformHelper.IsMac)
			{
				_mediaPlayer.NsObject = Native.gdk_quartz_window_get_nsview(this.Window.Handle);
			}
			else
			{
				throw new PlatformNotSupportedException();
			}

		}

		void Detach()
		{
			if (!IsRealized || _mediaPlayer == null)
			{
				return;
			}

			if (PlatformHelper.IsWindows)
			{
				_mediaPlayer.Hwnd = IntPtr.Zero;
			}
			else if (PlatformHelper.IsLinux)
			{
				_mediaPlayer.XWindow = 0;
			}
			else if (PlatformHelper.IsMac)
			{
				_mediaPlayer.NsObject = IntPtr.Zero;
			}
			else
			{
				throw new PlatformNotSupportedException();
			}
		}
	}
}
