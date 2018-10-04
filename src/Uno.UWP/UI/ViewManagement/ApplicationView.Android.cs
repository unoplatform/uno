#if __ANDROID__
using Android.App;
using Android.Views;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI;
using Windows.Foundation;
namespace Windows.UI.ViewManagement
{
	partial class ApplicationView
	{
		internal void SetCoreBounds(Rect windowBounds)
		{
			int statusBarHeightDp = 0;

			var activity = ContextHelper.Current as Activity;
			var decorView = activity.Window.DecorView;
			var isStatusBarVisible = ((int)decorView.SystemUiVisibility & (int)SystemUiFlags.Fullscreen) == 0;

			if (isStatusBarVisible)
			{
				int resourceId = Android.Content.Res.Resources.System.GetIdentifier("status_bar_height", "dimen", "android");
				if (resourceId > 0)
				{
					statusBarHeightDp = (int)(Android.Content.Res.Resources.System.GetDimensionPixelSize(resourceId) / Android.App.Application.Context.Resources.DisplayMetrics.Density);
				}
			}

			VisibleBounds = new Foundation.Rect(
				x: windowBounds.Left,
				y: windowBounds.Top,
				width: windowBounds.Width,
				height: windowBounds.Height - statusBarHeightDp
			);

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Updated visible bounds {VisibleBounds}");
			}

			VisibleBoundsChanged?.Invoke(this, null);
		}
	}
}
#endif
