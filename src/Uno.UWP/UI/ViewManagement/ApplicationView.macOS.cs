#if __MACOS__
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using AppKit;
using Uno.Extensions;
using Uno.Logging;
using Windows.Foundation;
using CoreGraphics;

namespace Windows.UI.ViewManagement
{
    partial class ApplicationView
	{
		internal void SetCoreBounds(NSWindow keyWindow, Foundation.Rect windowBounds)
		{
            VisibleBounds = windowBounds;

			if(this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Updated visible bounds {VisibleBounds}");
			}

			VisibleBoundsChanged?.Invoke(this, null);
		}

		public string Title
		{
			get
			{
				VerifyKeyWindowInitialized();
				return NSApplication.SharedApplication.KeyWindow.Title;
			}
			set
			{
				VerifyKeyWindowInitialized();
				NSApplication.SharedApplication.KeyWindow.Title = value;
			}
		}

		public bool IsFullScreen
		{
			get
			{
				VerifyKeyWindowInitialized();
				return NSApplication.SharedApplication.KeyWindow.StyleMask.HasFlag(NSWindowStyle.FullScreenWindow);
			}
		}

		public bool TryEnterFullScreenMode()
		{
			if (IsFullScreen)
			{
				return false;
			}
			NSApplication.SharedApplication.KeyWindow.ToggleFullScreen(null);
			return true;
		}

		public void ExitFullScreenMode()
		{
			if (IsFullScreen)
			{
				NSApplication.SharedApplication.KeyWindow.ToggleFullScreen(null);
			}
		}


		public bool TryResizeView(Size value)
		{
			VerifyKeyWindowInitialized();

			var window = NSApplication.SharedApplication.KeyWindow;
			var frame = window.Frame;
			frame.Size = value;
			window.SetFrame(frame, true, true);
			return true;
		}

		public void SetPreferredMinSize(Size minSize)
		{
			VerifyKeyWindowInitialized();

			var window = NSApplication.SharedApplication.KeyWindow;
			window.MinSize = new CGSize(minSize.Width, minSize.Height);
		}

		private void VerifyKeyWindowInitialized([CallerMemberName]string propertyName = null)
		{
			if (NSApplication.SharedApplication.KeyWindow == null)
			{
				throw new InvalidOperationException($"{propertyName} API must be used after KeyWindow is set");
			}
		}
	}
}
#endif
