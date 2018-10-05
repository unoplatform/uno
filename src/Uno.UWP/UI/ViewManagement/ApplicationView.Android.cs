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
		internal void SetCoreBounds(Rect visibleBounds)
		{
			VisibleBounds = visibleBounds;

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Updated visible bounds {VisibleBounds}");
			}

			VisibleBoundsChanged?.Invoke(this, null);
		}
	}
}
#endif
