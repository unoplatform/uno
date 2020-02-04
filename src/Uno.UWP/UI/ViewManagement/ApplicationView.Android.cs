#if __ANDROID__
using System;
using System.Runtime.CompilerServices;
using Android.App;
using Android.Views;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Core;

namespace Windows.UI.ViewManagement
{
	partial class ApplicationView
	{
		public bool IsScreenCaptureEnabled
		{
			get
			{
				var activity = GetCurrentActivity();
				return !activity.Window.Attributes.Flags.HasFlag(WindowManagerFlags.Secure);
			}
			set
			{
				var activity = GetCurrentActivity();
				if (value)
				{
					activity.Window.ClearFlags(WindowManagerFlags.Secure);
				}
				else
				{
					activity.Window.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
				}
			}
		}

		public string Title
		{
			get
			{
				var activity = GetCurrentActivity();
				return activity.Title;
			}
			set
			{
				var activity = GetCurrentActivity();
				activity.Title = value;
			}
		}

		internal void SetVisibleBounds(Rect newVisibleBounds)
		{
			if (newVisibleBounds != VisibleBounds)
			{
				VisibleBounds = newVisibleBounds;

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Updated visible bounds {VisibleBounds}");
				}

				VisibleBoundsChanged?.Invoke(this, null);
			}
		}

		public bool TryEnterFullScreenMode()
		{
			CoreDispatcher.CheckThreadAccess();
			UpdateFullScreenMode(true);
			return true;
		}

		public void ExitFullScreenMode()
		{
			CoreDispatcher.CheckThreadAccess();
			UpdateFullScreenMode(false);
		}

		private void UpdateFullScreenMode(bool isFullscreen)
		{
			var activity = ContextHelper.Current as Activity;
			var uiOptions = (int)activity.Window.DecorView.SystemUiVisibility;

			if (isFullscreen)
			{
				uiOptions |= (int)SystemUiFlags.Fullscreen;
				uiOptions |= (int)SystemUiFlags.ImmersiveSticky;
				uiOptions |= (int)SystemUiFlags.HideNavigation;
				uiOptions |= (int)SystemUiFlags.LayoutHideNavigation;
			}
			else
			{
				uiOptions &= ~(int)SystemUiFlags.Fullscreen;
				uiOptions &= ~(int)SystemUiFlags.ImmersiveSticky;
				uiOptions &= ~(int)SystemUiFlags.HideNavigation;
				uiOptions &= ~(int)SystemUiFlags.LayoutHideNavigation;
			}

			activity.Window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;
		}


		private Activity GetCurrentActivity([CallerMemberName]string propertyName = null)
		{
			if (!(ContextHelper.Current is Activity activity))
			{
				throw new InvalidOperationException($"{propertyName} API must be called when Activity is created");
			}

			return activity;
		}
	}
}
#endif
