#if __ANDROID__
using System;
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
		static partial void SetOrientationPartial(DisplayOrientations orientations)
		{
			var currentActivity = ContextHelper.Current as Activity;
			if (currentActivity != null)
			{
				currentActivity.RequestedOrientation = orientations.ToScreenOrientation();
			}
		}

		partial void Initialize() => UpdateProperties();

		private void UpdateProperties()
		{
			using (var realDisplayMetrics = new DisplayMetrics())
			using (var windowManager = ContextHelper.Current.GetSystemService(Context.WindowService).JavaCast<IWindowManager>())
			{
				windowManager.DefaultDisplay.GetRealMetrics(realDisplayMetrics);

				UpdateLogicalProperties(realDisplayMetrics);
				UpdateRawProperties(realDisplayMetrics);
				UpdateNativeOrientation(windowManager);
				UpdateCurrentOrientation(windowManager);
			}
		}

		private void UpdateLogicalProperties(DisplayMetrics realDisplayMetrics)
		{
			// DisplayMetrics of 1.0 matches 100%, or UWP's default 96.0 DPI.
			// https://stuff.mit.edu/afs/sipb/project/android/docs/reference/android/util/DisplayMetrics.html#density
			LogicalDpi = realDisplayMetrics.Density * 96.0f;
			ResolutionScale = (ResolutionScale)(int)(realDisplayMetrics.Density * 100.0);
		}

		private void UpdateRawProperties(DisplayMetrics realDisplayMetrics)
		{
			RawDpiX = realDisplayMetrics.Xdpi;
			RawDpiY = realDisplayMetrics.Ydpi;
			ScreenWidthInRawPixels = (uint)realDisplayMetrics.WidthPixels;
			ScreenHeightInRawPixels = (uint)realDisplayMetrics.HeightPixels;
			RawPixelsPerViewPixel = 1.0f * (int)realDisplayMetrics.DensityDpi / (int)DisplayMetricsDensity.Default;

			var x = Math.Pow(ScreenWidthInRawPixels / realDisplayMetrics.Xdpi, 2);
			var y = Math.Pow(ScreenHeightInRawPixels / realDisplayMetrics.Ydpi, 2);
			var screenInches = Math.Sqrt(x + y);
			DiagonalSizeInInches = screenInches;
		}

		/// <summary>
		/// Sets the NativeOrientation property
		/// </summary>
		/// <param name="windowManager">Window manager</param>
		/// <remarks>
		/// Based on responses in
		/// <see cref="https://stackoverflow.com/questions/4553650/how-to-check-device-natural-default-orientation-on-android-i-e-get-landscape">this SO question</see>
		/// </remarks>
		private void UpdateNativeOrientation(IWindowManager windowManager)
		{
			var orientation = ContextHelper.Current.Resources.Configuration.Orientation;
			if (orientation == Android.Content.Res.Orientation.Undefined)
			{
				NativeOrientation = DisplayOrientations.None;
				return;
			}

			var rotation = windowManager.DefaultDisplay.Rotation;
			bool isLandscape;
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

		/// <summary>
		/// Sets the CurrentOrientation property
		/// </summary>
		/// <param name="windowManager">Window manager</param>
		/// <remarks>
		/// Again quite complicated to do on Android, based on accepted solution at
		/// <see cref="https://stackoverflow.com/questions/10380989/how-do-i-get-the-current-orientation-activityinfo-screen-orientation-of-an-a">this SO question</see>
		/// </remarks>
		private void UpdateCurrentOrientation(IWindowManager windowManager)
		{
			var rotation = windowManager.DefaultDisplay.Rotation;
			using (var displayMetrics = new DisplayMetrics())
			{
				windowManager.DefaultDisplay.GetMetrics(displayMetrics);

				int width = displayMetrics.WidthPixels;
				int height = displayMetrics.HeightPixels;

				if (width == height)
				{
					//square device, can't tell orientation
					CurrentOrientation = DisplayOrientations.None;
					return;
				}

				if (NativeOrientation == DisplayOrientations.Portrait)
				{
					switch (rotation)
					{
						case SurfaceOrientation.Rotation0:
							CurrentOrientation = DisplayOrientations.Portrait;
							break;
						case SurfaceOrientation.Rotation90:
							CurrentOrientation = DisplayOrientations.Landscape;
							break;
						case SurfaceOrientation.Rotation180:
							CurrentOrientation = DisplayOrientations.PortraitFlipped;
							break;
						case SurfaceOrientation.Rotation270:
							CurrentOrientation = DisplayOrientations.LandscapeFlipped;
							break;
						default:
							//invalid rotation
							CurrentOrientation = DisplayOrientations.None;
							break;
					}
				}
				else if (NativeOrientation == DisplayOrientations.Landscape)
				{
					//device is landscape or square
					switch (rotation)
					{
						case SurfaceOrientation.Rotation0:
							CurrentOrientation = DisplayOrientations.Landscape;
							break;
						case SurfaceOrientation.Rotation90:
							CurrentOrientation = DisplayOrientations.Portrait;
							break;
						case SurfaceOrientation.Rotation180:
							CurrentOrientation = DisplayOrientations.LandscapeFlipped;
							break;
						case SurfaceOrientation.Rotation270:
							CurrentOrientation = DisplayOrientations.PortraitFlipped;
							break;
						default:
							//invalid rotation
							CurrentOrientation = DisplayOrientations.None;
							break;
					}
				}
				else
				{
					//fallback
					CurrentOrientation = DisplayOrientations.None;
				}
			}
		}


		internal void HandleConfigurationChange()
		{
			var previousOrientation = CurrentOrientation;
			Initialize();
			if (previousOrientation != CurrentOrientation)
			{
				OrientationChanged?.Invoke(this, CurrentOrientation);
			}
		}
	}
}
#endif
