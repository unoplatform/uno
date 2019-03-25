#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Text;
using Android.App;
using Android.Content;
using Android.Util;
using Android.Views;
using Java.Interop;
using Uno.UI;

namespace Windows.Graphics.Display
{
	public sealed partial class DisplayInformation
	{
		partial void Initialize()
		{
			InitializeDisplayProperties();
		}

		private void InitializeDisplayProperties()
		{
			var displayMetrics = new DisplayMetrics();
			var windowManager = ContextHelper.Current.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
			windowManager.DefaultDisplay.GetRealMetrics(displayMetrics);
			LogicalDpi = displayMetrics.Density * 100;
			RawDpiX = displayMetrics.Xdpi;
			RawDpiY = displayMetrics.Ydpi;
			ScreenWidthInRawPixels = (uint)displayMetrics.WidthPixels;
			ScreenHeightInRawPixels = (uint)displayMetrics.HeightPixels;

			double x = Math.Pow(ScreenWidthInRawPixels / displayMetrics.Xdpi, 2);
			double y = Math.Pow(ScreenHeightInRawPixels / displayMetrics.Ydpi, 2);
			double screenInches = Math.Sqrt(x + y);
			DiagonalSizeInInches = screenInches;
		}

		static partial void SetOrientationPartial(DisplayOrientations orientations)
		{
			var currentActivity = ContextHelper.Current as Activity;
			if (currentActivity != null)
			{
				currentActivity.RequestedOrientation = orientations.ToScreenOrientation();
			}
		}

		internal void HandleConfigurationChange(Android.Content.Res.Configuration configuration)
		{

		}

		private DisplayOrientations GetDisplayOrientation(IWindowManager windowManager)
		{
			bool flipped = false;
			if (windowManager.DefaultDisplay.Rotation == SurfaceOrientation.Rotation180 ||
				windowManager.DefaultDisplay.Rotation == SurfaceOrientation.Rotation270)
			{
				flipped = true;
			}
			
		}
	}
}
#endif
