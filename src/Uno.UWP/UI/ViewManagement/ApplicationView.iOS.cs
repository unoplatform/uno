using System;
using System.Collections.Generic;
using System.Text;
using UIKit;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.UI.Core;

#if NET6_0_OR_GREATER
using ObjCRuntime;
#endif

namespace Windows.UI.ViewManagement
{
	partial class ApplicationView
	{
		private static bool UseSafeAreaInsets => UIDevice.CurrentDevice.CheckSystemVersion(11, 0);

		internal void SetVisibleBounds(UIKit.UIWindow keyWindow, Foundation.Size windowSize)
		{
			var windowBounds = new Foundation.Rect(default, windowSize);

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

			SetVisibleBounds(newVisibleBounds);
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
