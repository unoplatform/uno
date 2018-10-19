#if __IOS__
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;
using Uno.Extensions;
using Uno.Logging;
using Windows.UI.Core;

namespace Windows.UI.ViewManagement
{
    partial class ApplicationView
	{
		private static bool UseSafeAreaInsets => UIDevice.CurrentDevice.CheckSystemVersion(11, 0);

		internal void SetCoreBounds(UIKit.UIWindow keyWindow, Foundation.Rect windowBounds)
		{
            var statusBarHeight = UIApplication.SharedApplication.StatusBarFrame.Size.Height;

			UIEdgeInsets inset = new UIEdgeInsets(statusBarHeight, 0, 0, 0);
			// Not respecting its own documentation. https://developer.apple.com/documentation/uikit/uiview/2891103-safeareainsets?language=objc
			// iOS returns all zeros for SafeAreaInsets on non-iPhoneX phones. (ignoring nav bars or status bars)
			// For that reason, we will set the window's visible bounds to the SafeAreaInsets only for iPhones with notches,
			// other phones will have insets that consider the status bar
			if (UseSafeAreaInsets)
			{
				if (keyWindow.SafeAreaInsets != UIEdgeInsets.Zero) // if we have a notch
				{
					inset = keyWindow.SafeAreaInsets;
				}
			}

            VisibleBounds = new Foundation.Rect(
				x: windowBounds.Left + inset.Left,
				y: windowBounds.Top + inset.Top,
				width: windowBounds.Width - inset.Right - inset.Left,
				height: windowBounds.Height - inset.Top - inset.Bottom
			);

			if(this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Updated visible bounds {VisibleBounds}, SafeAreaInsets: {inset}");
			}

			VisibleBoundsChanged?.Invoke(this, null);
		}

		public bool TryEnterFullScreenMode()
		{
			CoreDispatcher.CheckThreadAccess();
			UIApplication.SharedApplication.StatusBarHidden = true;
			return UIApplication.SharedApplication.StatusBarHidden;
		}

		public void ExitFullScreenMode()
		{
			CoreDispatcher.CheckThreadAccess();
			UIApplication.SharedApplication.StatusBarHidden = false;
		}
	}
}
#endif
