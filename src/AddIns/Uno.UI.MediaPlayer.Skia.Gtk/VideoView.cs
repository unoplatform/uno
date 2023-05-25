#nullable enable

using System;
using System.Runtime.InteropServices;
using Gdk;
using Gtk;
using LibVLCSharp.Shared;
using Uno.Extensions;
using Uno.Logging;

namespace LibVLCSharp.GTK
{
	/// <summary>
	/// GTK VideoView for Windows, Linux and Mac.
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
			/// Gets the window's X11 ID
			/// </summary>
			[DllImport("libgdk-3.so.0", CallingConvention = CallingConvention.Cdecl)]
			internal static extern uint gdk_x11_window_get_xid(IntPtr gdkWindow);

			/// <summary>
			/// Gets the nsview's handle
			/// </summary>
			/// <remarks>Mac only</remarks>
			/// <param name="gdkWindow">The pointer to the GdkWindow object</param>
			/// <returns>The nsview's handle</returns>
			[DllImport("libgdk-3.0.dylib", CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr gdk_quartz_window_get_nsview(IntPtr gdkWindow);

			/// <summary>
			/// Initializes X11 threads support, as required by LibVLCSharp.
			/// </summary>
			[DllImport("libX11.so")]
			internal static extern int XInitThreads();
		}

		private MediaPlayer? _mediaPlayer;
		private Gtk.Window? _videoWindow;

		internal event EventHandler? VideoSurfaceInteraction;

		/// <summary>
		/// GTK VideoView constructor
		/// </summary>
		public VideoView()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				Native.XInitThreads();
			}

			Core.Initialize();

			Realized += (s, e) => AttachToWidget();
			Unrealized += (s, e) => DetachFromWidget();
		}

		internal void SetVisible(bool visible)
		{
			Visible = visible;

			if (Visible)
			{
				_videoWindow?.Show();
			}
			else
			{
				_videoWindow?.Hide();
			}
		}

		/// <summary>
		/// The MediaPlayer property for that GTK VideoView
		/// </summary>
		public MediaPlayer? MediaPlayer
		{
			get => _mediaPlayer;
			set
			{
				if (ReferenceEquals(_mediaPlayer, value))
				{
					return;
				}

				DestroyChildWindow();
				_mediaPlayer = value;
				CreateChildWindow();
			}
		}

		private void CreateChildWindow()
		{
			if (_mediaPlayer == null)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Unable to attach player (_mediaPlayer: {_mediaPlayer is not null})");
				}

				return;
			}

			if (_videoWindow is not null)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"VideoView is already attached, skipping");
				}
			}

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("Attaching player");
			}

			//
			// Creating a child window ensures the video is explicitly rendered on full window
			// even if the window is rendered layered over the existing app. Otherwise, using the current
			// window may render incorrectly (on macOS), or fail to resize properly (on Windows).
			//
			_videoWindow = new("Window");
			_videoWindow.SkipTaskbarHint = true;
			_videoWindow.SkipPagerHint = true;
			_videoWindow.Decorated = false;

			_videoWindow.Events
				= EventMask.PointerMotionMask
				| EventMask.Button1MotionMask
				| EventMask.Button2MotionMask
				| EventMask.ButtonPressMask
				| EventMask.ButtonReleaseMask
				;

			_videoWindow.ButtonPressEvent += OnVideoWindowButtonPressEvent;
			_videoWindow.MotionNotifyEvent += OnVideoWindowMotionNotifyEvent;

			// Show the window once, so that we can get an ID for it.
			_videoWindow.Show();

			// Hide it immediately so it does not show outside of our own window
			_videoWindow.Hide();

			AssignWindowId();

			AttachToWidget();
		}

		private void AttachToWidget()
		{
			if (IsRealized && _videoWindow is not null)
			{
				// Reparent the window to the current window, so it appears inside.
				_videoWindow.Window.Reparent(Toplevel.Window, 0, 0);

				// Show the window once the ID has been associated in libVLC
				_videoWindow.Show();
			}
		}

		private void DetachFromWidget()
		{
			if (!IsRealized && _videoWindow is not null)
			{
				_videoWindow.Hide();
			}
		}

		private void OnVideoWindowMotionNotifyEvent(object o, MotionNotifyEventArgs args)
			=> VideoSurfaceInteraction?.Invoke(this, EventArgs.Empty);

		private void OnVideoWindowButtonPressEvent(object o, ButtonPressEventArgs args)
			=> VideoSurfaceInteraction?.Invoke(this, EventArgs.Empty);

		private void AssignWindowId()
		{
			if (_mediaPlayer is null || _videoWindow is null)
			{
				return;
			}

			if (PlatformHelper.IsWindows)
			{
				_mediaPlayer.Hwnd = Native.gdk_win32_window_get_handle(_videoWindow.Window.Handle);
			}
			else if (PlatformHelper.IsLinux)
			{
				var xid = Native.gdk_x11_window_get_xid(_videoWindow.Window.Handle);

				if (xid != 0)
				{
					_mediaPlayer.XWindow = xid;
				}
				else
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
					{
						this.Log().Error("Unable to get the X11 Window ID. This may be caused when running in a Wayland environment, such as WSLg.");
					}

					_mediaPlayer.Stop();
				}
			}
			else if (PlatformHelper.IsMac)
			{
				_mediaPlayer.NsObject = Native.gdk_quartz_window_get_nsview(_videoWindow.Window.Handle);
			}
			else
			{
				throw new PlatformNotSupportedException();
			}
		}

		private void DestroyChildWindow()
		{
			RemoveWindowId();

			if (_videoWindow is not null)
			{
				_videoWindow.Hide();
				_videoWindow.Destroy();
				_videoWindow = null;
			}
		}

		private void RemoveWindowId()
		{
			if (_mediaPlayer is not null)
			{
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

		internal void Arrange(Gdk.Rectangle value)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Trace))
			{
				this.Log().Trace($"Arranging child window to {value.X}x{value.Y} / {value.Width}x{value.Height}");
			}

			_videoWindow?.Window.MoveResize(value.X, value.Y, value.Width, value.Height);
		}
	}
}
