#if __ANDROID__
using System;
using Android.App;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Window = Windows.UI.Xaml.Window;

namespace Uno.UI.Controls
{
	public static class NavigationBarHelper
	{
		public static bool IsNavigationBarTranslucent
		{
			get
			{
				var activity = ContextHelper.Current as Activity;
				var windowFlags = activity.Window.Attributes.Flags;

				return windowFlags.HasFlag(WindowManagerFlags.TranslucentNavigation)
					|| windowFlags.HasFlag(WindowManagerFlags.LayoutNoLimits);
			}
		}

		public static bool IsNavigationBarVisible
		{
			get
			{
				var activity = ContextHelper.Current as Activity;
				var systemUiVisibility = activity.Window.DecorView.SystemUiVisibility;

				return (Window.Current.SystemUiVisibility & (int)SystemUiFlags.HideNavigation) == 0
					|| ((int)systemUiVisibility & (int)SystemUiFlags.HideNavigation) == 0;
			}
		}

		public static double LogicalNavigationBarHeight
		{
			get
			{
				var metrics = new DisplayMetrics();
				var defaultDisplay = (ContextHelper.Current as Activity)?.WindowManager?.DefaultDisplay;

				if (defaultDisplay == null)
				{
					return 0;
				}

				// [size] realMetrics	: screen 
				// [rect] displayRect	: display area = screen - (bottom: nav_bar) 
				var realMetrics = Get<DisplayMetrics>(defaultDisplay.GetRealMetrics);
				var displayRect = Get<Rect>(defaultDisplay.GetRectSize);

				return realMetrics.HeightPixels > displayRect.Height()
					? ViewHelper.PhysicalToLogicalPixels(realMetrics.HeightPixels - displayRect.Height())
					: 0;

				T Get<T>(Action<T> getter) where T : new()
				{
					var result = new T();
					getter(result);

					return result;
				}
			}
		}

		public static double PhysicalNavigationBarHeight => NavigationBarHelper.LogicalNavigationBarHeight * ViewHelper.Scale;
	}
}
#endif
