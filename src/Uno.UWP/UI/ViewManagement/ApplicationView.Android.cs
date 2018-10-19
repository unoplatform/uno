#if __ANDROID__
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
		internal void SetCoreBounds(Rect visibleBounds)
		{
			VisibleBounds = visibleBounds;

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Updated visible bounds {VisibleBounds}");
			}

			VisibleBoundsChanged?.Invoke(this, null);
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
	}
}
#endif
