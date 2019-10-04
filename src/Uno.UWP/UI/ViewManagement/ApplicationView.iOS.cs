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

		internal void SetVisibleBounds(UIKit.UIWindow keyWindow, Foundation.Rect windowBounds)
		{
			var inset = UseSafeAreaInsets
					? keyWindow.SafeAreaInsets
					: UIEdgeInsets.Zero;

			// Not respecting its own documentation. https://developer.apple.com/documentation/uikit/uiview/2891103-safeareainsets?language=objc
			// iOS returns all zeros for SafeAreaInsets on non-iPhones and iOS11. (ignoring nav bars or status bars)
			// So we need to update the top inset depending of the status bar visibility on other devices
			var statusBarHeight = UIApplication.SharedApplication.StatusBarHidden
					? 0
					: UIApplication.SharedApplication.StatusBarFrame.Size.Height;

			inset.Top = (nfloat)Math.Max(inset.Top, statusBarHeight);

			var newVisibleBounds = new Foundation.Rect(
				x: windowBounds.Left + inset.Left,
				y: windowBounds.Top + inset.Top,
				width: windowBounds.Width - inset.Right - inset.Left,
				height: windowBounds.Height - inset.Top - inset.Bottom
			);

			if (VisibleBounds != newVisibleBounds)
			{
				VisibleBounds = newVisibleBounds;

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Updated visible bounds {VisibleBounds}, SafeAreaInsets: {inset}");
				}

				VisibleBoundsChanged?.Invoke(this, null);
			}
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
