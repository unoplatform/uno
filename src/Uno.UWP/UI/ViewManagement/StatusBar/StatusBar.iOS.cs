using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;
using UIKit;
using Uno.Foundation.Logging;
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
		private bool _isUsingBackgroundColor;

		private void InitializeBackgroundColorObserver()
		{
			NSNotificationCenter.DefaultCenter.AddObserver(
				UIApplication.DidBecomeActiveNotification,
				_ => SetStatusBarBackgroundColor(_backgroundColor)
			);

			NSNotificationCenter.DefaultCenter.AddObserver(
				UIApplication.DidChangeStatusBarOrientationNotification,
				_ => SetStatusBarBackgroundColor(_backgroundColor)
			);
		}

		private void SetStatusBarForegroundType(StatusBarForegroundType foregroundType)
		{
			if (IsBasedStatusBarAppearanceDisabled())
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
			else
			{
				this.Log().Warn("The status bar foreground color couldn't be changed because UIViewControllerBasedStatusBarAppearance is not disabled.");
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

		public void SetStatusBarBackgroundColor(Color? color)
		{
			if (!_isUsingBackgroundColor)
			{
				InitializeBackgroundColorObserver();
				_isUsingBackgroundColor = true;
			}

			// random unique tag to avoid recreating the view
			const int StatusBarViewTag = 38482;
			var (windows, statusBarFrame) = GetWindowsAndStatusBarFrame();
			var removeStatusBar = statusBarFrame.Height == 0;

			foreach (var window in windows)
			{
				var sbar = window.ViewWithTag(StatusBarViewTag) ??
					new UIView(statusBarFrame)
					{
						Tag = StatusBarViewTag,
						AutoresizingMask = UIViewAutoresizing.FlexibleWidth
					};

				if (removeStatusBar)
				{
					sbar.RemoveFromSuperview();
					continue;
				}

				sbar.BackgroundColor = color;
				sbar.TintColor = color;
				window.AddSubview(sbar);
			}
		}

		private bool IsBasedStatusBarAppearanceDisabled()
		{
			var value = NSBundle.MainBundle.ObjectForInfoDictionary("UIViewControllerBasedStatusBarAppearance") as NSNumber;
			return !(value?.BoolValue ?? true); // this property is enabled by default
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

		private static (UIWindow[] Windows, CGRect StatusBarFrame) GetWindowsAndStatusBarFrame()
		{
			if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
			{
				IEnumerable<UIScene> scenes = UIApplication.SharedApplication.ConnectedScenes;
				var currentScene = scenes.FirstOrDefault(n => n.ActivationState == UISceneActivationState.ForegroundActive);

				if (currentScene is not UIWindowScene uiWindowScene)
				{
					// If no active scene is found, this can happen when app is in the background.
					return ([], CGRect.Empty);
				}

				if (uiWindowScene.StatusBarManager is not { } statusBarManager)
				{
					throw new InvalidOperationException("Unable to find a status bar manager.");
				}

				return (uiWindowScene.Windows, statusBarManager.StatusBarFrame);
			}
			else
			{
#pragma warning disable CA1422
				return (UIApplication.SharedApplication.Windows, UIApplication.SharedApplication.StatusBarFrame);
#pragma warning restore CA1422
			}
		}
	}
}
