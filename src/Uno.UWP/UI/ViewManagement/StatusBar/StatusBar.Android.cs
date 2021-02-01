#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Text;
using Android.App;
using Android.Util;
using Android.Views;
using Uno.Extensions;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Core;
using Uno.Logging;
using System.Threading.Tasks;

namespace Windows.UI.ViewManagement
{
	public sealed partial class StatusBar
	{
		private StatusBarForegroundType? _foregroundType;
		private bool? _isShown;

		private void SetStatusBarForegroundType(StatusBarForegroundType foregroundType)
		{
			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
			{
				_foregroundType = foregroundType;
				UpdateSystemUiVisibility();
			}
			else
			{
				this.Log().Warn("The status bar foreground color couldn't be changed. This API is only available starting from Android M (API 23).");
			}
		}

		private StatusBarForegroundType GetStatusBarForegroundType()
		{
			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
			{
				var activity = ContextHelper.Current as Activity;
#pragma warning disable 618
				int uiVisibility = (int)activity.Window.DecorView.SystemUiVisibility;
#pragma warning restore 618

				var isForegroundDark = (int)SystemUiFlags.LightStatusBar == (uiVisibility & (int)SystemUiFlags.LightStatusBar);

				return isForegroundDark
					? StatusBarForegroundType.Dark
					: StatusBarForegroundType.Light;
			}
			else
			{
				// The status bar foreground is always light below Android M (API 23)
				return StatusBarForegroundType.Light;
			}
		}

		public Rect GetOccludedRect()
		{
			var activity = ContextHelper.Current as Activity;

			var occludedRect = new Rect();

			// Height
			int resourceId = activity.Resources.GetIdentifier("status_bar_height", "dimen", "android");
			if (resourceId > 0)
			{
				var physicalStatusBarHeight = activity.Resources.GetDimensionPixelSize(resourceId);
				var logicalStatusBarHeight = PhysicalToLogicalPixels(physicalStatusBarHeight);
				occludedRect.Height = logicalStatusBarHeight;
			}

			// Width
			var physicalStatusBarWidth = activity.Window.DecorView.Width;
			var logicalStatusBarWidth = PhysicalToLogicalPixels(physicalStatusBarWidth);
			occludedRect.Width = logicalStatusBarWidth;

			return occludedRect;
		}

		private double PhysicalToLogicalPixels(int physicalPixels)
		{
			using (DisplayMetrics displayMetrics = Application.Context.Resources.DisplayMetrics)
			{
				return physicalPixels / displayMetrics.Density;
			}
		}

		public IAsyncAction ShowAsync()
		{
			return AsyncAction.FromTask(async ct =>
			{
				CoreDispatcher.CheckThreadAccess();
				_isShown = true;
				UpdateSystemUiVisibility();
				Showing?.Invoke(this, null);
			});
		}

		public IAsyncAction HideAsync()
		{
			return AsyncAction.FromTask(async ct =>
			{
				CoreDispatcher.CheckThreadAccess();
				_isShown = false;
				UpdateSystemUiVisibility();
				Hiding?.Invoke(this, null);
			});
		}

		private void UpdateSystemUiVisibility()
		{
#pragma warning disable 618
			var activity = ContextHelper.Current as Activity;
			var decorView = activity.Window.DecorView;
			var uiOptions = (int)decorView.SystemUiVisibility;
			var newUiOptions = (int)uiOptions;

			if (_isShown.HasValue)
			{
				if (_isShown.Value)
				{
					newUiOptions &= ~(int)SystemUiFlags.Fullscreen;
				}
				else
				{
					newUiOptions |= (int)SystemUiFlags.Fullscreen;
				}
			}

			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
			{
				if (_foregroundType == StatusBarForegroundType.Dark)
				{
					// Dark text to show up on your light status bar
					newUiOptions |= (int)SystemUiFlags.LightStatusBar;
				}
				else if (_foregroundType == StatusBarForegroundType.Light)
				{
					// Light text to show up on your dark status bar
					newUiOptions &= ~(int)SystemUiFlags.LightStatusBar;
				}
			}

			decorView.SystemUiVisibility = (StatusBarVisibility)newUiOptions;
			activity.OnConfigurationChanged(activity.Resources.Configuration);
#pragma warning restore 618
		}
	}
}
#endif
