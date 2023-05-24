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
		private Gdk.Window? _videoWindow;

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

			Realized += (s, e) => Attach();
			Unrealized += (s, e) => Detach();
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

				Detach();
				_mediaPlayer = value;
				Attach();
			}
		}

		private void Attach()
		{
			if (!IsRealized || _mediaPlayer == null)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Unable to attach player (IsRealized:{IsRealized}, _mediaPlayer: {_mediaPlayer is not null})");
				}

				return;
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
			var windowAttributes = new WindowAttr
			{
				WindowType = Gdk.WindowType.Child,
				X = 0,
				Y = 0,
				Width = 0,
				Height = 0,
				Wclass = WindowWindowClass.InputOutput,
				Visual = Screen.Default.RgbaVisual,
				EventMask = (int)EventMask.ExposureMask
			};

			var windowAttributesTypes = WindowAttributesType.X | WindowAttributesType.Y | WindowAttributesType.Visual;

			// Create the child window
			_videoWindow = new(Window.Toplevel, windowAttributes, windowAttributesTypes);
			_videoWindow.SkipTaskbarHint = true;
			_videoWindow.SkipPagerHint = true;

			_videoWindow.Show();

			AssignWindowId();
		}

		private void AssignWindowId()
		{
			if (_mediaPlayer is null || _videoWindow is null)
			{
				return;
			}

			if (PlatformHelper.IsWindows)
			{
				_mediaPlayer.Hwnd = Native.gdk_win32_window_get_handle(_videoWindow.Handle);
			}
			else if (PlatformHelper.IsLinux)
			{
				var xid = Native.gdk_x11_window_get_xid(_videoWindow.Handle);

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
				_mediaPlayer.NsObject = Native.gdk_quartz_window_get_nsview(_videoWindow.Handle);
			}
			else
			{
				throw new PlatformNotSupportedException();
			}
		}

		void Detach()
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

			if (_videoWindow is not null)
			{
				_videoWindow.Hide();
				_videoWindow.Destroy();
				_videoWindow = null;
			}
		}

		internal void Arrange(Gdk.Rectangle value)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Trace))
			{
				this.Log().Trace($"Arranging child window to {value.X}x{value.Y} / {value.Width}x{value.Height}");
			}

			_videoWindow?.MoveResize(value.X, value.Y, value.Width, value.Height);
		}
	}
}
