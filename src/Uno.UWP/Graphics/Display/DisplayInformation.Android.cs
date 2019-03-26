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

			ResolutionScale = (ResolutionScale)(int)LogicalDpi;
		}

		static partial void SetOrientationPartial(DisplayOrientations orientations)
		{
			var currentActivity = ContextHelper.Current as Activity;
			if (currentActivity != null)
			{
				currentActivity.RequestedOrientation = orientations.ToScreenOrientation();
			}
		}

		/// <summary>
		/// Sets the NativeOrientation property
		/// </summary>
		/// <param name="windowManager">Window manager</param>
		/// <remarks>
		/// Based on responses in
		/// <see cref="https://stackoverflow.com/questions/4553650/how-to-check-device-natural-default-orientation-on-android-i-e-get-landscape">this SO question</see>
		/// </remarks>
		private void SetNativeOrientation(IWindowManager windowManager)
		{
			var orientation = ContextHelper.Current.Resources.Configuration.Orientation;
			if (orientation == Android.Content.Res.Orientation.Undefined)
			{
				NativeOrientation = DisplayOrientations.None;
				return;
			}

			var isLandscape = false;
			var rotation = windowManager.DefaultDisplay.Rotation;			

			switch (rotation)
			{
				case SurfaceOrientation.Rotation0:
				case SurfaceOrientation.Rotation180:
					isLandscape = orientation == Android.Content.Res.Orientation.Landscape;
					break;
				default:
					isLandscape = orientation == Android.Content.Res.Orientation.Portrait;
					break;
			}
			NativeOrientation = isLandscape ? DisplayOrientations.Landscape : DisplayOrientations.Portrait;
		}


		internal void HandleConfigurationChange()
		{

		}

		private DisplayOrientations GetDisplayOrientation(IWindowManager windowManager)
		{
			throw new NotImplementedException();
			//bool flipped = false;
			//if (windowManager.DefaultDisplay.Rotation == SurfaceOrientation.Rotation180 ||
			//	windowManager.DefaultDisplay.Rotation == SurfaceOrientation.Rotation270)
			//{
			//	flipped = true;
			//}

		}
	}
}
#endif
