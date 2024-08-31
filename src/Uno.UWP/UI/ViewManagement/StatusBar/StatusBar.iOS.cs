using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UIKit;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;

namespace Windows.UI.ViewManagement
{
	/// <summary>
	/// Provides methods and properties for interacting with the status bar associated with an app view (window). The status bar is a user 
	/// experience that the system presents on the top edge (typically) of the screen that allows users to control behavior of the device 
	/// and can present progress.
	/// </summary>
	/// <remarks>On iOS, UIViewControllerBasedStatusBarAppearance needs to be set to false in the app's Info.plist to be able to 
	/// programmatically manipulate the status bar with <see cref="ShowAsync"/> and <see cref="HideAsync"/>.</remarks>
	public sealed partial class StatusBar
	{
		private void SetStatusBarForegroundType(StatusBarForegroundType foregroundType)
		{
			switch (foregroundType)
			{
				case StatusBarForegroundType.Dark:
					// iOS 13 and above requires explicit configuration of darkContent for light backgrounds statusbar
					// https://developer.apple.com/documentation/uikit/uistatusbarstyle/darkcontent
					UIApplication.SharedApplication.StatusBarStyle = UIDevice.CurrentDevice.CheckSystemVersion(13, 0)
						? UIStatusBarStyle.DarkContent
						: UIStatusBarStyle.Default;
					break;
				case StatusBarForegroundType.Light:
					UIApplication.SharedApplication.StatusBarStyle = UIStatusBarStyle.LightContent;
					break;
			}
		}

		private StatusBarForegroundType GetStatusBarForegroundType()
		{
			switch (UIApplication.SharedApplication.StatusBarStyle)
			{
				case UIStatusBarStyle.BlackOpaque:
				case UIStatusBarStyle.LightContent:
					return StatusBarForegroundType.Light;
				case UIStatusBarStyle.Default:
				default: // The status bar foreground on iOS is dark by default
					return StatusBarForegroundType.Dark;
			}
		}

		public Rect GetOccludedRect()
		{
			var rect = UIApplication.SharedApplication.StatusBarFrame;
			return new Rect(rect.X, rect.Y, rect.Width, rect.Height);
		}

		public IAsyncAction ShowAsync()
		{
			return AsyncAction.FromTask(ct =>
			{
				CoreDispatcher.CheckThreadAccess();
				UIApplication.SharedApplication.StatusBarHidden = false;
				Showing?.Invoke(this, null);
				return Task.CompletedTask;
			});
		}

		public IAsyncAction HideAsync()
		{
			return AsyncAction.FromTask(ct =>
			{
				CoreDispatcher.CheckThreadAccess();
				UIApplication.SharedApplication.StatusBarHidden = true;
				Hiding?.Invoke(this, null);
				return Task.CompletedTask;
			});
		}
	}
}
