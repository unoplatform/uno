#nullable enable

using System;
using System.Runtime.InteropServices;
using Gdk;
using Gtk;
using LibVLCSharp.Shared;
using Uno.Extensions;
using Uno.Logging;

namespace Uno.UI.Media
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
		private Rectangle? _lastArrange;

		internal event EventHandler? VideoSurfaceInteraction;

		/// <summary>
		/// GTK VideoView constructor
		/// </summary>
		public VideoView()
		{
			if (OperatingSystem.IsLinux())
			{
#pragma warning disable CA1806 // Do not ignore method results
				Native.XInitThreads();
#pragma warning restore CA1806 // Do not ignore method results
			}

			LibVLCSharp.Shared.Core.Initialize();

			Realized += (s, e) => AttachToWidget();
			Unrealized += (s, e) => DetachFromWidget();
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			DestroyChildWindow();
		}

		internal void SetVisible(bool visible)
		{
			Visible = visible;

			if (Visible)
			{
				if (_videoWindow?.TransientFor is not null)
				{
					if (_lastArrange is { Width: > 1, Height: > 1 })
					{
						if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
						{
							this.Log().Debug($"{GetHashCode():X8} Showing video window");
						}

						// Only show the child window if there's a parent set. If not, the window will
						// appear floating outside the app.
						_videoWindow.Show();
						ApplyLastArrange();
					}
					else
					{
						if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
						{
							this.Log().Debug($"{GetHashCode():X8} Skipping show video window, the parent widget is not arranged");
						}
					}
				}
				else
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"{GetHashCode():X8} Unable to show video window, the parent window is not set");
					}
				}
			}
			else
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"{GetHashCode():X8} Hiding video window");
				}

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

				if (PlatformHelper.IsMac)
				{
					ReportMacOSNotSupported();
					return;
				}

				DestroyChildWindow();
				_mediaPlayer = value;
			}
		}

		private void CreateChildWindow()
		{
			if (_mediaPlayer == null)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"{GetHashCode():X8} Unable to attach player (_mediaPlayer: {_mediaPlayer is not null})");
				}

				return;
			}

			if (PlatformHelper.IsMac)
			{
				ReportMacOSNotSupported();
				return;
			}

			if (_videoWindow is not null)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"{GetHashCode():X8} VideoView is already attached, skipping");
				}
				return;
			}

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"{GetHashCode():X8} Creating Child Window");
			}

			//
			// Creating a child window ensures the video is explicitly rendered on full window
			// even if the window is rendered layered over the existing app. Otherwise, using the current
			// window may render incorrectly (on macOS), or fail to resize properly (on Windows).
			//
			_videoWindow = new($"Window{GetHashCode():X8}");
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

			// Make the video window black (as we're will not be rendering it)
			_videoWindow.AppPaintable = true;

			// Realize the window, so that we can get an ID for it to provide to VLC
			// It is important to create the window without showing it first, otherwise
			// the window will be rendered on top of the app, and will not be able to
			// be moved or resized, or may not be rendered at the right position in the current
			// widget's window.
			_videoWindow.Realize();

			AssignWindowId();
		}

		internal static void ReportMacOSNotSupported()
		{
			if (typeof(VideoView).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
			{
				typeof(VideoView).Log().Error("macOS playback is not supported at this time. https://aka.platform.uno/mediaplayerelement");
			}
		}

		private void AttachToWidget()
		{
			if (IsRealized)
			{
				CreateChildWindow();

				if (_videoWindow is null)
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"{GetHashCode():X8} Skipping AttachToWidget (no video window available)");
					}

					return;
				}

				// Reparent the window to the current window, so it appears inside, positioned outside the bounds of the window
				// to avoid a temporary visual glitch
				_videoWindow.Window.Reparent(Toplevel.Window, Allocation.X, Allocation.Y);

				if (Toplevel is Gtk.Window gtkWindow)
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"{GetHashCode():X8} Setting transient for {Toplevel} ({Allocation.X}x{Allocation.Y};{AllocatedWidth}x{AllocatedHeight})");
					}

					// Set the transient window as well, to be more compatible 
					// with recent GTK versions.
					_videoWindow.TransientFor = gtkWindow;
				}
				else
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"{GetHashCode():X8} Toplevel widget is not a window ({Toplevel})");
					}
				}

				if (Allocation.X != -1 && Allocation.Y != -1)
				{
					// Show the window once the ID has been associated in libVLC, only
					// when the VideoView has been arranged.
					_videoWindow.Show();

					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"{GetHashCode():X8} Showing child player window {Toplevel.Window}");
					}

					ApplyLastArrange();
				}
				else
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"{GetHashCode():X8} Not showing child player window, the parent has not been arranged yet");
					}
				}

			}
		}

		protected override void OnSizeAllocated(Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"{GetHashCode():X8} OnSizeAllocated({allocation.Width}x{allocation.Height})");
			}

			if (_lastArrange is null && allocation is { Width: > 1, Height: > 1 })
			{
				// Store the first valid allocation to avoid the
				// child window to show at a wrong initial position.

				_lastArrange = allocation;

				ApplyLastArrange();
			}
		}

		private void DetachFromWidget()
		{
			if (!IsRealized && _videoWindow is not null)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"{GetHashCode():X8} Hiding child player window");
				}

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

					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"{GetHashCode():X8} Using X11 Window Id {xid}");
					}
				}
				else
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
					{
						this.Log().Error("Unable to get the X11 Window ID. This may be caused when running in a Wayland environment, such as WSLg. https://aka.platform.uno/mediaplayerelement");
					}

					_mediaPlayer.Stop();
				}
			}
			else if (PlatformHelper.IsMac)
			{
				// Not supported at this time, the child window is not
				// properly placing itself in the parent window.
				// _mediaPlayer.NsObject = Native.gdk_quartz_window_get_nsview(_videoWindow.Window.Handle);

				ReportMacOSNotSupported();

				_mediaPlayer.Stop();
			}
			else
			{
				throw new PlatformNotSupportedException();
			}
		}

		private void DestroyChildWindow()
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"{GetHashCode():X8} Destroying child video window");
			}

			RemoveWindowId();

			if (_videoWindow is not null)
			{
				_videoWindow.Hide();
				_videoWindow.Dispose();
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

		private void ApplyLastArrange()
		{
			if (_lastArrange is not null)
			{
				Arrange(_lastArrange.Value);
			}
		}

		internal void Arrange(Gdk.Rectangle value)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"{GetHashCode():X8} Arranging child window to {value.X}x{value.Y} / {value.Width}x{value.Height} (_videoWindow: {_videoWindow is not null}, visible:{_videoWindow?.Visible})");
			}

			_lastArrange = value;

			// Showing the window, even if visible, fixes positioning and sizing issues that may arise
			// during the interactions between the gdk window and the gtk window.
			_videoWindow?.Show();
			_videoWindow?.Window.MoveResize(value.X, value.Y, value.Width, value.Height);
		}
	}
}
